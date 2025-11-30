#nullable enable
using BannerWand.Constants;
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
        private static CheatSettings Settings => CheatSettings.Instance!;

        /// <summary>
        /// Gets the current target settings instance.
        /// </summary>
        private static CheatTargetSettings TargetSettings => CheatTargetSettings.Instance!;

        /// <summary>
        /// Tracks whether Infinite Health bonus has been applied in current mission.
        /// </summary>
        private bool _infiniteHealthApplied;

        #endregion

        #region Mission Events

        /// <summary>
        /// Called when the mission is ending.
        /// Resets one-time application flags for next battle.
        /// </summary>
        protected override void OnEndMission()
        {
            base.OnEndMission();

            // Reset application flag for next mission
            _infiniteHealthApplied = false;
        }


        /// <summary>
        /// Called every mission frame (approximately 60 times per second).
        /// Applies continuous combat cheats that need frame-by-frame updates.
        /// </summary>
        /// <param name="dt">Delta time since last frame in seconds.</param>
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
        /// - Unlimited ammunition (restore ammo to 999 for ranged weapons)
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

                // Apply Infinite Health bonus once when player spawns (uses flag to run only once)
                ApplyInfiniteHealth();

                // Apply continuous combat cheats
                ApplyUnlimitedHealth();
                ApplyUnlimitedHorseHealth();
                ApplyOneHitKills();
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

                // Handle One-Hit Kills - ensure enemies die from PLAYER's hits/shots
                // IMPORTANT: Only works when PLAYER is the attacker (not allies)
                if (Settings.OneHitKills)
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
            // Early returns for disabled cheat or non-player agent
            if (!Settings.UnlimitedShieldDurability || !TargetSettings.ApplyToPlayer)
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
        /// Maintains ammunition at 999 for all ranged weapons (bows, crossbows, throwables).
        /// </summary>
        /// <remarks>
        /// <para>
        /// This method iterates through all weapon slots and checks for ranged weapons that use ammunition.
        /// When ammo count falls below 999, it restores it back to 999.
        /// </para>
        /// <para>
        /// Supported weapon types:
        /// - Bows (arrows)
        /// - Crossbows (bolts)
        /// - Throwing weapons (javelins, throwing axes, throwing knives)
        /// </para>
        /// <para>
        /// Performance: Only restores ammo if below target (999) to avoid unnecessary API calls.
        /// Runs every frame (~60 FPS) but optimized with early returns.
        /// </para>
        /// </remarks>
        private static void ApplyUnlimitedAmmo()
        {
            // Early returns for disabled cheat or missing agent
            if (!Settings.UnlimitedAmmo || !TargetSettings.ApplyToPlayer)
            {
                return;
            }

            Agent? playerAgent = Mission.Current?.MainAgent;
            if (playerAgent?.IsActive() != true)
            {
                return;
            }

            // Iterate through all weapon slots to find ranged weapons
            for (EquipmentIndex i = EquipmentIndex.WeaponItemBeginSlot; i < EquipmentIndex.NumAllWeaponSlots; i++)
            {
                MissionWeapon weapon = playerAgent.Equipment[i];

                // Skip empty slots or weapons without ammo
                if (weapon.IsEmpty || weapon.CurrentUsageItem == null)
                {
                    continue;
                }

                // Check if weapon uses ammunition (bows, crossbows, throwables)
                // Bannerlord tracks ammo with MaxDataValue (max ammo) and DataValue (current ammo)
                if (weapon.ModifiedMaxAmount > 0) // Weapon has ammo capacity
                {
                    short currentAmmo = weapon.Amount;

                    // Only restore if below target (optimization: avoid unnecessary API calls)
                    if (currentAmmo < GameConstants.UnlimitedAmmoTarget)
                    {
                        // SetAmountOfSlot (4th parameter) sets the ammunition count
                        // We set it to 999 which is effectively unlimited for most battles
                        playerAgent.Equipment[i] = new MissionWeapon(
                            weapon.Item,
                            weapon.ItemModifier,
                            weapon.Banner,
                            GameConstants.UnlimitedAmmoTarget  // Set ammo to 999
                        );

                        // Log when ammo is restored (only once when it drops, not every frame)
                        ModLogger.Debug($"Unlimited Ammo: Restored {weapon.Item.Name} to {GameConstants.UnlimitedAmmoTarget}");
                    }
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
            // Early returns for disabled cheat or missing agent
            if (!Settings.UnlimitedHealth || !TargetSettings.ApplyToPlayer)
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
        /// Applied once per mission using _infiniteHealthApplied flag.
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
            // Early returns for disabled cheat or already applied
            if (!Settings.InfiniteHealth || !TargetSettings.ApplyToPlayer || _infiniteHealthApplied)
            {
                return;
            }

            Agent? playerAgent = Mission.Current?.MainAgent;
            if (playerAgent?.IsActive() != true)
            {
                return;
            }

            // Add bonus to max HP (HealthLimit)
            // This makes the player effectively unkillable by normal damage
            playerAgent.HealthLimit += GameConstants.InfiniteHealthBonus;
            playerAgent.Health = playerAgent.HealthLimit; // Fill to new max

            // Mark as applied to prevent repeated application
            _infiniteHealthApplied = true;

            ModLogger.Debug($"Infinite Health applied: +{GameConstants.InfiniteHealthBonus} HP (new limit: {playerAgent.HealthLimit})");
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
            // Early returns for disabled cheat or missing agent
            if (!Settings.UnlimitedHorseHealth || !TargetSettings.ApplyToPlayer)
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
            // Early return if cheat disabled
            if (!Settings.OneHitKills)
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
            foreach (Agent agent in Mission.Current!.Agents)
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
