#nullable enable
// System namespaces
using System;

// Third-party namespaces
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

// Project namespaces
using BannerWand.Behaviors.Handlers;
using BannerWand.Interfaces;
using BannerWand.Settings;
using BannerWand.Utils;

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
    /// <para>
    /// This class uses handler classes (<see cref="IHealthCheatHandler"/>, <see cref="IShieldCheatHandler"/>,
    /// <see cref="IOneHitKillHandler"/>, <see cref="IAmmoCheatHandler"/>, <see cref="INPCCheatHandler"/>)
    /// to encapsulate cheat logic, improving modularity and testability.
    /// </para>
    /// </remarks>
    public class CombatCheatBehavior : MissionLogic
    {
        #region Constants
        // Combat constants are now defined in GameConstants for consistency
        #endregion

        #region Handler Fields

        /// <summary>
        /// Handler for health-related cheats (unlimited health, infinite health, horse health).
        /// </summary>
        private readonly IHealthCheatHandler _healthCheatHandler;

        /// <summary>
        /// Handler for shield-related cheats (unlimited shield durability).
        /// </summary>
        private readonly IShieldCheatHandler _shieldCheatHandler;

        /// <summary>
        /// Handler for one-hit kill cheats.
        /// </summary>
        private readonly IOneHitKillHandler _oneHitKillHandler;

        /// <summary>
        /// Handler for ammo-related cheats (unlimited ammo).
        /// </summary>
        private readonly IAmmoCheatHandler _ammoCheatHandler;

        /// <summary>
        /// Handler for NPC-related cheats (unlimited HP, infinite HP, horse HP, shield HP, ammo for allied heroes).
        /// </summary>
        private readonly INPCCheatHandler _npcCheatHandler;

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

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="CombatCheatBehavior"/> class.
        /// </summary>
        /// <remarks>
        /// Initializes all cheat handlers with their default implementations.
        /// Handlers can be replaced with mock implementations for testing.
        /// </remarks>
        public CombatCheatBehavior()
        {
            _healthCheatHandler = new HealthCheatHandler();
            _shieldCheatHandler = new ShieldCheatHandler();
            _oneHitKillHandler = new OneHitKillHandler();
            _ammoCheatHandler = new AmmoCheatHandler();
            _npcCheatHandler = new NPCCheatHandler();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CombatCheatBehavior"/> class with custom handlers.
        /// </summary>
        /// <param name="healthCheatHandler">Handler for health-related cheats.</param>
        /// <param name="shieldCheatHandler">Handler for shield-related cheats.</param>
        /// <param name="oneHitKillHandler">Handler for one-hit kill cheats.</param>
        /// <param name="ammoCheatHandler">Handler for ammo-related cheats.</param>
        /// <param name="npcCheatHandler">Handler for NPC-related cheats.</param>
        /// <remarks>
        /// This constructor allows dependency injection for testing purposes.
        /// </remarks>
        public CombatCheatBehavior(
            IHealthCheatHandler healthCheatHandler,
            IShieldCheatHandler shieldCheatHandler,
            IOneHitKillHandler oneHitKillHandler,
            IAmmoCheatHandler ammoCheatHandler,
            INPCCheatHandler npcCheatHandler)
        {
            _healthCheatHandler = healthCheatHandler ?? throw new ArgumentNullException(nameof(healthCheatHandler));
            _shieldCheatHandler = shieldCheatHandler ?? throw new ArgumentNullException(nameof(shieldCheatHandler));
            _oneHitKillHandler = oneHitKillHandler ?? throw new ArgumentNullException(nameof(oneHitKillHandler));
            _ammoCheatHandler = ammoCheatHandler ?? throw new ArgumentNullException(nameof(ammoCheatHandler));
            _npcCheatHandler = npcCheatHandler ?? throw new ArgumentNullException(nameof(npcCheatHandler));
        }

        #endregion

        #region Mission Events

        /// <summary>
        /// Called when the mission is ending.
        /// Resets one-time application flags for next battle and removes AmmoConsumptionPatch.
        /// </summary>
        protected override void OnEndMission()
        {
            base.OnEndMission();

            // Remove AmmoConsumptionPatch to prevent breaking character models in menus
            Core.HarmonyManager.RemoveAmmoConsumptionPatch();

            // Reset handler tracking for next mission
            if (_healthCheatHandler is HealthCheatHandler healthHandler)
            {
                healthHandler.ClearInfiniteHealthTracking();
            }

            if (_npcCheatHandler is NPCCheatHandler npcHandler)
            {
                npcHandler.ClearInfiniteHealthTracking();
            }

            if (_ammoCheatHandler is AmmoCheatHandler ammoHandler)
            {
                ammoHandler.ResetTracking();
            }
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
                _healthCheatHandler.ApplyInfiniteHealth(agent);
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

                // Cache agents collection once per tick to avoid repeated property access
                MBReadOnlyList<Agent>? agents = Mission.Current.Agents;
                // OPTIMIZED: Early exit if no agents available
                if (agents == null || agents.Count == 0)
                {
                    return;
                }

                // Get player agent for health cheats
                Agent? playerAgent = Mission.Current.MainAgent;

                // Apply Infinite Health bonus once when player spawns (uses flag to run only once)
                // Also try to apply in OnMissionTick as fallback if OnAgentBuild didn't work
                if (playerAgent?.IsActive() == true)
                {
                    _healthCheatHandler.ApplyInfiniteHealth(playerAgent);
                    _healthCheatHandler.ApplyUnlimitedHealth(playerAgent);
                    _healthCheatHandler.ApplyUnlimitedHorseHealth(playerAgent);
                    _ammoCheatHandler.ApplyUnlimitedAmmo(playerAgent);
                }

                // Apply one-hit kills to enemies
                _oneHitKillHandler.ApplyOneHitKills(agents);

                // Apply NPC cheats (pass cached agents collection)
                _npcCheatHandler.ApplyNPCUnlimitedHP(agents);
                _npcCheatHandler.ApplyNPCInfiniteHP(agents);
                _npcCheatHandler.ApplyNPCUnlimitedHorseHP(agents);
                _npcCheatHandler.ApplyNPCUnlimitedShieldHP(agents);
                _npcCheatHandler.ApplyNPCUnlimitedAmmo(agents);

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

                // Handle health restoration for player
                if (affectedAgent?.IsPlayerControlled == true)
                {
                    _healthCheatHandler.OnAgentHit(affectedAgent, affectorAgent, blow);
                }

                // Handle one-hit kills (only if agent is not null)
                if (affectedAgent is not null)
                {
                    _oneHitKillHandler.OnAgentHit(affectedAgent, affectorAgent, blow);
                }

                // Handle shield durability restoration for player
                if (affectedAgent is not null)
                {
                    _shieldCheatHandler.OnAgentHit(affectedAgent, affectorAgent);
                }

                // Restore allied NPC hero shield durability if enabled
                Agent? mainAgent = Mission.Current?.MainAgent;
                if (mainAgent is not null && affectedAgent is not null && IsAlliedHeroOnPlayerSide(affectedAgent, mainAgent))
                {
                    if (settings.NPCUnlimitedShieldHP)
                    {
                        // Iterate through weapon slots to find and repair shields
                        for (EquipmentIndex i = EquipmentIndex.WeaponItemBeginSlot; i < EquipmentIndex.NumAllWeaponSlots; i++)
                        {
                            MissionWeapon equipment = affectedAgent.Equipment[i];

                            // Check if this slot contains a shield
                            if (equipment.CurrentUsageItem?.IsShield is true)
                            {
                                short maxHitPoints = equipment.ModifiedMaxHitPoints;

                                // Only repair if damaged (optimization: avoid unnecessary API calls)
                                if (equipment.HitPoints < maxHitPoints)
                                {
                                    affectedAgent.ChangeWeaponHitPoints(i, maxHitPoints);
                                }
                            }
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CombatCheatBehavior] Error in OnAgentHit: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Checks if an agent is an allied hero fighting on player's side in combat.
        /// Used to filter NPC cheats to only apply to allied heroes, not regular soldiers or enemies.
        /// </summary>
        /// <param name="agent">The <see cref="Agent"/> to check. Must be non-null, active, human, and a hero.</param>
        /// <param name="mainAgent">The player's main <see cref="Agent"/>. Used to determine alliance status.</param>
        /// <returns>
        /// <c>true</c> if the agent is an allied hero on player's side; otherwise, <c>false</c>.
        /// Returns <c>false</c> if <paramref name="agent"/> or <paramref name="mainAgent"/> is <c>null</c>,
        /// if the agent is not active, not human, not a hero, or is player-controlled or an enemy.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method performs the following checks in order:
        /// 1. Null checks for both agents
        /// 2. Agent must be active and human
        /// 3. Agent must be a hero (<see cref="Character.IsHero"/>)
        /// 4. Agent must not be player-controlled
        /// 5. Agent must not be an enemy of the main agent (<see cref="Agent.IsEnemyOf(Agent)"/>)
        /// </para>
        /// <para>
        /// This filtering ensures that NPC cheats (Unlimited HP, Infinite HP, Unlimited Horse HP,
        /// Unlimited Shield HP, Unlimited Ammo) only apply to allied heroes fighting alongside the player,
        /// not to regular soldiers, enemies, or the player themselves.
        /// </para>
        /// </remarks>
        private static bool IsAlliedHeroOnPlayerSide(Agent agent, Agent mainAgent)
        {
            if (agent is null || mainAgent is null)
            {
                return false;
            }

            // Must be active and human
            if (!agent.IsActive() || !agent.IsHuman)
            {
                return false;
            }

            // Must be a hero, not a regular soldier
            if (agent.Character?.IsHero != true)
            {
                return false;
            }

            // Skip player
            if (agent.IsPlayerControlled)
            {
                return false;
            }

            // Must be friendly (on player's side in combat)
            return !agent.IsEnemyOf(mainAgent);
        }

        #endregion
    }
}
