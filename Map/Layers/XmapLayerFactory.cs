using System;
using Ptv.XServer.Controls.Map.Tools;
using Ptv.XServer.Controls.Map.TileProviders;
using Ptv.XServer.Controls.Map.Localization;
using Ptv.XServer.Controls.Map.Layers.Untiled;
using Ptv.XServer.Controls.Map.Layers.Tiled;

namespace Ptv.XServer.Controls.Map.Layers
{
    /// <summary>
    /// This helper class implements extension methods to initialize the map 
    /// with xMapServer base layers, RoadEditor and POI layers available from XMapServer.
    /// </summary>
    public static class XMapLayerFactory
    {
        #region public methods

        /// <summary> The name of the layer which successes the base layer. </summary>
        private static string BaseLayerSuccessor;
        /// <summary> The name of the layer which precedes the base layer. </summary>
        private static string LabelLayerPredecessor;

        private const string BackgroundLayerName = "Background";
        private const string LabelsLayerName = "Labels";

        /// <summary>
        /// Inserts the xMapServer base layers, i.e. the background layers for areas like forests, rivers, population areas, et al,
        /// and their corresponding labels.
        /// </summary>
        /// <param name="layers">The LayerCollection instance, used as an extension. </param>
        /// <param name="meta">Meta information for xMapServer, further details can be seen in the <see cref="XMapMetaInfo"/> description. </param>
        public static void InsertXMapBaseLayers(this LayerCollection layers, XMapMetaInfo meta)
        {
            var baseLayer = new TiledLayer(BackgroundLayerName)
            {
                TiledProvider = new XMapTiledProvider(meta.Url, meta.User, meta.Password, XMapMode.Background),
                Copyright = meta.CopyrightText,
                Caption = MapLocalizer.GetString(MapStringId.Background),
                IsBaseMapLayer = true,
                Icon = ResourceHelper.LoadBitmapFromResource("Ptv.XServer.Controls.Map;component/Resources/Background.png"),
            };

            if (BaseLayerSuccessor != null && layers[BaseLayerSuccessor] != null)
            {
                layers.Insert(layers.IndexOf(layers[BaseLayerSuccessor]), baseLayer);
            }
            else
            {
                // add tile layer
                layers.Add(baseLayer);
                BaseLayerSuccessor = null;
            }

            // don't add overlay layer for decarta-powered maps (basemap is completely rendered on tiles)
            if (XServerUrl.IsDecartaBackend(meta.Url)) return;

            var labelLayer = new UntiledLayer(LabelsLayerName)
            {
                UntiledProvider = new XMapTiledProvider(meta.Url, meta.User, meta.Password, XMapMode.Town),
                MaxRequestSize = meta.MaxRequestSize,
                Caption = MapLocalizer.GetString(MapStringId.Labels),
                Icon = ResourceHelper.LoadBitmapFromResource("Ptv.XServer.Controls.Map;component/Resources/Labels.png"),
            };

            if (LabelLayerPredecessor != null && layers[LabelLayerPredecessor] != null && layers.IndexOf(layers[LabelLayerPredecessor]) < layers.Count)
            {
                layers.Insert(layers.IndexOf(layers[LabelLayerPredecessor]) + 1, labelLayer);
            }
            else
            {
                // add label layer
                layers.Add(labelLayer);
                LabelLayerPredecessor = null;
            }
        }


        /// <summary>
        /// Updates the copyright text for the XMapServer layers.
        /// </summary>
        /// <param name="layers"> The LayerCollection instance. </param>
        /// <param name="copyrightText"> The new copyight text</param>
        public static void UpdateXMapCoprightText(this LayerCollection layers, string copyrightText)
        {
            if (layers[BackgroundLayerName] != null)
                (layers[BackgroundLayerName] as TiledLayer).Copyright = copyrightText;
            if (layers[LabelsLayerName] != null)
                (layers[LabelsLayerName] as UntiledLayer).Copyright = copyrightText;
        }

        /// <summary>
        /// Updates the maximum zoom for the XMapServer layers.
        /// </summary>
        /// <param name="layers"> The LayerCollection instance. </param>
        /// <param name="maxZoom">Maximum zoom value to set. See remarks.</param>
        /// <param name="maxZoomLabels">Maximum zoom for the label layer. See remarks.</param>
        /// <remarks>Update the maximum zoom for the background and the label layer. If maxZoomLabels is unspecified, 
        /// maxZoom will be used for both layers. If maxZoomLabels is specified, maxZoom determines the value for
        /// the background and maxZoomLabels the value for the label layer.</remarks>
        public static void UpdateXMapMaxZoom(this LayerCollection layers, int maxZoom, int? maxZoomLabels = null)
        {
            // helper that tries to cast a provider object to 
            // XMapTiledProvider in order to set the maximum zoom value.
            Action<object, int> setMaxZoom = (provider, value) =>
            {
                var tiledProvider = provider as XMapTiledProvider;
                if (tiledProvider != null)
                    tiledProvider.MaxZoom = value;
            };

            setMaxZoom((layers[BackgroundLayerName] as TiledLayer)?.TiledProvider, maxZoom);
            setMaxZoom((layers[LabelsLayerName] as UntiledLayer)?.UntiledProvider, maxZoomLabels.GetValueOrDefault(maxZoom));
        }

        /// <summary>
        /// Updates the minimum zoom for the XMapServer layers.
        /// </summary>
        /// <param name="layers"> The LayerCollection instance. </param>
        /// <param name="minZoom">Minimum zoom value to set. See remarks.</param>
        /// <param name="minZoomLabels">Minimum zoom for the label layer. See remarks.</param>
        /// <remarks>Update the minimum zoom for the background and the label layer. If minZoomLabels is unspecified, 
        /// minZoom will be used for both layers. If minZoomLabels is specified, minZoom determines the value for
        /// the background and minZoomLabels the value for the label layer.</remarks>
        public static void UpdateXMapMinZoom(this LayerCollection layers, int minZoom, int? minZoomLabels = null)
        {
            // helper that tries to cast a provider object to 
            // XMapTiledProvider in order to set the minimum zoom value.
            Action<object, int> setMinZoom = (provider, value) =>
            {
                var tiledProvider = provider as XMapTiledProvider;
                if (tiledProvider != null)
                    tiledProvider.MinZoom = value;
            };

            setMinZoom((layers[BackgroundLayerName] as TiledLayer)?.TiledProvider, minZoom);
            setMinZoom((layers[LabelsLayerName] as UntiledLayer)?.UntiledProvider, minZoomLabels.GetValueOrDefault(minZoom));
        }

        /// <summary>
        /// Updates the profiles to be used in xMap Server requests.
        /// </summary>
        /// <param name="layers"> The LayerCollection instance. </param>
        /// <param name="profileBackground">The profile to be used in the background layer.</param>
        /// <param name="profileLabels">The profile to be used in the label layer.</param>
        public static void UpdateXMapProfiles(this LayerCollection layers, string profileBackground, string profileLabels)
        {
            // helper that tries to cast a provider object to 
            // XMapTiledProvider in order to set the custom profile.
            Action<object, string> setProfile = (provider, value) =>
            {
                var tiledProvider = provider as XMapTiledProvider;
                if (tiledProvider != null)
                    tiledProvider.CustomProfile = value;
            };

            setProfile((layers[BackgroundLayerName] as TiledLayer)?.TiledProvider, profileBackground);
            setProfile((layers[LabelsLayerName] as UntiledLayer)?.UntiledProvider, profileLabels);
        }


        /// <summary>
        /// Removes all xMapServer base layers.
        /// </summary>
        /// <param name="layers"> The LayerCollection instance. </param>
        public static void RemoveXMapBaseLayers(this LayerCollection layers)
        {
            var idx = layers.IndexOf(layers[BackgroundLayerName]);
            BaseLayerSuccessor = (layers.Count > idx + 1) ? layers[idx + 1].Name : null;

            idx = layers.IndexOf(layers[LabelsLayerName]);
            LabelLayerPredecessor = (idx > 0) ? layers[idx - 1].Name : null;

            layers.Remove(layers[BackgroundLayerName]);
            layers.Remove(layers[LabelsLayerName]);
        }
        #endregion
    }
}
