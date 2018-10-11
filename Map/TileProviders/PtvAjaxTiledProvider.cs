// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Drawing;
using System.IO;
using System.Net;
using System.Windows;

using Ptv.XServer.Controls.Map.Tools;
using Ptv.XServer.Controls.Map.Layers.Untiled;
using Ptv.XServer.Controls.Map.Layers.Tiled;


namespace Ptv.XServer.Controls.Map.TileProviders
{
    /// <summary> PtvAjaxTiledProvider retrieves a xMapServer-Bitmap using the Ajax Servlet. </summary>
    public class PtvAjaxTiledProvider :
        IUntiledProvider,
        ITiledProvider
    {
        #region private variables
        /// <summary> Url of the tile server. </summary>
        private readonly string m_Url;
        /// <summary> Documentation in progress... </summary>
        private readonly string m_AdditionalParam;
        /// <summary> Minimum value for x coordinates. </summary>
        private const int minX = -20015087;
        /// <summary> Maximum value for x coordinates. </summary>
        private const int maxX = 20015087;
        /// <summary> Minimum value for y coordinates. </summary>
        private const int minY = -10000000; // min y for xMapServer
        /// <summary> Maximum value for y coordinates. </summary>
        private const int maxY = 20015087;
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="PtvAjaxTiledProvider"/> class. </summary>
        /// <param name="url"> Url of the tile server. </param>
        /// <param name="additionalParam"> The additional parameters which should be relayed to the xMapServer. </param>
        public PtvAjaxTiledProvider(string url, string additionalParam)
        {
            m_AdditionalParam = additionalParam;
            m_Url = url;
        }
        #endregion

        #region public methods
        /// <summary> Retrieves the uri of the map image. </summary>
        /// <param name="left"> Left bound of the displayed map section. </param>
        /// <param name="top"> Top bound of the displayed map section. </param>
        /// <param name="right"> Right bound of the displayed map section. </param>
        /// <param name="bottom"> Bottom bound of the displayed map section. </param>
        /// <param name="width"> Width of the displayed map section. </param>
        /// <param name="height"> Height of the displayed map section. </param>
        /// <returns> The uri of the image. </returns>
        public string GetImageUri(double left, double top, double right, double bottom, int width, int height)
        {
            return $"{m_Url}/MapServlet?left={left}&right={right}&top={top}&bottom={bottom}&width={width}&height={height}&{m_AdditionalParam}";
        }

        /// <summary> The internal function for reading the image stream. </summary>
        /// <param name="left"> Left bound of the displayed map section. </param>
        /// <param name="top"> Top bound of the displayed map section. </param>
        /// <param name="right"> Right bound of the displayed map section. </param>
        /// <param name="bottom"> Bottom bound of the displayed map section. </param>
        /// <param name="width"> Width of the displayed map section. </param>
        /// <param name="height"> Height of the displayed map section. </param>
        /// <returns> The map image as stream. </returns>
        public Stream GetImageStreamInternal(double left, double top, double right, double bottom, int width, int height)
        {
            var url = $"{m_Url}/MapServlet?tok=t$o$k&left={left}&right={right}&top={top}&bottom={bottom}&width={width}&height={height}&{m_AdditionalParam}";

            return ReadURL(url);
        }

        /// <summary> The function which reads the image stream and adapts the request parameters. </summary>
        /// <param name="left"> Left bound of the displayed map section. </param>
        /// <param name="top"> Top bound of the displayed map section. </param>
        /// <param name="right"> Right bound of the displayed map section. </param>
        /// <param name="bottom"> Bottom bound of the displayed map section. </param>
        /// <param name="width"> Width of the displayed map section. </param>
        /// <param name="height"> Height of the displayed map section. </param>
        /// <returns> The map image as a stream. </returns>
        public Stream GetImageStream(double left, double top, double right, double bottom, int width, int height)
        {
            try
            {
                if ((left >= minX) && (right <= maxX) && (top >= minY) && (bottom <= maxY))
                    return GetImageStreamInternal(left, top, right, bottom, width, height);

                double leftClipped = Math.Max(left, minX);
                double rightClipped = Math.Min(right, maxX);
                double topClipped = Math.Max(top, minY);
                double bottomClipped = Math.Min(bottom, maxY);

                double rWidth = width * ((rightClipped - leftClipped) / (right - left));
                double rHeight = height * ((bottomClipped - topClipped) / (bottom - top));

                if (rWidth < 32 || rHeight < 32)
                {
                    var ms = new MemoryStream();

                    Bitmap bmp = new Bitmap(width, height);
                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Seek(0, SeekOrigin.Begin);

                    return ms;
                }

                using (var stream = GetImageStreamInternal(leftClipped, topClipped, rightClipped, bottomClipped, (int)rWidth, (int)rHeight))
                    // bitmapCache has to be converted to png. Silverlight doesn't support gif!?
                using (var img = Image.FromStream(stream))
                using (Bitmap bmp = new Bitmap(width, height))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        double offsetX = (leftClipped - left) / (right - left) * width;
                        double offsetY = (bottomClipped - bottom) / (top - bottom) * height;
                        g.DrawImageUnscaled(img, (int)Math.Ceiling(offsetX), (int)Math.Ceiling(offsetY));
                    }

                    // save to memory stream
                    var ms = new MemoryStream();

                    bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    ms.Seek(0, SeekOrigin.Begin);

                    return ms;
                }
            }
            catch (Exception exception)
            {
                return TileExceptionHandler.RenderException(exception, width, height);
            }
        }

        /// <summary> Reads the content from a given url and returns it as a stream. </summary>
        /// <param name="url"> Url where to lad the data from. </param>
        /// <returns> The content as a stream. </returns>
        public Stream ReadURL(string url)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.KeepAlive = false;

            return request.GetResponse().GetResponseStream();
        }
        #endregion

        #region ITiledProvider Members
        /// <inheritdoc/>
        public Stream GetImageStream(int tileX, int tileY, int zoom)
        {
            Rect rect = GeoTransform.TileToPtvMercatorAtZoom(tileX, tileY, zoom);

            return GetImageStream(rect.Left, rect.Top, rect.Right, rect.Bottom, 256, 256);
        }

        /// <inheritdoc/>
        public string CacheId => "PTVAjax" + m_Url + m_AdditionalParam;

        /// <inheritdoc/>
        public int MinZoom => 1;

        /// <inheritdoc/>
        public int MaxZoom => 19;

        #endregion
    }
}
