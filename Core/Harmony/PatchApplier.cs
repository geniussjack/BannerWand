#nullable enable
using BannerWand.Utils;
using HarmonyLib;
using System;
using System.Reflection;

namespace BannerWand.Core.Harmony
{
    /// <summary>
    /// Handles application and removal of Harmony patches.
    /// </summary>
    /// <param name="harmonyInstance">The Harmony instance to use for patching.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="harmonyInstance"/> is null.</exception>
    public class PatchApplier(HarmonyLib.Harmony harmonyInstance)
    {
        private readonly HarmonyLib.Harmony _harmonyInstance = harmonyInstance ?? throw new ArgumentNullException(nameof(harmonyInstance));

        /// <summary>
        /// Applies a Harmony patch to a target method.
        /// </summary>
        /// <param name="targetMethod">The method to patch.</param>
        /// <param name="patchMethod">The patch method to apply.</param>
        /// <param name="patchType">The type of patch (prefix, postfix, transpiler, etc.).</param>
        /// <returns><c>true</c> if the patch was applied successfully; otherwise, <c>false</c>.</returns>
        public bool ApplyPatch(MethodBase targetMethod, MethodInfo patchMethod, string patchType = "prefix")
        {
            try
            {
                if (targetMethod == null)
                {
                    ModLogger.Warning("[PatchApplier] Target method is null - cannot apply patch!");
                    return false;
                }

                if (patchMethod == null)
                {
                    ModLogger.Warning("[PatchApplier] Patch method is null - cannot apply patch!");
                    return false;
                }

                HarmonyMethod harmonyMethod = new(patchMethod);

                switch (patchType.ToLowerInvariant())
                {
                    case "prefix":
                        _ = _harmonyInstance.Patch(targetMethod, prefix: harmonyMethod);
                        break;
                    case "postfix":
                        _ = _harmonyInstance.Patch(targetMethod, postfix: harmonyMethod);
                        break;
                    case "transpiler":
                        _ = _harmonyInstance.Patch(targetMethod, transpiler: harmonyMethod);
                        break;
                    case "finalizer":
                        _ = _harmonyInstance.Patch(targetMethod, finalizer: harmonyMethod);
                        break;
                    default:
                        ModLogger.Warning($"[PatchApplier] Unknown patch type: {patchType}. Using prefix.");
                        _ = _harmonyInstance.Patch(targetMethod, prefix: harmonyMethod);
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[PatchApplier] Error applying patch: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Removes a Harmony patch from a target method.
        /// </summary>
        /// <param name="targetMethod">The method to unpatch.</param>
        /// <param name="patchMethod">The patch method to remove.</param>
        /// <returns><c>true</c> if the patch was removed successfully; otherwise, <c>false</c>.</returns>
        public bool RemovePatch(MethodBase targetMethod, MethodInfo patchMethod)
        {
            try
            {
                if (targetMethod == null || patchMethod == null)
                {
                    return false;
                }

                _harmonyInstance.Unpatch(targetMethod, patchMethod);
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Warning($"[PatchApplier] Error removing patch: {ex.Message}");
                return false;
            }
        }
    }
}

