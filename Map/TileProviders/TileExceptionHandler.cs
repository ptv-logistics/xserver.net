// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.Drawing.Imaging;
using System.IO;


namespace Ptv.XServer.Controls.Map.TileProviders
{
    /// <summary> Exception handler for tile servers. </summary>
    public class TileExceptionHandler
    {
        /// <summary> Builds a bitmap tile which shows the occurred exception. </summary>
        /// <param name="exception"> The exception which occurred. </param>
        /// <param name="width"> Width of the tile. </param>
        /// <param name="height"> Height of the tile. </param>
        /// <returns> The exception bitmap as stream. </returns>
        public static Stream RenderException(Exception exception, int width, int height)
        {            
#if DEBUG
            using (var bmp = new System.Drawing.Bitmap(width, height))
            {
                using (var graphics = System.Drawing.Graphics.FromImage(bmp))
                {
                    graphics.DrawString(exception.Message, System.Drawing.SystemFonts.DefaultFont, System.Drawing.Brushes.Red,
                        new System.Drawing.Rectangle(0, 0, width, height));
                }

                var stream = new MemoryStream();
                bmp.Save(stream, ImageFormat.Png);
                stream.Seek(0, SeekOrigin.Begin);
                return stream;
            }
#else
            return null;
#endif        
        }
    }
}
