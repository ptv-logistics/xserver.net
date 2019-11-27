// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using System.IO.Packaging;
using System.Windows;
using Ptv.Components.Projections.Proj4;
using Ptv.Components.Projections.Custom;

namespace Ptv.Components.Projections
{
    /// <summary>
    /// <para>
    /// The <see cref="Ptv.Components.Projections"/> namespace contains the classes that are 
    /// essential for projecting or transforming coordinates in GIS-like applications.
    /// </para><para>
    /// <see cref="CoordinateTransformation"/> is the main class for the transformations, offering a 
    /// set of methods for transforming coordinates specified either as <see cref="System.Windows.Point">
    /// System.Windows.Point</see>, <see cref="Location"/> or simply using doubles.
    /// </para>
    /// <para>
    /// <see cref="CoordinateTransformation"/> uses <see cref="CoordinateReferenceSystem"/> to describe the
    /// source and the target of a transformation. The <see cref="Registry"/> provides well known coordinate
    /// reference systems and provides an access using CRS identifiers (e.g. <c>EPSG:4326</c>). See classes
    /// <see cref="CoordinateReferenceSystem.Mapserver">CoordinateReferenceSystem.Mapserver</see> and 
    /// <see cref="CoordinateReferenceSystem.XServer">CoordinateReferenceSystem.XServer</see>,
    /// which provide an access for PTV legacy systems.
    /// </para>
    /// <para>
    /// At its base, the coordinate transformation uses the <a href="http://trac.osgeo.org/proj/">PROJ.4 
    /// Cartographic Projections Library</a>. The <see cref="Registry"/> included is initialized on startup 
    /// with specific PTV coordinate reference systems as well as other well known systems taken from an 
    /// internal EPSG database, defining the coordinate reference systems in the Proj4 well known text format. 
    /// A good and short summary on EPSG and its codes is provided by 
    /// <a href="http://de.wikipedia.org/wiki/European_Petroleum_Survey_Group_Geodesy">Wikipedia</a> 
    /// (german article). A good source for any coordinate reference system that might be missing is
    /// <a href="http://www.spatialreference.org">www.spatialreference.org</a>.
    /// </para>
    /// </summary>
    [System.Runtime.CompilerServices.CompilerGenerated]
    internal class NamespaceDoc
    {
        // NamespaceDoc is used for providing the root documentation for Ptv.Components.Projections
        // hint taken from http://stackoverflow.com/questions/156582/namespace-documentation-on-a-net-project-sandcastle
    }
    

    /// <summary>
    /// Represents an x- and y-coordinate pair with an optional z-coordinate. 
    /// </summary>
    /// <remarks>
    /// The struct uses double precision coordinates and has been introduced to handle special coordinate 
    /// transformations where a z-coordinate is mandatory. For the sake of differentiation the struct has 
    /// been named Location, as several other point structs and classes exist (
    /// <see cref="System.Windows.Point">System.Windows.Point</see>, 
    /// <see cref="System.Drawing.Point">System.Drawing.Point</see>, 
    /// <see cref="System.Drawing.PointF">System.Drawing.PointF</see> and others). In most cases it 
    /// is not necessary to work with Location at all, as the basic transformation routines provided through 
    /// <see cref="ICoordinateTransformation"/> are designed to work with <see cref="System.Windows.Point">
    /// System.Windows.Point</see> as well. 
    /// </remarks>
    public struct Location
    {
        /// <summary>
        /// The x-coordinate of the location.
        /// </summary>
        public double X;

        /// <summary>
        /// The y-coordinate of the location.
        /// </summary>
        public double Y;

        /// <summary>
        /// The optional z-coordinate of the location.
        /// </summary>
        public double? Z;

        /// <summary>
        /// Initializes a new instance of the <see cref="Location"/> struct, including its x- and y-coordinate from the given point.
        /// </summary>
        /// <param name="p">Point to initialize the location with.</param>
        /// <remarks>The z-coordinate keeps its default value, which is null.</remarks>
        public Location(Point p)
            : this(p.X, p.Y)
        {
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Location"/> struct, initializing its coordinates from the given location.
        /// </summary>
        /// <param name="location">Location instance to initialize the location with.</param>
        public Location(Location location)
            : this(location.X, location.Y, location.Z)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Location"/> struct, initializing it with the given coordinates.
        /// </summary>
        /// <param name="x">The x-coordinate of the location.</param>
        /// <param name="y">The y-coordinate of the location.</param>
        /// <param name="z">The optional z-coordinate of the location (defaults to null).</param>
        public Location(double x, double y, double? z = null)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// Returns a string representing of the current location.
        /// </summary>
        /// <returns>String representation of the current location.</returns>
        /// <remarks>A location is either formatted as <c>({x:0.00000}|{y:0.00000})</c> or 
        /// <c>({x:0.00000}|{y:0.00000}|{z:0.00000})</c>, depending on the value of its z-coordinate.
        /// </remarks>
        public override string ToString()
        {
            var z = Z.HasValue ? $"|{Z.Value:0.00000}" : string.Empty;
            return $"({X:0.00000}|{Y:0.00000}{z})";
        }

        /// <summary>
        /// Provides a shortcut for transforming the current location from one 
        /// coordinate reference system to another.
        /// </summary>
        /// <param name="sourceId">Identifies the source CRS, e.g. <c>EPSG:4326</c></param>
        /// <param name="targetId">Identifies the target CRS, e.g. <c>EPSG:3857</c></param>
        /// <returns>Returns the transformed location.</returns>
        /// <remarks>The returned location may be set to its defaults if the transformation 
        /// fails.</remarks>
        /// <example>The following is an example of transforming a location from <c>WGS84</c> 
        /// to <c>Google Mercator</c>:
        /// <code>
        /// // create a location
        /// Location Karlsruhe = new Location(8.4038, 49.0081);
        /// 
        /// // transform location to Google Mercator
        /// Location mapCenter = Karlsruhe.Transform("EPSG:4326", "EPSG:3857");
        /// 
        /// ...
        /// </code>
        /// </example>
        public Location Transform(string sourceId, string targetId)
        {
            return CoordinateTransformation.Get(sourceId, targetId).Transform(this);
        }

        /// <summary>
        /// Provides a shortcut for transforming the current location from one coordinate 
        /// reference system to another.
        /// </summary>
        /// <param name="sourceId">Identifier of the source CRS, e.g. <c>EPSG:4326</c>.</param>
        /// <param name="targetId">Identifier of the target CRS, e.g. <c>EPSG:3857</c>.</param>
        /// <param name="location">On return, contains the transformed Location.</param>
        /// <returns>Returns true, if the transformation succeeded, false otherwise.</returns>
        /// <example>Refer to <see cref="Transform(System.String, System.String)">Location.Transform</see> 
        /// for an example.</example>
        internal bool TryTransform(string sourceId, string targetId, out Location location)
        {
            try
            {
                location = CoordinateTransformation.Get(sourceId, targetId).Transform(this);
                return true;
            }
            catch
            {
                location = new Location();
                return false;
            }
        }
    }
    
    /// <summary>
    /// Represents a single coordinate reference system. Two of them are used when transforming 
    /// coordinates through <see cref="CoordinateTransformation"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="CoordinateReferenceSystem"/> stores the parametrization (Proj4 well known text) and the identifier of 
    /// a CRS. It is the base for any coordinate transformation as it also provides the necessary transformation handles
    /// and is bound to necessary custom pre- or post-transformation. 
    /// </remarks>
	public sealed class CoordinateReferenceSystem
	{
        /// <summary>
        /// Defines well known Mapserver coordinate reference systems.
        /// </summary>
        public static class Mapserver
        {
            /// <summary> Gets the 'PTV Mercator' reference system. </summary>
            public static CoordinateReferenceSystem cfMERCATOR => Registry.Get("cfMERCATOR");

            /// <summary> Gets the 'Gauß-Krüger' reference system. </summary>
            public static CoordinateReferenceSystem cfGK => Registry.Get("cfGK");

            /// <summary> Gets the 'UTM' reference system. </summary>
            public static CoordinateReferenceSystem cfUTM => Registry.Get("cfUTM");

            /// <summary> Gets the 'PTV GeoMinSek' reference system. </summary>
            public static CoordinateReferenceSystem cfGEOMINSEK => Registry.Get("cfGEOMINSEK");

            /// <summary> Gets the 'PTV Geodecimal' reference system. </summary>
            public static CoordinateReferenceSystem cfGEODECIMAL => Registry.Get("cfGEODECIMAL");
        }

        /// <summary> Defines well known XServer coordinate reference systems. </summary>
        public static class XServer
        {
            /// <summary> Gets the 'PTV Mercator' reference system. </summary>
            public static CoordinateReferenceSystem PTV_MERCATOR => Registry.Get("PTV_MERCATOR");

            /// <summary> Gets the 'PTV Geodecimal' reference system. </summary>
            public static CoordinateReferenceSystem PTV_GEODECIMAL => Registry.Get("PTV_GEODECIMAL");

            /// <summary> Gets the 'EPSG:4326' reference system. </summary>
            public static CoordinateReferenceSystem OG_GEODECIMAL => Registry.Get("OG_GEODECIMAL");

            /// <summary> Gets the 'PTV GeoMinSec' reference system. </summary>
            public static CoordinateReferenceSystem PTV_GEOMINSEC => Registry.Get("PTV_GEOMINSEC");

            /// <summary> Gets the 'PTV Smart Units' reference system. </summary>
            public static CoordinateReferenceSystem PTV_SMARTUNITS => Registry.Get("PTV_SMARTUNITS");

            /// <summary> Gets the 'PTV Smart Units' reference system. </summary>
            public static CoordinateReferenceSystem PTV_RASTERSMARTUNITS => Registry.Get("PTV_RASTERSMARTUNITS");

            /// <summary> Gets the 'PTV Superconform' reference system. </summary>
            public static CoordinateReferenceSystem PTV_SUPERCONFORM => Registry.Get("PTV_SUPERCONFORM");

            /// <summary> Gets the 'PTV Conform' reference system. </summary>
            public static CoordinateReferenceSystem PTV_CONFORM => Registry.Get("PTV_CONFORM");
        }

        /// <summary> Name of the parameter used for CRS redirection / aliasing. </summary>
        public const string RedirectionParameter = "+redirect=";

        /// <summary> Name of the parameter used for custom transformations. </summary>
        public const string CustomParameter = "custom";

        /// <summary> Internal PROJ.4 projection handle. </summary>
        private IntPtr pj = IntPtr.Zero;

	    /// <summary> Disposed flag. </summary>
        private bool disposed;

        /// <summary> CRS parameters (Proj4 well known text). </summary>
        private readonly string wkt = "";

        /// <summary> CRS identifier. </summary>
        internal string id = "";

        /// <summary> Lock object for <see cref="CoordinateReferenceSystem.Init"/> method. </summary>
        private readonly object lockInit = Guid.NewGuid();

        /// <summary> Gets the custom transformation associated with this CoordinateReferenceSystem. </summary>
        internal CustomTransformation CustomTransformation { get; private set; }

        /// <summary> The original, unmodified custom transformation  that has been used to construct the coordinate reference system. </summary>
        private readonly CustomTransformation originalCustomTransformation;

        /// <summary> Initializes a new instance of the <see cref="CoordinateReferenceSystem"/> class. </summary>
        /// <remarks> The default constructor initializes a coordinate reference system which is invalid. </remarks>
        internal CoordinateReferenceSystem()
        {
            Initialized = true;
        }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CoordinateReferenceSystem"/> class.
        /// </summary>
        /// <param name="id">Identifier of the CRS, optional.</param>
        /// <param name="wkt">CRS parameters, specified as Proj4 well known text.</param>
        /// <param name="customTransform">The <see cref="CustomTransformation"/> to bind to the CRS (optional).</param>
        /// <param name="lazy">Specifies if the transformation handles are to be created immediately or lazy, as needed.</param>
        /// <remarks>
        /// The identifier of the CRS is not needed to create and initialize the coordinate reference system. The identifier
        /// comes into play when storing the CRS in the <see cref="Registry"/>.
        /// </remarks>
        internal CoordinateReferenceSystem(string id, string wkt, CustomTransformation customTransform = null, bool lazy = false)
        {
            this.id = id.Trim();
            this.wkt = wkt.Trim();

            if (customTransform != null)
                originalCustomTransformation = CustomTransformation = customTransform.Clone();
            
            if (!lazy)
                Init();
        }

        /// <summary>
        /// Gets a value indicating whether a custom transformation is associated with this coordinate reference system.
        /// </summary>
        internal bool HasCustomTransform => CustomTransformation != null && CustomTransformation.Transforms;

	    /// <summary>
        /// Initializes the transformation handles of the coordinate reference system.
        /// </summary>
        /// <returns>Returns true, if the coordinate reference system has been successfully initialized. Returns false otherwise.</returns>
        /// <remarks>Initialization is done only once. Multiple calls to the <see cref="CoordinateReferenceSystem.Init"/> method
        /// do not re-initialize the CRS, even if initialization failed on the first call.</remarks>
        internal bool Init()
        {
            if (Initialized || disposed) return Valid;

            lock (lockInit)
            {
                // run only, if not yet initialized ...
                if (Initialized || disposed) return Valid;

                Initialized = true;

                // if we have a valid WKT, parse it and insert a 
                // DEG to RAD conversion for lat lon systems
                if (wkt.Length > 0 && !IsAlias)
                {
                    pj = Library.Instance.InitProjection(wkt);

                    InitLatLon();
                }
            }

            return Valid;
        }

        /// <summary>
        /// Special initialization for latitude/longitude systems. 
        /// When transformed via PROJ.4, these systems need a DEG/RAD to the expected results.
        /// </summary>
        private void InitLatLon()
        {
            const double DEG_TO_RAD = .0174532925199432958;

            if (!IsLatLon) return;

            var sst = new ShiftScaleTransformation(0, DEG_TO_RAD);

            if (CustomTransformation == null)
                CustomTransformation = sst;
            else
                CustomTransformation.InnerMost = sst;
        }

        /// <summary>
        /// Creates and initializes a new coordinate reference system.
        /// </summary>
        /// <param name="wkt">CRS parameters, specified as Proj4 well known text.</param>
        /// <param name="customTransform">The optional custom transformation to bind to the CRS.</param>
        /// <param name="lazy">Specifies if the transformation handles are to be created immediately or on demand.</param>
        /// <returns>Returns the newly created CoordinateReferenceSystem instance.</returns>
        /// <remarks>
        /// The identifier of a CRS is not needed to create and initialize the <see cref="CoordinateReferenceSystem"/>. 
        /// The identifier comes into play when storing the CRS in the <see cref="Registry"/>.
        /// </remarks>
        /// <example>See <see cref="Parse(System.String, bool)">CoordinateReferenceSystem.Parse</see> for an example.</example>
        internal static CoordinateReferenceSystem Parse(string wkt, CustomTransformation customTransform, bool lazy = false)
        {
            return Parse("", wkt, customTransform, lazy);
        }


        /// <summary>
        /// Creates and initializes a coordinate reference system.
        /// </summary>
        /// <param name="wkt">CRS parameters, specified as Proj4 well known text.</param>
        /// <param name="lazy">Specifies if the transformation handles are to be created immediately or on demand.</param>
        /// <returns>Returns the newly created CoordinateReferenceSystem instance.</returns>
        /// <remarks>
        /// The identifier of a CRS is not needed to create and initialize the <see cref="CoordinateReferenceSystem"/>. 
        /// The identifier comes into play when storing the CRS in the <see cref="Registry"/>.
        /// </remarks>
        /// <example>The following is an example of creating a <c>WGS84</c> like coordinate reference System:
        /// <code>
        /// // EPSG:4326, as taken from www.spatialreference.org
        /// string wkt = "+proj=longlat +ellps=WGS84 +datum=WGS84 +no_defs";
        /// 
        /// // create CRS
        /// CoordinateReferenceSystem crs = CoordinateReferenceSystem.Parse(wkt);
        /// 
        /// // use CRS
        /// ...
        /// </code>
        /// </example>
        public static CoordinateReferenceSystem Parse(string wkt, bool lazy = false)
        {
            return Parse(Guid.NewGuid().ToString(), wkt, null, lazy);
        }

        /// <summary>
        /// Creates and initializes a new coordinate reference system.
        /// </summary>
        /// <param name="id">Identifier of the coordinate reference system, optional.</param>
        /// <param name="wkt">CRS parameters, specified as Proj4 well known text.</param>
        /// <param name="customTransform">The optional custom transformation to bind to the CRS.</param>
        /// <param name="lazy">Specifies if the transformation handles are to be created immediately or on demand.</param>
        /// <returns>Returns the newly created CoordinateReferenceSystem instance.</returns>
        /// <remarks>
        /// The identifier of a CRS is not needed to create and initialize the <see cref="CoordinateReferenceSystem"/>. 
        /// The identifier comes into play when storing the CRS in the <see cref="Registry"/>.
        /// </remarks>
        /// <example>See <see cref="Parse(System.String, bool)">CoordinateReferenceSystem.Parse</see> for an example.</example>
        internal static CoordinateReferenceSystem Parse(string id, string wkt, CustomTransformation customTransform, bool lazy = false)
        {
            var ct = CustomTransformation.Parse(ref wkt, CustomParameter, true);

            if (ct != null)
                ct.InnerMost = customTransform;
            else
                ct = customTransform;

            return new CoordinateReferenceSystem(id, wkt, ct, lazy);
        }


        /// <summary>
        /// Creates and initializes a coordinate reference system.
        /// </summary>
        /// <param name="id">Identifier of the coordinate reference system, optional.</param>
        /// <param name="wkt">CRS parameters, specified as Proj4 well known text.</param>
        /// <param name="lazy">Specifies if the transformation handles are to be created immediately or on demand.</param>
        /// <returns>Returns the newly created CoordinateReferenceSystem instance.</returns>
        /// <remarks>
        /// The identifier of a CRS is not needed to create and initialize the <see cref="CoordinateReferenceSystem"/>. 
        /// The identifier comes into play when storing the CRS in the <see cref="Registry"/>.
        /// </remarks>
        /// <example>The following is an example of creating a <c>WGS84</c> like coordinate reference System:
        /// <code>
        /// // EPSG:4326, as taken from www.spatialreference.org
        /// string wkt = "+proj=longlat +ellps=WGS84 +datum=WGS84 +no_defs";
        /// 
        /// // create CRS
        /// CoordinateReferenceSystem crs = CoordinateReferenceSystem.Parse("WGS84", wkt);
        /// 
        /// // use CRS
        /// ...
        /// </code>
        /// </example>
        internal static CoordinateReferenceSystem Parse(string id, string wkt, bool lazy = false)
        {
            return Parse(id, wkt, null, lazy);
        }


        /// <summary>
        /// Gets a value indicating whether a <see cref="CoordinateReferenceSystem"/> is an alias for another one.
        /// </summary>
        internal bool IsAlias => wkt.StartsWith(RedirectionParameter);

	    /// <summary>
        /// Gets the identifier for which a <see cref="CoordinateReferenceSystem"/> is an alias.
        /// </summary>
        internal string AliasFor => IsAlias ? wkt.Substring(RedirectionParameter.Length).Trim() : "";

	    /// <summary>
        /// Gets a value indicating whether the <see cref="CoordinateReferenceSystem"/> is valid. 
        /// </summary><remarks>
        /// Unless the initialized handle is invalid, <see cref="CoordinateReferenceSystem.Valid"/> method
        /// will return true. Use <see cref="CoordinateReferenceSystem.Init"/> method to force the handle 
        /// initialization.
        /// </remarks>
        internal bool Valid => (!Initialized || pj != IntPtr.Zero) && !disposed;

	    /// <summary>
        /// Gets a value indicating whether the <see cref="CoordinateReferenceSystem"/> has already been initialized.
        /// </summary>
        internal bool Initialized { get; private set; }

	    /// <summary>
        /// Gets the WKT of the associated custom transformation, if any.
        /// </summary>
        private string CustomTransformationWKT
        {
            get
            {
                if (originalCustomTransformation == null)
                    return "";

                return "+" + CustomParameter + "=" + originalCustomTransformation;
            }
        }


        /// <summary>
        /// Gets the parameters of a <see cref="CoordinateReferenceSystem"/> (Proj4 well known text).
        /// </summary>
        public string WKT => (wkt + " " + CustomTransformationWKT).Trim();

	    /// <summary>
        /// Gets a value indicating whether the coordinate reference system is of type latitude/longitude.
        /// </summary>
        private bool IsLatLon => Init() && Library.Instance.IsLatLon(pj);

	    /// <summary>
        /// Gets the identifier of the <see cref="CoordinateReferenceSystem"/>.
        /// </summary>
        /// <remarks>
        /// The identifier of a CRS is empty by default until the <see cref="CoordinateReferenceSystem"/> 
        /// has been stored in the <see cref="Registry"/>.
        /// </remarks>
		public string Id => id;

	    /// <summary>
        /// Gets the transformation handle of the <see cref="CoordinateReferenceSystem"/>.
        /// </summary>
		internal IntPtr Handle
		{
			get
			{
                Init();
                return pj;
			}
		}

        /// <summary>
        /// Converts the <see cref="CoordinateReferenceSystem"/> to a string. 
        /// </summary>
        /// <returns>Returns the same string as <see cref="WKT">CoordinateReferenceSystem.WKT</see>.</returns>
        public override string ToString()
        {
            return WKT;
        }

        /// <summary>
        /// Finalizes an instance of the <see cref="CoordinateReferenceSystem"/> class.
        /// </summary>
        /// <remarks>
        /// This destructor frees native resources that may exist through <a href="http://trac.osgeo.org/proj/">PROJ.4 
        /// Cartographic Projections Library</a>, which is used for some transformations.
        /// </remarks>
        ~CoordinateReferenceSystem()
        {
            Dispose();
        }

        /// <summary>
        /// Disposes the coordinate reference system.
        /// </summary>
        internal void Dispose()
        {
            lock (lockInit)
            {
                disposed = true;

                if (pj == IntPtr.Zero) return;

                Library.Instance.FreeProjection(pj);
                pj = IntPtr.Zero;
            }
        }
    }

    /// <summary>
    /// Manages coordinate reference systems in a reusable way.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="Registry"/> stores <see cref="CoordinateReferenceSystem"/> 
    /// instances and provides an access using CRS identifiers, e.g. <c>EPSG:4326</c>.
    /// </para><para>
    /// On startup, the <see cref="Registry"/> is initialized from an internal EPSG database 
    /// so that it is ready to provide access to all well known coordinate reference systems, 
    /// e.g. <c>EPSG:4326</c> (WGS84), <c>EPSG:76131</c> (PTV Mercator) or <c>EPSG:3857</c> 
    /// (Google Mercator) to name a few. At the very base, the EPSG database included was 
    /// generated out of the EPSG Geodetic Parameter Dataset, which is maintained by the 
    /// Geodesy Subcommittee of OGP. In addition to those EPSG codes, aliases are injected (e.g. 
    /// <see cref="CoordinateReferenceSystem.Mapserver">CoordinateReferenceSystem.Mapserver</see>, 
    /// <see cref="CoordinateReferenceSystem.XServer">CoordinateReferenceSystem.XServer</see>)
    /// to make the access more convenient. For the rare case that coordinate reference systems 
    /// are missing, coordinate reference systems can be added or overwritten using 
    /// <see cref="Registry.Add(System.String, System.String, bool)">Registry.Add</see>.
    /// </para>
    /// </remarks>
    public static class Registry
    {
        /// <summary>
        /// The registry itself.
        /// </summary>
        private static SortedList<string, CoordinateReferenceSystem> internalRegistry;

        /// <summary>
        /// Internal flag indicating if EPSG database was already initialized.
        /// </summary>
        private static bool epsgDatabaseLoaded;

        /// <summary>
        /// Internal lock object used by <see cref="TryGetCoordinateSystem"/>.
        /// </summary>
        private static readonly object lockRegistry = Guid.NewGuid();

        /// <summary>
        /// Accesses the registry, lazily filling in known coordinate reference systems.
        /// </summary>
        /// <param name="requiresFullInitialization">Flag indicating if registry has to be fully initialized on return. Full
        /// initialization forces GetRegistry to read and parse the internal EPSG database as well.</param>
        /// <returns>Returns the coordinate reference system registry.</returns>
        private static SortedList<string, CoordinateReferenceSystem> GetRegistry(bool requiresFullInitialization = true)
        {
            if (internalRegistry == null)
            {
                // create registry
                internalRegistry = new SortedList<string, CoordinateReferenceSystem>();

                // add some core transformations
                Add(CoordinateReferenceSystem.Parse("EPSG:4326", "+proj=longlat +ellps=WGS84 +datum=WGS84 +no_defs", true));
                Add(CoordinateReferenceSystem.Parse("EPSG:31467", "+proj=tmerc +lat_0=0 +lon_0=9 +k=1 +x_0=3500000 +y_0=0 +ellps=bessel +datum=potsdam +units=m +no_defs", true));
                Add(CoordinateReferenceSystem.Parse("EPSG:3857", "+proj=merc +a=6378137 +b=6378137 +lat_ts=0.0 +lon_0=0.0 +x_0=0.0 +y_0=0 +k=1.0 +units=m +nadgrids=@null +wktext  +no_defs", true));
                Add(CoordinateReferenceSystem.Parse("EPSG:900913", "+proj=merc +a=6378137 +b=6378137 +lat_ts=0.0 +lon_0=0.0 +x_0=0.0 +y_0=0 +k=1.0 +units=m +nadgrids=@null +wktext  +no_defs", true));
                Add(CoordinateReferenceSystem.Parse("PTV_GEODECIMAL", "+proj=longlat +ellps=WGS84 +datum=WGS84 +no_defs", new ShiftScaleTransformation(0, 0.00001), true));
                Add(CoordinateReferenceSystem.Parse("EPSG:76131", "+proj=merc +a=6371000 +b=6371000 +lat_ts=0.0 +lon_0=0.0 +x_0=0.0 +y_0=0 +k=1.0 +units=m +nadgrids=@null +no_defs", true));
                Add(CoordinateReferenceSystem.Parse("EPSG:76132", "+proj=merc +a=6371000 +b=6371000 +units=m +lat_0=0.0 +lon_0=0.0 +x_0=4161536.13763304 +y_0=4161536.13763304 +k_0=0.207919962457972 +nadgrids=@null +no_defs", true));
                Add(CoordinateReferenceSystem.Parse("PTV_GEOMINSEC", "+proj=longlat +ellps=WGS84 +datum=WGS84 +no_defs", new GeoMinSekTransformation(), true));

                // add transformations for which no PROJ.4 implementation exists. Must add some dummy systems; they won't transform 
                // through Proj.4 as they are never valid. But thy will work through the direct implementations, if any is provided.
                Add("cfUTM", new CoordinateReferenceSystem("cfUTM", ""), false, false);
                Add("PTV_EUROCONFORM", new CoordinateReferenceSystem("PTV_EUROCONFORM", ""), false, false);
                Add("PTV_CONFORM", new CoordinateReferenceSystem("PTV_CONFORM", ""), false, false);

                // add some aliases
                Add("cfMERCATOR", CoordinateReferenceSystem.RedirectionParameter + "EPSG:76131");
                Add("cfGEODECIMAL", CoordinateReferenceSystem.RedirectionParameter + "PTV_GEODECIMAL");
                Add("cfGK", CoordinateReferenceSystem.RedirectionParameter + "EPSG:31467");
                Add("cfGEOMINSEK", CoordinateReferenceSystem.RedirectionParameter + "PTV_GEOMINSEC");
                Add("EPSG:505456", CoordinateReferenceSystem.RedirectionParameter + "EPSG:76131");
                Add("PTV_MERCATOR", CoordinateReferenceSystem.RedirectionParameter + "EPSG:76131");
                Add("PTV_SMARTUNITS", CoordinateReferenceSystem.RedirectionParameter + "EPSG:76132");
                Add("PTV_RASTERSMARTUNITS", CoordinateReferenceSystem.RedirectionParameter + "EPSG:76132");
                Add("OGC_GEODECIMAL", CoordinateReferenceSystem.RedirectionParameter + "EPSG:4326");
                Add("OG_GEODECIMAL", CoordinateReferenceSystem.RedirectionParameter + "EPSG:4326");
                Add("PTV_SUPERCONFORM", CoordinateReferenceSystem.RedirectionParameter + "PTV_EUROCONFORM");
            }

            if (!requiresFullInitialization || epsgDatabaseLoaded) return internalRegistry;

            try 
            {
                epsgDatabaseLoaded = true;
                ReadDatabase((id, wkt) => Add(id, CoordinateReferenceSystem.Parse(wkt, true), true));
            }
            catch
            {
            }

            return internalRegistry;
        }

        /// <summary>
        /// Reads the internal EPSG database and calls the specified action for every record.
        /// </summary>
        /// <param name="handleRecord">Action to trigger.</param>
        private static void ReadDatabase(Action<string, string> handleRecord)
        {
            using (var resources = Package.Open(Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(Registry).Namespace + ".resources.zip")))
                using (var stream = resources.GetPart(new Uri("/epsg", UriKind.Relative)).GetStream())
                {
                    int idx;
                    var lastWkt = "";
                    var sr = new StreamReader(stream);

                    for (var line = ""; line != null; line = sr.ReadLine())
                        if ((line = line.Trim()).Length > 0 && (idx = line.IndexOf(';')) > 0)
                            handleRecord("EPSG:" + line.Substring(0, idx).Trim(), line.Substring(idx + 1).Trim().Decompress(ref lastWkt));
                }
        }

        /// <summary>
        /// Tries to find a <see cref="CoordinateReferenceSystem"/> in the registry, given its identifier.
        /// </summary>
        /// <param name="id">Identifier of the coordinate reference system, e.g. <c>EPSG:4326</c>.</param>
        /// <param name="cs">Contains the coordinate reference system on return, which may be null 
        /// if no coordinate reference system could be found.</param>
        private static void TryGetCoordinateSystem(string id, out CoordinateReferenceSystem cs)
        {
            lock (lockRegistry)
            {
                GetRegistry(false).TryGetValue(id, out cs);

                if (cs == null && !epsgDatabaseLoaded)
                    GetRegistry().TryGetValue(id, out cs);
            }
        }

        /// <summary>
        /// Creates, initializes and stores a <see cref="CoordinateReferenceSystem"/> in the registry.
        /// </summary>
        /// <param name="id">Identifier of the coordinate references system, e.g. <c>EPSG:4326</c>.</param>
        /// <param name="wkt">Parameters of the coordinate reference system (Proj4 well known text).</param>
        /// <param name="allowReplace">Specifies if an existing coordinate reference system should be replaced, if it exists.</param>
        /// <returns>Returns true, if the coordinate reference system was added to the registry. Returns false otherwise.</returns>
        public static bool Add(string id, string wkt, bool allowReplace = false)
        {
            return Add(id, CoordinateReferenceSystem.Parse(wkt), allowReplace, true);
        }

        /// <summary>
        /// Stores a <see cref="CoordinateReferenceSystem"/> in the registry.
        /// </summary>
        /// <param name="id">Identifier of the coordinate references system, e.g. <c>EPSG:4326</c>.</param>
        /// <param name="cs"><see cref="CoordinateReferenceSystem"/> to add.</param>
        /// <param name="allowReplace">Specifies if an existing coordinate reference system should be replaced, if it exists.</param>
        /// <returns>Returns true, if the coordinate reference system was added to the registry. Returns false otherwise.</returns>
        public static bool Add(string id, CoordinateReferenceSystem cs, bool allowReplace = false)
        {
            return Add(id, cs, allowReplace, true);
        }


        /// <summary>
        /// Stores a <see cref="CoordinateReferenceSystem"/> in the registry.
        /// </summary>
        /// <param name="id">Identifier of the coordinate references system, e.g. <c>EPSG:4326</c>.</param>
        /// <param name="cs"><see cref="CoordinateReferenceSystem"/> to add.</param>
        /// <param name="allowReplace">Specifies if an existing coordinate reference system should be replaced, if it exists.</param>
        /// <param name="mustBeValid">Specifies if the coordinate reference system must be valid</param>
        /// <returns>Returns true, if the coordinate reference system was added to the registry. Returns false otherwise.</returns>
        private static bool Add(string id, CoordinateReferenceSystem cs, bool allowReplace = false, bool mustBeValid = true)
		{
            CoordinateReferenceSystem crs = Get(id);

            // fail if one of the following condition is true:
            // - id is invalid
            // - crs already exists and may not be overwritten
            // - given crs is invalid and not an alias
            // - given crs is an alias referring to an unknown crs
            if (id.Trim().Length < 1 || (crs != null && !allowReplace) || (!cs.Valid && mustBeValid && !cs.IsAlias) || (cs.IsAlias && !Contains(cs.AliasFor)))
                return false;

            lock (lockRegistry)
            {
                GetRegistry()[(cs.id = id).ToLower()] = cs;
            }

            return true;
        }

        /// <summary>
        /// Stores a <see cref="CoordinateReferenceSystem"/> in the registry.
        /// </summary>
        /// <param name="cs"><see cref="CoordinateReferenceSystem"/> to add.</param>
        /// <param name="allowReplace">Specifies if an existing coordinate reference system should be replaced, if it exists.</param>
        /// <returns>Returns true, if the coordinate reference system was added to the registry. Returns false otherwise.</returns>
        /// <remarks>The identifier of the CRS must have been already set in the specified <see cref="CoordinateReferenceSystem"/>.</remarks>
        internal static bool Add(CoordinateReferenceSystem cs, bool allowReplace = false)
        {
            return Add(cs.id, cs, allowReplace);
        }

        /// <summary>
        /// Checks if the registry contains a specific coordinate reference system.
        /// </summary>
        /// <param name="id">Identifier of the coordinate reference system, e.g. <c>EPSG:4326</c></param>
        /// <returns>Returns true, if the registry contains the coordinate reference system. Returns false otherwise.</returns>
        public static bool Contains(string id)
		{
            return Get(id) != null;
        }

        /// <summary>
        /// Tries to find a <see cref="CoordinateReferenceSystem"/> in the registry.
        /// </summary>
        /// <param name="id">Identifier of the coordinate reference system, e.g. <c>EPSG:4326</c>.</param>
        /// <returns>Returns a <see cref="CoordinateReferenceSystem"/> instance or null, if the identifier is 
        /// unknown. 
        /// </returns>
        /// <remarks>
        /// <see cref="Get">Registry.Get</see> automatically resolves redirections and aliases, so that it always 
        /// returns a <see cref="CoordinateReferenceSystem"/> with a valid and applicable 
        /// parametrization in <see cref="CoordinateReferenceSystem.WKT">CoordinateReferenceSystem.WKT</see>. 
        /// As a result, the identifier of the <see cref="CoordinateReferenceSystem"/> returned must not necessarily 
        /// be equal to identifier requested.
        /// </remarks>
        public static CoordinateReferenceSystem Get(string id)
		{
            CoordinateReferenceSystem cs = null;

            List<string> visited = new List<string>();

            do
            {
                id = (cs != null ? cs.AliasFor : id).ToLower();

                if (visited.BinarySearch(id) >= 0)
                    return null;

                TryGetCoordinateSystem(id, out cs);

                visited.Add(id);
                visited.Sort();
            }
            while (cs != null && cs.IsAlias);

            return cs;
		}

        /// <summary>
        /// Returns the identifiers of the coordinate reference systems currently known to the registry.
        /// </summary>
        /// <returns>Returns the list of the coordinate reference system identifiers.</returns>
        public static IList<string> GetIds()
        {
            return GetRegistry().Keys;
        }

        /// <summary>
        /// Returns the contents of the registry in CSV format.
        /// </summary>
        /// <param name="epsgDatabaseOnly">If set to true, returns the contents of the internal EPSG database only. If set to false, returns the current contents of the registry.</param>
        /// <returns>String containing the registry contents in the CSV format <c>id;wkt</c>, one CRS per line.</returns>
        public static string GetContent(bool epsgDatabaseOnly = false)
        {
            var stringBuilder = new StringBuilder();

            if (!epsgDatabaseOnly)
            {
                lock (lockRegistry)
                {
                    SortedList<string, CoordinateReferenceSystem> registry = GetRegistry();

                    foreach (string key in registry.Keys)
                        stringBuilder.Append(registry[key].Id + ";" + registry[key].WKT + "\n");
                }
            }
            else
            {
                try { ReadDatabase((id, wkt) => stringBuilder.Append(id + ";" + wkt + "\n")); }
                catch { stringBuilder = new StringBuilder(); }
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Adds the coordinate references system defined by a CSV formatted string to the registry.
        /// </summary>
        /// <param name="csv">String containing the coordinate reference systems in the format <c>id;wkt</c>, one CRS per line.</param>
        /// <param name="allowReplace">Specifies if an existing coordinate reference system should be replaced, if it exists.</param>
        /// <param name="reinitialize">Specifies if the registry is cleared before adding the coordinate reference system specified by csv.</param>
        /// <remarks>Note that <c>allowReplace</c> and <c>reinitialize</c> are not mutually exclusive. <c>allowReplace</c> is
        /// considered per record and has an effect on duplicates included in the csv data.</remarks>
        public static void SetContent(string csv, bool allowReplace = false, bool reinitialize = false)
        {
            lock (lockRegistry)
            {
                if (reinitialize)
                    Dispose();
            }

            string[] lines = csv?.Split('\n');

            var aliases = new List<CoordinateReferenceSystem>();

            if (lines != null && lines.Length > 0)
                for (int i=0, j; i<lines.Length; ++i)
                {
                    string line = lines[i].Trim();
                    if (line.Length <= 0 || (j = line.IndexOf(';')) <= 0) continue;

                    string id = line.Substring(0, j).Trim();
                    string wkt = line.Substring(j + 1).Trim();

                    var crs = CoordinateReferenceSystem.Parse(id, wkt, true);
                        
                    if (crs.IsAlias)
                        aliases.Add(crs);
                    else
                        Add(crs, allowReplace);
                }

            foreach (CoordinateReferenceSystem crs in aliases)
                Add(crs, allowReplace);
        }

        /// <summary>
        /// Disposes the <see cref="CoordinateReferenceSystem" /> objects currently stored in the registry.
        /// </summary>
        /// <remarks>Does not only dispose the corresponding objects but clears the registry contents also.</remarks>
        public static void Dispose()
        {
            lock (lockRegistry)
            {
                foreach (string key in internalRegistry.Keys)
                    internalRegistry[key].Dispose();

                internalRegistry.Clear();
            }
        }
	}

    /// <summary>
    /// Defines the core coordinate transformation routines. 
    /// </summary>
    /// <remarks>
    /// Use <see cref="CoordinateTransformation.Get(System.String, System.String)">CoordinateTransformation.Get</see> 
    /// to create an instance of <see cref="ICoordinateTransformation"/>.
    /// </remarks>
    public interface ICoordinateTransformation
    {
        /// <summary>
        /// Transforms a single <see cref="System.Windows.Point">System.Windows.Point</see>.
        /// </summary>
        /// <param name="point"><see cref="System.Windows.Point">System.Windows.Point</see> to transform.</param>
        /// <returns>The transformed <see cref="System.Windows.Point">System.Windows.Point</see>.</returns>
        /// <exception cref="TransformationException">Thrown in the unlikely event that a coordinate transformation fails.</exception>
        Point Transform(Point point);

        /// <summary>
        /// Transforms an array of <see cref="System.Windows.Point">System.Windows.Point</see>.
        /// </summary>
        /// <param name="pointsIn">Array containing the points to transform.</param>
        /// <returns>The array containing the transformed points.</returns>
        /// <exception cref="TransformationException">Thrown in the unlikely event that a coordinate transformation fails.</exception>
        Point[] Transform(Point[] pointsIn);

        /// <summary>
        /// Transforms an array of <see cref="System.Windows.Point">System.Windows.Point</see>.
        /// </summary>
        /// <param name="pointsIn">Array containing the points to transform.</param>
        /// <param name="pointsOut">Array in which to put the transformed points.</param>
        /// <remarks>If <c>pointsOut</c> is set to null the transformed locations are written back to 
        /// the input array. It is also valid to specify the input array in <c>pointsOut</c>, which is 
        /// the same as setting <c>pointsOut</c> to null.</remarks>
        /// <exception cref="TransformationException">Thrown in the unlikely event that a coordinate transformation fails.</exception>
        void Transform(Point[] pointsIn, Point[] pointsOut = null);

        /// <summary>
        /// Transforms a single <see cref="Location"/>.
        /// </summary>
        /// <param name="loc"><see cref="Location"/> to transform.</param>
        /// <returns>The transformed <see cref="Location"/>.</returns>
        Location Transform(Location loc);

        /// <summary>
        /// Transforms an array of <see cref="Location"/>.
        /// </summary>
        /// <param name="locationsIn">Array containing the locations to transform.</param>
        /// <returns>The array containing the transformed locations.</returns>
        /// <exception cref="TransformationException">Thrown in the unlikely event that a coordinate transformation fails.</exception>
        Location[] Transform(Location[] locationsIn);
       
        /// <summary>
        /// Transforms an array of <see cref="Location"/>.
        /// </summary>
        /// <param name="locationsIn">Array containing the locations to transform.</param>
        /// <param name="locationsOut">Array in which to put the transformed locations.</param>
        /// <exception cref="TransformationException">Thrown in the unlikely event that a coordinate transformation fails.</exception>
        /// <remarks>
        /// If <c>locationsOut</c> is set to null the transformed locations are written back to the input array. 
        /// It is also valid to specify the input array in <c>locationsOut</c>, which is the same as setting 
        /// <c>locationsOut</c> to null.
        /// </remarks>
        void Transform(Location[] locationsIn, Location[] locationsOut = null);

        /// <summary>
        /// Transforms a set of objects with coordinates.
        /// </summary>
        /// <typeparam name="T">The type of the objects to be transformed.</typeparam>
        /// <param name="enumerable">The enumerable delivering the objects to be transformed.</param>
        /// <param name="getLocation">Function that extracts a <see cref="Location"/> from an enumerated object.</param>
        /// <param name="setLocation">Action that handles a transformed <see cref="Location"/>.</param>
        /// <exception cref="TransformationException">Thrown in the unlikely event that a coordinate transformation fails.</exception>
        /// <remarks>
        /// <para>Please note that this transformation routine is restricted to object types. It cannot handle structs and 
        /// can therefore not be used for sets containing <see cref="Location"/> or <see cref="System.Windows.Point"/>.</para>
        /// <para><c>setLocation</c> is called for every transformation, passing in the original object.</para>
        /// </remarks>
        /// <example>The following is an example for transforming some places from <c>PTV GeoMinSek</c> 
        /// to <c>PTV Mercator</c>:
        /// <code>
        /// class Place
        /// {
        ///    // place location
        ///    public Location Location;
        ///    
        ///    // place name
        ///    public String Name;
        /// }
        /// 
        /// ...
        /// 
        /// // create some places
        ///  Place[] places = new Place[] {
        ///      new Place() { Location = new Location(841090, 5006420), Name = "Frankfurt" },
        ///      new Place() { Location = new Location(1323560, 5230020), Name = "Berlin" }
        ///  };
        /// 
        ///  // get a transformation for transforming from PTV GeoMinSek to PTV Mercator
        ///  ICoordinateTransformation t = CoordinateTransformation.Get(
        ///      CoordinateReferenceSystem.Mapserver.cfGEOMINSEK,
        ///      CoordinateReferenceSystem.XServer.PTV_MERCATOR
        ///  );
        /// 
        ///  // transform the places
        ///  t.Transform&lt;Place&gt;(
        ///      places, place => place.Location,
        ///      (place, location) => place.Location = location
        ///  );
        /// </code>
        /// </example>
        void Transform<T>(IEnumerable<T> enumerable, Func<T, Location> getLocation, Action<T, Location> setLocation) where T : class;

        /// <summary>
        /// Transforms a set of objects with coordinates.
        /// </summary>
        /// <typeparam name="T">The type of the objects to be transformed.</typeparam>
        /// <param name="enumerable">The enumerable delivering the objects to be transformed.</param>
        /// <param name="getPoint">Function that extracts a <see cref="System.Windows.Point">System.Windows.Point</see>
        /// from an enumerated object.</param>
        /// <param name="setPoint">Action that handles a transformed <see cref="System.Windows.Point">System.Windows.Point</see>.</param>
        /// <exception cref="TransformationException">Thrown in the unlikely event that a coordinate transformation fails.</exception>
        /// <remarks>
        /// <para>Please note that this transformation routine is restricted to object types. It cannot handle structs and 
        /// can therefore not be used for sets containing <see cref="Location"/> or <see cref="System.Windows.Point"/>.</para>
        /// <para><c>setLocation</c> is called for every transformation, passing in the original object.</para>
        /// </remarks>
        /// <example>The following is an example for transforming some places from <c>PTV GeoMinSek</c> 
        /// to <c>PTV Mercator</c>:
        /// <code>
        /// class Place
        /// {
        ///    // place location
        ///    public System.Windows.Point Location;
        ///    
        ///    // place name
        ///    public String Name;
        /// }
        /// 
        /// ...
        /// 
        /// // create some places
        ///  Place[] places = new Place[] {
        ///      new Place() { Location = new System.Windows.Point(841090, 5006420), Name = "Frankfurt" },
        ///      new Place() { Location = new System.Windows.Point(1323560, 5230020), Name = "Berlin" }
        ///  };
        /// 
        ///  // get a transformation for transforming from PTV GeoMinSek to PTV Mercator
        ///  ICoordinateTransformation t = CoordinateTransformation.Get(
        ///      CoordinateReferenceSystem.Mapserver.cfGEOMINSEK,
        ///      CoordinateReferenceSystem.XServer.PTV_MERCATOR
        ///  );
        /// 
        ///  // transform the places
        ///  t.Transform&lt;Place&gt;(
        ///      places, place => place.Location,
        ///      (place, p) => place.Location = p
        ///  );
        /// </code>
        /// </example>
        void Transform<T>(IEnumerable<T> enumerable, Func<T, Point> getPoint, Action<T, Point> setPoint) where T : class;

        /// <summary>
        /// Transforms a coordinate.
        /// </summary>
        /// <param name="xin">Value containing the x-coordinate.</param>
        /// <param name="yin">Value containing the y-coordinate.</param>
        /// <param name="xout">On return, contains the transformed x-coordinate.</param>
        /// <param name="yout">On return, contains the transformed y-coordinate.</param>
        /// <exception cref="TransformationException">Thrown in the unlikely event that a coordinate transformation fails.</exception>
        void Transform(double xin, double yin, out double xout, out double yout);

        /// <summary>
        /// Transforms a coordinate.
        /// </summary>
        /// <param name="xin">Value containing the x-coordinate.</param>
        /// <param name="yin">Value containing the y-coordinate.</param>
        /// <param name="zin">Value containing the z-coordinate, nullable.</param>
        /// <param name="xout">On return, contains the transformed x-coordinate.</param>
        /// <param name="yout">On return, contains the transformed y-coordinate.</param>
        /// <param name="zout">On return, contains the transformed z-coordinate.</param>
        /// <exception cref="TransformationException">Thrown in the unlikely event that a coordinate transformation fails.</exception>
        /// <remarks>The returned z-coordinate is set to null if the input z-coordinate was null as well.</remarks>
        void Transform(double xin, double yin, double? zin, out double xout, out double yout, out double? zout);

        /// <summary>
        /// Transforms a set of coordinates.
        /// </summary>
        /// <param name="xin">Array containing the x-coordinates to transform.</param>
        /// <param name="yin">Array containing the y-coordinates to transform.</param>
        /// <param name="xout">Array in which to store the transformed x-coordinates.</param>
        /// <param name="yout">Array in which to store the transformed y-coordinates.</param>
        /// <exception cref="TransformationException">Thrown in the unlikely event that a coordinate transformation fails.</exception>
        /// <remarks>The caller is responsible for allocating memory for the output arrays. It is allowed
        /// to use the input arrays in <c>xout</c> and <c>yout</c>.</remarks>
        void Transform(double[] xin, double[] yin, double[] xout, double[] yout);

        /// <summary>
        /// Transforms a set of coordinates.
        /// </summary>
        /// <param name="xin">Array containing the x-coordinates to transform.</param>
        /// <param name="yin">Array containing the y-coordinates to transform.</param>
        /// <param name="zin">Array containing the z-coordinates to transform.</param>
        /// <param name="xout">Array in which to store the transformed x-coordinates.</param>
        /// <param name="yout">Array in which to store the transformed y-coordinates.</param>
        /// <param name="zout">Array in which to store the transformed z-coordinates.</param>
        /// <exception cref="TransformationException">Thrown in the unlikely event that a coordinate transformation fails.</exception>
        /// <remarks>The caller is responsible for allocating memory for the output arrays. It is allowed
        /// to use the input arrays in <c>xout</c>, <c>yout</c> and <c>zout</c>.</remarks>
        void Transform(double[] xin, double[] yin, double[] zin, double[] xout, double[] yout, double[] zout);

        /*
         * Decision was to keep the following transformation routine out of the public API. 
         * It has been commented out to be able to put it back in quickly.
         *

        /// <summary>
        /// Transforms a set of coordinates.
        /// </summary>
        /// <param name="xin">Array containing the x-coordinates to transform.</param>
        /// <param name="yin">Array containing the y-coordinates to transform.</param>
        /// <param name="zin">Array containing the z-coordinates to transform.</param>
        /// <param name="idxin">Specifies the index of the first coordinate to transform.</param>
        /// <param name="xout">Array in which to store the transformed x-coordinates.</param>
        /// <param name="yout">Array in which to store the transformed y-coordinates.</param>
        /// <param name="zout">Array in which to store the transformed z-coordinates.</param>
        /// <param name="idxout">Specifies the index at which to begin to store transformed coordinates.</param>
        /// <param name="length">Number of coordinates to transform.</param>
        /// <exception cref="TransformationException">Thrown in the unlikely event that a coordinate transformation fails.</exception>
        /// <remarks>The caller is responsible for allocating memory for the output arrays. It is allowed
        /// to use the input arrays in <c>xout</c>, <c>yout</c> and <c>zout</c>.</remarks>
        void Transform(double[] xin, double[] yin, double[] zin, int idxin, double[] xout, double[] yout, double[] zout, int idxout, int length);
         
         * 
         * 
         */
    }

    /// <summary>
    /// Provides the core implementation of the coordinate transformation routines and is the root for 
    /// accessing <see cref="ICoordinateTransformation"/> implementations, each identified by a pair of 
    /// coordinate reference systems (namely source and target). 
    /// </summary>
    public abstract class CoordinateTransformation : ICoordinateTransformation
    {
        /// <summary>
        /// Gets a coordinate transformation for the specified coordinate reference system identifiers.
        /// </summary>
        /// <param name="sourceId">Identifier of the source coordinate reference system.</param>
        /// <param name="targetId">Identifier of the target coordinate reference system.</param>
        /// <returns>The coordinate transformation, provided through <see cref="ICoordinateTransformation"/>.</returns>
        /// <exception cref="TransformationNotFoundException">A TransformationNotFoundException will be thrown if the 
        /// coordinate transformation cannot be constructed.</exception>
        /// <remarks><para><see cref="Get(System.String, System.String)">CoordinateTransformation.Get</see> first tries 
        /// to get a direct transformation from <see cref="Direct.CoordinateTransformation">Direct.CoordinateTransformation</see> 
        /// before it tries to get a transformation from <see cref="Proj4.CoordinateTransformation">Proj4.CoordinateTransformation</see>.
        /// </para><para>Changing the configuration in <see cref="Direct.CoordinateTransformation.Enabled">Direct.CoordinateTransformation.Enabled</see> 
        /// and / or <see cref="Proj4.CoordinateTransformation.Enabled">Proj4.CoordinateTransformation.Enabled</see> may influence this behavior.</para>
        /// </remarks>
        public static ICoordinateTransformation Get(string sourceId, string targetId)
        {
            if (Direct.CoordinateTransformation.Enabled)
                if (Direct.CoordinateTransformation.TryGet(sourceId, targetId, out ICoordinateTransformation transform))
                    return transform;

            if (Proj4.CoordinateTransformation.Enabled)
                return Proj4.CoordinateTransformation.Get(sourceId, targetId);

            throw new TransformationNotFoundException(sourceId, targetId);
        }

        /// <summary>
        /// Gets a coordinate transformation for the specified coordinate reference systems.
        /// </summary>
        /// <param name="source">The source coordinate reference system.</param>
        /// <param name="target">The target coordinate reference system.</param>
        /// <returns>The coordinate transformation, provided through <see cref="ICoordinateTransformation"/>.</returns>
        /// <exception cref="TransformationNotFoundException">A TransformationNotFoundException will be thrown if the 
        /// coordinate transformation cannot be constructed.</exception>
        /// <remarks>See remarks on <see cref="CoordinateTransformation.Get(System.String, System.String)">CoordinateTransformation.Get</see>
        /// </remarks>
        public static ICoordinateTransformation Get(CoordinateReferenceSystem source, CoordinateReferenceSystem target)
        {
            if (Direct.CoordinateTransformation.Enabled)
                if (Direct.CoordinateTransformation.TryGet(source, target, out var transformation))
                    return transformation;

            if (Proj4.CoordinateTransformation.Enabled)
                return Proj4.CoordinateTransformation.Get(source, target);

            throw new TransformationNotFoundException(source.getId(), target.getId());
        }

        /// <summary>
        /// Internal helper that checks if the given arrays have a valid number of elements.
        /// </summary>
        /// <param name="x">Array containing the x-coordinates.</param>
        /// <param name="y">Array containing the y-coordinates.</param>
        /// <param name="z">Array containing the z-coordinates. This array is considered optional.</param>
        /// <param name="idx">Index of the first coordinate.</param>
        /// <param name="length">Number of coordinates.</param>
        /// <returns>True, if the arrays are valid, false otherwise.</returns>
        private static bool ChkLen(double[] x, double[] y, double[] z, int idx, int length)
        {
            bool IsValid(ICollection<double> theArray, bool arrayMayBeNull) => theArray == null ? arrayMayBeNull : idx >= 0 && length > 0 && idx < theArray.Count && (idx + length - 1) < theArray.Count;

            return IsValid(x, false) && IsValid(y, false) && IsValid(z, true);
        }

        /// <summary>
        /// Transforms a set of coordinates.
        /// </summary>
        /// <param name="xin">Array containing the x-coordinates to transform.</param>
        /// <param name="yin">Array containing the y-coordinates to transform.</param>
        /// <param name="zin">Array containing the z-coordinates to transform.</param>
        /// <param name="idxin">Specifies the index of the first coordinate to transform.</param>
        /// <param name="xout">Array in which to store the transformed x-coordinates.</param>
        /// <param name="yout">Array in which to store the transformed y-coordinates.</param>
        /// <param name="zout">Array in which to store the transformed z-coordinates.</param>
        /// <param name="idxout">Specifies the index at which to begin to store transformed coordinates.</param>
        /// <param name="length">Number of coordinates to transform.</param>
        /// <remarks>The caller is responsible for allocating memory for the output arrays. It is allowed
        /// to use the input arrays in <c>xout</c>, <c>yout</c> and <c>zout</c>.</remarks>
        internal abstract void TransformUnchecked(double[] xin, double[] yin, double[] zin, int idxin, double[] xout, double[] yout, double[] zout, int idxout, int length);

        /// <summary>
        /// Transforms a coordinate.
        /// </summary>
        /// <param name="xin">Value containing the x-coordinate.</param>
        /// <param name="yin">Value containing the y-coordinate.</param>
        /// <param name="zin">Value containing the z-coordinate, nullable.</param>
        /// <param name="xout">On return, contains the transformed x-coordinate.</param>
        /// <param name="yout">On return, contains the transformed y-coordinate.</param>
        /// <param name="zout">On return, contains the transformed z-coordinate.</param>
        /// <remarks>The returned z-coordinate is set to null if the input z-coordinate was null as well.</remarks>
        internal abstract void TransformUnchecked(double xin, double yin, double? zin, out double xout, out double yout, out double? zout);

        /// <summary>
        /// Transforms an array of <see cref="System.Windows.Point">System.Windows.Point</see>.
        /// </summary>
        /// <param name="pointsIn">Array containing the points to transform.</param>
        /// <param name="pointsOut">Array in which to put the transformed points.</param>
        /// <remarks>If <c>pointsOut</c> is set to null the transformed locations are written back to 
        /// the input array. It is also valid to specify the input array in <c>pointsOut</c>, which is 
        /// the same as setting <c>pointsOut</c> to null.</remarks>
        /// <exception cref="TransformationException">Thrown in the unlikely event that a coordinate transformation fails.</exception>
        internal abstract void TransformUnchecked(Point[] pointsIn, Point[] pointsOut);

        /// <summary>
        /// Transforms an array of <see cref="Location"/>.
        /// </summary>
        /// <param name="locsIn">Array containing the locations to transform.</param>
        /// <param name="locsOut">Array in which to put the transformed locations.</param>
        /// <exception cref="TransformationException">Thrown in the unlikely event that a coordinate transformation fails.</exception>
        /// <remarks>
        /// If <c>locationsOut</c> is set to null the transformed locations are written back to the input array. 
        /// It is also valid to specify the input array in <c>locationsOut</c>, which is the same as setting 
        /// <c>locationsOut</c> to null.
        /// </remarks>
        internal abstract void TransformUnchecked(Location[] locsIn, Location[] locsOut);

        #region ICoordinateTransformation

        /// <inheritdoc/>
        public virtual Point Transform(Point point)
        {
            Transform(point.X, point.Y, out double x, out double y);
            return new Point(x, y);
        }

        /// <inheritdoc/>
        public virtual Point[] Transform(Point[] pointsIn)
        {
            Point[] pointsOut = pointsIn != null ? new Point[pointsIn.Length] : null;

            Transform(pointsIn, pointsOut);

            return pointsOut;
        }

        /// <inheritdoc/>
        public virtual void Transform(Point[] pointsIn, Point[] pointsOut)
        {
            if (pointsOut == null)
                pointsOut = pointsIn;

            if (pointsIn == null && pointsOut == null)
                return;

            if (pointsIn == null || pointsIn.Length != pointsOut.Length)
                throw new TransformationException("point arrays differ in size");

            TransformUnchecked(pointsIn, pointsOut);
        }

        /// <inheritdoc/>
        public virtual Location Transform(Location loc)
        {

            Transform(loc.X, loc.Y, loc.Z, out double x, out double y, out double? z);
            return new Location(x, y, z);
        }


        /// <inheritdoc/>
        public virtual Location[] Transform(Location[] locationsIn)
        {
            Location[] locsOut = locationsIn != null ? new Location[locationsIn.Length] : null;

            Transform(locationsIn, locsOut);

            return locsOut;
        }

        /// <inheritdoc/>
        public virtual void Transform(Location[] locationsIn, Location[] locationsOut)
        {
            if (locationsOut == null)
                locationsOut = locationsIn;

            if (locationsIn == null && locationsOut == null)
                return;

            if (locationsIn == null || locationsIn.Length != locationsOut.Length)
                throw new TransformationException("location arrays differ in size");

            TransformUnchecked(locationsIn, locationsOut);
        }

        /// <inheritdoc/>
        public virtual void Transform<T>(IEnumerable<T> enumerable, Func<T, Location> getLocation, Action<T, Location> setLocation) where T : class
        {
            int i = 0;

            IEnumerator<T> enumerator = enumerable.GetEnumerator();

            if (!enumerator.MoveNext()) return;

            var hasZ = false;
            var xyz = new double[3][];
            T[] t = new T[1024];

            for (int j = 0; j < 3; j++)
                xyz[j] = new double[t.Length];

            do
            {
                if (i >= xyz[0].Length)
                {
                    Array.Resize(ref t, t.Length * 2);

                    for (int j = 0; j < 3; j++)
                        Array.Resize(ref xyz[j], t.Length);
                }

                Location currentLocation = getLocation(t[i] = enumerator.Current);

                xyz[0][i] = currentLocation.X;
                xyz[1][i] = currentLocation.Y;
                xyz[2][i] = currentLocation.Z.GetValueOrDefault();

                if (i++ == 0)
                    hasZ = currentLocation.Z.HasValue;

                if (hasZ != currentLocation.Z.HasValue)
                    throw new TransformationException("use of z-coordinate differs from previous locations");
            }
            while (enumerator.MoveNext());

            Transform(xyz[0], xyz[1], hasZ ? xyz[2] : null, 0, xyz[0], xyz[1], hasZ ? xyz[2] : null, 0, i);

            var loc = new Location();

            for (int j = 0; j < i; ++j)
            {
                loc.X = xyz[0][j];
                loc.Y = xyz[1][j];

                if (hasZ)
                    loc.Z = xyz[2][j];

                setLocation(t[j], loc);
            }
        }

        /// <inheritdoc/>
        public virtual void Transform<T>(IEnumerable<T> enumerable, Func<T, Point> getPoint, Action<T, Point> setPoint) where T : class
        {
            int i = 0;

            IEnumerator<T> enumerator = enumerable.GetEnumerator();

            if (!enumerator.MoveNext()) return;

            var t = new T[1024];

            var xy = new[] { new double[t.Length], new double[t.Length] };

            do
            {
                if (i >= xy[0].Length)
                {
                    Array.Resize(ref t, t.Length * 2);

                    Array.Resize(ref xy[0], t.Length);
                    Array.Resize(ref xy[1], t.Length);
                }

                Point currentPoint = getPoint(t[i] = enumerator.Current);

                xy[0][i] = currentPoint.X;
                xy[1][i] = currentPoint.Y;

                i++;
            }
            while (enumerator.MoveNext());

            Transform(xy[0], xy[1], null, 0, xy[0], xy[1], null, 0, i);

            var p = new Point();

            for (int j = 0; j < i; ++j)
            {
                p.X = xy[0][j];
                p.Y = xy[1][j];

                setPoint(t[j], p);
            }
        }

        /// <inheritdoc/>
        public virtual void Transform(double xin, double yin, out double xout, out double yout)
        {
            Transform(xin, yin, null, out xout, out yout, out _);
        }

        /// <inheritdoc/>
        public virtual void Transform(double xin, double yin, double? zin, out double xout, out double yout, out double? zout)
        {
            if (!Valid)
                throw new TransformationException("coordinate transformation and/or its associated coordinate reference systems are invalid");

            TransformUnchecked(xin, yin, zin, out xout, out yout, out zout);
        }

        /// <inheritdoc/>
        public virtual void Transform(double[] xin, double[] yin, double[] xout, double[] yout)
        {
            Transform(xin, yin, null, xout, yout, null);
        }

        /// <inheritdoc/>
        public virtual void Transform(double[] xin, double[] yin, double[] zin, double[] xout, double[] yout, double[] zout)
        {
            Transform(xin, yin, zin, 0, xout, yout, zout, 0, xin?.Length ?? 0);
        }

        internal virtual void Transform(double[] xin, double[] yin, double[] zin, int idxin, double[] xout, double[] yout, double[] zout, int idxout, int length)
        {
            // no points, no error :-)
            if (length == 0)
                return;

            // Transformation must be valid
            if (!Valid)
                throw new TransformationException("coordinate transformation and/or its associated coordinate reference systems are invalid");

            // zin, zout: both must be either null or non-null
            if (zin == null != (zout == null))
                throw new TransformationException("input and output z-coordinate array must both be either null or non-null");

            // arrays must be valid regarding indices and length
            if (!ChkLen(xin, yin, zin, idxin, length))
                throw new TransformationException("input coordinate arrays are invalid (regarding length a/o offset)");
            if (!ChkLen(xout, yout, zout, idxout, length))
                throw new TransformationException("output coordinate arrays are invalid (regarding length a/o offset)");

            TransformUnchecked(xin, yin, zin, idxin, xout, yout, zout, idxout, length);
        }

        #endregion

        /// <summary> Gets a value indicating whether a transformation is generally possible.  </summary>
        /// <remarks>
        /// Invalid parameters and lazy initialization of <see cref="CoordinateReferenceSystem"/> 
        /// may cause <see cref="Valid">CoordinateTransformation.Valid</see> to return false.
        /// </remarks>
        internal virtual bool Valid => false;
    }

    /// <summary>
    /// Exception that is thrown if a certain transformation, given a source and 
    /// target coordinate reference system, is not available or not possible.
    /// </summary>
    public class TransformationNotFoundException : Exception
    {
        /// <summary> Initializes a new instance of the <see cref="TransformationNotFoundException"/> class. </summary>
        public TransformationNotFoundException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformationNotFoundException"/> class, setting the provided message string.
        /// </summary>
        /// <param name="message">The message to associate with the exception.</param>
        public TransformationNotFoundException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformationNotFoundException"/> class, setting the provided message string and inner exception.
        /// </summary>
        /// <param name="message">The message to associate with the TransformationNotFoundException.</param>
        /// <param name="innerException">The inner exception to associate with the TransformationNotFoundException.</param>
        public TransformationNotFoundException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformationNotFoundException"/> class, setting the source and target reference systems.
        /// </summary>
        /// <param name="sourceId">The identifier of the source coordinate reference system.</param>
        /// <param name="targetId">The identifier of the target coordinate reference system.</param>
        internal TransformationNotFoundException(string sourceId, string targetId)
            : base("no transformation exists for source=\"" + sourceId + "\" and target=\"" + targetId + "\"")
        {
        }
    }

    /// <summary>
    /// Exception that is thrown in the unlikely event that a coordinate transformation fails.
    /// </summary>
    public class TransformationException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TransformationException"/> class. 
        /// </summary>
        public TransformationException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformationException"/> class. 
        /// </summary>
        /// <param name="message">The message to associate with the exception.</param>
        public TransformationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformationException"/> class. 
        /// </summary>
        /// <param name="message">The message to associate with the TransformationException.</param>
        /// <param name="innerException">The inner exception to associate with the TransformationException.</param>
        public TransformationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    /// <summary>
    /// Internal extension methods.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// Safely gets the id of the given coordinate reference system.
        /// </summary>
        /// <param name="crs">Coordinate reference system to get the id for. May be null.</param>
        /// <param name="nullString">String to return if <paramref name="crs"/> is null. Defaults to "&lt;null&gt;".</param>
        /// <returns>The id of the coordinate reference system.</returns>
        public static string getId(this CoordinateReferenceSystem crs, string nullString = "<null>")
        {
            return crs != null ? crs.Id : nullString;
        }
    }

    /// <summary>
    /// Provides distance calculation routines.
    /// </summary>
    public static class Distance
    {
        /// <summary>
        /// Calculates the haversine distance between to points, specified as lon/lat, aka WGS84, aka EPSG:4326.
        /// </summary>
        /// <param name="x0">Longitude of first point, in degress</param>
        /// <param name="y0">Latitude of first point, in degress</param>
        /// <param name="x1">Longitude of second point, in degress</param>
        /// <param name="y1">Latitude of second point, in degress</param>
        /// <returns>Haversine distance in [km].</returns>
        /// <example><code>
        /// double d_Karlsruhe_Berlin = GetHaversineDistance(8.40376, 49.00808, 13.4114, 52.5234);
        /// </code><code>
        /// double d_Barcelona_Moskau = GetHaversineDistance(2.16992, 41.38793, 37.6177, 55.75586);
        /// </code></example>
        public static double GetHaversineDistance(double x0, double y0, double x1, double y1)
        {
            const double R = 6371;
            const double f = Math.PI / 180.0;

            double hdlon = (1 - Math.Cos((x1 - x0) * f)) / 2.0;
            double hdlat = (1 - Math.Cos((y1 - y0) * f)) / 2.0;

            double a = hdlat + Math.Cos(y0 * f) * Math.Cos(y1 * f) * hdlon;
            double c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(a)));

            return R * c;
        }

        /// <summary>
        /// Extension method that calculates the haversine distance between to 
        /// points using a specific coordinate reference system.
        /// </summary>
        /// <param name="crs">Coordinate reference system</param>
        /// <param name="x0">X-coordinate of first point</param>
        /// <param name="y0">Y-coordinate of first point</param>
        /// <param name="x1">X-coordinate of second point</param>
        /// <param name="y1">Y-coordinate of second point</param>
        /// <returns>Haversine distance in [km].</returns>
        /// <remarks>This extension has been provided for convenience reasons only. It is good enough for single
        /// coordinates but should be used in batch processing scenarios where performance is an issue. On each 
        /// call this extension method gets a coordinate transformation for transforming from the specified CRS 
        /// to EPSG:4326, transforms the specified coordinates to EPSG:4326 and finally uses the transformed 
        /// coordinates with the default, lon/lat based haversine distance calculation. When it comes to 
        /// mass-calculations it is recommended to cache and re-use any coordinate transformation necessary by 
        /// the client thus avoid per coordinate transformation lookups.</remarks>
        /// <example><code>
        /// double d_Frankfurt_Berlin = CoordinateReferenceSystem.XServer.PTV_MERCATOR.GetHaversineDistance(965820, 6458402, 1489888, 6883432);
        /// </code></example>
        public static double GetHaversineDistance(this CoordinateReferenceSystem crs, double x0, double y0, double x1, double y1)
        {
            var t = CoordinateTransformation.Get(crs, CoordinateReferenceSystem.XServer.OG_GEODECIMAL);

            t.Transform(x0, y0, out x0, out y0);
            t.Transform(x1, y1, out x1, out y1);

            return GetHaversineDistance(x0, y0, x1, y1);
        }
    }
}
