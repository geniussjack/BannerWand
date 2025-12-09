#nullable enable
using BannerWandRetro.Settings;
using BannerWandRetro.Utils;
using HarmonyLib;
using System;
using System.Reflection;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace BannerWandRetro.Patches
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
    // CRITICAL: Do NOT use [HarmonyPatch] attribute - this patch must be applied dynamically
    // only during combat missions to prevent breaking character models in menus.
    // The patch is applied manually in OnMissionBehaviorInitialize and removed in OnEndMission.
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
                // Find Agent.SetWeaponAmountInSlot with all possible signatures
                // Try with ref short parameter first (most likely in Bannerlord)
                MethodInfo? method = typeof(Agent).GetMethod(
                    "SetWeaponAmountInSlot",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    [typeof(EquipmentIndex), typeof(short).MakeByRefType(), typeof(bool)],
                    null);

                if (method != null)
                {
                    ModLogger.Log("[AmmoConsumptionPatch] Found SetWeaponAmountInSlot(EquipmentIndex, ref short, bool) method");
                    return method;
                }

                // Try with ref short but without bool parameter
                method = typeof(Agent).GetMethod(
                    "SetWeaponAmountInSlot",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    [typeof(EquipmentIndex), typeof(short).MakeByRefType()],
                    null);

                if (method != null)
                {
                    ModLogger.Log("[AmmoConsumptionPatch] Found SetWeaponAmountInSlot(EquipmentIndex, ref short) method");
                    return method;
                }

                // Try without ref (for compatibility with older versions or different signatures)
                method = typeof(Agent).GetMethod(
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

                // Try alternative signature (without bool parameter and without ref)
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
        /// Blocks ammo consumption invoked from native side via Agent.OnWeaponAmmoConsume callback.
        /// This is the path used by the engine when ammo is actually decremented.
        /// </summary>
        [HarmonyPrefix]
        [HarmonyPatch(typeof(Agent), "OnWeaponAmmoConsume")]
#pragma warning disable IDE0060, RCS1163
        public static bool OnWeaponAmmoConsume_Prefix(Agent __instance, EquipmentIndex slotIndex, short totalAmmo)
#pragma warning restore IDE0060, RCS1163
        {
            try
            {
                // Log only for player in debug to reduce noise
                bool isPlayer = __instance?.IsMainAgent == true && __instance?.IsActive() == true;
                if (isPlayer)
                {
                    ModLogger.Debug($"[AmmoConsumptionPatch] OnWeaponAmmoConsume CALLED: Agent={__instance?.Index}, Slot={slotIndex}, totalAmmo={totalAmmo}");
                }

                if (!isPlayer)
                {
                    return true;
                }

                if (Settings == null || TargetSettings == null || !Settings.UnlimitedAmmo || !TargetSettings.ApplyToPlayer)
                {
                    return true;
                }

                MissionWeapon weapon = __instance.Equipment[slotIndex];
                if (weapon.IsEmpty || weapon.CurrentUsageItem?.IsShield == true)
                {
                    return true;
                }

                short current = weapon.Amount;
                short max = weapon.ModifiedMaxAmount;

                if (current < max)
                {
                    // Restore to max using the native method that normally updates ammo when picking up
                    __instance.SetWeaponAmountInSlot(slotIndex, max, true);
                    ModLogger.Debug($"[AmmoConsumptionPatch] BLOCKED ammo consume via OnWeaponAmmoConsume: {current} -> {totalAmmo}, restored to {max}");
                }
                else
                {
                    ModLogger.Debug($"[AmmoConsumptionPatch] OnWeaponAmmoConsume ignored (already at max): {current} -> {totalAmmo}");
                }

                return false; // skip original
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[AmmoConsumptionPatch] Error in OnWeaponAmmoConsume_Prefix: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return true;
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
        /// <returns>False to skip original method when blocking ammo decrease, true otherwise</returns>
        [HarmonyPrefix]
#pragma warning disable IDE0060, RCS1163 // Remove unused parameter - required for Harmony signature match
        public static bool Prefix(Agent __instance, EquipmentIndex equipmentSlot, ref short amount, bool enforcePrimaryItem)
#pragma warning restore IDE0060, RCS1163
        {
            try
            {
                // CRITICAL FIX: Only work in ACTIVE COMBAT MISSIONS, NOT in menus or character rendering
                // The problem: SetWeaponAmountInSlot is called even in menus for character model rendering.
                // Even if we return true (skip), the fact that the patch is applied can break character models.
                // Solution: Add strict checks to ensure we're in an actual combat mission, not just any mission.

                // First check: Mission must exist
                Mission? currentMission = Mission.Current;
                if (currentMission == null)
                {
                    return true; // No mission = menu/campaign map, skip patch
                }

                // Second check: Must have a main agent (player in combat)
                // In menus, there's no main agent, so this check prevents menu interference
                if (currentMission.MainAgent == null)
                {
                    return true; // No main agent = menu rendering, skip patch
                }

                // Third check: The agent must be the main agent (player)
                // Use IsMainAgent property for reliable main agent detection
                // This is more reliable than Index comparison or IsPlayerControlled check
                if (__instance?.IsMainAgent != true || __instance?.IsActive() != true)
                {
                    return true; // Not main agent or not active, skip patch
                }

                // Log that we're processing this call (first time only, to avoid spam)
                if (!_firstBlockLogged)
                {
                    ModLogger.Debug($"[AmmoConsumptionPatch] Prefix called for player agent (Index: {__instance.Index}, Slot: {equipmentSlot}, Amount: {amount})");
                }

                // Allow if cheat disabled
                // Note: We already checked that __instance is the main agent (player) above,
                // so we don't need to check IsPlayerControlled again
                if (Settings == null || TargetSettings == null || !Settings.UnlimitedAmmo || !TargetSettings.ApplyToPlayer)
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

                // CRITICAL FIX: Instead of blocking the call (return false), modify the amount parameter
                // to prevent decrease while still allowing the original method to execute.
                // This prevents breaking internal game state that might depend on SetWeaponAmountInSlot completing.
                // Blocking the call can corrupt character models in menus (inventory, party, clan).
                if (amount < currentAmount)
                {
                    // Log first block for debugging
                    if (!_firstBlockLogged)
                    {
                        _firstBlockLogged = true;
                        string weaponName = weapon.Item?.Name?.ToString() ?? "Unknown";
                        ModLogger.Log($"[AmmoConsumptionPatch] PREVENTED ammo decrease (ref version): {weaponName} ({currentAmount} → {amount}, setting to {currentAmount})");
                    }

                    // CRITICAL FIX: Modify amount to current value instead of blocking the call
                    // This allows SetWeaponAmountInSlot to execute and update internal state correctly,
                    // while preventing the actual ammo decrease. This fixes character model corruption.
                    amount = currentAmount;
                }
                else if (amount > currentAmount)
                {
                    // Log ammo increase for debugging (first time only)
                    if (!_firstBlockLogged)
                    {
                        _firstBlockLogged = true;
                        string weaponName = weapon.Item?.Name?.ToString() ?? "Unknown";
                        ModLogger.Debug($"[AmmoConsumptionPatch] Allowing ammo increase: {weaponName} ({currentAmount} → {amount})");
                    }
                }

                // Always allow original method to execute (with potentially modified amount)
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[AmmoConsumptionPatch] Error in Prefix: {ex.Message}");
                return true; // On error, allow original behavior
            }
        }

        /// <summary>
        /// Alternative Prefix for methods with different signature (without bool parameter).
        /// </summary>
        [HarmonyPrefix]
        public static bool Prefix(Agent __instance, EquipmentIndex equipmentSlot, ref short amount)
        {
            // Delegate to main prefix with default enforcePrimaryItem
            return Prefix(__instance, equipmentSlot, ref amount, false);
        }

        /// <summary>
        /// Alternative Prefix for methods WITHOUT ref parameter (for compatibility).
        /// This is used when the game method signature is SetWeaponAmountInSlot(EquipmentIndex, short, bool)
        /// instead of SetWeaponAmountInSlot(EquipmentIndex, ref short, bool).
        /// </summary>
        /// <remarks>
        /// CRITICAL: This Prefix is applied manually via HarmonyManager, NOT through PatchAll().
        /// Do NOT add [HarmonyPatch] attribute here, as it would conflict with manual patching.
        /// </remarks>
        [HarmonyPrefix]
#pragma warning disable IDE0060, RCS1163 // Remove unused parameter - required for Harmony signature match
        public static bool Prefix_NoRef(Agent __instance, EquipmentIndex equipmentSlot, short amount, bool enforcePrimaryItem)
#pragma warning restore IDE0060, RCS1163
        {
            // If method doesn't have ref parameter, we can't modify it directly
            // Instead, we need to block the call (return false) when we want to prevent decrease
            // But this can break character models, so we only do it in combat missions
            
            try
            {
                // Only log in debug to avoid spam
                ModLogger.Debug($"[AmmoConsumptionPatch] Prefix_NoRef CALLED: Agent={__instance?.Index}, Slot={equipmentSlot}, Amount={amount}, EnforcePrimary={enforcePrimaryItem}");
                
                // Same strict checks as main Prefix
                Mission? currentMission = Mission.Current;
                if (currentMission == null || currentMission.MainAgent == null)
                {
                    ModLogger.Debug($"[AmmoConsumptionPatch] Prefix_NoRef: Early exit - no mission or main agent");
                    return true;
                }

                // Use IsMainAgent property for reliable main agent detection
                // This is more reliable than Index comparison or IsPlayerControlled check
                if (__instance?.IsMainAgent != true || __instance?.IsActive() != true)
                {
                    ModLogger.Debug($"[AmmoConsumptionPatch] Prefix_NoRef: Early exit - not main agent (IsMainAgent={__instance?.IsMainAgent}, IsActive={__instance?.IsActive()})");
                    return true;
                }

                // Log that we're processing this call (first time only, to avoid spam)
                if (!_firstBlockLogged)
                {
                    ModLogger.Debug($"[AmmoConsumptionPatch] Prefix_NoRef called for player agent (Index: {__instance.Index}, Slot: {equipmentSlot}, Amount: {amount})");
                }

                if (Settings == null || TargetSettings == null || !Settings.UnlimitedAmmo || !TargetSettings.ApplyToPlayer)
                {
                    return true;
                }

                if (IsRestorationInProgress)
                {
                    return true;
                }

                MissionWeapon weapon = __instance.Equipment[equipmentSlot];
                if (weapon.IsEmpty || weapon.CurrentUsageItem?.IsShield == true)
                {
                    return true;
                }

                short currentAmount = weapon.Amount;

                // If amount is decreasing, block the call to prevent decrease
                // WARNING: This can break character models if called in menus, but we have strict checks above
                if (amount < currentAmount)
                {
                    if (!_firstBlockLogged)
                    {
                        _firstBlockLogged = true;
                        string weaponName = weapon.Item?.Name?.ToString() ?? "Unknown";
                        ModLogger.Debug($"[AmmoConsumptionPatch] BLOCKED ammo decrease (no-ref version): {weaponName} ({currentAmount} → {amount})");
                    }
                    return false; // Block the call to prevent decrease
                }

                return true; // Allow increase or same amount
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[AmmoConsumptionPatch] Error in Prefix (no-ref): {ex.Message}");
                return true;
            }
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

