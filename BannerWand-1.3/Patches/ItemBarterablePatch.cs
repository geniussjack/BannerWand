#nullable enable
// System namespaces
using System;
using System.Collections.Generic;

// Third-party namespaces
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.BarterSystem.Barterables;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

// Project namespaces
using BannerWand.Settings;
using BannerWand.Utils;

namespace BannerWand.Patches
{
    /// <summary>
    /// Harmony patch for preventing item loss during barter/trade.
    /// Restores items that were removed from player's inventory during barter transactions.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This patch works by:
    /// 1. Prefix: Saving current inventory state before barter is applied
    /// 2. Postfix: Restoring any items that were removed during the barter
    /// </para>
    /// <para>
    /// Only applies when:
    /// - TradeItemsNoDecrease cheat is enabled
    /// - ApplyToPlayer is enabled
    /// - The barter involves player's items (OriginalOwner is player)
    /// </para>
    /// </remarks>
    [HarmonyPatch(typeof(ItemBarterable), nameof(ItemBarterable.Apply))]
    public static class ItemBarterablePatch
    {
        private static CheatSettings? Settings => CheatSettings.Instance;
        private static CheatTargetSettings? TargetSettings => CheatTargetSettings.Instance;

        /// <summary>
        /// Thread-local backup of player's inventory before barter is applied.
        /// Key: ItemObject, Value: Original amount before barter
        /// </summary>
        [ThreadStatic]
        private static Dictionary<ItemObject, int>? _playerItemsBackup;

        /// <summary>
        /// Prefix that saves player's inventory state before barter is applied.
        /// </summary>
        /// <param name="__instance">The ItemBarterable instance being applied.</param>
        [HarmonyPrefix]
        public static void Prefix(ItemBarterable __instance)
        {
            try
            {
                // Early exit if settings not configured
                if (Settings is null || TargetSettings is null)
                {
                    return;
                }

                // Early exit if cheat not enabled
                if (!Settings.TradeItemsNoDecrease || !TargetSettings.ApplyToPlayer)
                {
                    return;
                }

                // Only apply to player's items
                if (__instance.OriginalOwner != Hero.MainHero)
                {
                    ModLogger.Debug($"[ItemBarterablePatch] Prefix: Skipping - OriginalOwner is not player ({__instance.OriginalOwner?.Name?.ToString() ?? "null"})");
                    return;
                }

                ModLogger.Log("[ItemBarterablePatch] Prefix: Processing barter for player item");

                // Early exit if player party not available
                if (MobileParty.MainParty?.ItemRoster is null)
                {
                    return;
                }

                try
                {
                    // Rent dictionary from pool
                    _playerItemsBackup = ItemBackupPool.Rent();

                    ItemRoster itemRoster = MobileParty.MainParty.ItemRoster;
                    int rosterCount = itemRoster.Count;

                    // Save current inventory state
                    // Use GetItemNumber() to get total count across all stacks for each unique item
                    for (int i = 0; i < rosterCount; i++)
                    {
                        ItemRosterElement element = itemRoster.GetElementCopyAtIndex(i);
                        ItemObject? item = element.EquipmentElement.Item;

                        if (item != null && !_playerItemsBackup.ContainsKey(item))
                        {
                            // Get total count for this item across all stacks
                            _playerItemsBackup[item] = itemRoster.GetItemNumber(item);
                        }
                    }

                    ModLogger.Log($"[ItemBarterablePatch] Prefix: Saved backup of {_playerItemsBackup.Count} items before barter (OriginalOwner: {__instance.OriginalOwner?.Name?.ToString() ?? "null"})");
                }
                catch (Exception ex)
                {
                    ModLogger.Error($"[ItemBarterablePatch] Error saving item backup: {ex.Message}");
                    ModLogger.Error($"Stack trace: {ex.StackTrace}");

                    // Cleanup on error
                    if (_playerItemsBackup != null)
                    {
                        ItemBackupPool.Return(_playerItemsBackup);
                        _playerItemsBackup = null;
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[ItemBarterablePatch] Error in Prefix: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Postfix that restores items removed during barter transaction.
        /// </summary>
        /// <param name="__instance">The ItemBarterable instance that was applied.</param>
        [HarmonyPostfix]
        public static void Postfix(ItemBarterable __instance)
        {
            try
            {
                // Early exit if no backup was created
                if (_playerItemsBackup is null)
                {
                    return;
                }

                // Early exit if settings not configured
                if (Settings is null || TargetSettings is null)
                {
                    CleanupBackup();
                    return;
                }

                // Early exit if cheat not enabled
                if (!Settings.TradeItemsNoDecrease || !TargetSettings.ApplyToPlayer)
                {
                    CleanupBackup();
                    return;
                }

                // Only restore player's items
                if (__instance.OriginalOwner != Hero.MainHero)
                {
                    ModLogger.Debug($"[ItemBarterablePatch] Postfix: Skipping - OriginalOwner is not player ({__instance.OriginalOwner?.Name?.ToString() ?? "null"})");
                    CleanupBackup();
                    return;
                }

                ModLogger.Log("[ItemBarterablePatch] Postfix: Processing restoration for player item");

                // Early exit if player party not available
                if (MobileParty.MainParty?.ItemRoster is null)
                {
                    CleanupBackup();
                    return;
                }

                try
                {
                    ItemRoster itemRoster = MobileParty.MainParty.ItemRoster;
                    int itemsRestored = 0;

                    // Restore items that were removed
                    foreach (KeyValuePair<ItemObject, int> backupEntry in _playerItemsBackup)
                    {
                        ItemObject item = backupEntry.Key;
                        int originalAmount = backupEntry.Value;
                        int currentAmount = itemRoster.GetItemNumber(item);

                        // If current amount is less than original, restore the difference
                        if (currentAmount < originalAmount)
                        {
                            int amountToRestore = originalAmount - currentAmount;
                            _ = itemRoster.AddToCounts(item, amountToRestore);
                            itemsRestored++;

                            ModLogger.Debug($"[ItemBarterablePatch] Restored {amountToRestore}× {item.Name} (was {currentAmount}, should be {originalAmount})");
                        }
                    }

                    if (itemsRestored > 0)
                    {
                        ModLogger.Log($"[ItemBarterablePatch] Restored {itemsRestored} item type(s) after barter");
                    }
                }
                catch (Exception ex)
                {
                    ModLogger.Error($"[ItemBarterablePatch] Error restoring items: {ex.Message}");
                    ModLogger.Error($"Stack trace: {ex.StackTrace}");
                }
                finally
                {
                    // Always cleanup backup dictionary
                    CleanupBackup();
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[ItemBarterablePatch] Error in Postfix: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                CleanupBackup();
            }
        }

        /// <summary>
        /// Cleans up the backup dictionary by returning it to the pool.
        /// </summary>
        private static void CleanupBackup()
        {
            if (_playerItemsBackup != null)
            {
                ItemBackupPool.Return(_playerItemsBackup);
                _playerItemsBackup = null;
            }
        }
    }
}
