using System.Collections.Generic;
using TagTool.Cache;
using TagTool.Common.Logging;
using TagTool.Tags.Definitions;

namespace TagTool.Bitmaps.Utils
{
    public delegate byte[] ConverterDelegate(byte[] data, int width, int height);

    public class PipelineBuilder
    {
        private readonly string TagName;
        private readonly BitmapFormat Format;
        private readonly List<ConverterDelegate> DirectTransforms = [];
        private readonly List<ConverterDelegate> PixelTransforms = [];

        public CacheVersion Version;
        public CachePlatform Platform;
        public BitmapFormat DestFormat;
        public CompressionQuality CompressionQuality;

        public PipelineBuilder(string tagName, BitmapFormat format, BitmapFormat destFormat, CacheVersion sourceVersion, CachePlatform sourcePlatform)
        {
            TagName = tagName;
            Format = format;
            DestFormat = destFormat;
            Version = sourceVersion;
            Platform = sourcePlatform;
        }

        public PipelineBuilder Transform(ConverterDelegate transform)
        {
            DirectTransforms.Add(transform);
            return this;
        }

        public PipelineBuilder TransformPixels(ConverterDelegate transform)
        {
            PixelTransforms.Add(transform);
            return this;
        }

        public PipelineBuilder ApplyConstraints(int width, int height)
        {
            if (BitmapUtils.IsCompressedFormat(DestFormat))
            {
                if (DestFormat == BitmapFormat.Dxn && !BitmapUtils.IsPowerOfTwo(width, height))
                {
                    DestFormat = BitmapFormat.V8U8;
                    Log.Warning($"DXN bitmap '{TagName}' has invalid dimensions {width}x{height} (must be pow2); Using {DestFormat}.");
                }
                else if (!BitmapUtils.IsBlockAligned(width, height))
                {
                    if (DestFormat == BitmapFormat.Dxt5nm)
                    {
                        TransformPixels((data, _, _) => BitmapConversionHelpers.SwizzleForDXT5nm(data));
                        DestFormat = BitmapFormat.A8Y8;
                    }
                    else
                    {
                        DestFormat = BitmapFormat.A8R8G8B8;
                    }
                    Log.Warning($"DXTn bitmap '{TagName}' has invalid dimensions {width}x{height} (must be divisible by 4); Using {DestFormat}.");
                }
            }

            return this;
        }

        public PipelineBuilder ApplyOptimization(IBitmapExtractor extractor, BitmapType type, int width, int height, Bitmap.BitmapUsageFormat usageFormat)
        {
            if (type != BitmapType.Texture2D || DestFormat != BitmapFormat.A8R8G8B8)
                return this;

            BitmapFormat chosenFormat = BitmapFormatSelector.ChooseOptimalBitmapFormat(
                extractor.ExtractSurface(0, 0), width, height, DestFormat, usageFormat);

            if (chosenFormat != DestFormat)
            {
                Log.Info($"Using {chosenFormat} instead of {DestFormat} for bitmap '{TagName}'");
                DestFormat = chosenFormat;
            }

            return this;
        }

        public ConverterDelegate Build()
        {
            return (data, width, height) =>
            {
                foreach (var transform in DirectTransforms)
                    data = transform(data, width, height);

                if (PixelTransforms.Count > 0 || Format != DestFormat)
                {
                    data = BitmapDecoder.DecodeBitmap(data, Format, width, height, Version, Platform);

                    foreach (var transform in PixelTransforms)
                        data = transform(data, width, height);

                    data = BitmapDecoder.EncodeBitmap(data, DestFormat, width, height, CompressionQuality);
                }

                return data;
            };
        }
    }
}
