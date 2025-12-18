#nullable enable
// System namespaces
using System;

// Third-party namespaces
using TaleWorlds.CampaignSystem.Settlements;

// Project namespaces
using BannerWand.Interfaces;

namespace BannerWand.Utils
{
    /// <summary>
    /// Wrapper class that implements <see cref="ISettlementCheatHelper"/> and delegates to the static <see cref="SettlementCheatHelper"/> class.
    /// Enables dependency injection and testability while maintaining backward compatibility with existing static usage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This wrapper allows the settlement targeting system to be injected as a dependency, which is useful for:
    /// - Unit testing with mock helpers
    /// - Dependency injection containers
    /// - Alternative targeting strategies (AI-controlled, custom rules)
    /// </para>
    /// <para>
    /// All method calls are forwarded directly to the static <see cref="SettlementCheatHelper"/> implementation,
    /// ensuring consistent behavior regardless of how the helper is accessed.
    /// </para>
    /// <para>
    /// Usage example:
    /// <code>
    /// ISettlementCheatHelper helper = new SettlementCheatHelperWrapper();
    /// if (helper.ShouldApplyCheatToSettlement(settlement))
    /// {
    ///     // Apply cheat to settlement
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public class SettlementCheatHelperWrapper : ISettlementCheatHelper
    {
        /// <summary>
        /// Determines if cheats should be applied to the specified settlement.
        /// </summary>
        /// <param name="settlement">The settlement to check.</param>
        /// <returns>True if the settlement should receive cheats, false otherwise.</returns>
        /// <remarks>
        /// Delegates to <see cref="SettlementCheatHelper.ShouldApplyCheatToSettlement(Settlement)"/>.
        /// </remarks>
        public bool ShouldApplyCheatToSettlement(Settlement? settlement)
        {
            try
            {
                return SettlementCheatHelper.ShouldApplyCheatToSettlement(settlement);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[SettlementCheatHelperWrapper] Error in ShouldApplyCheatToSettlement: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }
    }
}

