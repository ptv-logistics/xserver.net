// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Drawing;
using System.IO;
using System.Linq;
using Ptv.Components.Projections;

#pragma warning disable CS3003 // Type is not CLS-compliant

namespace Ptv.XServer.Controls.Map.Tools.Reprojection
{
    using SizeD = System.Windows.Size;
    using PointD = System.Windows.Point;

    /// <summary>
    /// Encapsulates the options for ReprojectionService.
    /// </summary>
    public class ReprojectionServiceOptions
    {
        /// <summary> Creates and initializes an instance ReprojectionServiceOptions. </summary>
        /// <remarks> This constructor sets the necessary defaults for ReprojectionServiceOptions. </remarks>
        public ReprojectionServiceOptions()
        {
            SourceBoundsSupportingPoints = 8;
            SourceSizeFactor = null;
        }

        /// <summary>
        /// Gets or sets the number of the supporting points that are used when determining the source bounding box.
        /// </summary>
        public int SourceBoundsSupportingPoints { get; set; }

        /// <summary>
        /// Gets or sets the factor used for determining the size of the images requested from the inner map service.
        /// </summary>
        /// <remarks>
        /// When re-projection applies, the size of the images requested from the inner WMS map services must
        /// be determined in a way to allow re-projection to produce high quality results. However, as the 
        /// source and target aspect ratio may differ significantly, it is not quite easy to determine a 
        /// viable source size.
        /// <br/>
        /// The general idea is to base the source size calculations on an amount pixels, which can simply be 
        /// transformed into the source size via the aspect ratio of the source bounding box. Based on the 
        /// request target size, we can determine a minimum and maximum source size using the shortest and 
        /// the longest side of the target rectangle as the base in the calculation. The minimum and maximum 
        /// size then can be transformed into a minimum and maximum pixel amount which, using the value of 
        /// sourceSizeFactor, can in turn be transformed into the final source amount of pixels.
        /// <br/>
        /// If sourceSizeFactor contains a positive value, the source size will be determined as described
        /// above: source = min + (max - min)*sourceSizeFactor.
        /// <br/>
        /// If sourceSizeFactor is invalid (or null) the source amount of pixels will be set to equal 
        /// the target amount of pixels: source = target.
        /// <br/>
        /// If sourceSizeFactor is negative, its absolute value will be multiplied with the target amount 
        /// of pixels: source = target * sourceSizeFactor.
        /// </remarks>
        /// 
        public double? SourceSizeFactor { get; set; }
    }

    /// <summary>
    /// This class represents map service adding re-projection to another one.
    ///  </summary>
    public class ReprojectionService : IMapService<MapRectangle>
    {
        /// <summary> the source map service delivering the map images to be re-projected </summary>
        protected readonly MapService SourceMapService;

        /// <summary> The coordinate transformation that transforms target (this.CRS) to source (mapService.CRS) points </summary>
        protected readonly ICoordinateTransformation TargetToSourceTransformation;

        /// <summary>Transparent white - the color used when creating default images</summary>
        protected static readonly Color TransparentWhite = Color.FromArgb(0, 255, 255, 255);

        /// <summary>In case the ReprojectionService is used to render tiles, prepare a transparent tile that can be re-used by RenderTransparentImage.</summary>
        protected static readonly byte[] TransparentTile = new Size(256, 256).CreateImage(TransparentWhite).StreamPng().ToArray();

        /// <summary>
        /// Creates and initializes an instance of ReprojectionService.
        /// </summary>
        /// <param name="sourceMapService">The inner map service that delivers the map images to be re-projected.</param>
        /// <param name="crs">The CRS to target by this instance.</param>
        /// <param name="reprojector">The re-projector to be used. Uses the default image re-projector if not explicitly set.</param>
        /// <param name="reprojectionServiceOptions">Additional options to be used by this map service.</param>
        public ReprojectionService(MapService sourceMapService, string crs, ImageReprojector reprojector = null, ReprojectionServiceOptions reprojectionServiceOptions = null)
        {
            // store parameters
            SourceMapService = sourceMapService;
            try { TargetToSourceTransformation = CoordinateTransformation.Get(Crs = crs, sourceMapService.Crs); }
            catch { throw new ArgumentException("Failed to setup coordinate transformation for source \"" + sourceMapService.Crs + "\" and target \"" + crs + "\""); }

            Reprojector = reprojector ?? new ImageReprojector();
            ReprojectionServiceOptions = reprojectionServiceOptions ?? new ReprojectionServiceOptions();
        }

        /// <summary> Accesses the options to be used by the map service. </summary>
        public ReprojectionServiceOptions ReprojectionServiceOptions { get; }

        /// <summary> Accesses the re-projector used by the map service. </summary>
        public ImageReprojector Reprojector { get; }
        
        /// <summary>
        /// Used internally to describe the covering of a requested map section.
        /// </summary>
        private enum Covering
        {
            /// <summary>
            /// MapService has no known bounds; cannot determine covering.
            /// </summary>
            Unknown,

            /// <summary>
            /// Requested map is outside of the MapService's bounds.
            /// </summary>
            None,

            /// <summary>
            /// Requested map is partially covered by the MapService's bounds.
            /// </summary>
            Partial,

            /// <summary>
            /// Requested map is fully covered by the MapService's bounds.
            /// </summary>
            Full
        }

        /// <summary>
        /// Determines the coverage of a requested map.
        /// </summary>
        /// <param name="target">MapRectangle of the requested map.</param>
        /// <param name="covering">Contains the covered area upon return.</param>
        /// <returns>Coverage of the requested map.</returns>
        private Covering DetermineCovering(MapRectangle target, out MapRectangle covering)
        {
            // assume full coverage
            covering = target;

            var source = SourceMapService.Limits;

            // no limits > unknown covering
            if (source == null)
                return Covering.Unknown;

            // requested map is within limits > full covering
            if (target.MinX >= source.MinX && target.MaxX <= source.MaxX && target.MinY >= source.MinY && target.MaxY <= source.MaxY)
                return Covering.Full;

            // intersect limits and requested map
            covering = new MapRectangle(
                Math.Max(target.MinX, source.MinX),
                Math.Min(target.MaxY, source.MaxY),
                Math.Min(target.MaxX, source.MaxX),
                Math.Max(target.MinY, source.MinY)
            );

            // if they don't intersect there's not covering; otherwise there's partial covering 
            //
            // we'll have to use left, right, top and bottom for the check; due to the implementation 
            // in MapRectangle, min < max will always be true. See how we set left, right, top and bottom 
            // above.
            return covering.Left < covering.Right && covering.Bottom < covering.Top
                ? Covering.Partial
                : Covering.None;
        }

        /// <inheritdoc />
        public virtual Stream GetImageStream(MapRectangle targetMapRectangle, Size targetSize)
        {
            // determine coverage, render accordingly
            switch (DetermineCovering(targetMapRectangle, out var coveredMapRectangle))
            {
                // MapService is only partially visible in the requested map
                case Covering.Partial:
                    using (var image = targetSize.CreateImage(TransparentWhite))
                    {
                        using (var graphics = Graphics.FromImage(image))
                        {
                            // determine the pixel rectangle corresponding to 
                            // the rectangle defined through covered

                            var scaleX = targetSize.Width / (targetMapRectangle.MaxX - targetMapRectangle.MinX);
                            var scaleY = targetSize.Height / (targetMapRectangle.MaxY - targetMapRectangle.MinY);

                            var x0 = (int)Math.Round(scaleX * (coveredMapRectangle.MinX - targetMapRectangle.MinX));
                            var y0 = (int)Math.Round(scaleY * (targetMapRectangle.MaxY - coveredMapRectangle.MaxY));
                            var x1 = (int)Math.Round(scaleX * (coveredMapRectangle.MaxX - targetMapRectangle.MinX));
                            var y1 = (int)Math.Round(scaleY * (targetMapRectangle.MaxY - coveredMapRectangle.MinY));

                            // using the pixel rectangle from above, 
                            // request the visible portion and render it into the result image
                            using (var stm = GetImageStream(coveredMapRectangle, new Size(x1 - x0, y1 - y0)))
                            {
                                if (stm == null)
                                    return null;

                                graphics.DrawImageUnscaled(Image.FromStream(stm), x0, y0);
                            }
                        }

                        return image.StreamPng();
                    }

                // visibility is either unknown or the MapService full covers the tile; proceed and request the full map
                case Covering.Unknown:
                case Covering.Full:
                    return GetImageStream(targetMapRectangle, targetSize, true, false);

                // MapService does not cover the requested map, return fully transparent map image
                case Covering.None:
                    return RenderTransparentImage(targetSize);

                // default = unknown enumeration value; also return fully transparent map image
                default:
                    return RenderTransparentImage(targetSize);
            }
        }

        /// <summary>
        /// Renders a PNG image filled with transparent white.
        /// </summary>
        /// <param name="size">Size if the image to return.</param>
        /// <returns>PNG image.</returns>
        private static Stream RenderTransparentImage(Size size)
        {
            // re-use transparent tile if possible, otherwise render image
            Stream stm = (size.Width == 256 && size.Height == 256 && TransparentTile != null)
                ? new MemoryStream(TransparentTile)
                : size.CreateImage(TransparentWhite).StreamPng();

            // be sure re-position stream
            stm.Seek(0, SeekOrigin.Begin);

            // return stream
            return stm;
        }

        /// <summary>
        /// Loads a source map image from the inner map service using the specified target bounding box and target size.  
        /// </summary>
        /// <param name="targetMapRectangle">Requested bounding box, based on the CRS defined by this class.</param>
        /// <param name="targetSize">Requested image size in pixels.</param>
        /// <param name="includeGrid">If set to true, renders the re-projection grid into the returned image. Used for debugging purposes.</param>
        /// <returns>
        /// The stream containing the map image.
        /// </returns>
        public virtual Stream GetInnerImageStream(MapRectangle targetMapRectangle, Size targetSize, bool includeGrid)
        {
            return GetImageStream(targetMapRectangle, targetSize, false, includeGrid);
        }

        /// <summary> Loads a map image using the specified bounding box and size. </summary>
        /// <param name="targetMapRectangle">Requested bounding box, based on the CRS defined by this class.</param>
        /// <param name="targetSize">Requested image size in pixels.</param>
        /// <param name="reproject">See remarks.</param>
        /// <param name="includeGrid">If set to true, renders the re-projection grid into the returned image. Used for debugging purposes.</param>
        /// <returns> The stream containing the map image. </returns>
        /// <remarks> For debugging purposes, re-projection can be suppressed by setting the reproject parameter to false. </remarks>
        protected virtual Stream GetImageStream(MapRectangle targetMapRectangle, Size targetSize, bool reproject, bool includeGrid)
        {
            // knowing the target parameters, determine the initial source bounds. 
            var sourceBounds = targetMapRectangle.TransformBoundingBox(TargetToSourceTransformation, ReprojectionServiceOptions.SourceBoundsSupportingPoints);

            // immediately fail if transformed bounds has invalid coordinates
            if (!sourceBounds.AllCoordinatesValid)
                return null;

            MapRectangle sourceBoundingBox;

            // we don't need to re-project if the source bounds are rectangular and have the same aspect ratio
            // as the target rectangle. In this case, we can also use the target size as the source size.
            if (sourceBounds.IsRectangular)
            {
                sourceBoundingBox = new MapRectangle(new[] { sourceBounds.LeftTop, sourceBounds.RightBottom });

                if (sourceBoundingBox.EqualsAspect(targetMapRectangle))
                {
                    // no re-projection required; load and return stream
                    return SourceMapService.GetImageStream(sourceBoundingBox, targetSize);
                }
            }

            // Re-projection required. The source bounding box either is not rectangular or does not have
            // the same aspect ratio as the target bounding box. We now need to determine a suitable source 
            // bounding box. Using this bounding box, we need to setup the source size so that the re-projection 
            // can work in an acceptable way.

            // approximate bounding box 
            sourceBoundingBox = sourceBounds.ApproximateBoundingBox(1.025);

            // For details on the source size, please refer to DetermineSourceSize.
            var sourceSize = DetermineSourceSize(sourceBoundingBox, targetSize);

            // Get original stream
            var originalStream = SourceMapService.GetImageStream(sourceBoundingBox, sourceSize, out sourceSize);

            // depending on the parametrization we can directly pass through inner image
            if ((!reproject && !includeGrid) || originalStream == null)
                return originalStream;

            // ... otherwise we need to process the image 

            // set up transformation function
            var targetToSource = GetTransformFunction(targetMapRectangle, targetSize, sourceBoundingBox, sourceSize);

            // render grid if requested
            Stream gridStream;
            if (includeGrid)
            {
                gridStream = Reprojector.DrawReprojectGrid(originalStream, targetSize, targetToSource);
                originalStream.Close();
            }
            else
            {
                gridStream = originalStream;
            }

            // suppress re-projection if requested
            Stream resultStream;
            if (reproject)
            {
                resultStream = Reprojector.Reproject(gridStream, targetSize, targetToSource);
                gridStream.Close();
            }
            else
            {
                resultStream = gridStream;
            }
                    
            return resultStream;
        }

        /// <summary>
        /// Determines the size of the source image. 
        /// This method is only used when re-projection applies.
        /// </summary>
        /// <param name="sourceBoundingBox">The source bounding box.</param>
        /// <param name="targetSize">Requested target size.</param>
        /// <returns>The computed source size.</returns>
        protected virtual Size DetermineSourceSize(IBoundingBox sourceBoundingBox, Size targetSize)
        {
            if (!ReprojectionServiceOptions.SourceSizeFactor.HasValue)
                return DetermineSourceSizeByAmountOfPixels(sourceBoundingBox, targetSize.Area());

            if (ReprojectionServiceOptions.SourceSizeFactor.Value < 0)
                return DetermineSourceSizeByAmountOfPixels(sourceBoundingBox, -ReprojectionServiceOptions.SourceSizeFactor.Value * targetSize.Area());

            var aspect = sourceBoundingBox.AspectRatio();

            var sizes = new [] {
                new SizeD(targetSize.Height*aspect, targetSize.Height),
                new SizeD(targetSize.Width*aspect, targetSize.Width), 
                new SizeD(targetSize.Width, targetSize.Width / aspect),
                new SizeD(targetSize.Height, targetSize.Height / aspect)
            };

            var min = sizes.Select(s => s.Area()).Min();
            var max = sizes.Select(s => s.Area()).Max();

            return DetermineSourceSizeByAmountOfPixels(sourceBoundingBox, min + (max - min) * ReprojectionServiceOptions.SourceSizeFactor.Value);
        }

        /// <summary>
        /// Knowing the source bounding box and its aspect ratio, this helper determines 
        /// an image size given the an amount of pixels that the image should contain.
        /// </summary>
        /// <param name="sourceBoundingBox">The source bounding box</param>
        /// <param name="nPixels">The number of pixels that the image should contain</param>
        /// <returns>The size determined out of bounding box and number of pixels, rounded to the nearest integer.</returns>
        protected virtual Size DetermineSourceSizeByAmountOfPixels(IBoundingBox sourceBoundingBox, double nPixels)
        {
            var aspectRatio = sourceBoundingBox.AspectRatio();
            var h = Math.Sqrt(nPixels/aspectRatio);
            var w = h*aspectRatio;

            return new Size((int)Math.Round(w), (int)Math.Round(h));
        }

        /// <summary>
        /// Creates the transformation function that determines the position of a 
        /// source pixel, given the position of a target pixel.
        /// </summary>
        /// <param name="targetMapRectangle">The requested target rectangle.</param>
        /// <param name="targetSize">The requested target size.</param>
        /// <param name="sourceBoundingBox">The source bounding box corresponding to the target map rectangle..</param>
        /// <param name="sourceSize">The source size corresponding to the target size.</param>
        /// <returns>Position of source pixel</returns>
        protected virtual Func<PointD, PointD> GetTransformFunction(MapRectangle targetMapRectangle, Size targetSize, IBoundingBox sourceBoundingBox, Size sourceSize)
        {
            return target =>
            {
                // knowing the map rectangle, we can compute the logical coordinate corresponding to the position of the target pixel
                var pLogicalTarget = new Location(
                    targetMapRectangle.Left + (targetMapRectangle.Right - targetMapRectangle.Left) * (target.X / (targetSize.Width - 1)),
                    targetMapRectangle.Top - (targetMapRectangle.Top - targetMapRectangle.Bottom) * (target.Y / (targetSize.Height - 1))
                );

                // transform this logical coordinate into the source CRS
                var pLogicalSource = TargetToSourceTransformation.Transform(pLogicalTarget);

                // knowing the source bounding box and its configured orientation, we can now
                // turn the logical source coordinate into the logical offsets (left and top)
                var sourceOffset = new SizeD(

                    new[] {ContentAlignment.TopLeft, ContentAlignment.BottomLeft}.Contains(SourceMapService.MinAlignment)
                        ? pLogicalSource.X - sourceBoundingBox.MinX
                        : sourceBoundingBox.MaxX - pLogicalSource.X,

                    new[] { ContentAlignment.BottomLeft, ContentAlignment.BottomRight }.Contains(SourceMapService.MinAlignment)
                        ? sourceBoundingBox.MaxY - pLogicalSource.Y
                        : pLogicalSource.Y - sourceBoundingBox.MinY
                );

                // and finally, we an turn the logical offsets into the pixel position  
                return new PointD(
                    sourceSize.Width * sourceOffset.Width / sourceBoundingBox.Size().Width,
                    sourceSize.Height * sourceOffset.Height / sourceBoundingBox.Size().Height
                );
            };
        }

        /// <inheritdoc />
        public virtual string Crs { get; }
    }
}

#pragma warning restore CS3003 // Type is not CLS-compliant
