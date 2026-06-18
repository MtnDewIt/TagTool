using TagTool.Cache;
using TagTool.Common;
using System.Collections.Generic;
using static TagTool.Tags.TagFieldFlags;
using TagTool.Tags.Definitions.Common;

namespace TagTool.Tags.Definitions
{
    [TagStructure(Name = "render_skins_object_globals", Tag = "rsod", Size = 0xC, Platform = CachePlatform.MCC, Version = CacheVersion.Halo3Retail)]
    public class RenderSkinsObjectGlobals : TagStructure
    {
        public List<RenderSkinsObject> RenderSkinsObjects;
    }

    [TagStructure(Size = 0x80)]
    public class RenderSkinsObject : TagStructure
    {
        [TagField(Flags = Label, Length = 32)]
        public string Name;
        public SkinnedObjectType Type;

        [TagField(ValidTags = ["mode"])]
        public CachedTag RenderModel;
        [TagField(ValidTags = ["vehi"])]
        public CachedTag Vehicle;

        public List<LayerBitmap> LayerBitmaps;
        public List<ReplaceMaterial> ReplaceMaterial;
        public List<ReplaceBitmap> ReplaceBitmaps;
        public List<ReplaceParametersMaterialIndex> ReplaceParameterMaterialIndices;
        public List<ReplaceParam> ReplaceParams;
    }

    [TagStructure(Size = 0x10)]
    public class LayerBitmap : TagStructure
    {
        [TagField(ValidTags = ["bitm"])]
        public CachedTag Bitmap;
    }

    [TagStructure(Size = 0x20)]
    public class ReplaceMaterial : TagStructure
    {
        [TagField(ValidTags = ["rm  "])]
        public CachedTag BaseMaterial;
        [TagField(ValidTags = ["rm  "])]
        public CachedTag NewMaterial;
    }

    [TagStructure(Size = 0x20)]
    public class ReplaceBitmap : TagStructure
    {
        [TagField(ValidTags = ["bitm"])]
        public CachedTag BaseBitmap;
        [TagField(ValidTags = ["bitm"])]
        public CachedTag NewBitmap;
    }

    [TagStructure(Size = 0x10)]
    public class ReplaceParametersMaterialIndex : TagStructure
    {
        [TagField(ValidTags = ["rm  "])]
        public CachedTag Material;
    }

    [TagStructure(Size = 0x10)]
    public class ReplaceParam : TagStructure
    {
        public ParamType Type;
        public EntryPointValue EntryPoint;
        public List<ValueBlock> Value;

        public enum ParamType : short
        {
            None,
            AlbedoColor,
            SpecularTint,
            AlbedoSecondColor,
            SpecularSecondTint,
            AnalyticalSpecularContribution,
            EnvironmentMapSpecularContribution,
            SpeedU,
            SpeedV
        }

        public enum EntryPointValue : short
        {
            Default,
            Albedo,
            StaticLightingDefault,
            StaticLightingPerPixel,
            StaticLightingPerVertex,
            StaticLightingSh,
            StaticLightingPrtAmbient,
            StaticLightingPrtLinear,
            StaticLightingPrtQuadratic,
            DynamicLighting,
            ShadowGenerate,
            ShadowApply,
            ActiveCamo,
            LightmapDebugMode,
            VertexColorLighting,
            WaterTessellation,
            WaterShading,
            DynamicLightingCinematic
        }

        [TagStructure(Size = 0x10)]
        public class ValueBlock : TagStructure
        {
            public RealVector4d Vector;
        }
    }

    public enum SkinnedObjectType
    {
        AssaultRifle,
        BattleRifle,
        Needler,
        MagnumPistol,
        PlasmaPistol,
        PlasmaRifle,
        RocketLauncher,
        Shotgun,
        SniperRifle,
        Flamethrower,
        FuelRodCannon,
        SMG,
        Carbine,
        EnergyBlade,
        Visor,
        Warthog,
        WarthogGauss,
        Ghost,
        Scorpion,
        Banshee,
        Hornet,
        Mongoose,
        None,
    }
}