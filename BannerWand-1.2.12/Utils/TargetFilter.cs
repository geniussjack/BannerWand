#nullable enable
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace BannerWandRetro.Utils
{
    /// <summary>
    /// Utility class for filtering cheat targets based on hero type and settings.
    /// Provides centralized logic for determining which entities should be affected by cheats.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class works in conjunction with <see cref="Settings.CheatTargetSettings"/> to implement
    /// a flexible targeting system. It evaluates heroes, clans, and parties against configured
    /// target criteria to determine if they should receive cheat effects.
    /// </para>
    /// <para>
    /// The filtering logic uses the new hierarchical target structure:
    /// - Player: Player character and clan members
    /// - Kingdoms: Rulers, vassals, and nobles
    /// - Minor Clans: Minor faction leaders and members
    /// </para>
    /// <para>
    /// Performance: For single-hero checks, use <see cref="ShouldApplyCheat(Hero)"/> (O(1)).
    /// For bulk operations, use <see cref="Settings.CheatTargetSettings.CollectTargetHeroes()"/>
    /// to get a HashSet of all targets (O(n) but called once).
    /// </para>
    /// </remarks>
    public static class TargetFilter
    {
        /// <summary>
        /// Determines if a hero should be affected by cheats based on current target settings.
        /// Uses the new hierarchical target structure.
        /// </summary>
        /// <param name="hero">The hero to evaluate.</param>
        /// <returns>True if the hero should receive cheats, false otherwise.</returns>
        /// <remarks>
        /// <para>
        /// Evaluation order (first match wins):
        /// 1. Is this the player hero? → Check ApplyToPlayer
        /// 2. Is this a player clan member? → Check ApplyToPlayerClanMembers
        /// 3. Is this in a kingdom?
        ///    - Is kingdom ruler? → Check ApplyToKingdomRulers or ApplyToPlayerKingdomRuler
        ///    - Is vassal clan leader? → Check ApplyToKingdomVassals or ApplyToPlayerKingdomVassals
        ///    - Is noble? → Check ApplyToKingdomNobles or ApplyToPlayerKingdomNobles
        /// 4. Is this in a minor clan?
        ///    - Is clan leader? → Check ApplyToMinorClanLeaders
        ///    - Is noble? → Check ApplyToMinorClanMembers
        /// </para>
        /// <para>
        /// Null safety: Returns false for null heroes or if settings are unavailable.
        /// </para>
        /// </remarks>
        public static bool ShouldApplyCheat(Hero? hero)
        {
            try
            {
                // Early returns for invalid input
                if (hero == null)
                {
                    return false;
                }

                Settings.CheatTargetSettings? settings = Settings.CheatTargetSettings.Instance;
                if (settings is null)
                {
                    return false;
                }

                // Priority 1: Check if hero is the player
                if (hero == Hero.MainHero)
                {
                    return settings.ApplyToPlayer;
                }

                // Priority 2: Check if hero is in player's clan (companions, family)
                if (hero.Clan == Clan.PlayerClan)
                {
                    return settings.ApplyToPlayerClanMembers;
                }

                // Priority 3: Check NPC targeting based on relationship to player and kingdoms
                Clan? heroClan = hero.Clan;
                if (heroClan == null)
                {
                    return false;
                }

                Kingdom? heroKingdom = heroClan.Kingdom;

                // Check kingdom-related targets
                if (heroKingdom != null)
                {
                    // Check if hero is a kingdom ruler
                    if (heroKingdom.Leader == hero)
                    {
                        // Check if this is the player's kingdom ruler
                        Kingdom? playerKingdom = Hero.MainHero?.Clan?.Kingdom;
                        if (playerKingdom == heroKingdom && settings.ApplyToPlayerKingdomRuler)
                        {
                            return true;
                        }
                        // Check if all kingdom rulers are enabled
                        if (settings.ApplyToKingdomRulers)
                        {
                            return true;
                        }
                    }

                    // Check if hero is a vassal clan leader
                    if (heroClan.Leader == hero && heroClan != heroKingdom.RulingClan)
                    {
                        // Check if this is a player kingdom vassal
                        Kingdom? playerKingdom = Hero.MainHero?.Clan?.Kingdom;
                        if (playerKingdom == heroKingdom && heroClan != Clan.PlayerClan && settings.ApplyToPlayerKingdomVassals)
                        {
                            return true;
                        }
                        // Check if all kingdom vassals are enabled
                        if (settings.ApplyToKingdomVassals)
                        {
                            return true;
                        }
                    }

                    // Check if hero is a noble in a kingdom (all heroes in clans are considered nobles)
                    if (hero.CharacterObject?.IsHero == true)
                    {
                        // Check if this is in player's kingdom
                        Kingdom? playerKingdom = Hero.MainHero?.Clan?.Kingdom;
                        if (playerKingdom == heroKingdom && settings.ApplyToPlayerKingdomNobles)
                        {
                            return true;
                        }
                        // Check if all kingdom nobles are enabled
                        if (settings.ApplyToKingdomNobles)
                        {
                            return true;
                        }
                    }
                }

                // Check minor clan targets (clans without kingdoms)
                if (heroKingdom == null && heroClan.IsMinorFaction)
                {
                    // Check if hero is a minor clan leader
                    if (heroClan.Leader == hero && settings.ApplyToMinorClanLeaders)
                    {
                        return true;
                    }

                    // Check if hero is a noble in a minor clan (all heroes in clans are considered nobles)
                    if (hero.CharacterObject?.IsHero == true && settings.ApplyToMinorClanMembers)
                    {
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[TargetFilter] Error in ShouldApplyCheat: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Determines if a clan should be affected by cheats based on current target settings.
        /// </summary>
        /// <param name="clan">The clan to evaluate.</param>
        /// <returns>True if the clan should receive cheats, false otherwise.</returns>
        /// <remarks>
        /// <para>
        /// Clan-level cheats typically affect influence and other clan-wide properties.
        /// This method provides simplified clan filtering compared to hero filtering.
        /// </para>
        /// <para>
        /// Evaluation logic:
        /// 1. Player clan → Check ApplyToPlayer setting
        /// 2. Player kingdom vassal → Check ApplyToPlayerKingdomVassals setting
        /// 3. Other kingdom clan → Check kingdom-related settings
        /// 4. Minor clan → Check minor clan settings
        /// </para>
        /// <para>
        /// Note: Uses a simplified heuristic for non-player clans. For precise control,
        /// use <see cref="ShouldApplyCheat(Hero)"/> with the clan leader or use
        /// <see cref="Settings.CheatTargetSettings.CollectTargetHeroes()"/>.
        /// </para>
        /// </remarks>
        public static bool ShouldApplyCheatToClan(Clan? clan)
        {
            try
            {
                // Early returns for invalid input
                if (clan == null)
                {
                    return false;
                }

                Settings.CheatTargetSettings? settings = Settings.CheatTargetSettings.Instance;
                if (settings is null)
                {
                    return false;
                }

                // Check if clan is player's clan
                if (clan == Clan.PlayerClan)
                {
                    return settings.ApplyToPlayer;
                }

                Kingdom? clanKingdom = clan.Kingdom;

                // Check if clan has a kingdom
                if (clanKingdom != null)
                {
                    // Check if clan belongs to player's kingdom (vassal)
                    Kingdom? playerKingdom = Hero.MainHero?.Clan?.Kingdom;
                    if (clanKingdom == playerKingdom && clan != Clan.PlayerClan)
                    {
                        // Check if this is the ruling clan (ruler)
                        if (clan == clanKingdom.RulingClan && settings.ApplyToPlayerKingdomRuler)
                        {
                            return true;
                        }
                        // Check if vassals are enabled
                        if (settings.ApplyToPlayerKingdomVassals || settings.ApplyToPlayerKingdomNobles)
                        {
                            return true;
                        }
                    }

                    // Clan is in a different kingdom
                    // Check if this is the ruling clan (ruler)
                    if (clan == clanKingdom.RulingClan && settings.ApplyToKingdomRulers)
                    {
                        return true;
                    }
                    // Check if vassals or nobles are enabled
                    if (settings.ApplyToKingdomVassals || settings.ApplyToKingdomNobles)
                    {
                        return true;
                    }
                }

                // Clan is independent (no kingdom) - check if it's a minor faction
                return clan.IsMinorFaction && (settings.ApplyToMinorClanLeaders || settings.ApplyToMinorClanMembers);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[TargetFilter] Error in ShouldApplyCheatToClan: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Determines if a mobile party should be affected by cheats based on current target settings.
        /// </summary>
        /// <param name="party">The party to evaluate.</param>
        /// <returns>True if the party should receive cheats, false otherwise.</returns>
        /// <remarks>
        /// <para>
        /// Party targeting is determined by the party's leader hero.
        /// This method is a convenience wrapper around <see cref="ShouldApplyCheat(Hero)"/>.
        /// </para>
        /// <para>
        /// Important: Parties without leaders (rare) are not targeted.
        /// Examples: Merchant caravans may not always have hero leaders.
        /// </para>
        /// <para>
        /// Performance: O(1) - delegates to hero check which is also O(1).
        /// </para>
        /// </remarks>
        public static bool ShouldApplyCheatToParty(MobileParty? party)
        {
            try
            {
                // Early return if party or leader is null
                if (party?.LeaderHero == null)
                {
                    return false;
                }

                // Delegate to hero filtering logic
                return ShouldApplyCheat(party.LeaderHero);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[TargetFilter] Error in ShouldApplyCheatToParty: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }
    }
}
