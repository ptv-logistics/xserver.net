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
            set { CanvasCategories[0] = value ? CanvasCategory.BaseMap : CanvasCategory.Content; }
            get { return CanvasCategories[0] == CanvasCategory.BaseMap; }
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

            // initialize cache for re-usable writeable bitmaps

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
            if (worker != null)
            {
                worker.CancelAsync();
                worker.DoWork -= Worker_DoWork;
            }

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
            if (worker != null)
            {
                worker.CancelAsync();
                worker.DoWork -= Worker_DoWork;
                worker = null;
            }

            var mapParam = new MapParam(MapView.ActualWidth, MapView.ActualHeight, MapView.FinalX, MapView.FinalY, GetTileZoom(), MapView.FinalZoom);

            currentlyVisibleTiles = new HashSet<TileParam>(GetVisibleTiles(mapParam));
            currentlyVisibleTiles.ExceptWith(new HashSet<TileParam>(shownImages.Keys));

            if (!MapView.Printing)
            {
                worker = new BackgroundWorker();
                worker.DoWork += Worker_DoWork;
                worker.WorkerSupportsCancellation = true;
                worker.RunWorkerAsync(currentlyVisibleTiles);

                //           RemoveRestOfTiles();
            }
            else
            {
                foreach (TileParam tile in GetVisibleTiles(mapParam))
                {
                    byte[] buffer;
                    GetImage(tile, out buffer);
                    DisplayImage(buffer, tile, !MapView.Printing, true);
                    RemoveRestOfTiles();
                }
            }
        }


        /// <summary> Just as the name says: Remove tiles with a different zoom. </summary>
        private void RemoveTilesWithDifferentZoom()
        {
            var visibleTiles = GetVisibleTiles(new MapParam(MapView.ActualWidth, MapView.ActualHeight, MapView.FinalX, MapView.FinalY, GetTileZoom(), MapView.FinalZoom))
                    .ToDictionary<TileParam, TileParam, object>(tile => tile, tile => null);

            var tmpList = new List<TileParam>(shownImages.Keys.Where(imageKey => !visibleTiles.ContainsKey(imageKey)));

            foreach (var key in tmpList)
                RemoveImage(key);
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
                var tk = new TileParam(dx, dy, tileZoom, tiledProvider.CacheId);
                if (shownImages.ContainsKey(tk) && (Math.Abs(shownImages[tk].Opacity - 1) < 0.00001))
                {
                    tmpList.Add(imageKey);
                }
            }
            foreach (var key in tmpList)
            {
                RemoveImage(key);
            }
        }

        /// <summary> Remove all tiles. </summary>
        private void RemoveAllTiles()
        {
            foreach (var key in shownImages.Keys.ToList())
            {
                RemoveImage(key);
            }
        }

        /// <summary> Remove rest of tiles. </summary>
        private void RemoveRestOfTiles()
        {
            var visibleTileKeys = GetVisibleTiles(new MapParam(MapView.ActualWidth, MapView.ActualHeight, MapView.FinalX, MapView.FinalY, GetTileZoom(), MapView.FinalZoom)).ToDictionary<TileParam, TileParam, object>(imageKey => imageKey, imageKey => null);

            if (visibleTileKeys.Keys.Any(key => !shownImages.ContainsKey(key) || Math.Abs(shownImages[key].Opacity - 1.0) > 0.00001))
            {
                return;
            }

            var tmpList = shownImages.Keys.Where(imageKey => !visibleTileKeys.ContainsKey(imageKey)).ToList();

            foreach (var key in tmpList)
            {
                RemoveImage(key);
            }

            foreach (var image in shownImages.Values)
            {
                SetZIndex(image, ((TileParam)image.Tag).Zoom);
            }
        }

        /// <summary> Remove image specified by a key. </summary>
        /// <param name="key"> String containing the key of the image to remove. </param>
        private void RemoveImage(TileParam key)
        {
            var image = shownImages[key];
            ((Canvas)image.Parent).Children.Remove(image);
            shownImages.Remove(key);
        }

        private HashSet<TileParam> currentlyVisibleTiles;

        /// <summary>
        /// Adopt the opacity of tiles whiled the map sections is changing.
        /// </summary>
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
            if (updateMode != UpdateMode.BeginTransition || TransitionUpdates)
                if (updateMode != UpdateMode.EndTransition || !TransitionUpdates)
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
                double factor = (tiledProvider is ITilingOptions) ? ((ITilingOptions)tiledProvider).Factor : 1;

                foreach (var i in shownImages)
                {
                    var image = i.Value;
                    var key = i.Key;

                    double size = MapView.ReferenceSize / factor / (1 << key.Zoom);

                    var rx = MapView.OriginOffset.X / MapView.LogicalSize * MapView.ReferenceSize;
                    var ry = MapView.OriginOffset.Y / MapView.LogicalSize * MapView.ReferenceSize;

                    SetLeft(image, rx + key.TileX * size - ((image.Width - size) / 2) - (MapView.ReferenceSize / factor / 2));
                    SetTop(image, ry + key.TileY * size - ((image.Height - size) / 2) - (MapView.ReferenceSize / factor / 2));
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

            if (zoom > tiledProvider.MaxZoom)
            {
                return tiledProvider.MaxZoom;
            }

            if (zoom < tiledProvider.MinZoom)
            {
                return tiledProvider.MinZoom;
            }

            return zoom;
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
            double dileFact = Math.Pow(2, tileDif);
            double factor = (tiledProvider is ITilingOptions) ? ((ITilingOptions)tiledProvider).Factor : 1;
            double logicalSize = MapView.LogicalSize / factor;

            // calculate the left upper tiles
            var p0x = (int)(((mapParam.MapX + (logicalSize / 2)) * (numTiles / logicalSize)) - (dileFact * mapParam.WpfWidth / 512));
            var p0y = (int)((((logicalSize / 2) - mapParam.MapY) * (numTiles / logicalSize)) - (dileFact * mapParam.WpfHeight / 512));
            var p1x = (int)(((mapParam.MapX + (logicalSize / 2)) * (numTiles / logicalSize)) + (dileFact * mapParam.WpfWidth / 512));
            var p1y = (int)((((logicalSize / 2) - mapParam.MapY) * (numTiles / logicalSize)) + (dileFact * mapParam.WpfHeight / 512));

            int numTilesX = p1x - p0x + 1;
            int numTilesY = p1y - p0y + 1;

            // this pattern generates the tile indexes starting from the center
            for (int k = 0; k < numTilesX; k++)
            {
                int i = (numTilesX / 2) + ((k % 2 == 0) ? (k / 2) : (-k / 2) - 1);

                for (int l = 0; l < numTilesY; l++)
                {
                    int j = (numTilesY / 2) + ((l % 2 == 0) ? (l / 2) : (-l / 2) - 1);

                    int tx = p0x + i;
                    int ty = p0y + j;

                    if (tx < 0 || ty < 0 || tx >= numTiles || ty >= numTiles)
                    {
                        continue;
                    }

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
            var tiles = e.Argument as IEnumerable<TileParam>;
            if (tiles == null) return;

            foreach (TileParam tile in tiles)
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
                    threadPool.PostRequest(new Action<TileParam>(LoadImage), new object[] { tile });
                }
                else
                {
                    LoadImage(tile); // single threaded for debugging
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
                var imageKey = tile;

                if (!(force || currentlyVisibleTiles.Contains(tile)))
                {
                    return;
                }

                if (shownImages.ContainsKey(imageKey))
                {
                    return;
                }

                double factor = (tiledProvider is ITilingOptions) ? ((ITilingOptions)tiledProvider).Factor : 1;
                double overlapFactor = (tiledProvider is ITilingOptions) ? ((ITilingOptions)tiledProvider).OverlapFactor : 0;

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

                Canvas.SetLeft(image, rx + tile.TileX * size - ((image.Width - size) / 2) - (MapView.ReferenceSize / factor / 2));
                Canvas.SetTop(image, ry + tile.TileY * size - ((image.Height - size) / 2) - (MapView.ReferenceSize / factor / 2));

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

            byte[] buffer;
            bool cached = GetImage(tileParam, out buffer);

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
            #region constructor
            /// <summary> Initializes a new instance of the <see cref="MapParam"/> class. Constructor of the container,
            /// providing important WPF-positioning properties. </summary>
            /// <param name="wpfWidth"> Width in WPF units. </param>
            /// <param name="wpfHeight"> Height in WPF units. </param>
            /// <param name="mapX"> X-coordinate in WPF units. </param>
            /// <param name="mapY"> Y-coordinate in WPF units. </param>
            /// <param name="tileZoom"> Zoom level in the map. </param>
            /// <param name="mapZoom"> Zoom level for tiles. </param>
            public MapParam(double wpfWidth, double wpfHeight, double mapX, double mapY, int tileZoom, double mapZoom)
            {
                WpfWidth = wpfWidth;
                WpfHeight = wpfHeight;

                MapX = mapX;
                MapY = mapY;

                TileZoom = tileZoom;
                MapZoom = mapZoom;
            }
            #endregion

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
                return string.Format("Tx{0}x{1}x{2}x{3}", Zoom, TileX, TileY, CacheKey);
            }

            #endregion


            public override bool Equals(Object obj)
            {
                // If parameter is null return false.
                if (obj == null) return false;

                // If parameter cannot be cast to Point return false.
                var p = obj as TileParam;
                if (p == null) return false;

                // Return true if the fields match:
                return (TileX == p.TileX) && (TileY == p.TileY) && (Zoom == p.Zoom) && (CacheKey == p.CacheKey);
            }

            public bool Equals(TileParam p)
            {
                // If parameter is null return false:
                if (p == null) return false;

                // Return true if the fields match:
                return (TileX == p.TileX) && (TileY == p.TileX) && (Zoom == p.Zoom) && (CacheKey == p.CacheKey);
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
