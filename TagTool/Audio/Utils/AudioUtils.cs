using System;
using System.Runtime.InteropServices;
using TagTool.Tags;
using TagTool.Tags.Resources;

namespace TagTool.Audio
{
    public static class AudioUtils
    {
        public static SoundResourceDefinition CreateSoundResourceDefinition(byte[] data)
        {
            return new SoundResourceDefinition
            {
                Data = new TagData(data)
            };
        }

        public static SoundResourceDefinition CreateEmptySoundResourceDefinition()
        {
            return new SoundResourceDefinition
            {
                Data = new TagData()
            };
        }

        public static string GetFormatFileExtension(Compression format)
        {
            switch (format)
            {
                case Compression.XMA:
                    return "xma";
                case Compression.OGG:
                    return "ogg";
                case Compression.Tagtool_WAV:
                    return "wav";
                case Compression.PCM:
                    return "pcm";
                case Compression.MP3:
                    return "mp3";
                case Compression.FSB4:
                    return "fsb";
                case Compression.IMAADPCM:
                case Compression.XboxADPCM:
                    return "adpcm";
                default:
                    throw new NotSupportedException();
            }
        }

        public static byte[] ConvertPCM32ToPCM16(ReadOnlySpan<byte> data)
        {
            var newData = new byte[data.Length / 2];
            var input = MemoryMarshal.Cast<byte, float>(data);
            var output = MemoryMarshal.Cast<byte, short>(newData.AsSpan());

            for (int i = 0; i < input.Length; i++)
            {
                output[i] = (short)(input[i] / 1.414f * short.MaxValue);
            }

            return newData;
        }

        public static byte[] Pad(byte[] data, int count)
        {
            byte[] permutationData = new byte[data.Length + (count * 2)];
            Array.Copy(data, 0, permutationData, count, data.Length);
            return permutationData;
        }
    }
}
