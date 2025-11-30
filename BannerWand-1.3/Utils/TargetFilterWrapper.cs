#nullable enable
using BannerWand.Interfaces;
using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace BannerWand.Utils
{
    /// <summary>
    /// Wrapper class that implements <see cref="ITargetFilter"/> and delegates to the static <see cref="TargetFilter"/> class.
    /// Enables dependency injection and testability while maintaining backward compatibility with existing static usage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This wrapper allows the target filtering system to be injected as a dependency, which is useful for:
    /// - Unit testing with mock filters
    /// - Dependency injection containers
    /// - Alternative filtering strategies (AI-controlled, custom rules)
    /// </para>
    /// <para>
    /// All method calls are forwarded directly to the static <see cref="TargetFilter"/> implementation,
    /// ensuring consistent behavior regardless of how the filter is accessed.
    /// </para>
    /// <para>
    /// Usage example:
    /// <code>
    /// ITargetFilter filter = new TargetFilterWrapper();
    /// if (filter.ShouldApplyCheat(hero))
    /// {
    ///     // Apply cheat to hero
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public class TargetFilterWrapper : ITargetFilter
    {
        /// <summary>
        /// Determines if a hero should be affected by cheats based on current target settings.
        /// </summary>
        /// <param name="hero">The hero to evaluate.</param>
        /// <returns>True if the hero should receive cheats, false otherwise.</returns>
        /// <remarks>
        /// Delegates to <see cref="TargetFilter.ShouldApplyCheat(Hero)"/>.
        /// </remarks>
        public bool ShouldApplyCheat(Hero? hero)
        {
            try
            {
                return TargetFilter.ShouldApplyCheat(hero);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[TargetFilterWrapper] Error in ShouldApplyCheat: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }


        /// <summary>
        /// Determines if a clan should be affected by cheats based on current target settings.
        /// </summary>
        /// <param name="clan">The clan to evaluate.</param>
        /// <returns>True if the clan should receive cheats, false otherwise.</returns>
        /// <remarks>
        /// Delegates to <see cref="TargetFilter.ShouldApplyCheatToClan(Clan)"/>.
        /// </remarks>
        public bool ShouldApplyCheatToClan(Clan? clan)
        {
            try
            {
                return TargetFilter.ShouldApplyCheatToClan(clan);

            }
            catch (Exception ex)
            {
                ModLogger.Error($"[TargetFilterWrapper] Error in ShouldApplyCheatToClan: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Determines if a mobile party should be affected by cheats based on current target settings.
        /// </summary>
        /// <param name="party">The party to evaluate.</param>
        /// <returns>True if the party should receive cheats, false otherwise.</returns>
        /// <remarks>
        /// Delegates to <see cref="TargetFilter.ShouldApplyCheatToParty(MobileParty)"/>.
        /// </remarks>
        public bool ShouldApplyCheatToParty(MobileParty? party)
        {
            try
            {
                return TargetFilter.ShouldApplyCheatToParty(party);

            }
            catch (Exception ex)
            {
                ModLogger.Error($"[TargetFilterWrapper] Error in ShouldApplyCheatToParty: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }
    }
}
