#nullable enable
using BannerWand.Constants;
using BannerWand.Interfaces;
using System;
using System.IO;

namespace BannerWand.Utils
{
    /// <summary>
    /// Default implementation of <see cref="ILogPathResolver"/> that resolves log paths
    /// using the module directory (BannerWand\logs\BannerWand.log).
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation:
    /// - Uses the module directory as the base (determined from assembly location)
    /// - Creates logs subdirectory automatically
    /// - Validates write permissions before returning paths
    /// - Falls back to the current directory if the primary location is unavailable
    /// </para>
    /// <para>
    /// Path structure: [GamePath]\Modules\BannerWand\logs\[LogFileName]
    /// Example: D:\SteamLibrary\steamapps\common\Mount &amp; Blade II Bannerlord\Modules\BannerWand\logs\BannerWand.log
    /// </para>
    /// </remarks>
    internal class LogPathResolver : ILogPathResolver
    {
        private string? _resolvedPath;

        /// <summary>
        /// Resolves the full path to the log file.
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
                // Get module directory path from assembly location
                // DLL is in: [GamePath]\Modules\BannerWand\bin\Win64_Shipping_Client\BannerWand.dll
                // Module path is: [GamePath]\Modules\BannerWand\
                System.Reflection.Assembly executingAssembly = System.Reflection.Assembly.GetExecutingAssembly();
                string? assemblyLocation = executingAssembly.Location;

                if (string.IsNullOrEmpty(assemblyLocation))
                {
                    TaleWorlds.Library.Debug.Print("[BannerWand] LogPathResolver: Assembly location is empty, using fallback");
                    _resolvedPath = GetFallbackPath();
                    return _resolvedPath;
                }

                // Get directory of DLL and navigate up to module root
                // From: ...\Modules\BannerWand\bin\Win64_Shipping_Client\BannerWand.dll
                // To:   ...\Modules\BannerWand\
                string? dllDirectory = Path.GetDirectoryName(assemblyLocation);
                if (string.IsNullOrEmpty(dllDirectory))
                {
                    TaleWorlds.Library.Debug.Print("[BannerWand] LogPathResolver: DLL directory is empty, using fallback");
                    _resolvedPath = GetFallbackPath();
                    return _resolvedPath;
                }

                // Navigate up: bin\Win64_Shipping_Client -> bin -> BannerWand
                string? moduleDirectory = Path.GetDirectoryName(Path.GetDirectoryName(dllDirectory));
                if (string.IsNullOrEmpty(moduleDirectory))
                {
                    TaleWorlds.Library.Debug.Print("[BannerWand] LogPathResolver: Module directory is empty, using fallback");
                    _resolvedPath = GetFallbackPath();
                    return _resolvedPath;
                }

                // Create logs directory in module folder
                string logDirectory = Path.Combine(moduleDirectory, "logs");

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

                string logPath = Path.Combine(logDirectory, LogConstants.LogFileName);

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
                return Path.Combine(Directory.GetCurrentDirectory(), LogConstants.LogFileName);
            }
            catch
            {
                return null;
            }
        }
    }
}

