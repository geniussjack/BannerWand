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
    /// Custom settlement militia model that enables militia recruitment multiplier and veteran chance override.
    /// Extends <see cref="DefaultSettlementMilitiaModel"/> to add cheat functionality.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This model is registered with the game engine via <see cref="CampaignGameStarter.AddModel"/>
    /// in <see cref="Core.SubModule.RegisterCustomModels"/>. Once registered, the game automatically
    /// uses this model instead of the default for all militia calculations.
    /// </para>
    /// <para>
    /// Cheat features provided:
    /// - Militia Recruitment Multiplier: Multiplies daily militia recruitment rate
    /// - Militia Veteran Chance: Adds percentage chance for veteran militiamen to appear
    /// </para>
    /// </remarks>
    public class CustomSettlementMilitiaModel : DefaultSettlementMilitiaModel
    {
        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings? Settings => CheatSettings.Instance;

        /// <summary>
        /// Text object for recruitment multiplier description (cached to avoid allocations).
        /// </summary>
        private static readonly TextObject RecruitmentMultiplierText = new("BannerWand Militia Recruitment Multiplier");

        /// <summary>
        /// Text object for veteran chance description (cached to avoid allocations).
        /// </summary>
        private static readonly TextObject VeteranChanceText = new("BannerWand Militia Veteran Chance");

        /// <summary>
        /// Calculates the daily militia change for a settlement.
        /// Applies recruitment multiplier if enabled.
        /// </summary>
        /// <param name="settlement">The settlement to calculate for. Cannot be null.</param>
        /// <param name="includeDescriptions">Whether to include detailed explanations in the result.</param>
        /// <returns>
        /// An <see cref="ExplainedNumber"/> containing the daily militia change value.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Base game behavior:
        /// - Calculates base militia change from various factors (prosperity, buildings, etc.)
        /// - Positive values increase militia, negative values decrease it
        /// - The result can be negative due to "Retired" factor (2.5% of current militia)
        /// </para>
        /// <para>
        /// Cheat behavior:
        /// - If multiplier > 0 and settlement qualifies, multiply only the positive parts
        /// - If final result is positive, apply multiplier to it
        /// - If final result is negative, don't apply multiplier (to avoid making it worse)
        /// - Multiplier of 2.0 = double recruitment, 10.0 = 10x recruitment, etc.
        /// </para>
        /// <para>
        /// Applies to player-owned settlements and targeted NPC settlements.
        /// Uses SettlementCheatHelper to determine if settlement qualifies.
        /// </para>
        /// </remarks>
        public override ExplainedNumber CalculateMilitiaChange(Settlement settlement, bool includeDescriptions = false)
        {
            try
            {
                // Get base value from default implementation
                ExplainedNumber baseChange = base.CalculateMilitiaChange(settlement, includeDescriptions);

                // Early exit for null or unconfigured settings
                if (Settings == null || settlement == null)
                {
                    return baseChange;
                }

                // Apply recruitment bonus if enabled (additive, not multiplier)
                if (Settings.MilitiaRecruitmentMultiplier > 0)
                {
                    if (SettlementCheatHelper.ShouldApplyCheatToSettlement(settlement))
                    {
                        // Validate bonus is within bounds (0-999)
                        int bonus = Math.Min(Settings.MilitiaRecruitmentMultiplier, GameConstants.MaxSettlementBonusValue);
                        bonus = Math.Max(bonus, 0);
                        // Add bonus directly (simple addition, prevents geometric progression)
                        baseChange.Add(bonus, RecruitmentMultiplierText);
                    }
                }

                return baseChange;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomSettlementMilitiaModel] Error in CalculateMilitiaChange: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return base.CalculateMilitiaChange(settlement, includeDescriptions);
            }
        }

        /// <summary>
        /// Calculates the chance for veteran militiamen to appear.
        /// Adds configured percentage chance if enabled.
        /// </summary>
        /// <param name="settlement">The settlement to calculate for. Cannot be null.</param>
        /// <returns>
        /// An <see cref="ExplainedNumber"/> containing the veteran spawn chance (as a decimal, e.g., 0.5 = 50%).
        /// </returns>
        /// <remarks>
        /// <para>
        /// Base game behavior:
        /// - Calculates base chance from perks, buildings, policies, etc.
        /// - Returns value as decimal (0.0 to 1.0+)
        /// - The game uses this value to determine if spawned militiamen are veterans
        /// </para>
        /// <para>
        /// Cheat behavior:
        /// - If veteran chance > 0 and settlement qualifies, add the percentage as decimal
        /// - 50% chance = 0.5f, 100% chance = 1.0f
        /// - Added to base chance, so can exceed 100% if base + cheat > 100%
        /// </para>
        /// <para>
        /// Applies to player-owned settlements and targeted NPC settlements.
        /// Uses SettlementCheatHelper to determine if settlement qualifies.
        /// </para>
        /// </remarks>
        public override ExplainedNumber CalculateVeteranMilitiaSpawnChance(Settlement settlement)
        {
            try
            {
                // Get base value from default implementation
                ExplainedNumber baseChance = base.CalculateVeteranMilitiaSpawnChance(settlement);

                // Early exit for null or unconfigured settings
                if (Settings == null || settlement == null)
                {
                    return baseChance;
                }

                // Apply veteran chance if enabled
                if (Settings.MilitiaVeteranChance > GameConstants.FloatEpsilon)
                {
                    if (SettlementCheatHelper.ShouldApplyCheatToSettlement(settlement))
                    {
                        // Convert percentage to decimal (50% = 0.5f)
                        float chanceDecimal = Settings.MilitiaVeteranChance / 100f;
                        float originalChance = baseChance.ResultNumber;
                        baseChance.Add(chanceDecimal, VeteranChanceText);
                        ModLogger.Debug($"[CustomSettlementMilitiaModel] Applied veteran chance {Settings.MilitiaVeteranChance:F0}% to {settlement.Name}: {originalChance:F3} -> {baseChance.ResultNumber:F3}");
                    }
                }

                return baseChance;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomSettlementMilitiaModel] Error in CalculateVeteranMilitiaSpawnChance: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return base.CalculateVeteranMilitiaSpawnChance(settlement);
            }
        }
    }
}
