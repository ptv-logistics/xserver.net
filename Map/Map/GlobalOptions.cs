// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

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
        /// The default setting is automatic, which disables it for 64-Bit and enables it
        /// when running in a 32-Bit process.
        /// Must be called before the first initialization of the map control!
        /// Note: This property is now ignored by default and will be removed in later versions!
        /// </summary>
        [Obsolete("MemoryPressureMode is deprecated.")]
        public static MemoryPressureMode MemoryPressureMode = MemoryPressureMode.Automatic;

        /// <summary>
        /// The number of tiles which are cached in memory.
        /// </summary>
        public static int TileCacheSize = 512;

        /// <summary>
        /// Indicated that the "Infinite Zoom" feature should be activated.
        /// This allows zoom factors beyond level 19, but must be supported by all layers.
        /// Note: This property is now true by default and will be removed in later versions!
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
        /// Disable memory pressure when running as 64-bi and enable it when running in a 32-Bit process.        
        /// </summary>
        Automatic = 2
    }
}
