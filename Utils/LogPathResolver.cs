#nullable enable
// System namespaces
// Project namespaces
using BannerWand.Constants;
using BannerWand.Interfaces;
using System;
using System.IO;

namespace BannerWand.Utils
{
    /// <summary>
    /// Default implementation of <see cref="ILogPathResolver"/> that resolves today's dated log file
    /// path under the user's Documents folder (Mount and Blade II Bannerlord\Configs\ModLogs\BannerWand_yyyyMMdd.log).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation:
    /// - Uses <see cref="Environment.SpecialFolder.MyDocuments"/> as the base, matching where
    ///   ButterLib and other Bannerlord mods already write their own logs
    /// - Creates the ModLogs subdirectory automatically
    /// - Validates write permissions before returning paths
    /// - Falls back to the current directory if the primary location is unavailable
    /// </para>
    /// <para>
    /// Path structure: [MyDocuments]\Mount and Blade II Bannerlord\Configs\ModLogs\BannerWand_yyyyMMdd.log
    /// Example: C:\Users\&lt;user&gt;\Documents\Mount and Blade II Bannerlord\Configs\ModLogs\BannerWand_20260819.log
    /// </para>
    /// </remarks>
    internal class LogPathResolver : ILogPathResolver
    {
        private string? _resolvedPath;

        /// <summary>
        /// Resolves the full path to today's log file.
        /// </summary>
        /// <returns>
        /// The full path to the log file, or null if the path cannot be determined.
        /// </returns>
        public string? ResolveLogFilePath()
        {
            // Return cached path if already resolved
            if (!string.IsNullOrEmpty(_resolvedPath))
            {
                return _resolvedPath;
            }

            try
            {
                string documentsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string logDirectory = Path.Combine(documentsDirectory, LogConstants.LogSubdirectory, LogConstants.LogConfigsFolderName, LogConstants.LogsFolderName);

                // Ensure log directory exists
                try
                {
                    _ = Directory.CreateDirectory(logDirectory);
                }
                catch (Exception ex)
                {
                    TaleWorlds.Library.Debug.Print($"[BannerWand] LogPathResolver: Failed to create log directory: {ex.Message}");
                    // Continue anyway - file creation will handle directory creation if needed
                }

                string logPath = Path.Combine(logDirectory, string.Format(LogConstants.LogFileNameFormat, DateTime.Now));

                // Validate that the path is writable
                if (IsPathWritable(logPath))
                {
                    _resolvedPath = logPath;
                    return _resolvedPath;
                }
                else
                {
                    // Path is not writable, try fallback
                    TaleWorlds.Library.Debug.Print("[BannerWand] LogPathResolver: Primary path not writable, using fallback");
                    _resolvedPath = GetFallbackPath();
                    return _resolvedPath;
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[BannerWand] LogPathResolver: Exception resolving path: {ex.Message}");
                _resolvedPath = GetFallbackPath();
                return _resolvedPath;
            }
        }

        /// <summary>
        /// Validates that the specified log file path is writable.
        /// </summary>
        /// <param name="logPath">The log file path to validate.</param>
        /// <returns>True if the path is writable, false otherwise.</returns>
        public bool IsPathWritable(string logPath)
        {
            if (string.IsNullOrEmpty(logPath))
            {
                return false;
            }

            try
            {
                // Test write by creating a temporary test file
                string testFile = logPath + ".test";
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets a fallback log file path when the primary path cannot be used.
        /// </summary>
        /// <returns>A fallback path in the current directory.</returns>
        public string? GetFallbackPath()
        {
            try
            {
                return Path.Combine(Directory.GetCurrentDirectory(), string.Format(LogConstants.LogFileNameFormat, DateTime.Now));
            }
            catch
            {
                return null;
            }
        }
    }
}

