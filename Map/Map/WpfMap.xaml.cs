// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System.Windows;
using System.Collections.Specialized;
using Ptv.XServer.Controls.Map.Gadgets;

namespace Ptv.XServer.Controls.Map
{
    /// <summary> The main WPF map control with pre-customized gadgets and themes. </summary>
    
    // TODO in Metadata Store/Assembly (Design assembly):
    //[Microsoft.Windows.Design.ToolboxBrowsable(true)]
    //[System.Drawing.ToolboxBitmap(typeof(WpfMap))]
    //[System.ComponentModel.Description("Ptv.XServer.Controls.Map.WpfMap")]

    // TODO: Is this used for WPF controls?
    //[System.ComponentModel.ToolboxItem(true)]

    public partial class WpfMap
    {
        #region private variables
        /// <summary> Flag showing if the scale gadget has already been initialized. </summary>
        private bool bScaleInitialized;
        /// <summary> Flag showing if the zoom slider gadget has already been initialized. </summary>
        private bool bZoomSliderInitialized;
        /// <summary> Flag showing if the coordinates gadget has already been initialized. </summary>
        private bool bCoordinatesInitialized;
        /// <summary> Flag showing if the overview map gadget has already been initialized. </summary>
        private bool bOverviewInitialized;
        /// <summary> Flag showing if the layers gadget has already been initialized. </summary>
        private bool bLayersInitialized;
        /// <summary> Flag showing if the magnifier gadget has already been initialized. </summary>
        private bool bMagnifierInitialized;
        /// <summary> Flag showing if the magnifier gadget has already been initialized. </summary>
        private bool bNavigationInitialized;
        #endregion

        #region dependency properties
        /// <summary> Property to show the scale gadget. </summary>
        public static readonly DependencyProperty ShowScaleProperty = DependencyProperty.Register("ShowScale", typeof(bool), typeof(WpfMap), new PropertyMetadata(true));
        /// <summary> Gets or sets a value indicating whether the scale gadget is shown or made invisible. </summary>
        public bool ShowScale
        {
            get => (bool)GetValue(ShowScaleProperty);
            set => SetValue(ShowScaleProperty, value);
        }

        /// <summary> Property to show the zoom slider gadget. </summary>
        public static readonly DependencyProperty ShowZoomSliderProperty = DependencyProperty.Register("ShowZoomSlider", typeof(bool), typeof(WpfMap), new PropertyMetadata(true));
        /// <summary> Gets or sets a value indicating whether the zoom slider gadget is shown or made invisible. </summary>
        public bool ShowZoomSlider
        {
            get => (bool)GetValue(ShowZoomSliderProperty);
            set => SetValue(ShowZoomSliderProperty, value);
        }

        /// <summary> Property to show the coordinates gadget. </summary>
        public static readonly DependencyProperty ShowCoordinatesProperty = DependencyProperty.Register("ShowCoordinates", typeof(bool), typeof(WpfMap), new PropertyMetadata(true));
        /// <summary> Gets or sets a value indicating whether the coordinates gadget is shown or made invisible. </summary>
        public bool ShowCoordinates
        {
            get => (bool)GetValue(ShowCoordinatesProperty);
            set => SetValue(ShowCoordinatesProperty, value);
        }

        /// <summary> Property to show the overview map. </summary>
        public static readonly DependencyProperty ShowOverviewProperty = DependencyProperty.Register("ShowOverview", typeof(bool), typeof(WpfMap), new PropertyMetadata(true));
        /// <summary> Gets or sets a value indicating whether the overview map is shown or made invisible. </summary>
        public bool ShowOverview
        {
            get => (bool)GetValue(ShowOverviewProperty);
            set => SetValue(ShowOverviewProperty, value);
        }

        /// <summary> Property to show the layers gadget. </summary>
        public static readonly DependencyProperty ShowLayersProperty = DependencyProperty.Register("ShowLayers", typeof(bool), typeof(WpfMap), new PropertyMetadata(true));
        /// <summary> Gets or sets a value indicating whether the layers gadget is shown or made invisible. </summary>
        public bool ShowLayers
        {
            get => (bool)GetValue(ShowLayersProperty);
            set => SetValue(ShowLayersProperty, value);
        }

        /// <summary> Property to show the magnifier gadget. </summary>
        public static readonly DependencyProperty ShowMagnifierProperty = DependencyProperty.Register("ShowMagnifier", typeof(bool), typeof(WpfMap), new PropertyMetadata(true));
        /// <summary> Gets or sets a value indicating whether the magnifier gadget is shown or made invisible. </summary>
        public bool ShowMagnifier
        {
            get => (bool)GetValue(ShowMagnifierProperty);
            set => SetValue(ShowMagnifierProperty, value);
        }

        /// <summary> Property to show the navigation gadget. </summary>
        public static readonly DependencyProperty ShowNavigationProperty = DependencyProperty.Register("ShowNavigation", typeof(bool), typeof(WpfMap), new PropertyMetadata(true));
        /// <summary> Gets or sets a value indicating whether the layers gadget is shown or made invisible. </summary>
        public bool ShowNavigation
        {
            get => (bool)GetValue(ShowNavigationProperty);
            set => SetValue(ShowNavigationProperty, value);
        }
        #endregion

        #region constructor
       
        /// <summary> Initializes a new instance of the <see cref="WpfMap"/> class. </summary>
        public WpfMap()
        {
            Gadgets.CollectionChanged += Gadgets_CollectionChanged;
            InitializeComponent();
        }
        #endregion

        #region event handling
        /// <summary> Handler for a change of the gadgets observable dictionary. Here we correct the hidden properties
        /// as they can be changed in the XAML code before the gadgets are initialized. </summary>
        /// <param name="sender"> Sender of the CollectionChanged event. </param>
        /// <param name="e"> Event parameters. </param>
        private void Gadgets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (bScaleInitialized && bZoomSliderInitialized && bCoordinatesInitialized && bOverviewInitialized && bLayersInitialized && bMagnifierInitialized && bNavigationInitialized)
                return;

            if (!bScaleInitialized && Gadgets.ContainsKey(GadgetType.Scale))
            {
                Gadgets[GadgetType.Scale].Visible = ShowScale;
                bScaleInitialized = true;
            }

            if (!bZoomSliderInitialized && Gadgets.ContainsKey(GadgetType.ZoomSlider))
            {
                Gadgets[GadgetType.ZoomSlider].Visible = ShowZoomSlider;
                bZoomSliderInitialized = true;
            }

            if (!bCoordinatesInitialized && Gadgets.ContainsKey(GadgetType.Coordinates))
            {
                Gadgets[GadgetType.Coordinates].Visible = ShowCoordinates;
                bCoordinatesInitialized = true;
            }

            if (!bOverviewInitialized && Gadgets.ContainsKey(GadgetType.Overview))
            {
                Gadgets[GadgetType.Overview].Visible = ShowOverview;
                bOverviewInitialized = true;
            }

            if (!bLayersInitialized && Gadgets.ContainsKey(GadgetType.Layers))
            {
                Gadgets[GadgetType.Layers].Visible = ShowLayers;
                bLayersInitialized = true;
            }

            if (!bMagnifierInitialized && Gadgets.ContainsKey(GadgetType.Magnifier))
            {
                Gadgets[GadgetType.Magnifier].Visible = ShowMagnifier;
                bMagnifierInitialized = true;
            }

            if (!bNavigationInitialized && Gadgets.ContainsKey(GadgetType.Navigation))
            {
                Gadgets[GadgetType.Navigation].Visible = ShowNavigation;
                bNavigationInitialized = true;
            }
        }
        #endregion
    }
}
