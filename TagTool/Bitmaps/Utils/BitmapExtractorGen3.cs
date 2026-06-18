using System;
using TagTool.Cache;
using TagTool.Tags.Definitions;
using TagTool.Tags.Resources;

namespace TagTool.Bitmaps.Utils
{
    public interface IBitmapExtractor
    {
        // TODO: consider returning a BaseBitmap
        byte[] ExtractSurface(int layerIndex, int mipIndex);
    }

    public class BitmapExtractorGen3 : IBitmapExtractor
    {
        private readonly GameCache Cache;
        private readonly byte[] PrimaryData;
        private readonly byte[] SecondaryData;
        private BitmapTextureInteropResource.BitmapDefinition InteropDefinition;
        private readonly BitmapTextureInteropDefinition Definition;
        private readonly BitmapTextureInteropDefinition OtherDefinition;
        private readonly int PairIndex;
        private readonly bool IsPaired;
        private readonly Bitmap Bitmap;
        private readonly int ImageIndex;

        public bool HasInvalidResource => PrimaryData == null && SecondaryData == null;

        public BitmapExtractorGen3(GameCache cache, Bitmap bitmap, int imageIndex)
        {
            Cache = cache;
            Bitmap = bitmap;
            ImageIndex = imageIndex;

            var image = bitmap.Images[imageIndex];
            if (image.XboxFlags.HasFlag(BitmapFlagsXbox.Xbox360UseInterleavedTextures))
            {
                BitmapTextureInterleavedInteropResource resource = cache.ResourceCache.GetBitmapTextureInterleavedInteropResource(bitmap.InterleavedHardwareTextures[image.InterleavedInterop]);
                if (resource == null)
                    return;

                Definition = resource.Texture.Definition.Bitmap1;
                OtherDefinition = resource.Texture.Definition.Bitmap2;
                PrimaryData = resource.Texture.Definition.PrimaryResourceData.Data;
                SecondaryData = resource.Texture.Definition.SecondaryResourceData.Data;
                PairIndex = image.InterleavedTextureIndex;
                IsPaired = true;
            }
            else
            {
                BitmapTextureInteropResource resource = cache.ResourceCache.GetBitmapTextureInteropResource(bitmap.HardwareTextures[imageIndex]);
                if (resource == null)
                    return;

                InteropDefinition = resource.Texture.Definition;
                Definition = resource.Texture.Definition.Bitmap;
                PrimaryData = resource.Texture.Definition.PrimaryResourceData.Data;
                SecondaryData = resource.Texture.Definition.SecondaryResourceData.Data;
            }
        }

        public byte[] ExtractSurface(int layerIndex, int mipIndex, out int width, out int height)
        {
            if (HasInvalidResource)
            {
                width = height = 0;
                return null;
            }

            var image = Bitmap.Images[ImageIndex];
            width = Math.Max(1, image.Width >> mipIndex);
            height = Math.Max(1, image.Height >> mipIndex);

            return ExtractSurface(layerIndex, mipIndex);
        }

        public byte[] ExtractSurface(int layerIndex, int mipIndex)
        {
            if (HasInvalidResource)
                return null;

            if (Cache.Platform == CachePlatform.MCC)
            {
                return BitmapUtilsPC.GetBitmapLevelData(InteropDefinition, Bitmap, ImageIndex, mipIndex, layerIndex);
            }
            else
            {
                return XboxBitmapUtils.GetXboxBitmapLevelData(PrimaryData, SecondaryData, Definition, mipIndex, layerIndex, IsPaired, PairIndex, OtherDefinition);
            }
        }
    }
}
