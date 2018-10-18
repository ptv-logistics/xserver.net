// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using TinyJson;

namespace Ptv.XServer.Controls.Map.Layers.Xmap2
{
    internal class ContentSnapshots : RequestBase
    {
        public ContentSnapshots(string baseUrl, string token) : base(baseUrl, token) {}

        public IEnumerable<ContentSnapshotDescription> Available =>
            GetResponseObject()?.contentSnapshotInformation?.Select(singleInformation => singleInformation.contentSnapshotDescription) ?? Enumerable.Empty<ContentSnapshotDescription>();

        private ResponseObject GetResponseObject()
        {
            var requestObject = new { }; // No sub elements are needed.
            return Response("/services/rs/XData/listContentSnapshots", requestObject.ToJson()).FromJson<ResponseObject>();
        }

        [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        [SuppressMessage("ReSharper", "CollectionNeverUpdated.Local")]
        private class ResponseObject
        {
            public List<ContentSnapshotInformation> contentSnapshotInformation { get; set; }

            public class ContentSnapshotInformation
            {
                public ContentSnapshotDescription contentSnapshotDescription { get; set; }
            }
        }
    }

    /// <summary>Relevant information concerning a content snapshot. </summary>
    public class ContentSnapshotDescription
    {
        /// <summary>Unique ID for addressing the corresponding snapshot. </summary>
        public string id { get; set; }

        /// <summary>Human readable description of a snapshot.</summary>
        public string label { get; set; }
    }
}
