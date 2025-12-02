#nullable enable
using BannerWand.Interfaces;
using System;
using System.IO;

namespace BannerWand.Utils
{
    /// <summary>
    /// Default implementation of <see cref="ILogWriter"/> that writes log messages to files.
    /// Provides thread-safe file writing operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This implementation:
    /// - Uses File.AppendAllText for appending log messages
    /// - Uses File.WriteAllText for writing headers
    /// - Uses File.Delete for clearing logs
    /// - Is thread-safe using a lock object
    /// </para>
    /// <para>
    /// All file operations are wrapped in try-catch blocks to prevent exceptions
    /// from interrupting the application flow.
    /// </para>
    /// </remarks>
    internal class LogWriter : ILogWriter
    {
        private readonly object _writeLock = new();

        /// <summary>
        /// Writes a log message to the log file.
        /// </summary>
        /// <param name="logPath">The full path to the log file.</param>
        /// <param name="message">The formatted log message to write.</param>
        public void WriteLog(string logPath, string message)
        {
            if (string.IsNullOrEmpty(logPath))
            {
                TaleWorlds.Library.Debug.Print("[BannerWand] LogWriter: Log path is null or empty, cannot write");
                return;
            }

            try
            {
                lock (_writeLock)
                {
                    File.AppendAllText(logPath, message + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                // File write failed, log to debug output only
                TaleWorlds.Library.Debug.Print($"[BannerWand] LogWriter: Failed to write to log file: {ex.Message}");
            }
        }

        /// <summary>
        /// Writes an initial header to a new log file.
        /// </summary>
        /// <param name="logPath">The full path to the log file.</param>
        /// <param name="header">The header text to write at the start of the log file.</param>
        public void WriteHeader(string logPath, string header)
        {
            if (string.IsNullOrEmpty(logPath))
            {
                TaleWorlds.Library.Debug.Print("[BannerWand] LogWriter: Log path is null or empty, cannot write header");
                return;
            }

            try
            {
                lock (_writeLock)
                {
                    File.WriteAllText(logPath, header);
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[BannerWand] LogWriter: Failed to write header: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears the log file, removing all existing content.
        /// </summary>
        /// <param name="logPath">The full path to the log file to clear.</param>
        public void ClearLog(string logPath)
        {
            if (string.IsNullOrEmpty(logPath))
            {
                return;
            }

            try
            {
                lock (_writeLock)
                {
                    if (File.Exists(logPath))
                    {
                        File.Delete(logPath);
                    }
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[BannerWand] LogWriter: Failed to clear log file: {ex.Message}");
            }
        }
    }
}

