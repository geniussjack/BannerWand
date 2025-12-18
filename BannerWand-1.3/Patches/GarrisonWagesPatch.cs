#nullable enable
// System namespaces
using System;
using System.Reflection;

// Third-party namespaces
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

// Project namespaces
using BannerWand.Constants;
using BannerWand.Settings;
using BannerWand.Utils;

namespace BannerWand.Patches
{
    /// <summary>
    /// Harmony patch to apply garrison wages multiplier.
    /// Patches PartyWageModel.GetTotalWage to modify garrison wages based on settings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This patch intercepts wage calculations for garrison parties and applies the configured multiplier.
    /// The multiplier can:
    /// - Be 1.0 (normal wages, no change)
    /// - Be 0.0 (free garrison, wages set to 0)
    /// - Be negative (negative wages = income for settlement owner)
    /// - Be greater than 1.0 (increase wages)
    /// - Be between 0 and 1.0 (divide wages, e.g., 0.5 = wages / 2)
    /// </para>
    /// <para>
    /// Why Harmony patch instead of model override:
    /// - Wages are calculated in PartyWageModel, not SettlementGarrisonModel
    /// - PartyWageModel is used for all parties (mobile, garrison, caravan, etc.)
    /// - We only want to modify garrison wages, not all party wages
    /// - Harmony patch allows us to check if party is garrison before applying multiplier
    /// </para>
    /// </remarks>
    /// <para>
    /// DEPRECATED: This patch is no longer used. We now use CustomPartyWageModel instead.
    /// This class is kept for reference but the [HarmonyPatch] attribute is removed
    /// to prevent PatchAll() from automatically applying it.
    /// </para>
    // [HarmonyPatch] - REMOVED: This patch causes TypeInitializationException
    // We now use CustomPartyWageModel instead, which is safer and more reliable
    public static class GarrisonWagesPatch
    {
        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings? Settings => CheatSettings.Instance;

        /// <summary>
        /// Gets the target method for patching - DefaultPartyWageModel.GetTotalWage (concrete implementation).
        /// </summary>
        /// <remarks>
        /// We patch the concrete implementation DefaultPartyWageModel.GetTotalWage instead of
        /// the abstract PartyWageModel.GetTotalWage because Harmony cannot patch abstract methods.
        /// </remarks>
        [HarmonyTargetMethod]
        public static MethodBase? TargetMethod()
        {
            try
            {
                // Get the concrete implementation type, not the abstract interface
                Type? defaultPartyWageModelType = null;
                foreach (System.Reflection.Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    defaultPartyWageModelType = assembly.GetType("TaleWorlds.CampaignSystem.GameComponents.DefaultPartyWageModel");
                    if (defaultPartyWageModelType != null)
                    {
                        break;
                    }
                }

                if (defaultPartyWageModelType == null)
                {
                    ModLogger.Warning("[GarrisonWagesPatch] DefaultPartyWageModel type not found");
                    return null;
                }

                MethodInfo? method = defaultPartyWageModelType.GetMethod(
                    "GetTotalWage",
                    BindingFlags.Instance | BindingFlags.Public,
                    null,
                    [typeof(MobileParty), typeof(TroopRoster), typeof(bool)],
                    null);

                if (method != null)
                {
                    ModLogger.Log("[GarrisonWagesPatch] Found DefaultPartyWageModel.GetTotalWage method");
                    return method;
                }

                ModLogger.Warning("[GarrisonWagesPatch] DefaultPartyWageModel.GetTotalWage method not found");
                return null;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[GarrisonWagesPatch] Error finding GetTotalWage method: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Postfix patch that applies garrison wages multiplier after wage calculation.
        /// </summary>
        /// <param name="mobileParty">The mobile party (parameter name must match original method).</param>
        /// <param name="__result">The calculated total wage (modified by ref).</param>
        [HarmonyPostfix]
        public static void GetTotalWage_Postfix(MobileParty mobileParty, ref ExplainedNumber __result)
        {
            try
            {
                // Early exit for null party or invalid settings
                if (mobileParty == null || Settings == null)
                {
                    return;
                }

                // Only apply to garrison parties
                if (!mobileParty.IsGarrison)
                {
                    return;
                }

                // Get settlement from garrison party
                Settlement? settlement = mobileParty.CurrentSettlement;
                if (settlement == null)
                {
                    return;
                }

                // Check if settlement should receive the cheat
                if (!SettlementCheatHelper.ShouldApplyCheatToSettlement(settlement))
                {
                    ModLogger.Debug($"[GarrisonWagesPatch] Settlement {settlement.Name} does not qualify for garrison wages multiplier");
                    return;
                }

                // Get multiplier from settings
                float multiplier = Settings.GarrisonWagesMultiplier;
                float originalWage = __result.ResultNumber;

                // Apply multiplier based on value
                if (Math.Abs(multiplier - GameConstants.DefaultGarrisonWagesMultiplier) < GameConstants.FloatEpsilon)
                {
                    // Multiplier is 1.0 (normal), no change needed
                    return;
                }

                if (Math.Abs(multiplier) < GameConstants.FloatEpsilon)
                {
                    // Multiplier is 0.0 (free garrison), set wages to 0
                    __result = new ExplainedNumber(0f, __result.IncludeDescriptions, null);
                    ModLogger.Debug($"[GarrisonWagesPatch] Set garrison wages to 0 (free) for {settlement.Name}: {originalWage:F2} -> 0");
                    return;
                }

                if (multiplier < 0f)
                {
                    // Negative multiplier = negative wages (income for owner)
                    // Multiply by absolute value, then negate
                    float baseWage = __result.ResultNumber;
                    float newWage = -(baseWage * Math.Abs(multiplier));
                    __result = new ExplainedNumber(newWage, __result.IncludeDescriptions, null);
                    ModLogger.Debug($"[GarrisonWagesPatch] Applied negative multiplier {multiplier:F2} to garrison wages for {settlement.Name}: {originalWage:F2} -> {newWage:F2} (income)");
                    return;
                }

                if (multiplier > GameConstants.MultiplierFactorBase)
                {
                    // Multiplier > 1.0 (increase wages)
                    float baseWage = __result.ResultNumber;
                    float newWage = baseWage * multiplier;
                    __result = new ExplainedNumber(newWage, __result.IncludeDescriptions, null);
                    ModLogger.Debug($"[GarrisonWagesPatch] Applied multiplier {multiplier:F2} to garrison wages for {settlement.Name}: {originalWage:F2} -> {newWage:F2}");
                    return;
                }

                // Multiplier between 0 and 1.0 (divide wages)
                // For example, 0.5 means divide by 2
                float baseWageForDivision = __result.ResultNumber;
                float divisor = multiplier;
                if (divisor > GameConstants.FloatEpsilon)
                {
                    float newWageDivided = baseWageForDivision / divisor;
                    __result = new ExplainedNumber(newWageDivided, __result.IncludeDescriptions, null);
                    ModLogger.Debug($"[GarrisonWagesPatch] Applied divisor {divisor:F2} to garrison wages for {settlement.Name}: {originalWage:F2} -> {newWageDivided:F2}");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[GarrisonWagesPatch] Error in GetTotalWage_Postfix: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
