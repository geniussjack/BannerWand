#nullable enable
using BannerWand.Utils;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace BannerWand.Core.Harmony
{
    /// <summary>
    /// Logs Harmony patch information for debugging and conflict detection.
    /// </summary>
    /// <param name="harmonyInstance">The Harmony instance to use for logging.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="harmonyInstance"/> is null.</exception>
    public class PatchLogger(HarmonyLib.Harmony harmonyInstance)
    {
        private readonly HarmonyLib.Harmony _harmonyInstance = harmonyInstance ?? throw new ArgumentNullException(nameof(harmonyInstance));

        /// <summary>
        /// Logs information about all patched methods.
        /// </summary>
        /// <remarks>
        /// This method logs all methods that have been patched by this mod,
        /// including the patch owner, patch method name, and patch type.
        /// Useful for debugging patch conflicts with other mods.
        /// </remarks>
        public void LogPatchedMethods()
        {
            try
            {
                IEnumerable<MethodBase> patchedMethods = _harmonyInstance.GetPatchedMethods();
                int patchedMethodCount = 0;

                ModLogger.Log("Patched methods:");

                foreach (MethodBase method in patchedMethods)
                {
                    HarmonyLib.Patches patchInfo = HarmonyLib.Harmony.GetPatchInfo(method);
                    string declaringTypeName = method.DeclaringType?.FullName ?? "Unknown";
                    ModLogger.Log($"  - {declaringTypeName}.{method.Name}");

                    if (patchInfo != null)
                    {
                        int prefixCount = patchInfo.Prefixes.Count;
                        int postfixCount = patchInfo.Postfixes.Count;

                        if (prefixCount > 0)
                        {
                            ModLogger.Debug($"    Prefixes: {prefixCount}");
                        }

                        if (postfixCount > 0)
                        {
                            ModLogger.Debug($"    Postfixes: {postfixCount}");
                        }
                    }

                    patchedMethodCount++;
                }

                ModLogger.Log($"Total patched methods: {patchedMethodCount}");
            }
            catch (Exception exception)
            {
                ModLogger.Warning($"Failed to log patched methods: {exception.Message}");
            }
        }

        /// <summary>
        /// Logs information about a specific patch application.
        /// </summary>
        /// <param name="patchName">The name of the patch being applied.</param>
        /// <param name="targetMethod">The target method being patched.</param>
        /// <param name="success">Whether the patch was applied successfully.</param>
        public void LogPatchApplication(string patchName, MethodBase? targetMethod, bool success)
        {
            if (targetMethod == null)
            {
                ModLogger.Warning($"[{patchName}] Target method is null");
                return;
            }

            string declaringTypeName = targetMethod.DeclaringType?.FullName ?? "Unknown";
            string methodName = targetMethod.Name;

            if (success)
            {
                ModLogger.Log($"[{patchName}] Patch applied successfully: {declaringTypeName}.{methodName}");
            }
            else
            {
                ModLogger.Error($"[{patchName}] Failed to apply patch: {declaringTypeName}.{methodName}");
            }
        }
    }
}

