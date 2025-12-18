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
    /// Custom settlement food model that enables food growth bonus.
    /// Extends <see cref="DefaultSettlementFoodModel"/> to add cheat functionality.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This model is registered with the game engine via <see cref="CampaignGameStarter.AddModel"/>
    /// in <see cref="Core.SubModule.RegisterCustomModels"/>. Once registered, the game automatically
    /// uses this model instead of the default for all food calculations.
    /// </para>
    /// <para>
    /// Cheat feature provided:
    /// - Food Increase Multiplier: Adds a numerical bonus to daily food growth (0-999)
    /// </para>
    /// <para>
    /// Note: This is NOT a multiplier, but a direct numerical addition to food growth.
    /// Similar to how "Hunting Rights" policy adds +2 food, this cheat adds the configured value.
    /// </para>
    /// </remarks>
    public class CustomSettlementFoodModel : DefaultSettlementFoodModel
    {
        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings? Settings => CheatSettings.Instance;

        /// <summary>
        /// Text object for food bonus description (cached to avoid allocations).
        /// </summary>
        private static readonly TextObject FoodBonusText = new("BannerWand Food Bonus");

        /// <summary>
        /// Calculates the daily food stocks change for a town.
        /// Adds configured bonus if enabled.
        /// </summary>
        /// <param name="town">The town to calculate for. Cannot be null.</param>
        /// <param name="includeMarketStocks">Whether to include market stocks in calculation.</param>
        /// <param name="includeDescriptions">Whether to include detailed explanations in the result.</param>
        /// <returns>
        /// An <see cref="ExplainedNumber"/> containing the daily food change value.
        /// Positive values increase food, negative values decrease it.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Base game behavior:
        /// - Calculates food production from villages, buildings, policies, etc.
        /// - Calculates food consumption from prosperity, garrison, etc.
        /// - Returns net change (production - consumption)
        /// </para>
        /// <para>
        /// Cheat behavior:
        /// - If bonus > 0 and settlement qualifies, add the bonus value directly
        /// - This is a numerical addition, not a multiplier
        /// - Example: +50 bonus means +50 food per day
        /// </para>
        /// <para>
        /// Applies to player-owned settlements and targeted NPC settlements.
        /// Uses SettlementCheatHelper to determine if settlement qualifies.
        /// </para>
        /// </remarks>
        public override ExplainedNumber CalculateTownFoodStocksChange(Town town, bool includeMarketStocks = true, bool includeDescriptions = false)
        {
            try
            {
                // Get base value from default implementation
                ExplainedNumber baseChange = base.CalculateTownFoodStocksChange(town, includeMarketStocks, includeDescriptions);

                // Early exit for null or unconfigured settings
                if (Settings == null || town?.Settlement == null)
                {
                    return baseChange;
                }

                // Apply food bonus if enabled
                if (Settings.FoodIncreaseMultiplier > 0)
                {
                    if (SettlementCheatHelper.ShouldApplyCheatToSettlement(town.Settlement))
                    {
                        // Add numerical bonus directly (not a multiplier)
                        baseChange.Add(Settings.FoodIncreaseMultiplier, FoodBonusText);
                    }
                }

                return baseChange;
            }
            catch (Exception ex)
            {
                Error($"[CustomSettlementFoodModel] Error in CalculateTownFoodStocksChange: {ex.Message}");
                Error($"Stack trace: {ex.StackTrace}");
                // Fallback to base implementation
                try
                {
                    return base.CalculateTownFoodStocksChange(town, includeMarketStocks, includeDescriptions);
                }
                catch (Exception fallbackEx)
                {
                    Error($"[CustomSettlementFoodModel] Fallback also failed: {fallbackEx.Message}");
                    // Last resort: return zero change to prevent crash
                    return new(0f, includeDescriptions, null);
                }
            }
        }
    }
}

