#nullable enable
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BannerWandRetro.Interfaces
{
    /// <summary>
    /// Defines the contract for handling one-hit kill cheats in combat.
    /// </summary>
    /// <remarks>
    /// This interface abstracts one-hit kill logic, allowing for different
    /// implementations and testability.
    /// </remarks>
    public interface IOneHitKillHandler
    {
        /// <summary>
        /// Applies one-hit kills to all enemy agents.
        /// </summary>
        /// <param name="agents">Collection of all agents in the mission.</param>
        void ApplyOneHitKills(MBReadOnlyList<Agent> agents);

        /// <summary>
        /// Handles one-hit kill logic when an agent is hit.
        /// </summary>
        /// <param name="affectedAgent">The agent that was hit.</param>
        /// <param name="affectorAgent">The agent that dealt the damage.</param>
        /// <param name="blow">Details about the blow.</param>
        void OnAgentHit(Agent affectedAgent, Agent? affectorAgent, in Blow blow);
    }
}

