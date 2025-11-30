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
    /// <example>
    /// Example usage scenario:
    /// <code>
    /// // User sets RenownMultiplier to 3.0 in MCM settings
    /// // When player wins a battle that normally gives 10 renown:
    /// //
    /// // Without patch: Clan.AddRenown(10f) → Player gains 10 renown
    /// // With patch:    Clan.AddRenown(10f) → Multiplied to 30f → Player gains 30 renown
    /// //
    /// // The patch modifies the 'ref float value' parameter before the original method executes
    /// </code>
    /// </example>
    [HarmonyPatch(typeof(Clan))]
    public static class RenownMultiplierPatch
    {
        /// <summary>
        /// Explicitly targets the AddRenown method by searching all available overloads.
        /// </summary>
        /// <returns>The MethodInfo for Clan.AddRenown method, or null if not found.</returns>
        /// <remarks>
        /// <para>
        /// This method searches for AddRenown with any signature to handle API changes
        /// across different Bannerlord versions. It attempts to find the method in this order:
        /// 1. AddRenown(float, bool) - preferred signature
        /// 2. AddRenown(float) - fallback signature
        /// 3. Any AddRenown method - last resort
        /// </para>
        /// <para>
        /// Logs all available methods for debugging purposes to help diagnose patching issues.
        /// </para>
        /// </remarks>
        [HarmonyTargetMethod]
        public static MethodBase TargetMethod()
        {
            try
            {
                // Get all AddRenown methods from Clan type
                Type clanType = typeof(Clan);
                MethodInfo[] clanMethods = clanType.GetMethods(BindingFlags.Public | BindingFlags.Instance);

                // Filter to only AddRenown methods
                List<MethodInfo> allMethods = [];
                foreach (MethodInfo methodInfo in clanMethods)
                {
                    if (methodInfo.Name == "AddRenown")
                    {
                        allMethods.Add(methodInfo);
                    }
                }

                // Log all found methods for debugging
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

                // Try to find method with (float, bool) signature first
                MethodInfo method = clanType.GetMethod(
                    "AddRenown",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    [typeof(float), typeof(bool)],
                    null
                );

                // If not found, try (float) signature only
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

                // If still not found, take any AddRenown method as last resort
                if (method is null && allMethods.Count > 0)
                {
                    ModLogger.Warning("RenownMultiplierPatch: Using first available AddRenown method");
                    method = allMethods[0];
                }

                // Log final result
                if (method is null)
                {
                    ModLogger.Error("RenownMultiplierPatch: Could not find any AddRenown method!");
                }
                else
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    List<string> paramStrings = [];

                    foreach (ParameterInfo param in parameters)
                    {
                        paramStrings.Add($"{param.ParameterType.Name} {param.Name}");
                    }

                    string paramStr = string.Join(", ", paramStrings);
                    ModLogger.Log($"RenownMultiplierPatch: Successfully targeting method AddRenown({paramStr})");
                }

                return method!;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"RenownMultiplierPatch: Exception in TargetMethod: {ex.Message}");
                return null!;
            }
        }
    }
}
