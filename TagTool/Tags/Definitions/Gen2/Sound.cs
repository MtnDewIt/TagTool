using TagTool.Cache;
using System;
using TagTool.Audio;
using TagTool.Common;

namespace TagTool.Tags.Definitions.Gen2
{
    [TagStructure(Name = "sound", Tag = "snd!", Platform = CachePlatform.Original, Size = 0x14)]
    [TagStructure(Name = "sound", Tag = "snd!", Platform = CachePlatform.MCC, Size = 0x1C)]
    public class Sound : TagStructure
    {
        [TagField(EnumType = typeof(ushort), MaxVersion = CacheVersion.Halo2PC)]
        public BitFlags<SoundFlags> Flags;
        [TagField(EnumType = typeof(short), MaxVersion = CacheVersion.Halo2Beta)]
        [TagField(EnumType = typeof(sbyte), MinVersion = CacheVersion.Halo2Xbox)]
        public SoundClass Class;

        [TagField(Platform = CachePlatform.Original)]
        public SampleRate SampleRate;
        [TagField(Platform = CachePlatform.Original)]
        public Gen2Encoding Encoding;
        [TagField(Platform = CachePlatform.Original)]
        public Compression Compression;

        [TagField(Platform = CachePlatform.MCC)]
        public byte CodecIndex;
        [TagField(Platform = CachePlatform.MCC)]
        public short RemasteredSoundReferenceIndex;

        public short PlaybackParamaterIndex;
        public short PitchRangeIndex;
        public sbyte PitchRangeCount;
        public sbyte ScaleIndex;
        public sbyte PromotionIndex;
        public sbyte CustomPlaybackIndex;
        public short ExtraInfoIndex;
        public int MaximumPlayTime;

        [TagField(Platform = CachePlatform.MCC)]
        public float InnerSilenceDistance;  // don't play below this distance
        [TagField(Platform = CachePlatform.MCC)]
        public short FirstReflectionParametersIndex;
        [TagField(Platform = CachePlatform.MCC)]
        public byte ReflectionParametersCount;
        [TagField(Platform = CachePlatform.MCC)]
        public byte LowPassCutoffParametersIndex;
    }
}

