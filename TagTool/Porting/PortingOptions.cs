using System;
using System.ComponentModel;
using TagTool.Audio;

namespace TagTool.Porting
{
    public class PortingOptions
    {
        [Description("Maximum number of threads to use")]
        public int MaxThreads = Environment.ProcessorCount * 2;

        [Description("Audio codec to use for ported sounds")]
        public Compression AudioCodec = Compression.MP3;

        [Description("Path to audio cache directory")]
        public string AudioCache = null;

        [Description("Path to reach lightmap cache directory")]
        public string ReachLightmapCache = null;

        [Description("Enable reach decorator porting (WIP)")]
        public bool ReachDecorators = true;

        [Description("Enable legacy Gen1 collision BSP generator")]
        public bool Gen1Collision = false;

        [Description("Convert CTX1 bitmaps to DXN (default is DXT1)")]
        public bool HqNormalMapConversion = false;

        [Description("Use DXT5nm for normal maps")]
        public bool UseExperimentalDxt5nm = false;

        [Description("Regenerates the structure surface triangle mappings")]
        public bool RegenerateStructureSurfaces = false;

        [Description("Enables the fix for vehicle_test_seat_list & vehicle_test_seat (requires dll changes)")]
        public bool EnableH3VehicleTestSeat = false;

        [Description("Enables bitmap format optimization to reduce cache sizes")]
        public bool BitmapFormatOptimization = true;
    }
}
