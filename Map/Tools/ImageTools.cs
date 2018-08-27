// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing;


namespace Ptv.XServer.Controls.Map.Tools
{
    /// <summary> Tool class for handling writeable bitmaps. </summary>
    public static class ImageTools
    {
        #region public methods
        /// <summary> Creates a writeable bitmap. </summary>
        /// <param name="width"> The width of the bitmap. </param>
        /// <param name="height"> The height of the bitmap. </param>
        /// <returns> The created bitmap. </returns>
        public static WriteableBitmap CreateWriteableBitmap(int width, int height)
        {
           return new WriteableBitmap(width, height, 96, 96, PixelFormats.Pbgra32, null);
        }

        /// <summary> Loads an image from a byte array using GDI. </summary>
        /// <param name="buffer"> The byte array containing the image. </param>
        /// <returns> The image loaded. </returns>
        public static Image LoadGdiImage(byte[] buffer)
        {
            using (var ms = new MemoryStream(buffer))
            {
                return Image.FromStream(ms);
            }
        }

        /// <summary> Writes an image into an existing writeable bitmap. </summary>
        /// <param name="image"> The image to write. </param>
        /// <param name="writeableBitmap"> The writeable bitmap. </param>
        public static void WriteImageIntoBitmap(Image image, WriteableBitmap writeableBitmap)
        {
            writeableBitmap.Lock();
            using (var bmp = new Bitmap(writeableBitmap.PixelWidth, writeableBitmap.PixelHeight,
                                     writeableBitmap.BackBufferStride,
                                     System.Drawing.Imaging.PixelFormat.Format32bppPArgb,
                                     writeableBitmap.BackBuffer))
            {
                using (var g = Graphics.FromImage(bmp)) // Good old Graphics
                {
                    g.Clear(System.Drawing.Color.Transparent);
                    g.DrawImageUnscaledAndClipped(image, new Rectangle(0, 0, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight));

                    writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight));
                }
            }
            writeableBitmap.Unlock();
        }

        //public static ImageSource LoadImage(byte[] buffer)
        //{
        //    //// this code triggers the GC!
        //    //// https://connect.microsoft.com/VisualStudio/feedback/details/687605/gc-is-forced-when-working-with-small-writeablebitmap 
        //    //var bmp = new BitmapImage();
        //    //bmp.BeginInit();
        //    //bmp.StreamSource = new MemoryStream(buffer);
        //    //bmp.EndInit();
        //    //bmp.Freeze();
        //    //return bmp;

        //    //var ms = new MemoryStream(buffer);
        //    //var img = System.Drawing.Image.FromStream(ms);
        //    //IntPtr hBitmap = ((Bitmap)img).GetHbitmap();
        //    //var interopBitmap = (System.Windows.Interop.InteropBitmap)System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
        //    //    hBitmap, IntPtr.Zero, Int32Rect.Empty, System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions());
        //    //return interopBitmap;

        //    // Workaround: Load image via GDI and render it to a WPF WriteableBitmap
        //    //var wb = new WriteableBitmap(img.Width, img.Height, 96, 96, PixelFormats.Pbgra32, null);
        //    //wb.Lock();

        //    //var bmp = new System.Drawing.Bitmap(wb.PixelWidth, wb.PixelHeight,
        //    //                                     wb.BackBufferStride,
        //    //                                     System.Drawing.Imaging.PixelFormat.Format32bppPArgb,
        //    //                                     wb.BackBuffer);
        //    //var g = System.Drawing.Graphics.FromImage(bmp); // Good old Graphics

        //    //g.DrawImage(img, new System.Drawing.Point(0, 0));
        //    //wb.AddDirtyRect(new Int32Rect(0, 0, img.Width, img.Height));

        //    //img.Dispose();
        //    //g.Dispose();
        //    //bmp.Dispose();
        //    //ms.Dispose();

        //    //wb.Unlock();
        //    //wb.Freeze();

        //    //return wb;
        //}
        #endregion
    }
}