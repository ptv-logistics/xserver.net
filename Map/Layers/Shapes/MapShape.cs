using System;
using System.Windows;
using System.Windows.Shapes;
using Ptv.XServer.Controls.Map.Canvases;

namespace Ptv.XServer.Controls.Map.Layers.Shapes
{    
    public abstract class MapShape : Shape
    {
        /// <summary> Gets or sets a method which transforms a point from one coordinate system to another. </summary>
        public Func<Point, Point> GeoTransform { get; set; }
        /// <summary> Gets or sets the  scale factor. See <see cref="ShapeCanvas.ScaleFactorProperty"/>. </summary>
        public double ScaleFactor
        {
            get { return (double)GetValue(ShapeCanvas.ScaleFactorProperty); }
            set { SetValue(ShapeCanvas.ScaleFactorProperty, value); }
        }

        #region properties
        /// <summary> Stroke thickness of the map shape. </summary>
        public static DependencyProperty MapStrokeThicknessProperty =
            DependencyProperty.Register("MapStrokeThickness", typeof(double), typeof(MapShape),
            new FrameworkPropertyMetadata(1.0, OnMapStrokeThicknessChanged));
        #endregion

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
            get { return (double)GetValue(MapStrokeThicknessProperty); }
            set { SetValue(MapStrokeThicknessProperty, value); }
        }

        #region private methods
        /// <summary> Event handler for a change of the stroke thickness. </summary>
        /// <param name="obj"> Element for which the stroke thickness has been changed. </param>
        /// <param name="args"> Event parameters. </param>
        private static void OnMapStrokeThicknessChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var element = obj as FrameworkElement;

            if (element.Parent is ShapeCanvas)
            {
                (element.Parent as ShapeCanvas).UpdateScale(element, (element.Parent as ShapeCanvas).MapView, UpdateMode.Refresh);
            }
        }

        protected bool NeedsUpdate(bool lazyUpdate, UpdateMode updateMode)
        {
            return
                (lazyUpdate && updateMode == UpdateMode.EndTransition)
                || (!lazyUpdate && updateMode == UpdateMode.WhileTransition)
                || updateMode == UpdateMode.Refresh;
        }

        #endregion

        #region public method
        /// <summary> Update the properties of the shape, according the current scale of the MapView object. </summary>
        /// <param name="mapView"> MapView object, to which this object belongs to. </param>
        /// <param name="mode"> Update mode currently set. </param>
        /// <param name="lazyUpdate"> Flag indicating if lazy updating is activated. </param>
        public virtual void UpdateShape(MapView mapView, UpdateMode mode, bool lazyUpdate)
        {
            if (!NeedsUpdate(lazyUpdate, mode))
                return;

            StrokeThickness = CurrentThickness(mapView.CurrentScale);
        }

        #endregion
    }
}