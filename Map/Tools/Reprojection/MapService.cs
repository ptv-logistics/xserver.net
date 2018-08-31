// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using Ptv.Components.Projections;

namespace Ptv.XServer.Controls.Map.Tools.Reprojection
{
    using SizeD = System.Windows.Size;

    /// <summary>
    /// Represents the bounding box of an object.
    /// </summary>
    public interface IBoundingBox
    {
        /// <summary> Returns the minimum x-coordinate. </summary>
        double MinX { get; }

        /// <summary> Returns the minimum y-coordinate. </summary>
        double MinY { get; }

        /// <summary> Returns the maximum x-coordinate. </summary>
        double MaxX { get; }

        /// <summary> Returns the maximum y-coordinate. </summary>
        double MaxY { get; }
    }

    /// <summary>
    /// Encapsulates a rectangle. Extends the bounding box with an orientation by defining 
    /// the bounds of an object through the properties Left, Top, Right and Bottom.    
    /// </summary>
    public struct MapRectangle : IBoundingBox
    {
        /// <summary> x-coordinate of the left hand side. </summary>
        public double Left;

        /// <summary> y-coordinate at the top. </summary>
        public double Top;

        /// <summary> x-coordinate of the right hand side. </summary>
        public double Right;

        /// <summary> y-coordinate at the bottom </summary>
        public double Bottom;

        /// <summary> Creates and initializes a MapRectangle instance. </summary>
        /// <param name="left">x-coordinate of the left hand side.</param>
        /// <param name="top">y-coordinate at the top</param>
        /// <param name="right">x-coordinate of the right hand side</param>
        /// <param name="bottom">y-coordinate at the bottom</param>
        public MapRectangle(double left, double top, double right, double bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        /// <summary> Creates and initializes a MapRectangle instance. </summary>
        /// <param name="lt">Upper left-hand corner.</param>
        /// <param name="rb">Bottom right-hand corner.</param>
        public MapRectangle(Location lt, Location rb)
        {
            Left = lt.X;
            Top = lt.Y;
            Right = rb.X;
            Bottom = rb.Y;
        }

        /// <summary>
        /// Creates and initializes a MapRectangle instance.
        /// </summary>
        /// <param name="locations">The set of locations out of which the bounding box that initializes this rectangle will be built.</param>
        /// <remarks>For setting up the rectangle it is assumed that Left&lt;=Right and Bottom&lt;=Top.</remarks>
        public MapRectangle(IEnumerable<Location> locations)
        {
            var a = locations.ToArray();

            Left = a.Min(l => l.X);
            Top = a.Max(l => l.Y);
            Right = a.Max(l => l.X);
            Bottom = a.Min(l => l.Y);
        }
        
        /// <inheritdoc />
        public double MinX => Math.Min(Left, Right);

        /// <inheritdoc />
        public double MinY => Math.Min(Top, Bottom);

        /// <inheritdoc />
        public double MaxX => Math.Max(Left, Right);

        /// <inheritdoc />
        public double MaxY => Math.Max(Top, Bottom);

        /// <summary>
        /// Resizes the area covered by the rectangle. Creates and returns a new rectangle reflecting the changes.
        /// </summary>
        /// <param name="f">Resize factor; e.g. 1.05 will resize the area by 5%.</param>
        /// <returns>Rectangle reflecting the changes.</returns>
        public MapRectangle Resize(double f)
        {
            f = (Math.Sqrt(f) - 1) / 2.0;

            var dx = (Right - Left) * f;
            var dy = (Top - Bottom) * f;

            return new MapRectangle(Left - dx, Top + dy, Right + dx, Bottom - dy);
        }
    }

    /// <summary> Base interface of a service that delivers map images. </summary>
    public interface IMapService<in T> where T : IBoundingBox
    {
        /// <summary> Defines the CRS to be used when requesting map images. </summary>
        string Crs { get; }

        /// <summary> Loads a map image using the specified bounding box and size. </summary>
        /// <param name="boundingBox">Requested bounding box, based on the CRS defined by this class.</param>
        /// <param name="size">Requested image size in pixels.</param>
        /// <returns> The stream containing the map image. </returns>
        Stream GetImageStream(T boundingBox, Size size);
    }

    /// <summary> Extended base interface of a service that delivers map images. </summary>
    public interface IExtendedMapService<in T> : IMapService<T> where T : IBoundingBox
    {
        /// <summary> Loads a map image using the specified bounding box and size. </summary>
        /// <param name="boundingBox">Requested bounding box, based on the CRS defined by this class.</param>
        /// <param name="requestedSize">Requested image size in pixels.</param>
        /// <param name="effectiveSize">Image size effectively rendered.</param>
        /// <returns>The stream containing the map image. </returns>
        Stream GetImageStream(T boundingBox, Size requestedSize, out Size effectiveSize);

        /// <summary>
        /// Optionally defines the limits of a MapService in PTV Mercator (EPSG:76131).
        /// </summary>
        /// <remarks>
        /// An approximated bounding box can be determined using the TransformBoundingBox and ApproximateBoundingBox 
        /// extensions methods provided of Ptv.XServer.Controls.Map.Tools.Reprojection.MapServiceExtensions.
        /// </remarks>
        IBoundingBox Limits { get; }
    }

    /// <summary>
    /// Class encapsulating a service that delivers map images.
    /// </summary>
    public abstract class MapService : IExtendedMapService<IBoundingBox>
    {
        /// <summary> Constructor of the abstract base class which encapsulates a service that delivers map images. </summary>
        protected MapService()
        {
            MinAlignment = ContentAlignment.BottomLeft;
        }
        /// <inheritdoc />
        public string Crs { get; protected set; }

        /// <inheritdoc />
        public virtual Stream GetImageStream(IBoundingBox boundingBox, Size size)
        {
            var stm = GetImageStream(boundingBox, size, out var effectiveSize);

            if (stm == null || effectiveSize == size)
                return stm;

            try
            {
                using (var src = Image.FromStream(stm))
                using (var bitmap = new Bitmap(size.Width, size.Height))
                using (var g = Graphics.FromImage(bitmap))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(src, new Rectangle(0, 0, bitmap.Width, bitmap.Height), new Rectangle(0, 0, src.Width, src.Height), GraphicsUnit.Pixel);

                    var memoryStream = new MemoryStream();
                    bitmap.Save(memoryStream, ImageFormat.Png);

                    memoryStream.Seek(0, SeekOrigin.Begin);

                    return memoryStream;
                }
            }
            finally
            {
                stm.Close();
            }
        }

        /// <inheritdoc />
        public abstract Stream GetImageStream(IBoundingBox boundingBox, Size requestedSize, out Size effectiveSize);

        /// <summary>
        /// Defines the position of (MinX|MinY) in images returned by this service.
        /// </summary>
        /// <remarks>
        /// The alignment is a required parameter when resulting map images must be re-projected. Usually the alignment will 
        /// be ContentAlignment.BottomLeft, as many rectangles follow the rules "Left &lt; Right" and "Top &gt; Bottom".  
        /// ContentAlignment.BottomLeft is also the value returned by the default implementation and you usually do not need 
        /// to change that.
        /// <br/>
        /// Unfortunately the alignment cannot be determined dynamically on the client side without knowing how services 
        /// actually map logical coordinates to pixels. Without services returning the actually rendered map rectangle in 
        /// their response (as PTV xMap Server e.g. does, as WMS services don't), this value must explicitly be configured 
        /// for re-projections to work properly.
        /// </remarks>
        public ContentAlignment MinAlignment { get; protected set; }

        /// <inheritdoc/>
        public virtual IBoundingBox Limits => null;
    }

    /// <summary>
    /// Static class providing some extensions.
    /// </summary>
    public static class MapServiceExtensions
    {
        /// <summary> Determines the aspect ratio, given a SizeF. </summary>
        /// <param name="size">Size out of which to determine the aspect ratio.</param>
        /// <returns>Aspect ratio</returns>
        public static double AspectRatio(this SizeF size) { return size.Width / size.Height; }

        /// <summary> Determines the aspect ratio, given a bounding box. </summary>
        /// <param name="box">Bounding box out of which to determine the aspect ratio.</param>
        /// <returns>Aspect ratio.</returns>
        public static double AspectRatio(this IBoundingBox box) { return (box.MaxX - box.MinX) / (box.MaxY - box.MinY); }

        /// <summary> Determines the size of a bounding box. </summary>
        /// <param name="box">Bounding box to determine the size of.</param>
        /// <returns>Size of the bounding box.</returns>
        public static SizeF Size(this IBoundingBox box)
        {
            return new SizeF((float)(box.MaxX - box.MinX), (float)(box.MaxY - box.MinY));
        }

        /// <summary> Computes the area, given a Size object. </summary>
        /// <param name="size">Size out of which to compute the area.</param>
        /// <returns>Computed area.</returns>
        public static double Area(this SizeD size) { return size.Width * size.Height; }

        /// <summary> Computes the area, given a Size object. </summary>
        /// <param name="size">Size out of which to compute the area.</param>
        /// <returns>Computed area.</returns>
        public static double Area(this Size size) { return size.Width * size.Height; }

        /// <summary> Computes the area, given a SizeF object. </summary>
        /// <param name="size">Size out of which to compute the area.</param>
        /// <returns>Computed area.</returns>
        public static double Area(this SizeF size) { return size.Width * size.Height; }

        /// <summary> Determines if two bounding boxes have the same aspect ratio. </summary>
        /// <param name="this">First bounding box.</param>
        /// <param name="other">Second bounding box.</param>
        /// <returns>True, if the aspect ratio is equal. False otherwise.</returns>
        public static bool EqualsAspect(this IBoundingBox @this, IBoundingBox other)
        {
            return Math.Abs(@this.AspectRatio() - other.AspectRatio()) < 1e-4;
        }

        /// <summary>
        /// Processes the given elements through a given action, either in parallel or in sequence, 
        /// depending on the requested degree of parallelism.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="enumT">Elements to process.</param>
        /// <param name="nDegreeOfParallelism">Requested degree of parallelism. Multi-threading applies for values >= 2.</param>
        /// <param name="actionT">Action that processes an element.</param>
        public static void ForEach<T>(this IEnumerable<T> enumT, int? nDegreeOfParallelism, Action<T> actionT)
        {
            if (!nDegreeOfParallelism.HasValue || nDegreeOfParallelism.Value < 2)
                foreach (var t in enumT) actionT(t);
            else
            {
                var enumeratorT = enumT.GetEnumerator();

                var done = 0;
                var sem = new Semaphore(0, 1);

                for (var i = 0; i < nDegreeOfParallelism.Value; ++i)
                    ThreadPool.QueueUserWorkItem(o =>
                    {
                        while (true)
                        {
                            T t;

                            lock (enumeratorT)
                            {
                                if (!enumeratorT.MoveNext())
                                {
                                    if (Interlocked.Increment(ref done) == nDegreeOfParallelism.Value)
                                        sem.Release();

                                    break;
                                }

                                t = enumeratorT.Current;
                            }

                            actionT(t);
                        }
                    });

                sem.WaitOne();
            }
        }

        /// <summary>
        /// Renders an image filled with a given color.
        /// </summary>
        /// <param name="color">Color to fill the image with.</param>
        /// <param name="width">Width of the image to return.</param>
        /// <param name="height">Height of the image to return.</param>
        /// <returns>Rendered image.</returns>
        public static Image CreateImage(this Color color, int width, int height)
        {
            return new Size(width, height).CreateImage(color);
        }

        /// <summary>
        /// Renders an image filled with a given color.
        /// </summary>
        /// <param name="color">Color to fill the image with.</param>
        /// <param name="size">Size of the image to return.</param>
        /// <returns>Rendered image.</returns>
        public static Image CreateImage(this Color color, Size size)
        {
            return size.CreateImage(color);
        }

        /// <summary>
        /// Renders an image filled with a given color.
        /// </summary>
        /// <param name="size">Size of the image to return.</param>
        /// <param name="color">Color to fill the image with.</param>
        /// <returns>Rendered image.</returns>
        public static Image CreateImage(this Size size, Color color)
        {
            // create bitmap
            var bmp = new Bitmap(size.Width, size.Height);

            // fill entire bitmap with given color
            using (var g = Graphics.FromImage(bmp))
            using (var b = new SolidBrush(color))
                g.FillRectangle(b, 0, 0, bmp.Width, bmp.Height);

            // return image
            return bmp;
        }

        /// <summary>
        /// Encodes an image as a PNG returning the PNG data stream.
        /// </summary>
        /// <param name="image">Image to encode as PNG.</param>
        /// <returns>PNG data stream.</returns>
        public static MemoryStream StreamPng(this Image image)
        {
            var stm = new MemoryStream();
            image.Save(stm, ImageFormat.Png);
            stm.Seek(0, SeekOrigin.Begin);
            return stm;
        }

        /// <summary>
        /// Helper for generating transformed points on a line (x0,y0) - (x1,y1),
        /// given the number of points to generate and a coordinate transformation.
        /// </summary>
        /// <param name="t">Coordinate transformation to use.</param>
        /// <param name="x0">x-coordinate of start point</param>
        /// <param name="y0">y-coordinate of start point</param>
        /// <param name="x1">x-coordinate of end point</param>
        /// <param name="y1">y-coordinate of end point</param>
        /// <param name="n">Number of points to generate.</param>
        /// <returns>Generated points</returns>
        private static Location[] MakeLineString(ICoordinateTransformation t, double x0, double y0, double x1, double y1, int n)
        {
            // setup interpolator
            var pointInterpolator = new
            {
                x = x0, xi = (x1 - x0) / (n - 1),
                y = y0, yi = (y1 - y0) / (n - 1)
            };

            // local function for interpolating points given an index
            Func<int, Location> interpolatedLocation = index => new Location(
                pointInterpolator.x + index * pointInterpolator.xi,
                pointInterpolator.y + index * pointInterpolator.yi
            );

            // create and return locations using interpolator and coordinate transformation
            return t.Transform(Enumerable.Range(0, n).Select(interpolatedLocation).ToArray());
        }

        /// <summary>
        /// Transforms a bounding box from one CRS to another returning the resulting BoundingLines structure.
        /// </summary>
        /// <param name="boundingBox">Bounding box to transform.</param>
        /// <param name="sourceCrs">The source CRS.</param>
        /// <param name="targetCrs">The target CRS.</param>
        /// <param name="nSupportingPoints">Number of supporting points to use.</param>
        /// <returns>BoundingLines structure that corresponds to the bounding box in the target CRS.</returns>
        public static BoundingLines TransformBoundingBox(this IBoundingBox boundingBox, string sourceCrs, string targetCrs, int nSupportingPoints)
        {
            return TransformBoundingBox(boundingBox, CoordinateTransformation.Get(sourceCrs, targetCrs), nSupportingPoints);
        }

        /// <summary>
        /// Transforms a bounding box from one CRS to another returning the resulting BoundingLines structure.
        /// </summary>
        /// <param name="boundingBox">Bounding box to transform.</param>
        /// <param name="transformation">The transformation that transforms points from the source CRS to the target CRS.</param>
        /// <param name="nSupportingPoints">Number of supporting points to use.</param>
        /// <returns>BoundingLines structure that corresponds to the bounding box in the target CRS.</returns>
        public static BoundingLines TransformBoundingBox(this IBoundingBox boundingBox, ICoordinateTransformation transformation, int nSupportingPoints)
        {
            return new BoundingLines
            {
                  Left = MakeLineString(transformation, boundingBox.MinX, boundingBox.MaxY, boundingBox.MinX, boundingBox.MinY, nSupportingPoints),
                   Top = MakeLineString(transformation, boundingBox.MinX, boundingBox.MaxY, boundingBox.MaxX, boundingBox.MaxY, nSupportingPoints),
                 Right = MakeLineString(transformation, boundingBox.MaxX, boundingBox.MaxY, boundingBox.MaxX, boundingBox.MinY, nSupportingPoints),
                Bottom = MakeLineString(transformation, boundingBox.MinX, boundingBox.MinY, boundingBox.MaxX, boundingBox.MinY, nSupportingPoints)
            };
        }

        /// <summary>
        /// Utility extension that provides an approximation of a bounding box given a BoundingLines structure.
        /// </summary>
        /// <param name="boundingLines">BoundingLines structure to determine the bounding box for.</param>
        /// <param name="resizeFactor">An additional factor for resizing the resulting bounding box; &lt;0 for adding an offset based on side line deviations, &gt;0 for simply resizing the resulting box.</param>
        /// <returns>The transformed bounding box.</returns>
        /// <remarks>This is extension does not only transform the corner points of a given bounding box but takes into 
        /// account that the bounds may be distorted when being transformed to another CRS.</remarks>
        public static MapRectangle ApproximateBoundingBox(this BoundingLines boundingLines, double resizeFactor)
        {
            // determine the bounding box using the bounding lines

            var minX = boundingLines.Left.Min(p => p.X);
            var minY = boundingLines.Bottom.Min(p => p.Y);
            var maxX = boundingLines.Right.Max(p => p.X);
            var maxY = boundingLines.Top.Max(p => p.Y);

            // if resizeFactor is valid 
            if (Math.Abs(resizeFactor) > 1e-4)
            {
                // if resize factor is negative, resize the bounds
                // based on the side deviations 

                if (resizeFactor < 0)
                {
                    resizeFactor = Math.Abs(resizeFactor) - 1;

                    minX -= boundingLines.LeftDeviation*resizeFactor;
                    minY -= boundingLines.BottomDeviation*resizeFactor;
                    maxX += boundingLines.RightDeviation*resizeFactor;
                    maxY += boundingLines.TopDeviation*resizeFactor;
                }

                // if resizeFactor positive, simply use the factor
                // to equally extend width and height of the bounds.

                else
                {
                    resizeFactor = (Math.Abs(resizeFactor) - 1)/2;

                    var rx = (maxX - minX)*resizeFactor;
                    var ry = (maxY - minY)*resizeFactor;

                    minX -= rx;
                    maxX += rx;
                    minY -= ry;
                    maxY += ry;
                }
            }

            // use the bounds to create and return a MapRectangle

            return new MapRectangle(minX, maxY, maxX, minY);
        }

        /// <summary>
        /// Transforms a bounding box from one CRS to another.
        /// </summary>
        /// <param name="boundingBox">Bounding box to transform.</param>
        /// <param name="transformation">The transformation that transforms points from the source CRS to the target CRS.</param>
        /// <param name="nSupportingPoints">Number of supporting points to use.</param>
        /// <param name="resizeFactor">An additional factor for resizing the resulting bounding box; &lt;0 for adding an offset based on side line deviations, &gt;0 for simply resizing the resulting box.</param>
        /// <returns>The transformed bounding box.</returns>
        /// <remarks>This is extension does not only transform the corner points of a given bounding box but takes into 
        /// account that the bounds may be distorted when being transformed to another CRS.</remarks>
        public static MapRectangle ApproximateBoundingBox(this IBoundingBox boundingBox, ICoordinateTransformation transformation, int nSupportingPoints, double resizeFactor)
        {
            return boundingBox.TransformBoundingBox(transformation, nSupportingPoints).ApproximateBoundingBox(resizeFactor);
        }

        /// <summary>
        /// Transforms a bounding box from one CRS to another.
        /// </summary>
        /// <param name="boundingBox">Bounding box to transform.</param>
        /// <param name="sourceCrs">The source CRS.</param>
        /// <param name="targetCrs">The target CRS.</param>
        /// <param name="nSupportingPoints">Number of supporting points to use.</param>
        /// <param name="resizeFactor">An additional factor for resizing the resulting bounding box; &lt;0 for adding an offset based on side line deviations, &gt;0 for simply resizing the resulting box.</param>
        /// <returns>The transformed bounding box.</returns>
        /// <remarks>This is extension does not only transform the corner points of a given bounding box but takes into 
        /// account that the bounds may be distorted when being transformed to another CRS.</remarks>
        public static MapRectangle ApproximateBoundingBox(this IBoundingBox boundingBox, string sourceCrs, string targetCrs, int nSupportingPoints, double resizeFactor)
        {
            return boundingBox.TransformBoundingBox(sourceCrs, targetCrs, nSupportingPoints).ApproximateBoundingBox(resizeFactor);
        }
    }

    /// <summary>
    /// Simple structure describing a bounding box with the four line strings on the left, 
    /// right, top and bottom side of the box. See remarks.
    /// </summary>
    /// <remarks>
    /// When transforming a bounding box from one CRS to another, the box may get distorted 
    /// and can no longer be defined by corner points. We use this structure to describe the 
    /// bounds. All line strings have the same length.
    /// </remarks>
    public struct BoundingLines
    {
        /// <summary>Left side; from top to bottom.</summary>
        public Location[] Left { get; internal set; }

        /// <summary>Top side; from left to right.</summary>
        public Location[] Top { get; internal set; }

        /// <summary>Right side; from top to bottom.</summary>
        public Location[] Right { get; internal set; }

        /// <summary>Bottom side; from left to right.</summary>
        public Location[] Bottom { get; internal set; }

        /// <summary>Left top corner point.</summary>
        public Location LeftTop => Left[0];

        /// <summary>Checks if all coordinates of all bounding lines are valid.</summary>
        public bool AllCoordinatesValid
        {
            get
            {
                Func<IEnumerable<Location>, IEnumerable<double>> enumCoordinates =
                    locations =>
                    {
                        var enumerable = locations.ToList();
                        return enumerable.Select(loc => loc.X).Concat(enumerable.Select(loc => loc.Y));
                    };

                return new[] {Left, Top, Right, Bottom}
                    .SelectMany(enumCoordinates)
                    .All(d => !double.IsInfinity(d) && !double.IsNaN(d));
            }
        }

        /// <summary>Right bottom corner point.</summary>
        public Location RightBottom => Bottom.Last();

        /// <summary>The x-deviation of the line string on the left side.</summary>
        /// <remarks>The line can be considered a straight line if the deviation is very small.</remarks>
        public double LeftDeviation { get { return Left.Max(p => p.X) - Left.Min(p => p.X); } }

        /// <summary>The x-deviation of the line string on the right side.</summary>
        /// <remarks>The line can be considered a straight line if the deviation is very small.</remarks>
        public double RightDeviation { get { return Right.Max(p => p.X) - Right.Min(p => p.X); } }

        /// <summary>The y-deviation of the line string on the top side.</summary>
        /// <remarks>The line can be considered a straight line if the deviation is very small.</remarks>
        public double TopDeviation { get { return Top.Max(p => p.Y) - Top.Min(p => p.Y); } }

        /// <summary>The y-deviation of the line string on the bottom side.</summary>
        /// <remarks>The line can be considered a straight line if the deviation is very small.</remarks>
        public double BottomDeviation { get { return Bottom.Max(p => p.Y) - Bottom.Min(p => p.Y); } }

        /// <summary>Checks if the bounding box described by the bounding lines can be considered rectangular.</summary>
        public bool IsRectangular => LeftDeviation < 1e-6 && TopDeviation < 1e-6 && RightDeviation < 1e-6 && BottomDeviation < 1e-6;
    }
}
