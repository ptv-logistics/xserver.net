// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

namespace Ptv.XServer.Controls.Map
{
    using System;
    using System.Collections.Generic;
    using System.Windows;

    /// <summary>
    /// <para>
    /// For geographical use cases, the type <see cref="System.Windows.Rect"/> is insufficient for different reasons.
    /// Especially the convention, the top value contains a lower coordinate value compared to the bottom
    /// value, often leads to implementing issues. Therefore, this class provides the compass directions
    /// as bounding properties directly.
    /// </para>
    /// <para>
    /// Another aspect is the paradigm of a center based reference point (compared to the edge-based format
    /// of type <see cref="System.Windows.Rect"/>).
    /// </para>
    /// </summary> 
    public class MapRectangle
    {
        #region Constructors

        /// <summary>
        /// Internal helper for setting all boundary values at once.
        /// </summary>
        /// <param name="west">Left boundary.</param>
        /// <param name="east">Right boundary.</param>
        /// <param name="south">Lower boundary.</param>
        /// <param name="north">Upper boundary.</param>
        /// <returns>The object itself is returned, so it can be reused in the same line of code.</returns>
        private MapRectangle SetValues(double west, double east, double south, double north)
        {
            West = west;
            East = east;
            South = south;
            North = north;
            return this;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapRectangle"/> class, by setting all boundaries to an empty state.
        /// This approach is helpful, when <see cref="System.Windows.Point"/> objects are inserted by the Union methods.
        /// </summary>
        public MapRectangle()
        {
            SetValues(Double.PositiveInfinity, Double.NegativeInfinity, Double.PositiveInfinity, Double.NegativeInfinity);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapRectangle"/> class, by copying the boundaries of parameter 
        /// <paramref name="originalRectangle"/>.
        /// </summary>
        /// <param name="originalRectangle">Original rectangle providing all values for the new instance.</param>
        public MapRectangle(MapRectangle originalRectangle)
        {
            SetValues(originalRectangle.West, originalRectangle.East, originalRectangle.South, originalRectangle.North);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapRectangle"/> class, by transforming the location and size of parameter 
        /// <paramref name="rect"/> into boundaries of the new instance.
        /// </summary>
        /// <param name="rect">Rectangle providing the boundaries of the new instance.</param>
        public MapRectangle(Rect rect)
        {
            SetValues(rect.X, rect.X + rect.Width, rect.Y, rect.Y + rect.Height);
        }

        /// <summary> 
        /// Initializes a new instance of the <see cref="MapRectangle"/> class, by setting the <see cref="West"/> and <see cref="East"/> boundaries
        /// to <paramref name="point"/>.X, as well as the <see cref="North"/> and <see cref="South"/> boundaries to <paramref name="point"/>.Y.
        /// Therefore, the <see cref="Width"/> and <see cref="Height"/> properties have a value of 0.
        /// </summary> 
        /// <param name="point">This point is used for defining the boundaries of the new instance.</param>
        public MapRectangle(Point point) : this()
        {
            Union(point);
        }

        /// <summary> 
        /// Initializes a new instance of the <see cref="MapRectangle"/> class. The resulting object represents the smallest rectangle
        /// containing both points, i.e. both points define the boundaries directly. The ordering of the parameters has no influence on the result.
        /// </summary> 
        /// <param name="point1">First point contained in the rectangle.</param>
        /// <param name="point2">Second point contained in the rectangle.</param>
        public MapRectangle(Point point1, Point point2) : this()
        {
            Union(point1).Union(point2);
        }

        /// <summary> 
        /// Initializes a new instance of the <see cref="MapRectangle"/> class. The resulting object represents the smallest rectangle
        /// containing all points provided by the point enumeration, i.e. the new object represents the minimal bounding rectangle
        /// of the point collection. The ordering of the points inside the enumeration has no influence on the result.
        /// </summary> 
        /// <param name="points">Enumeration of points, which are included in the new instance in a minimal way. An enumeration with
        /// no points results in an empty rectangle.</param>
        public MapRectangle(IEnumerable<Point> points) : this()
        {
            foreach (var point in points)
                Union(point);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapRectangle"/> class. The resulting object is defined by the <paramref name="center"/>
        /// and <paramref name="size"/>parameter. 
        /// </summary>
        /// <param name="center">Point defining the center of the rectangle.</param>
        /// <param name="size">Size defining the width and height of the rectangle.</param>
        public MapRectangle(Point center, Size size)
        {
            Center = center;
            Width = size.Width;
            Height = size.Height;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MapRectangle"/> class, by setting the boundaries directly. No checks are made
        /// for avoiding a lower <paramref name="east"/> value compared to the <paramref name="west"/> value. The same is true for the
        /// <paramref name="north"/>/<paramref name="south"/> pair.
        /// </summary>
        /// <remarks>
        /// The ordering of the parameters follows the scheme lower horizontal boundary - higher horizontal boundary -
        /// lower vertical boundary - higher vertical boundary. 
        /// </remarks>
        /// <param name="west">West boundary.</param>
        /// <param name="east">East boundary.</param>
        /// <param name="south">South boundary.</param>
        /// <param name="north">North boundary.</param>
        public MapRectangle(double west, double east, double south, double north)
        {
            SetValues(west, east, south, north);
        }

        #endregion // Constructors

        #region Public Properties

        /// <summary> 
        /// Gets a value indicating whether the rectangle is empty. This is true,
        /// if the width or height value is lower 0.
        /// </summary>
        public bool IsEmpty => (Width < 0) || (Height < 0);

        /// <summary>
        /// Gets or sets the West boundary of the rectangle. The rectangle is empty, if the value is higher than the <see cref="East"/> boundary value.
        /// No checks are made to avoid a higher value compared to the <see cref="East"/> boundary.
        /// </summary>
        public double West { get; set; }

        /// <summary>
        /// Gets or sets the East boundary of the rectangle. The rectangle is empty, if the value is lower than the <see cref="West"/> boundary value.
        /// No checks are made to avoid a lower value compared to the <see cref="West"/> boundary.
        /// </summary>
        public double East { get; set; }

        /// <summary>
        /// Gets or sets the South boundary of the rectangle. The rectangle is empty, if the value is higher than the <see cref="North"/> boundary value.
        /// No checks are made to avoid a higher value compared to the <see cref="North"/> boundary.
        /// </summary>
        public double South { get; set; }

        /// <summary>
        /// Gets or sets the North boundary of the rectangle. The rectangle is empty, if the value is lower than the <see cref="South"/> boundary value.
        /// No checks are made to avoid a higher value compared to the <see cref="South"/> boundary.
        /// </summary>
        public double North { get; set; }

        /// <summary> 
        /// Gets the SouthWest point of the rectangle. 
        /// </summary> 
        public Point SouthWest => new Point(West, South);

        /// <summary> 
        /// Gets the SouthEast point of the rectangle. 
        /// </summary>
        public Point SouthEast => new Point(East, South);

        /// <summary>
        /// Gets the NorthWest point of the rectangle. 
        /// </summary> 
        public Point NorthWest => new Point(West, North);

        /// <summary> 
        /// Gets the NorthEast point of the rectangle. 
        /// </summary> 
        public Point NorthEast => new Point(East, North);

        /// <summary>
        /// Gets or sets the width of the rectangle. If the width is set, the rectangle's center remains the same.
        /// </summary> 
        public double Width 
        {
            get => East - West;
            set
            {
                double centerX = Center.X;
                West = centerX - value / 2;
                East = centerX + value / 2;
            }
        }

        /// <summary>
        /// Gets or sets the height of the rectangle. If the height is set, the rectangle's center remains the same.
        /// </summary> 
        public double Height
        {
            get => North - South;
            set
            {
                double centerY = Center.Y;
                South = centerY - value / 2;
                North = centerY + value / 2;
            }
        }

        /// <summary>
        /// Gets or sets the center of the rectangle. If the rectangle is empty, the point (Double.NaN, Double.NaN) is returned.
        /// If the center is set to an empty rectangle, the value becomes the new center, the width and height of the rectangle are
        /// set to 0. If the rectangle was not empty, the center is changed, but the width and height remain unchanged.
        /// </summary>
        public Point Center
        {
            get => new Point(Width < 0 ? Double.NaN : West / 2 + East / 2, 
                Height < 0 ? Double.NaN : South / 2 + North / 2);
            set 
            {
                if (IsEmpty)
                    SetValues(value.X, value.X, value.Y, value.Y);
                else
                {
                    TranslateHorizontally(value.X - Center.X);
                    TranslateVertically(value.Y - Center.Y);
                }
            }
        }

        #endregion // Public Properties
                
        #region Contains methods

        /// <summary>
        /// Checks whether the <paramref name="point"/> parameter is within the rectangle, inclusive its edges. 
        /// </summary> 
        /// <param name="point">The point which is being tested for containment.</param> 
        /// <returns>
        /// If the rectangle is empty, the check always returns false.
        /// Otherwise it returns true, if the <paramref name="point"/> parameter is within the rectangle, false otherwise.
        /// </returns>
        public bool Contains(Point point)
        {
            return !IsEmpty && (West <= point.X) && (point.X <= East) && (South <= point.Y) && (point.Y <= North); 
        }

        /// <summary>
        /// Checks whether the <paramref name="rect"/> parameter is located completely within the rectangle, inclusive its edges. 
        /// </summary> 
        /// <param name="rect">The rectangle which is being tested for containment.</param> 
        /// <returns>
        /// If the object is empty, the check always returns false.
        /// Otherwise, if parameter <paramref name="rect"/> is empty, the check returns true (an empty rectangle is always located in 
        /// a non-empty rectangle).
        /// Otherwise it returns true, if the <paramref name="rect"/> is located completely within the rectangle, false otherwise.
        /// </returns>
        public bool Contains(MapRectangle rect)
        {
            if (IsEmpty) 
                return false;

            if (rect.IsEmpty)
                return true;
            
            return (West <= rect.West) && (East >= rect.East) && (South <= rect.South) && (North >= rect.North);
        }

        #endregion
        
        #region Intersection methods

        /// <summary>
        /// Checks whether the parameter <paramref name="rect"/> intersects with this rectangle.
        /// If one edge is coincident, it is considered an intersection. 
        /// </summary>
        /// <param name="rect">The rectangle which is being tested for intersection.</param> 
        /// <returns>Returns true if the MapRect intersects with this rectangle, false otherwise. </returns>
        public bool IntersectsWith(MapRectangle rect)
        {
            return !IsEmpty && !rect.IsEmpty && (rect.West <= East) && (rect.East >= West) && (rect.South <= North) && (rect.North >= South);
        }

        /// <summary> 
        /// Updates this rectangle to be the intersection of this and parameter <paramref name="rect"/>.
        /// If either this or <paramref name="rect"/> are empty, the result is empty as well. 
        /// </summary>
        /// <param name="rect"> The rect to intersect with this. </param>
        /// <returns>The object itself is returned, so it can be reused in the same line of code.</returns>
        public MapRectangle Intersect(MapRectangle rect)
        {
            return SetValues(Math.Max(West, rect.West), Math.Min(East, rect.East), Math.Max(South, rect.South), Math.Min(North, rect.North));
        }

        /// <summary>
        /// Calculates the result of the intersection of <paramref name="rect1"/> and <paramref name="rect2"/>. 
        /// If one of them is empty, the result is empty as well. 
        /// </summary>
        /// <param name="rect1">First rectangle to intersect.</param>
        /// <param name="rect2">Second rectangle to intersect.</param>
        /// <returns>The intersected rectangle is returned as a new instance.</returns>
        public static MapRectangle operator&(MapRectangle rect1, MapRectangle rect2)
        {
            return (new MapRectangle(rect1)).Intersect(rect2);
        }

        #endregion

        #region Union methods

        /// <summary> 
        /// Updates this rectangle to be the union of this and parameter <see cref="point"/>. I.e. the rectangle boundaries and the point
        /// itself are included in the new boundaries.
        /// If <see cref="point"/> has Double.IsNaN coordinates, the boundaries remain unchanged. 
        /// If this is empty, the boundaries are adapted to the point's coordinates (width and height will be 0).
        /// </summary>
        /// <param name="point">The point to 'incorporate' into this object. </param>
        /// <returns>The object itself is returned, so it can be reused in the same line of code.</returns>
        public MapRectangle Union(Point point)
        {
            return (Double.IsNaN(point.X) || Double.IsNaN(point.Y)) 
                ? this 
                : SetValues(Math.Min(West, point.X), Math.Max(East, point.X), Math.Min(South, point.Y), Math.Max(North, point.Y));
        }

        /// <summary>
        /// Unifies the rectangle <paramref name="rect"/> and <paramref name="point"/>. 
        /// If <paramref name="point"/> has Double.IsNaN coordinates, the resulting rectangle has the same values as <paramref name="rect"/>. 
        /// If <paramref name="rect"/> is empty, the boundaries of the resulting rectangle are adapted to the point's coordinates (width and height will be 0).
        /// </summary>
        /// <param name="rect">Rectangle of the unification.</param>
        /// <param name="point">Point to unify.</param>
        /// <returns>Unified rectangle returned as a new instance.</returns>
        public static MapRectangle operator |(MapRectangle rect, Point point)
        {
            return (new MapRectangle(rect)).Union(point);
        }

        /// <summary> 
        /// Updates this rectangle to be the union of this and parameter <paramref name="rect"/>. I.e. the rectangle boundaries of this 
        /// and <paramref name="rect"/> are included in the new boundaries.
        /// If <paramref name="rect"/> is empty, the boundaries remain unchanged. 
        /// If this is empty, the new boundaries are adapted to the <paramref name="rect"/>'s boundaries.
        /// </summary>
        /// <param name="rect">The rectangle to 'incorporate' into this object. </param>
        /// <returns>The object itself is returned, so it can be reused in the same line of code.</returns>
        public MapRectangle Union(MapRectangle rect)
        {
            return (rect.IsEmpty) ? this : Union(rect.SouthWest).Union(rect.NorthEast);
        }

        /// <summary>
        /// Unifies both rectangles <paramref name="rect1"/> and <paramref name="rect2"/>. 
        /// </summary>
        /// <param name="rect1">First rectangle for unification.</param>
        /// <param name="rect2">Second rectangle for unification.</param>
        /// <returns>The unified rectangle is returned as a new instance.</returns>
        public static MapRectangle operator |(MapRectangle rect1, MapRectangle rect2)
        {
            return (new MapRectangle(rect1)).Union(rect2);
        }

        #endregion

        #region Translate methods

        /// <summary> 
        /// Translates the rectangle horizontally, if it is not empty. The properties <see cref="West"/> and <see cref="East"/> are changed.
        /// </summary>
        /// <param name="translate">Amount of translation, positive values shifts the rectangle eastwards, negative values westwards.</param>
        /// <returns>The object itself is returned, so it can be reused in the same line of code.</returns>
        public MapRectangle TranslateHorizontally(double translate)
        {
            if (IsEmpty) return this;

            West += translate;
            East += translate;
            return this;
        }

        /// <summary> 
        /// Translates the rectangle vertically, if it is not empty. The properties <see cref="South"/> and <see cref="North"/> are changed.
        /// </summary>
        /// <param name="translate">Amount of translation, positive values shifts the rectangle northwards, negative values southwards.</param>
        /// <returns>The object itself is returned, so it can be reused in the same line of code.</returns>
        public MapRectangle TranslateVertically(double translate)
        {
            if (IsEmpty) return this;
            
            South += translate;
            North += translate;
            return this;
        }

        /// <summary> 
        /// Translates the rectangle horizontally and vertically, if it is not empty. All boundaries are changed.
        /// </summary>
        /// <param name="horizontal">Amount of horizontal translation, positive values shifts the rectangle eastwards, negative values westwards.</param>
        /// <param name="vertical">Amount of vertical translation, positive values shifts the rectangle northwards, negative values southwards.</param>
        /// <returns>The object itself is returned, so it can be reused in the same line of code.</returns>
        public MapRectangle Translate(double horizontal, double vertical)
        {
            return TranslateHorizontally(horizontal).TranslateVertically(vertical);
        }

        /// <summary> 
        /// Translates the rectangle parameter <paramref name="rect"/> horizontally and vertically, if it is not empty. All its boundaries are changed.
        /// </summary>
        /// <param name="rect">The rectangle which is the origin of the translate operation.</param>
        /// <param name="offset">The X-value specifies the amount of horizontal translation, positive values shifts the rectangle eastwards, negative values westwards.
        /// The Y-value specifies the amount of vertical translation, positive values shifts the rectangle northwards, negative values southwards.</param>
        /// <returns>The translated object is returned as a new instance.</returns>
        public static MapRectangle operator +(MapRectangle rect, Point offset)
        {
            return (new MapRectangle(rect.West + offset.X, rect.East + offset.X, rect.South + offset.Y, rect.North + offset.Y));
        }

        #endregion

        #region Inflate methods

        /// <summary>
        /// Inflates the object horizontally by specifying a factor. The width of the rectangle is multiplied by this factor.
        /// </summary>
        /// <param name="inflate">Factor which specifies the new value of the property <see cref="Width"/>.</param>
        /// <returns>The object itself is returned, so it can be reused in the same line of code.</returns>
        public MapRectangle InflateHorizontally(double inflate)
        {
            if (!IsEmpty)
            {
                Width *= inflate;
            }
            return this;
        }

        /// <summary>
        /// Inflates the object vertically by specifying a factor. The height of the rectangle is multiplied by this factor.
        /// </summary>
        /// <param name="inflate">Factor which specifies the new value of the property <see cref="Height"/>.</param>
        /// <returns>The object itself is returned, so it can be reused in the same line of code.</returns>
        public MapRectangle InflateVertically(double inflate)
        {
            if (!IsEmpty)
            {
                Height *= inflate;
            }
            return this;
        }

        /// <summary>
        /// Inflates the object horizontally and vertically by specifying two different factors for each dimension. 
        /// The width and height of the rectangle is multiplied by these factors.
        /// </summary>
        /// <param name="horizontal">Factor which specifies the new value of the property <see cref="Width"/>.</param>
        /// <param name="vertical">Factor which specifies the new value of the property <see cref="Height"/>.</param>
        /// <returns>The object itself is returned, so it can be reused in the same line of code.</returns>
        public MapRectangle Inflate(double horizontal, double vertical)
        {
            return InflateHorizontally(horizontal).InflateVertically(vertical);
        }

        /// <summary>
        /// Inflates the object horizontally and vertically by specifying one single factor, applied to both dimensions. 
        /// The width and height of the rectangle is multiplied by this factor.
        /// </summary>
        /// <param name="inflate">Factor which specifies the new value of the property <see cref="Width"/> and <see cref="Height"/>.</param>
        /// <returns>The object itself is returned, so it can be reused in the same line of code.</returns>
        public MapRectangle Inflate(double inflate)
        {
            return Inflate(inflate, inflate);
        }

        /// <summary>
        /// Inflates the object horizontally and vertically by specifying two different factors for each dimension. 
        /// The width and height of the rectangle is multiplied by these factors.
        /// </summary>
        /// <param name="rect">The rectangle which represents the origin the inflate operation.</param>
        /// <param name="inflate">The X-value specifies the new value of the property <see cref="Width"/>,
        /// whereas the Y-value specifies the new value of the property <see cref="Height"/>.</param>
        /// <returns>The object itself is returned, so it can be reused in the same line of code.</returns>
        public static MapRectangle operator *(MapRectangle rect, Point inflate)
        {
            return (new MapRectangle(rect)).Inflate(inflate.X, inflate.Y);
        }

        #endregion

        #region Object methods

        /// <summary>
        /// Compares two MapRectangle instances for object equality.
        /// </summary>
        /// <param name='rect1'>The first rectangle to compare.</param> 
        /// <param name='rect2'>The second rectangle to compare.</param>
        /// <returns>
        /// Returns true if the two rectangle instances are exactly equal, false otherwise.
        /// </returns>
        public static bool Equals(MapRectangle rect1, MapRectangle rect2)
        {
            bool rect1IsEmpty = rect1?.IsEmpty ?? true;
            bool rect2IsEmpty = rect2?.IsEmpty ?? true;

            if (rect1IsEmpty != rect2IsEmpty)
                return false;
            if (rect1IsEmpty) // rect2 is also null or empty
                return true;
            return (rect1.West == rect2.West) && (rect1.East == rect2.East) && (rect1.South == rect2.South) && (rect1.North == rect2.North);
        }

        /// <summary>
        /// Compares two rectangle instances for exact equality. 
        /// </summary>
        /// <param name='rect1'>The first rectangle to compare.</param>
        /// <param name='rect2'>The second rectangle to compare.</param> 
        /// <returns>
        /// Returns true if the two rectangle instances are exactly equal, false otherwise.
        /// </returns> 
        public static bool operator ==(MapRectangle rect1, MapRectangle rect2)
        {
            return Equals(rect1, rect2);
        }

        /// <summary> 
        /// Compares two rectangle instances for exact inequality. 
        /// </summary>
        /// <param name='rect1'>The first rectangle to compare.</param> 
        /// <param name='rect2'>The second rectangle to compare.</param> 
        /// <returns>
        /// Returns true if the two rectangle instances are exactly unequal, false otherwise.
        /// </returns>
        public static bool operator !=(MapRectangle rect1, MapRectangle rect2)
        {
            return !(rect1 == rect2);
        }

        /// <summary>
        /// Compares this rectangle with the passed in object.
        /// </summary> 
        /// <param name='o'>The object to compare to "this".</param> 
        /// <returns>
        /// Returns true if the object is an instance of MapRectangle and if it's equal to "this".
        /// </returns>
        public override bool Equals(object o)
        {
            // Check for null values and compare run-time types.
            if (o == null || GetType() != o.GetType())
                return false;

            var rectangle = o as MapRectangle;
            return (rectangle != null) && Equals(this, rectangle);
        }

        /// <summary> 
        /// Compares this Rect with the passed in object.
        /// </summary> 
        /// <param name='value'>The rectangle to compare to "this".</param>
        /// <returns>
        /// Returns true if <paramref name="value"/> is equal to "this".
        /// </returns> 
        public bool Equals(MapRectangle value)
        {
            return Equals(this, value);
        }

        /// <summary> 
        /// Returns the HashCode for this object.
        /// </summary> 
        /// <returns> 
        /// Returns the HashCode for this object.
        /// </returns> 
        public override int GetHashCode()
        {
            // Perform field-by-field XOR of HashCodes 
            return IsEmpty ? 0 : West.GetHashCode() ^ East.GetHashCode() ^ South.GetHashCode() ^ North.GetHashCode();
        }

        #endregion
    }
}
