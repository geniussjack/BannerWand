#nullable enable
using BannerWand.Constants;
using BannerWand.Settings;
using BannerWand.Utils;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace BannerWand.Behaviors
{
    /// <summary>
    /// Campaign behavior that ensures unlimited food supply for player's party and targeted NPC parties.
    /// Provides a simpler, safer alternative to overriding the food consumption model.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This behavior works in tandem with <see cref="Models.CustomMobilePartyFoodConsumptionModel"/>.
    /// The model prevents food consumption, while this behavior ensures a minimum food stock.
    /// </para>
    /// <para>
    /// Why both approaches:
    /// - Model override: Prevents automatic food consumption (most important)
    /// - This behavior: Safety net that adds food if it somehow gets low (backup)
    /// </para>
    /// <para>
    /// Performance: Runs every in-game hour, but only adds food when below threshold.
    /// Very lightweight - just one roster check and one conditional add per hour per party.
    /// </para>
    /// </remarks>
    public class FoodCheatBehavior : CampaignBehaviorBase
    {
        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings Settings => CheatSettings.Instance!;

        /// <summary>
        /// Gets the current target settings instance.
        /// </summary>
        private static CheatTargetSettings TargetSettings => CheatTargetSettings.Instance!;

        #region Event Registration

        /// <summary>
        /// Registers this behavior to listen to campaign events.
        /// </summary>
        /// <remarks>
        /// Only subscribes to <see cref="CampaignEvents.HourlyTickEvent"/> as food
        /// consumption happens gradually over time, not instantaneously.
        /// </remarks>
        public override void RegisterEvents()
        {
            try
            {
                CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[FoodCheatBehavior] Error in RegisterEvents: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
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
            try
            {
            }// No persistent data to sync - settings are managed by MCM            }
            catch (Exception ex)
            {
                ModLogger.Error($"[FoodCheatBehavior] Error in SyncData: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Called every in-game hour to monitor and replenish food supply for targeted parties.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Food consumption in Bannerlord happens gradually over hours/days.
        /// Checking hourly is sufficient to catch and prevent starvation.
        /// </para>
        /// <para>
        /// Replenishment strategy:
        /// - Check total food count in party inventory
        /// - If below threshold (10), add grain to bring it to safe level (50)
        /// - Use grain as it's a basic, universal food item available in all games
        /// </para>
        /// </remarks>
        private void OnHourlyTick()
        {
            // Early returns for disabled cheat
            if (!Settings.UnlimitedFood)
            {
                return;
            }

            // Early return if no targets enabled
            if (!TargetSettings.ApplyToPlayer && !TargetSettings.HasAnyNPCTargetEnabled())
            {
                return;
            }

            // Get grain item once (reuse for all parties to avoid repeated lookups)
            ItemObject? grainItem = Game.Current?.ObjectManager.GetObject<ItemObject>("grain");
            if (grainItem is null)
            {
                ModLogger.Warning("Failed to add food: 'grain' item not found in game object manager");
                return;
            }

            int partiesReplenished = 0;

            // Apply to player's party if enabled
            if (TargetSettings.ApplyToPlayer && MobileParty.MainParty?.ItemRoster is not null)
            {
                if (ReplenishPartyFood(MobileParty.MainParty, grainItem))
                {
                    partiesReplenished++;
                }
            }

            // Apply to NPC parties if any NPC targets are enabled
            if (TargetSettings.HasAnyNPCTargetEnabled())
            {
                foreach (MobileParty party in MobileParty.All)
                {
                    // Skip player party (already handled) and parties without item roster
                    if (party == MobileParty.MainParty || party.ItemRoster is null)
                    {
                        continue;
                    }

                    // Check if this party's leader should receive cheats
                    if (TargetFilter.ShouldApplyCheatToParty(party))
                    {
                        if (ReplenishPartyFood(party, grainItem))
                        {
                            partiesReplenished++;
                        }
                    }
                }
            }

            // Log summary if any parties were replenished
            if (partiesReplenished > 0)
            {
                ModLogger.Debug($"Replenished food for {partiesReplenished} parties");
            }
        }

        /// <summary>
        /// Replenishes food for a single party if below threshold.
        /// </summary>
        /// <param name="party">The party to replenish.</param>
        /// <param name="grainItem">The grain item to add.</param>
        /// <returns>True if food was added, false otherwise.</returns>
        /// <remarks>
        /// Only adds food when current supply falls below <see cref="GameConstants.MinFoodThreshold"/>.
        /// This prevents unnecessary roster manipulations and maintains reasonable food levels.
        /// </remarks>
        private static bool ReplenishPartyFood(MobileParty party, ItemObject grainItem)
        {
            if (party?.ItemRoster is null)
            {
                return false;
            }

            // Check current food supply
            int totalFood = party.ItemRoster.TotalFood;

            // Only add food if below safety threshold (optimization: avoid unnecessary roster updates)
            if (totalFood < GameConstants.MinFoodThreshold)
            {
                _ = party.ItemRoster.AddToCounts(grainItem, GameConstants.FoodReplenishAmount);
                return true;
            }

            return false;
        }

        #endregion
    }
}
