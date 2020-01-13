// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Ptv.XServer.Controls.Map.Layers;
using Ptv.XServer.Controls.Map.Tools;

namespace Ptv.XServer.Controls.Map.Canvases
{
    /// <summary> The main canvas which holds viewable elements for a map. </summary>
    public abstract class MapCanvas : Canvas, IWeakEventListener
    {
        #region constructor
        /// <summary> Initializes a new instance of the <see cref="MapCanvas"/> class. Stores the parent map and adds
        /// listeners to the map viewport changed events. </summary>
        /// <param name="mapView"> The instance of the parent map. </param>
        protected MapCanvas(MapView mapView)
        {
            MapView = mapView;
            ViewportBeginChangedWeakEventManager.AddListener(mapView, this);
            ViewportEndChangedWeakEventManager.AddListener(mapView, this);
            ViewportWhileChangedWeakEventManager.AddListener(mapView, this);
        }
        #endregion

        #region properties
        /// <summary> Gets the parent map instance. </summary>
        /// <value> The parent map. </value>
        public MapView MapView { get; }

        /// <summary> Gets or sets the <see cref="CanvasCategory"/> of the canvas. The canvas category defines the z
        /// order of the canvas in the map.</summary>
        /// <value> The canvas category. </value>
        public CanvasCategory CanvasCategory { get; set; }
        #endregion

        #region disposal
        /// <summary> Disposes the map canvas. During disposal the children of the canvas are removed and the viewport
        /// changed events are disconnected. </summary>
        public virtual void Dispose()
        {
            (Parent as Canvas)?.Children.Remove(this);

            ViewportBeginChangedWeakEventManager.RemoveListener(MapView, this);
            ViewportWhileChangedWeakEventManager.RemoveListener(MapView, this);
            ViewportEndChangedWeakEventManager.RemoveListener(MapView, this);
        }
        #endregion

        #region public methods

        /// <summary>
        /// Converts a geographic point to a canvas coordinate.
        /// </summary>
        /// <param name="geoPoint">The geographic point.</param>
        /// <param name="spatialReferenceId">The spatial reference identifier.</param>
        /// <returns> The transformed canvas coordinate. </returns>
        public Point GeoToCanvas(Point geoPoint, string spatialReferenceId)
        {
            return PtvMercatorToCanvas(GeoTransform.Transform(geoPoint, spatialReferenceId, "PTV_MERCATOR"));
        }

        /// <summary> Converts a geographic point to a canvas point. </summary>
        /// <param name="geoPoint"> The geographic point. </param>
        /// <returns> The canvas point. </returns>
        public Point GeoToCanvas(Point geoPoint)
        {
            return PtvMercatorToCanvas(GeoTransform.WGSToPtvMercator(geoPoint));
        }

        /// <summary> Converts a canvas point to a geographic point. </summary>
        /// <param name="canvasPoint"> The canvas point. </param>
        /// <returns> The geographic point. </returns>
        public Point CanvasToGeo(Point canvasPoint)
        {
            return GeoTransform.PtvMercatorToWGS(CanvasToPtvMercator(canvasPoint));
        }

        /// <summary> Updates the map content. The map content consists of all elements of the canvas. This method is
        /// for example triggered when the viewport changes. </summary>
        /// <param name="updateMode"> The update mode tells which kind of change is to be processed by the update call. </param>
        public abstract void Update(UpdateMode updateMode);

        /// <summary> Callback to precede the updating of the map content. </summary>
        /// <param name="updateMode"> The update mode tells which kind of change is to be processed by the update call. </param>
        protected virtual void BeforeUpdate(UpdateMode updateMode)
        {
        }
        #endregion

        #region IWeakEventListener Members
        /// <summary> Receives events from the centralized event manager. </summary>
        /// <param name="managerType">The type of the WeakEventManager calling this method.</param>
        /// <param name="sender">Object that originated the event.</param>
        /// <param name="e">Event data.</param>
        /// <returns> True if the listener handled the event. It is considered an error by the WeakEventManager
        /// handling in WPF to register a listener for an event that the listener does not handle. Regardless,
        /// the method should return false if it receives an event that it does not recognize or handle. </returns>
        public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            switch (managerType.Name)
            {
                case nameof(ViewportBeginChangedWeakEventManager):
                    BeforeUpdate(UpdateMode.BeginTransition);
                    Update(UpdateMode.BeginTransition);
                    return true;
                case nameof(ViewportWhileChangedWeakEventManager):
                    BeforeUpdate(UpdateMode.WhileTransition);
                    Update(UpdateMode.WhileTransition);
                    return true;
                case nameof(ViewportEndChangedWeakEventManager):
                    BeforeUpdate(UpdateMode.EndTransition);
                    Update(UpdateMode.EndTransition);
                    return true;
                default:
                    return false;
            }
        }
        #endregion

        #region protected methods
        /// <summary> This method implements a transformation from canvas coordinates to logical coordinates. </summary>
        /// <param name="canvasPoint"> The canvas point. </param>
        /// <returns> The logical point. </returns>
        public abstract Point CanvasToPtvMercator(Point canvasPoint);

        /// <summary> This method implements a transformation from logical coordinates to canvas coordinates. </summary>
        /// <param name="mercatorPoint"> The Mercator point. </param>
        /// <returns> The canvas point. </returns>
        public abstract Point PtvMercatorToCanvas(Point mercatorPoint);
        #endregion
    }
    
    /// <summary> Indicates the mode of the update. </summary>
    public enum UpdateMode
    {
        /// <summary> Called for initial insert or refresh on the layer. </summary>
        Refresh,

        /// <summary> Called when a transition begins. </summary>
        BeginTransition,

        /// <summary> Called during a transition. </summary>
        WhileTransition,

        /// <summary> Called after a transition (Current-xxx-Values == Final-xxx-values). </summary>
        EndTransition
    }

    /// <summary>
    /// The enumeration defines the category of map canvases. The categories specify the primary z-order of canvases on
    /// the map. All canvases within the same category are grouped according to their index in the layer collection.
    /// </summary>
    public enum CanvasCategory
    {
        /// <summary> Category for the base map (i.e. xServer/Bing) content. </summary>
        BaseMap,

        /// <summary> Category for the user content. </summary>
        Content,

        /// <summary> Category for the labels of the user content. </summary>
        ContentLabels,

        /// <summary> Category for the selected objects. </summary>
        SelectedObjects,

        /// <summary> Category for top-most objects. </summary>
        TopMost
    }

    /// <summary>
    /// A canvas holds the graphic items of a map. One or more canvases are used for a layer. There are two main types
    /// of canvases: A <see cref="WorldCanvas"/>, whose elements have positions and dimensions in world Mercator units
    /// and a <see cref="ScreenCanvas"/>, whose elements have positions and dimensions in screen (pixel) units. By
    /// using multiple canvases, the elements for different layers can interleave for different
    /// <see cref="CanvasCategory"/> types. 
    /// </summary>
    [CompilerGenerated]
    public class NamespaceDoc
    {
        // NamespaceDoc is used for providing the root documentation for Ptv.Components.Projections
        // hint taken from http://stackoverflow.com/questions/156582/namespace-documentation-on-a-net-project-sandcastle
    }
    
    /// <summary>
    /// Canvas with screen coordinates. Elements of the canvas have an absolute dimension (= size in pixels) but have
    /// to be repositioned whenever the viewport changes.
    /// </summary>
    public abstract class ScreenCanvas : MapCanvas
    {
        #region constructor
        /// <summary> Initializes a new instance of the <see cref="ScreenCanvas"/> class. </summary>
        /// <param name="mapView"> The instance of the parent map. </param>
        protected ScreenCanvas(MapView mapView) : this(mapView, true)
        {
        }

        /// <summary> Initializes a new instance of the <see cref="ScreenCanvas"/> class. If the second parameter
        /// <paramref name="addToMap"/> is set to true, the new screen canvas is added to the parent map.</summary>
        /// <param name="mapView"> The instance of the parent map. </param>
        /// <param name="addToMap"> Indicates that the canvas should be inserted to the parent map immediately. </param>
        protected ScreenCanvas(MapView mapView, bool addToMap)
            : base(mapView)
        {
            if (addToMap)
            {
                mapView.TopPaneCanvas.Children.Add(this);
            }
        }
        #endregion

        #region public methods
        /// <inheritdoc/>  
        public override Point CanvasToPtvMercator(Point canvasPoint)
        {
            return MapView.CanvasToPtvMercator(this, canvasPoint);
        }

        /// <inheritdoc/>  
        public override Point PtvMercatorToCanvas(Point mercatorPoint)
        {
            return MapView.PtvMercatorToCanvas(this, mercatorPoint);
        }
        #endregion
    }

    /// <summary>
    /// Canvas with world coordinates. Elements of the canvas do not have to be repositioned when the viewport changes,
    /// but have world-dimension (= size in Mercator units).
    /// </summary>
    public abstract class WorldCanvas : MapCanvas
    {
        #region private variables

        private TranslateTransform offsetTransform;

        private Point localOffset;
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="WorldCanvas"/> class. If the parameter
        /// <paramref name="addToMap"/> is set to true, the canvas is added to the parent map. </summary>
        /// <param name="mapView"> The instance of the parent map. </param>
        /// <param name="addToMap"> Indicates that the canvas should inserted to the parent map immediately. </param>
        /// <param name="localOffset"> An optional offset for the Mercator units. </param>
        protected WorldCanvas(MapView mapView, bool addToMap = true, Point localOffset = new Point())
            : base(mapView)
        {
            this.localOffset = localOffset; //new Point(935569, 6268360);

            InitializeTransform();

            if (addToMap)
            {
                mapView.GeoCanvas.Children.Add(this);
            }
        }
        #endregion

        #region public methods

        /// <summary> Initializes the transformation instance which is needed to transform coordinates from one format
        /// to another one. </summary>
        public virtual void InitializeTransform()
        {
            var trans = TransformFactory.CreateTransform(SpatialReference.PtvMercatorInvertedY);

            offsetTransform = new TranslateTransform(MapView.OriginOffset.X + localOffset.X,
                MapView.OriginOffset.Y - localOffset.Y);
            trans = new TransformGroup
            {
                Children = {offsetTransform, trans}
            };

            RenderTransform = trans;
        }

        #endregion

        #region protected methods
        /// <inheritdoc/>  
        public override Point CanvasToPtvMercator(Point canvasPoint)
        {
            return new Point(canvasPoint.X + localOffset.X, -canvasPoint.Y + localOffset.Y);
        }

        /// <inheritdoc/>  
        public override Point PtvMercatorToCanvas(Point mercatorPoint)
        {
            return new Point(mercatorPoint.X - localOffset.X, -mercatorPoint.Y + localOffset.Y);
        }

        /// <inheritdoc/>  
        protected override void BeforeUpdate(UpdateMode updateMode)
        {
            if (updateMode != UpdateMode.EndTransition) return;

            // unshift the local offset parameters
            if(offsetTransform != null)
            { 
                offsetTransform.X = MapView.OriginOffset.X + localOffset.X;
                offsetTransform.Y = MapView.OriginOffset.Y - localOffset.Y;
            }
        }

        #endregion
    }
}
