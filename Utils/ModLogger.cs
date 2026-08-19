#nullable enable
// System namespaces
// Project namespaces
using BannerWand.Constants;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace BannerWand.Utils
{
    /// <summary>
    /// Advanced logging system for BannerWand mod with multi-level logging support.
    /// Writes to both game log and separate file with timestamps, stack traces, and performance metrics.
    /// Thread-safe implementation ensures log integrity in concurrent scenarios.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Log levels: DEBUG, INFO, WARN, ERROR, CHEAT, PATCH, PERFORMANCE
    /// Log file location: [MyDocuments]\[LogSubdirectory]\[LogConfigsFolderName]\[LogsFolderName]\BannerWand_yyyyMMdd.log
    /// One file per calendar day; files older than <see cref="LogConstants.LogRetentionDays"/> days are deleted
    /// automatically. Platform-independent path that doesn't depend on game installation location.
    /// Example Windows: C:\Users\&lt;user&gt;\Documents\Mount and Blade II Bannerlord\Configs\ModLogs\BannerWand_20260819.log.
    /// Redirected to the OneDrive-backed Documents folder when OneDrive Known Folder Move is enabled.
    /// </para>
    /// </remarks>
    public static class ModLogger
    {
        private static readonly object _lock = new();
        private static bool _initialized = false;

        /// <summary>
        /// Gets the log file path, determining and caching it on first access.
        /// </summary>
        private static string? LogFilePath => field ??= DetermineLogFilePath();

        /// <summary>
        /// Determines today's log file path (Documents\Mount and Blade II Bannerlord\Configs\ModLogs\BannerWand_yyyyMMdd.log),
        /// creating the directory if needed and pruning dated log files older than <see cref="LogConstants.LogRetentionDays"/>.
        /// </summary>
        private static string DetermineLogFilePath()
        {
            try
            {
                string documentsDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                string logDirectory = Path.Combine(documentsDirectory, LogConstants.LogSubdirectory, LogConstants.LogConfigsFolderName, LogConstants.LogsFolderName);

                try
                {
                    _ = Directory.CreateDirectory(logDirectory);
                }
                catch (Exception dirEx)
                {
                    TaleWorlds.Library.Debug.Print($"[BannerWand] DetermineLogFilePath: Failed to create log directory: {dirEx.Message}");
                    // Continue anyway - file creation will handle directory creation if needed
                }

                try
                {
                    PruneOldLogFiles(logDirectory);
                }
                catch (Exception pruneEx)
                {
                    TaleWorlds.Library.Debug.Print($"[BannerWand] DetermineLogFilePath: Failed to prune old log files: {pruneEx.Message}");
                }

                return Path.Combine(logDirectory, string.Format(LogConstants.LogFileNameFormat, DateTime.Now));
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[BannerWand] DetermineLogFilePath: EXCEPTION: {ex.Message}");

                // Final fallback: use current directory
                return Path.Combine(Directory.GetCurrentDirectory(), string.Format(LogConstants.LogFileNameFormat, DateTime.Now));
            }
        }

        /// <summary>
        /// Deletes dated log files (BannerWand_yyyyMMdd.log) older than <see cref="LogConstants.LogRetentionDays"/> days,
        /// so the ModLogs folder does not accumulate one file per day of play forever.
        /// A single locked/inaccessible file is skipped rather than aborting the whole pass.
        /// </summary>
        /// <param name="logDirectory">Directory to scan for dated log files.</param>
        private static void PruneOldLogFiles(string logDirectory)
        {
            DateTime cutoffDate = DateTime.Now.Date.AddDays(-LogConstants.LogRetentionDays);

            foreach (string filePath in Directory.GetFiles(logDirectory, LogConstants.LogFileSearchPattern))
            {
                try
                {
                    if (File.GetLastWriteTime(filePath).Date < cutoffDate)
                    {
                        File.Delete(filePath);
                    }
                }
#pragma warning disable RCS1075 // Intentionally silent: one locked/inaccessible file must not abort the rest of the pass.
                catch (Exception)
                {
                    // Skip this file and keep pruning the rest.
                }
#pragma warning restore RCS1075
            }
        }

        /// <summary>
        /// Initializes the logger, creating today's log file with a header if it does not exist yet.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Log location: Documents\Mount and Blade II Bannerlord\Configs\ModLogs\BannerWand_yyyyMMdd.log
        /// </para>
        /// <para>
        /// This method should be called once at mod startup. If the game is launched more than once
        /// on the same calendar day, later launches append to the already-existing file for today
        /// rather than truncating it.
        /// </para>
        /// </remarks>
        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            try
            {
                string? logPath = LogFilePath;

                if (string.IsNullOrEmpty(logPath))
                {
                    TaleWorlds.Library.Debug.Print("[BannerWand] Initialize: Failed to determine log path");
                    _initialized = true;
                    return;
                }

                lock (_lock)
                {
                    if (!File.Exists(logPath))
                    {
                        try
                        {
                            string timestamp = DateTime.Now.ToString(LogConstants.TimestampFormat);
                            string logHeader = $"{LogConstants.LogHeader} Started at {timestamp}{Environment.NewLine}Log file location: {logPath}{Environment.NewLine}";
                            File.WriteAllText(logPath, logHeader);
                        }
                        catch (Exception ex)
                        {
                            TaleWorlds.Library.Debug.Print($"[BannerWand] Initialize: Failed to write log header: {ex.Message}");
                        }
                    }
                }

                _initialized = true;
                WriteLog(LogConstants.Info, $"BannerWand logger initialized. Log file: {logPath}");
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[BannerWand] Initialize: Failed to initialize file logging: {ex.Message}. Using game log only.");

                // Still mark as initialized so we can use game log
                _initialized = true;
            }
        }

        /// <summary>
        /// Checks if debug mode is enabled in settings.
        /// </summary>
        private static bool IsDebugModeEnabled()
        {
            try
            {
                Settings.CheatSettings? settings = Settings.CheatSettings.Instance;
                return settings?.DebugMode ?? false;
            }
            catch
            {
                // If settings are not available, default to false (no debug logging)
                return false;
            }
        }

        /// <summary>
        /// Logs an informational message with optional caller information.
        /// Only logs if DebugMode is enabled, except for initialization messages.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="memberName">Auto-captured calling member name.</param>
        /// <param name="sourceFilePath">Auto-captured source file path.</param>
        /// <param name="sourceLineNumber">Auto-captured source line number.</param>
        /// <param name="forceLog">If true, logs regardless of DebugMode (for initialization messages).</param>
        public static void Log(string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0,
            bool forceLog = false)
        {
            // Always log if forceLog is true (for initialization), otherwise check DebugMode
            if (forceLog || IsDebugModeEnabled())
            {
                WriteLog(LogConstants.Info, message, memberName, sourceFilePath, sourceLineNumber);
            }
        }

        /// <summary>
        /// Logs a warning message with optional caller information.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        /// <param name="memberName">Auto-captured calling member name.</param>
        public static void Warning(string message, [CallerMemberName] string memberName = "")
        {
            string formattedMessage = $"[{memberName}] {message}";
            WriteLog(LogConstants.Warning, formattedMessage);
        }

        /// <summary>
        /// Logs an error message with exception details and stack trace.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        /// <param name="exception">Optional exception to include in log.</param>
        /// <param name="memberName">Auto-captured calling member name.</param>
        public static void Error(string message, Exception? exception = null, [CallerMemberName] string memberName = "")
        {
            string fullMessage = exception != null
                ? $"[{memberName}] {message}\n  Exception: {exception.GetType().Name}: {exception.Message}\n  StackTrace: {exception.StackTrace}"
                : $"[{memberName}] {message}";
            WriteLog(LogConstants.Error, fullMessage);
        }

        /// <summary>
        /// Logs a debug message with caller context.
        /// Only logs if DebugMode is enabled in settings.
        /// </summary>
        /// <param name="message">The debug message to log.</param>
        /// <param name="memberName">Auto-captured calling member name.</param>
        /// <param name="sourceLineNumber">Auto-captured source line number.</param>
        public static void Debug(string message,
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (IsDebugModeEnabled())
            {
                string formattedMessage = $"[{memberName}:{sourceLineNumber}] {message}";
                WriteLog(LogConstants.Debug, formattedMessage, memberName, string.Empty, sourceLineNumber);
            }
        }

        #region Throttled Logging - Performance Optimization

        /// <summary>
        /// Tracks last log time for each throttled message key.
        /// Key format: "methodName:message" to allow different throttle intervals per message.
        /// </summary>
        private static readonly System.Collections.Generic.Dictionary<string, DateTime> _lastLogTimes =
            new(capacity: 50);

        /// <summary>
        /// Default throttle interval for debug messages (1 second).
        /// Prevents log spam when methods are called frequently (e.g., every frame/tick).
        /// </summary>
        private static readonly TimeSpan _defaultThrottleInterval = TimeSpan.FromSeconds(1);

        /// <summary>
        /// Logs a debug message with throttling to prevent spam.
        /// Only logs if DebugMode is enabled AND enough time has passed since last log.
        /// </summary>
        /// <param name="message">The debug message to log.</param>
        /// <param name="throttleInterval">Minimum time between logs. Defaults to 1 second.</param>
        /// <param name="memberName">Auto-captured calling member name.</param>
        /// <param name="sourceLineNumber">Auto-captured source line number.</param>
        /// <remarks>
        /// Use this for debug messages in hot paths (OnTick, Update, etc.) to avoid log spam.
        /// Each unique combination of memberName+message has its own throttle timer.
        /// </remarks>
        public static void DebugThrottled(
            string message,
            TimeSpan? throttleInterval = null,
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            if (!IsDebugModeEnabled())
            {
                return;
            }

            TimeSpan interval = throttleInterval ?? _defaultThrottleInterval;
            string key = $"{memberName}:{message}";
            DateTime now = DateTime.UtcNow;

            lock (_lastLogTimes)
            {
                if (_lastLogTimes.TryGetValue(key, out DateTime lastLogTime))
                {
                    if (now - lastLogTime < interval)
                    {
                        return; // Throttled - skip this log
                    }
                }

                // Update last log time
                _lastLogTimes[key] = now;
            }

            // Log the message
            string formattedMessage = $"[{memberName}:{sourceLineNumber}] {message}";
            WriteLog(LogConstants.Debug, formattedMessage, memberName, string.Empty, sourceLineNumber);
        }

        /// <summary>
        /// Logs an info message with throttling to prevent spam.
        /// </summary>
        /// <param name="message">The info message to log.</param>
        /// <param name="throttleInterval">Minimum time between logs. Defaults to 1 second.</param>
        /// <param name="memberName">Auto-captured calling member name.</param>
        /// <remarks>
        /// Use this for info messages that may be logged frequently.
        /// </remarks>
        public static void LogThrottled(
            string message,
            TimeSpan? throttleInterval = null,
            [CallerMemberName] string memberName = "")
        {
            TimeSpan interval = throttleInterval ?? _defaultThrottleInterval;
            string key = $"{memberName}:{message}";
            DateTime now = DateTime.UtcNow;

            lock (_lastLogTimes)
            {
                if (_lastLogTimes.TryGetValue(key, out DateTime lastLogTime))
                {
                    if (now - lastLogTime < interval)
                    {
                        return; // Throttled - skip this log
                    }
                }

                // Update last log time
                _lastLogTimes[key] = now;
            }

            // Log the message
            WriteLog(LogConstants.Info, message);
        }

        #endregion

        /// <summary>
        /// Logs model registration with details.
        /// Always logs (initialization message).
        /// </summary>
        /// <param name="modelName">Name of the registered model.</param>
        /// <param name="details">Additional details about registration.</param>
        public static void LogModelRegistration(string modelName, string details = "")
        {
            string info = string.IsNullOrEmpty(details) ? string.Empty : $" - {details}";
            string message = $"{modelName} registered{info}";
            WriteLog(LogConstants.Model, message);
        }

        /// <summary>
        /// Logs behavior registration with details.
        /// Always logs (initialization message).
        /// </summary>
        /// <param name="behaviorName">Name of the registered behavior.</param>
        /// <param name="details">Additional details about registration.</param>
        public static void LogBehaviorRegistration(string behaviorName, string details = "")
        {
            string info = string.IsNullOrEmpty(details) ? string.Empty : $" - {details}";
            string message = $"{behaviorName} registered{info}";
            WriteLog(LogConstants.Behavior, message);
        }

        /// <summary>
        /// Logs cheat activation/deactivation with detailed context.
        /// Only logs if DebugMode is enabled.
        /// </summary>
        /// <param name="cheatName">Name of the cheat.</param>
        /// <param name="enabled">Whether cheat is enabled or disabled.</param>
        /// <param name="value">Optional value associated with cheat.</param>
        /// <param name="target">Optional target description.</param>
        public static void LogCheat(string cheatName, bool enabled, object? value = null, string target = "player")
        {
            if (!IsDebugModeEnabled())
            {
                return;
            }

            string status = enabled ? Constants.MessageConstants.CheatStatusEnabled : Constants.MessageConstants.CheatStatusDisabled;
            string valueInfo = value != null ? $" (value: {value})" : string.Empty;
            string targetInfo = !string.IsNullOrEmpty(target) ? $" for {target}" : string.Empty;
            string message = $"{cheatName} {status}{valueInfo}{targetInfo}";
            WriteLog(LogConstants.Cheat, message);
        }

        /// <summary>
        /// Logs performance metrics for operations.
        /// Only logs if DebugMode is enabled.
        /// </summary>
        /// <param name="operationName">Name of the operation being measured.</param>
        /// <param name="elapsedMs">Elapsed time in milliseconds.</param>
        /// <param name="itemCount">Optional count of items processed.</param>
        public static void LogPerformance(string operationName, long elapsedMs, int itemCount = 0)
        {
            if (!IsDebugModeEnabled())
            {
                return;
            }

            string countInfo = string.Empty;
            if (itemCount > 0)
            {
                double msPerItem = (double)elapsedMs / itemCount;
                countInfo = $" ({itemCount} items, {msPerItem:F2}ms/item)";
            }

            string message = $"{operationName} completed in {elapsedMs}ms{countInfo}";
            WriteLog(LogConstants.Performance, message);
        }

        /// <summary>
        /// Starts a performance measurement scope. Use with 'using' statement.
        /// </summary>
        /// <param name="operationName">Name of the operation to measure.</param>
        /// <returns>Disposable performance scope.</returns>
        public static IDisposable BeginPerformanceScope(string operationName)
        {
            return new PerformanceScope(operationName);
        }

        /// <summary>
        /// Performance measurement scope for automatic timing.
        /// </summary>
        /// <remarks>
        /// Initializes a new instance of the <see cref="PerformanceScope"/> class.
        /// </remarks>
        /// <param name="operationName">Name of the operation to measure.</param>
        private class PerformanceScope(string operationName) : IDisposable
        {
            private readonly string _operationName = operationName;
            private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

            /// <summary>
            /// Stops the timer and logs the performance metrics.
            /// </summary>
            public void Dispose()
            {
                _stopwatch.Stop();
                LogPerformance(_operationName, _stopwatch.ElapsedMilliseconds);
            }
        }

        /// <summary>
        /// Writes a log entry to both file and game log with optional caller information.
        /// </summary>
        /// <param name="level">Log level (INFO, WARN, ERROR, etc.).</param>
        /// <param name="message">Message to log.</param>
        /// <param name="memberName">Optional caller member name.</param>
        /// <param name="sourceFilePath">Optional source file path.</param>
        /// <param name="sourceLineNumber">Optional source line number.</param>
        private static void WriteLog(string level, string message, string memberName = "", string sourceFilePath = "", int sourceLineNumber = 0)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                string callerInfo = string.Empty;

                // Build caller info if available
                if (!string.IsNullOrEmpty(memberName) && sourceLineNumber > 0)
                {
                    string fileName = Path.GetFileNameWithoutExtension(sourceFilePath);
                    callerInfo = $" [{fileName}::{memberName}:{sourceLineNumber}]";
                }

                // Build formatted message
                string formattedMessage = $"[{timestamp}] [{level}]{callerInfo} {message}";

                // Validate message is not null before processing
                if (string.IsNullOrEmpty(message))
                {
                    return;
                }

                // Write to file (try-catch to prevent failures from breaking logging)
                if (_initialized)
                {
                    try
                    {
                        string? logPath = LogFilePath;
                        if (string.IsNullOrEmpty(logPath))
                        {
                            TaleWorlds.Library.Debug.Print("[BannerWand] WriteLog: Log path is null, cannot write to file");
                            return;
                        }

                        lock (_lock)
                        {
                            File.AppendAllText(logPath, formattedMessage + Environment.NewLine);
                        }
                    }
                    catch (Exception ex)
                    {
                        // File write failed, log to debug output only
                        TaleWorlds.Library.Debug.Print($"[BannerWand] WriteLog: ✗ Failed to write to log file: {ex.Message}");
                        TaleWorlds.Library.Debug.Print($"[BannerWand] WriteLog: Exception type: {ex.GetType().Name}");
                    }
                }
                else
                {
                    TaleWorlds.Library.Debug.Print($"[BannerWand] WriteLog: Logger not initialized! Message: {message}");
                }

                // All logs go to file only - no chat messages
            }
            catch
            {
                // Silently fail if logging doesn't work
            }
        }

        /// <summary>
        /// Logs current state of all cheat settings for debugging and troubleshooting.
        /// </summary>
        /// <remarks>
        /// Outputs all cheat settings values to help diagnose configuration issues.
        /// </remarks>
        public static void LogSettingsState()
        {
            try
            {
                Settings.CheatSettings? settings = Settings.CheatSettings.Instance;
                Settings.CheatTargetSettings? targetSettings = Settings.CheatTargetSettings.Instance;

                if (settings == null || targetSettings == null)
                {
                    Warning("Settings are null, cannot log state");
                    return;
                }

                Log("=== Current Cheat Settings State ===", forceLog: true);
                Log($"Apply to Player: {targetSettings.ApplyToPlayer}", forceLog: true);

                // Player cheats
                Log($"Unlimited Health: {settings.UnlimitedHealth}", forceLog: true);
                Log($"Unlimited Horse Health: {settings.UnlimitedHorseHealth}", forceLog: true);
                Log($"Unlimited Shield Durability: {settings.UnlimitedShieldDurability}", forceLog: true);
                Log($"Max Morale: {settings.MaxMorale}", forceLog: true);
                Log($"Movement Speed: {settings.MovementSpeed}", forceLog: true);

                // Inventory cheats
                Log($"Edit Gold: {settings.EditGold}", forceLog: true);
                Log($"Edit Influence: {settings.EditInfluence}", forceLog: true);
                Log($"Unlimited Food: {settings.UnlimitedFood}", forceLog: true);
                Log($"Max Carrying Capacity: {settings.MaxCarryingCapacity}", forceLog: true);

                // Stats cheats
                Log($"Unlimited Skill XP: {settings.UnlimitedSkillXP}", forceLog: true);
                Log($"Skill XP Multiplier: {settings.SkillXPMultiplier}", forceLog: true);
                Log($"Unlimited Troops XP: {settings.UnlimitedTroopsXP}", forceLog: true);
                Log($"Troops XP Multiplier: {settings.TroopsXPMultiplier}", forceLog: true);
                Log($"Unlimited Renown: {settings.UnlimitedRenown}", forceLog: true);
                Log($"Renown Multiplier: {settings.RenownMultiplier}", forceLog: true);

                // Enemy cheats
                Log($"One Hit Kills: {settings.OneHitKills}", forceLog: true);

                Log("=== End of Settings State ===", forceLog: true);
            }
            catch (Exception ex)
            {
                Error("Failed to log settings state", ex);
            }
        }
    }
}
