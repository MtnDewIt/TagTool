using TagTool.Cache;
using TagTool.Common;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static TagTool.Tags.TagFieldFlags;
using TagTool.Audio;

namespace TagTool.Tags.Definitions.Gen4
{
    [TagStructure(Name = "cache_file_sound", Tag = "$#!+", Size = 0x24)]
    public class CacheFileSound : TagStructure
    {
        [TagField(EnumType = typeof(ushort))]
        public SoundFlags Flags;
        [TagField(EnumType = typeof(sbyte))]
        public SoundClass SoundClass;
        public sbyte PitchRangeCount;
        public short CodecIndex;
        public short FirstPitchRangeIndex;
        public short FirstLanguageDurationPitchRangeIndex;
        public short RuntimeGestaltIndexStorage;
        public short SubPriority;
        public short PlaybackIndex;
        public short ScaleIndex;
        public sbyte PromotionIndex;
        public sbyte CustomPlaybackIndex;
        public short ExtraInfoIndex;
        [TagField(Length = 0x2, Flags = TagFieldFlags.Padding)]
        public byte[] Padding;
        public int MaximumPlayTime; // ms
        public TagResourceReference SoundDataResource;
    }
}
