#nullable enable
// System namespaces
using System;
using System.Collections.Generic;

// Third-party namespaces
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

// Project namespaces
using BannerWand.Constants;
using BannerWand.Settings;
using BannerWand.Utils;

namespace BannerWand.Behaviors
{
    /// <summary>
    /// Campaign behavior that handles skill and troop XP multiplier cheats.
    /// Works in tandem with <see cref="Models.CustomGenericXpModel"/> for complete XP management.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Two-tiered XP system:
    /// 1. <see cref="Models.CustomGenericXpModel"/> - Multiplies XP from normal gameplay
    /// 2. This behavior - Adds periodic XP for "Unlimited" modes
    /// </para>
    /// <para>
    /// This separation allows:
    /// - Multiplier mode: Natural progression at boosted rates (via model)
    /// - Unlimited mode: Aggressive XP gains every hour (via this behavior)
    /// </para>
    /// <para>
    /// Performance: Runs hourly, processes all skills/troops.
    /// For unlimited mode, this is intentional to max out values quickly.
    /// For multiplier mode, the model handles it passively (better performance).
    /// </para>
    /// </remarks>
    public class SkillXPCheatBehavior : CampaignBehaviorBase
    {
        #region Static Fields for Logging Control

        /// <summary>
        /// Counter for troop XP logging to reduce spam when Game Speed is high.
        /// </summary>
        private static int _troopXpLogCounter = 0;

        /// <summary>
        /// Flag to prevent repeated "no troops" warnings.
        /// </summary>
        private static bool _noTroopsWarningLogged = false;

        #endregion

        #region Event Registration

        /// <summary>
        /// Registers this behavior to listen to campaign events.
        /// </summary>
        /// <remarks>
        /// Only subscribes to <see cref="CampaignEvents.HourlyTickEvent"/> to provide
        /// gradual XP gains without overwhelming the player with instant max levels.
        /// </remarks>
        public override void RegisterEvents()
        {
            try
            {
                CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[SkillXPCheatBehavior] Error in RegisterEvents: {ex.Message}");
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
            // No persistent data to sync - settings are managed by MCM
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Called every in-game hour to apply XP boosts.
        /// </summary>
        /// <remarks>
        /// <para>
        /// IMPORTANT: This method ONLY applies XP for "Unlimited" modes.
        /// Multiplier modes are handled passively by <see cref="Models.CustomGenericXpModel"/>.
        /// </para>
        /// <para>
        /// Why this distinction:
        /// - Unlimited: Player wants max level ASAP → aggressive hourly XP injection
        /// - Multiplier: Player wants faster but natural progression → passive multiplier
        /// </para>
        /// </remarks>
        private void OnHourlyTick()
        {
            // Apply skill XP boost (unlimited mode only)
            ApplyUnlimitedSkillXP();

            // Apply troop XP boost (both unlimited and multiplier modes)
            ApplyTroopXPBoost();
        }

        #endregion

        #region Skill XP

        /// <summary>
        /// Applies aggressive XP boost to player and targeted NPC skills when Unlimited Skill XP is enabled.
        /// Does NOT apply for Skill XP Multiplier (handled by <see cref="Models.CustomGenericXpModel"/>).
        /// </summary>
        /// <remarks>
        /// <para>
        /// Implementation strategy:
        /// - Iterate through ALL skills for each targeted hero
        /// - Add 999 XP to each skill below max level (330)
        /// - Continue until all skills reach max
        /// </para>
        /// <para>
        /// Time to max: Approximately 15-30 game hours to max all skills from 0
        /// (depends on starting skill levels and learning rate bonuses).
        /// </para>
        /// <para>
        /// Performance: This is intentionally aggressive for "Unlimited" mode.
        /// Players who want this cheat expect rapid progression.
        /// </para>
        /// </remarks>
        private static void ApplyUnlimitedSkillXP()
        {
            if (Hero.MainHero is null)
            {
                return;
            }

            CheatSettings? settings = CheatSettings.Instance;
            CheatTargetSettings? targetSettings = CheatTargetSettings.Instance;

            if (settings is null || targetSettings is null)
            {
                return;
            }

            // ONLY apply for Unlimited Skill XP mode (not multiplier mode)
            if (!settings.UnlimitedSkillXP)
            {
                return;
            }

            // Early return if no targets enabled
            if (!targetSettings.ApplyToPlayer && !targetSettings.HasAnyNPCTargetEnabled())
            {
                return;
            }

            int totalSkillsImproved = 0;
            int heroesImproved = 0;

            // Apply to player if enabled
            if (targetSettings.ApplyToPlayer)
            {
                int skillsImproved = ApplySkillXPToHero(Hero.MainHero);
                if (skillsImproved > 0)
                {
                    totalSkillsImproved += skillsImproved;
                    heroesImproved++;
                }
            }

            // Apply to NPCs if any NPC targets are enabled
            // Use cached collection to avoid repeated enumeration (runs hourly)
            if (targetSettings.HasAnyNPCTargetEnabled())
            {
                List<Hero>? allHeroes = CampaignDataCache.AllAliveHeroes;
                if (allHeroes is null)
                {
                    return;
                }

                foreach (Hero hero in allHeroes)
                {
                    // Skip player hero (already handled)
                    if (hero == Hero.MainHero)
                    {
                        continue;
                    }

                    // Check if this hero should receive cheats
                    if (TargetFilter.ShouldApplyCheat(hero))
                    {
                        int skillsImproved = ApplySkillXPToHero(hero);
                        if (skillsImproved > 0)
                        {
                            totalSkillsImproved += skillsImproved;
                            heroesImproved++;
                        }
                    }
                }
            }

            // Log progress for debugging
            if (totalSkillsImproved > 0)
            {
                ModLogger.Debug($"Unlimited Skill XP: Improved {totalSkillsImproved} skills across {heroesImproved} heroes");
            }

            // NOTE: Skill XP Multiplier is handled by CustomGenericXpModel.GetXpMultiplier
            // We deliberately DO NOT add periodic XP here for multiplier mode to avoid
            // double-boosting (passive multiplier + active injection)
        }

        /// <summary>
        /// Applies skill XP to a single hero's skills.
        /// </summary>
        /// <param name="hero">The hero to apply XP to.</param>
        /// <returns>Number of skills that received XP.</returns>
        private static int ApplySkillXPToHero(Hero hero)
        {
            if (hero?.HeroDeveloper is null)
            {
                return 0;
            }

            int skillsImproved = 0;

            // Iterate through all skills (avoid LINQ for better performance)
            foreach (SkillObject skill in Skills.All)
            {
                int currentLevel = hero.GetSkillValue(skill);

                // Only boost if below max level (optimization: avoid unnecessary XP additions)
                if (currentLevel < GameConstants.MaxSkillLevel)
                {
                    // Add XP with parameters:
                    // - Amount: from GameConstants
                    // - isAffectedByFocusFactor: true (respects focus points and learning rate)
                    hero.HeroDeveloper.AddSkillXp(skill, GameConstants.UnlimitedSkillXPPerHour, isAffectedByFocusFactor: true);
                    skillsImproved++;
                }
            }

            return skillsImproved;
        }

        #endregion

        #region Troop XP

        /// <summary>
        /// Applies XP boost to all troops in player's party and targeted NPC parties.
        /// Handles both Unlimited Troops XP and Troops XP Multiplier modes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Unlike skill XP, troop XP doesn't have a passive model override system.
        /// Therefore, this behavior handles both unlimited and multiplier modes.
        /// </para>
        /// <para>
        /// XP amounts:
        /// - Unlimited mode: 999 XP per troop per hour (rapid leveling)
        /// - Multiplier mode: 100 XP × multiplier per troop per hour (moderate leveling)
        /// </para>
        /// <para>
        /// Performance: Iterates through entire party roster for each targeted party.
        /// Hourly execution makes this acceptable. Daily would be too slow,
        /// per-tick would be too expensive.
        /// </para>
        /// </remarks>
        private static void ApplyTroopXPBoost()
        {
            CheatSettings? settings = CheatSettings.Instance;
            CheatTargetSettings? targetSettings = CheatTargetSettings.Instance;

            if (settings is null || targetSettings is null)
            {
                return;
            }

            // Check if either unlimited or multiplier mode is active
            bool unlimitedMode = settings.UnlimitedTroopsXP;
            bool multiplierMode = settings.TroopsXPMultiplier > 1f;

            if (!unlimitedMode && !multiplierMode)
            {
                return;
            }

            // Early return if no targets enabled
            if (!targetSettings.ApplyToPlayer && !targetSettings.HasAnyNPCTargetEnabled())
            {
                return;
            }

            // Calculate XP amount based on mode
            int xpToAdd = unlimitedMode
                ? GameConstants.UnlimitedTroopXPPerHour  // Unlimited mode: aggressive XP gain
                : (int)(GameConstants.TroopXPBaseAmount * settings.TroopsXPMultiplier);  // Multiplier mode: scaled XP gain

            int totalTroopsImproved = 0;
            int partiesProcessed = 0;

            // Apply to player's party if enabled
            if (targetSettings.ApplyToPlayer && MobileParty.MainParty?.MemberRoster is not null)
            {
                int troopsImproved = ApplyTroopXPToParty(MobileParty.MainParty, xpToAdd);
                if (troopsImproved > 0)
                {
                    totalTroopsImproved += troopsImproved;
                    partiesProcessed++;
                    ModLogger.Debug($"[TroopXP] Applied {xpToAdd} XP to {troopsImproved} troops in player party");
                }
            }

            // Apply to player clan members' parties if enabled
            if (targetSettings.ApplyToPlayerClanMembers && Hero.MainHero?.Clan != null)
            {
                foreach (Hero hero in Hero.MainHero.Clan.Heroes)
                {
                    if (hero?.PartyBelongedTo?.MemberRoster != null && hero != Hero.MainHero)
                    {
                        int troopsImproved = ApplyTroopXPToParty(hero.PartyBelongedTo, xpToAdd);
                        if (troopsImproved > 0)
                        {
                            totalTroopsImproved += troopsImproved;
                            partiesProcessed++;
                            ModLogger.Debug($"[TroopXP] Applied {xpToAdd} XP to {troopsImproved} troops in {hero.Name}'s party (clan member)");
                        }
                    }
                }
            }

            // Apply to NPC parties if any NPC targets are enabled
            // Use cached collection to avoid repeated enumeration (runs hourly)
            if (targetSettings.HasAnyNPCTargetEnabled())
            {
                List<MobileParty>? allParties = CampaignDataCache.AllParties;
                if (allParties is null)
                {
                    return;
                }

                foreach (MobileParty party in allParties)
                {
                    // Skip player party (already handled) and parties without member roster
                    if (party == MobileParty.MainParty || party.MemberRoster is null)
                    {
                        continue;
                    }

                    // Skip player clan parties (already handled above)
                    if (party.LeaderHero?.Clan == Clan.PlayerClan)
                    {
                        continue;
                    }

                    // Check if this party's leader should receive cheats
                    if (TargetFilter.ShouldApplyCheatToParty(party))
                    {
                        int troopsImproved = ApplyTroopXPToParty(party, xpToAdd);
                        if (troopsImproved > 0)
                        {
                            totalTroopsImproved += troopsImproved;
                            partiesProcessed++;
                        }
                    }
                }
            }

            // Log progress for debugging (only log once per hour to avoid spam)
            // Since Game Speed can make hours pass very quickly, we use a static counter
            if (totalTroopsImproved > 0)
            {
                string mode = unlimitedMode ? "Unlimited" : $"Multiplier x{settings.TroopsXPMultiplier:F1}";
                // Only log every Nth call to reduce spam when Game Speed is high
                if (_troopXpLogCounter % GameConstants.TroopXPLogInterval == 0)
                {
                    ModLogger.Log($"Troop XP ({mode}): Added {xpToAdd} XP to {totalTroopsImproved} troops across {partiesProcessed} parties (call #{_troopXpLogCounter + 1})");
                }
                _troopXpLogCounter++;
            }
            else if (multiplierMode && targetSettings.ApplyToPlayer)
            {
                // Log if no troops were improved (for debugging) - only once per session
                if (!_noTroopsWarningLogged)
                {
                    ModLogger.Debug($"[TroopXP] Multiplier mode active (x{settings.TroopsXPMultiplier:F1}) but no troops received XP. Player party has {MobileParty.MainParty?.MemberRoster?.TotalManCount ?? 0} troops.");
                    _noTroopsWarningLogged = true;
                }
            }
        }

        /// <summary>
        /// Applies troop XP to a single party's roster.
        /// </summary>
        /// <param name="party">The party to apply XP to.</param>
        /// <param name="xpAmount">Amount of XP to add to each troop.</param>
        /// <returns>Number of troop types that received XP.</returns>
        /// <remarks>
        /// <para>
        /// IMPORTANT: AddXpToTroopAtIndex adds the amount to CURRENT XP, not sets it.
        /// So if troop has 100 XP and we pass 8000, it becomes 8100 XP.
        /// </para>
        /// <para>
        /// This method iterates through all troop types in the party and adds XP to each.
        /// Heroes are skipped as they use the skill XP system instead.
        /// </para>
        /// </remarks>
        private static int ApplyTroopXPToParty(MobileParty party, int xpAmount)
        {
            if (party?.MemberRoster is null)
            {
                return 0;
            }

            int troopsImproved = 0;
            int rosterCount = party.MemberRoster.Count;

            // Iterate through party roster (use indexed for loop for better performance)
            for (int i = 0; i < rosterCount; i++)
            {
                TroopRosterElement element = party.MemberRoster.GetElementCopyAtIndex(i);

                // Only add XP to non-hero troops (heroes level via skill XP system)
                if (element.Character?.IsHero == false)
                {
                    // Get current XP before adding
                    _ = party.MemberRoster.GetElementXp(i);

                    // Add XP to this troop stack
                    // NOTE: AddXpToTroopAtIndex adds xpAmount to current XP, so result = currentXp + xpAmount
                    party.MemberRoster.AddXpToTroopAtIndex(xpAmount, i);

                    troopsImproved++;
                }
            }

            return troopsImproved;
        }

        #endregion
    }
}
