using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Ptv.XServer.Controls.Map.Layers;
using Ptv.XServer.Controls.Map.Tools;

namespace System.Collections.Generic
{
    /// <summary>
    /// Represents a generic read-only collection of key/value pairs.
    /// </summary>
    /// <typeparam name="TKey">The type of keys in the read-only dictionary.</typeparam>
    /// <typeparam name="TValue">The type of values in the read-only dictionary.</typeparam>
    /// <remarks>IReadOnlyDictionary is standard interface provided by .Net starting with version 4.5. As xServer.Net uses 
    /// an older version, we need to define the interface in xServer.Net itself.</remarks>
    public interface IReadOnlyDictionary<TKey, TValue>
    {
        /// <summary>
        /// Gets the number of elements in the collection.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets the element that has the specified key in the read-only dictionary.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <returns>The element that has the specified key in the read-only dictionary.</returns>
        TValue this[TKey key] { get; }

        /// <summary>
        /// Gets an enumerable collection that contains the keys in the read-only dictionary.
        /// </summary>
        IEnumerable<TKey> Keys { get; }

        /// <summary>
        /// Gets an enumerable collection that contains the values in the read-only dictionary.
        /// </summary>
        IEnumerable<TValue> Values { get; }

        /// <summary>
        /// Determines whether the read-only dictionary contains an element that has the specified key.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <returns>true if the read-only dictionary contains an element that has the specified key; otherwise, false.</returns>
        bool ContainsKey(TKey key);

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator();

        /// <summary>
        /// Gets the value that is associated with the specified key.
        /// </summary>
        /// <param name="key">The key to locate.</param>
        /// <param name="value">When this method returns, the value associated with the specified key, 
        /// if the key is found; otherwise, the default value for the type of the value parameter. This parameter is passed uninitialized.</param>
        /// <returns>true if the object that implements the interface contains an element that has the specified key; otherwise, false.</returns>
        bool TryGetValue(TKey key, out TValue value);
    }
}

namespace Ptv.XServer.Controls.Map.TileProviders
{
    /// <summary>
    /// Generic interface to access the information of map object.
    /// </summary>
    public interface IMapObject : IReadOnlyDictionary<string, string>, IEnumerable<KeyValuePair<string, string>>
    {
        /// <summary>
        /// The map object's id.
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Names the source of the map object. This could be the name of a map layer or a feature layer theme.
        /// </summary>
        string Layer { get; }

        /// <summary>
        /// Gets the location of the map object in the corresponding map image.
        /// </summary>
        Point Point { get; }

        /// <summary>
        /// Gets the location of the map object in world coordiantes (EPSG:76131).
        /// </summary>
        Point LogicalPosition { get; }

        /// <summary>
        /// Reads the source object that is wrapped by the IMapObject's implementation.
        /// </summary>
        Object Source { get; }
    }

    /// <summary>
    /// Provides a base implementation of IMapObject.
    /// </summary>
    public class MapObject : IMapObject
    {
        /// <summary>
        /// Dictionary that caches the attributes of a map object.
        /// </summary>
        private Dictionary<string, string> attributes;

        /// <summary>
        /// Function that provides attributes of a map object.
        /// </summary>
        private readonly Func<IEnumerable<KeyValuePair<string, string>>> provideAttributes;

        /// <summary>
        /// Creates and initializes an instance of MapObject.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="layer"></param>
        /// <param name="point"></param>
        /// <param name="logicalPosition"></param>
        /// <param name="attributes"></param>
        public MapObject(string id, string layer, Point point, Point logicalPosition, Func<IEnumerable<KeyValuePair<string, string>>> attributes)
        {
            Point = point;
            LogicalPosition = logicalPosition;
            Id = id;
            Layer = layer;
            provideAttributes = attributes;
        }

        /// <summary>
        /// Used internally to access the attributes of the Feature.
        /// </summary>
        private Dictionary<string, string> Attributes
        {
            get
            {
                if (attributes == null)
                {
                    var providedAttributes = provideAttributes?.Invoke();

                    attributes = providedAttributes?.ToDictionary(item => item.Key, item => item.Value)
                        ?? new Dictionary<string, string>();
                }

                return attributes;
            }
        }

        /// <inheritdoc/>
        public string this[string key] => Attributes.ContainsKey(key)
            ? Attributes[key]
            : string.Empty;

        /// <inheritdoc/>
        public string Id { get; protected set; }

        /// <inheritdoc/>
        public Point LogicalPosition { get; protected set; }

        /// <inheritdoc/>
        public Point Point { get; protected set; }

        /// <inheritdoc/>
        public string Layer { get; protected set; }

        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => Attributes.GetEnumerator();

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => Attributes.GetEnumerator();

        /// <inheritdoc/>
        public int Count => Attributes.Count;

        /// <inheritdoc/>
        public IEnumerable<string> Keys => Attributes.Keys;

        /// <inheritdoc/>
        public IEnumerable<string> Values => Attributes.Values;

        /// <inheritdoc/>
        public object Source { get; protected set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            var result = new StringBuilder();

            foreach (var attr in Attributes)
                result.AppendWithSeparator(attr.Key + " = " + attr.Value, Environment.NewLine);

            return result.ToString();
        }

        /// <inheritdoc/>
        public bool ContainsKey(string key) => Attributes.ContainsKey(key);

        /// <inheritdoc/>
        public bool TryGetValue(string key, out string value)
        {
            value = this[key];
            return ContainsKey(key);
        }
    }

    /// <summary>
    /// Represents a MapRectangle corresponding to a tile.
    /// </summary>
    public class TileMapRectangle : MapRectangle
    {
        /// <summary>
        /// Creates and initializes an instance of MapRectangleByTile.
        /// </summary>
        /// <param name="x">X-coordinate of the tile</param>
        /// <param name="y">Y-coordinate of the tile</param>
        /// <param name="z">Z-coordinate of the tile</param>
        public TileMapRectangle(int x, int y, int z)
        {
            var rect = ReprojectionProvider.TileToSphereMercator(X = x, Y = y, Z = z, 6371000);

            West = Math.Min(rect.Left, rect.Right);
            East = Math.Max(rect.Left, rect.Right);
            North = Math.Max(rect.Top, rect.Bottom);
            South = Math.Min(rect.Top, rect.Bottom);
        }

        /// <summary>
        /// Gets the x-coordinate of the tile.
        /// </summary>
        public int X { get; }

        /// <summary>
        /// Gets the y-coordinate of the tile.
        /// </summary>
        public int Y { get; }

        /// <summary>
        /// Gets the z-coordinate of the tile.
        /// </summary>
        public int Z { get; }
    }
}
