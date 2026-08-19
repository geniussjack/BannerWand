#nullable enable
namespace BannerWand.Interfaces
{
    /// <summary>
    /// Defines the contract for writing log messages to a file.
    /// Abstracts file I/O operations to allow for different writing strategies.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface allows for:
    /// - Different file writing implementations (buffered, unbuffered, async)
    /// - Mock implementations for testing
    /// - Alternative storage backends (database, network, etc.)
    /// </para>
    /// <para>
    /// Implementations should be thread-safe to handle concurrent logging scenarios.
    /// </para>
    /// </remarks>
    public interface ILogWriter
    {
        /// <summary>
        /// Writes a log message to the log file.
        /// </summary>
        /// <param name="logPath">The full path to the log file.</param>
        /// <param name="message">The formatted log message to write.</param>
        /// <remarks>
        /// <para>
        /// This method should:
        /// 1. Validate that logPath is not null or empty
        /// 2. Append the message to the existing log file (create if it doesn't exist)
        /// 3. Handle file I/O errors gracefully without throwing exceptions
        /// 4. Ensure thread-safe file access (use locks if needed)
        /// </para>
        /// <para>
        /// The message should already be formatted with timestamps, log levels, etc.
        /// The implementation should only handle the file writing part.
        /// </para>
        /// <para>
        /// Implementations should not throw exceptions - any errors should be logged
        /// via alternative means (Debug.Print, console, etc.) without interrupting
        /// the application flow.
        /// </para>
        /// </remarks>
        void WriteLog(string logPath, string message);

        /// <summary>
        /// Writes an initial header to a new log file.
        /// </summary>
        /// <param name="logPath">The full path to the log file.</param>
        /// <param name="header">The header text to write at the start of the log file.</param>
        /// <remarks>
        /// <para>
        /// This method is typically called when initializing logging or rotating log files.
        /// It should overwrite any existing file with the header content.
        /// </para>
        /// <para>
        /// If the file already exists, it should be cleared and replaced with the header.
        /// </para>
        /// </remarks>
        void WriteHeader(string logPath, string header);

        /// <summary>
        /// Clears the log file, removing all existing content.
        /// </summary>
        /// <param name="logPath">The full path to the log file to clear.</param>
        /// <remarks>
        /// This method should delete the file if it exists, or do nothing if it doesn't exist.
        /// </remarks>
        void ClearLog(string logPath);
    }
}

