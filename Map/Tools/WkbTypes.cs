// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

namespace Ptv.XServer.Controls.Map.Tools
{
    /// <summary> Specifies the byte order value. </summary>
    public enum WkbByteOrder : byte
    {
        /// <summary> Value for big-endian byte order. </summary>
        Xdr = 0,
        /// <summary> Value for little-endian byte order. </summary>
        Ndr = 1
    }

    /// <summary> Specifies the geometry type value. </summary>
    public enum WKBGeometryType : uint
    {
        /// <summary> Value for point. </summary>
        Point = 1,
        /// <summary> Value for line string. </summary>
        LineString = 2,
        /// <summary> Value for polygon. </summary>
        Polygon = 3,
        /// <summary> Value for multi point. </summary>
        MultiPoint = 4,
        /// <summary> Value for multi line string. </summary>
        MultiLineString = 5,
        /// <summary> Value for multi polygon. </summary>
        MultiPolygon = 6,
        /// <summary> Value for geometry collection. </summary>
        GeometryCollection = 7
    }
}
