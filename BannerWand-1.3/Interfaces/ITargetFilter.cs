#nullable enable
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace BannerWand.Interfaces
{
    /// <summary>
    /// Defines the contract for filtering cheat targets based on entity type and settings.
    /// Provides centralized logic for determining which entities should be affected by cheats.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface abstracts the targeting system, allowing for different filtering strategies
    /// and implementations. This is useful for testing, custom targeting logic, or AI-controlled targeting.
    /// </para>
    /// <para>
    /// The filtering logic is typically hierarchical:
    /// 1. Check if entity is player (highest priority)
    /// 2. Check if entity is in player's clan
    /// 3. Check if entity is in player's kingdom (vassals)
    /// 4. Check if entity is in other kingdoms
    /// 5. Check if entity is independent
    /// </para>
    /// <para>
    /// All methods should be O(1) complexity - just property lookups and boolean checks.
    /// Implementations should be safe to call frequently during cheat application.
    /// </para>
    /// <para>
    /// See <see cref="Utils.TargetFilter"/> for the default static implementation.
    /// </para>
    /// </remarks>
    public interface ITargetFilter
    {
        /// <summary>
        /// Determines if a hero should be affected by cheats based on current target settings.
        /// </summary>
        /// <param name="hero">The hero to evaluate. Can be null.</param>
        /// <returns>True if the hero should receive cheats, false otherwise.</returns>
        /// <remarks>
        /// <para>
        /// Evaluation order (first match wins):
        /// 1. Is this the player hero? → Check ApplyToPlayer setting
        /// 2. Is this a player clan member? → Check ApplyToPlayerClanMembers setting
        /// 3. Is this in a kingdom?
        ///    - Is kingdom ruler? → Check ApplyToKingdomRulers setting
        ///    - Is player kingdom vassal? → Check ApplyToPlayerKingdomVassals setting
        ///    - Is clan leader? → Check ApplyToClanLeadersInKingdoms setting
        ///    - Is clan member? → Check ApplyToClanMembersInKingdoms setting
        /// 4. Is this independent?
        ///    - Is clan leader? → Check ApplyToIndependentClanLeaders setting
        ///    - Is clan member? → Check ApplyToIndependentClanMembers setting
        /// </para>
        /// <para>
        /// Null safety: Should return false for null heroes or if settings are unavailable.
        /// </para>
        /// </remarks>
        bool ShouldApplyCheat(Hero? hero);

        /// <summary>
        /// Determines if a clan should be affected by cheats based on current target settings.
        /// </summary>
        /// <param name="clan">The clan to evaluate. Can be null.</param>
        /// <returns>True if the clan should receive cheats, false otherwise.</returns>
        /// <remarks>
        /// <para>
        /// Clan-level cheats typically affect influence and other clan-wide properties.
        /// This method provides simplified clan filtering compared to hero filtering.
        /// </para>
        /// <para>
        /// Evaluation logic:
        /// 1. Player clan → Check ApplyToPlayer setting
        /// 2. Player kingdom vassal → Check ApplyToPlayerKingdomVassals setting
        /// 3. Other kingdom clan → Check kingdom-related settings
        /// 4. Independent clan → Check independent settings
        /// </para>
        /// <para>
        /// Note: Uses a simplified heuristic for non-player clans. For precise control,
        /// use <see cref="ShouldApplyCheat(Hero)"/> with the clan leader.
        /// </para>
        /// </remarks>
        bool ShouldApplyCheatToClan(Clan? clan);

        /// <summary>
        /// Determines if a mobile party should be affected by cheats based on current target settings.
        /// </summary>
        /// <param name="party">The party to evaluate. Can be null.</param>
        /// <returns>True if the party should receive cheats, false otherwise.</returns>
        /// <remarks>
        /// <para>
        /// Party targeting is determined by the party's leader hero.
        /// This method is a convenience wrapper around <see cref="ShouldApplyCheat(Hero)"/>.
        /// </para>
        /// <para>
        /// Important: Parties without leaders (rare) should not be targeted.
        /// Examples: Some merchant caravans may not always have hero leaders.
        /// </para>
        /// <para>
        /// Performance: O(1) - delegates to hero check which is also O(1).
        /// </para>
        /// </remarks>
        bool ShouldApplyCheatToParty(MobileParty? party);
    }
}
