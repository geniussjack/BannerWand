#nullable enable
// System namespaces
using System;

// Third-party namespaces
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using TaleWorlds.Library;

// Project namespaces
using BannerWand.Constants;
using BannerWand.Settings;
using BannerWand.Utils;
using static BannerWand.Utils.ModLogger;

namespace BannerWand.Models
{
    /// <summary>
    /// Custom party wage model that applies garrison wages multiplier.
    /// Extends <see cref="DefaultPartyWageModel"/> to modify garrison wages based on settings.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This model is registered with the game engine via <see cref="CampaignGameStarter.AddModel"/>
    /// in <see cref="Core.SubModule.RegisterCustomModels"/>. Once registered, the game automatically
    /// uses this model instead of the default for all wage calculations.
    /// </para>
    /// <para>
    /// Cheat features provided:
    /// - Garrison Wages Multiplier: Modifies wages for garrison parties only
    /// </para>
    /// <para>
    /// Why model override instead of Harmony patch:
    /// - Harmony patching DefaultPartyWageModel causes TypeInitializationException
    /// - Model override is the official, safe way to modify game behavior
    /// - We check if party is garrison before applying multiplier, so other parties are unaffected
    /// </para>
    /// </remarks>
    public class CustomPartyWageModel : DefaultPartyWageModel
    {
        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings? Settings => CheatSettings.Instance;

        /// <summary>
        /// Calculates the total wage for a mobile party.
        /// Applies garrison wages multiplier if the party is a garrison.
        /// </summary>
        /// <param name="mobileParty">The mobile party to calculate wages for. Cannot be null.</param>
        /// <param name="troopRoster">The troop roster to calculate wages for. Cannot be null.</param>
        /// <param name="includeDescriptions">Whether to include detailed explanations in the result.</param>
        /// <returns>
        /// An <see cref="ExplainedNumber"/> containing the total wage value.
        /// For garrison parties, the wage is modified according to the configured multiplier.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Base game behavior:
        /// - Calculates total wage from all troops in the roster
        /// - Returns ExplainedNumber with base value and all modifiers
        /// </para>
        /// <para>
        /// Cheat behavior:
        /// - If party is a garrison and multiplier is configured, apply multiplier to the result
        /// - Multiplier can be:
        ///   - 1.0 (normal wages, no change)
        ///   - 0.0 (free garrison, wages set to 0)
        ///   - Greater than 1.0 (increase wages)
        ///   - Between 0 and 1.0 (divide wages, e.g., 0.5 = wages / 2)
        /// </para>
        /// <para>
        /// Applies to player-owned settlements and targeted NPC settlements.
        /// Uses SettlementCheatHelper to determine if settlement qualifies.
        /// </para>
        /// </remarks>
        public override ExplainedNumber GetTotalWage(MobileParty mobileParty, TroopRoster troopRoster, bool includeDescriptions = false)
        {
            try
            {
                // Get base wage calculation from base class implementation
                // base.GetTotalWage() always calls the base class method, avoiding recursion
                ExplainedNumber baseWage = base.GetTotalWage(mobileParty, troopRoster, includeDescriptions);

                // Early exit for null party or invalid settings
                if (mobileParty == null || Settings == null)
                {
                    return baseWage;
                }

                // Only apply to garrison parties
                if (!mobileParty.IsGarrison)
                {
                    return baseWage;
                }

                // Get settlement from garrison party
                Settlement? settlement = mobileParty.CurrentSettlement;
                if (settlement == null)
                {
                    return baseWage;
                }

                // Check if settlement should receive the cheat
                if (!SettlementCheatHelper.ShouldApplyCheatToSettlement(settlement))
                {
                    // Debug log removed for performance - this method is called very frequently
                    return baseWage;
                }

                // Get multiplier from settings
                float multiplier = Settings.GarrisonWagesMultiplier;
                float originalWage = baseWage.ResultNumber;

                // Apply multiplier based on value
                if (Math.Abs(multiplier - GameConstants.DefaultGarrisonWagesMultiplier) < GameConstants.FloatEpsilon)
                {
                    // Multiplier is 1.0 (normal), no change needed
                    return baseWage;
                }

                if (Math.Abs(multiplier) < GameConstants.FloatEpsilon)
                {
                    // Multiplier is 0.0 (free garrison), set wages to 0
                    // Create new ExplainedNumber with 0 base, preserving includeDescriptions flag
                    ExplainedNumber freeWage = new(0f, includeDescriptions, null);
                    Debug($"[CustomPartyWageModel] Set garrison wages to 0 (free) for {settlement.Name}: {originalWage:F2} -> 0");
                    return freeWage;
                }

                if (multiplier > GameConstants.MultiplierFactorBase)
                {
                    // Multiplier > 1.0 (increase wages)
                    // Instead of creating a new ExplainedNumber, modify the existing one to preserve descriptions
                    float newWage = originalWage * multiplier;
                    ExplainedNumber increasedWage = new(newWage, includeDescriptions, null);
                    Debug($"[CustomPartyWageModel] Applied multiplier {multiplier:F2} to garrison wages for {settlement.Name}: {originalWage:F2} -> {newWage:F2}");
                    return increasedWage;
                }

                // Multiplier between 0 and 1.0 (divide wages)
                // For example, 0.5 means divide by 2
                float divisor = multiplier;
                if (divisor > GameConstants.FloatEpsilon)
                {
                    float newWageDivided = originalWage / divisor;
                    ExplainedNumber dividedWage = new(newWageDivided, includeDescriptions, null);
                    // Debug log removed for performance - this method is called very frequently
                    return dividedWage;
                }

                return baseWage;
            }
            catch (Exception ex)
            {
                Error($"[CustomPartyWageModel] Error in GetTotalWage: {ex.Message}");
                Error($"Stack trace: {ex.StackTrace}");
                // Try to call base method as fallback
                try
                {
                    return base.GetTotalWage(mobileParty, troopRoster, includeDescriptions);
                }
                catch (Exception fallbackEx)
                {
                    Error($"[CustomPartyWageModel] Fallback also failed: {fallbackEx.Message}");
                    // Last resort: return zero wages to prevent crash
                    return new(0f, includeDescriptions, null);
                }
            }
        }
    }
}
