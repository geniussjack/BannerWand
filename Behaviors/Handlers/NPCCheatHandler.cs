#nullable enable
// System namespaces
// Project namespaces
using BannerWand.Constants;
using BannerWand.Settings;
using BannerWand.Utils;
using System.Collections.Generic;
// Third-party namespaces
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BannerWand.Behaviors.Handlers
{
    /// <summary>
    /// Handles NPC-related cheats in combat.
    /// </summary>
    public class NPCCheatHandler
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
        /// Checks if an agent is an allied hero, or - when
        /// <see cref="CheatSettings.NPCApplyToRegularTroops"/> is enabled - any allied human agent,
        /// fighting on player's side in combat.
        /// </summary>
        /// <param name="agent">The agent to check.</param>
        /// <param name="mainAgent">The player's main agent.</param>
        /// <returns><c>true</c> if the agent qualifies as an allied NPC target; otherwise, <c>false</c>.</returns>
        private static bool IsAlliedAgentOnPlayerSide(Agent agent, Agent mainAgent)
        {
            if (agent?.IsActive() != true || !agent.IsHuman || agent.IsPlayerControlled)
            {
                return false;
            }

            bool isHero = agent.Character?.IsHero == true;
            return (isHero || (Settings?.NPCApplyToRegularTroops) == true) && !agent.IsEnemyOf(mainAgent);
        }

        /// <summary>
        /// Applies unlimited HP to allied NPC heroes.
        /// </summary>
        /// <param name="agents">Collection of all agents in the mission.</param>
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

            // OPTIMIZED: Use for loop instead of foreach for better performance
            for (int i = 0; i < agents.Count; i++)
            {
                Agent agent = agents[i];
                if (!IsAlliedAgentOnPlayerSide(agent, mainAgent))
                {
                    continue;
                }

                if (agent.Health < agent.HealthLimit)
                {
                    agent.Health = agent.HealthLimit;
                }
            }
        }

        /// <summary>
        /// Applies infinite HP (+9999) to allied NPC heroes.
        /// </summary>
        /// <param name="agents">Collection of all agents in the mission.</param>
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

            // OPTIMIZED: Use for loop instead of foreach for better performance
            for (int i = 0; i < agents.Count; i++)
            {
                Agent agent = agents[i];
                if (!IsAlliedAgentOnPlayerSide(agent, mainAgent))
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

        /// <summary>
        /// Applies unlimited horse HP to allied NPC heroes.
        /// </summary>
        /// <param name="agents">Collection of all agents in the mission.</param>
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

            // OPTIMIZED: Use for loop instead of foreach for better performance
            for (int i = 0; i < agents.Count; i++)
            {
                Agent agent = agents[i];
                if (!IsAlliedAgentOnPlayerSide(agent, mainAgent))
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

        /// <summary>
        /// Applies unlimited shield HP to allied NPC heroes.
        /// </summary>
        /// <param name="agents">Collection of all agents in the mission.</param>
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

            // OPTIMIZED: Use for loop instead of foreach for better performance
            for (int i = 0; i < agents.Count; i++)
            {
                Agent agent = agents[i];
                if (!IsAlliedAgentOnPlayerSide(agent, mainAgent))
                {
                    continue;
                }

                for (EquipmentIndex j = EquipmentIndex.WeaponItemBeginSlot; j < EquipmentIndex.NumAllWeaponSlots; j++)
                {
                    MissionWeapon equipment = agent.Equipment[j];

                    if (equipment.CurrentUsageItem?.IsShield is true)
                    {
                        short maxHitPoints = equipment.ModifiedMaxHitPoints;

                        if (equipment.HitPoints < maxHitPoints)
                        {
                            agent.ChangeWeaponHitPoints(j, maxHitPoints);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Applies unlimited ammo to allied NPC heroes.
        /// </summary>
        /// <param name="agents">Collection of all agents in the mission.</param>
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

            // OPTIMIZED: Use for loop instead of foreach for better performance
            for (int i = 0; i < agents.Count; i++)
            {
                Agent agent = agents[i];
                if (!IsAlliedAgentOnPlayerSide(agent, mainAgent))
                {
                    continue;
                }

                for (EquipmentIndex j = EquipmentIndex.WeaponItemBeginSlot; j < EquipmentIndex.NumAllWeaponSlots; j++)
                {
                    MissionWeapon weapon = agent.Equipment[j];
                    if (weapon.IsEmpty || weapon.CurrentUsageItem == null || weapon.ModifiedMaxAmount <= 0)
                    {
                        continue;
                    }

                    short maxAmmo = weapon.ModifiedMaxAmount;
                    short currentAmmo = weapon.Amount;

                    if (currentAmmo < maxAmmo)
                    {
                        agent.SetWeaponAmountInSlot(j, maxAmmo, true);
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
                    ModLogger.DebugThrottled($"Infinite Health bonus was reset for agent {agentIndex}, reapplying...");
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

                ModLogger.DebugThrottled($"Infinite Health applied to NPC agent {agentIndex}: +{GameConstants.InfiniteHealthBonus} HP (original: {originalHealthLimit}, new limit: {agent.HealthLimit})");
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
