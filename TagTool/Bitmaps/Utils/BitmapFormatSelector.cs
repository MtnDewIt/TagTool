using System;
using static TagTool.Tags.Definitions.Bitmap;

namespace TagTool.Bitmaps.Utils
{
    public static class BitmapFormatSelector
    {
        public static BitmapFormat ChooseOptimalBitmapFormat(byte[] data, int width, int height, BitmapFormat format, BitmapUsageFormat usageFormat)
        {
            if (format != BitmapFormat.A8R8G8B8)
                return format;

            // bit heavy handed, unfortunately the metadata is not always reliable.
            (bool isMono, bool hasAlpha, bool isAlphaOnly) = AnalyzeChannels(data);

            if (hasAlpha && isAlphaOnly)
                return BitmapFormat.A8;

            if (isMono)
                return hasAlpha ? BitmapFormat.A8Y8 : BitmapFormat.Y8;

            if (CanCompress(usageFormat, width, height))
                return hasAlpha ? BitmapFormat.Dxt5 : BitmapFormat.Dxt1;

            return format;
        }

        static bool CanCompress(BitmapUsageFormat usageFormat, int width, int height)
        {
            switch (usageFormat)
            {
                case BitmapUsageFormat.BestUncompressedBumpFormat:
                case BitmapUsageFormat.BestUncompressedColorFormat:
                case BitmapUsageFormat.BestUncompressedMonochromeFormat:
                    return false;
            }

            // dimensions must be a multiple of 4
            return BitmapUtils.IsBlockAligned(width, height);
        }

        private static unsafe (bool isMono, bool hasAlpha, bool isAlphaOnly) AnalyzeChannels(byte[] data)
        {
            if ((data.Length & 3) != 0)
                throw new ArgumentException("Length must be multiple of 4");

            bool mono = true;
            bool alpha = false;
            bool alphaOnly = true;

            fixed (byte* p0 = data)
            {
                byte* p = p0;
                byte* end = p0 + data.Length;

                while (p < end)
                {
                    byte b = p[0];
                    byte g = p[1];
                    byte r = p[2];
                    byte a = p[3];

                    mono &= ((r ^ g) | (g ^ b)) == 0;
                    alpha |= a != 255;
                    alphaOnly &= (r | g | b) == 0;

                    if (!mono && alpha && !alphaOnly)
                        break;

                    p += 4;
                }
            }

            return (mono, alpha, alphaOnly);
        }
    }
}
