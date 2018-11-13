// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System.Collections.Generic;
using System.Linq;
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
        /// <summary>Contains the geometry provided by property <see cref="DefiningGeometry"/>. </summary>
        protected Geometry Data;

        /// <summary> Gets a value that represents the geometry of the shape. </summary>
        protected override Geometry DefiningGeometry => Data;

        /// <summary> Gets or sets the points of the polyline. </summary>
        public PointCollection Points
        {
            get => (PointCollection)GetValue(PointsProperty);
            set => SetValue(PointsProperty, value);
        }

        /// <summary> Gets or sets the transformed points. The transformed points are a collection of points which have
        /// been transformed to the currently applied spatial reference system. This property helps to improve
        /// performance since the point transformation is only done once. </summary>
        public PointCollection TransformedPoints { get; set; } = new PointCollection();

        /// <summary> Backing store for Points. This enables animation, styling, binding, etc.. </summary>
        public static readonly DependencyProperty PointsProperty =
            DependencyProperty.Register("Points", typeof(PointCollection), typeof(MapPolyline),
            new UIPropertyMetadata(new PointCollection(), OnPointCollectionChanged));

        /// <summary> Event handler for a change of the point collection. The polyline is updated in this case. </summary>
        /// <param name="obj"> Element for which the point collection has been changed. </param>
        /// <param name="args"> Event parameters. </param>
        private static void OnPointCollectionChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var shape = obj as MapShape;
            if (shape?.GeoTransform != null && shape.Parent is ShapeCanvas shapeCanvas)
                shape.UpdateShape(shapeCanvas.MapView, UpdateMode.Refresh, false);
        }
    }

    /// <summary><para> This class represents a polyline on the map. The MapPolyline is responsible for adapting the visual
    /// to the current map viewport in terms of scaling and clipping. </para>
    /// <para> See the <conceptualLink target="06a654f3-afbd-4f00-9c8e-36997e2a3951"/> topic for an example. </para></summary>
    public class MapPolyline : MapPolylineBase
    {
        /// <summary> Initializes a new instance of the <see cref="MapPolyline"/> class. </summary>
        public MapPolyline()
        {
            ScaleFactor = 0.0;
        }

        /// <summary> Transforms the polyline object according the transformation provided by
        /// <see cref="MapShape.GeoTransform"/>. </summary>
        /// <param name="mapView">Not used.</param>
        protected void TransformShape(MapView mapView)
        {
            if (GeoTransform == null)
                return;

            TransformedPoints.Clear();
            foreach (var point in Points)
                TransformedPoints.Add(GeoTransform(point));
        }

        /// <summary>Clip the Polyline object. </summary>
        /// <param name="mapView">Mapview object in which the clipping takes place.</param>
        /// <param name="updateMode"> The update mode tells which kind of change is to be processed by the update call. </param>
        /// <param name="lazyUpdate"> Flag indicating if lazy updating is activated. </param>
        protected virtual void ClipShape(MapView mapView, UpdateMode updateMode, bool lazyUpdate)
        {
            if (updateMode == UpdateMode.Refresh || updateMode == UpdateMode.EndTransition)
                TransformShape(mapView);

            if (updateMode != UpdateMode.EndTransition && !NeedsUpdate(lazyUpdate, updateMode))
                return;

            MapRectangle rect = mapView.CurrentEnvelope;
            Size mapViewSizeInPixels = new Size(mapView.ActualWidth, mapView.ActualHeight);
            var clippingRect = new Rect(rect.West, -rect.North, rect.East - rect.West, rect.North - rect.South);

            double thickness = CurrentThickness(mapView.CurrentScale);

            clippingRect.X -= .5 * thickness;
            clippingRect.Y -= .5 * thickness;
            clippingRect.Width += thickness;
            clippingRect.Height += thickness;

            ICollection<PointCollection> clippedLines = LineReductionClipping.ClipPolylineReducePoints<PointCollection, Point>(
                           mapViewSizeInPixels,
                           clippingRect,
                           TransformedPoints,
                           p => p,
                           (poly, pnt) => poly.Add(pnt));

            Data = BuildGeometry(clippedLines);

            InvalidateVisual();
        }

        /// <inheritdoc/>
        public override void UpdateShape(MapView mapView, UpdateMode updateMode, bool lazyUpdate)
        {
            ClipShape(mapView, updateMode, lazyUpdate);

            if (!NeedsUpdate(lazyUpdate, updateMode))
                return;

            base.UpdateShape(mapView, updateMode, lazyUpdate);
        }

        /// <summary> Builds the geometry of the polyline. </summary>
        /// <param name="lines"> A collection of point collections to build the geometry for (multiple polylines). </param>
        /// <returns> The geometry corresponding to the given point collections. </returns>
        protected Geometry BuildGeometry(ICollection<PointCollection> lines)
        {
            var geom = new StreamGeometry();

            using (StreamGeometryContext gc = geom.Open())
                foreach (var points in lines.Where(points => points.Count > 0))
                {
                    var destPoints = new PointCollection();
                    foreach (var point in points.Skip(1))
                        destPoints.Add(point);

                    gc.BeginFigure(points[0], true, false);
                    gc.PolyLineTo(destPoints, true, true);
                }

            return geom;
        }
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

        /// <summary>Clip the Polygon object. </summary>
        /// <param name="mapView"> Mapview object in which the clipping takes place.</param>
        /// <param name="updateMode"> The update mode tells which kind of change is to be processed by the update call. </param>
        /// <param name="lazyUpdate"> Flag indicating if lazy updating is activated. </param>
        protected override void ClipShape(MapView mapView, UpdateMode updateMode, bool lazyUpdate)
        {
            if (updateMode != UpdateMode.Refresh && updateMode != UpdateMode.EndTransition) return;

            TransformShape(mapView);
            Data = BuildGeometry(new[] { TransformedPoints });

            InvalidateVisual();
        }

        /// <inheritdoc/>
        public override void UpdateShape(MapView mapView, UpdateMode updateMode, bool lazyUpdate)
        {
            ClipShape(mapView, updateMode, lazyUpdate);

            if (lazyUpdate && updateMode != UpdateMode.Refresh && updateMode != UpdateMode.EndTransition)
                return;

            base.UpdateShape(mapView, updateMode, lazyUpdate);
        }

        /// <summary> Creates a new <see cref="Geometry"/> object by means of a set of <see cref="PointCollection"/>s. </summary>
        /// <param name="lines">Set of <see cref="PointCollection"/>s. </param>
        /// <returns>Geometry consisting of the input parameters.</returns>
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
