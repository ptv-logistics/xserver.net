// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Ptv.XServer.Controls.Map.Tools;
using Ptv.XServer.Controls.Map.Canvases;

namespace Ptv.XServer.Controls.Map.Layers.Shapes
{
    /// <summary><para> This class represents shape objects which are displayed on a map. </para>
    /// <para> See the <conceptualLink target="06a654f3-afbd-4f00-9c8e-36997e2a3951"/> topic for an example. </para></summary>
    public abstract class MapPolylineBase : MapShape
    {
        protected Geometry Data;

        #region protected variables
        /// <inheritdoc/>
        protected override Geometry DefiningGeometry => Data;

        #endregion

        #region public variables
        /// <summary> Gets or sets the points of the polyline. </summary>
        public PointCollection Points
        {
            get => (PointCollection)GetValue(PointsProperty);
            set => SetValue(PointsProperty, value);
        }

        /// <summary> Gets or sets the transformed points. The transformed points are a collection of points which have
        /// been transformed to the currently applied spatial reference system. This property helps to improve
        /// performance since the point transformation is only done once. </summary>
        public PointCollection TransformedPoints { get; set; }
        #endregion

        #region properties
        /// <summary> Backing store for Points. This enables animation, styling, binding, etc.. </summary>
        public static readonly DependencyProperty PointsProperty =
            DependencyProperty.Register("Points", typeof(PointCollection), typeof(MapPolyline),
            new UIPropertyMetadata(new PointCollection(), OnPointCollectionChanged));
        #endregion

        #region private methods
        /// <summary> Event handler for a change of the point collection. The polyline is updated in this case. </summary>
        /// <param name="obj"> Element for which the point collection has been changed. </param>
        /// <param name="args"> Event parameters. </param>
        private static void OnPointCollectionChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var shape = obj as MapShape;
            if ((shape?.GeoTransform != null) && (shape.Parent is ShapeCanvas shapeCanvas))
                shape.UpdateShape(shapeCanvas.MapView, UpdateMode.Refresh, false);
        }

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="MapPolylineBase"/> class. </summary>
        protected MapPolylineBase()
        {
            TransformedPoints = new PointCollection();
        }
        #endregion
    }

    /// <summary><para> This class represents a polyline on the map. The MapPolyline is responsible for adapting the visual
    /// to the current map viewport in terms of scaling and clipping. </para>
    /// <para> See the <conceptualLink target="06a654f3-afbd-4f00-9c8e-36997e2a3951"/> topic for an example. </para></summary>
    public class MapPolyline : MapPolylineBase
    {
        /// <summary> Initializes a new instance of the <see cref="MapPolyline"/> class. Initializes the
        /// <see cref="MapShape.ScaleFactor"/> to 0.5. </summary>
        public MapPolyline()
        {
            ScaleFactor = 0.0;
        }

        #endregion

        #region public methods

        protected void TransformShape(MapView mapView)
        {
            TransformedPoints.Clear();

            foreach (var point in Points)
                TransformedPoints.Add(GeoTransform(point));
        }

        protected virtual void ClipShape(MapView mapView, UpdateMode mode, bool lazyUpdate)
        {
            if (mode == UpdateMode.Refresh || (GlobalOptions.InfiniteZoom && mode == UpdateMode.EndTransition))  
                TransformShape(mapView);

            if (!(NeedsUpdate(lazyUpdate, mode) || (GlobalOptions.InfiniteZoom && mode == UpdateMode.EndTransition)))
                return;

            MapRectangle rect = mapView.CurrentEnvelope;
            Size sz = new Size(mapView.ActualWidth, mapView.ActualHeight);
            var minX = rect.West;
            var minY = rect.South;
            var maxX = rect.East;
            var maxY = rect.North;
            Rect clippingRect = new Rect(minX, -maxY, maxX - minX, maxY - minY);

            double thickness = CurrentThickness(mapView.CurrentScale);

            clippingRect.X -= .5 * thickness;
            clippingRect.Y -= .5 * thickness;
            clippingRect.Width += thickness;
            clippingRect.Height += thickness;

            ICollection<PointCollection> tmpPoints = LineReductionClipping.ClipPolylineReducePoints<PointCollection, Point>(
                           sz,
                           clippingRect,
                           TransformedPoints,
                           p => p,
                           (poly, pnt) => poly.Add(pnt));

            Data = BuildGeometry(tmpPoints);

            InvalidateVisual();
        }

        /// <inheritdoc/>
        public override void UpdateShape(MapView mapView, UpdateMode mode, bool lazyUpdate)
        {
            ClipShape(mapView, mode, lazyUpdate);

            if (!NeedsUpdate(lazyUpdate, mode))
                return;

            base.UpdateShape(mapView, mode, lazyUpdate);
        }

        /// <summary> Builds the geometry of the polyline. </summary>
        /// <param name="lines"> A collection of point collections to build the geometry for (multiple polylines). </param>
        /// <returns> The geometry corresponding to the given point collections. </returns>
        protected Geometry BuildGeometry(ICollection<PointCollection> lines)
        {
            var geom = new StreamGeometry();

            using (StreamGeometryContext gc = geom.Open())
            {
                foreach (PointCollection points in lines)
                {
                    if (points.Count <= 0) continue;
                    var destPoints = new PointCollection();

                    for (int i = 1; i < points.Count; ++i)
                        destPoints.Add(points[i]);

                    gc.BeginFigure(points[0], true, false);
                    gc.PolyLineTo(destPoints, true, true);
                }
            }

            return geom;
        }

        #endregion
    }

    /// <summary><para> This class represents a polygon on the map. </para>
    /// <para> See the <conceptualLink target="06a654f3-afbd-4f00-9c8e-36997e2a3951"/> topic for an example. </para></summary>
    public class MapPolygon : MapPolyline
    {
        /// <summary> Initializes a new instance of the <see cref="MapPolygon"/> class. Initializes the
        /// <see cref="MapShape.ScaleFactor"/> to 0.0. </summary>
        public MapPolygon()
        {
            ScaleFactor = 0.0;
        }

        protected override void ClipShape(MapView mapView, UpdateMode updateMode, bool lazyUpdate)
        {
            if (updateMode == UpdateMode.Refresh || (GlobalOptions.InfiniteZoom && updateMode == UpdateMode.EndTransition))  
            {
                TransformShape(mapView);
                Data = BuildGeometry(new[] { TransformedPoints });

                InvalidateVisual();
            }
        }

        /// <inheritdoc/>
        public override void UpdateShape(MapView mapView, UpdateMode mode, bool lazyUpdate)
        {
            ClipShape(mapView, mode, lazyUpdate);

            if (lazyUpdate && mode != UpdateMode.Refresh && mode != UpdateMode.EndTransition)
                return;

            base.UpdateShape(mapView, mode, lazyUpdate);
        }

        protected new Geometry BuildGeometry(ICollection<PointCollection> lines)
        {
            var geom = new StreamGeometry();
            using (StreamGeometryContext gc = geom.Open())
            {
                foreach (var points in lines)
                {
                    for (int i = 0; i < points.Count; i++)
                    {
                        var mercatorPoint = points[i];

                        if (i == 0)
                            gc.BeginFigure(new Point(mercatorPoint.X, mercatorPoint.Y), true, true);
                        else
                            gc.LineTo(new Point(mercatorPoint.X, mercatorPoint.Y), true, true);
                    }
                }
            }

            return geom;
        }
    }
}
