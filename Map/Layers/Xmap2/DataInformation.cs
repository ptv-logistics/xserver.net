// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TinyJson;

namespace Ptv.XServer.Controls.Map.Layers.Xmap2
{
    internal class DataInformation : RequestBase
    {
        public DataInformation(IXServerVersion xServerVersion) : base(xServerVersion) { }

        public IEnumerable<string> AvailableFeatureLayerThemes =>
            GetResponseObject()?.mapDescription?.copyright?.featureLayers?.Select(theme => theme.themeId) ?? Enumerable.Empty<string>();

        public IEnumerable<string> CopyRights(IEnumerable<string> themes)
        {
            var result = new HashSet<string>();

            var copyrightsFromResponse = GetResponseObject()?.mapDescription?.copyright;
            result.UnionWith(copyrightsFromResponse?.basemap ?? Enumerable.Empty<string>());
            result.UnionWith(copyrightsFromResponse?.featureLayers?.Where(theme => themes.Contains(theme.themeId)).SelectMany(theme => theme.copyright) ?? Enumerable.Empty<string>());

            return result;
        }

        private ResponseObject responseObject;
        private ResponseObject GetResponseObject()
        {
            if (responseObject != null) return responseObject;

            var requestObject = new { }; // No sub elements are needed.
            return responseObject = Response("rs", "XRuntime", "getDataInformation", requestObject.ToJson()).FromJson<ResponseObject>();
        }

        [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        [SuppressMessage("ReSharper", "CollectionNeverUpdated.Local")]
        private class ResponseObject
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
