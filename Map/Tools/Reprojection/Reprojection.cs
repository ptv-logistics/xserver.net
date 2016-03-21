using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;


namespace Ptv.XServer.Controls.Map.Tools.Reprojection
{
    /// <summary> Class encapsulating the options for image re-projection. </summary>
    public class ReprojectionOptions
    {
        /// <summary> Creates and initializes an instance of ReprojectionOptions. </summary>
        /// <remarks> Sets appropriate parameter defaults. </remarks>
        public ReprojectionOptions()
        {
            InterpolationMode = InterpolationMode.Bicubic;
            BlockSize = 32;
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
        public ReprojectionOptions ReprojectionOptions { get; private set; }
        
        /// <summary> Re-projects an image given a mapping function that maps target to source pixels. </summary>
        /// <param name="stm">The stream containing the source image.</param>
        /// <param name="size">Determines the size of the resulting image.</param>
        /// <param name="targetToSource">Mapping function that maps target to source pixels.</param>
        /// <returns>The stream containing the resulting image.</returns>
        public virtual Stream Reproject(Stream stm, Size size, Func<PointF, PointF> targetToSource)
        {
            return Reproject(Image.FromStream(stm), size, targetToSource);
        }

        /// <summary> Re-projects an image given a mapping function that maps target to source pixels. </summary>
        /// <param name="image">The image to re-project.</param>
        /// <param name="size">Determines the size of the resulting image.</param>
        /// <param name="targetToSource">Mapping function that maps target to source pixels.</param>
        public virtual Stream Reproject(Image image, Size size, Func<PointF, PointF> targetToSource)
        {
            // Determine the scale to use. The scale is used to virtually inflate the target rectangle to be filled when 
            // re-projecting a single block. The scale specifies the lowest possible integer values that, multiplied with 
            // width and height of the target image, would make the target image larger than the source image. Virtually, 
            // the target image has to be larger than the source image for the interpolation filters to produce proper 
            // results.

            var scale = new Size(
                Math.Max(1, (int)Math.Ceiling((float)image.Width / size.Width)),
                Math.Max(1, (int)Math.Ceiling((float)image.Height / size.Height))
            );

            // setup source and target image
            var source = ArgbImage.FromImage(image, ReprojectionOptions.InterpolationMode);
            var target = new ArgbImage(size);

            foreach (var block in GetBlocks(size))
                Reproject(source, target, block, targetToSource, scale);

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
        public virtual Stream DrawReprojectGrid(Stream stm, Size size, Func<PointF, PointF> targetToSource)
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
        public virtual Stream DrawReprojectGrid(Image image, Size size, Func<PointF, PointF> targetToSource)
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
        protected virtual IEnumerable<ReprojectionBlock> GetBlocks(Size size)
        {
            var blockSize = Math.Max(1, ReprojectionOptions.BlockSize);

            // determine the number of blocks in x- and y-direction
            var nx = Math.Max(1, (size.Width + (blockSize >> 1)) / blockSize);
            var ny = Math.Max(1, (size.Height + (blockSize >> 1)) / blockSize);

            // lambda for determining an end coordinate, given the start coordinate, the size and the remainder
            Func<int, int, int, int> c1 = (c0, sz, rmndr) => c0 + sz + Math.Max(0, Math.Min(1, rmndr)) - 1;

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
        protected virtual void Reproject(ArgbImage source, ArgbImage target, ReprojectionBlock block, Func<PointF, PointF> transformTargetToSource, Size scale)
        {
            // determine the number of sections of the scaled block
            var sections = new {
                nx = (block.X1 - block.X0 + 1) * scale.Width - 1,
                ny = (block.Y1 - block.Y0 + 1) * scale.Height - 1
            };

            // setup interpolators for determining source points on top and bottom boundary. 
            // Use the mapping function for determining the required source points
            var topInterpolator = new PointInterpolator(block.LeftTop, block.RightTop, transformTargetToSource, sections.nx);
            var bottomInterpolator = new PointInterpolator(block.LeftBottom, block.RightBottom, transformTargetToSource, sections.nx);

            // set up a temporary block storing the "scaled pixels" 
            // These will be averaged into the target pixels below.
            var temporaryBlock = new uint[scale.Width * scale.Height];

            // cache for source point interpolators of a temporary block
            var sourcePointInterpolators = new PointInterpolator[scale.Width];

            // loop through the columns of temporary blocks
            for (int column = 0, targetX = block.X0; column <= sections.nx; column += scale.Width, ++targetX)
            {
                // in the following loop, we'll pre-calculate and cache the
                // source point interpolators for the current column

                for (var columnOffset = 0; columnOffset < scale.Width; ++columnOffset)
                    sourcePointInterpolators[columnOffset] = new PointInterpolator(
                        topInterpolator[column + columnOffset],
                        bottomInterpolator[column + columnOffset],
                        sections.ny
                    );

                // loop through the temporary block cells of the current column (row-wise)
                for (int row = 0, targetY = block.Y0; row <= sections.ny; row += scale.Height, ++targetY)
                {
                    // process a single temporary block
                    for (int columnOffset = 0, blockIndex = 0; columnOffset < scale.Width; ++columnOffset)
                        for (var rowOffset = 0; rowOffset < scale.Height; ++rowOffset, ++blockIndex)
                        {
                            // interpolate point in source image, then get and cache color for the current source point
                            temporaryBlock[blockIndex] = source[sourcePointInterpolators[columnOffset][row + rowOffset]];
                        }

                    // average the collected colors of the temporary block into the target pixel
                    target[targetX, targetY] = Average(temporaryBlock);
                }
            }
        }

        /// <summary>
        /// Averages the color of pixels given in a 32bppArgb buffer, returning the resulting color.
        /// </summary>
        /// <param name="pixels">Buffer containing the pixels.</param>
        /// <returns>Averaged color</returns>
        protected virtual uint Average(uint[] pixels)
        {
            uint color = 0;

            // loop the four color components
            for (int componentOffset = 0, componentMask = 0; componentOffset < 4; ++componentOffset, componentMask += 8)
                // average up the values for the current color component
                color |= Math.Min(255, (uint)Math.Round(
                    pixels.Average(pixel => (double)((pixel >> componentMask) & 0x000000ff)))
                ) << componentMask;

            return color;
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
        public PointF LeftTop { get { return new PointF(X0, Y0); } }

        /// <summary>Point (right|top)</summary>
        public PointF RightTop { get { return new PointF(X1, Y0); } }

        /// <summary>Point (left|bottom)</summary>
        public PointF LeftBottom { get { return new PointF(X0, Y1); } }

        /// <summary>Point (right|bottom)</summary>
        public PointF RightBottom { get { return new PointF(X1, Y1); } }

        /// <summary>
        /// Renders the block using the given pen. Used for debugging purposes.
        /// </summary>
        /// <param name="g">Graphics object to render into.</param>
        /// <param name="p">Pen to use.</param>
        /// <param name="t">A transformation function to call with each point before drawing the actual lines.</param>
        public void Render(Graphics g, Pen p, Func<PointF, PointF> t)
        {
            g.DrawLines(p, new[] { LeftTop, RightTop, RightBottom, LeftBottom, LeftTop }.Select(t).ToArray());
        }
    };

    /// <summary>
    /// Utility class for interpolating points on a line p0 > p1 in n steps. Used in image re-projecting below.
    /// </summary>
    internal struct PointInterpolator
    {
        /// <summary> start point </summary>
        private PointF p0;

        /// <summary> increment for calculating intermediate points </summary>
        private SizeF increment;

        /// <summary> Creates and initializes a PointInterpolator. </summary>
        /// <param name="x0">x-coordinate of the start point</param>
        /// <param name="y0">y-coordinate of the start point</param>
        /// <param name="x1">x-coordinate of the end point</param>
        /// <param name="y1">y-coordinate of the end point</param>
        /// <param name="n">Number steps</param>
        public PointInterpolator(double x0, double y0, double x1, double y1, int n)
            : this(new PointF((float)x0, (float)y0), new PointF((float)x1, (float)y1), n) { }

        /// <summary>
        /// Creates and initializes a PointInterpolator.
        /// </summary>
        /// <param name="p0">Start point</param>
        /// <param name="p1">End point</param>
        /// <param name="n">Number steps</param>
        public PointInterpolator(PointF p0, PointF p1, int n)
        {
            this.p0 = p0;

            increment = n == 0
                ? new SizeF()
                : new SizeF(
                    (p1.X - p0.X) / n,
                    (p1.Y - p0.Y) / n
                );
        }

        /// <summary> Creates and initializes a PointInterpolator. </summary>
        /// <param name="p0">Start point</param>
        /// <param name="p1">End point</param>
        /// <param name="t">Transformation function to apply on the points.</param>
        /// <param name="n">Number steps</param>
        public PointInterpolator(PointF p0, PointF p1, Func<PointF, PointF> t, int n) : this(t(p0), t(p1), n)
        {
        }

        /// <summary> Interpolates a point given its index. </summary>
        /// <param name="index">Point index</param>
        /// <returns>Interpolated point</returns>
        public PointF this[int index]
        {
            get { return new PointF(p0.X + increment.Width * index, p0.Y + increment.Height * index); }
        }
    }
}
