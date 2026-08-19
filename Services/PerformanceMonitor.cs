#nullable enable
// System namespaces
// Project namespaces
using BannerWand.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace BannerWand.Services
{
    /// <summary>
    /// Monitors and tracks performance metrics for BannerWand mod operations.
    /// Provides detailed timing information for optimization and debugging.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This service tracks execution time for various mod operations and provides
    /// statistics to help identify performance bottlenecks.
    /// </para>
    /// <para>
    /// Usage:
    /// <code>
    /// using (PerformanceMonitor.BeginScope("MyOperation"))
    /// {
    ///     // Your code here
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public static class PerformanceMonitor
    {
        /// <summary>
        /// Tracks performance metrics for each operation.
        /// Key: Operation name, Value: Performance statistics.
        /// </summary>
        private static readonly Dictionary<string, OperationStats> _operationStats = new(capacity: 50);

        /// <summary>
        /// Lock object for thread-safe access to statistics.
        /// </summary>
        private static readonly object _lock = new();

        /// <summary>
        /// Starts a performance measurement scope.
        /// </summary>
        /// <param name="operationName">Name of the operation to measure.</param>
        /// <returns>Disposable scope that automatically records timing on disposal.</returns>
        public static IDisposable BeginScope(string operationName)
        {
            return new PerformanceScope(operationName);
        }

        /// <summary>
        /// Records a completed operation's execution time.
        /// </summary>
        /// <param name="operationName">Name of the operation.</param>
        /// <param name="elapsedMs">Elapsed time in milliseconds.</param>
        public static void RecordOperation(string operationName, long elapsedMs)
        {
            lock (_lock)
            {
                if (!_operationStats.TryGetValue(operationName, out OperationStats? stats))
                {
                    stats = new OperationStats(operationName);
                    _operationStats[operationName] = stats;
                }

                stats.RecordExecution(elapsedMs);
            }
        }

        /// <summary>
        /// Gets performance statistics for a specific operation.
        /// </summary>
        /// <param name="operationName">Name of the operation.</param>
        /// <returns>Statistics for the operation, or null if not found.</returns>
        public static OperationStats? GetStats(string operationName)
        {
            lock (_lock)
            {
                return _operationStats.TryGetValue(operationName, out OperationStats? stats) ? stats : null;
            }
        }

        /// <summary>
        /// Gets all tracked operation statistics.
        /// </summary>
        /// <returns>Dictionary of operation names to statistics.</returns>
        public static Dictionary<string, OperationStats> GetAllStats()
        {
            lock (_lock)
            {
                return new Dictionary<string, OperationStats>(_operationStats);
            }
        }

        /// <summary>
        /// Clears all performance statistics.
        /// </summary>
        public static void ClearStats()
        {
            lock (_lock)
            {
                _operationStats.Clear();
            }
        }

        /// <summary>
        /// Logs performance summary for all tracked operations.
        /// </summary>
        public static void LogPerformanceSummary()
        {
            lock (_lock)
            {
                if (_operationStats.Count == 0)
                {
                    ModLogger.Log("No performance data collected yet.");
                    return;
                }

                ModLogger.Log("=== Performance Summary ===");
                foreach (KeyValuePair<string, OperationStats> kvp in _operationStats)
                {
                    OperationStats stats = kvp.Value;
                    ModLogger.Log($"{stats.OperationName}: Avg={stats.AverageMs:F2}ms, Min={stats.MinMs}ms, Max={stats.MaxMs}ms, Count={stats.ExecutionCount}");
                }
                ModLogger.Log("=========================");
            }
        }

        /// <summary>
        /// Performance measurement scope that automatically records timing.
        /// </summary>
        private class PerformanceScope : IDisposable
        {
            private readonly string _operationName;
            private readonly Stopwatch _stopwatch;

            public PerformanceScope(string operationName)
            {
                _operationName = operationName;
                _stopwatch = Stopwatch.StartNew();
            }

            public void Dispose()
            {
                _stopwatch.Stop();
                RecordOperation(_operationName, _stopwatch.ElapsedMilliseconds);
            }
        }
    }

    /// <summary>
    /// Stores performance statistics for a specific operation.
    /// </summary>
    public class OperationStats
    {
        /// <summary>
        /// Name of the operation being tracked.
        /// </summary>
        public string OperationName { get; }

        /// <summary>
        /// Total number of times this operation has been executed.
        /// </summary>
        public int ExecutionCount { get; private set; }

        /// <summary>
        /// Total elapsed time across all executions (milliseconds).
        /// </summary>
        public long TotalMs { get; private set; }

        /// <summary>
        /// Minimum execution time (milliseconds).
        /// </summary>
        public long MinMs { get; private set; } = long.MaxValue;

        /// <summary>
        /// Maximum execution time (milliseconds).
        /// </summary>
        public long MaxMs { get; private set; }

        /// <summary>
        /// Average execution time (milliseconds).
        /// </summary>
        public double AverageMs => ExecutionCount > 0 ? (double)TotalMs / ExecutionCount : 0;

        public OperationStats(string operationName)
        {
            OperationName = operationName;
        }

        /// <summary>
        /// Records a new execution of this operation.
        /// </summary>
        /// <param name="elapsedMs">Elapsed time in milliseconds.</param>
        public void RecordExecution(long elapsedMs)
        {
            ExecutionCount++;
            TotalMs += elapsedMs;

            if (elapsedMs < MinMs)
            {
                MinMs = elapsedMs;
            }

            if (elapsedMs > MaxMs)
            {
                MaxMs = elapsedMs;
            }
        }
    }
}
