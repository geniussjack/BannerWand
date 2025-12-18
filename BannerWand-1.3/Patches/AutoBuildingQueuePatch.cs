#nullable enable
// System namespaces
using System;
using System.Linq;

// Third-party namespaces
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Buildings;
using TaleWorlds.Core;
using TaleWorlds.Library;

// Project namespaces
using BannerWand.Settings;
using BannerWand.Utils;
using static BannerWand.Utils.ModLogger;

namespace BannerWand.Patches
{
    /// <summary>
    /// Harmony patch that automatically starts the next building project after one completes.
    /// Only active when "One Day Settlements Construction" cheat is enabled.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This patch intercepts <see cref="BuildingHelper.CheckIfBuildingIsComplete"/> to detect
    /// when a building project finishes. After completion, it automatically selects and starts
    /// a random building project that is not at maximum level.
    /// </para>
    /// <para>
    /// Works for both player and NPC settlements when the cheat is enabled.
    /// </para>
    /// </remarks>
    [HarmonyLib.HarmonyPatch(typeof(Helpers.BuildingHelper), nameof(Helpers.BuildingHelper.CheckIfBuildingIsComplete))]
    public static class AutoBuildingQueuePatch
    {
        /// <summary>
        /// Postfix method that runs after building completion check.
        /// Automatically starts next building project if cheat is enabled and queue is empty.
        /// </summary>
        /// <param name="building">The building that was checked for completion.</param>
        /// <remarks>
        /// <para>
        /// This method is called after the original <see cref="BuildingHelper.CheckIfBuildingIsComplete"/>
        /// has processed building completion. If a building was completed and removed from the queue,
        /// this method checks if the cheat is enabled and automatically starts the next project.
        /// </para>
        /// <para>
        /// Selection logic:
        /// - Gets all buildings that are not at maximum level (CurrentLevel < 3)
        /// - Filters out daily projects (IsDailyProject = false)
        /// - Randomly selects one building to start
        /// - Adds it to the BuildingsInProgress queue
        /// </para>
        /// </remarks>
        [HarmonyLib.HarmonyPostfix]
        public static void Postfix(Building building)
        {
            try
            {
                // Early exit if settings not available or cheat disabled
                CheatSettings? settings = CheatSettings.Instance;
                if (settings?.OneDaySettlementsConstruction != true)
                {
                    return;
                }

                // Early exit if building or town is null
                if (building?.Town == null)
                {
                    return;
                }

                Town town = building.Town;

                // Only proceed if the building queue is now empty (building was just completed)
                if (town.BuildingsInProgress.Any())
                {
                    return;
                }

                // Get all buildings that can be upgraded (not at max level and not daily projects)
                MBReadOnlyList<Building> availableBuildings = town.Buildings
                    .Where(b => b.CurrentLevel < 3 && !b.BuildingType.IsDailyProject)
                    .ToMBList();

                // If no buildings available, exit
                if (availableBuildings.Count == 0)
                {
                    return;
                }

                // Randomly select a building to start
                int randomIndex = MBRandom.RandomInt(availableBuildings.Count);
                Building? nextBuilding = availableBuildings[randomIndex];

                if (nextBuilding != null)
                {
                    // Add to queue to start construction
                    town.BuildingsInProgress.Enqueue(nextBuilding);
                    Debug($"[AutoBuildingQueuePatch] Automatically started building project: {nextBuilding.Name} (Level {nextBuilding.CurrentLevel}) in {town.Name}");
                }
            }
            catch (Exception ex)
            {
                Error($"[AutoBuildingQueuePatch] Error in Postfix: {ex.Message}");
                Error($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
