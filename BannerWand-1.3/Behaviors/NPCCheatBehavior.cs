using BannerWand.Constants;
using BannerWand.Settings;
using BannerWand.Utils;
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;

namespace BannerWand.Behaviors
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
        private static CheatSettings Settings => CheatSettings.Instance;

        /// <summary>
        /// Gets the current target settings instance.
        /// </summary>
        private static CheatTargetSettings TargetSettings => CheatTargetSettings.Instance;

        /// <summary>
        /// Tracks whether attribute points have been applied to prevent repeated application.
        /// </summary>
        private bool _attributePointsApplied = false;

        /// <summary>
        /// Tracks whether focus points have been applied to prevent repeated application.
        /// </summary>
        private bool _focusPointsApplied = false;

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
            // No persistent data to sync - settings are managed by MCM
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
        private void ApplyAttributeAndFocusPoints()
        {
            // === ATTRIBUTE POINTS (PLAYER ONLY) ===

            if (Settings.EditAttributePoints != 0 && !_attributePointsApplied)
            {
                if (TargetSettings.ApplyToPlayer && Hero.MainHero is not null)
                {
                    Hero.MainHero.HeroDeveloper.UnspentAttributePoints += Settings.EditAttributePoints;

                    // Clamp to prevent negative values
                    if (Hero.MainHero.HeroDeveloper.UnspentAttributePoints < 0)
                    {
                        Hero.MainHero.HeroDeveloper.UnspentAttributePoints = 0;
                    }

                    ModLogger.LogCheat("Attribute Points Edit", true, Settings.EditAttributePoints, "player");
                }

                _attributePointsApplied = true;
            }
            else if (Settings.EditAttributePoints == 0)
            {
                // Reset flag when setting returns to zero (ready for next application)
                _attributePointsApplied = false;
            }

            // === FOCUS POINTS (PLAYER ONLY) ===

            if (Settings.EditFocusPoints != 0 && !_focusPointsApplied)
            {
                if (TargetSettings.ApplyToPlayer && Hero.MainHero is not null)
                {
                    Hero.MainHero.HeroDeveloper.UnspentFocusPoints += Settings.EditFocusPoints;

                    // Clamp to prevent negative values
                    if (Hero.MainHero.HeroDeveloper.UnspentFocusPoints < 0)
                    {
                        Hero.MainHero.HeroDeveloper.UnspentFocusPoints = 0;
                    }

                    ModLogger.LogCheat("Focus Points Edit", true, Settings.EditFocusPoints, "player");
                }

                _focusPointsApplied = true;
            }
            else if (Settings.EditFocusPoints == 0)
            {
                // Reset flag when setting returns to zero (ready for next application)
                _focusPointsApplied = false;
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
            // Early return if cheat not enabled
            if (!Settings.UnlimitedRenown)
            {
                return;
            }

            // Apply to player clan
            if (TargetSettings.ApplyToPlayer && Clan.PlayerClan is not null)
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
            if (TargetSettings.HasAnyNPCTargetEnabled())
            {
                List<Clan> allClans = CampaignDataCache.AllClans;
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
