#nullable enable
// System namespaces
// Project namespaces
using BannerWand.Utils;
using System.Collections.Generic;
// Third-party namespaces
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;

namespace BannerWand.Services
{
    /// <summary>
    /// Centralized caching service for frequently accessed game data.
    /// Reduces repeated queries to game collections and improves performance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This service caches collections that are accessed frequently but change rarely,
    /// such as lists of all heroes, clans, and settlements. Caches are invalidated
    /// on campaign events (daily tick, hero death, etc.) to ensure data freshness.
    /// </para>
    /// <para>
    /// Benefits:
    /// - Reduces CPU overhead from repeated collection queries
    /// - Provides consistent snapshots of game state within a tick
    /// - Centralizes cache invalidation logic
    /// </para>
    /// </remarks>
    public static class CacheService
    {
        #region Cache Fields

        /// <summary>
        /// Cached list of all alive heroes.
        /// Invalidated daily or when heroes die.
        /// </summary>
        private static List<Hero>? _cachedAliveHeroes;

        /// <summary>
        /// Cached list of all clans.
        /// Invalidated daily or when clans are created/destroyed.
        /// </summary>
        private static List<Clan>? _cachedAllClans;

        /// <summary>
        /// Cached list of all settlements.
        /// Invalidated when settlements are captured or created.
        /// </summary>
        private static List<Settlement>? _cachedAllSettlements;

        /// <summary>
        /// Cached list of all mobile parties.
        /// Invalidated hourly due to frequent changes.
        /// </summary>
        private static List<MobileParty>? _cachedAllParties;

        /// <summary>
        /// Last cache update time for heroes.
        /// </summary>
        private static CampaignTime _lastHeroesUpdate = CampaignTime.Zero;

        /// <summary>
        /// Last cache update time for clans.
        /// </summary>
        private static CampaignTime _lastClansUpdate = CampaignTime.Zero;

        /// <summary>
        /// Last cache update time for settlements.
        /// </summary>
        private static CampaignTime _lastSettlementsUpdate = CampaignTime.Zero;

        /// <summary>
        /// Last cache update time for parties.
        /// </summary>
        private static CampaignTime _lastPartiesUpdate = CampaignTime.Zero;

        /// <summary>
        /// Cache validity duration for heroes (1 day).
        /// </summary>
        private static readonly CampaignTime HeroesCacheValidity = CampaignTime.Days(1);

        /// <summary>
        /// Cache validity duration for clans (1 day).
        /// </summary>
        private static readonly CampaignTime ClansCacheValidity = CampaignTime.Days(1);

        /// <summary>
        /// Cache validity duration for settlements (1 day).
        /// </summary>
        private static readonly CampaignTime SettlementsCacheValidity = CampaignTime.Days(1);

        /// <summary>
        /// Cache validity duration for parties (1 hour).
        /// </summary>
        private static readonly CampaignTime PartiesCacheValidity = CampaignTime.Hours(1);

        #endregion

        #region Public Methods

        /// <summary>
        /// Gets cached list of all alive heroes.
        /// Updates cache if expired or invalid.
        /// </summary>
        /// <returns>List of all alive heroes.</returns>
        public static List<Hero> GetAliveHeroes()
        {
            CampaignTime now = CampaignTime.Now;

            if (_cachedAliveHeroes == null || now - _lastHeroesUpdate > HeroesCacheValidity)
            {
                RefreshHeroesCache();
            }

            return _cachedAliveHeroes!;
        }

        /// <summary>
        /// Gets cached list of all clans.
        /// Updates cache if expired or invalid.
        /// </summary>
        /// <returns>List of all clans.</returns>
        public static List<Clan> GetAllClans()
        {
            CampaignTime now = CampaignTime.Now;

            if (_cachedAllClans == null || now - _lastClansUpdate > ClansCacheValidity)
            {
                RefreshClansCache();
            }

            return _cachedAllClans!;
        }

        /// <summary>
        /// Gets cached list of all settlements.
        /// Updates cache if expired or invalid.
        /// </summary>
        /// <returns>List of all settlements.</returns>
        public static List<Settlement> GetAllSettlements()
        {
            CampaignTime now = CampaignTime.Now;

            if (_cachedAllSettlements == null || now - _lastSettlementsUpdate > SettlementsCacheValidity)
            {
                RefreshSettlementsCache();
            }

            return _cachedAllSettlements!;
        }

        /// <summary>
        /// Gets cached list of all mobile parties.
        /// Updates cache if expired or invalid.
        /// </summary>
        /// <returns>List of all mobile parties.</returns>
        public static List<MobileParty> GetAllParties()
        {
            CampaignTime now = CampaignTime.Now;

            if (_cachedAllParties == null || now - _lastPartiesUpdate > PartiesCacheValidity)
            {
                RefreshPartiesCache();
            }

            return _cachedAllParties!;
        }

        /// <summary>
        /// Invalidates all caches, forcing refresh on next access.
        /// </summary>
        public static void InvalidateAll()
        {
            _cachedAliveHeroes = null;
            _cachedAllClans = null;
            _cachedAllSettlements = null;
            _cachedAllParties = null;

            _lastHeroesUpdate = CampaignTime.Zero;
            _lastClansUpdate = CampaignTime.Zero;
            _lastSettlementsUpdate = CampaignTime.Zero;
            _lastPartiesUpdate = CampaignTime.Zero;

            ModLogger.Debug("[CacheService] All caches invalidated");
        }

        /// <summary>
        /// Invalidates hero cache.
        /// </summary>
        public static void InvalidateHeroes()
        {
            _cachedAliveHeroes = null;
            _lastHeroesUpdate = CampaignTime.Zero;
            ModLogger.Debug("[CacheService] Heroes cache invalidated");
        }

        /// <summary>
        /// Invalidates clan cache.
        /// </summary>
        public static void InvalidateClans()
        {
            _cachedAllClans = null;
            _lastClansUpdate = CampaignTime.Zero;
            ModLogger.Debug("[CacheService] Clans cache invalidated");
        }

        /// <summary>
        /// Invalidates settlement cache.
        /// </summary>
        public static void InvalidateSettlements()
        {
            _cachedAllSettlements = null;
            _lastSettlementsUpdate = CampaignTime.Zero;
            ModLogger.Debug("[CacheService] Settlements cache invalidated");
        }

        /// <summary>
        /// Invalidates party cache.
        /// </summary>
        public static void InvalidateParties()
        {
            _cachedAllParties = null;
            _lastPartiesUpdate = CampaignTime.Zero;
            ModLogger.Debug("[CacheService] Parties cache invalidated");
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Refreshes the heroes cache from game data.
        /// </summary>
        private static void RefreshHeroesCache()
        {
            _cachedAliveHeroes = new List<Hero>(capacity: 500);

            _cachedAliveHeroes.AddRange(Hero.AllAliveHeroes);

            _lastHeroesUpdate = CampaignTime.Now;
            ModLogger.Debug($"[CacheService] Heroes cache refreshed: {_cachedAliveHeroes.Count} heroes");
        }

        /// <summary>
        /// Refreshes the clans cache from game data.
        /// </summary>
        private static void RefreshClansCache()
        {
            _cachedAllClans = new List<Clan>(capacity: 100);

            _cachedAllClans.AddRange(Clan.All);

            _lastClansUpdate = CampaignTime.Now;
            ModLogger.Debug($"[CacheService] Clans cache refreshed: {_cachedAllClans.Count} clans");
        }

        /// <summary>
        /// Refreshes the settlements cache from game data.
        /// </summary>
        private static void RefreshSettlementsCache()
        {
            _cachedAllSettlements = new List<Settlement>(capacity: 100);

            _cachedAllSettlements.AddRange(Settlement.All);

            _lastSettlementsUpdate = CampaignTime.Now;
            ModLogger.Debug($"[CacheService] Settlements cache refreshed: {_cachedAllSettlements.Count} settlements");
        }

        /// <summary>
        /// Refreshes the parties cache from game data.
        /// </summary>
        private static void RefreshPartiesCache()
        {
            _cachedAllParties = new List<MobileParty>(capacity: 500);

            _cachedAllParties.AddRange(MobileParty.All);

            _lastPartiesUpdate = CampaignTime.Now;
            ModLogger.Debug($"[CacheService] Parties cache refreshed: {_cachedAllParties.Count} parties");
        }

        #endregion
    }
}
