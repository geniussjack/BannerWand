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
    /// Custom settlement security model that enables security growth bonus.
    /// Extends <see cref="DefaultSettlementSecurityModel"/> to add cheat functionality.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This model is registered with the game engine via <see cref="CampaignGameStarter.AddModel"/>
    /// in <see cref="Core.SubModule.RegisterCustomModels"/>. Once registered, the game automatically
    /// uses this model instead of the default for all security calculations.
    /// </para>
    /// <para>
    /// Cheat feature provided:
    /// - Security Increase Multiplier: Adds a numerical bonus to daily security growth (0-999)
    /// </para>
    /// <para>
    /// Note: This is NOT a multiplier, but a direct numerical addition to security growth.
    /// Similar to how "Garrison" adds +19.18 security, this cheat adds the configured value.
    /// </para>
    /// </remarks>
    public class CustomSettlementSecurityModel : DefaultSettlementSecurityModel
    {
        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings? Settings => CheatSettings.Instance;

        /// <summary>
        /// Text object for security bonus description (cached to avoid allocations).
        /// </summary>
        private static readonly TextObject SecurityBonusText = new("BannerWand Security Bonus");

        /// <summary>
        /// Calculates the daily security change for a town.
        /// Adds configured bonus if enabled.
        /// </summary>
        /// <param name="town">The town to calculate for. Cannot be null.</param>
        /// <param name="includeDescriptions">Whether to include detailed explanations in the result.</param>
        /// <returns>
        /// An <see cref="ExplainedNumber"/> containing the daily security change value.
        /// Positive values increase security, negative values decrease it.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Base game behavior:
        /// - Calculates security change from garrison, prosperity, policies, buildings, etc.
        /// - Returns net change per day
        /// </para>
        /// <para>
        /// Cheat behavior:
        /// - If bonus > 0 and settlement qualifies, add the bonus value directly
        /// - This is a numerical addition, not a multiplier
        /// - Example: +50 bonus means +50 security per day
        /// </para>
        /// <para>
        /// Applies to player-owned settlements and targeted NPC settlements.
        /// Uses SettlementCheatHelper to determine if settlement qualifies.
        /// </para>
        /// </remarks>
        public override ExplainedNumber CalculateSecurityChange(Town town, bool includeDescriptions = false)
        {
            try
            {
                // Get base value from default implementation
                ExplainedNumber baseChange = base.CalculateSecurityChange(town, includeDescriptions);

                // Early exit for null or unconfigured settings
                if (Settings == null || town?.Settlement == null)
                {
                    return baseChange;
                }

                // Apply security bonus if enabled
                if (Settings.SecurityIncreaseMultiplier > 0)
                {
                    if (SettlementCheatHelper.ShouldApplyCheatToSettlement(town.Settlement))
                    {
                        // Add numerical bonus directly (not a multiplier)
                        baseChange.Add(Settings.SecurityIncreaseMultiplier, SecurityBonusText);
                    }
                }

                return baseChange;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomSettlementSecurityModel] Error in CalculateSecurityChange: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return base.CalculateSecurityChange(town, includeDescriptions);
            }
        }
    }
}
