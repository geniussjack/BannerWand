#nullable enable
// Third-party namespaces
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace BannerWand.Interfaces
{
    /// <summary>
    /// Defines the contract for handling NPC-related cheats in combat.
    /// </summary>
    /// <remarks>
    /// This interface abstracts NPC cheat logic, allowing for different
    /// implementations and testability.
    /// </remarks>
    public interface INPCCheatHandler
    {
        /// <summary>
        /// Applies unlimited HP to allied NPC heroes.
        /// </summary>
        /// <param name="agents">Collection of all agents in the mission.</param>
        void ApplyNPCUnlimitedHP(MBReadOnlyList<Agent> agents);

        /// <summary>
        /// Applies infinite HP (+9999) to allied NPC heroes.
        /// </summary>
        /// <param name="agents">Collection of all agents in the mission.</param>
        void ApplyNPCInfiniteHP(MBReadOnlyList<Agent> agents);

        /// <summary>
        /// Applies unlimited horse HP to allied NPC heroes.
        /// </summary>
        /// <param name="agents">Collection of all agents in the mission.</param>
        void ApplyNPCUnlimitedHorseHP(MBReadOnlyList<Agent> agents);

        /// <summary>
        /// Applies unlimited shield HP to allied NPC heroes.
        /// </summary>
        /// <param name="agents">Collection of all agents in the mission.</param>
        void ApplyNPCUnlimitedShieldHP(MBReadOnlyList<Agent> agents);

        /// <summary>
        /// Applies unlimited ammo to allied NPC heroes.
        /// </summary>
        /// <param name="agents">Collection of all agents in the mission.</param>
        void ApplyNPCUnlimitedAmmo(MBReadOnlyList<Agent> agents);
    }
}

