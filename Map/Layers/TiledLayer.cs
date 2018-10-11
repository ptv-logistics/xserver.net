// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

using Ptv.XServer.Controls.Map.Canvases;
using Ptv.XServer.Controls.Map.Tools;
using Ptv.XServer.Controls.Map.Tools.Reprojection;


namespace Ptv.XServer.Controls.Map.Layers.Tiled
{
    /// <summary>
    /// A layer which renders maps by using tiled bitmaps.
    /// </summary>
    public class TiledLayer : BaseLayer
    {
        #region public variables

        /// <summary>
        /// Gets or sets a value indicating whether the layer is a base map layer, which means it is part of the
        /// basic map and not additional content.
        /// </summary>
        public bool IsBaseMapLayer
        {
            get => CanvasCategories[0] == CanvasCategory.BaseMap;
            set => CanvasCategories[0] = value ? CanvasCategory.BaseMap : CanvasCategory.Content;
        }

        /// <summary>
        /// Gets or sets a value indicating whether layer tiles use an alpha channel.
        /// If false the layer uses some internal optimizations to reduce rendering artifacts.
        /// </summary>
        public bool IsTransparentLayer { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether layer tiles are transparent labels.
        /// If true the layer uses some internal optimizations to reduce rendering artifacts.
        /// </summary>
        public bool IsLabelLayer { get; set; }

        /// <summary>
        /// Gets or sets the provider for the tiles.
        /// </summary>
        public ITiledProvider TiledProvider { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether dynamic update is enabled.
        /// </summary>
        public bool TransitionUpdates { get; set;  }
        #endregion

        #region constructor
        /// <summary>
        /// Initializes a new instance of the <see cref="TiledLayer"/> class.
        /// </summary>
        /// <param name="name">The unique name of the layer.</param>
        public TiledLayer(string name)
            : base(name)
        {
            TransitionUpdates = true;

            InitializeFactory(
                CanvasCategory.Content,
                map => new TiledCanvas(map, TiledProvider) { IsTransparentLayer = IsTransparentLayer, IsLabelLayer = IsLabelLayer, GetTransitionUpdates = () => TransitionUpdates });
        }
        #endregion
    }

    /// <summary> Canvas which renders tile-based content. </summary>
    public class TiledCanvas : WorldCanvas
    {
        #region private variables
        /// <summary> Reference to the tile provider. </summary>
        private readonly ITiledProvider tiledProvider;

        /// <summary> Reference to the tile cache. </summary>
        private readonly TileCache tileCache = TileCache.GlobalCache;

        /// <summary> The worker thread for fetching the tiles. </summary>
        private BackgroundWorker worker;

        /// <summary> The dictionary for holding the displayed imaged by tile key. </summary>
        private readonly Dictionary<TileParam, Image> shownImages = new Dictionary<TileParam, Image>();

        /// <summary> The thread pool used for fetching the tiles for a viewport. </summary>
        private readonly DevelopMentor.ThreadPool threadPool = new DevelopMentor.ThreadPool(2, 6, Guid.NewGuid().ToString());

        /// <summary> Stores the last zoom factor. Used to detect if a zoom change takes place. </summary>
        private int lastZoom = -1;
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="TiledCanvas"/> class. By default, the instance is
        /// using threading (<see cref="UseThreading"/> = true). </summary>
        /// <param name="mapView"> Parent map object associated to this tile canvas. </param>
        /// <param name="tiledProvider"> Object providing the tiles. </param>
        public TiledCanvas(MapView mapView, ITiledProvider tiledProvider)
            : base(mapView)
        {
            UseThreading = true;
            SnapsToDevicePixels = true;

            this.tiledProvider = tiledProvider;

            // initialize cache for re-usable writable bitmaps

            threadPool.Start();
        }
        #endregion

        #region public properties
        /// <summary> Gets or sets a value indicating whether tiles are labels. </summary>
        /// <value> Flag showing whether tiles are labels. </value>
        public bool IsLabelLayer { get; set; }

        /// <summary> Gets or sets a value indicating whether the tiles of this layer are a transparent overlay of the
        /// map. </summary>
        /// <value> Flag showing whether tiles are transparent overlay. </value>
        public bool IsTransparentLayer { get; set; }

        /// <summary> Gets or sets a value indicating whether threading is used for tile loading. </summary>
        /// <value> Flag indicating whether threading is used for tile loading. </value>
        public bool UseThreading { get; set; }

        /// <summary> Gets or sets an function that returns a value indicating whether dynamic update is used for tile loading. </summary>
        public Func<bool> GetTransitionUpdates { get; set; }
        #endregion

        #region private properties
        /// <summary> Gets a value indicating whether animation is used for map actions. </summary>
        private bool UseAnimation
        {
            get
            {
                var map = MapElementExtensions.FindParent<Map>(this);
                return (map != null) && map.UseAnimation;
            }
        }
        #endregion

        #region disposal
        /// <inheritdoc/>  
        public override void Dispose()
        {
            StopBackgroundWorker();
            threadPool.Stop();

            base.Dispose();
        }
        #endregion

        #region public methods
        /// <inheritdoc/>  
        public override void InitializeTransform()
        {
            var translateTransform = new TranslateTransform(1.0 / MapView.ZoomAdjust * MapView.ReferenceSize / 2, 1.0 / MapView.ZoomAdjust * MapView.ReferenceSize / 2);
            var zoomTransform = new ScaleTransform(MapView.ZoomAdjust, MapView.ZoomAdjust);
            var transformGroup = new TransformGroup();

            transformGroup.Children.Add(translateTransform);
            transformGroup.Children.Add(zoomTransform);

            translateTransform.Freeze();
            zoomTransform.Freeze();
            transformGroup.Freeze();

            RenderTransform = transformGroup;
        }

        private bool TransitionUpdates => (GetTransitionUpdates?.Invoke()).GetValueOrDefault(true);

        /// <inheritdoc/>  
        public override void Update(UpdateMode updateMode)
        {
            switch (updateMode)
            {
                case UpdateMode.Refresh:
                    RemoveAllTiles();
                    OnMapSectionStartChange(updateMode);
                    break;
                case UpdateMode.BeginTransition:
                    OnMapSectionStartChange(updateMode);
                    break;
                case UpdateMode.WhileTransition:
                    WhileMapSectionChange();
                    break;
                case UpdateMode.EndTransition:
                    if (GetTransitionUpdates != null && !GetTransitionUpdates())
                    {
                        RemoveTilesWithDifferentZoom();
                        OnMapSectionStartChange(updateMode);
                    }
                    OnAnimationFinished();
                    break;
            }
        }
        #endregion

        #region private methods
        /// <summary> Start worker thread for retrieving the tiles from the tile provider. </summary>
        private void GetTiles()
        {
            StopBackgroundWorker();

            var mapParam = new MapParam(MapView, GetTileZoom());

            currentlyVisibleTiles = new HashSet<TileParam>(GetVisibleTiles(mapParam));
            currentlyVisibleTiles.ExceptWith(new HashSet<TileParam>(shownImages.Keys));

            if (MapView.Printing)
            {
                GetVisibleTiles(mapParam).ForEach(null, tileParam =>
                {
                    GetImage(tileParam, out var buffer);
                    DisplayImage(buffer, tileParam, false, true);
                    RemoveRestOfTiles();
                });
            }
            else
            {
                worker = new BackgroundWorker();
                worker.DoWork += Worker_DoWork;
                worker.WorkerSupportsCancellation = true;
                worker.RunWorkerAsync(currentlyVisibleTiles);
            }
        }

        private void StopBackgroundWorker()
        {
            if (worker == null) return;
            
            worker.CancelAsync();
            worker.DoWork -= Worker_DoWork;
            worker = null;
        }

        private void RemoveTilesWithDifferentZoom()
        {
            var visibleTiles = GetVisibleTiles(new MapParam(MapView, GetTileZoom()))
                    .ToDictionary<TileParam, TileParam, object>(tile => tile, tile => null);

            var tmpList = new List<TileParam>(shownImages.Keys.Where(imageKey => !visibleTiles.ContainsKey(imageKey)));
            tmpList.ForEach(null, RemoveImage);
        }

        /// <summary> Just as the name says: Remove all visible tiles. </summary>
        private void RemoveInvisibleTiles()
        {
            if (!MapView.IsAnimating)
            {
                RemoveRestOfTiles();
                return;
            }

            if (IsLabelLayer)
            {
                RemoveTilesWithDifferentZoom();
                return;
            }

            // remove all images at deeper level that contains the current level
            var tileZoom = GetTileZoom();
            var tmpList = new List<TileParam>();
            foreach (var imageKey in shownImages.Keys)
            {
                if (imageKey.Zoom <= tileZoom) continue;
                int dx = imageKey.TileX / (imageKey.Zoom - tileZoom + 1);
                int dy = imageKey.TileY / (imageKey.Zoom - tileZoom + 1);
                if (ContainsShownImagesTransparentImage(new TileParam(dx, dy, tileZoom, tiledProvider.CacheId)))
                {
                    tmpList.Add(imageKey);
                }
            }

            tmpList.ForEach(null, RemoveImage);
        }

        private bool ContainsShownImagesTransparentImage(TileParam tileParam) => shownImages.ContainsKey(tileParam) && IsImageTransparent(shownImages[tileParam]);
        private bool IsImageTransparent(Image image) => Math.Abs(image.Opacity - 1) < 0.00001;

        private void RemoveAllTiles()
        {
            shownImages.Keys.ToList().ForEach(null, RemoveImage);
        }

        private void RemoveRestOfTiles()
        {
            var visibleTileKeys = GetVisibleTiles(new MapParam(MapView, GetTileZoom())).ToDictionary<TileParam, TileParam, object>(imageKey => imageKey, imageKey => null);

            if (visibleTileKeys.Keys.Any(key => !ContainsShownImagesTransparentImage(key)))
                return;

            var tmpList = shownImages.Keys.Where(key => !visibleTileKeys.ContainsKey(key)).ToList();
            tmpList.ForEach(null, RemoveImage);

            shownImages.Values.ForEach(null, image => SetZIndex(image, ((TileParam)image.Tag).Zoom));
        }

        private void RemoveImage(TileParam key)
        {
            var image = shownImages[key];
            ((Canvas)image.Parent).Children.Remove(image);
            shownImages.Remove(key);
        }

        private HashSet<TileParam> currentlyVisibleTiles;

        /// <summary> Adopt the opacity of tiles whiled the map section is changing. </summary>
        private void WhileMapSectionChange()
        {

            // if (Map.ZoomF < this.tiledProvider.MinZoom)
            // {
            //     double opc = Map.ZoomF - this.tiledProvider.MinZoom + 1;
            //     if (opc < 0)
            //         opc = 0;
            //     if (opc > 1)
            //         opc = 1;
            //     this.Opacity = opc;
            // }
            // else
            //     this.Opacity = oldOpacity;
        }

        /// <summary>
        /// Refreshes the tiles which are shown on the canvas. This method is designed to be called at the start of a map section change.
        /// </summary>
        /// <param name="updateMode"> The update mode. This mode tells which kind of change is to be processed by the update call. </param>
        private void OnMapSectionStartChange(UpdateMode updateMode)
        {
            if (((updateMode != UpdateMode.BeginTransition) && (updateMode != UpdateMode.EndTransition)) || TransitionUpdates)
            {
                if (MapView.FinalZoom < tiledProvider.MinZoom - 1)
                {
                    RemoveAllTiles();
                    return;
                }

                GetTiles();
            }
        }

        /// <summary>
        /// Removes all tiles from the canvas. This method is designed to be called when an animation has finished.
        /// </summary>
        private void OnAnimationFinished()
        {
            RemoveRestOfTiles();

            if (GlobalOptions.InfiniteZoom)
            {
                double factor = (tiledProvider as ITilingOptions)?.Factor ?? 1;

                foreach (var i in shownImages)
                {
                    var key = i.Key;
                    var image = i.Value;

                    double size = MapView.ReferenceSize / factor / (1 << key.Zoom);

                    var rx = MapView.OriginOffset.X / MapView.LogicalSize * MapView.ReferenceSize;
                    var ry = MapView.OriginOffset.Y / MapView.LogicalSize * MapView.ReferenceSize;

                    SetLeft(image, rx + key.TileX * size - ((image.Width - size) / 2) - MapView.ReferenceSize / factor / 2);
                    SetTop(image, ry + key.TileY * size - ((image.Height - size) / 2) - MapView.ReferenceSize / factor / 2);
                }
            }
        }

        /// <summary>
        /// Retrieves the current zoom level. The values reside between <see cref="ITiledProvider.MinZoom"/> and <see cref="ITiledProvider.MaxZoom"/> of the tiled provider.
        /// </summary>
        /// <returns> The current zoom level. </returns>
        private int GetTileZoom()
        {
            var zoom = (int)Math.Round(MapView.FinalZoom - .25, MidpointRounding.AwayFromZero);
            return Math.Min(tiledProvider.MaxZoom, Math.Max(tiledProvider.MinZoom, zoom));
        }

        /// <summary>
        /// Retrieves a list of currently visible tiles on the canvas.
        /// </summary>
        /// <param name="mapParam"> The map on which the canvas is painted. </param>
        /// <returns> The currently visible tiles. </returns>
        private IEnumerable<TileParam> GetVisibleTiles(MapParam mapParam)
        {
            var result = new List<TileParam>();

            int numTiles = 1 << mapParam.TileZoom; // number of tiles for a zoom level    
            double tileDif = mapParam.TileZoom - mapParam.MapZoom;
            double tileFact = Math.Pow(2, tileDif);
            double factor = (tiledProvider as ITilingOptions)?.Factor ?? 1;
            double logicalSize = MapView.LogicalSize / factor;

            // calculate the left upper tiles
            var p0x = (int)((mapParam.MapX + (logicalSize / 2)) * (numTiles / logicalSize) - tileFact * mapParam.WpfWidth / 512);
            var p0y = (int)(((logicalSize / 2) - mapParam.MapY) * (numTiles / logicalSize) - tileFact * mapParam.WpfHeight / 512);
            var p1x = (int)((mapParam.MapX + (logicalSize / 2)) * (numTiles / logicalSize) + tileFact * mapParam.WpfWidth / 512);
            var p1y = (int)(((logicalSize / 2) - mapParam.MapY) * (numTiles / logicalSize) + tileFact * mapParam.WpfHeight / 512);

            int numTilesX = p1x - p0x + 1;
            int numTilesY = p1y - p0y + 1;

            // this pattern generates the tile indexes starting from the center
            for (int k = 0; k < numTilesX; k++)
            {
                int i = (numTilesX / 2) + ((k % 2 == 0) ? (k / 2) : (-k / 2) - 1);
                int tx = p0x + i;
                if (tx < 0 || tx >= numTiles)
                    continue;

                for (int l = 0; l < numTilesY; l++)
                {
                    int j = (numTilesY / 2) + ((l % 2 == 0) ? (l / 2) : (-l / 2) - 1);
                    int ty = p0y + j;
                    if (ty < 0 || ty >= numTiles)
                        continue;

                    result.Add(new TileParam(tx, ty, mapParam.TileZoom, tiledProvider.CacheId));
                }
            }

            return result;
        }

        /// <summary>
        /// Event handler which is called when the worker starts its work. Loads the tile images.
        /// </summary>
        /// <param name="sender"> Sender of the DoWork event. </param>
        /// <param name="e"> Event parameters. </param>
        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            if (!(e.Argument is IEnumerable<TileParam> tileParams)) return;

            foreach (var tileParam in tileParams)
            {
                if (((BackgroundWorker)sender).CancellationPending)
                {
                    e.Result = null;
                    return;
                }

                if (UseThreading)
                {
                    // new Thread(new ParameterizedThreadStart(LoadImage)).Start(tile);
                    // ThreadPool.QueueUserWorkItem(new WaitCallback(LoadImage), tile);
                    threadPool.PostRequest(new Action<TileParam>(LoadImage), new object[] { tileParam });
                }
                else
                {
                    LoadImage(tileParam); // single threaded for debugging
                }
            }
        }

        /// <summary>
        /// Displays the image of a certain tile on the canvas.
        /// </summary>
        /// <param name="buffer"> The bytes for holding the image. </param>
        /// <param name="tile"> The tile parameters object. </param>
        /// <param name="useAnimation"> Flag indicating if the bitmap is to be shown with an animation or without. </param>
        /// <param name="force"> Flag indicating the tile is displayed even it is not part of the current zoom level. </param>
        private void DisplayImage(byte[] buffer, TileParam tile, bool useAnimation, bool force)
        {
            try
            {
                if (!(force || currentlyVisibleTiles.Contains(tile)))
                {
                    return;
                }

                var imageKey = tile;
                if (shownImages.ContainsKey(imageKey))
                {
                    return;
                }

                double factor = (tiledProvider as ITilingOptions)?.Factor ?? 1;
                double overlapFactor = (tiledProvider as ITilingOptions)?.OverlapFactor ?? 0;

                var image = new Image();
                double size = MapView.ReferenceSize / factor / (1 << tile.Zoom);

                // inflate size so tiles overlap a bit
                if (Math.Abs(overlapFactor) < 0.00001)
                {
                    image.Width = size + ((!IsTransparentLayer && !GlobalOptions.InfiniteZoom) ? size * 1.0 / 512 : 0);
                    image.Height = size + ((!IsTransparentLayer && !GlobalOptions.InfiniteZoom) ? size * 1.0 / 512 : 0);
                }
                else
                {
                    image.Width = size + (size * overlapFactor); // ;
                    image.Height = size + (size * overlapFactor); // +;
                }

                var rx = MapView.OriginOffset.X / MapView.LogicalSize * MapView.ReferenceSize;
                var ry = MapView.OriginOffset.Y / MapView.LogicalSize * MapView.ReferenceSize;

                SetLeft(image, rx + tile.TileX * size - ((image.Width - size) / 2) - (MapView.ReferenceSize / factor / 2));
                SetTop(image, ry + tile.TileY * size - ((image.Height - size) / 2) - (MapView.ReferenceSize / factor / 2));

                using (var stream = new MemoryStream(buffer))
                using (var wrapper = new WrappingStream(stream))
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.StreamSource = wrapper;
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    bmp.Freeze();
                    image.Source = bmp;
                }

                Children.Add(image);
                image.Tag = imageKey;
                SetZIndex(image, useAnimation && !force ? imageKey.Zoom + 32 : imageKey.Zoom);
                if (useAnimation)
                {
                    var animation = new TileDoubleAnimation(0, 1, new Duration(new TimeSpan(0, 0, 0, 0, 250)), FillBehavior.HoldEnd) { Image = image };
                    animation.Completed += Animation_Completed;
                    image.Opacity = 0;
                    animation.Freeze();

                    image.BeginAnimation(OpacityProperty, animation);
                }

                shownImages.Add(imageKey, image);

                if (!useAnimation)
                    RemoveInvisibleTiles();
            }
            finally
            {
                lastZoom = tile.Zoom;
            }
        }

        /// <summary>
        /// Event handler which is called when an animation has completed. Shows the images on the canvas and removes invisible tiles.
        /// </summary>
        /// <param name="sender"> Sender of the completed event. </param>
        /// <param name="e"> Event parameters. </param>
        private void Animation_Completed(object sender, EventArgs e)
        {
            ((AnimationClock)sender).Completed -= Animation_Completed;

            RemoveInvisibleTiles();
        }


        /// <summary> Reads an image from the provider. </summary>
        /// <param name="tileKey"> Tile which is to be read out as an image. </param>
        /// <param name="buffer"> The buffer containing the image data or null if the image was not found. </param>
        /// <returns> True if the image was found in the cache, false otherwise. </returns>
        private bool GetImage(TileParam tileKey, out byte[] buffer)
        {
            // don't use memory-cache if the provider returns an empty CacheId
            var useTileCache = !string.IsNullOrEmpty(tiledProvider.CacheId);
            string cacheKey = tiledProvider.CacheId + tileKey;
            if (useTileCache && tileCache.TryGetValue(cacheKey, out buffer))
                return true;
   
            try
            {
                using (var stream = tiledProvider.GetImageStream(tileKey.TileX, tileKey.TileY, tileKey.Zoom))
                {
                    buffer = stream?.GetBytes(true);

                    if(useTileCache)
                        tileCache.AddValue(cacheKey, buffer);
                }
            }
            catch { buffer = null; } // should handle web exception gracefully here 
            finally { if(useTileCache) tileCache.UnlockKey(cacheKey); }

            return false;
        }
        /// <summary>
        /// Loads an image of a certain tile and shows it on the canvas.
        /// </summary>
        /// <param name="tileParam"> Tile to be shown on the image. </param>
        /// <param name="forceTile"> Flag indicating that the image has to be reloaded even if the content might be the same. </param>
        private void LoadImage(TileParam tileParam, bool forceTile)
        {
            if (!(forceTile || currentlyVisibleTiles.Contains(tileParam)))
            {
                return;
            }

            bool cached = GetImage(tileParam, out var buffer);

            int x = tileParam.TileX;
            int y = tileParam.TileY;
            int zoom = tileParam.Zoom;

            if (buffer == null && zoom > tiledProvider.MinZoom)
            {
                LoadImage(new TileParam(x / 2, y / 2, zoom - 1, tiledProvider.CacheId), true);
            }

            // only animate if the tile is not in the cache or the zoom level changed
            bool animate = (!cached || lastZoom == -1 || lastZoom != zoom) && UseAnimation;

            if (buffer != null)
            {
                Dispatcher.BeginInvoke(new Action<byte[], TileParam, bool, bool>(DisplayImage), buffer, tileParam, animate, forceTile);
            }
        }

        /// <summary>
        /// Loads the image of a certain tile and shows it on the canvas. The tile is entered of type object.
        /// </summary>
        /// <param name="stateInfo"> Tile of type object. </param>
        private void LoadImage(object stateInfo)
        {
            var tileParam = stateInfo as TileParam;
            LoadImage(tileParam, false);
        }
        #endregion

        /// <summary> Class containing all import parameters concerning the WPF-positioning of a map object. </summary>
        private class MapParam
        {
            public MapParam(MapView mapView, int tileZoom)
            {
                WpfWidth = mapView.ActualWidth;
                WpfHeight = mapView.ActualHeight;

                MapX = mapView.FinalX;
                MapY = mapView.FinalY;

                TileZoom = tileZoom;
                MapZoom = mapView.FinalZoom;
            }

            #region public properties
            /// <summary> Gets or sets the width in WPF units.</summary>
            public double WpfWidth { get; set; }

            /// <summary> Gets or sets the height in WPF units.</summary>
            public double WpfHeight { get; set; }

            /// <summary> Gets or sets the x-coordinate in WPF units.</summary>
            public double MapX { get; set; }

            /// <summary> Gets or sets the y-coordinate in WPF units.</summary>
            public double MapY { get; set; }

            /// <summary> Gets or sets the zoom level in the map.</summary>
            public double MapZoom { get; set; }

            /// <summary> Gets or sets the zoom level for tiles.</summary>
            public int TileZoom { get; set; }
            #endregion
        }

        /// <summary> Class containing all information needed for addressing a concrete tile. </summary>
        private class TileParam
        {
            #region constructor
            /// <summary> Initializes a new instance of the <see cref="TileParam"/> class. Constructor, receiving all
            /// needed information for addressing a concrete tile. </summary>
            /// <param name="tileX"> Tile position for according x-coordinate. </param>
            /// <param name="tileY"> Tile position for according y-coordinate. </param>
            /// <param name="zoom"> Zoom level. </param>
            /// <param name="cacheKey"> Cache Key. </param>
            public TileParam(int tileX, int tileY, int zoom, string cacheKey)
            {
                TileX = tileX;
                TileY = tileY;
                Zoom = zoom;
                CacheKey = cacheKey;
            }
            #endregion

            #region public properties
            /// <summary> Gets or sets the unique id of the provider.</summary>

            /// <summary> Gets or sets the x-coordinate of the tile position.</summary>
            public int TileX { get; set; }

            /// <summary> Gets or sets the y-coordinate of the tile position.</summary>
            public int TileY { get; set; }

            /// <summary> Gets or sets the zoom level.</summary>
            public int Zoom { get; set; }

            public string CacheKey { get; set; }
            #endregion

            #region public methods
            /// <inheritdoc/>
            public override string ToString()
            {
                return $"Tx{Zoom}x{TileX}x{TileY}x{CacheKey}";
            }

            #endregion


            public override bool Equals(Object obj)
            {
                return (obj is TileParam p) && (TileX == p.TileX) && (TileY == p.TileY) && (Zoom == p.Zoom) && (CacheKey == p.CacheKey);
            }

            public bool Equals(TileParam p)
            {
                // Return true if the fields match:
                return (p != null) && (TileX == p.TileX) && (TileY == p.TileX) && (Zoom == p.Zoom) && (CacheKey == p.CacheKey);
            }

            public override int GetHashCode()
            {
                return TileX ^ TileY ^ Zoom ^ CacheKey.GetHashCode();
            }
        }
    }

    /// <summary> Returns a bitmap cache for a tile index at a specific zoom level. </summary>
    public interface ITiledProvider
    {
        /// <summary> Returns a serialized bitmapCache. </summary>
        /// <param name="x"> X tile. </param>
        /// <param name="y"> Y tile. </param>
        /// <param name="zoom"> Zoom level. </param>
        /// <returns> The stream containing the map image. </returns>
        Stream GetImageStream(int x, int y, int zoom);

        /// <summary> Gets the cache id used to cache the tiled map. </summary>
        string CacheId { get; }

        /// <summary> Gets the minimum level where tiles are available. </summary>
        int MinZoom { get; }

        /// <summary> Gets the maximum level where tiles are available. </summary>
        int MaxZoom { get; }
    }

    /// <summary> Indicates whether the tile sources use a tiling system which differs by a scale factor from the
    /// standard tiling scheme. This interface is used for xServer tile-sources to avoid rounding errors at deep zoom
    /// levels. </summary>
    public interface ITilingOptions
    {
        /// <summary> Gets or sets the factor relative to the standard tiling scheme. </summary>
        double Factor { get; set; }

        /// <summary> Gets or sets the amount of overlapping area for neighboring tiles. </summary>
        double OverlapFactor { get; set; }
    }

    /// <summary> A helper class which holds the associated image for an animation.  </summary>
    internal class TileDoubleAnimation : DoubleAnimation
    {
        /// <inheritdoc/>  
        public TileDoubleAnimation(double fromValue, double toValue, Duration duration, FillBehavior fillBehavior)
            : base(fromValue, toValue, duration, fillBehavior)
        {
        }

        /// <summary>
        /// The associated image for the animation.
        /// </summary>
        public Image Image { get; set; }
    }
}
