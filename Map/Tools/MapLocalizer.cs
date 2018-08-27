// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

namespace Ptv.XServer.Controls.Map.Localization
{
    /// <summary>
    /// <para>
    /// The <see cref="MapLocalizer"/> types can be used to set a custom localization for
    /// the texts used by the map control. 
    /// The <see cref="MapStringId"/> defines the string to be localized.
    /// </para>
    /// </summary>
    [System.Runtime.CompilerServices.CompilerGenerated]
    class NamespaceDoc
    {
        // NamespaceDoc is used for providing the root documentation for Ptv.Components.Projections
        // hint taken from http://stackoverflow.com/questions/156582/namespace-documentation-on-a-net-project-sandcastle
    }
    
    /// <summary>
    /// Override this class to implement a custom localizer.
    /// </summary>
    public class MapLocalizer
    {
        /// <summary>
        /// The active localizer used by the map controls.
        /// </summary>
        public static MapLocalizer Active;

        /// <summary>
        /// Gets the localizes string for a <see cref="MapStringId"/>.
        /// </summary>
        /// <param name="id">The id of the string.</param>
        /// <returns> The localized text. </returns>
        public static string GetString(MapStringId id)
        {
            return Active == null ? ResourceManagerLocalizer.GetString(id) : Active.GetLocalizedString(id);
        }

        /// <summary>
        /// Override this method to use a custom localization.
        /// </summary>
        /// <param name="id">The id of the string.</param>
        /// <returns> The localized text. </returns>
        public virtual string GetLocalizedString(MapStringId id)
        {
            return ResourceManagerLocalizer.GetString(id);
        }
    }

    /// <summary>
    /// Documentation in progress...
    /// </summary>
    public class ResourceManagerLocalizer
    {
        /// <summary>
        /// Documentation in progress...
        /// </summary>
        private static System.Resources.ResourceManager resourceMan;

        /// <summary> Gets the cached ResourceManager instance used by this class. </summary>
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Advanced)]
        public static System.Resources.ResourceManager ResourceManager
        {
            get
            {
                if (!ReferenceEquals(resourceMan, null)) return resourceMan;

                return resourceMan = new System.Resources.ResourceManager("Ptv.XServer.Controls.Map.Resources.Strings", typeof(MapLocalizer).Assembly);
            }
        }

        /// <summary>
        /// Gets the localized string for a <see cref="MapStringId"/>.
        /// </summary>
        /// <param name="id">The id of the string.</param>
        /// <returns> The localized text. </returns>
        public static string GetString(MapStringId id)
        {
            return ResourceManager.GetString(id.ToString());
        }
    }

    /// <summary>
    /// Documentation in progress...
    /// </summary>
    public enum MapStringId
    {
        /// <summary> Text for aerial layer. </summary>
        Aerials,
        /// <summary> Text for background layer. </summary>
        Background,
        /// <summary> Text for caption. </summary>
        Caption,
        /// <summary> Text for east. </summary>
        East,
        /// <summary> Text for label layer. </summary>
        Labels,
        /// <summary> Text for north. </summary>
        North,
        /// <summary> Text for options. </summary>
        Options,
        /// <summary> Text for layer selectability. </summary>
        Selectability,
        /// <summary> Text for south. </summary>
        South,
        /// <summary> Text for truck attributes layer. </summary>
        TruckAttributes,
        /// <summary> Text for layer visibility. </summary>
        Visibility,
        /// <summary> Text for south. </summary>
        West,
        /// <summary> Tool tip text for absolute speed value of traffic incidents. </summary>
        ToolTipTrafficIncidentsAbsoluteSpeed,
        /// <summary> Tool tip text for length of a traffic incident. </summary>
        ToolTipTrafficIncidentsLength,
        /// <summary> Tool tip text for delay caused by a traffic incident. </summary>
        ToolTipTrafficIncidentsDelay,
        /// <summary> Tool tip text for more detailed description of a traffic incident. </summary>
        ToolTipTrafficIncidentsMessage,
        /// <summary> Tool tip text for value of truck attribute permitted total weight. </summary>
        ToolTipTruckAttributesPermittedTotalWeight,
        /// <summary> Tool tip text for value of load type. </summary>
        ToolTipTruckAttributesLoadType,
        /// <summary> Tool tip text for value of load type passenger. </summary>
        ToolTipTruckAttributesLoadTypePassenger,
        /// <summary> Tool tip text for value of load type goods. </summary>
        ToolTipTruckAttributesLoadTypeGoods,
        /// <summary> Tool tip text for value of load type passenger. </summary>
        ToolTipTruckAttributesLoadTypeMixed,
        /// <summary> Tool tip text for value of maximal height. </summary>
        ToolTipTruckAttributesMaxHeight,
        /// <summary> Tool tip text for value of maximal weight. </summary>
        ToolTipTruckAttributesMaxWeight,
        /// <summary> Tool tip text for value of maximal width. </summary>
        ToolTipTruckAttributesMaxWidth,
        /// <summary> Tool tip text for value of maximal length. </summary>
        ToolTipTruckAttributesMaxLength,
        /// <summary> Tool tip text for value of maximal axle load. </summary>
        ToolTipTruckAttributesMaxAxleLoad,
        /// <summary> Tool tip text for not allowed for vehicles carrying goods hazardous to waters. </summary>
        ToolTipTruckAttributesHazardousToWaters,
        /// <summary> Tool tip text for not allowed for vehicles carrying hazardous goods. </summary>
        ToolTipTruckAttributesHazardousGoods,
        /// <summary> Tool tip text for not allowed for vehicles carrying combustibles. </summary>
        ToolTipTruckAttributesCombustibles,
        /// <summary> Tool tip text for delivery vehicles which are not concerned by the restriction. </summary>
        ToolTipTruckAttributesFreeForDelivery,
        /// <summary> Tool tip text for tunnel restriction code. </summary>
        ToolTipTruckAttributesTunnelRestriction,
        /// <summary> Text for transport layer </summary>
        Transport
    }
}
