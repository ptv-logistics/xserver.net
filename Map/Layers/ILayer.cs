// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System.Windows;
using System.Windows.Media;
using System.ComponentModel;
using System.Collections.Generic;
using Ptv.XServer.Controls.Map.Canvases;


namespace Ptv.XServer.Controls.Map.Layers
{
    /// <summary> Interface for layer management and rendering relevant methods. </summary>
    public interface ILayer : INotifyPropertyChanged
    {
        /// <summary> Gets the name of the layer. </summary>
        string Name { get; }

        /// <summary> Gets or sets the priority (base z index) of all layer canvases. </summary>
        int Priority { get; set; }

        /// <summary> Gets or sets the caption of the layer. </summary>
        string Caption { get; set; }

        /// <summary> Gets or sets the icon of the layer. </summary>
        ImageSource Icon { get; set; }

        /// <summary> Gets or sets the opacity of the layer. </summary>
        double Opacity { get; set; }

        /// <summary> Adds the different layer canvases to the map. </summary>
        /// <param name="mapView"> The map to which the canvases are to be added. </param>
        void AddToMapView(MapView mapView);

        /// <summary> Removes the different layer canvases from the map. </summary>
        /// <param name="mapView"> The map from which the canvases are to be removed. </param>
        void RemoveFromMapView(MapView mapView);

        /// <summary> Gets the copyright string of the layer. </summary>
        string Copyright { get; }

        /// <summary> Gets a value indicating whether the layer has a settings dialog. </summary>
        bool HasSettingsDialog { get; }

        /// <summary> Gets the available canvas categories. </summary>
        CanvasCategory[] CanvasCategories { get; }
    }

    /// <summary> Retrieves information around a certain geographical point.</summary>
    public interface IToolTips
    {
        /// <summary> Retrieves information around a certain geographical point.</summary>
        /// <param name="center">Pixel coordinate of the point.</param>
        /// <param name="maxPixelDistance">Maximal number of pixels to search for.</param>
        /// <returns>A collection of strings describing the geographical objects.</returns>
        IEnumerable<string> Get(Point center, double maxPixelDistance);
    }
}
