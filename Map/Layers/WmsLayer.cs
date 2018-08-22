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
        private readonly ILayer layer;

        /// <summary> Create and initialize a new layer which integrates the images provided by Web Map Services into the map control.  </summary>
        /// <param name="urlTemplate">URL needed for retrieving images from the corresponding Web Map Service. The layout of this URL must
        /// achieve the requirements according the OpenGIS Specification, details can be found in http://www.opengeospatial.org/standards/wms .
        /// <br />
        /// Because the visible region of a map control changes during a user session, and the size in pixel may vary, some parameters in the URL's
        /// query string have to be parameterized. I.e. the parameters 'BBOX', 'WIDTH' and 'HEIGHT' must be used in a parameterized way: They have to
        /// look like: ..&amp;BBOX=${boundingbox}&amp;WIDTH=${width}&amp;HEIGHT=${height}.."
        /// </param>
        /// <param name="isTiled"> Indicating if a tiled variant is used for filling the map control with content, or not. </param>
        /// <param name="isBaseMap"> Indicating if the content represents a background information which completely fills the drawing area of the map control
        /// (= true), or only punctual information is shown (= false). This value influences the sequence order when the different layers are drawn. </param>
        /// <param name="name"> Name of the layer in the internal layer management. </param>
        /// <param name="copyRight">Copyright text visible in the lower right corner of the map control. </param>
        public WmsLayer(string urlTemplate, bool isTiled, bool isBaseMap, string name, string copyRight = null)
        {
            var canvasCategories = new[] { isBaseMap ? CanvasCategory.BaseMap : CanvasCategory.Content };
            ReprojectionProvider = new ReprojectionProvider(urlTemplate);
            layer = isTiled
                ? (ILayer)new TiledLayer(name) { TiledProvider = ReprojectionProvider, Copyright = copyRight, CanvasCategories = canvasCategories }
                : new UntiledLayer(name) { UntiledProvider = ReprojectionProvider, Copyright = copyRight, CanvasCategories = canvasCategories };
        }

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;
        /// <inheritdoc/>
        public string Name => layer.Name;
        /// <inheritdoc/>
        public int Priority { get { return layer.Priority; } set { layer.Priority = value; } }
        /// <inheritdoc/>
        public string Caption { get { return layer.Caption; } set { layer.Caption = value; } }
        /// <inheritdoc/>
        public ImageSource Icon { get { return layer.Icon; } set { layer.Icon = value; } }
        /// <inheritdoc/>
        public double Opacity { get { return layer.Opacity; } set { layer.Opacity = value; } }
        /// <inheritdoc/>
        public void AddToMapView(MapView mapView) { layer.AddToMapView(mapView); }
        /// <inheritdoc/>
        public void RemoveFromMapView(MapView mapView) { layer.RemoveFromMapView(mapView); }
        /// <inheritdoc/>
        public string Copyright => layer.Copyright;

        /// <inheritdoc/>
        public bool HasSettingsDialog => layer.HasSettingsDialog;

        /// <inheritdoc/>
        public CanvasCategory[] CanvasCategories => layer.CanvasCategories;

        /// <summary> Provider used for retrieving images from the specified URL and reprojecting it if necessary. </summary>
        public readonly ReprojectionProvider ReprojectionProvider;
    }

    /// <summary> Provider, which is capable of matching coordinate reference systems, which 
    /// commonly do not match, because of different used projections. For example, Mercator
    /// projection does not match Gauss-Krueger-Format. 
    /// </summary>
    public class ReprojectionProvider : ITiledProvider, IUntiledProvider
    {
        private readonly string template;
        private readonly ReprojectionService reprojectionService;

        /// <summary>
        /// Constructor of the reprojection provider, which performs an image processing
        /// to match coordinate reference systems, which commonly differ in their projections.
        /// </summary>
        /// <param name="urlTemplate">URL needed for retrieving images from the corresponding Web Map Service. The layout of this URL must
        /// achieve the requirements according the OpenGIS Specification, details can be found in http://www.opengeospatial.org/standards/wms .
        /// <br />
        /// Because the visible region of a map controls changes during a user session, and the size in pixel may vary, some parameters in the URL's
        /// query string have to be parameterized. I.e. the parameters 'BBOX', 'WIDTH' and 'HEIGHT' must used in a parameterized way: They have to
        /// look like: ..&amp;BBOX=${boundingbox}&amp;WIDTH=${width}&amp;HEIGHT=${height}.."
        /// </param>
        public ReprojectionProvider(string urlTemplate)
        {
            template = urlTemplate;

            // create source service; a WMS service using the template above 
            var wmsService = new WmsMapService(template);
            // hook event, customize timeout
            wmsService.OnRequestCreated += request =>
            {
                request.Timeout = 8000;

                var httpWebRequest = request as HttpWebRequest;
                if (httpWebRequest != null)
                {
                    httpWebRequest.UserAgent = UserAgent;
                    if (Proxy != null)
                        httpWebRequest.Proxy = Proxy;
                }
            };

            // create the reprojection service that re-projects the wms images
            reprojectionService = new ReprojectionService(wmsService, "EPSG:76131");
        }

        /// <inheritdoc/>
        public string CacheId => template;

        /// <inheritdoc/>
        public Stream GetImageStream(int x, int y, int z)
        {
            var rect = TileToSphereMercator(x, y, z, 6371000); // PTV mercator, gibt's wahrscheinlich auch irgendwo im Core :)
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
            double earthCircum = earthHalfCircum * 2.0;

            double arc = earthCircum / Math.Pow(2, z);
            double x1 = earthHalfCircum - x * arc;
            double y1 = earthHalfCircum - y * arc;
            double x2 = earthHalfCircum - (x + 1) * arc;
            double y2 = earthHalfCircum - (y + 1) * arc;

            return new Rect(new Point(-x1, y2), new Point(-x2, y1));
        }

        /// <inheritdoc/>
        public Stream GetImageStream(double left, double top, double right, double bottom, int width, int height)
        {
            return reprojectionService.GetImageStream(new Tools.Reprojection.MapRectangle(left, bottom, right, top), new System.Drawing.Size(width, height));
        }


        private string Clean(string toClean)
        {
            if (toClean == null) return null;

            toClean = toClean.Trim();
            return string.IsNullOrEmpty(toClean) ? null : toClean;
        }

        private string userAgent;
        /// <summary> Gets or sets the value of the user agent HTTP header. </summary>
        public string UserAgent
        {
            get { return userAgent; }
            set { userAgent = Clean(value); }
        }

        /// <summary> Gets or sets the proxy used for a web request. Especially for setting the proxy URI
        /// and/or credentials, a new WebProxy object can be created assigned to this property. </summary>
        public IWebProxy Proxy { get; set; }
    }    
}
