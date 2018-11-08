// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using Ptv.XServer.Controls.Map.Layers.Untiled;
using System.IO;
using TinyJson;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Ptv.XServer.Controls.Map.TileProviders;
using System.Windows;

namespace Ptv.XServer.Controls.Map.Layers.Xmap2
{
    /// <summary> Loads untiled bitmaps from a given xServer 2 URL, like
    /// http://xserver-2:40000/services/rs/XMap/renderMap. 
    /// Its main purpose is to get labels from xServer 2, which shows a proper rendering of textual objects
    /// independent from the current scale.
    /// By means of tiled access some unpleasant artifacts may occur when fractional rendering is used. 
    /// With untiled rendering this issue can be avoided.</summary>
    public class UntiledProvider : IUntiledProviderWithMapObjects
    {
        /// <summary>URL of the service which provides untiled access of a map image via JSON request. </summary>
        public string RequestUriString { get; set; }

        /// <summary> xToken needed for authentication in cloud based environments.</summary>
        public string XToken { get; set; }

        /// <summary>A set of themes for which a map should be rendered. Examples are <em>labels</em>,
        /// but also Feature Layer themes like <em>Truck Attributes</em>.</summary>
        public IEnumerable<string> ThemesForRendering { get; set; }

        /// <summary>A set of themes for which map object information should be calculated during
        /// the renderMap service request. Commonly, this set is restricted to Feature Layer themes like <em>Truck Attributes</em>.</summary>
        public IEnumerable<string> ThemesWithMapObjects { get; set; }

        /// <summary>Time consideration scenario which should be used when the map is rendered and
        /// map objects are retrieved. Currently supported scenarios are
        /// <em>OptimisticTimeConsideration</em>, <em>SnapshotTimeConsideration</em> and <em>TimeSpanConsideration</em>. 
        /// For all other return values (including null string), no scenario is used and all time dependent features are not relevant.
        /// </summary>
        public string TimeConsiderationScenario { get; set; }

        /// <summary>For <em>SnapshotTimeConsideration</em> and <em>TimeSpanConsideration</em> it is necessary to define a reference
        /// time to determine which time dependent features should be active or not. This reference time comes along with the following format:
        /// <c>yyyy-MM-ddTHH:mm:ss[+-]HH:mm</c>, for example <c>2018-08-05T04:00:00+02:00</c>. </summary>
        public string ReferenceTime { get; set; }

        /// <summary>Time span (in seconds) which is added to the reference time 
        /// and needed for the <em>TimeSpanConsideration</em> scenario. </summary>
        public double? TimeSpan { get; set; }

        /// <summary>Indicator if the non-relevant features should be shown or not.</summary>
        public bool ShowOnlyRelevantByTime { get; set; }

        /// <summary>The language used for textual messages, for example provided
        /// by the theme <em>traffic incidents</em>. The language code is defined in BCP47, 
        /// for example <em>en</em>, <em>fr</em> or <em>de</em>. </summary>
        public string UserLanguage { get; set; }

        /// <summary>The language code used for geographical objects in the map like names
        /// for town and streets. The language code is defined in BCP47,
        /// for example <em>en</em>, <em>fr</em> or <em>de</em>. </summary>
        public string MapLanguage { get; set; }

        /// <summary>Profile containing the styles of a map.</summary>
        public string StoredProfile { get; set; }

        /// <summary>ID of the content snapshot.</summary>
        public string ContentSnapshotId { get; set; }

        /// <inheritdoc/>
        public Action<IEnumerable<IMapObject>, Size> Update { get; set; }

        /// <inheritdoc/>
        public Stream GetImageStream(double left, double top, double right, double bottom, int width, int height)
        {
            return GetImageStreamAndMapObjects(left, top, right, bottom, width, height, out _);
        }

        /// <inheritdoc/>
        public Stream GetImageStreamAndMapObjects(double left, double top, double right, double bottom, int width, int height,
            out IEnumerable<IMapObject> mapObjects)
        {
            var responseObject = new RequestBase.RequestBuilder(RequestUriString, XToken, GetJsonRequest(left, top, right, bottom, width, height)).Response.FromJson<ResponseObject>();

            mapObjects = responseObject?.features?.Select(feature =>
                (IMapObject)new MapObject(
                    feature.id,
                    feature.themeId,
                    new Point(feature.referencePixelPoint.x, feature.referencePixelPoint.y),
                    new Point(feature.referenceCoordinate.x, feature.referenceCoordinate.y),
                    () => feature.attributes?.Select(attribute =>
                        new KeyValuePair<string, string>(attribute.key, attribute.value))
                ));

            return responseObject?.image != null
                ? new MemoryStream(Convert.FromBase64String(responseObject.image))
                : null;
        }

        private string GetJsonRequest(double left, double top, double right, double bottom, int width, int height)
        {
            var mapRequest = new
            {
                requestProfile = new
                {
                    userLanguage = UserLanguage ?? "en",
                    mapLanguage = MapLanguage ?? "x-ptv-DFT"
                },
                storedProfile = StoredProfile,
                mapSection = new
                {
                    _type = "MapSectionByBounds",
                    bounds = new { minX = left, maxX = right, minY = top, maxY = bottom }
                },
                imageOptions = new
                {
                    width,
                    height
                },
                mapOptions = new
                {
                    contentSnapshotId = ContentSnapshotId,
                    layers = ThemesForRendering?.ToArray(),
                    timeConsideration = GetTimeConsideration(),
                    showOnlyRelevantByTime = ShowOnlyRelevantByTime
                },
                resultFields = new
                {
                    image = true,
                    featureThemeIds = ThemesWithMapObjects?.ToArray()
                },
                coordinateFormat = "EPSG:76131"
            };

            return mapRequest.ToJson();
        }

        private object GetTimeConsideration()
        {
            try
            {
                var timeConsiderationScenario = TimeConsiderationScenario;
                var referenceTime = ReferenceTime;
                switch (timeConsiderationScenario)
                {
                    case "OptimisticTimeConsideration":
                        return new
                        {
                            _type = timeConsiderationScenario
                        };

                    case "SnapshotTimeConsideration":
                        return referenceTime == null ? null : new
                        {
                            _type = timeConsiderationScenario,
                            referenceTime
                        };

                    case "TimeSpanConsideration":
                        double? timeSpan = TimeSpan;
                        return referenceTime == null || !timeSpan.HasValue ? null : new
                        {
                            _type = timeConsiderationScenario,
                            referenceTime,
                            timeSpan = timeSpan.Value
                        };

                    default: return null;
                }
            }
            catch (Exception) { return null; }
        }

        // Helper class for conversion of JSON response
        [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local")]
        [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
        [SuppressMessage("ReSharper", "CollectionNeverUpdated.Local")]
        private class ResponseObject
        {
            public string image { get; set; }
            public List<Feature> features { get; set; }

            public class Feature
            {
                public string id { get; set; }
                public ReferenceCoordinate referenceCoordinate { get; set; }
                public ReferencePixelPoint referencePixelPoint { get; set; }
                public string themeId { get; set; }
                public List<Attribute> attributes { get; set; }
            }

            public class ReferenceCoordinate
            {
                public double x { get; set; }
                public double y { get; set; }
            }

            public class ReferencePixelPoint
            {
                public int x { get; set; }
                public int y { get; set; }
            }

            public class Attribute
            {
                public string key { get; set; }
                public string value { get; set; }
            }
        }
    }
}
