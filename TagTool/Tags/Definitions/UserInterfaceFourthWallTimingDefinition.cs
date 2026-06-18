using System.Collections.Generic;
using TagTool.Cache;
using TagTool.Common;

namespace TagTool.Tags.Definitions
{
    [TagStructure(Name = "user_interface_fourth_wall_timing_definition", Tag = "fwtg", Size = 0x18, MinVersion = CacheVersion.Halo3ODST, MaxVersion = CacheVersion.Halo3ODST)]
    public class UserInterfaceFourthWallTimingDefinition
    {
        public List<FourthWallTiming> DisplayGroups;
        public List<FourthWallTiming> Text;

        [TagStructure(Size = 0x10)]
        public class FourthWallTiming
        {
            public StringId DisplayGroup;
            public List<LocalizedTiming> LocalizedTimings;
        }

        [TagStructure(Size = 0x8)]
        public class LocalizedTiming
        {
            public GameLanguage Language;
            public float StartTime;
        }
    }
}