#nullable enable
using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace BannerWand.Utils
{
    /// <summary>
    /// Provides thread-safe caching for frequently accessed campaign data collections.
    /// Reduces performance overhead from repeated enumeration of Hero.AllAliveHeroes, Clan.All, etc.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Performance impact: Without caching, each access to Hero.AllAliveHeroes creates a new
    /// LINQ query that enumerates the entire hero collection. With multiple behaviors calling
    /// this per tick, we can enumerate the same collection 5-10 times per game hour.
    /// </para>
    /// <para>
    /// Caching strategy: Cache is invalidated on every campaign tick to ensure data freshness.
    /// This balances performance (reduced enumerations) with correctness (heroes can die/spawn).
    /// </para>
    /// <para>
    /// Thread safety: All cached collections are read-only snapshots. The cache itself uses
    /// lazy initialization which is safe for single-threaded campaign execution.
    /// </para>
    /// <para>
    /// This static class provides the default implementation of campaign data caching.
    /// For dependency injection scenarios, use <see cref="Interfaces.ICampaignDataCache"/> interface
    /// with <see cref="CampaignDataCacheWrapper"/> wrapper class.
    /// </para>
    /// </remarks>
    public static class CampaignDataCache
    {
        #region Fields

        private static long _lastCacheTick = -1;
        private static readonly object _lockObject = new();

        #endregion

        #region Properties

        /// <summary>
        /// Gets a cached snapshot of all alive heroes in the campaign.
        /// </summary>
        /// <returns>
        /// Read-only list of alive heroes. Cache is refreshed each campaign tick.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Performance: First call per tick enumerates Hero.AllAliveHeroes once and caches.
        /// Subsequent calls return cached list (O(1) instead of O(n) enumeration).
        /// </para>
        /// <para>
        /// Typical usage: Behaviors iterate this collection to apply cheats to NPCs.
        /// Without caching: 5-10 full enumerations per tick.
        /// With caching: 1 full enumeration per tick + instant cached access.
        /// </para>
        /// </remarks>
        public static List<Hero>? AllAliveHeroes
        {
            get
            {
                RefreshCacheIfNeeded();
                field ??= [.. Hero.AllAliveHeroes];
                return field;
            }
            private set;
        }

        /// <summary>
        /// Gets a cached snapshot of all clans in the campaign.
        /// </summary>
        /// <returns>
        /// Read-only list of clans. Cache is refreshed each campaign tick.
        /// </returns>
        /// <remarks>
        /// Used for influence application to NPC clans.
        /// </remarks>
        public static List<Clan>? AllClans
        {
            get
            {
                RefreshCacheIfNeeded();
                field ??= [.. Clan.All];
                return field;
            }
            private set;
        }

        /// <summary>
        /// Gets a cached snapshot of all mobile parties in the campaign.
        /// </summary>
        /// <returns>
        /// Read-only list of mobile parties. Cache is refreshed each campaign tick.
        /// </returns>
        /// <remarks>
        /// Used for troop XP application to NPC parties.
        /// </remarks>
        public static List<MobileParty>? AllParties
        {
            get
            {
                RefreshCacheIfNeeded();
                field ??= [.. MobileParty.All];
                return field;
            }
            private set;
        }

        /// <summary>
        /// Gets a cached snapshot of all kingdoms in the campaign.
        /// </summary>
        /// <returns>
        /// Read-only list of kingdoms. Cache is refreshed each campaign tick.
        /// </returns>
        /// <remarks>
        /// Used for kingdom-related target filtering in CheatTargetSettings.
        /// </remarks>
        public static List<Kingdom>? AllKingdoms
        {
            get
            {
                RefreshCacheIfNeeded();
                field ??= [.. Kingdom.All];
                return field;
            }
            private set;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Invalidates the cache if campaign has progressed to a new tick.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Cache invalidation uses CampaignTime.Now.ToHours as the tick identifier.
        /// When this value changes, we know the campaign has progressed and cached
        /// data may be stale (heroes could have died, clans formed, parties spawned).
        /// </para>
        /// <para>
        /// This ensures cache freshness while minimizing re-enumeration overhead.
        /// Multiple cheat applications within the same tick reuse the same cache.
        /// </para>
        /// <para>
        /// Thread safety: Uses lock to prevent race conditions when multiple threads
        /// access the cache simultaneously (though rare in Bannerlord's single-threaded campaign).
        /// </para>
        /// </remarks>
        private static void RefreshCacheIfNeeded()
        {
            if (Campaign.Current is null)
            {
                // No campaign active, clear cache
                ClearCache();
                return;
            }

            long currentTick = (long)CampaignTime.Now.ToHours;

            // Thread-safe check and update
            lock (_lockObject)
            {
                if (currentTick != _lastCacheTick)
                {
                    // New tick detected, invalidate cache
                    ClearCache();
                    _lastCacheTick = currentTick;
                }
            }
        }

        /// <summary>
        /// Clears all cached data.
        /// </summary>
        /// <remarks>
        /// Called automatically when cache is stale. Can also be called manually
        /// if external code modifies campaign data outside of normal tick progression.
        /// </remarks>
        public static void ClearCache()
        {
            try
            {
                AllAliveHeroes = null;
                AllClans = null;
                AllParties = null;
                AllKingdoms = null;

            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CampaignDataCache] Error in ClearCache: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Forces immediate cache refresh by clearing and rebuilding on next access.
        /// </summary>
        /// <remarks>
        /// Use this if you know campaign data has changed significantly (e.g., after
        /// major story events that spawn/remove many heroes at once).
        /// </remarks>
        public static void ForceRefresh()
        {
            try
            {
                ClearCache();
                _lastCacheTick = -1;

            }
            catch (Exception ex)
            {
                ModLogger.Error($"[CampaignDataCache] Error in ForceRefresh: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        #endregion
    }
}
