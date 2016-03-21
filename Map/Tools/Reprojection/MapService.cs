using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using Ptv.Components.Projections;

namespace Ptv.XServer.Controls.Map.Tools.Reprojection
{
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
        public double MinX { get { return Math.Min(Left, Right); } }

        /// <inheritdoc />
        public double MinY { get { return Math.Min(Top, Bottom); } }

        /// <inheritdoc />
        public double MaxX { get { return Math.Max(Left, Right); } }

        /// <inheritdoc />
        public double MaxY { get { return Math.Max(Top, Bottom); } }

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

    /// <summary>
    /// Class encapsulating a service that delivers map images.
    /// </summary>
    public abstract class MapService : IMapService<IBoundingBox>
    {
        /// <summary> Constructor of the abstract base class which encapsulates a service that delivers map images. </summary>
        protected MapService()
        {
            MinAlignment = ContentAlignment.BottomLeft;
        }
        /// <inheritdoc />
        public string Crs { get; protected set; }

        /// <inheritdoc />
        public abstract Stream GetImageStream(IBoundingBox boundingBox, Size size);

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
    }
}
