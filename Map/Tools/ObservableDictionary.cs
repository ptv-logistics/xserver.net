using System.Collections.Generic;
using System.Collections.Specialized;


namespace Ptv.XServer.Controls.Map.Tools
{
    /// <summary> Helper class providing an observable dictionary which sends notifications on add, remove and change. </summary>
    /// <typeparam name="TKey"> Type of the entry keys. </typeparam>
    /// <typeparam name="TValue"> Type of the entry values. </typeparam>
    public class ObservableDictionary<TKey, TValue> : IDictionary<TKey, TValue>, INotifyCollectionChanged
    {
        #region private variables
        /// <summary> Internally hold dictionary. </summary>
        private readonly Dictionary<TKey, TValue> dictionary;
        #endregion

        #region event
        /// <summary> The event which is sent on a change. </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="ObservableDictionary{TKey, TValue}"/> class. Simple constructor of the ObservableDictionary class taking no parameters. </summary>
        public ObservableDictionary()
        {
            dictionary = new Dictionary<TKey, TValue>();
        }

        /// <summary> Initializes a new instance of the <see cref="ObservableDictionary{TKey, TValue}"/> class. </summary>
        /// <param name="copyDictionary"> The <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> whose elements are copied to the new <see cref="System.Collections.Generic.Dictionary{TKey, TValue}"/>. </param>
        public ObservableDictionary(IDictionary<TKey, TValue> copyDictionary)
        {
            dictionary = new Dictionary<TKey, TValue>(copyDictionary);
        }

        /// <summary> Initializes a new instance of the <see cref="ObservableDictionary{TKey, TValue}"/> class. </summary>
        /// <param name="comparer">The <see cref="System.Collections.Generic.IEqualityComparer{T}"/> implementation to use
        /// when comparing keys, or null to use the default <see cref="System.Collections.Generic.IEqualityComparer{T}"/> for the type of the key.</param>
        public ObservableDictionary(IEqualityComparer<TKey> comparer)
        {
            dictionary = new Dictionary<TKey, TValue>(comparer);
        }

        /// <summary> Initializes a new instance of the <see cref="ObservableDictionary{TKey, TValue}"/> class. </summary>
        /// <param name="capacity">The initial number of elements that the <see cref="System.Collections.Generic.Dictionary{TKey, TValue}"/> can contain.</param>
        public ObservableDictionary(int capacity)
        {
            dictionary = new Dictionary<TKey, TValue>(capacity);
        }

        /// <summary> Initializes a new instance of the <see cref="ObservableDictionary{TKey, TValue}"/> class. </summary>
        /// <param name="copyDictionary"> The <see cref="System.Collections.Generic.IDictionary{TKey, TValue}"/> whose elements are copied to the new <see cref="System.Collections.Generic.Dictionary{TKey, TValue}"/>. </param>
        /// <param name="comparer">The <see cref="System.Collections.Generic.IEqualityComparer{T}"/> implementation to use
        /// when comparing keys, or null to use the default <see cref="System.Collections.Generic.IEqualityComparer{T}"/> for the type of the key.</param>
        public ObservableDictionary(IDictionary<TKey, TValue> copyDictionary, IEqualityComparer<TKey> comparer)
        {
            dictionary = new Dictionary<TKey, TValue>(copyDictionary, comparer);
        }

        /// <summary> Initializes a new instance of the <see cref="ObservableDictionary{TKey, TValue}"/> class. </summary>
        /// <param name="capacity">The initial number of elements that the <see cref="System.Collections.Generic.Dictionary{TKey, TValue}"/> can contain.</param>
        /// <param name="comparer">The <see cref="System.Collections.Generic.IEqualityComparer{T}"/> implementation to use
        /// when comparing keys, or null to use the default <see cref="System.Collections.Generic.IEqualityComparer{T}"/> for the type of the key.</param>
        public ObservableDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            dictionary = new Dictionary<TKey, TValue>(capacity, comparer);
        }
        #endregion

        #region sending the event
        /// <summary> Adds a new key value pair to the dictionary. </summary>
        /// <param name="key"> Key of the key value pair to add. </param>
        /// <param name="value"> Value of the key value pair to add. </param>
        public void Add(TKey key, TValue value)
        {
            dictionary.Add(key, value);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new KeyValuePair<TKey, TValue>(key, value)));
        }

        /// <summary> Removes a certain item from the dictionary. </summary>
        /// <param name="key"> Key of the item to remove. </param>
        /// <returns> True if the item has been found and removed successfully. </returns>
        public bool Remove(TKey key)
        {
            var item = new KeyValuePair<TKey, TValue>(key, dictionary[key]);
            bool bTmp = dictionary.Remove(key);
            if (!bTmp || (CollectionChanged == null)) return bTmp;

            CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
            return true;
        }

        /// <summary> Finds the value matching with a given key. </summary>
        /// <param name="key"> Key of the item to find. </param>
        /// <returns> Value of the item to find. </returns>
        public TValue this[TKey key]
        {
            get { return dictionary[key]; }
            set
            {
                var oldItem = new KeyValuePair<TKey, TValue>(key, dictionary[key]);
                dictionary[key] = value;
                var newItem = new KeyValuePair<TKey, TValue>(key, dictionary[key]);

                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItem, oldItem));
            }
        }

        /// <summary> Adds a key value pair to the dictionary. </summary>
        /// <param name="item"> Item to add. </param>
        public void Add(KeyValuePair<TKey, TValue> item)
        {
            (dictionary as IDictionary<TKey, TValue>).Add(item);
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        }

        /// <summary> Clears the dictionary and removes all items. </summary>
        public void Clear()
        {
            dictionary.Clear();
            if (CollectionChanged == null) return;

            var args = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            CollectionChanged(this, args);
        }

        /// <summary> Removes a given item from the dictionary. </summary>
        /// <param name="item"> Item to remove. </param>
        /// <returns> True if the item has been found and removed successfully. </returns>
        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            bool bTmp = (dictionary as IDictionary<TKey, TValue>).Remove(item);
            if (!bTmp || (CollectionChanged == null)) return bTmp;

            CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
            return true;
        }
        #endregion

        #region calling directly the Dictionary methods
        /// <summary><see cref="System.Collections.Generic.Dictionary{TKey, TValue}.ContainsKey"/></summary>
        /// <param name="key"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public bool ContainsKey(TKey key) { return dictionary.ContainsKey(key); }
        /// <summary> Gets the collection of keys in the dictionary. See <see cref="System.Collections.Generic.Dictionary{TKey, TValue}.Keys"/>. </summary>
        /// <returns> Documentation in progress... </returns>
        public ICollection<TKey> Keys => dictionary.Keys;
        /// <summary><see cref="Dictionary{TKey, TValue}.TryGetValue"/></summary>
        /// <param name="key"> Documentation in progress... </param>
        /// <param name="value"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public bool TryGetValue(TKey key, out TValue value) { return dictionary.TryGetValue(key, out value); }
        /// <summary> Gets the collection of values in the dictionary. See <see cref="System.Collections.Generic.Dictionary{TKey, TValue}.Values"/>. </summary>
        /// <returns> Documentation in progress... </returns>
        public ICollection<TValue> Values => dictionary.Values;
        /// <summary><see cref="System.Collections.Generic.ICollection{T}.Contains"/></summary>
        /// <param name="item"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public bool Contains(KeyValuePair<TKey, TValue> item) { return (dictionary as IDictionary<TKey, TValue>).Contains(item); }
        /// <summary><see cref="System.Collections.Generic.ICollection{T}.CopyTo"/></summary>
        /// <param name="array"> Documentation in progress... </param>
        /// <param name="arrayIndex"> Documentation in progress... </param>
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) { (dictionary as IDictionary<TKey, TValue>).CopyTo(array, arrayIndex); }
        /// <summary> Gets the count of elements in the dictionary. See <see cref="System.Collections.Generic.Dictionary{TKey, TValue}.Count"/>. </summary>
        /// <returns> Documentation in progress... </returns>
        public int Count => dictionary.Count;
        /// <summary> Gets a value indicating whether the dictionary is observable. See <see cref="System.Collections.Generic.ICollection{T}.IsReadOnly"/>. </summary>
        /// <returns> Documentation in progress... </returns>
        public bool IsReadOnly => (dictionary as IDictionary<TKey, TValue>).IsReadOnly;
        /// <summary><see cref="System.Collections.Generic.IEnumerable{T}.GetEnumerator"/></summary>
        /// <returns> Documentation in progress... </returns>
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() { return (dictionary as IDictionary<TKey, TValue>).GetEnumerator(); }
        /// <summary><see cref="System.Collections.Generic.Dictionary{TKey, TValue}.GetEnumerator"/></summary>
        /// <returns> Documentation in progress... </returns>
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return dictionary.GetEnumerator(); }
        #endregion
    }
}
