using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Ptv.Components.Projections.Custom
{
    /// <summary>
    /// The <see cref="Ptv.Components.Projections.Custom"/> namespace contains classes 
    /// implementing custom pre- and post-transformations for use with <see cref="CoordinateReferenceSystem"/>.
    /// </summary>
    [System.Runtime.CompilerServices.CompilerGenerated]
    class NamespaceDoc
    {
        // NamespaceDoc is used for providing the root documentation for Ptv.Components.Projections
        // hint taken from http://stackoverflow.com/questions/156582/namespace-documentation-on-a-net-project-sandcastle
    }

    /// <summary>
    /// Utility class introducing nested pre- and post-transformation routines. 
    /// </summary>
    /// <remarks>
    /// <para>Custom transformations have been introduced to be able to offer special coordinate reference 
    /// systems such as <c>PTV GeoMinSek</c> or <c>PTV Geodecimal</c>, which are based on <c>WGS84</c> but differ
    /// in formatting.</para>
    /// <para>Custom transformations are also used to handle simple RAD/DEG conversions which are mandatory 
    /// for any coordinate reference systems using latitude/longitude coordinates.</para> 
    /// </remarks>
    internal abstract class CustomTransformation
    {
        /// <summary>
        /// Gets or sets the inner transformation.
        /// </summary>
        internal CustomTransformation InnerTransformation { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomTransformation"/> class.
        /// </summary>
        /// <param name="args">Parameters of <see cref="ShiftScaleTransformation"/>.</param>
        internal CustomTransformation(Stack<String> args)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomTransformation"/> class.
        /// </summary>
        /// <param name="innerTransformation">The inner <see cref="CustomTransformation"/> instance, non-null for nested transformations.</param>
        public CustomTransformation(CustomTransformation innerTransformation = null)
        {
            this.InnerTransformation = innerTransformation;
        }

        /// <summary>
        /// Pre-transformation of the given coordinates. The default implementation 
        /// just calls the inner <see cref="CustomTransformation"/>, if valid. 
        /// </summary>
        /// <param name="x">Value containing the X-coordinate (in and out).</param>
        /// <param name="y">Value containing the y-coordinate (in and out).</param>
        public virtual void Pre(ref double x, ref double y)
        {
            if (InnerTransformation != null)
                InnerTransformation.Pre(ref x, ref y);
        }

        /// <summary>
        /// Post-transformation of the given coordinates. 
        /// </summary>
        /// <remarks>The default implementation just calls the inner 
        /// <see cref="CustomTransformation"/>, if valid.</remarks>
        /// <param name="x">Value containing the x-coordinate (in and out).</param>
        /// <param name="y">Value containing the y-coordinate (in and out).</param>
        public virtual void Post(ref double x, ref double y)
        {
            if (InnerTransformation != null)
                InnerTransformation.Post(ref x, ref y);
        }

        /// <summary>
        /// Gets a value indicating whether any parameters are set in a way that makes pre- or post-transformation necessary.
        /// </summary>
        /// <remarks>The default implementation just calls the inner 
        /// <see cref="CustomTransformation"/>, if valid.</remarks>
        public virtual bool Transforms
        {
            get
            {
                return InnerTransformation != null ? InnerTransformation.Transforms : false;
            }
        }

        /// <summary>
        /// Gets or sets the innermost <see cref="CustomTransformation"/> instance. 
        /// </summary>
        /// <remarks>
        /// Using this property it is possible to nest custom transformations.
        /// </remarks>
        public CustomTransformation InnerMost
        {
            set
            {
                InnerMost.InnerTransformation = value;
            }
            get
            {
                CustomTransformation ct = this;

                while (ct.InnerTransformation != null)
                    ct = ct.InnerTransformation;

                return ct;
            }
        }

        /// <summary>
        /// Pre-transforms the given coordinates.
        /// </summary>
        /// <param name="length">Specifies the number of coordinates to transform.</param>
        /// <param name="ofs">Specifies the index of the first coordinate to transform.</param>
        /// <param name="x">Value containing the x-coordinates.</param>
        /// <param name="y">Value containing the y-coordinates.</param>
        /// <remarks>The resulting coordinates are written back to the arrays provided.</remarks>
        public void Pre(int length, int ofs, double[] x, double[] y)
        {
            if (Transforms)
                for (int i = ofs; i < (ofs + length); ++i)
                    Pre(ref x[i], ref y[i]);
        }

        /// <summary>
        /// Post-transforms the given coordinates.
        /// </summary>
        /// <param name="length">Specifies the number of coordinates to transform.</param>
        /// <param name="ofs">Specifies the index of the first coordinates to transform.</param>
        /// <param name="x">Array containing the x-coordinates.</param>
        /// <param name="y">Array containing the y-coordinates.</param>
        /// <remarks>The resulting coordinates are written back to the arrays provided.</remarks>
        public void Post(int length, int ofs, double[] x, double[] y)
        {
            if (Transforms)
                for (int i = ofs; i < (ofs + length); ++i)
                    Pre(ref x[i], ref y[i]);
        }

        /// <summary>
        /// Returns a string describing the custom transformation chain.
        /// </summary>
        /// <returns>String describing the custom transformation chain.</returns>
        /// <remarks>The transformation chain can be recreated using the returned 
        /// string with <see cref="Parse(ref System.String, System.String, bool)">CustomTransformation.Parse</see>.</remarks>
        public override string ToString()
        {
            return GetType().FullName + (InnerTransformation != null ? "," + InnerTransformation.ToString() : "");
        }

        /// <summary>
        /// Creates a custom transformation chain out of coordinate reference system description.
        /// </summary>
        /// <param name="wkt">The projection parameters, specified as Proj4 well known text.</param>
        /// <param name="param">The name of the parameter containing the custom transformation description.</param>
        /// <param name="remove">If set to true, removes the custom transformation description from the well known text after processing it.</param>
        /// <returns>The <see cref="CustomTransformation"/> matching the description.</returns>
        public static CustomTransformation Parse(ref string wkt, string param, bool remove)
        {
            // i: index of parameter in wkt
            // j: index of parameter's value in wkt
            // k: index of follow up parameter, if any
            // l: adjusted begin of follow up parameter. 
            //    Points to string end if there is no follow up parameter.

            int i = wkt.IndexOf("+" + param + "=");

            if (i < 0)
                return null;

            int j = i == -1 ? -1 : i + 2 + param.Length;
            int k = i == -1 ? -1 : wkt.IndexOf('+', j);
            int l = k < 0 ? wkt.Length : k;

            Stack<String> args = 
                new Stack<String>(wkt.Substring(j, l - j).Trim().Split(',').Reverse());

            CustomTransformation ct = 
                args.Count < 1 ? null : Parse(args);

            if (remove)
                wkt = wkt.Substring(0, i) + (l < wkt.Length ? wkt.Substring(l) : "");

            return ct;
        }

        /// <summary>
        /// Creates a custom transformation chain out of a textual description.
        /// </summary>
        /// <param name="args">The custom transformation description.</param>
        /// <returns>The <see cref="CustomTransformation"/> matching the description.</returns>
        private static CustomTransformation Parse(Stack<String> args)
        {
            CustomTransformation ct = null;

            while (args.Count > 0)
            {
                ConstructorInfo c = Type.GetType(args.Pop()).GetConstructor(BindingFlags.Instance | BindingFlags.Public | 
                    BindingFlags.NonPublic, null, new Type[] { typeof(Stack<String>) }, null );

                CustomTransformation instance = (CustomTransformation)c.Invoke(new object[] { args });
                
                if (ct == null)
                    ct = instance;
                else
                    ct.InnerMost = instance;
            }

            return ct;
        }

        /// <summary>
        /// Returns a deep copy of the <see cref="CustomTransformation"/> object.
        /// </summary>
        /// <returns>The <see cref="CustomTransformation"/>'s clone.</returns>
        public abstract CustomTransformation Clone();
    }

    /// <summary>
    /// Custom transformation that implements a simple shift and scale transform.
    /// </summary>
    /// <remarks>
    /// <para>Pre-transformation: c' = (c + shift) * scale .</para>
    /// <para>Post-transformation: c' = c / scale - shift .</para>
    /// </remarks>
    internal class ShiftScaleTransformation : CustomTransformation
    {
        /// <summary>
        /// Scale and shift values.
        /// </summary>
        private double shx = 0, shy = 0, scx = 1, scy = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShiftScaleTransformation"/> class.
        /// </summary>
        /// <param name="inner">Inner custom transformation (optional).</param>
        public ShiftScaleTransformation(CustomTransformation inner)
            : base(inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShiftScaleTransformation"/> class.
        /// </summary>
        /// <param name="args">Parameters of <see cref="ShiftScaleTransformation"/>.</param>
        internal ShiftScaleTransformation(Stack<String> args)
            : this (Double.Parse(args.Pop()), Double.Parse(args.Pop()), Double.Parse(args.Pop()), Double.Parse(args.Pop()))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShiftScaleTransformation"/> class.
        /// </summary>
        /// <param name="shx">Shift value for the x-coordinate.</param>
        /// <param name="shy">Shift value for the y-coordinate.</param>
        /// <param name="scx">Scale factor for the x-coordinate.</param>
        /// <param name="scy">Scale factor for the y-coordinate.</param>
        /// <param name="inner">Inner custom transformation (optional).</param>
        public ShiftScaleTransformation(double shx, double shy, double scx, double scy, CustomTransformation inner = null)
            : base(inner)
        {
            this.shx = shx;
            this.shy = shy;
            this.scx = scx;
            this.scy = scy;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShiftScaleTransformation"/> class.
        /// </summary>
        /// <param name="shift">Shift value for the x- and y-coordinate.</param>
        /// <param name="scale">Scale factor for the x- and y-coordinate.</param>
        /// <param name="inner">Inner custom transformation (optional).</param>
        public ShiftScaleTransformation(double shift, double scale, CustomTransformation inner = null)
            : base(inner)
        {
            shx = shy = shift;
            scx = scy = scale;
        }

        /// <summary>
        /// Pre-transforms the given coordinates.
        /// </summary>
        /// <remarks>
        /// Uses the formula <c>c' = (c + shift) * scale)</c> for the transformation,
        /// then passes the coordinates on to the inner custom transformation.
        /// </remarks>
        /// <param name="x">Value containing the x-coordinate (in and out).</param>
        /// <param name="y">Value containing the y-coordinate (in and out).</param>
        public override void Pre(ref double x, ref double y)
        {
            x = (x + shx) * scx;
            y = (y + shy) * scy;

            base.Pre(ref x, ref y);
        }


        /// <summary>
        /// Post-transforms the given coordinates.
        /// </summary>
        /// <remarks>
        /// Passes the coordinates on to the inner transformation, then  
        /// transforms the coordinates using the formula <c>c' = c / scale - shift</c>.
        /// </remarks>
        /// <param name="x">Value containing the x-coordinate (in and out).</param>
        /// <param name="y">Value containing the y-coordinate (in and out).</param>
        public override void Post(ref double x, ref double y)
        {
            base.Post(ref x, ref y);

            x = x / scx - shx;
            y = y / scy - shy;
        }

        /// <summary>
        /// Checks if any parameters are set in a way that makes pre- or post-transformation necessary.
        /// </summary>
        /// <remarks>
        /// For the <c>ShiftScaleTransform</c> the transformation is unnecessary if the shift values are 
        /// set to 0 and the scale factors are set to 1.
        /// </remarks>
        public override bool Transforms
        {
            get
            {
                return Math.Abs(shx) > 1e-4 || Math.Abs(shy) > 1e-4 || Math.Abs(scx - 1) > 1e-4 || Math.Abs(scy - 1) > 1e-4 || base.Transforms;
            }
        }

        /// <summary>
        /// Returns a string describing the custom transformation chain.
        /// </summary>
        /// <returns>String describing the custom transformation chain.</returns>
        /// <remarks>The transformation chain can be recreated using the returned 
        /// string with <see cref="CustomTransformation.Parse(ref System.String, System.String, bool)">CustomTransformation.Parse</see>.</remarks>
        public override string ToString()
        {
            return 
                GetType().FullName + "," + shx + "," + shy + "," + scx + "," + scy + 
                (InnerTransformation != null ? "," + InnerTransformation.ToString() : "");
        }

        /// <summary>
        /// Returns a deep copy of the <see cref="ShiftScaleTransformation"/> object.
        /// </summary>
        /// <returns>The <see cref="ShiftScaleTransformation"/>'s clone.</returns>
        public override CustomTransformation Clone()
        {
            return new ShiftScaleTransformation(shx, shy, scx, scy, InnerTransformation != null ? InnerTransformation.Clone() : null);
        }
    }

    /// <summary>
    /// Custom transformation that handles <c>EPSG:4326</c> to <c>PTV GeoMinSek</c> transformation and vice versa.
    /// </summary>
    internal class GeoMinSekTransformation : CustomTransformation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GeoMinSekTransformation"/> class.
        /// </summary>
        /// <param name="inner">Inner custom transformation (optional).</param>
        public GeoMinSekTransformation(CustomTransformation inner = null)
            : base(inner)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeoMinSekTransformation"/> class.
        /// </summary>
        /// <param name="args">Parameters of <see cref="ShiftScaleTransformation"/>.</param>
        internal GeoMinSekTransformation(Stack<String> args)
        {
        }
        
        /// <summary>
        /// Pre-transforms the given coordinates.
        /// </summary><remarks>
        /// Pre-transforms the given coordinates from <c>PTV GeoMinSek</c> to 
        /// <c>EPSG:4326</c>, then passes them on to the inner custom transformation.
        /// </remarks>
        /// <param name="x">Value containing the x-coordinate (in and out).</param>
        /// <param name="y">Value containing the y-coordinate (in and out).</param>
        public override void Pre(ref double x, ref double y)
        {
            GeoMinSekToWgs84(ref x);
            GeoMinSekToWgs84(ref y);

            base.Pre(ref x, ref y);
        }

        /// <summary>
        /// Post-transforms the given coordinates.
        /// </summary><remarks>
        /// Post-transforms the given coordinates using the inner custom transformation,
        /// then transforms the coordinate from <c>EPSG:4326</c> to <c>PTV GeoMinSek</c>.
        /// </remarks>
        /// <param name="x">Value containing the x-coordinate (in and out).</param>
        /// <param name="y">Value containing the y-coordinate (in and out).</param>
        public override void Post(ref double x, ref double y)
        {
            base.Post(ref x, ref y);

            Wgs84ToGeoMinSek(ref x);
            Wgs84ToGeoMinSek(ref y);
        }

        /// <summary>
        /// Transforms a coordinate from <c>EPSG:4326</c> to <c>PTV GeoMinSek</c>.
        /// </summary>
        /// <param name="c">Coordinate to transform (in and out).</param>
        internal static void Wgs84ToGeoMinSek(ref double c)
        {
            double g = c;
            double m = (g - (int)g) * 60;
            double s = (m - (int)m) * 60;

            c = 10 * ((double)(10000 * (int)g) + (double)(100 * (int)m) + s);
        }

        /// <summary>
        /// Transforms a coordinate from <c>PTV GeoMinSek</c> to <c>EPSG:4326</c>.
        /// </summary>
        /// <param name="c">Coordinate to transform (in and out).</param>
        internal static void GeoMinSekToWgs84(ref double c)
        {
            double g = (int)(c / 100000);
            double m = (int)((c / 10 - g * 10000) / 100);
            double s = c / 10 - g * 10000 - m * 100;

            c = g + m / 60 + s / 3600;
        }

        /// <summary>
        /// Checks if any parameters are set in a way that makes pre- or post-transformation necessary.
        /// </summary>
        /// <remarks>
        /// For <c>GeoMinSekTransform</c> the transformation is necessary in an case.
        /// </remarks>
        public override bool Transforms
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Returns a deep copy of the <see cref="GeoMinSekTransformation"/> object.
        /// </summary>
        /// <returns>The <see cref="GeoMinSekTransformation"/>'s clone.</returns>
        public override CustomTransformation Clone()
        {
            return new GeoMinSekTransformation(InnerTransformation != null ? InnerTransformation.Clone() : null);
        }
    }
}
