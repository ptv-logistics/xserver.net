// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Collections.Generic;
using System.Runtime.Remoting.Contexts;
using System.Threading;


namespace Ptv.XServer.Controls.Map.Tools
{
    /// <summary> Memory cache accessible in multiple threads. </summary>
    [Synchronization(true)]
    public class MemoryCacheMultiThreaded : ContextBoundObject
    {
        #region protected variables
        /// <summary> Gets or sets Documentation in progress... </summary>
        protected int CacheSize { get; set; }
        /// <summary> Documentation in progress... </summary>
        protected Dictionary<string, byte[]> valueCache = new Dictionary<string, byte[]>();
        /// <summary> Documentation in progress... </summary>
        protected LinkedList<string> lastUsed = new LinkedList<string>();
        /// <summary> The Dictionary which holds the locked keys. </summary>
        protected Dictionary<string, ManualResetEvent> lockedKeys = new Dictionary<string, ManualResetEvent>();
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="MemoryCacheMultiThreaded"/> class with a specific size. </summary>
        /// <param name="cacheSize"> Documentation in progress... </param>
        public MemoryCacheMultiThreaded(int cacheSize)
        {
            CacheSize = cacheSize;
        }
        #endregion

        #region public methods
        /// <summary> Tries to get a value for a given key. </summary>
        /// <param name="key"> Documentation in progress... </param>
        /// <param name="value"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public bool TryGetValue(string key, out byte[] value)
        {
            if (TryGetValueInternal(key, out value))
                return true;

            if (!(lockedKeys.TryGetValue(key, out var keyLock)))
            {
                lockedKeys.Add(key, new ManualResetEvent(false));

                return false;
            }

            try { keyLock.WaitOne(Timeout.Infinite, true); }
            catch (InvalidCastException) { return false; } // thrown if i shutdown the MSTO-interop demo

            return TryGetValueInternal(key, out value);
        }

        /// <summary> Adds a value to the cache. </summary>
        /// <param name="key"> The key. </param>
        /// <param name="value"> The value. </param>
        public void AddValue(string key, byte[] value)
        {
            if (value == null)
                return; // only cache empty (bing) images, but not null images


            // remove unlocked entries while cache size is exceeded
            LinkedListNode<string> node = lastUsed.Last;
            while (node != null && lastUsed.Count >= CacheSize)
            {
                if (!(lockedKeys.ContainsKey(node.Value)))
                {
                    valueCache.Remove(node.Value);
                    lastUsed.Remove(node);
                }

                node = node.Previous;
            }

            valueCache.Add(key, value);
            lastUsed.AddFirst(key);
        }

        /// <summary> Unlocks a key. </summary>
        /// <param name="key"> The key. </param>
        public void UnlockKey(string key)
        {
            if (!lockedKeys.ContainsKey(key)) return;

            ManualResetEvent keyLock = lockedKeys[key];
            lockedKeys.Remove(key);
            keyLock.Set();
        }
        #endregion

        #region private methods
        /// <summary> Returns the value of an entry. </summary>
        /// <param name="key"> The key of the entry. </param>
        /// <param name="value"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        private bool TryGetValueInternal(string key, out byte[] value)
        {
            if (!valueCache.TryGetValue(key, out value)) return false;

            lastUsed.Remove(key);
            lastUsed.AddFirst(key);
            return true;
        }
        #endregion
    }
}
