// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Ptv.Components.Projections
{
    class Program
    {        
        public static CoreProjectionInfo[] rawProjectionData = new CoreProjectionInfo[] {
            new CoreProjectionInfo() { code = 76131, proj4 = "+proj=merc +a=6371000 +b=6371000 +lat_ts=0.0 +lon_0=0.0 +x_0=0.0 +y_0=0 +k=1.0 +units=m +nadgrids=@null +no_defs", esri = "PROJCS[\"WGS 84 / Pseudo-Mercator\",GEOGCS[\"Popular Visualisation CRS\",DATUM[\"D_Popular_Visualisation_Datum\",SPHEROID[\"Popular_Visualisation_Sphere\",6371000,0]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"Meter\",1]]" },
            new CoreProjectionInfo() { code = 3857, proj4 = "+proj=merc +a=6378137 +b=6378137 +lat_ts=0.0 +lon_0=0.0 +x_0=0.0 +y_0=0 +k=1.0 +units=m +nadgrids=@null +wktext +no_defs", esri = "PROJCS[\"WGS 84 / Pseudo-Mercator\",GEOGCS[\"Popular Visualisation CRS\",DATUM[\"D_Popular_Visualisation_Datum\",SPHEROID[\"Popular_Visualisation_Sphere\",6378137,0]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",0],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",0],PARAMETER[\"false_northing\",0],UNIT[\"Meter\",1]]" },
            new CoreProjectionInfo() { code = 25833, proj4 = "+proj=utm +zone=33 +ellps=GRS80 +units=m +no_defs", esri="PROJCS[\"ETRS89 / UTM zone 33N\",GEOGCS[\"ETRS89\",DATUM[\"D_ETRS_1989\",SPHEROID[\"GRS_1980\",6378137,298.257222101]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",15],PARAMETER[\"scale_factor\",0.9996],PARAMETER[\"false_easting\",500000],PARAMETER[\"false_northing\",0],UNIT[\"Meter\",1]]" },
            new CoreProjectionInfo() { code = 4326, proj4 = "+proj=longlat +ellps=WGS84 +datum=WGS84 +no_defs", esri="GEOGCS[\"GCS_WGS_1984\",DATUM[\"D_WGS_1984\",SPHEROID[\"WGS_1984\",6378137,298.257223563]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.017453292519943295]]" },
            new CoreProjectionInfo() { code = 31467, proj4 = "+proj=tmerc +lat_0=0 +lon_0=9 +k=1 +x_0=3500000 +y_0=0 +ellps=bessel +datum=potsdam +units=m +no_defs", esri="PROJCS[\"DHDN / Gauss-Kruger zone 3\",GEOGCS[\"DHDN\",DATUM[\"D_Deutsches_Hauptdreiecksnetz\",SPHEROID[\"Bessel_1841\",6377397.155,299.1528128]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",9],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",3500000],PARAMETER[\"false_northing\",0],UNIT[\"Meter\",1]]" },
            new CoreProjectionInfo() { code = 31468, proj4 = "+proj=tmerc +lat_0=0 +lon_0=12 +k=1 +x_0=4500000 +y_0=0 +ellps=bessel +datum=potsdam +units=m +no_defs", esri="PROJCS[\"DHDN / Gauss-Kruger zone 4\",GEOGCS[\"DHDN\",DATUM[\"D_Deutsches_Hauptdreiecksnetz\",SPHEROID[\"Bessel_1841\",6377397.155,299.1528128]],PRIMEM[\"Greenwich\",0],UNIT[\"Degree\",0.017453292519943295]],PROJECTION[\"Transverse_Mercator\"],PARAMETER[\"latitude_of_origin\",0],PARAMETER[\"central_meridian\",12],PARAMETER[\"scale_factor\",1],PARAMETER[\"false_easting\",4500000],PARAMETER[\"false_northing\",0],UNIT[\"Meter\",1]]" }
        };

        public static List<SortedList<Int32, ProjectionInfo>> projections =
            new List<SortedList<Int32, ProjectionInfo>>();

        public static Transform[] transformers = new Transform[] {
            new Proj4Transform(),
            // new DotSpatialTransform(),
            // new DotSpatialTransformVia4326(),
            // new ProjNetTransform()
        };

        public static TestPoint[] testPoints = new TestPoint[] {
            new TestPoint() { epsgCode = 76131, p = new Location(1401229, 7297586) },
            new TestPoint() { epsgCode = 76131, p = new Location(1565661, 7199672) },
            new TestPoint() { epsgCode = 4326, p = new Location(8, 49) },
            new TestPoint() { epsgCode = 3857, p = new Location(1567415, 7207737) },
            new TestPoint() { epsgCode = 31467, p = new Location(3831498, 6019443) }
        };

        [System.Runtime.InteropServices.DllImport("TrKoorNT.dll")]
        static extern short SetCoorType (short ct);

        [System.Runtime.InteropServices.DllImport("TrKoorNT.dll")]
        static extern short KoorTrans (short transTyp, int xEin, int yEin, ref int xAus, ref int yAus, ref short zone);

        class Place
        {
            // place location
            public Location Location;

            // place name
            public String Name;
        }

        static void SimpleTest()
        {
            // create some places
            var places = new Place[] {
                new Place { Location = new Location(841090, 5006420), Name = "Frankfurt" },
                new Place { Location = new Location(1323560, 5230020), Name = "Berlin" }
            };

            // get a transformation for transforming from PTV GeoMinSek to PTV Mercator
            ICoordinateTransformation t = CoordinateTransformation.Get(
                CoordinateReferenceSystem.Mapserver.cfGEOMINSEK,
                CoordinateReferenceSystem.XServer.PTV_MERCATOR
            );

            // transform the places
            t.Transform<Place>(
                places,
                place => place.Location,
                (place, loc) => place.Location = loc
            );

            double d_Frankfurt_Berlin = CoordinateReferenceSystem.XServer.PTV_MERCATOR.GetHaversineDistance(965820, 6458402, 1489888, 6883432);

            Console.WriteLine("-- simple test --\n");

            // Location l = new Location(1528170, 6623338);
            // Location m = new Location(33411907, 5656638);

            // Console.WriteLine("" + m + " == " + l.Transform("cfMERCATOR", "cfUTM"));

            TestBase.Run();

            Console.WriteLine("\n<press return to continue>");
            Console.ReadLine();
        }

        static void ExtendedTest()
        {
            Console.WriteLine("-- extended test --\n");

            foreach (Transform dummy in transformers)
                projections.Add(new SortedList<int, ProjectionInfo>());

            foreach (CoreProjectionInfo cpi in rawProjectionData)
                for (int j = 0; j < transformers.Length; j++)
                    ProjectionInfo.addNew(projections[j], cpi, transformers[j]);

            TestPoint[] reference =
                new TestPoint[testPoints.Length * projections[0].Count];

            ConsoleColor defaultFgColor =
                Console.ForegroundColor;

            for (int i = 0; i < projections.Count; i++)
            {
                SortedList<int, ProjectionInfo> projectionInfos = projections[i];

                for (int j = 0; j < testPoints.Length; j++)
                {
                    for (int k = 0; k < projectionInfos.Count; k++)
                    {
                        int refIdx = k * testPoints.Length + j;

                        KeyValuePair<int, ProjectionInfo> projectionInfo = projectionInfos.ElementAt(k);

                        DateTime then = DateTime.Now;
                        TestPoint p = new TestPoint();

                        Exception _ex = null;

                        int m = 1;
                        for (int l = 0; l < m; l++)
                            try
                            {
                                p = projectionInfo.Value.transform(testPoints[j]);
                            }
                            catch (Exception ex)
                            {
                                _ex = _ex ?? ex;
                                p = new TestPoint { epsgCode = projectionInfo.Key };
                            }


                        StringBuilder output = new StringBuilder(
                            $"{projectionInfo.Value.transformer.Name}: {testPoints[j]} ==> {p} in {(double) (1000 * (DateTime.Now - then).TotalMilliseconds / m):0.00}µs");

                        ConsoleColor fgColor = defaultFgColor;

                        if (i == 0)
                            reference[refIdx] = p;
                        else
                        {
                            Delta delta = reference[refIdx] - p;
                            output.Append($", delta={delta.d:0.00} (dx={delta.dx:0.00}, dy={delta.dy:0.00})");

                            if (delta.d > 1e-1)
                                fgColor = ConsoleColor.Red;
                        }

                        Console.ForegroundColor = fgColor;
                        Console.WriteLine(output.ToString());

                        if (_ex != null)
                        {
                            Console.WriteLine("caught " + _ex.GetType().Name + ", message: " + _ex.Message);
                            Console.WriteLine(_ex.StackTrace.ToString());
                        }

                        Console.ForegroundColor = defaultFgColor;
                    }
                }
            }

            Console.WriteLine("\n<press return to continue>");
            Console.ReadLine();
        }
        
        static void Main(string[] args)
        {
            SimpleTest();

            Console.SetWindowSize(120, 60);

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            string r1 = Registry.GetContent();
            string r2 = Registry.GetContent(true);

            Direct.CoordinateTransformation.Enabled = true;

            double x = 8, y = 49;
            CoordinateTransformation.Get("EPSG:4326", "PTV_MERCATOR").Transform(x, y, out x, out y);

            string wkt = Registry.Get("cfGeoMinSek").WKT;
            
            MessageBox.Show(wkt);

            CoordinateReferenceSystem.Parse("+proj=longlat +ellps=WGS84 +datum=WGS84 +no_defs +custom=Ptv.Components.Projections.Custom.GeoMinSekTransformation");

            Registry.SetContent(r1, true, true);

            ICoordinateTransformation t = Proj4.CoordinateTransformation.Get("cfGEOMINSEK", "EPSG:76131");

            System.Windows.Point p = 
                new System.Windows.Point(959361, 5332563);

            t.Transform(p);

            SimpleTest();
            ExtendedTest();
        }
    }

    public class Delta
    {
        public double dy;
        public double dx;

        public double d => Math.Sqrt(dx * dx + dy * dy);
    }

    public class TestPoint
    {
        public int epsgCode = -1;
        public Location p = new Location();

        public override string ToString()
        {
            return $"{epsgCode},{p.X:0.00},{p.Y:0.00}";
        }

        public static Delta operator -(TestPoint p, TestPoint q)
        {
            return new Delta
            {
                dx = p.p.X - q.p.X,
                dy = p.p.Y - q.p.Y  
            };
        }
    }

    public interface Transform
    {
        Location transform(object from, object to, Location p);
        object infoFromWkt(CoreProjectionInfo info);
        String Name { get; }
    }

    public class CoreProjectionInfo
    {
        public int code;
        public string proj4;
        public string esri;
    }

    public class ProjectionInfo : CoreProjectionInfo
    {
        public object info;
        public Transform transformer;
        private SortedList<Int32, ProjectionInfo> projections;

        private ProjectionInfo()
        {
        }

        public static void addNew(SortedList<Int32, ProjectionInfo> projections, CoreProjectionInfo cpi, Transform transformer)
        {
            projections[cpi.code] = new ProjectionInfo
            {
                code = cpi.code,
                proj4 = cpi.proj4,
                transformer = transformer,
                projections = projections,
                info = transformer.infoFromWkt(cpi)
            };
        }

        public TestPoint transform(TestPoint p)
        {
            return new TestPoint { epsgCode = code, p = transformer.transform(projections[p.epsgCode].info, info, p.p) };
        }
    }

    public class Proj4Transform : Transform
    {
        public Location transform(object from, object to, Location p)
        {
            return CoordinateTransformation.Get(from as CoordinateReferenceSystem, to as CoordinateReferenceSystem).Transform(p);
        }


        public object infoFromWkt(CoreProjectionInfo info)
        {
            return Registry.Get("EPSG:" + info.code);
        }


        public string Name => "PROJ.4";
    }

    /*
    public class DotSpatialTransform : Transform
    {
        double[] xy = new double[2];
        double[] z = new double[1];

        public Location transform(object from, object to, Location p)
        {
            xy[0] = p.X;
            xy[1] = p.Y;
            z[0] = 0;

            DotSpatial.Projections.Reproject.ReprojectPoints(xy, z, from as DotSpatial.Projections.ProjectionInfo, to as DotSpatial.Projections.ProjectionInfo, 0, 1);

            return new Location(xy[0], xy[1]);
        }


        public object infoFromWkt(CoreProjectionInfo info)
        {
            return DotSpatial.Projections.ProjectionInfo.FromProj4String(info.proj4);
        }


        public string Name
        {
            get { return "DotSpatial"; }
        }
    }


    public class DotSpatialTransformVia4326 : Transform
    {
        double[] xy = new double[2];
        double[] z = new double[1];

        DotSpatial.Projections.ProjectionInfo pi =
            DotSpatial.Projections.ProjectionInfo.FromProj4String("+proj=longlat +ellps=WGS84 +datum=WGS84 +no_defs");

        public Location transform(object from, object to, Location p)
        {
            xy[0] = p.X;
            xy[1] = p.Y;
            z[0] = 0;

            DotSpatial.Projections.Reproject.ReprojectPoints(xy, z, from as DotSpatial.Projections.ProjectionInfo, pi, 0, 1);
            DotSpatial.Projections.Reproject.ReprojectPoints(xy, z, pi, to as DotSpatial.Projections.ProjectionInfo, 0, 1);

            return new Location(xy[0], xy[1]);
        }


        public object infoFromWkt(CoreProjectionInfo info)
        {
            return DotSpatial.Projections.ProjectionInfo.FromProj4String(info.proj4);
        }


        public string Name
        {
            get { return "DotSpatial-via-4326"; }
        }
    }
    */

    /*
    public class ProjNetTransform : Transform
    {
        double[] xy = new double[2];

        ProjNet.CoordinateSystems.Transformations.CoordinateTransformationFactory ctFactory =
            new ProjNet.CoordinateSystems.Transformations.CoordinateTransformationFactory();
        ProjNet.CoordinateSystems.CoordinateSystemFactory csFactory =
            new ProjNet.CoordinateSystems.CoordinateSystemFactory();

        public Location transform(object from, object to, Location p)
        {
            xy[0] = p.X;
            xy[1] = p.Y;

            double[] _xy = ctFactory.CreateFromCoordinateSystems(
                (ProjNet.CoordinateSystems.ICoordinateSystem)from,
                (ProjNet.CoordinateSystems.ICoordinateSystem)to).MathTransform.Transform(xy);

            return new Location(_xy[0], _xy[1]);
        }

        public object infoFromWkt(CoreProjectionInfo info)
        {
            return ProjNet.Converters.WellKnownText.CoordinateSystemWktReader.Parse(info.esri);
        }

        public string Name
        {
            get { return "ProjNet"; }
        }
    }
    */

    internal class TestBase
    {
        private readonly Random random = new Random();

        private readonly ICoordinateTransformation directTransformation = Direct.CoordinateTransformation.Get("cfGEOMINSEK", "EPSG:76131");

        private readonly ICoordinateTransformation proj4Transformation = Proj4.CoordinateTransformation.Get("cfGEOMINSEK", "EPSG:76131");

        private static readonly System.Windows.Point p = new System.Windows.Point(959361, 5332563);

        ICoordinateTransformation GetTransformation(bool bDirect)
        {
            return bDirect ? directTransformation : proj4Transformation;
        }

        private readonly double[][] bulk_XY_1000 = new double[][] {
            GetX(GetPoints(1000)),
            GetY(GetPoints(1000)),
            new double[1000],
            new double[1000]
        };

        private readonly System.Windows.Point[][] bulk_1000 = new System.Windows.Point[][] {
            GetPoints(1000),
            new System.Windows.Point[1000]
        };

        private readonly double[][] bulk_XY_100000 = new double[][] {
            GetX(GetPoints(100000)),
            GetY(GetPoints(100000)),
            new double[100000],
            new double[100000]
        };

        private readonly System.Windows.Point[][] bulk_100000 = new System.Windows.Point[][] {
            GetPoints(100000),
            new System.Windows.Point[100000]
        };
        
        static System.Windows.Point[] GetPoints(int size, bool zeroInit = false)
        {
            System.Windows.Point[] a = new System.Windows.Point[size];

            for (int i = 0; i < size; i++)
                a[i] = new System.Windows.Point(zeroInit ? 0 : p.X, zeroInit ? 0 : p.Y);

            return a;
        }

        static double[] GetX(System.Windows.Point[] points)
        {
            var d = new double[points.Length];

            for (int i = 0; i < d.Length; ++i)
                d[i] = points[i].X;

            return d;
        }

        static double[] GetY(System.Windows.Point[] points)
        {
            var d = new double[points.Length];

            for (int i = 0; i < d.Length; ++i)
                d[i] = points[i].Y;

            return d;
        }

        private TestBase()
        {
        }

        private void Run(System.Reflection.MethodInfo mi, TestAttribute ta)
        {
            proj4Transformation.Transform(new Location());

            try
            {
                double x = 0, y = 0;
                object[] args = new object[] { ta, null, x, y };

                HiResTimer timer = new HiResTimer();

                bool bSucceeded = ta.Enabled;
                
                if (bSucceeded)
                    try
                    {
                        mi.Invoke(this, args);
                    }
                    catch
                    {
                        bSucceeded = false;
                    }

                double d = timer.Elapsed;

                if (ta.Enabled)
                    Console.WriteLine("{0}: {1} after {2:0}ms ({3:0}ns per point), x={4:0}, y={5:0}",
                        ta.TestName, bSucceeded ? "succeeded" : "FAILED", d, 1000000.0 * d / ta.PerPoint, args[2], args[3]);
            }
            catch(Exception ex)
            {
                Console.WriteLine("Test \"" + mi.Name + "\" failed with " + ex.GetType().FullName + ": " + ex.Message);
                Console.WriteLine(ex.StackTrace.ToString());
            }
        }

        public static void Run()
        {
            TestBase t = new TestBase();

            foreach (System.Reflection.MethodInfo mi in t.GetType().GetMethods())
            {
                object[] customAttributes = mi.GetCustomAttributes(typeof(TestAttribute), true);

                if (customAttributes.Length == 1)
                    t.Run(mi, customAttributes[0] as TestAttribute);
            }
        }

        [Test("Single-Direct", null, 500000, true)]
        public bool Single_Direct(TestAttribute ta, object setup, ref double x, ref double y)
        {
            ICoordinateTransformation t = GetTransformation(true);

            for (int i = 0; i < ta.PerPoint; ++i)
            {
                x = p.X;
                y = p.Y;

                double? z;
                t.Transform(x, y, null, out x, out y, out z);
            }

            return true;
        }

        [Test("Single-Transform-Direct-XY", null, 500000, true)]
        public void Single_Transform__Direct_XY(TestAttribute ta, object setup, ref double x, ref double y)
        {
            ICoordinateTransformation t = GetTransformation(true);

            for (int i = 0; i < ta.PerPoint; ++i)
                t.Transform(p.X, p.Y, out x, out y);
        }

        [Test("Single-Transform-Direct-Point", null, 500000, true)]
        public void Single_Transform_Direct_Point(TestAttribute ta, object setup, ref double x, ref double y)
        {
            System.Windows.Point pOut =
                new System.Windows.Point();

            ICoordinateTransformation t = GetTransformation(true);

            for (int i = 0; i < ta.PerPoint; ++i)
                pOut = t.Transform(p);

            x = pOut.X;
            y = pOut.Y;
        }

        [Test("Single-Transform-Proj-XY", null, 500000, true)]
        public void Single_Transform_Proj_XY(TestAttribute ta, object setup, ref double x, ref double y)
        {
            ICoordinateTransformation t = GetTransformation(false);

            for (int i = 0; i < ta.PerPoint; ++i)
                t.Transform(p.X, p.Y, out x, out y);
        }

        [Test("Single-Transform-Proj-Point", null, 500000, true)]
        public void Single_Transform_Proj_Point(TestAttribute ta, object setup, ref double x, ref double y)
        {
            ICoordinateTransformation t = GetTransformation(false);

            System.Windows.Point pOut =
                new System.Windows.Point();

            for (int i = 0; i < ta.PerPoint; ++i)
                pOut = t.Transform(p);

            x = pOut.X;
            y = pOut.Y;
        }

        [Test("Bulk-1000-Direct-XY", null, 500000, true)]
        public void Bulk_1000_Direct_XY(TestAttribute ta, object setup, ref double x, ref double y)
        {
            double[][] xy = bulk_XY_1000;

            int j = random.Next(xy[0].Length);

            ICoordinateTransformation t = GetTransformation(true);

            for (int i = 0; i < ta.PerPoint / xy[0].Length; ++i)
            {
                t.Transform(xy[0], xy[1], null, xy[2], xy[3], null);

                x = xy[2][j];
                y = xy[3][j];
            }
        }

        [Test("Bulk-100000-Direct-XY", null, 500000, true)]
        public void Bulk_100000_Direct_XY(TestAttribute ta, object setup, ref double x, ref double y)
        {
            double[][] xy = bulk_XY_100000;

            int j = random.Next(xy[0].Length);

            ICoordinateTransformation t = GetTransformation(true);

            for (int i = 0; i < ta.PerPoint / xy[0].Length; ++i)
            {
                t.Transform(xy[0], xy[1], null, xy[2], xy[3], null);

                x = xy[2][j];
                y = xy[3][j];
            }
        }

        public void Bulk_Transform_XY(double[][] xy, bool bProj, TestAttribute ta, object setup, ref double x, ref double y)
        {
            int j = random.Next(xy[0].Length);

            ICoordinateTransformation t = GetTransformation(!bProj);

            for (int i = 0; i < ta.PerPoint / xy[0].Length; ++i)
            {
                t.Transform(xy[0], xy[1], xy[2], xy[3]);

                x = xy[2][j];
                y = xy[3][j];
            }
        }

        public void Bulk_Transform_Point(System.Windows.Point[][] xy, bool bProj, TestAttribute ta, object setup, ref double x, ref double y)
        {
            int j = random.Next(xy[0].Length);

            ICoordinateTransformation t = GetTransformation(!bProj);

            for (int i = 0; i < ta.PerPoint / xy[0].Length; ++i)
            {
                xy[1] = t.Transform(xy[0]);

                x = xy[1][j].X;
                y = xy[1][j].Y;
            }
        }

        class Test
        {
            public System.Windows.Point p;
        }

        public void Bulk_Transform_Enum(System.Windows.Point[][] xy, bool test, bool bProj, TestAttribute ta, object setup, ref double x, ref double y)
        {
            Console.WriteLine("--- WARNING --- Bulk_Transform_Enum has been rewritten, name is misleading ---");

            int j = random.Next(xy[0].Length);

            List<Test> _test = new List<Test>();

            for (int i=0; i<xy[0].Length; i++)
                _test.Add(new Test { p = xy[0][i] });

            ICoordinateTransformation t = GetTransformation(!bProj);

            t.Transform<Test>(_test, u => u.p, (u, v) => u.p = v);

            for (int i = 0; i < ta.PerPoint / xy[0].Length; ++i)
            {
                t.Transform(xy[0], xy[1]);

                x = xy[1][j].X;
                y = xy[1][j].Y;
            }
        }
        
        [Test("Bulk-1000-Transform-Direct-XY", null, 5000000, true)]
        public void Bulk_1000_Transform_Direct_XY(TestAttribute ta, object setup, ref double x, ref double y)
        {
            Bulk_Transform_XY(bulk_XY_1000, false, ta, setup, ref x, ref y);
        }

        [Test("Bulk-100000-Transform-Direct-XY", null, 5000000, false)]
        public void Bulk_100000_Transform_Direct_XY(TestAttribute ta, object setup, ref double x, ref double y)
        {
            Bulk_Transform_XY(bulk_XY_100000, false, ta, setup, ref x, ref y);
        }

        [Test("Bulk-1000-Transform-Direct-Point", null, 5000000, true)]
        public void Bulk_1000_Transform_Direct_Point(TestAttribute ta, object setup, ref double x, ref double y)
        {
            Bulk_Transform_Point(bulk_1000, false, ta, setup, ref x, ref y);
        }

        [Test("Bulk-100000-Transform-Direct-Point", null, 5000000, false)]
        public void Bulk_100000_Transform_Direct_Point(TestAttribute ta, object setup, ref double x, ref double y)
        {
            Bulk_Transform_Point(bulk_100000, false, ta, setup, ref x, ref y);
        }

        [Test("Bulk-1000-Transform-Direct-Enum", null, 5000000, true)]
        public void Bulk_1000_Transform_Direct_Enum(TestAttribute ta, object setup, ref double x, ref double y)
        {
            Bulk_Transform_Enum(bulk_1000, false, false, ta, setup, ref x, ref y);
        }

        [Test("Bulk-100000-Transform-Direct-Enum", null, 5000000, true)]
        public void Bulk_100000_Transform_Direct_Enum(TestAttribute ta, object setup, ref double x, ref double y)
        {
            Bulk_Transform_Enum(bulk_100000, false, false, ta, setup, ref x, ref y);
        }

        [Test("Bulk-1000-Transform-Proj-XY", null, 5000000, true)]
        public void Bulk_1000_Transform_Proj_XY(TestAttribute ta, object setup, ref double x, ref double y)
        {
            Bulk_Transform_XY(bulk_XY_1000, true, ta, setup, ref x, ref y);
        }

        [Test("Bulk-100000-Transform-Proj-XY", null, 5000000, false)]
        public void Bulk_100000_Transform_Proj_XY(TestAttribute ta, object setup, ref double x, ref double y)
        {
            Bulk_Transform_XY(bulk_XY_100000, true, ta, setup, ref x, ref y);
        }

        [Test("Bulk-1000-Transform-Proj-Point", null, 5000000, true)]
        public void Bulk_1000_Transform_Proj_Point(TestAttribute ta, object setup, ref double x, ref double y)
        {
            Bulk_Transform_Point(bulk_1000, true, ta, setup, ref x, ref y);
        }

        [Test("Bulk-100000-Transform-Proj-Point", null, 5000000, false)]
        public void Bulk_100000_Transform_Proj_Point(TestAttribute ta, object setup, ref double x, ref double y)
        {
            Bulk_Transform_Point(bulk_100000, true, ta, setup, ref x, ref y);
        }

        [Test("Bulk-1000-Transform-Proj-Enum", null, 5000000, true)]
        public void Bulk_1000_Transform_Proj_Enum(TestAttribute ta, object setup, ref double x, ref double y)
        {
            Bulk_Transform_Enum(bulk_1000, false, true, ta, setup, ref x, ref y);
        }

        [Test("Bulk-100000-Transform-Proj-Enum", null, 5000000, true)]
        public void Bulk_100000_Transform_Proj_Enum(TestAttribute ta, object setup, ref double x, ref double y)
        {
            Bulk_Transform_Enum(bulk_100000, true, true, ta, setup, ref x, ref y);
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class TestAttribute : Attribute
    {
        public String TestName
        {
            get;
            set; 
        }

        public int PerPoint
        {
            get;
            set;
        }

        public bool Enabled
        {
            get;
            set;
        }

        public string Setup
        {
            get;
            set;
        }

        public TestAttribute(string testName, String setup, int perPoint, bool enabled)
        {
            TestName = testName;
            PerPoint = perPoint;
            Setup = setup;
            Enabled = enabled;
        }
    }

    public class HiResTimer
    {
        private readonly bool isPerfCounterSupported;
        private readonly Int64 frequency = 0;
        private readonly Int64 startValue;

        [DllImport("kernel32")]
        private static extern int QueryPerformanceCounter(ref Int64 count);

        [DllImport("kernel32")]
        private static extern int QueryPerformanceFrequency(ref Int64 frequency);

        public HiResTimer()
        {
            int returnVal = QueryPerformanceFrequency(ref frequency);

            if (!(isPerfCounterSupported = (returnVal != 0 && frequency != 1000)))
                frequency = 1000;

            startValue = Value;
        }

        private Int64 Frequency => frequency;

        private Int64 Value
        {
            get
            {
                Int64 tickCount = 0;

                if (isPerfCounterSupported)
                {
                    QueryPerformanceCounter(ref tickCount);
                    return tickCount;
                }
                else
                {
                    return (Int64)Environment.TickCount;
                }
            }
        }

        public double Elapsed
        {
            get
            {
                double timeElapsedInTicks = Value - startValue;
                return timeElapsedInTicks * 1000 / Frequency;
            }
        }
    }
}


