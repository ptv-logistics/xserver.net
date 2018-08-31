// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace Ptv.XServer.Controls.Map.Tools
{
    /// <summary> Converts Well-known Binary representations to a GraphicsPath instance. </summary>
    public class WkbToGdi
    {
        #region private methods
        /// <summary> Creates a <see cref="System.Drawing.Point"/> from a binary. </summary>
        /// <param name="reader"> The binary reader. </param>
        /// <param name="byteOrder"> The byte order. </param>
        /// <returns> The <see cref="System.Drawing.Point"/> </returns>
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
        private static Point[] ReadCoordinates(BinaryReader reader, WkbByteOrder byteOrder, Func<double, double, Point> transform)
        {
            // Get the number of points in this linestring.
            int numPoints = (int)ReadUInt32(reader, byteOrder);

            // Create a new array of coordinates.
            var coords = new Point[numPoints];

            // Loop on the number of points in the ring.
            for (int i = 0; i < numPoints; i++)
                coords[i] = transform(ReadDouble(reader, byteOrder), ReadDouble(reader, byteOrder));

            return coords;
        }

        /// <summary> Creates a line string from a binary. </summary>
        /// <param name="reader"> The binary reader. </param>
        /// <param name="byteOrder"> The byte order. </param>
        /// <param name="transform"> The transformation function. </param>
        /// <returns> The linear ring. </returns>
        private static Point[] CreateWKBLinearRing(BinaryReader reader, WkbByteOrder byteOrder, Func<double, double, Point> transform)
        {
            return ReadCoordinates(reader, byteOrder, transform);
        }

        /// <summary>
        /// Creates a graphics path for a line string binary.
        /// </summary>
        /// <param name="reader"> The binary reader. </param>
        /// <param name="byteOrder"> The byte order. </param>
        /// <param name="transform"> The transformation function. </param>
        /// <returns> The line path. </returns>
        private static GraphicsPath CreateWKBLineString(BinaryReader reader, WkbByteOrder byteOrder, Func<double, double, Point> transform)
        {
            var path = new GraphicsPath();

            path.AddLines(ReadCoordinates(reader, byteOrder, transform));

            return path;
        }

        /// <summary>
        /// Creates a graphics path for a polygon binary.
        /// </summary>
        /// <param name="reader"> The binary reader. </param>
        /// <param name="byteOrder"> The byte order. </param>
        /// <param name="transform"> The transformation function. </param>
        /// <returns> The polygon path. </returns>returns>
        private static GraphicsPath CreateWKBPolygon(BinaryReader reader, WkbByteOrder byteOrder, Func<double, double, Point> transform)
        {
            // Get the Number of rings in this Polygon.
            int numRings = (int)ReadUInt32(reader, byteOrder);

            Debug.Assert(numRings >= 1, "Number of rings in polygon must be 1 or more.");

            var gp = new GraphicsPath();

            gp.AddPolygon(CreateWKBLinearRing(reader, byteOrder, transform));

            // Create a new array of linear rings for the interior rings.
            for (int i = 0; i < (numRings - 1); i++)
                gp.AddPolygon(CreateWKBLinearRing(reader, byteOrder, transform));

            // Create and return the Polygon.
            return gp;
        }

        /// <summary>
        /// Creates a graphics path for a multi polygon binary.
        /// </summary>
        /// <param name="reader"> The binary reader. </param>
        /// <param name="byteOrder"> The byte order. </param>
        /// <param name="transform"> The transformation function. </param>
        /// <returns> The polygon path. </returns>returns>
        private static GraphicsPath CreateWKBMultiPolygon(BinaryReader reader, WkbByteOrder byteOrder, Func<double, double, Point> transform)
        {
            var gp = new GraphicsPath();

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
                gp.AddPath(CreateWKBPolygon(reader, byteOrder, transform), false);
            }

            //Create and return the MultiPolygon.
            return gp;
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
        /// <summary> Reads a <see cref="System.Drawing.Drawing2D.GraphicsPath"/> 
        /// object from a well known binary byte array.
        /// The graphics path is either a GDI line or a polygon object.
        /// The transformation function defines the transformation from geographic
        /// to screen coordinates. </summary>
        /// <param name="bytes"> The byte array containing the well known binary. </param>
        /// <param name="transform"> The transformation function. </param>
        /// <returns> The <see cref="System.Drawing.Drawing2D.GraphicsPath"/> object. </returns>
        public static GraphicsPath Parse(byte[] bytes, Func<double, double, Point> transform)
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

        /// <summary> Reads a <see cref="System.Drawing.Drawing2D.GraphicsPath"/> 
        /// object from a well known binary byte array.
        /// The graphics path is either a GDI line or a polygon object.
        /// The transformation function defines the transformation from geographic
        /// to screen coordinates. </summary>
        /// <param name="reader"> The binary reader for the well known binary. </param>
        /// <param name="transform"> The transformation function. </param>
        /// <returns> The <see cref="System.Drawing.Drawing2D.GraphicsPath"/> object. </returns>
        public static GraphicsPath Parse(BinaryReader reader, Func<double, double, Point> transform)
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

            switch ((WKBGeometryType)type)
            {
                case WKBGeometryType.Polygon: return CreateWKBPolygon(reader, (WkbByteOrder)byteOrder, transform);
                case WKBGeometryType.MultiPolygon: return CreateWKBMultiPolygon(reader, (WkbByteOrder)byteOrder, transform);
                case WKBGeometryType.LineString: return CreateWKBLineString(reader, (WkbByteOrder)byteOrder, transform);
                default: throw new NotSupportedException("Geometry type '" + type + "' not supported");
            }
        }
        #endregion
    }
}