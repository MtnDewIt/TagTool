using System;
using System.Collections.Generic;
using TagTool.Cache;
using TagTool.Common;
using TagTool.Tags;

namespace TagTool.Audio
{
    [TagStructure(Size = 0x10, MaxVersion = CacheVersion.Halo3ODST, Platform = CachePlatform.Original)]
    [TagStructure(Size = 0x14, MinVersion = CacheVersion.Halo3Retail, MaxVersion = CacheVersion.Halo3ODST, Platform = CachePlatform.MCC)]
    [TagStructure(Size = 0x2C, MinVersion = CacheVersion.HaloOnlineED, MaxVersion = CacheVersion.HaloOnline700123, Platform = CachePlatform.Original)]
    [TagStructure(Size = 0x14, MinVersion = CacheVersion.HaloReach, Platform = CachePlatform.Original, BuildType = CacheBuildType.ReleaseBuild)]
    [TagStructure(Size = 0x24, MinVersion = CacheVersion.HaloReach, Platform = CachePlatform.Original, BuildType = CacheBuildType.TagsBuild)]
    [TagStructure(Size = 0x18, MinVersion = CacheVersion.HaloReach, Platform = CachePlatform.MCC)]
    public class Permutation : TagStructure
    {
        [TagField(Gen = CacheGeneration.Third, BuildType = CacheBuildType.ReleaseBuild)]
        public short ImportNameIndex;

        [TagField(Gen = CacheGeneration.HaloOnline)]
        [TagField(BuildType = CacheBuildType.TagsBuild)]
        public StringId ImportName;

        //Convert
        [TagField(Gen = CacheGeneration.Third, BuildType = CacheBuildType.ReleaseBuild)]
        public ushort EncodedSkipFraction;

        [TagField(Gen = CacheGeneration.HaloOnline)]
        [TagField(BuildType = CacheBuildType.TagsBuild)]
        public float SkipFraction;

        [TagField(Gen = CacheGeneration.HaloOnline)]
        [TagField(BuildType = CacheBuildType.TagsBuild)]
        public float Gain;

        [TagField(MaxVersion = CacheVersion.HaloOnline700123)]
        [TagField(Version = CacheVersion.HaloReach)]
        public uint SampleCount;

        [TagField(Gen = CacheGeneration.Third, BuildType = CacheBuildType.ReleaseBuild)]
        public int FirstPermutationChunkIndex;

        [TagField(Gen = CacheGeneration.Third, BuildType = CacheBuildType.ReleaseBuild)]
        public short PermutationChunkCount;

        [TagField(Gen = CacheGeneration.Third, BuildType = CacheBuildType.ReleaseBuild)]
        public sbyte EncodedGain;

        [TagField(Gen = CacheGeneration.Third, BuildType = CacheBuildType.ReleaseBuild)]
        public sbyte PermutationInfoIndex;

        [TagField(MinVersion = CacheVersion.HaloReach, BuildType = CacheBuildType.ReleaseBuild)]
        public short FirstLayerMarkerIndex;

        [TagField(MinVersion = CacheVersion.HaloReach, BuildType = CacheBuildType.ReleaseBuild)]
        public short LayerMarkerCount;

        [TagField(Gen = CacheGeneration.Third, BuildType = CacheBuildType.TagsBuild)]
        [TagField(Gen = CacheGeneration.HaloOnline)]
        public short RawInfoIndex; // indexes LanguagePermutations and EncodedPermutationSections

        [TagField(Gen = CacheGeneration.Third, BuildType = CacheBuildType.TagsBuild)]
        public short PlayFractionType;

        [TagField(MinVersion = CacheVersion.HaloReach, BuildType = CacheBuildType.TagsBuild)]
        public Bounds<short> MissionRange;

        [TagField(Gen = CacheGeneration.HaloOnline)]
        public short Unknown1;

        [TagField(Gen = CacheGeneration.Third, BuildType = CacheBuildType.TagsBuild)]
        [TagField(Gen = CacheGeneration.HaloOnline)]
        public ushort PermutationFlags;

        [TagField(Gen = CacheGeneration.Third, BuildType = CacheBuildType.TagsBuild)]
        [TagField(Gen = CacheGeneration.HaloOnline)]
        public ushort Flags;

        [TagField(Gen = CacheGeneration.Third, BuildType = CacheBuildType.TagsBuild)]
        public List<PermutationLanguage> Languages;

        [TagField(Gen = CacheGeneration.HaloOnline)]
        public List<PermutationChunk> PermutationChunks;

        [TagField(Gen = CacheGeneration.HaloOnline)]
        public PermutationFlagsHaloOnline PermutationFlagsHO;

        [TagField(Gen = CacheGeneration.HaloOnline, Flags = TagFieldFlags.Padding, Length = 2)]
        public byte[] Padding;

        [TagField(Gen = CacheGeneration.HaloOnline)]
        public uint FirstSample;

        [TagField(Platform = CachePlatform.MCC)]
        public uint FsbSoundHash;

        [Flags]
        public enum PermutationFlagsHaloOnline : short
        {
            /// <summary>
            /// Wait for the currently playing permutation to finish
            /// </summary>
            SequencedBit = (1 << 0)
        }
    }

    [TagStructure(Size = 0x10)]
    public class Gen2Permutation : TagStructure
    {
        public short Name;
        public ushort EncodedSkipFraction;
        public sbyte EncodedGain; // dB
        public sbyte PermutationInfoIndex;
        public short LanguageNeutralTime; // ms
        [TagField(Platform = CachePlatform.Original)]
        public uint SampleSize;
        [TagField(Platform = CachePlatform.Original)]
        public short FirstChunk;
        [TagField(Platform = CachePlatform.Original)]
        public short ChunkCount;

        [TagField(Platform = CachePlatform.MCC)]
        public List<LocalizedChunkInfoBlock> LocalizedChunksInfo;

        [TagStructure(Size = 0x8)]
        public class LocalizedChunkInfoBlock : TagStructure
        {
            public int SampleSize;
            public short FirstChunk;
            public short ChunkCount;
        }
    }
}
