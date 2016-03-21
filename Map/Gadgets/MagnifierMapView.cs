using System.Windows;
using Ptv.XServer.Controls.Map.Tools;


namespace Ptv.XServer.Controls.Map.Gadgets
{
    /// <summary><para> Map displayed in the magnifier. </para>
    /// <para> See the <conceptualLink target="eb8e522c-5ed2-4481-820f-bfd74ee2aeb8"/> topic for an example. </para></summary>
    public class MagnifierMapView : MapView
    {
        #region private variables
        /// <summary> Parent map of the magnifier. </summary>
        private MapView parentMapView;
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="MagnifierMapView"/> class. </summary>
        public MagnifierMapView()
        {
            IsEnabled = false;
        }
        #endregion

        #region public methods
        /// <summary> Gets or sets the zoom delta of the magnifier in reference to the map. </summary>
        /// <value> Zoom delta of the magnifier. </value>
        public int ZoomDelta { get; set; }

        /// <summary> Gets or sets the parent map which is magnified. </summary>
        /// <value> Parent map. </value>
        public MapView ParentMapView
        {
            set
            {
                if (parentMapView != null)
                {
                    MouseWheel -= MagnifierMap_MouseWheel;
                    MouseMove -= parentMapView_MouseMove;
                }

                parentMapView = value;

                if (parentMapView != null)
                {
                    MouseMove += parentMapView_MouseMove;
                    MouseWheel += MagnifierMap_MouseWheel;
                }
            }
            get
            {
                return parentMapView;
            }
        }

        /// <summary> Gets a value indicating whether animation is used for map actions. </summary>
        private bool UseAnimation
        {
            get
            {
                var map = MapElementExtensions.FindParent<Map>(this);
                return (map != null) && map.UseAnimation;
            }
        }

        /// <summary> Sets the position of the magnifier and calculates the zoom in order to display the magnified map. </summary>
        /// <param name="mousePosition"> Current position of the mouse indicating where the magnifier is positioned. </param>
        public void SetPosition(Point mousePosition)
        {
            double dxa = mousePosition.X - ReferenceSize / 2;
            double dya = mousePosition.Y - ReferenceSize / 2;

            double x = dxa / ZoomAdjust * LogicalSize / ReferenceSize;
            double y = -dya / ZoomAdjust * LogicalSize / ReferenceSize;

            int newZoom = (int)parentMapView.FinalZoom + ZoomDelta;

            SetXYZ(x, y, newZoom, UseAnimation);
        }
        #endregion

        #region event handling
        /// <summary> Event handler for changing the mouse wheel when the map is active. Scrolling the mouse wheel in
        /// forward direction zooms in the map, scrolling back zooms out of the map. </summary>
        /// <param name="sender"> Sender of the MouseWheel event. </param>
        /// <param name="e"> The event parameters. </param>
        private void MagnifierMap_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            int delta = e.Delta / 120;

            ZoomDelta += delta;

            if (ZoomDelta + parentMapView.FinalZoom > parentMapView.MaxZoom)
                ZoomDelta = parentMapView.MaxZoom - (int)parentMapView.FinalZoom;

            if (parentMapView.FinalZoom + ZoomDelta < parentMapView.MinZoom)
                ZoomDelta = parentMapView.MinZoom - (int)parentMapView.FinalZoom;

            SetZoom(parentMapView.FinalZoom + ZoomDelta, UseAnimation);

            e.Handled = true;
        }

        /// <summary> Event handler for moving the mouse over the map. Moves the magnifier over the map and shows the
        /// corresponding map part magnified. </summary>
        /// <param name="sender"> Sender of the MouseMove event. </param>
        /// <param name="e"> The event parameters. </param>
        private void parentMapView_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(parentMapView.GeoCanvas);
            SetPosition(mousePosition);
        }
        #endregion
    }
}
