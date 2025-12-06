#nullable enable
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Extensions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace BannerWand.Utils
{
    /// <summary>
    /// Extension methods and utility helpers for cheat functionality.
    /// Provides convenient methods for checking hero status, managing parties, and manipulating agents.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class contains extension methods that extend Bannerlord's core types (Hero, Clan, MobileParty, Agent, etc.)
    /// with cheat-specific functionality. Extension methods allow for cleaner, more readable code throughout
    /// the mod by adding domain-specific methods to existing types.
    /// </para>
    /// <para>
    /// All methods are static and optimized for frequent use with minimal allocations.
    /// </para>
    /// </remarks>
    public static class CheatExtensions
    {
        #region Hero Extensions

        /// <summary>
        /// Checks if a hero is a kingdom ruler (king or queen).
        /// </summary>
        /// <param name="hero">The hero to check.</param>
        /// <returns>True if the hero leads their clan's kingdom, false otherwise.</returns>
        /// <remarks>
        /// A hero is considered a kingdom ruler if:
        /// 1. They have a clan
        /// 2. Their clan has a kingdom
        /// 3. They are the leader of that kingdom
        /// </remarks>
        public static bool IsKingdomRuler(this Hero hero)
        {
            try
            {
                return hero?.Clan?.Kingdom != null && hero == hero.Clan.Kingdom.Leader;
            }
            catch (Exception ex)
            {
                LogException(ex, nameof(IsKingdomRuler));
                return false;
            }
        }


        /// <summary>
        /// Checks if a hero is a clan leader.
        /// </summary>
        /// <param name="hero">The hero to check.</param>
        /// <returns>True if the hero leads their clan, false otherwise.</returns>
        /// <remarks>
        /// Clan leaders have special responsibilities and abilities in Bannerlord.
        /// This is useful for targeting nobles specifically.
        /// </remarks>
        public static bool IsClanLeader(this Hero hero)
        {
            try
            {
                return hero?.Clan != null && hero == hero.Clan.Leader;

            }
            catch (Exception ex)
            {
                LogException(ex, nameof(IsClanLeader));
                return false;
            }
        }

        /// <summary>
        /// Checks if a hero belongs to an independent clan (no kingdom affiliation).
        /// </summary>
        /// <param name="hero">The hero to check.</param>
        /// <returns>True if the hero's clan has no kingdom, false otherwise.</returns>
        /// <remarks>
        /// Independent clans include minor factions, mercenaries, and bandits.
        /// Useful for applying cheats to specific faction types.
        /// </remarks>
        public static bool IsIndependent(this Hero hero)
        {
            try
            {
                return hero?.Clan != null && hero.Clan.Kingdom == null;

            }
            catch (Exception ex)
            {
                LogException(ex, nameof(IsIndependent));
                return false;
            }
        }

        /// <summary>
        /// Checks if a hero is in the player's clan.
        /// </summary>
        /// <param name="hero">The hero to check.</param>
        /// <returns>True if the hero belongs to the player's clan, false otherwise.</returns>
        /// <remarks>
        /// Player clan includes the player, companions, spouse, and children.
        /// </remarks>
        public static bool IsPlayerClanMember(this Hero hero)
        {
            try
            {
                return hero?.Clan == Clan.PlayerClan;

            }
            catch (Exception ex)
            {
                LogException(ex, nameof(IsPlayerClanMember));
                return false;
            }
        }

        /// <summary>
        /// Checks if a hero's clan is a vassal of the player's kingdom.
        /// </summary>
        /// <param name="hero">The hero to check.</param>
        /// <returns>True if the hero's clan serves the player's kingdom, false otherwise.</returns>
        /// <remarks>
        /// <para>
        /// A hero is considered a player kingdom vassal if:
        /// 1. Player has a kingdom
        /// 2. Hero's clan belongs to that kingdom
        /// 3. Hero's clan is not the player's own clan
        /// </para>
        /// <para>
        /// Useful for strengthening friendly lords without affecting enemies.
        /// </para>
        /// </remarks>
        public static bool IsPlayerKingdomVassal(this Hero hero)
        {
            try
            {
                return (hero?.Clan) != null && (Clan.PlayerClan?.Kingdom) != null && hero.Clan.Kingdom == Clan.PlayerClan.Kingdom && hero.Clan != Clan.PlayerClan;

            }
            catch (Exception ex)
            {
                LogException(ex, nameof(IsPlayerKingdomVassal));
                return false;
            }
        }

        /// <summary>
        /// Gets all skills available in the game.
        /// </summary>
        /// <returns>A list of all <see cref="SkillObject"/> instances.</returns>
        /// <remarks>
        /// <para>
        /// Skills in Bannerlord include combat skills (one-handed, bow, etc.)
        /// and non-combat skills (trade, charm, etc.). This method returns all of them.
        /// </para>
        /// <para>
        /// Uses LINQ .ToList() to create a list from the Skills.All enumerable.
        /// </para>
        /// </remarks>
        public static List<SkillObject> GetAllSkills()
        {
            return [.. Skills.All];
        }

        #endregion

        #region Clan Extensions

        /// <summary>
        /// Checks if a clan is independent (not part of any kingdom).
        /// </summary>
        /// <param name="clan">The clan to check.</param>
        /// <returns>True if the clan has no kingdom, false otherwise.</returns>
        /// <remarks>
        /// Independent clans can be minor factions, mercenaries, or bandit clans.
        /// </remarks>
        public static bool IsIndependent(this Clan clan)
        {
            try
            {
                return clan?.Kingdom == null;

            }
            catch (Exception ex)
            {
                LogException(ex, nameof(IsIndependent));
                return false;
            }
        }

        /// <summary>
        /// Checks if a clan is a vassal of the player's kingdom.
        /// </summary>
        /// <param name="clan">The clan to check.</param>
        /// <returns>True if the clan serves the player's kingdom, false otherwise.</returns>
        /// <remarks>
        /// Excludes the player's own clan (you're not a vassal of yourself).
        /// </remarks>
        public static bool IsPlayerKingdomVassal(this Clan clan)
        {
            try
            {
                return clan != null && (Clan.PlayerClan?.Kingdom) != null && clan.Kingdom == Clan.PlayerClan.Kingdom && clan != Clan.PlayerClan;

            }
            catch (Exception ex)
            {
                LogException(ex, nameof(IsPlayerKingdomVassal));
                return false;
            }
        }

        #endregion

        #region Party Extensions

        /// <summary>
        /// Checks if a party is friendly to the player (not at war).
        /// </summary>
        /// <param name="party">The party to check.</param>
        /// <returns>True if the party's faction is not at war with the player's faction, false otherwise.</returns>
        /// <remarks>
        /// <para>
        /// Friendly parties include:
        /// - Allies in the same kingdom
        /// - Neutral factions
        /// - Your own parties
        /// </para>
        /// <para>
        /// Excludes: Enemies at war with player.
        /// </para>
        /// </remarks>
        public static bool IsFriendlyToPlayer(this MobileParty party)
        {
            try
            {
                return party != null && MobileParty.MainParty != null && party.MapFaction?.IsAtWarWith(MobileParty.MainParty.MapFaction) == false;

            }
            catch (Exception ex)
            {
                LogException(ex, nameof(IsFriendlyToPlayer));
                return false;
            }
        }

        /// <summary>
        /// Gets the total troop count in a party (including wounded).
        /// </summary>
        /// <param name="party">The party to count troops in.</param>
        /// <returns>The total number of troops, or 0 if party/roster is null.</returns>
        /// <remarks>
        /// This includes healthy, wounded, and prisoner troops.
        /// </remarks>
        public static int GetTotalTroopCount(this MobileParty party)
        {
            try
            {
                return party?.MemberRoster?.TotalManCount ?? 0;

            }
            catch (Exception ex)
            {
                LogException(ex, nameof(GetTotalTroopCount));
                return 0;
            }
        }

        /// <summary>
        /// Gets the healthy (non-wounded) troop count in a party.
        /// </summary>
        /// <param name="party">The party to count healthy troops in.</param>
        /// <returns>The number of healthy troops, or 0 if party/roster is null.</returns>
        /// <remarks>
        /// Only counts troops that can fight immediately (not wounded).
        /// </remarks>
        public static int GetHealthyTroopCount(this MobileParty party)
        {
            try
            {
                return party?.MemberRoster?.TotalHealthyCount ?? 0;

            }
            catch (Exception ex)
            {
                LogException(ex, nameof(GetHealthyTroopCount));
                return 0;
            }
        }

        /// <summary>
        /// Heals all wounded troops in a party.
        /// </summary>
        /// <param name="party">The party whose troops to heal.</param>
        /// <returns>The total number of troops healed.</returns>
        /// <remarks>
        /// <para>
        /// This method iterates through the party roster and moves wounded troops
        /// back to healthy status by manipulating the roster counts.
        /// </para>
        /// <para>
        /// Performance: O(n) where n is the number of different troop types in the roster.
        /// Typically 10-30 iterations for most parties.
        /// </para>
        /// </remarks>
        public static int HealAllWounded(this MobileParty party)
        {
            try
            {
                if (party?.MemberRoster == null)
                {
                    return 0;
                }

                int totalHealed = 0;

                // Iterate through all troop types in the roster
                for (int i = 0; i < party.MemberRoster.Count; i++)
                {
                    TroopRosterElement element = party.MemberRoster.GetElementCopyAtIndex(i);
                    int woundedCount = element.WoundedNumber;

                    // Only process if there are wounded troops
                    if (woundedCount > 0)
                    {
                        // AddToCountsAtIndex(index, healthyToAdd, woundedToRemove)
                        // We're removing wounded (negative number) to convert them to healthy
                        _ = party.MemberRoster.AddToCountsAtIndex(i, 0, -woundedCount);
                        totalHealed += woundedCount;
                    }
                }

                return totalHealed;

            }
            catch (Exception ex)
            {
                LogException(ex, nameof(HealAllWounded));
                return 0;
            }
        }

        #endregion

        #region Agent Extensions

        /// <summary>
        /// Checks if an agent is an enemy of the player in the current mission.
        /// </summary>
        /// <param name="agent">The agent to check.</param>
        /// <returns>True if the agent is hostile to the player, false otherwise.</returns>
        /// <remarks>
        /// Only works during missions (battles, arenas, etc.).
        /// Returns false if not in a mission or if player agent doesn't exist.
        /// </remarks>
        public static bool IsPlayerEnemy(this Agent agent)
        {
            try
            {
                Agent? mainAgent = Mission.Current?.MainAgent;
                return mainAgent != null && agent?.IsEnemyOf(mainAgent) == true;

            }
            catch (Exception ex)
            {
                LogException(ex, nameof(IsPlayerEnemy));
                return false;
            }
        }

        /// <summary>
        /// Fully heals an agent to maximum health.
        /// </summary>
        /// <param name="agent">The agent to heal.</param>
        /// <remarks>
        /// <para>
        /// Sets the agent's health to their HealthLimit (maximum possible health).
        /// Only works if the agent is active (alive and not removed from mission).
        /// </para>
        /// <para>
        /// Commonly used in combat cheat behaviors to maintain full health.
        /// </para>
        /// </remarks>
        public static void FullHeal(this Agent agent)
        {
            try
            {
                if (agent?.IsActive() == true)
                {
                    agent.Health = agent.HealthLimit;
                }

            }
            catch (Exception ex)
            {
                LogException(ex, nameof(FullHeal));
            }
        }

        /// <summary>
        /// Checks if an agent is the player character.
        /// </summary>
        /// <param name="agent">The agent to check.</param>
        /// <returns>True if the agent is player-controlled, false otherwise.</returns>
        /// <remarks>
        /// Useful for filtering agents in mission behaviors to only affect the player.
        /// </remarks>
        public static bool IsPlayer(this Agent agent)
        {
            try
            {
                return agent?.IsPlayerControlled == true;

            }
            catch (Exception ex)
            {
                LogException(ex, nameof(IsPlayer));
                return false;
            }
        }

        #endregion

        #region Item Extensions

        /// <summary>
        /// Checks if an item is food.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>True if the item is consumable food, false otherwise.</returns>
        /// <remarks>
        /// Food items include grain, meat, butter, fish, etc.
        /// Used for food management features.
        /// </remarks>
        public static bool IsFoodItem(this ItemObject item)
        {
            try
            {
                return item?.IsFood == true;

            }
            catch (Exception ex)
            {
                LogException(ex, nameof(IsFoodItem));
                return false;
            }
        }

        /// <summary>
        /// Checks if an item is a weapon.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>True if the item has a weapon component, false otherwise.</returns>
        /// <remarks>
        /// Weapons include swords, bows, shields, etc.
        /// Useful for filtering items by category.
        /// </remarks>
        public static bool IsWeaponItem(this ItemObject item)
        {
            try
            {
                return item?.WeaponComponent != null;

            }
            catch (Exception ex)
            {
                LogException(ex, nameof(IsWeaponItem));
                return false;
            }
        }

        /// <summary>
        /// Checks if an item is armor.
        /// </summary>
        /// <param name="item">The item to check.</param>
        /// <returns>True if the item has an armor component, false otherwise.</returns>
        /// <remarks>
        /// Armor includes helmets, body armor, gloves, boots, etc.
        /// </remarks>
        public static bool IsArmorItem(this ItemObject item)
        {
            try
            {
                return item?.ArmorComponent != null;

            }
            catch (Exception ex)
            {
                LogException(ex, nameof(IsArmorItem));
                return false;
            }
        }

        #endregion

        #region Skill Extensions

        /// <summary>
        /// Gets all combat-related skills.
        /// </summary>
        /// <returns>A list of combat <see cref="SkillObject"/> instances.</returns>
        /// <remarks>
        /// <para>
        /// Combat skills include:
        /// - Melee: One-handed, Two-handed, Polearm
        /// - Ranged: Bow, Crossbow, Throwing
        /// - Movement: Athletics, Riding
        /// </para>
        /// <para>
        /// Useful for selective skill XP boosting (e.g., only level combat skills).
        /// </para>
        /// </remarks>
        public static List<SkillObject> GetCombatSkills()
        {
            return
            [
                DefaultSkills.OneHanded,
                    DefaultSkills.TwoHanded,
                    DefaultSkills.Polearm,
                    DefaultSkills.Bow,
                    DefaultSkills.Crossbow,
                    DefaultSkills.Throwing,
                    DefaultSkills.Athletics,
                    DefaultSkills.Riding
            ];
        }

        /// <summary>
        /// Gets all non-combat skills (social, economic, and strategic).
        /// </summary>
        /// <returns>A list of non-combat <see cref="SkillObject"/> instances.</returns>
        /// <remarks>
        /// <para>
        /// Non-combat skills include:
        /// - Social: Charm, Leadership
        /// - Economic: Trade, Steward
        /// - Strategic: Tactics, Scouting, Engineering
        /// - Special: Crafting, Roguery, Medicine
        /// </para>
        /// <para>
        /// Useful for selective skill XP boosting or analysis.
        /// </para>
        /// </remarks>
        public static List<SkillObject> GetNonCombatSkills()
        {
            return
            [
                DefaultSkills.Crafting,
                    DefaultSkills.Tactics,
                    DefaultSkills.Scouting,
                    DefaultSkills.Roguery,
                    DefaultSkills.Charm,
                    DefaultSkills.Leadership,
                    DefaultSkills.Trade,
                    DefaultSkills.Steward,
                    DefaultSkills.Medicine,
                    DefaultSkills.Engineering
            ];
        }

        #endregion

        #region Exception Handling

        /// <summary>
        /// Logs an exception with consistent formatting for CheatExtensions methods.
        /// </summary>
        /// <param name="ex">The exception to log.</param>
        /// <param name="methodName">The name of the method where the exception occurred.</param>
        /// <remarks>
        /// This helper method centralizes exception logging to ensure consistent error reporting
        /// and reduce code duplication across all CheatExtensions methods.
        /// </remarks>
        private static void LogException(Exception ex, string methodName)
        {
            ModLogger.Error($"[CheatExtensions] Error in {methodName}: {ex.Message}");
            ModLogger.Error($"Stack trace: {ex.StackTrace}");
        }

        #endregion
    }
}
