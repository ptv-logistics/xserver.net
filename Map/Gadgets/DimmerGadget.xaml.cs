// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;


namespace Ptv.XServer.Controls.Map.Gadgets
{
    /// <summary> Gadget with a dimmer slider to change the dim state of the map. </summary>
    public partial class DimmerGadget
    {
        #region private variables
        /// <summary> Canvas which dims the map by showing some transparent white or black colored layer over the map. </summary>
        private Canvas dimmerCanvas;
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="DimmerGadget"/> class. </summary>
        public DimmerGadget()
        {
            InitializeComponent();
        }
        #endregion

        #region protected methods
        /// <inheritdoc/>
        protected override void Initialize()
        {
            base.Initialize();

            dimmerCanvas = new Canvas
            {
                IsHitTestVisible = false,
                Width = MapView.ZoomAdjust * MapView.LogicalSize,
                Height = MapView.ZoomAdjust * MapView.LogicalSize
            };

            Canvas.SetLeft(dimmerCanvas, -dimmerCanvas.Width / 2);
            Canvas.SetTop(dimmerCanvas, -dimmerCanvas.Height / 2);
            Panel.SetZIndex(dimmerCanvas, 990);
            MapView.GeoCanvas.Children.Add(dimmerCanvas);
        }
        #endregion

        #region event handling
        /// <summary> Event handler for a change of the dim slider value. Adapts the dimmer canvas opacity due to the
        /// given dim value. </summary>
        /// <param name="sender"> Sender of the ValueChanged event. </param>
        /// <param name="e"> The event parameters. </param>
        private void dimmSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            dimmerCanvas.Background = dimmSlider.Value >= 0 ? 
                new SolidColorBrush(Color.FromArgb((byte)(dimmSlider.Value / 100.0 * 255.0), 255, 255, 255)) : 
                new SolidColorBrush(Color.FromArgb((byte)(-dimmSlider.Value / 100.0 * 255.0), 0, 0, 0));
        }

        /// <summary> Event handler for a click on the dim slider with the right mouse button. A right click on the dim
        /// slider resets its value to zero which means that the layer is no more dimmed. </summary>
        /// <param name="sender"> Sender of the MouseRightButtonDown event. </param>
        /// <param name="e"> The event parameters. </param>
        private void dimmSlider_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            dimmSlider.Value = 0;
            e.Handled = true;
        }
        #endregion
    }
}
