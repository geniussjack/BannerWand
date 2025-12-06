using BannerWandRetro.Interfaces;
using BannerWandRetro.Utils;
using HarmonyLib;
using System;

namespace BannerWandRetro.Core
{
    /// <summary>
    /// Wrapper class that implements <see cref="IHarmonyManager"/> and delegates to the static <see cref="HarmonyManager"/> class.
    /// Enables dependency injection and testability while maintaining backward compatibility with existing static usage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This wrapper allows the Harmony patch management to be injected as a dependency, which is useful for:
    /// - Unit testing with mock patch managers (no-patch mode)
    /// - Dependency injection containers
    /// - Alternative patching strategies or libraries
    /// </para>
    /// <para>
    /// All property accesses and method calls are forwarded directly to the static <see cref="HarmonyManager"/> implementation,
    /// ensuring consistent behavior regardless of how the manager is accessed.
    /// </para>
    /// <para>
    /// Usage example:
    /// <code>
    /// IHarmonyManager harmonyManager = new HarmonyManagerWrapper();
    /// if (harmonyManager.Initialize())
    /// {
    ///     // Patches applied successfully
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public class HarmonyManagerWrapper : IHarmonyManager
    {
        /// <summary>
        /// Gets the active Harmony instance.
        /// </summary>
        /// <value>The Harmony instance, or null if not initialized.</value>
        /// <remarks>
        /// Delegates to <see cref="HarmonyManager.Instance"/>.
        /// Callers should check for null before use if initialization may not have occurred.
        /// </remarks>
        public Harmony Instance => HarmonyManager.Instance;

        /// <summary>
        /// Gets whether Harmony patches have been initialized.
        /// </summary>
        /// <value>True if initialized, false otherwise.</value>
        /// <remarks>
        /// Delegates to <see cref="HarmonyManager.IsInitialized"/>.
        /// </remarks>
        public bool IsInitialized => HarmonyManager.IsInitialized;

        /// <summary>
        /// Initializes and applies all Harmony patches.
        /// </summary>
        /// <returns>
        /// <c>true</c> if initialization successful (all patches applied);
        /// <c>false</c> if initialization failed (check logs for details).
        /// </returns>
        /// <remarks>
        /// Delegates to <see cref="HarmonyManager.Initialize"/>.
        /// </remarks>
        public bool Initialize()
        {
            try
            {
                return HarmonyManager.Initialize();
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[HarmonyManagerWrapper] Error in Initialize: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }


        /// <summary>
        /// Removes all Harmony patches applied by this mod.
        /// </summary>
        /// <remarks>
        /// Delegates to <see cref="HarmonyManager.Uninitialize"/>.
        /// </remarks>
        public void Uninitialize()
        {
            try
            {
                HarmonyManager.Uninitialize();

            }
            catch (Exception ex)
            {
                ModLogger.Error($"[HarmonyManagerWrapper] Error in Uninitialize: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
