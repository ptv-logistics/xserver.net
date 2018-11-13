// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Media;
using Ptv.XServer.Controls.Map.Canvases;
using Ptv.XServer.Controls.Map.Layers.Tiled;
using Ptv.XServer.Controls.Map.Layers.Untiled;
using Ptv.XServer.Controls.Map.Tools.Reprojection;

namespace Ptv.XServer.Controls.Map.Layers
{
    /// <summary> Customized layer type, which is capable of integrating Web Map Services (WMS) into the map control. </summary>
    /// <remarks> Coordinate reference systems different from the PTV Mercator format can be integrated, because a reprojection
    /// is transformed to map these reference systems to the internally used format.</remarks>
    public class WmsLayer : ILayer
    {
        private readonly ILayer wrappedLayer;

        /// <summary> Creates and initializes a new layer which integrates the images provided by Web Map Services into the map control.  </summary>
        /// <param name="urlTemplate">URL needed for retrieving images from the corresponding Web Map Service. The layout of this URL must
        /// achieve the requirements according the OpenGIS Specification, details can be found in http://www.opengeospatial.org/standards/wms .
        /// <br />
        /// Because the visible region of a map control changes during a user session, and the size in pixel may vary, some parameters in the URL
        /// query string have to be parameterized. I.e. the parameters 'BBOX', 'WIDTH' and 'HEIGHT' must be used in a parameterized way: They have to
        /// look like: ..&amp;BBOX=${boundingbox}&amp;WIDTH=${width}&amp;HEIGHT=${height}.."
        /// </param>
        /// <param name="isTiled"> Indicating if a tiled variant is used for filling the map control with content, or not. </param>
        /// <param name="isBaseMap"> Indicating if the content represents a background information which completely fills the drawing area of the map control
        /// (= true), or only punctual information is shown (= false). This value influences the sequence order when the different layers are drawn. </param>
        /// <param name="name"> Name of the layer in the internal layer management. </param>
        /// <param name="copyRight">Copyright text visible in the lower right corner of the map control. </param>
        /// <param name="timeout">Longest time waiting for WMS request.</param>
        public WmsLayer(string urlTemplate, bool isTiled, bool isBaseMap, string name, string copyRight = null, int timeout = 8000)
        {
            var canvasCategories = new[] { isBaseMap ? CanvasCategory.BaseMap : CanvasCategory.Content };
            ReprojectionProvider = new ReprojectionProvider(urlTemplate, timeout);
            wrappedLayer = isTiled
                ? (ILayer) new TiledLayer(name) { TiledProvider = ReprojectionProvider, Copyright = copyRight, CanvasCategories = canvasCategories }
                : new UntiledLayer(name) { UntiledProvider = ReprojectionProvider, Copyright = copyRight, CanvasCategories = canvasCategories };

            // In LayerCollection the first parameter of PropertyChanged method is determined by class BaseLayer, which is equal to the wrapper layer. 
            // But this layer wasn't inserted in the LayerCollection, so the sender has to be corrected. 
            wrappedLayer.PropertyChanged += (_, e) => PropertyChanged?.Invoke(this, e);
        }

        /// <summary> Occurs when a property value changes. </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <inheritdoc/>
        public string Name => wrappedLayer.Name;
        /// <inheritdoc/>
        public int Priority {
            get => wrappedLayer.Priority;
            set => wrappedLayer.Priority = value;
        }
        /// <inheritdoc/>
        public string Caption {
            get => wrappedLayer.Caption;
            set => wrappedLayer.Caption = value;
        }
        /// <inheritdoc/>
        public ImageSource Icon {
            get => wrappedLayer.Icon;
            set => wrappedLayer.Icon = value;
        }
        /// <inheritdoc/>
        public double Opacity {
            get => wrappedLayer.Opacity;
            set => wrappedLayer.Opacity = value;
        }
        /// <inheritdoc/>
        public void AddToMapView(MapView mapViewToAdd) => wrappedLayer.AddToMapView(mapViewToAdd); 
        /// <inheritdoc/>
        public void RemoveFromMapView(MapView mapView) => wrappedLayer.RemoveFromMapView(mapView); 
        /// <inheritdoc/>
        public string Copyright => wrappedLayer.Copyright;

        /// <inheritdoc/>
        public bool HasSettingsDialog => wrappedLayer.HasSettingsDialog;

        /// <inheritdoc/>
        public CanvasCategory[] CanvasCategories => wrappedLayer.CanvasCategories;

        /// <summary> Provider used for retrieving images from the specified URL and re-projecting it if necessary. </summary>
        public readonly ReprojectionProvider ReprojectionProvider;
    }

    /// <summary> Provider, which is capable of matching coordinate reference systems, which 
    /// commonly do not match, because of different used projections. For example, Mercator
    /// projection does not match Gauss-Krueger-Format. 
    /// </summary>
    public class ReprojectionProvider : ITiledProvider, IUntiledProvider
    {
        private readonly ReprojectionService reprojectionService;

        /// <summary>
        /// Constructor of the reprojection provider, which performs an image processing
        /// to match coordinate reference systems, which commonly differ in their projections.
        /// </summary>
        /// <param name="urlTemplate">URL needed for retrieving images from the corresponding Web Map Service. The layout of this URL must
        /// achieve the requirements according the OpenGIS Specification, details can be found in http://www.opengeospatial.org/standards/wms .
        /// <br />
        /// Because the visible region of a map controls changes during a user session, and the size in pixel may vary, some parameters in the URL
        /// query string have to be parameterized. I.e. the parameters 'BBOX', 'WIDTH' and 'HEIGHT' must used in a parameterized way: They have to
        /// look like: ..&amp;BBOX=${boundingbox}&amp;WIDTH=${width}&amp;HEIGHT=${height}.."
        /// </param>
        /// <param name="timeout">Longest time waiting for WMS request.</param>
        public ReprojectionProvider(string urlTemplate, int timeout = 8000)
        {
            CacheId = urlTemplate;

            // create source service; a WMS service using the template above 
            var wmsService = new WmsMapService(CacheId);
            // hook event, customize timeout
            wmsService.OnRequestCreated += request =>
            {
                request.Timeout = timeout;

                if (!(request is HttpWebRequest httpWebRequest)) return;

                httpWebRequest.UserAgent = UserAgent;
                if (Proxy != null)
                    httpWebRequest.Proxy = Proxy;
            };

            // create the reprojection service that re-projects the wms images
            reprojectionService = new ReprojectionService(wmsService, "EPSG:76131");
        }

        /// <inheritdoc/>
        public string CacheId { get; }

        /// <inheritdoc/>
        public Stream GetImageStream(int x, int y, int z)
        {
            var rect = TileToSphereMercator(x, y, z, 6371000); // PTV mercator, exists somewhere in core :)
            return reprojectionService.GetImageStream(new Tools.Reprojection.MapRectangle(rect.Left, rect.Bottom, rect.Right, rect.Top), new System.Drawing.Size(256, 256));
        }

        /// <inheritdoc/>
        public int MaxZoom => 23;

        /// <inheritdoc/>
        public int MinZoom => 0;

        /// <summary>
        /// Calculate the Mercator bounds for a tile key
        /// </summary>
        public static Rect TileToSphereMercator(int x, int y, int z, double radius = 6378137)
        {
            double earthHalfCircum = radius * Math.PI;
            double arc = earthHalfCircum * 2.0 / Math.Pow(2, z);

            double x1 = x * arc - earthHalfCircum;
            double y1 = earthHalfCircum - y * arc;
            double x2 = (x + 1) * arc - earthHalfCircum;
            double y2 = earthHalfCircum - (y + 1) * arc;

            return new Rect(new Point(x1, y2), new Point(x2, y1));
        }

        /// <inheritdoc/>
        public Stream GetImageStream(double left, double top, double right, double bottom, int width, int height)
        {
            return reprojectionService.GetImageStream(new Tools.Reprojection.MapRectangle(left, bottom, right, top), new System.Drawing.Size(width, height));
        }

        private static string Clean(string toClean) => string.IsNullOrEmpty(toClean = toClean?.Trim()) ? null : toClean;

        private string userAgent;
        /// <summary> Gets or sets the value of the user agent HTTP header. </summary>
        public string UserAgent
        {
            get => userAgent;
            set => userAgent = Clean(value);
        }

        /// <summary> Gets or sets the proxy used for a web request. Especially for setting the proxy URI
        /// and/or credentials, a new WebProxy object can be created assigned to this property. </summary>
        public IWebProxy Proxy { get; set; }
    }    
}
