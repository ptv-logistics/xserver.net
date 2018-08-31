// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Windows;
using System.Windows.Controls;

namespace Ptv.XServer.Controls.Map.Layers.Shapes
{
    /// <summary>
    /// <para> A MapIcon can be located with a certain anchor on the map. The anchor defines how the MapIcon should be
    /// situated in relation to its location. Set one of the values of this enumeration to the Anchor property of
    /// this class to define the behavior. </para>
    /// <para> See the <conceptualLink target="06a654f3-afbd-4f00-9c8e-36997e2a3951"/> topic for an example. </para>
    /// </summary>
    public enum LocationAnchor
    {
        /// <summary> The MapIcon is centered over the location. </summary>
        Center = 0,
        /// <summary> The MapIcon´s left upper corner is centered over the location. </summary>
        LeftTop = 1,
        /// <summary> The MapIcon´s right upper corner is centered over the location. </summary>
        RightTop = 2,
        /// <summary> The MapIcon´s right lower corner is centered over the location. </summary>
        RightBottom = 3,
        /// <summary> The MapIcon´s left lower corner is centered over the location. </summary>
        LeftBottom = 4
    }

    /// <summary>
    /// <para>
    /// This class represents a symbol on the map. Since a symbol cannot adapt its size and position according
    /// to the map scale by default, this class acts as a wrapper which extends this functionality. So it is 
    /// responsible for placing the symbol at the appropriate coordinates and change its size according to the current
    /// map scale.
    /// </para>
    /// <para>
    /// DO NOT SET DIRECTLY THE WIDTH AND THE HEIGHT OF THE MapIcon!!! SET THESE PROPERTIES DIRECTLY ON THE WRAPPED
    /// UserControl INSTEAD!!!
    /// </para>
    /// <para> See the <conceptualLink target="06a654f3-afbd-4f00-9c8e-36997e2a3951"/> topic for an example. </para>
    /// </summary>
    [Obsolete("Use ShapeCanvas dependency properties instead", false)]
    public class MapIcon : ContentControl
    {
        #region public properties

        /// <summary> Gets or sets the location of the MapIcon. </summary>
        public Point Location
        {
            get => (Point)GetValue(ShapeCanvas.LocationProperty);
            set => SetValue(ShapeCanvas.LocationProperty, value);
        }

        /// <summary> Gets or sets the anchor of the MapIcon. </summary>
        public LocationAnchor Anchor
        {
            get => (LocationAnchor)GetValue(ShapeCanvas.AnchorProperty);
            set => SetValue(ShapeCanvas.AnchorProperty, value);
        }

        /// <summary> Gets or sets the  scale factor. See <see cref="ShapeCanvas.ScaleFactorProperty"/>. </summary>
        public double ScaleFactor
        {
            get => (double)GetValue(ShapeCanvas.ScaleFactorProperty);
            set => SetValue(ShapeCanvas.ScaleFactorProperty, value);
        }
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="MapIcon"/> class for the given UserControl. </summary>
        /// <param name="content"> The UserControl to be wrapped. </param>
        public MapIcon(FrameworkElement content)
        {
            Content = content;
            Anchor = LocationAnchor.Center;
            ScaleFactor = 0;
            Width = content.Width;
            Height = content.Height;
        }
        #endregion
    }
}
