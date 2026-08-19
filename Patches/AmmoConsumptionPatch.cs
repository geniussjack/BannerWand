#nullable enable
// System namespaces
// Project namespaces
using BannerWand.Settings;
using BannerWand.Utils;
// Third-party namespaces
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
    /// <para>
    /// IMPORTANT: This patch is applied dynamically only during combat missions
    /// to prevent breaking character models in menus. The patch is applied manually
    /// in OnMissionBehaviorInitialize and removed in OnEndMission.
    /// Do NOT use [HarmonyPatch] attribute on the main patch methods to prevent
    /// automatic application via PatchAll(). OnWeaponAmmoConsume_Prefix is applied
    /// manually in HarmonyManager.ApplyAmmoConsumptionPatch().
    /// </para>
    /// </remarks>
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
        /// Gets the target method for patching - <see cref="Agent.SetWeaponAmountInSlot"/>.
        /// </summary>
        /// <returns>
        /// The <see cref="MethodBase"/> representing <see cref="Agent.SetWeaponAmountInSlot"/> method,
        /// or <c>null</c> if the method cannot be found.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method searches for <see cref="Agent.SetWeaponAmountInSlot"/> with multiple possible signatures:
        /// 1. <c>SetWeaponAmountInSlot(EquipmentIndex, ref short, bool)</c> - Most common signature
        /// 2. <c>SetWeaponAmountInSlot(EquipmentIndex, ref short)</c> - Alternative signature
        /// 3. <c>SetWeaponAmountInSlot(EquipmentIndex, short, bool)</c> - Fallback signature
        /// </para>
        /// <para>
        /// The method uses reflection to find the correct overload based on available parameters.
        /// If no matching method is found, returns <c>null</c> and the patch will not be applied.
        /// </para>
        /// </remarks>
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
        /// Prefix patch that prevents ammo decrease for player when Unlimited Ammo is enabled.
        /// Instead of blocking the call (return false), we modify the amount parameter
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
                // Optimized null checks using pattern matching
                // Combines multiple checks into single expression for better performance
                if (Mission.Current?.MainAgent == null ||
                    __instance?.IsMainAgent != true ||
                    !__instance.IsActive())
                {
                    return true; // No mission, no main agent, or not main agent - skip patch
                }

                // Log that we're processing this call (first time only, to avoid spam)
                if (!_firstBlockLogged)
                {
                    ModLogger.Debug($"[AmmoConsumptionPatch] Prefix called for player agent (Index: {__instance.Index}, Slot: {equipmentSlot}, Amount: {amount})");
                }

                // Allow if cheat disabled
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

                // Allow if weapon is empty or is a shield
                if (weapon.IsEmpty || weapon.CurrentUsageItem?.IsShield == true)
                {
                    return true;
                }

                // Get current ammo amount
                short currentAmount = weapon.Amount;

                // Prevent ammo decrease by modifying amount parameter
                if (amount < currentAmount)
                {
                    // Log first block for debugging (only once to avoid log spam)
                    if (!_firstBlockLogged)
                    {
                        _firstBlockLogged = true;
                        string weaponName = weapon.Item?.Name?.ToString() ?? "Unknown";
                        ModLogger.Log($"[AmmoConsumptionPatch] PREVENTED ammo decrease: {weaponName} ({currentAmount} → {amount}, setting to {currentAmount})");
                    }

                    // Modify amount to current value instead of blocking the call
                    amount = currentAmount;
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
        /// This Prefix is applied manually via HarmonyManager, NOT through PatchAll().
        /// Do NOT add [HarmonyPatch] attribute here, as it would conflict with manual patching.
        /// </remarks>
        [HarmonyPrefix]
#pragma warning disable IDE0060, RCS1163 // enforcePrimaryItem is unused here but must keep this exact name - Harmony binds prefix parameters to the patched method's parameters by name.
        public static bool Prefix_NoRef(Agent __instance, EquipmentIndex equipmentSlot, short amount, bool enforcePrimaryItem)
#pragma warning restore IDE0060, RCS1163
        {
            try
            {
                // Optimized null checks using pattern matching
                if (Mission.Current?.MainAgent == null ||
                    __instance?.IsMainAgent != true ||
                    !__instance.IsActive())
                {
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
        /// <summary>
        /// Blocks ammo consumption invoked from native side via Agent.OnWeaponAmmoConsume callback.
        /// This is the path used by the engine when ammo is actually decremented.
        /// </summary>
        /// <remarks>
        /// This patch is applied manually in HarmonyManager.ApplyAmmoConsumptionPatch(), not via PatchAll().
        /// Do NOT add [HarmonyPatch] attribute here to prevent double-patching.
        /// </remarks>
        [HarmonyPrefix]
        public static bool OnWeaponAmmoConsume_Prefix(Agent __instance, EquipmentIndex slotIndex, short totalAmmo)
        {
            try
            {
                // Safety checks to avoid conflicts with other mods (e.g., Achievement Unblocker)
                if (__instance == null || Mission.Current == null)
                {
                    return true; // Allow original method to run
                }

                // Log only for player in debug to reduce noise
                bool isPlayer = __instance.IsMainAgent && __instance.IsActive();
                if (isPlayer)
                {
                    ModLogger.Debug($"[AmmoConsumptionPatch] OnWeaponAmmoConsume CALLED: Agent={__instance.Index}, Slot={slotIndex}, totalAmmo={totalAmmo}");
                }

                if (!isPlayer)
                {
                    return true;
                }

                if (Settings == null || TargetSettings == null || !Settings.UnlimitedAmmo || !TargetSettings.ApplyToPlayer)
                {
                    return true;
                }

                // Additional safety check - verify agent is still valid
                if (!__instance.IsActive())
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
                    // Set flag to prevent recursive calls from SetWeaponAmountInSlot patch
                    if (__instance.IsActive())
                    {
                        IsRestorationInProgress = true;
                        try
                        {
                            __instance.SetWeaponAmountInSlot(slotIndex, max, true);
                            ModLogger.Debug($"[AmmoConsumptionPatch] BLOCKED ammo consume via OnWeaponAmmoConsume: {current} -> {totalAmmo}, restored to {max}");
                        }
                        finally
                        {
                            IsRestorationInProgress = false;
                        }
                    }
                }
                else
                {
                    ModLogger.Debug($"[AmmoConsumptionPatch] OnWeaponAmmoConsume ignored (already at max): {current} -> {totalAmmo}");
                }

                return false; // skip original
            }
            catch (Exception ex)
            {
                // Enhanced error handling to avoid conflicts with other mods
                ModLogger.Error($"[AmmoConsumptionPatch] Error in OnWeaponAmmoConsume_Prefix: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                // Always return true on error to allow original method to run and avoid breaking other mods
                return true;
            }
        }
    }
}
