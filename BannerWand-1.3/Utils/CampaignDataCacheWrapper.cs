#nullable enable
// System namespaces
using System;
using System.Collections.Generic;

// Third-party namespaces
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

// Project namespaces
using BannerWand.Interfaces;

namespace BannerWand.Utils
{
    /// <summary>
    /// Wrapper class that implements <see cref="ICampaignDataCache"/> and delegates to the static <see cref="CampaignDataCache"/> class.
    /// Enables dependency injection and testability while maintaining backward compatibility with existing static usage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This wrapper allows the caching system to be injected as a dependency, which is useful for:
    /// - Unit testing with mock caches
    /// - Dependency injection containers
    /// - Alternative caching strategies (no-cache for debugging, persistent cache, etc.)
    /// </para>
    /// <para>
    /// All property accesses and method calls are forwarded directly to the static <see cref="CampaignDataCache"/> implementation,
    /// ensuring consistent behavior regardless of how the cache is accessed.
    /// </para>
    /// <para>
    /// Usage example:
    /// <code>
    /// ICampaignDataCache cache = new CampaignDataCacheWrapper();
    /// foreach (var hero in cache.AllAliveHeroes)
    /// {
    ///     // Process hero
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public class CampaignDataCacheWrapper : ICampaignDataCache
    {
        /// <summary>
        /// Gets a cached snapshot of all alive heroes in the campaign.
        /// </summary>
        /// <returns>
        /// Read-only list of alive heroes. Cache is refreshed each campaign tick.
        /// </returns>
        /// <remarks>
        /// Delegates to <see cref="CampaignDataCache.AllAliveHeroes"/>.
        /// </remarks>
        public List<Hero> AllAliveHeroes => CampaignDataCache.AllAliveHeroes ?? [];

        /// <summary>
        /// Gets a cached snapshot of all clans in the campaign.
        /// </summary>
        /// <returns>
        /// Read-only list of clans. Cache is refreshed each campaign tick.
        /// </returns>
        /// <remarks>
        /// Delegates to <see cref="CampaignDataCache.AllClans"/>.
        /// </remarks>
        public List<Clan> AllClans => CampaignDataCache.AllClans ?? [];

        /// <summary>
        /// Gets a cached snapshot of all mobile parties in the campaign.
        /// </summary>
        /// <returns>
        /// Read-only list of mobile parties. Cache is refreshed each campaign tick.
        /// </returns>
        /// <remarks>
        /// Delegates to <see cref="CampaignDataCache.AllParties"/>.
        /// </remarks>
        public List<MobileParty> AllParties => CampaignDataCache.AllParties ?? [];

        /// <summary>
        /// Gets a cached snapshot of all kingdoms in the campaign.
        /// </summary>
        /// <returns>
        /// Read-only list of kingdoms. Cache is refreshed each campaign tick.
        /// </returns>
        /// <remarks>
        /// Delegates to <see cref="CampaignDataCache.AllKingdoms"/>.
        /// </remarks>
        public List<Kingdom> AllKingdoms => CampaignDataCache.AllKingdoms ?? [];

        /// <summary>
        /// Clears all cached data.
        /// </summary>
        /// <remarks>
        /// Delegates to <see cref="CampaignDataCache.ClearCache"/>.
        /// </remarks>
        public void ClearCache()
        {
            try
            {
                CampaignDataCache.ClearCache();
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CampaignDataCacheWrapper] Error in ClearCache: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }


        /// <summary>
        /// Forces immediate cache refresh by clearing and rebuilding on next access.
        /// </summary>
        /// <remarks>
        /// Delegates to <see cref="CampaignDataCache.ForceRefresh"/>.
        /// </remarks>
        public void ForceRefresh()
        {
            try
            {
                CampaignDataCache.ForceRefresh();

            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CampaignDataCacheWrapper] Error in ForceRefresh: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
