using BannerWand.Constants;
using BannerWand.Settings;
using BannerWand.Utils;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;

namespace BannerWand.Models
{
    /// <summary>
    /// Custom building construction model that enables instant settlement building construction.
    /// Extends <see cref="DefaultBuildingConstructionModel"/> to add cheat functionality without Harmony patches.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This model is registered with the game engine via <see cref="CampaignGameStarter.AddModel"/>
    /// in <see cref="Core.SubModule.RegisterCustomModels"/>. Once registered, the game automatically
    /// uses this model instead of the default for all building construction calculations.
    /// </para>
    /// <para>
    /// Cheat feature provided:
    /// - One Day Settlements Construction: Adds massive construction progress to complete buildings in one day for player and targeted NPC settlements
    /// </para>
    /// <para>
    /// Construction system in Bannerlord:
    /// - Buildings in towns/castles take days/weeks to construct
    /// - Daily progress is calculated based on governor, prosperity, etc.
    /// - This cheat adds 999,999 progress per day to complete any building instantly
    /// </para>
    /// </remarks>
    public class CustomBuildingConstructionModel : DefaultBuildingConstructionModel
    {
        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings Settings => CheatSettings.Instance;

        /// <summary>
        /// Gets the current target settings instance.
        /// </summary>
        private static CheatTargetSettings TargetSettings => CheatTargetSettings.Instance;

        /// <summary>
        /// Calculates daily construction progress for settlement buildings with cheat override.
        /// Overrides <see cref="DefaultBuildingConstructionModel.CalculateDailyConstructionPower"/>.
        /// </summary>
        /// <param name="town">The town where construction is happening. Cannot be null.</param>
        /// <param name="includeDescriptions">
        /// Whether to include detailed explanations in the result.
        /// When true, the returned ExplainedNumber contains human-readable descriptions of each modifier.
        /// </param>
        /// <returns>
        /// An <see cref="ExplainedNumber"/> containing the daily construction progress value.
        /// The value represents how much construction progress is made per day.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Construction progress calculation:
        /// - Base progress: Typically 10-100 per day depending on town stats
        /// - Building costs: Range from 100 to 1000+ progress points
        /// - Cheat adds: 999,999 progress (completes any building in one day)
        /// </para>
        /// <para>
        /// Why such a large number:
        /// - Most expensive buildings cost ~1000 progress
        /// - 999,999 ensures instant completion even for modded mega-buildings
        /// - Avoids edge cases where progress might not be enough
        /// </para>
        /// <para>
        /// Applies to player-owned settlements and targeted NPC settlements.
        /// Uses TargetFilter to determine if an NPC's settlement should receive the cheat.
        /// </para>
        /// <para>
        /// Performance consideration:
        /// - Called once per day per settlement with active construction
        /// - Not performance-critical due to infrequent calls
        /// - Early exit when settings are null or cheat is disabled
        /// </para>
        /// </remarks>
        public override ExplainedNumber CalculateDailyConstructionPower(Town town, bool includeDescriptions = false)
        {
            try
            {
                ExplainedNumber baseProgress = base.CalculateDailyConstructionPower(town, includeDescriptions);

                // Early exit for null or unconfigured settings
                if (Settings == null || TargetSettings == null || town == null)
                {
                    return baseProgress;
                }

                // Apply instant construction cheat when enabled
                if (Settings.OneDaySettlementsConstruction)
                {
                    bool shouldApplyInstantConstruction = ShouldApplyInstantConstructionToTown(town);

                    if (shouldApplyInstantConstruction)
                    {
                        baseProgress.Add(GameConstants.InstantConstructionPower, null);
                    }
                }

                return baseProgress;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CustomBuildingConstructionModel] Error in CalculateDailyConstructionPower: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return new ExplainedNumber(0f);
            }
        }

        /// <summary>
        /// Determines if instant construction should be applied to the specified town.
        /// </summary>
        /// <param name="town">The town to check. Cannot be null.</param>
        /// <returns>
        /// True if the town should receive instant construction, false otherwise.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method centralizes the logic for determining if a settlement qualifies for the cheat.
        /// It checks two main conditions:
        /// </para>
        /// <para>
        /// 1. Player-owned settlements: If the town belongs to the player's clan and ApplyToPlayer is enabled
        /// 2. NPC-owned settlements: If the town belongs to a targeted NPC clan (based on TargetFilter settings)
        /// </para>
        /// <para>
        /// This separation of concerns makes the code more maintainable and testable.
        /// </para>
        /// </remarks>
        private bool ShouldApplyInstantConstructionToTown(Town town)
        {
            // Check if this is player's settlement
            // ApplyToPlayer must be enabled for player settlements to receive the cheat
            if (town.OwnerClan == Clan.PlayerClan && TargetSettings.ApplyToPlayer)
            {
                return true;
            }

            // Check if this is a targeted NPC settlement
            // HasAnyNPCTargetEnabled checks if any NPC target options are enabled (companions, vassals, etc.)
            // ShouldApplyCheatToClan checks if the specific clan matches the target criteria
            return town.OwnerClan != Clan.PlayerClan &&
                TargetSettings.HasAnyNPCTargetEnabled() &&
                Utils.TargetFilter.ShouldApplyCheatToClan(town.OwnerClan);
        }
    }
}
