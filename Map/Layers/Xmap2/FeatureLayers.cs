// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
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

        /// <summary>Function which returns the time consideration scenario which should be used when the map is rendered and
        /// map objects are retrieved. Currently supported scenarios are
        /// <em>OptimisticTimeConsideration</em>, <em>SnapshotTimeConsideration</em> and <em>TimeSpanConsideration</em>. 
        /// For all other return values (including null string), no scenario is used and all time dependent features are not relevant.
        /// </summary>
        public Func<string> TimeConsiderationScenarioFunc
        {
            get => ((UntiledProvider) layerFactory.ForegroundLayer.UntiledProvider).TimeConsiderationScenarioFunc;
            set => ((UntiledProvider) layerFactory.ForegroundLayer.UntiledProvider).TimeConsiderationScenarioFunc = value;
        }

        /// <summary>For <em>SnapshotTimeConsideration</em> and <em>TimeSpanConsideration</em> it is necessary to define a reference
        /// time to determine which time dependent features should be active or not. The function returns a reference time
        /// including a time zone in the following format:
        /// <c>yyyy-MM-ddTHH:mm:ss[+-]HH:mm</c>, for example <c>2018-08-05T04:00:00+02:00</c>. </summary>
        public Func<string> ReferenceTimeFunc
        {
            get => ((UntiledProvider) layerFactory.ForegroundLayer.UntiledProvider).ReferenceTimeFunc;
            set => ((UntiledProvider) layerFactory.ForegroundLayer.UntiledProvider).ReferenceTimeFunc = value;
        }

        /// <summary>Function which defines the time span (in seconds) which is added to the reference time 
        /// and needed for the <em>TimeSpanConsideration</em> scenario. </summary>
        public Func<double> TimeSpanFunc
        {
            get => ((UntiledProvider) layerFactory.ForegroundLayer.UntiledProvider).TimeSpanFunc;
            set => ((UntiledProvider) layerFactory.ForegroundLayer.UntiledProvider).TimeSpanFunc = value;
        }

        /// <summary>Function which indicates if the non-relevant Features should be shown or not.</summary>
        public Func<bool> ShowOnlyRelevantByTimeFunc
        {
            get => ((UntiledProvider) layerFactory.ForegroundLayer.UntiledProvider).ShowOnlyRelevantByTimeFunc;
            set => ((UntiledProvider) layerFactory.ForegroundLayer.UntiledProvider).ShowOnlyRelevantByTimeFunc = value;
        }

        /// <summary>Function which returns the language, used for textual messages provided
        /// by the theme <em>traffic incidents</em>. The language code is defined in BCP47, 
        /// for example <em>en</em>, <em>fr</em> or <em>de</em>. </summary>
        public Func<string> LanguageFunc
        {
            get => ((UntiledProvider) layerFactory.ForegroundLayer.UntiledProvider).UserLanguageFunc;
            set => ((UntiledProvider) layerFactory.ForegroundLayer.UntiledProvider).UserLanguageFunc = value;
        }
    }
}
