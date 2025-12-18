#nullable enable
#pragma warning disable CS0169 // Fields _npcAttributePointsApplied and _npcFocusPointsApplied are used in conditional logic
using BannerWandRetro.Constants;
using BannerWandRetro.Settings;
using BannerWandRetro.Utils;
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace BannerWandRetro.Behaviors
{
    /// <summary>
    /// Campaign behavior that applies cheats for attribute points, focus points, and renown.
    /// Attribute/Focus: PLAYER ONLY. Renown: Player and NPC clans (based on TargetSettings).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This behavior extends <see cref="CampaignBehaviorBase"/> and manages cheats
    /// that primarily affect character development (attributes, focus) and clan renown.
    /// </para>
    /// <para>
    /// One-time cheats use a flag system similar to <see cref="PlayerCheatBehavior"/>
    /// to ensure they're only applied once when the setting changes from 0 to a non-zero value.
    /// </para>
    /// <para>
    /// Performance: Attribute/focus point application is player-only. Renown runs hourly
    /// and can affect multiple clans if NPC targets are enabled.
    /// </para>
    /// </remarks>
    public class NPCCheatBehavior : CampaignBehaviorBase
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
        /// Tracks whether attribute points have been applied to prevent repeated application.
        /// </summary>
        private bool _attributePointsApplied = false;

        /// <summary>
        /// Tracks whether focus points have been applied to prevent repeated application.
        /// </summary>
        private bool _focusPointsApplied = false;

        /// <summary>
        /// Tracks whether NPC attribute points have been applied to prevent repeated application.
        /// Used in conditional logic: if (settings.NPCEditAttributePoints != 0 && !_npcAttributePointsApplied)
        /// </summary>
        private bool _npcAttributePointsApplied = false;

        /// <summary>
        /// Tracks whether NPC focus points have been applied to prevent repeated application.
        /// Used in conditional logic: if (settings.NPCEditFocusPoints != 0 && !_npcFocusPointsApplied)
        /// </summary>
        private bool _npcFocusPointsApplied = false;

        #endregion

        #region Event Registration

        /// <summary>
        /// Registers this behavior to listen to campaign events.
        /// </summary>
        /// <remarks>
        /// Events registered:
        /// - <see cref="CampaignEvents.DailyTickEvent"/> - For attribute and focus points
        /// - <see cref="CampaignEvents.HourlyTickEvent"/> - For renown
        /// - <see cref="CampaignEvents.OnSessionLaunchedEvent"/> - For initialization
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
                ModLogger.Error($"[NPCCheatBehavior] Error in RegisterEvents: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        // NOTE: OnHeroGainedSkillEvent no longer exists in current Bannerlord API
        // Skill XP multiplier is now handled by CustomGenericXpModel and SkillXPCheatBehavior


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
                ModLogger.Error($"[NPCCheatBehavior] Error in SyncData: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
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
            ModLogger.Log("NPCCheatBehavior session launched - resetting one-time flags");

            // Reset one-time application flags
            _attributePointsApplied = false;
            _focusPointsApplied = false;
            _npcAttributePointsApplied = false;
            _npcFocusPointsApplied = false;

            // Explicit usage to suppress CS0169 warnings (fields are used in conditional logic in ApplyAttributeAndFocusPoints)
            _ = _npcAttributePointsApplied;
            _ = _npcFocusPointsApplied;
        }

        /// <summary>
        /// Called every in-game hour.
        /// Handles renown boost for player clan and attribute/focus point changes.
        /// </summary>
        private void OnHourlyTick()
        {
            // Renown management - gradually increase player clan renown
            ApplyRenown();

            // Apply attribute and focus point changes immediately (moved from daily tick)
            ApplyAttributeAndFocusPoints();
        }

        /// <summary>
        /// Called every in-game day.
        /// Reserved for future use.
        /// </summary>
        private void OnDailyTick()
        {
            // Attribute and focus points now applied hourly for immediate effect
        }

        #endregion

        #region Attribute and Focus Points

        /// <summary>
        /// Applies attribute and focus point changes to PLAYER ONLY (no NPCs).
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
        private void ApplyAttributeAndFocusPoints()
        {
            CheatSettings? settings = Settings;
            CheatTargetSettings? targetSettings = TargetSettings;
            if (settings is null || targetSettings is null)
            {
                return;
            }

            // === ATTRIBUTE POINTS (PLAYER ONLY) ===

            if (settings.EditAttributePoints != 0 && !_attributePointsApplied)
            {
                if (targetSettings.ApplyToPlayer && Hero.MainHero is not null)
                {
                    Hero.MainHero.HeroDeveloper.UnspentAttributePoints += settings.EditAttributePoints;

                    // Clamp to prevent negative values
                    if (Hero.MainHero.HeroDeveloper.UnspentAttributePoints < 0)
                    {
                        Hero.MainHero.HeroDeveloper.UnspentAttributePoints = 0;
                    }

                    ModLogger.LogCheat("Attribute Points Edit", true, settings.EditAttributePoints, "player");
                }

                _attributePointsApplied = true;
            }
            else if (settings.EditAttributePoints == 0)
            {
                // Reset flag when setting returns to zero
                _attributePointsApplied = false;
            }

            // === FOCUS POINTS (PLAYER ONLY) ===

            if (settings.EditFocusPoints != 0 && !_focusPointsApplied)
            {
                if (targetSettings.ApplyToPlayer && Hero.MainHero is not null)
                {
                    Hero.MainHero.HeroDeveloper.UnspentFocusPoints += settings.EditFocusPoints;

                    // Clamp to prevent negative values
                    if (Hero.MainHero.HeroDeveloper.UnspentFocusPoints < 0)
                    {
                        Hero.MainHero.HeroDeveloper.UnspentFocusPoints = 0;
                    }

                    ModLogger.LogCheat("Focus Points Edit", true, settings.EditFocusPoints, "player");
                }

                _focusPointsApplied = true;
            }
            else if (settings.EditFocusPoints == 0)
            {
                // Reset flag when setting returns to zero
                _focusPointsApplied = false;
            }

            // === NPC ATTRIBUTE POINTS ===

            if (settings.NPCEditAttributePoints != 0 && !_npcAttributePointsApplied)
            {
                if (targetSettings.HasAnyNPCTargetEnabled())
                {
                    List<Hero>? allHeroes = CampaignDataCache.AllAliveHeroes;
                    if (allHeroes is not null)
                    {
                        int affectedCount = 0;
                        foreach (Hero hero in allHeroes)
                        {
                            if (hero != Hero.MainHero && TargetFilter.ShouldApplyCheat(hero))
                            {
                                hero.HeroDeveloper.UnspentAttributePoints += settings.NPCEditAttributePoints;

                                // Clamp to prevent negative values
                                if (hero.HeroDeveloper.UnspentAttributePoints < 0)
                                {
                                    hero.HeroDeveloper.UnspentAttributePoints = 0;
                                }

                                affectedCount++;
                            }
                        }
                        if (affectedCount > 0)
                        {
                            ModLogger.LogCheat("NPC Attribute Points Edit", true, settings.NPCEditAttributePoints, $"{affectedCount} NPC heroes");
                        }
                    }
                }
                _npcAttributePointsApplied = true;
            }
            else if (settings.NPCEditAttributePoints == 0)
            {
                // Reset flag when setting returns to zero
                _npcAttributePointsApplied = false;
            }

            // === NPC FOCUS POINTS ===

            if (settings.NPCEditFocusPoints != 0 && !_npcFocusPointsApplied)
            {
                if (targetSettings.HasAnyNPCTargetEnabled())
                {
                    List<Hero>? allHeroes = CampaignDataCache.AllAliveHeroes;
                    if (allHeroes is not null)
                    {
                        int affectedCount = 0;
                        foreach (Hero hero in allHeroes)
                        {
                            if (hero != Hero.MainHero && TargetFilter.ShouldApplyCheat(hero))
                            {
                                hero.HeroDeveloper.UnspentFocusPoints += settings.NPCEditFocusPoints;

                                // Clamp to prevent negative values
                                if (hero.HeroDeveloper.UnspentFocusPoints < 0)
                                {
                                    hero.HeroDeveloper.UnspentFocusPoints = 0;
                                }

                                affectedCount++;
                            }
                        }
                        if (affectedCount > 0)
                        {
                            ModLogger.LogCheat("NPC Focus Points Edit", true, settings.NPCEditFocusPoints, $"{affectedCount} NPC heroes");
                        }
                    }
                }
                _npcFocusPointsApplied = true;
            }
            else if (settings.NPCEditFocusPoints == 0)
            {
                // Reset flag when setting returns to zero
                _npcFocusPointsApplied = false;
            }
        }

        #endregion

        #region Renown

        /// <summary>
        /// Applies unlimited renown boost to player and NPC clans.
        /// Gradually increases renown to a high value (10,000) when enabled.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Renown is added incrementally (999 per hour) rather than set to max immediately.
        /// This creates rapid progression while avoiding potential game engine issues
        /// with sudden massive renown changes.
        /// </para>
        /// <para>
        /// Maximum renown target: 10,000 (sufficient for all gameplay purposes).
        /// Increment: 999 per hour (reaches max in ~10 game hours if starting from 0).
        /// </para>
        /// <para>
        /// NOTE: Renown Multiplier feature is currently disabled due to API limitations.
        /// The multiplier would require hooking into renown gain events which don't
        /// exist in the current Bannerlord API.
        /// </para>
        /// </remarks>
        private static void ApplyRenown()
        {
            CheatSettings? settings = Settings;
            CheatTargetSettings? targetSettings = TargetSettings;
            if (settings is null || targetSettings is null)
            {
                return;
            }

            // Early return if cheat not enabled
            if (!settings.UnlimitedRenown)
            {
                return;
            }

            // Apply to player clan
            if (targetSettings.ApplyToPlayer && Clan.PlayerClan is not null)
            {
                Clan playerClan = Clan.PlayerClan;

                // Only add renown if below target (optimization: avoid unnecessary method calls)
                if (playerClan.Renown < GameConstants.MaxUnlimitedRenown)
                {
                    // Add renown (false = don't show notification for every tick)
                    playerClan.AddRenown(GameConstants.UnlimitedRenownPerHour, false);

                    // Log at milestones (when crossing thousand mark)
                    float currentRenown = playerClan.Renown;
                    if (currentRenown % 1000 < GameConstants.UnlimitedRenownPerHour)
                    {
                        ModLogger.Debug($"Player clan renown increased to {currentRenown:F0}");
                    }
                }
            }

            // Apply to NPC clans if any NPC targets are enabled
            if (targetSettings.HasAnyNPCTargetEnabled())
            {
                List<Clan>? allClans = CampaignDataCache.AllClans;
                if (allClans is null)
                {
                    return;
                }
                foreach (Clan clan in allClans)
                {
                    // Skip player clan (already handled above)
                    if (clan == Clan.PlayerClan)
                    {
                        continue;
                    }

                    // Check if this clan should receive the cheat
                    if (TargetFilter.ShouldApplyCheatToClan(clan))
                    {
                        // Only add renown if below target (optimization: avoid unnecessary method calls)
                        if (clan.Renown < GameConstants.MaxUnlimitedRenown)
                        {
                            clan.AddRenown(GameConstants.UnlimitedRenownPerHour, false);
                        }
                    }
                }
            }

            // NOTE: Renown Multiplier and Skill XP Multiplier features are disabled
            // due to lack of appropriate event hooks in current Bannerlord API
            // Consider implementing via Harmony patches if needed in future
        }

        #endregion
    }
}
#pragma warning restore CS0169
