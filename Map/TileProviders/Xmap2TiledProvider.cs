using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using Ptv.XServer.Controls.Map.Tools;
using SimpleJSON;
using xserver2;

namespace Ptv.XServer.Controls.Map.TileProviders
{
    /// <summary>
    /// Provides an implementation of IMapObject for xServer-2 features.
    /// </summary>
    public class XServer2Feature : MapObject
    {
        /// <summary>
        /// Creates and initializes an instance of XServer2MapObject.
        /// </summary>
        /// <param name="feature">The feataure out of which to create the map object.</param>
        public XServer2Feature(Feature feature) : base(
            feature.id, feature.themeId, 
            new Point(feature.referencePixelPoint.x, feature.referencePixelPoint.y),
            new Point(feature.referenceCoordinate.x, feature.referenceCoordinate.y),
            () => feature.attributes?.Select(a => new KeyValuePair<string, string>(a.key, a.value))
            )
        {
            Source = feature;
        }
    }

    /// <summary>
    /// Definition of a delegate for receiving information on the objects drawn on a map.
    /// </summary>
    /// <param name="mapObjects">Provides the map object information.</param>
    /// <param name="mapSection">Information on the section for which object information has been delivered.</param>
    /// <param name="size">The pixel size of the map image correspondonding to mapSection.</param>
    /// <remarks>Map objects can be delivered for a complete map or a single map tile. 
    /// If they are delivered for a single tile, the given map section will be of type TileMapRectangle.</remarks>
    public delegate void MapObjectInformationSink(IEnumerable<IMapObject> mapObjects, MapRectangle mapSection, Size size);

    /// <summary>
    /// Interface of a provider that delivers information on the objects drawn on a map.
    /// </summary>
    public interface IMapObjectInformationProvider
    {
        /// <summary>Object informtion sink. See remarks on <see cref="MapObjectInformationSink"/>. </summary>
        event MapObjectInformationSink MapObjectInformationSink;  
    }

    /// <summary> A provider implementation for the xMapServer-2 delivering tiled bitmaps. </summary>
    public class XMap2TiledProvider : XMapTiledProviderBase, IMapObjectInformationProvider
    {
        /// <summary> Logging restricted to this class. </summary>
        private static readonly Logger logger = new Logger("XMap2TiledProvider");

        /// <summary> URL of the xMap server. </summary>
        private readonly string url = string.Empty;

        /// <summary> Mode of the xMap layer. </summary>
        private readonly XMapMode mode;

        /// <summary>backing field for User property</summary>
        private string user = null;

        /// <summary>backing field for Password property</summary>
        private string password = null;

        /// <summary>backing field for CustomXMapLayers property</summary>
        private IEnumerable<XMapLayer> customXMapLayers = null;

        /// <summary>cache identifier for this provider</summary>
        private string cacheId = null;

        /// <summary> The user name for basic Http authentication. </summary>
        public string User
        {
            get { return user; }
            set { user = value; cacheId = null; }
        }

        /// <summary> The password for basic Http authentication. </summary>
        public string Password
        {
            get { return password; }
            set { password = value; cacheId = null; }
        }

        /// <summary> Workaround; currently, xM-2 cannot turn layers on and off via its SOAP request. </summary>
        public bool IgnoreMissingLayeredMapCapabilities { get; set; }

        /// <summary> Initializes a new instance of the <see cref="XMap2TiledProvider"/> class with the given connection
        /// string and mode. </summary>
        /// <param name="url"> The url to connect to the xMap server. </param>
        /// <param name="user">User name of the XMap authentication.</param>
        /// <param name="password">Password of the XMap authentication.</param>
        /// <param name="mode"> The mode of this tiled provider instance. </param>
        public XMap2TiledProvider(string url, string user, string password, XMapMode mode)
        {
            this.url = url;
            User = user;
            Password = password;
            this.mode = mode;

            base.needsTransparency = false;
            base.OverlapFactor = 0;
            base.Border = 0;

            IgnoreMissingLayeredMapCapabilities = false;


            // TODO: REVIEW ME - DEEP ZOOM
            //
            // To support 'deep zoom', the UntiledLayer was modified to take MaxZoom defined by this 
            // provider into consideration. As MaxZoom defaults to 18 (see XMapTiledProviderBase) this 
            // change also affects the default rendering behavior as it restricts label rendering where 
            // no restriction was before. We therefore change MaxZoom for the label layer at the least 
            // to 19, which is also the default for the map control itself. This way all the changes are 
            // considered to provide backward compatibility for the map's default configuration.
            //  
            // If this is ok, remove this comment. Otherwise find a better solution.

            if (mode == XMapMode.Town)
                base.MaxZoom = 19;
        }

        /// <summary> Initializes a new instance of the <see cref="XMap2TiledProvider"/> class with the given connection
        /// string and mode. </summary>
        /// <param name="url"> The url to connect to the xMap server. </param>
        /// <param name="mode"> The mode of this tiled provider instance. </param>
        public XMap2TiledProvider(string url, XMapMode mode) : this(url, string.Empty, string.Empty, mode) { }

        /// <summary> MapUpdate event. See remarks on <see cref="MapUpdateDelegate"/>. </summary>
        public event MapObjectInformationSink MapObjectInformationSink;

        /// <summary> Gets or sets the custom layers of the xMapServer. </summary>
        public IEnumerable<XMapLayer> CustomXMapLayers
        {
            get { return customXMapLayers; }
            set { customXMapLayers = value; cacheId = null; }
        }

        /// <summary> Gets the base layers of xMapServer, each of which enabled or disabled depending on the current XMapMode. </summary>
        private List<XMapLayer> BaseXMapLayers => new List<XMapLayer> {
            XMapLayer.Create("background", mode == XMapMode.Background),
            XMapLayer.Create("transport", mode == XMapMode.Background || mode == XMapMode.Street),
            XMapLayer.Create("labels", mode == XMapMode.Town)
        };

        /// <summary>
        /// Determines if parameters for autentication have been provided.
        /// </summary>
        private bool Authenticate => !string.IsNullOrEmpty(User) && !string.IsNullOrEmpty(Password);

        /// <summary>
        /// Determines if parameters for token autentication have been provided.
        /// </summary>
        private bool AuthenticateWithToken => Authenticate && User == "xtok";

        /// <inheritdoc/>
        public override string CustomProfile
        {
            get
            {
                // force profile to be valid using silkysand as the default
                return !string.IsNullOrEmpty(base.CustomProfile)
                    ? base.CustomProfile
                    : "silkysand";
            }
            set
            {
                // xMap1 used to append fg (foreground) and bg (background) to the the profile name. This is neither 
                // necessary nor supported in xMap-2, so we're going to strip that suffix just in case it has been set.

                base.CustomProfile = value != null
                    ? Regex.Replace(value, "-[fb]g$", "")
                    : null;

                cacheId = null;
            }
        }

        /// <summary>
        /// Checks if there is at least one layer in CustomXMapLayers that is not a base layer.
        /// </summary>
        protected bool HasRealCustomLayers => (CustomXMapLayers?.Any(a => BaseXMapLayers.All(b => a.Name != b.Name))).GetValueOrDefault();

        /// <summary>
        /// Determines the effective list of layers to disable / enable when requesting a map.
        /// </summary>
        protected virtual IEnumerable<XMapLayer> EffectiveXMapLayers
        {
            get
            {
                // initialize the list with base layers. 
                var layers = BaseXMapLayers;

                // if custom layers have been defined, add them to the layer list. Merging those lists 
                // may result in duplicates which must be removed. Custom layer dominate base layers, 
                // let the last entry win.
                if ((CustomXMapLayers?.Any()).GetValueOrDefault())
                {
                    layers.AddRange(CustomXMapLayers);

                    // now remove duplicates
                    var layerNames = layers.Select(layer => layer.Name).Distinct().ToArray();
                    layers = layerNames.Select(name => layers.Last(layer => layer.Name == name)).ToList();
                }

                // return effective list
                return layers;
            }
        }

        /// <summary>
        /// Builds the url for requesting tiles.
        /// </summary>
        /// <param name="key">Tile coordinate.</param>
        /// <returns>Tile url.</returns>
        public string GetTileUrl(MapSectionByTileKey key)
        {
            var url = new StringBuilder();

            // derive url from SOAP service url
            url.Append(this.url.Replace("/ws/", "/rest/"));

            // append tile coordinate
            url.AppendFormat("/tile/{0}/{1}/{2}", key.zoomLevel, key.x, key.y);

            // setup and append the profile part of the request
            url.AppendFormat("/{0}{1}", CustomProfile,
                string.Join("", EffectiveXMapLayers.Select(layer => layer.AsProfileSuffix()).ToArray()));

            // a valid object information sink requires us to request JSON
            // But: If we're dealing with base layers only, we don't need to request object information.
            if (MapObjectInformationSink != null && HasRealCustomLayers)
                url.Append("/json");

            // and finally append token authentication when set
            if (AuthenticateWithToken)
                url.AppendFormat("?xtok={0}", Password);

            // return full url string
            return url.ToString();
        }

        /// <inheritdoc/>
        public override Stream GetImageStream(int tileX, int tileY, int zoom)
        {
            // check auth parameters. We can only handle tile requests with none or token authentication; 
            // if the auth parameter do not fit that scheme, pass the GetImageStream-request to the base
            // class

            if (Authenticate && !AuthenticateWithToken)
                return base.GetImageStream(tileX, tileY, zoom);

            // call GetImageStream(MapSectionByTileKey) helper

            return GetImageStream(new MapSectionByTileKey {x = tileX, y = tileY, zoomLevel = zoom});
        }

        /// <summary>
        /// Requests a tile; Please refer to GetImageStream(int, int, int) for a description.
        /// </summary>
        /// <param name="key">The tile coordinate in form of a MapSectionByTileKey.</param>
        /// <returns>The stream wrapping the tile image.</returns>
        private Stream GetImageStream(MapSectionByTileKey key)
        {
            // create and initialize request
            var req = WebRequest.Create(GetTileUrl(key));
            req.Timeout = 8000;

            // get response
            var resp = req.GetResponse();

            // return response as is if content type is not application/json
            if (resp.ContentType != "application/json")
                return resp.GetResponseStream();

            // response is in json format; we need to read and parse the json string 
            try
            {
                using (var reader = new StreamReader(resp.GetResponseStream()))
                {
                    var jsonResponse = JSON.Parse(reader.ReadToEnd());

                    // parse the object information and call the object information sink, when set
                    MapObjectInformationSink?.Invoke(
                        ParseFeatures(jsonResponse),
                        new TileMapRectangle(key.x, key.y, key.zoomLevel),
                        new Size(256, 256)
                    );

                    // read and parse the image
                    return new MemoryStream(Convert.FromBase64String(jsonResponse["image"].Value));
                }
            }
            // be sure to close reponse when done
            finally { resp.Close(); }
        }

        /// <summary>
        /// Parses the feature information returned by xM-2 in JSON response. Be sure to check the remarks on TryGet.
        /// </summary>
        /// <param name="response">JSON object representing xM-2's response.</param>
        /// <returns></returns>
        private static IEnumerable<IMapObject> ParseFeatures(JSONNode response)
        {
            var mapObjects = new List<IMapObject>();

            // get features array
            if (response["features"] != null)
            {
                // loop through features
                for (var i = 0; i < response["features"].Count; ++i)
                {
                    // current feature
                    var feature = response["features"][i];

                    // feature attribute check
                    if (new[] { "id", "themeId", "referenceCoordinate", "referencePixelPoint", "pixelBoundingBox", "attributes" }.All(q => feature[q] != null))
                    {
                        var parsedFeature = new Feature
                        {
                            id = feature["id"].Value,
                            themeId = feature["themeId"].Value
                        };

                        var referenceCoordinate = feature["referenceCoordinate"];

                        // referenceCoordinate attribute check
                        if (new[] {"x", "y"}.All(q => referenceCoordinate[q] != null))
                            parsedFeature.referenceCoordinate = new Coordinate()
                            {
                                x = referenceCoordinate["x"].AsDouble,
                                y = referenceCoordinate["y"].AsDouble,
                                z = 0,
                                zSpecified = false
                            };

                        var referencePixelPoint = feature["referencePixelPoint"];

                        // referencePixelPoint attribute check
                        if (new[] {"x", "y"}.All(q => referencePixelPoint[q] != null))
                            parsedFeature.referencePixelPoint = new PixelPoint()
                            {
                                x = referencePixelPoint["x"].AsInt,
                                y = referencePixelPoint["y"].AsInt
                            };

                        var pixelBoundingBox = feature["pixelBoundingBox"];

                        // pixelBoundingBox attribute check
                        if (new[] {"left", "top", "right", "bottom"}.All(q => pixelBoundingBox[q] != null))
                            parsedFeature.pixelBoundingBox = new PixelBoundingBox()
                            {
                                left = pixelBoundingBox["left"].AsInt,
                                top = pixelBoundingBox["top"].AsInt,
                                right = pixelBoundingBox["right"].AsInt,
                                bottom = pixelBoundingBox["bottom"].AsInt
                            };


                        var attributes = feature["attributes"];
                        var attributeList = new List<KeyValuePair>();

                        // loop through attributes
                        for (var j = 0; j < attributes.Count; ++j)
                        {
                            // current attribute
                            var attribute = attributes[j];

                            // attribute check
                            if (new[] {"key", "value"}.All(q => attribute[q] != null))
                                attributeList.Add(new KeyValuePair
                                {
                                    key = attribute["key"].Value,
                                    value = attribute["value"].Value
                                });
                        }

                        if (attributeList.Any())
                            parsedFeature.attributes = attributeList.ToArray();

                        // if feature is valid add it to the list.
                        if (IsFeatureValid(parsedFeature))
                            mapObjects.Add(new XServer2Feature(parsedFeature));
                    }
                }
            }

            // return the list
            return mapObjects;
        }

        /// <summary>
        /// Checks if all mandatory fields of a Feature instance are set.
        /// </summary>
        /// <param name="feature">The Feature instance to check.</param>
        /// <returns>true, if the Feature instance is valid. False otherwise.</returns>
        private static bool IsFeatureValid(Feature feature)
        {
            return
                !string.IsNullOrEmpty(feature.id) &&
                !string.IsNullOrEmpty(feature.themeId) &&
                feature.pixelBoundingBox != null &&
                feature.referenceCoordinate != null &&
                feature.referencePixelPoint != null &&
                feature.attributes != null &&
                feature.attributes.Length > 0;
        }

        /// <inheritdoc/>
        public override byte[] TryGetStreamInternal(double left, double top, double right, double bottom, int width, int height)
        {
            // load effective layers into an array to avoid multiple enumerations
            var effectiveLayers = EffectiveXMapLayers
                .Select(layer => new { IsBaseLayer = BaseXMapLayers.Any(baseLayer => baseLayer.Name == layer.Name), Layer = layer })
                .ToArray();

            // setup size 
            var size = new System.Windows.Size(width, height);

            // setup map section
            var mapSection = new MapSectionByBounds()
            {
                bounds = new Bounds()
                {
                    minX = left,
                    maxX = right,
                    minY = Math.Min(top, bottom),
                    maxY = Math.Max(top, bottom)
                }
            };
            
            // TODO: implement layer visibility as soon as xMap-2 supports this through its SOAP interface
            //
            // the xMap-2 that was integrated was not able to disable base layers when using the SOAP. In debug versions this will 
            // be ignored; for release versions we're going to throw a NotImplementedException when any of the base layers hase 
            // been disabled.

            var disabledBaseLayers = effectiveLayers
                .Where(item => item.IsBaseLayer && !item.Layer.Enabled)
                .Select(item => item.Layer.Name)
                .ToArray();

            if (disabledBaseLayers.Length > 0)
            {
                var message = "currently base layers cannot be disabled when requesting map images by bounds [affects: " + string.Join(", ", disabledBaseLayers) + "]";
#if DEBUG
                Debug.WriteLine("WARNING: " + message);
#else
                if (!IgnoreMissingLayeredMapCapabilities)
                    throw new NotImplementedException(message);
#endif
            }

            // create and initialize the service
            using (var service = new XMap())
            {
                service.Url = url;
                service.Timeout = 8000;

                if (!string.IsNullOrEmpty(User) && !string.IsNullOrEmpty(Password))
                {
                    service.PreAuthenticate = true;
                    service.Credentials = new CredentialCache { { new Uri(url), "Basic", new NetworkCredential(User, Password) } };
                }

                // setup map image request
                var mapRequest = new MapRequest() {
                    imageOptions = new ImageOptions() {
                        format = ImageFormat.PNG, formatSpecified = true,
                        width = width, widthSpecified = true,
                        height = height, heightSpecified = true,
                    },
                    mapSection = mapSection,
                    coordinateFormat = "EPSG:76131",
                    storedProfile = CustomProfile,
                    resultFields = new ResultFields() {
                        image = true, imageSpecified = true
                    }
                };

                // TODO: assumption: all non-base layers in EffectiveXMapLayers are to be enabled via mapRequest.requestProfile.featureLayerProfile
                //
                // Turn enabled non-base layers into Theme elements. Furthermore, request object inormation for 
                // the Theme elements added when a map object information sink has been provided. 

                var featureLayerThemes = effectiveLayers
                    .Where(item => !item.IsBaseLayer && item.Layer.Enabled)
                    .Select(item => item.Layer.AsTheme())
                    .ToArray();

                if (featureLayerThemes.Any())
                {
                    mapRequest.requestProfile = new RequestProfile() {
                        featureLayerProfile = new FeatureLayerProfile() {
                            themes = featureLayerThemes
                        }
                    };

                    if (MapObjectInformationSink != null)
                        mapRequest.resultFields.featureThemeIds = featureLayerThemes.Select(theme => theme.id).ToArray();
                }

                // send request
                var mapResponse = service.renderMap(mapRequest);

#if DEBUG
                // for debug version, check if the image returned matches our request

                var requestedBounds = ((MapSectionByBounds)mapRequest.mapSection).bounds;

                if (!BoundingBoxesAreEqual(requestedBounds, mapResponse.bounds))
                    IssueBoundingBoxWarning(requestedBounds, mapResponse.bounds, width, height, mapRequest.storedProfile ?? "");
#endif

                // call object information sink when object information has been delivered
                if (MapObjectInformationSink != null && mapResponse.features != null)
                    MapObjectInformationSink(
                        mapResponse.features.Select(feature => new XServer2Feature(feature) as IMapObject),
                        new MapRectangle(mapSection.bounds.minX, mapSection.bounds.maxX, mapSection.bounds.minY, mapSection.bounds.maxY), 
                        size
                    );

                // done, return rendered map image
                return mapResponse.image;
            }
        }

#if DEBUG
        /// <summary>
        /// Convert a bounding box to a [minx, miny, maxx, maxy] representation.
        /// </summary>
        /// <param name="b">Bounding box to convert.</param>
        /// <returns>The [minx, miny, maxx, maxy] representation.</returns>
        public double[] GetMinMax(Bounds b)
        {
            return new[] { b.minX, b.minY, b.maxX, b.maxY };
        }

        /// <summary>
        /// Calculates the delta for the minx, miny, maxx and maxy values of the given bounding boxes.
        /// </summary>
        /// <param name="b1">First bounding box.</param>
        /// <param name="b2">Second bounding box.</param>
        /// <returns>Delta values, in this order: minx, miny, maxx, maxy.</returns>
        public IEnumerable<double> GetDelta(Bounds b1, Bounds b2)
        {
            var minMax1 = GetMinMax(b1);
            var minMax2 = GetMinMax(b2);

            for (var i = 0; i < 4; ++i)
                yield return Math.Abs(minMax1[i] - minMax2[i]);
        }

        /// <summary>
        /// Checks if two bounding boxes are equal.
        /// </summary>
        /// <param name="b1">First bounding box.</param>
        /// <param name="b2">Second bounding box.</param>
        /// <param name="epsilon">Allowable tolerance.</param>
        /// <returns>True, if the bounding boxes are equal. False otherwise.</returns>
        public bool BoundingBoxesAreEqual(Bounds b1, Bounds b2, double epsilon = 1e-4)
        {
            return GetDelta(b1, b2).All(delta => (delta <= epsilon));
        }

        /// <summary>
        /// Generates a diagnostic output.
        /// </summary>
        /// <param name="requested">Requested bounding box.</param>
        /// <param name="returned">Returned bounding box.</param>
        /// <param name="width">Requested width.</param>
        /// <param name="height">Requested height.</param>
        /// <param name="profile">Requested profile.</param>
        public void IssueBoundingBoxWarning(Bounds requested, Bounds returned, int width, int height, string profile)
        {
            double[] minMaxRequested = GetMinMax(requested);
            double[] minMaxReturned = GetMinMax(returned);
            double[] delta = GetDelta(requested, returned).ToArray();

            var culture = System.Threading.Thread.CurrentThread.CurrentCulture;
            System.Threading.Thread.CurrentThread.CurrentCulture = System.Globalization.CultureInfo.InvariantCulture;

            logger.Writeline(TraceEventType.Warning, String.Format("xMap2 did not return the requested map rectangle:\n" +
                "\trequested: [{3:0.000000}, {4:0.000000}, {5:0.000000}, {6:0.000000}], {1}x{2}, {0},\n" +
                "\t returned: [{7:0.000000}, {8:0.000000}, {9:0.000000}, {10:0.000000}]\n" +
                "\t    delta: [{11:0.000000}, {12:0.000000}, {13:0.000000}, {14:0.000000}]",
                profile, width, height, minMaxRequested[0], minMaxRequested[1], minMaxRequested[2],
                minMaxRequested[3], minMaxReturned[0], minMaxReturned[1], minMaxReturned[2],
                minMaxReturned[3], delta[0], delta[1], delta[2], delta[3]
            ));

            System.Threading.Thread.CurrentThread.CurrentCulture = culture;
        }
#endif

        /// <inheritdoc/>
        public override string CacheId 
        { 
            get
            {
                if (cacheId == null)
                {
                    cacheId = string.Join("|", new[]
                    {
                        $"PTVXMAP2",
                        $"url={url}",
                        $"mode={mode}",
                        $"layers={string.Join(",", EffectiveXMapLayers.Select(l => l.AsProfileSuffix()).ToArray())}",
                        $"usr={User ?? "<null>"}",
                        $"pwd={Password ?? "<null>"}",
                        $"custProfile={CustomProfile ?? "<null>"}"
                    });
                }

                return cacheId;
            } 
        }
    }

    /// <summary>
    /// Represents an layer element in the context of the xServer-2 map SOAP and REST service.
    /// </summary><remarks>
    /// TODO: This is the draft for replacing xM-1's Layer element as it is used in the context of XmapTiledProvider.CustomXMapLayers.
    /// </remarks>
    public class XMapLayer
    {
        /// <summary>
        /// Name of the layer.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Layer visibility.
        /// </summary>
        public bool Enabled { get; }

        /// <summary>
        /// Creates and initializes an instance of XMapLayer.
        /// </summary>
        /// <param name="name">Layer name.</param>
        /// <param name="enabled">Layer visibility.</param>
        private XMapLayer(string name, bool enabled)
        {
            Name = name;
            Enabled = enabled;
        }

        /// <summary>
        /// Creates and initializes an instance of XMapLayer.
        /// </summary>
        /// <param name="name">Layer name.</param>
        /// <remarks>The created layer will be enabled.</remarks>
        public static XMapLayer Create(string name)
        {
            var first = name.Substring(0, 1);

            return (first == "+" || first == "-")
                ? new XMapLayer(name.Substring(1), first == "+")
                : new XMapLayer(name, true);
        }

        /// <summary>
        /// Creates and initializes an instance of XMapLayer.
        /// </summary>
        /// <param name="name">Layer name.</param>
        /// <param name="enabled">Layer visibility.</param>
        public static XMapLayer Create(string name, bool enabled)
        {
            return new XMapLayer(name, enabled);
        }

        /// <summary>
        /// Turns the XMapLayer into a profile suffix as it is used in the xServer's REST API.
        /// </summary>
        /// <returns>Profile suffix.</returns>
        internal string AsProfileSuffix() => (Enabled ? "+" : "-") + Name;

        /// <summary>
        /// Turns the XMapLayer into a Theme element as it is used in the xServer's SOAP API.
        /// </summary>
        /// <returns>Theme element</returns>
        internal Theme AsTheme() => new Theme { id = Name, enabled = Enabled, enabledSpecified = true };
    }
}

