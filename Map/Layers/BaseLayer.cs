// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Media;
using Ptv.XServer.Controls.Map.Canvases;
using System.ComponentModel;


namespace Ptv.XServer.Controls.Map.Layers
{
    /// <summary>
    /// <para>
    /// A map is built-up with different <see cref="Ptv.XServer.Controls.Map.Layers"/>. There are several types of
    /// layers. The main types are <see cref="Ptv.XServer.Controls.Map.Layers.Tiled.TiledLayer"/>, which renders map content split-up in bitmap tiles,
    /// <see cref="Ptv.XServer.Controls.Map.Layers.Untiled.UntiledLayer"/>, which renders a new image whenever the map viewport changes and
    /// <see cref="Ptv.XServer.Controls.Map.Layers.Shapes.ShapeLayer"/>, which can be used to display dynamic vector data. The map control has a
    /// <see cref="LayerCollection"/> to add, remove and manipulate the order and visibility of layers.
    /// </para>
    /// </summary>
    [CompilerGenerated]
    class NamespaceDoc
    {
        // NamespaceDoc is used for providing the root documentation for Ptv.Components.Projections
        // hint taken from http://stackoverflow.com/questions/156582/namespace-documentation-on-a-net-project-sandcastle
    }
    
    /// <summary> A layer which adds one or more canvases to the map. </summary>
    public class BaseLayer : ILayer
    {
        #region private variables
        /// <summary> Opacity of the layer. </summary>
        private double opacity = 1.0;
        /// <summary> The priority (base zIndex) of the layer which defines if the layer is painted in front of or behind other layers. </summary>
        private int priority;
        /// <summary> Caption of the layer. </summary>
        private string caption;
        /// <summary> List of canvases which have been added to a map. </summary>
        private readonly List<MapCanvas> canvasInstances = new List<MapCanvas>();

        private string copyright;
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="BaseLayer"/> class. By default, opacity is set to 1. </summary>
        /// <param name="name"> Name of the base layer. </param>
        public BaseLayer(string name)
        {
            Name = name;
        }

        /// <summary> Initializes the base layer factory. A default category for the created canvases is set as well as a
        /// default factory method which creates the new canvas instances. </summary>
        /// <param name="category"> Default category of all base layers. </param>
        /// <param name="factory"> Default factory delegate. </param>
        public void InitializeFactory(CanvasCategory category, CanvasFactoryDelegate factory)
        {
            CanvasCategories = new[] { category };
            CanvasFactories = new[] { factory };
        }
        #endregion

        #region public methods
        /// <summary> Gets or sets the copyright text of the layer. </summary>
        public string Copyright
        {
            get => copyright;
            set
            {
                if (value == copyright) return;

                copyright = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Copyright"));
            }
        }

        /// <summary> Canvas factory delegate. Takes a map as parameter, creates a canvas for this map and returns the
        /// newly created canvas. </summary>
        /// <param name="mapView"> Map for which a canvas should be created. </param>
        /// <returns> Newly created canvas. </returns>
        public delegate MapCanvas CanvasFactoryDelegate(MapView mapView);

        /// <summary> Gets or sets the array of factory delegates. There may be existing different factory methods for
        /// canvases of different categories. </summary>
        public CanvasFactoryDelegate[] CanvasFactories { get; set; }

        /// <summary> Updates the layer instances. </summary>
        public void Refresh()
        {
            canvasInstances.ForEach(canvas => canvas.Update(UpdateMode.Refresh));
        }
        #endregion

        #region ILayer Members
        /// <summary> Gets or sets the name of the layer. </summary>
        public string Name { get; set; }

        /// <summary> Gets or sets the canvas categories for the layer. </summary>
        public CanvasCategory[] CanvasCategories { get; set; }

        private static int UniqueCanvasID;

        /// <summary> Adds the layer to a map. </summary>
        /// <param name="mapView"> The map to which the layer is to be added. </param>
        public virtual void AddToMapView(MapView mapView)
        {
            // Check if the maximum number of canvases is reached.
            if (canvasInstances.Count + 1 > 1000)
                return;

            for (int i = 0; i < CanvasCategories.Length; i++)
            {
                var mapCanvas = CanvasFactories[i](mapView);
                if (mapCanvas == null)
                    continue;
                mapCanvas.CanvasCategory = CanvasCategories[i];
                mapCanvas.Opacity = opacity;
                // Her: In previous versions the name of the baseLayer is used for the MapCanvas' name. Because this name 
                // may contain any characters, but the name of mapCanvas has to be conform to a C#-identifier (MapCanvas is a UIElement!),
                // a generic naming of MapCanvases is used.
                mapCanvas.Name = "Canvas" + ++UniqueCanvasID;
                mapCanvas.Update(UpdateMode.Refresh);
                canvasInstances.Add(mapCanvas);
                UpdateZindex();
            }
        }

        /// <summary> Removes the layer from a map. </summary>
        /// <param name="mapView"> The map from which the layer is to be removed. </param>
        public virtual void RemoveFromMapView(MapView mapView)
        {
            // ReSharper disable once PossibleUnintendedReferenceComparison
            foreach (var canvas in (from canvas in canvasInstances where canvas.MapView == mapView select canvas).ToList())
            {
                canvas.Dispose();
                canvasInstances.Remove(canvas);
            }
        }

        /// <summary> Gets a value indicating whether the layer has a settings dialog describing its properties. </summary>
        public virtual bool HasSettingsDialog => false;

        /// <summary> Gets or sets the zIndex of the layer. </summary>
        public int Priority
        {
            get => priority;
            set
            {
                if (priority == value)
                    return;

                priority = value;
                UpdateZindex();

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Priority"));
            }
        }

        /// <summary> Gets or sets the caption of the layer. </summary>
        public string Caption
        {
            get => caption ?? Name;
            set => caption = value;
        }

        /// <summary> Gets or sets the icon of the layer. </summary>
        public ImageSource Icon { get; set; }

        /// <summary> Gets or sets the opacity of the layer. </summary>
        public double Opacity
        {
            get => opacity;
            set
            {
                if (opacity == value)
                    return;

                opacity = value;
                canvasInstances.ForEach(canvas => canvas.Opacity = opacity);

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Opacity"));
            }
        }
        #endregion

        #region helper methods
        /// <summary> Updates the zIndex of the layer. </summary>
        private void UpdateZindex()
        {
            for (int canvasPosition = 0; canvasPosition < canvasInstances.Count; canvasPosition++)
                Panel.SetZIndex(canvasInstances[canvasPosition], (int)canvasInstances[canvasPosition].CanvasCategory * 1000000 + priority * 1000 + canvasPosition);
        }
        #endregion

        #region INotifyPropertyChanged Members

        /// <inheritdoc/>  
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}