#nullable enable
namespace BannerWand.Interfaces
{
    /// <summary>
    /// Defines the contract for resolving log file paths.
    /// Abstracts the logic for determining where log files should be written.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface allows for different path resolution strategies:
    /// - Platform-specific paths
    /// - Custom user-defined paths
    /// - Test/mock paths for unit testing
    /// </para>
    /// <para>
    /// Implementations should handle path validation, directory creation, and fallback scenarios.
    /// </para>
    /// </remarks>
    public interface ILogPathResolver
    {
        /// <summary>
        /// Resolves the full path to the log file.
        /// </summary>
        /// <returns>
        /// The full path to the log file, or null if the path cannot be determined.
        /// The returned path should be ready for immediate file operations (directory exists, writable).
        /// </returns>
        /// <remarks>
        /// <para>
        /// This method should:
        /// 1. Determine the appropriate base directory for logs
        /// 2. Create necessary subdirectories if they don't exist
        /// 3. Validate that the location is writable
        /// 4. Return a fallback path if the primary location is unavailable
        /// </para>
        /// <para>
        /// The method should be idempotent - calling it multiple times should return the same path
        /// (assuming the environment hasn't changed).
        /// </para>
        /// </remarks>
        string? ResolveLogFilePath();

        /// <summary>
        /// Validates that the specified log file path is writable.
        /// </summary>
        /// <param name="logPath">The log file path to validate.</param>
        /// <returns>True if the path is writable, false otherwise.</returns>
        /// <remarks>
        /// This method should test write permissions without modifying existing files.
        /// </remarks>
        bool IsPathWritable(string logPath);

        /// <summary>
        /// Gets a fallback log file path when the primary path cannot be used.
        /// </summary>
        /// <returns>A fallback path that can be used for logging, or null if no fallback is available.</returns>
        /// <remarks>
        /// Fallback paths are typically simpler locations like the current directory
        /// that have a higher chance of being writable.
        /// </remarks>
        string? GetFallbackPath();
    }
}

