namespace Ptv.XServer.Controls.Map.Tools
{
    /// <summary> Class for caching the tile images. </summary>
    public class TileCache : MemoryCacheMultiThreaded
    {
        #region public variables
        /// <summary> The global instance of the tile cache. </summary>
        public static TileCache GlobalCache = new TileCache();
        #endregion

        #region constructor
        /// <summary> Initializes a new instance of the <see cref="TileCache"/> class. </summary>
        public TileCache()
            : base(GlobalOptions.TileCacheSize)
        {
        }
        #endregion
    }
}
