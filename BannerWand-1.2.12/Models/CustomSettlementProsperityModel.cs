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
    /// Custom settlement prosperity model that enables prosperity and hearth growth bonuses.
    /// Extends <see cref="DefaultSettlementProsperityModel"/> to add cheat functionality.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This model is registered with the game engine via <see cref="CampaignGameStarter.AddModel"/>
    /// in <see cref="Core.SubModule.RegisterCustomModels"/>. Once registered, the game automatically
    /// uses this model instead of the default for all prosperity and hearth calculations.
    /// </para>
    /// <para>
    /// Cheat features provided:
    /// - Prosperity Increase Multiplier: Adds a numerical bonus to daily prosperity growth for towns/castles (0-999)
    /// - Hearth Increase Multiplier: Adds a numerical bonus to daily hearth growth for villages (0-999)
    /// </para>
    /// <para>
    /// Note: These are NOT multipliers, but direct numerical additions to growth.
    /// Similar to how "Surplus Food" adds +6.3 prosperity, these cheats add the configured values.
    /// </para>
    /// </remarks>
    public class CustomSettlementProsperityModel : DefaultSettlementProsperityModel
    {
        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings? Settings => CheatSettings.Instance;

        /// <summary>
        /// Text object for prosperity bonus description (cached to avoid allocations).
        /// </summary>
        private static readonly TextObject ProsperityBonusText = new("BannerWand Prosperity Bonus");

        /// <summary>
        /// Text object for hearth bonus description (cached to avoid allocations).
        /// </summary>
        private static readonly TextObject HearthBonusText = new("BannerWand Hearth Bonus");

        /// <summary>
        /// Calculates the daily prosperity change for a town or castle.
        /// Adds configured bonus if enabled.
        /// </summary>
        /// <param name="fortification">The town or castle to calculate for. Cannot be null.</param>
        /// <param name="includeDescriptions">Whether to include detailed explanations in the result.</param>
        /// <returns>
        /// An <see cref="ExplainedNumber"/> containing the daily prosperity change value.
        /// Positive values increase prosperity, negative values decrease it.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Base game behavior:
        /// - Calculates prosperity change from food surplus, buildings, policies, etc.
        /// - Returns net change per day
        /// </para>
        /// <para>
        /// Cheat behavior:
        /// - If bonus > 0 and settlement qualifies, add the bonus value directly
        /// - This is a numerical addition, not a multiplier
        /// - Example: +50 bonus means +50 prosperity per day
        /// </para>
        /// <para>
        /// Applies to player-owned settlements and targeted NPC settlements.
        /// Uses SettlementCheatHelper to determine if settlement qualifies.
        /// </para>
        /// </remarks>
        public override ExplainedNumber CalculateProsperityChange(Town fortification, bool includeDescriptions = false)
        {
            try
            {
                // Get base value from default implementation
                ExplainedNumber baseChange = base.CalculateProsperityChange(fortification, includeDescriptions);

                // Early exit for null or unconfigured settings
                if (Settings == null || fortification?.Settlement == null)
                {
                    return baseChange;
                }

                // Apply prosperity bonus if enabled
                if (Settings.ProsperityIncreaseMultiplier > 0)
                {
                    if (SettlementCheatHelper.ShouldApplyCheatToSettlement(fortification.Settlement))
                    {
                        // Add numerical bonus directly (not a multiplier)
                        baseChange.Add(Settings.ProsperityIncreaseMultiplier, ProsperityBonusText);
                    }
                }

                return baseChange;
            }
            catch (Exception ex)
            {
                Error($"[CustomSettlementProsperityModel] Error in CalculateProsperityChange: {ex.Message}");
                Error($"Stack trace: {ex.StackTrace}");
                // Fallback to base implementation
                try
                {
                    return base.CalculateProsperityChange(fortification, includeDescriptions);
                }
                catch (Exception fallbackEx)
                {
                    Error($"[CustomSettlementProsperityModel] Fallback also failed: {fallbackEx.Message}");
                    // Last resort: return zero change to prevent crash
                    return new(0f, includeDescriptions, null);
                }
            }
        }

        /// <summary>
        /// Calculates the daily hearth change for a village.
        /// Adds configured bonus if enabled.
        /// </summary>
        /// <param name="village">The village to calculate for. Cannot be null.</param>
        /// <param name="includeDescriptions">Whether to include detailed explanations in the result.</param>
        /// <returns>
        /// An <see cref="ExplainedNumber"/> containing the daily hearth change value.
        /// Positive values increase hearth, negative values decrease it.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Base game behavior:
        /// - Calculates hearth change from village state, policies, perks, buildings, etc.
        /// - Returns net change per day
        /// - Hearth is the village equivalent of prosperity for towns/castles
        /// </para>
        /// <para>
        /// Cheat behavior:
        /// - If bonus > 0 and village's settlement qualifies, add the bonus value directly
        /// - This is a numerical addition, not a multiplier
        /// - Example: +50 bonus means +50 hearth per day
        /// </para>
        /// <para>
        /// Applies to player-owned villages and targeted NPC villages.
        /// Uses SettlementCheatHelper to determine if village's settlement qualifies.
        /// </para>
        /// </remarks>
        public override ExplainedNumber CalculateHearthChange(Village village, bool includeDescriptions = false)
        {
            try
            {
                // Get base value from default implementation
                ExplainedNumber baseChange = base.CalculateHearthChange(village, includeDescriptions);

                // Early exit for null or unconfigured settings
                if (Settings == null || village?.Settlement == null)
                {
                    return baseChange;
                }

                // Apply hearth bonus if enabled
                if (Settings.HearthIncreaseMultiplier > 0)
                {
                    if (SettlementCheatHelper.ShouldApplyCheatToSettlement(village.Settlement))
                    {
                        // Add numerical bonus directly (not a multiplier)
                        baseChange.Add(Settings.HearthIncreaseMultiplier, HearthBonusText);
                    }
                }

                return baseChange;
            }
            catch (Exception ex)
            {
                Error($"[CustomSettlementProsperityModel] Error in CalculateHearthChange: {ex.Message}");
                Error($"Stack trace: {ex.StackTrace}");
                // Fallback to base implementation
                try
                {
                    return base.CalculateHearthChange(village, includeDescriptions);
                }
                catch (Exception fallbackEx)
                {
                    Error($"[CustomSettlementProsperityModel] Fallback also failed: {fallbackEx.Message}");
                    // Last resort: return zero change to prevent crash
                    return new(0f, includeDescriptions, null);
                }
            }
        }
    }
}

