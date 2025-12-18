#nullable enable
using BannerWandRetro.Constants;
using BannerWandRetro.Interfaces;
using BannerWandRetro.Settings;
using BannerWandRetro.Utils;
using System.Collections.Generic;
using TaleWorlds.MountAndBlade;

namespace BannerWandRetro.Behaviors.Handlers
{
    /// <summary>
    /// Handles health-related cheats for the player in combat.
    /// </summary>
    /// <remarks>
    /// This class encapsulates health cheat logic, making it easier to test
    /// and maintain. It implements <see cref="IHealthCheatHandler"/> for dependency injection.
    /// </remarks>
    public class HealthCheatHandler : IHealthCheatHandler
    {
        private readonly Dictionary<int, bool> _infiniteHealthApplied = [];

        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings? Settings => CheatSettings.Instance;

        /// <summary>
        /// Gets the current target settings instance.
        /// </summary>
        private static CheatTargetSettings? TargetSettings => CheatTargetSettings.Instance;

        /// <inheritdoc />
        public void ApplyUnlimitedHealth(Agent agent)
        {
            CheatSettings? settings = Settings;
            CheatTargetSettings? targetSettings = TargetSettings;
            if (settings is null || targetSettings is null)
            {
                return;
            }

            if (!settings.UnlimitedHealth || !targetSettings.ApplyToPlayer)
            {
                return;
            }

            if (agent?.IsActive() != true)
            {
                return;
            }

            if (agent.Health < agent.HealthLimit)
            {
                float restoredFrom = agent.Health;
                agent.Health = agent.HealthLimit;
                ModLogger.Debug($"Unlimited Health: Restored health from {restoredFrom:F1} to {agent.HealthLimit:F1}");
            }
        }

        /// <inheritdoc />
        public void ApplyInfiniteHealth(Agent agent)
        {
            CheatSettings? settings = Settings;
            CheatTargetSettings? targetSettings = TargetSettings;
            if (settings is null || targetSettings is null)
            {
                return;
            }

            if (!settings.InfiniteHealth || !targetSettings.ApplyToPlayer)
            {
                return;
            }

            if (agent?.IsActive() != true)
            {
                return;
            }

            ApplyInfiniteHealthToAgent(agent);
        }

        /// <inheritdoc />
        public void ApplyUnlimitedHorseHealth(Agent agent)
        {
            CheatSettings? settings = Settings;
            CheatTargetSettings? targetSettings = TargetSettings;
            if (settings is null || targetSettings is null)
            {
                return;
            }

            if (!settings.UnlimitedHorseHealth || !targetSettings.ApplyToPlayer)
            {
                return;
            }

            if (agent?.IsActive() != true || !agent.HasMount)
            {
                return;
            }

            Agent? mount = agent.MountAgent;
            if (mount?.IsActive() != true)
            {
                return;
            }

            if (mount.Health < mount.HealthLimit)
            {
                mount.Health = mount.HealthLimit;
            }
        }

        /// <inheritdoc />
        public void OnAgentHit(Agent affectedAgent, Agent? affectorAgent, in Blow blow)
        {
            CheatSettings? settings = Settings;
            CheatTargetSettings? targetSettings = TargetSettings;
            if (settings is null || targetSettings is null)
            {
                return;
            }

            // Restore health immediately after taking damage if Infinite Health is enabled
            if (settings.InfiniteHealth && targetSettings.ApplyToPlayer && affectedAgent?.IsPlayerControlled == true)
            {
                ApplyInfiniteHealthToAgent(affectedAgent);

                if (affectedAgent.IsActive() && affectedAgent.Health < affectedAgent.HealthLimit)
                {
                    affectedAgent.Health = affectedAgent.HealthLimit;
                }
            }

            // Also restore health if Unlimited Health is enabled
            if (settings.UnlimitedHealth && targetSettings.ApplyToPlayer && affectedAgent?.IsPlayerControlled == true)
            {
                if (affectedAgent.IsActive() && affectedAgent.Health < affectedAgent.HealthLimit)
                {
                    affectedAgent.Health = affectedAgent.HealthLimit;
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

            // Ensure agent is fully initialized before modifying HealthLimit
            if (agent.Character == null || agent.HealthLimit <= 0 || agent.BaseHealthLimit <= 0)
            {
                return;
            }

            if (!agent.IsActive() || agent.Index < 0)
            {
                return;
            }

            int agentIndex = agent.Index;

            // Check if already applied to this agent
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

                ModLogger.Debug($"Infinite Health applied to agent {agentIndex}: +{GameConstants.InfiniteHealthBonus} HP (original: {originalHealthLimit}, new limit: {agent.HealthLimit})");
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

