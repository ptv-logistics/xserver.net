// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Linq;
using Ptv.XServer.Controls.Map.Layers.Tiled;
using Ptv.XServer.Controls.Map.Layers.Untiled;
using Ptv.XServer.Controls.Map.TileProviders;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Net;

namespace Ptv.XServer.Controls.Map.Layers.Xmap2
{
    /// <summary>Generates layer objects based on xServer 2. </summary>
    /// <remarks>The provided layers have to be inserted into the <see cref="Map"/>'s layer management client-side.
    /// There exist two different layers to make it is possible to 'inject' additional layers between background and label layer.</remarks>
    public class LayerFactory
    {
        /// <summary>Initializes the background and foreground layer which can be configured client-side with different themes.</summary>
        /// <param name="xServerVersion">Version helper for URL specifying the root part of the URL from which a service like rendering a map can be composed.</param>
        internal LayerFactory(IXServerVersion xServerVersion)
        {
            this.xServerVersion = xServerVersion;

            DataInformation = new DataInformation(xServerVersion);
            ServerConfiguration = new ServerConfiguration(xServerVersion);
            ContentSnapshots = new ContentSnapshots(xServerVersion);

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
                RequestBuilderDelegate = (x, y, z) =>
                {
                    var uri = xServerVersion.WithServicePath("rest", "XMap") + $"/tile/{z}/{x}/{y}?";
                    if (!string.IsNullOrEmpty(MapStyle))
                        uri += $"storedProfile={MapStyle}&";
                    if (BackgroundThemes.Count > 0)
                        uri += $"layers={string.Join(",", BackgroundThemes.ToArray())}&";
                    if (!string.IsNullOrEmpty(Token))
                        uri += $"xtok={Token}&";

                    // remove last separator
                    uri = uri.Substring(0, uri.Length - 1);

                    return uri;
                }
            };

            BackgroundLayer.TiledProvider = tiledProvider;
            BackgroundLayer.IsBaseMapLayer = true; // set to the base map category -> cannot be moved on top of overlays

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
                RequestUriString = DataInformation.CompleteUrl("rs", "XMap", "renderMap"),
                XToken = Token
            };

            LabelLayer.UntiledProvider = untiledProvider;

            LabelThemes.CollectionChanged += (sender, e) =>
            {
                untiledProvider.ThemesForRendering = LabelThemes;
                untiledProvider.ThemesWithMapObjects = LabelThemes.Where(theme => FeatureLayers.AvailableThemes.Contains(theme));
                LabelLayer.Copyright = FormatCopyRight(LabelThemes);
                LabelLayer.Refresh();
            };

            PreferredRouteTypes.CollectionChanged += (sender, e) =>
            {
                untiledProvider.PreferredRouteTypes = PreferredRouteTypes;
                LabelLayer.Refresh();
            };
        }

        private string FormatCopyRight(IEnumerable<string> themes) => string.Join("|", DataInformation.CopyRights(themes).ToArray());

        private readonly IXServerVersion xServerVersion;

        /// <summary>URL specifying the root part of the URL from which a service like rendering a map can be composed. For example, 
        /// https://xserver2-europe-eu-test.cloud.ptvgroup.com
        /// can be used as a base URL providing access to a Cloud based XServer2 system. The renderMap service is composed to:
        /// https://xserver2-europe-eu-test.cloud.ptvgroup.com/services/rs/XMap/{version}/renderMap
        /// </summary>
        public string BaseUrl => xServerVersion.AdjustedUrl();

        /// <summary>For xServer internet, a token must be specified for authentication. </summary>
        public string Token => xServerVersion.Token;

        /// <summary>An <see cref="ILayer"/> object providing a tile-based (i.e. more responsive) rendering. The geographical content is defined 
        /// by the list of background themes, see <see cref="BackgroundThemes"/>. This layer object should be inserted in a <see cref="Map"/>
        /// before the <see cref="LabelLayer"/>. </summary>
        public TiledLayer BackgroundLayer { get; } = new TiledLayer("Background");

        /// <summary>Collection of themes which determine the geographical content of the <see cref="BackgroundLayer"/>,
        /// for example themes like <c>background</c> or <c>transport</c>.
        /// </summary>
        public ObservableCollection<string> BackgroundThemes { get; } = new ObservableCollection<string>();

        /// <summary>An <see cref="ILayer"/> object providing an untiled rendering, the whole map is comprised in a single bitmap. 
        /// The geographical content is defined by the list of foreground themes, see <see cref="LabelThemes"/>. 
        /// This layer object should be inserted in a <see cref="Map"/> after the <see cref="BackgroundLayer"/>. </summary>
        public UntiledLayer LabelLayer { get; } = new UntiledLayer("Labels");

        /// <summary>Collection of themes which determine the geographical content for the <see cref="LabelLayer"/>,
        /// for example themes like <c>labels</c> or <c>PTV_TruckAttributes</c>. The major intention of this layer
        /// is to avoid blurred objects like texts or traffic signs when fractional rendering is applied. Fractional rendering
        /// is provided by the <see cref="Map"/> object allowing seamless zooming. In most mapping frameworks there are only
        /// zoom levels available according the classification of tile sizes. Only in such environments a tile-based rendering is
        /// recommended. </summary>
        public ObservableCollection<string> LabelThemes { get; } = new ObservableCollection<string>();

        /// <summary>A set of network classifications of the preferred routes. Examples are <em>BK_2</em>. <summary>A
        public ObservableCollection<string> PreferredRouteTypes { get; } = new ObservableCollection<string>();

        /// <summary>Function which returns the language, used for  geographical objects in the map like names for town and streets. 
        /// The language code is defined in BCP47, for example <em>en</em>, <em>fr</em> or <em>de</em>. </summary>
        public string MapLanguage
        {
            get => ((UntiledProvider)LabelLayer.UntiledProvider).MapLanguage;
            set
            {
                if (Equals(MapLanguage, value)) return;
                ((UntiledProvider)LabelLayer.UntiledProvider).MapLanguage = value;
                LabelLayer.Refresh();
            }
        }


        /// <summary>The language used for textual messages, for example provided
        /// by the theme <em>traffic incidents</em>. The language code is defined in BCP47, 
        /// for example <em>en</em>, <em>fr</em> or <em>de</em>. </summary>
        public string UserLanguage
        {
            get => ((UntiledProvider)LabelLayer.UntiledProvider).UserLanguage;
            set
            {
                if (Equals(UserLanguage, value)) return;
                ((UntiledProvider)LabelLayer.UntiledProvider).UserLanguage = value;
                LabelLayer.Refresh();
            }
        }

        /// <summary>All available map styles configured on the corresponding xMap2 server.</summary>
        public IEnumerable<string> AvailableMapStyles => ServerConfiguration.AvailableMapStyles;

        /// <summary>Style of a map.</summary>
        public string MapStyle
        {
            get => ((UntiledProvider)LabelLayer.UntiledProvider).StoredProfile;
            set
            {
                if (Equals(((UntiledProvider)LabelLayer.UntiledProvider).StoredProfile, value)) return;

                ((UntiledProvider)LabelLayer.UntiledProvider).StoredProfile = value;
                BackgroundLayer.Refresh();
                LabelLayer.Refresh();
            }
        }

        /// <summary> Provides functionality all around Feature Layers. </summary>
        public FeatureLayers FeatureLayers { get; }

        internal DataInformation DataInformation { get; }

        internal ServerConfiguration ServerConfiguration { get; }

        internal ContentSnapshots ContentSnapshots { get; }

        /// <summary>Callback for a final adaptation of a WebRequest before it is sent to XMap2 server.</summary>
        public static Func<WebRequest, WebRequest> ModifyRequest;

        /// <summary>Callback for a reporting of errors which may occur during xServer requests.</summary>
        public static Action<WebException> ReportXServerError;
    }
}
