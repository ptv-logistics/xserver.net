// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Drawing;
using System.IO;
using Ptv.XServer.Controls.Map.Layers.Tiled;
using Ptv.XServer.Controls.Map.Tools.Reprojection;

namespace Ptv.XServer.Controls.Map.Layers.WmtsLayer
{
    /// <summary>
    /// Provider that is capable to produce tiles given a MapService that in turn is capable to render map sections
    /// in a specific CRS that differs from EPSG:76131. The ReprojectionTileProvider will internally use a ReprojectionService to 
    /// wrap MapService. This ReprojectionService is used to "map" the MapService's CRS to PTV Mercator using 
    /// image reprojection.
    /// </summary>
    public class ReprojectionTileProvider : ITiledProvider
    {
        // default tile; 256x256 PNG image, filled with transparent white
        private static readonly byte[] defaultTile =
            Color.FromArgb(0, 255, 255, 255).CreateImage(256, 256).StreamPng().ToArray();

        /// <summary>
        /// Creates and initializes an instance of ReprojectionTileProvider. The given map service will internally be wrapped in a ReprojectionService which is capable
        /// to map any source CRS to our EPSG:76131 using image reprojection.
        /// </summary>
        /// <param name="mapService">A map service that is capable to render map images, given the bounding box and the pixel size of the map section to render.</param>
        /// <param name="reprojectionServiceOptions">Additional options that are passed to the ReprojectionService. Optional, defaults to null.</param>
        public ReprojectionTileProvider(MapService mapService, ReprojectionServiceOptions reprojectionServiceOptions = null)
        {
            MinZoom = 0;
            MaxZoom = 19;

            // initialize CacheId, create a unique identifier
            CacheId = $"{GetType().FullName}.{Guid.NewGuid().ToString()}";

            // wrap MapService in a ReprojectionService. 
            // The ReprojectionService is capable to map the source CRS to EPSG:76131 using image reprojection.
            ReprojectionService = new ReprojectionService(mapService, "EPSG:76131", null, reprojectionServiceOptions);
        }

        /// <inheritdoc/>  
        public string CacheId { get; set;  }

        /// <inheritdoc/>  
        public int MaxZoom { get; set;  }

        /// <inheritdoc/>  
        public int MinZoom { get; set;  }

        /// <summary>
        /// Gets the ReprojectionService that wraps the outer MapService and is used to render our tiles.
        /// </summary>
        protected ReprojectionService ReprojectionService { get; }
        
        /// <inheritdoc/>  
        public Stream GetImageStream(int x, int y, int zoom)
        {
            // get tile bounds in mercator units
            var rect = ReprojectionProvider.TileToSphereMercator(x, y, zoom, 6371000);
            var mapRect = new Tools.Reprojection.MapRectangle(rect.Left, rect.Bottom, rect.Right, rect.Top);

            // Request the map tile from the ReprojectionService. The returned image stream may be 
            // null, e.g. when the requested tile is out of bounds regarding the inner MapServer configuration. 
            // In that case return a default  tile preventing the map from displaying enlarged images from other 
            // zoom levels (> avoids "zoom artifacts").
            return (ReprojectionService.GetImageStream(mapRect, new Size(256, 256)) ?? new MemoryStream(defaultTile)).Reset();
        }
    }
}
