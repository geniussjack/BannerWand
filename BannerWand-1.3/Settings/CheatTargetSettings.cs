#nullable enable
using BannerWand.Utils;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace BannerWand.Settings
{
    /// <summary>
    /// Target selection settings for BannerWand cheat mod.
    /// Determines which heroes are affected by cheats.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This settings class is separate from <see cref="CheatSettings"/> to provide
    /// a dedicated UI page for target configuration. It works with <see cref="Utils.TargetFilter"/>
    /// to determine which game entities should receive cheat effects.
    /// </para>
    /// <para>
    /// Target Categories:
    /// - Player: The player character and their clan members
    /// - Kingdoms: Rulers, vassals, and nobles within kingdoms
    /// - Minor Clans: Leaders and members of minor factions
    /// </para>
    /// <para>
    /// Performance Note: Enabling many NPC targets can impact performance as cheats
    /// will iterate through large numbers of heroes. Use selectively for best results.
    /// </para>
    /// </remarks>
    public class CheatTargetSettings : AttributeGlobalSettings<CheatTargetSettings>
    {
        #region MCM Configuration

        /// <summary>
        /// Gets the unique identifier for this settings instance.
        /// Must be different from <see cref="CheatSettings.Id"/> to create separate UI page.
        /// </summary>
        public override string Id => "BannerWandTargets";

        /// <summary>
        /// Gets the display name shown in MCM settings menu.
        /// </summary>
        public override string DisplayName => "BannerWand - Targets";

        /// <summary>
        /// Gets the folder name for settings persistence.
        /// Shares the same folder as <see cref="CheatSettings"/> for organization.
        /// </summary>
        public override string FolderName => "BannerWand";

        /// <summary>
        /// Gets the settings file format version.
        /// </summary>
        public override string FormatType => "json2";

        #endregion

        #region Category: Player

        /// <summary>
        /// Apply cheats to the player character (the main hero).
        /// </summary>
        [SettingPropertyBool("{=BW_Target_ApplyToPlayer}Apply to Player", Order = 0, RequireRestart = false, HintText = "{=BW_Target_ApplyToPlayer_Hint}Applies selected cheats to the player character.")]
        [SettingPropertyGroup("{=BW_Category_Player}Player", GroupOrder = 0)]
        public bool ApplyToPlayer { get; set; } = false;

        /// <summary>
        /// Apply cheats to all members of the player's clan (companions, family).
        /// </summary>
        [SettingPropertyBool("{=BW_Target_ApplyToPlayerClanMembers}Apply to Player's Clan Members", Order = 1, RequireRestart = false, HintText = "{=BW_Target_ApplyToPlayerClanMembers_Hint}Applies selected cheats to all members of the player's clan.")]
        [SettingPropertyGroup("{=BW_Category_Player}Player", GroupOrder = 0)]
        public bool ApplyToPlayerClanMembers { get; set; } = false;

        #endregion

        #region Category: Kingdoms

        /// <summary>
        /// Apply cheats to all kingdom rulers (kings/queens of all kingdoms).
        /// </summary>
        [SettingPropertyBool("{=BW_Target_ApplyToKingdomRulers}Apply to Kingdom Rulers", Order = 0, RequireRestart = false, HintText = "{=BW_Target_ApplyToKingdomRulers_Hint}Applies selected cheats to rulers of all kingdoms.")]
        [SettingPropertyGroup("{=BW_Category_Kingdoms}Kingdoms", GroupOrder = 1)]
        public bool ApplyToKingdomRulers { get; set; } = false;

        /// <summary>
        /// Apply cheats to the ruler of the player's current kingdom (if player is a vassal or the ruler).
        /// </summary>
        [SettingPropertyBool("{=BW_Target_ApplyToPlayerKingdomRuler}Apply to Player Kingdom Ruler", Order = 1, RequireRestart = false, HintText = "{=BW_Target_ApplyToPlayerKingdomRuler_Hint}Applies cheats to the ruler of the player's current kingdom.")]
        [SettingPropertyGroup("{=BW_Category_Kingdoms}Kingdoms", GroupOrder = 1)]
        public bool ApplyToPlayerKingdomRuler { get; set; } = false;

        /// <summary>
        /// Apply cheats to all vassal clan leaders of any kingdom.
        /// </summary>
        [SettingPropertyBool("{=BW_Target_ApplyToKingdomVassals}Apply to Kingdom Vassals", Order = 2, RequireRestart = false, HintText = "{=BW_Target_ApplyToKingdomVassals_Hint}Applies selected cheats to all vassal clan leaders of any kingdom.")]
        [SettingPropertyGroup("{=BW_Category_Kingdoms}Kingdoms", GroupOrder = 1)]
        public bool ApplyToKingdomVassals { get; set; } = false;

        /// <summary>
        /// Apply cheats to all vassal clan leaders of the player's kingdom.
        /// </summary>
        [SettingPropertyBool("{=BW_Target_ApplyToPlayerKingdomVassals}Apply to Player Kingdom Vassals", Order = 3, RequireRestart = false, HintText = "{=BW_Target_ApplyToPlayerKingdomVassals_Hint}Applies selected cheats to all vassal clan leaders of the player's kingdom.")]
        [SettingPropertyGroup("{=BW_Category_Kingdoms}Kingdoms", GroupOrder = 1)]
        public bool ApplyToPlayerKingdomVassals { get; set; } = false;

        /// <summary>
        /// Apply cheats to all noble heroes of any kingdom (including clan members).
        /// </summary>
        [SettingPropertyBool("{=BW_Target_ApplyToKingdomNobles}Apply to Kingdom Nobles", Order = 4, RequireRestart = false, HintText = "{=BW_Target_ApplyToKingdomNobles_Hint}Applies selected cheats to all noble heroes of any kingdom.")]
        [SettingPropertyGroup("{=BW_Category_Kingdoms}Kingdoms", GroupOrder = 1)]
        public bool ApplyToKingdomNobles { get; set; } = false;

        /// <summary>
        /// Apply cheats to all noble heroes of the player's kingdom.
        /// </summary>
        [SettingPropertyBool("{=BW_Target_ApplyToPlayerKingdomNobles}Apply to Player Kingdom Nobles", Order = 5, RequireRestart = false, HintText = "{=BW_Target_ApplyToPlayerKingdomNobles_Hint}Applies selected cheats to all noble heroes of the player's kingdom.")]
        [SettingPropertyGroup("{=BW_Category_Kingdoms}Kingdoms", GroupOrder = 1)]
        public bool ApplyToPlayerKingdomNobles { get; set; } = false;

        #endregion

        #region Category: Minor Clans

        /// <summary>
        /// Apply cheats to leaders of all minor factions (clans listed in Encyclopedia → Clans → Minor).
        /// </summary>
        [SettingPropertyBool("{=BW_Target_ApplyToMinorClanLeaders}Apply to Minor Clan Leaders", Order = 0, RequireRestart = false, HintText = "{=BW_Target_ApplyToMinorClanLeaders_Hint}Applies selected cheats to leaders of all minor factions.")]
        [SettingPropertyGroup("{=BW_Category_MinorClans}Minor Clans", GroupOrder = 2)]
        public bool ApplyToMinorClanLeaders { get; set; } = false;

        /// <summary>
        /// Apply cheats to all noble heroes from minor clans.
        /// </summary>
        [SettingPropertyBool("{=BW_Target_ApplyToMinorClanMembers}Apply to Minor Clan Members", Order = 1, RequireRestart = false, HintText = "{=BW_Target_ApplyToMinorClanMembers_Hint}Applies selected cheats to all noble heroes from minor clans.")]
        [SettingPropertyGroup("{=BW_Category_MinorClans}Minor Clans", GroupOrder = 2)]
        public bool ApplyToMinorClanMembers { get; set; } = false;

        #endregion

        #region Info Text

        /// <summary>
        /// Static informational text displayed at the bottom of the MCM page.
        /// The group name itself serves as the informational message.
        /// This is a placeholder property to ensure the group is displayed.
        /// </summary>
        [SettingPropertyGroup("{=BW_Target_InfoText}Multiple options can be combined. Duplicate applications are automatically prevented.", GroupOrder = 3)]
        public bool InfoTextPlaceholder { get; set; } = false;

        #endregion

        #region Helper Methods

        /// <summary>
        /// Collects all target heroes based on current settings into a HashSet to prevent duplicates.
        /// </summary>
        /// <returns>A HashSet containing all heroes that should receive cheat effects.</returns>
        /// <remarks>
        /// <para>
        /// This method iterates through all relevant game entities and collects heroes
        /// based on enabled target options. Uses HashSet to automatically prevent duplicates
        /// (e.g., when player is also the kingdom ruler).
        /// </para>
        /// <para>
        /// Performance: O(n) where n is the number of heroes/clans/kingdoms in the game.
        /// Should be called once per cheat application, not every frame.
        /// </para>
        /// <para>
        /// All null checks are performed to prevent crashes when game state is invalid
        /// (e.g., player not in a kingdom, clan without leader, etc.).
        /// </para>
        /// </remarks>
        public HashSet<Hero> CollectTargetHeroes()
        {
            HashSet<Hero> targets = [];

            try
            {
                // Category: Player
                if (ApplyToPlayer && Hero.MainHero != null)
                {
                    _ = targets.Add(Hero.MainHero);
                }

                if (ApplyToPlayerClanMembers && Hero.MainHero?.Clan != null)
                {
                    foreach (Hero hero in Hero.MainHero.Clan.Heroes)
                    {
                        if (hero != null)
                        {
                            _ = targets.Add(hero);
                        }
                    }
                }

                // Category: Kingdoms
                if (ApplyToKingdomRulers)
                {
                    foreach (Kingdom kingdom in Kingdom.All)
                    {
                        if (kingdom?.Leader != null)
                        {
                            _ = targets.Add(kingdom.Leader);
                        }
                    }
                }

                if (ApplyToPlayerKingdomRuler)
                {
                    Kingdom? playerKingdom = Hero.MainHero?.Clan?.Kingdom;
                    if (playerKingdom?.Leader != null)
                    {
                        _ = targets.Add(playerKingdom.Leader);
                    }
                }

                if (ApplyToKingdomVassals)
                {
                    foreach (Kingdom kingdom in Kingdom.All)
                    {
                        if (kingdom != null)
                        {
                            foreach (Clan clan in kingdom.Clans)
                            {
                                // Exclude ruling clan (ruler is handled by ApplyToKingdomRulers)
                                if (clan != null && clan != kingdom.RulingClan && clan.Leader != null)
                                {
                                    _ = targets.Add(clan.Leader);
                                }
                            }
                        }
                    }
                }

                if (ApplyToPlayerKingdomVassals)
                {
                    Kingdom? playerKingdom = Hero.MainHero?.Clan?.Kingdom;
                    if (playerKingdom != null)
                    {
                        foreach (Clan clan in playerKingdom.Clans)
                        {
                            // Exclude player clan and ruling clan
                            if (clan != null && clan != Clan.PlayerClan && clan != playerKingdom.RulingClan && clan.Leader != null)
                            {
                                _ = targets.Add(clan.Leader);
                            }
                        }
                    }
                }

                if (ApplyToKingdomNobles)
                {
                    foreach (Kingdom kingdom in Kingdom.All)
                    {
                        if (kingdom != null)
                        {
                            foreach (Clan clan in kingdom.Clans)
                            {
                                if (clan != null)
                                {
                                    foreach (Hero hero in clan.Heroes)
                                    {
                                        if (hero != null && hero.CharacterObject?.IsHero == true)
                                        {
                                            _ = targets.Add(hero);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (ApplyToPlayerKingdomNobles)
                {
                    Kingdom? playerKingdom = Hero.MainHero?.Clan?.Kingdom;
                    if (playerKingdom != null)
                    {
                        foreach (Clan clan in playerKingdom.Clans)
                        {
                            if (clan != null)
                            {
                                foreach (Hero hero in clan.Heroes)
                                {
                                    if (hero != null && hero.CharacterObject?.IsHero == true)
                                    {
                                        _ = targets.Add(hero);
                                    }
                                }
                            }
                        }
                    }
                }

                // Category: Minor Clans
                if (ApplyToMinorClanLeaders)
                {
                    foreach (Clan clan in Clan.All)
                    {
                        if (clan?.IsMinorFaction == true && clan.Leader != null)
                        {
                            _ = targets.Add(clan.Leader);
                        }
                    }
                }

                if (ApplyToMinorClanMembers)
                {
                    foreach (Clan clan in Clan.All)
                    {
                        if (clan?.IsMinorFaction == true)
                        {
                            foreach (Hero hero in clan.Heroes)
                            {
                                if (hero != null && hero.CharacterObject?.IsHero == true)
                                {
                                    _ = targets.Add(hero);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CheatTargetSettings] Error in CollectTargetHeroes: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }

            return targets;
        }

        /// <summary>
        /// Checks if any NPC target option is enabled.
        /// Used for early-exit optimizations in cheat code.
        /// </summary>
        /// <returns>True if at least one NPC target option is enabled.</returns>
        public bool HasAnyNPCTargetEnabled()
        {
            try
            {
                return ApplyToPlayerClanMembers ||
                       ApplyToKingdomRulers ||
                       ApplyToPlayerKingdomRuler ||
                       ApplyToKingdomVassals ||
                       ApplyToPlayerKingdomVassals ||
                       ApplyToKingdomNobles ||
                       ApplyToPlayerKingdomNobles ||
                       ApplyToMinorClanLeaders ||
                       ApplyToMinorClanMembers;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CheatTargetSettings] Error in HasAnyNPCTargetEnabled: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Checks if the player target is enabled.
        /// Convenience method for clearer code readability.
        /// </summary>
        /// <returns>True if player target is enabled, false otherwise.</returns>
        public bool IsPlayerTargetEnabled()
        {
            try
            {
                return ApplyToPlayer;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CheatTargetSettings] Error in IsPlayerTargetEnabled: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        #endregion
    }
}
