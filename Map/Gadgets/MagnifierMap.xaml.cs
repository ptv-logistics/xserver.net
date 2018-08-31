// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;


namespace Ptv.XServer.Controls.Map.Gadgets
{
    /// <summary><para> Gadget showing a magnifier on the map. The magnifier offers you to get a deeper look into the map
    /// without zooming in. </para>
    /// <para> See the <conceptualLink target="eb8e522c-5ed2-4481-820f-bfd74ee2aeb8"/> topic for an example. </para></summary>
    public partial class MagnifierMap
    {
        #region private variables
        /// <summary>The default size of the magnifier.</summary>
        private const double DefaultMagnifierSize = 500.0d;
        
        /// <summary> Minimum magnifier size. </summary>
        private const double MinMagnifierSize = 200.0d;

        /// <summary> Current pixel position of the mouse. </summary>
        private Point m_CurrentMousePosition;
        
        /// <summary> Current position of the mouse in geo coordinates. </summary>
        private Point m_CurrentMapPosition;
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="MagnifierMap"/> class. Initializes the base size
        /// with the default of 500 pixels. </summary>
        public MagnifierMap()
        {
            BaseSize = DefaultMagnifierSize;
            InitializeComponent();
        }
        #endregion

        #region protected methods
        /// <inheritdoc/>
        protected override void Initialize()
        {
            base.Initialize();

            MapView.KeyDown += map_KeyDown;
            MapView.MouseMove += parentMap_MouseMove;
            magnifierMap.MouseMove += parentMap_MouseMove;
            MapView.KeyUp += parentMap_KeyUp;
            MapView.LostFocus += HideMagnifier;
            MapView.LostKeyboardFocus += HideMagnifier; 
            magnifierMap.ParentMapView = MapView;

            Map.Gadgets.Add(GadgetType.Magnifier, this);
        }

        /// <inheritdoc/>
        protected override void UnInitialize()       {

            MapView.KeyDown -= map_KeyDown;
            MapView.MouseMove -= parentMap_MouseMove;
            magnifierMap.MouseMove -= parentMap_MouseMove;
            MapView.KeyUp -= parentMap_KeyUp;
            MapView.LostFocus -= HideMagnifier;
            MapView.LostKeyboardFocus -= HideMagnifier;

            Map.Gadgets.Remove(GadgetType.Magnifier);
            
            base.UnInitialize();
        }
        #endregion

        #region public properties
        /// <summary> Gets or sets the base value used to calculate the sizes of the magnifier. The value of this
        /// property is the overall diameter of the magnifier. This property can be altered manually so that a size
        /// change of the magnifier at runtime is possible. </summary>
        /// <value> Magnifier size. </value>
        public double BaseSize
        {
            get => (double)GetValue(BaseSizeProperty);
            set => SetValue(BaseSizeProperty, value);
        }

        /// <summary> The base size of the magnifier. </summary>
        public static readonly DependencyProperty BaseSizeProperty =
            DependencyProperty.Register("BaseSize", typeof(double), typeof(MagnifierMap));

        /// <summary> Gets or sets the magnifier radius.</summary>
        private double MagnifierRadius
        {
            get => (double)GetValue(MagnifierRadiusProperty);
            set => SetValue(MagnifierRadiusProperty, value);
        }

        /// <summary> The radius of the magnifier. This property is calculated on the basis of the BaseSize property.
        /// Do not alter it manually. </summary>
        public static readonly DependencyProperty MagnifierRadiusProperty =
            DependencyProperty.Register("MagnifierRadius", typeof(double), typeof(MagnifierMap));

        /// <summary> Gets or sets the content size (height and width of the magnifier).</summary>
        private double ContentSize
        {
            get => (double)GetValue(ContentSizeProperty);
            set => SetValue(ContentSizeProperty, value);
        }

        /// <summary> The content height and width of the magnifier. This property is calculated on the basis of the
        /// BaseSize property. Do not alter it manually. </summary>
        public static readonly DependencyProperty ContentSizeProperty =
            DependencyProperty.Register("ContentSize", typeof(double), typeof(MagnifierMap));

        /// <summary> Gets or sets the magnifier center point. </summary>
        private Point MagnifierCenter
        {
            get => (Point)GetValue(MagnifierCenterProperty);
            set => SetValue(MagnifierCenterProperty, value);
        }

        /// <summary> The center point of the magnifier. This property is calculated on the basis of the BaseSize
        /// property. Do not alter it manually. </summary>
        public static readonly DependencyProperty MagnifierCenterProperty =
            DependencyProperty.Register("MagnifierCenter", typeof(Point), typeof(MagnifierMap));

        /// <summary> Gets or sets the magnifier margin.</summary>
        private Thickness MagnifierMargin
        {
            get => (Thickness)GetValue(MagnifierMarginProperty);
            set => SetValue(MagnifierMarginProperty, value);
        }

        /// <summary> The margin of the magnifier. This property is calculated on the basis of the BaseSize property.
        /// Do not alter it manually. </summary>
        public static readonly DependencyProperty MagnifierMarginProperty =
            DependencyProperty.Register("MagnifierMargin", typeof(Thickness), typeof(MagnifierMap));

        /// <summary> Gets or sets the background path size.</summary>
        private double BackgroundPathSize
        {
            get => (double)GetValue(BackgroundPathSizeProperty);
            set => SetValue(BackgroundPathSizeProperty, value);
        }

        /// <summary> The size of the background paths. This property is calculated on the basis of the BaseSize
        /// property. Do not alter it manually. </summary>
        public static readonly DependencyProperty BackgroundPathSizeProperty =
            DependencyProperty.Register("BackgroundPathSizeMargin", typeof(double), typeof(MagnifierMap));
        #endregion

        #region event handling
        /// <summary> Event handler for pressing a key while the map is active. If you press "m", the magnifier is
        /// shown on the map. </summary>
        /// <param name="sender"> Sender of the KeyDown event. </param>
        /// <param name="e"> The event parameters. </param>
        private void map_KeyDown(object sender, KeyEventArgs e)
        {
            if (!visible || e.Key != Key.M || Visibility != Visibility.Collapsed) return;

            magnifierMap.ZoomDelta = 2;

            double mapSize = Math.Min(BaseSize, Math.Min(MapView.ActualHeight, MapView.ActualWidth));

            ApplySizes(this, Math.Max(mapSize, MinMagnifierSize));

            Canvas.SetLeft(this, m_CurrentMousePosition.X - Width / 2);
            Canvas.SetTop(this, m_CurrentMousePosition.Y - Height / 2);

            magnifierMap.SetPosition(m_CurrentMapPosition);
            Visibility = Visibility.Visible;
            magnifierMap.IsEnabled = true;
        }

        /// <summary> Event handler for releasing a key while the map is active. If you release the "m" key, the
        /// magnifier is no more shown on the map. </summary>
        /// <param name="sender"> Sender of the KeyUp event. </param>
        /// <param name="e"> The event parameters. </param>
        private void parentMap_KeyUp(object sender, KeyEventArgs e)
        {
            if (visible && e.Key == Key.M && Visibility == Visibility.Visible)
            {
                HideMagnifier(null, null);
            }
        }

        /// <summary> Event handler for moving the mouse over the map. Moves the magnifier on the map. </summary>
        /// <param name="sender"> Sender of the MouseMove event. </param>
        /// <param name="e"> The event parameters. </param>
        private void parentMap_MouseMove(object sender, MouseEventArgs e)
        {
            m_CurrentMousePosition = e.GetPosition(MapView);
            m_CurrentMapPosition = e.GetPosition(MapView.GeoCanvas);

            if (Visibility != Visibility.Visible) return;

            Canvas.SetLeft(this, m_CurrentMousePosition.X - Width / 2);
            Canvas.SetTop(this, m_CurrentMousePosition.Y - Height / 2);
        }
        #endregion

        #region treating the visible property
        /// <summary> Flag showing if the coordinates gadget is to be shown or hidden. </summary>
        private bool visible = true;
        
        /// <inheritdoc/>
        public override bool Visible
        {
            get => visible;
            set => visible = value;
        }
        #endregion

        /// <summary> Helper method to calculate the sizes of the magnifier properties like radius, center, margin and
        /// size. </summary>
        /// <param name="magnifier"> The magnifier to be altered. </param>
        /// <param name="baseSize"> The new base size. </param>
        private static void CalculateSizes(MagnifierMap magnifier, double baseSize)
        {
            magnifier.MagnifierRadius = baseSize / 4;
            magnifier.ContentSize = baseSize / 2;
            magnifier.MagnifierCenter = new Point(magnifier.MagnifierRadius, magnifier.MagnifierRadius);
            magnifier.MagnifierMargin = new Thickness(magnifier.MagnifierRadius, magnifier.MagnifierRadius, 0, 0);
            magnifier.BackgroundPathSize = baseSize * 0.576075d;
        }

        /// <summary> Helper method to set the size of the magnifier UIElements. </summary>
        /// <param name="magnifier"> The magnifier to alter. </param>
        /// <param name="baseSize"> The new base size. </param>
        private static void ApplySizes(MagnifierMap magnifier, double baseSize)
        {
            CalculateSizes(magnifier, baseSize);

            magnifier.Width = baseSize;
            magnifier.Height = baseSize;

            if (magnifier.magnifierMap != null)
            {
                magnifier.magnifierMap.Width = magnifier.ContentSize;
                magnifier.magnifierMap.Height = magnifier.ContentSize;

                if (magnifier.magnifierMap.Clip != null)
                {
                    ((EllipseGeometry)magnifier.magnifierMap.Clip).Center = magnifier.MagnifierCenter;
                }
            }

            if (magnifier.BackgroundEllipse != null)
            {
                magnifier.BackgroundEllipse.Width = magnifier.ContentSize;
                magnifier.BackgroundEllipse.Height = magnifier.ContentSize;
                magnifier.BackgroundEllipse.Margin = magnifier.MagnifierMargin;
            }

            if (magnifier.BackgroundPath1 != null)
            {
                magnifier.BackgroundPath1.Margin = magnifier.MagnifierMargin;
            }

            if (magnifier.BackgroundPath2 != null)
            {
                magnifier.BackgroundPath2.Margin = magnifier.MagnifierMargin;
            }
        }

        /// <summary> Helper method to hide the magnifier. </summary>
        /// <param name="sender"> Sender of the LostFocus event. </param>
        /// <param name="e"> Event parameters. </param>
        private void HideMagnifier(object sender, object e)
        {
            Visibility = Visibility.Collapsed;
            magnifierMap.IsEnabled = false;
        }
    }   
}
