// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System.Collections.Generic;

namespace Ptv.XServer.Controls.Map.Layers.Xmap2
{
    /// <summary>Concentrates functionality around Feature Layers in XMap2 environment, especially the installed
    /// <a href="https://xserver2-dashboard.cloud.ptvgroup.com/dashboard/Default.htm#TechnicalConcepts/FeatureLayer/DSC_About_FeatureLayer.htm%3FTocPath%3DTechnical%2520Concepts%7CFeature%2520Layer%7C_____2">Feature Layer themes</a>
    /// and all parameters related to
    /// <a href="https://xserver2-dashboard.cloud.ptvgroup.com/dashboard/Default.htm#API-Documentation/timeconsideration.html%3FTocPath%3DAPI%2520Documentation%7Ccommon%7Ctimeconsideration%7C_____0">time consideration</a>.
    /// </summary>
    public class FeatureLayers
    {
        /// <summary>Initializes the new instance with a reference to the possessing layer factory.</summary>
        /// <param name="owner">Owner of the new object.</param>
        public FeatureLayers(LayerFactory owner)
        {
            layerFactory = owner;
        }

        private readonly LayerFactory layerFactory;

        /// <summary>Provides all available Feature Layer themes installed on the server defined by the <see cref="LayerFactory.BaseUrl"/> property.</summary>
        /// <returns>String list of all available Feature Layer themes.</returns>
        public IEnumerable<string> AvailableThemes => layerFactory.DataInformation.AvailableFeatureLayerThemes;

        /// <summary>Time consideration scenario which should be used when the map is rendered and
        /// map objects are retrieved. Currently supported scenarios are
        /// <em>OptimisticTimeConsideration</em>, <em>SnapshotTimeConsideration</em> and <em>TimeSpanConsideration</em>. 
        /// For all other return values (including null string), no scenario is used and all time dependent features are not relevant.
        /// </summary>
        public string TimeConsiderationScenario
        {
            get => ((UntiledProvider) layerFactory.LabelLayer.UntiledProvider).TimeConsiderationScenario;
            set
            {
                if (Equals(TimeConsiderationScenario, value)) return;
                ((UntiledProvider)layerFactory.LabelLayer.UntiledProvider).TimeConsiderationScenario = value;
                layerFactory.LabelLayer.Refresh();
            }
        }

        /// <summary>For <em>SnapshotTimeConsideration</em> and <em>TimeSpanConsideration</em> it is necessary to define a reference
        /// time to determine which time dependent features should be active or not. The reference time
        /// including a time zone comes along in the following format:
        /// <c>yyyy-MM-ddTHH:mm:ss[+-]HH:mm</c>, for example <c>2018-08-05T04:00:00+02:00</c>. </summary>
        public string ReferenceTime
        {
            get => ((UntiledProvider) layerFactory.LabelLayer.UntiledProvider).ReferenceTime;
            set
            {
                if (Equals(ReferenceTime, value)) return;
                ((UntiledProvider)layerFactory.LabelLayer.UntiledProvider).ReferenceTime = value;
                layerFactory.LabelLayer.Refresh();
            }
        }

        /// <summary>Time span (in seconds) which is added to the reference time 
        /// and needed for the <em>TimeSpanConsideration</em> scenario. </summary>
        public double? TimeSpan
        {
            get => ((UntiledProvider) layerFactory.LabelLayer.UntiledProvider).TimeSpan;
            set
            {
                if (Equals(TimeSpan, value)) return;
                ((UntiledProvider)layerFactory.LabelLayer.UntiledProvider).TimeSpan = value;
                layerFactory.LabelLayer.Refresh();
            }
        }

        /// <summary>Function which indicates if the non-relevant Features should be shown or not.</summary>
        public bool ShowOnlyRelevantByTime
        {
            get => ((UntiledProvider) layerFactory.LabelLayer.UntiledProvider).ShowOnlyRelevantByTime;
            set
            {
                if (Equals(ShowOnlyRelevantByTime, value)) return;
                ((UntiledProvider) layerFactory.LabelLayer.UntiledProvider).ShowOnlyRelevantByTime = value;
                layerFactory.LabelLayer.Refresh();
            }
        }

        /// <summary>Provides all available content snapshots configured on the server defined by the <see cref="LayerFactory.BaseUrl"/> property.</summary>
        /// <returns>Description list of all available content snapshots.</returns>
        public IEnumerable<ContentSnapshotDescription> AvailableContentSnapshots => layerFactory.ContentSnapshots.Available;

        /// <summary>ID of the content snapshot.</summary>
        public string ContentSnapshotId
        {
            get => ((UntiledProvider)layerFactory.LabelLayer.UntiledProvider).ContentSnapshotId;
            set
            {
                if (Equals(((UntiledProvider)layerFactory.LabelLayer.UntiledProvider).ContentSnapshotId, value)) return;

                ((UntiledProvider)layerFactory.LabelLayer.UntiledProvider).ContentSnapshotId = value;
                layerFactory.LabelLayer.Refresh();
            }
        }
    }
}
