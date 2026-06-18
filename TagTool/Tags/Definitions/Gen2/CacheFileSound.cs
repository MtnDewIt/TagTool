using TagTool.Cache;
using TagTool.Common;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static TagTool.Tags.TagFieldFlags;
using TagTool.Audio;

namespace TagTool.Tags.Definitions.Gen2
{
    [TagStructure(Name = "cache_file_sound", Tag = "$#!+", Size = 0x14)]
    public class CacheFileSound : TagStructure
    {
        [TagField(EnumType = typeof(ushort))]
        public SoundFlags Flags;
        [TagField(EnumType = typeof(sbyte))]
        public SoundClass SoundClass;
        public SampleRate SampleRate;
        public EncodingValue Encoding;
        public CompressionValue Compression;
        public short PlaybackIndex;
        public short FirstPitchRangeIndex;
        public sbyte PitchRangeCount;
        public sbyte ScaleIndex;
        public sbyte PromotionIndex;
        public sbyte CustomPlaybackIndex;
        public short ExtraInfoIndex;
        public int MaximumPlayTime; // ms
        
        public enum EncodingValue : sbyte
        {
            Mono,
            Stereo,
            Codec
        }
        
        public enum CompressionValue : sbyte
        {
            NoneBigEndian,
            XboxAdpcm,
            ImaAdpcm,
            NoneLittleEndian,
            Wma
        }
    }
}

