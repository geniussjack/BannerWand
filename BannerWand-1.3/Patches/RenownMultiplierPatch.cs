#nullable enable
using BannerWand.Settings;
using BannerWand.Utils;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using TaleWorlds.CampaignSystem;

namespace BannerWand.Patches
{
    /// <summary>
    /// Harmony patch for multiplying renown gains.
    /// Patches <see cref="Clan.AddRenown"/> to multiply renown amount before adding.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This patch intercepts all calls to Clan.AddRenown() and multiplies the renown
    /// amount by the configured multiplier when the cheat is enabled.
    /// </para>
    /// <para>
    /// Target Method: TaleWorlds.CampaignSystem.Clan.AddRenown(float value, bool shouldNotify)
    /// Patch Type: Prefix (modifies parameter before original method executes)
    /// </para>
    /// <para>
    /// Why Harmony is needed:
    /// - No RenownModel exists in Bannerlord API
    /// - No events for renown gain
    /// - AddRenown() is called from various game systems without hooks
    /// </para>
    /// </remarks>
    [HarmonyPatch(typeof(Clan))]
    public static class RenownMultiplierPatch
    {
        private static bool _firstCallLogged = false;

        /// <summary>
        /// Explicitly targets the AddRenown method by searching all available overloads.
        /// </summary>
        [HarmonyTargetMethod]
        public static MethodBase? TargetMethod()
        {
            try
            {
                Type clanType = typeof(Clan);
                MethodInfo[] clanMethods = clanType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

                List<MethodInfo> allMethods = [];
                foreach (MethodInfo methodInfo in clanMethods)
                {
                    if (methodInfo.Name == "AddRenown")
                    {
                        allMethods.Add(methodInfo);
                    }
                }

                ModLogger.Log($"RenownMultiplierPatch: Found {allMethods.Count} AddRenown method(s):");
                foreach (MethodInfo methodInfo in allMethods)
                {
                    ParameterInfo[] parameters = methodInfo.GetParameters();
                    List<string> paramStrings = [];
                    foreach (ParameterInfo param in parameters)
                    {
                        paramStrings.Add($"{param.ParameterType.Name} {param.Name}");
                    }
                    string paramStr = string.Join(", ", paramStrings);
                    ModLogger.Log($"  - AddRenown({paramStr})");
                }

                // Try (float, bool) signature first
                MethodInfo method = clanType.GetMethod(
                    "AddRenown",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    [typeof(float), typeof(bool)],
                    null
                );

                // Fallback to (float) signature
                if (method is null)
                {
                    ModLogger.Warning("RenownMultiplierPatch: AddRenown(float, bool) not found, trying AddRenown(float)");
                    method = clanType.GetMethod(
                        "AddRenown",
                        BindingFlags.Public | BindingFlags.Instance,
                        null,
                        [typeof(float)],
                        null
                    );
                }

                // Last resort - any AddRenown
                if (method is null && allMethods.Count > 0)
                {
                    ModLogger.Warning("RenownMultiplierPatch: Using first available AddRenown method");
                    method = allMethods[0];
                }

                if (method is null)
                {
                    ModLogger.Error("RenownMultiplierPatch: Could not find any AddRenown method! Patch will not be applied.");
                    return null;
                }

                ParameterInfo[] finalParameters = method.GetParameters();
                List<string> finalParamStrings = [];
                foreach (ParameterInfo param in finalParameters)
                {
                    finalParamStrings.Add($"{param.ParameterType.Name} {param.Name}");
                }
                string finalParamStr = string.Join(", ", finalParamStrings);
                ModLogger.Log($"RenownMultiplierPatch: Successfully targeting AddRenown({finalParamStr})");

                return method;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"RenownMultiplierPatch: Exception in TargetMethod: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Prefix that multiplies renown value before AddRenown executes.
        /// </summary>
        /// <param name="__instance">The Clan instance receiving renown.</param>
        /// <param name="value">The renown amount to add (modified by ref).</param>
        /// <param name="shouldNotify">Whether to notify about renown change (unused, required for signature match).</param>
        [HarmonyPrefix]
#pragma warning disable IDE0060, RCS1163 // Remove unused parameter - required for Harmony signature match
        public static void Prefix(Clan __instance, ref float value, bool shouldNotify = true)
#pragma warning restore IDE0060, RCS1163
        {
            try
            {
                CheatSettings? settings = CheatSettings.Instance;
                CheatTargetSettings? targetSettings = CheatTargetSettings.Instance;

                if (settings == null || targetSettings == null)
                {
                    return;
                }

                // Only apply if multiplier is greater than 0 (0 = disabled)
                if (settings.RenownMultiplier <= 0f)
                {
                    return;
                }

                // Only apply to player clan
                if (!targetSettings.ApplyToPlayer || __instance != Clan.PlayerClan)
                {
                    return;
                }

                // Skip if value is negative (renown loss) or zero
                if (value <= 0f)
                {
                    return;
                }

                float originalValue = value;
                value *= settings.RenownMultiplier;

                // Log first call for debugging
                if (!_firstCallLogged)
                {
                    _firstCallLogged = true;
                    ModLogger.Log($"[RenownMultiplier] Patch active! First multiplied renown: {originalValue:F1} × {settings.RenownMultiplier:F1} = {value:F1}");
                }

                ModLogger.Debug($"[RenownMultiplier] {originalValue:F1} × {settings.RenownMultiplier:F1} = {value:F1}");
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[RenownMultiplier] Error in Prefix: {ex.Message}", ex);
            }
        }
    }
}
