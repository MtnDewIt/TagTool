using TagTool.Common;
using TagTool.IO;
using System;
using System.IO;
using System.Linq;
using TagTool.Tags;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace System.IO
{
    public static class BinaryWriterExtensions
    {
		private unsafe static T ToLittleEndian<T>(T value, EndianFormat format) where T : unmanaged
		{
			if (format == EndianFormat.BigEndian) MemoryMarshal.CreateSpan(ref Unsafe.As<T, byte>(ref value), sizeof(T)).Reverse();
			return value;
		}

        public static void Write(this BinaryWriter writer, ushort value, EndianFormat format) => writer.Write(ToLittleEndian(value, format));
        public static void Write(this BinaryWriter writer, short value, EndianFormat format) => writer.Write(ToLittleEndian(value, format));
        public static void Write(this BinaryWriter writer, uint value, EndianFormat format) => writer.Write(ToLittleEndian(value, format));
        public static void Write(this BinaryWriter writer, int value, EndianFormat format) => writer.Write(ToLittleEndian(value, format));
		public static void Write(this BinaryWriter writer, ulong value, EndianFormat format) => writer.Write(ToLittleEndian(value, format));
		public static void Write(this BinaryWriter writer, long value, EndianFormat format) => writer.Write(ToLittleEndian(value, format));

		public static void Write(this BinaryWriter writer, float value, TagFieldCompression compression)
        {
            switch (compression)
            {
                case TagFieldCompression.Int8:
                    writer.Write((sbyte)(value * sbyte.MaxValue));
                    break;

                case TagFieldCompression.Int16:
                    writer.Write((short)(value * short.MaxValue));
                    break;

                default:
                    writer.Write(value);
                    break;
            }
        }

        public static void Write(this BinaryWriter writer, Tag value, EndianFormat format = EndianFormat.LittleEndian)
        {
            writer.Write(value.Value, format);
        }

        public static void Write(this BinaryWriter writer, Half value, EndianFormat format = EndianFormat.LittleEndian)
        {
            writer.Write(BitConverter.HalfToUInt16Bits(value), format);
        }

        public static void Write(this BinaryWriter writer, Point2d value, EndianFormat format = EndianFormat.LittleEndian)
        {
            writer.Write(value.X, format);
            writer.Write(value.Y, format);
        }

        public static void Write(this BinaryWriter writer, Rectangle2d value, EndianFormat format = EndianFormat.LittleEndian)
        {
            writer.Write(value.Top, format);
            writer.Write(value.Left, format);
            writer.Write(value.Bottom, format);
            writer.Write(value.Right, format);
        }

        public static void Write(this BinaryWriter writer, DatumHandle value, EndianFormat format = EndianFormat.LittleEndian)
        {
            writer.Write(value.Value, format);
        }
    }
}
