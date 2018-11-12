// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Windows;
using System.Windows.Input;
using Ptv.XServer.Controls.Map.Tools;
using System.Windows.Media;
using System.Globalization;


namespace Ptv.XServer.Controls.Map.Gadgets
{
    /// <summary><para> Gadget showing the map coordinates of the current mouse position in logical format (GeoMinSec). </para>
    /// <para> See the <conceptualLink target="eb8e522c-5ed2-4481-820f-bfd74ee2aeb8"/> topic for an example. </para></summary>
    public partial class CoordinatesGadget
    {
        #region private variables
        /// <summary> Boolean flag showing whether the mouse is on the map or not. </summary>
        private bool isActive;

        /// <summary> Constant holding the text which is shown if the mouse is not currently on the map. </summary>
        private const string INVALID_COORD_TEXT = "\u2013\u2013° \u2013\u2013′ \u2013\u2013″ \u2013, \u2013\u2013\u2013° \u2013\u2013′ \u2013\u2013″ \u2013";
        
        /// <summary> Constant holding the text which is used to calculate the width of the coordinates gadget text box. </summary>
        private const string EXTENT_CALC_DUMMY = "XX° XX′ XX″ X, XXX° XX′ XX″ X";
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="CoordinatesGadget"/> class. </summary>
        public CoordinatesGadget()
        {
            InitializeComponent();
        }
        #endregion

        #region protected methods
        /// <inheritdoc/>
        protected override void Initialize()
        {
            base.Initialize();
            CoordinatesText.Text = INVALID_COORD_TEXT;

            MapView.MouseMove += Map_MouseMove;
            MapView.MouseEnter += Map_MouseEnter;

            Map.Gadgets.Add(GadgetType.Coordinates, this);
        }

        /// <inheritdoc/>
        protected override void UnInitialize()
        {
            MapView.MouseMove -= Map_MouseMove;
            MapView.MouseEnter -= Map_MouseEnter;

            Map.Gadgets.Remove(GadgetType.Coordinates);

            base.UnInitialize();
        }
        #endregion

        #region private methods
        /// <summary> Updates the text which is showing the current coordinates. </summary>
        private void UpdateText()
        {
            if (!isActive)
                return;

            Point pixelPoint = Mouse.GetPosition(MapView);
            Point wgsPoint = MapView.CanvasToWgs(MapView.Layers, pixelPoint);

            if (wgsPoint.Y < -90 || wgsPoint.Y > 90 || wgsPoint.X < -180 || wgsPoint.X > 180)
            {
                CoordinatesText.Text = INVALID_COORD_TEXT;
            }
            else
            {
                switch (Map.CoordinateDiplayFormat)
                {
                    case CoordinateDiplayFormat.Decimal:
                        CoordinatesText.Text = $"{wgsPoint.Y:.000000}°, {wgsPoint.X:.000000}°";
                        break;
                    case CoordinateDiplayFormat.Degree:
                        CoordinatesText.Text = GeoTransform.LatLonToString(wgsPoint.Y, wgsPoint.X, true);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
        #endregion

        #region event handling
        /// <summary> Event handler for entering the map with the mouse. If the mouse enters the map, the coordinates
        /// text is updated. </summary>
        /// <param name="sender"> Sender of the MouseEnter event. </param>
        /// <param name="e"> The event parameters. </param>
        private void Map_MouseEnter(object sender, MouseEventArgs e)
        {
            isActive = true;
            UpdateText();
        }

        /// <summary> Event handler for moving the mouse over the map. At each new position, the coordinates text is
        /// updated. </summary>
        /// <param name="sender"> Sender of the MouseMove event. </param>
        /// <param name="e"> The event parameters. </param>
        private void Map_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton.Equals(MouseButtonState.Pressed))
                return;
            UpdateText();
        }

        /// <summary> Event handler for having finished loading the coordinates text. Calculates the appropriate width
        /// for this gadget by respecting the current font family and the font size. </summary>
        /// <param name="sender"> Sender of the Loaded event. </param>
        /// <param name="e"> Event parameters. </param>
        private void CoordinatesText_Loaded(object sender, RoutedEventArgs e)
        {
            var formattedText = new FormattedText(EXTENT_CALC_DUMMY, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                new Typeface(CoordinatesText.FontFamily.ToString()), CoordinatesText.FontSize, Brushes.Black);
            CoordinatesText.Width = formattedText.Width + CoordinatesText.Padding.Left + CoordinatesText.Padding.Right;
            CoordinatesText.MinWidth = CoordinatesText.Width;
        }
        #endregion

        #region treating the hidden property
        /// <inheritdoc/>
        public override bool Visible
        {
            get => base.Visible;
            set
            {
                base.Visible = value;
                UpdateText();
            }
        }
        #endregion
    }
}
