// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Ptv.XServer.Controls.Map.Layers;
using Ptv.XServer.Controls.Map.Tools;


namespace Ptv.XServer.Controls.Map.Gadgets
{
    /// <summary><para> Map showing a small overview of the parent map. </para>
    /// <para> See the <conceptualLink target="eb8e522c-5ed2-4481-820f-bfd74ee2aeb8"/> topic for an example. </para></summary>
    public class OverviewMapView : MapView, IWeakEventListener
    {
        #region private variables
        /// <summary> The parent map of the overview map. </summary>
        private MapView parentMapView;
        /// <summary> Flag showing if a change in the map should lead to an update or not. </summary>
        private bool selfNotify;
        /// <summary> Rectangle showing the currently visible part of the parent map. </summary>
        private readonly Rectangle dragRectangle;
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="OverviewMapView"/> class. </summary>
        public OverviewMapView()
        {
            ZoomDelta = 4;
            IsEnabled = false;

            dragRectangle = new Rectangle
            {
                IsHitTestVisible = false,
                Fill = new SolidColorBrush(Color.FromArgb(0x80, 0xaa, 0xaa, 0xaa)),
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeDashArray = new DoubleCollection(new double[] {1, 4}),
                StrokeEndLineCap = PenLineCap.Round,
                StrokeDashCap = PenLineCap.Round,
                StrokeThickness = 1.5,
                RadiusX = 8,
                RadiusY = 8
            };

            var c = new Canvas();
            c.Children.Add(dragRectangle);
            c.RenderTransform = TransformFactory.CreateTransform(SpatialReference.PtvMercator);
            Panel.SetZIndex(c, 15000);
            GeoCanvas.Children.Add(c);
        }
        #endregion

        #region public methods
        /// <summary> Gets or sets the zoom delta of the overview map in reference to the parent map. The default zoom delta is 4. </summary>
        /// <value> Zoom delta of the overview map. </value>
        public int ZoomDelta { get; set; }

        /// <summary> Gets or sets the parent map for which the overview map shows the overview. </summary>
        /// <value> Parent map of the overview map. </value>
        public MapView ParentMapView
        {
            set
            {
                if (parentMapView != null)
                {
                    ViewportBeginChangedWeakEventManager.RemoveListener(parentMapView, this);
                    ViewportBeginChangedWeakEventManager.RemoveListener(this, this);
                }

                parentMapView = value;

                if (parentMapView != null)
                {
                    ViewportBeginChangedWeakEventManager.AddListener(parentMapView, this);
                    ViewportBeginChangedWeakEventManager.AddListener(this, this);

                    SetZoom(false);
                }
            }
            get => parentMapView;
        }

        /// <summary> Updates the rectangle showing the currently visible part of the parent map. </summary>
        public void UpdateRect()
        {
            MapRectangle rect = parentMapView.FinalEnvelope;

            Canvas.SetLeft(dragRectangle, rect.West + parentMapView.OriginOffset.X);
            Canvas.SetTop(dragRectangle, rect.South - parentMapView.OriginOffset.Y);
            dragRectangle.Width = rect.Width;
            dragRectangle.Height = rect.Height;
            dragRectangle.StrokeThickness = 1.5 * FinalScale;
        }

        /// <summary> Gets a value indicating whether animation is used for map actions. </summary>
        private bool UseAnimation
        {
            get
            {
                var map = MapElementExtensions.FindParent<Map>(this);
                return map != null && map.UseAnimation;
            }
        }
 
        /// <summary> Updates the overview map. </summary>
        /// <param name="animate"> Flag showing if the update is to be animated or not. </param>
        public void UpdateOverviewMap(bool animate)
        {
            if (!MapElementExtensions.IsControlVisible(this))
                return;

            SetZoom(animate);
        }
        #endregion

        #region helper methods
        /// <summary> Sets the zoom of the overview map. </summary>
        /// <param name="animatePan"> Flag showing if zooming is animated or not. </param>
        private void SetZoom(bool animatePan)
        {
            int newZoom = (int)parentMapView.FinalZoom - ZoomDelta;
            if (newZoom < parentMapView.MinZoom)
                newZoom = parentMapView.MinZoom;

            selfNotify = true;

            SetXYZ(parentMapView.FinalX, parentMapView.FinalY, newZoom, animatePan && UseAnimation, UseAnimation);

            selfNotify = false;

            UpdateRect();
        }

        /// <summary> Updates the parent map. </summary>
        private void UpdateParentMap()
        {
            if (selfNotify)
                return;

            int newZoom = (int)FinalZoom + ZoomDelta;
            if (newZoom > parentMapView.MaxZoom)
                newZoom = parentMapView.MaxZoom;

            parentMapView.SetXYZ(FinalX, FinalY, newZoom, false);
        }
        #endregion
        
        #region IWeakEventListener Members
        /// <summary> Receives events from the centralized event manager. </summary>
        /// <param name="managerType"> The type of the System.Windows.WeakEventManager calling this method. </param>
        /// <param name="sender"> Object that originated the event. </param>
        /// <param name="e"> Event data. </param>
        /// <returns> True if the listener handled the event. It is considered an error by the
        ///           System.Windows.WeakEventManager handling in WPF to register a listener for an event that the
        ///           listener does not handle. Regardless, the method should return false if it receives an event that
        ///           it does not recognize or handle. </returns>
        public bool ReceiveWeakEvent(Type managerType, object sender, EventArgs e)
        {
            if (managerType != typeof (ViewportBeginChangedWeakEventManager)) return false;
            if (ReferenceEquals(sender, parentMapView))
                UpdateOverviewMap(UseAnimation);

            return true;
        }

        #endregion
    }
}
