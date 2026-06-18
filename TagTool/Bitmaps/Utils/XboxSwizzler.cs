using System;
using System.Runtime.InteropServices;

namespace TagTool.Bitmaps.Utils
{
    public static class XboxSwizzler
    {
        public static byte[] Swizzle(ReadOnlySpan<byte> src, int width, int height, int depth, int bpp, bool deswizzle = false)
        {
            byte[] dst = new byte[src.Length];

            switch (bpp)
            {
                case 1:
                    Swizzle(src, dst.AsSpan(), width, height, depth, deswizzle);
                    break;
                case 2:
                    Swizzle(
                        MemoryMarshal.Cast<byte, ushort>(src),
                        MemoryMarshal.Cast<byte, ushort>(dst.AsSpan()),
                        width, height, depth, deswizzle);
                    break;
                case 4:
                    Swizzle(
                        MemoryMarshal.Cast<byte, uint>(src),
                        MemoryMarshal.Cast<byte, uint>(dst.AsSpan()),
                        width, height, depth, deswizzle);
                    break;
                default:
                    throw new NotSupportedException("### ERROR unsupported bitmap format (bytes per pixel)");
            }

            return dst;
        }

        private static void Swizzle<T>(ReadOnlySpan<T> src, Span<T> dst, int width, int height, int depth, bool deswizzle = false) where T : unmanaged
        {
            if (src.Length < (width * height * depth))
                throw new ArgumentException("Source buffer too small");
            if (dst.Length < width * height * depth)
                throw new ArgumentException("Destination buffer too small");

            var mask = new MaskSet(width, height, depth);
            int mortonX = 0, mortonY = 0, mortonZ = 0;
            int index = 0;

            for (int z = 0; z < depth; z++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (deswizzle)
                            dst[index++] = src[mortonX | mortonY | mortonZ];
                        else
                            dst[mortonX | mortonY | mortonZ] = src[index++];

                        mortonX = mask.X & (mortonX - mask.X);
                    }
                    mortonY = mask.Y & (mortonY - mask.Y);
                }
                mortonZ = mask.Z & (mortonZ - mask.Z);
            }
        }

        readonly ref struct MaskSet
        {
            public readonly int X;
            public readonly int Y;
            public readonly int Z;

            public MaskSet(int w, int h, int d)
            {
                var bit = 1;
                var index = 1;

                while (bit < w || bit < h || bit < d)
                {
                    if (bit < w)
                    {
                        X |= index;
                        index <<= 1;
                    }

                    if (bit < h)
                    {
                        Y |= index;
                        index <<= 1;
                    }

                    if (bit < d)
                    {
                        Z |= index;
                        index <<= 1;
                    }

                    bit <<= 1;
                }
            }
        }

    }
}
