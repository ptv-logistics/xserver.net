//--------------------------------------------------------------
// Copyright (c) PTV Group
// 
// For license details, please refer to the file COPYING, which 
// should have been provided with this distribution.
//--------------------------------------------------------------

using System.Windows;
using Ptv.XServer.Controls.Map.Canvases;
using Ptv.XServer.Controls.Map.Layers;
using Ptv.XServer.Demo.GeoRSS;

namespace Ptv.XServer.Demo.UseCases.GeoRss
{
    /// <summary>
    /// Provides the functionality to add a GeoRSS Layer to a WpfMap.
    /// </summary>
    public class GeoRssUseCase : UseCase
    {
        /// <summary>
        // Creates the GeoRss layer and sets the view of the WpfMap to global settings.
        /// </summary>
        protected override void Enable()
        {
            if (wpfMap.Layers.Contains(wpfMap.Layers["GeoRss"]))
                return;

            #region doc:geoRSS
            var geoRss = new BaseLayer("GeoRss")
            {
                CanvasCategories = new[] { CanvasCategory.Content },
                CanvasFactories = new BaseLayer.CanvasFactoryDelegate[] { map => new GeoRssCanvas(map) }
            };

            // Adds the BaseLayer for GeoRss to the WpfMap.
            wpfMap.Layers.Add(geoRss);
            #endregion

            wpfMap.SetMapLocation(new Point(0, 0), 1);
        }

        /// <summary>
        /// Deletes the GeoRss layer from the WpfMap.
        /// </summary>
        protected override void Disable()
        {
            wpfMap.Layers.Remove(wpfMap.Layers["GeoRss"]);
        }
    }
}
