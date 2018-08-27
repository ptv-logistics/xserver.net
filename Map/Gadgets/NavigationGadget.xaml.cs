// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;


namespace Ptv.XServer.Controls.Map.Gadgets
{
    /// <summary><para> Interaction logic for NavigationGadget. </para>
    /// <para> See the <conceptualLink target="eb8e522c-5ed2-4481-820f-bfd74ee2aeb8"/> and
    /// <conceptualLink target="fdaa5363-b092-43cc-950e-6f120dee0d92"/> topics for examples. </para></summary>
    public partial class NavigationGadget
    {
        #region constructor
        /// <summary> Initializes a new instance of the <see cref="NavigationGadget"/> class. </summary>
        public NavigationGadget()
        {
            InitializeComponent();
        }
        #endregion

        #region protected methods
        /// <inheritdoc/>
        protected override void Initialize()
        {
            base.Initialize();
            Map.Gadgets.Add(GadgetType.Navigation, this);

            //if (BackgroundColorBrush is SolidColorBrush)
            //{
            //    Color tmpColor = (BackgroundColorBrush as SolidColorBrush).Color;
            //    Color color1 = Color.FromRgb(ColorExtensions.Lighten(tmpColor.R), ColorExtensions.Lighten(tmpColor.G), ColorExtensions.Lighten(tmpColor.B));
            //    Color color2 = Color.FromRgb(ColorExtensions.Darken(tmpColor.R), ColorExtensions.Darken(tmpColor.G), ColorExtensions.Darken(tmpColor.B));
            //    ButtonBackgroundColorBrush = new LinearGradientBrush(color1, color2, new Point(0, 0), new Point(0, 1));
            //}
            //else
            //    ButtonBackgroundColorBrush = BackgroundColorBrush;
        }

        /// <inheritdoc/>
        protected override void UnInitialize()
        {
            Map.Gadgets.Remove(GadgetType.Navigation);

            base.UnInitialize();
        }
        #endregion

        #region event handling
        /// <summary> Event handler for a click on the up button. Scrolls the map in upwards direction. </summary>
        /// <param name="sender"> Sender of the Click event. </param>
        /// <param name="e"> Event parameters. </param>
        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            #region doc:move up
            // move the current map location by 25 %
            double dY = MapView.FinalEnvelope.Height * 0.25;

            // Setting the new map location.
            MapView.SetXYZ(MapView.FinalX, MapView.FinalY + dY, MapView.FinalZoom, Map.UseAnimation);
            #endregion
        }

        /// <summary> Handler for a click on the down button. Scrolls the map in downwards direction. </summary>
        /// <param name="sender"> Sender of the Click event. </param>
        /// <param name="e"> Event parameters. </param>
        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            #region doc:move down
            // move the current map location by 25 %
            double dY = MapView.FinalEnvelope.Height * 0.25;

            // Setting the new map location.
            MapView.SetXYZ(MapView.FinalX, MapView.FinalY - dY, MapView.FinalZoom, Map.UseAnimation);
            #endregion
        }

        /// <summary> Handler for a click on the right button. Scrolls the map in right direction. </summary>
        /// <param name="sender"> Sender of the Click event. </param>
        /// <param name="e"> Event parameters. </param>
        private void RightButton_Click(object sender, RoutedEventArgs e)
        {
            #region doc:move right
            // move the current map location by 25 %
            double dX = MapView.FinalEnvelope.Width * 0.25;

            // Setting the new map location.
            MapView.SetXYZ(MapView.FinalX + dX, MapView.FinalY, MapView.FinalZoom, Map.UseAnimation);
            #endregion
        }

        /// <summary> Handler for a click on the left button. Scrolls the map in left direction. </summary>
        /// <param name="sender"> Sender of the Click event. </param>
        /// <param name="e"> Event parameters. </param>
        private void LeftButton_Click(object sender, RoutedEventArgs e)
        {
            #region doc:move left
            // move the current map location by 25 %
            double dX = MapView.FinalEnvelope.Width * 0.25;

            // Setting the new map location.
            MapView.SetXYZ(MapView.FinalX - dX, MapView.FinalY, MapView.FinalZoom, Map.UseAnimation);
            #endregion
        }

        /// <summary> Handler for entering the navigation gadget with the mouse. The navigation gadget is faded in
        /// and shown in opaque colors. </summary>
        /// <param name="sender"> Sender of the MouseEnter event. </param>
        /// <param name="e"> Event parameters. </param>
        private void Navigation_MouseEnter(object sender, MouseEventArgs e)
        {
            ((Storyboard)FindResource("fadeIn")).Begin(Navigation);
        }

        /// <summary> Handler for leaving the navigation gadget with the mouse. The navigation gadget is faded out
        /// and shown in transparent colors. </summary>
        /// <param name="sender"> Sender of the MouseLeave event. </param>
        /// <param name="e"> Event parameters. </param>
        private void Navigation_MouseLeave(object sender, MouseEventArgs e)
        {
            ((Storyboard)FindResource("fadeOut")).Begin(Navigation);
        }
        #endregion
    }
}
