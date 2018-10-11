// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Windows.Media;


namespace Ptv.XServer.Controls.Map.Tools
{
    /// <summary> Spatial reference systems which can directly used for WPF map. </summary>
    public enum SpatialReference
    {
        /// <summary> PTV/Esri Spheric Mercator (SRID 53004/1076131). </summary>
        PtvMercator,
        /// <summary> PTV/Esri Spheric Mercator with inverted Y axis. </summary>
        PtvMercatorInvertedY,
        /// <summary> Google/MS Spheric Mercator (SRID 3857/900913). </summary>
        WebMercator,
        /// <summary> Google/MS Spheric Mercator with inverted Y axis. </summary>
        WebMercatorInvertedY
    }

    /// <summary>
    /// The TransformFactory creates WPF transformations which can be used 
    /// as RenderTransform for child canvases of the GeoCanvas object.
    /// </summary>
    public class TransformFactory
    {
        /// <summary> Returns a Transform object for a spatial reference. </summary>
        /// <param name="reference"> The spatial reference. </param>
        /// <returns> The resulting render transform. </returns>
        public static Transform CreateTransform(SpatialReference reference)
        {
            switch (reference)
            {
                case SpatialReference.PtvMercator:
                    {
                        const double EARTH_RADIUS = 6371000.0;
                        double mercatorSize = EARTH_RADIUS * 2.0 * Math.PI;

                        var translateTransform = new TranslateTransform(MapView.ReferenceSize / 2, MapView.ReferenceSize / 2);
                        var zoomTransform = new ScaleTransform(MapView.ZoomAdjust * MapView.ReferenceSize / mercatorSize, -MapView.ZoomAdjust * MapView.ReferenceSize / mercatorSize, MapView.ReferenceSize / 2, MapView.ReferenceSize / 2);
                        var transformGroup = new TransformGroup();
                        transformGroup.Children.Add(translateTransform);
                        transformGroup.Children.Add(zoomTransform);

                        zoomTransform.Freeze();
                        translateTransform.Freeze();
                        transformGroup.Freeze();
  
                        return transformGroup;
                    }
                case SpatialReference.PtvMercatorInvertedY:
                    {
                        const double EARTH_RADIUS = 6371000.0;
                        double mercatorSize = EARTH_RADIUS * 2.0 * Math.PI;
                        
                        var translateTransform = new TranslateTransform(MapView.ReferenceSize / 2, MapView.ReferenceSize / 2);
                        var zoomTransform = new ScaleTransform(MapView.ZoomAdjust * MapView.ReferenceSize / mercatorSize, MapView.ZoomAdjust * MapView.ReferenceSize / mercatorSize, MapView.ReferenceSize / 2, MapView.ReferenceSize / 2);
                        var transformGroup = new TransformGroup();
                        transformGroup.Children.Add(translateTransform);
                        transformGroup.Children.Add(zoomTransform);

                        zoomTransform.Freeze();
                        translateTransform.Freeze();
                        transformGroup.Freeze();

                        return transformGroup;
                    }
                case SpatialReference.WebMercator:
                    {
                        const double EARTH_RADIUS = 6378137.0;
                        double mercatorSize = EARTH_RADIUS * 2.0 * Math.PI;

                        var translateTransform = new TranslateTransform(MapView.ReferenceSize / 2, MapView.ReferenceSize / 2);
                        var zoomTransform = new ScaleTransform(MapView.ZoomAdjust * MapView.ReferenceSize / mercatorSize, -MapView.ZoomAdjust * MapView.ReferenceSize / mercatorSize, MapView.ReferenceSize / 2, MapView.ReferenceSize / 2);
                        var transformGroup = new TransformGroup();
                        transformGroup.Children.Add(translateTransform);
                        transformGroup.Children.Add(zoomTransform);

                        zoomTransform.Freeze();
                        translateTransform.Freeze();
                        transformGroup.Freeze();

                        return transformGroup;
                    }
                case SpatialReference.WebMercatorInvertedY:
                    {
                        const double EARTH_RADIUS = 6378137.0;
                        double mercatorSize = EARTH_RADIUS * 2.0 * Math.PI;

                        var translateTransform = new TranslateTransform(MapView.ReferenceSize / 2, MapView.ReferenceSize / 2);
                        var zoomTransform = new ScaleTransform(MapView.ZoomAdjust * MapView.ReferenceSize / mercatorSize, MapView.ZoomAdjust * MapView.ReferenceSize / mercatorSize, MapView.ReferenceSize / 2, MapView.ReferenceSize / 2);
                        var transformGroup = new TransformGroup();
                        transformGroup.Children.Add(translateTransform);
                        transformGroup.Children.Add(zoomTransform);

                        zoomTransform.Freeze();
                        translateTransform.Freeze();
                        transformGroup.Freeze();

                        return transformGroup;
                    }
                default:
                    throw new ArgumentException("not supported");
            }
        }
    }
}
