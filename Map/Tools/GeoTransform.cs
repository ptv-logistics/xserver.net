// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Text;
using System.Windows;
using Ptv.XServer.Controls.Map.Localization;
using System.Runtime.CompilerServices;
using Ptv.Components.Projections;


namespace Ptv.XServer.Controls.Map.Tools
{
    /// <summary>
    /// <para>
    /// This namespace contains tool classes for support.
    /// </para>
    /// <para>    
    /// The <see cref="GeoTransform"/> class contains methods to transform
    /// from world to map and screen coordinates.
    /// </para>
    /// <para>    
    /// The <see cref="WkbToGdi"/> and <see cref="WkbToWpf"/> classes contain methods
    /// to convert an OGC well-known-binary byte array to GDI and WPF geometry types.
    /// </para>
    /// <para>    
    /// The <see cref="LineReductionClipping"/> class can be used to reduce the
    /// number of points for complex line string. This can be used to display
    /// large routes for the vector layer.
    /// </para>
    /// <para>   
    /// The <see cref="Ptv.XServer.Controls.Map.Tools.TileBasedPointClusterer&lt;T&gt;"/> class can be used
    /// to implement a clustering mechanism for a large amount of map locations.
    /// </para>
    /// </summary>
    [CompilerGenerated]
    class NamespaceDoc
    {
        // NamespaceDoc is used for providing the root documentation for Ptv.Components.Projections
        // hint taken from http://stackoverflow.com/questions/156582/namespace-documentation-on-a-net-project-sandcastle
    }

    /// <summary> Native class performing transformations of different coordinate formats. </summary>
    public static class GeoTransform
    {
        /// <summary> Converts tile XY coordinates into a QuadKey at a specified level of detail. </summary>
        /// <param name="tileX">Tile X coordinate.</param>
        /// <param name="tileY">Tile Y coordinate.</param>
        /// <param name="levelOfDetail">Level of detail, from 1 (lowest detail)
        /// to 23 (highest detail).</param>
        /// <returns>A string containing the QuadKey.</returns>
        public static string TileXYToQuadKey(int tileX, int tileY, int levelOfDetail)
        {
            var quadKey = new StringBuilder();
            for (int i = levelOfDetail; i > 0; i--)
            {
                char digit = '0';
                int mask = 1 << (i - 1);
                if ((tileX & mask) != 0)
                {
                    digit++;
                }
                if ((tileY & mask) != 0)
                {
                    digit++;
                    digit++;
                }
                quadKey.Append(digit);
            }
            return quadKey.ToString();
        }

        /// <summary> Check if the coordinates of the input point have valid values (i.e. unequal zero and not NaN). </summary>
        /// <param name="p">Point to be checked if x and y are valid.</param>
        /// <returns>False if one the coordinates are NaN or both coordinates are equal zero.</returns>
        public static bool IsValidGeoCoordinate(this Point p)
        {
            const double TOLERANCE = 0.0001;
            return !double.IsNaN(p.X) && !double.IsNaN(p.Y) && (Math.Abs(p.X) > TOLERANCE || Math.Abs(p.Y) > TOLERANCE);
        }

        /// <summary> Envelope rectangle of a certain tile, specified by x- and y-position and zoom level. </summary>
        /// <param name="tileX">X-position of a tile.</param>
        /// <param name="tileY">Y-position of a tile.</param>
        /// <param name="zoom">Zoom level.</param>
        /// <returns>Envelope rectangle of the specified tile, specified in Web Mercator coordinates (srid = 3857).</returns>
        public static Rect TileToWebMercatorAtZoom(int tileX, int tileY, int zoom)
        {
            // build "web Mercator" for provider (srid: 900913)
            const double EARTH_RADIUS = 6378137.0;
            const double EARTH_CIRCUM = EARTH_RADIUS * 2.0 * Math.PI;
            const double EARTH_HALF_CIRC = EARTH_CIRCUM / 2;

            double arc = EARTH_CIRCUM / (1 << zoom);
            double x1 = EARTH_HALF_CIRC - (tileX * arc);
            double y1 = EARTH_HALF_CIRC - (tileY * arc);
            double x2 = EARTH_HALF_CIRC - ((tileX + 1) * arc);
            double y2 = EARTH_HALF_CIRC - ((tileY + 1) * arc);

            return new Rect(new Point(-x1, y2), new Point(-x2, y1));            
        }

        /// <summary> Envelope rectangle of a certain tile, specified by x- and y-position and zoom level. </summary>
        /// <param name="tileX">X-position of a tile.</param>
        /// <param name="tileY">Y-position of a tile.</param>
        /// <param name="zoom">Zoom level.</param>
        /// <returns>Envelope rectangle of the specified tile, specified in PTV-internal Mercator coordinates.</returns>
        public static Rect TileToPtvMercatorAtZoom(int tileX, int tileY, int zoom)
        {
            // build "ptv Mercator" from tile key
            const double EARTH_RADIUS = 6371000.0; 
            const double EARTH_CIRCUM = EARTH_RADIUS * 2.0 * Math.PI;
            const double EARTH_HALF_CIRC = EARTH_CIRCUM / 2;

            double arc = EARTH_CIRCUM / (1 << zoom);
            double x1 = EARTH_HALF_CIRC - (tileX * arc);
            double y1 = EARTH_HALF_CIRC - (tileY * arc);
            double x2 = EARTH_HALF_CIRC - ((tileX + 1) * arc);
            double y2 = EARTH_HALF_CIRC - ((tileY + 1) * arc);

           return new Rect(new Point(-x1, y2), new Point(-x2, y1));
        }

        /// <summary> Envelope rectangle of a certain tile, specified by x- and y-position and zoom level. </summary>
        /// <param name="tileX">X-position of a tile.</param>
        /// <param name="tileY">Y-position of a tile.</param>
        /// <param name="zoom">Zoom level.</param>
        /// <returns>Envelope rectangle of the specified tile, specified in PTV-internal SmartUnit coordinates.</returns>
        public static Rect TileToPtvSmartUnitAtZoom(int tileX, int tileY, int zoom)
        {
            double arc = 127 / Math.Pow(2, zoom - 16);

            // invert tile y
            tileY = (1 << zoom) - tileY;

            double x1 = tileX * arc;
            double y1 = tileY * arc;
            double x2 = (tileX + 1) * arc;
            double y2 = (tileY - 1) * arc;

            return new Rect(new Point(x1, y2), new Point(x2, y1));
        }

        /// <summary> Transforms a distance value given in WGS format to the PTV-internal Mercator format. </summary>
        /// <param name="distance"> Distance given by two points in WGS format. </param>
        /// <returns> Distance value given in PTV-internal Mercator format. </returns>
        public static double WGSToPtvMercator(double distance)
        {
            return Math.Abs(WGSToPtvMercator(new Point(0, 0)).X - WGSToPtvMercator(new Point(distance, 0)).X);
        }

        /// <summary> Transforms a point given in WGS format to the PTV-internal Mercator format. </summary>
        /// <param name="point">Point containing WGS coordinates.</param>
        /// <returns>Transformed point containing coordinates in PTV-internal Mercator format.</returns>
        public static Point WGSToPtvMercator(Point point)
        {
            double x = 6371000.0 * point.X * Math.PI / 180.0;
            double y = 6371000.0 * Math.Log(Math.Tan(Math.PI / 4.0 + point.Y * Math.PI / 360.0)); 
                
            return new Point(x, y);
        }

        /// <summary> Transforms a point given in PTV-internal Mercator format to the WGS format. </summary>
        /// <param name="point">Point containing PTV-internal Mercator coordinates.</param>
        /// <returns>Transformed point containing coordinates in WGS format.</returns>
        public static Point PtvMercatorToWGS(Point point)
        {
            double x = (180 / Math.PI) * (point.X / 6371000.0);
            double y = (360 / Math.PI) * (Math.Atan(Math.Exp(point.Y / 6371000.0)) - (Math.PI / 4));

            return new Point(x, y);
        }

        /// <summary> Converts latitude/longitude coordinates to a textual representation. </summary>
        /// <param name="lat"> Latitude of the coordinate. </param>
        /// <param name="lon"> Longitude of the coordinate. </param>
        /// <param name="padZeroes"> If true the latitude and longitude degrees are padded with zeroes. </param>
        /// <returns> Textual representation of the input coordinates in a grad/min/sec format. </returns>
        public static string LatLonToString(double lat, double lon, bool padZeroes)
        {
            bool latIsNeg = lat < 0;
            lat = Math.Abs(lat);

            int degLat = (int)(lat);
            int minLat = (int)((lat - degLat) * 60);
            double secLat = (lat - degLat - (double)minLat / 60) * 3600;

            bool lonIsNeg = lon < 0;
            lon = Math.Abs(lon);
            int degLon = (int)(lon);
            int minLon = (int)((lon - degLon) * 60);
            double secLon = (lon - degLon - (double)minLon / 60) * 3600;
            
            string format = padZeroes ? "{0:00}° {1:00}′ {2:00}″ {3}, {4:000}° {5:00}′ {6:00}″ {7}" : "{0}° {1:00}′ {2:00}″ {3}, {4}° {5:00}′ {6:00}″ {7}"; 

            return string.Format(format,
                degLat, minLat, Math.Floor(secLat), latIsNeg ? MapLocalizer.GetString(MapStringId.South) : MapLocalizer.GetString(MapStringId.North),
                degLon, minLon, Math.Floor(secLon), lonIsNeg ? MapLocalizer.GetString(MapStringId.West) : MapLocalizer.GetString(MapStringId.East));
        }
        
        /// <summary> Converts latitude/longitude coordinates to a textual representation. </summary>
        /// <param name="lat">Latitude of the coordinate.</param>
        /// <param name="lon">Longitude of the coordinate.</param>
        /// <returns>Textual representation of the input coordinates in a grad/min/sec format.</returns>
        public static string LatLonToString(double lat, double lon)
        {
            return LatLonToString(lat, lon, false);
        }

        /// <summary>
        /// Transforms a point of the given reference system to a point of another reference system.
        /// </summary>
        /// <param name="p">The point to transform.</param>
        /// <param name="sourceSrid">The current reference system.</param>
        /// <param name="destSrid">The desired reference system.</param>
        /// <returns>The transformed point.</returns>
        public static Point Transform(Point p, string sourceSrid, string destSrid)
        {
            return GetTransform(sourceSrid, destSrid)(p);
        }

        /// <summary>
        /// Gets a transformation function for a point.
        /// </summary>
        /// <param name="sourceSrid">The current reference system.</param>
        /// <param name="destSrid">The desired reference system.</param>
        /// <returns>The transformation.</returns>
        public static Func<Point, Point> GetTransform(string sourceSrid, string destSrid)
        {
            if (sourceSrid == destSrid)
                return p => p;
            if (sourceSrid == "PTV_MERCATOR" && destSrid == "EPSG:4326")
                return PtvMercatorToWGS;
            if (sourceSrid == "EPSG:4326" && destSrid == "PTV_MERCATOR")
                return WGSToPtvMercator;
            return TransformProj4(sourceSrid, destSrid);
        }

        /// <summary>
        /// Gets a transformation function for a point using proj4.
        /// </summary>
        /// <param name="sourceSrid"> The current reference system. </param>
        /// <param name="destSrid"> The desired reference system. </param>
        /// <returns> The transformation. </returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static Func<Point, Point> TransformProj4(string sourceSrid, string destSrid)
        {
            return CoordinateTransformation.Get(sourceSrid, destSrid).Transform;
        }
    }
}

namespace System.Windows
{
    /// <summary>
    /// <para>
    /// Extension methods for WPF types. The type
    /// <see cref="PointExtensions"/> adds spatial transformation methods to
    /// the <see cref="System.Windows.Point"/> type. 
    /// </para>
    /// </summary>
    [CompilerGenerated]
    class NamespaceDoc
    {
        // NamespaceDoc is used for providing the root documentation for Ptv.Components.Projections
        // hint taken from http://stackoverflow.com/questions/156582/namespace-documentation-on-a-net-project-sandcastle
    }

    /// <summary>
    /// Extensions for the Point API.
    /// </summary>
    public static class PointExtensions
    {
        /// <summary>
        /// This extension method adds a GeoTransform API to the 
        /// <see cref="System.Windows.Point"/> class. The call is forwarded to the 
        /// <see cref="Ptv.XServer.Controls.Map.Tools.GeoTransform.Transform"/> method.
        /// </summary>
        /// <param name="p">The point to transform.</param>
        /// <param name="sourceSrid">The source reference system.</param>
        /// <param name="destSrid">The destination reference system.</param>
        /// <returns>The transformed point.</returns>
        public static Point GeoTransform(this Point p, string sourceSrid, string destSrid)
        {
            return Ptv.XServer.Controls.Map.Tools.GeoTransform.Transform(p, sourceSrid, destSrid);
        }
    }
}