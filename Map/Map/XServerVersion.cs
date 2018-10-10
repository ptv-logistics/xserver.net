// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using Ptv.XServer.Controls.Map.Layers;
using System.Globalization;
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
        string AdjustedUrl(string moduleName = "xmap");
        string WithServicePath(string protocolShortcut, string moduleName);

        bool IsValidUrl();
        bool IsCloudBased();

        void InitializeMapLayers(Map map, string xmapCredentials);
    }

    internal class XServer1Version : IXServerVersion
    {
        private readonly string baseUrl;

        public XServer1Version(string url)
        {
            baseUrl = url;
        }

        public string AdjustedUrl(string moduleName = "xmap")
        {
            return XServerUrl.Complete(baseUrl, moduleName);
        }

        private static bool Match(string pattern, string input, out Match match)
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            match = regex.Match(input);
            return match.Success;
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
                using (var response = request.GetResponse() as HttpWebResponse)
                    return response?.StatusCode == HttpStatusCode.OK;
            }
            catch { return false; }
        }

        public bool IsCloudBased()
        {
            return AdjustedUrl().ToLower().Contains(".cloud.ptvgroup.com");
        }

        public void InitializeMapLayers(Map map, string xmapCredentials)
        {
            map.Layers.RemoveXMapBaseLayers();

            string adjustedUrl = AdjustedUrl();
            if (string.IsNullOrEmpty(adjustedUrl)) return;

            var xmapMetaInfo = new XMapMetaInfo(adjustedUrl);
            if (xmapCredentials?.Contains(":") ?? false)
            {
                var userPassword = xmapCredentials.Split(':');
                xmapMetaInfo.SetCredentials(userPassword[0], userPassword[1]);
            }
            map.Layers.InsertXMapBaseLayers(xmapMetaInfo);
        }
    }

    internal class XServer2Version : IXServerVersion
    {
        private readonly string baseUrl;

        public XServer2Version(string url)
        {
            baseUrl = url;
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

            return scheme + host + port; // Everything beyond the port has to be omitted
        }

        public string WithServicePath(string protocolShortcut, string moduleName)
        {
            string camelModuleName = moduleName.Substring(0, 2).ToUpper() + moduleName.Substring(2).ToLower();

            return $"{AdjustedUrl(moduleName).TrimEnd('/')}/services/{protocolShortcut}/{camelModuleName}";
        }

        public bool IsValidUrl()
        {
            if (string.IsNullOrEmpty(baseUrl)) return false;

            try
            {
                var wsdlUrl = WithServicePath("ws", "xmap") + "?wsdl";

                var request = (HttpWebRequest) WebRequest.Create(wsdlUrl);
                request.Timeout = 5000;
                using (var response = request.GetResponse() as HttpWebResponse)
                    return response?.StatusCode == HttpStatusCode.OK;
            }
            catch { return false; }
        }

        public bool IsCloudBased()
        {
            var adjustedUrl = AdjustedUrl().ToLower();
            return adjustedUrl.Contains(".cloud.ptvgroup.com") && adjustedUrl.Contains("xserver2");
        }

        public void InitializeMapLayers(Map map, string xmapCredentials)
        {
            if (map.Xmap2LayerFactory != null)
            {
                map.Layers.Remove(map.Xmap2LayerFactory.BackgroundLayer);
                map.Layers.Remove(map.Xmap2LayerFactory.ForegroundLayer);
                map.Xmap2LayerFactory = null;
            }

            if (string.IsNullOrEmpty(AdjustedUrl())) return;

            map.Xmap2LayerFactory = new LayerFactory(AdjustedUrl(), xmapCredentials);
            map.Xmap2LayerFactory.BackgroundLayer.Icon = ResourceHelper.LoadBitmapFromResource("Ptv.XServer.Controls.Map;component/Resources/Background.png");
            map.Xmap2LayerFactory.BackgroundLayer.Caption = MapLocalizer.GetString(MapStringId.Background);
            map.Xmap2LayerFactory.BackgroundThemes.Add("Background");
            map.Xmap2LayerFactory.BackgroundThemes.Add("Transport");

            map.Xmap2LayerFactory.ForegroundLayer.Icon = ResourceHelper.LoadBitmapFromResource("Ptv.XServer.Controls.Map;component/Resources/Labels.png");
            map.Xmap2LayerFactory.ForegroundLayer.Caption = MapLocalizer.GetString(MapStringId.Labels);
            map.Xmap2LayerFactory.ForegroundThemes.Add("Labels");

            map.Layers.Add(map.Xmap2LayerFactory.BackgroundLayer);
            map.Layers.Add(map.Xmap2LayerFactory.ForegroundLayer);
        }
    }
}
