#nullable enable
using TaleWorlds.MountAndBlade;

namespace BannerWandRetro.Interfaces
{
    /// <summary>
    /// Defines the contract for handling ammo-related cheats in combat.
    /// </summary>
    /// <remarks>
    /// This interface abstracts ammo cheat logic, allowing for different
    /// implementations and testability.
    /// </remarks>
    public interface IAmmoCheatHandler
    {
        /// <summary>
        /// Applies unlimited ammo to the player agent.
        /// </summary>
        /// <param name="agent">The player agent to apply the cheat to.</param>
        void ApplyUnlimitedAmmo(Agent agent);

        /// <summary>
        /// Handles ammo restoration after consumption.
        /// </summary>
        /// <param name="agent">The agent whose ammo to restore.</param>
        void RestoreAmmo(Agent agent);
    }
}

