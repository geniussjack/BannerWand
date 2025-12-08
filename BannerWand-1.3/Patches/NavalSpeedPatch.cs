#nullable enable
using BannerWand.Settings;
using BannerWand.Utils;
using HarmonyLib;
using System;
using System.IO;
using System.Reflection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;

namespace BannerWand.Patches
{
    /// <summary>
    /// Harmony patch to support War Sails DLC (NavalDLC) speed calculations.
    /// Patches NavalDLCPartySpeedCalculationModel to apply movement speed multiplier
    /// for both land and sea travel, ensuring speed display updates correctly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// When War Sails DLC is active, the game uses NavalDLCPartySpeedCalculationModel
    /// instead of DefaultPartySpeedCalculatingModel. This patch ensures that the
    /// movement speed multiplier is applied correctly for both land and sea travel.
    /// </para>
    /// <para>
    /// The patch intercepts CalculateBaseSpeed and CalculateFinalSpeed methods
    /// to apply the speed boost after the base calculation, preserving all
    /// naval-specific modifiers (wind, crew, etc.).
    /// </para>
    /// </remarks>
    [HarmonyPatch]
    public static class NavalSpeedPatch
    {
        private static CheatSettings? Settings => CheatSettings.Instance;
        private static CheatTargetSettings? TargetSettings => CheatTargetSettings.Instance;

        /// <summary>
        /// Tracks the last desired speed value to detect changes for logging.
        /// </summary>
        private static float _lastDesiredSpeed = 0f;

        /// <summary>
        /// Text object for speed override description (cached to avoid allocations).
        /// </summary>
        private static readonly TextObject SpeedOverrideText = new("BannerWand Speed Override");

        /// <summary>
        /// Checks if NavalDLC folder exists in the Modules directory.
        /// This is a supplementary check for logging/debugging purposes.
        /// The primary check is via Type.GetType() which verifies the DLL is actually loaded.
        /// </summary>
        /// <returns>True if NavalDLC folder exists, false otherwise.</returns>
        private static bool CheckNavalDlcFolderExists()
        {
            try
            {
                // Get the Modules directory path from our assembly location
                // DLL is in: [GamePath]\Modules\BannerWand\bin\Win64_Shipping_Client\BannerWand.dll
                // Modules path is: [GamePath]\Modules\
                Assembly executingAssembly = Assembly.GetExecutingAssembly();
                string? assemblyLocation = executingAssembly.Location;

                if (string.IsNullOrEmpty(assemblyLocation))
                {
                    return false;
                }

                // Navigate up to Modules directory
                // From: ...\Modules\BannerWand\bin\Win64_Shipping_Client\BannerWand.dll
                // To:   ...\Modules\
                string? dllDirectory = Path.GetDirectoryName(assemblyLocation);
                if (string.IsNullOrEmpty(dllDirectory))
                {
                    return false;
                }

                // Navigate up: bin\Win64_Shipping_Client -> bin -> BannerWand -> Modules
                string? moduleDirectory = Path.GetDirectoryName(Path.GetDirectoryName(dllDirectory));
                if (string.IsNullOrEmpty(moduleDirectory))
                {
                    return false;
                }

                string? modulesDirectory = Path.GetDirectoryName(moduleDirectory);
                if (string.IsNullOrEmpty(modulesDirectory))
                {
                    return false;
                }

                // Check if NavalDLC folder exists
                string navalDlcPath = Path.Combine(modulesDirectory, "NavalDLC");
                return Directory.Exists(navalDlcPath);
            }
            catch
            {
                // If we can't check, assume it doesn't exist (safe default)
                return false;
            }
        }

        /// <summary>
        /// Gets the target method for patching - NavalDLCPartySpeedCalculationModel.CalculateFinalSpeed
        /// </summary>
        [HarmonyTargetMethod]
        public static MethodBase? TargetMethod()
        {
            try
            {
                // First, check if NavalDLC folder exists in Modules directory (for logging/debugging)
                bool navalDlcFolderExists = CheckNavalDlcFolderExists();

                // Try to find NavalDLCPartySpeedCalculationModel class via reflection
                // First try Type.GetType() - this works if the assembly is already loaded
                Type? navalSpeedModelType = Type.GetType("NavalDLC.GameComponents.NavalDLCPartySpeedCalculationModel, NavalDLC");

                // If Type.GetType() fails, try searching through loaded assemblies
                // This is necessary because DLC may load after OnSubModuleLoad
                if (navalSpeedModelType == null)
                {
                    foreach (System.Reflection.Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            if (assembly.GetName().Name == "NavalDLC")
                            {
                                navalSpeedModelType = assembly.GetType("NavalDLC.GameComponents.NavalDLCPartySpeedCalculationModel");
                                if (navalSpeedModelType != null)
                                {
                                    ModLogger.Log("[NavalSpeedPatch] Found NavalDLCPartySpeedCalculationModel via assembly search");
                                    break;
                                }
                            }
                        }
                        catch
                        {
                            // Ignore errors when checking assemblies
                        }
                    }
                }

                if (navalSpeedModelType == null)
                {
                    // NavalDLC not available (either not installed or not loaded)
                    if (navalDlcFolderExists)
                    {
                        ModLogger.Log("[NavalSpeedPatch] NavalDLC folder found but DLL not loaded - DLC may be disabled in launcher or not yet loaded");
                    }
                    else
                    {
                        ModLogger.Log("[NavalSpeedPatch] NavalDLC not found - War Sails DLC is not installed");
                    }
                    return null;
                }

                // Find CalculateFinalSpeed method
                MethodInfo? method = navalSpeedModelType.GetMethod(
                    "CalculateFinalSpeed",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    [typeof(MobileParty), typeof(ExplainedNumber)],
                    null);

                if (method != null)
                {
                    ModLogger.Log("[NavalSpeedPatch] Found NavalDLCPartySpeedCalculationModel.CalculateFinalSpeed method");
                    return method;
                }

                ModLogger.Warning("[NavalSpeedPatch] NavalDLCPartySpeedCalculationModel.CalculateFinalSpeed method not found");
                return null;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[NavalSpeedPatch] Error finding CalculateFinalSpeed method: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Gets the target method for patching CalculateBaseSpeed - NavalDLCPartySpeedCalculationModel.CalculateBaseSpeed
        /// </summary>
        public static MethodBase? TargetCalculateBaseSpeedMethod()
        {
            try
            {
                // Try to find NavalDLCPartySpeedCalculationModel class via reflection
                // First try Type.GetType() - this works if the assembly is already loaded
                Type? navalSpeedModelType = Type.GetType("NavalDLC.GameComponents.NavalDLCPartySpeedCalculationModel, NavalDLC");

                // If Type.GetType() fails, try searching through loaded assemblies
                if (navalSpeedModelType == null)
                {
                    foreach (System.Reflection.Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            if (assembly.GetName().Name == "NavalDLC")
                            {
                                navalSpeedModelType = assembly.GetType("NavalDLC.GameComponents.NavalDLCPartySpeedCalculationModel");
                                if (navalSpeedModelType != null)
                                {
                                    break;
                                }
                            }
                        }
                        catch
                        {
                            // Ignore errors when checking assemblies
                        }
                    }
                }

                if (navalSpeedModelType == null)
                {
                    return null;
                }

                // Find CalculateBaseSpeed method
                MethodInfo? method = navalSpeedModelType.GetMethod(
                    "CalculateBaseSpeed",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    [typeof(MobileParty), typeof(bool), typeof(int), typeof(int)],
                    null);

                if (method != null)
                {
                    ModLogger.Log("[NavalSpeedPatch] Found NavalDLCPartySpeedCalculationModel.CalculateBaseSpeed method");
                    return method;
                }

                ModLogger.Warning("[NavalSpeedPatch] NavalDLCPartySpeedCalculationModel.CalculateBaseSpeed method not found");
                return null;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[NavalSpeedPatch] Error finding CalculateBaseSpeed method: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Postfix patch that applies movement speed multiplier after base speed calculation.
        /// This ensures the speed boost is applied for naval base speed calculations.
        /// </summary>
        /// <param name="__instance">The NavalDLCPartySpeedCalculationModel instance</param>
        /// <param name="party">The mobile party (parameter name must match original method: "party")</param>
        /// <param name="__result">The calculated base speed (modified by ref)</param>
        [HarmonyPostfix]
        public static void CalculateBaseSpeed_Postfix(object __instance, MobileParty party, ref ExplainedNumber __result)
        {
            try
            {
                // Early exit for null party or invalid instance
                if (party == null || __instance == null)
                {
                    return;
                }

                // Additional safety check: verify that __instance is actually from NavalDLC
                if (__instance.GetType().Namespace != "NavalDLC.GameComponents")
                {
                    return;
                }

                // Only apply for sea travel (when IsCurrentlyAtSea is true)
                // For land travel, the base model will handle it
                if (!party.IsCurrentlyAtSea)
                {
                    return;
                }

                // DLC FIX: The DLC model's CalculateNavalBaseSpeed has already been called
                // and the result is in __result. The fix is in CustomPartySpeedModel which calls
                // base.BaseModel correctly, ensuring this method is called for ALL parties at sea.
                // No modification needed here - the DLC model already calculates correctly.
                // This ensures the fix works for ALL parties at sea, not just the player.

                // Apply speed override cheat if enabled (applies to all parties on the map)
                // This is a CHEAT feature, separate from the DLC fix
                if (Settings != null && TargetSettings != null && ShouldApplyPlayerSpeedOverride(party))
                {
                    ApplyPlayerSpeedBoost(ref __result);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[NavalSpeedPatch] Error in CalculateBaseSpeed_Postfix: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Postfix patch that applies movement speed multiplier after final speed calculation.
        /// This ensures the speed boost is applied after all naval modifiers (wind, crew, etc.).
        /// </summary>
        /// <param name="__instance">The NavalDLCPartySpeedCalculationModel instance</param>
        /// <param name="mobileParty">The mobile party</param>
        /// <param name="__result">The calculated final speed (modified by ref)</param>
        [HarmonyPostfix]
        public static void CalculateFinalSpeed_Postfix(object __instance, MobileParty mobileParty, ref ExplainedNumber __result)
        {
            try
            {
                // Early exit for null party or invalid instance
                if (mobileParty == null || __instance == null)
                {
                    return;
                }

                // Additional safety check: verify that __instance is actually from NavalDLC
                // This prevents the patch from running if NavalDLC is not available
                // (though TargetMethod() should already prevent this, this is a double-check)
                if (__instance.GetType().Namespace != "NavalDLC.GameComponents")
                {
                    return;
                }

                // DLC FIX: The DLC model's CalculateFinalSpeed has already been called
                // and added all naval modifiers (wind, crew, terrain, etc.) to __result.
                // The fix is in CustomPartySpeedModel which calls base.BaseModel correctly,
                // ensuring this method is called for ALL parties at sea.
                // No modification needed here - the DLC model already calculates correctly.
                // This ensures the fix works for ALL parties at sea, not just the player.

                // Apply speed override cheat if enabled (applies to all parties on the map)
                // This is a CHEAT feature, separate from the DLC fix
                // We apply it in CalculateFinalSpeed to ensure it works after all naval modifiers
                // (wind, crew, terrain, etc.). This is the final step in speed calculation.
                if (Settings != null && TargetSettings != null && ShouldApplyPlayerSpeedOverride(mobileParty))
                {
                    ApplyPlayerSpeedBoost(ref __result);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[NavalSpeedPatch] Error in CalculateFinalSpeed_Postfix: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Determines if player speed override should be applied.
        /// </summary>
#pragma warning disable IDE0060, RCS1163 // Remove unused parameter - parameter name required for method signature
        private static bool ShouldApplyPlayerSpeedOverride(MobileParty mobileParty)
#pragma warning restore IDE0060, RCS1163
        {
            // Early exit if settings are null
            if (Settings is null || TargetSettings is null)
            {
                return false;
            }

            bool movementSpeedOk = Settings.MovementSpeed > 0f;
            bool applyToPlayer = TargetSettings.ApplyToPlayer;
            bool campaignActive = Campaign.Current is not null;

            // Apply to ALL parties on the map when MovementSpeed > 0 and ApplyToPlayer is enabled
            // This ensures all parties (player, AI, etc.) get the speed boost
            return movementSpeedOk && applyToPlayer && campaignActive;
        }

        /// <summary>
        /// Applies player speed boost by calculating a multiplier factor.
        /// Preserves all terrain, composition, and naval modifiers.
        /// </summary>
        private static void ApplyPlayerSpeedBoost(ref ExplainedNumber speed)
        {
            // MovementSpeed setting is the FINAL speed value the user wants
            float desiredSpeed = Settings?.MovementSpeed ?? 0f;
            bool speedChanged = desiredSpeed != _lastDesiredSpeed;

            if (desiredSpeed <= 0f)
            {
                // Disabled - reset tracking
                _lastDesiredSpeed = 0f;
                return;
            }

            // Calculate the multiplier needed to achieve the desired speed
            // This preserves all terrain, composition, and naval modifiers
            float currentBaseSpeed = speed.BaseNumber;

            // Cap the multiplier to prevent extreme values when base speed is very small
            const float maxMultiplier = 100f;
            const float minBaseSpeedThreshold = 0.01f;

            if (currentBaseSpeed > minBaseSpeedThreshold)
            {
                // Calculate multiplier: desiredSpeed / currentBaseSpeed
                float multiplier = desiredSpeed / currentBaseSpeed;
                if (multiplier > maxMultiplier)
                {
                    // If multiplier would be too large, cap the desired speed instead
                    desiredSpeed = currentBaseSpeed * maxMultiplier;
                    multiplier = maxMultiplier;
                }
                // Then add factor = (multiplier - 1.0) to get the desired result
                float factorToAdd = multiplier - 1.0f;
                speed.AddFactor(factorToAdd, SpeedOverrideText);
            }
            else
            {
                // If base speed is too low, use multiplicative approach with capped multiplier
                float cappedDesiredSpeed = Math.Min(desiredSpeed, currentBaseSpeed * maxMultiplier);
                float multiplier = cappedDesiredSpeed / Math.Max(currentBaseSpeed, minBaseSpeedThreshold);
                float factorToAdd = multiplier - 1.0f;
                speed.AddFactor(factorToAdd, SpeedOverrideText);
            }

            // Track desired speed
            _lastDesiredSpeed = desiredSpeed;

            // Log once for debugging or when speed changes
            if (speedChanged)
            {
                // Removed excessive logging - speed boost is applied silently
            }
        }
    }
}
