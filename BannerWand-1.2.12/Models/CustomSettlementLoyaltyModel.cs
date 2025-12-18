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
    /// Custom settlement loyalty model that enables loyalty growth bonus.
    /// Extends <see cref="DefaultSettlementLoyaltyModel"/> to add cheat functionality.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This model is registered with the game engine via <see cref="CampaignGameStarter.AddModel"/>
    /// in <see cref="Core.SubModule.RegisterCustomModels"/>. Once registered, the game automatically
    /// uses this model instead of the default for all loyalty calculations.
    /// </para>
    /// <para>
    /// Cheat feature provided:
    /// - Loyalty Increase Multiplier: Adds a numerical bonus to daily loyalty growth (0-999)
    /// </para>
    /// <para>
    /// Note: This is NOT a multiplier, but a direct numerical addition to loyalty growth.
    /// Similar to how "Parade" adds +50 loyalty, this cheat adds the configured value.
    /// </para>
    /// </remarks>
    public class CustomSettlementLoyaltyModel : DefaultSettlementLoyaltyModel
    {
        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings? Settings => CheatSettings.Instance;

        /// <summary>
        /// Text object for loyalty bonus description (cached to avoid allocations).
        /// </summary>
        private static readonly TextObject LoyaltyBonusText = new("BannerWand Loyalty Bonus");

        /// <summary>
        /// Calculates the daily loyalty change for a town.
        /// Adds configured bonus if enabled.
        /// </summary>
        /// <param name="town">The town to calculate for. Cannot be null.</param>
        /// <param name="includeDescriptions">Whether to include detailed explanations in the result.</param>
        /// <returns>
        /// An <see cref="ExplainedNumber"/> containing the daily loyalty change value.
        /// Positive values increase loyalty, negative values decrease it.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Base game behavior:
        /// - Calculates loyalty change from governor culture, owner culture, security, policies, etc.
        /// - Returns net change per day
        /// </para>
        /// <para>
        /// Cheat behavior:
        /// - If bonus > 0 and settlement qualifies, add the bonus value directly
        /// - This is a numerical addition, not a multiplier
        /// - Example: +50 bonus means +50 loyalty per day
        /// </para>
        /// <para>
        /// Applies to player-owned settlements and targeted NPC settlements.
        /// Uses SettlementCheatHelper to determine if settlement qualifies.
        /// </para>
        /// </remarks>
        public override ExplainedNumber CalculateLoyaltyChange(Town town, bool includeDescriptions = false)
        {
            try
            {
                // Get base value from default implementation
                ExplainedNumber baseChange = base.CalculateLoyaltyChange(town, includeDescriptions);

                // Early exit for null or unconfigured settings
                if (Settings == null || town?.Settlement == null)
                {
                    return baseChange;
                }

                // Apply loyalty bonus if enabled
                if (Settings.LoyaltyIncreaseMultiplier > 0)
                {
                    if (SettlementCheatHelper.ShouldApplyCheatToSettlement(town.Settlement))
                    {
                        // Add numerical bonus directly (not a multiplier)
                        baseChange.Add(Settings.LoyaltyIncreaseMultiplier, LoyaltyBonusText);
                    }
                }

                return baseChange;
            }
            catch (Exception ex)
            {
                Error($"[CustomSettlementLoyaltyModel] Error in CalculateLoyaltyChange: {ex.Message}");
                Error($"Stack trace: {ex.StackTrace}");
                // Fallback to base implementation
                try
                {
                    return base.CalculateLoyaltyChange(town, includeDescriptions);
                }
                catch (Exception fallbackEx)
                {
                    Error($"[CustomSettlementLoyaltyModel] Fallback also failed: {fallbackEx.Message}");
                    // Last resort: return zero change to prevent crash
                    return new(0f, includeDescriptions, null);
                }
            }
        }
    }
}

