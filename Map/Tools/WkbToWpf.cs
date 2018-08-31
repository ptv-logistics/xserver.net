// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;


namespace Ptv.XServer.Controls.Map.Tools
{
    /// <summary> Converts Well-known Binary representations to a WPF Geometry instance. </summary>
    public class WkbToWpf
    {
        #region private methods
        /// <summary> Creates a <see cref="System.Windows.Point"/> from a binary. </summary>
        /// <param name="reader"> The binary reader. </param>
        /// <param name="byteOrder"> The byte order. </param>
        /// <returns> The <see cref="System.Windows.Point"/> </returns>
        private static Point CreateWKBPoint(BinaryReader reader, WkbByteOrder byteOrder)
        {
            // Create and return the point.
            return new Point((int)ReadDouble(reader, byteOrder), (int)ReadDouble(reader, byteOrder));
        }

        /// <summary> Creates a line string from a binary. </summary>
        /// <param name="reader"> The binary reader. </param>
        /// <param name="byteOrder"> The byte order. </param>
        /// <param name="transform"> The transformation function. </param>
        /// <returns> The line string. </returns>
        private static Point[] ReadCoordinates(BinaryReader reader, WkbByteOrder byteOrder, Func<Point, Point> transform)
        {
            // Get the number of points in this linestring.
            int numPoints = (int)ReadUInt32(reader, byteOrder);

            // Create a new array of coordinates.
            var coords = new Point[numPoints];

            // Loop on the number of points in the ring.
            for (int i = 0; i < numPoints; i++)
            {
                var point = new Point(ReadDouble(reader, byteOrder), ReadDouble(reader, byteOrder));

                // Add the coordinate.
                coords[i] = transform?.Invoke(point) ?? point;
            }

            return coords;
        }

        /// <summary> Creates a line string from a binary. </summary>
        /// <param name="reader"> The binary reader. </param>
        /// <param name="byteOrder"> The byte order. </param>
        /// <param name="transform"> The transformation function. </param>
        /// <returns> The linear ring. </returns>
        private static Point[] CreateWKBLinearRing(BinaryReader reader, WkbByteOrder byteOrder, Func<Point, Point> transform)
        {
            return ReadCoordinates(reader, byteOrder, transform);
        }

        /// <summary>
        /// Creates a PathFigure collection for a polygon binary.
        /// </summary>
        /// <param name="reader"> The binary reader. </param>
        /// <param name="byteOrder"> The byte order. </param>
        /// <param name="transform"> The transformation function. </param>
        /// <returns> The  PathFigure collection. </returns>returns>
        private static IEnumerable<PathFigure> CreateWKBPolygon(BinaryReader reader, WkbByteOrder byteOrder, Func<Point, Point> transform)
        {
            // Get the Number of rings in this Polygon.
            int numRings = (int)ReadUInt32(reader, byteOrder);

            Debug.Assert(numRings >= 1, "Number of rings in polygon must be 1 or more.");

            yield return BuildPathFigure(CreateWKBLinearRing(reader, byteOrder, transform));

            // Create a new array of linear rings for the interior rings.
            for (int i = 0; i < (numRings - 1); i++)
                yield return BuildPathFigure(CreateWKBLinearRing(reader, byteOrder, transform));
        }

        /// <summary>
        /// Creates a PathFigure collection for a multi polygon binary.
        /// </summary>
        /// <param name="reader"> The binary reader. </param>
        /// <param name="byteOrder"> The byte order. </param>
        /// <param name="transform"> The transformation function. </param>
        /// <returns> The  PathFigure collection. </returns>returns>
        private static IEnumerable<PathFigure> CreateWKBMultiPolygon(BinaryReader reader, WkbByteOrder byteOrder, Func<Point, Point> transform)
        {
            // Get the number of Polygons.
            int numPolygons = (int)ReadUInt32(reader, byteOrder);

            // Loop on the number of polygons.
            for (int i = 0; i < numPolygons; i++)
            {
                // read polygon header
                reader.ReadByte();
                ReadUInt32(reader, byteOrder);

                // TODO: Validate type

                // Create the next polygon and add it to the array.
                foreach (var figure in CreateWKBPolygon(reader, byteOrder, transform))
                {
                    bool skip = false;
                    // Here we check the coordinates of the figures to skip invalid once and return the valid figures.
                    foreach (var seg in figure.Segments)
                    {
                        if (seg is PolyLineSegment segment)
                        {
                            foreach (var p in segment.Points)
                            {
                                skip = (double.IsInfinity(p.X) || double.IsInfinity(p.Y));
                                if (skip)
                                    break;
                            }
                        }

                        if (skip)
                            break;
                    }
                    
                    if(!skip)
                        yield return figure;
                }
            }
        }

        /// <summary> Reads an unsigned integer value from a binary. </summary>
        /// <param name="reader"> The binary reader. </param>
        /// <param name="byteOrder"> The byte order. </param>
        /// <returns> The unsigned integer value. </returns>
        private static uint ReadUInt32(BinaryReader reader, WkbByteOrder byteOrder)
        {
            if (byteOrder != WkbByteOrder.Xdr) return reader.ReadUInt32();

            byte[] bytes = BitConverter.GetBytes(reader.ReadUInt32());
            Array.Reverse(bytes);
            return BitConverter.ToUInt32(bytes, 0);
        }

        /// <summary> Reads a double value from a binary. </summary>
        /// <param name="reader"> The binary reader. </param>
        /// <param name="byteOrder"> The byte order. </param>
        /// <returns> The double value. </returns>
        private static double ReadDouble(BinaryReader reader, WkbByteOrder byteOrder)
        {
            if (byteOrder != WkbByteOrder.Xdr) return reader.ReadDouble();

            byte[] bytes = BitConverter.GetBytes(reader.ReadDouble());
            Array.Reverse(bytes);
            return BitConverter.ToDouble(bytes, 0);
        }

        #endregion

        #region public methods
        /// <summary> Reads a <see cref="System.Windows.Media.Geometry"/> 
        /// object from a well known binary byte array.
        /// The graphics path is either a GDI line or a polygon object.
        /// The transformation function defines the transformation from geographic
        /// to screen coordinates. </summary>
        /// <param name="bytes"> The byte array containing the well known binary. </param>
        /// <param name="transform"> The transformation function. </param>
        /// <returns> The <see cref="System.Windows.Media.Geometry"/> object. </returns>
        public static Geometry Parse(byte[] bytes, Func<Point, Point> transform)
        {
            // Create a memory stream using the supplied byte array.
            using (var memoryStream = new MemoryStream(bytes))
            {
                // Create a new binary reader using the newly created memory stream.
                using (var reader = new BinaryReader(memoryStream))
                {
                    // Call the main create function.
                    return Parse(reader, transform);
                }
            }
        }

        /// <summary> Reads a <see cref="System.Windows.Media.Geometry"/> 
        /// object from a well known binary byte array.
        /// The graphics path is either a GDI line or a polygon object.
        /// The transformation function defines the transformation from geographic
        /// to screen coordinates. </summary>
        /// <param name="reader"> The binary reader for the well known binary. </param>
        /// <param name="transform"> The transformation function. </param>
        /// <returns> The <see cref="System.Windows.Media.Geometry"/> object. </returns>
        public static Geometry Parse(BinaryReader reader, Func<Point, Point> transform)
        {
            // Get the first byte in the array.  This specifies if the WKB is in
            // XDR (big-endian) format of NDR (little-endian) format.
            byte byteOrder = reader.ReadByte();

            if (!Enum.IsDefined(typeof(WkbByteOrder), byteOrder))
            {
                throw new ArgumentException("Byte order not recognized");
            }

            // Get the type of this geometry.
            var type = ReadUInt32(reader, (WkbByteOrder)byteOrder);

            if (!Enum.IsDefined(typeof(WKBGeometryType), type))
                throw new ArgumentException("Geometry type not recognized");

            var figures = new PathFigureCollection();

            switch ((WKBGeometryType)type)
            {
                case WKBGeometryType.Polygon:
                    foreach (var figure in CreateWKBPolygon(reader, (WkbByteOrder)byteOrder, transform))
                        figures.Add(figure);
                    break;
                case WKBGeometryType.MultiPolygon:
                    foreach (var figure in CreateWKBMultiPolygon(reader, (WkbByteOrder)byteOrder, transform))
                        figures.Add(figure);
                    break;
                default:
                    throw new NotSupportedException("Geometry type '" + type + "' not supported");
            }

            return new PathGeometry { Figures = figures };
        }

        /// <summary> Builds a path figure for a point array. </summary>
        /// <param name="coordinates"> The list of points. </param>
        /// <returns> The path figure. </returns>
        public static PathFigure BuildPathFigure(Point[] coordinates)
        {
            var figure = new PathFigure
            {
                IsFilled = true,
                IsClosed = true,
                StartPoint = new Point { X = coordinates[0].X, Y = coordinates[0].Y },
            };

            var segments = new PathSegmentCollection {new PolyLineSegment {Points = ToSegments(coordinates)}};
            segments.Freeze();
            figure.Segments = segments;
            figure.Freeze();
            return figure;
        }

        /// <summary> Convert a point array to a <see cref="System.Windows.Media.PointCollection"/> class. </summary>
        /// <param name="points"> The array of points. </param>
        /// <returns> The <see cref="System.Windows.Media.PointCollection"/>. </returns>
        public static PointCollection ToSegments(Point[] points)
        {
            var result = new PointCollection();

            foreach (var point in points)
                result.Add(new Point { X = point.X, Y = point.Y });

            return result;
        }
        #endregion
    }
}