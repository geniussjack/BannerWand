#nullable enable
using TaleWorlds.MountAndBlade;

namespace BannerWandRetro.Interfaces
{
    /// <summary>
    /// Defines the contract for handling shield-related cheats in combat.
    /// </summary>
    /// <remarks>
    /// This interface abstracts shield cheat logic, allowing for different
    /// implementations and testability.
    /// </remarks>
    public interface IShieldCheatHandler
    {
        /// <summary>
        /// Applies unlimited shield durability to an agent.
        /// </summary>
        /// <param name="agent">The agent whose shield to apply the cheat to.</param>
        void ApplyUnlimitedShieldDurability(Agent agent);

        /// <summary>
        /// Handles shield durability restoration after an agent is hit.
        /// </summary>
        /// <param name="affectedAgent">The agent that was hit.</param>
        /// <param name="affectorAgent">The agent that dealt the damage.</param>
        void OnAgentHit(Agent affectedAgent, Agent? affectorAgent);
    }
}

