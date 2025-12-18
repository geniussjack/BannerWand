#nullable enable
// System namespaces
using System;

// Third-party namespaces
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

// Project namespaces
using BannerWand.Constants;
using BannerWand.Settings;
using BannerWand.Utils;
using static BannerWand.Utils.ModLogger;

namespace BannerWand.Models
{
    /// <summary>
    /// Custom settlement garrison model that enables garrison recruitment multiplier.
    /// Extends <see cref="DefaultSettlementGarrisonModel"/> to add cheat functionality.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This model is registered with the game engine via <see cref="CampaignGameStarter.AddModel"/>
    /// in <see cref="Core.SubModule.RegisterCustomModels"/>. Once registered, the game automatically
    /// uses this model instead of the default for all garrison calculations.
    /// </para>
    /// <para>
    /// Cheat features provided:
    /// - Garrison Recruitment Multiplier: Multiplies daily auto-recruitment count for garrisons
    /// </para>
    /// <para>
    /// Note: Garrison wages multiplier is handled via Harmony patch (GarrisonWagesPatch)
    /// because wages are calculated in PartyWageModel, not in SettlementGarrisonModel.
    /// </para>
    /// </remarks>
    public class CustomSettlementGarrisonModel : DefaultSettlementGarrisonModel
    {
        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings? Settings => CheatSettings.Instance;

        /// <summary>
        /// Text object for recruitment multiplier description (cached to avoid allocations).
        /// </summary>
        private static readonly TextObject RecruitmentMultiplierText = new("BannerWand Garrison Recruitment Multiplier");

        /// <summary>
        /// Calculates the maximum daily auto-recruitment count for a town's garrison.
        /// Applies recruitment multiplier if enabled.
        /// </summary>
        /// <param name="town">The town where recruitment is happening. Cannot be null.</param>
        /// <returns>
        /// The maximum number of soldiers that can be recruited per day.
        /// Base value is 1, multiplied by the configured multiplier if enabled.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Base game behavior:
        /// - Default maximum is 1 soldier per day
        /// - This method is called daily to determine recruitment limit
        /// </para>
        /// <para>
        /// Cheat behavior:
        /// - If multiplier > 0 and settlement qualifies, multiply base value
        /// - Multiplier of 2.0 = 2 soldiers per day, 10.0 = 10 soldiers per day, etc.
        /// </para>
        /// <para>
        /// Applies to player-owned settlements and targeted NPC settlements.
        /// Uses SettlementCheatHelper to determine if settlement qualifies.
        /// </para>
        /// </remarks>
        public override int GetMaximumDailyAutoRecruitmentCount(Town town)
        {
            try
            {
                // Get base value from default implementation
                int baseCount = base.GetMaximumDailyAutoRecruitmentCount(town);

                // Early exit for null or unconfigured settings
                if (Settings == null || town?.Settlement == null)
                {
                    return baseCount;
                }

                // Apply recruitment bonus if enabled (additive, not multiplier)
                if (Settings.GarrisonRecruitmentMultiplier > 0)
                {
                    if (SettlementCheatHelper.ShouldApplyCheatToSettlement(town.Settlement))
                    {
                        // Validate bonus is within bounds (0-999)
                        int bonus = Math.Min(Settings.GarrisonRecruitmentMultiplier, GameConstants.MaxSettlementBonusValue);
                        bonus = Math.Max(bonus, 0);

                        // Add bonus to base count
                        int newCount = baseCount + bonus;

                        // Ensure at least 1 if bonus is active (prevents 0 recruitment)
                        if (newCount < 1 && bonus > 0)
                        {
                            newCount = 1;
                        }

                        return newCount;
                    }
                }

                return baseCount;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomSettlementGarrisonModel] Error in GetMaximumDailyAutoRecruitmentCount: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return base.GetMaximumDailyAutoRecruitmentCount(town);
            }
        }

        /// <summary>
        /// Calculates the base daily garrison change for a settlement.
        /// Applies recruitment multiplier if enabled.
        /// </summary>
        /// <param name="settlement">The settlement to calculate for. Cannot be null.</param>
        /// <param name="includeDescriptions">Whether to include detailed explanations in the result.</param>
        /// <returns>
        /// An <see cref="ExplainedNumber"/> containing the base daily garrison change value.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Base game behavior:
        /// - Calculates base garrison change (usually 0 or small positive value for rebel settlements)
        /// - This is added to auto-recruitment to get total garrison change
        /// </para>
        /// <para>
        /// Cheat behavior:
        /// - If multiplier > 0 and settlement qualifies, multiply the base change
        /// - This ensures that both base growth and auto-recruitment are affected
        /// </para>
        /// <para>
        /// Applies to player-owned settlements and targeted NPC settlements.
        /// Uses SettlementCheatHelper to determine if settlement qualifies.
        /// </para>
        /// </remarks>
        public override ExplainedNumber CalculateBaseGarrisonChange(Settlement settlement, bool includeDescriptions = false)
        {
            try
            {
                // Get base value from default implementation
                ExplainedNumber baseChange = base.CalculateBaseGarrisonChange(settlement, includeDescriptions);

                // Early exit for null or unconfigured settings
                if (Settings == null || settlement == null)
                {
                    return baseChange;
                }

                // Apply recruitment bonus if enabled (additive, not multiplier)
                if (Settings.GarrisonRecruitmentMultiplier > 0)
                {
                    if (SettlementCheatHelper.ShouldApplyCheatToSettlement(settlement))
                    {
                        // Validate bonus is within bounds (0-999)
                        int bonus = Math.Min(Settings.GarrisonRecruitmentMultiplier, GameConstants.MaxSettlementBonusValue);
                        bonus = Math.Max(bonus, 0);

                        // Add bonus directly (simple addition, prevents geometric progression)
                        baseChange.Add(bonus, RecruitmentMultiplierText);
                    }
                }

                return baseChange;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomSettlementGarrisonModel] Error in CalculateBaseGarrisonChange: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return base.CalculateBaseGarrisonChange(settlement, includeDescriptions);
            }
        }
    }
}
