// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using Ptv.XServer.Controls.Map.Tools;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Windows.Point;
using Ptv.XServer.Controls.Map.Gadgets;
using Ptv.XServer.Controls.Map.Layers.Xmap2;

namespace Ptv.XServer.Controls.Map
{
    /// <summary> A wrapper for WpfMap for easy WinForms integration. </summary>
    [ToolboxItem(true)]
    [ToolboxBitmap(typeof(FormsMap), "WpfMap.Icon")]
    [Description("Ptv.XServer.Controls.Map.FormsMap")]
    [Docking(DockingBehavior.Ask)]
    public class FormsMap : UserControl, IMap
    {
        #region events
        /// <inheritdoc/>  
        public event EventHandler ViewportBeginChanged
        {
            add => wpfMap.ViewportBeginChanged += value;
            remove => wpfMap.ViewportBeginChanged -= value;
        }

        /// <inheritdoc/>  
        public event EventHandler ViewportEndChanged
        {
            add => wpfMap.ViewportEndChanged += value;
            remove => wpfMap.ViewportEndChanged -= value;
        }

        /// <inheritdoc/>  
        public event EventHandler ViewportWhileChanged
        {
            add => wpfMap.ViewportWhileChanged += value;
            remove => wpfMap.ViewportWhileChanged -= value;
        }
        #endregion //events

        #region private variables
        /// <summary> Gets or sets the WPF map which is shown in the form. </summary>
        private WpfMap wpfMap { get; set; }

        /// <summary> Gets or sets the element host which encapsulates the WPF map and offers it as WinForms object. </summary>
        private ElementHost host { get; set; }
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="FormsMap"/> class. </summary>
        public FormsMap()
        {
            wpfMap = new WpfMap();
            Load += FormsMap_Load;
        }

        /// <summary> Event handler for a successful load of the forms map. </summary>
        /// <param name="sender"> Sender of the Load event. </param>
        /// <param name="e"> Event parameters. </param>
        private void FormsMap_Load(object sender, EventArgs e)
        {            
            Load -= FormsMap_Load;

            host = new ElementHost {Dock = DockStyle.Fill, Enabled = !DesignTest.DesignMode};
            Controls.Add(host);
            host.Child = wpfMap;
        }
        #endregion

        #region disposal
        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
                wpfMap.Dispose();

            base.Dispose(disposing);
        }
        #endregion

        #region public methods
        /// <summary> Sets the current theme from a XAML file provided by the stream. The XAML file must contain a
        /// ResourceDictionary on the top level. See the attached XAML files in the demo project. </summary>
        /// <param name="stream"> The stream providing a XML file with a ResourceDictionary on the top level. </param>
        public void SetThemeFromXaml(Stream stream)
        {
            wpfMap.SetThemeFromXaml(stream);
        }

        /// <summary> Gets or sets a value indicating whether the coordinates gadget is shown or made invisible. </summary>
        public bool ShowCoordinates
        {
            get => wpfMap.ShowCoordinates;
            set => wpfMap.ShowCoordinates = value;
        }

        /// <summary> Gets or sets a value indicating whether the layers gadget is shown or made invisible. </summary>
        public bool ShowLayers
        {
            get => wpfMap.ShowLayers;
            set => wpfMap.ShowLayers = value;
        }

        /// <summary> Gets or sets a value indicating whether the magnifier gadget is shown or made invisible. </summary>
        public bool ShowMagnifier
        {
            get => wpfMap.ShowMagnifier;
            set => wpfMap.ShowMagnifier = value;
        }

        /// <summary> Gets or sets a value indicating whether the layers gadget is shown or made invisible. </summary>
        public bool ShowNavigation
        {
            get => wpfMap.ShowNavigation;
            set => wpfMap.ShowNavigation = value;
        }

        /// <summary> Gets or sets a value indicating whether the overview map is shown or made invisible. </summary>
        public bool ShowOverview
        {
            get => wpfMap.ShowOverview;
            set => wpfMap.ShowOverview = value;
        }

        /// <summary> Gets or sets a value indicating whether the scale gadget is shown or made invisible. </summary>
        public bool ShowScale
        {
            get => wpfMap.ShowScale;
            set => wpfMap.ShowScale = value;
        }

        /// <summary> Gets or sets a value indicating whether the zoom slider gadget is shown or made invisible. </summary>
        public bool ShowZoomSlider
        {
            get => wpfMap.ShowZoomSlider;
            set => wpfMap.ShowZoomSlider = value;
        }

        /// <summary> Gets or sets a value indicating whether the built-in PTV theme should be used, and thus overriding an optionally set
        /// style. The application theme can be set in the App.xaml. If no theme at all is set and the default PTV
        /// theme is not used either, the UI will look like the current Windows theme. </summary>
        public bool UseDefaultTheme
        {
            get => wpfMap.UseDefaultTheme;
            set => wpfMap.UseDefaultTheme = value;
        }

        /// <summary>
        /// Gets the wrapped Wpf map instance. This can be used to get to the inner Wpf elements, e.g. to support 
        /// mouse events on Wpf level.
        /// </summary>
        public WpfMap WrappedMap => wpfMap;

        #endregion

        #region IMap Members
        /// <inheritdoc/>
        public LayerCollection Layers => wpfMap.Layers;

        /// <inheritdoc/>
        public string XMapUrl
        {
            get => wpfMap.XMapUrl;
            set => wpfMap.XMapUrl = value;
        }

        /// <inheritdoc/>
        public string XMapCopyright
        {
            get => wpfMap.XMapCopyright;
            set => wpfMap.XMapCopyright = value;
        }

        /// <inheritdoc/>
        public string XMapCredentials
        {
            get => wpfMap.XMapCredentials;
            set => wpfMap.XMapCredentials = value;
        }

        /// <inheritdoc/>
        public string XMapStyle
        {
            get => wpfMap.XMapStyle;
            set => wpfMap.XMapStyle = value;
        }

        
        /// <inheritdoc/>  
        public bool FitInWindow
        {
            get => wpfMap.FitInWindow;
            set => wpfMap.FitInWindow = value;
        }

        /// <inheritdoc/>  
        public bool IsAnimating => wpfMap.IsAnimating;

        /// <inheritdoc/>  
        public int MaxZoom
        {
            get => wpfMap.MaxZoom;
            set => wpfMap.MaxZoom = value;
        }

        /// <inheritdoc/>  
        public int MinZoom
        {
            get => wpfMap.MinZoom;
            set => wpfMap.MinZoom = value;
        }

        /// <inheritdoc/>  
        public double MetersPerPixel => wpfMap.MetersPerPixel;

        /// <inheritdoc/>
        public void SetMapLocation(Point point, double zoom)
        {
            wpfMap.SetMapLocation(point, zoom);
        }

        /// <inheritdoc/>
        public void SetMapLocation(Point point, double zoom, string spatialReferenceId)
        {
            wpfMap.SetMapLocation(point, zoom, spatialReferenceId);
        }

        /// <inheritdoc/>
        public bool UseMiles
        {
            get => wpfMap.UseMiles;
            set => wpfMap.UseMiles = value;
        }

        /// <inheritdoc/>
        public MapRectangle GetEnvelope()
        {
            return wpfMap.GetEnvelope();
        }

        /// <inheritdoc/>
        public MapRectangle GetEnvelope(string spatialReferenceId)
        {
            return wpfMap.GetEnvelope(spatialReferenceId);
        }

        /// <inheritdoc/>  
        public void SetEnvelope(MapRectangle rectangle)
        {
            wpfMap.SetEnvelope(rectangle);
        }

        /// <inheritdoc/>  
        public void SetEnvelope(MapRectangle rectangle, string spatialReferenceId)
        {
            wpfMap.SetEnvelope(rectangle, spatialReferenceId);
        }

        /// <inheritdoc/>
        public MapRectangle GetCurrentEnvelope()
        {
            return wpfMap.GetCurrentEnvelope();
        }

        /// <inheritdoc/>
        public MapRectangle GetCurrentEnvelope(string spatialReferenceId)
        {
            return wpfMap.GetCurrentEnvelope(spatialReferenceId);
        }

        /// <inheritdoc/>  
        public Point MouseToGeo(MouseEventArgs e, string spatialReferenceId)
        {
            return wpfMap.MouseToGeo(e, spatialReferenceId);
        }

        /// <inheritdoc/>  
        public Point MouseToGeo(MouseEventArgs e)
        {
            return wpfMap.MouseToGeo(e);
        }

        /// <inheritdoc/>  
        public Point RelToMapViewAsGeo(Point point, string spatialReferenceId)
        {
            return wpfMap.RelToMapViewAsGeo(point, spatialReferenceId);
        }

        /// <inheritdoc/>  
        public Point RelToMapViewAsGeo(Point point)
        {
            return wpfMap.RelToMapViewAsGeo(point);
        }

        /// <inheritdoc/>  
        public Point GeoAsRelToMapView(Point point, string spatialReferenceId)
        {
            return wpfMap.GeoAsRelToMapView(point, spatialReferenceId);
        }

        /// <inheritdoc/>  
        public Point GeoAsRelToMapView(Point point)
        {
            return wpfMap.GeoAsRelToMapView(point);
        }

        /// <inheritdoc/>  
        public bool UseAnimation
        {
            get => wpfMap.UseAnimation;
            set => wpfMap.UseAnimation = value;
        }

        /// <inheritdoc/>  
        public bool InvertMouseWheel
        {
            get => wpfMap.InvertMouseWheel;
            set => wpfMap.InvertMouseWheel = value;
        }

        /// <inheritdoc/>  
        public double MouseWheelSpeed
        {
            get => wpfMap.MouseWheelSpeed;
            set => wpfMap.MouseWheelSpeed = value;
        }

        /// <inheritdoc/>
        public bool MouseDoubleClickZoom 
        {
            get => wpfMap.MouseDoubleClickZoom;
            set => wpfMap.MouseDoubleClickZoom = value;
        }

        /// <inheritdoc/>
        public DragMode MouseDragMode
        {
            get => wpfMap.MouseDragMode;
            set => wpfMap.MouseDragMode = value;
        }

        /// <inheritdoc/>
        public CoordinateDiplayFormat CoordinateDiplayFormat 
        {
            get => wpfMap.CoordinateDiplayFormat;
            set => wpfMap.CoordinateDiplayFormat = value;
        }

        /// <inheritdoc/>  
        public double ZoomLevel
        {
            get => wpfMap.ZoomLevel;
            set => wpfMap.ZoomLevel = value;
        }

        /// <inheritdoc/>  
        public new double Scale => wpfMap.Scale;

        /// <inheritdoc/>  
        public Point Center
        {
            get => wpfMap.Center;
            set => wpfMap.Center = value;
        }

        /// <inheritdoc/>  
        public double CurrentZoomLevel => wpfMap.CurrentZoomLevel;

        /// <inheritdoc/>  
        public double CurrentScale => wpfMap.CurrentScale;

        /// <inheritdoc/>  
        public Point CurrentCenter => wpfMap.CurrentCenter;

        /// <inheritdoc/>  
        public void PrintMap(bool useScaling, string description)
        {
            wpfMap.PrintMap(useScaling, description);
        }

        /// <inheritdoc/>  
        public ToolTipManagement ToolTipManagement => wpfMap.ToolTipManagement;

        /// <inheritdoc/>  
        public LayerFactory Xmap2LayerFactory => wpfMap.Xmap2LayerFactory;
        #endregion

        #region fix for http://support.microsoft.com/kb/955753

        /// <summary> The constant value for the child window handle. </summary>
        private const uint GW_CHILD = 5;

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

        [DllImport("user32.dll")]
        private static extern bool EnableWindow(IntPtr hWnd, bool bEnable);

        /// <summary> Synchronizes the enabled state for all child windows. </summary>
        private void SynchChildEnableState()
        {
            IntPtr childHandle = GetWindow(Handle, GW_CHILD);
            if (childHandle != IntPtr.Zero)
            {
                EnableWindow(childHandle, Enabled);
            }
        }

        /// <summary>
        /// Contains a bug fix resolving an issue, when this control is not enabled correctly in ElementHost.
        /// </summary>
        /// <remarks>
        /// Further details can be seen in http://support.microsoft.com/kb/955753 .
        /// </remarks>
        /// <param name="e"> The event args of the changed event. </param>
        protected override void OnEnabledChanged(EventArgs e)
        {
            try { SynchChildEnableState(); }
            catch { }

            try { base.OnEnabledChanged(e); }
            catch { }
        }
        #endregion
    }
}
