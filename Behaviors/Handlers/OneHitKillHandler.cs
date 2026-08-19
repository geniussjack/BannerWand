#nullable enable
using BannerWand.Constants;
using BannerWand.Settings;
using BannerWand.Utils;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BannerWand.Behaviors.Handlers
{
    /// <summary>
    /// Handles one-hit kill cheats in combat.
    /// </summary>
    public class OneHitKillHandler
    {
        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings? Settings => CheatSettings.Instance;

        /// <summary>
        /// Applies one-hit kills to all enemy agents.
        /// </summary>
        /// <param name="agents">Collection of all agents in the mission.</param>
        public void ApplyOneHitKills(MBReadOnlyList<Agent> agents)
        {
            CheatSettings? settings = Settings;
            if (settings is null)
            {
                return;
            }

            if (!settings.OneHitKills)
            {
                return;
            }

            Agent? mainAgent = Mission.Current?.MainAgent;
            if (mainAgent == null)
            {
                return;
            }

            // OPTIMIZED: Use for loop instead of foreach for better performance
            // Process all active enemy agents
            for (int i = 0; i < agents.Count; i++)
            {
                Agent agent = agents[i];

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

                // Set health to minimum threshold
                if (agent.Health > GameConstants.OneHitKillHealthThreshold)
                {
                    float oldHealth = agent.Health;
                    agent.Health = GameConstants.OneHitKillHealthThreshold;
                    ModLogger.DebugThrottled($"One-Hit Kills: Set enemy agent {agent.Index} health from {oldHealth:F1} to {GameConstants.OneHitKillHealthThreshold:F1}");
                }
            }
        }

        /// <summary>
        /// Handles one-hit kill logic when an agent is hit.
        /// </summary>
        /// <param name="affectedAgent">The agent that was hit.</param>
        /// <param name="affectorAgent">The agent that dealt the damage.</param>
        /// <param name="blow">Details about the blow.</param>
        public void OnAgentHit(Agent affectedAgent, Agent? affectorAgent, Blow blow)
        {
            if (Settings?.OneHitKills != true)
            {
                return;
            }

            Agent? mainAgent = Mission.Current?.MainAgent;
            if (mainAgent == null)
            {
                return;
            }

            // Check: 1) Player exists, 2) Attacker is player, 3) Victim is enemy, 4) Victim is alive
            if (affectorAgent?.IsPlayerControlled == true &&
                affectedAgent?.IsEnemyOf(mainAgent) == true &&
                affectedAgent.IsActive() && affectedAgent.IsHuman)
            {
                // Set health to 0 to kill instantly
                affectedAgent.Health = GameConstants.InstantKillHealth;
            }
        }
    }
}
