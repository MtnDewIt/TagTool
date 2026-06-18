using System;
using TagTool.Cache;
using TagTool.Common.Logging;
using BitmapGen2 = TagTool.Tags.Definitions.Gen2.Bitmap;
using BitmapDataGen2 = TagTool.Tags.Definitions.Gen2.Bitmap.BitmapDataBlock;
using BitmapFlagsGen2 = TagTool.Tags.Definitions.Gen2.Bitmap.BitmapDataBlock.FlagsValue;
using BitmapFormatGen2 = TagTool.Tags.Definitions.Gen2.Bitmap.BitmapDataBlock.FormatValue;
using BitmapTypeGen2 = TagTool.Tags.Definitions.Gen2.Bitmap.BitmapDataBlock.TypeValue;

namespace TagTool.Bitmaps.Utils
{
    // Temporary class until we unify the gens
    public static class BitmapUtilsGen2
    {
        public static int GetMipmapPixelCount(BitmapDataGen2 bitmap, int mipmapIndex)
        {
            int width = GetMipmapWidth(bitmap, mipmapIndex);
            int height = GetMipmapHeight(bitmap, mipmapIndex);
            int result = width * height * GetMipmapDepth(bitmap, mipmapIndex);
            if (bitmap.Type == BitmapTypeGen2.CubeMap)
                result *= 6;
            return result;
        }

        public static int GetMipmapRowPitch(BitmapDataGen2 bitmap, int mipmapIndex)
        {
            return GetMipmapWidth(bitmap, mipmapIndex) * GetBitsPerPixel(bitmap.Format) / 8;
        }

        public static int GetMipmapPixelDataSize(BitmapDataGen2 bitmap, int mipmapIndex)
        {
            return GetMipmapPixelCount(bitmap, mipmapIndex) * GetBitsPerPixel(bitmap.Format) / 8;
        }

        public static int GetMipmapDepth(BitmapDataGen2 bitmap, int mipmapIndex)
        {
            return Math.Max(1, bitmap.Depth >> mipmapIndex);
        }

        public static int GetMipmapWidth(BitmapDataGen2 bitmap, int mipmapIndex)
        {
            int width = Math.Max(1, bitmap.Width >> mipmapIndex);
            if (bitmap.Flags.HasFlag(BitmapFlagsGen2.Compressed))
                width = Align(width, 4);
            return width;
        }

        public static int GetMipmapHeight(BitmapDataGen2 bitmap, int mipmapIndex)
        {
            int height = Math.Max(1, bitmap.Height >> mipmapIndex);
            if (bitmap.Flags.HasFlag(BitmapFlagsGen2.Compressed))
                height = Align(height, 4);
            return height;
        }


        public static int Align(int size, int align)
        {
            return (size + (align - 1)) & ~(align - 1);
        }

        public static int GetMipmapOffset(BitmapDataGen2 bitmap, int mipmapIndex)
        {
            switch (bitmap.Type)
            {
                case BitmapTypeGen2._2dTexture:
                    return GetTexture2DOffset(bitmap, 0, 0, mipmapIndex);
                case BitmapTypeGen2._3dTexture:
                    return GetTexture3DOffset(bitmap, 0, 0, 0, mipmapIndex);
                case BitmapTypeGen2.CubeMap:
                    return GetTextureCubemapOffset(bitmap, 0, 0, 0, mipmapIndex);
                default:
                    throw new NotImplementedException();
            }
        }

        public static int GetTexture2DOffset(BitmapDataGen2 bitmap, int x, int y, int mipmapIndex)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            int align = bitmap.Flags.HasFlag(BitmapFlagsGen2.Compressed) ? 4 : 1;
            int bitsPerPixel = GetBitsPerPixel(bitmap.Format);

            int offset = 0;
            for (int i = 0; i < mipmapIndex; i++)
            {
                offset += ((width + (align - 1)) & ~(align - 1)) * ((height + (align - 1)) & ~(align - 1));
                width = Math.Max(width >> 1, align);
                height = Math.Max(height >> 1, align);
            }

            return (offset + x + y * width) * bitsPerPixel / 8;
        }

        public static int GetTexture3DOffset(BitmapDataGen2 bitmap, int x, int y, int z, int mipmapIndex)
        {
            int width = bitmap.Width;
            int height = bitmap.Height;
            int depth = bitmap.Depth;
            int align = bitmap.Flags.HasFlag(BitmapFlagsGen2.Compressed) ? 4 : 1;
            int bitsPerPixel = GetBitsPerPixel(bitmap.Format);

            int offset = 0;
            for (int i = 0; i < mipmapIndex; ++i)
            {
                offset += width * height * depth;
                width = Math.Max(width >> 1, align);
                height = Math.Max(height >> 1, align);
                depth = Math.Max(depth >> 1, align);
            }

            return bitsPerPixel * (offset + x + y * width + z * width * height) / 8;
        }

        public static int GetTextureCubemapOffset(BitmapDataGen2 bitmap, int x, int y, int z, int mipmapIndex)
        {
            int width = bitmap.Width;
            int align = bitmap.Flags.HasFlag(BitmapFlagsGen2.Compressed) ? 4 : 1;
            int bitsPerPixel = GetBitsPerPixel(bitmap.Format);

            int offset = 0;
            for (int i = 0; i < mipmapIndex; i++)
            {
                offset += 6 * width * width;
                width = Math.Max(width >> 1, align);
            }

            return (offset + x + y * width + z * width * width) * bitsPerPixel / 8;
        }

        public static int GetBitsPerPixel(BitmapFormatGen2 format)
        {
            switch (format)
            {
                case BitmapFormatGen2.Dxt1:
                    return 4;

                case BitmapFormatGen2.A8:
                case BitmapFormatGen2.Y8:
                case BitmapFormatGen2.Ay8:
                case BitmapFormatGen2.P8:
                case BitmapFormatGen2.P8Bump:
                case BitmapFormatGen2.Dxt3:
                case BitmapFormatGen2.Dxt5:
                    return 8;

                case BitmapFormatGen2.A8y8:
                case BitmapFormatGen2.R5g6b5:
                case BitmapFormatGen2.A1r5g5b5:
                case BitmapFormatGen2.A4r4g4b4:
                case BitmapFormatGen2.V8u8:
                case BitmapFormatGen2.G8b8:
                    return 16;

                case BitmapFormatGen2.X8r8g8b8:
                case BitmapFormatGen2.A8r8g8b8:
                    return 32;

                case BitmapFormatGen2.Rgbfp16:
                    return 48;

                case BitmapFormatGen2.Rgbfp32:
                    return 96;

                case BitmapFormatGen2.Argbfp32:
                    return 128;

                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }

        public static BitmapType MapBitmapType(BitmapTypeGen2 type)
        {
            switch (type)
            {
                case BitmapTypeGen2._2dTexture:
                    return BitmapType.Texture2D;
                case BitmapTypeGen2._3dTexture:
                    return BitmapType.Texture3D;
                case BitmapTypeGen2.CubeMap:
                    return BitmapType.CubeMap;
                default:
                    return BitmapType.Texture2D;
            }
        }

        public static BitmapFormat MapBitmapFormat(BitmapFormatGen2 format)
        {
            if (Enum.TryParse(format.ToString(), true, out BitmapFormat result))
            {
                return result;
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(format), $"Failed to find bitmap format matching {format}");
            }
        }

        public static bool IsNormalMap(GameCacheGen2 cache, BitmapGen2 gen2Bitmap, int imageIndex)
        {
            switch (gen2Bitmap.Bitmaps[imageIndex].Format)
            {
                case BitmapGen2.BitmapDataBlock.FormatValue.P8Bump:
                    return true;
            }

            switch (gen2Bitmap.Usage)
            {
                case BitmapGen2.UsageValue.HeightMap:
                case BitmapGen2.UsageValue.HeightMapG8b8:
                case BitmapGen2.UsageValue.HeightMapA8l8:
                case BitmapGen2.UsageValue.HeightMapBlue255:
                    //case BitmapGen2.UsageValue.VectorMap:
                    return true;
            }

            return false;
        }

    }
}
