#nullable enable
using TaleWorlds.CampaignSystem.Settlements;

namespace BannerWandRetro.Interfaces
{
    /// <summary>
    /// Defines the contract for determining if settlement cheats should be applied.
    /// Provides centralized logic for checking if a settlement qualifies for cheat effects.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface abstracts the settlement targeting system, allowing for different filtering strategies
    /// and implementations. This is useful for testing, custom targeting logic, or AI-controlled targeting.
    /// </para>
    /// <para>
    /// The filtering logic typically follows this pattern:
    /// 1. Check if settlement is player-owned (highest priority)
    /// 2. Check if settlement is rebelling (special case)
    /// 3. Check if settlement is NPC-owned and matches target criteria
    /// </para>
    /// <para>
    /// All methods should be O(1) complexity - just property lookups and boolean checks.
    /// Implementations should be safe to call frequently during cheat application.
    /// </para>
    /// <para>
    /// See <see cref="Utils.SettlementCheatHelper"/> for the default static implementation.
    /// </para>
    /// </remarks>
    public interface ISettlementCheatHelper
    {
        /// <summary>
        /// Determines if cheats should be applied to the specified settlement.
        /// </summary>
        /// <param name="settlement">The settlement to check. Can be null.</param>
        /// <returns>
        /// True if the settlement should receive cheats, false otherwise.
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method checks three main conditions:
        /// 1. Player-owned settlements: If the settlement belongs to the player's clan
        /// 2. Rebelling settlements: If the settlement is rebelling, it's treated as an NPC settlement and checked against NPC target settings
        /// 3. NPC-owned settlements: If the settlement belongs to a targeted NPC clan (based on TargetFilter settings)
        /// </para>
        /// <para>
        /// Returns false if:
        /// - Settlement is null
        /// - OwnerClan is null (unless it's a rebelling settlement)
        /// - TargetSettings is null
        /// - Settlement doesn't match any target criteria
        /// </para>
        /// </remarks>
        bool ShouldApplyCheatToSettlement(Settlement? settlement);
    }
}

