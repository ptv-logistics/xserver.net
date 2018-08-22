using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using TinyJson;

namespace Ptv.XServer.Controls.Map.Layers.Xmap2
{
    internal class DataInformation
    {
        public DataInformation(string baseUrl, string token)
        {
            this.baseUrl = baseUrl;
            this.token = token;
        }

        private readonly string baseUrl;
        private readonly string token;

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
                try
                {
                    var requestObject = new { }; // No sub elements are needed.

                    var request = WebRequest.Create(CompleteUrl("/services/rs/XRuntime/experimental/getDataInformation"));
                    request.Method = "POST";
                    request.ContentType = "application/json";
                    request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("xtok:" + token));

                    using (var stream = request.GetRequestStream())
                    using (var writer = new StreamWriter(stream))
                    {
                        writer.Write(requestObject.ToJson());
                    }

                    using (var webResponse = request.GetResponse())
                    using (var stream = webResponse.GetResponseStream())
                        if (stream != null)
                            using (var reader = new StreamReader(stream, Encoding.UTF8))
                                response = reader.ReadToEnd().FromJson<_Response>();
                }
                catch (Exception)
                { 
                    // ignored
                }

                return response;
            }
        }

        internal string CompleteUrl(string rightUrl) => baseUrl + '/' + rightUrl.TrimStart('/');

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
