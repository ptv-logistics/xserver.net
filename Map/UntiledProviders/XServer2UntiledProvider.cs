using Ptv.XServer.Controls.Map.Layers.Untiled;
using System.Text;
using System.IO;
using System.Net;
using TinyJson;
using System;
using System.Collections.Generic;
using System.Linq;
using Ptv.XServer.Controls.Map.TileProviders;
using System.Windows;

namespace Ptv.XServer.Controls.Map.UntiledProviders
{
    public interface IXmap2ObjectInfos
    {
        Action<IEnumerable<IMapObject>, Size> Update { get; set; }
    }

    /// <summary> Provider loading untiled bitmaps from a given URL, like
    /// http://xserver-2:40000/services/rs/XMap/renderMap. 
    /// Its main purpose is to get labels from xServer 2, which shows a proper rendering of textual objects.
    /// By means of tiled access some unpleasant artifacts occur when fractional rendering is used. 
    /// With untiled rendering this issue can be avoided.</summary>
    public class XServer2UntiledProvider : IUntiledProvider, IXmap2ObjectInfos
    {
        /// <summary>URL of the service which provides untiled access of a map image via JSON request. </summary>
        public string RequestUriString { get; set; }

        public Func<string> GetXTokenFunc { get; set; }
        public Func<IEnumerable<string>> GetThemesFunc { get; set; }
        public Func<IEnumerable<string>> GetFeatureLayerThemesFunc { get; set; }

        public Action<IEnumerable<IMapObject>, Size> Update { get; set; }

        /// <inheritdoc/>
        public Stream GetImageStream(double left, double top, double right, double bottom, int width, int height)
        {
            var request = WebRequest.Create(RequestUriString);
            request.Method = "POST";
            request.ContentType = "application/json";

            string xToken = GetXTokenFunc?.Invoke();
            if (xToken != null)
                request.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("xtok:" + xToken));

            using (var stream = request.GetRequestStream())
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(getJsonRequest(left, top, right, bottom, width, height));
            }

            using (var response = request.GetResponse())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream, Encoding.UTF8))
            {
                var responseObject = JSONParser.FromJson<ResponseObject>(reader.ReadToEnd());

                IEnumerable<IMapObject> mapObjects = responseObject?.features?.Select(feature => (IMapObject) new MapObject(
                    feature.id,
                    feature.themeId,
                    new Point(feature.referencePixelPoint.x, feature.referencePixelPoint.y),
                    new Point(feature.referenceCoordinate.x, feature.referenceCoordinate.y),
                    () => feature?.attributes?.Select(attribute => new KeyValuePair<string, string>(attribute.key, attribute.value))
                ));
                Update?.Invoke(mapObjects, new Size(width, height));

                return (responseObject?.image != null)
                    ? new MemoryStream(System.Convert.FromBase64String(responseObject.image)) 
                    : null;
            }
        }

        private string getJsonRequest(double left, double top, double right, double bottom, int width, int height)
        {
            var mapRequest = new
            {
                mapSection = new
                {
                    _type = "MapSectionByBounds",
                    bounds = new { minX = left, maxX = right, minY = top, maxY = bottom }
                },
                imageOptions = new { width = width, height = height },
                mapOptions = new { layers = GetThemesFunc?.Invoke().ToArray() },
                resultFields = new { image = true, featureThemeIds = GetFeatureLayerThemesFunc?.Invoke().ToArray() },
                coordinateFormat = "EPSG:76131"
            };

            return mapRequest.ToJson();
        }

        // Helper class for conversion of JSON response
        private class ResponseObject
        {
            public string image { get; set; }
            public List<Feature> features { get; set; }
        }

        private class Feature
        {
            public string id { get; set; }
            public ReferenceCoordinate referenceCoordinate { get; set; }
            public ReferencePixelPoint referencePixelPoint { get; set; }
            public PixelBoundingBox pixelBoundingBox { get; set; }
            public string themeId { get; set; }
            public List<Attribute> attributes { get; set; }
        }

        private class ReferenceCoordinate
        {
            public double x { get; set; }
            public double y { get; set; }
        }

        private class ReferencePixelPoint
        {
            public int x { get; set; }
            public int y { get; set; }
        }

        private class PixelBoundingBox
        {
            public int left { get; set; }
            public int top { get; set; }
            public int right { get; set; }
            public int bottom { get; set; }
        }

        private class Attribute
        {
            public string key { get; set; }
            public string value { get; set; }
        }
    }
}
