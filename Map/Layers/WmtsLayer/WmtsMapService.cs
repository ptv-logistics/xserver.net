// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using Ptv.XServer.Controls.Map.Tools;
using Ptv.XServer.Controls.Map.Tools.Reprojection;

namespace Ptv.XServer.Controls.Map.Layers.WmtsLayer
{
    /// <summary>
    /// Defines a function that determines the best matching tile matrix for rendering a map image 
    /// with the given logical bounds and image size.
    /// This delegate is used by WmtsMapService to decouple the service from the full tile matrix set. Being 
    /// used by WmtsMapService, the CRS of the bounding box passed in is the very same as the CRS that has 
    /// been passed to the WmtsMapService during initialization. To be quite clear in the logic to implement, 
    /// the CRS will not be repeated as a parameter in the call to the delegate.
    /// </summary>
    /// <param name="boundingBox">The logical bounds of the map image to be rendered.</param>
    /// <param name="size">The image size of the map to be rendered.</param>
    /// <returns>The best matching tile matrix represented by an implementation of ITileMatrix.</returns>
    public delegate ITileMatrix SelectTileMatrixDelegate(IBoundingBox boundingBox, Size size);

    /// <summary>
    /// MapService implementation that turns a WMTS service into a WMS-like service 
    /// allowing arbitrary map sections and zoom levels to be requested.
    /// </summary>
    public class WmtsMapService : MapService
    {
        /// <summary>
        /// Both size and bounding box passed to GetImageStream below have been determined by multiple transformations. With some 
        /// CRSes being restricted geographically, the actual values of size a/o bounding box may be invalid. As a simple check 
        /// for that, we use the value of SizeLimit to check if pixels sizes are in a valid range. 
        /// 
        /// TODO: Improve the size check and move it to the caller of GetImageStream.
        /// </summary>
        private const int SizeLimit = 8192;

        /// <summary>
        /// Setup a tile cache to cache up to 16MiB of WMTS tile data. This cache is used by LoadImage below.
        /// </summary>
        private readonly LruCache<string, byte[]> tileCache = new LruCache<string, byte[]>(16777216, item => item.Length);

        /// <summary>
        /// Creates and initializes an instance of WmtsMapService.
        /// </summary>
        /// <param name="urlTemplate">The template for settings up the URLs for requesting tiles. See remarks below.</param>
        /// <param name="crs">The CRS of the WMTS service we're calling.</param>
        /// <param name="selectTileMatrix">A function determining the best matching tile matrix for a given rendering request.</param>
        /// <param name="limits">Optionally defines the limits of the MapService in EPSG:76131.</param>
        /// <remarks>
        /// The url template is expected to contain the placeholders "{x}", "{y}" and "{z}", e.g. 
        /// "http://mywmts.service.de/wmts/mylayer/mymatrixset/{z}/{x}/{y}.png". The placeholders will be 
        /// replaced when requesting tiles; the value of "{z}" will be taken from the identifier of the tile 
        /// matrix actually returned by the given SelectTileMatrixDelegate. Each web request created will be
        /// passed to a delegate that allows additional modifications.
        /// </remarks>
        public WmtsMapService(string urlTemplate, string crs, SelectTileMatrixDelegate selectTileMatrix, IBoundingBox limits = null)
        {
            Crs = crs;
            Template = urlTemplate;
            SelectTileMatrix = selectTileMatrix;
            Limits = limits;
        }

        /// <inheritdoc/>
        public override IBoundingBox Limits { get; }

        /// <summary> 
        /// Additional event that will be triggered after the request has been created. Used for request customization. 
        /// </summary>
        public RequestCreatedHandler OnRequestCreated = null;

        /// <summary>
        /// Gets the function that determines the best matching tile matrix for a given rendering request.
        /// </summary>
        private SelectTileMatrixDelegate SelectTileMatrix { get; }

        /// <summary>
        /// Gets the URL template.
        /// </summary>
        private string Template { get; }

        /// <summary>
        /// Helper class based on ImageReprojector that is used to divide an image into 
        /// blocks of a certain size. Used internally for coloring our test image.
        /// </summary>
        private class BlockBuilder : ImageReprojector
        {
            /// <summary>
            /// Creates and initializes an instance of BlockBuilder.
            /// </summary>
            /// <param name="blockSize">Target block size.</param>
            private BlockBuilder(int blockSize) : base(new ReprojectionOptions() { BlockSize = blockSize })
            {
            }

            /// <summary>
            /// Gets the image blocks
            /// </summary>
            public static IEnumerable<ReprojectionBlock> GetBlocks(int width, int height, int blockSize)
            {
                return new BlockBuilder(blockSize).GetBlocks(new Size(width, height));
            }
        }

        /// <summary>
        /// Renders a test image.
        /// </summary>
        /// <param name="width">Width of the test image.</param>
        /// <param name="height">Height of the test image.</param>
        /// <param name="blockSize">Block size used for coloring the test image.</param>
        /// <param name="tileX">The x-coordinate of the tile being requested; used for dynamically coloring the image.</param>
        /// <param name="tileY">The y-coordinate of the tile being requested; used for dynamically coloring the image.</param>
        /// <returns></returns>
        protected virtual Image RenderTestImage(int width, int height, int? blockSize, int tileX, int tileY)
        {
            // correct block size
            if (blockSize.GetValueOrDefault(0) < 8)
                blockSize = Math.Max(width, height);

            // set of colors we're working with
            var colors = new[] { Color.Red, Color.Blue, Color.Green, Color.Cyan, Color.Violet, Color.Yellow };

            // determine colors to be used, 
            // set up reduced color set

            var mod = 64;
            var color0 = colors[(tileX + tileY)%colors.Length];
            var color1 = Color.FromArgb(255, Math.Max(0, color0.R - mod), Math.Max(0, color0.G - mod), Math.Max(0, color0.B - mod));
            var color2 = Color.FromArgb(255, Math.Min(255, color0.R + mod), Math.Min(255, color0.G + mod), Math.Min(255, color0.B + mod));

            colors = new[] { color1, color2 };

            Func<int, int> nextColor = index => (index + 1) % colors.Length;

            var colorStart = 0;
            var colorIndex = 0;

            // create resulting image
            var bmp = new Bitmap(width, height);

            // get graphics object for drawing into the image
            using (var g = Graphics.FromImage(bmp))
                // loop through blocks
                if (blockSize != null)
                    foreach (var block in BlockBuilder.GetBlocks(width, height, blockSize.Value))
                    {
                        // BlockBuilder.GetBlocks returns blocks column-wise
                        // so we use block.y0 == 0 to trigger a color swap
                        if (block.Y0 == 0)
                            colorStart = nextColor(colorIndex = colorStart);

                        // render current block
                        g.FillRectangle(new SolidBrush(colors[colorIndex]), block.X0, block.Y0, block.X1 - block.X0 + 1,
                            block.Y1 - block.Y0 + 1);

                        // next block > next color
                        colorIndex = nextColor(colorIndex);
                    }

            // return image
            return bmp;
        }

        /// <summary>
        /// Loads tile images from the WMTS service.
        /// </summary>
        /// <param name="identifier">Identifier of the tile matrix; Value for the {z} template parameter.</param>
        /// <param name="x">Value for the {x} template parameter.</param>
        /// <param name="y">Value for the {y} template parameter.</param>
        /// <returns>Tile image.</returns>
        protected virtual Image LoadImage(string identifier, int x, int y)
        {
            var cacheKey = $"{identifier}/{x}/{y}";

            // lookup image in cache
            var imageBytes = tileCache[cacheKey];

            // return the cached image, when found
            if (imageBytes != null)
                return Image.FromStream(new MemoryStream(imageBytes));

            // no cache tile has been found, so setup the url and create a web request
            var url = Template.Replace("{z}", identifier).Replace("{x}", x.ToString()).Replace("{y}", y.ToString());
            var req = WebRequest.Create(url);

            // call the optional delegate
            OnRequestCreated?.Invoke(req);

            // send request and get response stream
            using (var resp = req.GetResponse())
            using (var stm = resp.GetResponseStream())
            {
                // fully read response into memory
                var mem = new MemoryStream();
                stm?.CopyTo(mem);

                // be sure to re-position the stream for successive reads,
                // turn the data into an image
                mem.Seek(0, SeekOrigin.Begin);
                var image = Image.FromStream(mem);

                // we did not fail; data has been read + contained an image 
                // add the tile to the cache
                tileCache[cacheKey] = mem.ToArray();

                // return image
                return image;
            }
        }

        /// <summary>
        /// Internal helper that creates and optionally initializes a bitmap image, 
        /// validating the dimensions of the image to be created.
        /// </summary>
        /// <param name="width">Image width</param>
        /// <param name="height">Image height</param>
        /// <param name="initialize">If set to true, initializes the image by filling it with a transparent white color.</param>
        /// <returns>Image or null, if either width of height is invalid.</returns>
        private static Bitmap CreateBitmap(int width, int height, bool initialize = false)
        {
            var bmp = width > 0 && width <= SizeLimit && height > 0 && height <= SizeLimit
                ? new Bitmap(width, height)
                : null;

            if (bmp != null && initialize)
            {
                using (var g = Graphics.FromImage(bmp))
                using (var transparentWhite = new SolidBrush(Color.FromArgb(0, 255, 255, 255)))
                    g.FillRectangle(transparentWhite, 0, 0, bmp.Width, bmp.Height);
            }

            return bmp;
        }

        /// <summary>
        /// Due to limitations of certain CRS the given bounding box a/o size may be invalid. 
        /// This happens when transforming Mercator tile bounds to the CRS. This helper checks 
        /// the bounds and size.
        /// </summary>
        /// <param name="boundingBox"></param>
        /// <param name="size"></param>
        /// <returns>True, if bounds and size are valid.</returns>
        private static bool ParamsValid(IBoundingBox boundingBox, Size size)
        {
            return size.Width > 0 && size.Width <= SizeLimit && size.Height > 0 && size.Height <= SizeLimit
                && boundingBox.Stream().All(dbl => !(double.IsInfinity(dbl) || double.IsNaN(dbl)));
        }

        /// <inheritdoc/>
        public override Stream GetImageStream(IBoundingBox boundingBox, Size requestedSize, out Size effectiveSize)
        {
            // must initialize effective size ... assuming we're returning the requested size.
            effectiveSize = requestedSize;

            // see ParamsValid for explanation; 
            // if parameters are invalid, we simply return null (= no image available)
            if (!ParamsValid(boundingBox, requestedSize))
                return null;

            // find the matrix set that is closest to the given bounds and size
            var matrixSet = SelectTileMatrix(boundingBox, requestedSize);

            // if no matrix set has been found, we simply return null (= no image available)
            if (matrixSet == null)
                return null;

            // determine the logical width & height of a single tile
            var logicalTileWidth = (matrixSet.BottomRightCorner.X - matrixSet.TopLeftCorner.X) / matrixSet.MatrixWidth;
            var logicalTileHeight = (matrixSet.TopLeftCorner.Y - matrixSet.BottomRightCorner.Y) / matrixSet.MatrixHeight;

            // determine the top left tile that is required to cover the requested bounding box
            var tileLeft = (int)Math.Floor((boundingBox.MinX - matrixSet.TopLeftCorner.X) / logicalTileWidth);
            var tileTop = (int)Math.Floor((matrixSet.TopLeftCorner.Y - boundingBox.MaxY) / logicalTileHeight);

            // determine the lower right tile that is required to cover the requested bounding box
            var tileRight = (int)Math.Ceiling((boundingBox.MaxX - matrixSet.TopLeftCorner.X) / logicalTileWidth) - 1;
            var tileBottom = (int)Math.Ceiling((matrixSet.TopLeftCorner.Y - boundingBox.MinY) / logicalTileHeight) - 1;

            // apply limits as defined by the selected matrix set
            tileLeft = Math.Max(tileLeft, matrixSet.MinX());
            tileTop = Math.Max(tileTop, matrixSet.MinY());
            tileRight = Math.Min(tileRight, matrixSet.MaxX());
            tileBottom = Math.Min(tileBottom, matrixSet.MaxY());

            try
            {
                // resulting image
                Image resultingImage = null;

                // check if there is anything to be rendered
                if (tileRight < tileLeft || tileBottom < tileTop)
                { 
                    // there are no tile to be rendered; return empty image
                    resultingImage = CreateBitmap(requestedSize.Width, requestedSize.Height, true);
                }
                else
                {
                    try
                    {
                        // create a temporary image in which we place the tiles
                        var tilesImage = CreateBitmap((tileRight - tileLeft + 1) * matrixSet.TileWidth, (tileBottom - tileTop + 1) * matrixSet.TileHeight);

                        // fail, if image is invalid. This happens e.g. due to an invalid image size.
                        if (tilesImage == null)
                            return null;

                        using (var tileGraphics = Graphics.FromImage(tilesImage))
                        {
                            // loop through the tiles, request and draw the images
                            // we'll pass on WebExceptions but we will swallow any other exception

                            for (int tx = tileLeft, ox = 0; tx <= tileRight; ++tx, ox += matrixSet.TileWidth)
                                for (int ty = tileTop, oy = 0; ty <= tileBottom; ++ty, oy += matrixSet.TileHeight)
                                    try
                                    {
                                        var image = Template.ToLowerInvariant() == "test"
                                            ? RenderTestImage(matrixSet.TileWidth, matrixSet.TileHeight, 32, tx, ty)
                                            : LoadImage(matrixSet.Identifier, tx, ty);

                                        tileGraphics.DrawImageUnscaled(image, new Point(ox, oy));
                                    }

                                    // pass on WebException, swallow others

                                    catch (WebException) { throw; }
                                    catch { /* ignored */ }
                        }

                        // determine the logical bounds covered by map rendered above 
                        var logicalTileRect = new Tools.Reprojection.MapRectangle(
                            matrixSet.TopLeftCorner.X + tileLeft * logicalTileWidth,
                            matrixSet.TopLeftCorner.Y - tileTop * logicalTileHeight,
                            matrixSet.TopLeftCorner.X + (tileRight + 1) * logicalTileHeight,
                            matrixSet.TopLeftCorner.Y - (tileBottom + 1) * logicalTileHeight
                        );


                        // test, if the tile image fully covers the bounding box that has initially been requested.
                        // 
                        // if that is the case, we'll extract the requested bounding box from that image (unscaled) and 
                        // return that. This avoids an additional drawing operation that scales the image and decreases quality.
                        
                        if (boundingBox.MinX >= logicalTileRect.MinX && boundingBox.MaxX <= logicalTileRect.MaxX && boundingBox.MinY >= logicalTileRect.MinX && boundingBox.MaxY <= logicalTileRect.MaxY)
                        {
                            // the tile image fully covers the bounding box that has initially been requested.

                            // determine position and extent of the originally requested bounding box in the tile image
                            var x = (int)Math.Round(tilesImage.Width * (boundingBox.MinX - logicalTileRect.MinX) / logicalTileRect.Size().Width);
                            var y = (int)Math.Round(tilesImage.Height * (logicalTileRect.MaxY - boundingBox.MaxY) / logicalTileRect.Size().Height);

                            var w = (int)Math.Round((double)tilesImage.Width * boundingBox.Size().Width / logicalTileRect.Size().Width);
                            var h = (int)Math.Round((double)tilesImage.Height * boundingBox.Size().Height / logicalTileRect.Size().Height);

                            // the following code "extracts" the originally requested bounding box into the result image
                            // since we're not going to return the requestedSize, we must set effectiveSize accordingly.

                            effectiveSize = new Size(w, h);
                            resultingImage = CreateBitmap(w, h);

                            using (var g = Graphics.FromImage(resultingImage))
                                g.DrawImageUnscaled(tilesImage, -x, -y);
                        }
                        else
                        {
                            resultingImage = CreateBitmap(requestedSize.Width, requestedSize.Height, true);

                            using (var graphics = Graphics.FromImage(resultingImage))
                            {
                                // calculate position and size for placing the tile image into 
                                // the resulting image that was initially requested

                                var zoomX = boundingBox.Size().Width / requestedSize.Width;
                                var zoomY = boundingBox.Size().Height / requestedSize.Height;

                                var dstRect = new Rectangle(
                                    // x & y
                                    (int)Math.Round((logicalTileRect.Left - boundingBox.MinX) / zoomX),
                                    (int)Math.Round((boundingBox.MaxY - logicalTileRect.Top) / zoomY),

                                    // width & height
                                    (int)Math.Round(((tileRight - tileLeft + 1) * logicalTileWidth) / zoomX),
                                    (int)Math.Round(((tileBottom - tileTop + 1) * logicalTileHeight) / zoomY)
                                );

                                // draw the tile image using a high quality mode

                                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                                graphics.DrawImage(tilesImage, dstRect, 0, 0, tilesImage.Size.Width, tilesImage.Size.Height, GraphicsUnit.Pixel);
                            }
                        }
                    }

                    // pass on WebException, swallow others

                    catch (WebException) { throw; }
                    catch { /* ignored */ }
                }

                return resultingImage.StreamPng();
            }

            // pass on WebException, swallow others

            catch (WebException) { throw; }
            catch { return null; }
        }
    }
}
