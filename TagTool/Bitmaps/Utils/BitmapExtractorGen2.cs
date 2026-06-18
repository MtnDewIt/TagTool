using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using TagTool.Cache;
using TagTool.IO;
using TagTool.Tags.Definitions.Gen2;
using static TagTool.Bitmaps.Utils.SurfaceLayout;
using static TagTool.Tags.Definitions.Gen2.Bitmap;
using BitmapFlagsGen2 = TagTool.Tags.Definitions.Gen2.Bitmap.BitmapDataBlock.FlagsValue;
using BitmapTypeGen2 = TagTool.Tags.Definitions.Gen2.Bitmap.BitmapDataBlock.TypeValue;

namespace TagTool.Bitmaps.Utils
{
    public class BitmapExtractorGen2 : IBitmapExtractor
    {
        private readonly GameCacheGen2 Cache;
        private readonly BitmapDataBlock Bitmap;
        private byte[] Data;
        private readonly int FaceAlignBytes;

        private readonly Dictionary<SurfaceKey, SurfaceDesc> Surfaces;

        public BitmapExtractorGen2(GameCacheGen2 cache, Bitmap bitmap, int imageIndex)
        {
            Cache = cache;
            Bitmap = bitmap.Bitmaps[imageIndex];
            FaceAlignBytes = Cache.Version == CacheVersion.Halo2PC ? 1 : 128;
            Data = ExtractData();

            var type = BitmapUtilsGen2.MapBitmapType(Bitmap.Type);
            var format = BitmapUtilsGen2.MapBitmapFormat(Bitmap.Format);

            Surfaces = BuildLayout(type, format, Bitmap.Width, Bitmap.Height, Bitmap.Depth, Bitmap.MipmapCount + 1,
                new LayoutOptions
                {
                    FaceAlignBytes = FaceAlignBytes,
                    CubeLayout = cache.Version == CacheVersion.Halo2PC ? CubeMapLayout.MipMajor : CubeMapLayout.FaceMajor,
                    FaceOrder = cache.Version == CacheVersion.Halo2PC ? [0, 1, 2, 3, 4, 5] : [0, 2, 1, 3, 4, 5]
                });
        }

        public byte[] ExtractSurface(int layerIndex, int mipIndex)
        {
            if (Surfaces.TryGetValue(new(layerIndex, mipIndex), out var src))
            {
                return Data.AsSpan(src.Offset, src.Size).ToArray();
            }

            return null;
        }

        private byte[] ExtractData()
        {
            int rowPitchAlignBytes = Cache.Version == CacheVersion.Halo2PC ? 1 : 64;

            //get raw bitmap data and create resource
            byte[] rawData = Cache.GetCacheRawData((uint)Bitmap.Lod0Pointer, Bitmap.Lod0Size);

            if (Cache.Version == CacheVersion.Halo2PC)
            {
                //h2v raw bitmap data is gz compressed
                rawData = Decompress(rawData);
            }

            if (Bitmap.Flags.HasFlag(BitmapFlagsGen2.Linear))
            {
                rawData = Delinearize(Bitmap, rawData, rowPitchAlignBytes);
            }
            else if (Bitmap.Flags.HasFlag(BitmapFlagsGen2.Swizzled))
            {
                rawData = Deswizzle(Bitmap, rawData, FaceAlignBytes);
            }

            return rawData;
        }

        static byte[] Decompress(byte[] rawData)
        {
            using var stream = new MemoryStream(rawData);
            using var resultStream = new MemoryStream();
            using var zstream = new ZLibStream(stream, CompressionMode.Decompress);
            zstream.CopyTo(resultStream);
            return resultStream.ToArray();
        }

        private static byte[] Delinearize(BitmapDataBlock bitmap, byte[] rawData, int rowPitchAlignBytes)
        {
            if (bitmap.Flags.HasFlag(BitmapFlagsGen2.Swizzled))
                throw new ArgumentException("Linear bitmaps should not be swizzled");
            if (bitmap.Flags.HasFlag(BitmapFlagsGen2.Compressed))
                throw new ArgumentException("Linear bitmaps should not be compressed");

            int faceCount = bitmap.Type == BitmapTypeGen2.CubeMap ? 6 : 1;

            var ms = new MemoryStream();
            int offset = 0;

            for (int face = 0; face < faceCount; face++)
            {
                int rowPitch = BitmapUtilsGen2.GetMipmapRowPitch(bitmap, 0);
                byte[] buffer = new byte[rowPitch * bitmap.Height];
                int alignedRowPitch = (rowPitch + (rowPitchAlignBytes - 1)) & ~(rowPitchAlignBytes - 1);

                for (int y = 0; y < bitmap.Height; y++)
                    Buffer.BlockCopy(rawData, offset + y * alignedRowPitch, buffer, y * rowPitch, rowPitch);

                ms.Write(buffer);

                offset += alignedRowPitch;
                offset += -offset & 127;
            }

            bitmap.Flags &= ~BitmapFlagsGen2.Linear;

            return ms.ToArray();
        }

        private static byte[] Deswizzle(BitmapDataBlock bitmap, byte[] rawData, int faceAlignBytes)
        {
            int faceCount = bitmap.Type == BitmapTypeGen2.CubeMap ? 6 : 1;
            int mipCount = bitmap.MipmapCount + 1;

            var resultStream = new MemoryStream();

            int offset = 0;
            for (int face = 0; face < faceCount; face++)
            {
                for (int mipIndex = 0; mipIndex < mipCount; mipIndex++)
                {
                    int mipSize = BitmapUtilsGen2.GetMipmapPixelDataSize(bitmap, mipIndex);

                    if (bitmap.Type == BitmapTypeGen2.CubeMap)
                    {
                        mipSize /= 6;
                    }

                    if (offset > rawData.Length - mipSize)
                        break;

                    int width = BitmapUtilsGen2.GetMipmapWidth(bitmap, mipIndex);
                    int height = BitmapUtilsGen2.GetMipmapHeight(bitmap, mipIndex);
                    int depth = BitmapUtilsGen2.GetMipmapDepth(bitmap, mipIndex);
                    int bpp = BitmapUtilsGen2.GetBitsPerPixel(bitmap.Format);

                    if (bitmap.Flags.HasFlag(BitmapFlagsGen2.Swizzled))
                        resultStream.Write(XboxSwizzler.Swizzle(rawData.AsSpan(offset, mipSize), width, height, depth, bpp / 8, deswizzle: true));
                    else
                        resultStream.Write(rawData.AsSpan(offset, mipSize));

                    offset += mipSize;
                }

                int pad = -offset & (faceAlignBytes - 1);
                StreamUtil.Fill(resultStream, 0, pad);
                offset += pad;
            }

            return resultStream.ToArray();
        }
    }
}
