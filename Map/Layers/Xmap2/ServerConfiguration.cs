// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TinyJson;

namespace Ptv.XServer.Controls.Map.Layers.Xmap2
{
    internal class ServerConfiguration : RequestBase
    {
        public ServerConfiguration(IXServerVersion xServerVersion) : base(xServerVersion) { }

        public IEnumerable<string> AvailableMapStyles => GetResponseObject()?.profiles?
                                                             .Where(profile => (profile?.useCases?.Contains("mapping") ?? false) 
                                                                               || (profile?.useCases?.Contains("rendering") ?? false))
                                                             .Select(profile => profile.name) 
                                                         ?? Enumerable.Empty<string>();

        private ResponseObject response;
        private ResponseObject GetResponseObject()
        {
            if (response != null) return response;

            var requestObject = new
            {
                resultFields = new { profiles = true }
            };

            return response = Response("rs", "XRuntime", "getServerConfiguration", requestObject.ToJson()).FromJson<ResponseObject>();
        }

        [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        [SuppressMessage("ReSharper", "CollectionNeverUpdated.Local")]
        private class ResponseObject
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