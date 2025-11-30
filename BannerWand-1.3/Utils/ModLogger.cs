#nullable enable
using BannerWand.Constants;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using TaleWorlds.Library;

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
    /// Log file location: Documents\Mount and Blade II Bannerlord\logs\BannerWand.log
    /// </para>
    /// <para>
    /// This static class provides the default implementation of logging functionality.
    /// For dependency injection scenarios, use <see cref="Interfaces.IModLogger"/> interface
    /// with <see cref="ModLoggerWrapper"/> wrapper class.
    /// </para>
    /// </remarks>
    public static class ModLogger
    {
        private static string? _logFilePath;
        private static readonly object _lock = new();
        private static bool _initialized = false;

        /// <summary>
        /// Initializes the logger with the correct file path and clears any existing log file.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Log location: C:\ProgramData\Mount and Blade II Bannerlord\logs\BannerWand.log
        /// </para>
        /// <para>
        /// If initialization fails, falls back to game log only with error message.
        /// </para>
        /// </remarks>
        public static void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            {
                try
                {
                    // Use C:\ProgramData\Mount and Blade II Bannerlord\logs for consistency
                    string programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
                    string bannerlordPath = Path.Combine(programDataPath, "Mount and Blade II Bannerlord", "logs");

                    // Ensure directory exists
                    if (!Directory.Exists(bannerlordPath))
                    {
                        _ = Directory.CreateDirectory(bannerlordPath);
                    }

                    _logFilePath = Path.Combine(bannerlordPath, LogConstants.LogFileName);

                    // Clear old log on initialization with header and timestamp
                    string timestamp = DateTime.Now.ToString(LogConstants.TimestampFormat);
                    string logHeader = $"{LogConstants.LogHeader} Started at {timestamp}{Environment.NewLine}";
                    File.WriteAllText(_logFilePath, logHeader);

                    _initialized = true;
                    Log(LogConstants.LoggerInitialized);
                }
                catch (Exception ex)
                {
                    // Fallback to game log only if file logging fails
                    string errorMessage = string.Format(LogConstants.LoggerInitErrorFormat, ex.Message);
                    InformationManager.DisplayMessage(new InformationMessage(errorMessage, GameConstants.ErrorColor));
                }
            }
        }

        /// <summary>
        /// Logs an informational message with optional caller information.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="memberName">Auto-captured calling member name.</param>
        /// <param name="sourceFilePath">Auto-captured source file path.</param>
        /// <param name="sourceLineNumber">Auto-captured source line number.</param>
        public static void Log(string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            WriteLog(LogConstants.Info, message, memberName, sourceFilePath, sourceLineNumber);
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
        /// Logs a debug message (only in debug builds) with caller context.
        /// </summary>
        /// <param name="message">The debug message to log.</param>
        /// <param name="memberName">Auto-captured calling member name.</param>
        /// <param name="sourceLineNumber">Auto-captured source line number.</param>
        public static void Debug(string message,
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
#if DEBUG
            string formattedMessage = $"[{memberName}:{sourceLineNumber}] {message}";
            WriteLog(LogConstants.Debug, formattedMessage, memberName, string.Empty, sourceLineNumber);
#else
            _ = message;
            _ = memberName;
            _ = sourceLineNumber;
#endif
        }

        /// <summary>
        /// Logs model registration with details.
        /// </summary>
        /// <param name="modelName">Name of the registered model.</param>
        /// <param name="details">Additional details about registration.</param>
        public static void LogModelRegistration(string modelName, string details = "")
        {
            string info = string.IsNullOrEmpty(details) ? string.Empty : $" - {details}";
            string message = $"{modelName} registered{info}";
            WriteLog("MODEL", message);
        }

        /// <summary>
        /// Logs behavior registration with details.
        /// </summary>
        /// <param name="behaviorName">Name of the registered behavior.</param>
        /// <param name="details">Additional details about registration.</param>
        public static void LogBehaviorRegistration(string behaviorName, string details = "")
        {
            string info = string.IsNullOrEmpty(details) ? string.Empty : $" - {details}";
            string message = $"{behaviorName} registered{info}";
            WriteLog("BEHAVIOR", message);
        }

        /// <summary>
        /// Logs cheat activation/deactivation with detailed context.
        /// </summary>
        /// <param name="cheatName">Name of the cheat.</param>
        /// <param name="enabled">Whether cheat is enabled or disabled.</param>
        /// <param name="value">Optional value associated with cheat.</param>
        /// <param name="target">Optional target description.</param>
        public static void LogCheat(string cheatName, bool enabled, object? value = null, string target = "player")
        {
            string status = enabled ? "ENABLED" : "DISABLED";
            string valueInfo = value != null ? $" (value: {value})" : string.Empty;
            string targetInfo = !string.IsNullOrEmpty(target) ? $" for {target}" : string.Empty;
            string message = $"{cheatName} {status}{valueInfo}{targetInfo}";
            WriteLog("CHEAT", message);
        }

        /// <summary>
        /// Logs performance metrics for operations.
        /// </summary>
        /// <param name="operationName">Name of the operation being measured.</param>
        /// <param name="elapsedMs">Elapsed time in milliseconds.</param>
        /// <param name="itemCount">Optional count of items processed.</param>
        public static void LogPerformance(string operationName, long elapsedMs, int itemCount = 0)
        {
            string countInfo = string.Empty;
            if (itemCount > 0)
            {
                double msPerItem = (double)elapsedMs / itemCount;
                countInfo = $" ({itemCount} items, {msPerItem:F2}ms/item)";
            }

            string message = $"{operationName} completed in {elapsedMs}ms{countInfo}";
            WriteLog("PERF", message);
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

                // Build formatted message with padding for level alignment
                string paddedLevel = level.PadRight(0);
                string formattedMessage = $"[{timestamp}] [{paddedLevel}]{callerInfo} {message}";

                // Write to file
                if (_initialized && !string.IsNullOrEmpty(_logFilePath))
                {
                    lock (_lock)
                    {
                        File.AppendAllText(_logFilePath, formattedMessage + Environment.NewLine);
                    }
                }

                // Also write to game log for critical messages
                if (level is LogConstants.Error or LogConstants.Warning)
                {
                    Color color = level == LogConstants.Error ? GameConstants.ErrorColor : GameConstants.WarningColor;
                    string gameMessage = $"BannerWand: {message}";
                    InformationManager.DisplayMessage(new InformationMessage(gameMessage, color));
                }
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
                Settings.CheatSettings settings = Settings.CheatSettings.Instance!;
                Settings.CheatTargetSettings targetSettings = Settings.CheatTargetSettings.Instance!;

                if (settings == null || targetSettings == null)
                {
                    Warning("Settings are null, cannot log state");
                    return;
                }

                Log("=== Current Cheat Settings State ===");
                Log($"Apply to Player: {targetSettings.ApplyToPlayer}");

                // Player cheats
                Log($"Unlimited Health: {settings.UnlimitedHealth}");
                Log($"Unlimited Horse Health: {settings.UnlimitedHorseHealth}");
                Log($"Unlimited Shield Durability: {settings.UnlimitedShieldDurability}");
                Log($"Max Morale: {settings.MaxMorale}");
                Log($"Movement Speed: {settings.MovementSpeed}");

                // Inventory cheats
                Log($"Edit Gold: {settings.EditGold}");
                Log($"Edit Influence: {settings.EditInfluence}");
                Log($"Unlimited Food: {settings.UnlimitedFood}");
                Log($"Max Carrying Capacity: {settings.MaxCarryingCapacity}");

                // Stats cheats
                Log($"Unlimited Skill XP: {settings.UnlimitedSkillXP}");
                Log($"Skill XP Multiplier: {settings.SkillXPMultiplier}");
                Log($"Unlimited Troops XP: {settings.UnlimitedTroopsXP}");
                Log($"Troops XP Multiplier: {settings.TroopsXPMultiplier}");
                Log($"Unlimited Renown: {settings.UnlimitedRenown}");
                Log($"Renown Multiplier: {settings.RenownMultiplier}");

                // Enemy cheats
                Log($"One Hit Kills: {settings.OneHitKills}");
                Log($"Slow AI Movement Speed: {settings.SlowAIMovementSpeed}");

                Log("=== End of Settings State ===");
            }
            catch (Exception ex)
            {
                Error("Failed to log settings state", ex);
            }
        }
    }
}
