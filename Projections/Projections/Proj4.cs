// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Runtime.InteropServices;
using System.IO;
using System.IO.Packaging;
using System.Reflection;
using System.Windows;

namespace Ptv.Components.Projections.Proj4
{
    /// <summary>
    /// Provides the classes for transforming coordinates through the <a href="http://trac.osgeo.org/proj/">PROJ.4 
    /// Cartographic Projections Library</a>. The core transformations provided by <see cref="CoordinateTransformation"/> can 
    /// either be used directly or implicitly through 
    /// <see cref="Ptv.Components.Projections.CoordinateTransformation">Ptv.Components.Projections.CoordinateTransformation</see>, 
    /// which uses <see cref="CoordinateTransformation"/> where necessary.
    /// </summary>
    [System.Runtime.CompilerServices.CompilerGenerated]
    class NamespaceDoc
    {
        // NamespaceDoc is used for providing the root documentation for Ptv.Components.Projections.Proj4
        // hint taken from http://stackoverflow.com/questions/156582/namespace-documentation-on-a-net-project-sandcastle
    }

    /// <summary>
    /// Defines the interface of the unmanaged <a href="http://trac.osgeo.org/proj/">PROJ.4 Cartographic Projections Library</a>.
    /// </summary>
    public interface ICoordinateTransformation
    {
        /// <summary>
        /// Initializes a projection.
        /// </summary>
        /// <param name="wkt">The projection parameters, specified as Proj4 well known text.</param>
        /// <returns>The projection handle which will be invalid on any error.</returns>
        [ApiFactory.DllImport(CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Ansi, EntryPoint = "initProjection")]
        IntPtr InitProjection(string wkt);

        /// <summary>
        /// Checks if a projection is using latitude/longitude coordinates.
        /// </summary>
        /// <param name="pj">The projection to check.</param>
        /// <returns>True, if the projection is using latitude/longitude coordinates, false otherwise.</returns>
        /// <remarks>This check is used to automatically handle DEG/RAD conversion in 
        /// transformations based on PROJ.4.</remarks>
        [ApiFactory.DllImport(CallingConvention = CallingConvention.StdCall, EntryPoint = "isLatLon")]
        bool IsLatLon(IntPtr pj);

        /// <summary>
        /// Frees a projection.
        /// </summary>
        /// <param name="pj">The projection to free.</param>
        [ApiFactory.DllImport(CallingConvention = CallingConvention.StdCall, EntryPoint = "freeProjection")]
        void FreeProjection(IntPtr pj);

        /// <summary>
        /// Transforms a set of coordinates.
        /// </summary>
        /// <param name="src">The source projection.</param>
        /// <param name="dst">The target projection.</param>
        /// <param name="cnt">Number of coordinates.</param>
        /// <param name="ofs">Index of the first coordinate to transform.</param>
        /// <param name="x">Array containing the x-coordinates.</param>
        /// <param name="y">Array containing the y-coordinates.</param>
        /// <param name="z">Array containing the z-coordinates. This parameter is optional and may be set to null.</param>
        /// <returns>Zero on success, a PROJ.4 error code otherwise.</returns>
        /// <remarks>
        /// If no error occurred, the specified arrays contain the transformed coordinates on return.
        /// </remarks>
        [ApiFactory.DllImport(CallingConvention = CallingConvention.StdCall, EntryPoint="transformPoints")]
        int Transform(IntPtr src, IntPtr dst, int cnt, int ofs, double[] x, double[] y, double[] z);

        /// <summary>
        /// Transforms a set of coordinates.
        /// </summary>
        /// <param name="src">The source projection.</param>
        /// <param name="dst">The target projection.</param>
        /// <param name="cnt">Number of coordinates.</param>
        /// <param name="ofs">Index of the first coordinate to transform.</param>
        /// <param name="x">Array containing the x-coordinates.</param>
        /// <param name="y">Array containing the y-coordinates.</param>
        /// <returns>Zero on success, a PROJ.4 error code otherwise.</returns>
        /// <remarks>
        /// If no error occurred, the specified arrays contain the transformed coordinates on return.
        /// </remarks>
        [ApiFactory.DllImport(CallingConvention = CallingConvention.StdCall, EntryPoint = "transformSimplePoints")]
        int Transform(IntPtr src, IntPtr dst, int cnt, int ofs, double[] x, double[] y);

        /// <summary>
        /// Transforms a single coordinate.
        /// </summary>
        /// <param name="src">The source projection.</param>
        /// <param name="dst">The target projection.</param>
        /// <param name="x">The x-coordinate to transform (in and out).</param>
        /// <param name="y">The y-coordinate to transform (in and out).</param>
        /// <param name="z">The z-coordinate to transform (in and out).</param>
        /// <returns>Zero on success, a PROJ.4 error code otherwise.</returns>
        [ApiFactory.DllImport(CallingConvention = CallingConvention.StdCall, EntryPoint = "transformPoint")]
        int Transform(IntPtr src, IntPtr dst, ref double x, ref double y, ref double z);

        /// <summary>
        /// Transforms a single coordinate.
        /// </summary>
        /// <param name="src">The source projection.</param>
        /// <param name="dst">The target projection.</param>
        /// <param name="x">The x-coordinate to transform (in and out).</param>
        /// <param name="y">The y-coordinate to transform (in and out).</param>
        /// <returns>Zero on success, a PROJ.4 error code otherwise.</returns>
        [ApiFactory.DllImport(CallingConvention = CallingConvention.StdCall, EntryPoint = "transformSimplePoint")]
        int Transform(IntPtr src, IntPtr dst, ref double x, ref double y);
    }

    /// <summary>
    /// Default implementation of <see cref="ICoordinateTransformation"/>.
    /// </summary>
    internal class Proj4Default : ICoordinateTransformation
    {
        /// <summary>
        /// Initializes a projection.
        /// </summary>
        /// <param name="wkt">The projection parameters, specified as Proj4 well known text.</param>
        /// <returns><c>IntPtr.Zero</c> in any case.</returns>
        public IntPtr InitProjection(string wkt)
        {
            return IntPtr.Zero;
        }

        /// <summary>
        /// Gets a value indicating whether a projection is using latitude/longitude coordinates.
        /// </summary>
        /// <param name="pj">The projection to check.</param>
        /// <returns>False in any case.</returns>
        public bool IsLatLon(IntPtr pj)
        {
            return false;
        }

        /// <summary>
        /// Frees a projection.
        /// </summary>
        /// <param name="pj">The projection to free.</param>
        public void FreeProjection(IntPtr pj)
        {
           
        }

        /// <summary>
        /// Transforms a set of coordinates.
        /// </summary>
        /// <param name="src">The source projection.</param>
        /// <param name="dst">The target projection.</param>
        /// <param name="cnt">Number of coordinates.</param>
        /// <param name="ofs">Index of the first coordinate to transform.</param>
        /// <param name="x">Array containing the x-coordinates.</param>
        /// <param name="y">Array containing the y-coordinates.</param>
        /// <param name="z">Array containing the z-coordinates. This parameter is optional and may be set to null.</param>
        /// <returns>E_FAIL in any case.</returns>
        public int Transform(IntPtr src, IntPtr dst, int cnt, int ofs, double[] x, double[] y, double[] z)
        {
            return unchecked((int)0x80004005);
        }

        /// <summary>
        /// Transforms a set of coordinates.
        /// </summary>
        /// <param name="src">The source projection.</param>
        /// <param name="dst">The target projection.</param>
        /// <param name="cnt">Number of coordinates.</param>
        /// <param name="ofs">Index of the first coordinate to transform.</param>
        /// <param name="x">Array containing the x-coordinates.</param>
        /// <param name="y">Array containing the y-coordinates.</param>
        /// <returns>E_FAIL in any case.</returns>
        public int Transform(IntPtr src, IntPtr dst, int cnt, int ofs, double[] x, double[] y)
        {
            return unchecked((int)0x80004005);
        }

        /// <summary>
        /// Transforms a single coordinate.
        /// </summary>
        /// <param name="src">The source projection.</param>
        /// <param name="dst">The target projection.</param>
        /// <param name="x">The x-coordinate to transform (in and out).</param>
        /// <param name="y">The y-coordinate to transform (in and out).</param>
        /// <param name="z">The z-coordinate to transform (in and out).</param>
        /// <returns>E_FAIL in any case.</returns>
        public int Transform(IntPtr src, IntPtr dst, ref double x, ref double y, ref double z)
        {
            return unchecked((int)0x80004005);
        }

        /// <summary>
        /// Transforms a single coordinate.
        /// </summary>
        /// <param name="src">The source projection.</param>
        /// <param name="dst">The target projection.</param>
        /// <param name="x">The x-coordinate to transform (in and out).</param>
        /// <param name="y">The y-coordinate to transform (in and out).</param>
        /// <returns>E_FAIL in any case.</returns>
        public int Transform(IntPtr src, IntPtr dst, ref double x, ref double y)
        {
            return unchecked((int)0x80004005);
        }
    }

    /// <summary>
    /// Provides the core <see cref="ICoordinateTransformation"/> implementation and manages the PROJ.4 library initialization.
    /// </summary>
    internal class Library
    {
        /// <summary>
        /// The <see cref="ICoordinateTransformation"/> instance, initialized 
        /// on demand by <see cref="Instance">Library.Instance</see>.
        /// </summary>
        private static ICoordinateTransformation instance = null;

        /// <summary>
        /// Initialization lock.
        /// </summary>
        private static readonly object instanceLock = Guid.NewGuid();

        /// <summary>
        /// Gets the possible names of the core PROJ.4 library.
        /// </summary>
        private static string[] LibraryNames
        {
            get
            {
                string thisName = Assembly.GetExecutingAssembly().Location;
                string baseName = "Proj.4-Core." + (IntPtr.Size == 4 ? "x86" : "x64");
                
#if DEBUG
                string[] suffix = new string[] { "d", "" };
#else
                string[] suffix = new string[] { "", "d" };
#endif

                return new string[] {
                    baseName + suffix[0] + Path.GetExtension(thisName),
                    baseName + suffix[1] + Path.GetExtension(thisName)
                };
            }
        }

        /// <summary>
        /// Tries to load the core PROJ.4 library from a file.
        /// </summary>
        /// <param name="name">File name of the library.</param>
        /// <returns>True, if library was loaded, false otherwise.</returns>
        private static bool TryLoad(string name)
        {
            string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                Path.DirectorySeparatorChar + name;

            if (File.Exists(path))
                try { instance = ApiFactory.CreateNativeApi<ICoordinateTransformation>(path); }
                catch { instance = null; }

            return instance != null;
        }

        /// <summary>
        /// This helper fully reads the contents of a stream.
        /// </summary>
        /// <param name="stm">The stream to read.</param>
        /// <returns>The data read from the stream.</returns>
        private static byte[] Read(Stream stm)
        {
            byte[] buffer = new byte[16384];

            using (MemoryStream m = new MemoryStream())
            {
                for (int read; (read = stm.Read(buffer, 0, buffer.Length)) > 0; )
                    m.Write(buffer, 0, read);

                return m.ToArray();
            }
        }

        /// <summary>
        /// Tries to load the core PROJ.4 library from a resource package.
        /// </summary>
        /// <param name="name">File name of the library.</param>
        /// <returns>True, if library was loaded, false otherwise.</returns>
        private static bool TryLoadResource(string name)
        {
            try
            {
                byte[] raw;

                using (Package resources = Package.Open(Assembly.GetExecutingAssembly().GetManifestResourceStream(typeof(CoordinateReferenceSystem).Namespace + ".resources.zip")))
                {
                    Uri partUri = new Uri("/" + name, UriKind.Relative);

                    if (!resources.PartExists(partUri))
                        return false;

                    using (Stream dllStm = resources.GetPart(partUri).GetStream())
                        raw = Read(dllStm);
                }

                File.WriteAllBytes(name = TempSpace.TryMakeSpace() + Path.DirectorySeparatorChar + name, raw);
            }
            catch
            {
                return false;
            }

            try { instance = ApiFactory.CreateNativeApi<ICoordinateTransformation>(name); }
            catch { instance = null; }

            return instance != null;
        }

        /// <summary>
        /// Gets the <see cref="ICoordinateTransformation"/> implementation.
        /// </summary>
        public static ICoordinateTransformation Instance
        {
            get
            {
                if (instance == null)
                    lock (instanceLock)
                        if (instance == null)
                        {
                            if (Proj4.CoordinateTransformation.Enabled)
                            {
                                foreach (string name in LibraryNames)
                                    if (TryLoad(name))
                                        break;

                                if (instance == null)
                                    foreach (string name in LibraryNames)
                                        if (TryLoadResource(name))
                                            break;
                            }

                            if (instance == null)
                                instance = new Proj4Default();
                        }

                return instance;
            }
        }

        /// <summary> Gets a value indicating whether the core was already initialized. </summary>
        public static bool HasInstance => instance != null;
    }

    /// <summary>
    /// Provides an implementation of <see cref="Ptv.Components.Projections.ICoordinateTransformation"/> 
    /// that uses the <a href="http://trac.osgeo.org/proj/">PROJ.4 Cartographic Projections Library</a>.
    /// </summary>
    public class CoordinateTransformation : Ptv.Components.Projections.CoordinateTransformation
    {
        /// <summary>
        /// Source coordinate reference system.
        /// </summary>
        private readonly CoordinateReferenceSystem source = null;

        /// <summary>
        /// Target coordinate reference system.
        /// </summary>
        private readonly CoordinateReferenceSystem target = null;

        /// <summary>
        /// Helper performing array initialization.
        /// </summary>
        /// <param name="idx_in">Index of the first coordinate in the source arrays.</param>
        /// <param name="idx_out">Index of the first coordinate in the target arrays.</param>
        /// <param name="length">Number of coordinate to transform.</param>
        /// <param name="arrays">Source and target arrays, specified in pairs <c>{ x-source, x-target, ... }.</c></param>
        /// <remarks>
        /// <para>
        /// This helper initializes the target arrays with the coordinates from the source array. 
        /// In case that the target arrays are equal to the source arrays, no copy process is started.
        /// </para><para>
        /// Used by <see cref="TransformUnchecked(double[], double[], double[], int, double[], double[], double[], int, int)"/>.
        /// </para>
        /// </remarks>
        private void InitArrays(int idx_in, int idx_out, int length, params double[][] arrays)
        {
            for (int i = 0; i < arrays.Length; i += 2)
                if (arrays[i] != null && arrays[i] != arrays[i + 1] || idx_in != idx_out)
                    Array.Copy(arrays[i], idx_in, arrays[i + 1], idx_out, length);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoordinateTransformation"/> class.
        /// </summary>
        /// <param name="source">Source coordinate reference system.</param>
        /// <param name="target">Target coordinate reference system.</param>
        private CoordinateTransformation(CoordinateReferenceSystem source, CoordinateReferenceSystem target)
        {
            this.source = source;
            this.target = target;

            if (source == null || !source.Valid || target == null || !target.Valid)
                throw new TransformationNotFoundException(source.getId(), target.getId());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoordinateTransformation"/> class.
        /// </summary>
        /// <param name="sourceId">Identifier of the source coordinate reference system.</param>
        /// <param name="targetId">Identifier of the target coordinate reference system.</param>
        private CoordinateTransformation(String sourceId, String targetId)
        {
            source = Registry.Get(sourceId);
            target = Registry.Get(targetId);

            if (source == null || !source.Valid || target == null || !target.Valid)
                throw new TransformationNotFoundException(sourceId, targetId);
        }

        /// <summary>
        /// Gets a PROJ.4 coordinate transformation for the specified coordinate reference systems.
        /// </summary>
        /// <param name="source">The source coordinate reference system.</param>
        /// <param name="target">The target coordinate reference system.</param>
        /// <returns>The PROJ.4 coordinate transformation, provided through <see cref="Ptv.Components.Projections.ICoordinateTransformation"/>.</returns>
        /// <exception cref="TransformationNotFoundException">Thrown if no transformation is available to transform coordinates 
        /// from the specified source to the specified target coordinate reference system.</exception>
        public new static Ptv.Components.Projections.ICoordinateTransformation Get(CoordinateReferenceSystem source, CoordinateReferenceSystem target)
        {
            return new CoordinateTransformation(source, target);
        }

        /// <summary>
        /// Gets a PROJ.4 coordinate transformation for the specified coordinate reference system identifiers.
        /// </summary>
        /// <param name="sourceId">Identifier of the source coordinate reference system.</param>
        /// <param name="targetId">Identifier of the target coordinate reference system.</param>
        /// <returns>The PROJ.4 coordinate transformation, provided through <see cref="Ptv.Components.Projections.ICoordinateTransformation"/>.</returns>
        /// <exception cref="TransformationNotFoundException">Thrown if no transformation is available to transform coordinates 
        /// from the specified source to the specified target coordinate reference system.</exception>
        public new static Ptv.Components.Projections.ICoordinateTransformation Get(String sourceId, String targetId)
        {
            return new CoordinateTransformation(sourceId, targetId);
        }

        /// <inheritdoc/>
        internal override void TransformUnchecked(double xin, double yin, double? zin, out double xout, out double yout, out double? zout)
        {
            xout = xin;
            yout = yin;
            zout = null;

            if (source.HasCustomTransform)
                source.CustomTransformation.Pre(ref xout, ref yout);

            int errorCode;

            if (!zin.HasValue)
                errorCode = Library.Instance.Transform(source.Handle, target.Handle, ref xout, ref yout);
            else
            {
                double ztmp = zin.GetValueOrDefault();
                errorCode = Library.Instance.Transform(source.Handle, target.Handle, ref xout, ref yout, ref ztmp);

                if (errorCode == 0)
                    zout = ztmp;
            }

            if (errorCode != 0)
                throw new TransformationException("coordinate transformation failed with error code " + errorCode);

            if (target.HasCustomTransform)
                target.CustomTransformation.Post(ref xout, ref yout);
        }

        /// <inheritdoc/>
        internal override void TransformUnchecked(double[] xin, double[] yin, double[] zin, int idxin, double[] xout, double[] yout, double[] zout, int idxout, int length)
        {
            // copy input elements to output arrays
            InitArrays(idxin, idxout, length, xin, xout, yin, yout, zin, zout);

            // pre transformation
            if (source.HasCustomTransform)
                source.CustomTransformation.Pre(length, idxout, xout, yout);

            // main transformation
            int errorCode = Library.Instance.Transform(source.Handle, target.Handle, length, idxout, xout, yout, zout);

            if (errorCode != 0)
                throw new TransformationException("coordinate transformation failed with error code " + errorCode);

            // post transformation
            if (target.HasCustomTransform)
                target.CustomTransformation.Post(length, idxout, xout, yout);
        }

        /// <inheritdoc/>
        internal override void TransformUnchecked(Point[] pntsIn, Point[] pntsOut)
        {
            double[][] xy =
                new double[][] { new double[pntsIn.Length], new double[pntsIn.Length] };

            for (int i = 0; i < pntsIn.Length; ++i)
            {
                xy[0][i] = pntsIn[i].X;
                xy[1][i] = pntsIn[i].Y;
            }

            Transform(xy[0], xy[1], xy[0], xy[1]);

            for (int i = 0; i < pntsIn.Length; ++i)
            {
                pntsOut[i].X = xy[0][i];
                pntsOut[i].Y = xy[1][i];
            }
        }

        /// <inheritdoc/>
        internal override void TransformUnchecked(Location[] locsIn, Location[] locsOut)
        {
            double[][] xyz =
                new double[][] { new double[locsIn.Length], new double[locsIn.Length], new double[locsIn.Length] };

            bool hasZ = locsIn.Length > 0 && locsIn[0].Z.HasValue;

            for (int i = 0; i < locsIn.Length; ++i)
            {
                xyz[0][i] = locsIn[i].X;
                xyz[1][i] = locsIn[i].Y;
                xyz[2][i] = locsIn[i].Z.GetValueOrDefault();

                if (hasZ != locsIn[i].Z.HasValue)
                    throw new TransformationException("use of z-coordinate differs from previous locations");
            }

            Transform(xyz[0], xyz[1], hasZ ? xyz[2] : null, 0, xyz[0], xyz[1], hasZ ? xyz[2] : null, 0, locsIn.Length);

            for (int i = 0; i < locsIn.Length; ++i)
            {
                locsOut[i].X = xyz[0][i];
                locsOut[i].Y = xyz[1][i];
                locsOut[i].Z = xyz[2][i];
            }
        }

        /// <inheritdoc/>
        internal override bool Valid => source.Init() && target.Init();

        /// <summary>
        /// Internal flag for <see cref="Enabled"/> property.
        /// </summary>
        private static bool enabled = true;

        /// <summary>
        /// Gets or sets a value indicating whether the core PROJ.4 coordinate transformation is used by the 
        /// <see cref="Ptv.Components.Projections.CoordinateTransformation"/> class.
        /// </summary>
        /// <remarks>
        /// <see cref="Enabled"/> is mainly used for disabling the unmanaged extensions used by implementation of 
        /// <see cref="CoordinateTransformation"/>. As those extensions are initialized with the first transformation triggered, 
        /// <see cref="Enabled"/> must be called prior to using any coordinate transformation to have an effect. To indicate 
        /// such problem, <see cref="Enabled"/> will throw an exception when trying to disable the PROJ.4 transformation after 
        /// using any coordinate transformation.
        /// </remarks>
        public static bool Enabled
        {
            get
            {
                return enabled;
            }
            set
            {
                if (enabled != value)
                {
                    if (Library.HasInstance && !value)
                        throw new Exception("PROJ.4 transformation must be disabled before using any coordinate transformation.");

                    enabled = value;
                }
            }
        }
    }

    /// <summary>
    /// Internal extension providing some helpers to the <see cref="Registry"/>.
    /// </summary>
    internal static class WktExtensions
    {
        /// <summary> Returns the first n characters of a string.  </summary>
        /// <param name="s">The string to process.</param>
        /// <param name="n">Number of characters to return.</param>
        /// <returns>The first <c>Min(n, s.Length)</c> characters of the specified string.</returns>
        public static String Left(this String s, uint n)
        {
            if (string.IsNullOrEmpty(s) || n >= s.Length)
                return s;
            else if (n == 0)
                return "";
            else
                return s.Substring(0, (int)Math.Min(s.Length, n));
        }

        /// <summary>
        /// Returns the last n characters of a string. 
        /// </summary>
        /// <param name="s">The string to process.</param>
        /// <param name="n">Number of characters to return.</param>
        /// <returns>The last <c>Min(n, s.Length)</c> characters of the specified string.</returns>
        public static String Right(this String s, uint n)
        {
            if (string.IsNullOrEmpty(s) || n >= s.Length)
                return s;
            else if (n == 0)
                return "";
            else
                return s.Substring((int)(s.Length - Math.Min(s.Length, n)));
        }

        /// <summary>
        /// Decompresses a WKT record.
        /// </summary>
        /// <param name="wkt">The current WKT record.</param>
        /// <param name="lastWkt">The last WKT record.</param>
        /// <returns>The decompressed WKT record.</returns>
        /// <remarks>
        /// Decompression is based on the assumption:<br/>
        /// - first record contains a full WKT description<br/>
        /// - all other records are in the form "l;wkt;r" with wkt being a remainder 
        /// and l and r the numbers of characters to reuse from the previous WKT description 
        /// (from left and right respectively).
        /// </remarks>
        internal static String Decompress(this String wkt, ref String lastWkt)
        {
            try
            {
                if (lastWkt == null)
                    return "";

                String[] fields = wkt.Split(';');

                uint nl = uint.Parse(fields[0]);
                uint nr = uint.Parse(fields[2]);

                return lastWkt = lastWkt.Left(nl) + fields[1] + lastWkt.Right(nr);
            }
            catch
            {
                lastWkt = null;
                return "";
            }
        }
    }
}
