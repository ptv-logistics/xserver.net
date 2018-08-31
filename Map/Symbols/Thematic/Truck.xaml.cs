using System.Windows;
using System.Windows.Media;


namespace Ptv.XServer.Controls.Map.Symbols
{
    /// <summary> Interaction logic for Truck.xaml </summary>
    public partial class Truck
    {
        /// <summary> Initializes a new instance of the <see cref="Truck"/> class. </summary>
        public Truck()
        {
            InitializeComponent();

            // Calculate the initial light and dark color shades.
            SetValue(LightColorProperty, Color.Lighten(1.5f));
            SetValue(DarkColorProperty, Color.Lighten(0.5f));
        }

        /// <summary> Gets or sets the color of this truck. </summary>
        public Color Color
        {
            get => (Color)GetValue(ColorProperty);
            set => SetValue(ColorProperty, value);
        }

        /// <summary> Event handling a change of the truck color. </summary>
        /// <param name="d"> Object to change. </param>
        /// <param name="e"> Event parameters containing the new color. </param>
        private static void ColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            d.SetValue(LightColorProperty, ((Color)e.NewValue).Lighten(1.5f));
            d.SetValue(DarkColorProperty, ((Color)e.NewValue).Lighten(0.5f));
        }

        /// <summary> Using a DependencyProperty as the backing store for Color. This enables animation, styling, binding, etc. . </summary>
        public static readonly DependencyProperty ColorProperty =
            DependencyProperty.Register("Color", typeof(Color), typeof(Truck), new PropertyMetadata(Colors.Blue, ColorChanged));

        /// <summary> Using a DependencyProperty as the backing store for DarkColor. This enables animation, styling, binding, etc. .</summary>
        public static readonly DependencyProperty DarkColorProperty =
            DependencyProperty.Register("DarkColor", typeof(Color), typeof(Truck));

        /// <summary> Using a DependencyProperty as the backing store for LightColor. This enables animation, styling, binding, etc. .</summary>
        public static readonly DependencyProperty LightColorProperty =
            DependencyProperty.Register("LightColor", typeof(Color), typeof(Truck));
    }
}
