using System.Collections.Generic;
using TagTool.Cache;
using TagTool.Common;

namespace TagTool.Tags.Definitions
{
    [TagStructure(Name = "scenario_required_resource", Tag = "sdzg", Size = 0xC, Platform = CachePlatform.MCC, MinVersion = CacheVersion.Halo3ODST)]
    [TagStructure(Name = "scenario_required_resource", Tag = "sdzg", Size = 0xC, Platform = CachePlatform.Original, MinVersion = CacheVersion.HaloReach)]
    public class ScenarioRequiredResource : TagStructure
    {
        public List<TagReferenceBlock> Reference;
    }
}
