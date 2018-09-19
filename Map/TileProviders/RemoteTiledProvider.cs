// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using Ptv.XServer.Controls.Map.Layers.Tiled;
using Ptv.XServer.Controls.Map.Tools;

namespace Ptv.XServer.Controls.Map.TileProviders
{
    /// <summary> Provider loading tiled bitmaps from a given url. </summary>
    public class RemoteTiledProvider : ITiledProvider
    {
        /// <summary> Logging restricted to this class. </summary>
        private static readonly Logger logger = new Logger("RemoteTiledProvider");

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="RemoteTiledProvider"/> class. </summary>
        public RemoteTiledProvider()
        {
            MinZoom = 0;
            MaxZoom = 19;
        }
        #endregion

        #region public variables
        /// <summary> Gets or sets a method which can be used to build a request. </summary>
        public RequestBuilder RequestBuilderDelegate { get; set; }
        #endregion

        #region public methods
        /// <summary> Reads the content from a given url and returns it as a stream. </summary>
        /// <param name="url"> The url to look for. </param>
        /// <returns> The url content as a stream. </returns>
        public Stream ReadURL(string url)
        {
            try
            {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request.KeepAlive = true;

                return request.GetResponse().GetResponseStream();

            }
            catch (Exception exception)
            {
                logger.Writeline(TraceEventType.Error, url + ":" + Environment.NewLine + exception.Message);
                throw;
            }
        }

        /// <summary> Method building a request by the given tile information. </summary>
        /// <param name="x"> X coordinate of the requested tile. </param>
        /// <param name="y"> Y coordinate of the requested tile. </param>
        /// <param name="level"> Zoom level of the requested tile. </param>
        /// <returns> The request string. </returns>
        public delegate string RequestBuilder(int x, int y, int level);
        #endregion

        #region ITiledProvider Members
        /// <inheritdoc/>
        public Stream GetImageStream(int tileX, int tileY, int zoom)
        {
            try { return ReadURL(RequestBuilderDelegate(tileX, tileY, zoom)); }
            catch (Exception exception) { return TileExceptionHandler.RenderException(exception, 256, 256); }
        }

        /// <inheritdoc/>
        public string CacheId => RequestBuilderDelegate(0, 0, 0);

        /// <inheritdoc/>
        public int MinZoom {get; set; }

        /// <inheritdoc/>
        public int MaxZoom { get; set; }
        #endregion
    }
}
