// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Collections.Generic;
using NUnit.Framework;
using Moq;
using System.IO;
using xserver;
using Ptv.XServer.Controls.Map.Tools;
using System.Reflection;

namespace Ptv.XServer.Controls.Map.UnitTests
{
    /// <summary>
    /// Tests the XMapTools class.
    /// </summary>
    [TestFixture]
    class XMapToolsTest
    {
        /// <summary>
        /// Clears cached layer checks.
        /// </summary>
        [SetUp]
        public void Initialize()
        {
            XMapTools.CheckedXMapLayers.Clear();
        }

        /// <summary>
        /// Clears cached layer checks.
        /// </summary>
        [TearDown]
        public void Cleanup()
        {
            XMapTools.CheckedXMapLayers.Clear();
        }

        /// <summary>
        /// Helper method which creates a byte[] from a Bitmap.
        /// </summary>
        /// <param name="imageSource">The bitmap to create the byte[] from.</param>
        /// <returns>The created byte[] or null if the imageSource stream is null or empty.</returns>
        public Byte[] BufferFromImage(System.Windows.Media.Imaging.BitmapImage imageSource)
        {
            var stream = imageSource.StreamSource;
            if (stream == null || stream.Length <= 0) return null;

            using (var binaryReader = new BinaryReader(stream))
            {
                return binaryReader.ReadBytes((Int32)stream.Length);
            }
        }

        /// <summary>
        /// Tests that unexpected exceptions from the XMap are passed through.
        /// </summary>
        [Test,
        UnitCategory,
        Description("Tests that appropriate exceptions are raised under certain circumstances.")]
        public void TestExceptionsAreXMapLayersAvailable()
        {
            var customLayers = new List<Layer>();
            var xMapMock = new Mock<IXMapWSBinding>();
            const string exceptionMessage = "fragged";
            const string expectedCode = "230";
            const string unexpectedCode = "0815";
            
            customLayers.Add(new RoadEditorLayer { name = "fragattributes", visible = true });
            customLayers.Add(new StaticPoiLayer { name = "street", visible = true, category = -1, detailLevel = 0 });
            // this set of layers will be used to mock up another invalid request which raises a SoapException with an unhandled error code
            Layer[] notWorkingLayers = customLayers.ToArray();

            customLayers.Add(new StaticPoiLayer { name = "town", visible = true, category = -1, detailLevel = 0 });
            // this set of layers will be used to mock up another invalid request which raises an unhandled exception
            Layer[] otherNotWorkingLayers = customLayers.ToArray();
            
            // mock up another invalid request with an unhandled error code
            xMapMock.Setup(xmap => xmap.renderMapBoundingBox(It.IsAny<BoundingBox>(), 
                It.IsAny<MapParams>(), 
                It.IsAny<ImageInfo>(), 
                notWorkingLayers, 
                It.IsAny<bool>(), 
                It.IsAny<CallerContext>())).Throws(new System.Web.Services.Protocols.SoapException(exceptionMessage, new System.Xml.XmlQualifiedName(unexpectedCode)));
            // mock up another invalid request with an unhandled exception
            xMapMock.Setup(xmap => xmap.renderMapBoundingBox(It.IsAny<BoundingBox>(), 
                It.IsAny<MapParams>(), 
                It.IsAny<ImageInfo>(), 
                otherNotWorkingLayers, 
                It.IsAny<bool>(), 
                It.IsAny<CallerContext>())).Throws(new Exception(exceptionMessage));

            FieldInfo fi = typeof(XMapTools).GetField("Service", BindingFlags.NonPublic | BindingFlags.Static);
            fi?.SetValue(null, xMapMock.Object);
            Assert.Throws(typeof(System.Web.Services.Protocols.SoapException), () => XMapTools.AreXMapLayersAvailable(null, null, notWorkingLayers, null, expectedCode), exceptionMessage);
            fi?.SetValue(null, (xserver.IXMapWSBinding)xMapMock.Object);
            Assert.Throws(typeof(Exception), () => XMapTools.AreXMapLayersAvailable(null, null, otherNotWorkingLayers, null, expectedCode), exceptionMessage);
        }

        /// <summary>
        /// Tests that the check for available layers returns true when the XMap says that the layers
        /// are supported. The test also verifies that subsequent calls with same parameters result in a
        /// cache lookup rather than real service calls and vice versa.
        /// </summary>
        [Test,
        UnitCategory,
        Description("Tests if the XMapTools.AreXMapLayersAvailable() works as expected for positive results.")]
        public void TestPositiveAreXMapLayersAvailable()
        {
            const string profile = "truckattributes";
            var customLayers = new List<Layer>();
            var xMapMock = new Mock<IXMapWSBinding>();
            var xImage = new Image();
            var xMap = new xserver.Map();
            var bitMapImage = ResourceHelper.LoadBitmapFromResource("Ptv.XServer.Controls.Map;component/Resources/LayerDefault.png");
            var calls = 0;
            const string expectedCode = "230";

            customLayers.Add(new RoadEditorLayer { name = profile, visible = true });
            customLayers.Add(new StaticPoiLayer { name = "street", visible = true, category = -1, detailLevel = 0 });
            // this set of layers will be used to mock up a valid request
            Layer[] workingLayers = customLayers.ToArray();

            // this set of layers will be used to mock up another valid request and to validate that a separate service call was made
            customLayers.Add(new StaticPoiLayer { name = "town", visible = true, category = -1, detailLevel = 0 });
            Layer[] otherWorkingLayers = customLayers.ToArray();

            xImage.rawImage = BufferFromImage(bitMapImage);
            xMap.image = xImage;

            // mock up valid request
            xMapMock.Setup(xmap => xmap.renderMapBoundingBox(It.IsAny<BoundingBox>(),
                It.IsAny<MapParams>(),
                It.IsAny<ImageInfo>(),
                It.IsAny<Layer[]>(),
                It.IsAny<bool>(),
                It.IsAny<CallerContext>())).Returns(xMap).Callback(() => calls++);

            FieldInfo fi = typeof(XMapTools).GetField("Service", BindingFlags.NonPublic | BindingFlags.Static);
            fi?.SetValue(null, xMapMock.Object);
            Assert.True(XMapTools.AreXMapLayersAvailable(null, null, workingLayers, null, expectedCode));
            fi?.SetValue(null, xMapMock.Object);
            // assert that the next call with the same layers does not result in a service call
            Assert.True(XMapTools.AreXMapLayersAvailable(null, null, workingLayers, null, expectedCode));
            fi?.SetValue(null, xMapMock.Object);
            Assert.AreEqual(1, calls);
            // assert that the next call with other layers results in a service call
            fi?.SetValue(null, xMapMock.Object);
            Assert.True(XMapTools.AreXMapLayersAvailable(null, null, otherWorkingLayers, null, expectedCode));
            fi?.SetValue(null, xMapMock.Object);
            Assert.AreEqual(2, calls);

        }

        /// <summary>
        /// Tests that the check for available layers returns faöse when the XMap says that the layers
        /// are not supported. The test also verifies that subsequent calls with same parameters result in a
        /// cache lookup rather than real service calls and vice versa.
        /// </summary>
        [Test,
        UnitCategory,
        Description("Tests if the XMapTools.AreXMapLayersAvailable() works as expected with for negative results.")]
        public void TestNegativeAreXMapLayersAvailable()
        {
            var customLayers = new List<Layer>();
            var xMapMock = new Mock<IXMapWSBinding>();
            const string exceptionMessage = "fragged";
            const string expectedCode = "230";
            var calls = 0;

            customLayers.Add(new RoadEditorLayer { name = "fragattributes", visible = true });
            customLayers.Add(new StaticPoiLayer { name = "street", visible = true, category = -1, detailLevel = 0 });
            // this set of layers will be used to mock up an invalid request
            Layer[] notWorkingLayers = customLayers.ToArray();

            // this set of layers will be used to mock up another valid request and to validate that a separate service call was made
            customLayers.Add(new StaticPoiLayer { name = "town", visible = true, category = -1, detailLevel = 0 });
            Layer[] otherNotWorkingLayers = customLayers.ToArray();

            // mock up an invalid request
            xMapMock.Setup(xmap => xmap.renderMapBoundingBox(It.IsAny<BoundingBox>(),
                It.IsAny<MapParams>(),
                It.IsAny<ImageInfo>(),
                It.IsAny<Layer[]>(),
                It.IsAny<bool>(),
                It.IsAny<CallerContext>())).Callback(() => calls++).Throws(new System.Web.Services.Protocols.SoapException(exceptionMessage, new System.Xml.XmlQualifiedName(expectedCode)));

            FieldInfo fi = typeof(XMapTools).GetField("Service", BindingFlags.NonPublic | BindingFlags.Static);
            fi?.SetValue(null, xMapMock.Object);
            Assert.False(XMapTools.AreXMapLayersAvailable(null, null, notWorkingLayers, null, expectedCode));
            // assert that the next call with the same layers does not result in a service call
            fi?.SetValue(null, xMapMock.Object);
            Assert.False(XMapTools.AreXMapLayersAvailable(null, null, notWorkingLayers, null, expectedCode));
            fi?.SetValue(null, xMapMock.Object);
            Assert.AreEqual(1, calls);
            // assert that the next call with other layers results in a service call
            fi?.SetValue(null, xMapMock.Object);
            Assert.False(XMapTools.AreXMapLayersAvailable(null, null, otherNotWorkingLayers, null, expectedCode));
            fi?.SetValue(null, xMapMock.Object);
            Assert.AreEqual(2, calls);
        }
    }
}
