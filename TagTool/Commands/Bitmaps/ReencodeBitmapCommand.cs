using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TagTool.Bitmaps;
using TagTool.Bitmaps.Utils;
using TagTool.Cache;
using TagTool.Commands.Common;
using TagTool.Common.Logging;
using TagTool.Tags.Definitions;
using TagTool.Tags.Resources;

namespace TagTool.Commands.Bitmaps
{
    internal class ReencodeBitmapCommand : Command
    {
        private GameCache Cache { get; }
        private CachedTag Tag { get; }
        private Bitmap Bitmap { get; }

        public ReencodeBitmapCommand(GameCache cache, CachedTag tag, Bitmap bitmap)
            : base(false,

                  "ReencodeBitmap",
                  "Re-encodes the current bitmap as the specified format.",

                  "ReencodeBitmap <format> [image index]",
                  $"Valid format types are: {string.Join(", ", Enum.GetNames(typeof(BitmapFormat)))}\n")
        {
            Cache = cache;
            Tag = tag;
            Bitmap = bitmap;
        }

        public override object Execute(List<string> args)
        {
            // parse args
            if (!Enum.TryParse<BitmapFormat>(args[0], ignoreCase: true, out BitmapFormat destFormat))
                return new TagToolError(CommandError.ArgInvalid, $"\"{args[0]}\" is not a valid bitmap format");
            int imageIndex = 0;
            if (args.Count > 1 && (!int.TryParse(args[1], out imageIndex) || imageIndex >= Bitmap.Images.Count))
                return new TagToolError(CommandError.ArgInvalid, $"\"{args[1]}\" is not a valid image index");

            var resourceDefinition = Cache.ResourceCache.GetBitmapTextureInteropResource(Bitmap.HardwareTextures[imageIndex]);
            if (resourceDefinition == null)
            {
                Log.Warning("No bitmap resource");
                return true;
            }

            BaseBitmap resultBitmap = ReEncodeBitmap(Bitmap, imageIndex, resourceDefinition, destFormat, shouldGenerateMips: true);

            BitmapUtils.SetBitmapImageData(resultBitmap, Bitmap.Images[imageIndex]);

            // write to new resource

            BitmapTextureInteropResource newResourceDefinition = BitmapUtils.CreateBitmapTextureInteropResource(resultBitmap);
            var resourceRef = Bitmap.HardwareTextures[imageIndex];
            var hoCache = (GameCacheHaloOnlineBase)Cache;
            hoCache.ResourceCaches.ReplaceResource(resourceRef.HaloOnlinePageableResource, newResourceDefinition);
            return true;
        }

        private BaseBitmap ReEncodeBitmap(Bitmap bitmap, int imageIndex, BitmapTextureInteropResource resourceDefinition, BitmapFormat destFormat, bool shouldGenerateMips)
        {
            Bitmap.Image image = bitmap.Images[imageIndex];

            // for d3d9 dxn mips need to be >= 4x4 to avoid crashes
            int mipCount = destFormat == BitmapFormat.Dxn
                ? BitmapUtils.GetMipmapCountTruncate(image.Width, image.Height, 4, 4)
                : BitmapUtils.GetMipmapCount(image.Width, image.Height);

            int layerCount = image.Type == BitmapType.CubeMap ? 6 : image.Depth;

            // for each layer extract the base level and generate mips

            var newSurfaces = new List<MipMap>();
            for (int layerIndex = 0; layerIndex < layerCount; layerIndex++)
            {
                byte[] baseLevelData = BitmapUtilsPC.GetBitmapLevelData(resourceDefinition.Texture.Definition, Bitmap, imageIndex, 0, layerIndex);
                baseLevelData = BitmapDecoder.DecodeBitmap(baseLevelData, image.Format, image.Width, image.Height, Cache.Version, Cache.Platform);

                var mipGenerator = new MipMapGenerator();
                mipGenerator.GenerateMipMap(image.Width, image.Height, baseLevelData, 4, mipCount);
                Debug.Assert(mipCount == mipGenerator.MipMaps.Count + 1);

                // append the base level to the list of surfaces
                newSurfaces.Add(new MipMap(baseLevelData, image.Width, image.Height));
                // append the mips to the list of surfaces
                newSurfaces.AddRange(mipGenerator.MipMaps);
            }

            // re-encode the surface and append to the result data in the correct order

            var result = new MemoryStream();
            foreach (var (layerIndex, mipLevel) in BitmapUtils.GetBitmapSurfacesEnumerable(layerCount, mipCount, forDDS: false))
            {
                MipMap surface = newSurfaces[layerIndex * mipCount + mipLevel];
                byte[] encoded = BitmapDecoder.EncodeBitmap(surface.Data, destFormat, surface.Width, surface.Height);
                result.Write(encoded, 0, encoded.Length);
            }

            // rebuld the result bitmap

            BaseBitmap resultBitmap = new BaseBitmap(image, result.ToArray());
            resultBitmap.MipMapCount = mipCount;
            resultBitmap.UpdateFormat(destFormat);

            return resultBitmap;
        }
    }
}
