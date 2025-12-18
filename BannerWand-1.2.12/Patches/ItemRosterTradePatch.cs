#nullable enable
using BannerWandRetro.Settings;
using BannerWandRetro.Utils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace BannerWandRetro.Patches
{
    /// <summary>
    /// Harmony patch for preventing item loss during all types of trade (towns, villages, caravans, etc.).
    /// Patches ItemRoster.AddToCounts to restore items removed from player's inventory.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This patch uses Prefix to save inventory state and Postfix to restore removed items.
    /// Works for all trade types: town merchants, village traders, caravan merchants, etc.
    /// </para>
    /// <para>
    /// Only applies when:
    /// - TradeItemsNoDecrease cheat is enabled
    /// - ApplyToPlayer is enabled
    /// - The roster belongs to player's party (MobileParty.MainParty)
    /// </para>
    /// </remarks>
    [HarmonyPatch(typeof(ItemRoster), nameof(ItemRoster.AddToCounts), [typeof(ItemObject), typeof(int)])]
    public static class ItemRosterTradePatch
    {
        private static CheatSettings? Settings => CheatSettings.Instance;
        private static CheatTargetSettings? TargetSettings => CheatTargetSettings.Instance;

        /// <summary>
        /// Thread-local backup of player's inventory before AddToCounts is called.
        /// Key: ItemObject, Value: Original amount before operation
        /// </summary>
        [ThreadStatic]
        private static Dictionary<ItemObject, int>? _inventoryBackup;

        /// <summary>
        /// Thread-local flag to track if this is a player inventory operation.
        /// </summary>
        [ThreadStatic]
        private static bool _isPlayerInventory;

        /// <summary>
        /// Thread-local flag to prevent recursive calls during restoration.
        /// </summary>
        [ThreadStatic]
        private static bool _isRestoring;

        /// <summary>
        /// Prefix that saves inventory state before AddToCounts is called.
        /// </summary>
        /// <param name="__instance">The ItemRoster instance.</param>
        /// <param name="item">The item to add/remove.</param>
        /// <param name="number">The amount to add (positive) or remove (negative).</param>
        [HarmonyPrefix]
        public static void Prefix(ItemRoster __instance, ItemObject item, int number)
        {
            try
            {
                // Reset flag
                _isPlayerInventory = false;

                // Skip if we're in the middle of a restoration to prevent recursion
                if (_isRestoring)
                {
                    return;
                }

                // Early exit if settings not configured or cheat not enabled
                if (Settings is null || TargetSettings is null || !Settings.TradeItemsNoDecrease || !TargetSettings.ApplyToPlayer)
                {
                    return;
                }

                // Check if this roster belongs to player's party
                MobileParty? ownerParty = GetOwnerParty(__instance);
                if (ownerParty == null || ownerParty != MobileParty.MainParty)
                {
                    return;
                }

                // This is player's inventory - save state
                _isPlayerInventory = true;

                // Only save backup if removing items (negative number)
                if (number < 0)
                {
                    try
                    {
                        // Rent dictionary from pool
                        _inventoryBackup = ItemBackupPool.Rent();

                        // Save current inventory state for this specific item
                        int currentAmount = __instance.GetItemNumber(item);
                        if (currentAmount > 0)
                        {
                            _inventoryBackup[item] = currentAmount;
                        }
                    }
                    catch (Exception ex)
                    {
                        ModLogger.Error($"[ItemRosterTradePatch] Error saving backup: {ex.Message}");
                        CleanupBackup();
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[ItemRosterTradePatch] Error in Prefix: {ex.Message}");
                CleanupBackup();
            }
        }

        /// <summary>
        /// Postfix that restores items removed from player's inventory.
        /// </summary>
        /// <param name="__instance">The ItemRoster instance.</param>
        /// <param name="item">The item that was added/removed.</param>
        /// <param name="number">The amount that was added (positive) or removed (negative).</param>
        [HarmonyPostfix]
        public static void Postfix(ItemRoster __instance, ItemObject item, int number)
        {
            try
            {
                // Early exit if not player inventory or no backup
                if (!_isPlayerInventory || _inventoryBackup == null)
                {
                    return;
                }

                // Only restore if items were removed (negative number)
                if (number >= 0)
                {
                    CleanupBackup();
                    return;
                }

                // Early exit if settings not configured or cheat not enabled
                if (Settings is null || TargetSettings is null || !Settings.TradeItemsNoDecrease || !TargetSettings.ApplyToPlayer)
                {
                    CleanupBackup();
                    return;
                }

                // Restore removed items with recursion protection
                if (_inventoryBackup.TryGetValue(item, out int originalAmount))
                {
                    int currentAmount = __instance.GetItemNumber(item);
                    if (currentAmount < originalAmount)
                    {
                        int amountToRestore = originalAmount - currentAmount;

                        // Set flag to prevent recursive Prefix/Postfix calls
                        _isRestoring = true;
                        try
                        {
                            _ = __instance.AddToCounts(item, amountToRestore);
                        }
                        finally
                        {
                            _isRestoring = false;
                        }
                    }
                }

                CleanupBackup();
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[ItemRosterTradePatch] Error in Postfix: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                CleanupBackup();
            }
        }

        /// <summary>
        /// Cleans up the backup dictionary by returning it to the pool.
        /// </summary>
        private static void CleanupBackup()
        {
            if (_inventoryBackup != null)
            {
                ItemBackupPool.Return(_inventoryBackup);
                _inventoryBackup = null;
            }
            _isPlayerInventory = false;
        }

        /// <summary>
        /// Gets the MobileParty that owns this ItemRoster.
        /// Uses reflection to access the private OwnerParty property.
        /// </summary>
        private static MobileParty? GetOwnerParty(ItemRoster roster)
        {
            try
            {
                // First check: Compare reference directly (fastest)
                if (MobileParty.MainParty?.ItemRoster == roster)
                {
                    return MobileParty.MainParty;
                }

                // Second check: Try to get OwnerParty property via reflection
                System.Reflection.PropertyInfo? ownerPartyProperty = typeof(ItemRoster).GetProperty(
                    "OwnerParty",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

                if (ownerPartyProperty != null)
                {
                    if (ownerPartyProperty.GetValue(roster) is MobileParty owner)
                    {
                        return owner;
                    }
                }

                // Third check: Try to find party by iterating all parties (fallback)
                if (Campaign.Current?.MobileParties != null)
                {
                    foreach (MobileParty party in Campaign.Current.MobileParties)
                    {
                        if (party?.ItemRoster == roster)
                        {
                            return party;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                ModLogger.Log($"[ItemRosterTradePatch] Error getting owner party: {ex.Message}");
                return null;
            }
        }
    }
}
