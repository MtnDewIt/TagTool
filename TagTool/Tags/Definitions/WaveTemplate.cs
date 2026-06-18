using System;
using System.Collections.Generic;
using TagTool.Cache;
using TagTool.Common;

namespace TagTool.Tags.Definitions
{
    [TagStructure(Name = "wave_template", Tag = "wave", Size = 0xC, Platform = CachePlatform.MCC, MinVersion = CacheVersion.Halo3ODST)]
    [TagStructure(Name = "wave_template", Tag = "wave", Size = 0xC, Platform = CachePlatform.Original, MinVersion = CacheVersion.HaloReach)]
    public class WaveTemplate : TagStructure
    {
        public List<SquadSpecification> SquadSpecifications;
        
        [TagStructure(Size = 0x28, Platform = CachePlatform.MCC, MaxVersion = CacheVersion.Halo3ODST)]
        [TagStructure(Size = 0x24, MinVersion = CacheVersion.HaloReach)]
        public class SquadSpecification : TagStructure
        {
            [TagField(ValidTags = new [] { "sqtm" })]
            public CachedTag SquadTemplate;

            public GameDifficultyFlags DifficultyFlags;
            [TagField(Length = 0x2, Flags = TagFieldFlags.Padding)]
            public byte[] Padding;

            public Bounds<short> RoundRange;
            public Bounds<short> SetRange;
            // The relative weight given to this squad spawning
            public short Weight;

            // Spawn AT LEAST or NO MORE than this number of squads. Value of 0 means "no minimum" or "no maximum"
            public Bounds<sbyte> SpawnCount;

            // Filter where this squad specification can spawn by matching this value with the values in squad definitions in the
            // scenario
            public WavePlacementFilterEnum PlacementFilter;

            [TagField(MaxVersion = CacheVersion.Halo3ODST, Platform = CachePlatform.MCC)]
            public SpawnFlagsValue SpawnFlags;
            [TagField(MaxVersion = CacheVersion.Halo3ODST, Platform = CachePlatform.MCC)]
            public DefaultTeamValue Team;

            public enum WavePlacementFilterEnum : uint
            {
                None,
                HeavyInfantry,
                BossInfantry,
                LightVehicle,
                HeavyVehicle,
                FlyingInfantry,
                FlyingVehicle,
                Bonus
            }

            public enum DefaultTeamValue : ushort
            {
                Default,
                Player,
                Human,
                Covenant,
                Flood,
                Sentinel,
                Heretic,
                Prophet,
                Guilty,
                Unused9,
                Unused10,
                Unused11,
                Unused12,
                Unused13,
                Unused14,
                Unused15
            }

            [Flags]
            public enum SpawnFlagsValue : ushort
            {
                None = 0,
                IncompatibleWithDropships = 1 << 0,
            }
        }
    }
}
