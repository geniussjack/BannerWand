#nullable enable
using BannerWandRetro.Interfaces;
using System;
using System.Runtime.CompilerServices;

namespace BannerWandRetro.Utils
{
    /// <summary>
    /// Wrapper class that implements <see cref="IModLogger"/> and delegates to the static <see cref="ModLogger"/> class.
    /// Enables dependency injection and testability while maintaining backward compatibility with existing static usage.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This wrapper allows the logging system to be injected as a dependency, which is useful for:
    /// - Unit testing with mock loggers
    /// - Dependency injection containers
    /// - Alternative logging implementations
    /// </para>
    /// <para>
    /// All method calls are forwarded directly to the static <see cref="ModLogger"/> implementation,
    /// ensuring consistent behavior regardless of how the logger is accessed.
    /// </para>
    /// <para>
    /// Usage example:
    /// <code>
    /// IModLogger logger = new ModLoggerWrapper();
    /// logger.Log("Message from wrapper");
    /// </code>
    /// </para>
    /// </remarks>
    public class ModLoggerWrapper : IModLogger
    {
        /// <summary>
        /// Initializes the logger with the correct file path and configuration.
        /// </summary>
        /// <remarks>
        /// Delegates to <see cref="ModLogger.Initialize"/>.
        /// </remarks>
        public void Initialize()
        {
            try
            {
                ModLogger.Initialize();
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[ModLoggerWrapper] Error in Initialize: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }


        /// <summary>
        /// Logs an informational message with optional caller information.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="memberName">Auto-captured calling member name.</param>
        /// <param name="sourceFilePath">Auto-captured source file path.</param>
        /// <param name="sourceLineNumber">Auto-captured source line number.</param>
        /// <remarks>
        /// Delegates to <see cref="ModLogger.Log"/>.
        /// </remarks>
        public void Log(string message,
                [CallerMemberName] string memberName = "",
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int sourceLineNumber = 0)
        {
            try
            {
                ModLogger.Log(message, memberName, sourceFilePath, sourceLineNumber);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[ModLoggerWrapper] Error in Log: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Logs a warning message with optional caller information.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        /// <param name="memberName">Auto-captured calling member name.</param>
        /// <remarks>
        /// Delegates to <see cref="ModLogger.Warning"/>.
        /// </remarks>
        public void Warning(string message, [CallerMemberName] string memberName = "")
        {
            try
            {
                ModLogger.Warning(message, memberName);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[ModLoggerWrapper] Error in Warning: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Logs an error message with exception details and stack trace.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        /// <param name="exception">Optional exception to include in log.</param>
        /// <param name="memberName">Auto-captured calling member name.</param>
        /// <remarks>
        /// Delegates to <see cref="ModLogger.Error"/>.
        /// </remarks>
        public void Error(string message, Exception? exception = null, [CallerMemberName] string memberName = "")
        {
            try
            {
                ModLogger.Error(message, exception, memberName);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[ModLoggerWrapper] Error in Error: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Logs a debug message with caller context.
        /// </summary>
        /// <param name="message">The debug message to log.</param>
        /// <param name="memberName">Auto-captured calling member name.</param>
        /// <param name="sourceLineNumber">Auto-captured source line number.</param>
        /// <remarks>
        /// Delegates to <see cref="ModLogger.Debug"/>.
        /// </remarks>
        public void Debug(string message,
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            try
            {
                ModLogger.Debug(message, memberName, sourceLineNumber);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[ModLoggerWrapper] Error in Debug: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Logs model registration with details.
        /// </summary>
        /// <param name="modelName">Name of the registered model.</param>
        /// <param name="details">Additional details about registration.</param>
        /// <remarks>
        /// Delegates to <see cref="ModLogger.LogModelRegistration"/>.
        /// </remarks>
        public void LogModelRegistration(string modelName, string details = "")
        {
            try
            {
                ModLogger.LogModelRegistration(modelName, details);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[ModLoggerWrapper] Error in LogModelRegistration: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Logs behavior registration with details.
        /// </summary>
        /// <param name="behaviorName">Name of the registered behavior.</param>
        /// <param name="details">Additional details about registration.</param>
        /// <remarks>
        /// Delegates to <see cref="ModLogger.LogBehaviorRegistration"/>.
        /// </remarks>
        public void LogBehaviorRegistration(string behaviorName, string details = "")
        {
            try
            {
                ModLogger.LogBehaviorRegistration(behaviorName, details);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[ModLoggerWrapper] Error in LogBehaviorRegistration: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Logs cheat activation/deactivation with detailed context.
        /// </summary>
        /// <param name="cheatName">Name of the cheat.</param>
        /// <param name="enabled">Whether cheat is enabled or disabled.</param>
        /// <param name="value">Optional value associated with cheat.</param>
        /// <param name="target">Optional target description.</param>
        /// <remarks>
        /// Delegates to <see cref="ModLogger.LogCheat"/>.
        /// </remarks>
        public void LogCheat(string cheatName, bool enabled, object? value = null, string target = "player")
        {
            try
            {
                ModLogger.LogCheat(cheatName, enabled, value, target);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[ModLoggerWrapper] Error in LogCheat: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Logs performance metrics for operations.
        /// </summary>
        /// <param name="operationName">Name of the operation being measured.</param>
        /// <param name="elapsedMs">Elapsed time in milliseconds.</param>
        /// <param name="itemCount">Optional count of items processed.</param>
        /// <remarks>
        /// Delegates to <see cref="ModLogger.LogPerformance"/>.
        /// </remarks>
        public void LogPerformance(string operationName, long elapsedMs, int itemCount = 0)
        {
            try
            {
                ModLogger.LogPerformance(operationName, elapsedMs, itemCount);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[ModLoggerWrapper] Error in LogPerformance: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Starts a performance measurement scope. Use with 'using' statement.
        /// </summary>
        /// <param name="operationName">Name of the operation to measure.</param>
        /// <returns>Disposable performance scope.</returns>
        /// <remarks>
        /// Delegates to <see cref="ModLogger.BeginPerformanceScope"/>.
        /// </remarks>
        public IDisposable BeginPerformanceScope(string operationName)
        {
            try
            {
                return ModLogger.BeginPerformanceScope(operationName);
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[ModLoggerWrapper] Error in BeginPerformanceScope: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
                // Return empty disposable to prevent NullReferenceException when using 'using' statement
                return new EmptyDisposable();
            }
        }

        /// <summary>
        /// Empty disposable implementation for error fallback.
        /// </summary>
        private class EmptyDisposable : IDisposable
        {
            public void Dispose() { }
        }

        /// <summary>
        /// Logs current state of all cheat settings for debugging and troubleshooting.
        /// </summary>
        /// <remarks>
        /// Delegates to <see cref="ModLogger.LogSettingsState"/>.
        /// </remarks>
        public void LogSettingsState()
        {
            ModLogger.LogSettingsState();
        }
    }
}
