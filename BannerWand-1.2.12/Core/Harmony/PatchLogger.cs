#nullable enable
using BannerWandRetro.Interfaces;
using BannerWandRetro.Utils;
using System.Collections.Generic;
using System.Reflection;

namespace BannerWandRetro.Core.Harmony
{
    /// <summary>
    /// Logs Harmony patch information for debugging and conflict detection.
    /// </summary>
    /// <remarks>
    /// This class encapsulates patch logging logic, making it easier to test
    /// and maintain. It implements <see cref="IPatchLogger"/> for dependency injection.
    /// </remarks>
    public class PatchLogger : IPatchLogger
    {
        private readonly HarmonyLib.Harmony _harmonyInstance;

        /// <summary>
        /// Initializes a new instance of the <see cref="PatchLogger"/> class.
        /// </summary>
        /// <param name="harmonyInstance">The Harmony instance to use for logging.</param>
        /// <exception cref="System.ArgumentNullException">Thrown when <paramref name="harmonyInstance"/> is null.</exception>
        public PatchLogger(HarmonyLib.Harmony harmonyInstance)
        {
            _harmonyInstance = harmonyInstance ?? throw new System.ArgumentNullException(nameof(harmonyInstance));
        }

        /// <inheritdoc />
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
            catch (System.Exception exception)
            {
                ModLogger.Warning($"Failed to log patched methods: {exception.Message}");
            }
        }

        /// <inheritdoc />
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

