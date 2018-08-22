using System;

namespace Ptv.XServer.Controls.Map
{
    /// <summary>
    /// This class manages the global options of the map
    /// </summary>
    public static class GlobalOptions
    {
        /// <summary>
        /// Enable the incrementation of memory pressure for bitmap images
        /// The default setting is automatic, wich disables it for 64-Bit and enables it
        /// when running in a 32-Bit process.
        /// Must be called before the first initialization of the map control!
        /// </summary>
        public static MemoryPressureMode MemoryPressureMode = MemoryPressureMode.Automatic;

        /// <summary>
        /// The number of tiles which are cached in memory.
        /// </summary>
        public static int TileCacheSize = 512;

        /// <summary>
        /// Indicated that the "Infinite Zoom" feature should be activated.
        /// This allows zoom factors beyond level 19, but must be supported by all layers.
        /// </summary>
        [Obsolete("InfiniteZoom is deprecated.")]
        public static bool InfiniteZoom = true;
    }

    /// <summary>
    /// The memory pressure modes for internal bitmap images.
    /// </summary>
    public enum MemoryPressureMode
    {
        /// <summary>
        /// Increase memory pressure for bitmap images. This triggers the garbage collector
        /// very often and can cause stuttering when a large amount of managed memory is allocated.
        /// </summary>
        Enable = 0,
        /// <summary>
        /// Disable the incrementation of memory pressure for BitmapImages
        /// </summary>
        Disable = 1,
        /// <summary>
        /// Disable memory pressore when running as 64-bi and enable it when running in a 32-Bit process.        
        /// </summary>
        Automatic = 2,
    }
}
