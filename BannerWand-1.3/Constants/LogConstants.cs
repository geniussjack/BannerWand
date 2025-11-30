namespace BannerWand.Constants
{
    /// <summary>
    /// Constants for logging levels and log file management.
    /// </summary>
    /// <remarks>
    /// Defines standard logging levels and file paths used throughout the mod.
    /// </remarks>
    public static class LogConstants
    {
        #region Log Levels

        /// <summary>
        /// Log level for error messages.
        /// </summary>
        public const string Error = "ERROR";

        /// <summary>
        /// Log level for warning messages.
        /// </summary>
        public const string Warning = "WARNING";

        /// <summary>
        /// Log level for informational messages.
        /// </summary>
        public const string Info = "INFO";

        /// <summary>
        /// Log level for debug messages.
        /// </summary>
        public const string Debug = "DEBUG";

        #endregion

        #region Log File Configuration

        /// <summary>
        /// Name of the log file.
        /// </summary>
        public const string LogFileName = "BannerWand.log";

        /// <summary>
        /// Base directory for Bannerlord logs.
        /// </summary>
        /// <remarks>
        /// Platform-independent path using forward slashes.
        /// </remarks>
        public const string LogDirectory = "C:/ProgramData/Mount and Blade II Bannerlord/logs";

        /// <summary>
        /// Maximum size of log file before rotation (in bytes).
        /// </summary>
        /// <remarks>
        /// Set to 10MB (10 * 1024 * 1024 bytes).
        /// When exceeded, old log is renamed and new log is created.
        /// </remarks>
        public const long MaxLogFileSize = 10485760; // 10MB

        #endregion

        #region Log Message Formats

        /// <summary>
        /// Format for log entry timestamp.
        /// </summary>
        public const string TimestampFormat = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// Format for complete log entry.
        /// Parameters: {0} = timestamp, {1} = level, {2} = message
        /// </summary>
        public const string LogEntryFormat = "[{0}] [{1}] {2}";

        /// <summary>
        /// Separator between log entries (for readability).
        /// </summary>
        public const string LogSeparator = "---";

        #endregion

        #region Log Initialization Messages

        /// <summary>
        /// Header written at the start of each log file.
        /// </summary>
        public const string LogHeader = "=== BannerWand Mod Log ===";

        /// <summary>
        /// Message when logger initializes successfully.
        /// </summary>
        public const string LoggerInitialized = "Logger initialized";

        /// <summary>
        /// Format for logger initialization error.
        /// Parameters: {0} = error message
        /// </summary>
        public const string LoggerInitErrorFormat = "BannerWand: Failed to initialize logger - {0}";

        #endregion
    }
}
