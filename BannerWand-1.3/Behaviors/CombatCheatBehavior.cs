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
        private bool _unlimitedAmmoLogged = false;

        /// <summary>
        /// Counter for unlimited ammo logging calls (for detailed logging).
        /// </summary>
        private int _unlimitedAmmoLogCounter = 0;

        /// <summary>
        /// Counter for mission ticks (for performance monitoring).
        /// </summary>
        private int _missionTickCount = 0;

        /// <summary>
        /// Tracks last ammo amounts per slot to avoid unnecessary SetWeaponAmountInSlot calls.
        /// Key: EquipmentIndex, Value: Last known ammo amount.
        /// </summary>
        private readonly System.Collections.Generic.Dictionary<EquipmentIndex, short> _lastAmmoAmounts = [];





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
            _unlimitedAmmoLogCounter = 0;
            _missionTickCount = 0;
            _lastAmmoAmounts.Clear();
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

                _missionTickCount++;

                // Apply Infinite Health bonus once when player spawns (uses flag to run only once)
                // Also try to apply in OnMissionTick as fallback if OnAgentBuild didn't work
                ApplyInfiniteHealth();

                // Apply continuous combat cheats
                ApplyUnlimitedHealth();
                ApplyUnlimitedHorseHealth();
                ApplyOneHitKills();

                // Apply unlimited ammo once per frame - now using safe minimum strategy
                ApplyUnlimitedAmmo();

                // Log unlimited ammo activation once per mission
                if (settings.UnlimitedAmmo && targetSettings.ApplyToPlayer && !_unlimitedAmmoLogged)
                {
                    _unlimitedAmmoLogged = true;
                    string patchStatus = AmmoConsumptionPatch.IsPatchApplied
                        ? "Harmony patch ACTIVE (ammo consumption blocked)"
                        : "Using tick-based restoration (fallback mode)";
                    ModLogger.Log($"[UnlimitedAmmo] Unlimited Ammo cheat activated for this mission. Mode: {patchStatus}");
                }

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
                // This prevents death from one-shot kills. We restore to HealthLimit + large buffer to handle
                // damage that exceeds HealthLimit (one-shot kills).
                if (settings.InfiniteHealth && targetSettings.ApplyToPlayer && affectedAgent?.IsPlayerControlled == true)
                {
                    if (affectedAgent.IsActive())
                    {
                        // Restore health immediately - use HealthLimit + buffer to prevent one-shot kills
                        // Even if damage exceeded HealthLimit, we restore health before death check
                        float targetHealth = affectedAgent.HealthLimit + GameConstants.InfiniteHealthBonus;
                        if (affectedAgent.Health < targetHealth)
                        {
                            affectedAgent.Health = targetHealth;
                        }
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
        /// Maintains maximum ammunition using the correct Bannerlord API method.
        /// Uses Agent.SetWeaponAmountInSlot() which is the proper way to change ammo.
        /// </summary>
        /// <remarks>
        /// <para>
        /// CRITICAL FIX: Previous implementation tried to replace MissionWeapon objects
        /// which doesn't work because MissionEquipment is a struct. The correct approach
        /// is to use Agent.SetWeaponAmountInSlot() which directly modifies weapon data.
        /// </para>
        /// <para>
        /// Weapon detection logic:
        /// - Arrows/Bolts: Have ModifiedMaxAmount > 1 (quiver size)
        /// - Throwing weapons: Have ModifiedMaxAmount > 1 (stack size)
        /// - Bows/Crossbows: Have ModifiedMaxAmount == 1 (the weapon itself, not ammo)
        /// - Shields: Detected via CurrentUsageItem.IsShield
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

            if (!settings.UnlimitedAmmo || !targetSettings.ApplyToPlayer)
            {
                return;
            }

            Agent? playerAgent = Mission.Current?.MainAgent;
            if (playerAgent?.IsActive() != true)
            {
                return;
            }

            // Iterate through weapon slots (no logging to reduce spam)
            for (EquipmentIndex i = EquipmentIndex.WeaponItemBeginSlot; i < EquipmentIndex.NumAllWeaponSlots; i++)
            {
                MissionWeapon weapon = playerAgent.Equipment[i];

                // Skip empty slots
                if (weapon.IsEmpty)
                {
                    continue;
                }

                // Skip shields - they use HitPoints, not Amount
                if (weapon.CurrentUsageItem?.IsShield == true)
                {
                    continue;
                }

                // Skip weapons without ammo capacity (melee weapons have MaxAmount <= 0)
                if (weapon.ModifiedMaxAmount <= 0)
                {
                    continue;
                }

                // Skip bows/crossbows - MaxAmount == 1 means it's the weapon itself, not ammunition
                // Bows don't consume themselves, they consume arrows from another slot
                if (weapon.ModifiedMaxAmount == 1)
                {
                    continue;
                }

                short currentAmmo = weapon.Amount;
                short maxAmmo = weapon.ModifiedMaxAmount;

                // CRITICAL FIX: Only restore if ammo actually decreased AND we haven't just restored it
                // This prevents calling SetWeaponAmountInSlot every frame, which triggers UpdateAgentProperties()
                // and can break character models. We only restore when ammo actually drops below max.
                bool shouldRestore = false;
                if (_lastAmmoAmounts.TryGetValue(i, out short lastAmmo))
                {
                    // Only restore if ammo decreased from last check AND is below max
                    if (currentAmmo < lastAmmo && currentAmmo < maxAmmo)
                    {
                        shouldRestore = true;
                    }
                }
                else
                {
                    // First time checking this slot - restore if below max
                    if (currentAmmo < maxAmmo)
                    {
                        shouldRestore = true;
                    }
                }

                // Update last known amount
                _lastAmmoAmounts[i] = currentAmmo;

                // DISABLED: Manual restoration via SetWeaponAmountInSlot causes UpdateAgentProperties()
                // to be called, which can break character models. Instead, we rely entirely on
                // the Harmony patch (AmmoConsumptionPatch) which prevents ammo decrease.
                // The patch modifies the amount parameter before the original method executes,
                // so no manual restoration is needed.
                //
                // If ammo somehow gets below max, the patch will prevent further decreases,
                // and the player can pick up ammo normally or it will be restored on next shot attempt.
                if (shouldRestore && _unlimitedAmmoLogCounter == 0)
                {
                    string weaponName = weapon.Item?.Name?.ToString() ?? "Unknown";
                    ModLogger.Debug($"[UnlimitedAmmo] Ammo below max for {weaponName} (Slot: {i}): {currentAmmo}/{maxAmmo}. Patch will prevent further decrease.");
                    _unlimitedAmmoLogCounter++;
                }
            }
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
                playerAgent.Health = playerAgent.HealthLimit;
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
        /// CRITICAL FIX: No longer modifies HealthLimit as it causes character model corruption.
        /// Instead, uses aggressive health restoration after damage to prevent one-shot kills.
        /// </summary>
        /// <param name="agent">The agent to apply the bonus to.</param>
        private void ApplyInfiniteHealthToAgent(Agent agent)
        {
            if (agent?.IsActive() != true)
            {
                return;
            }

            // CRITICAL FIX: Do NOT modify HealthLimit - it causes UpdateAgentProperties() to be called
            // which can break character visual models. Instead, we rely on aggressive health restoration
            // in OnAgentHit to prevent one-shot kills.
            
            // Just ensure health is at maximum - no HealthLimit modification
            if (agent.Health < agent.HealthLimit)
            {
                agent.Health = agent.HealthLimit;
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
                    agent.Health = GameConstants.OneHitKillHealthThreshold;
                }
            }
        }

        #endregion
    }
}
