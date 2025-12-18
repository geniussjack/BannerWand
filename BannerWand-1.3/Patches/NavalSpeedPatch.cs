#nullable enable
// System namespaces
using System;
using System.IO;
using System.Reflection;

// Third-party namespaces
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Localization;

// Project namespaces
using BannerWand.Constants;
using BannerWand.Settings;
using BannerWand.Utils;

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
        /// Cached type for NavalDLCPartySpeedCalculationModel to avoid repeated reflection calls.
        /// </summary>
        private static Type? _navalDlcModelType;

        /// <summary>
        /// Gets the NavalDLC model type, caching it for performance.
        /// </summary>
        private static Type? GetNavalDlcModelType()
        {
            if (_navalDlcModelType != null)
            {
                return _navalDlcModelType;
            }

            try
            {
                _navalDlcModelType = Type.GetType("NavalDLC.GameComponents.NavalDLCPartySpeedCalculationModel, NavalDLC");
                if (_navalDlcModelType == null)
                {
                    foreach (System.Reflection.Assembly assembly in System.AppDomain.CurrentDomain.GetAssemblies())
                    {
                        try
                        {
                            if (assembly.GetName().Name == "NavalDLC")
                            {
                                _navalDlcModelType = assembly.GetType("NavalDLC.GameComponents.NavalDLCPartySpeedCalculationModel");
                                if (_navalDlcModelType != null)
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
            }
            catch
            {
                // If we can't find the type, cache null to avoid repeated attempts
            }

            return _navalDlcModelType;
        }

        /// <summary>
        /// Checks if the instance is of the NavalDLC model type.
        /// </summary>
        private static bool IsNavalDlcModel(object instance)
        {
            if (instance == null)
            {
                return false;
            }

            Type? navalDlcType = GetNavalDlcModelType();
            if (navalDlcType == null)
            {
                return false;
            }

            return navalDlcType.IsAssignableFrom(instance.GetType());
        }

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
                if (!IsNavalDlcModel(__instance))
                {
                    return;
                }

                // Only apply for sea travel (when IsCurrentlyAtSea is true)
                // For land travel, the base model will handle it
                if (!party.IsCurrentlyAtSea)
                {
                    return;
                }

                // Apply speed override cheat if enabled (applies to all parties on the map)
                if (Settings != null && TargetSettings != null)
                {
                    // Check for player speed override first (only for player's main party)
                    if (ShouldApplyPlayerSpeedOverride(party))
                    {
                        ApplySpeedBoost(ref __result, Settings.MovementSpeed);
                    }
                    // Check for NPC speed override (only for non-player parties)
                    else if (ShouldApplyNPCSpeedOverride(party))
                    {
                        ApplySpeedBoost(ref __result, Settings.NPCMovementSpeed);
                    }
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
                if (!IsNavalDlcModel(__instance))
                {
                    return;
                }

                // Apply speed override cheat if enabled (applies to all parties on the map)
                // We apply it in CalculateFinalSpeed to ensure it works after all naval modifiers
                if (Settings != null && TargetSettings != null)
                {
                    // Check for player speed override first (only for player's main party)
                    if (ShouldApplyPlayerSpeedOverride(mobileParty))
                    {
                        ApplySpeedBoost(ref __result, Settings.MovementSpeed);
                    }
                    // Check for NPC speed override (only for non-player parties)
                    else if (ShouldApplyNPCSpeedOverride(mobileParty))
                    {
                        ApplySpeedBoost(ref __result, Settings.NPCMovementSpeed);
                    }
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
        /// Only applies to the player's main party when MovementSpeed > 0 and ApplyToPlayer is enabled.
        /// </summary>
        private static bool ShouldApplyPlayerSpeedOverride(MobileParty mobileParty)
        {
            // Early exit if settings are null
            return Settings is not null && TargetSettings is not null && Settings.MovementSpeed > 0f &&
                   mobileParty == MobileParty.MainParty &&
                   TargetSettings.ApplyToPlayer &&
                   Campaign.Current != null;
        }

        /// <summary>
        /// Determines if NPC speed override should be applied.
        /// Applies only to non-player parties when NPCMovementSpeed > 0.
        /// Player's party should use Player speed override instead.
        /// </summary>
        private static bool ShouldApplyNPCSpeedOverride(MobileParty mobileParty)
        {
            // Early exit if settings are null
            // Do NOT apply NPC speed to player's main party - player should use Player speed instead
            return Settings?.NPCMovementSpeed > 0f &&
                   Campaign.Current != null &&
                   mobileParty != MobileParty.MainParty;
        }

        /// <summary>
        /// Applies speed boost by setting a fixed final speed value.
        /// The speed will always equal the desired speed, regardless of base speed changes.
        /// </summary>
        /// <param name="speed">The speed ExplainedNumber to modify</param>
        /// <param name="desiredSpeed">The FINAL speed value the user wants (0-16)</param>
        private static void ApplySpeedBoost(ref ExplainedNumber speed, float desiredSpeed)
        {
            if (desiredSpeed <= 0f)
            {
                // Disabled - reset tracking
                _lastDesiredSpeed = 0f;
                return;
            }

            // Cap desired speed to maximum allowed value
            // This prevents speed from exceeding the maximum set in MCM settings
            if (desiredSpeed > GameConstants.AbsoluteMaxGameSpeed)
            {
                desiredSpeed = GameConstants.AbsoluteMaxGameSpeed;
            }

            // CRITICAL FIX: Create a new ExplainedNumber with the desired speed as the base value
            // and NO additional modifiers. This ensures the speed is ALWAYS exactly equal to desiredSpeed.
            //
            // Previous issue: speed.Add(0f, SpeedOverrideText) was being called, which might have
            // affected the result or allowed other modifiers to stack. Now we create a completely
            // fresh ExplainedNumber with ONLY the desired speed as the base value.
            //
            // Why this works:
            // 1. ExplainedNumber stores modifiers internally and calculates ResultNumber = BaseNumber + sum of modifiers
            // 2. By creating a new ExplainedNumber with desiredSpeed as BaseNumber and NO modifiers,
            //    ResultNumber will ALWAYS equal desiredSpeed
            // 3. This prevents any fluctuations or stacking of modifiers from multiple calls
            // 4. The speed becomes completely static and constant at the desired value
            //
            // We preserve the includeDescriptions flag from the original speed for UI display
            bool includeDescriptions = speed.IncludeDescriptions;
            speed = new ExplainedNumber(desiredSpeed, includeDescriptions, null);
            // DO NOT call speed.Add() - this ensures no modifiers are added that could affect the result

            // Track desired speed for debugging and state tracking
            _lastDesiredSpeed = desiredSpeed;
        }
    }
}
