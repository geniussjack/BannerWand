#nullable enable
using BannerWand.Settings;
using BannerWand.Utils;
using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace BannerWand.Patches
{
    /// <summary>
    /// Harmony patch to prevent ammunition consumption for player's ranged weapons.
    /// Intercepts SetWeaponAmountInSlot calls and blocks ammo decrease when cheat is enabled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This patch works by intercepting the SetWeaponAmountInSlot method which is used
    /// by the game to update weapon ammunition counts. When the player has Unlimited Ammo
    /// enabled, we prevent any decrease in ammo count.
    /// </para>
    /// <para>
    /// The patch allows ammo INCREASES (from our restoration code or pickups) but blocks
    /// DECREASES (from shooting/throwing).
    /// </para>
    /// <para>
    /// Additionally, this file contains WeaponAmountPatch which ensures the weapon's Amount
    /// property always reports at least 1 for the player, preventing the "no ammo" check
    /// from blocking shots.
    /// </para>
    /// </remarks>
    [HarmonyPatch]
    public static class AmmoConsumptionPatch
    {
        private static CheatSettings? Settings => CheatSettings.Instance;
        private static CheatTargetSettings? TargetSettings => CheatTargetSettings.Instance;

        /// <summary>
        /// Flag to track if restoration is in progress (to avoid blocking our own restore calls).
        /// </summary>
        internal static bool IsRestorationInProgress { get; set; } = false;

        private static bool _firstBlockLogged = false;

        /// <summary>
        /// Flag indicating whether the main patch was successfully applied.
        /// </summary>
        public static bool IsPatchApplied { get; internal set; } = false;

        /// <summary>
        /// Gets the target method for patching - Agent.SetWeaponAmountInSlot
        /// </summary>
        [HarmonyTargetMethod]
        public static MethodBase? TargetMethod()
        {
            try
            {
                // Find Agent.SetWeaponAmountInSlot(EquipmentIndex, short, bool)
                MethodInfo? method = typeof(Agent).GetMethod(
                    "SetWeaponAmountInSlot",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    [typeof(EquipmentIndex), typeof(short), typeof(bool)],
                    null);

                if (method != null)
                {
                    return method;
                }

                // Try alternative signature (without bool parameter)
                method = typeof(Agent).GetMethod(
                    "SetWeaponAmountInSlot",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    [typeof(EquipmentIndex), typeof(short)],
                    null);

                if (method != null)
                {
                    return method;
                }

                ModLogger.Warning("[AmmoConsumptionPatch] SetWeaponAmountInSlot method not found - searching for alternatives...");

                // Log all Agent methods containing relevant keywords
                foreach (MethodInfo m in typeof(Agent).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    string methodName = m.Name.ToLowerInvariant();
                    if (methodName.Contains("weapon") && (methodName.Contains("amount") || methodName.Contains("ammo") || methodName.Contains("consume")))
                    {
                        ParameterInfo[] parameters = m.GetParameters();
                        string paramStr = string.Join(", ", Array.ConvertAll(parameters, p => $"{p.ParameterType.Name} {p.Name}"));
                        ModLogger.Log($"[AmmoConsumptionPatch] Found related method: {m.Name}({paramStr})");
                    }
                }

                // Also search MissionEquipment class
                ModLogger.Log("[AmmoConsumptionPatch] Searching MissionEquipment methods...");
                foreach (MethodInfo m in typeof(MissionEquipment).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    string methodName = m.Name.ToLowerInvariant();
                    if (methodName.Contains("amount") || methodName.Contains("ammo") || methodName.Contains("consume") || methodName.Contains("set"))
                    {
                        ParameterInfo[] parameters = m.GetParameters();
                        string paramStr = string.Join(", ", Array.ConvertAll(parameters, p => $"{p.ParameterType.Name} {p.Name}"));
                        ModLogger.Log($"[AmmoConsumptionPatch] MissionEquipment method: {m.Name}({paramStr})");
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[AmmoConsumptionPatch] Error finding target method: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Prefix patch that prevents ammo decrease for player when Unlimited Ammo is enabled.
        /// CRITICAL FIX: Instead of blocking the call (return false), we modify the amount parameter
        /// to prevent decrease while still allowing the original method to execute. This prevents
        /// breaking internal game state that might depend on SetWeaponAmountInSlot completing.
        /// </summary>
        /// <param name="__instance">The Agent instance (player or NPC)</param>
        /// <param name="equipmentSlot">The weapon slot being modified</param>
        /// <param name="amount">The new ammo amount to set (modified by ref to prevent decrease)</param>
        /// <param name="enforcePrimaryItem">Whether to enforce primary item (unused, required for Harmony signature)</param>
        /// <returns>True to continue with original method (always true now)</returns>
        [HarmonyPrefix]
        public static bool Prefix(Agent __instance, EquipmentIndex equipmentSlot, ref short amount, bool enforcePrimaryItem)
        {
            try
            {
                // CRITICAL FIX: Only work in missions (battle/combat), NOT on campaign map
                // Agent.SetWeaponAmountInSlot should only be called in missions, but we add
                // this check as a safety measure to prevent any interference with campaign map operations
                if (Mission.Current == null)
                {
                    return true;
                }

                // Allow if settings not loaded
                if (Settings == null || TargetSettings == null)
                {
                    return true;
                }

                // Allow if cheat disabled
                if (!Settings.UnlimitedAmmo || !TargetSettings.ApplyToPlayer)
                {
                    return true;
                }

                // Allow if not player agent
                if (__instance?.IsPlayerControlled != true)
                {
                    return true;
                }

                // Allow if this is our restoration call
                if (IsRestorationInProgress)
                {
                    return true;
                }

                // Get current weapon in slot
                MissionWeapon weapon = __instance.Equipment[equipmentSlot];

                // Allow if weapon is empty
                if (weapon.IsEmpty)
                {
                    return true;
                }

                // Allow if this is a shield (shields don't use Amount for ammo)
                if (weapon.CurrentUsageItem?.IsShield == true)
                {
                    return true;
                }

                // Get current ammo amount
                short currentAmount = weapon.Amount;

                // PREVENT DECREASE by modifying amount parameter instead of blocking call
                if (amount < currentAmount)
                {
                    // Log first block for debugging
                    if (!_firstBlockLogged)
                    {
                        _firstBlockLogged = true;
                        string weaponName = weapon.Item?.Name?.ToString() ?? "Unknown";
                        ModLogger.Log($"[AmmoConsumptionPatch] PREVENTED ammo decrease: {weaponName} ({currentAmount} → {amount}, setting to {currentAmount})");
                    }

                    // Modify amount to prevent decrease, but allow original method to execute
                    amount = currentAmount;
                }

                // Always allow original method to execute (don't block it)
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[AmmoConsumptionPatch] Error in Prefix: {ex.Message}");
                return true; // On error, allow original behavior
            }
        }

        /// <summary>
        /// Alternative Prefix for methods with different signature.
        /// </summary>
        [HarmonyPrefix]
        public static bool Prefix(Agent __instance, EquipmentIndex equipmentSlot, ref short amount)
        {
            // Delegate to main prefix with default enforcePrimaryItem
            return Prefix(__instance, equipmentSlot, ref amount, false);
        }
    }

    /// <summary>
    /// Alternative patch that tries to intercept MissionEquipment modifications.
    /// REMOVED: This patch causes character model corruption because MissionEquipment.set_Item
    /// is used for ALL equipment changes, including visual model updates on campaign map.
    /// Even with careful checks, it interferes with character rendering in menus.
    /// </summary>
    // CLASS REMOVED - was causing character model corruption on campaign map
    /*
    public static class MissionEquipmentAmmoSafetyPatch
    {
        private static CheatSettings? Settings => CheatSettings.Instance;
        private static CheatTargetSettings? TargetSettings => CheatTargetSettings.Instance;

        /// <summary>
        /// Gets the target method - tries to find any method that modifies weapon amounts.
        /// </summary>
        [HarmonyTargetMethod]
        public static MethodBase? TargetMethod()
        {
            try
            {
                // Try to find MissionEquipment indexer setter
                PropertyInfo? indexerProperty = typeof(MissionEquipment).GetProperty("Item",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null, typeof(MissionWeapon), [typeof(EquipmentIndex)], null);

                if (indexerProperty?.GetSetMethod(true) != null)
                {
                    ModLogger.Log("[MissionEquipmentAmmoSafetyPatch] Found MissionEquipment indexer setter");
                    return indexerProperty.GetSetMethod(true);
                }

                ModLogger.Warning("[MissionEquipmentAmmoSafetyPatch] MissionEquipment indexer setter not found");
                return null;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[MissionEquipmentAmmoSafetyPatch] Error finding target: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Prefix that intercepts weapon slot modifications and prevents ammo decrease.
        /// CRITICAL: This patch modifies MissionEquipment.set_Item which is also used for
        /// visual model updates. We must be VERY careful to only modify ammo-related changes.
        /// </summary>
        [HarmonyPrefix]
        public static bool Prefix(MissionEquipment __instance, EquipmentIndex index, ref MissionWeapon value)
        {
            try
            {
                // Only work in missions (battle/combat), not in menus
                if (Mission.Current == null)
                {
                    return true;
                }

                if (Settings == null || TargetSettings == null)
                {
                    return true;
                }

                if (!Settings.UnlimitedAmmo || !TargetSettings.ApplyToPlayer)
                {
                    return true;
                }

                if (AmmoConsumptionPatch.IsRestorationInProgress)
                {
                    return true;
                }

                // Get current weapon
                MissionWeapon currentWeapon = __instance[index];

                // CRITICAL FIX: If current weapon is empty, this is likely initial equipment setup
                // or a weapon switch. DO NOT MODIFY to avoid breaking visual models!
                if (currentWeapon.IsEmpty)
                {
                    return true;
                }

                // CRITICAL FIX: If the new weapon is a completely different item (not just amount change),
                // this is a weapon switch or equipment update, NOT ammo consumption. Allow it!
                if (!value.IsEmpty && value.Item != currentWeapon.Item)
                {
                    return true;
                }

                // CRITICAL FIX: If new weapon is empty, this is unequipping. Allow it!
                if (value.IsEmpty)
                {
                    return true;
                }

                // Skip shields - they don't use ammo
                if (currentWeapon.CurrentUsageItem?.IsShield == true)
                {
                    return true;
                }

                // Skip weapons without ammo capacity (melee weapons, bows without arrows loaded)
                if (currentWeapon.ModifiedMaxAmount <= 1)
                {
                    return true;
                }

                // CRITICAL FIX: Only block ammo decrease if:
                // 1. Same weapon (same Item reference)
                // 2. Amount decreased
                // 3. Has ammo capacity
                if (value.Item == currentWeapon.Item && 
                    value.Amount < currentWeapon.Amount && 
                    currentWeapon.ModifiedMaxAmount > 1)
                {
                    // Create new weapon with original amount to prevent decrease
                    value = new MissionWeapon(
                        value.Item,
                        value.ItemModifier,
                        value.Banner,
                        currentWeapon.Amount);

                    ModLogger.Debug($"[MissionEquipmentAmmoSafetyPatch] Prevented ammo decrease: {currentWeapon.Item?.Name}");
                }

                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Warning($"[MissionEquipmentAmmoSafetyPatch] Error in Prefix: {ex.Message}");
                return true;
            }
        }
    }
    */
}
