// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TinyJson;

namespace Ptv.XServer.Controls.Map.Layers.Xmap2
{
    internal class DataInformation : RequestBase
    {
        public DataInformation(string baseUrl, string token) : base(baseUrl, token) { }

        public IEnumerable<string> AvailableFeatureLayerThemes =>
            Response?.mapDescription?.copyright?.featureLayers?.Select(theme => theme.themeId) ?? Enumerable.Empty<string>();

        public IEnumerable<string> CopyRights(IEnumerable<string> themes)
        {
            var result = new HashSet<string>();

            var copyrightsFromResponse = Response?.mapDescription?.copyright;
            result.UnionWith(copyrightsFromResponse?.basemap ?? Enumerable.Empty<string>());
            result.UnionWith(copyrightsFromResponse?.featureLayers?.Where(theme => themes.Contains(theme.themeId)).SelectMany(theme => theme.copyright) ?? Enumerable.Empty<string>());

            return result;
        }

        private _Response response;
        private _Response Response
        {
            get
            {
                if (response != null) return response;

                var requestObject = new { }; // No sub elements are needed.
                return response = Response("/services/rs/XRuntime/experimental/getDataInformation", requestObject.ToJson()).FromJson<_Response>();
            }
        }

        [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        [SuppressMessage("ReSharper", "CollectionNeverUpdated.Local")]
        private class _Response
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
