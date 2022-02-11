// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using Ptv.XServer.Controls.Map.Layers;
using System.Net;
using System.Text.RegularExpressions;
using Ptv.XServer.Controls.Map.Tools;
using Ptv.XServer.Controls.Map.Layers.Xmap2;
using Ptv.XServer.Controls.Map.Localization;

// ReSharper disable once CheckNamespace
namespace Ptv.XServer.Controls.Map
{
    internal interface IXServerVersion
    {
        bool IsValidUrl();
        bool IsCloudBased();
        string AdjustedUrl(string moduleName = "xmap");
        string WithServicePath(string protocolShortcut, string moduleName);
        void Initialize(Map map);
        string XMapCredentials { get; set; }
        string Token { get; }
    }

    internal class XServer1Version : XServerVersionBase, IXServerVersion
    {
        public XServer1Version(string url, string xMapCredentials) : base(url, xMapCredentials) { }

        public string AdjustedUrl(string moduleName = "xmap")
        {
            return XServerUrl.Complete(baseUrl, moduleName);
        }

        public static int Port(string moduleName)
        {
            switch (moduleName)
            {
                case "xmap": return 50010;
                case "xlocate": return 50020;
                case "xroute": return 50030;
                case "xmapmatch": return 50040;
                case "xtour": return 50090;
                default: return 50010; // All add-ons are currently associated to the xMap
            }
        }

        public string WithServicePath(string protocolShortcut, string moduleName)
        {
            var lowerModuleName = moduleName.ToLower();
            var camelModuleName = moduleName.Substring(0, 2).ToUpper() + moduleName.Substring(2).ToLower();

            return $"{AdjustedUrl(moduleName).TrimEnd('/')}/{lowerModuleName}/{protocolShortcut}/{camelModuleName}";
        }

        public bool IsValidUrl()
        {
            if (string.IsNullOrEmpty(baseUrl)) return false;

            try
            {
                string wsdlUrl = WithServicePath("ws", "xmap") + "?wsdl";

                var request = (HttpWebRequest) WebRequest.Create(wsdlUrl);
                request.Timeout = 5000;
                var response = request.GetResponse() as HttpWebResponse;
                return response?.StatusCode == HttpStatusCode.OK;
            }
            catch { return false; }
        }

        public bool IsCloudBased()
        {
            return AdjustedUrl().ToLower().Contains(".cloud.ptvgroup.com");
        }

        public void Initialize(Map map)
        {
            map.Layers.RemoveXMapBaseLayers();

            string adjustedUrl = AdjustedUrl();
            if (string.IsNullOrEmpty(adjustedUrl)) return;

            var xmapMetaInfo = new XMapMetaInfo(adjustedUrl);
            if (XMapCredentials?.Contains(":") ?? false)
            {
                var userPassword = XMapCredentials.Split(':');
                xmapMetaInfo.SetCredentials(userPassword[0], userPassword[1]);
            }
            map.Layers.InsertXMapBaseLayers(xmapMetaInfo);
        }
    }

    internal class XServer2Version : XServerVersionBase, IXServerVersion
    {
        private readonly string minorVersion;

        public XServer2Version(string url, string xMapCredentials) : base(url, xMapCredentials)
        {
            string[] items = url.Contains(";") ? url.Split(';') : null;
            baseUrl = (items?[0] ?? url).Trim();

            string[] minorVersionSubStrings = (items?[1].Contains("=") ?? false) ? items[1].Split('=') : null;
            minorVersion = (minorVersionSubStrings?[0].Trim().Equals("version") ?? false) ? minorVersionSubStrings[1].Trim() : string.Empty;
        }

        public string AdjustedUrl(string moduleName = "xmap")
        {
            var regex = new Regex(@"^(https?://)?([^\:\/]+)(:\d+)?", RegexOptions.IgnoreCase);
            var match = regex.Match(baseUrl);
            if (!match.Success)
                return baseUrl;

            var scheme = match.Groups[1].ToString();
            var host = match.Groups[2].ToString();
            var port = match.Groups[3].ToString();

            if (string.IsNullOrEmpty(scheme))
                scheme = host.ToLower().StartsWith("xserver2") ? "https://" : "http://";

            if (host.ToLower().StartsWith("xserver2") && !host.ToLower().EndsWith(".cloud.ptvgroup.com"))
                host += ".cloud.ptvgroup.com";

            return scheme + host + port; // Everything beyond the port has to be omitted
        }

        public string WithServicePath(string protocolShortcut, string moduleName)
        {
            var camelModuleName = moduleName.Substring(0, 2).ToUpper() + moduleName.Substring(2).ToLower();
            string minorVersionStringToAdd = string.IsNullOrEmpty(minorVersion) ? string.Empty : "/" + minorVersion;

            return $"{AdjustedUrl(moduleName).TrimEnd('/')}/services/{protocolShortcut}/{camelModuleName}{minorVersionStringToAdd}";
        }

        public bool IsValidUrl()
        {
            if (string.IsNullOrEmpty(baseUrl)) return false;

            try
            {
                var wsdlUrl = WithServicePath("ws", "xmap") + "?wsdl";

                var request = (HttpWebRequest) WebRequest.Create(wsdlUrl);
                request.Timeout = 5000;
                var response = request.GetResponse() as HttpWebResponse;
                return response?.StatusCode == HttpStatusCode.OK;
            }
            catch { return false; }
        }

        public bool IsCloudBased()
        {
            var adjustedUrl = AdjustedUrl().ToLower();
            return adjustedUrl.Contains(".cloud.ptvgroup.com") && adjustedUrl.Contains("xserver2");
        }

        public void Initialize(Map map)
        {
            if (map.Xmap2LayerFactory != null)
            {
                map.Layers.Remove(map.Xmap2LayerFactory.BackgroundLayer);
                map.Layers.Remove(map.Xmap2LayerFactory.LabelLayer);
                map.Xmap2LayerFactory = null;
            }

            if (string.IsNullOrEmpty(AdjustedUrl())) return;

            map.Xmap2LayerFactory = new LayerFactory(this);
            map.Xmap2LayerFactory.MapStyle = map.XMapStyle;
            map.Xmap2LayerFactory.BackgroundLayer.Icon = ResourceHelper.LoadBitmapFromResource("Ptv.XServer.Controls.Map;component/Resources/Background.png");
            map.Xmap2LayerFactory.BackgroundLayer.Caption = MapLocalizer.GetString(MapStringId.Background);
            map.Xmap2LayerFactory.BackgroundThemes.Add("Background");
            map.Xmap2LayerFactory.BackgroundThemes.Add("Transport");

            map.Xmap2LayerFactory.LabelLayer.Icon = ResourceHelper.LoadBitmapFromResource("Ptv.XServer.Controls.Map;component/Resources/Labels.png");
            map.Xmap2LayerFactory.LabelLayer.Caption = MapLocalizer.GetString(MapStringId.Labels);
            map.Xmap2LayerFactory.LabelThemes.Add("Labels");

            map.Layers.Add(map.Xmap2LayerFactory.BackgroundLayer);
            map.Layers.Add(map.Xmap2LayerFactory.LabelLayer);
        }
    }

    internal class XServerVersionBase
    {
        protected XServerVersionBase(string url, string xMapCredentials)
        {
            baseUrl = url;
            XMapCredentials = xMapCredentials;
        }

        protected string baseUrl;

        public string XMapCredentials { get; set; }

        public string Token => (XMapCredentials?.Contains(":") ?? false) ? XMapCredentials.Split(':')[1] : XMapCredentials;
    }

}
