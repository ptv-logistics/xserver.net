using NUnit.Framework;

using Ptv.XServer.Controls.Map.Tools;

namespace Ptv.XServer.Controls.Map.UnitTests
{
    [TestFixture]
    public class XServerUrlTest
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

        [Test, IntegrationCategory, Description("Tests the completion of URLs for XServer web services.")]
        public void TestCompleting()
        {
            // No scheme . is Azure
            Assert.That(XServerUrl.Complete("api", "XMAP"), Is.EqualTo("https://api.cloud.ptvgroup.com/xmap/ws/XMap"), "Check minimal input for world map in Azure.");
            Assert.That(XServerUrl.Complete("api-eu", "XMAP"), Is.EqualTo("https://api-eu.cloud.ptvgroup.com/xmap/ws/XMap"), "Check minimal input for world map in Azure.");
            Assert.That(XServerUrl.Complete("api-test", "XMAP"), Is.EqualTo("https://api-test.cloud.ptvgroup.com/xmap/ws/XMap"), "Check minimal input for world map in Azure.");
            Assert.That(XServerUrl.Complete("api-eu-test", "XMAP"), Is.EqualTo("https://api-eu-test.cloud.ptvgroup.com/xmap/ws/XMap"), "Check minimal input for world map in Azure.");

            Assert.That(XServerUrl.Complete("china", "XMAP"), Is.EqualTo("https://china.cloud.ptvgroup.com/xmap/ws/XMap"), "Check minimal input for world map in Azure.");
            Assert.That(XServerUrl.Complete("china-cn", "XMAP"), Is.EqualTo("https://china-cn.cloud.ptvgroup.com/xmap/ws/XMap"), "Check minimal input for world map in Azure.");
            Assert.That(XServerUrl.Complete("china-test", "XMAP"), Is.EqualTo("https://china-test.cloud.ptvgroup.com/xmap/ws/XMap"), "Check minimal input for world map in Azure.");
            Assert.That(XServerUrl.Complete("china-cn-test", "XMAP"), Is.EqualTo("https://china-cn-test.cloud.ptvgroup.com/xmap/ws/XMap"), "Check minimal input for world map in Azure.");

            Assert.That(XServerUrl.Complete("eu-n", "XMAP"), Is.EqualTo("https://xmap-eu-n.cloud.ptvgroup.com/xmap/ws/XMap"), "Check minimal input for European map in Azure.");
            Assert.That(XServerUrl.Complete("eu-n", "XROUTE"), Is.EqualTo("https://xroute-eu-n.cloud.ptvgroup.com/xroute/ws/XRoute"), "Check minimal input for European map in Azure.");
            Assert.That(XServerUrl.Complete("eu-n", "XLOCATE"), Is.EqualTo("https://xlocate-eu-n.cloud.ptvgroup.com/xlocate/ws/XLocate"), "Check minimal input for European map in Azure.");

            Assert.That(XServerUrl.Complete("eu-n-test", "XMAP"), Is.EqualTo("https://xmap-eu-n-test.cloud.ptvgroup.com/xmap/ws/XMap"), "Check minimal input for Test European map in Azure.");
            Assert.That(XServerUrl.Complete("eu-n-integration", "XMAP"), Is.EqualTo("https://xmap-eu-n-integration.cloud.ptvgroup.com/xmap/ws/XMap"), "Check minimal input for Integration European map in Azure.");
            Assert.That(XServerUrl.Complete("eu-n-foobar", "XMAP"), Is.EqualTo("http://eu-n-foobar:50010/xmap/ws/XMap"), "Check minimal input for wrong European map in Azure.");

            Assert.That(XServerUrl.Complete("eu-h", "XMAP"), Is.EqualTo("https://xmap-eu-h.cloud.ptvgroup.com/xmap/ws/XMap"), "Check minimal input for European map in Azure.");
            Assert.That(XServerUrl.Complete("eu-t", "XMAP"), Is.EqualTo("https://xmap-eu-t.cloud.ptvgroup.com/xmap/ws/XMap"), "Check minimal input for European map in Azure.");
            Assert.That(XServerUrl.Complete("eu-x", "XMAP"), Is.EqualTo("http://eu-x:50010/xmap/ws/XMap"), "Check wrong input for European map (not corrected).");

            Assert.That(XServerUrl.Complete("xmap-eu-n", "xmAP"), Is.EqualTo("https://xmap-eu-n.cloud.ptvgroup.com/xmap/ws/XMap"), "Check minimal input + module for European map in Azure.");
            Assert.That(XServerUrl.Complete("xroute-eu-n", "xroUTE"), Is.EqualTo("https://xroute-eu-n.cloud.ptvgroup.com/xroute/ws/XRoute"), "Check minimal input + module for European map in Azure.");
            Assert.That(XServerUrl.Complete("xlocate-eu-n", "xlocATE"), Is.EqualTo("https://xlocate-eu-n.cloud.ptvgroup.com/xlocate/ws/XLocate"), "Check minimal input + module for European map in Azure.");
            Assert.That(XServerUrl.Complete("eu-n.cloud.ptvgroup.com", "xmap"), Is.EqualTo("https://xmap-eu-n.cloud.ptvgroup.com/xmap/ws/XMap"), "Check minimal input + cloud.ptvgroup.com for European map in Azure.");
            Assert.That(XServerUrl.Complete("xmap-eu-n.cloud.ptvgroup.com", "xmap"), Is.EqualTo("https://xmap-eu-n.cloud.ptvgroup.com/xmap/ws/XMap"), "Check minimal input + module + cloud.ptvgroup.com for European map in Azure.");
            Assert.That(XServerUrl.Complete("xmap-eu-n.cloud.ptvgroup.com:50010", "xmap"), Is.EqualTo("https://xmap-eu-n.cloud.ptvgroup.com/xmap/ws/XMap"), "Check minimal input + module + cloud.ptvgroup.com + port for European map in Azure.");
            Assert.That(XServerUrl.Complete("xmap-eu-n.cloud.ptvgroup.com/foo/bar", "xmap"), Is.EqualTo("https://xmap-eu-n.cloud.ptvgroup.com/xmap/ws/XMap"), "Check minimal input + module + cloud.ptvgroup.com + port for European map in Azure.");

            Assert.That(XServerUrl.Complete("tralala-n", "XMAP"), Is.EqualTo("https://xmap-tralala-n.cloud.ptvgroup.com/xmap/ws/XMap"), "Check abnormal map name in Azure, which will be corrected.");
            Assert.That(XServerUrl.Complete("ymap-humba-t", "XMAP"), Is.EqualTo("http://ymap-humba-t:50010/xmap/ws/XMap"), "Check abnormal module name in Azure, which will not be corrected.");

            // No scheme . On premise
            Assert.That(XServerUrl.Complete("myHost.com", "XMAP"), Is.EqualTo("http://myHost.com:50010/xmap/ws/XMap"), "Check minimal input on premise.");
            Assert.That(XServerUrl.Complete("myHost.com", "XROUTE"), Is.EqualTo("http://myHost.com:50030/xroute/ws/XRoute"), "Check minimal input on premise.");
            Assert.That(XServerUrl.Complete("myHost.com", "XLOCATE"), Is.EqualTo("http://myHost.com:50020/xlocate/ws/XLocate"), "Check minimal input on premise.");
            Assert.That(XServerUrl.Complete("myHost.com:4711", "XMAP"), Is.EqualTo("http://myHost.com:4711/xmap/ws/XMap"), "Check minimal input on premise.");
            Assert.That(XServerUrl.Complete("myHost.com/foo/bar", "XMAP"), Is.EqualTo("http://myHost.com:50010/xmap/ws/XMap"), "Check minimal input with no scheme on premise.");

            Assert.That(XServerUrl.Complete("127.0.0.1", "XMAP"), Is.EqualTo("http://127.0.0.1:50010/xmap/ws/XMap"), "Check 127.0.0.1.");
            Assert.That(XServerUrl.Complete("127.0.0.1:50010", "XMAP"), Is.EqualTo("http://127.0.0.1:50010/xmap/ws/XMap"), "Check 127.0.0.1. with correct port.");
            Assert.That(XServerUrl.Complete("127.0.0.1:50011", "XMAP"), Is.EqualTo("http://127.0.0.1:50011/xmap/ws/XMap"), "Check 127.0.0.1. with correct port.");

            // With scheme: URL is not corrected or completed
            Assert.That(XServerUrl.Complete("http://127.0.0.1", "XMAP"), Is.EqualTo("http://127.0.0.1"), "Check 127.0.0.1. with http protocol.");
            Assert.That(XServerUrl.Complete("https://127.0.0.1:4711", "XMAP"), Is.EqualTo("https://127.0.0.1:4711"), "Check 127.0.0.1. with https protocol.");
            Assert.That(XServerUrl.Complete("https://xmap-eu-n.cloud.ptvgroup.com/xmap/ws/XMap", "XMAP"), Is.EqualTo("https://xmap-eu-n.cloud.ptvgroup.com/xmap/ws/XMap"), "https address remains unchanged.");
            Assert.That(XServerUrl.Complete("https://xmap-eu-n.cloud.ptvgroup.com/foo/bar", "XMAP"), Is.EqualTo("https://xmap-eu-n.cloud.ptvgroup.com/foo/bar"), "https address with wrong path.");
            Assert.That(XServerUrl.Complete("https://xmap-eu-n.cloud.ptvgroup.com", "XMAP"), Is.EqualTo("https://xmap-eu-n.cloud.ptvgroup.com"), "https address without path.");
            Assert.That(XServerUrl.Complete("http://xmap-eu-n.cloud.ptvgroup.com", "XMAP"), Is.EqualTo("http://xmap-eu-n.cloud.ptvgroup.com"), "https address with wrong protocol remains wrong.");

            Assert.That(XServerUrl.Complete("https://172.23.112.181:51412/xmap/ws/XMap", "XMAP"), Is.EqualTo("https://172.23.112.181:51412/xmap/ws/XMap"), "SmartTour address.");
        }
    }
}
