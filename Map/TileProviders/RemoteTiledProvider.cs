﻿// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

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
        /// <summary> Initializes a new instance of the <see cref="RemoteTiledProvider"/> class. </summary>
        public RemoteTiledProvider()
        {
            MinZoom = 0;
            MaxZoom = 19;
        }

        /// <inheritdoc/>
        public Stream GetImageStream(int tileX, int tileY, int zoom)
        {
            try { return ReadURL(RequestBuilderDelegate(tileX, tileY, zoom)); }
            catch (Exception exception) { return TileExceptionHandler.RenderException(exception, 256, 256); }
        }

        /// <summary> Reads the content from a given url and returns it as a stream. </summary>
        /// <param name="url"> The url to look for. </param>
        /// <returns> The url content as a stream. </returns>
        public Stream ReadURL(string url)
        {
            try
            {
                var request = (HttpWebRequest) WebRequest.Create(url);
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request.KeepAlive = true;
                request.Proxy.Credentials = CredentialCache.DefaultCredentials;

                request = (HttpWebRequest) Layers.Xmap2.LayerFactory.ModifyRequest?.Invoke(request) ?? request;
                return request.GetResponse().GetResponseStream();
            }
            catch (WebException webException)
            {
                logger.Writeline(TraceEventType.Error, "WebException occured :" + Environment.NewLine + "Exception Message : " + webException.Message);
                logger.Writeline(TraceEventType.Error, "URL :" + url);
                logger.Writeline(TraceEventType.Error, string.Format("WebException Status : {0}", webException.Status));
                if (webException.Status == WebExceptionStatus.ProtocolError)
                {
                    logger.Writeline(TraceEventType.Error, string.Format("Status Code : {0}", ((HttpWebResponse)webException.Response).StatusCode));
                    logger.Writeline(TraceEventType.Error, string.Format("Status Description : {0}", ((HttpWebResponse)webException.Response).StatusDescription));
                }
                throw;
            }
            catch (Exception exception)
            {
                logger.Writeline(TraceEventType.Error, url + ":" + Environment.NewLine + exception.Message);
                throw;
            }
        }

        /// <summary> Gets or sets a method which can be used to build a request. </summary>
        public RequestBuilder RequestBuilderDelegate { get; set; }

        /// <summary> Method building a request by the given tile information. </summary>
        /// <param name="x"> X coordinate of the requested tile. </param>
        /// <param name="y"> Y coordinate of the requested tile. </param>
        /// <param name="level"> Zoom level of the requested tile. </param>
        /// <returns> The request string. </returns>
        public delegate string RequestBuilder(int x, int y, int level);

        /// <inheritdoc/>
        public string CacheId => RequestBuilderDelegate(0, 0, 0);

        /// <inheritdoc/>
        public int MinZoom {get; set; }

        /// <inheritdoc/>
        public int MaxZoom { get; set; }

        private static string Clean(string toClean) => string.IsNullOrEmpty(toClean = toClean?.Trim()) ? null : toClean;

        private string userAgent;
        /// <summary> Gets or sets the value of the user agent HTTP header. </summary>
        public string UserAgent
        {
          get => userAgent;
          set => userAgent = Clean(value);
        }

    /// <summary> Logging restricted to this class. </summary>
    private static readonly Logger logger = new Logger("RemoteTiledProvider");
    }
}
