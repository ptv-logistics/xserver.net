// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System.Reflection;
using System.Runtime.InteropServices;

namespace System.IO
{
    /// <summary>
    /// Utility class providing an application specific temporary space in the Windows' TEMP directory.
    /// </summary>
    internal static class TempSpace
    {
        /// <summary>
        /// The tempBase variable.
        /// </summary>
        private static readonly string tempBase;
        
        /// <summary>
        /// Tries to delete a directory.
        /// </summary>
        /// <param name="dir">The directory to delete.</param>
        /// <remarks>Used by the static constructor to initially create the temp space.</remarks>
        private static void TryCleanup(string dir)
        {
            if (!Directory.Exists(dir)) return;
            foreach (var subdir in Directory.GetDirectories(dir))
                try { Directory.Delete(subdir, true); }
                catch { }
        }

        /// <summary>
        /// Tries to create a directory.
        /// </summary>
        /// <param name="dir">The directory to create.</param>
        /// <remarks>Used by the static constructor to force clean the applications temp space.</remarks>
        private static void TryCreate(string dir)
        {
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        /// <summary>
        /// Initializes static members of the <see cref="TempSpace"/> class.
        /// </summary>
        static TempSpace()
        {
            try
            {
                GuidAttribute guid = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(GuidAttribute), true)[0] as GuidAttribute;

                tempBase += Path.GetTempPath() + guid.Value;

                TryCleanup(tempBase);
                TryCreate(tempBase);

                if (!Directory.Exists(tempBase))
                    throw new IOException("failed to create temp space in \"" + tempBase + "\"");
            }
            catch(Exception e)
            {
                throw new SystemException("failed to create temp space", e);
            }
        }

        /// <summary>
        /// Tries to create a temporary directory.
        /// </summary>
        /// <returns>The path of the temporary directory or null on any error.</returns>
        public static string TryMakeSpace()
        {
            try
            {
                string result = null;

                if (Directory.Exists(tempBase))
                    Directory.CreateDirectory(result = tempBase + Path.DirectorySeparatorChar + Guid.NewGuid());

                if (!Directory.Exists(result))
                    result = null;

                return result;
            }
            catch
            {
                return null;
            }
        }
    }
}
