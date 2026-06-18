using TagTool.Cache;
using TagTool.Common;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static TagTool.Tags.TagFieldFlags;
using TagTool.Audio;

namespace TagTool.Tags.Definitions.Gen4
{
    [TagStructure(Name = "cinematic_transition", Tag = "citr", Size = 0x1CC)]
    public class CinematicTransition : TagStructure
    {
        public CinematicTransitionElementBlock FadeOutFromGameStruct;
        public CinematicTransitionElementBlock FadeInToCinematicStruct;
        public CinematicTransitionElementBlock FadeOutFromCinematicStruct;
        public CinematicTransitionElementBlock FadePostCoreLoadStruct;
        public CinematicTransitionElementBlock FadeInToGameStruct;
        
        [TagStructure(Size = 0x5C)]
        public class CinematicTransitionElementBlock : TagStructure
        {
            public CinematicTransitionGlobalFadeBlock GlobalFade;
            public List<CinematicTransitionGlobalGainBlockStruct> SoundGlobalGain;
            public List<CinematicTransitionSoundClassGainBlockStruct> SoundClassGains;
            public List<CinematicTransitionSoundReferenceBlockStruct> StopSounds;
            public List<CinematicTransitionSoundReferenceBlockStruct> StartSounds;
            public List<CinematicTransitionLoopingSoundReferenceBlockStruct> ResumeLoopingSounds;
            public List<CinematicTransitionLoopingSoundStateBlockStruct> LoopingSoundStates;
            public int SleepTime; // hs_ticks
            
            [TagStructure(Size = 0x10)]
            public class CinematicTransitionGlobalFadeBlock : TagStructure
            {
                public RealRgbColor FadeColor;
                public int FadeTime; // hs_ticks
            }
            
            [TagStructure(Size = 0x8)]
            public class CinematicTransitionGlobalGainBlockStruct : TagStructure
            {
                public float Gain; // dB^
                public int Time; // hs_ticks
            }
            
            [TagStructure(Size = 0xC)]
            public class CinematicTransitionSoundClassGainBlockStruct : TagStructure
            {
                [TagField(EnumType = typeof(sbyte))]
                public SoundClass Class;
                [TagField(Length = 0x3, Flags = TagFieldFlags.Padding)]
                public byte[] Padding;
                public float Gain; // dB
                public int Time; // hs_ticks
            }
            
            [TagStructure(Size = 0x10)]
            public class CinematicTransitionSoundReferenceBlockStruct : TagStructure
            {
                [TagField(ValidTags = new [] { "scmb","sndo","lsnd","snd!" })]
                public CachedTag Sound;
            }
            
            [TagStructure(Size = 0x10)]
            public class CinematicTransitionLoopingSoundReferenceBlockStruct : TagStructure
            {
                [TagField(ValidTags = new [] { "lsnd" })]
                public CachedTag LoopingSound;
            }
            
            [TagStructure(Size = 0x18)]
            public class CinematicTransitionLoopingSoundStateBlockStruct : TagStructure
            {
                [TagField(ValidTags = new [] { "lsnd" })]
                public CachedTag LoopingSound;
                public float Scale;
                public CinematicTransitionLoopingSoundAlternateFlags Alternate;
                public CinematicTransitionLoopingSoundLayerFlags Layers;
                
                [Flags]
                public enum CinematicTransitionLoopingSoundAlternateFlags : ushort
                {
                    Alternate = 1 << 0
                }
                
                [Flags]
                public enum CinematicTransitionLoopingSoundLayerFlags : ushort
                {
                    None = 1 << 0,
                    Layer1 = 1 << 1,
                    Layer2 = 1 << 2,
                    Layer3 = 1 << 3,
                    Layer4 = 1 << 4
                }
            }
        }
    }
}
