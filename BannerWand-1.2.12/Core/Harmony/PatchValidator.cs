#nullable enable
using BannerWandRetro.Interfaces;
using BannerWandRetro.Utils;
using System.Linq;
using System.Reflection;

namespace BannerWandRetro.Core.Harmony
{
    /// <summary>
    /// Validates Harmony patch application status.
    /// </summary>
    /// <remarks>
    /// This class encapsulates patch validation logic, making it easier to test
    /// and maintain. It implements <see cref="IPatchValidator"/> for dependency injection.
    /// </remarks>
    public class PatchValidator : IPatchValidator
    {
        /// <inheritdoc />
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
            catch (System.Exception ex)
            {
                // If we can't check, assume not applied to be safe
                ModLogger.Warning($"[PatchValidator] Failed to check if patch is already applied: {ex.Message}");
                return false;
            }
        }
    }
}

