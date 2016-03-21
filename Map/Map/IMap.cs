using Ptv.XServer.Controls.Map.Gadgets;
using System;
using System.Windows;
using System.Windows.Input;

namespace Ptv.XServer.Controls.Map
{
    /// <summary> This the root interface of the map control. It is implemented both for WpfMap and FormsMap. The
    /// interface has methods for setting and getting the current map viewport, configuring the gadgets and getting
    /// access to the layers collection. </summary>
    public interface IMap
    {
        /// <summary> Event indicating the beginning of a change of the visible map section. This event is intended for
        /// more longtime actions (for example reading DB-objects), when the map section will change. </summary>
        event EventHandler ViewportBeginChanged;

        /// <summary> Event indicating the ending of a change of the visible map section. It is the counterpart of the
        /// <seealso cref="ViewportBeginChanged"/> event. </summary>
        event EventHandler ViewportEndChanged;

        /// <summary> Event indicating an intermediate view, when animation mode is active. It can be used to adapt the
        /// size of WPF objects, or other actions, which are not time-consuming. This event may called multiple times,
        /// when animation mode is active. </summary>
        event EventHandler ViewportWhileChanged;
        
        /// <summary> Gets the collection of map layers. </summary>
        LayerCollection Layers { get; }

        /// <summary> Gets or sets the url pointing to where the xMapServer is located (e.g. http://127.0.0.1:50010/xmap/ws/XMap). </summary>
        string XMapUrl { get; set; }
        /// <summary> Gets or sets the coyright text. </summary>
        string XMapCopyright { get; set; }
        /// <summary> Gets or sets the credentials for xMapServer basic HTTP authentication.
        /// The format of the credential string is "&lt;user&gt;:&lt;password&gt;". </summary>
        string XMapCredentials { get; set; }
        /// <summary> Gets or sets the style profile of the xMapServer base map. </summary>
        string XMapStyle { get; set; }

        /// <summary> Gets or sets a value indicating whether the map should be fitted in the window or not. </summary>
        bool FitInWindow { get; set; }

        /// <summary> Gets a value indicating whether an animation is in progress. Returns true while the map performs
        /// a transition to a new map section. </summary>
        bool IsAnimating { get; }
        
        /// <summary> Gets or sets the maximal level of detail according to the standard tiling scheme. The current
        /// detail level (see <see cref="ZoomLevel"/> property) is corrected, if it is higher than the new maximum value. </summary>
        int MaxZoom { get; set; }

        /// <summary> Gets or sets the minimal level of detail according to the standard tiling scheme. The current
        /// detail level (see <see cref="ZoomLevel"/> property) is corrected, if it is lower than the new minimum value. </summary>
        int MinZoom { get; set; }

        /// <summary> Gets the number of meters spanned by one pixel. </summary>
        double MetersPerPixel { get; }

        /// <summary> Sets the center (specified in WGS84 units) and zoom level of the map. </summary>
        /// <param name="center"> The center in WGS84 units (X=Longitude, Y=Latitude). </param>
        /// <param name="zoomLevel"> Zoom level of the map. </param>
        void SetMapLocation(Point center, double zoomLevel);

        /// <summary> Sets the center (coordinate format specified in parameter <paramref name="spatialReferenceId"/>) and zoom level of the map. </summary>
        /// <param name="center"> The center point. </param>
        /// <param name="zoomLevel"> The zoom level. </param>
        /// <param name="spatialReferenceId"> The spatial reference identifier. </param>
        void SetMapLocation(Point center, double zoomLevel, string spatialReferenceId);

        /// <summary> Gets the anticipated bounding box of the visible map section after the map was in animation mode / the
        /// current box while the map is in animation mode. </summary>
        /// <returns> The rectangle in WGS84 units. </returns>
        MapRectangle GetEnvelope();

        /// <summary> Gets the anticipated bounding box of the visible map section after the map was in animation mode / the
        /// current box while the map is in animation mode. The bounding box coordinates are specified in a coordinate format
        /// according the parameter value of <paramref name="spatialReferenceId"/>. </summary>
        /// <param name="spatialReferenceId"> The spatial reference identifier. </param>
        /// <returns> The rectangle of the bounding box, specified in a coordinate format
        /// according the parameter value of <paramref name="spatialReferenceId"/>. </returns>
        MapRectangle GetEnvelope(string spatialReferenceId);

        /// <summary> Centers the map resulting in all elements within the specified rectangle are visible. </summary>
        /// <param name="rectangle"> The rectangle in WGS84 units. </param>
        void SetEnvelope(MapRectangle rectangle);

        /// <summary> Centers the map, so all elements within the specified rectangle are visible. </summary>
        /// <param name="rectangle"> The rectangle containing coordinates specified in a coordinate format
        /// according the parameter value of <paramref name="spatialReferenceId"/>. </param>
        /// <param name="spatialReferenceId"> The spatial reference identifier. </param>
        void SetEnvelope(MapRectangle rectangle, string spatialReferenceId);

        /// <summary> Gets the bounding box of the visible map section while the map is in animation mode. </summary>
        /// <returns> The rectangle in WGS84 units. </returns>
        MapRectangle GetCurrentEnvelope();

        /// <summary> Gets the bounding box of the visible map section while the map is in animation mode. </summary>
        /// <param name="spatialReferenceId"> The spatial reference identifier. </param>
        /// <returns> The rectangle of the bounding box, specified in a coordinate format
        /// according the parameter value of <paramref name="spatialReferenceId"/>. </returns>
        MapRectangle GetCurrentEnvelope(string spatialReferenceId);

        /// <summary> Converts the click point of the mouse event to a geographic point corresponding to the given 
        /// spatial reference id. The click point of the mouse is determined relative to the map. </summary>
        /// <param name="e"> The mouse event args to convert. </param>
        /// <param name="spatialReferenceId"> The spatial reference identifier. </param>
        /// <returns> The point in geographical units. </returns>
        Point MouseToGeo(MouseEventArgs e, string spatialReferenceId);

        /// <summary> Converts the click point of the mouse event to a geographic point in WGS84 units. 
        /// The click point of the mouse is determined relative to the map. </summary>
        /// <param name="e"> The mouse event args to convert. </param>
        /// <returns> The point in WGS84 units. </returns>
        Point MouseToGeo(MouseEventArgs e);

        /// <summary> Converts a point from WPF coordinates to a geographic point. </summary>
        /// <param name="point"> The point in WPF units. </param>
        /// <param name="spatialReferenceId"> The spatial reference identifier. </param>
        /// <returns> The point in geographical units. </returns>
        Point RelToMapViewAsGeo(Point point, string spatialReferenceId);

        /// <summary> Converts a point from WPF coordinates to a geographic point. </summary>
        /// <param name="point"> The point in WPF units. </param>
        /// <returns> The point in WGS84 units. </returns>
        Point RelToMapViewAsGeo(Point point);

        /// <summary> Converts a geographic point to a point in WPF units. </summary>
        /// <param name="point"> The geographic point. </param>
        /// <param name="spatialReferenceId"> The spatial reference. </param>
        /// <returns> The point in WPF units. </returns>
        Point GeoAsRelToMapView(Point point, string spatialReferenceId);

        /// <summary> Converts a geographic point to a point in WPF units. </summary>
        /// <param name="point"> The geographic point. </param>
        /// <returns> The point in WPF units. </returns>
        Point GeoAsRelToMapView(Point point);

        /// <summary> Gets or sets a value indicating whether the scale unit is miles. The default value is false. </summary>
        bool UseMiles { get; set; }

        /// <summary> Gets or sets a value indicating whether the map uses transitions for panning, zooming and fade-in
        /// of tiles. The default value is true. </summary>
        bool UseAnimation { get; set; }

        /// <summary> Gets or sets a value indicating whether the orientation for mouse-wheel zoom is inverted. If so,
        /// zoom in and zoom out mouse wheel direction is exchanged. The default value is false. </summary>
        bool InvertMouseWheel { get; set; }

        /// <summary> Gets or sets the speed for mouse-wheel zoom. The value defines the number of map zoom levels per
        /// wheel delta. The default value is 0.5. </summary>
        double MouseWheelSpeed { get; set; }

        /// <summary> Gets or sets the activation for zoom on double-click. If true, the user can zoom-in be double left-click
        /// and zoom-out by double right-click. </summary>
        bool MouseDoubleClickZoom { get; set; }

        /// <summary> Gets or sets the behavior when dragging the mouse while holding the left button. </summary>
        DragMode MouseDragMode { get; set; }

        /// <summary> Gets or sets the display format for coordinates. </summary>
        CoordinateDiplayFormat CoordinateDiplayFormat { get; set; }

        /// <summary> Gets or sets the zoom level of the map. </summary>
        double ZoomLevel { get; set; }

        /// <summary> Gets the scale of the map in meters per pixel. </summary>
        double Scale { get; }

        /// <summary> Gets or sets the center of the map in WGS84 coordinates. </summary>
        Point Center { get; set; }

        /// <summary> Gets the current zoom level of the map while it is in animation mode. </summary>
        double CurrentZoomLevel { get; }

        /// <summary> Gets the current scale of the map in meters per pixel while it is in animation mode. </summary>
        double CurrentScale { get; }

        /// <summary> Gets the center of the map in WGS84 coordinates while it is in animation mode. </summary>
        Point CurrentCenter { get; }

        /// <summary> Prints the currently displayed map. </summary>
        /// <param name="useScaling"> Flag indicating whether the map should be scaled to the paper size. </param>
        /// <param name="description"> Description text for the print document. </param>
        void PrintMap(bool useScaling, string description);
    }

    /// <summary>
    /// Separate interface of the map control, dedicated to the management of tool tips.
    /// </summary>
    public interface IToolTipManagement
    {
        /// <summary> Enables/disables the management for tool tips. </summary>
        bool IsEnabled { get; set; }

        /// <summary> Reads or writes the value (in [ms]) to delay tool tip display. </summary>
        int ToolTipDelay { get; set; }

        /// <summary>
        /// Distance (specified in Pixels) between the current mouse position and a layer object, for which tooltip information should be shown.
        /// Each layer has to interpolate to its own coordinate format, what is meant by the pixel sized distance.  
        /// </summary>
        double MaxPixelDistance { get; set; }
    }

    /// <summary>
    /// The coordinate display format type.
    /// </summary>
    public enum CoordinateDiplayFormat
    {
        /// <summary>
        /// Display Latitude/Longitude by degree, minutes, seconds.
        /// </summary>
        Degree,
        /// <summary>
        /// Display Latitude/Longitude with decimal values.
        /// </summary>
        Decimal
    }
}
