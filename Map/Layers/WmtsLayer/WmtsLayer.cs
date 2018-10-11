// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using Ptv.XServer.Controls.Map.Layers.Tiled;

namespace Ptv.XServer.Controls.Map.Layers.WmtsLayer
{
    /// <summary>A layer which draws geographic content based on Web Map Tile Services (WMTS)
    /// by means of pre-rendered or run-time computed geo-referenced map tiles. For its major object
    /// only two resources are needed: An URL template for addressing each individual tile and a
    /// customized classification of the different zoom levels for which tiles have to be generated for.
    /// </summary>
    public class WmtsLayer : TiledLayer
    {
        /// <summary> Initializes a new instance of the <see cref="WmtsLayer"/> class. </summary>
        /// <param name="name"> The unique name of the layer. </param>
        /// <param name="urlTemplate">Template of an URL which is needed for addressing individual tiles
        /// by means of placeholders for x, y and z (zooming level) parameters. As a typical representative
        /// the following template can be used:
        /// <c>http://laermkartierung1.eisenbahn-bundesamt.de/mapproxy/wmts/ballungsraum_wmts/wmtsgrid/{z}/{x}/{y}.png</c></param>
        /// <param name="tileMatrixSet">WMTS uses a tile matrix set to split the map into equal sized squares at different zoom levels. 
        /// Such a square (so-called <em>tile</em>) is a matrix image which contains geographic data.
        /// So a map is cut into multiple tiles according to a fixed scale, which is called a tile matrix.
        /// Multiple matrices can be defined for different resolutions, resulting in a <em>tile matrix set</em> composed of one or more tile matrices. 
        /// Each tile matrix uses a tile matrix identifier, commonly the position of the matrix in the set, 
        /// the layer with the lowest resolution to be Layer 0. Further details can be seen 
        /// <a href="https://www.supermap.com/EN/online/iServer%20Java%206R/API/WMTS/wmts_introduce.htm">here</a>.
        /// The used tile matrix set by a service can be determined via the WMTS capabilities request. In the example shown for URL
        /// template the following tile matrix set was extracted from the capabilities 
        /// <c>http://laermkartierung1.eisenbahn-bundesamt.de/mb3/WMTSCapabilities.xml</c>:
        /// <code>
        /// new TileMatrixSet("EPSG:25832")
        /// {
        ///     new TileMatrix("00", 12980398.9955000000, 204485.0, 6134557.0,      1,      1),
        ///     new TileMatrix("01",  6490199.4977700000, 204485.0, 6134557.0,      2,      2),
        ///     new TileMatrix("02",  3245099.7488800000, 204485.0, 6134557.0,      4,      4),
        ///     new TileMatrix("03",  1622549.8744400000, 204485.0, 6134557.0,      7,      8),
        ///     new TileMatrix("04",   811274.9372210000, 204485.0, 6134557.0,     14,     16),
        ///     new TileMatrix("05",   405637.4686100000, 204485.0, 6134557.0,     28,     32),
        ///     new TileMatrix("06",   202818.7343050000, 204485.0, 6134557.0,     56,     64),
        ///     new TileMatrix("07",   101409.3671530000, 204485.0, 6134557.0,    111,    128),
        ///     new TileMatrix("08",    50704.6835763000, 204485.0, 6134557.0,    222,    256),
        ///     new TileMatrix("09",    25352.3417882000, 204485.0, 6134557.0,    443,    512),
        ///     new TileMatrix("10",    12676.1708941000, 204485.0, 6134557.0,    885,   1024),
        ///     new TileMatrix("11",     6338.0854470400, 204485.0, 6134557.0,   1770,   2048),
        ///     new TileMatrix("12",     3169.0427235200, 204485.0, 6134557.0,   3540,   4096),
        ///     new TileMatrix("13",     1584.5213617600, 204485.0, 6134557.0,   7080,   8192),
        ///     new TileMatrix("14",      792.2606808800, 204485.0, 6134557.0,  14160,  16384),
        ///     new TileMatrix("15",      396.1303404400, 204485.0, 6134557.0,  28320,  32768),
        ///     new TileMatrix("16",      198.0651702200, 204485.0, 6134557.0,  56639,  65536),
        ///     new TileMatrix("17",       99.0325851100, 204485.0, 6134557.0, 113278, 131072),
        ///     new TileMatrix("18",       49.5162925550, 204485.0, 6134557.0, 226555, 262144),
        ///     new TileMatrix("19",       24.7581462775, 204485.0, 6134557.0, 453109, 524288)
        /// };
        /// </code></param>
        public WmtsLayer(string name, string urlTemplate, TileMatrixSet tileMatrixSet) : base(name)
        {
            TileMatrixSet = tileMatrixSet;
            UrlTemplate = urlTemplate;
        }

        private string urlTemplate;

        /// <summary>Template of an URL which is needed for addressing individual tiles
        /// by means of placeholders for x, y and z (zooming level) parameters. As a typical representative
        /// the following template can be used:
        /// <c>http://laermkartierung1.eisenbahn-bundesamt.de/mapproxy/wmts/ballungsraum_wmts/wmtsgrid/{z}/{x}/{y}.png</c>
        /// </summary>
        public string UrlTemplate
        {
            get => urlTemplate;
            set
            {
                WmtsMapService service = new WmtsMapService(
                            value,
                            TileMatrixSet.CRS, // the CRS of the WMTS service
                            TileMatrixSet.SelectTileMatrix, // the delegate that selects the best matching tile matrix from a tile matrix set
                            TileMatrixSet.ApproximateBoundingBox("EPSG:76131") // the area covered by the WMTS in PTV Mercator
                        );

                // wrap the service in a ReprojectionTileProvider which can actually feed a standard TiledLayer.
                TiledProvider = new ReprojectionTileProvider(service);

                urlTemplate = value;

                Refresh();
            }
        }

        /// <summary>WMTS uses a tile matrix set to split the map into equal sized squares at different zoom levels. 
        /// Such a square (so-called <em>tile</em>) is a matrix image which contains geographic data.
        /// So a map is cut into multiple tiles according to a fixed scale, which is called a tile matrix.
        /// Multiple matrices can be defined for different resolutions, resulting in a <em>tile matrix set</em> composed of one or more tile matrices. 
        /// Each tile matrix uses a tile matrix identifier, commonly the position of the matrix in the set, 
        /// the layer with the lowest resolution to be Layer 0. Further details can be seen 
        /// <a href="https://www.supermap.com/EN/online/iServer%20Java%206R/API/WMTS/wmts_introduce.htm">here</a>.
        /// The used tile matrix set by a service can be determined via the WMTS capabilities request. In the example shown for URL
        /// template the following tile matrix set was extracted from the capabilities 
        /// <c>http://laermkartierung1.eisenbahn-bundesamt.de/mb3/WMTSCapabilities.xml</c>:
        /// <code>
        /// new TileMatrixSet("EPSG:25832")
        /// {
        ///     new TileMatrix("00", 12980398.9955000000, 204485.0, 6134557.0,      1,      1),
        ///     new TileMatrix("01",  6490199.4977700000, 204485.0, 6134557.0,      2,      2),
        ///     new TileMatrix("02",  3245099.7488800000, 204485.0, 6134557.0,      4,      4),
        ///     new TileMatrix("03",  1622549.8744400000, 204485.0, 6134557.0,      7,      8),
        ///     new TileMatrix("04",   811274.9372210000, 204485.0, 6134557.0,     14,     16),
        ///     new TileMatrix("05",   405637.4686100000, 204485.0, 6134557.0,     28,     32),
        ///     new TileMatrix("06",   202818.7343050000, 204485.0, 6134557.0,     56,     64),
        ///     new TileMatrix("07",   101409.3671530000, 204485.0, 6134557.0,    111,    128),
        ///     new TileMatrix("08",    50704.6835763000, 204485.0, 6134557.0,    222,    256),
        ///     new TileMatrix("09",    25352.3417882000, 204485.0, 6134557.0,    443,    512),
        ///     new TileMatrix("10",    12676.1708941000, 204485.0, 6134557.0,    885,   1024),
        ///     new TileMatrix("11",     6338.0854470400, 204485.0, 6134557.0,   1770,   2048),
        ///     new TileMatrix("12",     3169.0427235200, 204485.0, 6134557.0,   3540,   4096),
        ///     new TileMatrix("13",     1584.5213617600, 204485.0, 6134557.0,   7080,   8192),
        ///     new TileMatrix("14",      792.2606808800, 204485.0, 6134557.0,  14160,  16384),
        ///     new TileMatrix("15",      396.1303404400, 204485.0, 6134557.0,  28320,  32768),
        ///     new TileMatrix("16",      198.0651702200, 204485.0, 6134557.0,  56639,  65536),
        ///     new TileMatrix("17",       99.0325851100, 204485.0, 6134557.0, 113278, 131072),
        ///     new TileMatrix("18",       49.5162925550, 204485.0, 6134557.0, 226555, 262144),
        ///     new TileMatrix("19",       24.7581462775, 204485.0, 6134557.0, 453109, 524288)
        /// };
        /// </code>
        /// </summary>
        public TileMatrixSet TileMatrixSet { get; }
    }
}
