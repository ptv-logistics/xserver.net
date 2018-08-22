//--------------------------------------------------------------
// Copyright (c) PTV Group
// 
// For license details, please refer to the file COPYING, which 
// should have been provided with this distribution.
//--------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Ptv.XServer.Controls.Map;

namespace Ptv.XServer.Demo.Geocoding
{
    /// <summary> <para>Control which offers a multi field geocoding. The geocoding request address is entered field by field.</para>
    /// <para>See the <conceptualLink target="fe48cb51-c6ce-487e-b4c0-168537c184c3"/> topic for an example.</para> </summary>
    public partial class MultiFieldControl
    {
        /// <summary> Initializes a new instance of the <see cref="MultiFieldControl"/> class. </summary>
        public MultiFieldControl()
        {
            InitializeComponent();
        }

        /// <summary> Event handler for a click on the locate button. Starts a multi field geocoding with the given
        /// field entries. </summary>
        /// <param name="sender"> Sender of the Click event. </param>
        /// <param name="e"> Event parameters. </param>
        private void LocateButton_Click(object sender, RoutedEventArgs e)
        {
            var source = FindResource("MultiFieldDataSource") as MultiFieldData;
            (FindResource("Geocoder") as GeocoderDemo)?.LocateMultiField(source);
        }

        /// <summary> Event handler for a change of the control visibility property. Adds or removes the geocoding
        /// layer to / from the map. </summary>
        /// <param name="sender"> Sender of the IsVisibleChanged event. </param>
        /// <param name="e"> Event parameters. </param>
        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            var geocoder = Resources["Geocoder"] as GeocoderDemo;

            // subsequent null checks keep the designer working
            if ((geocoder == null) || !(sender is UserControl)) return;

            switch (((UserControl) sender).Visibility)
            {
                case Visibility.Collapsed:
                {
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow != null)
                    {
                        geocoder.Remove(mainWindow.FindName("wpfMap") as WpfMap, false);
                    }
                    break;
                }
                case Visibility.Visible:
                {
                    var mainWindow = Application.Current.MainWindow as MainWindow;
                    if (mainWindow != null)
                    {
                        geocoder.AddTo(mainWindow.FindName("wpfMap") as WpfMap, "MultiFieldGC", false);
                    }
                    break;
                }
            }
        }

        /// <summary> Event handler for a key down in one of the text boxes. Starts geocoding when "enter" is pressed. </summary>
        /// <param name="sender"> Sender of the KeyDown event. </param>
        /// <param name="e"> Event handler. </param>
        private void box_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            if (   ((sender as TextBox) == countrybox) || ((sender as TextBox) == statebox) || ((sender as TextBox) == countrycodebox)
                || ((sender as TextBox) == citybox) || ((sender as TextBox) == streetbox))
            {
                locateButton.Focus();
                LocateButton_Click(this, null);
            }
        }
    }
}
