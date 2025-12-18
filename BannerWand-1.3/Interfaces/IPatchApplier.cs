#nullable enable
// System namespaces
using System.Reflection;

namespace BannerWand.Interfaces
{
    /// <summary>
    /// Defines the contract for applying Harmony patches to target methods.
    /// </summary>
    /// <remarks>
    /// This interface abstracts patch application logic, allowing for different
    /// patching strategies and testability.
    /// </remarks>
    public interface IPatchApplier
    {
        /// <summary>
        /// Applies a Harmony patch to a target method.
        /// </summary>
        /// <param name="targetMethod">The method to patch.</param>
        /// <param name="patchMethod">The patch method to apply.</param>
        /// <param name="patchType">The type of patch (prefix, postfix, transpiler, etc.).</param>
        /// <returns><c>true</c> if the patch was applied successfully; otherwise, <c>false</c>.</returns>
        bool ApplyPatch(MethodBase targetMethod, MethodInfo patchMethod, string patchType = "prefix");

        /// <summary>
        /// Removes a Harmony patch from a target method.
        /// </summary>
        /// <param name="targetMethod">The method to unpatch.</param>
        /// <param name="patchMethod">The patch method to remove.</param>
        /// <returns><c>true</c> if the patch was removed successfully; otherwise, <c>false</c>.</returns>
        bool RemovePatch(MethodBase targetMethod, MethodInfo patchMethod);
    }
}

