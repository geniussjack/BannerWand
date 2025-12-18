#nullable enable
using BannerWandRetro.Constants;
using BannerWandRetro.Interfaces;
using BannerWandRetro.Settings;
using BannerWandRetro.Utils;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BannerWandRetro.Behaviors.Handlers
{
    /// <summary>
    /// Handles one-hit kill cheats in combat.
    /// </summary>
    /// <remarks>
    /// This class encapsulates one-hit kill logic, making it easier to test
    /// and maintain. It implements <see cref="IOneHitKillHandler"/> for dependency injection.
    /// </remarks>
    public class OneHitKillHandler : IOneHitKillHandler
    {
        /// <summary>
        /// Gets the current cheat settings instance.
        /// </summary>
        private static CheatSettings? Settings => CheatSettings.Instance;

        /// <inheritdoc />
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
            if (mainAgent is null)
            {
                return;
            }

            // Process all active enemy agents
            foreach (Agent agent in agents)
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

                // Set health to minimum threshold
                if (agent.Health > GameConstants.OneHitKillHealthThreshold)
                {
                    float oldHealth = agent.Health;
                    agent.Health = GameConstants.OneHitKillHealthThreshold;
                    ModLogger.Debug($"One-Hit Kills: Set enemy agent {agent.Index} health from {oldHealth:F1} to {GameConstants.OneHitKillHealthThreshold:F1}");
                }
            }
        }

        /// <inheritdoc />
#pragma warning disable RCS1242 // Interface requires 'in Blow' parameter signature
        public void OnAgentHit(Agent affectedAgent, Agent? affectorAgent, in Blow blow)
#pragma warning restore RCS1242
        {
            if (Settings?.OneHitKills != true)
            {
                return;
            }

            Agent? mainAgent = Mission.Current?.MainAgent;
            if (mainAgent is null)
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

