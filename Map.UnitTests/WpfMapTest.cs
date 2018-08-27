// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using NUnit.Framework;


namespace Ptv.XServer.Controls.Map.UnitTests
{
    [TestFixture]
    public class WpfMapTest
    {
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

        [Test,
        IntegrationCategory,
        Description("Tests the XYZ of the ABC.")]
        public void WpfMapTestXYZ()
        {
            Assert.IsTrue(true, "Assert true is true");
        }

        [Test,
        IntegrationCategory,
        Description("Tests the FAIL of the ABC.")]
        public void WpfMapTestFailXYZ()
        {
            // Assert.IsTrue(false, "Assert true is false");
        }
    }
}
