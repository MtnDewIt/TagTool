using System;
using System.IO;
using System.Numerics;
using TagTool.Common;

namespace TagTool.Geometry.Utils
{
    /// <summary>
    /// StreamWriter with formatting for JMS/JMA/ASS values.
    /// </summary>
    public class BlamAssetWriter : StreamWriter
    {
        private BlamAssetWriter(string path) : base(path, false)
        {
        }

        public static BlamAssetWriter Create(FileInfo file)
        {
            Directory.CreateDirectory(file.Directory.FullName);
            file.Refresh();
            return new(file.FullName);
        }

        public void WritePoint2d(RealPoint2d point) => WriteFloats(point.X, point.Y);
        public void WritePoint3d(RealPoint3d point) => WriteFloats(point.X, point.Y, point.Z);
        public void WriteVector3d(RealVector3d vector) => WriteFloats(vector.I, vector.J, vector.K);
        public void WriteQuaternion(Quaternion quaternion) => WriteFloats(quaternion.X, quaternion.Y, quaternion.Z, quaternion.W);
        public void WriteQuaternion(RealQuaternion quaternion) => WriteFloats(quaternion.I, quaternion.J, quaternion.K, quaternion.W);
        public void WriteRealRGB(RealRgbColor color) => WriteFloats(color.Red, color.Green, color.Blue);
        public void WriteFloat(float value) => WriteFloats(value);

        public void WriteFloats(params float[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                Write(FormatFloat(values[i]));

                if (i < values.Length - 1)
                    Write('\t');
            }
            WriteLine();
        }

        private static string FormatFloat(float value)
        {
            if (value == -0.0f)
                value = 0.0f;

            return value.ToString("0.0000000000");
        }
    }
}
