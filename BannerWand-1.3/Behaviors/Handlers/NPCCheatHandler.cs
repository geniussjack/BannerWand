#nullable enable
// System namespaces
using System.Collections.Generic;

// Third-party namespaces
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

// Project namespaces
using BannerWand.Constants;
using BannerWand.Interfaces;
using BannerWand.Settings;
using BannerWand.Utils;

namespace BannerWand.Behaviors.Handlers
{
    /// <summary>
    /// Handles NPC-related cheats in combat.
    /// </summary>
    /// <remarks>
    /// This class encapsulates NPC cheat logic, making it easier to test
    /// and maintain. It implements <see cref="INPCCheatHandler"/> for dependency injection.
    /// </remarks>
    public class NPCCheatHandler : INPCCheatHandler
    {
        /// <summary>
        /// Tracks whether Infinite Health bonus has been applied to NPC agents.
        /// Key: Agent index, Value: Whether bonus was applied.
        /// </summary>
        private readonly Dictionary<int, bool> _infiniteHealthApplied = [];

        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings? Settings => CheatSettings.Instance;

        /// <summary>
        /// Checks if an agent is an allied hero fighting on player's side in combat.
        /// </summary>
        /// <param name="agent">The agent to check.</param>
        /// <param name="mainAgent">The player's main agent.</param>
        /// <returns><c>true</c> if the agent is an allied hero on player's side; otherwise, <c>false</c>.</returns>
        private static bool IsAlliedHeroOnPlayerSide(Agent agent, Agent mainAgent)
        {
            return agent?.IsActive() == true && agent.IsHuman && agent.Character?.IsHero == true && !agent.IsPlayerControlled && !agent.IsEnemyOf(mainAgent);
        }

        /// <inheritdoc />
        public void ApplyNPCUnlimitedHP(MBReadOnlyList<Agent> agents)
        {
            if (Settings?.NPCUnlimitedHP != true)
            {
                return;
            }

            Agent? mainAgent = Mission.Current?.MainAgent;
            if (mainAgent == null)
            {
                return;
            }

            foreach (Agent agent in agents)
            {
                if (!IsAlliedHeroOnPlayerSide(agent, mainAgent))
                {
                    continue;
                }

                if (agent.Health < agent.HealthLimit)
                {
                    agent.Health = agent.HealthLimit;
                }
            }
        }

        /// <inheritdoc />
        public void ApplyNPCInfiniteHP(MBReadOnlyList<Agent> agents)
        {
            if (Settings?.NPCInfiniteHP != true)
            {
                return;
            }

            Agent? mainAgent = Mission.Current?.MainAgent;
            if (mainAgent == null)
            {
                return;
            }

            foreach (Agent agent in agents)
            {
                if (!IsAlliedHeroOnPlayerSide(agent, mainAgent))
                {
                    continue;
                }

                int agentIndex = agent.Index;
                if (!_infiniteHealthApplied.ContainsKey(agentIndex))
                {
                    ApplyInfiniteHealthToAgent(agent);
                    _infiniteHealthApplied[agentIndex] = true;
                }
            }
        }

        /// <inheritdoc />
        public void ApplyNPCUnlimitedHorseHP(MBReadOnlyList<Agent> agents)
        {
            if (Settings?.NPCUnlimitedHorseHP != true)
            {
                return;
            }

            Agent? mainAgent = Mission.Current?.MainAgent;
            if (mainAgent == null)
            {
                return;
            }

            foreach (Agent agent in agents)
            {
                if (!IsAlliedHeroOnPlayerSide(agent, mainAgent))
                {
                    continue;
                }

                Agent? mount = agent.MountAgent;
                if (mount?.IsActive() == true && mount.Health < mount.HealthLimit)
                {
                    mount.Health = mount.HealthLimit;
                }
            }
        }

        /// <inheritdoc />
        public void ApplyNPCUnlimitedShieldHP(MBReadOnlyList<Agent> agents)
        {
            if (Settings?.NPCUnlimitedShieldHP != true)
            {
                return;
            }

            Agent? mainAgent = Mission.Current?.MainAgent;
            if (mainAgent == null)
            {
                return;
            }

            foreach (Agent agent in agents)
            {
                if (!IsAlliedHeroOnPlayerSide(agent, mainAgent))
                {
                    continue;
                }

                for (EquipmentIndex i = EquipmentIndex.WeaponItemBeginSlot; i < EquipmentIndex.NumAllWeaponSlots; i++)
                {
                    MissionWeapon equipment = agent.Equipment[i];

                    if (equipment.CurrentUsageItem?.IsShield is true)
                    {
                        short maxHitPoints = equipment.ModifiedMaxHitPoints;

                        if (equipment.HitPoints < maxHitPoints)
                        {
                            agent.ChangeWeaponHitPoints(i, maxHitPoints);
                        }
                    }
                }
            }
        }

        /// <inheritdoc />
        public void ApplyNPCUnlimitedAmmo(MBReadOnlyList<Agent> agents)
        {
            if (Settings?.NPCUnlimitedAmmo != true)
            {
                return;
            }

            Agent? mainAgent = Mission.Current?.MainAgent;
            if (mainAgent == null)
            {
                return;
            }

            foreach (Agent agent in agents)
            {
                if (!IsAlliedHeroOnPlayerSide(agent, mainAgent))
                {
                    continue;
                }

                for (EquipmentIndex i = EquipmentIndex.WeaponItemBeginSlot; i < EquipmentIndex.NumAllWeaponSlots; i++)
                {
                    MissionWeapon weapon = agent.Equipment[i];
                    if (weapon.IsEmpty || weapon.CurrentUsageItem == null || weapon.ModifiedMaxAmount <= 0)
                    {
                        continue;
                    }

                    short maxAmmo = weapon.ModifiedMaxAmount;
                    short currentAmmo = weapon.Amount;

                    if (currentAmmo < maxAmmo)
                    {
                        agent.SetWeaponAmountInSlot(i, maxAmmo, true);
                    }
                }
            }
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

            if (agent.Character == null || agent.HealthLimit <= 0 || agent.BaseHealthLimit <= 0)
            {
                return;
            }

            if (!agent.IsActive() || agent.Index < 0)
            {
                return;
            }

            int agentIndex = agent.Index;

            if (_infiniteHealthApplied.TryGetValue(agentIndex, out bool applied) && applied)
            {
                float expectedMinHealth = agent.HealthLimit - GameConstants.InfiniteHealthBonus;
                if (agent.HealthLimit < expectedMinHealth + (GameConstants.InfiniteHealthBonus * GameConstants.InfiniteHealthBonusThresholdMultiplier))
                {
                    ModLogger.Debug($"Infinite Health bonus was reset for agent {agentIndex}, reapplying...");
                    _infiniteHealthApplied[agentIndex] = false;
                }
                else
                {
                    if (agent.Health < agent.HealthLimit)
                    {
                        agent.Health = agent.HealthLimit;
                    }
                    return;
                }
            }

            float originalHealthLimit = agent.HealthLimit;
            float originalBaseHealthLimit = agent.BaseHealthLimit;

            if (originalHealthLimit > 0 && originalBaseHealthLimit > 0)
            {
                float newHealthLimit = originalBaseHealthLimit + GameConstants.InfiniteHealthBonus;
                agent.HealthLimit = newHealthLimit;
                agent.Health = newHealthLimit;

                _infiniteHealthApplied[agentIndex] = true;

                ModLogger.Debug($"Infinite Health applied to NPC agent {agentIndex}: +{GameConstants.InfiniteHealthBonus} HP (original: {originalHealthLimit}, new limit: {agent.HealthLimit})");
            }
        }

        /// <summary>
        /// Clears the infinite health applied tracking dictionary.
        /// </summary>
        public void ClearInfiniteHealthTracking()
        {
            _infiniteHealthApplied.Clear();
        }
    }
}
