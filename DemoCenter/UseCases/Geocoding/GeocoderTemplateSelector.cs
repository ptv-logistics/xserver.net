//--------------------------------------------------------------
// Copyright (c) PTV Group
// 
// For license details, please refer to the file COPYING, which 
// should have been provided with this distribution.
//--------------------------------------------------------------

using System.Windows.Controls;
using System.Windows;

namespace Ptv.XServer.Demo.Geocoding
{
    /// <summary> Selector of the data template which is used for the AutoCompleteTextBox. Either a wait message or the
    /// geocoding results can be displayed there. </summary>
    public class GeocoderTemplateSelector : DataTemplateSelector
    {
        /// <inheritdoc/>
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            Window wnd = Application.Current.MainWindow;
            return (item is string) ? wnd.FindResource("WaitTemplate") as DataTemplate : wnd.FindResource("GeocoderSuggestionTemplate") as DataTemplate;
        }
    }
}
