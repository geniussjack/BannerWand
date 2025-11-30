#nullable enable
using System;
using System.Runtime.CompilerServices;

namespace BannerWandRetro.Interfaces
{
    /// <summary>
    /// Defines the contract for logging functionality in BannerWand mod.
    /// Provides multi-level logging with performance tracking and caller information.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface abstracts the logging system, allowing for different implementations
    /// such as file logging, console logging, or mock logging for testing purposes.
    /// </para>
    /// <para>
    /// Log levels supported: DEBUG, INFO, WARN, ERROR, CHEAT, PATCH, PERFORMANCE
    /// </para>
    /// <para>
    /// Implementations should be thread-safe to handle concurrent logging scenarios.
    /// </para>
    /// <para>
    /// See <see cref="Utils.ModLogger"/> for the default static implementation.
    /// </para>
    /// </remarks>
    public interface IModLogger
    {
        /// <summary>
        /// Initializes the logger with the correct file path and configuration.
        /// </summary>
        /// <remarks>
        /// Should be called once during mod initialization before any logging operations.
        /// Implementations should handle repeated calls gracefully (no-op if already initialized).
        /// </remarks>
        void Initialize();

        /// <summary>
        /// Logs an informational message with optional caller information.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="memberName">Auto-captured calling member name.</param>
        /// <param name="sourceFilePath">Auto-captured source file path.</param>
        /// <param name="sourceLineNumber">Auto-captured source line number.</param>
        /// <remarks>
        /// Use this for general information about mod operations, state changes, and important events.
        /// </remarks>
        void Log(string message,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0);

        /// <summary>
        /// Logs a warning message with optional caller information.
        /// </summary>
        /// <param name="message">The warning message to log.</param>
        /// <param name="memberName">Auto-captured calling member name.</param>
        /// <remarks>
        /// Use this for non-critical issues that don't prevent operation but should be noted.
        /// Examples: unexpected values, fallback behaviors, deprecated features.
        /// </remarks>
        void Warning(string message, [CallerMemberName] string memberName = "");

        /// <summary>
        /// Logs an error message with exception details and stack trace.
        /// </summary>
        /// <param name="message">The error message to log.</param>
        /// <param name="exception">Optional exception to include in log.</param>
        /// <param name="memberName">Auto-captured calling member name.</param>
        /// <remarks>
        /// Use this for critical errors that prevent normal operation or cause failures.
        /// Should include exception details when available for debugging.
        /// </remarks>
        void Error(string message, Exception? exception = null, [CallerMemberName] string memberName = "");

        /// <summary>
        /// Logs a debug message with caller context.
        /// </summary>
        /// <param name="message">The debug message to log.</param>
        /// <param name="memberName">Auto-captured calling member name.</param>
        /// <param name="sourceLineNumber">Auto-captured source line number.</param>
        /// <remarks>
        /// <para>
        /// Debug messages are typically only logged in DEBUG builds.
        /// Use for detailed diagnostic information during development.
        /// </para>
        /// <para>
        /// These messages should not appear in release builds to reduce log noise.
        /// </para>
        /// </remarks>
        void Debug(string message,
            [CallerMemberName] string memberName = "",
            [CallerLineNumber] int sourceLineNumber = 0);

        /// <summary>
        /// Logs model registration with details.
        /// </summary>
        /// <param name="modelName">Name of the registered model.</param>
        /// <param name="details">Additional details about registration.</param>
        /// <remarks>
        /// Used during game initialization to track which custom models are being registered.
        /// Helps identify loading issues and conflicts with other mods.
        /// </remarks>
        void LogModelRegistration(string modelName, string details = "");

        /// <summary>
        /// Logs behavior registration with details.
        /// </summary>
        /// <param name="behaviorName">Name of the registered behavior.</param>
        /// <param name="details">Additional details about registration.</param>
        /// <remarks>
        /// Used during game initialization to track which campaign behaviors are being registered.
        /// Helps identify loading issues and execution order problems.
        /// </remarks>
        void LogBehaviorRegistration(string behaviorName, string details = "");

        /// <summary>
        /// Logs cheat activation/deactivation with detailed context.
        /// </summary>
        /// <param name="cheatName">Name of the cheat.</param>
        /// <param name="enabled">Whether cheat is enabled or disabled.</param>
        /// <param name="value">Optional value associated with cheat.</param>
        /// <param name="target">Optional target description (e.g., "player", "NPCs").</param>
        /// <remarks>
        /// Provides a standardized format for logging cheat operations.
        /// Useful for debugging cheat application and troubleshooting settings.
        /// </remarks>
        void LogCheat(string cheatName, bool enabled, object? value = null, string target = "player");

        /// <summary>
        /// Logs performance metrics for operations.
        /// </summary>
        /// <param name="operationName">Name of the operation being measured.</param>
        /// <param name="elapsedMs">Elapsed time in milliseconds.</param>
        /// <param name="itemCount">Optional count of items processed.</param>
        /// <remarks>
        /// Use to track execution time of expensive operations.
        /// Helps identify performance bottlenecks and optimize hot paths.
        /// </remarks>
        void LogPerformance(string operationName, long elapsedMs, int itemCount = 0);

        /// <summary>
        /// Starts a performance measurement scope. Use with 'using' statement.
        /// </summary>
        /// <param name="operationName">Name of the operation to measure.</param>
        /// <returns>Disposable performance scope that automatically logs elapsed time when disposed.</returns>
        /// <remarks>
        /// <para>
        /// Example usage:
        /// <code>
        /// using (logger.BeginPerformanceScope("My Operation"))
        /// {
        ///     // Your code here
        /// }
        /// // Automatically logs elapsed time when scope ends
        /// </code>
        /// </para>
        /// <para>
        /// This pattern ensures performance is always logged, even if exceptions occur.
        /// </para>
        /// </remarks>
        IDisposable BeginPerformanceScope(string operationName);

        /// <summary>
        /// Logs current state of all cheat settings for debugging and troubleshooting.
        /// </summary>
        /// <remarks>
        /// Outputs comprehensive snapshot of all cheat settings values.
        /// Useful for diagnosing configuration issues and user support.
        /// </remarks>
        void LogSettingsState();
    }
}
