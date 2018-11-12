// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Windows;
using System.Windows.Controls;


namespace Ptv.XServer.Controls.Map.Gadgets
{
    /// <summary> Gadget showing a zoom slider on the map. </summary>
    public partial class ZoomSliderGadget
    {
        #region private variables
        /// <summary> Flag showing if a change of the map should lead to an update or not. </summary>
        private bool selfNotify;
        /// <summary> Orientation of the zoom slider. </summary>
        private Orientation orientation = Orientation.Vertical;
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="ZoomSliderGadget"/> class. </summary>
        public ZoomSliderGadget()
        {
            InitializeComponent();
        }

        /// <inheritdoc/>
        protected override void Initialize()
        {
            base.Initialize();

            zoomSlider.Maximum = MapView.MaxZoom * 10;
            zoomSlider.Minimum = MapView.MinZoom * 10;
            zoomSlider.Value = MapView.FinalZoom * 10;


            zoomSlider.ValueChanged += zoomSlider_ValueChanged;
            MapView.ViewportBeginChanged += mapView_MapChangedEvent;

            Map.Gadgets.Add(GadgetType.ZoomSlider, this);
        }

        /// <inheritdoc/>
        protected override void UnInitialize()
        {
            zoomSlider.ValueChanged -= zoomSlider_ValueChanged;
            MapView.ViewportBeginChanged -= mapView_MapChangedEvent;

            Map.Gadgets.Remove(GadgetType.ZoomSlider);

            base.UnInitialize();
        }
        #endregion

        #region public methods
        /// <summary> Gets or sets the orientation (horizontal or vertical) of the zoom slider. </summary>
        /// <value> Orientation of the zoom slider. </value>
        public Orientation Orientation
        {
            get => orientation;

            set
            {
                orientation = value;
                
                zoomSlider.Orientation = orientation;

                if (orientation == Orientation.Vertical)
                {
                    contentGrid.HorizontalAlignment = HorizontalAlignment.Center;
                    contentGrid.VerticalAlignment = VerticalAlignment.Stretch;

                    contentGrid.RowDefinitions.Clear();
                    contentGrid.RowDefinitions.Add(new RowDefinition {Height = GridLength.Auto});
                    contentGrid.RowDefinitions.Add(new RowDefinition {Height = new GridLength(1, GridUnitType.Star)});
                    contentGrid.RowDefinitions.Add(new RowDefinition {Height = GridLength.Auto});

                    contentGrid.ColumnDefinitions.Clear();
                    contentGrid.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(1, GridUnitType.Star)});

                    UpViewbox.SetValue(Grid.RowProperty, 0);
                    UpViewbox.SetValue(Grid.ColumnProperty, 0);
                    SliderViewbox.SetValue(Grid.RowProperty, 1);
                    SliderViewbox.SetValue(Grid.ColumnProperty, 0);
                    DownViewbox.SetValue(Grid.RowProperty, 2);
                    DownViewbox.SetValue(Grid.ColumnProperty, 0);

                    zoomSlider.Margin = new Thickness(10,2,0,2);
                    zoomSlider.Height = 260;
                    zoomSlider.Width = 25;
                    zoomSlider.Orientation = Orientation.Vertical;
                    zoomSlider.HorizontalAlignment = HorizontalAlignment.Center;
                    zoomSlider.VerticalAlignment = VerticalAlignment.Stretch;
                    zoomSlider.TickPlacement = System.Windows.Controls.Primitives.TickPlacement.TopLeft;
                }
                else
                {
                    contentGrid.HorizontalAlignment = HorizontalAlignment.Stretch;
                    contentGrid.VerticalAlignment = VerticalAlignment.Center;

                    contentGrid.RowDefinitions.Clear();
                    contentGrid.RowDefinitions.Add(new RowDefinition {Height = new GridLength(1, GridUnitType.Star)});

                    contentGrid.ColumnDefinitions.Clear();
                    contentGrid.ColumnDefinitions.Add(new ColumnDefinition {Width = GridLength.Auto});
                    contentGrid.ColumnDefinitions.Add(new ColumnDefinition {Width = new GridLength(1, GridUnitType.Star)});
                    contentGrid.ColumnDefinitions.Add(new ColumnDefinition {Width = GridLength.Auto});

                    DownViewbox.SetValue(Grid.RowProperty, 0);
                    DownViewbox.SetValue(Grid.ColumnProperty, 0);
                    SliderViewbox.SetValue(Grid.RowProperty, 0);
                    SliderViewbox.SetValue(Grid.ColumnProperty, 1);
                    UpViewbox.SetValue(Grid.RowProperty, 0);
                    UpViewbox.SetValue(Grid.ColumnProperty, 2);

                    zoomSlider.Margin = new Thickness(2, 10, 2, 0);
                    zoomSlider.Height = 25;
                    zoomSlider.Width = 260;
                    zoomSlider.Orientation = Orientation.Horizontal;
                    zoomSlider.HorizontalAlignment = HorizontalAlignment.Stretch;
                    zoomSlider.VerticalAlignment = VerticalAlignment.Center;
                    zoomSlider.TickPlacement = System.Windows.Controls.Primitives.TickPlacement.BottomRight;
                }
            }
        }

        /// <summary> Zoom factor of the map. </summary>
        public static readonly DependencyProperty MapZoomProperty = DependencyProperty.Register("MapZoom", typeof(int), typeof(ZoomSliderGadget), null);

        /// <summary> Gets or sets current map zoom. </summary>
        /// <value> Zoom factor of the map. </value>
        public int MapZoom
        {
            get => (int)GetValue(MapZoomProperty);
            set
            {
                selfNotify = true;
                SetValue(MapZoomProperty, value);
                selfNotify = false;
            }
        }
        #endregion

        #region event handling
        /// <summary> Event handler for changing the zoom slider. On a change, the map is zoomed in or out due to the
        /// change direction ad amount of the zoom slider value. </summary>
        /// <param name="sender"> Sender of the ValueChanged event. </param>
        /// <param name="e"> The event parameters. </param>
        private void zoomSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (selfNotify)
                return;

            if (MapView.FinalZoom * 10 != MapZoom)
                MapView.SetZoom((double)MapZoom / 10, Map.UseAnimation);
        }

        /// <summary> Event handler for a change of the map. Updates the value of the zoom slider. </summary>
        /// <param name="sender"> Sender of the MapChangedEvent. </param>
        /// <param name="e"> The event parameters. </param>
        private void mapView_MapChangedEvent(object sender, EventArgs e)
        {
            MapZoom = (int)(MapView.FinalZoom * 10);
        }

        /// <summary> Handles a click on the up button which zooms in the map. Updates the zoom slider value. </summary>
        /// <param name="sender"> Sender of the Click event. </param>
        /// <param name="e"> Event parameters. </param>
        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            zoomSlider.Value = Math.Min(zoomSlider.Maximum, zoomSlider.Value + 10);
        }

        /// <summary> Handler for a click on the down button which zooms out of the map. Updates the zoom slider value. </summary>
        /// <param name="sender"> Sender of the Click event. </param>
        /// <param name="e"> Event parameters. </param>
        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            zoomSlider.Value = Math.Max(zoomSlider.Minimum, zoomSlider.Value - 10);
        }
        #endregion
    }
}
