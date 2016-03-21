using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Controls;
using System.Threading;
using System.Windows.Threading;
using System.Windows.Media.Imaging;
using Ptv.XServer.Controls.Map.Canvases;
using Ptv.XServer.Controls.Map.TileProviders;
using Ptv.XServer.Controls.Map.Tools;
using Ptv.XServer.Controls.Map.Layers.Tiled;
using Ptv.XServer.Controls.Map.Localization;

namespace Ptv.XServer.Controls.Map.Layers.Untiled
{
    /// <summary> Returns a bitmap for a PTV_Mercator rectangle. </summary>
    public interface IUntiledProvider
    {
        /// <summary> Returns a bitmap for a map section specified in PTV-internal Mercator format. </summary>
        /// <param name="left"> Left coordinate of the requested map section. </param>
        /// <param name="top"> Top coordinate of the requested map section. </param>
        /// <param name="right"> Right coordinate of the requested map section. </param>
        /// <param name="bottom"> Bottom coordinate of the requested map section. </param>
        /// <param name="width"> Width in pixel of the bitmap. </param>
        /// <param name="height"> Height in pixel of the bitmap. </param>
        /// <returns> Stream containing the bitmap. </returns>
        Stream GetImageStream(double left, double top, double right, double bottom, int width, int height);
    }


    /// <summary> A layer which draws a bitmap overlay on the current map section. </summary>
    ///
    /// UntiledLayer uses some basic hit testing for finding tool tips and it displays the descriptions returned from xMap 
    /// Server, sometimes in a slightly modified way. When using UntiledLayer, you may want to look at:
    /// <remarks>
    /// - <see cref="ToolTipHitTest"/> implements the basic hit test on <see cref="xserver.LayerObject"/> information 
    ///   objects. Elements within a range of 10 pixels (measured taking either the reference point or the full response 
    ///   geometry into account - depending on what actually is available) qualify for tool tip display. Depending on the 
    ///   xMap content requested you may want to change this behavior. <see cref="ToolTipHitTest"/> can be overridden in 
    ///   derived classes.
    /// 
    /// - <see cref="GetToolTipFromLayerObject" /> transforms a <see cref="xserver.LayerObject"/> information object into 
    ///   a tool tip text. By default, <see cref="GetToolTipFromLayerObject" /> returns the xMap Server description as it 
    ///   is. Usually this description needs further formatting; <see cref="GetToolTipFromLayerObject" /> can be overridden 
    ///   in derived classes.
    /// </remarks>
    public class UntiledLayer : BaseLayer, IToolTips
    {
        #region public variables
        /// <summary> Gets or sets the provider which delivers map images as bitmaps, not in diverse tiles. </summary>
        public IUntiledProvider UntiledProvider { get; set; }

        /// <summary> Gets or sets the maximum size of the bitmaps in pixels. </summary>
        public Size MaxRequestSize { get; set; }

        /// <summary> Gets or sets the maximum leve up to which images are requested. </summary>
        public double MinLevel { get; set; }
        #endregion

        /// <summary>the map itself</summary>
        protected UserControl map;
        /// <summary>xMap object information</summary>
        protected xserver.ObjectInfos[] objectInfos;
        /// <summary>xMap image size</summary>
        protected Size imageSize;

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="UntiledLayer"/> class. </summary>
        /// <param name="name"> The unique name of the layer. </param>
        public UntiledLayer(string name) 
            : base(name)
        {
            MaxRequestSize = new Size(2048, 2048);
            MinLevel = 0;

            InitializeFactory(CanvasCategory.Content, map => new UntiledCanvas(map, UntiledProvider) {MaxRequestSize = MaxRequestSize, MinLevel = MinLevel});
        }
        #endregion

        /// <inheritdoc/>
        public override void AddToMapView(MapView mapView)
        {
            if (mapView != null && mapView.Name == "Map")
            {
                map = mapView;
                map = map.FindAncestor<WpfMap>();

                mapView.ViewportBeginChanged += ViewportBeginChanged;
            }

            var xMapTiledProvider = UntiledProvider as IObjectInfoProvider;
            if (xMapTiledProvider != null)
                xMapTiledProvider.MapUdpate += UdpateOjectInfos;

            base.AddToMapView(mapView);
        }

        /// <inheritdoc/>
        public override void RemoveFromMapView(MapView mapView)
        {
            if (mapView != null && mapView.Name == "Map")
            {
                map = mapView;
                map = map.FindAncestor<WpfMap>();

                mapView.ViewportBeginChanged -= ViewportBeginChanged;
            }

            base.RemoveFromMapView(mapView);
        }

        private void ViewportBeginChanged(object o, EventArgs e)
        {
            objectInfos = null;
        }

        /// <summary>Object infos needed for providing texts for tool tips are stored when request are sent to xMap.
        /// See comments on <see cref="Ptv.XServer.Controls.Map.TileProviders.MapUpdateDelegate"/>.
        /// </summary>
        /// <param name="xServerMap">Map object of xServer</param>
        /// <param name="requestedSize">Requested image size.</param>
        public void UdpateOjectInfos(xserver.Map xServerMap, Size requestedSize)
        {
            if (map == null) return;

            map.Dispatcher.Invoke((Action)(() =>
            {
                objectInfos = xServerMap != null ? xServerMap.wrappedObjects : null;
                imageSize = requestedSize;
            }), DispatcherPriority.Send, null);
        }

        /// <summary> Gets a method for returning a tool tip for a LayerObject. </summary>
        /// <returns>Method for returning a tool tip string.</returns>
        /// <remarks>The default implementation returns the description of the layer object (as is).</remarks>
        public Func<xserver.LayerObject, string> GetToolTipFromLayerObject = layerObject =>
        {
            var result = new StringBuilder(4);
            var message = string.Empty;

            var subStrings = layerObject.descr.Split(new [] {'|'});
            foreach (var subString in subStrings)
            {
                var keyValue = subString.Split(new[] {'='});
                var key = keyValue[0].Trim().ToUpper();
                var value = keyValue[1].Trim();
                switch (key)
                {
                    // Traffic incidents 
                    case "ABSOLUTESPEED": 
                        var s = Convert.ToInt16(value);
                        if (s > 0)
                            result.AppendWithSeparator(string.Format(MapLocalizer.GetString(MapStringId.ToolTipTrafficIncidentsAbsoluteSpeed), s), Environment.NewLine); 
                        break;
                    case "MESSAGE": message = value; break;
                    case "LENGTH": result.AppendWithSeparator(string.Format(MapLocalizer.GetString(MapStringId.ToolTipTrafficIncidentsLength), Convert.ToDouble(value) / 1000), Environment.NewLine); break;
                    case "DELAY": result.AppendWithSeparator(string.Format(MapLocalizer.GetString(MapStringId.ToolTipTrafficIncidentsDelay), Math.Round(Convert.ToDouble(value) / 60)), Environment.NewLine); break;

                    // Truck attributes
                    case "TOTALPERMITTEDWEIGHT": result.AppendWithSeparator(string.Format(MapLocalizer.GetString(MapStringId.ToolTipTruckAttributesPermittedTotalWeight), Convert.ToDouble(value)), Environment.NewLine); break;
                    case "LOADTYPE":
                        switch (value)
                        {
                            case "0": value = MapLocalizer.GetString(MapStringId.ToolTipTruckAttributesLoadTypePassenger); break;
                            case "1": value = MapLocalizer.GetString(MapStringId.ToolTipTruckAttributesLoadTypeGoods); break;
                            default: value = MapLocalizer.GetString(MapStringId.ToolTipTruckAttributesLoadTypeMixed); break;
                        }
                        result.AppendWithSeparator(string.Format(MapLocalizer.GetString(MapStringId.ToolTipTruckAttributesLoadType), value), Environment.NewLine); 
                        break;
                    case "MAXHEIGHT": result.AppendWithSeparator(string.Format(MapLocalizer.GetString(MapStringId.ToolTipTruckAttributesMaxHeight), Convert.ToDouble(value) / 100), Environment.NewLine); break;
                    case "MAXWEIGHT": result.AppendWithSeparator(string.Format(MapLocalizer.GetString(MapStringId.ToolTipTruckAttributesMaxWeight), Convert.ToDouble(value)), Environment.NewLine); break;
                    case "MAXWIDTH": result.AppendWithSeparator(string.Format(MapLocalizer.GetString(MapStringId.ToolTipTruckAttributesMaxWidth), Convert.ToDouble(value) / 100), Environment.NewLine); break;
                    case "MAXLENGTH": result.AppendWithSeparator(string.Format(MapLocalizer.GetString(MapStringId.ToolTipTruckAttributesMaxLength), Convert.ToDouble(value) / 100), Environment.NewLine); break;
                    case "MAXAXLELOAD": result.AppendWithSeparator(string.Format(MapLocalizer.GetString(MapStringId.ToolTipTruckAttributesMaxAxleLoad), Convert.ToDouble(value)), Environment.NewLine); break;
                    case "HAZARDOUSTOWATERS": result.AppendWithSeparator(MapLocalizer.GetString(MapStringId.ToolTipTruckAttributesHazardousToWaters), Environment.NewLine); break;
                    case "HAZARDOUSGOODS": result.AppendWithSeparator(MapLocalizer.GetString(MapStringId.ToolTipTruckAttributesHazardousGoods), Environment.NewLine); break;
                    case "COMBUSTIBLES": result.AppendWithSeparator(MapLocalizer.GetString(MapStringId.ToolTipTruckAttributesCombustibles), Environment.NewLine); break;
                    case "FREEFORDELIVERY": result.AppendWithSeparator(MapLocalizer.GetString(MapStringId.ToolTipTruckAttributesFreeForDelivery), Environment.NewLine); break;
                    case "TUNNELRESTRICTION": result.AppendWithSeparator(string.Format(MapLocalizer.GetString(MapStringId.ToolTipTruckAttributesTunnelRestriction), value), Environment.NewLine); break;
                }
            }

            if (string.IsNullOrEmpty(message)) return result.ToString();

            var startText = MapLocalizer.GetString(MapStringId.ToolTipTrafficIncidentsMessage);
            result.AppendWithSeparator(startText + BreakIntoLines(message, startText.Length, 50), Environment.NewLine);
            
            return result.ToString();
        };

        private static string BreakIntoLines(string text, int charsInLine, int maxLength)
        {
            var result = new StringBuilder();
            var words = text.Split(null);
            foreach (var word in words)
            {
                if (charsInLine + word.Length + 1 <= maxLength)
                { // This word fits into line
                    result.Append(' ');
                    result.Append(word);
                    charsInLine += (1 + word.Length);
                }
                else
                { // This word does not fit, insert it into the next line.
                    result.Append(Environment.NewLine);
                    result.Append("  "); // Indent
                    result.Append(word);
                    charsInLine = 2 + word.Length;
                }
            }

            return result.ToString();
        }

        /// <summary> Determines the tool tip texts for a given position </summary>
        /// <param name="center">Position to get the tool tips for.</param>
        /// <param name="maxPixelDistance">Maximal distance from the specified position to get the tool tips for.</param>
        /// <returns>Tool tip texts.</returns>
        public IEnumerable<string> Get(Point center, double maxPixelDistance)
        {
            if (map == null || objectInfos == null || objectInfos.Length <= 0) yield break;

            center = new Point(center.X * imageSize.Width / map.ActualWidth, center.Y * imageSize.Height / map.ActualHeight);
            foreach (var description in ToolTipHitTest(objectInfos, center, maxPixelDistance)
                                             .Select(layerObject => (GetToolTipFromLayerObject != null) ? GetToolTipFromLayerObject(layerObject) : layerObject.descr)
                                             .Where(description => !String.IsNullOrEmpty(description))) 
                                             { yield return description; }
        }

        /// <summary> Hit tests the given layer objects.  </summary>
        /// <param name="infos">Object information to hit test.</param>
        /// <param name="center">Point to test</param>
        /// <param name="maxPixelDistance">Maximal distance from the specified position to get the tool tips for.</param>
        /// <returns>Matching layer objects.</returns>
        protected virtual IEnumerable<xserver.LayerObject> ToolTipHitTest(IEnumerable<xserver.ObjectInfos> infos, Point center, double maxPixelDistance) 
        {
            return infos.Where(info => (info.wrappedObjects != null) && (info.wrappedObjects.Length > 0))
                        .SelectMany(info => info.wrappedObjects.Where(xServerObject => (center - new Point(xServerObject.pixel.x, xServerObject.pixel.y)).Length <= maxPixelDistance));
        }
    }

    /// <summary> A canvas which renders a non-tiled bitmap. </summary>
    public class UntiledCanvas : WorldCanvas
    {
        #region private variables
        /// <summary> Maximum bounding box (left bound) for PTV_Mercator. </summary>
        private const int EnvMinX = -20015087;

        /// <summary> Maximum bounding box (right bound) for PTV_Mercator. </summary>
        private const int EnvMaxX = 20015087;

        /// <summary> Maximum bounding box (bottom bound) for PTV_Mercator. </summary>
        private const int EnvMinY = -20015087;

        /// <summary> Maximum bounding box (top bound) for PTV_Mercator. </summary>
        private const int EnvMaxY = 20015087;

        /// <summary> Provider which delivers the requested map images by an untiled bitmap. </summary>
        private readonly IUntiledProvider untiledProvider;

        /// <summary> The background worker instance for fetching the tiles. </summary>
        private BackgroundWorker worker;

        /// <summary> The last map parameters used. </summary>
        private MapParam lastParam;

        /// <summary> The last zoom level used. </summary>
        private double lastZoom;

        /// <summary> Image containing the complete map content. </summary>
        private readonly Image mapImage;

        /// <summary> The timer for the update delay. </summary>
        private Timer timer;

        /// <summary> Delay for updating the overlay. </summary>
        private const int updateDelay = 150;

        private int index = 0;

        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="UntiledCanvas"/> class. This method
        /// initializes the maximum request size for map bitmaps to 2048 x 2048 pixels. </summary>
        /// <param name="mapView"> The parent map instance. </param>
        /// <param name="untiledProvider"> The instance of the provider, which delivers untiled bitmaps. </param>
        public UntiledCanvas(MapView mapView, IUntiledProvider untiledProvider)
            : this(mapView, untiledProvider, true)
        {
            MaxRequestSize = new Size(2048, 2048);
            MinLevel = 0;
        }

        /// <summary> Initializes a new instance of the <see cref="UntiledCanvas"/> class. If the parameter
        /// <paramref name="addToMap"/> is set to true, the new canvas instance is added to the parent map. </summary>
        /// <param name="mapView"> The parent map instance. </param>
        /// <param name="untiledProvider"> The instance of the provider delivering bitmaps of a map. </param>
        /// <param name="addToMap"> Indicates that the map should be inserted to the parent map initially. </param>
        public UntiledCanvas(MapView mapView, IUntiledProvider untiledProvider, bool addToMap)
            : base(mapView, addToMap)
        {
            this.untiledProvider = untiledProvider;
            mapImage = new Image {IsHitTestVisible = false};

            Children.Add(mapImage);
        }
        #endregion

        #region properties
        /// <summary> Gets or sets the maximum map bitmap request size in pixels. Default is 2048x2048. </summary>
        /// <value>  Maximum map request size in pixels. </value>
        public Size MaxRequestSize { get; set; }

        /// <summary> Gets or sets the maximum leve up to which images are requested. </summary>
        public double MinLevel { get; set; }
        
        #endregion

        #region disposal
        /// <inheritdoc/>
        public override void Dispose()
        {
            if (worker != null)
            {
                worker.CancelAsync();
                worker.DoWork -= Worker_DoWork;
            }

            Children.Remove(mapImage);
            base.Dispose();
        }
        #endregion

        #region public methods
        /// <inheritdoc/>  
        public override void InitializeTransform()
        {
            RenderTransform = TransformFactory.CreateTransform(SpatialReference.PtvMercatorInvertedY);
        }

        /// <inheritdoc/>  
        public override void Update(UpdateMode updateMode)
        {
            switch (updateMode)
            {
                case UpdateMode.Refresh: UpdateOverlay(true); break;
                case UpdateMode.BeginTransition:
                    if (updateDelay == 0 || MapView.Printing)
                    {
                        UpdateOverlay(false);
                    }
                    else
                    {
                        if (timer == null)
                            timer = new Timer(InvokeUpdate);

                        timer.Change(updateDelay, 0);
                    }

                    break;
                case UpdateMode.WhileTransition:
                    // fade out for big scale differences
                    if (mapImage != null && lastZoom != 0)
                        mapImage.Opacity = 1.0 - Math.Min(1, .25 * Math.Abs(MapView.CurrentZoom - lastZoom));

                    break;
                case UpdateMode.EndTransition:
                    if (GlobalOptions.InfiniteZoom && mapImage != null & mapImage.Tag != null)
                    {
                        var mapParam = (MapParam)mapImage.Tag;
                        SetLeft(mapImage, mapParam.Left + MapView.OriginOffset.X);
                        SetTop(mapImage, -mapParam.Bottom + MapView.OriginOffset.Y);
                    }

                    break;
            }
        }
        #endregion

        #region private methods
        /// <summary> Get the map parameters object for the current viewport. </summary>
        /// <returns> The map parameters instance. </returns>
        private MapParam GetMapParam()
        {
            MapRectangle rect = MapView.FinalEnvelope;
            var mapParam = new MapParam(rect.West, rect.South, rect.East, rect.North, MapView.ActualWidth, MapView.ActualHeight);

            if (mapParam.Width == 0 || mapParam.Height == 0)
                return mapParam;

            // clip the rectangle to the maximum rectangle
            if (mapParam.Left < EnvMinX || mapParam.Right > EnvMaxX || mapParam.Top < EnvMinY || mapParam.Bottom > EnvMaxY)
            {
                double leftClipped = Math.Max(EnvMinX, mapParam.Left);
                double rightClipped = Math.Min(EnvMaxX, mapParam.Right);
                double topClipped = Math.Max(EnvMinY, mapParam.Top);
                double bottomClipped = Math.Min(EnvMaxY, mapParam.Bottom);

                double widthClipped = mapParam.Width * (rightClipped - leftClipped) / (mapParam.Right - mapParam.Left);
                double heightClipped = mapParam.Height * (bottomClipped - topClipped) / (mapParam.Bottom - mapParam.Top);

                mapParam = new MapParam(leftClipped, topClipped, rightClipped, bottomClipped, widthClipped, heightClipped);
            }

            // resize if > MaxSize
            if ((mapParam.Width <= MaxRequestSize.Width) && (mapParam.Height <= MaxRequestSize.Height)) return mapParam;

            double ratio = Math.Min(MaxRequestSize.Height / mapParam.Height, MaxRequestSize.Width / mapParam.Width);
            mapParam.Width *= ratio;
            mapParam.Height *= ratio;

            return mapParam;
        }

        /// <summary>
        /// Loads an map image for the specified map parameters.
        /// </summary>
        /// <param name="mapParam">Map parameters (map section and image size)</param>
        /// <returns>The bytes of the encoded map image.</returns>
        private byte[] GetImageBytes(MapParam mapParam)
        {
            MapParam reqParam = mapParam.Scale(untiledProvider as ITiledProvider);

            if (!reqParam.IsSizeInRange(new Size(32, 32), MaxRequestSize))
                return null;
            
            using (var stream = untiledProvider.GetImageStream(reqParam.Left, reqParam.Top, reqParam.Right, reqParam.Bottom, (int)reqParam.Width, (int)reqParam.Height))
            {
                if (stream == null) return null;
                if (mapParam == reqParam) return stream.GetBytes();

                // mapParam != reqParam ==> this is the case if image has been 
                // scaled through mapParam.Scale call above. Must resize image.
                using (var stmResized = stream.ResizeImage((int)mapParam.Width, (int)mapParam.Height))
                    return stmResized.GetBytes();
            }
        }

        /// <summary> Event handler which is called when the worker starts its work. Loads and displays the map image. </summary>
        /// <param name="sender"> Sender of the DoWork event. </param>
        /// <param name="e"> Event parameters. </param>
        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                var mapParam = (MapParam)e.Argument;
                byte[] imageBytes = GetImageBytes(mapParam);

                Dispatcher.BeginInvoke(new Action<byte[], MapParam>(DisplayImage), imageBytes, mapParam);
            }
            catch (Exception) { } // ignore
        }

        /// <summary> Display the loaded image. </summary>
        /// <param name="buffer"> The byte array containing the image data. </param>
        /// <param name="mapParam"> The corresponding map parameters object. </param>
        private void DisplayImage(byte[] buffer, MapParam mapParam)
        {
            // map viewport changed already
            if (mapParam.Index != this.index)
            {
                return;
            }

            lastZoom = MapView.FinalZoom;
            mapImage.Width = mapParam.Right - mapParam.Left;
            mapImage.Height = mapParam.Bottom - mapParam.Top;
            mapImage.Opacity = 1;
            SetLeft(mapImage, mapParam.Left + MapView.OriginOffset.X);
            SetTop(mapImage, -mapParam.Bottom + MapView.OriginOffset.Y);
            mapImage.Tag = mapParam;

            if (buffer == null)
            {
                mapImage.Source = null;
                return;
            }

            using (var stream = new MemoryStream(buffer))
            using (var wrapper = new WrappingStream(stream))
            {
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = wrapper;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                mapImage.Source = bitmapImage;
            }
        }

        /// <summary> Updates the overlay image. </summary>
        /// <param name="forceUpdate"> True, if the update should be forced even if the viewport didn't change. </param>
        private void UpdateOverlay(bool forceUpdate)
        {
            if (timer != null)
            {
                timer.Dispose();
                timer = null;
            }

            var mapParam = GetMapParam();
            if (!forceUpdate && (mapParam == lastParam)) return;

            lastParam = mapParam;
            mapParam.Index = ++this.index;

            if (worker != null)
            {
                worker.CancelAsync();
                worker.DoWork -= Worker_DoWork;
                worker = null;
            }

            if (mapParam.Width < 32 || mapParam.Height < 32 || MinLevel > MapView.FinalZoom )
            {
                mapImage.Source = null; 
                return;
            }

            if (!MapView.Printing)
            {
                worker = new BackgroundWorker();
                worker.DoWork += Worker_DoWork;
                worker.WorkerSupportsCancellation = true;

                worker.RunWorkerAsync(mapParam);
            }
            else
            {
                try { DisplayImage(GetImageBytes(mapParam), mapParam); }
                catch { } // ignore
            }
        }

        /// <summary> This method call the update message using an invoke. </summary>
        /// <param name="stateInfo"> The object containing the corresponding stateInfo object. </param>
        private void InvokeUpdate(object stateInfo)
        {
            Dispatcher.BeginInvoke(new Action<bool>(UpdateOverlay), false);
        }
        #endregion

        #region private struct MapParam
        /// <summary> Map parameters. The parameters describe the dimensions of the displayed map image. </summary>
        private struct MapParam : IEquatable<MapParam>
        {
            /// <summary> Left border of the map display region. </summary>
            public readonly double Left;

            /// <summary> Top border of the map display region. </summary>
            public readonly double Top;

            /// <summary> Right border of the map display region. </summary>
            public readonly double Right;

            /// <summary> Bottom border of the map display region. </summary>
            public readonly double Bottom;

            /// <summary> Width of the map display region. </summary>
            public double Width;

            /// <summary> Height of the map display region. </summary>
            public double Height;

            public int Index;

            /// <summary> Initializes a new instance of the <see cref="MapParam"/> struct. </summary>
            /// <param name="left"> Left border of the map display region. </param>
            /// <param name="top"> Top border of the map display region. </param>
            /// <param name="right"> Right border of the map display region. </param>
            /// <param name="bottom"> Bottom border of the map display region. </param>
            /// <param name="width"> Width of the map display region. </param>
            /// <param name="height"> Height of the map display region. </param>
            public MapParam(double left, double top, double right, double bottom, double width, double height)
            {
                Left = left;
                Top = top;
                Right = right;
                Bottom = bottom;
                Width = width;
                Height = height;
                Index = 0;
            }

            #region IEquatable<MapParam> Members
            // add this code to class ThreeDPoint as defined previously

            /// <summary> Compares two map parameters and returns a boolean value showing if the map parameters are
            /// equal. </summary>
            /// <param name="a"> First map parameter to compare. </param>
            /// <param name="b"> Second map parameter to compare. </param>
            /// <returns> Boolean value showing if the map parameters are equal. </returns>
            public static bool operator ==(MapParam a, MapParam b)
            {
                // Return true if the fields match:
                return
                    a.Left == b.Left && a.Top == b.Top &&
                    a.Right == b.Right && a.Bottom == b.Bottom &&
                    a.Width == b.Width && a.Height == b.Height;
            }

            /// <summary> Compares two map parameters and returns a boolean value showing if the map parameters are not
            /// equal. </summary>
            /// <param name="a"> First map parameter to compare. </param>
            /// <param name="b"> Second map parameter to compare. </param>
            /// <returns> Boolean value showing if the map parameters are not equal. </returns>
            public static bool operator !=(MapParam a, MapParam b)
            {
                return !(a == b);
            }

            /// <summary> Compares the current map parameter to another map parameter and returns a boolean value
            /// showing if the map parameters are equal. </summary>
            /// <param name="other"> Map parameter to compare with the current map parameter. </param>
            /// <returns> A boolean value showing if the map parameters are equal. </returns>
            public bool Equals(MapParam other)
            {
                return this == other;
            }
            #endregion

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                return Left.GetHashCode() + Top.GetHashCode() + Right.GetHashCode() + Bottom.GetHashCode() + Width.GetHashCode() + Height.GetHashCode();
            }

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                return Equals((MapParam)obj);
            }

            /// <summary>
            /// Scales width and height to conform with the minimum and maximum zoom value specified by the given provider.
            /// </summary>
            /// <param name="provider">Provider specifying the minimum and maximum zoom value.</param>
            /// <returns>New MapParam instance with scaled width and height.</returns>
            public MapParam Scale(ITiledProvider provider)
            {
                if (provider == null)
                    return this;

                Func<double, double> scaleFromZoom = zoom => Math.Pow(2, zoom + 7) / (6371000.0 * Math.PI);

                double minScale = scaleFromZoom(provider.MinZoom);
                double maxScale = scaleFromZoom(provider.MaxZoom);

                double scale = Width / (Right - Left);

                double? f = null;

                if (scale - minScale < -1e-4)
                    f = minScale / scale;
                else if (scale - maxScale > 1e-4)
                    f = maxScale / scale;

                return !f.HasValue ? this : new MapParam(Left, Top, Right, Bottom, Width * f.Value, Height * f.Value);
            }

            /// <summary>
            /// Checks if Width and Height are in the specified range.
            /// </summary>
            /// <param name="minSize">Specifies the minimum size.</param>
            /// <param name="maxSize">Specifies the maximum size.</param>
            /// <returns>True, if Width and Height are in range. False otherwise.</returns>
            public bool IsSizeInRange(Size minSize, Size maxSize)
            {
                return (Width >= minSize.Width && Height >= minSize.Height && Width <= maxSize.Width && Height <= maxSize.Height);
            }
        }
        #endregion
    }
}
