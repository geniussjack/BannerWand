#nullable enable
using BannerWandRetro.Constants;
using BannerWandRetro.Settings;
using BannerWandRetro.Utils;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;
using static BannerWandRetro.Utils.ModLogger;

namespace BannerWandRetro.Models
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
        /// - Militia Veteran Chance: NOT AVAILABLE in Bannerlord 1.2.12 (method added in 1.3.x)
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

        // NOTE: VeteranChanceText removed - CalculateVeteranMilitiaSpawnChance not available in 1.2.12

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
        /// - If multiplier > 0 and settlement qualifies, add bonus directly
        /// - This is a numerical addition, not a multiplier
        /// - Example: +10 bonus means +10 militiamen per day
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
                Error($"[CustomSettlementMilitiaModel] Error in CalculateMilitiaChange: {ex.Message}");
                Error($"Stack trace: {ex.StackTrace}");
                // Fallback to base implementation
                try
                {
                    return base.CalculateMilitiaChange(settlement, includeDescriptions);
                }
                catch (Exception fallbackEx)
                {
                    Error($"[CustomSettlementMilitiaModel] Fallback also failed: {fallbackEx.Message}");
                    // Last resort: return zero change to prevent crash
                    return new(0f, includeDescriptions, null);
                }
            }
        }

        // NOTE: CalculateVeteranMilitiaSpawnChance method does not exist in DefaultSettlementMilitiaModel for Bannerlord 1.2.12
        // This method was added in version 1.3.x, so we cannot override it in 1.2.12
        // The MilitiaVeteranChance setting will not work in 1.2.12 version
    }
}

