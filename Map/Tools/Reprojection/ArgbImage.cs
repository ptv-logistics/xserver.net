using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace Ptv.XServer.Controls.Map.Tools.Reprojection
{
    using PointD = System.Windows.Point;

    /// <summary>
    /// Represents an image in 32bpp ARGB format.
    /// </summary>
    public class ArgbImage
    {
        /// <summary>
        /// the images pixels
        /// <br />
        /// Four bytes represent on pixel, bytes ordered this way (this predetermined!): 
        /// [pixelIndex+0] > blue, [pixelIndex+1] > green, [pixelIndex+2] > red, [pixelIndex+3] > alpha. 
        /// <br />
        /// Pixel (0|0) starts at index 0.
        /// </summary>
        private readonly byte[] argbValues;

        /// <summary>
        /// the interpolation mode to use when accessing pixels
        /// </summary>
        private readonly int interpolationLevel;
        
        /// <summary>
        /// Creates and initializes an ArgbImage.
        /// </summary>
        /// <param name="size">The size of the image.</param>
        /// <param name="mode">Controls interpolation when reading pixels.</param>
        /// <remarks>Image will be made up of fully transparent black pixels.</remarks>
        public ArgbImage(Size size, InterpolationMode mode = InterpolationMode.Bicubic) : this(size.Width, size.Height, null, mode) { }

        /// <summary>
        /// Creates and initializes an ArgbImage.
        /// </summary>
        /// <param name="width">The width of the image.</param>
        /// <param name="height">The height of the image.</param>
        /// <param name="argbValues">The image's pixels. May be null.</param>
        /// <param name="mode">Controls interpolation when reading pixels.</param>
        /// <remarks>If argbValues is null, image will be made up of fully transparent black pixels. 
        /// If argbValues is non-null, the array length must equal width*height*4.
        /// </remarks>
        public ArgbImage(int width, int height, byte[] argbValues = null, InterpolationMode mode = InterpolationMode.Bicubic)
        {
            if (width < 1 || height < 1)
                throw new ArgumentException("unexpected image size");

            if (argbValues == null)
                argbValues = new byte[height * (width << 2)];

            if (argbValues.Length != width * height * 4)
                throw new InvalidDataException("invalid image buffer");

            Width = width;
            Height = height;

            this.argbValues = argbValues;

            // setup interpolation level by evaluating the given interpolation mode.
            switch (mode)
            {
                case InterpolationMode.Bicubic:
                case InterpolationMode.Default:
                case InterpolationMode.HighQualityBicubic:
                case InterpolationMode.High:
                    interpolationLevel = 2;
                    break;

                case InterpolationMode.Bilinear:
                case InterpolationMode.HighQualityBilinear:
                    interpolationLevel = 1;
                    break;

                case InterpolationMode.Low:
                case InterpolationMode.NearestNeighbor:
                    interpolationLevel = 0;
                    break;

                default:
                    throw new ArgumentException("unsupported interpolation mode " + mode);
            }
        }

        /// <summary>
        /// Creates and initializes an ArgbImage from a given System.Drawing.Image.
        /// </summary>
        /// <param name="image">The image to create the ArgbImage from.</param>
        /// <param name="mode">Controls the interpolation mode when accessing pixels.</param>
        /// <returns>The created ArgbImage.</returns>
        /// <remarks>If the given image is not a bitmap or does not equal the pixel format 
        /// PixelFormat.Format32bppArgb, an additional conversion step is applied to turn the 
        /// image into a 32bpp ARGB bitmap.</remarks>
        public static ArgbImage FromImage(Image image, InterpolationMode mode = InterpolationMode.Bicubic)
        {
            if ((image is Bitmap) && ((image as Bitmap).PixelFormat == PixelFormat.Format32bppArgb))
                return FromImage(image as Bitmap);

            var bitmap = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);

            using (var g = Graphics.FromImage(bitmap))
                g.DrawImage(image, 0, 0);

            return FromImage(bitmap, mode);
        }

        /// <summary>
        /// Creates and initializes an ArgbImage from a given Bitmap.
        /// </summary>
        /// <param name="bmp">The bitmap to create the ArgbImage from.</param>
        /// <param name="mode">Controls the interpolation mode when accessing pixels.</param>
        /// <returns>The created ArgbImage.</returns>
        /// <remarks>If the pixel format of the given bitmap is not PixelFormat.Format32bppArgb, an
        /// additional conversion step applies to turn the image into a 32bpp ARGB bitmap.</remarks>
        public static ArgbImage FromImage(Bitmap bmp, InterpolationMode mode = InterpolationMode.Bicubic)
        {
            if (bmp.PixelFormat != PixelFormat.Format32bppArgb)
                return FromImage((Image)bmp);

            // Lock the bitmap's bits.  
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var bmpData = bmp.LockBits(rect, ImageLockMode.ReadOnly, bmp.PixelFormat);

            // Get the address of the first line.
            var ptr = bmpData.Scan0;

            // Check stride
            if (bmpData.Stride != (bmp.Width << 2))
                throw new InvalidDataException("bitmap stride contains an unexpected value");

            // Declare an array to hold the bytes of the bitmap. 
            var bytes = Math.Abs(bmpData.Stride) * bmp.Height;
            var argbValues = new byte[bytes];

            // Copy the ARGB values into the array.
            System.Runtime.InteropServices.Marshal.Copy(ptr, argbValues, 0, bytes);

            // Unlock the bits.
            bmp.UnlockBits(bmpData);

            // create ArgbImage
            return new ArgbImage(bmp.Width, bmp.Height, argbValues, mode);
        }

        /// <summary>
        /// Tests if a given point lies within the image.
        /// </summary>
        /// <param name="x">x-coordinate</param>
        /// <param name="y">y-coordinate</param>
        /// <returns></returns>
        private bool ValidIndex(int x, int y)
        {
            return (x >= 0 && y >= 0 && x < Width && y < Height);
        }

        /// <summary>
        /// Determines the ARGB buffer index of a pixel, given its location.
        /// </summary>
        /// <param name="x">x-coordinate</param>
        /// <param name="y">y-coordinate</param>
        /// <returns>The pixels index.</returns>
        private int GetIndex(int x, int y)
        {
            return 4 * (x + y * Width);
        }

        /// <summary>
        /// Determines the nearest color, given the location of a pixel.
        /// </summary>
        /// <param name="x">x-coordinate</param>
        /// <param name="y">y-coordinate</param>
        /// <returns>Nearest color or "transparent white" for invalid locations.</returns>
        uint GetNearestColor(double x, double y)
        {
            var nearestX = (int)Math.Round(x);
            var nearestY = (int)Math.Round(y);

            if (!ValidIndex(nearestX, nearestY))
                return 0x00ffffff;

            var idx = GetIndex(nearestX, nearestY);

            return
                ((uint)argbValues[idx + 3] << 24) |
                ((uint)argbValues[idx + 2] << 16) |
                ((uint)argbValues[idx + 1] << 8) |
                ((uint)argbValues[idx + 0] << 0);
        }

        /// <summary>
        /// Set the color of the nearest pixel, given its location.
        /// </summary>
        /// <param name="x">x-coordinate</param>
        /// <param name="y">y-coordinate</param>
        /// <param name="value">color value</param>
        private void SetNearestColor(double x, double y, uint value)
        {
            var nearestX = (int)Math.Round(x);
            var nearestY = (int)Math.Round(y);

            if (!ValidIndex(nearestX, nearestY))
                return;

            for (int i = 0, idx = GetIndex(nearestX, nearestY); i < 4; ++i)
                argbValues[idx + i] = (byte)((value >> (i << 3)) & 0x000000ff);
        }

        /// <summary>
        /// Kernel function of the bicubic interpolation.
        /// </summary>
        /// <param name="distance">The distance to a position.</param>
        /// <returns>Kernel value.</returns>
        /// <remarks><see href="http://www.engr.mun.ca/~baxter/Publications/ImageZooming.pdf"/> was very 
        /// helpful to make the color interpolation produce proper results.</remarks>
        private static double BicubicInterpolationKernel(double distance)
        {
            distance = Math.Abs(distance);

            if (distance < 1)
                return 1.5 * Math.Pow(distance, 3) - 2.5 * Math.Pow(distance, 2) + 1;

            if (distance < 2)
                return -0.5 * Math.Pow(distance, 3) + 2.5 * Math.Pow(distance, 2) - 4 * distance + 2;

            return 0;
        }

        /// <summary>
        /// Core interpolation function that determines an interpolated color given the index of the (left|top) point 
        /// of the block to process and the values of interpolation kernel in x- and y-direction.
        /// </summary>
        /// <param name="idx0">Index of the left, top point</param>
        /// <param name="interpolationKernelH">Values of the interpolation kernel in x-direction</param>
        /// <param name="interpolationKernelV">Values of the interpolation kernel in y-direction</param>
        /// <returns>Interpolated color</returns>
        /// <remarks><see href="http://www.engr.mun.ca/~baxter/Publications/ImageZooming.pdf"/> was very 
        /// helpful to make the color interpolation produce proper results.</remarks>
        uint Interpolate(int idx0, IList<double> interpolationKernelH, IList<double> interpolationKernelV)
        {
            // color variable that will be updated in the loop. Initialize to 0.
            uint color = 0;

            // loop the four color component
            for (int componentOffset = 0, componentMask = 0; componentOffset < 4; ++componentOffset, componentMask += 8)
            {
                double intensity = 0;

                // interpolation loops for the current color component. We need to interpolate 
                // horizontally and vertically using the specified kernel function values.

                for (int i = 0, idxH0 = idx0 + componentOffset; i < interpolationKernelV.Count; ++i, idxH0 += (Width << 2))
                {
                    double intensityH = 0;

                    for (int j = 0, idx = idxH0; j < interpolationKernelH.Count; ++j, idx += 4)
                        intensityH += interpolationKernelH[j] * argbValues[idx];

                    intensity += interpolationKernelV[i] * intensityH;
                }

                // update color with interpolated color component
                color |= (uint)Math.Max(0, Math.Min(255, Math.Round(intensity))) << componentMask;
            }

            // done, return color
            return color;
        }

        /// <summary>
        /// Gets the color of a pixel using color interpolation.
        /// </summary>
        /// <param name="x">x-coordinate of the pixel to get the color of.</param>
        /// <param name="y">y-coordinate of the pixel to get the color of.</param>
        /// <param name="floorX">Conveniently specifies the value of Math.Floor(x).</param>
        /// <param name="floorY">Conveniently specifies the value of Math.Floor(y).</param>
        /// <returns>The color of the pixel or "transparent white" if the location is invalid.</returns>
        /// <remarks>Uses either bicubic interpolation, bilinear interpolation or nearest color interpolation, 
        /// depending on the given coordinates.<br/>
        /// <see href="http://www.engr.mun.ca/~baxter/Publications/ImageZooming.pdf"/> 
        /// was very helpful to make the color interpolation produce proper results.</remarks>
        uint GetColor(double x, double y, int floorX, int floorY)
        {
            // test pixel location for bicubic interpolation
            if (interpolationLevel >= 2 && floorX > 0 && floorY > 0 && floorX < (Width - 2) && floorY < (Height - 2))
            {
                var interpolationKernelH = new double[4];
                var interpolationKernelV = new double[4];

                for (var i = 0; i < 4; ++i)
                {
                    interpolationKernelH[i] = BicubicInterpolationKernel(x - floorX - i + 1);
                    interpolationKernelV[i] = BicubicInterpolationKernel(y - floorY - i + 1);
                }

                return Interpolate(
                    GetIndex(floorX - 1, floorY - 1),
                    interpolationKernelH,
                    interpolationKernelV
                );
            }

            // test pixel location for bilinear interpolation
            if (interpolationLevel < 1 || floorX < 0 || floorY < 0 || floorX >= (Width - 1) || floorY >= (Height - 1))
                // simply do nearest color
                return GetNearestColor(x, y);
            
            // bilinear interpolation it is
            var fracX = x - floorX;
            var fracY = y - floorY;

            return Interpolate(GetIndex(floorX, floorY), new[] { 1 - fracX, fracX }, new[] { 1 - fracY, fracY });
        }

        /// <summary> Reads or writes the color of a pixel. </summary>
        /// <param name="x"> x-coordinate of the pixel location. </param>
        /// <param name="y"> y-coordinate of the pixel location. </param>
        /// <returns>The color of the pixel or "transparent white" if the location is invalid.</returns>
        /// <remarks>Uses bicubic interpolation when reading colors. When settings colors, changes the nearest pixel.</remarks>
        public uint this[double x, double y]
        {
            get { return GetColor(x, y, (int)Math.Floor(x), (int)Math.Floor(y)); }
            set { SetNearestColor(x, y, value); }
        }

        /// <summary>
        /// Reads or writes the color of a pixel.
        /// </summary>
        /// <param name="p">Pixel location.</param>
        /// <returns>The color of the pixel or "transparent white" if the location is invalid.</returns>
        /// <remarks>Uses bicubic interpolation when reading colors. When settings colors, changes the nearest pixel.</remarks>
        public uint this[PointF p]
        {
            get { return this[p.X, p.Y]; }
            set { this[p.X, p.Y] = value; }
        }

        /// <summary>
        /// Reads or writes the color of a pixel.
        /// </summary>
        /// <param name="p">Pixel location.</param>
        /// <returns>The color of the pixel or "transparent white" if the location is invalid.</returns>
        /// <remarks>Uses bicubic interpolation when reading colors. When settings colors, changes the nearest pixel.</remarks>
        public uint this[PointD p]
        {
            get { return this[p.X, p.Y]; }
            set { this[p.X, p.Y] = value; }
        }

        /// <summary> Returns an image stream for further processing. </summary>
        /// <remarks> The stream returned will contain a 32bpp png image, including alpha. </remarks>
        public Stream Stream
        {
            get
            {
                // This property formerly produced a 32bpp BMP image. However, as the BMP image format does not 
                // fully support alpha transparency, this property has been changed to produce a valid PNG image 
                // stream instead. BMP files including an alpha channel do not work with every application (e.g. 
                // they work in Office applications, but not in .NET).
                //
                // We favor speed over size; therefore we're creating an uncompressed PNG image fully residing in 
                // memory. The following links were helpful when writing the code:
                //
                // - http://stackoverflow.com/questions/7942635/write-png-quickly
                // - http://mainisusuallyafunction.blogspot.de/2012/04/minimal-encoder-for-uncompressed-pngs.html
                // 
                // - http://www.libpng.org/pub/png/spec/1.2/PNG-Compression.html
                // - ftp://ftp.isi.edu/in-notes/rfc1950.txt
                //
                // - http://www.w3.org/TR/PNG-CRCAppendix.html
                // - http://www.java2s.com/Code/CSharp/Security/ComputesAdler32checksumforastreamofdata.htm

                // create the stream that will contain the PNG image at the end
                var mem = new MemoryStream();

                // PNG magic bytes
                mem.Write(0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a);

                // IHDR: Header describing the image.

                // byte length of IHDR chunk (fixed value, 13 bytes)
                mem.Write(0x00, 0x00, 0x00, 0x0d);

                // begin crc calculation
                using (var crc = mem.GetCrcUpdateRegion())
                {
                    // IHDR magic bytes
                    mem.Write(0x49, 0x48, 0x44, 0x52);

                    // width, height in network byte order (big endian)
                    mem.Write(BitConverter.GetBytes(Width).ToBigEndian());
                    mem.Write(BitConverter.GetBytes(Height).ToBigEndian());

                    mem.Write(
                        // bit depth and color type, see http://www.w3.org/TR/PNG/#table111
                        // set to RGBA with 8 bits per color component
                        0x08, 0x06,

                        // compression method, filter method, interlace method - not used here
                        0x00, 0x00, 0x00
                    );

                    // CRC of IHDR chunk
                    mem.Write(crc.Bytes);
                }


                // 
                // IDAT
                //
                // Contains the actual image data.
                //

                // write pre-calculated length of IDAT chunk. Formula explained:
                //
                // - we're writing Width*4+1 bytes per scan line, and Height lines altogether. Note that every line is 
                //   prefixed by one additional byte set to 0 (thisis PNG specific). This is why it's Width*4+1 and not 
                //   Width *4.
                // 
                // - for the zlib lib stream, the pixel bytes have to be divided into block of max. 65535 bytes in size. 
                //   Every block is prefixed by a 5-byte header.
                // 
                // - and finally there are another 6 fixed bytes in zlib stream format. 
                //   (CMF, FLG bytes at the beginning and the 4-byte Adler32 checksum at the end)

                var lineSize = (Width << 2) + 1;
                var linesPerBlock = 32768 / lineSize;
                var nBlocks = (Height + linesPerBlock - 1) / linesPerBlock;
                var idatSize = 6 + (nBlocks * 5) + (lineSize * Height);

                mem.Write(BitConverter.GetBytes(idatSize).ToBigEndian());

                // crc calculation
                using (var crc = mem.GetCrcUpdateRegion())
                {
                    // write IDAT magic bytes
                    mem.Write(0x49, 0x44, 0x41, 0x54);

                    // start zlib stream with CMF & FLG byte
                    mem.Write(
                        // CMF field: set "deflate" compression method (0x08) + 32k window size (0x70)
                        0x78,

                        // FLG field: 0x01 refers to the FCHECK of the FLG field. The rest says "no compression".
                        // According to https://www.ietf.org/rfc/rfc1951.txt:
                        // 
                        //    SPEC: The FCHECK value must be such that CMF and FLG, when viewed as a 16-bit unsigned integer 
                        //    SPEC: stored in MSB order (CMF*256 + FLG), is a multiple of 31.
                        //
                        // In our case: (0x7801 = 30721) / 31 = 991
                        0x01
                    );

                    // setup Adler32 checksum calculation
                    var adler = new Adler32();

                    // loop through scan lines
                    for (var h = 0; h < Height; h += linesPerBlock)
                    {
                        // determine the number of lines to write in this zlib stream block
                        var nLines = Math.Min(Height - h, linesPerBlock);

                        // size in bytes of current zlib block
                        var size = nLines * lineSize;

                        // begin zlib block header, write 1 if this if the last block and 0 otherwise. 
                        // According to https://www.ietf.org/rfc/rfc1951.txt:
                        //
                        //    SPEC:  ...
                        //    SPEC: 
                        //    SPEC:  Each block of compressed data begins with 3 header bits containing the following data:
                        //    SPEC: 
                        //    SPEC:     first bit  BFINAL
                        //    SPEC:    next 2 bits BTYPE
                        //    SPEC: 
                        //    SPEC:  ...
                        //    SPEC: 
                        //    SPEC:  Any bits of input up to the next byte boundary are ignored. The rest of the block consists 
                        //    SPEC:  of the following information:
                        //    SPEC: 
                        //    SPEC:  ...
                        //
                        // Be aware that the last header byte is 0x80 and not 0x01 (BTYPE=00).

                        mem.WriteByte(h + linesPerBlock >= Height ? (byte) 0x80 : (byte) 0);

                        // block size in bytes. Little endian required here.
                        mem.Write(BitConverter.GetBytes((ushort)size).ToLittleEndian());

                        // complement of the block size. Little endian required here.
                        mem.Write(BitConverter.GetBytes((ushort)~size).ToLittleEndian());

                        // The adler checksum is calculated for the data only, no header or other fixed 
                        // bytes are included. Begin update block now.
                        using (var update = mem.GetAdlerUpdateRegion(adler))
                        {
                            // loop through the lines we're going to write into the current block
                            for (int i = h, j = Math.Min(Height, h + linesPerBlock); i < j; ++i)
                            {
                                // every scan line is to be prefixed with ony bytes set to 0. PNG specific.
                                mem.WriteByte(0);

                                // write the pixels. Our buffer is BGRA encoded, we need RGBA 
                                // for the PNG. Must swap color components on the fly.
                                for (int o = i * (Width << 2), p = o + (Width << 2); o < p; o += 4)
                                    mem.Write(argbValues[o + 2], argbValues[o + 1], argbValues[o + 0], argbValues[o + 3]);
                            }
                        }
                    }

                    // Adler32 checksum of pixel data, as described in RFC1950. 
                    // Network byte order (= big endian) is used here.
                    mem.Write(BitConverter.GetBytes((uint)adler.Value).ToBigEndian());

                    // Zlib stream ends with the Adler32 written above. 
                    // We're back in PNG format here. End the IDAT chunk with CRC.
                    mem.Write(crc.Bytes);
                }

                //
                // IEND
                //
                // Ends the PNG file.
                //

                mem.Write(
                    // byte length of IEND chunk, 4 bytes, IEND has not data.
                    0x00, 0x00, 0x00, 0x00,

                    // IEND magic bytes
                    0x49, 0x45, 0x4e, 0x44,

                    // Precalculated CRC of IEND chunk. As this chunk includes no data, 
                    // the crc is just made up from the 4-byte chunk magic.
                    0xae, 0x42, 0x60, 0x82
                );

                //
                // we're done; return stream
                // 

                mem.Seek(0, SeekOrigin.Begin);

                return mem;
            }
        }


        /// <summary> Determines the width of the image. </summary>
        public int Width { get; private set; }

        /// <summary> Determines the height of the image. </summary>
        public int Height { get; private set; }
    }
}
