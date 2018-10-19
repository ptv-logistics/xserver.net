// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Linq;

using Ptv.XServer.Controls.Map.Layers;
using Ptv.XServer.Controls.Map.Tools;
using Ptv.XServer.Controls.Map.Gadgets;
using Ptv.XServer.Controls.Map.Layers.Tiled;
using Ptv.XServer.Controls.Map.Layers.Untiled;
using Ptv.XServer.Controls.Map.TileProviders;
using Ptv.XServer.Controls.Map.Layers.Xmap2;

// ReSharper disable once CheckNamespace
namespace Ptv.XServer.Controls.Map
{
    /// <summary>
    /// <para>
    /// The <see cref="Map"/> namespace contains the classes to visualize your data in an interactive map.
    /// </para>
    /// <para>
    /// The main type is the <see cref="WpfMap"/>, which can be added to a XAML user control. An alternative
    /// <see cref="FormsMap"/> can be used for easy WinForms integration. Both controls inherit from
    /// <see cref="Map"/> and implement <see cref="IMap"/>. The Map type can be used to build up a
    /// customized map control.
    /// </para>
    /// </summary>
    [System.Runtime.CompilerServices.CompilerGenerated]
    class NamespaceDoc
    {
        // NamespaceDoc is used for providing the root documentation for Ptv.Components.Projections
        // hint taken from http://stackoverflow.com/questions/156582/namespace-documentation-on-a-net-project-sandcastle
    }

    /// <summary> This class represents the basic map control without any gadgets. It is used as the WPFMap's base class,
    /// and as a proxy for the WinForms map control. So, it is possible to provide a common implementation for both
    /// environments, i.e. WPF and WinForms.</summary>
    public class Map : UserControl, IMap, IToolTipManagement, IDisposable
    {    
        #region constructor
        /// <summary> Initializes a new instance of the <see cref="Map"/> class. By default, the map uses animation
        /// and the scale is shown in km. </summary>
        public Map()
        {
            Loaded += Map_Loaded;

            DefaultThemeResources = new ResourceDictionary { Source = new Uri("Ptv.XServer.Controls.Map;component/Resources/Themes/PTVDefault.xaml", UriKind.Relative) };
            UseDefaultTheme = true;
            UseMiles = false;
            InitializeToolTipManagement();
        }

        private void InitializeToolTipManagement()
        {
            ToolTipManagement = new ToolTipManagement(this)
            {
                IsHitTestOKFunc = currentPosition => VisualTreeHelper.HitTest(this, currentPosition)?.VisualHit?.FindAncestor<MapGadget>() == null,
                FillToolTipMapObjectsFunc = (position, maxPixelDistance) => Layers.OfType<IToolTips>().SelectMany(layer => layer.Get(position, maxPixelDistance))
            };
        }
        #endregion


        #region events
        /// <inheritdoc/>  
        public event EventHandler ViewportBeginChanged
        {
            add => mapView.ViewportBeginChanged += value;
            remove => mapView.ViewportBeginChanged -= value;
        }

        /// <inheritdoc/>  
        public event EventHandler ViewportEndChanged
        {
            add => mapView.ViewportEndChanged += value;
            remove => mapView.ViewportEndChanged -= value;
        }

        /// <inheritdoc/>  
        public event EventHandler ViewportWhileChanged
        {
            add => mapView.ViewportWhileChanged += value;
            remove => mapView.ViewportWhileChanged -= value;
        }
        #endregion //events

        #region private variables

        /// <summary> The style profile of the xMapServer base map. </summary>
        private string xmapStyle = "";
        /// <summary> The text block which displays the hint for the missing xMap url. </summary>
        protected TextBlock copyrightHintText;

        private string xMapCopyright;
        #endregion

        #region protected variables
        /// <summary> MapView to be displayed in the map. </summary>
        protected MapView mapView = new MapView { Name = "Map" };
        #endregion

        #region public variables
        /// <summary> Dictionary holding all existing gadgets like Scale gadget, zoom slider etc. </summary>
        public ObservableDictionary<GadgetType, IGadget> Gadgets = new ObservableDictionary<GadgetType, IGadget>();

        /// <summary> Gets or sets the default theme. The default theme is initialized in the WpfMap.xaml file and used
        /// by <see cref="UseDefaultTheme"/>. </summary>
        public ResourceDictionary DefaultThemeResources { get; set; }
        #endregion

        #region event handling
        /// <summary> Event handler for a successful load of the map. Shows the map in a grid. </summary>
        /// <param name="sender"> Sender of the Loaded event. </param>
        /// <param name="e"> Event parameters. </param>
        private void Map_Loaded(object sender, RoutedEventArgs e)
        {
            Loaded -= Map_Loaded;

            SetXMapUrlHint();
            ((Grid)Content).Children.Insert(0, mapView);

            foreach (var view in MapElementExtensions.FindChildren<MapView>(this))
                Layers.Register(view);
        }

        private void SetXMapUrlHint()
        {
            if (copyrightHintText != null)
                mapView.Layers.Children.Remove(copyrightHintText);

            if (!DesignTest.DesignMode || !string.IsNullOrEmpty(XMapUrl)) return;

            copyrightHintText = new TextBlock { Text = "Please insert a valid xMapServer URL to display a base map." };
            Panel.SetZIndex(copyrightHintText, 1024);
            copyrightHintText.TextWrapping = TextWrapping.Wrap;
            copyrightHintText.HorizontalAlignment = HorizontalAlignment.Center;
            copyrightHintText.VerticalAlignment = VerticalAlignment.Center;
            mapView.Layers.Children.Add(copyrightHintText);
        }

        /// <summary> Event indicating a change of the UseMiles property. It can be used to update the scale gadget. </summary>
        public event EventHandler UseMilesChanged;

        /// <inheritdoc/>
        public ToolTipManagement ToolTipManagement { get; private set; }

        /// <inheritdoc/>
        public LayerFactory Xmap2LayerFactory { get; internal set; }

        /// <inheritdoc/>
        bool IToolTipManagement.IsEnabled
        {
            get => ToolTipManagement.IsEnabled;
            set => ToolTipManagement.IsEnabled = value;
        }

        /// <inheritdoc/>
        public int ToolTipDelay
        {
            get => ToolTipManagement.ToolTipDelay;
            set => ToolTipManagement.ToolTipDelay = value;
        }

        /// <inheritdoc/>
        public double MaxPixelDistance
        {
            get => ToolTipManagement.MaxPixelDistance;
            set => ToolTipManagement.MaxPixelDistance = value;
        }


        /// <summary> Flag to choose whether the metric or the mile based system is to be used for example for the scale display. </summary>
        private bool useMiles;
        /// <summary> Gets or sets a value indicating whether the scale is displayed in miles or it is displayed in km. </summary>
        public bool UseMiles
        {
            get => useMiles;
            set
            {
                useMiles = value;
                UseMilesChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <summary> Flag indicating whether the default theme is to be used for gadget display. </summary>
        private bool bUseDefaultTheme;

        /// <summary><para> Gets or sets a value indicating whether the built-in PTV theme should be used, and thus
        /// overriding an optionally set style. The application theme can be set in the App.xaml. If no theme at all is
        /// set and the default PTV theme is not used either, the UI will look like the current Windows theme. </para>
        /// <para> See the <conceptualLink target="5e97e57f-ad50-4dda-af0b-e117af8c4fcd"/> topic for an example. </para></summary>
        public bool UseDefaultTheme
        {
            get => bUseDefaultTheme;
            set
            {
                bUseDefaultTheme = value;
                Resources.MergedDictionaries.Clear();

                if (bUseDefaultTheme)
                {
                    Resources.MergedDictionaries.Add(DefaultThemeResources);
                }
            }
        }

        /// <inheritdoc/>  
        public LayerCollection Layers { get; } = new LayerCollection();

        /// <inheritdoc/>  
        public string XMapUrl
        {
            get => xmapUrl;
            set
            {
                if (xmapUrl == value) return;

                xServerVersion = GetAllXserverVersions(value).FirstOrDefault(xServer => xServer.IsValidUrl());

                xmapUrl = value;

                InitializeMapLayers();
                SetXMapUrlHint();
            }
        }

        private string xmapUrl = "";

        private static IEnumerable<IXServerVersion> GetAllXserverVersions(string url)
        {
            yield return new XServer1Version(url);
            yield return new XServer2Version(url);
        }

        /// <inheritdoc/>  
        public string XMapCredentials
        {
            get => xmapCredentials;
            set
            {
                if (xmapCredentials == value) return;
                xmapCredentials = value;

                if (xServerVersion?.IsCloudBased() ?? false)
                    InitializeMapLayers();
            }
        }

        private string xmapCredentials = "";

        private void InitializeMapLayers()
        {
            if (xServerVersion == null || (xServerVersion.IsCloudBased() && string.IsNullOrEmpty(xmapCredentials)))
                return;

            xServerVersion.InitializeMapLayers(this, xmapCredentials);
        }

        private IXServerVersion xServerVersion;

        /// <inheritdoc/>  
        public string XMapStyle
        {
            get => xmapStyle;
            set
            {
                xmapStyle = value;

                if (Layers["Background"] is TiledLayer tiledLayer)
                {
                    if (tiledLayer.TiledProvider is XMapTiledProvider xMapTiledProvider)
                        xMapTiledProvider.CustomProfile = xmapStyle != null ? xmapStyle + "-bg" : null;
                    tiledLayer.Refresh();
                }

                if (Layers["Labels"] is UntiledLayer untiledLayer)
                {
                    if (untiledLayer.UntiledProvider is XMapTiledProvider xMapTiledProvider)
                        xMapTiledProvider.CustomProfile = xmapStyle != null ? xmapStyle + "-fg" : null;
                    untiledLayer.Refresh();
                }
            }
        }

        /// <inheritdoc/>  
        public string XMapCopyright
        {
            get => (string.IsNullOrEmpty(xMapCopyright) || xMapCopyright.Length < 3)
                ? "Please configure a valid copyright text!"
                : xMapCopyright;
            set
            {
                xMapCopyright = value;
                Layers.UpdateXMapCoprightText(xMapCopyright);
                if (!Gadgets.ContainsKey(GadgetType.Copyright)) return;

                (Gadgets[GadgetType.Copyright] as MapGadget)?.UpdateContent();
            }
        }

        /// <inheritdoc/>  
        public bool FitInWindow
        {
            get => mapView.FitInWindow;
            set => mapView.FitInWindow = value;
        }

        /// <inheritdoc/>  
        public bool IsAnimating => mapView.IsAnimating;

        /// <inheritdoc/>  
        public int MaxZoom
        {
            get => mapView.MaxZoom;
            set => mapView.MaxZoom = value;
        }

        /// <inheritdoc/>  
        public int MinZoom
        {
            get => mapView.MinZoom;
            set => mapView.MinZoom = value;
        }

        /// <inheritdoc/>  
        public double MetersPerPixel => mapView.MetersPerPixel;

        /// <summary><para> Sets the current theme from a XAML file provided by the stream. The XAML file must contain a
        /// ResourceDictionary on the top level. See the attached XAML files in the demo project. </para>
        /// <para> See the <conceptualLink target="5e97e57f-ad50-4dda-af0b-e117af8c4fcd"/> topic for an example. </para></summary>
        /// <param name="stream"> The stream providing a XML file with a ResourceDictionary on the top level. </param>
        public void SetThemeFromXaml(System.IO.Stream stream)
        {
            Resources.MergedDictionaries.Clear();
            Resources.MergedDictionaries.Add((ResourceDictionary)System.Windows.Markup.XamlReader.Load(stream));
        }

        /// <summary> Set the center and zooming level of the map. </summary>
        /// <param name="point"> The center point. </param>
        /// <param name="zoom"> The zoom level. </param>
        public void SetMapLocation(Point point, double zoom)
        {
            SetMapLocation(point, zoom, "EPSG:4326");
        }

        /// <summary> Set the center and zooming level of the map. </summary>
        /// <param name="point"> The center point. </param>
        /// <param name="zoom"> The zoom level. </param>
        /// <param name="spatialReferenceId"> The spatial reference identifier. </param>
        public void SetMapLocation(Point point, double zoom, string spatialReferenceId)
        {
            var mercatorPoint = GeoTransform.Transform(point, spatialReferenceId, "PTV_MERCATOR");
            mapView.SetXYZ(mercatorPoint.X, mercatorPoint.Y, zoom, UseAnimation);
        }

        /// <summary> Gets or sets the zoom level of the map. </summary>
        public double Zoom
        {
            get => mapView.FinalZoom;
            set => mapView.SetZoom(value, UseAnimation);
        }

        /// <inheritdoc/>  
        public void PrintMap(bool useScaling, string description)
        {
            // Open the print dialog.
            var print = new PrintDialog();
            if (print.ShowDialog() == false)
                return;

            // Initialize variables.
            var capabilities = print.PrintQueue.GetPrintCapabilities(print.PrintTicket);
            if (capabilities == null) return;

            Transform oldTransform = null;

            if (useScaling && capabilities.PageImageableArea != null)
            {
                // Set the transform object for scaling.
                double scale = Math.Min(capabilities.PageImageableArea.ExtentWidth / mapView.ActualWidth,
                    capabilities.PageImageableArea.ExtentHeight / mapView.ActualHeight);
                oldTransform = mapView.LayoutTransform;
                mapView.LayoutTransform = new ScaleTransform(scale, scale);
            }

            // Set the size.
            var oldSize = new Size(mapView.ActualWidth, mapView.ActualHeight);
            if (capabilities.PageImageableArea != null)
            {
                var sz = new Size(capabilities.PageImageableArea.ExtentWidth, capabilities.PageImageableArea.ExtentHeight);
                mapView.Measure(sz);
                mapView.Arrange(new Rect(new Point(capabilities.PageImageableArea.OriginWidth, capabilities.PageImageableArea.OriginHeight), sz));
            }

            // Print.
            mapView.Printing = true;
            mapView.UpdateLayout();
            print.PrintVisual(mapView, description);
            mapView.Printing = false;

            // Reset the old values.
            if (useScaling)
                mapView.LayoutTransform = oldTransform;
            mapView.Measure(oldSize);
            mapView.Arrange(new Rect(new Point(0, 0), oldSize));
        }
        #endregion

        #region IDisposable Members
        /// <summary> Disposal of the map. All layers are disposed. </summary>
        public void Dispose()
        {
            Layers.Dispose();
        }
        #endregion

        #region IMap Members
        /// <inheritdoc/>
        public Point MouseToGeo(MouseEventArgs e, string spatialReferenceId)
        {
            return RelToMapViewAsGeo(e.GetPosition(mapView), spatialReferenceId);
        }

        /// <inheritdoc/>
        public Point MouseToGeo(MouseEventArgs e)
        {
            return RelToMapViewAsGeo(e.GetPosition(mapView), "EPSG:4326");
        }

        /// <inheritdoc/>
        public Point RelToMapViewAsGeo(Point point, string spatialReferenceId)
        {
            var mercatorPoint = mapView.CanvasToPtvMercator(mapView, point);

            return mercatorPoint.GeoTransform("PTV_MERCATOR", spatialReferenceId);
        }

        /// <inheritdoc/>
        public Point RelToMapViewAsGeo(Point point)
        {
            return RelToMapViewAsGeo(point, "EPSG:4326");
        }

        /// <inheritdoc/>
        public Point GeoAsRelToMapView(Point point, string spatialReferenceId)
        {
            var mercatorPoint = point.GeoTransform(spatialReferenceId, "PTV_MERCATOR");

            return mapView.PtvMercatorToCanvas(mapView, mercatorPoint);
        }

        /// <inheritdoc/>
        public Point GeoAsRelToMapView(Point point)
        {
            return GeoAsRelToMapView(point, "EPSG:4326");
        }

        /// <inheritdoc/>
        public MapRectangle GetEnvelope()
        {
            return GetEnvelope("EPSG:4326");
        }

        /// <inheritdoc/>
        public MapRectangle GetEnvelope(string spatialReferenceId)
        {
            MapRectangle rectangle = mapView.FinalEnvelope;
            return new MapRectangle(rectangle.SouthWest.GeoTransform("PTV_MERCATOR", spatialReferenceId),
                                    rectangle.NorthEast.GeoTransform("PTV_MERCATOR", spatialReferenceId));
        }

        /// <inheritdoc/>
        public void SetEnvelope(MapRectangle rectangle)
        {
            SetEnvelope(rectangle, "EPSG:4326");
        }

        /// <inheritdoc/>
        public void SetEnvelope(MapRectangle rectangle, string spatialReferenceId)
        {
            var p1 = rectangle.SouthWest.GeoTransform(spatialReferenceId, "PTV_MERCATOR");
            var p2 = rectangle.NorthEast.GeoTransform(spatialReferenceId, "PTV_MERCATOR");

            mapView.SetEnvelope(new MapRectangle(p1, p2), UseAnimation);
        }

        /// <inheritdoc/>
        public MapRectangle GetCurrentEnvelope()
        {
            return GetCurrentEnvelope("EPSG:4326");
        }

        /// <inheritdoc/>
        public MapRectangle GetCurrentEnvelope(string spatialReferenceId)
        {
            MapRectangle rectangle = mapView.CurrentEnvelope;
            return new MapRectangle(rectangle.SouthWest.GeoTransform("PTV_MERCATOR", spatialReferenceId),
                                    rectangle.NorthEast.GeoTransform("PTV_MERCATOR", spatialReferenceId));
        }

        /// <inheritdoc/>  
        public bool UseAnimation { get; set; } = true;

        /// <inheritdoc/>  
        public bool InvertMouseWheel { get; set; } = false;

        /// <inheritdoc/>
        public double MouseWheelSpeed { get; set; } = .5;

        /// <inheritdoc/>
        public bool MouseDoubleClickZoom { get; set; } = true;

        /// <inheritdoc/>
        public DragMode MouseDragMode { get; set; } = DragMode.SelectOnShift;

        /// <inheritdoc/>
        public CoordinateDiplayFormat CoordinateDiplayFormat { get; set; } = CoordinateDiplayFormat.Degree;

        /// <inheritdoc/>  
        public double ZoomLevel
        {
            get => mapView.FinalZoom;
            set => mapView.SetZoom(value, UseAnimation);
        }

        /// <inheritdoc/>  
        public double Scale => mapView.FinalScale;

        /// <inheritdoc/>  
        public Point Center
        {
            get => GeoTransform.PtvMercatorToWGS(new Point(mapView.FinalX, mapView.FinalY));
            set
            {
                var p = GeoTransform.WGSToPtvMercator(value);
                mapView.SetXYZ(p.X, p.Y, mapView.FinalZoom, UseAnimation);
            }
        }

        /// <inheritdoc/>  
        public double CurrentZoomLevel => mapView.CurrentZoom;

        /// <inheritdoc/>  
        public double CurrentScale => mapView.CurrentScale;

        /// <inheritdoc/>  
        public Point CurrentCenter => GeoTransform.PtvMercatorToWGS(new Point(mapView.CurrentX, mapView.CurrentY));

        #endregion
    }

    /// <summary> Provides extensions for VisualTreeHelper. </summary>
    public static class VisualTreeHelperExtensions
    {
        /// <summary> Gets the parent of a dependency object </summary>
        /// <param name="obj">The dependency object to get the parent for</param>
        /// <returns>Parent of the dependency object</returns>
        public static DependencyObject GetParent(this DependencyObject obj)
        {
            if (obj == null) return null;

            return (obj is ContentElement contentElement)
                ? ContentOperations.GetParent(contentElement) ?? (contentElement as FrameworkContentElement)?.Parent
                : VisualTreeHelper.GetParent(obj);
        }

        /// <summary> Finds a specific ancestor of a dependency object. </summary>
        /// <typeparam name="T">Type to lookup</typeparam>
        /// <param name="obj">Dependency object to find the ancestor for.</param>
        /// <returns>Found ancestor or null.</returns>
        public static T FindAncestor<T>(this DependencyObject obj) where T : DependencyObject
        {
            for (; obj != null; obj = GetParent(obj))
            {
                if (obj is T objTest) return objTest;
            }

            return null;
        }
    }

}
