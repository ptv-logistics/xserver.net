// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using Ptv.XServer.Controls.Map.Tools;
using Ptv.XServer.Controls.Map.Canvases;
using Ptv.Components.Projections;

namespace Ptv.XServer.Controls.Map.Layers.Shapes
{
    /// <summary><para> This class represents a layer containing shape objects. </para>
    /// <para> See the <conceptualLink target="06a654f3-afbd-4f00-9c8e-36997e2a3951"/> topic for an example. </para></summary>
    public class ShapeLayer : BaseLayer
    {
        #region private variables
        /// <summary> Collections of shape elements contained in this layer. </summary>
        private readonly ObservableCollection<FrameworkElement> shapes = new ObservableCollection<FrameworkElement>();
        #endregion

        #region public variables

        /// <summary> Gets or sets the spatial reference number as a string. The spatial reference number defines the
        /// coordinate system to which the coordinates of the shapes belong. </summary>
        public string SpatialReferenceId { get; set; }

        /// <summary> Gets the collection of shapes to be displayed by this layer. </summary>
        public ObservableCollection<FrameworkElement> Shapes => shapes;

        #endregion

        /// <summary> Gets or sets the update strategy for shapes when the map viewport changes. If lazy update is activated,
        /// the shapes are only updated at the end of the viewport transition. </summary>
        public bool LazyUpdate { get; set; }

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="ShapeLayer"/> class. By default, the spatial reference system is set to "EPSG:4326". </summary>
        /// <param name="name"> Name of the layer. </param>
        public ShapeLayer(string name)
            : base(name)
        {
            SpatialReferenceId = "EPSG:4326";
            InitializeFactory(CanvasCategory.Content,
             map => (map.Name == "Map") ? new ShapeCanvas(map, shapes, SpatialReferenceId, LazyUpdate) : null);
        }
        #endregion
    }

    /// <summary><para> Canvas displaying shape elements on a map. </para>
    /// <para> See the <conceptualLink target="06a654f3-afbd-4f00-9c8e-36997e2a3951"/> topic for an example. </para></summary>
    public class ShapeCanvas : WorldCanvas
    {
        #region private variables
        /// <summary> Collection of shapes to be painted on this canvas. </summary>
        private readonly ObservableCollection<FrameworkElement> shapes;
        /// <summary> The map to which this canvas is added. </summary>
        private readonly MapView mapView;

        /// <summary> Gets or sets if the shapes should updated after viewport end only. </summary>
        private bool lazyUpdate { get; }
        #endregion

        #region public variables
        /// <summary> Transformation method which transforms the shape points from one coordinate format to another
        /// depending on the given spatial reference system. </summary>
        public Func<Point, Point> transform;
        #endregion

        #region properties
        /// <summary> Location of a shape element. </summary>
        public static readonly DependencyProperty LocationProperty =
            DependencyProperty.Register("LocationProperty", typeof(Point), typeof(FrameworkElement),
            new FrameworkPropertyMetadata(OnLocationChanged));

        /// <summary> Retrieves the location of a certain shape element. </summary>
        /// <param name="element"> The element of which the location should be retrieved. </param>
        /// <returns> The shape element location as a point. Returns a Point with zeroed coordinates if none was set.</returns>
        [AttachedPropertyBrowsableForChildren]
        public static Point GetLocation(UIElement element)
        {
            var result = (Point)element.GetValue(LocationProperty);
            if (!result.IsValidGeoCoordinate() && (element is MapPolylineBase mapPolyline))
                result = new MapRectangle(mapPolyline.Points).Center;
            return result;
        }
        /// <summary> Sets the location of a certain shape element. </summary>
        /// <param name="element"> Shape element for which a location should be set. </param>
        /// <param name="location"> New location of the shape element. </param>
        public static void SetLocation(FrameworkElement element, Point location)
        {
            element.SetValue(LocationProperty, location);
        }

        /// <summary> Anchor of a shape element. </summary>
        public static readonly DependencyProperty AnchorProperty =
            DependencyProperty.Register("AnchorProperty", typeof(LocationAnchor), typeof(FrameworkElement),
            new FrameworkPropertyMetadata(OnAnchorChanged));

        /// <summary> Retrieves the Anchor of a certain shape element. </summary>
        /// <param name="element"> The element of which the anchor should be retrieved. </param>
        /// <returns> The shape element anchor as a point. Returns a Point with zeroed coordinates if none was set.</returns>
        [AttachedPropertyBrowsableForChildren]
        public static LocationAnchor GetAnchor(UIElement element)
        {
            return (LocationAnchor)element.GetValue(AnchorProperty);
        }
        /// <summary> Sets the anchor of a certain shape element. </summary>
        /// <param name="element"> Shape element for which a anchor should be set. </param>
        /// <param name="anchor"> New anchor of the shape element. </param>
        public static void SetAnchor(FrameworkElement element, LocationAnchor anchor)
        {
            element.SetValue(AnchorProperty, anchor);
        }

        /// <summary>
        /// The ScaleFactor element defines the dependency between map scale and
        /// the size of map objects. A scale factor of 0 indicates a constant object size. The size of
        /// the map object is the size in pixel, without regarding the zoom factor. A scale factor of 1
        /// indicates an object size in coordinate units. The object will be enlarged if the map is
        /// zoomed. It is also possible to choose a value between 0 and 1. According to that
        /// value, a nonlinear enlargement will be used.
        /// </summary>
        public static readonly DependencyProperty ScaleFactorProperty =
            DependencyProperty.Register("ScaleFactorProperty", typeof(double), typeof(FrameworkElement),
            new FrameworkPropertyMetadata(OnScaleFactorChanged));

        /// <summary> Retrieves the scale factor of a certain shape element. </summary>
        /// <param name="element"> Shape element of which the scale factor should be retrieved. </param>
        /// <returns> Shape element scale factor. Returns 0 if none was set.</returns>
        [AttachedPropertyBrowsableForChildren]
        public static double GetScaleFactor(UIElement element)
        {
            return (double)element.GetValue(ScaleFactorProperty);
        }

        /// <summary> Sets the scale factor of a certain shape element. </summary>
        /// <param name="element"> Shape element for which the scale factor is to be set. </param>
        /// <param name="scaleFactor"> New scale factor of the shape element. </param>
        public static void SetScaleFactor(FrameworkElement element, double scaleFactor)
        {
            element.SetValue(ScaleFactorProperty, scaleFactor);
        }

        /// <summary>
        /// The Scale element defines the dependency between map scale and
        /// the size of map objects. An LSF of 0 indicates a constant object size. The size of
        /// the map object is the size in pixel, without regarding the zoom factor. An LSF of 1
        /// indicates an object size in  units. The object will be enlarged if the map is
        /// zoomed. It’s also possible to choose a value between 0 and 1. According to that
        /// value, a nonlinear enlargement will be used.
        /// </summary>
        public static readonly DependencyProperty ScaleProperty =
            DependencyProperty.Register("ScaleProperty", typeof(double), typeof(FrameworkElement),
            new FrameworkPropertyMetadata(1.0, OnScaleChanged));

        /// <summary> Retrieves the scale factor of a certain shape element. </summary>
        /// <param name="element"> Shape element of which the scale factor should be retrieved. </param>
        /// <returns> Shape element scale factor. Returns 0 if none was set.</returns>
        [AttachedPropertyBrowsableForChildren]
        public static double GetScale(UIElement element)
        {
            return (double)element.GetValue(ScaleProperty);
        }

        /// <summary> Sets the scale factor of a certain shape element. </summary>
        /// <param name="element"> Shape element for which the scale factor is to be set. </param>
        /// <param name="scale"> New scale factor of the shape element. </param>
        public static void SetScale(FrameworkElement element, double scale)
        {
            element.SetValue(ScaleProperty, scale);
        }
        #endregion

        #region constructor

        /// <summary> Initializes a new instance of the <see cref="ShapeCanvas"/> class. Adds all shape elements to the canvas. </summary>
        /// <param name="mapView"> Map on which the canvas is to be displayed. </param>
        /// <param name="shapes"> The shape elements which are to be painted on the canvas. </param>
        /// <param name="spatialReferenceId"> Spatial reference system to which the point of the shapes refer. </param>
        /// <param name="lazyUpdate">The shapes should be updated after viewport end only.</param>
        public ShapeCanvas(MapView mapView, ObservableCollection<FrameworkElement> shapes, string spatialReferenceId, bool lazyUpdate)
            : base(mapView)
        {
            this.mapView = mapView;
            this.lazyUpdate = lazyUpdate;

            switch (spatialReferenceId)
            {
                case "PTV_MERCATOR": transform = PtvMercatorToCanvas; break;
                case "EPSG:4326": transform = GeoToCanvas; break;
                default: SetProj4Transform(spatialReferenceId); break;
            }

            this.shapes = shapes;
            shapes.CollectionChanged += shapes_CollectionChanged;

            foreach (var shape in shapes)
                Add(shape);

            UpdateScales(UpdateMode.EndTransition);
        }
        #endregion

        #region private methods
        /// <summary> Event handler for a change of the scale factor of a certain shape element. Updates the scale of
        /// the element in the shape canvas. </summary>
        /// <param name="obj"> Element for which the scale has been changed. </param>
        /// <param name="args"> Event parameters. </param>
        private static void OnScaleFactorChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var element = obj as FrameworkElement;

            (element?.Parent as ShapeCanvas)?.UpdateScale(element, ((ShapeCanvas) element.Parent).MapView, UpdateMode.Refresh);
        }

        /// <summary> Event handler for a change of the scale factor of a certain shape element. Updates the scale of
        /// the element in the shape canvas. </summary>
        /// <param name="obj"> Element for which the scale has been changed. </param>
        /// <param name="args"> Event parameters. </param>
        private static void OnScaleChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var element = obj as FrameworkElement;

            (element?.Parent as ShapeCanvas)?.UpdateScale(element, ((ShapeCanvas) element.Parent).MapView, UpdateMode.Refresh);
        }
        
        /// <summary> Event handler for a location change of a certain shape element. Updates the location of the
        /// element on the shape canvas. </summary>
        /// <param name="obj"> Element for which the location has been changed. </param>
        /// <param name="args"> Event parameters. </param>
        private static void OnLocationChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var element = obj as FrameworkElement;

            if (element?.Parent is ShapeCanvas shapeCanvas)
            {
                var location = (Point)args.NewValue;
                shapeCanvas.UpdateLocation(element, location);
                var canvasPoint = shapeCanvas.transform(location);
                shapeCanvas.UpdateLocation(element, canvasPoint);
            }
        }

        private void UpdateLocation(FrameworkElement element, Point canvasPoint)
        {
            double width = element.ActualWidth;
            double height = element.ActualHeight;
            double dx = 0, dy = 0;
            var x = GetAnchor(element);
            switch (x)
            {
                case LocationAnchor.Center: dx = width / 2; dy = height / 2; break;
                case LocationAnchor.LeftTop: dx = 0; dy = 0; break;
                case LocationAnchor.LeftBottom: dx = 0; dy = height; break;
                case LocationAnchor.RightTop: dx = width; dy = 0; break;
                case LocationAnchor.RightBottom: dx = width; dy = height; break;
            }

            SetLeft(element, canvasPoint.X - dx);
            SetTop(element, canvasPoint.Y - dy);
        }

        /// <summary> Event handler for a location change of a certain shape element. Updates the location of the
        /// element on the shape canvas. </summary>
        /// <param name="obj"> Element for which the location has been changed. </param>
        /// <param name="args"> Event parameters. </param>
        private static void OnAnchorChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
        {
            var element = obj as FrameworkElement;

            if (element?.Parent is ShapeCanvas shapeCanvas)
            {
                var location = GetLocation(obj as UIElement);
                shapeCanvas.UpdateLocation(element, location);
                var canvasPoint = shapeCanvas.transform(location);
                shapeCanvas.UpdateLocation(element, canvasPoint);
            }
        }

        /// <summary> Sets a new coordinate transformation method. </summary>
        /// <param name="spatialReferenceId"> The new spatial reference id to be used. </param>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void SetProj4Transform(string spatialReferenceId)
        {
            var t = CoordinateTransformation.Get(spatialReferenceId, "PTV_MERCATOR");
            transform = p => PtvMercatorToCanvas(t.Transform(p));
        }
        #endregion

        #region public methods
        /// <summary> Adds a shape element to the shape canvas. </summary>
        /// <param name="shape"> Shape element to be added. </param>
        private void Add(FrameworkElement shape)
        {
            if (shape is MapShape mapShape)
            {
                mapShape.GeoTransform = transform;

                if (mapView != null)
                {
                    mapShape.UpdateShape(mapView, UpdateMode.Refresh, false);
                }

                Children.Add(shape);
                return;
            }

            shape.SizeChanged += ShapeSizeChanged;
            Children.Add(shape);
            shape.UpdateLayout();

            if (shape.GetValue(LocationProperty) != null)
            {
                UpdateLocation(shape, transform(GetLocation(shape)));
            }

            UpdateScale(shape, MapView, UpdateMode.Refresh);
        }

        private void Remove(FrameworkElement shape)
        {
            shape.SizeChanged -= ShapeSizeChanged;
            Children.Remove(shape);
        }
        
        private void ShapeSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var element = sender as FrameworkElement;

            if (element?.Parent is ShapeCanvas shapeCanvas)
            {
                var location = GetLocation(element);
                UpdateLocation(element, location);
                var canvasPoint = shapeCanvas.transform(location);

                UpdateLocation(element, canvasPoint);
            }
        }

        /// <inheritdoc/>
        public override void Update(UpdateMode updateMode)
        {
            UpdateScales(updateMode);
        }

        /// <summary>
        /// Check if an update of a shape object is necessary, according to 
        /// the lazy mode or update mode.
        /// </summary>
        /// <param name="isLazyUpdate">Indicates if object should be updated only at the end of a transition.</param>
        /// <param name="updateMode">At which part the current transition is.</param>
        /// <returns>True if an update is necessary, otherwise false.</returns>
        protected bool NeedsUpdate(bool isLazyUpdate, UpdateMode updateMode)
        {
            return
                (isLazyUpdate && updateMode == UpdateMode.EndTransition)
                || (!isLazyUpdate && updateMode == UpdateMode.WhileTransition)
                || updateMode == UpdateMode.Refresh;
        }


        /// <summary> Updates the scale of a certain shape element. </summary>
        /// <param name="shape"> Shape element for which the scale is to be updated. </param>
        /// <param name="inputMapView"> MapView object, which contains the corresponding shape layer. It is needed for obtaining its currently used scale.</param>
        /// <param name="updateMode">At which part the current transition is.</param>
        public void UpdateScale(FrameworkElement shape, MapView inputMapView, UpdateMode updateMode)
        {
            if (shape is MapShape mapShape)
            {
                mapShape.UpdateShape(inputMapView, updateMode, lazyUpdate);
            }
            else
            {
                if (updateMode == UpdateMode.EndTransition && GlobalOptions.InfiniteZoom)
                {
                    if (shape.GetValue(LocationProperty) != null)
                    {
                        UpdateLocation(shape, transform(GetLocation(shape)));
                    }
                }
                if (NeedsUpdate(lazyUpdate, updateMode))
                {
                    var scale = inputMapView.CurrentScale;
                    double lsf = GetScaleFactor(shape);
                    double elementScale = GetScale(shape);
                    if (lsf == 1 && elementScale == 1)
                    {
                        shape.RenderTransform = null;
                    }
                    else
                    {
                        double scl = Math.Pow(scale, 1 - lsf);
                        shape.RenderTransform = new ScaleTransform(scl*elementScale, scl*elementScale);
                    }

                    var x = GetAnchor(shape);
                    switch (x)
                    {
                        case LocationAnchor.Center: shape.RenderTransformOrigin = new Point(.5, .5); break;
                        case LocationAnchor.LeftTop: shape.RenderTransformOrigin = new Point(0, 0); break;
                        case LocationAnchor.LeftBottom: shape.RenderTransformOrigin = new Point(0, 1); break;
                        case LocationAnchor.RightTop: shape.RenderTransformOrigin = new Point(1, 0); break;
                        case LocationAnchor.RightBottom: shape.RenderTransformOrigin = new Point(1, 1); break;
                    }
                }
            }
        }

        /// <summary> Updates the scale of a whole set of shape elements. All shape elements which are currently
        /// visible on the displayed map section are updated. </summary>
        /// <param name="updateMode"> Mode specifying in which context the update method has been called.. </param>
        public void UpdateScales(UpdateMode updateMode)
        {
            foreach (var shape in shapes)
                UpdateScale(shape, MapView, updateMode);
        }
        #endregion

        #region event handling
        /// <summary> Event handler for a change of the shape elements collection. New shapes are added to the canvas,
        /// for a reset all shapes are deleted from the canvas and all persisting elements are updated. </summary>
        /// <param name="sender"> Sender of the CollectionChanged event. </param>
        /// <param name="e"> Event parameters. </param>
        private void shapes_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case System.Collections.Specialized.NotifyCollectionChangedAction.Add:
                    foreach (var shape in e.NewItems)
                        Add(shape as FrameworkElement);
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Remove:
                    foreach (var shape in e.OldItems)
                        Remove(shape as FrameworkElement);
                    break;
                case System.Collections.Specialized.NotifyCollectionChangedAction.Reset:
                    Children.Clear();
                    break;
            }
        }
        #endregion

        #region disposal
        /// <inheritdoc/>
        public override void Dispose()
        {
            base.Dispose();
            Children.Clear();

            shapes.CollectionChanged -= shapes_CollectionChanged;
        }
        #endregion
    }
}
