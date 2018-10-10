// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TinyJson;

namespace Ptv.XServer.Controls.Map.Layers.Xmap2
{
    internal class ServerConfiguration : RequestBase
    {
        public ServerConfiguration(string baseUrl, string token) : base(baseUrl, token) {}

        public IEnumerable<string> AvailableMapStyles => Response?.profiles?
                                                             .Where(profile => profile?.useCases?.Contains("mapping") ?? false)
                                                             .Select(profile => profile.name) 
                                                         ?? Enumerable.Empty<string>();

        private _Response response;
        private _Response Response
        {
            get
            {
                if (response != null) return response;

                var requestObject = new
                {
                    resultFields = new { profiles = true }
                };

                return response = Response("/services/rs/XRuntime/experimental/getServerConfiguration", requestObject.ToJson()).FromJson<_Response>();
            }
        }

        [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        [SuppressMessage("ReSharper", "CollectionNeverUpdated.Local")]
        private class _Response
        {
            public List<ProfileDescription> profiles { get; set; }

            public class ProfileDescription
            {
                public string name { get; set; }
                public List<string> useCases { get; set; }
            }
        }
    }
}