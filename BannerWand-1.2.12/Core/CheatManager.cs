#nullable enable
using BannerWandRetro.Constants;
using BannerWandRetro.Settings;
using BannerWandRetro.Utils;
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Library;

namespace BannerWandRetro.Core
{
    /// <summary>
    /// Central manager for coordinating all cheat functionalities in BannerWandRetro.
    /// Provides utility methods, state management, and validation for cheat operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="CheatManager"/> acts as a facade for common cheat operations,
    /// providing a centralized interface for applying cheats across different game entities.
    /// It handles validation, logging, and performance tracking for all cheat operations.
    /// </para>
    /// <para>
    /// This class is static and maintains no state itself - all settings are retrieved
    /// from <see cref="CheatSettings"/> and <see cref="CheatTargetSettings"/> instances.
    /// </para>
    /// </remarks>
    public static class CheatManager
    {
        #region Constants
        // Constants moved to GameConstants for consistency
        #endregion

        #region Fields

        /// <summary>
        /// Flag to track if the initialization message has been shown to prevent duplicates.
        /// </summary>
        private static bool _initializationMessageShown = false;

        #endregion

        #region Properties

        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        /// <remarks>
        /// This property provides quick access to the settings instance with null-safety.
        /// </remarks>
        private static CheatSettings Settings => CheatSettings.Instance!;

        /// <summary>
        /// Gets the current target settings instance.
        /// </summary>
        /// <remarks>
        /// Target settings determine which entities (player, NPCs, clans) are affected by cheats.
        /// </remarks>
        private static CheatTargetSettings TargetSettings => CheatTargetSettings.Instance!;

        #endregion

        #region Initialization and Cleanup

        /// <summary>
        /// Initializes the cheat manager when a campaign starts.
        /// </summary>
        /// <param name="showMessage">Whether to show the initialization message to the user. Default is true.</param>
        /// <remarks>
        /// This method is called from <see cref="SubModule.OnGameStart"/> and performs
        /// initial setup, validation, and logging. Currently minimal, but reserved for
        /// future expansion (e.g., caching, pre-computation).
        /// </remarks>
        public static void Initialize(bool showMessage = true)
        {
            try
            {
                // Validate that settings are available before proceeding
                if (Settings == null || TargetSettings == null)
                {
                    ModLogger.Error("Failed to initialize CheatManager - settings are null");
                    InformationManager.DisplayMessage(new InformationMessage(MessageConstants.SettingsError, GameConstants.ErrorColor));
                    return;
                }

                // Log successful initialization to file
                ModLogger.Log(MessageConstants.CheatManagerInitialized);

                // Calculate and display active cheat count for user feedback (only once, when player appears on map)
                if (showMessage && !_initializationMessageShown)
                {
                    int activeCheatCount = GetActiveCheatCount();
                    string welcomeMessage = string.Format(MessageConstants.CheatsInitializedFormat, activeCheatCount);

                    InformationManager.DisplayMessage(new InformationMessage(welcomeMessage, GameConstants.SuccessColor));
                    ModLogger.Log(string.Format(MessageConstants.ActiveCheatsCountFormat, activeCheatCount));

                    _initializationMessageShown = true;
                }

            }
            catch (Exception ex)
            {
                ModLogger.Error($"Exception in CheatManager.cs - {ex}: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                ModLogger.Error($"[CheatManager] Error in Initialize: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Cleans up the cheat manager when a campaign ends.
        /// </summary>
        /// <remarks>
        /// Called from <see cref="SubModule.OnGameEnd"/> to perform any necessary cleanup.
        /// Currently minimal, but reserved for future resource management.
        /// </remarks>
        public static void Cleanup()
        {
            try
            {
                // Reset initialization flag so message can show again on next game start
                _initializationMessageShown = false;

                ModLogger.Log(MessageConstants.CheatManagerCleanup);

            }
            catch (Exception ex)
            {
                ModLogger.Error($"Exception in CheatManager.cs - {ex}: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                ModLogger.Error($"[CheatManager] Error in Cleanup: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        #endregion

        #region Player Cheats

        /// <summary>
        /// Applies gold to the player and targeted NPCs with validation.
        /// </summary>
        /// <param name="amount">Amount of gold to add (positive) or remove (negative).</param>
        /// <remarks>
        /// <para>
        /// This method applies gold changes based on <see cref="CheatTargetSettings"/>.
        /// It will affect:
        /// - Player hero if <see cref="CheatTargetSettings.ApplyToPlayer"/> is true
        /// - NPC heroes that match target filters
        /// </para>
        /// <para>
        /// Performance: O(n) where n is the number of alive heroes when NPC targets are enabled.
        /// For player-only mode, this is O(1).
        /// </para>
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown if amount exceeds reasonable bounds (±1,000,000,000).</exception>
        public static void ApplyGold(int amount)
        {
            try
            {                // Early return for zero amount - avoid unnecessary work
                if (amount == 0)
                {
                    return;
                }

                // Validation: prevent extreme values that could cause integer overflow
                const int maxGoldAmount = 1_000_000_000;
                if (Math.Abs(amount) > maxGoldAmount)
                {
                    ModLogger.Warning($"Gold amount {amount} exceeds safe limit (±{maxGoldAmount}), capping value");
                    amount = Math.Sign(amount) * maxGoldAmount;
                }

                using (ModLogger.BeginPerformanceScope($"Apply Gold ({amount})"))
                {
                    int affectedHeroCount = 0;

                    // Apply to player if target settings allow
                    if (TargetSettings.ApplyToPlayer && Hero.MainHero != null)
                    {
                        Hero.MainHero.ChangeHeroGold(amount);
                        affectedHeroCount++;
                        ModLogger.LogCheat("Gold Edit", true, amount, "player");
                    }

                    // Apply to NPCs if any NPC targets are enabled in settings
                    if (TargetSettings.HasAnyNPCTargetEnabled())
                    {
                        // Iterate through all alive heroes to find matching targets
                        foreach (Hero hero in Hero.AllAliveHeroes)
                        {
                            // Skip player hero and check if hero matches target filter criteria
                            if (hero != Hero.MainHero && TargetFilter.ShouldApplyCheat(hero))
                            {
                                hero.ChangeHeroGold(amount);
                                affectedHeroCount++;
                            }
                        }
                    }

                    // Display result message to user if any heroes were affected
                    if (affectedHeroCount > 0)
                    {
                        string resultMessage = affectedHeroCount == 1
                            ? $"Gold changed by {amount}"
                            : $"Gold changed by {amount} for {affectedHeroCount} heroes";
                        InformationManager.DisplayMessage(new InformationMessage(resultMessage, GameConstants.SuccessColor));
                        ModLogger.Log($"Gold modified for {affectedHeroCount} heroes (amount: {amount})");
                    }
                }

            }
            catch (Exception ex)
            {
                ModLogger.Error($"Exception in CheatManager.cs - {ex}: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                ModLogger.Error($"[CheatManager] Error in ApplyGold: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Applies influence to the player clan and targeted NPC clans with validation.
        /// </summary>
        /// <param name="amount">Amount of influence to add (positive) or remove (negative).</param>
        /// <remarks>
        /// <para>
        /// Influence is a clan-level resource, not a hero-level resource. This method
        /// affects entire clans based on <see cref="CheatTargetSettings"/>.
        /// </para>
        /// <para>
        /// Performance: O(n) where n is the number of clans when NPC targets are enabled.
        /// For player-only mode, this is O(1).
        /// </para>
        /// </remarks>
        public static void ApplyInfluence(float amount)
        {
            try
            {                // Early return for zero amount - avoid unnecessary computation
                if (Math.Abs(amount) < GameConstants.FloatEpsilon)
                {
                    return;
                }

                // Validation: prevent extreme values that could cause overflow
                const float maxInfluenceAmount = 1_000_000f;
                if (Math.Abs(amount) > maxInfluenceAmount)
                {
                    ModLogger.Warning($"Influence amount {amount} exceeds safe limit (±{maxInfluenceAmount}), capping value");
                    amount = Math.Sign(amount) * maxInfluenceAmount;
                }

                using (ModLogger.BeginPerformanceScope($"Apply Influence ({amount})"))
                {
                    int affectedClanCount = 0;

                    // Apply to player clan if target settings allow
                    if (TargetSettings.ApplyToPlayer && Clan.PlayerClan != null)
                    {
                        Clan.PlayerClan.Influence += amount;
                        affectedClanCount++;
                        ModLogger.LogCheat("Influence Edit", true, amount, "player clan");
                    }

                    // Apply to NPC clans if any NPC targets are enabled in settings
                    if (TargetSettings.HasAnyNPCTargetEnabled())
                    {
                        // Iterate through all clans to find matching targets
                        foreach (Clan clan in Clan.All)
                        {
                            // Skip player clan and check if clan matches target filter criteria
                            if (clan != Clan.PlayerClan && TargetFilter.ShouldApplyCheatToClan(clan))
                            {
                                clan.Influence += amount;
                                affectedClanCount++;
                            }
                        }
                    }

                    // Display result message to user if any clans were affected
                    if (affectedClanCount > 0)
                    {
                        string resultMessage = affectedClanCount == 1
                            ? $"Influence changed by {amount:F0}"
                            : $"Influence changed by {amount:F0} for {affectedClanCount} clans";
                        InformationManager.DisplayMessage(new InformationMessage(resultMessage, GameConstants.InfoColor));
                        ModLogger.Log($"Influence modified for {affectedClanCount} clans (amount: {amount:F0})");
                    }
                }

            }
            catch (Exception ex)
            {
                ModLogger.Error($"Exception in CheatManager.cs - {ex}: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                ModLogger.Error($"[CheatManager] Error in ApplyInfluence: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Applies attribute points to the player and targeted NPCs with validation.
        /// </summary>
        /// <param name="amount">Amount of attribute points to add (positive) or remove (negative).</param>
        /// <remarks>
        /// Negative amounts are automatically clamped to prevent negative attribute points.
        /// </remarks>
        public static void ApplyAttributePoints(int amount)
        {
            try
            {                // Early return for zero amount - avoid unnecessary work
                if (amount == 0)
                {
                    return;
                }

                using (ModLogger.BeginPerformanceScope($"Apply Attribute Points ({amount})"))
                {
                    int affectedHeroCount = 0;

                    // Apply to player if target settings allow
                    if (TargetSettings.ApplyToPlayer && Hero.MainHero != null)
                    {
                        ApplyAttributePointsToHero(Hero.MainHero, amount);
                        affectedHeroCount++;
                        ModLogger.LogCheat("Attribute Points Edit", true, amount, "player");
                    }

                    // Apply to NPCs if any NPC targets are enabled in settings
                    if (TargetSettings.HasAnyNPCTargetEnabled())
                    {
                        // Iterate through all alive heroes to find matching targets
                        foreach (Hero hero in Hero.AllAliveHeroes)
                        {
                            // Skip player hero and check if hero matches target filter criteria
                            if (hero != Hero.MainHero && TargetFilter.ShouldApplyCheat(hero))
                            {
                                ApplyAttributePointsToHero(hero, amount);
                                affectedHeroCount++;
                            }
                        }
                    }

                    // Display result message to user if any heroes were affected
                    if (affectedHeroCount > 0)
                    {
                        string resultMessage = affectedHeroCount == 1
                            ? $"Attribute points changed by {amount}"
                            : $"Attribute points changed by {amount} for {affectedHeroCount} heroes";
                        InformationManager.DisplayMessage(new InformationMessage(resultMessage, GameConstants.SuccessColor));
                        ModLogger.Log($"Attribute points modified for {affectedHeroCount} heroes (amount: {amount})");
                    }
                }

            }
            catch (Exception ex)
            {
                ModLogger.Error($"Exception in CheatManager.cs - {ex}: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                ModLogger.Error($"[CheatManager] Error in ApplyAttributePoints: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Applies focus points to the player and targeted NPCs with validation.
        /// </summary>
        /// <param name="amount">Amount of focus points to add (positive) or remove (negative).</param>
        /// <remarks>
        /// Negative amounts are automatically clamped to prevent negative focus points.
        /// </remarks>
        public static void ApplyFocusPoints(int amount)
        {
            try
            {                // Early return for zero amount - avoid unnecessary work
                if (amount == 0)
                {
                    return;
                }

                using (ModLogger.BeginPerformanceScope($"Apply Focus Points ({amount})"))
                {
                    int affectedHeroCount = 0;

                    // Apply to player if target settings allow
                    if (TargetSettings.ApplyToPlayer && Hero.MainHero != null)
                    {
                        ApplyFocusPointsToHero(Hero.MainHero, amount);
                        affectedHeroCount++;
                        ModLogger.LogCheat("Focus Points Edit", true, amount, "player");
                    }

                    // Apply to NPCs if any NPC targets are enabled in settings
                    if (TargetSettings.HasAnyNPCTargetEnabled())
                    {
                        // Iterate through all alive heroes to find matching targets
                        foreach (Hero hero in Hero.AllAliveHeroes)
                        {
                            // Skip player hero and check if hero matches target filter criteria
                            if (hero != Hero.MainHero && TargetFilter.ShouldApplyCheat(hero))
                            {
                                ApplyFocusPointsToHero(hero, amount);
                                affectedHeroCount++;
                            }
                        }
                    }

                    // Display result message to user if any heroes were affected
                    if (affectedHeroCount > 0)
                    {
                        string resultMessage = affectedHeroCount == 1
                            ? $"Focus points changed by {amount}"
                            : $"Focus points changed by {amount} for {affectedHeroCount} heroes";
                        InformationManager.DisplayMessage(new InformationMessage(resultMessage, GameConstants.SuccessColor));
                        ModLogger.Log($"Focus points modified for {affectedHeroCount} heroes (amount: {amount})");
                    }
                }

            }
            catch (Exception ex)
            {
                ModLogger.Error($"Exception in CheatManager.cs - {ex}: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                ModLogger.Error($"[CheatManager] Error in ApplyFocusPoints: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Applies attribute points to a specific hero with automatic clamping to prevent negative values.
        /// </summary>
        /// <param name="hero">The hero to modify.</param>
        /// <param name="amount">The amount of attribute points to add (can be negative).</param>
        /// <remarks>
        /// This helper method encapsulates the logic for applying and clamping attribute points,
        /// reducing code duplication across player and NPC application logic.
        /// </remarks>
        private static void ApplyAttributePointsToHero(Hero hero, int amount)
        {
            hero.HeroDeveloper.UnspentAttributePoints += amount;

            // Clamp to prevent negative values - heroes cannot have negative attribute points
            if (hero.HeroDeveloper.UnspentAttributePoints < 0)
            {
                hero.HeroDeveloper.UnspentAttributePoints = 0;
            }
        }

        /// <summary>
        /// Applies focus points to a specific hero with automatic clamping to prevent negative values.
        /// </summary>
        /// <param name="hero">The hero to modify.</param>
        /// <param name="amount">The amount of focus points to add (can be negative).</param>
        /// <remarks>
        /// This helper method encapsulates the logic for applying and clamping focus points,
        /// reducing code duplication across player and NPC application logic.
        /// </remarks>
        private static void ApplyFocusPointsToHero(Hero hero, int amount)
        {
            hero.HeroDeveloper.UnspentFocusPoints += amount;

            // Clamp to prevent negative values - heroes cannot have negative focus points
            if (hero.HeroDeveloper.UnspentFocusPoints < 0)
            {
                hero.HeroDeveloper.UnspentFocusPoints = 0;
            }
        }

        #endregion

        #region Party Management

        /// <summary>
        /// Gets all mobile parties that should be affected by cheats based on current target settings.
        /// </summary>
        /// <returns>A list of <see cref="MobileParty"/> instances that match the target criteria.</returns>
        /// <remarks>
        /// <para>
        /// This method builds a list of parties dynamically based on <see cref="CheatTargetSettings"/>.
        /// The result is not cached as party composition and leadership can change frequently.
        /// </para>
        /// <para>
        /// Performance: O(n) where n is the number of mobile parties in the game.
        /// Consider caching if called frequently within a short time window.
        /// </para>
        /// </remarks>
        public static List<MobileParty> GetAffectedParties()
        {
            List<MobileParty> affectedParties = [];

            // Add player's main party if target settings allow
            if (TargetSettings.ApplyToPlayer && MobileParty.MainParty != null)
            {
                affectedParties.Add(MobileParty.MainParty);
            }

            // Add NPC parties if any NPC targets are enabled in settings
            if (TargetSettings.HasAnyNPCTargetEnabled())
            {
                // Use foreach loop for better performance and fewer allocations than LINQ
                foreach (MobileParty party in MobileParty.All)
                {
                    if (party != MobileParty.MainParty && TargetFilter.ShouldApplyCheatToParty(party))
                    {
                        affectedParties.Add(party);
                    }
                }
            }

            return affectedParties;
        }

        /// <summary>
        /// Heals all wounded troops in affected parties.
        /// </summary>
        /// <returns>Total number of troops healed across all parties.</returns>
        /// <remarks>
        /// <para>
        /// This method is useful for the "Unlimited Party Health" cheat and can be called
        /// manually or periodically by behaviors.
        /// </para>
        /// <para>
        /// Performance: O(n*m) where n is the number of affected parties and m is the
        /// average roster size. Uses performance logging to track execution time.
        /// </para>
        /// </remarks>
        public static int HealAllTroops()
        {
            try
            {
                using (ModLogger.BeginPerformanceScope("Heal All Troops"))
                {
                    List<MobileParty> affectedParties = GetAffectedParties();
                    int totalHealedTroops = 0;

                    // Iterate through all affected parties to heal wounded troops
                    foreach (MobileParty party in affectedParties)
                    {
                        // Skip parties with invalid rosters
                        if (party?.MemberRoster == null)
                        {
                            continue;
                        }

                        // Process each troop type in the party roster
                        for (int rosterIndex = 0; rosterIndex < party.MemberRoster.Count; rosterIndex++)
                        {
                            TroopRosterElement troopElement = party.MemberRoster.GetElementCopyAtIndex(rosterIndex);
                            int woundedTroopCount = troopElement.WoundedNumber;

                            // Only heal if there are wounded troops and at least one healthy troop exists
                            if (woundedTroopCount > 0 && troopElement.Number > woundedTroopCount)
                            {
                                // Negative value removes wounded status and restores to healthy
                                _ = party.MemberRoster.AddToCountsAtIndex(rosterIndex, 0, -woundedTroopCount);
                                totalHealedTroops += woundedTroopCount;
                            }
                        }
                    }

                    // Display result to user if any troops were healed
                    if (totalHealedTroops > 0)
                    {
                        string healMessage = $"Healed {totalHealedTroops} wounded troops";
                        InformationManager.DisplayMessage(new InformationMessage(healMessage, GameConstants.SuccessColor));
                        ModLogger.Log($"Healed {totalHealedTroops} troops across {affectedParties.Count} parties");
                    }

                    return totalHealedTroops;
                }

            }
            catch (Exception ex)
            {
                ModLogger.Error($"Exception in CheatManager.cs - {ex}: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                ModLogger.Error($"[CheatManager] Error in HealAllTroops: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }

            return 0;
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Counts the number of currently active cheats.
        /// </summary>
        /// <returns>The total number of enabled cheats.</returns>
        /// <remarks>
        /// <para>
        /// This method counts all boolean cheats that are set to true and all
        /// numeric cheats that have non-zero values. Useful for status displays
        /// and debugging.
        /// </para>
        /// <para>
        /// Performance: O(1) - just checking boolean and numeric fields.
        /// </para>
        /// </remarks>
        public static int GetActiveCheatCount()
        {
            try
            {
                if (Settings == null)
                {
                    return 0;
                }

                int activeCheatCount = 0;

                // Boolean cheats - each enabled cheat increments the counter
                if (Settings.UnlimitedHealth)
                {
                    activeCheatCount++;
                }

                if (Settings.UnlimitedHorseHealth)
                {
                    activeCheatCount++;
                }

                if (Settings.UnlimitedShieldDurability)
                {
                    activeCheatCount++;
                }

                if (Settings.MaxMorale)
                {
                    activeCheatCount++;
                }

                if (Settings.BarterAlwaysAccepted)
                {
                    activeCheatCount++;
                }

                if (Settings.UnlimitedSmithyStamina)
                {
                    activeCheatCount++;
                }

                if (Settings.MaxCharacterRelationship)
                {
                    activeCheatCount++;
                }

                if (Settings.UnlimitedFood)
                {
                    activeCheatCount++;
                }

                if (Settings.TradeItemsNoDecrease)
                {
                    activeCheatCount++;
                }

                if (Settings.MaxCarryingCapacity)
                {
                    activeCheatCount++;
                }

                if (Settings.UnlimitedSmithyMaterials)
                {
                    activeCheatCount++;
                }

                if (Settings.UnlockAllSmithyParts)
                {
                    activeCheatCount++;
                }

                if (Settings.UnlimitedRenown)
                {
                    activeCheatCount++;
                }

                if (Settings.UnlimitedSkillXP)
                {
                    activeCheatCount++;
                }

                if (Settings.UnlimitedTroopsXP)
                {
                    activeCheatCount++;
                }

                if (Settings.SlowAIMovementSpeed)
                {
                    activeCheatCount++;
                }

                if (Settings.OneHitKills)
                {
                    activeCheatCount++;
                }

                if (Settings.FreezeDaytime)
                {
                    activeCheatCount++;
                }

                if (Settings.PersuasionAlwaysSucceed)
                {
                    activeCheatCount++;
                }

                if (Settings.OneDaySettlementsConstruction)
                {
                    activeCheatCount++;
                }

                if (Settings.InstantSiegeConstruction)
                {
                    activeCheatCount++;
                }

                // Numeric cheats - count if non-zero values are set
                if (Settings.MovementSpeed > GameConstants.FloatEpsilon)
                {
                    activeCheatCount++;
                }

                if (Settings.RenownMultiplier > GameConstants.FloatEpsilon)
                {
                    activeCheatCount++;
                }

                if (Settings.SkillXPMultiplier > GameConstants.FloatEpsilon)
                {
                    activeCheatCount++;
                }

                if (Settings.TroopsXPMultiplier > GameConstants.FloatEpsilon)
                {
                    activeCheatCount++;
                }

                if (Settings.GameSpeed > GameConstants.FloatEpsilon)
                {
                    activeCheatCount++;
                }

                if (Settings.EditGold != 0)
                {
                    activeCheatCount++;
                }

                if (Settings.EditInfluence != 0)
                {
                    activeCheatCount++;
                }

                if (Settings.EditAttributePoints != 0)
                {
                    activeCheatCount++;
                }

                if (Settings.EditFocusPoints != 0)
                {
                    activeCheatCount++;
                }

                return activeCheatCount;

            }
            catch (Exception ex)
            {
                ModLogger.Error($"Exception in CheatManager.cs - {ex}: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                ModLogger.Error($"[CheatManager] Error in GetActiveCheatCount: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }

            return 0;
        }

        /// <summary>
        /// Checks if any cheat is currently active.
        /// </summary>
        /// <returns>True if at least one cheat is enabled, false otherwise.</returns>
        /// <remarks>
        /// This is an optimized version that returns as soon as it finds an active cheat,
        /// unlike <see cref="GetActiveCheatCount"/> which counts all of them.
        /// </remarks>
        public static bool IsAnyCheatActive()
        {
            try
            {
                if (Settings == null)
                {
                    return false;
                }

                // Early return optimization - check most commonly used cheats first
                // Returns immediately upon finding any active cheat
                return Settings.UnlimitedHealth ||
                        Settings.UnlimitedHorseHealth ||
                        Settings.UnlimitedShieldDurability ||
                        Settings.MaxMorale ||
                        Settings.MovementSpeed > GameConstants.FloatEpsilon ||
                        Settings.BarterAlwaysAccepted ||
                        Settings.UnlimitedSmithyStamina ||
                        Settings.MaxCharacterRelationship ||
                        Settings.UnlimitedFood ||
                        Settings.TradeItemsNoDecrease ||
                        Settings.MaxCarryingCapacity ||
                        Settings.UnlimitedSmithyMaterials ||
                        Settings.UnlockAllSmithyParts ||
                        Settings.UnlimitedRenown ||
                        Settings.RenownMultiplier > GameConstants.FloatEpsilon ||
                        Settings.UnlimitedSkillXP ||
                        Settings.SkillXPMultiplier > GameConstants.FloatEpsilon ||
                        Settings.UnlimitedTroopsXP ||
                        Settings.TroopsXPMultiplier > GameConstants.FloatEpsilon ||
                        Settings.SlowAIMovementSpeed ||
                        Settings.OneHitKills ||
                        Settings.FreezeDaytime ||
                        Settings.PersuasionAlwaysSucceed ||
                        Settings.OneDaySettlementsConstruction ||
                        Settings.InstantSiegeConstruction ||
                        Settings.GameSpeed > GameConstants.FloatEpsilon ||
                        Settings.EditGold != 0 ||
                        Settings.EditInfluence != 0 ||
                        Settings.EditAttributePoints != 0 ||
                        Settings.EditFocusPoints != 0;

            }
            catch (Exception ex)
            {
                ModLogger.Error($"Exception in CheatManager.cs - {ex}: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                ModLogger.Error($"[CheatManager] Error in IsAnyCheatActive: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
            return false;
        }

        /// <summary>
        /// Gets a human-readable summary of active cheats.
        /// </summary>
        /// <returns>A string describing all currently active cheats.</returns>
        /// <remarks>
        /// <para>
        /// This method returns a comma-separated list of active cheat names.
        /// Useful for logging and user feedback.
        /// </para>
        /// <para>
        /// Only the most important/visible cheats are included to keep the summary concise.
        /// </para>
        /// </remarks>
        public static string? GetActiveCheatSummary()
        {
            try
            {
                if (Settings == null)
                {
                    return "No settings available";
                }

                List<string> activeCheatsList = [];

                // Only list the most significant/visible cheats to keep summary concise
                if (Settings.UnlimitedHealth)
                {
                    activeCheatsList.Add("Unlimited Health");
                }

                if (Settings.UnlimitedHorseHealth)
                {
                    activeCheatsList.Add("Unlimited Horse Health");
                }

                if (Settings.MaxMorale)
                {
                    activeCheatsList.Add("Max Morale");
                }

                if (Settings.UnlimitedFood)
                {
                    activeCheatsList.Add("Unlimited Food");
                }

                if (Settings.MaxCarryingCapacity)
                {
                    activeCheatsList.Add("Max Carrying Capacity");
                }

                if (Settings.UnlimitedSkillXP)
                {
                    activeCheatsList.Add("Unlimited Skill XP");
                }

                if (Settings.UnlimitedTroopsXP)
                {
                    activeCheatsList.Add("Unlimited Troops XP");
                }

                if (Settings.OneHitKills)
                {
                    activeCheatsList.Add("One-Hit Kills");
                }

                if (Settings.PersuasionAlwaysSucceed)
                {
                    activeCheatsList.Add("Persuasion Always Succeed");
                }

                if (Settings.MovementSpeed > GameConstants.FloatEpsilon)
                {
                    activeCheatsList.Add($"Movement Speed x{Settings.MovementSpeed:F1}");
                }

                if (Settings.SkillXPMultiplier > GameConstants.FloatEpsilon)
                {
                    activeCheatsList.Add($"Skill XP x{Settings.SkillXPMultiplier:F1}");
                }

                return activeCheatsList.Count == 0 ? "No cheats active" : $"Active cheats: {string.Join(", ", activeCheatsList)}";

            }
            catch (Exception ex)
            {
                ModLogger.Error($"Exception in CheatManager.cs - {ex}: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                ModLogger.Error($"[CheatManager] Error in GetActiveCheatSummary: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Logs the current cheat status to the game UI and log file.
        /// </summary>
        /// <remarks>
        /// Displays an in-game message with the active cheat summary.
        /// Useful for debugging or user confirmation.
        /// </remarks>
        public static void LogCheatStatus()
        {
            try
            {
                string summary = GetActiveCheatSummary()!;
                InformationManager.DisplayMessage(new InformationMessage(summary, GameConstants.WarningColor));
                ModLogger.Log($"Cheat status: {summary}");

            }
            catch (Exception ex)
            {
                ModLogger.Error($"Exception in CheatManager.cs - {ex}: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                ModLogger.Error($"[CheatManager] Error in LogCheatStatus: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        #endregion
    }
}
