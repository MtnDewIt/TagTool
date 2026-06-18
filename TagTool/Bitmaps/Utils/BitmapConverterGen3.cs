using System.IO;
using TagTool.Cache;
using TagTool.Common.Logging;
using TagTool.Tags.Definitions;

namespace TagTool.Bitmaps.Utils
{
    public enum BitmapConverterMode
    {
        None,
        DiffuseToNormal
    }

    public class BitmapConverterGen3
    {
        public GameCache Cache { get; }
        public bool HqNormalMapCompression { get; set; }
        public bool ForceDxt5nm { get; set; }
        public BitmapConverterMode Mode { get; set; }
        public bool AllowOptimization { get; set; } = true;

        public BitmapConverterGen3(GameCache cache)
        {
            Cache = cache;
        }

        public BaseBitmap ConvertBitmap(Bitmap bitmap, int imageIndex, string tagName, bool forDDS = false)
        {
            var extractor = new BitmapExtractorGen3(Cache, bitmap, imageIndex);
            if (extractor.HasInvalidResource)
                return null;

            Bitmap.Image image = bitmap.Images[imageIndex];

            PipelineBuilder pipeline = CreateConverterPipeline(tagName, bitmap, imageIndex);
            pipeline.ApplyConstraints(image.Width, image.Height);

            if (AllowOptimization)
                pipeline.ApplyOptimization(extractor, image.Type, image.Width, image.Height, BitmapUtils.GetBitmapUsageFormat(bitmap));

            ConverterDelegate converter = pipeline.Build();
            BitmapFormat destFormat = pipeline.DestFormat;

            int mipCount = image.MipmapCount + 1;
            int layerCount = image.Type == BitmapType.CubeMap ? 6 : image.Depth;
            bool swapCubemapFaces = image.Type == BitmapType.CubeMap && Cache.Platform != CachePlatform.MCC;

            // array bitmaps will be converted to texture3d and thus can't have mipmaps unfortunately
            if (image.Type == BitmapType.Array && mipCount > 1)
                mipCount = 1;
            // for d3d9 dxn mips have to be >= 4x4 to avoid a crash
            if (destFormat == BitmapFormat.Dxn)
                mipCount = BitmapUtils.GetMipmapCountTruncate(image.Width, image.Height, 4, 4, maxCount: mipCount);

            // convert the surfaces
            var result = new MemoryStream();
            foreach (var (layerIndex, mipLevel) in BitmapUtils.GetBitmapSurfacesEnumerable(layerCount, mipCount, forDDS))
            {
                int sourceLayerIndex = layerIndex;
                if (swapCubemapFaces)
                {
                    if (layerIndex == 1) sourceLayerIndex = 2;
                    else if (layerIndex == 2) sourceLayerIndex = 1;
                }

                byte[] surface = extractor.ExtractSurface(sourceLayerIndex, mipLevel, out int levelWidth, out int levelHeight);
                surface = converter?.Invoke(surface, levelWidth, levelHeight);
                result.Write(surface);
            }

            // build the result bitmap
            BaseBitmap resultBitmap = new BaseBitmap(image);
            resultBitmap.Data = result.ToArray();
            resultBitmap.MipMapCount = mipCount;
            if (resultBitmap.Type == BitmapType.Array)
                resultBitmap.Type = BitmapType.Texture3D;

            if (Mode == BitmapConverterMode.DiffuseToNormal)
            {
                bitmap.Usage = Bitmap.BitmapUsageGlobalEnum.BumpMapfromHeightMap;
                bitmap.UsageOverrides.Clear();
                bitmap.ForceBitmapFormat = Bitmap.BitmapUsageFormat.UseDefaultDefinedByUsage;
                resultBitmap.Curve = BitmapImageCurve.Linear;
            }

            resultBitmap.UpdateFormat(destFormat);

            return resultBitmap;
        }

        private PipelineBuilder CreateConverterPipeline(string tagName, Bitmap bitmap, int imageIndex)
        {
            Bitmap.Image image = bitmap.Images[imageIndex];
            BitmapFormat format = image.Format;

            BitmapFormat destFormat = (BitmapUtils.IsNormalMap(bitmap, imageIndex) || Mode == BitmapConverterMode.DiffuseToNormal)
                ? GetNormalMapFormat(format)
                : BitmapUtils.GetEquivalentBitmapFormat(format);

            var pipeline = new PipelineBuilder(tagName, image.Format, destFormat, Cache.Version, Cache.Platform)
            {
                CompressionQuality = HqNormalMapCompression ? CompressionQuality.High : CompressionQuality.Default
            };

            // Direct conversions
            if (format == destFormat)
            {
                switch (format)
                {
                    case BitmapFormat.A2R10G10B10 when Cache.Platform == CachePlatform.MCC:
                        pipeline.Transform(BitmapConversionHelpers.Convert_RGB10A2_To_A2RGB10);
                        break;
                    case BitmapFormat.Dxn when Cache.Platform == CachePlatform.MCC:
                        pipeline.Transform(BitmapConversionHelpers.Convert_BC5SNorm_To_ATI2);
                        break;
                    case BitmapFormat.Dxn when Cache.Platform == CachePlatform.Original:
                        pipeline.Transform(BitmapDecoder.SwapXYDxn);
                        break;
                }
                pipeline.Platform = CachePlatform.Original;
                pipeline.Version = CacheVersion.HaloOnlineED;
            }

            // array textures will be converted to texture3d which doesn't support v8u8
            if (bitmap.Usage == Bitmap.BitmapUsageGlobalEnum.WaterArray)
            {
                pipeline.DestFormat = BitmapFormat.A8R8G8B8;
            }

            if ((format == BitmapFormat.Dxt5 && tagName == @"objects\vehicles\wraith\bitmaps\wraith_bump") || Mode == BitmapConverterMode.DiffuseToNormal)
            {
                pipeline.TransformPixels((data, _, _) => BitmapConversionHelpers.Diffuse_To_Normal(data, image.Curve));
            }

            // cubemap compatibility - this is required for h3 shaders to look correct when using reach dynamic cubemaps
            if (Cache.Version >= CacheVersion.HaloReach && image.ExponentBias == 2)
            {
                pipeline.TransformPixels((data, _, _) => BitmapConversionHelpers.ApplyExponentBias(data, image.ExponentBias));
            }

            return pipeline;
        }

        private BitmapFormat GetNormalMapFormat(BitmapFormat format)
        {
            if (ForceDxt5nm)
                return BitmapFormat.Dxt5nm;

            if (format == BitmapFormat.V8U8)
                return format;

            return HqNormalMapCompression ? BitmapFormat.Dxn : BitmapFormat.Dxt1;
        }
    }
}
