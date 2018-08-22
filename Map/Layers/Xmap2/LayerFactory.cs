using System.Linq;
using Ptv.XServer.Controls.Map.Layers.Tiled;
using Ptv.XServer.Controls.Map.Layers.Untiled;
using Ptv.XServer.Controls.Map.TileProviders;
using System.Collections.ObjectModel;

namespace Ptv.XServer.Controls.Map.Layers.Xmap2
{
    /// <summary>Generates layer objects based on xServer 2. </summary>
    /// <remarks>The provided layers have to be inserted into the <see cref="Map"/>'s layer management client-side.
    /// There exist two different layers to make it is possible to 'inject' additional layers between background and foreground layer.</remarks>
    public class LayerFactory
    {
        /// <summary>Initializes the background and foreground layer which can be configured client-side with different themes.</summary>
        /// <param name="baseUrl">URL specifying the root part of the URL from which a service like rendering a map can be composed.</param>
        /// <param name="token">String needed for authentication in context of xServer Internet environments.</param>
        public LayerFactory(string baseUrl, string token)
        {
            BaseUrl = baseUrl.TrimEnd('/');
            Token = (token?.Contains(":") ?? false) ? token.Split(':')[1] : token;

            DataInformation = new DataInformation(BaseUrl, Token);

            InitializeTiledLayer();
            InitializeUntiledLayer();

            FeatureLayers = new FeatureLayers(this);
        }

        private void InitializeTiledLayer()
        {
            var tiledProvider = new RemoteTiledProvider
            {
                MinZoom = 0,
                MaxZoom = 22,
                RequestBuilderDelegate = (x, y, z) => BaseUrl
                                                  + $"/services/rest/XMap/tile/{z}/{x}/{y}"
                                                  + "?storedProfile=gravelpit"
                                                  + $"&layers={string.Join(",", BackgroundThemes.ToArray())}"
                                                  + $"&xtok={Token}"
            };

            BackgroundLayer.TiledProvider = tiledProvider;
            BackgroundLayer.IsBaseMapLayer = true; // set to the basemap category -> cannot be moved on top of overlays

            BackgroundThemes.CollectionChanged += (sender, e) =>
            {
                BackgroundLayer.Copyright = FormatCopyRight(BackgroundThemes);
                BackgroundLayer.Refresh();
            };
        }

        private void InitializeUntiledLayer()
        {
            var untiledProvider = new UntiledProvider
            {
                RequestUriString = DataInformation.CompleteUrl("services/rs/XMap/renderMap"),
                XTokenFunc = () => Token,
                ThemesForRenderingFunc = () => ForegroundThemes,
                ThemesWithMapObjectsFunc = () => ForegroundThemes.Where(theme => FeatureLayers.AvailableThemes.Contains(theme))
            };

            ForegroundLayer.UntiledProvider = untiledProvider;

            ForegroundThemes.CollectionChanged += (sender, e) =>
            {
                ForegroundLayer.Copyright = FormatCopyRight(ForegroundThemes);
                ForegroundLayer.Refresh();
            };
        }

        private string FormatCopyRight(ObservableCollection<string> themes) => string.Join("|", DataInformation.CopyRights(themes).ToArray());

        /// <summary>URL specifying the root part of the URL from which a service like rendering a map can be composed. For example, 
        /// https://xserver2-europe-eu-test.cloud.ptvgroup.com
        /// can be used as a base URL providing access to a Cloud based XServer2 system. The renderMap service is composed to:
        /// https://xserver2-europe-eu-test.cloud.ptvgroup.com/services/rs/XMap/renderMap
        /// </summary>
        public string BaseUrl { get; }

        /// <summary>For xServer internet, a token must be specified for authentication. </summary>
        public string Token { get; }

        /// <summary>An <see cref="ILayer"/> object providing a tile-based (i.e. more responsive) rendering. The geographical content is defined 
        /// by the list of background themes, see <see cref="BackgroundThemes"/>. This layer object should be inserted in a <see cref="Map"/>
        /// before the <see cref="ForegroundLayer"/>. </summary>
        public TiledLayer BackgroundLayer { get; } = new TiledLayer("background");

        /// <summary>Collection of themes which determine the geographical content of the <see cref="BackgroundLayer"/>,
        /// for example themes like <c>background</c> or <c>transport</c>.
        /// </summary>
        public ObservableCollection<string> BackgroundThemes { get; } = new ObservableCollection<string>();

        /// <summary>An <see cref="ILayer"/> object providing an untiled rendering, the whole map is comprised in a single bitmap. 
        /// The geographical content is defined by the list of foreground themes, see <see cref="ForegroundThemes"/>. 
        /// This layer object should be inserted in a <see cref="Map"/> after the <see cref="BackgroundLayer"/>. </summary>
        public UntiledLayer ForegroundLayer { get; } = new UntiledLayer("foreground");

        /// <summary>Collection of themes which determine the geographical content for the <see cref="ForegroundLayer"/>,
        /// for example themes like <c>labels</c> or <c>PTV_TruckAttributes</c>. The major intention of this layer
        /// is to avoid blurred objects like texts or traffic signs when fractional rendering is applied. Fractional rendering
        /// is provided by the <see cref="Map"/> object allowing seemless zooming. In most mapping frameworks there are only
        /// zoom levels available according the classification of tile sizes. Only in such environments a tile-based redering is
        /// recommended. </summary>
        public ObservableCollection<string> ForegroundThemes { get; } = new ObservableCollection<string>();

        /// <summary> Provides functionality all around Feature Layers. </summary>
        public FeatureLayers FeatureLayers { get; }

        internal DataInformation DataInformation { get; }
    }
}
