// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Linq;

// ReSharper disable SpecifyACultureInStringConversionExplicitly


namespace Ptv.XServer.Controls.Map.Gadgets
{
    /// <summary><para> Gadget showing the current scale of the map. </para>
    /// <para> See the <conceptualLink target="eb8e522c-5ed2-4481-820f-bfd74ee2aeb8"/> topic for an example. </para></summary>
    public partial class ScaleGadget
    {
        #region private variables

        /// <summary> Maximum length of the scale gadget. </summary>
        private const double maxLength = 80;

        /// <summary> Possible km-scales of the map. </summary>
        private readonly ScaleInfo[] scalesKm =
        {
            new ScaleInfo {Dimension = 1000000, Text = "1000 km"},
            new ScaleInfo {Dimension = 500000, Text = "500 km"},
            new ScaleInfo {Dimension = 250000, Text = "250 km"},
            new ScaleInfo {Dimension = 100000, Text = "100 km"},
            new ScaleInfo {Dimension = 50000, Text = "50 km"},
            new ScaleInfo {Dimension = 25000, Text = "25 km"},
            new ScaleInfo {Dimension = 10000, Text = "10 km"},
            new ScaleInfo {Dimension = 5000, Text = "5 km"},
            new ScaleInfo {Dimension = 2500, Text = Convert.ToString(2.5) + " km"},
            new ScaleInfo {Dimension = 1000, Text = "1000 m"},
            new ScaleInfo {Dimension = 500, Text = "500 m"},
            new ScaleInfo {Dimension = 250, Text = "250 m"},
            new ScaleInfo {Dimension = 100, Text = "100 m"},
            new ScaleInfo {Dimension = 50, Text = "50 m"},
            new ScaleInfo {Dimension = 25, Text = "25 m"},
            new ScaleInfo {Dimension = 10, Text = "10 m"},
            new ScaleInfo {Dimension = 5, Text = "5 m"},
            new ScaleInfo {Dimension = 2.5, Text = Convert.ToString(2.5) + " m"},
            new ScaleInfo {Dimension = 1, Text = "1 m"}
        };

        /// <summary> Possible mile-scales of the map. </summary>
        private readonly ScaleInfo[] scalesMiles =
        {
            // 1 km = 0.621371192 miles
            // 1 mile = 1760 yard
            new ScaleInfo {Dimension = 804672.000/*metersPerPixel*/, Text = "500 miles"},
            new ScaleInfo {Dimension = 402336.000, Text = "250 miles"},
            new ScaleInfo {Dimension = 160934.400, Text = "100 miles"},
            new ScaleInfo {Dimension = 80467.200, Text = "50 miles"},
            new ScaleInfo {Dimension = 40233.600, Text = "25 miles"},
            new ScaleInfo {Dimension = 16093.440, Text = "10 miles"},
            new ScaleInfo {Dimension = 8046.720, Text = "5 miles"},
            new ScaleInfo {Dimension = 4023.360, Text = Convert.ToString(2.5) + " miles"},
            new ScaleInfo {Dimension = 1609.344, Text = "1 mile"},
            new ScaleInfo {Dimension = 914.400, Text = "1000 yard"},
            new ScaleInfo {Dimension = 457.200, Text = "500 yard"},
            new ScaleInfo {Dimension = 228.600, Text = "250 yard"},
            new ScaleInfo {Dimension = 91.440, Text = "100 yard"},
            new ScaleInfo {Dimension = 45.720, Text = "50 yard"},
            new ScaleInfo {Dimension = 22.860, Text = "25 yard"},
            new ScaleInfo {Dimension = 9.144, Text = "10 yard"},
            new ScaleInfo {Dimension = 4.527, Text = "5 yard"},
            new ScaleInfo {Dimension = 2.286, Text = Convert.ToString(2.5) + " yard"},
            new ScaleInfo {Dimension = 0.914, Text = "1 yard"}
        };

        /// <summary> Flag to choose whether the scale is to be shown in km or in miles. </summary>
        private bool showMiles;
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="ScaleGadget"/> class. </summary>
        public ScaleGadget()
        {
            InitializeComponent();
        }

        /// <inheritdoc/>
        protected override void Initialize()
        {
            base.Initialize();

            MapView.ViewportWhileChanged += Map_ViewportWhileChanged;
            Map.UseMilesChanged += Map_UseMilesChanged;
            Map.Gadgets.Add(GadgetType.Scale, this);

            UpdateScale();
        }

        /// <inheritdoc/>
        protected override void UnInitialize()
        {
            MapView.ViewportWhileChanged -= Map_ViewportWhileChanged;
            Map.UseMilesChanged -= Map_UseMilesChanged;

            Map.Gadgets.Remove(GadgetType.Scale);

            base.UnInitialize();
        }
        #endregion

        #region helper methods
        /// <summary> Retrieves the best fitting scale for the currently displayed map. </summary>
        /// <param name="metersPerPixel"> Meters in reality which you can find per pixel on screen. </param>
        /// <returns> The best fitting scale information. </returns>
        private ScaleInfo FindBestScale(double metersPerPixel)
        {
            var scaleInfos = showMiles ? scalesMiles : scalesKm;
            return scaleInfos.FirstOrDefault(scaleInfo => scaleInfo.Dimension / metersPerPixel <= maxLength) ?? scaleInfos.Last();
        }

        /// <summary> Updates the scale information. </summary>
        private void UpdateScale()
        {
            // Do not show if map is too small.
            Opacity = MapView.CurrentScale > 30000 ? 0 : 1;

            showMiles = Map.UseMiles;

            // calculate meters per pixel considering the Mercator projection
            ScaleInfo scaleInfo = FindBestScale(MapView.MetersPerPixel);
            double length = scaleInfo.Dimension / MapView.MetersPerPixel;

            ScaleCanvas.Width = length;
            Text.Text = scaleInfo.Text;
        }
        #endregion

        #region event handling
        /// <summary> Event handler for a change of the map viewport. Updates the scale. </summary>
        /// <param name="sender"> Sender of the ViewportWhileChanged event. </param>
        /// <param name="e"> The event parameters. </param>
        private void Map_ViewportWhileChanged(object sender, EventArgs e)
        {
            UpdateScale();
        }

        /// <summary> Event handler for a change of the showMiles property. Updates the scale. </summary>
        /// <param name="sender"> Sender of the UseMilesChanged event. </param>
        /// <param name="e"> Event parameters. </param>
        private void Map_UseMilesChanged(object sender, EventArgs e)
        {
            UpdateScale();
        }
        #endregion
    }

    /// <summary><para> Scale information object. </para>
    /// <para> See the <conceptualLink target="eb8e522c-5ed2-4481-820f-bfd74ee2aeb8"/> topic for an example. </para></summary>
    public class ScaleInfo
    {
        #region public variables
        /// <summary> Gets or sets the text describing the scale. </summary>
        /// <value> Description text of the scale. </value>
        public string Text { get; set; }
        /// <summary> Gets or sets the dimension of the scale. </summary>
        /// <value> Dimension of the scale. </value>
        public double Dimension { get; set; }
        #endregion
    }
}
