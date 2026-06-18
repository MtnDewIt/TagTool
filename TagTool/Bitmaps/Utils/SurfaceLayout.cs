using System;
using System.Collections.Generic;

namespace TagTool.Bitmaps.Utils
{
    public static class SurfaceLayout
    {
        public static Dictionary<SurfaceKey, SurfaceDesc> BuildLayout(BitmapType type, BitmapFormat format, int width, int height, int depth, int mipCount, LayoutOptions options)
        {
            var surfaces = new Dictionary<SurfaceKey, SurfaceDesc>();

            foreach (var surface in EnumerateSurfaces(type, format, width, height, depth, mipCount, options))
            {
                surfaces.Add(new(surface.Layer, surface.Mip), surface);
            }

            return surfaces;
        }

        public static IEnumerable<SurfaceDesc> EnumerateSurfaces(BitmapType type, BitmapFormat format, int width, int height, int depth, int mipCount, LayoutOptions options)
        {
            if (type == BitmapType.CubeMap)
                return EnumerateSurfacesCube(format, width, height, depth, mipCount, options);

            if (type == BitmapType.Texture3D)
                return EnumerateSurfaces3D(format, width, height, depth, mipCount, options);

            if (type == BitmapType.Texture2D)
                return EnumerateSurfaces2D(format, width, height, depth, mipCount, options);

            throw new NotImplementedException();
        }

        static IEnumerable<SurfaceDesc> EnumerateSurfaces2D(BitmapFormat format, int width, int height, int depth, int mipCount, LayoutOptions options)
        {
            int offset = 0;
            int bpp = BitmapFormatUtils.GetBitsPerPixel(format);
            int blockAlign = BitmapUtils.IsCompressedFormat(format) ? 4 : 1;

            for (int mip = 0; mip < mipCount; mip++)
            {
                int mipWidth = Math.Max(blockAlign, width >> mip);
                int mipHeight = Math.Max(blockAlign, height >> mip);

                int size = mipWidth * mipHeight * bpp / 8;

                yield return new SurfaceDesc()
                {
                    Width = Math.Max(1, width >> mip),
                    Height = Math.Max(1, height >> mip),
                    Layer = 0,
                    Mip = mip,
                    Offset = offset,
                    Size = size
                };

                offset += size;
            }
        }

        static IEnumerable<SurfaceDesc> EnumerateSurfaces3D(BitmapFormat format, int width, int height, int depth, int mipCount, LayoutOptions options)
        {
            int offset = 0;
            int bpp = BitmapFormatUtils.GetBitsPerPixel(format);
            int blockAlign = BitmapUtils.IsCompressedFormat(format) ? 4 : 1;

            for (int mip = 0; mip < mipCount; mip++)
            {
                int mipWidth = Math.Max(blockAlign, width >> mip);
                int mipHeight = Math.Max(blockAlign, height >> mip);
                int mipDepth = Math.Max(blockAlign, depth >> mip);

                int size = mipWidth * mipHeight * bpp / 8;

                for (int slice = 0; slice < mipDepth; slice++)
                {
                    yield return new SurfaceDesc()
                    {
                        Width = Math.Max(1, width >> mip),
                        Height = Math.Max(1, height >> mip),
                        Layer = slice,
                        Mip = mip,
                        Offset = offset,
                        Size = size
                    };

                    offset += size;
                }
            }
        }

        static IEnumerable<SurfaceDesc> EnumerateSurfacesCube(BitmapFormat format, int width, int height, int depth, int mipCount, LayoutOptions options)
        {
            int offset = 0;
            int bpp = BitmapFormatUtils.GetBitsPerPixel(format);
            int blockAlign = BitmapUtils.IsCompressedFormat(format) ? 4 : 1;

            if (options.CubeLayout == CubeMapLayout.MipMajor)
            {
                for (int mip = 0; mip < mipCount; mip++)
                {
                    int mipWidth = Math.Max(blockAlign, width >> mip);
                    int mipHeight = Math.Max(blockAlign, height >> mip);

                    for (int face = 0; face < 6; face++)
                    {
                        int adjustedFace = options.FaceOrder[face];

                        int size = mipWidth * mipHeight * bpp / 8;

                        yield return new SurfaceDesc()
                        {
                            Width = Math.Max(1, width >> mip),
                            Height = Math.Max(1, height >> mip),
                            Layer = adjustedFace,
                            Mip = mip,
                            Offset = offset,
                            Size = size
                        };

                        offset += size;
                    }

                    offset += -offset & (options.FaceAlignBytes - 1);
                }
            }
            else
            {
                for (int face = 0; face < 6; face++)
                {
                    int adjustedFace = options.FaceOrder[face];

                    int mipWidth = Math.Max(blockAlign, width);
                    int mipHeight = Math.Max(blockAlign, height);

                    for (int mip = 0; mip < mipCount; mip++)
                    {
                        int size = mipWidth * mipHeight * bpp / 8;

                        yield return new SurfaceDesc()
                        {
                            Width = Math.Max(1, width >> mip),
                            Height = Math.Max(1, height >> mip),
                            Layer = adjustedFace,
                            Mip = mip,
                            Offset = offset,
                            Size = size
                        };

                        offset += size;

                        mipWidth = Math.Max(blockAlign, mipWidth >> 1);
                        mipHeight = Math.Max(blockAlign, mipHeight >> 1);
                    }

                    offset += -offset & (options.FaceAlignBytes - 1);
                }
            }
        }

        public enum CubeMapLayout
        {
            MipMajor,
            FaceMajor
        }

        public readonly record struct SurfaceKey(int Layer, int Mip);

        public readonly record struct SurfaceDesc
        {
            public required int Width { get; init; }
            public required int Height { get; init; }
            public required int Offset { get; init; }
            public required int Size { get; init; }
            public required int Mip { get; init; }
            public required int Layer { get; init; }
        }

        public class LayoutOptions
        {
            public static readonly LayoutOptions Default = new();

            public CubeMapLayout CubeLayout { get; set; } = CubeMapLayout.MipMajor;
            public int[] FaceOrder { get; set; } = [0, 1, 2, 3, 4, 5];
            public int FaceAlignBytes { get; set; } = 1;
        }
    }
}
