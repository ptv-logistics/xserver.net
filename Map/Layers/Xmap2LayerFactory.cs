using System.Collections.Generic;
using System.Linq;
using Ptv.XServer.Controls.Map.Layers.Tiled;
using Ptv.XServer.Controls.Map.Layers.Untiled;
using Ptv.XServer.Controls.Map.TileProviders;
using Ptv.XServer.Controls.Map.UntiledProviders;
using System.Net;
using System.IO;
using TinyJson;
using System.Text;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Ptv.XServer.Controls.Map.Layers
{
    /// <summary>Helper class for generation of Layer objects based on xServer 2. /// </summary>
    /// <remarks>The provided layers have to be inserted into the <see cref="Map"/>'s layer management client-side.
    /// because it is possible that additional layers have to be placed between background and foreground layer.</remarks>
    public class Xmap2LayerFactory
    {
        /// <summary>URL specifying the root part of the URL from which a service like rendering a map can be composed. For example, 
        /// https://xserver2-europe-eu-test.cloud.ptvgroup.com
        /// can be used as a base URL providing access to a Cloud based XServer2 system. The renderMap service is composed to:
        /// https://xserver2-europe-eu-test.cloud.ptvgroup.com/services/rs/XMap/renderMap
        /// </summary>
        public string BaseUrl { get; private set; }

        /// <summary>For xServer internet, a token must be specified for authentication. </summary>
        public string Token { get; private set; }

        /// <summary>A <see cref="ILayer"/> object providing a tile-based (i.e. more responsive) rendering. The geographical content is defined 
        /// by the list of background themes, see <see cref="BackgroundThemes"/>. This layer object should be inserted in a <see cref="Map"/>
        /// before the <see cref="ForegroundLayer"/>. </summary>
        public TiledLayer BackgroundLayer { get; } = new TiledLayer("background");

        /// <summary>Collection of themes which determine the geographical content for the <see cref="BackgroundLayer"/>,
        /// for example themes like <c>background</c> or <c>transport</c>.
        /// </summary>
        public ObservableCollection<string> BackgroundThemes { get; } = new ObservableCollection<string>();

        /// <summary>A <see cref="ILayer"/> object providing an untiled rendering, the whole map is comprised in a single bitmap. 
        /// The geographical content is defined by the list of foreground themes, see <see cref="ForegroundThemes"/>. 
        /// This layer object should be inserted in a <see cref="Map"/> after the <see cref="BackgroundLayer"/>. </summary>
        public UntiledLayer ForegroundLayer { get; } = new UntiledLayer("foreground");

        /// <summary>Collection of themes which determine the geographical content for the <see cref="ForegroundLayer"/>,
        /// for example themes like <c>labels</c> or <c>PTV_TruckAttributes</c>. The major intention of this layer
        /// is to avoid blurred objects like texts or traffic signs when fractional rendering is applied. Fractional rendering
        /// is provided by the <see cref="Map"/> object allowing seemless zooming. In Javascript-based frameworks there are only
        /// zoom levels available according the classification of tile sizes. In such environments a tile-based redering is
        /// recommended. </summary>
        public ObservableCollection<string> ForegroundThemes { get; } = new ObservableCollection<string>();

        /// <summary>All available Feature Layer themes provided by the server defined via the <see cref="BaseUrl"/> property.</summary>
        /// <returns>String list of Feature Layer themes.</returns>
        public IEnumerable<string> AvailableFeatureLayerThemes =>
            getDataInformationResponseObject()?.mapDescription?.copyright?.featureLayers?.Select(theme => theme.themeId) ?? new List<string>();

        /// <summary>
        /// Constructor needed for initializing the background and foreground layer.
        /// </summary>
        /// <param name="baseUrl">URL specifying the root part of the URL from which a service like rendering a map can be composed.</param>
        /// <param name="token">String needed for authentication in context of xServer Internet environments.</param>
        public Xmap2LayerFactory(string baseUrl, string token)
        {
            BaseUrl = baseUrl.TrimEnd('/');
            Token = token;

            InitializeTiledLayer();
            InitializeUntiledLayer();
        }

        private void InitializeTiledLayer()
        {
            var tiledProvider = new RemoteTiledProvider();
            tiledProvider.MinZoom = 0;
            tiledProvider.MaxZoom = 22;
            tiledProvider.RequestBuilderDelegate = (x, y, z) => BaseUrl
                + $"/services/rest/XMap/tile/{z}/{x}/{y}"
                + $"?storedProfile=gravelpit"
                + $"&layers={string.Join(",", BackgroundThemes.ToArray())}"
                + $"&xtok={Token}";

            BackgroundLayer.TiledProvider = tiledProvider;
            BackgroundLayer.IsBaseMapLayer = true; // set to the basemap category -> cannot be moved on top of overlays

            BackgroundThemes.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) =>
            {
                BackgroundLayer.Copyright = FormatCopyRight(BackgroundThemes);
                BackgroundLayer.Refresh();
            };
        }

        private string FormatCopyRight(ObservableCollection<string> themes)
        {
            var copyrightsFromResponse = getDataInformationResponseObject()?.mapDescription?.copyright;

            var copyrightSet = new HashSet<string>();
            copyrightSet.UnionWith(copyrightsFromResponse?.basemap);
            copyrightSet.UnionWith(copyrightsFromResponse?.featureLayers?.Where(theme => themes.Contains(theme.themeId)).SelectMany(theme => theme.copyright));

            return string.Join("|", copyrightSet.ToArray());
        }

        private void InitializeUntiledLayer()
        {
            var untiledProvider = new XServer2UntiledProvider();
            untiledProvider.RequestUriString = CompleteUrl("services/rs/XMap/renderMap");
            untiledProvider.GetThemesFunc = () => ForegroundThemes;
            untiledProvider.GetFeatureLayerThemesFunc = () => ForegroundThemes.Where(theme => AvailableFeatureLayerThemes.Contains(theme));
            untiledProvider.GetXTokenFunc = () => Token;

            ForegroundLayer.UntiledProvider = untiledProvider;

            ForegroundThemes.CollectionChanged += (object sender, NotifyCollectionChangedEventArgs e) =>
            {
                ForegroundLayer.Copyright = FormatCopyRight(ForegroundThemes);
                ForegroundLayer.Refresh();
            };
        }

        private string CompleteUrl(string rightUrl) => BaseUrl + '/' + rightUrl.TrimStart('/');

        private DataInformationResponseObject dataInformationResponseObject;

        private DataInformationResponseObject getDataInformationResponseObject()
        {
            if (dataInformationResponseObject == null)
            {
                var requestObject = new { }; // No sub elements are needed.

                var request = WebRequest.Create(CompleteUrl("/services/rs/XRuntime/experimental/getDataInformation"));
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("xtok:" + Token));

                using (var stream = request.GetRequestStream())
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(requestObject.ToJson());
                }

                using (var response = request.GetResponse())
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    dataInformationResponseObject = JSONParser.FromJson<DataInformationResponseObject>(reader.ReadToEnd());
                }
            }

            return dataInformationResponseObject;
        }

        private class DataInformationResponseObject
        {
            public MapDescription mapDescription { get; set; }

            public class MapDescription
            {
                public Copyright copyright { get; set; }

                public class Copyright
                {
                    public List<string> basemap { get; set; }
                    public List<FeatureLayer> featureLayers { get; set; }

                    public class FeatureLayer
                    {
                        public string themeId { get; set; }
                        public List<string> copyright { get; set; }
                    }
                }
            }
        }
    }
}
