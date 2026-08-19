#nullable enable
using BannerWand.Interfaces;
using BannerWand.Utils;
using HarmonyLib;
using System.Reflection;

namespace BannerWand.Core.Harmony
{
    /// <summary>
    /// Handles application and removal of Harmony patches.
    /// </summary>
    /// <remarks>
    /// This class encapsulates patch application logic, making it easier to test
    /// and maintain. It implements <see cref="IPatchApplier"/> for dependency injection.
    /// </remarks>
    public class PatchApplier : IPatchApplier
    {
        private readonly HarmonyLib.Harmony _harmonyInstance;

        /// <summary>
        /// Initializes a new instance of the <see cref="PatchApplier"/> class.
        /// </summary>
        /// <param name="harmonyInstance">The Harmony instance to use for patching.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="harmonyInstance"/> is null.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0290:Use primary constructor", Justification = ".NET Framework 4.7.2 does not support primary constructors")]
        public PatchApplier(HarmonyLib.Harmony harmonyInstance)
        {
            _harmonyInstance = harmonyInstance ?? throw new System.ArgumentNullException(nameof(harmonyInstance));
        }

        /// <inheritdoc />
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
            catch (System.Exception ex)
            {
                ModLogger.Error($"[PatchApplier] Error applying patch: {ex.Message}");
                return false;
            }
        }

        /// <inheritdoc />
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
            catch (System.Exception ex)
            {
                ModLogger.Warning($"[PatchApplier] Error removing patch: {ex.Message}");
                return false;
            }
        }
    }
}

