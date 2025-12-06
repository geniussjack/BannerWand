using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace BannerWandRetro.Interfaces
{
    /// <summary>
    /// Defines the contract for caching frequently accessed campaign data collections.
    /// Reduces performance overhead from repeated enumeration of game collections.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface abstracts the caching system, allowing for different caching strategies
    /// such as in-memory caching, persistent caching, or no-cache implementations for testing.
    /// </para>
    /// <para>
    /// Performance impact: Without caching, each access to Hero.AllAliveHeroes creates a new
    /// LINQ query that enumerates the entire hero collection. With multiple behaviors calling
    /// this per tick, we can enumerate the same collection 5-10 times per game hour.
    /// </para>
    /// <para>
    /// Caching strategy: Cache should be invalidated on campaign tick changes to ensure data freshness.
    /// This balances performance (reduced enumerations) with correctness (heroes can die/spawn).
    /// </para>
    /// <para>
    /// Thread safety: All cached collections should be read-only snapshots to prevent modification.
    /// Implementations should handle single-threaded or concurrent access based on game requirements.
    /// </para>
    /// <para>
    /// See <see cref="Utils.CampaignDataCache"/> for the default static implementation.
    /// </para>
    /// </remarks>
    public interface ICampaignDataCache
    {
        /// <summary>
        /// Gets a cached snapshot of all alive heroes in the campaign.
        /// </summary>
        /// <returns>
        /// Read-only list of alive heroes. Cache should be refreshed based on implementation strategy.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Performance: First call per tick should enumerate Hero.AllAliveHeroes once and cache.
        /// Subsequent calls should return cached list (O(1) instead of O(n) enumeration).
        /// </para>
        /// <para>
        /// Typical usage: Behaviors iterate this collection to apply cheats to NPCs.
        /// Without caching: 5-10 full enumerations per tick.
        /// With caching: 1 full enumeration per tick + instant cached access.
        /// </para>
        /// </remarks>
        List<Hero> AllAliveHeroes { get; }

        /// <summary>
        /// Gets a cached snapshot of all clans in the campaign.
        /// </summary>
        /// <returns>
        /// Read-only list of clans. Cache should be refreshed based on implementation strategy.
        /// </returns>
        /// <remarks>
        /// Used for influence application to NPC clans and clan-wide operations.
        /// </remarks>
        List<Clan> AllClans { get; }

        /// <summary>
        /// Gets a cached snapshot of all mobile parties in the campaign.
        /// </summary>
        /// <returns>
        /// Read-only list of mobile parties. Cache should be refreshed based on implementation strategy.
        /// </returns>
        /// <remarks>
        /// Used for troop XP application to NPC parties and party-wide operations.
        /// </remarks>
        List<MobileParty> AllParties { get; }

        /// <summary>
        /// Gets a cached snapshot of all kingdoms in the campaign.
        /// </summary>
        /// <returns>
        /// Read-only list of kingdoms. Cache should be refreshed based on implementation strategy.
        /// </returns>
        /// <remarks>
        /// Used for kingdom-related target filtering in CheatTargetSettings.
        /// </remarks>
        List<Kingdom> AllKingdoms { get; }

        /// <summary>
        /// Clears all cached data, forcing refresh on next access.
        /// </summary>
        /// <remarks>
        /// Called automatically when cache is stale based on implementation strategy.
        /// Can also be called manually if external code modifies campaign data outside of normal tick progression.
        /// </remarks>
        void ClearCache();

        /// <summary>
        /// Forces immediate cache refresh by clearing and rebuilding on next access.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Use this if you know campaign data has changed significantly (e.g., after
        /// major story events that spawn/remove many heroes at once).
        /// </para>
        /// <para>
        /// This is more aggressive than <see cref="ClearCache"/> as it also resets
        /// internal tick tracking or similar mechanisms.
        /// </para>
        /// </remarks>
        void ForceRefresh();
    }
}
