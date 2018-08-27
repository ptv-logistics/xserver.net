// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System.Collections.Generic;


namespace Ptv.XServer.Controls.Map.Tools
{
    /// <summary> Generic class for caching objects in memory. </summary>
    /// <typeparam name="TKey"> The key type. </typeparam>
    /// <typeparam name="TValue"> The value type. </typeparam>
    public class MemoryCache<TKey, TValue>
    {
        #region protected variables
        /// <summary> Gets or sets Documentation in progress... </summary>
        protected int CacheSize { get; set; }
        /// <summary> TGets or sets ODO: Comment. </summary>
        protected Dictionary<TKey, TValue> valueCache = new Dictionary<TKey, TValue>();
        /// <summary> Documentation in progress... </summary>
        protected LinkedList<TKey> lastUsed = new LinkedList<TKey>();
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="MemoryCache{TKey, TValue}"/> class. </summary>
        /// <param name="cacheSize"> The number of cached objects. </param>
        public MemoryCache(int cacheSize)
        {
            CacheSize = cacheSize;
        }
        #endregion

        #region public methods
        /// <summary> Returns the value of an entry. </summary>
        /// <param name="key"> The key of the entry. </param>
        /// <param name="value"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public virtual bool TryGetValue(TKey key, out TValue value)
        {
            if (!valueCache.TryGetValue(key, out value)) return false;

            lastUsed.Remove(key);
            lastUsed.AddFirst(key);

            return true;
        }

        /// <summary>
        /// Adds an entry to the cache.
        /// </summary>
        /// <param name="key"> The key of the entry. </param>
        /// <param name="value"> The value of the entry. </param>
        public virtual void AddValue(TKey key, TValue value)
        {
            valueCache.Add(key, value);
            lastUsed.AddFirst(key);
            if (lastUsed.Count <= CacheSize) return;

            valueCache.Remove(lastUsed.Last.Value);
            lastUsed.RemoveLast();
        }
        #endregion
    }
}
