// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.IO;
using System.Net;

using Ptv.XServer.Controls.Map.Layers.Untiled;
using Ptv.XServer.Controls.Map.Layers.Tiled;
using System.Collections.Generic;

namespace Ptv.XServer.Controls.Map.TileProviders
{
    /// <summary> Provider delivering bitmap tiles which are requested from an xMapServer. </summary>
    public abstract class XMapTiledProviderBase : IUntiledProviderWithMapObjects, ITiledProvider, ITilingOptions
    {
        #region private variables

        /// <summary> Minimum x coordinate value. </summary>
        protected int minX = -20000000;

        /// <summary> Maximum x coordinate value. </summary>
        protected int maxX = 20000000;

        /// <summary> Minimum y coordinate value. </summary>
        protected int minY = -10000000;

        /// <summary> Maximum y coordinate value. </summary>
        protected int maxY = 20000000;

        /// <summary> Constant value for the earth radius used in calculations. </summary>
        private const double earthRadius = 6371000.0;

        /// <summary> Constant value for the half earth circumference used in calculations. </summary>
        private const double earthHalfCircum = earthRadius*Math.PI;

        #endregion

        #region protected variables
        /// <summary> Flag indicating whether the background is to be set to a transparent color. </summary>
        protected bool needsTransparency;
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="XMapTiledProviderBase"/> class. </summary>
        protected XMapTiledProviderBase()
        {
            // parameters for scaling of the MSI
            Factor = 1;

            MinZoom = 0;
            MaxZoom = 19;
        }
        #endregion

        #region protected methods
        /// <summary> Saves an image in a stream and returns this stream. </summary>
        /// <param name="image"> The image to process. </param>
        /// <returns> The image as stream. </returns>
        protected MemoryStream SaveAndConvert(System.Drawing.Bitmap image)
        {
            var memoryStream = new MemoryStream();
            image.Save(memoryStream, System.Drawing.Imaging.ImageFormat.Png);
            memoryStream.Seek(0, SeekOrigin.Begin);

            return memoryStream;
        }

        /// <summary> Converts the tile position to PTV Mercator coordinates respecting the zoom factor. </summary>
        /// <param name="tileX"> X position of the tile. </param>
        /// <param name="tileY"> Y position of the tile. </param>
        /// <param name="zoom"> Zoom factor of the tile. </param>
        /// <param name="xMin"> Minimum x value of the returned map rectangle. </param>
        /// <param name="yMin"> Minimum y value of the returned map rectangle. </param>
        /// <param name="xMax"> Maximum x value of the returned map rectangle. </param>
        /// <param name="yMax"> Maximum y value of the returned map rectangle. </param>
        protected void TileToPtvMercatorAtZoom(int tileX, int tileY, int zoom,
                                                out double xMin, out double yMin, out double xMax, out double yMax)
        {
            double arc = earthHalfCircum / Math.Pow(2, zoom - 1);

            xMin = tileX * arc - earthHalfCircum;
            yMax = earthHalfCircum - tileY * arc;

            xMax = xMin + arc;
            yMin = yMax - arc;

            xMin = xMin / Factor;
            yMax = yMax / Factor;
            xMax = xMax / Factor;
            yMin = yMin / Factor;

            double dWidth = (xMax - xMin) * OverlapFactor / 2;
            double lHeight = (yMax - yMin) * OverlapFactor / 2;

            xMin -= dWidth;
            xMax += dWidth;
            yMin -= lHeight;
            yMax += lHeight;
        }
        #endregion

        #region public methods
        /// <summary> Gets or sets the context key. </summary>
        public string ContextKey { get; set; }

        /// <summary> Retrieves the image stream for a certain map rectangle. </summary>
        /// <param name="left"> Left coordinates. </param>
        /// <param name="top"> Top coordinates. </param>
        /// <param name="right"> Right coordinates. </param>
        /// <param name="bottom"> Bottom coordinates. </param>
        /// <param name="width"> Width of the image. </param>
        /// <param name="height"> Height of the image. </param>
        /// <param name="border"> Border width. </param>
        /// <param name="mapObjects"> Set of map objects. </param>
        /// <returns> The image as a stream. </returns>
        public Stream GetStream(double left, double top, double right, double bottom, int width, int height, int border, out IEnumerable<IMapObject> mapObjects)
        {
            mapObjects = null;

            if (left >= minX && right <= maxX && top >= minY && bottom <= maxY && border <= 0)
                return GetStreamInternal(left, top, right, bottom, width, height, out mapObjects);

            // request must be resized or clipped
            double leftResized, rightResized, topResized, bottomResized;

            // calculate resized bounds depending on border
            // the resize factor internally resizes requested tiles to avoid clipping problems
            if (border > 0)
            {
                double resize = (double)border / width;
                double lWidth = (right - left) * resize;
                double lHeight = (bottom - top) * resize;

                leftResized = left - lWidth;
                rightResized = right + lWidth;
                topResized = top - lHeight;
                bottomResized = bottom + lHeight;
            }
            else
            {
                leftResized = left;
                rightResized = right;
                topResized = top;
                bottomResized = bottom;
            }

            // calculate clipped bounds
            double leftClipped = leftResized < minX ? minX : leftResized;
            double rightClipped = rightResized > maxX ? maxX : rightResized;
            double topClipped = topResized < minY ? minY : topResized;
            double bottomClipped = bottomResized > maxY ? maxY : bottomResized;

            // calculate corresponding pixel width and height 
            //
            // TODO: There might be a deep zoom issue arising from using Math.Round, especially when clipping 
            // is active. In this case , Math.Round changes the aspect ratio of the pixel rectangle significantly 
            // on lower levels. Look at MapParam.FixMe that tries to handle aspect issues. Ignored for the moment.
            int widthClipped = (int)Math.Round(width * (rightClipped - leftClipped) / (right - left));
            int heightClipped = (int)Math.Round(height * (bottomClipped - topClipped) / (bottom - top));

            if (widthClipped < 32 || heightClipped < 32)
            {
                // resulting image will be too small -> return empty image
                using (var bmp = new System.Drawing.Bitmap(width, height))
                {
                    return SaveAndConvert(bmp);
                }
            }
            using (var stream = GetStreamInternal(leftClipped, topClipped, rightClipped, bottomClipped, widthClipped, heightClipped, out mapObjects))
            {
                // paste resized/clipped image on new image
                using (var img = System.Drawing.Image.FromStream(stream))
                {
                    using (var bmp = new System.Drawing.Bitmap(width, height))
                    {
                        using (System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bmp))
                        {
                            double offsetX = (leftClipped - left) / (right - left) * width;
                            double offsetY = (bottomClipped - bottom) / (top - bottom) * height;

                            g.DrawImageUnscaled(img, (int)Math.Round(offsetX), (int)Math.Round(offsetY));
                        }

                        return SaveAndConvert(bmp);
                    }
                }
            }
        }

        /// <summary> Retrieves the map image from the xMapServer and returns it as a stream. </summary>
        /// <param name="left"> Left coordinates. </param>
        /// <param name="top"> Top coordinates. </param>
        /// <param name="right"> Right coordinates. </param>
        /// <param name="bottom"> Bottom coordinates. </param>
        /// <param name="width"> Width of the image. </param>
        /// <param name="height"> Height of the image. </param>
        /// <param name="mapObjects"> Set of map objects. </param>
        /// <returns> The map image as stream. </returns>
        public Stream GetStreamInternal(double left, double top, double right, double bottom, int width, int height, out IEnumerable<IMapObject> mapObjects)
        {
            mapObjects = null;
            int trials = 0;

            while (true)
            {
                try
                {
                    var imageStream = new MemoryStream(TryGetStreamInternal(left, top, right, bottom, width, height, out mapObjects));
                    if (!needsTransparency) return imageStream;

                    using(var image = System.Drawing.Image.FromStream(imageStream))
                    {
                        var bmp = image as System.Drawing.Bitmap;

                        // make map background color transparent
                        bmp?.MakeTransparent(System.Drawing.Color.FromArgb(255, 254, 185));

                        return SaveAndConvert(bmp);
                    }
                }
                catch (WebException ex)
                {
                    var res = (HttpWebResponse)ex.Response;
                    if (res != null && res.StatusCode != HttpStatusCode.InternalServerError && res.StatusCode != HttpStatusCode.ServiceUnavailable)
                        return TileExceptionHandler.RenderException(ex, width, height);

                    if (++trials >= 3) 
                        return TileExceptionHandler.RenderException(ex, width, height);

                    System.Threading.Thread.Sleep(50);
                }
                catch (Exception ex)
                {
                    return TileExceptionHandler.RenderException(ex, width, height);
                }
            }
        }

        /// <summary> Gets or sets the custom profile of the xMapServer. </summary>
        public virtual string CustomProfile { get; set; }

        /// <summary> Retrieves the map image from the xMapServer as stream. </summary>
        /// <param name="left"> Left coordinates. </param>
        /// <param name="top"> Top coordinates. </param>
        /// <param name="right"> Right coordinates. </param>
        /// <param name="bottom"> Bottom coordinates. </param>
        /// <param name="width"> Width of the image. </param>
        /// <param name="height"> Height of the image. </param>
        /// <param name="mapObjects"> Set of map objects. </param>
        /// <returns> The map image as stream. </returns>
        public abstract byte[] TryGetStreamInternal(double left, double top, double right, double bottom, int width, int height, out IEnumerable<IMapObject> mapObjects);
        #endregion

        #region ITiledProvider Members
        /// <inheritdoc/>
        public virtual Stream GetImageStream(int tileX, int tileY, int zoom)
        {
            TileToPtvMercatorAtZoom(tileX, tileY, zoom, out var xMin, out var yMin, out var xMax, out var yMax);

            return GetStream(xMin, yMin, xMax, yMax,
                256 + (int)Math.Round(256 * OverlapFactor), 256 + (int)Math.Round(256 * OverlapFactor), Border, out _);
        }

        /// <inheritdoc/>
        public abstract string CacheId { get; }

        /// <inheritdoc/>
        public int MinZoom { get; set; }

        /// <inheritdoc/>
        public int MaxZoom { get; set; }
        #endregion

        #region IUntiledProvider
        /// <summary> Retrieves the image stream. </summary>
        /// <param name="left"> Left coordinate. </param>
        /// <param name="top"> Top coordinate. </param>
        /// <param name="right"> Right coordinate. </param>
        /// <param name="bottom"> Bottom coordinate. </param>
        /// <param name="width"> Width of the image. </param>
        /// <param name="height"> Height of the image. </param>
        /// <returns> The image as a stream. </returns>
        public Stream GetImageStream(double left, double top, double right, double bottom, int width, int height)
        {
            return GetImageStreamAndMapObjects(left, top, height, bottom, width, height, out _);
        }

        /// <summary> Retrieves the image stream and the corresponding map objects belonging to the returned map image. </summary>
        /// <param name="left"> Left coordinate. </param>
        /// <param name="top"> Top coordinate. </param>
        /// <param name="right"> Right coordinate. </param>
        /// <param name="bottom"> Bottom coordinate. </param>
        /// <param name="width"> Width of the image. </param>
        /// <param name="height"> Height of the image. </param>
        /// <param name="mapObjects">Set of map objects belonging to the map image. These objects can be used to provide tool tips in an interactive map.</param>
        /// <returns> The image as a stream. </returns>
        public Stream GetImageStreamAndMapObjects(double left, double top, double right, double bottom, int width, int height, out IEnumerable<IMapObject> mapObjects)
        {
            return GetStream(left, top, right, bottom, width, height, 0, out mapObjects);
        }

        #endregion

        #region ITilingOptions Members

        /// <inheritdoc/>
        public double Factor { get; set; }

        /// <inheritdoc/>
        public double OverlapFactor { get; set; }

        /// <inheritdoc/>
        public int Border { get; set; }
        #endregion
    }
}
