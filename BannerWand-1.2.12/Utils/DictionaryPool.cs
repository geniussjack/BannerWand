#nullable enable
using System;
using System.Collections.Generic;
using TaleWorlds.Core;

namespace BannerWandRetro.Utils
{
    /// <summary>
    /// Object pool for Dictionary instances to reduce allocation overhead in hot paths.
    /// Specifically designed for ItemBarterable patch backup dictionaries.
    /// </summary>
    /// <typeparam name="TKey">Dictionary key type.</typeparam>
    /// <typeparam name="TValue">Dictionary value type.</typeparam>
    /// <remarks>
    /// <para>
    /// Performance benefit: Each trade creates a temporary Dictionary for item backup.
    /// Frequent trading can cause GC pressure from repeated allocations.
    /// Pooling reuses Dictionary instances, reducing allocations significantly.
    /// </para>
    /// <para>
    /// Pool size: Limited to prevent unbounded memory growth. When pool is full,
    /// dictionaries are simply discarded and GC'd normally.
    /// </para>
    /// <para>
    /// Thread safety: Uses [ThreadStatic] to ensure each thread has its own pool.
    /// Safe for concurrent barter operations (though rare in Bannerlord).
    /// </para>
    /// </remarks>
    public static class DictionaryPool<TKey, TValue> where TKey : notnull
    {
        // Thread-local pool to avoid locking overhead

        private const int MaxPoolSize = 4;

        /// <summary>
        /// Gets the thread-local pool instance, creating it if needed.
        /// </summary>
        [field: ThreadStatic]
        private static Stack<Dictionary<TKey, TValue>> Pool
        {
            get
            {
                field ??= new Stack<Dictionary<TKey, TValue>>(MaxPoolSize);
                return field;
            }
        }

        /// <summary>
        /// Rents a dictionary from the pool.
        /// </summary>
        /// <returns>
        /// A dictionary instance from the pool, or a new instance if pool is empty.
        /// The returned dictionary is guaranteed to be empty.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Usage pattern:
        /// <code>
        /// var dict = DictionaryPool&lt;ItemObject, int&gt;.Rent();
        /// try
        /// {
        ///     // Use dictionary
        ///     dict[item] = 100;
        /// }
        /// finally
        /// {
        ///     DictionaryPool&lt;ItemObject, int&gt;.Return(dict);
        /// }
        /// </code>
        /// </para>
        /// <para>
        /// IMPORTANT: Always return rented dictionaries via <see cref="Return"/> in a finally block
        /// to ensure proper cleanup even on exceptions.
        /// </para>
        /// </remarks>
        public static Dictionary<TKey, TValue> Rent()
        {
            Stack<Dictionary<TKey, TValue>> pool = Pool;

            if (pool.Count > 0)
            {
                Dictionary<TKey, TValue> dict = pool.Pop();
                // Ensure it's empty (defensive check)
                dict.Clear();
                return dict;
            }

            // Pool empty, create new instance
            return [];
        }

        /// <summary>
        /// Returns a dictionary to the pool for reuse.
        /// </summary>
        /// <param name="dictionary">The dictionary to return. Must not be null.</param>
        /// <remarks>
        /// <para>
        /// The dictionary is cleared before being returned to the pool to prevent
        /// memory leaks from retained references.
        /// </para>
        /// <para>
        /// If the pool is full, the dictionary is discarded and will be GC'd normally.
        /// This prevents unbounded pool growth.
        /// </para>
        /// <para>
        /// Safe to call with null (no-op), but avoid this pattern - always return rented dictionaries.
        /// </para>
        /// </remarks>
        public static void Return(Dictionary<TKey, TValue>? dictionary)
        {
            try
            {
                if (dictionary is null)
                {
                    return;
                }

                // Clear to release references (prevent memory leaks)
                dictionary.Clear();

                Stack<Dictionary<TKey, TValue>> pool = Pool;

                // Only return to pool if not full
                if (pool.Count < MaxPoolSize)
                {
                    pool.Push(dictionary);
                }
                // Otherwise let it be GC'd (prevents unbounded pool growth)

            }
            catch (Exception ex)
            {
                ModLogger.Error($"[DictionaryPool] Error in Return: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// Clears the thread-local pool, releasing all pooled dictionaries.
        /// </summary>
        /// <remarks>
        /// Only needed if you want to force GC of pooled dictionaries.
        /// Normally not required as pool size is limited.
        /// </remarks>
        public static void Clear()
        {
            try
            {
                Pool.Clear();

            }
            catch (Exception ex)
            {
                ModLogger.Error($"[DictionaryPool] Error in Clear: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }
    }

    /// <summary>
    /// Specialized pool for ItemObject-to-int dictionaries used in item backup scenarios.
    /// </summary>
    /// <remarks>
    /// Convenience wrapper around <see cref="DictionaryPool{TKey, TValue}"/> with
    /// common type parameters for the ItemBarterable patch use case.
    /// </remarks>
    public static class ItemBackupPool
    {
        /// <summary>
        /// Rents a dictionary for storing item backup counts.
        /// </summary>
        /// <returns>Empty dictionary ready for use.</returns>
        public static Dictionary<ItemObject, int> Rent()
        {
            return DictionaryPool<ItemObject, int>.Rent();
        }

        /// <summary>
        /// Returns a dictionary to the pool.
        /// </summary>
        /// <param name="dictionary">The dictionary to return.</param>
        public static void Return(Dictionary<ItemObject, int>? dictionary)
        {
            try
            {
                DictionaryPool<ItemObject, int>.Return(dictionary);

            }
            catch (Exception ex)
            {
                ModLogger.Error($"[DictionaryPool] Error in Return: {ex.Message}");
                ModLogger.Error($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
