#nullable enable
namespace BannerWandRetro.Interfaces
{
    /// <summary>
    /// Defines the contract for managing Harmony patches in the BannerWand mod.
    /// Provides lifecycle management for runtime method patching.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface abstracts Harmony patch management, allowing for different patching strategies,
    /// testing scenarios (no-patch mode), or alternative patching libraries.
    /// </para>
    /// <para>
    /// Harmony is used to patch game methods that cannot be overridden through the official
    /// Game Model API. This includes methods in sealed classes or specific behaviors that
    /// don't have corresponding model hooks.
    /// </para>
    /// <para>
    /// Patches typically include:
    /// - TradeItemsNoDecrease: Patches ItemBarterable.Apply() to prevent item loss
    /// - UnlockAllSmithyParts: Patches CraftingCampaignBehavior to unlock smithing parts
    /// - RenownMultiplier: Patches Clan.AddRenown() to multiply renown gains
    /// </para>
    /// <para>
    /// Thread safety: Harmony patching should occur during mod initialization (single-threaded).
    /// Runtime patch operations are not thread-safe and should be avoided.
    /// </para>
    /// <para>
    /// See <see cref="Core.HarmonyManager"/> for the default static implementation.
    /// </para>
    /// </remarks>
    public interface IHarmonyManager
    {
        /// <summary>
        /// Gets the active Harmony instance used for patching.
        /// </summary>
        /// <value>The Harmony instance, or null if not initialized.</value>
        /// <remarks>
        /// Access to the raw Harmony instance allows for advanced patching scenarios
        /// and debugging patch conflicts with other mods.
        /// </remarks>
        HarmonyLib.Harmony? Instance { get; }

        /// <summary>
        /// Gets whether Harmony patches have been successfully initialized.
        /// </summary>
        /// <value>True if initialized and patches applied, false otherwise.</value>
        /// <remarks>
        /// Check this property before assuming patches are active. Initialization can fail
        /// due to missing dependencies, wrong game version, or mod conflicts.
        /// </remarks>
        bool IsInitialized { get; }

        /// <summary>
        /// Initializes and applies all Harmony patches.
        /// </summary>
        /// <returns>
        /// <c>true</c> if initialization successful (all patches applied);
        /// <c>false</c> if initialization failed (check logs for details).
        /// </returns>
        /// <remarks>
        /// <para>
        /// Should be safe to call multiple times - subsequent calls should be no-ops if already initialized.
        /// </para>
        /// <para>
        /// Patches are typically discovered automatically through assembly scanning.
        /// Classes marked with [HarmonyPatch] attribute are automatically detected and patched.
        /// </para>
        /// <para>
        /// Errors during initialization should be logged but not throw exceptions,
        /// allowing the mod to function with reduced functionality.
        /// </para>
        /// </remarks>
        bool Initialize();

        /// <summary>
        /// Removes all Harmony patches applied by this mod.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Cleanly unpatches all methods modified by this mod, restoring original behavior.
        /// Other mods' patches should remain unaffected (Harmony uses unique IDs per mod).
        /// </para>
        /// <para>
        /// Should be safe to call even if not initialized - implementations should log warning and return.
        /// </para>
        /// <para>
        /// Typically called during mod unload or game shutdown to ensure clean state.
        /// </para>
        /// </remarks>
        void Uninitialize();
    }
}
