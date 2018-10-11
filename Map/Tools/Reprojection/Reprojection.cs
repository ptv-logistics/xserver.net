// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Ptv.XServer.Controls.Map.Tools.Reprojection
{
    using PointD = System.Windows.Point;

    /// <summary> Class encapsulating the options for image re-projection. </summary>
    public class ReprojectionOptions
    {
        /// <summary> Creates and initializes an instance of ReprojectionOptions. </summary>
        /// <remarks> Sets appropriate parameter defaults. </remarks>
        public ReprojectionOptions()
        {
            InterpolationMode = InterpolationMode.Bicubic;
            BlockSize = 32;
            DegreeOfParallelism = Math.Max(Environment.ProcessorCount-2, 1);
        }

        /// <summary> Controls the interpolation mode when re-projecting pixels. </summary>
        /// <remarks> Defaults to InterpolationMode.Bicubic. </remarks>
        public InterpolationMode InterpolationMode { get; set; }

        /// <summary> The block size (width/height) for which linear interpolation applies. </summary>
        /// <remarks>
        /// Defaults to 32.
        /// <br />
        /// Mapping target to source pixels is considered an expensive operation requiring multiple transformations
        /// on both, pixel and logical coordinates. Being expensive, re-projection uses this mapping only to calculate
        /// a grid consisting of supporting points, while applying a linear interpolation for intermediate points 
        /// (point withing a grid block). The parameter blockSize determines both width and height of a single cell.
        /// </remarks>
        public int BlockSize { get; set; }

        /// <summary>
        /// Specifies, if not null and > 1, that multi-threading should be used during reprojection.
        /// </summary>
        public int? DegreeOfParallelism { get; set; }
    }

    /// <summary> An class providing methods for image re-projection. </summary>
    public class ImageReprojector
    {
        /// <summary> Creates and initializes an instance of ImageReprojector. </summary>
        /// <param name="reprojectionOptions">The options to use for re-projection. 
        /// This parameter is optional; see ReprojectionOptions for defaults.</param>
        public ImageReprojector(ReprojectionOptions reprojectionOptions = null)
        {
            ReprojectionOptions = reprojectionOptions ?? new ReprojectionOptions();
        }

        /// <summary> Accesses the options that are used for re-projection. </summary>
        public ReprojectionOptions ReprojectionOptions { get; }
        
        /// <summary> Re-projects an image given a mapping function that maps target to source pixels. </summary>
        /// <param name="stm">The stream containing the source image.</param>
        /// <param name="size">Determines the size of the resulting image.</param>
        /// <param name="targetToSource">Mapping function that maps target to source pixels.</param>
        /// <returns>The stream containing the resulting image.</returns>
        public virtual Stream Reproject(Stream stm, Size size, Func<PointD, PointD> targetToSource)
        {
            return Reproject(Image.FromStream(stm), size, targetToSource);
        }

        /// <summary> Re-projects an image given a mapping function that maps target to source pixels. </summary>
        /// <param name="image">The image to re-project.</param>
        /// <param name="size">Determines the size of the resulting image.</param>
        /// <param name="targetToSource">Mapping function that maps target to source pixels.</param>
        public virtual Stream Reproject(Image image, Size size, Func<PointD, PointD> targetToSource)
        {
            // Determine the scale to use. The scale is used to virtually inflate the target rectangle to be filled when 
            // re-projecting a single block. The scale specifies the lowest possible integer values that, multiplied with 
            // width and height of the target image, would make the target image larger than the source image. Virtually, 
            // the target image has to be larger than the source image for the interpolation filters to produce proper 
            // results.

            var scale = new Size(
                Math.Max(1, (int)Math.Ceiling((double)image.Width / size.Width)),
                Math.Max(1, (int)Math.Ceiling((double)image.Height / size.Height))
            );

            // setup source and target image
            var source = ArgbImage.FromImage(image, ReprojectionOptions.InterpolationMode);
            var target = new ArgbImage(size);

            // divide into blocks and re-project each block
            GetBlocks(size).ForEach(ReprojectionOptions.DegreeOfParallelism, block =>
            {
                Reproject(source, target, block, targetToSource, scale);
            });

            return target.Stream;
        }



        /// <summary>
        /// Renders the re-projection grid into the given image without performing the actual re-projection.
        /// Used for debugging purposes.
        /// </summary>
        /// <param name="stm">The stream containing the image to re-project.</param>
        /// <param name="size">Determines the requested image size.</param>
        /// <param name="targetToSource">Mapping function that maps target to source pixels.</param>
        /// <returns>The stream containing the resulting image.</returns>
        public virtual Stream DrawReprojectGrid(Stream stm, Size size, Func<PointD, PointD> targetToSource)
        {
            return DrawReprojectGrid(Image.FromStream(stm), size, targetToSource);
        }

        /// <summary>
        /// Renders the re-projection into the given image without performing the actual re-projection.
        /// Used for debugging purposes.
        /// </summary>
        /// <param name="image">The image to re-project.</param>
        /// <param name="size">Determines the requested image size.</param>
        /// <param name="targetToSource">Mapping function that maps target to source pixels.</param>
        /// <returns>The stream containing the resulting image.</returns>
        public virtual Stream DrawReprojectGrid(Image image, Size size, Func<PointD, PointD> targetToSource)
        {
            var bmp = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);

            using (var g = Graphics.FromImage(bmp))
            using (var p = new Pen(Color.FromArgb(32, 255, 32, 32), 2))
            {
                g.DrawImage(image, new Point(0, 0));

                foreach (var block in GetBlocks(size))
                    block.Render(g, p, targetToSource);
            }

            return StreamImage(bmp);
        }

        /// <summary> Writes the bytes of an image into a MemoryStream and returns that stream. </summary>
        /// <param name="image">Image from which a memory stream should be generated. </param>
        /// <returns> Memory stream of the image.</returns>
        protected virtual Stream StreamImage(Image image)
        {
            var mem = new MemoryStream();
            image.Save(mem, ImageFormat.Bmp);
            mem.Seek(0, SeekOrigin.Begin);
            return mem;
        }

        /// <summary> Helper that divides the target re-projection area into several blocks. </summary>
        /// <param name="size">Size of the target area.</param>
        /// <returns>The generated blocks.</returns>
        protected IEnumerable<ReprojectionBlock> GetBlocks(Size size)
        {
            var blockSize = Math.Max(1, ReprojectionOptions.BlockSize);

            // determine the number of blocks in x- and y-direction
            var nx = Math.Max(1, (size.Width + (blockSize >> 1)) / blockSize);
            var ny = Math.Max(1, (size.Height + (blockSize >> 1)) / blockSize);

            // lambda for determining an end coordinate, given the start coordinate, the size and the remainder
            Func<int, int, int, int> c1 = (startCoordinate, partialSize, remainder) => startCoordinate + partialSize + Math.Max(0, Math.Min(1, remainder)) - 1;

            // knowing the number of blocks to build, we can determine the effective block size 
            // (sx and sy below) and the remaining pixels that must be equally spread among the 
            // blocks being build (rx and ry below).
            //
            // All we need to do now to is to loop and setup the nx*ny blocks. We'll do this by keeping the 
            // block start coordinates (x0, y0) updated while using the above lambda to determine the end
            // coordinates on the fly. The remainders are just counted down, the lambda above turns them
            // into pixels.

            for (int x0 = 0, sx = size.Width / nx, rx = size.Width % nx, ix = 0; ix < nx; ++ix, x0 = c1(x0, sx, rx) + 1, --rx)
                for (int y0 = 0, sy = size.Height / ny, ry = size.Height % ny, iy = 0; iy < ny; ++iy, y0 = c1(y0, sy, ry) + 1, --ry)
                    yield return new ReprojectionBlock(x0, y0, c1(x0, sx, rx), c1(y0, sy, ry));
        }

        /// <summary>
        /// Performs a linear re-projection into the specified target block.
        /// <br/>
        /// Uses the given mapping function only to determine the corresponding corner points in the source image, 
        /// uses a linear interpolation to determine intermediate points.
        /// </summary>
        /// <param name="source">The image to be re-projected.</param>
        /// <param name="target">The resulting image to be filled.</param>
        /// <param name="block">The block being targeted.</param>
        /// <param name="transformTargetToSource">Mapping function that maps target to source pixels.</param>
        /// <param name="scale">A factor to apply when re-projecting the block. See code comments in Reproject method above, where this parameter is set up.</param>
        protected virtual void Reproject(ArgbImage source, ArgbImage target, ReprojectionBlock block, Func<PointD, PointD> transformTargetToSource, Size scale)
        {
            try
            {
                if (scale.Width != 1)
                    ScaledReprojection(source, target, block, transformTargetToSource, scale);
                else
                {
                    if (scale.Height == 1)
                        // use optimized reprojection when scale values are both set to "1" (= unscaled)
                        UnscaledReprojection(source, target, block, transformTargetToSource);
                    else
                        // use optimized reprojection when scale.Width is set to "1" (vertical scaling)
                        VerticallyScaledReprojection(source, target, block, transformTargetToSource, scale.Height);
                }
            }
            catch (Components.Projections.TransformationException)
            {
                // ignore transformation exceptions; the given block will simply remain empty.
                //
                // seen transformation exceptions to happen in Proj4.cs with the transformation returning error code -14.
                // This is probably caused by coordinates exceeding the limits; See
                // 
                // proj-4.8.0\src\pj_strerrno.c
                //   ...
                //   "latitude or longitude exceeded limits",	/* -14 */
                //   ...
                //
                // proj-4.8.0\src\pj_transform.c
                //   ...
                //   if( pj_Convert_Geodetic_To_Geocentric( &gi, y[io], x[io], z[io], x + io, y + io, z + io ) != 0 ) {
                //     ret_errno = -14;
                //   ...
            }
        }

        /// <summary>
        /// Simple internal data structure for storing coordinates and increments 
        /// as needed by InterpolateBlock for interpolating pixel coordinates.
        /// </summary>
        private struct InterpolationData
        {
            /// <summary>
            /// x-coordinate of the current position
            /// </summary>
            public double x;
            
            /// <summary>
            /// y-coordinate of the current position
            /// </summary>
            public double y;

            /// <summary>
            /// x-increment for stepping
            /// </summary>
            private double xi;

            /// <summary>
            /// y-increment for stepping
            /// </summary>
            private double yi;

            /// <summary>
            /// Operator used for stepping the interpolated coordinates.
            /// </summary>
            /// <param name="data">InterpolationData instance to step.</param>
            /// <returns>The very same InterpolationData instance given in the call to the operator.</returns>
            /// <remarks>
            /// This operator does not create and step a copy of the given InterpolationData - TODO: should it?
            /// </remarks>
            public static InterpolationData operator ++ (InterpolationData data)
            {
                data.x += data.xi;
                data.y += data.yi;

                return data;
            }

            /// <summary>
            /// Creates and initializes an instance of InterpolationData given a start and end position and 
            /// the number of steps to be taken to interpolate coordinates between them.
            /// </summary>
            /// <param name="a">An InterpolationData instance whose current position is used as the start position.</param>
            /// <param name="b">An InterpolationData instance whose current position is used as the end position.</param>
            /// <param name="n">The number of steps.</param>
            /// <returns>InterpolationData instance.</returns>
            public static InterpolationData Create(InterpolationData a, InterpolationData b, int n)
            {
                return new InterpolationData { x = a.x, xi = (b.x - a.x) / n, y = a.y, yi = (b.y - a.y) / n };
            }

            /// <summary>
            /// Creates and initializes an instance of InterpolationData given a start and end position and 
            /// the number of steps to be taken to interpolate coordinates between them.
            /// </summary>
            /// <param name="a">Start coordinate.</param>
            /// <param name="b">End coordinate.</param>
            /// <param name="n">The number of steps.</param>
            /// <returns>InterpolationData instance.</returns>
            public static InterpolationData Create(PointD a, PointD b, int n)
            {
                return new InterpolationData { x = a.X, xi = (b.X - a.X) / n, y = a.Y, yi = (b.Y - a.Y) / n };
            }
        }

        /// <summary>
        /// Performs a unscaled linear re-projection into the specified target block.
        /// <br/>
        /// Uses the given mapping function only to determine the corresponding corner points in the source image, 
        /// uses a linear interpolation to determine intermediate points.
        /// </summary>
        /// <param name="source">The image to be re-projected.</param>
        /// <param name="target">The resulting image to be filled.</param>
        /// <param name="block">The block being targeted.</param>
        /// <param name="transformTargetToSource">Mapping function that maps target to source pixels.</param>
        private static void UnscaledReprojection(ArgbImage source, ArgbImage target, ReprojectionBlock block, Func<PointD, PointD> transformTargetToSource)
        {
            // Interpolators for upper and lower line of block
            var upper = InterpolationData.Create(transformTargetToSource(block.LeftTop), transformTargetToSource(block.RightTop), block.X1 - block.X0);
            var lower = InterpolationData.Create(transformTargetToSource(block.LeftBottom), transformTargetToSource(block.RightBottom), block.X1 - block.X0);

            for (var x = block.X0; x <= block.X1; ++x, ++upper, ++lower)
            {
                // interpolator for points on the current line defined through upper and lower
                var sourcePoint = InterpolationData.Create(upper, lower, block.Y1 - block.Y0);

                for (var y = block.Y0; y <= block.Y1; ++y, ++sourcePoint)
                    target[x, y] = source[sourcePoint.x, sourcePoint.y];
            }
        }

        /// <summary>
        /// Performs a scaled linear re-projection into the specified target block.
        /// <br/>
        /// Uses the given mapping function only to determine the corresponding corner points in the source image, 
        /// uses a linear interpolation to determine intermediate points.
        /// </summary>
        /// <param name="source">The image to be re-projected.</param>
        /// <param name="target">The resulting image to be filled.</param>
        /// <param name="block">The block being targeted.</param>
        /// <param name="transformTargetToSource">Mapping function that maps target to source pixels.</param>
        /// <param name="scaleY">A factor to apply when re-projecting the block. See code comments in Reproject method above, where this parameter is set up.</param>
        private static void VerticallyScaledReprojection(ArgbImage source, ArgbImage target, ReprojectionBlock block, Func<PointD, PointD> transformTargetToSource, int scaleY)
        {
            // determine the number of sections of the scaled block
            var nx = (block.X1 - block.X0);
            var ny = (block.Y1 - block.Y0 + 1) * scaleY - 1;

            // Interpolators for upper and lower line of block
            var upper = InterpolationData.Create(transformTargetToSource(block.LeftTop), transformTargetToSource(block.RightTop), nx);
            var lower = InterpolationData.Create(transformTargetToSource(block.LeftBottom), transformTargetToSource(block.RightBottom), nx);

            // total number of color components collected in a color block due to scaling
            var colorBlockSize = (uint)scaleY;

            for (var x = block.X0; x <= block.X1; ++x, ++upper, ++lower)
            {
                // setup interpolator for interpolating points on the line defined through upper and lower

                var sourcePoint = InterpolationData.Create(upper, lower, ny);

                for (var y = block.Y0; y <= block.Y1; ++y)
                {
                    // storage for color components 
                    uint a, r, g, b;
                    a = r = g = b = colorBlockSize >> 2;

                    // collect color components of sub pixels. 
                    // In the inner loop, we'll step our sourcePoint interpolators.
                    for (var ysub = 0; ysub < scaleY; ++ysub, ++sourcePoint)
                    {
                        var color = source[sourcePoint.x, sourcePoint.y];

                        a += (color >> 24) & 0xff;
                        r += (color >> 16) & 0xff;
                        g += (color >> 8) & 0xff;
                        b += (color) & 0xff;
                    }

                    // average the collected color components and set the target pixel
                    target[x, y] =
                        ((a / colorBlockSize) << 24) |
                        ((r / colorBlockSize) << 16) |
                        ((g / colorBlockSize) << 8) |
                        ((b / colorBlockSize));
                }
            }
        }


        /// <summary>
        /// Performs a scaled linear re-projection into the specified target block.
        /// <br/>
        /// Uses the given mapping function only to determine the corresponding corner points in the source image, 
        /// uses a linear interpolation to determine intermediate points.
        /// </summary>
        /// <param name="source">The image to be re-projected.</param>
        /// <param name="target">The resulting image to be filled.</param>
        /// <param name="block">The block being targeted.</param>
        /// <param name="transformTargetToSource">Mapping function that maps target to source pixels.</param>
        /// <param name="scale">A factor to apply when re-projecting the block. See code comments in Reproject method above, where this parameter is set up.</param>
        private static void ScaledReprojection(ArgbImage source, ArgbImage target, ReprojectionBlock block, Func<PointD, PointD> transformTargetToSource, Size scale)
        {
            // determine the number of sections of the scaled block
            var nx = (block.X1 - block.X0 + 1)*scale.Width - 1;
            var ny = (block.Y1 - block.Y0 + 1)*scale.Height - 1;

            // Interpolators for upper and lower line of block
            var upper = InterpolationData.Create(transformTargetToSource(block.LeftTop), transformTargetToSource(block.RightTop), nx);
            var lower = InterpolationData.Create(transformTargetToSource(block.LeftBottom), transformTargetToSource(block.RightBottom), nx);

            // total number of color components collected in a color block due to scaling
            var colorBlockSize = (uint)(scale.Width * scale.Height);

            for (var x = block.X0; x <= block.X1; ++x)
            {
                // setup scale.Width interpolators for interpolating points on the line 
                // defined through upper+n and lower+n, with n=0..(scale.Width-1)

                var sourcePoint = new InterpolationData[scale.Width];

                for (var xsub = 0; xsub < scale.Width; ++xsub)
                    sourcePoint[xsub] = InterpolationData.Create(upper++, lower++, ny);

                for (var y = block.Y0; y <= block.Y1; ++y)
                {
                    // storage for color components 
                    uint a, r, g, b;
                    a = r = g = b = colorBlockSize >> 2;

                    // collect color components of subpixels. 
                    // In the inner loop, we'll step our sourcePoint interpolators.
                    for (var ysub = 0; ysub < scale.Height; ++ysub)
                        for (var xsub = 0; xsub < scale.Width; ++sourcePoint[xsub], ++xsub)
                        {
                            var color = source[sourcePoint[xsub].x, sourcePoint[xsub].y];

                            a += (color >> 24) & 0xff;
                            r += (color >> 16) & 0xff;
                            g += (color  >> 8) & 0xff;
                            b += (color      ) & 0xff;
                        }

                    // average the collected color components and set the target pixel
                    target[x, y] =
                        ((a / colorBlockSize) << 24) |
                        ((r / colorBlockSize) << 16) |
                        ((g / colorBlockSize) <<  8) |
                        ((b / colorBlockSize)      );
                }
            }
        }
    }

    /// <summary> Encapsulates the coordinates of a target block in image re-projection. </summary>
    /// <remarks>
    /// In re-projection, this structure is easier to use than .NET's rectangle class, 
    /// offering only (x0|y0) along with a width and a height.
    /// </remarks>
    public struct ReprojectionBlock
    {
        /// <summary>marks the left</summary>
        public readonly int X0;

        /// <summary>marks the top</summary>
        public readonly int Y0;

        /// <summary>marks the right</summary>
        public readonly int X1;

        /// <summary>marks the bottom</summary>
        public readonly int Y1;

        /// <summary>
        /// Creates and initializes the ReprojectionBlock.
        /// </summary>
        /// <param name="x0">x-coordinate, marks the left</param>
        /// <param name="y0">y-coordinate, marks the top</param>
        /// <param name="x1">x-coordinate, marks the right</param>
        /// <param name="y1">y-coordinate, marks the bottom</param>
        public ReprojectionBlock(int x0, int y0, int x1, int y1) { X0 = x0; Y0 = y0; X1 = x1; Y1 = y1; }

        /// <summary>Point (left|top)</summary>
        public PointD LeftTop => new PointD(X0, Y0);

        /// <summary>Point (right|top)</summary>
        public PointD RightTop => new PointD(X1, Y0);

        /// <summary>Point (left|bottom)</summary>
        public PointD LeftBottom => new PointD(X0, Y1);

        /// <summary>Point (right|bottom)</summary>
        public PointD RightBottom => new PointD(X1, Y1);

        /// <summary>
        /// Renders the block using the given pen. Used for debugging purposes.
        /// </summary>
        /// <param name="g">Graphics object to render into.</param>
        /// <param name="p">Pen to use.</param>
        /// <param name="t">A transformation function to call with each point before drawing the actual lines.</param>
        public void Render(Graphics g, Pen p, Func<PointD, PointD> t)
        {
            Func<PointD, PointF> pointF = pointD => new PointF((float) pointD.X, (float) pointD.Y);

            g.DrawLines(p, new[] { LeftTop, RightTop, RightBottom, LeftBottom, LeftTop }.Select(pointF).ToArray());
        }
    };
}
