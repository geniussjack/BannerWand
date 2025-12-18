#nullable enable
using System.Reflection;

namespace BannerWandRetro.Interfaces
{
    /// <summary>
    /// Defines the contract for logging Harmony patch information.
    /// </summary>
    /// <remarks>
    /// This interface abstracts patch logging logic, allowing for different
    /// logging strategies and testability.
    /// </remarks>
    public interface IPatchLogger
    {
        /// <summary>
        /// Logs information about all patched methods.
        /// </summary>
        /// <remarks>
        /// This method should log all methods that have been patched by this mod,
        /// including the patch owner, patch method name, and patch type.
        /// Useful for debugging patch conflicts with other mods.
        /// </remarks>
        void LogPatchedMethods();

        /// <summary>
        /// Logs information about a specific patch application.
        /// </summary>
        /// <param name="patchName">The name of the patch being applied.</param>
        /// <param name="targetMethod">The target method being patched.</param>
        /// <param name="success">Whether the patch was applied successfully.</param>
        void LogPatchApplication(string patchName, MethodBase? targetMethod, bool success);
    }
}

