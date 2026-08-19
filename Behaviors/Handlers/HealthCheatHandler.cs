#nullable enable
// System namespaces
// Project namespaces
using BannerWand.Constants;
using BannerWand.Settings;
using BannerWand.Utils;
using System.Collections.Generic;
// Third-party namespaces
using TaleWorlds.MountAndBlade;

namespace BannerWand.Behaviors.Handlers
{
    /// <summary>
    /// Handles health-related cheats for the player in combat.
    /// </summary>
    public class HealthCheatHandler
    {
        private readonly Dictionary<int, bool> _infiniteHealthApplied = [];

        /// <summary>
        /// The agent currently flagged invulnerable by <see cref="ApplyUnlimitedHealth"/>, if any.
        /// Tracked so the flag can be toggled back off if the cheat is disabled mid-mission -
        /// <see cref="Agent.ToggleInvulnerable"/> flips a boolean rather than setting it directly,
        /// so calling it twice on the same agent restores the original state.
        /// </summary>
        private Agent? _invulnerableAgent;

        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings? Settings => CheatSettings.Instance;

        /// <summary>
        /// Gets the current target settings instance.
        /// </summary>
        private static CheatTargetSettings? TargetSettings => CheatTargetSettings.Instance;

        /// <summary>
        /// Applies unlimited health to the player agent.
        /// </summary>
        /// <param name="agent">The player agent to apply the cheat to.</param>
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
                RevertInvulnerability();
                return;
            }

            if (agent?.IsActive() != true)
            {
                return;
            }

            // Agent.ToggleInvulnerable() makes the agent immune to damage outright, unlike the old
            // approach of only topping HP back up after the fact - which could still lose the agent
            // to a single hit dealing more damage than current HP in the same frame the topping-off
            // logic runs. The HP top-off below is kept as a defensive fallback in case invulnerability
            // does not cover every damage path (e.g. scripted/non-Blow damage).
            if (_invulnerableAgent != agent)
            {
                agent.ToggleInvulnerable();
                _invulnerableAgent = agent;
            }

            if (agent.Health < agent.HealthLimit)
            {
                agent.Health = agent.HealthLimit;
            }
        }

        /// <summary>
        /// Toggles invulnerability back off on <see cref="_invulnerableAgent"/> if it was set by
        /// <see cref="ApplyUnlimitedHealth"/>, then clears the tracking field.
        /// </summary>
        private void RevertInvulnerability()
        {
            if (_invulnerableAgent?.IsActive() == true)
            {
                _invulnerableAgent.ToggleInvulnerable();
            }

            _invulnerableAgent = null;
        }

        /// <summary>
        /// Applies infinite health (+9999 HP) to the player agent.
        /// </summary>
        /// <param name="agent">The player agent to apply the cheat to.</param>
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

        /// <summary>
        /// Applies unlimited horse health to the player's mount.
        /// </summary>
        /// <param name="agent">The player agent whose mount to apply the cheat to.</param>
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

        /// <summary>
        /// Handles health restoration after an agent is hit.
        /// </summary>
        /// <param name="affectedAgent">The agent that was hit.</param>
        /// <param name="affectorAgent">The agent that dealt the damage.</param>
        /// <param name="blow">Details about the blow.</param>
        public void OnAgentHit(Agent affectedAgent, Agent? affectorAgent, Blow blow)
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

                ModLogger.DebugThrottled($"Infinite Health applied to agent {agentIndex}: +{GameConstants.InfiniteHealthBonus} HP (original: {originalHealthLimit}, new limit: {agent.HealthLimit})");
            }
        }

        /// <summary>
        /// Clears the infinite health applied tracking dictionary and forgets the last agent
        /// flagged invulnerable by <see cref="ApplyUnlimitedHealth"/> - a fresh mission spawns a
        /// new <see cref="Agent"/> instance, so there is nothing left to toggle back off.
        /// </summary>
        public void ClearInfiniteHealthTracking()
        {
            _infiniteHealthApplied.Clear();
            _invulnerableAgent = null;
        }
    }
}

