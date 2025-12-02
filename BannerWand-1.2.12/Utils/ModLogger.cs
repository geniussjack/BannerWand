#nullable enable
using BannerWandRetro.Constants;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;

namespace BannerWandRetro.Utils
{
    /// <summary>
    /// Advanced logging system for BannerWand mod with multi-level logging support.
    /// Writes to both game log and separate file with timestamps, stack traces, and performance metrics.
    /// Thread-safe implementation ensures log integrity in concurrent scenarios.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Log levels: DEBUG, INFO, WARN, ERROR, CHEAT, PATCH, PERFORMANCE
    /// Log file location: [GamePath]\Modules\BannerWand\logs\[LogFileName]
    /// Logs are created in the module directory for easy access.
    /// Example: D:\Mount &amp; Blade II Bannerlord\Modules\BannerWand\logs\BannerWandRetro.log
    /// </para>
    /// <para>
    /// This static class provides the default implementation of logging functionality.
    /// For dependency injection scenarios, use <see cref="Interfaces.IModLogger"/> interface
    /// with <see cref="ModLoggerWrapper"/> wrapper class.
    /// </para>
    /// </remarks>
    public static class ModLogger
    {
        private static readonly object _lock = new();
        private static bool _initialized = false;
        private static string? _logFilePath;

        /// <summary>
        /// Gets or sets the log file path, initializing it if necessary.
        /// </summary>
        private static string? LogFilePath
        {
            get
            {
                _logFilePath ??= DetermineLogFilePath();
                return _logFilePath;
            }
            set => _logFilePath = value;
        }

        /// <summary>
        /// Determines the module directory and returns log file path (BannerWand\logs\BannerWand.log).
        /// </summary>
        /// <returns>Full path to the log file.</returns>
        private static string DetermineLogFilePath()
        {
            TaleWorlds.Library.Debug.Print("[BannerWand] DetermineLogFilePath: Starting path determination...");

            try
            {
                // Get module directory path from assembly location
                // DLL is in: [GamePath]\Modules\BannerWand\bin\Win64_Shipping_Client\BannerWand.dll
                // Module path is: [GamePath]\Modules\BannerWand\
                System.Reflection.Assembly executingAssembly = System.Reflection.Assembly.GetExecutingAssembly();
                string? assemblyLocation = executingAssembly.Location;

                if (string.IsNullOrEmpty(assemblyLocation))
                {
                    throw new InvalidOperationException("Assembly location is empty");
                }

                // Get directory of DLL and navigate up to module root
                // From: ...\Modules\BannerWand\bin\Win64_Shipping_Client\BannerWand.dll
                // To:   ...\Modules\BannerWand\
                string? dllDirectory = Path.GetDirectoryName(assemblyLocation);
                if (string.IsNullOrEmpty(dllDirectory))
                {
                    throw new InvalidOperationException("DLL directory is empty");
                }

                // Navigate up: bin\Win64_Shipping_Client -> bin -> BannerWand
                string? moduleDirectory = Path.GetDirectoryName(Path.GetDirectoryName(dllDirectory));
                if (string.IsNullOrEmpty(moduleDirectory))
                {
                    throw new InvalidOperationException("Module directory is empty");
                }

                // Create logs directory in module folder
                string logDirectory = Path.Combine(moduleDirectory, "logs");

                // Ensure log directory exists
                try
                {
                    _ = Directory.CreateDirectory(logDirectory);
                    TaleWorlds.Library.Debug.Print($"[BannerWand] DetermineLogFilePath: Created log directory: {logDirectory}");
                }
                catch (Exception dirEx)
                {
                    TaleWorlds.Library.Debug.Print($"[BannerWand] DetermineLogFilePath: Failed to create log directory: {dirEx.Message}");
                    // Continue anyway - file creation will handle directory creation if needed
                }

                string logPath = Path.Combine(logDirectory, LogConstants.LogFileName);
                TaleWorlds.Library.Debug.Print($"[BannerWand] DetermineLogFilePath: Primary log path: {logPath}");

                // Test if we can write to this location
                try
                {
                    string testFile = logPath + ".test";
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);
                    TaleWorlds.Library.Debug.Print($"[BannerWand] DetermineLogFilePath: ✓ Write test successful for: {logPath}");

                    // Immediately create log file with initial diagnostic message
                    try
                    {
                        string initialMsg = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [DIAGNOSTIC] Log file path determined: {logPath}{Environment.NewLine}";
                        File.WriteAllText(logPath, initialMsg);
                        TaleWorlds.Library.Debug.Print("[BannerWand] DetermineLogFilePath: ✓ Initial log file created");
                    }
                    catch (Exception initEx)
                    {
                        TaleWorlds.Library.Debug.Print($"[BannerWand] DetermineLogFilePath: Could not create initial log file: {initEx.Message}");
                    }

                    return logPath;
                }
                catch (Exception writeEx)
                {
                    TaleWorlds.Library.Debug.Print($"[BannerWand] DetermineLogFilePath: ✗ Write test FAILED for {logPath}: {writeEx.Message}");
                    // Fall through to fallback logic
                }
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[BannerWand] DetermineLogFilePath: EXCEPTION in primary path: {ex.Message}");
                TaleWorlds.Library.Debug.Print($"[BannerWand] DetermineLogFilePath: StackTrace: {ex.StackTrace}");
                // Fall through to fallback logic
            }

            // Fallback: Try current directory
            try
            {
                string currentDir = Directory.GetCurrentDirectory();
                string fallbackPath = Path.Combine(currentDir, LogConstants.LogFileName);
                TaleWorlds.Library.Debug.Print($"[BannerWand] DetermineLogFilePath: Using fallback path: {fallbackPath}");
                return fallbackPath;
            }
            catch (Exception fallbackEx)
            {
                TaleWorlds.Library.Debug.Print($"[BannerWand] DetermineLogFilePath: Fallback path failed: {fallbackEx.Message}");
                // Last resort: use temp directory
                string tempPath = Path.Combine(Path.GetTempPath(), LogConstants.LogFileName);
                TaleWorlds.Library.Debug.Print($"[BannerWand] DetermineLogFilePath: Using temp directory: {tempPath}");
                return tempPath;
            }
        }

        /// <summary>
        /// Initializes the logger by clearing any existing log file and writing header.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Log location: [GamePath]\Modules\BannerWand\logs\[LogFileName]
        /// Logs are created in the module directory for easy access.
        /// </para>
        /// <para>
        /// This method should be called once at mod startup to clear old logs.
        /// The log file is created in the module's logs directory.
        /// </para>
        /// </remarks>
        public static void Initialize()
        {
            TaleWorlds.Library.Debug.Print("[BannerWand] Initialize: Starting logger initialization...");
            TaleWorlds.Library.Debug.Print($"[BannerWand] Initialize: Already initialized: {_initialized}");

            if (_initialized)
            {
                TaleWorlds.Library.Debug.Print("[BannerWand] Initialize: Already initialized, skipping");
                return;
            }

            try
            {
                TaleWorlds.Library.Debug.Print("[BannerWand] Initialize: Determining log file path...");
                // Ensure log path is determined
                string? logPath = LogFilePath;
                TaleWorlds.Library.Debug.Print($"[BannerWand] Initialize: Log path determined: {logPath}");

                // Immediately write diagnostic info to file (if possible)
                try
                {
                    string diagnosticMsg = $"[{DateTime.Now:HH:mm:ss.fff}] [DIAGNOSTIC] Logger initialization started. Log path: {logPath}{Environment.NewLine}";
                    File.AppendAllText(logPath, diagnosticMsg);
                    TaleWorlds.Library.Debug.Print("[BannerWand] Initialize: ✓ Diagnostic message written to file");
                }
                catch (Exception diagEx)
                {
                    TaleWorlds.Library.Debug.Print($"[BannerWand] Initialize: Could not write diagnostic message: {diagEx.Message}");
                }

                // Clear old log on initialization with header and timestamp
                string timestamp = DateTime.Now.ToString(LogConstants.TimestampFormat);
                string logHeader = $"{LogConstants.LogHeader} Started at {timestamp}{Environment.NewLine}Log file location: {logPath}{Environment.NewLine}";
                TaleWorlds.Library.Debug.Print($"[BannerWand] Initialize: Log header prepared, length: {logHeader.Length} chars");

                lock (_lock)
                {
                    try
                    {
                        TaleWorlds.Library.Debug.Print($"[BannerWand] Initialize: Checking if log file exists: {File.Exists(logPath)}");
                        if (File.Exists(logPath))
                        {
                            TaleWorlds.Library.Debug.Print("[BannerWand] Initialize: Deleting existing log file...");
                            File.Delete(logPath);
                            TaleWorlds.Library.Debug.Print("[BannerWand] Initialize: Existing log file deleted");
                        }

                        TaleWorlds.Library.Debug.Print($"[BannerWand] Initialize: Writing log header to: {logPath}");
                        File.WriteAllText(logPath, logHeader);
                        TaleWorlds.Library.Debug.Print("[BannerWand] Initialize: ✓ Log file created successfully!");

                        // Verify file was created
                        if (File.Exists(logPath))
                        {
                            long fileSize = new FileInfo(logPath).Length;
                            TaleWorlds.Library.Debug.Print($"[BannerWand] Initialize: ✓ Log file verified! Size: {fileSize} bytes");
                        }
                        else
                        {
                            TaleWorlds.Library.Debug.Print("[BannerWand] Initialize: ✗ WARNING: Log file does not exist after creation!");
                        }
                    }
                    catch (Exception ex)
                    {
                        TaleWorlds.Library.Debug.Print($"[BannerWand] Initialize: ✗ EXCEPTION writing to log file: {ex.Message}");
                        TaleWorlds.Library.Debug.Print($"[BannerWand] Initialize: Exception type: {ex.GetType().Name}");
                        TaleWorlds.Library.Debug.Print($"[BannerWand] Initialize: StackTrace: {ex.StackTrace}");

                        // If we can't write to the determined path, try to find another
                        string errorMsg = $"Failed to write to log file {logPath}: {ex.Message}. Trying alternative path...";
                        TaleWorlds.Library.Debug.Print($"[BannerWand] Initialize: {errorMsg}");

                        // Error logged to file only

                        // Force re-determination of path
                        LogFilePath = null;
                        logPath = LogFilePath;

                        // Validate that logPath is not null before using it
                        if (string.IsNullOrEmpty(logPath))
                        {
                            TaleWorlds.Library.Debug.Print("[BannerWand] Initialize: ✗ Failed to determine log path after retry");
                            // Still mark as initialized so we can use game log only
                            _initialized = true;
                            return;
                        }

                        TaleWorlds.Library.Debug.Print($"[BannerWand] Initialize: Retrying with new path: {logPath}");

                        // Try again with new path
                        if (File.Exists(logPath))
                        {
                            File.Delete(logPath);
                        }
                        File.WriteAllText(logPath, logHeader);
                        TaleWorlds.Library.Debug.Print("[BannerWand] Initialize: ✓ Retry successful!");
                    }
                }

                _initialized = true;
                TaleWorlds.Library.Debug.Print("[BannerWand] Initialize: Logger marked as initialized");

                // Log initialization to file only
                string initMessage = $"BannerWand logger initialized. Log file: {logPath}";
                TaleWorlds.Library.Debug.Print($"[BannerWand] Initialize: Writing initial log message: {initMessage}");
                WriteLog(LogConstants.Info, initMessage);

                TaleWorlds.Library.Debug.Print("[BannerWand] Initialize: ✓ Initialization complete!");
            }
            catch (Exception ex)
            {
                TaleWorlds.Library.Debug.Print($"[BannerWand] Initialize: ✗ FATAL EXCEPTION: {ex.Message}");
                TaleWorlds.Library.Debug.Print($"[BannerWand] Initialize: Exception type: {ex.GetType().Name}");
                TaleWorlds.Library.Debug.Print($"[BannerWand] Initialize: StackTrace: {ex.StackTrace}");

                // Fallback to game log only if file logging fails (error logged to debug output only)
                string errorMessage = $"BannerWand: Failed to initialize file logging: {ex.Message}. Using game log only.";
                TaleWorlds.Library.Debug.Print($"[BannerWand] Initialize: {errorMessage}");

                // Still mark as initialized so we can use game log
                _initialized = true;
                TaleWorlds.Library.Debug.Print("[BannerWand] Initialize: Marked as initialized (game log only mode)");
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
            WriteLog(LogConstants.Model, message);
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
            WriteLog(LogConstants.Behavior, message);
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
            WriteLog(LogConstants.Cheat, message);
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

                // Build formatted message with padding for level alignment
                string paddedLevel = level.PadRight(0);
                string formattedMessage = $"[{timestamp}] [{paddedLevel}]{callerInfo} {message}";

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
