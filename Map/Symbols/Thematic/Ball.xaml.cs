using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;


namespace Ptv.XServer.Controls.Map.Symbols
{
    /// <summary> <para> A ball symbol.</para>
    /// <para>See the <conceptualLink target="101dba72-fb36-468b-aa99-4b9c5bbfb62f"/> topic for an example.</para> </summary>
    public partial class Ball
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Ball"/> class.
        /// </summary>
		public Ball()
		{
			// Required to initialize variables
			InitializeComponent();

			SetValue(LightColorProperty, Color.Lighten(1.5f));
            SetValue(DarkColorProperty, Color.Lighten(0.5f));
        }

        /// <summary>
        /// Gets or sets the color of this ball.
        /// </summary>
		public Color Color
        {
            get { return (Color)GetValue(ColorProperty); }
            set { SetValue(ColorProperty, value); }
        }

        /// <summary>
        /// Gets or sets the stroke of this ball.
        /// </summary>
        public Color Stroke
        {
          get { return (Color)GetValue(StrokeProperty); }
          set { SetValue(StrokeProperty, value); }
        }

        /// <summary> Documentation in progress... </summary>
        /// <param name="d"> Documentation in progress... </param>
        /// <param name="e"> Documentation in progress... </param>
		private static void ColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
             d.SetValue(LightColorProperty, ((Color)e.NewValue).Lighten(1.5f));
             d.SetValue(DarkColorProperty, ((Color)e.NewValue).Lighten(0.5f));
		}

        /// <summary>Using a DependencyProperty as the backing store for DarkColor.  This enables animation, styling, binding, etc...</summary>
        public static readonly DependencyProperty DarkColorProperty =   
            DependencyProperty.Register("DarkColor", typeof(Color), typeof(Ball));

        /// <summary>Using a DependencyProperty as the backing store for LightColor.  This enables animation, styling, binding, etc...</summary>
        public static readonly DependencyProperty LightColorProperty =
            DependencyProperty.Register("LightColor", typeof(Color), typeof(Ball));

        /// <summary>Using a DependencyProperty as the backing store for Color.  This enables animation, styling, binding, etc...</summary>
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(Color), typeof(Ball), new PropertyMetadata(Colors.Blue, ColorChanged));

        /// <summary>Using a DependencyProperty as the backing store for Stroke.  This enables animation, styling, binding, etc...</summary>
        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register("Stroke", typeof(Color), typeof(Ball), new PropertyMetadata(Colors.Black));
    }
}