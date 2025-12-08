#nullable enable
using BannerWandRetro.Constants;
using BannerWandRetro.Settings;
using BannerWandRetro.Utils;
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace BannerWandRetro.Behaviors
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
        #region Properties

        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings? Settings => CheatSettings.Instance;

        /// <summary>
        /// Gets the current target settings instance.
        /// </summary>
        private static CheatTargetSettings? TargetSettings => CheatTargetSettings.Instance;

        /// <summary>
        /// Tracks whether gold has been applied to prevent repeated application.
        /// </summary>
        private bool _goldApplied;

        /// <summary>
        /// Tracks whether influence has been applied to prevent repeated application.
        /// </summary>
        private bool _influenceApplied;

        /// <summary>
        /// Flag to track if Max All Character Relationships has been applied.
        /// </summary>
        private static bool _maxAllRelationshipsApplied;


        /// <summary>
        /// Backup of player's inventory for trade restoration.
        /// Key: ItemObject, Value: Original amount when backup was first created
        /// </summary>
        private Dictionary<ItemObject, int>? _inventoryBackup;

        /// <summary>
        /// Flag to track if inventory backup has been initialized.
        /// Prevents newly looted items from being added to the backup.
        /// </summary>
        private bool _inventoryBackupInitialized;

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
                LogException(ex, nameof(RegisterEvents));
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
                // No persistent data to sync - settings are managed by MCM
            }
            catch (Exception ex)
            {
                LogException(ex, nameof(SyncData));
            }
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Called when a campaign session is launched (new game or loaded game).
        /// Resets one-time application flags.
        /// </summary>
        /// <param name="starter">The campaign game starter instance.</param>
        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            ModLogger.Log("PlayerCheatBehavior session launched - resetting one-time flags");

            // Reset one-time application flags
            _goldApplied = false;
            _influenceApplied = false;
            _maxAllRelationshipsApplied = false;
            _inventoryBackupInitialized = false;
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

            // Game speed - adjust campaign map time flow
            ApplyGameSpeed();

            // Trade items restoration - check and restore removed items
            CheckAndRestoreTradeItems();
        }

        /// <summary>
        /// Called every in-game day (approximately every minute of real time).
        /// Handles less frequent updates: relationships.
        /// </summary>
        private void OnDailyTick()
        {
            // Relationship management - instantly max all relationships
            ApplyMaxAllCharacterRelationships();

            // Note: Smithing materials are now applied hourly in OnHourlyTick() for faster response
        }

        #endregion

        #region Gold and Influence

        /// <summary>
        /// Applies gold and influence changes to player only (no NPCs).
        /// Uses a flag system to ensure one-time application.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The flag system works as follows:
        /// - When setting changes from 0 to non-zero, apply the change and set flag
        /// - When setting returns to 0, clear the flag
        /// - This allows multiple applications by toggling: 0 → value → 0 → value
        /// </para>
        /// </remarks>
        private void ApplyGoldAndInfluence()
        {
            // Early exit if settings are null
            CheatSettings? settings = Settings;
            CheatTargetSettings? targetSettings = TargetSettings;
            if (settings is null || targetSettings is null)
            {
                return;
            }

            // Apply gold if set and not yet applied (PLAYER ONLY)
            if (settings.EditGold != 0 && !_goldApplied)
            {
                if (targetSettings.ApplyToPlayer && Hero.MainHero is not null)
                {
                    Hero.MainHero.ChangeHeroGold(settings.EditGold);
                    ModLogger.LogCheat("Gold Edit", true, settings.EditGold, "player");
                    // Only set flag after confirming the change was actually applied
                    _goldApplied = true;
                }
            }
            else if (settings.EditGold == 0)
            {
                // Reset flag when setting returns to zero
                _goldApplied = false;
            }

            // Apply influence if set and not yet applied (PLAYER ONLY)
            if (settings.EditInfluence != 0 && !_influenceApplied)
            {
                if (targetSettings.ApplyToPlayer && Clan.PlayerClan is not null)
                {
                    Clan.PlayerClan.Influence += settings.EditInfluence;
                    ModLogger.LogCheat("Influence Edit", true, settings.EditInfluence, "player clan");
                    // Only set flag after confirming the change was actually applied
                    _influenceApplied = true;
                }
            }
            else if (settings.EditInfluence == 0)
            {
                // Reset flag when setting returns to zero
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
            // Early exit if settings are null
            CheatSettings? settings = Settings;
            CheatTargetSettings? targetSettings = TargetSettings;
            if (settings is null || targetSettings is null)
            {
                return;
            }

            // Early return if cheat not enabled or player target disabled
            if (!settings.MaxAllCharacterRelationships || !targetSettings.ApplyToPlayer)
            {
                // Reset flag when cheat is disabled so it can be reapplied
                if (!settings.MaxAllCharacterRelationships)
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

            ModLogger.Debug("Max All Character Relationships: Starting to process all alive heroes...");

            // OPTIMIZED: Use cached collection instead of direct Hero.AllAliveHeroes enumeration
            List<Hero>? allHeroes = CampaignDataCache.AllAliveHeroes;
            if (allHeroes == null)
            {
                return;
            }

            foreach (Hero hero in allHeroes)
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
            ModLogger.Debug($"Max All Character Relationships: Completed - {heroesImproved} heroes improved out of {totalHeroes} total alive heroes");
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
            // Early exit if settings are null
            CheatSettings? settings = Settings;
            CheatTargetSettings? targetSettings = TargetSettings;
            if (settings is null || targetSettings is null)
            {
                return;
            }

            // Early returns for disabled cheat or missing data
            if (!settings.UnlimitedSmithyMaterials || !targetSettings.ApplyToPlayer)
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

            // Replenish all smithing materials to 9999
            // Using CORRECT IDs for Bannerlord 1.2.12

            // Hardwood
            AddSmithingMaterialIfLow(itemRoster, "hardwood", GameConstants.SmithingMaterialTargetAmount);

            // Charcoal
            AddSmithingMaterialIfLow(itemRoster, "charcoal", GameConstants.SmithingMaterialTargetAmount);

            // Iron Ore (raw material)
            AddSmithingMaterialIfLow(itemRoster, "iron", GameConstants.SmithingMaterialTargetAmount);

            // Crude Iron (tier 1 ingot)
            AddSmithingMaterialIfLow(itemRoster, "ironIngot1", GameConstants.SmithingMaterialTargetAmount);

            // Wrought Iron (tier 2 ingot)
            AddSmithingMaterialIfLow(itemRoster, "ironIngot2", GameConstants.SmithingMaterialTargetAmount);

            // Iron (tier 3 ingot)
            AddSmithingMaterialIfLow(itemRoster, "ironIngot3", GameConstants.SmithingMaterialTargetAmount);

            // Steel (tier 4 ingot)
            AddSmithingMaterialIfLow(itemRoster, "ironIngot4", GameConstants.SmithingMaterialTargetAmount);

            // Fine Steel (tier 5 ingot)
            AddSmithingMaterialIfLow(itemRoster, "ironIngot5", GameConstants.SmithingMaterialTargetAmount);

            // Thamaskene Steel (tier 6 ingot)
            AddSmithingMaterialIfLow(itemRoster, "ironIngot6", GameConstants.SmithingMaterialTargetAmount);
        }

        /// <summary>
        /// Adds smithing material to roster to maintain target amount (9999).
        /// </summary>
        /// <param name="roster">The item roster to modify.</param>
        /// <param name="itemId">The Bannerlord item ID (e.g., "iron", "charcoal").</param>
        /// <param name="targetAmount">The desired amount to maintain (9999).</param>
        /// <remarks>
        /// Threshold set to <see cref="GameConstants.SmithingMaterialReplenishThreshold"/> to restore materials if used during smithing sessions.
        /// This ensures materials are replenished daily without excessive roster updates.
        /// </remarks>
        private static void AddSmithingMaterialIfLow(ItemRoster roster, string itemId, int targetAmount)
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
                if (currentAmount < GameConstants.SmithingMaterialReplenishThreshold)
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
                // Note: This catch block has additional debug logging, so we keep it separate
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
        /// Applies custom campaign map time speed multiplier.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Game speed range: 0.1 to 10.0 (as limited by MCM UI settings).
        /// Default game speed is 1.0 (normal time flow).
        /// </para>
        /// <para>
        /// Note: The MCM UI limits GameSpeed to a maximum of 10.0 to prevent instability.
        /// Values are persisted in the config file and will remain active after game restarts.
        /// </para>
        /// </remarks>
        private static void ApplyGameSpeed()
        {
            // Early exit if settings are null
            CheatSettings? settings = Settings;
            CheatTargetSettings? targetSettings = TargetSettings;
            if (settings is null || targetSettings is null)
            {
                return;
            }

            // Early returns for disabled or invalid settings
            // GameSpeed is active when > 0f (matches CheatManager.cs line 625)
            if (settings.GameSpeed <= 0f || !targetSettings.ApplyToPlayer)
            {
                return;
            }

            if (Campaign.Current is null)
            {
                return;
            }

            try
            {
                // Use Math.Ceiling to ensure fractional speeds (e.g., 0.5) round up to at least 1
                // Math.Round uses banker's rounding which can round 0.5 down to 0, freezing the game
                // Ensure minimum speed of 1 to prevent game freeze
                int timeSpeed = Math.Max(1, (int)Math.Ceiling(settings.GameSpeed));
                Campaign.Current.SetTimeSpeed(timeSpeed);
            }
            catch (Exception ex)
            {
                LogException(ex, nameof(ApplyGameSpeed));
            }
        }

        #endregion

        #region Trade Items Restoration

        /// <summary>
        /// Checks player inventory and restores items that were removed during trade.
        /// </summary>
        /// <remarks>
        /// This method is called hourly to check if items were removed from player's inventory
        /// during barter/trade operations. If TradeItemsNoDecrease is enabled, it restores
        /// the items from the backup.
        /// </remarks>
        private void CheckAndRestoreTradeItems()
        {
            try
            {
                // Early exit if settings are null
                CheatSettings? settings = Settings;
                CheatTargetSettings? targetSettings = TargetSettings;
                if (settings is null || targetSettings is null)
                {
                    return;
                }

                // Check if cheat is disabled - reset flags to allow fresh backup when re-enabled
                bool cheatEnabled = settings.TradeItemsNoDecrease && targetSettings.ApplyToPlayer;
                if (!cheatEnabled)
                {
                    // Reset backup when cheat is disabled to allow re-initialization when re-enabled
                    // This ensures a fresh backup is created if the cheat is re-enabled later
                    if (_inventoryBackupInitialized || _inventoryBackup != null)
                    {
                        _inventoryBackup = null;
                        _inventoryBackupInitialized = false;
                        ModLogger.Debug("[TradeItemsNoDecrease] Cheat disabled - backup reset for re-initialization");
                    }
                    return;
                }

                if (MobileParty.MainParty?.ItemRoster == null)
                {
                    return;
                }

                ItemRoster currentRoster = MobileParty.MainParty.ItemRoster;

                // Initialize backup only once when cheat is first enabled
                // This captures the original inventory state and prevents newly looted items from being added
                if (!_inventoryBackupInitialized)
                {
                    _inventoryBackup = [];

                    // Capture current inventory state as the baseline
                    for (int i = 0; i < currentRoster.Count; i++)
                    {
                        ItemRosterElement element = currentRoster.GetElementCopyAtIndex(i);
                        ItemObject? item = element.EquipmentElement.Item;

                        if (item != null)
                        {
                            // Store original amount - this is the baseline that will be preserved
                            _inventoryBackup[item] = currentRoster.GetItemNumber(item);
                        }
                    }

                    _inventoryBackupInitialized = true;
                    ModLogger.Debug("[TradeItemsNoDecrease] Inventory backup initialized with current items");
                }

                // Restore items that were in backup but are now missing or reduced
                // Only restore items that were in the original backup, not newly looted items
                if (_inventoryBackup != null)
                {
                    foreach (KeyValuePair<ItemObject, int> backupEntry in _inventoryBackup)
                    {
                        ItemObject item = backupEntry.Key;
                        int originalAmount = backupEntry.Value;
                        int currentAmount = currentRoster.GetItemNumber(item);

                        if (currentAmount < originalAmount)
                        {
                            int amountToRestore = originalAmount - currentAmount;
                            _ = currentRoster.AddToCounts(item, amountToRestore);
                            ModLogger.Debug($"[TradeItemsNoDecrease] Restored {amountToRestore} x {item.Name} (from {currentAmount} to {originalAmount})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogException(ex, nameof(CheckAndRestoreTradeItems));
            }
        }

        /// <summary>
        /// Logs an exception with consistent formatting for PlayerCheatBehavior methods.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        /// <param name="methodName">The name of the method where the exception occurred.</param>
        /// <remarks>
        /// This helper method centralizes exception logging to ensure consistent error reporting
        /// and reduce code duplication across all PlayerCheatBehavior methods.
        /// </remarks>
        private static void LogException(Exception ex, string methodName)
        {
            ModLogger.Error($"Exception in PlayerCheatBehavior.cs - {ex}: {ex.Message}");
            ModLogger.Error($"Stack trace: {ex.StackTrace}");
            ModLogger.Error($"[PlayerCheatBehavior] Error in {methodName}: {ex.Message}");
            ModLogger.Error($"Stack trace: {ex.StackTrace}");
        }

        #endregion
    }
}
