using System.Windows;
using System.Windows.Media;


namespace Ptv.XServer.Controls.Map.Symbols
{
	/// <summary>
    /// <para>Interaction logic for TriangleDown.xaml.</para>
    /// <para>See the <conceptualLink target="101dba72-fb36-468b-aa99-4b9c5bbfb62f"/> topic for an example.</para>
	/// </summary>
    public partial class TriangleDown
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="TriangleDown"/> class.
        /// </summary>
		public TriangleDown()
		{
			InitializeComponent();
		}

        /// <summary>
        /// Gets or sets the color of this triangle.
        /// </summary>
		public Color Color
        {
            get => (Color)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the stroke of this triangle.
        /// </summary>
        public Color Stroke
        {
            get => (Color)GetValue(StrokeProperty);
            set => SetValue(StrokeProperty, value);
        }

        /// <summary>Using a DependencyProperty as the backing store for Color.  This enables animation, styling, binding, etc...</summary>
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(Color), typeof(TriangleDown), new PropertyMetadata(Colors.Blue));

        /// <summary>Using a DependencyProperty as the backing store for Stroke Color.  This enables animation, styling, binding, etc...</summary>
        public static readonly DependencyProperty StrokeProperty =
            DependencyProperty.Register("Stroke", typeof(Color), typeof(TriangleDown), new PropertyMetadata(Colors.Black));
    }
}