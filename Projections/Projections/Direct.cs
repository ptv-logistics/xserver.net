// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Collections.Generic;
using System.Windows;
using System.Reflection;
using Ptv.Components.Projections.Custom;

namespace Ptv.Components.Projections.Direct
{
    /// <summary>
    /// Provides classes for transforming coordinates using plain managed code. The core transformations 
    /// provided by <see cref="CoordinateTransformations"/> can either be used directly or implicitly through 
    /// <see cref="Ptv.Components.Projections.CoordinateTransformation">Ptv.Components.Projections.CoordinateTransformation</see>, 
    /// which uses <see cref="CoordinateTransformation"/> where possible.
    /// </summary>
    [System.Runtime.CompilerServices.CompilerGenerated]
    class NamespaceDoc
    {
        // NamespaceDoc is used for providing the root documentation for Ptv.Components.Projections.Direct
        // hint taken from http://stackoverflow.com/questions/156582/namespace-documentation-on-a-net-project-sandcastle
    }
   
    /// <summary>
    /// Defines the source and target identifiers of a managed coordinate transformation routine.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    internal class TransformationAttribute : Attribute
    {
        /// <summary>
        /// Identifier of the source coordinate reference system.
        /// </summary>
        public string SourceId;

        /// <summary>
        /// Identifier of the target coordinate reference system.
        /// </summary>
        public string TargetId;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformationAttribute"/> class.
        /// </summary>
        /// <param name="sourceId">Value containing the source spatial reference identifier.</param>
        /// <param name="targetId">Value containing the target spatial reference identifier.</param>
        public TransformationAttribute(string sourceId, string targetId)
        {
            SourceId = sourceId;
            TargetId = targetId;
        }
    }

    /// <summary>
    /// A delegate for transforming a coordinate.
    /// </summary>
    /// <param name="x">The x-coordinate to transform (in and out).</param>
    /// <param name="y">The y-coordinate to transform (in and out).</param>
    /// <param name="z">The z-coordinate to transform (in and out, see remarks on <see cref="CoordinateTransformations"/>).</param>
    /// <remarks>
    /// This delegate is used along with <see cref="GetTransformDelegate"/> / 
    /// <see cref="CoordinateTransformation.OnGetTransform">CoordinateTransformation.OnGetTransform</see> to 
    /// provide additional coordinate transformation routines to <see cref="CoordinateTransformation"/>.
    /// </remarks>
    public delegate void TransformDelegate(ref double x, ref double y, ref double z);

    /// <summary>
    /// A delegate providing a transformation for specific coordinate reference systems.
    /// </summary>
    /// <param name="sourceId">The identifier of the source coordinate reference system.</param>
    /// <param name="targetId">The identifier of the target coordinate reference system.</param>
    /// <returns>A <see cref="TransformDelegate"/> that can handle the coordinate transformation or null.</returns>
    /// <remarks>
    /// <see cref="GetTransformDelegate"/> along with 
    /// <see cref="CoordinateTransformation.OnGetTransform">CoordinateTransformation.OnGetTransform</see> is used  
    /// to provide additional coordinate transformation routines to <see cref="CoordinateTransformation"/>.
    /// </remarks>
    public delegate TransformDelegate GetTransformDelegate(string sourceId, string targetId);

    /// <summary>
    /// Defines frequently used managed coordinate transformation routines.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The provided routines are mainly used by <see cref="CoordinateTransformation"/> 
    /// to implement a direct coordinate transformation given a source and target identifier
    /// of a coordinate reference system. 
    /// </para><para>
    /// The coordinate transformations may be used directly. The z-coordinate of the transformation 
    /// methods can be ignored (reserved for future use).
    /// </para>
    /// </remarks>
    public static class CoordinateTransformations
    {
        #region RAD / DEG

        /// <summary>
        /// DEG to RAD conversion
        /// </summary>
        private const double DEG_TO_RAD = Math.PI / 360.0;

        /// <summary>
        /// RAD to DEG conversion
        /// </summary>
        private const double RAD_TO_DEG = 360.0 / Math.PI;

        #endregion

        #region CRSID (for which transformations exists)

        /// <summary>
        /// CRSID: PTV Geodecimal
        /// </summary>
        private const string PTV_GEODECIMAL = "PTV_GEODECIMAL";

        /// <summary>
        /// CRSID: WGS84
        /// </summary>
        private const string WGS84 = "EPSG:4326";

        /// <summary>
        /// CRSID: Google Mercator
        /// </summary>
        private const string GOOGLE_MERCATOR = "EPSG:3857";

        /// <summary>
        /// CRSID: PTV GeoMinSec
        /// </summary>
        private const string PTV_GEOMINSEC = "PTV_GEOMINSEC";

        /// <summary>
        /// CRSID: PTV Mercator
        /// </summary>
        private const string PTV_MERCATOR = "PTV_MERCATOR";

        /// <summary>
        /// CRSID: UTM
        /// </summary>
        private const string UTM = "cfUTM";

        /// <summary>
        /// CRSID: PTV Conform
        /// </summary>
        private const string PTV_CONFORM = "PTV_CONFORM";

        /// <summary>
        /// CRSID: PTV Euro Conform
        /// </summary>
        private const string PTV_EUROCONFORM = "PTV_EUROCONFORM";

        /// <summary>
        /// PTV Raster Smart Units
        /// </summary>
        private const string PTV_RASTERSMARTUNITS = "PTV_RASTERSMARTUNITS";

        #endregion

        #region Constants used by Conform transformations

        /// <summary>
        /// use by conform transformations, taken from claw (Coordinates.cs)
        /// </summary>
        private static readonly double gpEuro_n = Math.Log(Math.Cos(DEG_TO_RAD * 90.0) / Math.Cos(DEG_TO_RAD * 110.0)) / Math.Log(Math.Tan(DEG_TO_RAD * 145.0) / Math.Tan(DEG_TO_RAD * 135.0));

        /// <summary>
        /// use by conform transformations, taken from claw (Coordinates.cs)
        /// </summary>
        private static readonly double gpEuro_nInv = 1.0 / gpEuro_n;

        /// <summary>
        /// use by conform transformations, taken from claw (Coordinates.cs)
        /// </summary>                                                                                                                                   
        private static readonly double gpEuro_RF = Math.Cos(DEG_TO_RAD * 90.0) * Math.Pow(Math.Tan(DEG_TO_RAD * 135.0), gpEuro_n) / gpEuro_n;

        /// <summary>
        /// use by conform transformations, taken from claw (Coordinates.cs)
        /// </summary>                                                                                                                              
        private static readonly double gpEuro_rho0 = gpEuro_RF / Math.Pow(Math.Tan(DEG_TO_RAD * 120.0), gpEuro_n);

        #endregion

        /// <summary>
        /// Provides the managed PTV Geodecimal to WGS84 transformation.
        /// </summary>
        /// <param name="x">The x-coordinate to transform (in and out).</param>
        /// <param name="y">The y-coordinate to transform (in and out).</param>
        /// <param name="z">The z-coordinate to transform (in and out, see remarks on <see cref="CoordinateTransformations"/>).</param>
        [Transformation(PTV_GEODECIMAL, WGS84)]
        public static void TransformPtvGeodecimalToWgs84(ref double x, ref double y, ref double z)
        {
            x /= 100000;
            y /= 100000;
        }

        /// <summary>
        /// Provides the managed WGS84 to PTV Geodecimal transformation.
        /// </summary>
        /// <param name="x">The x-coordinate to transform (in and out).</param>
        /// <param name="y">The y-coordinate to transform (in and out).</param>
        /// <param name="z">The z-coordinate to transform (in and out, see remarks on <see cref="CoordinateTransformations"/>).</param>
        [Transformation(WGS84, PTV_GEODECIMAL)]
        public static void TransformWgs84ToPtvGeodecimal(ref double x, ref double y, ref double z)
        {
            x *= 100000;
            y *= 100000;
        }

        /// <summary>
        /// Provides the managed WGS84 to PTV GeoMinSek transformation.
        /// </summary>
        /// <param name="x">The x-coordinate to transform (in and out).</param>
        /// <param name="y">The y-coordinate to transform (in and out).</param>
        /// <param name="z">The z-coordinate to transform (in and out, see remarks on <see cref="CoordinateTransformations"/>).</param>
        [Transformation(WGS84, PTV_GEOMINSEC)]
        public static void TransformWgs84ToPtvGeoMinSek(ref double x, ref double y, ref double z)
        {
            GeoMinSekTransformation.Wgs84ToGeoMinSek(ref x);
            GeoMinSekTransformation.Wgs84ToGeoMinSek(ref y);
        }

        /// <summary>
        /// Provides the managed PTV GeoMinSek to WGS84 transformation.
        /// </summary>
        /// <param name="x">The x-coordinate to transform (in and out).</param>
        /// <param name="y">The y-coordinate to transform (in and out).</param>
        /// <param name="z">The z-coordinate to transform (in and out, see remarks on <see cref="CoordinateTransformations"/>).</param>
        [Transformation(PTV_GEOMINSEC, WGS84)]
        public static void TransformPtvGeoMinSekToWgs84(ref double x, ref double y, ref double z)
        {
            GeoMinSekTransformation.GeoMinSekToWgs84(ref x);
            GeoMinSekTransformation.GeoMinSekToWgs84(ref y);
        }

        /// <summary>
        /// Provides the managed WGSPTV Geodecimal to UTM transformation.
        /// </summary>
        /// <param name="x">The x-coordinate to transform (in and out).</param>
        /// <param name="y">The y-coordinate to transform (in and out).</param>
        /// <param name="z">The z-coordinate to transform (in and out, see remarks on <see cref="CoordinateTransformations"/>).</param>
        [Transformation(WGS84, UTM)]
        public static void TransformWgs84ToUtm(ref double x, ref double y, ref double z)
        {
            throw new TransformationException("PTV Geodecimal to UTM transformation is not yet implemented");
        }

        /// <summary>
        /// Provides the managed UTM to PTV Geodecimal transformation.
        /// </summary>
        /// <param name="x">The x-coordinate to transform (in and out).</param>
        /// <param name="y">The y-coordinate to transform (in and out).</param>
        /// <param name="z">The z-coordinate to transform (in and out, see remarks on <see cref="CoordinateTransformations"/>).</param>
        [Transformation(UTM, WGS84)]
        public static void TransformUtmToWgs84(ref double x, ref double y, ref double z)
        {
            throw new TransformationException("UTM transformation is not yet implemented");
        }

        /// <summary>
        /// Provides the managed WGS84 to PTV Mercator transformation.
        /// </summary>
        /// <param name="x">The x-coordinate to transform (in and out).</param>
        /// <param name="y">The y-coordinate to transform (in and out).</param>
        /// <param name="z">The z-coordinate to transform (in and out, see remarks on <see cref="CoordinateTransformations"/>).</param>
        [Transformation(WGS84, PTV_MERCATOR)]
        public static void TransformWgs84ToPtvMercator(ref double x, ref double y, ref double z)
        {
            x = 6371000.0 * 2.0 * DEG_TO_RAD * x;
            y = 6371000.0 * Math.Log(Math.Tan(DEG_TO_RAD * (90.0 + y)));
        }

        /// <summary>
        /// Provides the managed PTV Mercator to WGS84 transformation.
        /// </summary>
        /// <param name="x">The x-coordinate to transform (in and out).</param>
        /// <param name="y">The y-coordinate to transform (in and out).</param>
        /// <param name="z">The z-coordinate to transform (in and out, see remarks on <see cref="CoordinateTransformations"/>).</param>
        [Transformation(PTV_MERCATOR, WGS84)]
        public static void TransformPtvMercatorToWgs84(ref double x, ref double y, ref double z)
        {
            x = RAD_TO_DEG * x / 2.0 / 6371000.0;
            y = RAD_TO_DEG * Math.Atan(Math.Exp(y / 6371000.0)) - 90.0;
        }

        /// <summary>
        /// Provides the managed WGS84 to Google Mercator transformation.
        /// </summary>
        /// <param name="x">The x-coordinate to transform (in and out).</param>
        /// <param name="y">The y-coordinate to transform (in and out).</param>
        /// <param name="z">The z-coordinate to transform (in and out, see remarks on <see cref="CoordinateTransformations"/>).</param>
        [Transformation(WGS84, GOOGLE_MERCATOR)]
        public static void TransformWgs84ToGoogleMercator(ref double x, ref double y, ref double z)
        {
            x = 6378137.0 * 2.0 * DEG_TO_RAD * x;
            y = 6378137.0 * Math.Log(Math.Tan(DEG_TO_RAD * (90.0 + y)));
        }

        /// <summary>
        /// Provides the managed Google Mercator to WGS84 transformation.
        /// </summary>
        /// <param name="x">The x-coordinate to transform (in and out).</param>
        /// <param name="y">The y-coordinate to transform (in and out).</param>
        /// <param name="z">The z-coordinate to transform (in and out, see remarks on <see cref="CoordinateTransformations"/>).</param>
        [Transformation(GOOGLE_MERCATOR, WGS84)]
        public static void TransformGoogleMercatorToWgs84(ref double x, ref double y, ref double z)
        {
            x = RAD_TO_DEG * x / 2.0 / 6378137.0;
            y = RAD_TO_DEG * Math.Atan(Math.Exp(y / 6378137.0)) - 90.0;
        }

        /// <summary>
        /// Provides the managed PTV Mercator to Google Mercator transformation.
        /// </summary>
        /// <param name="x">The x-coordinate to transform (in and out).</param>
        /// <param name="y">The y-coordinate to transform (in and out).</param>
        /// <param name="z">The z-coordinate to transform (in and out, see remarks on <see cref="CoordinateTransformations"/>).</param>
        [Transformation(PTV_MERCATOR, GOOGLE_MERCATOR)]
        public static void TransformPtvMercatorToGoogleMercator(ref double x, ref double y, ref double z)
        {
            const double f = 6378137.0 / 6371000.0;

            x *= f;
            y *= f;
        }

        /// <summary>
        /// Provides the managed Google Mercator to PTV Mercator transformation.
        /// </summary>
        /// <param name="x">The x-coordinate to transform (in and out).</param>
        /// <param name="y">The y-coordinate to transform (in and out).</param>
        /// <param name="z">The z-coordinate to transform (in and out, see remarks on <see cref="CoordinateTransformations"/>).</param>
        [Transformation(GOOGLE_MERCATOR, PTV_MERCATOR)]
        public static void TransformGoogleMercatorToPtvMercator(ref double x, ref double y, ref double z)
        {
            const double f = 6371000.0 / 6378137.0;

            x *= f;
            y *= f;
        }

        /// <summary>
        /// Provides the managed PTV Mercator to PTV Conform transformation.
        /// </summary>
        /// <param name="x">The x-coordinate to transform (in and out).</param>
        /// <param name="y">The y-coordinate to transform (in and out).</param>
        /// <param name="z">The z-coordinate to transform (in and out, see remarks on <see cref="CoordinateTransformations"/>).</param>
        [Transformation(WGS84, PTV_CONFORM)]
        public static void TransformWgs84ToConform(ref double x, ref double y, ref double z)
        {
            double teta = gpEuro_n * 2.0 * DEG_TO_RAD * (x - 10.0);
            double rho = gpEuro_RF / Math.Pow(Math.Tan(DEG_TO_RAD * (y + 90.0)), gpEuro_n);

            x = rho * Math.Sin(teta) * 6365000 + 1800000;
            y = (gpEuro_rho0 - rho * Math.Cos(teta)) * 6365000 - 500000;
        }

        /// <summary>
        /// Provides the managed PTV Conform to PTV Mercator transformation.
        /// </summary>
        /// <param name="x">The x-coordinate to transform (in and out).</param>
        /// <param name="y">The y-coordinate to transform (in and out).</param>
        /// <param name="z">The z-coordinate to transform (in and out, see remarks on <see cref="CoordinateTransformations"/>).</param>
        [Transformation(PTV_CONFORM, WGS84)]
        public static void TransformConformToWgs84(ref double x, ref double y, ref double z)
        {
            x = (x - 1800000) / 6365000;
            y = (y + 500000) / 6365000;

            double h = gpEuro_rho0 - y;
            double teta = Math.Atan(x / h);
            double rho = Math.Sqrt(x * x + h * h);

            x = RAD_TO_DEG * (teta / gpEuro_n + DEG_TO_RAD * 20.0) / 2.0;
            y = RAD_TO_DEG * Math.Atan(Math.Pow(gpEuro_RF / rho, gpEuro_nInv)) - 90.0;
        }

        /// <summary>
        /// Provides the managed PTV Euro Conform to PTV Mercator transformation.
        /// </summary>
        /// <param name="x">The x-coordinate to transform (in and out).</param>
        /// <param name="y">The y-coordinate to transform (in and out).</param>
        /// <param name="z">The z-coordinate to transform (in and out, see remarks on <see cref="CoordinateTransformations"/>).</param>
        [Transformation(PTV_EUROCONFORM, WGS84)]
        public static void TransformEuroConformToWgs84(ref double x, ref double y, ref double z)
        {
            x -= 3400000;
            y -= 1700000;

            TransformConformToWgs84(ref x, ref y, ref z);
        }

        /// <summary>
        /// Provides the managed PTV Mercator to PTV Euro Conform transformation.
        /// </summary>
        /// <param name="x">The x-coordinate to transform (in and out).</param>
        /// <param name="y">The y-coordinate to transform (in and out).</param>
        /// <param name="z">The z-coordinate to transform (in and out, see remarks on <see cref="CoordinateTransformations"/>).</param>
        [Transformation(WGS84, PTV_EUROCONFORM)]
        public static void TransformWgs84ToEuroConform(ref double x, ref double y, ref double z)
        {
            TransformWgs84ToConform(ref x, ref y, ref z);

            x += 3400000;
            y += 1700000;
        }

        /// <summary>
        /// Provides the managed PTV Mercator to PTV Raster Smart Units transformation.
        /// </summary>
        /// <param name="x">The x-coordinate to transform (in and out).</param>
        /// <param name="y">The y-coordinate to transform (in and out).</param>
        /// <param name="z">The z-coordinate to transform (in and out, see remarks on <see cref="CoordinateTransformations"/>).</param>
        [Transformation(PTV_MERCATOR, PTV_RASTERSMARTUNITS)]
        public static void TransformMercatorToRasterSmartUnits(ref double x, ref double y, ref double z)
        {
            x = (x + 20015087) / 4.809543;
            y = (y + 20015087) / 4.809543;
        }

        /// <summary>
        /// Provides the managed PTV Mercator to PTV Raster Smart Units transformation.
        /// </summary>
        /// <param name="x">The x-coordinate to transform (in and out).</param>
        /// <param name="y">The y-coordinate to transform (in and out).</param>
        /// <param name="z">The z-coordinate to transform (in and out, see remarks on <see cref="CoordinateTransformations"/>).</param>
        [Transformation(PTV_RASTERSMARTUNITS, PTV_MERCATOR)]
        public static void TransformRasterSmartUnitsToMercator(ref double x, ref double y, ref double z)
        {
            x = x * 4.809543 - 20015087;
            y = y * 4.809543 - 20015087;
        }

        /// <summary>
        /// Stores a CRSIDs and its aliases.
        /// </summary>
        private class RootCRSID
        {
            /// <summary>
            /// Creates an instance of <see cref="RootCRSID"/>.
            /// </summary>
            /// <param name="crsId">The identifier of the coordinate reference system.</param>
            /// <param name="aliases">The aliases for the coordinate reference system</param>
            public RootCRSID(string crsId, params string[] aliases)
            {
                Id = crsId;
                Aliases = aliases;
            }

            /// <summary>
            /// Creates an instance of <see cref="RootCRSID"/>.
            /// </summary>
            /// <param name="crsId">The identifier of the coordinate reference system.</param>
            public RootCRSID(string crsId) : this(crsId, new string[] { }) { }

            /// <summary>
            /// Reads and writes the coordinate reference system identifier.
            /// </summary>
            public string Id { get; set; }

            /// <summary>
            /// Reads and writes the aliases of the coordinate reference system.
            /// </summary>
            public string[] Aliases { get; set; }
        }

        /// <summary>
        /// Defines the 'root CRSIDs' and their aliases, if any. 
        /// The first entry in the arrays specifies a 'root CRSID', follow-up entries define the aliases.
        /// </summary>
        private static readonly RootCRSID[] rootCRSIDs = new RootCRSID[] 
        {
            new RootCRSID(PTV_GEODECIMAL, "cfGEODECIMAL"),
            new RootCRSID(WGS84, "OG_GEODECIMAL", "OGC_GEODECIMAL"),
            new RootCRSID(GOOGLE_MERCATOR, "EPSG:3785", "EPSG:3857"),
            new RootCRSID(PTV_GEOMINSEC, "cfGEOMINSEK"),
            new RootCRSID(PTV_MERCATOR, "cfMERCATOR", "EPSG:76131", "EPSG:505456"),
            new RootCRSID(UTM /* no aliases */),
            new RootCRSID(PTV_CONFORM /* no aliases */),
            new RootCRSID(PTV_EUROCONFORM, "PTV_SUPERCONFORM"),
            new RootCRSID(PTV_RASTERSMARTUNITS, "PTV_SMARTUNITS")
        };

        /// <summary>
        /// Maps CRSIDs to their 'root CRSID' using the aliases defined through alias_def. Initialized in static constructor.
        /// </summary>
        private static readonly SortedList<string, string> alias_map = new SortedList<string, string>();

        /// <summary>
        /// Cache storing the delegates of the well known coordinate transformation routines.
        /// </summary>
        private static readonly SortedList<string, TransformDelegate> transformations = new SortedList<string, TransformDelegate>();

        /// <summary>
        /// Caches a transformation method, described by its source and target coordinate reference system.
        /// </summary>
        /// <param name="sourceId">The identifier of the source coordinate reference system.</param>
        /// <param name="targetId">The identifier of the target coordinate reference system.</param>
        /// <param name="mi">The transformation method to cache.</param>
        private static void AddTransformation(string sourceId, string targetId, MethodInfo mi)
        {
            string key = GetTransformationKey(sourceId, targetId);

            if (!transformations.ContainsKey(key))
                transformations.Add(key, (TransformDelegate)Delegate.CreateDelegate(typeof(TransformDelegate), mi));
        }

        /// <summary>
        /// Initializes static members of the <see cref="CoordinateTransformations"/> class.
        /// </summary>
        static CoordinateTransformations()
        {
            // setup alias map
            foreach (RootCRSID r in rootCRSIDs)
                foreach (string alias in r.Aliases)
                    alias_map.Add(alias.ToLower(), r.Id.ToLower());

            // find possible transformations
            foreach (MethodInfo mi in typeof(CoordinateTransformations).GetMethods(BindingFlags.Static | BindingFlags.Public))
            {
                // check method for TransformationAttribute
                object[] attribs = mi.GetCustomAttributes(typeof(TransformationAttribute), true);

                // if it has an TransformationAttribute, add the transformation
                if (attribs == null || attribs.Length != 1) continue;
                TransformationAttribute ta = (TransformationAttribute)attribs[0];
                AddTransformation(ta.SourceId, ta.TargetId, mi);
            }

            // loop through the root crs'es
            foreach (RootCRSID src in rootCRSIDs)
                foreach (RootCRSID trgt in rootCRSIDs)
                {
                    // avoid IdentityTransformation and avoid those transformations for which a delegate already exists
                    if (src == trgt || TryGetTransformation(src.Id, trgt.Id, out var d)) continue;

                    // try to find a transformation chain for the src -> trgt transformation
                    if (TryGetTransformationChain(src.Id, trgt.Id, out d))
                    {
                        // found a chain, store the corresponding delegate
                        transformations.Add(GetTransformationKey(src.Id, trgt.Id), d);
                    }
                }
        }

        /// <summary>
        /// Provides a chained transformation that iterates over a given array of <see cref="TransformDelegate"/> delegates.
        /// </summary>
        private class TransformationChain
        {
            /// <summary>
            /// The transformation delegates to iterate over.
            /// </summary>
            private readonly TransformDelegate[] chain;

            /// <summary>
            /// Create the <see cref="TransformationChain"/> instance.
            /// </summary>
            /// <param name="chain"></param>
            public TransformationChain(params TransformDelegate[] chain)
            {
                this.chain = chain;
            }

            /// <summary>
            /// Provides a transformation that calls the different <see cref="TransformDelegate"/> instances in the chain.
            /// </summary>
            /// <param name="x">The x-coordinate to transform (in and out).</param>
            /// <param name="y">The y-coordinate to transform (in and out).</param>
            /// <param name="z">The z-coordinate to transform (in and out, see remarks on <see cref="CoordinateTransformations"/>).</param>
            public void Transform(ref double x, ref double y, ref double z)
            {
                // call every transformation ...
                foreach (TransformDelegate d in chain)
                    d(ref x, ref y, ref z);
            }
        }

        /// <summary>
        /// Builds a transformation identifier out of source and target coordinate reference system identifiers.
        /// </summary>
        /// <param name="sourceId">Identifier of the source coordinate reference system.</param>
        /// <param name="targetId">Identifier of the target coordinate reference system.</param>
        /// <returns>The transformation identifier.</returns>
        private static string GetTransformationKey(string sourceId, string targetId)
        {
            return (sourceId + " -> " + targetId).ToLower();
        }

        /// <summary>
        /// Returns the 'root CRSId' for the given coordinate reference system identifier.
        /// </summary>
        /// <param name="crsId">The identifier of the coordinate reference system to get the root identifier for.</param>
        /// <returns>Root coordinate reference system identifier.</returns>
        private static string GetRootId(this string crsId)
        {
            string crsid = crsId.ToLower();
            return alias_map.ContainsKey(crsid) ? alias_map[crsid] : crsid;
        }
        
        /// <summary>
        /// Gets a managed transformation for the given CRS identifiers.
        /// </summary>
        /// <param name="sourceId">Identifier of the source coordinate reference system.</param>
        /// <param name="targetId">Identifier of the target coordinate reference system.</param>
        /// <returns>The <see cref="TransformDelegate"/> for performing a direct coordinate transformation, which  
        /// may be null, if the specified transformation does not exist.</returns>
        /// <exception cref="TransformationNotFoundException">Thrown if no transformation is available to transform coordinates 
        /// from the specified source to the specified target coordinate reference system.</exception>
        public static TransformDelegate GetTransformation(string sourceId, string targetId)
        {
            if (TryGetTransformation(sourceId, targetId, out var handler))
                return handler;
            
            throw new TransformationNotFoundException(sourceId, targetId);
        }

        /// <summary>
        /// Tries to get a managed transformation for the given CRS identifiers.
        /// </summary>
        /// <param name="sourceId">Identifier of the source coordinate reference system.</param>
        /// <param name="targetId">Identifier of the target coordinate reference system.</param>
        /// <param name="transform">The transformmation deleagte, returned on success.</param>
        /// <returns>True, if a valid managed transformation could be found. False otherwise.</returns>
        public static bool TryGetTransformation(string sourceId, string targetId, out TransformDelegate transform)
        {
            sourceId = sourceId.GetRootId();
            targetId = targetId.GetRootId();

            if (string.Compare(sourceId, targetId, true) != 0)
                return transformations.TryGetValue(GetTransformationKey(sourceId, targetId), out transform);

            transform = new TransformDelegate(CoordinateTransformation.IdentityTransform);
            return true;

        }

        /// <summary>
        /// Tries to get a transformation chain for the given CRS identifiers.
        /// </summary>
        /// <param name="sourceId">Identifier of the source coordinate reference system.</param>
        /// <param name="targetId">Identifier of the target coordinate reference system.</param>
        /// <param name="transform">The transformmation deleagte, returned on success.</param>
        /// <returns>True, if a valid managed transformation chain could be found. False otherwise.</returns>
        private static bool TryGetTransformationChain(string sourceId, string targetId, out TransformDelegate transform)
        {
            transform = null;

            string[][] chains = new string[][] {
                new[] { sourceId, WGS84, targetId },
                new[] { sourceId, PTV_MERCATOR, targetId },
                new[] { sourceId, WGS84, PTV_MERCATOR, targetId },
                new[] { sourceId, PTV_MERCATOR, WGS84, targetId }
            };

            foreach (string[] chain in chains)
            {
                TransformDelegate[] delegates = new TransformDelegate[chain.Length - 1];

                for (int i = 0; delegates != null && i < chain.Length - 1; ++i)
                    if (!TryGetTransformation(chain[i], chain[i + 1], out delegates[i]))
                        delegates = null;

                if (delegates == null) continue;

                transform = new TransformDelegate(new TransformationChain(delegates).Transform);
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Provides an implementation of <see cref="ICoordinateTransformation"/> that uses the managed 
    /// coordinate transformation routines provided by <see cref="CoordinateTransformations"/>.
    /// </summary>
    public class CoordinateTransformation : Ptv.Components.Projections.CoordinateTransformation
    {
        /// <summary>
        /// Hook for providing additional coordinate transformation routines.
        /// </summary>
        public static GetTransformDelegate OnGetTransform = null;

        /// <summary>
        /// The transform delegate used internally by the current instance of <see cref="CoordinateTransformation"/>.
        /// </summary>
        private readonly TransformDelegate transform;

        /// <summary>
        /// Initializes static members of the <see cref="CoordinateTransformation"/> class.
        /// </summary>
        static CoordinateTransformation()
        {
            Enabled = true;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoordinateTransformation"/> class.
        /// </summary>
        /// <param name="transformMethod">The core coordinate transformation method to encapsulate.</param>
        private CoordinateTransformation(MethodInfo transformMethod)
        {
            transform = transformMethod == null ? null :
                (TransformDelegate)Delegate.CreateDelegate(typeof(TransformDelegate), transformMethod);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoordinateTransformation"/> class.
        /// </summary>
        /// <param name="transformMethod">The transform delegate to encapsulate.</param>
        private CoordinateTransformation(TransformDelegate transformMethod)
        {
            transform = transformMethod;
        }

        /// <summary>
        /// Identity transformation.
        /// </summary>
        /// <param name="x">The x-coordinate, returned as passed in.</param>
        /// <param name="y">The y-coordinate, returned as passed in.</param>
        /// <param name="z">The z-coordinate, returned as passed in.</param>
        /// <remarks>
        /// This transformation is used by <see cref="Get(System.String, System.String)">CoordinateTransformation.Get</see> in case the 
        /// identifiers of the source and target coordinate reference system are equal.
        /// </remarks>
        internal static void IdentityTransform(ref double x, ref double y, ref double z)
        {
        }

        /// <summary>
        /// Tries to get a managed coordinate transformation for the specified coordinate reference systems.
        /// </summary>
        /// <param name="sourceId">The source coordinate reference system.</param>
        /// <param name="targetId">The source coordinate reference system.</param>
        /// <param name="transform">The managed coordinate transformation, returned on success.</param>
        /// <returns>True, if a valid managed coordinate transformation could be found a/o created. False otherwise.</returns>
        internal static bool TryGet(string sourceId, string targetId, out ICoordinateTransformation transform)
        {
            transform = null;
            TransformDelegate t = null;

            if (OnGetTransform != null)
                t = OnGetTransform(sourceId, targetId);

            if (t == null) 
            {
                if (string.Compare(sourceId, targetId, true) == 0)
                    t = IdentityTransform;
                else 
                    CoordinateTransformations.TryGetTransformation(sourceId, targetId, out t);
            }

            if (t != null)
                transform = new CoordinateTransformation(t);

            return transform != null;
        }

        /// <summary>
        /// Tries to get a managed coordinate transformation for the specified coordinate reference systems.
        /// </summary>
        /// <param name="source">The source coordinate reference system.</param>
        /// <param name="target">The source coordinate reference system.</param>
        /// <param name="transform">The managed coordinate transformation, returned on success.</param>
        /// <returns>True, if a valid managed coordinate transformation could be found a/o created. False otherwise.</returns>
        internal static bool TryGet(CoordinateReferenceSystem source, CoordinateReferenceSystem target, out ICoordinateTransformation transform)
        {
            transform = null;

            if (source == null || target == null)
                return false;

            return TryGet(source.getId(), target.getId(), out transform);
        }

        /// <summary>
        /// Gets a managed coordinate transformation for the specified coordinate reference system identifiers.
        /// </summary>
        /// <param name="sourceId">Identifier of the source coordinate reference system.</param>
        /// <param name="targetId">Identifier of the target coordinate reference system.</param>
        /// <returns>The managed coordinate transformation, provided through <see cref="ICoordinateTransformation"/>.</returns>
        /// <exception cref="TransformationNotFoundException">Thrown if no transformation is available to transform coordinates 
        /// from the specified source to the specified target coordinate reference system.</exception>
        public new static ICoordinateTransformation Get(string sourceId, string targetId)
        {
            TransformDelegate t = null;

            if (OnGetTransform != null)
                t = OnGetTransform(sourceId, targetId);

            return new CoordinateTransformation(
                t ?? (string.Compare(sourceId, targetId, true) == 0 ? new TransformDelegate(IdentityTransform) : 
                    CoordinateTransformations.GetTransformation(sourceId, targetId))
            );
        }

        /// <summary>
        /// Gets a managed coordinate transformation for the specified coordinate reference systems.
        /// </summary>
        /// <param name="source">The source coordinate reference system.</param>
        /// <param name="target">The source coordinate reference system.</param>
        /// <returns>The managed coordinate transformation, provided through <see cref="ICoordinateTransformation"/>.</returns>
        /// <exception cref="TransformationNotFoundException">Thrown if no transformation is available to transform coordinates 
        /// from the specified source to the specified target coordinate reference system.</exception>
        public new static ICoordinateTransformation Get(CoordinateReferenceSystem source, CoordinateReferenceSystem target)
        {
            if (source == null || target == null)
                throw new TransformationNotFoundException(source.getId(), target.getId());

            return Get(source.getId(), target.getId());
        }

        /// <inheritdoc/>
        public override void Transform<T>(IEnumerable<T> enumerable, Func<T, Location> getLocation, Action<T, Location> setLocation)
        {
            IEnumerator<T> enumerator = enumerable.GetEnumerator();

            while (enumerator.MoveNext())
            {
                T t = enumerator.Current;

                Location l = getLocation(t);

                Transform(l.X, l.Y, l.Z, out l.X, out l.Y, out l.Z);

                setLocation(t, l);
            }
        }

        /// <inheritdoc/>
        public override void Transform<T>(IEnumerable<T> enumerable, Func<T, Point> getPoint, Action<T, Point> setPoint)
        {
            IEnumerator<T> enumerator = enumerable.GetEnumerator();

            Point q = new Point();

            while (enumerator.MoveNext())
            {
                T t = enumerator.Current;

                Point p = getPoint(t);

                Transform(p.X, p.Y, null, out var x, out var y, out var z);

                q.X = x;
                q.Y = y;

                setPoint(t, q);
            }
        }

        /// <inheritdoc/>
        internal override void TransformUnchecked(double xin, double yin, double? zin, out double xout, out double yout, out double? zout)
        {
            xout = xin;
            yout = yin;

            double z = zin.GetValueOrDefault();

            transform(ref xout, ref yout, ref z);

            zout = zin.HasValue ? z : (double?)null;
        }

        /// <inheritdoc/>
        internal override void TransformUnchecked(double[] xin, double[] yin, double[] zin, int idxin, double[] xout, double[] yout, double[] zout, int idxout, int length)
        {
            double? ztmp = null;

            if (zin != null)
            {
                for (int i = 0, j = idxin, k = idxout; i < length; ++i, ++j, ++k)
                {
                    Transform(xin[j], yin[j], zin[j], out xout[k], out yout[k], out ztmp);
                    zout[k] = ztmp.Value;
                }
            }
            else
            {
                for (int i = 0, j = idxin, k = idxout; i < length; ++i, ++j, ++k)
                    Transform(xin[j], yin[j], null, out xout[k], out yout[k], out ztmp);
            }
        }

        /// <inheritdoc/>
        internal override void TransformUnchecked(Point[] pntsIn, Point[] pntsOut)
        {
            for (var i = 0; i < pntsIn.Length; ++i)
            {
                double x = pntsIn[i].X;
                double y = pntsIn[i].Y;
                double z = 0;

                transform(ref x, ref y, ref z);

                pntsOut[i] = new Point(x, y);
            }
        }

        /// <inheritdoc/>
        internal override void TransformUnchecked(Location[] locsIn, Location[] locsOut)
        {
            for (int i = 0; i < locsIn.Length; ++i)
                Transform(locsIn[i].X, locsIn[i].Y, locsIn[i].Z, out locsOut[i].X, out locsOut[i].Y, out locsOut[i].Z);
        }

        /// <inheritdoc/>
        internal override bool Valid => true;

        /// <summary>
        /// Gets or sets a value indicating whether the direct coordinate transformation <see cref="CoordinateTransformation"/>
        /// is used by the <see cref="Ptv.Components.Projections.CoordinateTransformation"/> class.
        /// </summary>
        public static bool Enabled
        {
            get;
            set;
        }
    }
}
