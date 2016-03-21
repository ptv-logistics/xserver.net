using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using Ptv.XServer.Controls.Map.Tools;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Ptv.XServer.Controls.Map.Layers.Tiled;


namespace Ptv.XServer.Controls.Map.TileProviders
{
    /// <summary> Holds extension methods for adding a Microsoft Bing layer to the map. </summary>
    public static class BingExtensions
    {
        /// <summary> Extension method which adds a Microsoft Bing layer to the map. </summary>
        /// <param name="wpfMap"> The map to add the layer to. </param>
        /// <param name="name"> The name of the layer. </param>
        /// <param name="idx"> The index of the layer in the layer hierarchy. </param>
        /// <param name="bingKey"> The Microsoft Bing key to use. </param>
        /// <param name="set"> The imagery set to be used. </param>
        /// <param name="version"> The Microsoft Bing version. </param>
        /// <param name="isBaseMapLayer"> Specifies if the added layer should act as a base layer. </param>
        /// <param name="opacity"> The initial opacity of the layer. </param>
        /// <param name="icon"> The icon of the layer used within the layer gadget. </param>
        /// <param name="copyrightImagePanel"> The panel where the bing logo should be added. </param>
        public static void AddBingLayer(this WpfMap wpfMap, string name, int idx, string bingKey, BingImagerySet set, BingMapVersion version, 
            bool isBaseMapLayer, double opacity, BitmapImage icon, Panel copyrightImagePanel)
        {
            var metaInfo = new BingMetaInfo(set, version, bingKey);

            // add a bing aerial layer
            var bingLayer = new TiledLayer(name)
            {
                TiledProvider = new BingTiledProvider(metaInfo),
                IsBaseMapLayer = isBaseMapLayer,
                Opacity = opacity,
                Icon = icon
            };

            wpfMap.Layers.Insert(idx, bingLayer);

            try
            {
                var bingLogo = new Image
                {
                    Stretch = Stretch.None,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Source = new BitmapImage(new Uri(metaInfo.LogoUri))
                };

                copyrightImagePanel.Children.Add(bingLogo);
                copyrightImagePanel.Visibility = Visibility.Visible;
            }
            catch (Exception)
            {
                //Just silently catch exceptions if the image cannot be displayed!
            }
        }

        /// <summary> Extension method which adds a Microsoft Bing layer to the map. </summary>
        /// <param name="wpfMap"> The map to add the layer to. </param>
        /// <param name="name"> The name of the layer. </param>
        /// <param name="idx"> The index of the layer in the layer hierarchy. </param>
        /// <param name="bingKey"> The Microsoft Bing key to use. </param>
        /// <param name="set"> The imagery set to be used. </param>
        /// <param name="version"> The Microsoft Bing version. </param>
        /// <param name="isBaseMapLayer"> Specifies if the added layer should act as a base layer. </param>
        /// <param name="opacity"> The initial opacity of the layer. </param>
        /// <param name="icon"> The icon of the layer used within the layer gadget. </param>
        public static void AddBingLayer(this WpfMap wpfMap, string name, int idx, string bingKey, BingImagerySet set, BingMapVersion version, bool isBaseMapLayer, double opacity, BitmapImage icon)
        {
            AddBingLayer(wpfMap, name, idx, bingKey, set, version, isBaseMapLayer, opacity, icon, wpfMap.CopyrightImagePanel);
        }

        /// <summary> Extension method which removes a Microsoft Bing layer from the map. </summary>
        /// <param name="wpfMap"> The map to remove the layer from. </param>
        /// <param name="name"> The name of the layer to be removed. </param>
        public static void RemoveBingLayer(this WpfMap wpfMap, string name)
        {
            wpfMap.CopyrightImagePanel.Children.Clear();
            wpfMap.CopyrightImagePanel.Visibility = Visibility.Hidden;
            wpfMap.Layers.Remove(wpfMap.Layers[name]);
        }
    }

    /// <summary> Tile provider delivering Bing maps images of specified tiles. </summary>
    public class BingTiledProvider : ITiledProvider
    {
        #region private variables
        /// <summary> Meta information of the tile provider like minimum and maximum zoom and url. </summary>
        private readonly BingMetaInfo metaInfo;
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="BingTiledProvider"/> class. </summary>
        /// <param name="metaInfo"> Meta information of the tile provider like minimum and maximum zoom and url. </param>
        public BingTiledProvider(BingMetaInfo metaInfo)
        {
            this.metaInfo = metaInfo;
        }
        #endregion

        #region ITiledProvider Members
        /// <summary> Retrieves the image of a certain map tile and returns it as image stream. </summary>
        /// <param name="x"> X coordinate of the tile. </param>
        /// <param name="y"> Y coordinate of the tile. </param>
        /// <param name="zoom"> Zoom factor of the tile. </param>
        /// <returns> Image stream of the specified tile. </returns>
        public Stream GetImageStream(int x, int y, int zoom)
        {
            string tmpUrl = metaInfo.ImageUrl;
            tmpUrl = tmpUrl.Replace("{subdomain}", metaInfo.ImageUrlSubDomains[((x ^ y) % metaInfo.ImageUrlSubDomains.Length)]);
            tmpUrl = tmpUrl.Replace("{quadkey}", GeoTransform.TileXYToQuadKey(x, y, zoom));
            if (tmpUrl.Contains("{culture}"))
                tmpUrl = tmpUrl.Replace("{culture}", Thread.CurrentThread.CurrentUICulture.Name.ToLower());

            // append key
            if (!string.IsNullOrEmpty(metaInfo.Key))
                tmpUrl = tmpUrl + "&key=" + metaInfo.Key;

            // n=z will return 404 if the tile is not available instead of the "no imagery" image.
            tmpUrl = tmpUrl + "&n=z";

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(tmpUrl);
                request.CachePolicy = new RequestCachePolicy(RequestCacheLevel.CacheIfAvailable);
                request.KeepAlive = false;

                return request.GetResponse().GetResponseStream();
            }
            catch (Exception exception)
            {
                return TileExceptionHandler.RenderException(exception, 256, 256);
            }
        }

        /// <summary> Gets the url of the image as string. </summary>
        public string CacheId { get { return metaInfo.ImageUrl; } }

        /// <summary> Gets the minimum zoom level of the map where tiles are available. </summary>
        public int MinZoom
        {
            get { return metaInfo.MinZoom; }
        }

        /// <summary> Gets the maximum zoom level of the map where tiles are available. </summary>
        public int MaxZoom
        {
            get { return metaInfo.MaxZoom; }
        }
        #endregion
    }

    /// <summary> Enumeration listing the different map contents which can be requested. </summary>
    public enum BingImagerySet
    {
        /// <summary> Only an aerial view of the map is delivered. </summary>
        Aerial = 0,
        /// <summary> Delivers an aerial view together with labels describing towns, streets etc. . </summary>
        AerialWithLabels = 1,
        /// <summary> Delivers Roads in the bitmap. </summary>
        Road = 2
    }

    /// <summary> Version number of the bing map. </summary>
    public enum BingMapVersion
    {
        /// <summary> Version 0. </summary>
        v0 = 0,
        /// <summary> Version 1. </summary>
        v1 = 1
    }

    /// <summary> Meta information which can be requested for a bing map. </summary>
    public class BingMetaInfo
    {
        #region constructor
        /// <summary> Initializes a new instance of the <see cref="BingMetaInfo"/> class. </summary>
        public BingMetaInfo()
        {
        }

        /// <summary> Initializes a new instance of the <see cref="BingMetaInfo"/> class and sets the imagery set, map version and Bing license key. </summary>
        /// <param name="imagerySet"> Type of map content which is requested. </param>
        /// <param name="mapVersion"> Map version which is to be used. </param>
        /// <param name="key"> Microsoft license key for using Bing. </param>
        public BingMetaInfo(BingImagerySet imagerySet, BingMapVersion mapVersion, string key)
        {
            Key = key;
            var url = string.Format(@"http://dev.virtualearth.net/REST/v1/Imagery/Metadata/{0}?mapVersion={1}&o=xml&key={2}", Enum.GetName(typeof(BingImagerySet), imagerySet), Enum.GetName(typeof(BingMapVersion), mapVersion), key);

            // parse xml using linq
            XNamespace restns = "http://schemas.microsoft.com/search/local/ws/rest/v1";
            var metaXml = XDocument.Load(url);
            var resourceSets = from resourceSet in metaXml.Descendants(restns + "ResourceSets")
                               select new
                               {
                                   Resource = from resource in resourceSet.Descendants(restns + "Resources")
                                              select new
                                              {
                                                  ImageryMetaData = from meta in resource.Descendants(restns + "ImageryMetadata")
                                                                    select new
                                                                    {
                                                                        ImagerUrl = meta.Element(restns + "ImageUrl").Value,
                                                                        MinZoom = Convert.ToInt32(meta.Element(restns + "ZoomMin").Value),
                                                                        MaxZoom = Convert.ToInt32(meta.Element(restns + "ZoomMax").Value),
                                                                        SubDomains = from subDomain in meta.Descendants(restns + "ImageUrlSubdomains")
                                                                                     select subDomain.Elements(restns + "string")
                                                                    }
                                              }
                               };

            // initialize properties
            var imageMeta = resourceSets.First().Resource.First().ImageryMetaData.First();
            var logoUriMeta = from brandLogoUri in metaXml.Descendants(restns + "BrandLogoUri")
                          select new
                          {
                              URI = brandLogoUri.Value
                          };
            LogoUri = logoUriMeta.First().URI;
            ImageUrl = imageMeta.ImagerUrl;
            MinZoom = imageMeta.MinZoom;
            MaxZoom = imageMeta.MaxZoom;
            ImageUrlSubDomains = imageMeta.SubDomains.First().Select(subDomain => subDomain.Value).ToArray();
        }
        #endregion

        #region public variables
        /// <summary> Gets or sets the minimum zoom level of the bing map where tiles are available. </summary>
        public int MinZoom { get; set; }
        /// <summary> Gets or sets the maximum zoom level of the bing map where tiles are available. </summary>
        public int MaxZoom { get; set; }

        /// <summary> Gets or sets the Microsoft Bing maps license key. </summary>
        public string Key { get; set; }
        /// <summary> Gets or sets the url of the map image. </summary>
        public string ImageUrl { get; set; }
        /// <summary> Gets or sets the uri of the logo. </summary>
        public string LogoUri { get; set; }

        /// <summary> Gets or sets the set of subdomains of the image url. </summary>
        public string[] ImageUrlSubDomains { get; set; }
        #endregion
    }
}
