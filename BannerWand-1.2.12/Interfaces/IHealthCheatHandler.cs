#nullable enable
using TaleWorlds.MountAndBlade;

namespace BannerWandRetro.Interfaces
{
    /// <summary>
    /// Defines the contract for handling health-related cheats in combat.
    /// </summary>
    /// <remarks>
    /// This interface abstracts health cheat logic, allowing for different
    /// implementations and testability.
    /// </remarks>
    public interface IHealthCheatHandler
    {
        /// <summary>
        /// Applies unlimited health to the player agent.
        /// </summary>
        /// <param name="agent">The player agent to apply the cheat to.</param>
        void ApplyUnlimitedHealth(Agent agent);

        /// <summary>
        /// Applies infinite health (+9999 HP) to the player agent.
        /// </summary>
        /// <param name="agent">The player agent to apply the cheat to.</param>
        void ApplyInfiniteHealth(Agent agent);

        /// <summary>
        /// Applies unlimited horse health to the player's mount.
        /// </summary>
        /// <param name="agent">The player agent whose mount to apply the cheat to.</param>
        void ApplyUnlimitedHorseHealth(Agent agent);

        /// <summary>
        /// Handles health restoration after an agent is hit.
        /// </summary>
        /// <param name="affectedAgent">The agent that was hit.</param>
        /// <param name="affectorAgent">The agent that dealt the damage.</param>
        /// <param name="blow">Details about the blow.</param>
        void OnAgentHit(Agent affectedAgent, Agent? affectorAgent, in Blow blow);
    }
}

