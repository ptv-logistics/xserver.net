// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

namespace Ptv.XServer.Controls.Map.UnitTests
{
    using System;
    using System.Windows;
    using System.Collections.Generic;
    using NUnit.Framework;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    [TestFixture]
    public class MapRectangleTest
    {
        private const double epsilon = 0.0005d;

        private readonly MapRectangle empty = new MapRectangle();
        private readonly Point outsidePoint = new Point(Double.NaN, Double.NaN);
        private readonly Point origin = new Point(0, 0);


        private static void AreEqual(Point expected, Point actual, string memberName)
        {
            Assert.That(actual.X, Is.EqualTo(expected.X).Within(epsilon), "Check X property of " + memberName);
            Assert.That(actual.Y, Is.EqualTo(expected.Y).Within(epsilon), "Check Y property of " + memberName);
        }

        [TestFixtureSetUp]
        public void Initialize()
        {
            // Initialize something for the tests, e.g. network and/or DB connections, disk space, etc.
        }

        [TestFixtureTearDown]
        public void Cleanup()
        {
            // Clean up after the tests, e.g. network and/or DB connections, disk space, etc.
        }

        [Test, IntegrationCategory, Description("Tests the default constructor.")]
        public void TestDefaultConstructor()
        {
            var rect = new MapRectangle();

            Assert.IsTrue(rect.IsEmpty, "Check property IsEmpty");

            Assert.That(rect.West, Is.EqualTo(Double.PositiveInfinity).Within(epsilon), "Check property West");
            Assert.That(rect.South, Is.EqualTo(Double.PositiveInfinity).Within(epsilon), "Check property South");
            Assert.That(rect.East, Is.EqualTo(Double.NegativeInfinity).Within(epsilon), "Check property East");
            Assert.That(rect.North, Is.EqualTo(Double.NegativeInfinity).Within(epsilon), "Check property North");
            Assert.That(rect.Width, Is.EqualTo(Double.NegativeInfinity).Within(epsilon), "Check property Width");
            Assert.That(rect.Height, Is.EqualTo(Double.NegativeInfinity).Within(epsilon), "Check property Height");

            AreEqual(new Point(Double.PositiveInfinity, Double.PositiveInfinity), rect.SouthWest, "SouthWest");
            AreEqual(new Point(Double.NegativeInfinity, Double.PositiveInfinity), rect.SouthEast, "SouthEast");
            AreEqual(new Point(Double.PositiveInfinity, Double.NegativeInfinity), rect.NorthWest, "NorthWest");
            AreEqual(new Point(Double.NegativeInfinity, Double.NegativeInfinity), rect.NorthEast, "NorthEast");
            AreEqual(new Point(Double.NaN, Double.NaN), rect.Center, "Center");
        }

        [Test, IntegrationCategory, Description("Tests the constructor with point.")]
        public void TestConstructorPoint()
        {
            var p = new Point(47.11, 8.15);
            var rect = new MapRectangle(p);

            Assert.IsTrue(!rect.IsEmpty, "Check property IsEmpty");

            Assert.That(rect.West, Is.EqualTo(47.11).Within(epsilon), "Check property West");
            Assert.That(rect.South, Is.EqualTo(8.15).Within(epsilon), "Check property South");
            Assert.That(rect.East, Is.EqualTo(47.11).Within(epsilon), "Check property East");
            Assert.That(rect.North, Is.EqualTo(8.15).Within(epsilon), "Check property North");
            Assert.That(rect.Width, Is.EqualTo(0.0).Within(epsilon), "Check property Width");
            Assert.That(rect.Height, Is.EqualTo(0.0).Within(epsilon), "Check property Height");

            AreEqual(new Point(47.11, 8.15), rect.SouthWest, "SouthWest");
            AreEqual(new Point(47.11, 8.15), rect.SouthEast, "SouthEast");
            AreEqual(new Point(47.11, 8.15), rect.NorthWest, "NorthWest");
            AreEqual(new Point(47.11, 8.15), rect.NorthEast, "NorthEast");
            AreEqual(new Point(47.11, 8.15), rect.Center, "Center");
        }

        [Test, IntegrationCategory, Description("Tests the constructor with two points.")]
        public void TestConstructor2Points()
        {
            var southWest = new Point(47.11, 8.15);
            var northEast = new Point(48.12, 9.16);
            // White box testing: Points must be exchanged internally
            var rect = new MapRectangle(northEast, southWest);

            Assert.IsTrue(!rect.IsEmpty, "Check property IsEmpty");

            Assert.That(rect.West, Is.EqualTo(47.11).Within(epsilon), "Check property West");
            Assert.That(rect.South, Is.EqualTo(8.15).Within(epsilon), "Check property South");
            Assert.That(rect.East, Is.EqualTo(48.12).Within(epsilon), "Check property East");
            Assert.That(rect.North, Is.EqualTo(9.16).Within(epsilon), "Check property North");
            Assert.That(rect.Width, Is.EqualTo(1.01).Within(epsilon), "Check property Width");
            Assert.That(rect.Height, Is.EqualTo(1.01).Within(epsilon), "Check property Height");

            AreEqual(new Point(47.11, 8.15), rect.SouthWest, "SouthWest");
            AreEqual(new Point(48.12, 8.15), rect.SouthEast, "SouthEast");
            AreEqual(new Point(47.11, 9.16), rect.NorthWest, "NorthWest");
            AreEqual(new Point(48.12, 9.16), rect.NorthEast, "NorthEast");
            AreEqual(new Point((southWest.X + northEast.X) / 2, (southWest.Y + northEast.Y) / 2), rect.Center, "Center");
        }

        [Test, IntegrationCategory, Description("Tests the constructor with IEnumerable<Point>.")]
        public void TestConstructorIEnumerablePoint()
        {
            // Build a point list with X- and Y-Ranges [-1000.0, +1000.0]
            var pointList = new List<Point>
            {
                new Point(47.11, 8.15),
                new Point(-100.101, 95.17),
                new Point(-1000.0, 507.123),
                new Point(-34.0, 1000.0),
                new Point(1000.0, -507.123),
                new Point(34.0, -1000.0)
            };

            // White box testing: Points must be exchanged internally
            var rect = new MapRectangle(pointList);

            Assert.IsTrue(!rect.IsEmpty, "Check property IsEmpty");

            Assert.That(rect.West, Is.EqualTo(-1000.0).Within(epsilon), "Check property West");
            Assert.That(rect.South, Is.EqualTo(-1000.0).Within(epsilon), "Check property South");
            Assert.That(rect.East, Is.EqualTo(1000.0).Within(epsilon), "Check property East");
            Assert.That(rect.North, Is.EqualTo(1000.0).Within(epsilon), "Check property North");
            Assert.That(rect.Width, Is.EqualTo(2000.0).Within(epsilon), "Check property Width");
            Assert.That(rect.Height, Is.EqualTo(2000.0).Within(epsilon), "Check property Height");

            AreEqual(new Point(-1000.0, -1000.0), rect.SouthWest, "SouthWest");
            AreEqual(new Point(1000.0, -1000.0), rect.SouthEast, "SouthEast");
            AreEqual(new Point(-1000.0, 1000.0), rect.NorthWest, "NorthWest");
            AreEqual(new Point(1000.0, 1000.0), rect.NorthEast, "NorthEast");
            AreEqual(new Point(0.0, 0.0), rect.Center, "Center");
        }

        [Test, IntegrationCategory, Description("Tests the constructor with center and size.")]
        public void TestConstructorCenterSize()
        {
            var center = new Point(-1000000.0, -1000000.0);
            var size = new Size(2000000.0, 2000000.0);
            var rect = new MapRectangle(center, size);

            Assert.IsTrue(!rect.IsEmpty, "Check property IsEmpty");

            Assert.That(rect.West, Is.EqualTo(-2000000.0).Within(epsilon), "Check property West");
            Assert.That(rect.South, Is.EqualTo(-2000000.0).Within(epsilon), "Check property South");
            Assert.That(rect.East, Is.EqualTo(0.0).Within(epsilon), "Check property East");
            Assert.That(rect.North, Is.EqualTo(0.0).Within(epsilon), "Check property North");
            Assert.That(rect.Width, Is.EqualTo(2000000.0).Within(epsilon), "Check property Width");
            Assert.That(rect.Height, Is.EqualTo(2000000.0).Within(epsilon), "Check property Height");

            AreEqual(new Point(-2000000.0, -2000000.0), rect.SouthWest, "SouthWest");
            AreEqual(new Point(0.0, -2000000.0), rect.SouthEast, "SouthEast");
            AreEqual(new Point(-2000000.0, 0.0), rect.NorthWest, "NorthWest");
            AreEqual(new Point(0.0, 0.0), rect.NorthEast, "NorthEast");
            AreEqual(new Point(-1000000.0, -1000000.0), rect.Center, "Center");
        }

        [Test, IntegrationCategory, Description("Tests the constructor with compass parameters.")]
        public void TestConstructorCompassParameters()
        {
            var rect = new MapRectangle(-1000.0, 1000.0, -1000.0, 1000.0);

            Assert.IsTrue(!rect.IsEmpty, "Check property IsEmpty");

            Assert.That(rect.West, Is.EqualTo(-1000.0).Within(epsilon), "Check property West");
            Assert.That(rect.South, Is.EqualTo(-1000.0).Within(epsilon), "Check property South");
            Assert.That(rect.East, Is.EqualTo(1000.0).Within(epsilon), "Check property East");
            Assert.That(rect.North, Is.EqualTo(1000.0).Within(epsilon), "Check property North");
            Assert.That(rect.Width, Is.EqualTo(2000.0).Within(epsilon), "Check property Width");
            Assert.That(rect.Height, Is.EqualTo(2000.0).Within(epsilon), "Check property Height");

            AreEqual(new Point(-1000.0, -1000.0), rect.SouthWest, "SouthWest");
            AreEqual(new Point(1000.0, -1000.0), rect.SouthEast, "SouthEast");
            AreEqual(new Point(-1000.0, 1000.0), rect.NorthWest, "NorthWest");
            AreEqual(new Point(1000.0, 1000.0), rect.NorthEast, "NorthEast");
            AreEqual(new Point(0.0, 0.0), rect.Center, "Center");
        }

        [Test, IntegrationCategory, Description("Tests the Contains-methods.")]
        public void TestProperties()
        {
            // ReSharper disable once UseObjectOrCollectionInitializer
            var rect = new MapRectangle();

            rect.West = -1000.0;
            Assert.That(rect.West, Is.EqualTo(-1000.0).Within(epsilon), "Check property West");
            Assert.That(rect.Width, Is.EqualTo(Double.NegativeInfinity).Within(epsilon), "Check property Width after change of West");
            AreEqual(new Point(Double.NaN, Double.NaN), rect.Center, "Center after change of West");
            
            rect.East = 1000.0;
            Assert.That(rect.East, Is.EqualTo(1000.0).Within(epsilon), "Check property East");
            Assert.That(rect.Width, Is.EqualTo(2000.0).Within(epsilon), "Check property Width after change of East");
            AreEqual(new Point(0.0, Double.NaN), rect.Center, "Center after change of East");

            // Now, the upper bound is set first.
            rect.North = 1000.0;
            Assert.That(rect.North, Is.EqualTo(1000.0).Within(epsilon), "Check property North");
            Assert.That(rect.Height, Is.EqualTo(Double.NegativeInfinity).Within(epsilon), "Check property Height after change of North");
            AreEqual(new Point(0.0, Double.NaN), rect.Center, "Center after change of North");

            rect.South = -1000.0;
            Assert.That(rect.South, Is.EqualTo(-1000.0).Within(epsilon), "Check property South");
            Assert.That(rect.Height, Is.EqualTo(2000.0), "Check property Height after change of South");
            AreEqual(new Point(0.0, 0.0), rect.Center, "Center after change of South");
            
            rect.Width = 1000.0;
            Assert.That(rect.Width, Is.EqualTo(1000.0).Within(epsilon), "Check property Width");
            Assert.That(rect.West, Is.EqualTo(-500.0).Within(epsilon), "Check property West after change of Width");
            Assert.That(rect.East, Is.EqualTo(500.0).Within(epsilon), "Check property East after change of Width");
            AreEqual(new Point(0.0, 0.0), rect.Center, "Center after change of Width");

            rect.Height = 1000.0;
            Assert.That(rect.Height, Is.EqualTo(1000.0).Within(epsilon), "Check property Height");
            Assert.That(rect.South, Is.EqualTo(-500.0).Within(epsilon), "Check property South after change of Height");
            Assert.That(rect.North, Is.EqualTo(500.0).Within(epsilon), "Check property North after change of Height");
            AreEqual(new Point(0.0, 0.0), rect.Center, "Center after change of Height");

            rect.Center = new Point(500.0, 500.0);
            Assert.That(rect.West, Is.EqualTo(0.0).Within(epsilon), "Check property West after change of Center");
            Assert.That(rect.East, Is.EqualTo(1000.0).Within(epsilon), "Check property East after change of Center");
            Assert.That(rect.South, Is.EqualTo(0.0).Within(epsilon), "Check property South after change of Center");
            Assert.That(rect.North, Is.EqualTo(1000.0).Within(epsilon), "Check property North after change of Center");
            Assert.That(rect.Width, Is.EqualTo(1000.0).Within(epsilon), "Check property Width after change of Center");
            Assert.That(rect.Height, Is.EqualTo(1000.0).Within(epsilon), "Check property Height after change of Center");
        }

        [Test, IntegrationCategory, Description("Tests the Contains methods.")]
        public void TestContainsMethods()
        {
            var outerRectangle = new MapRectangle(0.0, 1000.0, 0.0, 1000.0);
            var innerRectangle = new MapRectangle(0.0, 100.0, 0.0, 100.0);

            Assert.IsTrue(!empty.Contains(outsidePoint), "Empty point in empty rectangle");
            Assert.IsTrue(!empty.Contains(origin), "Origin in empty rectangle");
            Assert.IsTrue(!empty.Contains(empty), "Empty rectangle in empty rectangle");
            Assert.IsTrue(!empty.Contains(innerRectangle), "Filled rectangle in empty rectangle");

            Assert.IsTrue(!outerRectangle.Contains(outsidePoint), "Empty point in outer rectangle");
            Assert.IsTrue(outerRectangle.Contains(origin), "Origin in outer rectangle");
            Assert.IsTrue(outerRectangle.Contains(empty), "Empty rectangle in outer rectangle");
            Assert.IsTrue(outerRectangle.Contains(outerRectangle), "Filled rectangle in empty rectangle");
            Assert.IsTrue(outerRectangle.Contains(innerRectangle), "InnerRight rectangle in outer rectangle");
        }

        [Test, IntegrationCategory, Description("Tests the Intersect methods. The true Intersect method is tested by using operator*")]
        public void TestIntersectMethods()
        {
            var upperRight = new MapRectangle(0.0, 1000.0, 0.0, 1000.0);
            var expectedUpperRight = new MapRectangle(upperRight);
            var lowerLeft = new MapRectangle(-1000.0, 0.0, -1000.0, 0.0);

            Assert.IsTrue(!empty.IntersectsWith(empty), "Empty rectangle intersects empty rectangle");
            Assert.IsTrue(!empty.IntersectsWith(upperRight), "Filled rectangle intersects empty rectangle");
            Assert.IsTrue((empty & empty) == new MapRectangle(), "Intersection of an Empty rectangle with empty rectangle");
            Assert.IsTrue((empty & upperRight) == new MapRectangle(), "Intersection of a Filled rectangle with empty rectangle");

            Assert.IsTrue(!upperRight.IntersectsWith(empty), "Empty rectangle intersects filled rectangle");
            Assert.IsTrue(upperRight.IntersectsWith(upperRight), "Filled rectangle intersects filled rectangle");
            Assert.IsTrue((upperRight & empty) == new MapRectangle(), "Intersection of an Empty rectangle with filled rectangle");
            Assert.IsTrue((upperRight & upperRight) == expectedUpperRight, "Intersection of a upperright rectangle with filled rectangle");
            Assert.IsTrue((upperRight & lowerLeft) == new MapRectangle(new Point(0, 0)), "Intersection of a lowerleft rectangle with filled rectangle");
        }

        [Test, IntegrationCategory, Description("Tests the Union methods. The true Union method is tested by using operator+")]
        public void TestUnionMethods()
        {
            var upperRight = new MapRectangle(0.0, 1000.0, 0.0, 1000.0);
            var expectedUpperRight = new MapRectangle(upperRight);
            var lowerLeft = new MapRectangle(-1000.0, 0.0, -1000.0, 0.0);

            Assert.IsTrue((empty | outsidePoint) == new MapRectangle(), "Union of an outside point with empty rectangle");
            Assert.IsTrue((empty | origin) == new MapRectangle(new Point(0,0)), "Union of origin point with empty rectangle");
            Assert.IsTrue((empty | empty) == new MapRectangle(), "Union of an Empty rectangle with empty rectangle");
            Assert.IsTrue((empty | upperRight) == expectedUpperRight, "Union of a Filled rectangle with empty rectangle");

            Assert.IsTrue((upperRight | outsidePoint) == expectedUpperRight, "Union of an outside point with filled rectangle");
            Assert.IsTrue((upperRight | origin) == expectedUpperRight, "Union of origin point with filled rectangle");
            Assert.IsTrue((upperRight | new Point(-1000.0, -1000.0)) == new MapRectangle(-1000.0, 1000.0, -1000.0, 1000.0), "Union of outside point with filled rectangle");
            Assert.IsTrue((upperRight | empty) == expectedUpperRight, "Union of an Empty rectangle with filled rectangle");
            Assert.IsTrue((upperRight | upperRight) == expectedUpperRight, "Union of a upperright rectangle with filled rectangle");
            Assert.IsTrue((upperRight | lowerLeft) == new MapRectangle(-1000.0, 1000.0, -1000.0, 1000.0), "Union of a lowerleft rectangle with filled rectangle");
        }

        [Test, IntegrationCategory, Description("Tests the Translate methods.")]
        public void TestTranslateMethods()
        {
            Assert.IsTrue(empty.TranslateHorizontally(1.0) == new MapRectangle(), "Translate horizontally the empty rectangle");
            Assert.IsTrue(empty.TranslateVertically(1.0) == new MapRectangle(), "Translate vertically the empty rectangle");
            Assert.IsTrue(empty.Translate(1.0, 1.0) == new MapRectangle(), "Translate vertically the empty rectangle");
            Assert.IsTrue((empty + new Point(-1.0, 1.0)) == new MapRectangle(), "Translate by Point the empty rectangle");

            var rect = new MapRectangle(0.0, 1000.0, 0.0, 1000.0);

            Assert.IsTrue(rect.TranslateHorizontally(1.0) == new MapRectangle(1.0, 1001.0, 0.0, 1000.0), "Translate horizontally a filled rectangle");
            Assert.IsTrue(rect.TranslateVertically(1.0) == new MapRectangle(1.0, 1001.0, 1.0, 1001.0), "Translate vertically a filled rectangle");
            Assert.IsTrue(rect.Translate(1.0, 1.0) == new MapRectangle(2.0, 1002.0, 2.0, 1002.0), "Translate vertically a filled rectangle");
            Assert.IsTrue((rect + new Point(-1.0, 1.0)) == new MapRectangle(1.0, 1001.0, 3.0, 1003.0), "Translate by Point a filled rectangle");
        }

        [Test, IntegrationCategory, Description("Tests the Inflate methods.")]
        public void TestInflateMethods()
        {
            Assert.IsTrue(empty.InflateHorizontally(2.0) == new MapRectangle(), "Inflate horizontally the empty rectangle");
            Assert.IsTrue(empty.InflateVertically(0.5) == new MapRectangle(), "Inflate vertically the empty rectangle");
            Assert.IsTrue(empty.Inflate(0.5, 2.0) == new MapRectangle(), "Inflate vertically the empty rectangle");
            Assert.IsTrue(empty.Inflate(2.0) == new MapRectangle(), "Inflate vertically the empty rectangle");
            Assert.IsTrue((empty * new Point(0.1, 0.1)) == new MapRectangle(), "Inflate by Point the empty rectangle");

            var rect = new MapRectangle(0.0, 1000.0, 0.0, 1000.0);

            Assert.IsTrue(rect.InflateHorizontally(2.0) == new MapRectangle(-500.0, 1500.0, 0.0, 1000.0), "Inflate horizontally a filled rectangle");
            Assert.IsTrue(rect.InflateVertically(0.5) == new MapRectangle(-500.0, 1500.0, 250.0, 750.0), "Inflate vertically a filled rectangle");
            Assert.IsTrue(rect.Inflate(0.5, 2.0) == new MapRectangle(0.0, 1000.0, 0.0, 1000.0), "Inflate vertically a filled rectangle");
            Assert.IsTrue(rect.Inflate(2.0) == new MapRectangle(-500.0, 1500.0, -500.0, 1500.0), "Inflate vertically a filled rectangle");
            Assert.IsTrue((rect * new Point(0.1, 0.1)) == new MapRectangle(400.0, 600.0, 400.0, 600.0), "Inflate by Point a filled rectangle");
        }

    }
}
