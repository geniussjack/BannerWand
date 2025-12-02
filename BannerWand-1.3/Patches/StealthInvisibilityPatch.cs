// =============================================================================
// REMOVED: StealthInvisibilityPatch
// =============================================================================
// This patch has been REMOVED because it caused critical visual bugs:
// - NPC models displayed in broken/horizontal poses
// - The aggressive patching of ALL Agent bool methods affected skeletal animation
// 
// The original approach patched methods like:
// - IsSynchedPrefabComponentVisible
// - CanMove, ShouldStand, IsVisible
// - And 100+ other bool-returning methods
// 
// This broke the NPC rendering system and caused models to display incorrectly.
// =============================================================================

namespace BannerWand.Patches
{
    /// <summary>
    /// REMOVED: Stealth invisibility patch has been removed due to causing critical visual bugs.
    /// NPC models were displayed in broken/horizontal poses because this patch interfered
    /// with skeletal animation and model synchronization methods.
    /// </summary>
    internal static class StealthInvisibilityPatch
    {
        // Intentionally empty - patch removed
    }
}
