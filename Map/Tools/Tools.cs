// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media.Imaging;
using xserver;
using Ptv.XServer.Controls.Map.TileProviders;
using System.Text.RegularExpressions;
using System.Web.Services.Protocols;
using System.Net;

namespace Ptv.XServer.Controls.Map.Tools
{
    /// <summary> Helper class extending the functionality of rectangles/envelopes. </summary>
    public static class RectExtensions
    {
        #region public methods
        /// <summary> Inflate the specified rectangle by a rectangle, keeping the center invariant. </summary>
        /// <param name="rect"> Rectangle (top less than bottom!!!) to inflate. </param>
        /// <param name="factor"> Values higher than 1 inflate the rectangle, values between 0 and 1 shrink it. Negative values are not recommended. </param>
        /// <returns> Inflated/shrunken rectangle. </returns>
        public static Rect Inflate(this Rect rect, double factor)
        {
            var dx = rect.Width / 2 * (factor - 1);
            var dy = rect.Height / 2 * (factor - 1);

            return new Rect(new System.Windows.Point(rect.Left - dx, rect.Top - dy), new System.Windows.Point(rect.Right + dx, rect.Bottom + dy));
        }

        /// <summary> Create the minimal bounding rectangle around a polygon. </summary>
        /// <param name="points">Set of points defining the polygon.</param>
        /// <returns>Minimal bounding rectangle of the polygon.</returns>
        public static Rect CreateEnvelope(IEnumerable<System.Windows.Point> points)
        {
            double left = double.MaxValue, top = double.MinValue, right = double.MinValue, bottom = double.MaxValue;
            foreach (var point in points)
            {
                left = Math.Min(left, point.X);
                top = Math.Max(top, point.Y);
                right = Math.Max(right, point.X);
                bottom = Math.Min(bottom, point.Y);
            }
            return new Rect(new System.Windows.Point(left, top), new System.Windows.Point(right, bottom));
        }

        /// <summary> Create the minimal bounding rectangle around a shape object. </summary>
        /// <param name="shape">Shape object from which the minimal bounding rectangle has to be calculated.</param>
        /// <returns>Minimal bounding rectangle of the shape.</returns>
        public static Rect Envelope(this Shape shape)
        {
            return new Rect(Canvas.GetLeft(shape), Canvas.GetTop(shape), shape.Width, shape.Height);
        }
        #endregion
    }

    /// <summary> Helper class for converting points and distance into different coordinate formats. </summary>
    public static class WgsExtensions
    {
        #region public methods
        /// <summary> Conversion of a pixel distance into a Mercator distance. </summary>
        /// <param name="mapView">Map object needed for pixel (resolution) information.</param>
        /// <param name="canvas">Canvas object needed for the current scale information.</param>
        /// <param name="distance">Distance given in pixel format.</param>
        /// <returns>Distance given in PTV-internal Mercator format.</returns>
        public static double CanvasToPtvMercator(this MapView mapView, UIElement canvas, double distance)
        {
            var point1 = new System.Windows.Point(0, 0);
            point1 = CanvasToPtvMercator(mapView, canvas, point1);
            var point2 = new System.Windows.Point(distance, 0);
            point2 = CanvasToPtvMercator(mapView, canvas, point2);
            return Math.Abs(point1.X - point2.X);
        }

        /// <summary> Conversion of a pixel coordinate into a PTV-internal Mercator format. </summary>
        /// <param name="mapView">Map object needed for pixel (resolution) information.</param>
        /// <param name="canvas">Canvas object needed for the current scale information.</param>
        /// <param name="point">Point containing pixel coordinates.</param>
        /// <returns>Point containing PTV-internal Mercator coordinates.</returns>
        public static System.Windows.Point CanvasToPtvMercator(this MapView mapView, UIElement canvas, System.Windows.Point point)
        {
            System.Windows.Point geoCanvasPoint = canvas.TransformToVisual(mapView.GeoCanvas).Transform(point);

            return new System.Windows.Point(
               (geoCanvasPoint.X / MapView.ZoomAdjust * MapView.LogicalSize / MapView.ReferenceSize) - 1.0 / MapView.ZoomAdjust * MapView.LogicalSize / 2 - mapView.OriginOffset.X,
               -(geoCanvasPoint.Y / MapView.ZoomAdjust * MapView.LogicalSize / MapView.ReferenceSize) + 1.0 / MapView.ZoomAdjust * MapView.LogicalSize / 2 + mapView.OriginOffset.Y);
        }

        /// <summary> Conversion of a rectangle containing pixel coordinates into a PTV-internal Mercator format. </summary>
        /// <param name="mapView">Map object needed for pixel (resolution) information.</param>
        /// <param name="canvas">Canvas object needed for the current scale information.</param>
        /// <param name="rect">Rectangle containing pixel coordinates.</param>
        /// <returns>Rectangle containing PTV-internal Mercator coordinates.</returns>
        public static MapRectangle CanvasToPtvMercator(this MapView mapView, UIElement canvas, MapRectangle rect)
        {
            return new MapRectangle(CanvasToPtvMercator(mapView, canvas, rect.NorthWest), CanvasToPtvMercator(mapView, canvas, rect.SouthEast));
        }

        /// <summary> Conversion of multiple pixel coordinates into a PTV-internal Mercator format. </summary>
        /// <param name="mapView">Map object needed for pixel (resolution) information.</param>
        /// <param name="canvas">Canvas object needed for the current scale information.</param>
        /// <param name="points">Multiple points (enumeration) containing pixel coordinates.</param>
        /// <returns>Multiple points (enumeration) containing PTV-internal Mercator coordinates.</returns>
        public static IEnumerable<System.Windows.Point> CanvasToPtvMercator(this MapView mapView, UIElement canvas, IEnumerable<System.Windows.Point> points)
        {
            return points.Select(point => CanvasToPtvMercator(mapView, canvas, point));
        }

        /// <summary> Conversion of a pixel coordinate into WGS format. </summary>
        /// <param name="mapView">Map object needed for pixel (resolution) information.</param>
        /// <param name="canvas">Canvas object needed for the current scale information.</param>
        /// <param name="point">Point containing pixel coordinates.</param>
        /// <returns>Point containing WGS coordinates.</returns>
        public static System.Windows.Point CanvasToWgs(this MapView mapView, UIElement canvas, System.Windows.Point point)
        {
            return GeoTransform.PtvMercatorToWGS(CanvasToPtvMercator(mapView, canvas, point));
        }

        /// <summary> Conversion of a PTV-internal Mercator coordinate into pixel coordinates. </summary>
        /// <param name="mapView">Map object needed for pixel (resolution) information.</param>
        /// <param name="canvas">Canvas object needed for the current scale information.</param>
        /// <param name="mercatorPoint">Point containing PTV-internal Mercator coordinates.</param>
        /// <returns>Point containing pixel coordinates.</returns>
        public static System.Windows.Point PtvMercatorToCanvas(this MapView mapView, UIElement canvas, System.Windows.Point mercatorPoint)
        {
            var geoCanvasPoint = new System.Windows.Point(
               (mercatorPoint.X + mapView.OriginOffset.X + 1.0 / MapView.ZoomAdjust * MapView.LogicalSize / 2) * MapView.ZoomAdjust / MapView.LogicalSize * MapView.ReferenceSize,
               (-mercatorPoint.Y + mapView.OriginOffset.Y + 1.0 / MapView.ZoomAdjust * MapView.LogicalSize / 2) * MapView.ZoomAdjust / MapView.LogicalSize * MapView.ReferenceSize);

            return mapView.GeoCanvas.TransformToVisual(canvas).Transform(geoCanvasPoint);
        }

        /// <summary> Conversion of a PTV-internal Mercator distance into a WGS distance. </summary>
        /// <param name="distance">Distance given in PTV-internal Mercator format.</param>
        /// <returns>Distance given in pixel format.</returns>
        public static double PtvMercatorToWGS(double distance)
        {
            var point1 = new System.Windows.Point(0, 0);
            point1 = GeoTransform.PtvMercatorToWGS(point1);
            var point2 = new System.Windows.Point(distance, 0);
            point2 = GeoTransform.PtvMercatorToWGS(point2);
            return Math.Abs(point1.X - point2.X);
        }

        /// <summary> Conversion of a WGS coordinate into pixel format. </summary>
        /// <param name="mapView">Map object needed for pixel (resolution) information.</param>
        /// <param name="canvas">Canvas object needed for the current scale information.</param>
        /// <param name="wgsPoint">Point containing WGS coordinates.</param>
        /// <returns>Point containing pixel coordinates.</returns>
        public static System.Windows.Point WgsToCanvas(this MapView mapView, UIElement canvas, System.Windows.Point wgsPoint)
        {
            return PtvMercatorToCanvas(mapView, canvas, GeoTransform.WGSToPtvMercator(wgsPoint));
        }

        /// <summary> Gets the bounding box of the visible map section while the map is in animation mode. </summary>
        /// <param name="mapView"> Map object containing the current map section. </param>
        /// <returns> The rectangle for the current envelope. </returns>
        public static MapRectangle GetCurrentEnvelopePtvMercator(this MapView mapView)
        {
            return mapView.CurrentEnvelope;
        }

        #endregion
    }

    /// <summary> Helper functions for the visual tree. </summary>
    public static class MapElementExtensions
    {
        #region public methods
        /// <summary> Finds an object of type T for a framework element which is somewhere in the visual tree. </summary>
        /// <typeparam name="T"> The type of the object. </typeparam>
        /// <param name="fe"> The framework element. </param>
        /// <returns> The instance of the object or null if not found. </returns>
        public static T FindRelative<T>(this FrameworkElement fe) where T : DependencyObject
        {
            if (fe.Parent is T variable)
                return variable;

            return FindChild<T>(fe.Parent) ?? (fe.Parent is FrameworkElement element ? FindRelative<T>(element) : null);
        }

        /// <summary> Finds an object of type T for a framework element which is a parent in the visual tree. </summary>
        /// <typeparam name="T"> The type of the object. </typeparam>
        /// <param name="fe"> The framework element. </param>
        /// <returns> The instance of the object or null if not found. </returns>
        public static T FindParent<T>(FrameworkElement fe) where T : DependencyObject
        {
            return fe.Parent is T variable 
                ? variable
                : fe.Parent is FrameworkElement element ? FindParent<T>(element) : null;
        }

        /// <summary> Gets whether a control and all its parents are visible. </summary>
        /// <param name="element"> The element to check. </param>
        /// <returns> True if the element is visible. </returns>
        public static bool IsControlVisible(FrameworkElement element)
        {
            while (element != null)
            {
                var visibility = (Visibility)element.GetValue(UIElement.VisibilityProperty);

                if (visibility == Visibility.Collapsed)
                    return false;

                element = element.Parent as FrameworkElement;
            }

            return true;
        }

        /// <summary> Documentation in progress... </summary>
        /// <typeparam name="T"> Documentation in progress... </typeparam>
        /// <param name="depObj"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public static T FindChild<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) return null;

            foreach(object o in LogicalTreeHelper.GetChildren(depObj))
            {
                if (!(o is DependencyObject child))
                    continue;

                if (child is T variable)
                    return variable;

                foreach (T childOfChild in FindChildren<T>(child))
                    return childOfChild;
            }

            return null;
        }

        /// <summary> Documentation in progress... </summary>
        /// <typeparam name="T"> Documentation in progress... </typeparam>
        /// <param name="depObj"> Documentation in progress... </param>
        /// <returns> Documentation in progress... </returns>
        public static IEnumerable<T> FindChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield break;

            foreach (object o in LogicalTreeHelper.GetChildren(depObj))
            {
                if (!(o is DependencyObject child))
                    yield break;

                if (child is T variable)
                {
                    yield return variable;
                }

                foreach (T childOfChild in FindChildren<T>(child))
                {
                    yield return childOfChild;
                }
            }
        }

        #endregion
    }

    /// <summary> Extensions for the streaming API. </summary>
    public static class StreamExtensions
    {
        #region public methods
        /// <summary> Byte wise copy of a stream. </summary>
        /// <param name="src"> Source stream. </param>
        /// <param name="destinationStream"> Destination stream. </param>
        /// <returns>Destination stream.</returns>
        public static Stream CopyTo(this Stream src, Stream destinationStream)
        {
            var buffer = new byte[4096];
            int bytesRead;
            while ((bytesRead = src.Read(buffer, 0, buffer.Length)) > 0)
                destinationStream.Write(buffer, 0, bytesRead);
            return destinationStream;
        }

        /// <summary>
        /// Reads and returns the bytes from the given stream.
        /// </summary>
        /// <param name="stm">Stream to get the bytes from.</param>
        /// <param name="forceNullIfEmpty">If set to true, forces null to be returned if the stream is empty.</param>
        /// <returns>Bytes read.</returns>
        public static byte[] GetBytes(this Stream stm, bool forceNullIfEmpty = false)
        {
            if (stm is MemoryStream memoryStream)
                return memoryStream.Length == 0 && forceNullIfEmpty ? null : memoryStream.ToArray();

            using (memoryStream = new MemoryStream())
                return stm.CopyTo(memoryStream).GetBytes(forceNullIfEmpty);
        }
        
        #endregion
    }

    /// <summary> Extensions helpful for StringBuilder class. </summary>
    public static class StringBuilderExtensions
    {
        /// <summary> Method adding a separator between the appended texts, but only if the string builder is not empty. 
        /// This avoids trailing separators, which commonly should appear only 'inside' the individual text elements. </summary>
        /// <param name="stringBuilder">Variable to add texts with separators. </param>
        /// <param name="text"> Text which should be appended to the stringBuilder. </param>
        /// <param name="separator">Separator inserted between the appended texts. </param>
        public static void AppendWithSeparator(this System.Text.StringBuilder stringBuilder, string text, string separator=",")
        {
            if (stringBuilder.Length > 0)
                stringBuilder.Append(Environment.NewLine);
            stringBuilder.Append(text);
        }
    }

    /// <summary> Helper functions for handling resources. </summary>
    public static class ResourceHelper
    {
        /// <summary> Loads a bitmap image from an internal resource path. </summary>
        /// <param name="resourcePath"> The path of the resource. </param>
        /// <returns> The bitmap image. </returns>
        public static BitmapImage LoadBitmapFromResource(string resourcePath)
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.StreamSource = Application.GetResourceStream(new Uri(resourcePath, UriKind.Relative))?.Stream;
            bmp.EndInit();
            bmp.Freeze();

            return bmp;
        }
    }

    /// <summary>
    /// Collects tooling methods for the XMap server.
    /// </summary>
    public static class XMapTools
    {
        /// <summary>
        /// Cache for requests to AreXMapLayersAvailable(...). Clear this cache to force new XMap requests
        /// instead of returning cache results.
        /// </summary>
        public static Dictionary<string, bool?> CheckedXMapLayers = new Dictionary<string, bool?>();

        /// <summary>Service implementation of the xMap server. It is implemented as a static variable to make
        /// it possible to modify it for mocking (see XMapToolsTest.cs).</summary>
        [ThreadStatic]
        private static IXMapWSBinding Service;

        /// <summary>
        /// Checks if one or n certain layers are available or not.
        /// </summary>
        /// <param name="url">The url to the XMap instance.</param>
        /// <param name="contextKey">The used context key or null if none is used.</param>
        /// <param name="layers">The layers to check.</param>
        /// <param name="profile">The profile used to check the availability.</param>
        /// <param name="expectedErrorCode">The expected error code if the at least one layer does not exist. 
        /// SoapExceptions carrying this error code will be caught and the return value of the method will be false.
        /// SoapExceptions carrying another error code will not be caught and thus be thrown by this API.
        /// </param>
        /// <param name="xServerUser">User name needed for Azure Cloud.</param>
        /// <param name="xServerPassword">Password needed for Azure Cloud.</param>
        /// <returns>True if one or n layers are available, false if at least one layer is not available. 
        /// May throw exceptions in case of malformed url or wrong contextKey definition.</returns>
        public static bool AreXMapLayersAvailable(string url, string contextKey, Layer[] layers, string profile, string expectedErrorCode, string xServerUser = null, string xServerPassword = null)
        {
            string key = url + profile;

            key = layers.Aggregate(key, (current, l) => current + "_" + l.name);

            if (CheckedXMapLayers.ContainsKey(key) && CheckedXMapLayers[key] != null)
                return CheckedXMapLayers[key].Value;

            try
            {
                Service = Service ?? new XMapWSServiceImpl(url, xServerUser, xServerPassword);

                var mapParams = new MapParams { showScale = false, useMiles = false };
                var imageInfo = new ImageInfo { format = ImageFileFormat.GIF, height = 32, width = 32 };
                var bbox = new BoundingBox
                {
                    leftTop = new xserver.Point { point = new PlainPoint { x = 0, y = 10 } },
                    rightBottom = new xserver.Point { point = new PlainPoint { x = 0, y = 10 } }
                };

                var callerContextProps = new List<CallerContextProperty>
                {
                    new CallerContextProperty {key = "CoordFormat", value = "PTV_MERCATOR"},
                    new CallerContextProperty {key = "Profile", value = profile}
                };
                if (!string.IsNullOrEmpty(contextKey))
                    callerContextProps.Add(new CallerContextProperty { key = "ContextKey", value = contextKey });

                var cc = new CallerContext { wrappedProperties = callerContextProps.ToArray() };

                try
                {
                    Service.renderMapBoundingBox(bbox, mapParams, imageInfo, layers, true, cc);
                }
                catch (SoapException se)
                {
                    if (!se.Code.Name.Equals(expectedErrorCode)) throw;

                    CheckedXMapLayers.Add(key, false);
                    return false;
                }

                CheckedXMapLayers.Add(key, true);
                return true;
            }
            finally
            {
                (Service as IDisposable)?.Dispose();
                Service = null;
            }
        }
    }

    /// <summary> Helper class for changing a color value. </summary>
    public static class ColorExtensions
    {
        /// <summary> Helper method to lighten a color value (R, G or B). </summary>
        /// <param name="color"> Color to be lightened. </param>
        /// <returns> The lightened color. </returns>
        public static byte Lighten(byte color)
        {
            byte tmp = color;

            if (tmp < 70) tmp += 80;
            else if (tmp > 200) tmp = Convert.ToByte(Math.Min(tmp + 40, 255));
            else tmp = Convert.ToByte(Math.Min(tmp * 1.5, 255));

            return tmp;
        }

        /// <summary> Helper method to darken a color value (R, G or B). </summary>
        /// <param name="color"> Color to be darkened. </param>
        /// <returns> The darkened color. </returns>
        public static byte Darken(byte color)
        {
            byte tmp = color;

            if (tmp < 50) tmp = Convert.ToByte(Math.Max(tmp - 30, 0));
            else if (tmp > 200) tmp -= 50;
            else tmp = Convert.ToByte(tmp * 0.75);

            return tmp;
        }
    }

    internal static class DesignTest
    {
        /// <summary> 
        /// Initializes an instance of Util class 
        /// </summary> 
        static DesignTest()
        {
            // design mode is true if host process is: Visual Studio,  
            // Visual Studio Express Versions (C#, VB, C++) or SharpDevelop 
            var designerHosts = new List<string> { "devenv", "vcsexpress", "vbexpress", "vcexpress", "sharpdevelop", "wdexpress" };

            using (var process = System.Diagnostics.Process.GetCurrentProcess())
            {
                var processName = process.ProcessName.ToLower();
                DesignMode = designerHosts.Contains(processName);
            }
        }

        /// <summary> 
        /// Gets true, if we are in design mode of Visual Studio etc.. 
        /// </summary> 
        public static bool DesignMode { get; }
    }

    /// <summary>
    /// Extensions for the streaming API.
    /// </summary>
    public static class ImageExtensions
    {
        #region public methods

        /// <summary>
        /// Resizes an image to a specific size.
        /// </summary>
        /// <param name="stream">Stream containing the image.</param>
        /// <param name="targetWidth">Target width</param>
        /// <param name="targetHeight">Target height</param>
        /// <returns>Resized image.</returns>
        public static Stream ResizeImage(this Stream stream, int targetWidth, int targetHeight)
        {
            var memoryStream = new MemoryStream();

            using (var bitmap = new System.Drawing.Bitmap(targetWidth, targetHeight))
            {
                using (var image = System.Drawing.Image.FromStream(stream))
                {
                    using (var graphics = System.Drawing.Graphics.FromImage(bitmap))
                        graphics.DrawImage(image, 0, 0, targetWidth, targetHeight);
                    
                    bitmap.Save(memoryStream, image.RawFormat);
                }
            }

            memoryStream.Seek(0, SeekOrigin.Begin);

            return memoryStream;
        }

        #endregion
    }

    /// <summary>
    /// Helper class for converting URL address into the correct format.
    /// </summary>
    public static class XServerUrl
    {
        /// <summary>
        /// To access the functionality provided by the XServer's web services, a URL has to be specified, which has to
        /// fulfill some constraints. 
        /// <para>
        /// At first, according to the concrete xServer (for example xMap), the URL has to look like
        /// (scheme)://(host):(port)/xmap/ws/XMap, especially the path with its mixed uppercase and lower case characters.
        /// If no scheme is specified in the input URL, then a completion is started, concerning an adequate value for this
        /// scheme, a check for an xServer internet deployed XServer and eventually insertion of the default port, if no one is specifed
        /// in the input URL.
        /// </para>
        /// <para>
        /// If the scheme (or protocol) like http is missing in the URL, then this method examines the host for
        /// an xServer internet-like name. The host indicates an Azure XServer, if it contains some characters followed by a '-',
        /// followed by one of the letters 'h', 'n' or 't'. Optionally, the module name may precede this construct, and the texts
        /// '-test' or '-integration' may follow also optionally. The path is completely ignored. 
        /// The scheme is set to https, if an xServer internet host name is detected, otherwise http is used.
        /// The following URLs will all be transformed to https://xmap-eu-n.cloud.ptvgroup.com/xmap/ws/XMap :
        /// eu-n,
        /// xmap-eu-n,
        /// eu-n.cloud.ptvgroup.com
        /// xmap-eu-n.cloud.ptvgroup.com
        /// </para>
        /// <para>
        /// If a port is part of the URL, it will be used, except an xServer internet host is detected (then the port is completely ignored).
        /// If no scheme and no port is specified (and no xServer internet host is detected), then a default port-assignment is used.
        /// </para>
        /// </summary>
        /// <param name="baseUrl">Eventually partial specified URL needed for XServer's web services, for example 'eu-n-test' or 'http://localhost:50010'.</param>
        /// <param name="moduleName">Name of the XServer module, like XMap or XRoute. The casing is corrected according the internal requirements.</param>
        /// <returns></returns>
        public static string Complete(string baseUrl, string moduleName)
        {
            string lowerModuleName = moduleName.ToLower();
            string camelModuleName = moduleName.Substring(0, 2).ToUpper() + moduleName.Substring(2).ToLower();

            // Divide URL into scheme, host and port. The values can be found in match.Groups
            var regex = new Regex(@"^(https?://)?([^\:\/]+)(:\d+)?", RegexOptions.IgnoreCase);
            var match = regex.Match(baseUrl);
            if (!match.Success)
                return baseUrl;

            var scheme = match.Groups[1].ToString();
            if (!string.IsNullOrEmpty(scheme))
                return baseUrl;
            // if no scheme is specified, the host is examined for an xServer internet like name

            var host = match.Groups[2].ToString();
            var port = match.Groups[3].ToString();

            if (Match(@"^(?:" + lowerModuleName + @"-)?([a-z]+-[hnt](?:-test|-integration)?)(?:\.cloud\.ptvgroup\.com)?$", host, out match)) // eu-n style
            {
                scheme = "https://";
                host = lowerModuleName + "-" + match.Groups[1] + ".cloud.ptvgroup.com";
                port = string.Empty;
            }
            else if (Match(@"^(?:([a-z]+(?:-[a-z]{2})?(?:-test|-integration)?))(?:\.cloud\.ptvgroup\.com)?$", host, out match)) // api(-eu) style
            {
                scheme = "https://";
                host = match.Groups[1] + ".cloud.ptvgroup.com";
                port = string.Empty;
            }
            else // on-premise
            {
                scheme = "http://";
                if (string.IsNullOrEmpty(port))
                    port = ":" + Port(lowerModuleName).ToString(CultureInfo.InvariantCulture);
            }

            return scheme + host + port + "/" + lowerModuleName + "/ws/" + camelModuleName;
        }

        private static bool Match(string pattern, string input, out Match match)
        {
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);
            match = regex.Match(input);
            return match.Success;
        }

        /// <summary> Checks if the provided URL address is an XServer deployed in an XServer Internet environment. </summary>
        /// <param name="url">URL to check for xServer internet deployment.</param>
        /// <returns>True if it contains ".cloud.ptvgroup.com".</returns>
        public static bool IsXServerInternet(string url)
        {
            return url.ToLower().Contains(".cloud.ptvgroup.com");
        }

        /// <summary>
        /// Checks if the provided URL addresses uses a DeCarta backend.
        /// </summary>
        /// <param name="url">URL to check for xServer internet deployment.</param>
        /// <returns></returns>
        public static bool IsDecartaBackend(string url)
        {
            return IsXServerInternet(url) && (url.ToLower().Contains("-cn-n") || url.ToLower().Contains("-jp-n"));
        }

        /// <summary>
        /// Matches a module name to the corresponding port. For example, 'xmap' results in a return value of 50010,
        /// which represents the default port number, commonly used for XMap.
        /// </summary>
        /// <param name="moduleName">Name of the mode, for example 'XMap' or 'XRoute'.</param>
        /// <returns>Default port number of the module specified in <paramref name="moduleName"/></returns>
        public static int Port(string moduleName)
        {
            switch (moduleName.ToLower())
            {
                case "xmap": return 50010;
                case "xlocate": return 50020;
                case "xroute": return 50030;
                case "xmapmatch": return 50040;
                case "xtour": return 50090;
                default: return 50010; // All add-ons are currently associated to the xMap
            }
        }
    }
}
