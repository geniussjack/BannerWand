#nullable enable
// System namespaces
using System.Reflection;

namespace BannerWand.Interfaces
{
    /// <summary>
    /// Defines the contract for validating Harmony patch application status.
    /// </summary>
    /// <remarks>
    /// This interface abstracts patch validation logic, allowing for different
    /// validation strategies and testability.
    /// </remarks>
    public interface IPatchValidator
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
        bool IsPatchAlreadyApplied(MethodBase targetMethod, MethodInfo patchMethod);
    }
}

