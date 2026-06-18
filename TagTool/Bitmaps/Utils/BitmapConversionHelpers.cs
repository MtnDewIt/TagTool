using System;
using TagTool.Extensions;

namespace TagTool.Bitmaps.Utils
{
    public static class BitmapConversionHelpers
    {
        public static byte[] Convert_BC5SNorm_To_ATI2(byte[] data, int width, int height)
        {
            for (int i = 0; i < data.Length; i += 16)
            {
                // signed -> unsigned
                data[i + 0] += 128;
                data[i + 1] += 128;
                data[i + 8] += 128;
                data[i + 9] += 128;

                // swap X/Y
                for (int j = 0; j < 8; j++)
                {
                    byte tmp = data[i + j];
                    data[i + j] = data[i + j + 8];
                    data[i + j + 8] = tmp;
                }
            }

            return data;
        }

        public static unsafe byte[] Convert_RGB10A2_To_A2RGB10(byte[] data, int width, int height)
        {
            fixed (byte* ptr = data)
            {
                for (int i = 0; i < width * height; i++)
                {
                    uint* pixel = (uint*)&ptr[i * 4];
                    uint R = (*pixel & 0x3ff00000) >> 20;
                    uint G = (*pixel & 0x000ffc00);
                    uint B = (*pixel & 0x000003ff) << 20;
                    uint A = (*pixel & 0xC0000000);
                    *pixel = B | G | R | A;
                }
            }
            return data;
        }

        public static byte[] ApplyExponentBias(byte[] data, int exponentBias)
        {
            const float oneDiv255 = 1.0f / 255.0f;
            float expBias = (float)Math.Pow(2.0f, exponentBias); // 4.0f
            for (int i = 0; i < data.Length; i += 4)
            {
                var vector = VectorExtensions.InitializeVector([data[i], data[i + 1], data[i + 2], data[i + 3]]);

                vector *= oneDiv255; // 0-1 range
                                     // need more math here. not sure if it can be prebaked. this should do for now.
                vector *= vector;
                vector *= 255.0f; // 0-255 range

                data[i + 0] = (byte)vector[0];
                data[i + 1] = (byte)vector[1];
                data[i + 2] = (byte)vector[2];
                //rawData[i + 3] = (byte)(biasedAlpha * 255.0f); // no need to touch alpha.
            }
            return data;
        }

        public static byte[] SwizzleForDXT5nm(byte[] data)
        {
            for (int i = 0; i < data.Length; i += 4)
            {
                byte g = data[i + 1];
                byte r = data[i + 2];

                data[i + 0] = g;
                data[i + 1] = g;
                data[i + 2] = g;
                data[i + 3] = r;
            }

            return data;
        }

        public static byte[] Diffuse_To_Normal(byte[] data, BitmapImageCurve imageCurve)
        {
            var curve = BitmapCurves.GetCurve(imageCurve);

            for (int i = 0; i < data.Length; i += 4)
            {
                float x = curve.ToLinear(data[i + 2] / 255f);
                float y = curve.ToLinear(data[i + 1] / 255f);
                float z = BitmapUtils.CalculateNormalZ(x, y);

                data[i + 0] = (byte)((z + 1f) * 127.5f + 0.5f);
                data[i + 1] = (byte)((y + 1f) * 127.5f + 0.5f);
                data[i + 2] = (byte)((x + 1f) * 127.5f + 0.5f);
                data[i + 3] = 255;
            }

            return data;
        }
    }
}
