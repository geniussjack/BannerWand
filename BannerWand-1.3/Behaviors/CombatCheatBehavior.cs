#nullable enable
using BannerWand.Constants;
using BannerWand.Patches;
using BannerWand.Settings;
using BannerWand.Utils;
using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace BannerWand.Behaviors
{
    /// <summary>
    /// Mission behavior that applies combat-specific cheats during tactical battles.
    /// Handles health, shield durability, damage mitigation, one-hit kills, and unlimited ammo in real-time combat.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This behavior extends <see cref="MissionLogic"/> (not <see cref="CampaignBehaviorBase"/>) because
    /// it operates during missions (battles, arenas, tournaments) rather than on the campaign map.
    /// </para>
    /// <para>
    /// Performance: Runs every mission tick (~60 FPS), so optimizations are critical.
    /// Uses early returns and minimal allocations to maintain smooth gameplay.
    /// </para>
    /// <para>
    /// Agent iteration is optimized by caching Mission.Current.Agents and using minimal LINQ.
    /// For large battles (100+ agents), this can save significant CPU time.
    /// </para>
    /// </remarks>
    public class CombatCheatBehavior : MissionLogic
    {
        #region Constants
        // Combat constants are now defined in GameConstants for consistency
        #endregion

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
        /// Tracks whether Infinite Health bonus has been applied to the player agent.
        /// Key: Agent index, Value: Whether bonus was applied.
        /// </summary>
        private readonly System.Collections.Generic.Dictionary<int, bool> _infiniteHealthApplied = [];

        /// <summary>
        /// Tracks whether unlimited ammo has been logged for current mission.
        /// </summary>
        private bool _unlimitedAmmoLogged;





        #endregion

        #region Mission Events

        /// <summary>
        /// Called when the mission is ending.
        /// Resets one-time application flags for next battle.
        /// </summary>
        protected override void OnEndMission()
        {
            base.OnEndMission();

            // Reset application flags for next mission
            _infiniteHealthApplied.Clear();
            _unlimitedAmmoLogged = false;
        }

        /// <summary>
        /// Called when an agent is built (created) in the mission.
        /// This is the perfect time to apply Infinite Health bonus.
        /// </summary>
        /// <param name="agent">The agent that was built.</param>
        /// <param name="banner">The banner for the agent (can be null).</param>
        public override void OnAgentBuild(Agent agent, Banner? banner)
        {
            base.OnAgentBuild(agent, banner);

            // Early exit if settings are null
            CheatSettings? settings = Settings;
            CheatTargetSettings? targetSettings = TargetSettings;
            if (settings is null || targetSettings is null)
            {
                return;
            }

            // Apply Infinite Health bonus immediately when player agent is built
            if (settings.InfiniteHealth && targetSettings.ApplyToPlayer && agent?.IsPlayerControlled == true)
            {
                ModLogger.Debug($"OnAgentBuild: Player agent built, applying Infinite Health (Agent Index: {agent.Index})");
                ApplyInfiniteHealthToAgent(agent);
            }
        }


        /// <summary>
        /// Called every mission frame (approximately 60 times per second).
        /// Applies continuous combat cheats that need frame-by-frame updates.
        /// </summary>
        /// <remarks>
        /// <para>
        /// PERFORMANCE CRITICAL: This method runs 60+ times per second during combat.
        /// All operations must be highly optimized with early returns and minimal allocations.
        /// </para>
        /// <para>
        /// Cheats applied per-frame:
        /// - Infinite Health (one-time HP boost when player spawns)
        /// - Unlimited player health (restore health if below max)
        /// - Unlimited horse health (restore mount health if below max)
        /// - One-hit kills (reduce enemy health to near-death)
        /// - Unlimited ammunition (prevent ammo decrease by restoring to original amount)
        /// </para>
        /// </remarks>
        public override void OnMissionTick(float dt)
        {
            try
            {
                base.OnMissionTick(dt);

                // Early return if mission not loaded
                if (Mission.Current == null)
                {
                    return;
                }

                // Early exit if settings are null
                CheatSettings? settings = Settings;
                CheatTargetSettings? targetSettings = TargetSettings;
                if (settings is null || targetSettings is null)
                {
                    return;
                }

                // Apply Infinite Health bonus once when player spawns (uses flag to run only once)
                // Also try to apply in OnMissionTick as fallback if OnAgentBuild didn't work
                ApplyInfiniteHealth();

                // Apply continuous combat cheats
                ApplyUnlimitedHealth();
                ApplyUnlimitedHorseHealth();
                ApplyOneHitKills();

                // Apply unlimited ammo - now handled purely by AmmoConsumptionPatch
                ApplyUnlimitedAmmo();

                // Note: Movement speed for campaign map is handled in CustomPartySpeedModel

            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CombatCheatBehavior] Error in OnMissionTick: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Called after an agent receives damage from any source.
        /// Handles damage mitigation and shield durability restoration.
        /// </summary>
        /// <param name="affectedAgent">The agent that was hit.</param>
        /// <param name="affectorAgent">The agent that dealt the damage (can be null).</param>
        /// <param name="affectorWeapon">The weapon used to deal damage.</param>
        /// <param name="blow">Details about the blow (damage, type, etc.).</param>
        /// <param name="attackCollisionData">Collision data for the attack.</param>
        /// <remarks>
        /// <para>
        /// This event is called AFTER damage is applied, so we restore health/durability
        /// rather than preventing damage. This approach is more compatible with game mechanics.
        /// </para>
        /// <para>
        /// Performance: Called frequently during combat but only for agents that take damage.
        /// Much less frequent than OnMissionTick, so slightly more complex logic is acceptable.
        /// </para>
        /// </remarks>
        public override void OnAgentHit(Agent affectedAgent, Agent affectorAgent, in MissionWeapon affectorWeapon,
            in Blow blow, in AttackCollisionData attackCollisionData)
        {
            try
            {
                base.OnAgentHit(affectedAgent, affectorAgent, affectorWeapon, blow, attackCollisionData);

                // Early exit if settings are null
                CheatSettings? settings = Settings;
                CheatTargetSettings? targetSettings = TargetSettings;
                if (settings is null || targetSettings is null)
                {
                    return;
                }

                // CRITICAL FIX: Restore health immediately after taking damage if Infinite Health is enabled
                // This prevents death from one-shot kills that exceed HealthLimit
                if (settings.InfiniteHealth && targetSettings.ApplyToPlayer && affectedAgent?.IsPlayerControlled == true)
                {
                    // Ensure Infinite Health bonus is applied (in case it wasn't applied on spawn)
                    ApplyInfiniteHealthToAgent(affectedAgent);

                    // Restore health immediately after damage to prevent death
                    // If health dropped below a safe threshold, restore it
                    if (affectedAgent.IsActive() && affectedAgent.Health < affectedAgent.HealthLimit)
                    {
                        affectedAgent.Health = affectedAgent.HealthLimit;
                    }
                }

                // Also restore health if Unlimited Health is enabled (but this won't prevent one-shot kills)
                if (settings.UnlimitedHealth && targetSettings.ApplyToPlayer && affectedAgent?.IsPlayerControlled == true)
                {
                    if (affectedAgent.IsActive() && affectedAgent.Health < affectedAgent.HealthLimit)
                    {
                        affectedAgent.Health = affectedAgent.HealthLimit;
                    }
                }

                // Handle One-Hit Kills - ensure enemies die from PLAYER's hits/shots
                // IMPORTANT: Only works when PLAYER is the attacker (not allies)
                if (settings.OneHitKills)
                {
                    Agent? mainAgent = Mission.Current?.MainAgent;

                    // Check: 1) Player exists, 2) Attacker is player, 3) Victim is enemy, 4) Victim is alive
                    if (mainAgent is not null &&
                        affectorAgent?.IsPlayerControlled == true && // Attacker must be player
                        affectedAgent?.IsEnemyOf(mainAgent) == true &&
                        affectedAgent.IsActive() && affectedAgent.IsHuman)
                    {
                        // Set health to 0 to kill instantly
                        // Works for: melee hits, ranged shots (arrows/bolts/thrown), horse charges, etc.
                        affectedAgent.Health = GameConstants.InstantKillHealth;
                    }
                }

                // Restore shield durability for player (if enabled and applicable)
                if (affectedAgent is not null)
                {
                    ApplyUnlimitedShieldDurability(affectedAgent);
                }

            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CombatCheatBehavior] Error in OnAgentHit: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        #endregion

        #region Shield Durability

        /// <summary>
        /// Restores shield durability for player agent after taking damage.
        /// </summary>
        /// <param name="agent">The agent whose shield to restore.</param>
        /// <remarks>
        /// <para>
        /// Shield durability is tracked per equipment slot. This method iterates through
        /// all weapon slots to find shields and restore their hit points.
        /// </para>
        /// <para>
        /// Bannerlord shield mechanics: Shields have ModifiedMaxHitPoints (with bonuses)
        /// and current HitPoints. We restore to the modified max for full effectiveness.
        /// </para>
        /// </remarks>
        private static void ApplyUnlimitedShieldDurability(Agent agent)
        {
            // Early exit if settings are null
            if (Settings is null || TargetSettings is null)
            {
                return;
            }

            // Early exit if settings are null
            CheatSettings? settings = Settings;
            CheatTargetSettings? targetSettings = TargetSettings;
            if (settings is null || targetSettings is null)
            {
                return;
            }

            // Early returns for disabled cheat or non-player agent
            if (!settings.UnlimitedShieldDurability || !targetSettings.ApplyToPlayer)
            {
                return;
            }

            if (agent?.IsPlayerControlled is not true)
            {
                return;
            }

            // Iterate through weapon slots to find and repair shields
            for (EquipmentIndex i = EquipmentIndex.WeaponItemBeginSlot; i < EquipmentIndex.NumAllWeaponSlots; i++)
            {
                MissionWeapon equipment = agent.Equipment[i];

                // Check if this slot contains a shield
                if (equipment.CurrentUsageItem?.IsShield is true)
                {
                    short maxHitPoints = equipment.ModifiedMaxHitPoints;

                    // Only repair if damaged (optimization: avoid unnecessary API calls)
                    if (equipment.HitPoints < maxHitPoints)
                    {
                        agent.ChangeWeaponHitPoints(i, maxHitPoints);
                    }
                }
            }
        }

        #endregion

        #region Unlimited Ammunition

        /// <summary>
        /// Unlimited Ammo is now handled PURELY by AmmoConsumptionPatch (Harmony).
        /// This method is kept as a placeholder for future tick-based fallback if needed.
        /// </summary>
        /// <remarks>
        /// <para>
        /// CRITICAL FIX: Removed all calls to SetWeaponAmountInSlot() to prevent character model corruption.
        /// </para>
        /// <para>
        /// The previous implementation called SetWeaponAmountInSlot() every frame when ammo decreased,
        /// which internally triggers UpdateAgentProperties(). This recalculates the entire character
        /// visual model (mesh, skeleton, animations) 60 times per second during combat.
        /// </para>
        /// <para>
        /// When exiting battle and opening menus (inventory, clan, encyclopedia), this created a
        /// conflict between:
        /// - Combat system model updates (from SetWeaponAmountInSlot)
        /// - Menu system model rendering
        /// </para>
        /// <para>
        /// Result: Character model appears distorted/broken in all menu screens.
        /// </para>
        /// <para>
        /// SOLUTION: Rely ONLY on AmmoConsumptionPatch which blocks ammo consumption at the source
        /// (Agent.SetWeaponAmountInSlot Prefix) without triggering model updates.
        /// </para>
        /// </remarks>
        private void ApplyUnlimitedAmmo()
        {
            // Early exit if settings are null
            CheatSettings? settings = Settings;
            CheatTargetSettings? targetSettings = TargetSettings;
            if (settings is null || targetSettings is null)
            {
                return;
            }

            // Log unlimited ammo activation once per mission
            if (settings.UnlimitedAmmo && targetSettings.ApplyToPlayer && !_unlimitedAmmoLogged)
            {
                _unlimitedAmmoLogged = true;
                string patchStatus = AmmoConsumptionPatch.IsPatchApplied
                    ? "Harmony patch ACTIVE (ammo consumption blocked at source)"
                    : "WARNING: Harmony patch NOT applied - unlimited ammo may not work!";
                ModLogger.Log($"[UnlimitedAmmo] {patchStatus}");

                if (!AmmoConsumptionPatch.IsPatchApplied)
                {
                    ModLogger.Error("[UnlimitedAmmo] AmmoConsumptionPatch failed to apply - check HarmonyManager logs!");
                }
            }

            // No tick-based restoration needed - AmmoConsumptionPatch handles everything
            // This method is kept as a placeholder for future enhancements if needed
        }

        #endregion

        #region Unlimited Health

        /// <summary>
        /// Maintains player agent at maximum health.
        /// WARNING: Does not prevent one-shot kills from damage exceeding HealthLimit.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Performance optimization: Only restores health if below maximum.
        /// This avoids unnecessary API calls when health is already full.
        /// </para>
        /// <para>
        /// Health vs HealthLimit: HealthLimit is the maximum possible health for an agent.
        /// We restore to HealthLimit to maintain full health at all times.
        /// </para>
        /// <para>
        /// LIMITATION: If damage exceeds HealthLimit (e.g., 120 damage to 100 HP),
        /// player will die instantly. Use Infinite Health cheat to prevent this.
        /// </para>
        /// </remarks>
        private static void ApplyUnlimitedHealth()
        {
            // Early exit if settings are null
            CheatSettings? settings = Settings;
            CheatTargetSettings? targetSettings = TargetSettings;
            if (settings is null || targetSettings is null)
            {
                return;
            }

            // Early returns for disabled cheat or missing agent
            if (!settings.UnlimitedHealth || !targetSettings.ApplyToPlayer)
            {
                return;
            }

            Agent? playerAgent = Mission.Current?.MainAgent;
            if (playerAgent?.IsActive() != true)
            {
                return;
            }

            // Only restore if health is below maximum (optimization: avoid unnecessary assignments)
            if (playerAgent.Health < playerAgent.HealthLimit)
            {
                float restoredFrom = playerAgent.Health;
                playerAgent.Health = playerAgent.HealthLimit;
                ModLogger.Debug($"Unlimited Health: Restored health from {restoredFrom:F1} to {playerAgent.HealthLimit:F1}");
            }
        }

        /// <summary>
        /// Adds +9999 HP to player at the start of battle.
        /// Prevents one-shot kills from high damage attacks.
        /// </summary>
        /// <remarks>
        /// <para>
        /// This cheat modifies HealthLimit (max HP) rather than just restoring health.
        /// The +9999 HP bonus makes it virtually impossible to die from any single attack.
        /// </para>
        /// <para>
        /// Applied once per agent using _infiniteHealthApplied dictionary.
        /// This prevents repeated application every frame.
        /// </para>
        /// <para>
        /// Difference from Unlimited Health:
        /// - Unlimited HP: Keeps health bar full, but can die from one-shot
        /// - Infinite Health: Adds +9999 HP, prevents one-shot kills
        /// </para>
        /// </remarks>
        private void ApplyInfiniteHealth()
        {
            // Early exit if settings are null
            CheatSettings? settings = Settings;
            CheatTargetSettings? targetSettings = TargetSettings;
            if (settings is null || targetSettings is null)
            {
                return;
            }

            // Early returns for disabled cheat
            if (!settings.InfiniteHealth || !targetSettings.ApplyToPlayer)
            {
                return;
            }

            Agent? playerAgent = Mission.Current?.MainAgent;
            if (playerAgent?.IsActive() != true)
            {
                return;
            }

            // Apply bonus to the agent
            ApplyInfiniteHealthToAgent(playerAgent);
        }

        /// <summary>
        /// Applies Infinite Health bonus to a specific agent.
        /// </summary>
        /// <param name="agent">The agent to apply the bonus to.</param>
        private void ApplyInfiniteHealthToAgent(Agent agent)
        {
            if (agent?.IsActive() != true)
            {
                return;
            }

            // CRITICAL: Ensure agent is fully initialized before modifying HealthLimit
            // The game modifies HealthLimit AFTER OnAgentBuild in SpawningBehaviorBase,
            // so we must wait until agent is fully ready. Check multiple conditions:
            if (agent.Character == null || agent.HealthLimit <= 0 || agent.BaseHealthLimit <= 0)
            {
                // Agent not fully ready yet, skip for now (will retry next frame)
                return;
            }

            // Additional safety check: Ensure agent has been in mission for at least a few frames
            // This gives the game time to complete all initialization
            if (!agent.IsActive() || agent.Index < 0)
            {
                return;
            }

            int agentIndex = agent.Index;

            // Check if already applied to this agent
            if (_infiniteHealthApplied.TryGetValue(agentIndex, out bool applied) && applied)
            {
                // Verify bonus is still there (in case HealthLimit was reset)
                float expectedMinHealth = agent.HealthLimit - GameConstants.InfiniteHealthBonus;
                if (agent.HealthLimit < expectedMinHealth + (GameConstants.InfiniteHealthBonus * 0.9f))
                {
                    // Bonus seems to have been reset, reapply it
                    ModLogger.Debug($"Infinite Health bonus was reset for agent {agentIndex}, reapplying...");
                    _infiniteHealthApplied[agentIndex] = false;
                }
                else
                {
                    // Bonus is still there, just restore health
                    if (agent.Health < agent.HealthLimit)
                    {
                        agent.Health = agent.HealthLimit;
                    }
                    return;
                }
            }

            // Store original HealthLimit before applying bonus (for verification)
            float originalHealthLimit = agent.HealthLimit;
            float originalBaseHealthLimit = agent.BaseHealthLimit;

            // Set HealthLimit to original base + bonus to ensure consistency
            if (originalHealthLimit > 0 && originalBaseHealthLimit > 0)
            {
                float newHealthLimit = originalBaseHealthLimit + GameConstants.InfiniteHealthBonus;
                agent.HealthLimit = newHealthLimit;
                agent.Health = newHealthLimit; // Fill to new max

                // Mark as applied to prevent repeated application
                _infiniteHealthApplied[agentIndex] = true;

                ModLogger.Debug($"Infinite Health applied to agent {agentIndex}: +{GameConstants.InfiniteHealthBonus} HP (original: {originalHealthLimit}, new limit: {agent.HealthLimit})");
            }
        }

        #endregion

        #region Unlimited Horse Health

        /// <summary>
        /// Maintains player's mount at maximum health.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Mount mechanics: Agents have a MountAgent property that references their horse.
        /// Mounts are separate agents with their own health pools.
        /// </para>
        /// <para>
        /// Performance: Only processes if player has a mount and mount health is low.
        /// Multiple early returns minimize overhead when not mounted.
        /// </para>
        /// </remarks>
        private static void ApplyUnlimitedHorseHealth()
        {
            // Early exit if settings are null
            CheatSettings? settings = Settings;
            CheatTargetSettings? targetSettings = TargetSettings;
            if (settings is null || targetSettings is null)
            {
                return;
            }

            // Early returns for disabled cheat or missing agent
            if (!settings.UnlimitedHorseHealth || !targetSettings.ApplyToPlayer)
            {
                return;
            }

            Agent? playerAgent = Mission.Current?.MainAgent;
            if (playerAgent?.IsActive() != true || !playerAgent.HasMount)
            {
                return;
            }

            Agent? mount = playerAgent.MountAgent;
            if (mount?.IsActive() != true)
            {
                return;
            }

            // Only restore if health is below maximum (optimization: avoid unnecessary assignments)
            if (mount.Health < mount.HealthLimit)
            {
                mount.Health = mount.HealthLimit;
            }
        }

        #endregion

        #region One Hit Kills

        /// <summary>
        /// Reduces all enemy agents to minimum health for instant kills.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Implementation strategy: Set enemy health to 1 (not 0) each frame.
        /// This ensures any hit will kill them, but they don't die spontaneously.
        /// Setting to 0 would kill them immediately, which looks unnatural.
        /// </para>
        /// <para>
        /// The actual killing is also handled in <see cref="OnAgentHit"/> for reliability.
        /// This method serves as a backup to ensure enemies stay at low health.
        /// </para>
        /// <para>
        /// Performance: Only processes human enemies that are active and alive.
        /// Early continues for non-enemy, non-human, or already-dead agents.
        /// </para>
        /// </remarks>
        private static void ApplyOneHitKills()
        {
            // Early exit if settings are null
            CheatSettings? settings = Settings;
            if (settings is null)
            {
                return;
            }

            // Early return if cheat disabled
            if (!settings.OneHitKills)
            {
                return;
            }

            Agent? mainAgent = Mission.Current?.MainAgent;
            if (mainAgent is null)
            {
                return;
            }

            // Process all active enemy agents
            // Using foreach instead of LINQ for better performance (runs every frame)
            if (Mission.Current?.Agents == null)
            {
                return;
            }

            foreach (Agent agent in Mission.Current.Agents)
            {
                // Skip null, inactive, or non-human agents
                if (agent?.IsActive() != true || !agent.IsHuman)
                {
                    continue;
                }

                // Only process enemies
                if (!agent.IsEnemyOf(mainAgent))
                {
                    continue;
                }

                // Set health to minimum threshold (low enough for instant kill, but not spontaneous death)
                // If health somehow exceeded threshold, bring it back down
                if (agent.Health > GameConstants.OneHitKillHealthThreshold)
                {
                    float oldHealth = agent.Health;
                    agent.Health = GameConstants.OneHitKillHealthThreshold;
                    ModLogger.Debug($"One-Hit Kills: Set enemy agent {agent.Index} health from {oldHealth:F1} to {GameConstants.OneHitKillHealthThreshold:F1}");
                }
            }
        }

        #endregion
    }
}
