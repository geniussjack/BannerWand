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

        /// <summary>
        /// Log level for model registration messages.
        /// </summary>
        public const string Model = "MODEL";

        /// <summary>
        /// Log level for behavior registration messages.
        /// </summary>
        public const string Behavior = "BEHAVIOR";

        /// <summary>
        /// Log level for cheat activation/deactivation messages.
        /// </summary>
        public const string Cheat = "CHEAT";

        /// <summary>
        /// Log level for performance metrics messages.
        /// </summary>
        public const string Performance = "PERF";

        #endregion

        #region Log File Configuration

        /// <summary>
        /// Format string for the per-day log file name. Parameter {0} is the current date.
        /// </summary>
        public const string LogFileNameFormat = "BannerWand_{0:yyyyMMdd}.log";

        /// <summary>
        /// Search pattern matching every dated log file this mod writes, for pruning old files.
        /// </summary>
        public const string LogFileSearchPattern = "BannerWand_*.log";

        /// <summary>
        /// Log subdirectory name directly under the user's Documents folder.
        /// </summary>
        public const string LogSubdirectory = "Mount and Blade II Bannerlord";

        /// <summary>
        /// Configs folder name within <see cref="LogSubdirectory"/>, shared with other Bannerlord mods,
        /// such as ButterLib, that already write their own logs into the same parent folder.
        /// </summary>
        public const string LogConfigsFolderName = "Configs";

        /// <summary>
        /// Logs folder name within <see cref="LogConfigsFolderName"/>.
        /// </summary>
        public const string LogsFolderName = "ModLogs";

        /// <summary>
        /// Number of days a dated log file is kept before being deleted automatically.
        /// </summary>
        public const int LogRetentionDays = 14;

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
