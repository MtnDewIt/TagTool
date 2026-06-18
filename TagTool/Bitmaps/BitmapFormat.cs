using System;
using TagTool.Tags;

namespace TagTool.Bitmaps
{
    /// <summary>
    /// Bitmap data formats.
    /// </summary>
    [TagEnum(IsVersioned = true)]
    public enum BitmapFormat
    {
        A8,
        Y8,
        AY8,
        A8Y8,
        R8,
        Unused2,
        R5G6B5,
        Unused3,
        A1R5G5B5,
        A4R4G4B4,
        X8R8G8B8,
        A8R8G8B8,
        Unused4,
        Dxt5nm,
        Dxt1,
        Dxt3,
        Dxt5,
        [TagEnumMember(Gen = Cache.CacheGeneration.Second)]
        P8Bump,
        [TagEnumMember(MinVersion = Cache.CacheVersion.Halo3Beta)]
        A4R4G4B4Font,
        P8, // gen2 only
        Argbfp32, // gen2 only
        Rgbfp32,
        Rgbfp16, // gen2 only
        V8U8,
        G8B8,
        Abgrfp32,
        Abgrfp16,
        [TagEnumMember(MinVersion = Cache.CacheVersion.HaloReach)]
        F16Mono,
        [TagEnumMember(MinVersion = Cache.CacheVersion.HaloReach)]
        F16Red,
        Q8W8V8U8,
        A2R10G10B10,
        A16B16G16R16,
        V16U16,
        [TagEnumMember(MinVersion = Cache.CacheVersion.HaloReach)]
        L16,
        [TagEnumMember(MinVersion = Cache.CacheVersion.HaloReach)]
        R16G16,
        [TagEnumMember(MinVersion = Cache.CacheVersion.HaloReach)]
        SignedR16G16B16A16,
        Dxt3a,
        Dxt5a,
        Dxt3A1111,
        Dxn,
        Ctx1,
        Dxt3aAlpha,
        Dxt3aMono,
        Dxt5aAlpha,
        Dxt5aMono,
        DxnMonoAlpha,
        [TagEnumMember(MinVersion = Cache.CacheVersion.HaloReach)]
        Dxt5Red,
        [TagEnumMember(MinVersion = Cache.CacheVersion.HaloReach)]
        Dxt5Green,
        [TagEnumMember(MinVersion = Cache.CacheVersion.HaloReach)]
        Dxt5Blue,
        [TagEnumMember(MinVersion = Cache.CacheVersion.HaloReach)]
        Depth24,
    }

    public static class BitmapFormatUtils
    {
        /// <summary>
        /// Get the number of bits per pixel of a bitmap format.
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static int GetBitsPerPixel(BitmapFormat format)
        {
            switch (format)
            {
                case BitmapFormat.Dxt1:
                case BitmapFormat.Dxt3a:
                case BitmapFormat.Dxt5a:
                case BitmapFormat.Dxt3A1111:
                case BitmapFormat.Ctx1:
                case BitmapFormat.Dxt3aAlpha:
                case BitmapFormat.Dxt3aMono:
                case BitmapFormat.Dxt5aAlpha:
                case BitmapFormat.Dxt5aMono:
                    return 4;

                case BitmapFormat.A8:
                case BitmapFormat.Y8:
                case BitmapFormat.AY8:
                case BitmapFormat.R8:
                case BitmapFormat.P8:
                case BitmapFormat.P8Bump:
                case BitmapFormat.Dxt5nm:
                case BitmapFormat.Dxt3:
                case BitmapFormat.Dxt5:
                case BitmapFormat.Dxn:
                case BitmapFormat.DxnMonoAlpha:
                case BitmapFormat.Dxt5Red:
                case BitmapFormat.Dxt5Green:
                case BitmapFormat.Dxt5Blue:
                    return 8;

                case BitmapFormat.A8Y8:
                case BitmapFormat.R5G6B5:
                case BitmapFormat.A1R5G5B5:
                case BitmapFormat.A4R4G4B4:
                case BitmapFormat.A4R4G4B4Font:
                case BitmapFormat.V8U8:
                case BitmapFormat.G8B8:
                case BitmapFormat.F16Mono:
                case BitmapFormat.F16Red:
                case BitmapFormat.L16:
                    return 16;

                case BitmapFormat.X8R8G8B8:
                case BitmapFormat.A8R8G8B8:
                case BitmapFormat.Q8W8V8U8:
                case BitmapFormat.A2R10G10B10:
                case BitmapFormat.V16U16:
                case BitmapFormat.R16G16:
                case BitmapFormat.Depth24:
                    return 32;

                case BitmapFormat.Rgbfp16:
                    return 48;

                case BitmapFormat.Abgrfp16:
                case BitmapFormat.A16B16G16R16:
                case BitmapFormat.SignedR16G16B16A16:
                    return 64;

                case BitmapFormat.Rgbfp32:
                    return 96;

                case BitmapFormat.Argbfp32:
                case BitmapFormat.Abgrfp32:
                    return 128;

                default:
                    throw new ArgumentOutOfRangeException(nameof(format), format, null);
            }
        }

        /// <summary>
        /// Get the size in bytes of a block compressed format.
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static int GetBlockSize(BitmapFormat format)
        {
            int blockSize = 0;
            switch (format)
            {
                case BitmapFormat.Dxt1:
                case BitmapFormat.Dxt3a:
                case BitmapFormat.Dxt3aMono:
                case BitmapFormat.Dxt3aAlpha:
                case BitmapFormat.Dxt3A1111:
                case BitmapFormat.Dxt5a:
                case BitmapFormat.Dxt5aAlpha:
                case BitmapFormat.Dxt5aMono:
                case BitmapFormat.Ctx1:
                    blockSize = 8;
                    break;
                case BitmapFormat.Dxt3:
                case BitmapFormat.Dxt5:
                case BitmapFormat.Dxt5nm:
                case BitmapFormat.Dxn:
                case BitmapFormat.DxnMonoAlpha:
                    blockSize = 16;
                    break;
                default:
                    blockSize = -1;
                    break;
            }
            return blockSize;
        }

        /// <summary>
        /// Get the dimension of a block in block compressed formats.
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static int GetBlockDimension(BitmapFormat format)
        {
            return BitmapUtils.IsCompressedFormat(format) ? 4 : 1;
        }
    }
}
