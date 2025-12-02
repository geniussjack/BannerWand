#nullable enable
using BannerWand.Constants;
using BannerWand.Settings;
using BannerWand.Utils;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace BannerWand.Behaviors
{
    /// <summary>
    /// Campaign behavior that applies player-specific cheats during the campaign.
    /// Handles gold, influence, relationships, food, smithing, time manipulation, and game speed.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This behavior extends <see cref="CampaignBehaviorBase"/> and subscribes to campaign events
    /// (hourly tick, daily tick, session launch) to apply cheats at appropriate intervals.
    /// </para>
    /// <para>
    /// One-time cheats (gold, influence, attribute/focus points) use a flag system to ensure
    /// they're only applied once when the setting changes from 0 to a non-zero value.
    /// </para>
    /// <para>
    /// Performance: Most operations run on hourly or daily ticks, distributing load over time.
    /// </para>
    /// </remarks>
    public class PlayerCheatBehavior : CampaignBehaviorBase
    {
        #region Constants

        /// <summary>
        /// Default time speed value when feature is disabled.
        /// </summary>
        private const float DefaultTimeSpeed = 0f;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings Settings => CheatSettings.Instance!;

        /// <summary>
        /// Gets the current target settings instance.
        /// </summary>
        private static CheatTargetSettings TargetSettings => CheatTargetSettings.Instance!;

        /// <summary>
        /// Tracks whether gold has been applied to prevent repeated application.
        /// </summary>
        private bool _goldApplied = false;

        /// <summary>
        /// Tracks whether influence has been applied to prevent repeated application.
        /// </summary>
        private bool _influenceApplied = false;

        /// <summary>
        /// Stores the last time of day for freeze daytime feature.
        /// </summary>
        private float _lastTimeOfDay;

        /// <summary>
        /// Flag to track if Max All Character Relationships has been applied.
        /// </summary>
        private static bool _maxAllRelationshipsApplied;

        #endregion

        #region Event Registration

        /// <summary>
        /// Registers this behavior to listen to campaign events.
        /// </summary>
        /// <remarks>
        /// Events registered:
        /// - <see cref="CampaignEvents.DailyTickEvent"/> - For gold, influence, relationships
        /// - <see cref="CampaignEvents.HourlyTickEvent"/> - For food, materials, time, speed
        /// - <see cref="CampaignEvents.OnSessionLaunchedEvent"/> - For initialization and smithing
        /// </remarks>
        public override void RegisterEvents()
        {
            try
            {
                CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
                CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
                CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[PlayerCheatBehavior] Error in RegisterEvents: {ex.Message}");
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
                ModLogger.Error($"Exception in PlayerCheatBehavior.cs - {ex}: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                ModLogger.Error($"[PlayerCheatBehavior] Error in SyncData: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Called when a campaign session is launched (new game or loaded game).
        /// Resets one-time application flags and attempts to unlock smithy parts.
        /// </summary>
        /// <param name="starter">The campaign game starter instance.</param>
        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            ModLogger.Log("PlayerCheatBehavior session launched - resetting one-time flags");

            // Reset one-time application flags
            _goldApplied = false;
            _influenceApplied = false;
            _maxAllRelationshipsApplied = false;

            // Attempt to unlock smithy parts (if enabled)
            UnlockAllSmithyParts();
        }

        /// <summary>
        /// Called every in-game hour (approximately every 3-4 real-time seconds).
        /// Handles frequent updates: food, smithing materials, time freeze, game speed, and INSTANT gold/influence.
        /// </summary>
        private void OnHourlyTick()
        {
            // FIXED: Apply gold/influence HOURLY for near-instant response when user changes settings
            ApplyGoldAndInfluence();

            // Smithing materials - ensure player has enough for unlimited smithing
            ApplyUnlimitedSmithyMaterials();

            // Time manipulation - freeze daytime if enabled
            ApplyFreezeDaytime();

            // Game speed - adjust campaign map time flow
            ApplyGameSpeed();
        }

        /// <summary>
        /// Called every in-game day (approximately every minute of real time).
        /// Handles less frequent updates: relationships.
        /// </summary>
        private void OnDailyTick()
        {
            // Relationship management - instantly max all relationships
            ApplyMaxAllCharacterRelationships();

            // Note: Gold/influence and smithing materials are now applied hourly in OnHourlyTick() for faster response
        }

        #endregion

        #region Gold and Influence

        /// <summary>
        /// Applies gold and influence changes to player only (no NPCs).
        /// Uses a flag system to ensure one-time application, but applies immediately when value changes from 0.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The flag system works as follows:
        /// - When setting changes from 0 to non-zero, apply the change immediately and set flag
        /// - When setting returns to 0, clear the flag (ready for next application)
        /// - This allows immediate application: 0 → value applies immediately
        /// - To apply again: value → 0 → newValue applies immediately
        /// </para>
        /// </remarks>
        private void ApplyGoldAndInfluence()
        {
            // Apply gold if set and not yet applied (PLAYER ONLY)
            if (Settings.EditGold != 0 && !_goldApplied)
            {
                if (TargetSettings.ApplyToPlayer && Hero.MainHero is not null)
                {
                    Hero.MainHero.ChangeHeroGold(Settings.EditGold);
                    ModLogger.LogCheat("Gold Edit", true, Settings.EditGold, "player");
                }

                _goldApplied = true;
            }
            else if (Settings.EditGold == 0)
            {
                // Reset flag when setting returns to zero (ready for next application)
                _goldApplied = false;
            }

            // Apply influence if set and not yet applied (PLAYER ONLY)
            if (Settings.EditInfluence != 0 && !_influenceApplied)
            {
                if (TargetSettings.ApplyToPlayer && Clan.PlayerClan is not null)
                {
                    Clan.PlayerClan.Influence += Settings.EditInfluence;
                    ModLogger.LogCheat("Influence Edit", true, Settings.EditInfluence, "player clan");
                }

                _influenceApplied = true;
            }
            else if (Settings.EditInfluence == 0)
            {
                // Reset flag when setting returns to zero (ready for next application)
                _influenceApplied = false;
            }
        }

        #endregion

        #region Relationships

        /// <summary>
        /// Instantly maximizes player's relationship with ALL alive heroes in the game.
        /// This is a one-time application that sets relationships to 100 immediately.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Unlike the old implementation that processed 20 heroes per day, this new version:
        /// - Processes ALL alive heroes immediately when the cheat is first enabled
        /// - Uses Hero.AllAliveHeroes instead of cached collection for complete coverage
        /// - Sets relationships to maximum (100) in a single tick
        /// - Only runs once when cheat is enabled (uses flag system)
        /// </para>
        /// <para>
        /// Performance: O(n) where n = number of alive heroes (~200-400 in typical campaign).
        /// This runs only once when enabled, so the one-time cost is acceptable.
        /// </para>
        /// <para>
        /// This is the "Max All Character Relationships" cheat.
        /// For incremental relationship changes, see the RelationshipBoostPatch which handles
        /// the "Max Character Relationship" cheat (boosts any relation gain to +99).
        /// </para>
        /// </remarks>
        private static void ApplyMaxAllCharacterRelationships()
        {
            // Early return if cheat not enabled or player target disabled
            if (!Settings.MaxAllCharacterRelationships || !TargetSettings.ApplyToPlayer)
            {
                // Reset flag when cheat is disabled so it can be reapplied
                if (!Settings.MaxAllCharacterRelationships)
                {
                    _maxAllRelationshipsApplied = false;
                }
                return;
            }

            // Only apply once when enabled (flag system)
            if (_maxAllRelationshipsApplied)
            {
                return;
            }

            if (Hero.MainHero is null)
            {
                return;
            }

            // Process ALL alive heroes immediately - no daily limit
            int heroesImproved = 0;
            int totalHeroes = 0;

            // Use Hero.AllAliveHeroes for complete coverage of all kingdoms
            foreach (Hero hero in Hero.AllAliveHeroes)
            {
                totalHeroes++;

                // Skip player hero (player can't have relationship with themselves)
                if (hero == Hero.MainHero)
                {
                    continue;
                }

                // Get current relationship
                int currentRelation = Hero.MainHero.GetRelation(hero);

                // Only update if not already at max to avoid unnecessary work
                if (currentRelation < GameConstants.MaxRelationship)
                {
                    // Use CharacterRelationManager for direct relationship setting
                    CharacterRelationManager.SetHeroRelation(Hero.MainHero, hero, GameConstants.MaxRelationship);
                    heroesImproved++;
                }
            }

            // Mark as applied
            _maxAllRelationshipsApplied = true;

            // Log success
            ModLogger.Log($"[Max All Relationships] Maximized relationships with {heroesImproved} heroes out of {totalHeroes} total alive heroes");
            ModLogger.LogCheat("Max All Character Relationships", true, heroesImproved, "heroes");
        }

        #endregion

        #region Smithy Materials and Parts

        /// <summary>
        /// Ensures player has unlimited smithing materials by restoring them when low.
        /// Prevents running out of materials during smithing sessions.
        /// </summary>
        /// <remarks>
        /// <para>
        /// ENHANCED VERSION (v2.0.1):
        /// - Extended material ID list to support multiple game versions
        /// - Comprehensive logging for troubleshooting
        /// - Auto-detection of available materials
        /// </para>
        /// <para>
        /// With the Harmony patch preventing consumption, this method now serves as a fallback
        /// to ensure player always has materials available even if they start with zero.
        /// </para>
        /// <para>
        /// All smithing materials monitored (with alternative IDs for different game versions):
        /// - Hardwood: "hardwood", "hard_wood"
        /// - Charcoal: "charcoal", "coal"
        /// - Iron Ore: "iron_ore", "ironore", "crude_iron", "iron1"
        /// - Wrought Iron: "iron2", "wrought_iron"
        /// - Iron: "iron", "iron3"
        /// - Steel: "iron4", "steel"
        /// - Fine Steel: "iron5", "fine_steel"
        /// - Thamaskene Steel: "iron6", "thamaskene_steel"
        /// </para>
        /// <para>
        /// Replenishment strategy: When stock falls below threshold, top up to 9999.
        /// This ensures materials never run out completely.
        /// </para>
        /// </remarks>
        private static void ApplyUnlimitedSmithyMaterials()
        {
            // Early returns for disabled cheat or missing data
            if (!Settings.UnlimitedSmithyMaterials || !TargetSettings.ApplyToPlayer)
            {
                return;
            }

            if (Hero.MainHero?.PartyBelongedTo?.ItemRoster is null)
            {
                return;
            }

            ItemRoster itemRoster = Hero.MainHero.PartyBelongedTo.ItemRoster;

            // DIAGNOSTIC: Log available smithing materials (only once per session)
            LogAvailableSmithingMaterials(itemRoster);

            // Get target amount from settings (user-configurable)
            int targetAmount = Settings.SmithyMaterialsQuantity;
            int threshold = targetAmount - 10; // Replenish when below this

            // Replenish all smithing materials to user-configured amount
            // Using CORRECT IDs for Bannerlord 1.3.x

            // Hardwood
            AddSmithingMaterialIfLow(itemRoster, "hardwood", targetAmount, threshold);

            // Charcoal
            AddSmithingMaterialIfLow(itemRoster, "charcoal", targetAmount, threshold);

            // Iron Ore (raw material)
            AddSmithingMaterialIfLow(itemRoster, "iron", targetAmount, threshold);

            // Crude Iron (tier 1 ingot)
            AddSmithingMaterialIfLow(itemRoster, "ironIngot1", targetAmount, threshold);

            // Wrought Iron (tier 2 ingot)
            AddSmithingMaterialIfLow(itemRoster, "ironIngot2", targetAmount, threshold);

            // Iron (tier 3 ingot)
            AddSmithingMaterialIfLow(itemRoster, "ironIngot3", targetAmount, threshold);

            // Steel (tier 4 ingot)
            AddSmithingMaterialIfLow(itemRoster, "ironIngot4", targetAmount, threshold);

            // Fine Steel (tier 5 ingot)
            AddSmithingMaterialIfLow(itemRoster, "ironIngot5", targetAmount, threshold);

            // Thamaskene Steel (tier 6 ingot)
            AddSmithingMaterialIfLow(itemRoster, "ironIngot6", targetAmount, threshold);
        }

        /// <summary>
        /// Adds smithing material to roster to maintain target amount.
        /// </summary>
        /// <param name="roster">The item roster to modify.</param>
        /// <param name="itemId">The Bannerlord item ID (e.g., "iron", "charcoal").</param>
        /// <param name="targetAmount">The desired amount to maintain.</param>
        /// <param name="threshold">Replenish when stock falls below this threshold.</param>
        /// <remarks>
        /// This ensures materials are replenished when used during smithing sessions.
        /// </remarks>
        private static void AddSmithingMaterialIfLow(ItemRoster roster, string itemId, int targetAmount, int threshold)
        {
            try
            {
                ItemObject? item = Game.Current?.ObjectManager.GetObject<ItemObject>(itemId);
                if (item is null)
                {
                    return;
                }

                int currentAmount = roster.GetItemNumber(item);

                // Replenish if below threshold (meaning materials were used)
                if (currentAmount < threshold)
                {
                    int amountToAdd = targetAmount - currentAmount;
                    _ = roster.AddToCounts(item, amountToAdd);
                    ModLogger.Log($"[Smithing] Restored {amountToAdd}× {item.Name} (from {currentAmount} to {targetAmount})");
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"Exception in PlayerCheatBehavior.cs - {ex}: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                ModLogger.Debug($"[Smithing] Could not add material '{itemId}': {ex.Message}");
            }
        }

        /// <summary>
        /// Unlocks all smithy crafting parts for the player.
        /// </summary>
        /// <remarks>
        /// <para>
        /// WARNING: This feature is currently DISABLED due to API changes in recent Bannerlord versions.
        /// The methods <c>Hero.IsAlreadyKnowingPiece</c> and <c>Hero.AddSmithingPieceData</c>
        /// no longer exist in the current API.
        /// </para>
        /// <para>
        /// TODO: Find alternative API to unlock smithing pieces in current Bannerlord version.
        /// Possible approaches:
        /// - Check for new API in CraftingPiece or Hero classes
        /// - Use reflection to access internal methods (not recommended)
        /// - Wait for official mod support in future game updates
        /// </para>
        /// </remarks>
        public static void UnlockAllSmithyParts()
        {
            try
            {                // DISABLED: API methods no longer available in current Bannerlord version
                // See remarks for details and potential solutions

                if (!Settings.UnlockAllSmithyParts || !TargetSettings.ApplyToPlayer)
                {
                    return;
                }

                ModLogger.Debug("Unlock All Smithy Parts is enabled but feature is disabled (API not available)");

                /*
                // Original implementation (no longer works):
                if (Hero.MainHero is null)
                    return;

                foreach (var craftingPiece in CraftingPiece.All)
                {
                    if (!Hero.MainHero.IsAlreadyKnowingPiece(craftingPiece))
                    {
                        Hero.MainHero.AddSmithingPieceData(craftingPiece);
                    }
                }
                */

            }
            catch (Exception ex)
            {
                ModLogger.Error($"Exception in PlayerCheatBehavior.cs - {ex}: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                ModLogger.Error($"[PlayerCheatBehavior] Error in UnlockAllSmithyParts: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// DIAGNOSTIC METHOD: Logs all smithing materials currently in player's inventory.
        /// Use this to discover the correct item IDs for your game version.
        /// </summary>
        /// <param name="roster">The item roster to scan.</param>
        /// <remarks>
        /// <para>
        /// This method is for debugging only. It scans the player's inventory and logs
        /// all items that might be smithing materials based on their properties.
        /// </para>
        /// <para>
        /// To use: Uncomment the call in ApplyUnlimitedSmithyMaterials() and check the log file.
        /// </para>
        /// </remarks>
        private static void LogAvailableSmithingMaterials(ItemRoster roster)
        {
            // Only log once per session to avoid spam
            if (_smithingMaterialsLogged)
            {
                return;
            }

            ModLogger.Log("========== SMITHING MATERIALS DIAGNOSTIC ==========");
            ModLogger.Log("Scanning player inventory for smithing materials...");

            int rosterCount = roster.Count;
            int materialsFound = 0;

            for (int i = 0; i < rosterCount; i++)
            {
                ItemRosterElement element = roster.GetElementCopyAtIndex(i);
                ItemObject item = element.EquipmentElement.Item;

                if (item is null)
                {
                    continue;
                }

                // Log items that look like smithing materials
                // Usually they have "iron", "steel", "charcoal", "wood" in their ID or are marked as crafting materials
                string itemId = item.StringId.ToLower();
                if (itemId.Contains("iron") || itemId.Contains("steel") ||
                    itemId.Contains("charcoal") || itemId.Contains("coal") ||
                    itemId.Contains("wood") || itemId.Contains("hardwood") ||
                    item.ItemCategory?.StringId == "craftingmaterial")
                {
                    ModLogger.Log($"  - ID: '{item.StringId}', Name: '{item.Name}', Amount: {element.Amount}, Category: {item.ItemCategory?.StringId ?? "none"}");
                    materialsFound++;
                }
            }

            if (materialsFound == 0)
            {
                ModLogger.Log("  No smithing materials found in inventory.");
                ModLogger.Log("  SUGGESTION: Buy some iron ore, charcoal, or hardwood from a town, then check the log again.");
            }
            else
            {
                ModLogger.Log($"Found {materialsFound} potential smithing materials.");
            }

            ModLogger.Log("====================================================");

            _smithingMaterialsLogged = true;
        }

        /// <summary>
        /// Flag to ensure smithing materials are only logged once per session.
        /// </summary>
        private static bool _smithingMaterialsLogged = false;

        #endregion

        #region Time and Game Speed

        /// <summary>
        /// [WIP] Freezes the campaign time of day when enabled.
        /// Currently disabled - freezes entire game instead of just time.
        /// </summary>
        /// <remarks>
        /// <para>
        /// PROBLEM: Campaign.Current.SetTimeSpeed(0) stops ALL game processing,
        /// not just the visual time progression. This makes the game unplayable.
        /// </para>
        /// <para>
        /// TODO: Find alternative method to freeze time of day visual only
        /// without stopping game logic, events, and player controls.
        /// </para>
        /// </remarks>
        private void ApplyFreezeDaytime()
        {
            if (!Settings.FreezeDaytime || !TargetSettings.ApplyToPlayer)
            {
                // Reset stored time when disabled
                _lastTimeOfDay = DefaultTimeSpeed;
                return;
            }

            if (Campaign.Current is null)
            {
                return;
            }

            // Store the time when first enabled
            if (_lastTimeOfDay == DefaultTimeSpeed)
            {
                _lastTimeOfDay = Campaign.CurrentTime;
                ModLogger.Debug($"Daytime frozen at: {_lastTimeOfDay}");
            }

            // [WIP] DISABLED: This stops the entire game, not just time
            // TODO: Find alternative method to freeze time of day visual only
            // Campaign.Current.SetTimeSpeed(0);
        }

        /// <summary>
        /// Applies custom campaign map time speed multiplier.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Game speed range: 0 (frozen) to 16 (very fast).
        /// Default game speed is 1 (normal time flow).
        /// </para>
        /// <para>
        /// WARNING: Extreme speeds (&gt;10) can cause instability and are auto-reset
        /// on game start by <see cref="SubModule.AutoResetDangerousSettings"/>.
        /// </para>
        /// </remarks>
        private static void ApplyGameSpeed()
        {
            // Early returns for disabled or invalid settings
            if (Settings.GameSpeed <= DefaultTimeSpeed || !TargetSettings.ApplyToPlayer)
            {
                return;
            }

            if (Campaign.Current is null)
            {
                return;
            }

            // Apply the time speed multiplier
            // Note: SetTimeSpeed expects an int, so we cast the float
            int gameSpeed = (int)Settings.GameSpeed;
            Campaign.Current.SetTimeSpeed(gameSpeed);
        }

        #endregion
    }
}
