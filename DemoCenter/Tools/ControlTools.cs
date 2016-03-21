//--------------------------------------------------------------
// Copyright (c) PTV Group
// 
// For license details, please refer to the file COPYING, which 
// should have been provided with this distribution.
//--------------------------------------------------------------

using System.Windows;
using System.Windows.Controls;

namespace Ptv.XServer.Demo.Tools
{
    public static class ControlTools
    {
        private static void LostFocus(TextBox textBox, string defaultText)
        {
            if (!string.IsNullOrEmpty(textBox.Text)) return;

            textBox.Text = defaultText;
            textBox.FontStyle = FontStyles.Italic;
        }

        public static void DefaultText(this TextBox textBox, string defaultText, string initialText = "")
        {
            textBox.Text = initialText;
            LostFocus(textBox, defaultText);

            textBox.LostFocus += (o, args) => LostFocus(o as TextBox, defaultText);

            textBox.GotFocus += (o, args) =>
            {
                if (textBox.Text != defaultText) return;

                textBox.Text = string.Empty;
                textBox.FontStyle = FontStyles.Normal;
            };
        }

        private static void LostFocus(ComboBox comboBox, string defaultText)
        {
            if (!string.IsNullOrEmpty(comboBox.Text)) return;

            comboBox.Text = defaultText;
            comboBox.FontStyle = FontStyles.Italic;
        }

        public static void DefaultText(this ComboBox comboBox, string defaultText, string initialText = "")
        {
            comboBox.Text = initialText;
            LostFocus(comboBox, defaultText);

            comboBox.LostFocus += (o, args) => LostFocus(o as ComboBox, defaultText);

            comboBox.GotFocus += (o, args) =>
            {
                if (comboBox.Text != defaultText) return;

                comboBox.Text = string.Empty;
                comboBox.FontStyle = FontStyles.Normal;
            };
        }
    }
}
