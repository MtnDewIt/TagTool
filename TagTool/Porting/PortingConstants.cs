using System.Collections.Generic;
using TagTool.Common;

namespace TagTool.Porting
{
    public static class PortingConstants
    {
        public static readonly Tag[] RenderMethodGroups =
        [
            "rmbk",
            "rmcs",
            "rmd ",
            "rmfl",
            "rmhg",
            "rmsh",
            "rmss",
            "rmtr",
            "rmw ",
            "rmct",
            "rmgl",
        ];

        public static readonly Tag[] EffectGroups =
        [
            "beam",
            "cntl",
            "decs",
            "ltvl",
            "prt3",
        ];

        public static readonly Tag[] ResourceTagGroups = 
        [
            "bink",
            "bitm",
            "jmad",
            "Lbsp",
            "mode",
            "pmdf",
            "sbsp",
            "snd!",     
        ];

        public static readonly Tag[] DoNotReplaceGroups =
        [
            "glps",
            "glvs",
            "pixl",
            "rmdf",
            "rmt2",
            "vtsh",
        ];

        public static readonly Tag[] UnreadyGroups =
        [
            "glps",
            "glvs",
            "rmcs",
            "rmcs",
            "rmct",
            "rmdf",
            "rmhg",
            "rmt2",
            "rmw ",
            "shit",
            "sncl",
        ];

        public static readonly IReadOnlyDictionary<Tag, string> DefaultTagNames = new Dictionary<Tag, string>
        {
            { "beam", @"objects\weapons\support_high\spartan_laser\fx\firing_3p" },
            { "bitm", @"shaders\default_bitmaps\bitmaps\gray_50_percent_linear" },
            { "cntl", @"objects\weapons\pistol\needler\fx\projectile" },
            { "decs", @"fx\decals\impact_plasma\impact_plasma_medium\hard" },
            { "glps", @"shaders\shader_shared_pixel_shaders" },
            { "glvs", @"shaders\shader_shared_vertex_shaders" },
            { "ltvl", @"objects\weapons\pistol\plasma_pistol\fx\charged\projectile" },
            { "prt3", @"fx\particles\energy\sparks\impact_spark_orange" },
            { "rmd ", @"objects\gear\human\military\shaders\human_military_decals" },
            { "rmdf", @"shaders\shader" },
            { "rmfl", @"levels\multi\riverworld\shaders\riverworld_tree_leafa" },
            { "rmhg", @"objects\multi\shaders\koth_shield" },
            { "rmsh", @"shaders\invalid" },
            { "rmtr", @"levels\multi\riverworld\shaders\riverworld_ground" },
            { "rmw ", @"levels\multi\riverworld\shaders\riverworld_water_rough" },
            { "shit", @"globals\global_shield_impact_settings" },
            { "sncl", @"sound\sound_classes" },
            { "snd!", @"sound\default_silent" },
        };
    }
}
