#nullable enable
using BannerWandRetro.Settings;
using BannerWandRetro.Utils;
using System;
using System.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;
using static BannerWandRetro.Utils.ModLogger;

namespace BannerWandRetro.Behaviors
{
    /// <summary>
    /// Campaign behavior that automatically starts the next building project after one completes.
    /// Only active when "One Day Settlements Construction" cheat is enabled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This behavior runs on daily tick to check all settlements and automatically start
    /// the next building project if the construction queue is empty and there are available
    /// buildings to upgrade.
    /// </para>
    /// <para>
    /// Works for both player and NPC settlements when the cheat is enabled.
    /// Uses the same target filter logic as other settlement cheats.
    /// </para>
    /// <para>
    /// Why CampaignBehavior instead of Harmony patch:
    /// - More reliable: Runs on daily tick, guaranteed to execute
    /// - Easier to debug: Clear execution flow
    /// - Better integration: Uses same target filter system as other cheats
    /// </para>
    /// </remarks>
    public class AutoBuildingQueueBehavior : CampaignBehaviorBase
    {
        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings? Settings => CheatSettings.Instance;

        /// <summary>
        /// Gets the current target settings instance.
        /// </summary>
        private static CheatTargetSettings? TargetSettings => CheatTargetSettings.Instance;

        #region Event Registration

        /// <summary>
        /// Registers this behavior to listen to campaign events.
        /// </summary>
        public override void RegisterEvents()
        {
            try
            {
                CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
            }
            catch (Exception ex)
            {
                Error($"[AutoBuildingQueueBehavior] Error in RegisterEvents: {ex.Message}");
                Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Synchronizes persistent data with save files.
        /// </summary>
        /// <param name="dataStore">The data store for save/load operations.</param>
        /// <remarks>
        /// This behavior has no persistent data to sync - all state is derived from settings.
        /// </remarks>
        public override void SyncData(IDataStore dataStore)
        {
            // No persistent data to sync - settings are managed by MCM
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Called every in-game day to check settlements and automatically start next building projects.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method checks all towns/castles and automatically starts the next building project
        /// if the construction queue is empty and there are available buildings to upgrade.
        /// </para>
        /// <para>
        /// Selection logic:
        /// - Gets all buildings that are not at maximum level (CurrentLevel < 3)
        /// - Filters out daily projects (IsDefaultProject = false in 1.2.12, IsDailyProject in 1.3.x)
        /// - Randomly selects one building to start
        /// - Adds it to the BuildingsInProgress queue
        /// </para>
        /// <para>
        /// Only processes settlements that qualify for the cheat (player settlements or targeted NPC settlements).
        /// </para>
        /// </remarks>
        private void OnDailyTick()
        {
            try
            {
                // Early exit if settings not available or cheat disabled
                CheatSettings? settings = Settings;
                CheatTargetSettings? targetSettings = TargetSettings;
                if (settings == null || targetSettings == null)
                {
                    return;
                }

                if (!settings.OneDaySettlementsConstruction)
                {
                    return;
                }

                // Early return if no targets enabled
                if (!targetSettings.ApplyToPlayer && !targetSettings.HasAnyNPCTargetEnabled())
                {
                    return;
                }

                int settlementsProcessed = 0;
                int buildingsStarted = 0;

                // Process all towns and castles
                foreach (Settlement settlement in Settlement.All)
                {
                    // Only process towns and castles (not villages)
                    if (settlement?.Town == null)
                    {
                        continue;
                    }

                    Town town = settlement.Town;

                    // Check if this settlement qualifies for the cheat
                    if (!ShouldProcessSettlement(town))
                    {
                        continue;
                    }

                    // Only proceed if the building queue is empty
                    if (town.BuildingsInProgress.Count > 0)
                    {
                        continue;
                    }

                    // Get all buildings that can be upgraded (not at max level and not daily projects)
                    // NOTE: In Bannerlord 1.2.12, daily projects are identified by IsDefaultProject property
                    // In version 1.3.x, this was renamed to IsDailyProject
                    MBReadOnlyList<Building> availableBuildings = town.Buildings
                        .Where(b => b.CurrentLevel < 3 && !b.BuildingType.IsDefaultProject)
                        .ToMBList();

                    // If no buildings available, skip this settlement
                    if (availableBuildings.Count == 0)
                    {
                        continue;
                    }

                    // Randomly select a building to start
                    int randomIndex = MBRandom.RandomInt(availableBuildings.Count);
                    Building? nextBuilding = availableBuildings[randomIndex];

                    if (nextBuilding != null)
                    {
                        // Add to queue to start construction
                        town.BuildingsInProgress.Enqueue(nextBuilding);
                        settlementsProcessed++;
                        buildingsStarted++;
                        Debug($"[AutoBuildingQueueBehavior] Automatically started building project: {nextBuilding.Name} (Level {nextBuilding.CurrentLevel}) in {town.Name}");
                    }
                }

                // Log summary if any buildings were started
                if (buildingsStarted > 0)
                {
                    Debug($"[AutoBuildingQueueBehavior] Processed {settlementsProcessed} settlements, started {buildingsStarted} building projects");
                }
            }
            catch (Exception ex)
            {
                Error($"[AutoBuildingQueueBehavior] Error in OnDailyTick: {ex.Message}");
                Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Determines if a settlement should be processed for automatic building queue.
        /// </summary>
        /// <param name="town">The town to check.</param>
        /// <returns>True if the town should receive automatic building queue, false otherwise.</returns>
        /// <remarks>
        /// Uses the same logic as other settlement cheats to determine if a settlement qualifies.
        /// Checks if it's a player settlement (with ApplyToPlayer enabled) or a targeted NPC settlement.
        /// </remarks>
        private static bool ShouldProcessSettlement(Town town)
        {
            if (town?.OwnerClan == null)
            {
                return false;
            }

            CheatTargetSettings? targetSettings = TargetSettings;
            if (targetSettings == null)
            {
                return false;
            }

            // Check if this is player's settlement
            if (town.OwnerClan == Clan.PlayerClan && targetSettings.ApplyToPlayer)
            {
                return true;
            }

            // Check if this is a targeted NPC settlement
            return town.OwnerClan != Clan.PlayerClan &&
                targetSettings.HasAnyNPCTargetEnabled() &&
                Utils.TargetFilter.ShouldApplyCheatToClan(town.OwnerClan);
        }

        #endregion
    }
}

