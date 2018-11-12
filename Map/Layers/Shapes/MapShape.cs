// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Windows;
using System.Windows.Shapes;
using Ptv.XServer.Controls.Map.Canvases;

namespace Ptv.XServer.Controls.Map.Layers.Shapes
{
    /// <summary> Extends class <see cref="Shape"/> to add functionality for geographical objects which change their presentation
    /// according the currently used scale of the map. </summary>
    public abstract class MapShape : Shape
    {
        /// <summary> Gets or sets a method which transforms a point from one coordinate system to another. </summary>
        public Func<Point, Point> GeoTransform { get; set; }

        /// <summary> Gets or sets the scale factor. See <see cref="ShapeCanvas.ScaleFactorProperty"/>. </summary>
        public double ScaleFactor
        {
            get => (double)GetValue(ShapeCanvas.ScaleFactorProperty);
            set => SetValue(ShapeCanvas.ScaleFactorProperty, value);
        }

        /// <summary> Stroke thickness of the map shape. </summary>
        public static DependencyProperty MapStrokeThicknessProperty =
            DependencyProperty.Register("MapStrokeThickness", typeof(double), typeof(MapShape),
            new FrameworkPropertyMetadata(1.0, OnMapStrokeThicknessChanged));

        /// <summary> Retrieves the thickness for lines depending on the current scale. </summary>
        /// <param name="scale"> Current scale of the map shape. </param>
        /// <returns> Calculates stroke thickness. </returns>
        public double CurrentThickness(double scale)
        {
            // thickness, in  units
            return MapStrokeThickness * Math.Pow(scale, 1.0 - ScaleFactor);
        }

        /// <summary> Gets or sets the map stroke thickness. </summary>
        public double MapStrokeThickness
        {
            get => (double)GetValue(MapStrokeThicknessProperty);
            set => SetValue(MapStrokeThicknessProperty, value);
        }

        /// <summary> Event handler for a change of the stroke thickness. </summary>
        /// <param name="obj"> Element for which the stroke thickness has been changed. </param>
        /// <param name="args"> Event parameters. </param>
        private static void OnMapStrokeThicknessChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var element = obj as FrameworkElement;
            (element?.Parent as ShapeCanvas)?.UpdateScale(element, ((ShapeCanvas) element.Parent).MapView, UpdateMode.Refresh);
        }

        /// <summary> Indicates if the update process should be started or not. </summary>
        /// <param name="lazyUpdate"> Flag indicating if lazy updating is activated. </param>
        /// <param name="updateMode"> The update mode tells which kind of change is to be processed by the corresponding update call. </param>
        /// <returns></returns>
        protected bool NeedsUpdate(bool lazyUpdate, UpdateMode updateMode)
        {
            switch (updateMode)
            {
                case UpdateMode.Refresh: return true;
                case UpdateMode.WhileTransition: return !lazyUpdate;
                case UpdateMode.EndTransition: return lazyUpdate;
                default: return false;
            }
        }

        /// <summary> Update the properties of the shape, according the current scale of the MapView object. </summary>
        /// <param name="mapView"> MapView object, to which this object belongs to. </param>
        /// <param name="mode"> The update mode tells which kind of change is to be processed by the corresponding update call. </param>
        /// <param name="lazyUpdate"> Flag indicating if lazy updating is activated. </param>
        public virtual void UpdateShape(MapView mapView, UpdateMode mode, bool lazyUpdate)
        {
            if (!NeedsUpdate(lazyUpdate, mode))
                return;

            StrokeThickness = Math.Max(0.51, CurrentThickness(mapView.CurrentScale));
        }
    }
}