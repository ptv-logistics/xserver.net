// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Collections.Generic;

namespace Ptv.XServer.Controls.Map.Layers.WmtsLayer
{
    /// <summary>Implements a very simple, size-restricted and thread safe LRU cache for caching arbitrary elements. </summary>
    /// <typeparam name="TK">Type of the key that identifies elements.</typeparam>
    /// <typeparam name="T">Type of the elements store in the LRU cache.</typeparam>
    public class LruCache<TK, T> where T : class
    {
        /// <summary>
        /// Dictionary that stores our elements and maps keys to elements.
        /// </summary>
        private readonly Dictionary<TK, LinkedListNode<KeyValuePair<TK, T>>> dict = new Dictionary<TK, LinkedListNode<KeyValuePair<TK, T>>>();

        /// <summary>
        /// Linked list that reflects the LRU order of our elements.
        /// </summary>
        private readonly LinkedList<KeyValuePair<TK, T>> list = new LinkedList<KeyValuePair<TK, T>>();

        /// <summary>
        /// A function that calculates the size of an element. See constructor.
        /// </summary>
        private readonly Func<T, int> itemSize;

        /// <summary>
        /// The size limit of the LRU cache.
        /// </summary>
        private readonly int limit;

        /// <summary>
        /// The current size of the LRU cache.
        /// </summary>
        private int size;

        /// <summary>
        /// Creates and initializes an instance of the LRU cache.
        /// </summary>
        /// <param name="limit">Size limit of the LRU cache.</param>
        /// <param name="size">
        /// Optionally provides the function to calculate the size of an element.
        /// If the size of the LRU cache is to be restricted simply by the number of elements, the size function
        /// should return a constant value of "1". This is also the default that is used in case the size parameter 
        /// is set to null. If you want to limit the LRU cache by a real size, use the size function to return the 
        /// byte size of an element.
        /// </param>
        public LruCache(int limit, Func<T, int> size = null)
        {
            this.limit = limit;
            itemSize = size ?? (item => 1);
        }

        /// <summary>
        /// Reads and writes elements using the key of an element. 
        /// </summary>
        /// <param name="key">Element key. Trying to read not existing elements results in a null value being returned - no 
        /// exception will be thrown in that case. Writing elements with an existing key will 
        /// update the value in the cache. Every element access will make this element the 
        /// "least recently used" one. The implementation is thread safe using a synchronization 
        /// lock on the inner dictionary.
        /// </param>
        public T this[TK key]
        {
            get
            {
                // synchronize ...
                lock (dict)
                {
                    // lookup element
                    var node = dict.ContainsKey(key) ? dict[key] : null;

                    // return null when not found
                    if (node == null)
                        return null;

                    // we found an element; make it the least recently used 
                    // one by making it the last element in our linked list.
                    list.Remove(node);
                    list.AddLast(node);

                    // return the element
                    return node.Value.Value;
                }
            }

            set
            {
                // synchronize ...
                lock (dict)
                {
                    // update the size of the LRU cache, add the size of the element
                    size += itemSize(value);

                    // check if the key already exists 
                    if (dict.ContainsKey(key))
                    {
                        // key exists, get the existing node 
                        var node = dict[key];

                        // we're going to replace the old element, decrease the size accordingly
                        size -= itemSize(node.Value.Value);

                        // replace element
                        node.Value = new KeyValuePair<TK, T>(key, value);

                        // finally make the element the least recently used 
                        // one by making it the last element in our linked list.
                        list.Remove(node);
                        list.AddLast(node);
                    }
                    else
                    {
                        // key does not exist, so create a new node and add 
                        // the element to the linked list and to the dictionary.
                        list.AddLast(new KeyValuePair<TK, T>(key, value));
                        dict[key] = list.Last;
                    }

                    // apply size limit; while the current size is large than the limit ...
                    while (size > limit)
                    {
                        // remove the head element (= oldest element) from the 
                        // linked list and the dictionary accordingly
                        size -= itemSize(list.First.Value.Value);
                        dict.Remove(list.First.Value.Key);
                        list.RemoveFirst();
                    }
                }
            }
        }
    }
}
