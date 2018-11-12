// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Ptv.XServer.Controls.Map.Layers;
using Ptv.XServer.Controls.Map.Tools;

namespace Ptv.XServer.Controls.Map.TileProviders
{
    /// <summary>
    /// Generic interface to access the information of map object.
    /// </summary>
    public interface IMapObject : IEnumerable<KeyValuePair<string, string>>
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
        /// Gets the location of the map object in world coordinates (EPSG:76131).
        /// </summary>
        Point LogicalPosition { get; }

        /// <summary>
        /// Reads the source object that is wrapped by the IMapObject's implementation.
        /// </summary>
        object Source { get; }
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
                if (attributes != null) return attributes;
                var providedAttributes = provideAttributes?.Invoke();

                attributes = providedAttributes?.ToDictionary(item => item.Key, item => item.Value)
                             ?? new Dictionary<string, string>();

                return attributes;
            }
        }

        /// <summary>Returns value which belongs to the key attribute. </summary>
        /// <param name="key">Key of the corresponding key/value pair.</param>
        /// <returns>Value of the corresponding key/value pair.</returns>
        public string this[string key] => Attributes.ContainsKey(key)
            ? Attributes[key]
            : string.Empty;

        /// <summary> Identifier of the object. </summary>
        public string Id { get; protected set; }

        /// <summary> Logical position of the map object. </summary>
        public Point LogicalPosition { get; protected set; }

        /// <summary> Geographical coordinate. </summary>
        public Point Point { get; protected set; }

        /// <summary> Layer to which this map object belongs to. </summary>
        public string Layer { get; protected set; }

        /// <summary> Set of key/value pairs. </summary>
        /// <returns> Enumeration of key value pairs. </returns>
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => Attributes.GetEnumerator();

        /// <summary> Set of key/value pairs. </summary>
        /// <returns> Enumeration of key value pairs. </returns>
        IEnumerator IEnumerable.GetEnumerator() => Attributes.GetEnumerator();

        /// <summary> Number of key/value pairs. </summary>
        public int Count => Attributes.Count;

        /// <summary> Enumeration of all available keys. </summary>
        public IEnumerable<string> Keys => Attributes.Keys;

        /// <summary> Enumeration of all available values. </summary>
        public IEnumerable<string> Values => Attributes.Values;

        /// <summary> Source of the map object. </summary>
        public object Source { get; protected set; }

        /// <summary> Textual representation of the whole object. </summary>
        public override string ToString()
        {
            var result = new StringBuilder();

            foreach (var attribute in Attributes)
                result.AppendWithSeparator(attribute.Key + " = " + attribute.Value, Environment.NewLine);

            return result.ToString();
        }

        /// <summary> Checks if value exists for specified key. </summary>
        /// <param name="key"> Key to look for. </param>
        /// <returns> Indicates if a value exists for the specified key. </returns>
        public bool ContainsKey(string key) => Attributes.ContainsKey(key);

        /// <summary> Tries to get a value for the specified key. </summary>
        /// <param name="key"> Key to look for. </param>
        /// <param name="value">If available, value belonging to the key. </param>
        /// <returns> Indicates if a value exists for the specified key. </returns>
        public bool TryGetValue(string key, out string value)
        {
            value = this[key];
            return ContainsKey(key);
        }
    }

    /// <summary> Represents a MapRectangle corresponding to a tile. </summary>
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
