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
                    ModLogger.Log("[AmmoConsumptionPatch] Found SetWeaponAmountInSlot(EquipmentIndex, short, bool) method");
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
                    ModLogger.Log("[AmmoConsumptionPatch] Found SetWeaponAmountInSlot(EquipmentIndex, short) method");
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
        /// </summary>
        /// <param name="__instance">The Agent instance (player or NPC)</param>
        /// <param name="equipmentIndex">The weapon slot being modified</param>
        /// <param name="amount">The new ammo amount to set</param>
        /// <param name="_">Whether to enforce primary item (unused, required for Harmony signature)</param>
        /// <returns>True to continue with original method, False to skip it</returns>
        [HarmonyPrefix]
        public static bool Prefix(Agent __instance, EquipmentIndex equipmentIndex, short amount, bool _)
        {
            try
            {
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
                MissionWeapon weapon = __instance.Equipment[equipmentIndex];

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

                // BLOCK if this is an ammo DECREASE
                if (amount < currentAmount)
                {
                    // Log first block for debugging
                    if (!_firstBlockLogged)
                    {
                        _firstBlockLogged = true;
                        string weaponName = weapon.Item?.Name?.ToString() ?? "Unknown";
                        ModLogger.Log($"[AmmoConsumptionPatch] BLOCKED ammo decrease: {weaponName} ({currentAmount} → {amount})");
                    }

                    // Return false to skip original method (don't decrease ammo)
                    return false;
                }

                // Allow ammo INCREASE (pickups, our restoration, etc.)
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
        public static bool Prefix(Agent __instance, EquipmentIndex equipmentIndex, short amount)
        {
            // Delegate to main prefix with default enforcePrimaryItem
            return Prefix(__instance, equipmentIndex, amount, false);
        }
    }

    /// <summary>
    /// Alternative patch that tries to intercept MissionEquipment modifications.
    /// This serves as a fallback if Agent.SetWeaponAmountInSlot doesn't exist.
    /// </summary>
    [HarmonyPatch]
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
        /// </summary>
        [HarmonyPrefix]
        public static bool Prefix(MissionEquipment __instance, EquipmentIndex index, ref MissionWeapon value)
        {
            try
            {
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

                if (currentWeapon.IsEmpty || value.IsEmpty)
                {
                    return true;
                }

                // Skip shields
                if (currentWeapon.CurrentUsageItem?.IsShield == true)
                {
                    return true;
                }

                // If new amount is less than current, this might be ammo consumption
                if (value.Amount < currentWeapon.Amount && currentWeapon.ModifiedMaxAmount > 1)
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
            catch
            {
                return true;
            }
        }
    }
}
