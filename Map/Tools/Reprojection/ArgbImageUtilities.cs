// This source file is covered by the LICENSE.TXT file in the root folder of the SDK.

using System;
using System.IO;
using System.Linq;

#pragma warning disable CS3001 // Argument type is not CLS-compliant
#pragma warning disable CS3002 // Return type is not CLS-compliant
#pragma warning disable CS3003 // Type is not CLS-compliant

namespace Ptv.XServer.Controls.Map.Tools.Reprojection
{
    /// <summary>
    /// Code for crc calculation, initially taken from here:
    /// http://www.w3.org/TR/PNG-CRCAppendix.html
    /// 
    /// TODO: could we use .NET's crc implementation instead of this?
    /// </summary>
    public class Crc
    {
        /* Table of CRCs of all 8-bit messages. */
        private static readonly uint[] crc_table = new uint[256];

        /* Make the table for a fast CRC. */
        static Crc()
        {
            for (uint n = 0; n < 256; n++)
            {
                var c = n;

                for (int k = 0; k < 8; k++)
                    c = ((c & 1) != 0) ? (uint)(0xedb88320L ^ (c >> 1)) : c >> 1;

                crc_table[n] = c;
            }
        }

        /// <summary> Update a running CRC with the bytes buf[0..len-1].
        /// The CRC should be initialized to all 1's, and the transmitted value is the 1's complement of the final running CRC (see
        /// method <see cref="crc"/>)). </summary>
        /// <param name="crc">Cyclic redundancy check value.</param>
        /// <param name="buf">Buffer of image bytes.</param>
        /// <param name="offset">Where to start in buf. </param>
        /// <param name="length">Number of bytes in buf. </param>
        /// <returns>Final cyclic redundancy check value. </returns>
        public static uint update_crc(uint crc, byte[] buf, long offset, long length)
        {
            for (long n = offset, o = offset + length; n < o; n++)
                crc = crc_table[(crc ^ buf[n]) & 0xff] ^ (crc >> 8);

            return crc;
        }

        /// <summary> Return the CRC of the bytes buf[0..len-1]. </summary>
        /// <param name="buf">Buffer of image bytes.</param>
        /// <param name="offset">Where to start in buf. </param>
        /// <param name="length">Number of bytes in buf. </param>
        /// <returns>Final cyclic redundancy check value. </returns>
        public static uint crc(byte[] buf, long offset, long length)
        {
            return update_crc(0xffffffff, buf, offset, length) ^ 0xffffffff;
        }
    }

    /// <summary>
    /// Adler-32 checksum calculation, initially taken from here:
    /// http://www.java2s.com/Code/CSharp/Security/ComputesAdler32checksumforastreamofdata.htm
    /// 
    /// 
    /// 
    /// ----- original description ------------------------------------------------------------------
    /// 
    /// Computes Adler32 checksum for a stream of data. An Adler32
    /// checksum is not as reliable as a CRC32 checksum, but a lot faster to
    /// compute.
    /// 
    /// The specification for Adler32 may be found in RFC 1950.
    /// ZLIB Compressed Data Format Specification version 3.3)
    /// 
    /// 
    /// From that document:
    /// 
    ///      "ADLER32 (Adler-32 checksum)
    ///       This contains a checksum value of the uncompressed data
    ///       (excluding any dictionary data) computed according to Adler-32
    ///       algorithm. This algorithm is a 32-bit extension and improvement
    ///       of the Fletcher algorithm, used in the ITU-T X.224 / ISO 8073
    ///       standard.
    /// 
    ///       Adler-32 is composed of two sums accumulated per byte: s1 is
    ///       the sum of all bytes, s2 is the sum of all s1 values. Both sums
    ///       are done modulo 65521. s1 is initialized to 1, s2 to zero.  The
    ///       Adler-32 checksum is stored as s2*65536 + s1 in most-
    ///       significant-byte first (network) order."
    /// 
    ///  "8.2. The Adler-32 algorithm
    /// 
    ///    The Adler-32 algorithm is much faster than the CRC32 algorithm yet
    ///    still provides an extremely low probability of undetected errors.
    /// 
    ///    The modulo on unsigned long accumulators can be delayed for 5552
    ///    bytes, so the modulo operation time is negligible.  If the bytes
    ///    are a, b, c, the second sum is 3a + 2b + c + 3, and so is position
    ///    and order sensitive, unlike the first sum, which is just a
    ///    checksum.  That 65521 is prime is important to avoid a possible
    ///    large class of two-byte errors that leave the check unchanged.
    ///    (The Fletcher checksum uses 255, which is not prime and which also
    ///    makes the Fletcher check insensitive to single byte changes 0 -
    ///    255.)
    /// 
    ///    The sum s1 is initialized to 1 instead of zero to make the length
    ///    of the sequence part of s2, so that the length does not have to be
    ///    checked separately. (Any sequence of zeroes has a Fletcher
    ///    checksum of zero.)"
    /// </summary>
    public sealed class Adler32
    {
        /// <summary>
        /// largest prime smaller than 65536
        /// </summary>
        const uint BASE = 65521;

        /// <summary>
        /// Returns the Adler32 data checksum computed so far.
        /// </summary>
        public long Value => checksum;

        /// <summary>
        /// Creates a new instance of the Adler32 class.
        /// The checksum starts off with a value of 1.
        /// </summary>
        public Adler32()
        {
            Reset();
        }

        /// <summary>
        /// Resets the Adler32 checksum to the initial value.
        /// </summary>
        public void Reset()
        {
            checksum = 1;
        }

        /// <summary>
        /// Updates the checksum with a byte value.
        /// </summary>
        /// <param name="value">
        /// The data value to add. The high byte of the int is ignored.
        /// </param>
        public void Update(int value)
        {
            // We could make a length 1 byte array and call update again, but I
            // would rather not have that overhead
            uint s1 = checksum & 0xFFFF;
            uint s2 = checksum >> 16;

            s1 = (s1 + ((uint)value & 0xFF)) % BASE;
            s2 = (s1 + s2) % BASE;

            checksum = (s2 << 16) + s1;
        }

        /// <summary>
        /// Updates the checksum with an array of bytes.
        /// </summary>
        /// <param name="buffer">
        /// The source of the data to update with.
        /// </param>
        public void Update(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            Update(buffer, 0, buffer.Length);
        }

        /// <summary>
        /// Updates the checksum with the bytes taken from the array.
        /// </summary>
        /// <param name="buffer">
        /// an array of bytes
        /// </param>
        /// <param name="offset">
        /// the start of the data used for this update
        /// </param>
        /// <param name="count">
        /// the number of bytes to use for this update
        /// </param>
        public void Update(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), "cannot be negative");

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count), "cannot be negative");

            if (offset >= buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(offset), "not a valid index into buffer");

            if (offset + count > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(count), "exceeds buffer size");

            //(By Per Bothner)
            uint s1 = checksum & 0xFFFF;
            uint s2 = checksum >> 16;

            while (count > 0)
            {
                // We can defer the modulo operation:
                // s1 maximally grows from 65521 to 65521 + 255 * 3800
                // s2 maximally grows by 3800 * median(s1) = 2090079800 < 2^31
                int n = 3800;
                if (n > count)
                {
                    n = count;
                }
                count -= n;
                while (--n >= 0)
                {
                    s1 = s1 + (uint)(buffer[offset++] & 0xff);
                    s2 = s2 + s1;
                }
                s1 %= BASE;
                s2 %= BASE;
            }

            checksum = (s2 << 16) | s1;
        }

        #region Instance Fields
        uint checksum;
        #endregion
    }

    /// <summary>
    /// Generic helper class that snapshots the position of a memory stream for 
    /// checksum calculation of the data added afterwards.
    /// </summary>
    public class ChecksumUpdateRegion : IDisposable
    {
        private readonly long start;
        private long end;
        private uint? value;
        private readonly MemoryStream stm;
        private readonly Func<byte[], int, int, uint> update;

        private ChecksumUpdateRegion(MemoryStream stm, Func<byte[], int, int, uint> update)
        {
            this.stm = stm;
            this.update = update;

            start = end = stm.Position;
        }

        /// <summary>
        /// Creates and initializes an ChecksumUpdateRegion instance for 
        /// CRC checksum calculation implement in the Crc class above.
        /// </summary>
        /// <param name="stm">The stream to snapshot.</param>
        /// <param name="crc">Checksum value to update.</param>
        /// <returns></returns>
        public static ChecksumUpdateRegion ForCrc(MemoryStream stm, uint? crc = null)
        {
            return new ChecksumUpdateRegion(stm, (buf, offset, len) => crc.HasValue
                ? Crc.update_crc(crc.Value, buf, offset, len)
                : Crc.crc(buf, offset, len));
        }

        /// <summary>
        /// Creates and initializes an ChecksumUpdateRegion instance for 
        /// adler checksum calculation implement in the Adler32 class above.
        /// </summary>
        /// <param name="stm">The stream to snapshot.</param>
        /// <param name="adler">The Adler32 instance to update.</param>
        /// <returns></returns>
        public static ChecksumUpdateRegion ForAdler(MemoryStream stm, Adler32 adler)
        {
            return new ChecksumUpdateRegion(stm, (buf, offset, len) =>
            {
                adler.Update(buf, offset, len);
                return (uint)adler.Value;
            });
        }

        /// <summary>
        /// Dispose the instance. 
        /// No real implementation required, just a fake.
        /// </summary>
        public void Dispose()
        {
            // not really needed :-/, just a fake
        }

        /// <summary>
        /// Get the current checksum value.
        /// </summary>
        public uint Value
        {
            get
            {
                if (!value.HasValue || end != stm.Length)
                    value = update(stm.GetBuffer(), (int)start, (int)((end = stm.Length) - start));

                return value.Value;
            }
        }

        /// <summary>
        /// Turn the current checksum into a streamable byte array (network byte order).
        /// </summary>
        public byte[] Bytes => BitConverter.GetBytes(Value).ToBigEndian();
    }


    /// <summary>
    /// Provides extensions to ArgbImage.
    /// </summary>
    public static class ArgbImageExtensions
    {
        /// <summary>
        /// Write all the bytes given in the byte array.
        /// </summary>
        /// <param name="stm"></param>
        /// <param name="buf"></param>
        public static void Write(this Stream stm, params byte[] buf)
            { stm.Write(buf, 0, buf.Length); }

        /// <summary>
        /// Ensure the endianess of the encoded number.
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="bigEndian"></param>
        /// <returns></returns>
        private static byte[] EnsureEndianess(this byte[] bytes, bool bigEndian)
            { return BitConverter.IsLittleEndian == bigEndian ? bytes.Reverse().ToArray() : bytes; }

        /// <summary>
        /// Convert encoded number to big endian byte order.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[] ToBigEndian(this byte[] bytes)
            { return bytes.EnsureEndianess(true); }

        /// <summary>
        /// Convert encoded number to little endian byte order.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static byte[] ToLittleEndian(this byte[] bytes)
            { return bytes.EnsureEndianess(false); }

        /// <summary>
        /// Shortcuts ChecksumUpdateRegion.ForCrc.
        /// </summary>
        /// <param name="stm"></param>
        /// <param name="crc"></param>
        /// <returns></returns>
        public static ChecksumUpdateRegion GetCrcUpdateRegion(this MemoryStream stm, uint? crc = null)
            { return ChecksumUpdateRegion.ForCrc(stm, crc); }

        /// <summary>
        /// Shortcuts ChecksumUpdateRegion.ForAdler.
        /// </summary>
        /// <param name="stm"></param>
        /// <param name="adler"></param>
        /// <returns></returns>
        public static ChecksumUpdateRegion GetAdlerUpdateRegion(this MemoryStream stm, Adler32 adler)
            { return ChecksumUpdateRegion.ForAdler(stm, adler); }
    }
}

#pragma warning restore CS3003 // Type is not CLS-compliant
#pragma warning restore CS3002 // Return type is not CLS-compliant
#pragma warning restore CS3001 // Argument type is not CLS-compliant
