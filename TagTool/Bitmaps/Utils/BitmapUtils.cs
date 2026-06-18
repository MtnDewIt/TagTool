using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using TagTool.Bitmaps.DDS;
using TagTool.Bitmaps.Utils;
using TagTool.Cache;
using TagTool.Direct3D.D3D9;
using TagTool.Porting;
using TagTool.Tags;
using TagTool.Tags.Definitions;
using TagTool.Tags.Resources;

namespace TagTool.Bitmaps
{
    public static class BitmapUtils
    {
        public static int RoundSize(int size, int blockDimension)
        {
            return blockDimension * ((size + (blockDimension - 1)) / blockDimension);
        }

        public static bool IsPowerOfTwo(int size)
        {
            return (size != 0) && ((size & (size - 1)) == 0);
        }

        public static bool IsPowerOfTwo(int width, int height)
        {
            return IsPowerOfTwo(width) && IsPowerOfTwo(height);
        }

        public static bool IsBlockAligned(int width, int height)
        {
            return ((width | height) & 3) == 0;
        }

        public static BitmapTextureInteropDefinition CreateBitmapTextureInteropDefinition(BaseBitmap bitmap)
        {
            var result = new BitmapTextureInteropDefinition
            {
                Width = (short)bitmap.Width,
                Height = (short)bitmap.Height,
                Depth = (byte)bitmap.Depth,
                MipmapCount = (sbyte)Math.Max(1, bitmap.MipMapCount),
                HighResInSecondaryResource = 0,
            };

            result.BitmapType = bitmap.Type;
            result.Format = bitmap.Format;

            switch (result.Format)
            {
                case BitmapFormat.Dxt1:
                    result.D3DFormat = (int)D3DFormat.D3DFMT_DXT1;
                    result.Flags |= BitmapFlags.Compressed;
                    break;
                case BitmapFormat.Dxt3:
                    result.D3DFormat = (int)D3DFormat.D3DFMT_DXT3;
                    result.Flags |= BitmapFlags.Compressed;
                    break;
                case BitmapFormat.Dxt5:
                case BitmapFormat.Dxt5nm:
                    result.D3DFormat = (int)D3DFormat.D3DFMT_DXT5;
                    result.Flags |= BitmapFlags.Compressed;
                    break;
                case BitmapFormat.Dxn:
                    result.D3DFormat = (int)D3DFormat.D3DFMT_ATI2;
                    result.Flags |= BitmapFlags.Compressed;
                    break;
                default:
                    result.D3DFormat = (int)D3DFormat.D3DFMT_UNKNOWN;
                    break;
            }

            result.Curve = bitmap.Curve;

            if (IsPowerOfTwo(bitmap.Width) && IsPowerOfTwo(bitmap.Height))
                result.Flags |= BitmapFlags.PowerOfTwoDimensions;

            return result;
        }

        public static BitmapTextureInteropDefinition CreateBitmapTextureInteropDefinition(DDSHeader header, BitmapImageCurve bitmapCurve)
        {
            var result = new BitmapTextureInteropDefinition
            {
                Width = (short)header.Width,
                Height = (short)header.Height,
                Depth = (byte)Math.Max(1, header.Depth),
                MipmapCount = (sbyte)Math.Max(1, header.MipMapCount),
                HighResInSecondaryResource = 0,
            };

            result.BitmapType = BitmapDdsFormatDetection.DetectType(header);
            result.Format = BitmapDdsFormatDetection.DetectFormat(header);
            result.D3DFormat = (int)header.PixelFormat.FourCC;

            result.Curve = bitmapCurve;

            if (IsCompressedFormat(result.Format))
                result.Flags |= BitmapFlags.Compressed;

            if (IsPowerOfTwo(header.Width) && IsPowerOfTwo(header.Height))
                result.Flags |= BitmapFlags.PowerOfTwoDimensions;

            return result;
        }

        public static Bitmap.Image CreateBitmapImageFromResourceDefinition(BitmapTextureInteropDefinition definition)
        {
            var result = new Bitmap.Image()
            {
                Signature = "bitm",
                Width = definition.Width,
                Height = definition.Height,
                Depth = (sbyte)definition.Depth,
                Format = definition.Format,
                Type = definition.BitmapType,
                MipmapCount = (sbyte)Math.Max(0, definition.MipmapCount - 1),
                Flags = definition.Flags,
                Curve = definition.Curve
            };
            return result;
        }

        public static BitmapTextureInteropResource CreateEmptyBitmapTextureInteropResource()
        {
            return new BitmapTextureInteropResource
            {
                Texture = new D3DStructure<BitmapTextureInteropResource.BitmapDefinition>
                {
                    Definition = new BitmapTextureInteropResource.BitmapDefinition
                    {
                        PrimaryResourceData = new TagData(),
                        SecondaryResourceData = new TagData(),
                        Bitmap = new BitmapTextureInteropDefinition()
                    },
                    AddressType = CacheAddressType.Definition
                }
            };
        }

        public static BitmapTextureInteropResource CreateBitmapTextureInteropResource(BaseBitmap bitmap)
        {
            var result = CreateEmptyBitmapTextureInteropResource();
            result.Texture.Definition.PrimaryResourceData.Data = bitmap.Data;
            result.Texture.Definition.Bitmap = CreateBitmapTextureInteropDefinition(bitmap);
            return result;
        }

        public static void SetBitmapImageData(BaseBitmap bitmap, Bitmap.Image image)
        {
            image.Width = (short)bitmap.Width;
            image.Height = (short)bitmap.Height;
            image.Depth = (sbyte)bitmap.Depth;
            image.Format = bitmap.Format;
            image.Type = bitmap.Type;
            image.MipmapCount = (sbyte)(bitmap.MipMapCount - 1);
            image.PixelDataSize = bitmap.Data.Length;
            image.XboxFlags = BitmapFlagsXbox.None;
            image.Flags = bitmap.Flags;
            image.Curve = bitmap.Curve;

            if (image.Format == BitmapFormat.Dxn)
                image.Flags |= BitmapFlags.Unknown3;

        }

        public static bool IsBitmapResourceValid(GameCache cache, Bitmap bitmap)
        {
            return bitmap.HardwareTextures.All(cache.ResourceCache.IsResourceValid) &&
                   bitmap.InterleavedHardwareTextures.All(cache.ResourceCache.IsResourceValid);
        }


        /// <summary>
        /// When converting xbox bitmap formats (and other rare formats), get the standard format that it can be converted it without loss
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static BitmapFormat GetEquivalentBitmapFormat(BitmapFormat format)
        {
            switch (format)
            {
                case BitmapFormat.Dxn:
                case BitmapFormat.Ctx1:
                    return BitmapFormat.Dxn;

                case BitmapFormat.AY8:
                    return BitmapFormat.A8Y8;

                case BitmapFormat.Dxt5aAlpha:
                case BitmapFormat.Dxt3aAlpha:
                    return BitmapFormat.A8;

                case BitmapFormat.Dxt5aMono:
                case BitmapFormat.Dxt3aMono:
                case BitmapFormat.L16: // lossy
                    return BitmapFormat.Y8;

                case BitmapFormat.Q8W8V8U8:
                case BitmapFormat.DxnMonoAlpha:
                case BitmapFormat.Dxt5a:
                case BitmapFormat.Dxt3a:
                    return BitmapFormat.A8R8G8B8;

                default:
                    return format;
            }
        }

        public static bool IsNormalMap(Bitmap bitmap, int imageIndex)
        {
            switch (bitmap.Images[imageIndex].Format)
            {
                case BitmapFormat.Ctx1:
                case BitmapFormat.Dxn:
                case BitmapFormat.Dxt5nm:
                    return true;
            }

            Bitmap.BitmapUsageFormat usageFormat = GetBitmapUsageFormat(bitmap);
            if (usageFormat != Bitmap.BitmapUsageFormat.UseDefaultDefinedByUsage)
            {
                switch (usageFormat)
                {
                    case Bitmap.BitmapUsageFormat.BestCompressedBumpFormat:
                    case Bitmap.BitmapUsageFormat.BestUncompressedBumpFormat:
                    case Bitmap.BitmapUsageFormat.NormalMapFormats:
                    case Bitmap.BitmapUsageFormat.Ctx1CompressedNormalsSmaller:
                    case Bitmap.BitmapUsageFormat.DxnCompressedNormalsBetter:
                    case Bitmap.BitmapUsageFormat._16BitNormals:
                    case Bitmap.BitmapUsageFormat._32BitNormals:
                    case Bitmap.BitmapUsageFormat._8Bit4ChannelVector:
                        return true;
                }
            }
            else
            {
                switch (bitmap.Usage)
                {
                    case Bitmap.BitmapUsageGlobalEnum.BumpMapfromHeightMap:
                    case Bitmap.BitmapUsageGlobalEnum.ZBrushBumpMapfromBumpMap:
                    case Bitmap.BitmapUsageGlobalEnum.NormalMapAkaZbump:
                    case Bitmap.BitmapUsageGlobalEnum.DetailZbrushBumpMap:
                    case Bitmap.BitmapUsageGlobalEnum.DetailNormalMap:
                    case Bitmap.BitmapUsageGlobalEnum.WarpMapEMBM:
                        return true;
                }
            }

            return false;
        }

        public static bool IsCompressedFormat(BitmapFormat format)
        {
            switch (format)
            {
                case BitmapFormat.Dxt1:
                case BitmapFormat.Dxt3:
                case BitmapFormat.Dxt5:
                case BitmapFormat.Dxt3a:
                case BitmapFormat.Dxt3aMono:
                case BitmapFormat.Dxt3aAlpha:
                case BitmapFormat.Dxt3A1111:
                case BitmapFormat.Dxt5a:
                case BitmapFormat.Dxt5aMono:
                case BitmapFormat.Dxt5aAlpha:
                case BitmapFormat.Dxt5nm:
                case BitmapFormat.Dxn:
                case BitmapFormat.DxnMonoAlpha:
                case BitmapFormat.Ctx1:
                    return true;
            }
            return false;
        }

        public static IEnumerable<(int layerIndex, int mipLevel)> GetBitmapSurfacesEnumerable(int layerCount, int mipLevelCount, bool forDDS)
        {
            if (!forDDS)
            {
                // order for d3d9, all faces first, then mipmaps
                for (int mipLevel = 0; mipLevel < mipLevelCount; mipLevel++)
                    for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
                        yield return (layerIndex, mipLevel);
            }
            else
            {
                for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
                    for (int mipLevel = 0; mipLevel < mipLevelCount; mipLevel++)
                        yield return (layerIndex, mipLevel);
            }
        }

        public static int GetMipmapCount(int width, int height, int maxCount = int.MaxValue)
        {
            int count = 1; // include the base level
            while (count < maxCount && (width > 1 || height > 1))
            {
                width = Math.Max(1, width / 2);
                height = Math.Max(1, height / 2);
                count++;
            }
            return count;
        }

        public static int GetMipmapCountTruncate(int width, int height, int minWidth = 1, int minHeight = 1, int maxCount = int.MaxValue)
        {
            int count = 1; // include the base level
            while (count < maxCount && (width > minWidth && height > minHeight))
            {
                width = Math.Max(1, width / 2);
                height = Math.Max(1, height / 2);
                count++;
            }
            return count;
        }

        public static void TrimLowestMipmaps(BaseBitmap resultBitmap)
        {
            int dataSize = 0;
            int mipMapCount;
            int width = resultBitmap.Width;
            int height = resultBitmap.Height;
            for (mipMapCount = 0; mipMapCount < resultBitmap.MipMapCount; mipMapCount++)
            {
                if (width < 4 || height < 4)
                    break;

                dataSize += RoundSize(width, 4) * RoundSize(height, 4);
                width /= 2;
                height /= 2;
            }

            resultBitmap.MipMapCount = mipMapCount;
            byte[] raw = new byte[dataSize];
            Array.Copy(resultBitmap.Data, raw, dataSize);
            resultBitmap.Data = raw;
        }

        public static Bitmap.BitmapUsageFormat GetBitmapUsageFormat(Bitmap bitmap)
        {
            if (bitmap.ForceBitmapFormat != Bitmap.BitmapUsageFormat.UseDefaultDefinedByUsage)
                return bitmap.ForceBitmapFormat;

            if (bitmap.UsageOverrides.Count > 0)
                return bitmap.UsageOverrides[0].BitmapFormat;

            return Bitmap.BitmapUsageFormat.UseDefaultDefinedByUsage;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalculateNormalZ(float x, float y)
        {
            return MathF.Sqrt(Math.Clamp(1f - (x * x) - (y * y), 0f, 1f));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte CalculateNormalZ(byte r, byte g)
        {
            float x = (r / 127.5f) - 1f;
            float y = (g / 127.5f) - 1f;
            float z = CalculateNormalZ(x, y);
            return (byte)((z + 1f) * 127.5f + 0.5f);
        }
    }
}
