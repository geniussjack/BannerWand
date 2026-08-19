#nullable enable
// System namespaces
// Project namespaces
using BannerWand.Utils;
using System;
using System.Linq;
using System.Reflection;

namespace BannerWand.Core.Harmony
{
    /// <summary>
    /// Validates Harmony patch application status.
    /// </summary>
    public class PatchValidator
    {
        /// <summary>
        /// Checks if a patch is already applied to a target method.
        /// </summary>
        /// <param name="targetMethod">The method that may be patched.</param>
        /// <param name="patchMethod">The patch method to check for.</param>
        /// <returns>
        /// <c>true</c> if the patch is already applied; otherwise, <c>false</c>.
        /// </returns>
        /// <remarks>
        /// This method checks if the patch method is already in the prefixes,
        /// postfixes, transpilers, or finalizers of the target method.
        /// </remarks>
        public bool IsPatchAlreadyApplied(MethodBase targetMethod, MethodInfo patchMethod)
        {
            try
            {
                if (targetMethod == null || patchMethod == null)
                {
                    return false;
                }

                HarmonyLib.Patches? existingPatches = HarmonyLib.Harmony.GetPatchInfo(targetMethod);
                if (existingPatches == null)
                {
                    return false;
                }

                // Check if our patch method is already in the prefixes or postfixes
                // Compare by declaring type and method name, not exact MethodInfo
                // This handles cases where PatchAll() applied a different overload than what we're checking
                string patchClassName = patchMethod.DeclaringType?.FullName ?? "";
                string patchMethodName = patchMethod.Name;

                return existingPatches.Prefixes.Any(p =>
                           (p.PatchMethod == patchMethod) ||
                           (p.PatchMethod.DeclaringType?.FullName == patchClassName && p.PatchMethod.Name == patchMethodName)) ||
                       existingPatches.Postfixes.Any(p =>
                           (p.PatchMethod == patchMethod) ||
                           (p.PatchMethod.DeclaringType?.FullName == patchClassName && p.PatchMethod.Name == patchMethodName)) ||
                       existingPatches.Transpilers.Any(p =>
                           (p.PatchMethod == patchMethod) ||
                           (p.PatchMethod.DeclaringType?.FullName == patchClassName && p.PatchMethod.Name == patchMethodName)) ||
                       existingPatches.Finalizers.Any(p =>
                           (p.PatchMethod == patchMethod) ||
                           (p.PatchMethod.DeclaringType?.FullName == patchClassName && p.PatchMethod.Name == patchMethodName));
            }
            catch (Exception ex)
            {
                // If we can't check, assume not applied to be safe
                ModLogger.Warning($"[PatchValidator] Failed to check if patch is already applied: {ex.Message}");
                return false;
            }
        }
    }
}

