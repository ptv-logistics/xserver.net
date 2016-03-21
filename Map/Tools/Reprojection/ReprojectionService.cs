using System;
using System.Drawing;
using System.IO;
using System.Linq;
using Ptv.Components.Projections;

namespace Ptv.XServer.Controls.Map.Tools.Reprojection
{
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
        public ReprojectionServiceOptions ReprojectionServiceOptions { get; private set; }

        /// <summary> Accesses the re-projector used by the map service. </summary>
        public ImageReprojector Reprojector { get; private set; }
        
        /// <summary>
        /// Generates a line string, given the start and end location and the number of points to generate.
        /// This helper is used by DetermineSourceRectangle. Coordinates are expected in the target CRS, 
        /// the generated line string is transformed into the source CRS.
        /// </summary>
        /// <param name="n">Number of equidistant points to generate, at least two.</param>
        /// <param name="x0">x-coordinate of start point</param>
        /// <param name="y0">y-coordinate of start point</param>
        /// <param name="x1">x-coordinate of end point</param>
        /// <param name="y1">y-coordinate of end point</param>
        /// <returns>Generated line string, transformed into source CRS.</returns>
        protected virtual Location[] MakeLineString(int n, double x0, double y0, double x1, double y1)
        {
            // create interpolator
            var pointInterpolator = new PointInterpolator(x0, y0, x1, y1, n - 1);

            // create locations using interpolator
            var locations = Enumerable.Range(0, n)
                .Select(index => pointInterpolator[index])
                .Select(point => new Location(point.X, point.Y))
                .ToArray();

            // transform into source CRS.
            return TargetToSourceTransformation.Transform(locations);
        }

        /// <summary>
        /// This method determines the initial source bounds out of the target rectangle, knowing both, 
        /// source and the target CRS. The source bounds at this stage may not be rectangular and are 
        /// therefore represented by four line strings, one at each side (order: left, top, right, bottom).
        /// </summary>
        /// <param name="rc">The targeted map rectangle.</param>
        /// <returns>Transformed rectangle represented by four line strings. </returns>
        protected virtual Location[][] DetermineSourceBounds(MapRectangle rc)
        {
            var n = ReprojectionServiceOptions.SourceBoundsSupportingPoints;

            return new[]
            {
                MakeLineString(n, rc.Left, rc.Top, rc.Left, rc.Bottom),
                MakeLineString(n, rc.Left, rc.Top, rc.Right, rc.Top),
                MakeLineString(n, rc.Right, rc.Top, rc.Right, rc.Bottom),
                MakeLineString(n, rc.Left, rc.Bottom, rc.Right, rc.Bottom)
            };
        }

        /// <summary> Checks if bounds, represented by four line strings, are rectangular. </summary>
        /// <param name="bounds">Bounds to check</param>
        /// <returns>True, if the bounds are rectangular. False otherwise.</returns>
        protected virtual bool IsRectangular(Location[][] bounds)
        {
            // Function that checks if a set of locations share a common coordinate, 
            // given the function that selects the coordinate of the location.
            Func<int, Func<Location, double>, bool> shareCoordinate = (boundsIndex, selectCoordinate) =>
            {
                var coordinates = bounds[boundsIndex].Select(selectCoordinate).ToArray();
                return coordinates.All(c => Math.Abs(coordinates[0] - c) < 1e-6);
            };

            // check each side
            return shareCoordinate(0, loc => loc.X) && shareCoordinate(1, loc => loc.Y) && shareCoordinate(2, loc => loc.X) && shareCoordinate(3, loc => loc.Y);
        }

        /// <inheritdoc />
        public virtual Stream GetImageStream(MapRectangle targetMapRectangle, Size targetSize)
        {
            return GetImageStream(targetMapRectangle, targetSize, true, false);
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
            var sourceBounds = DetermineSourceBounds(targetMapRectangle);

            MapRectangle sourceBoundingBox;

            // we don't need to re-project if the source bounds are rectangular and have the same aspect ratio
            // as the target rectangle. In this case, we can also use the target size as the source size.
            if (IsRectangular(sourceBounds))
            {
                sourceBoundingBox = new MapRectangle(new[] { sourceBounds.First().First() /* lt */, sourceBounds.Last().Last() /* rb */ });

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

            // The source bounding box is calculated using the minimum and maximum x- and y-coordinates 
            // from sourceBounds, adding an additional buffer to be on the safe side.
            sourceBoundingBox = new MapRectangle(sourceBounds.SelectMany(p => p)).Resize(1.025);

            // For details on the source size, please refer to DetermineSourceSize.
            var sourceSize = DetermineSourceSize(sourceBoundingBox, targetSize);

            // Get original stream
            var originalStream = SourceMapService.GetImageStream(sourceBoundingBox, sourceSize);

            // depending on the parameterization we can directly pass through inner image
            if (!reproject && !includeGrid)
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
                new SizeF((float)(targetSize.Height*aspect), targetSize.Height),
                new SizeF((float)(targetSize.Width*aspect), targetSize.Width), 
                new SizeF(targetSize.Width, (float)(targetSize.Width / aspect)),
                new SizeF(targetSize.Height, (float)(targetSize.Height / aspect))
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
        protected virtual Func<PointF, PointF> GetTransformFunction(MapRectangle targetMapRectangle, Size targetSize, IBoundingBox sourceBoundingBox, Size sourceSize)
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
                var sourceOffset = new SizeF(

                    new[] {ContentAlignment.TopLeft, ContentAlignment.BottomLeft}.Contains(SourceMapService.MinAlignment)
                        ? (float)(pLogicalSource.X - sourceBoundingBox.MinX)
                        : (float)(sourceBoundingBox.MaxX - pLogicalSource.X),

                    new[] { ContentAlignment.BottomLeft, ContentAlignment.BottomRight }.Contains(SourceMapService.MinAlignment)
                        ? (float)(sourceBoundingBox.MaxY - pLogicalSource.Y)
                        : (float)(pLogicalSource.Y - sourceBoundingBox.MinY)
                );

                // and finally, we an turn the logical offsets into the pixel position  
                return new PointF(
                    sourceSize.Width * sourceOffset.Width / sourceBoundingBox.Size().Width,
                    sourceSize.Height * sourceOffset.Height / sourceBoundingBox.Size().Height
                );
            };
        }

        /// <inheritdoc />
        public virtual string Crs {get; private set; }
    }
}

