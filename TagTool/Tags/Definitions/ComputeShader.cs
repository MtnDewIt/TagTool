using System;
using System.Collections.Generic;
using TagTool.Cache;
using TagTool.Common;
using TagTool.Shaders;

namespace TagTool.Tags.Definitions
{
    [TagStructure(Name = "compute_shader", Tag = "cmpu", Size = 0x20, Platform = CachePlatform.MCC, MaxVersion = CacheVersion.Halo3ODST)]
    public class ComputeShader : TagStructure
    {
        [TagField(MaxVersion = CacheVersion.Halo3ODST)]
        public EntryPointFlags EntryPointsFlags;
        public List<TagBlockIndex> EntryPoints;
        public uint Version;
        public List<CompiledComputeShader> CompiledShaders;
    }

    [Flags]
    public enum EntryPointFlags : int
    {
        None,
        Default = 1 << 0,
        Albedo = 1 << 1,
        StaticDefault = 1 << 2,
        StaticPerPixel = 1 << 3,
        StaticPerVertex = 1 << 4,
        StaticSh = 1 << 5,
        StaticPrtAmbient = 1 << 6,
        StaticPrtLinear = 1 << 7,
        StaticPrtQuadratic = 1 << 8,
        DynamicLight = 1 << 9,
        ShadowGenerate = 1 << 10,
        ShadowApply = 1 << 11,
        ActiveCamo = 1 << 12,
        LightmapDebugMode = 1 << 13,
        VertexColorLighting = 1 << 14,
        WaterTesselation = 1 << 15,
        WaterShading = 1 << 16,
        DynamicLightCinematic = 1 << 17
    }

    [TagStructure(Size = 0x90)]
    public class CompiledComputeShader : TagStructure
    {
        [TagField(Version = CacheVersion.HaloReach)]
        public FlagsValue ShaderFlags; 
        public byte[] XenonCompiledShader;
        public byte[] Dx9CompiledShader;
        public byte[] DurangoCompiledShader;
        public ShaderConstantTable XenonConstants;
        public ShaderConstantTable Dx9Constants;
        public ShaderConstantTable DurangoConstants;
        public int GeneralPurposeRegisters;
        public UInt64 RuntimeComputeShader;

        [Flags]
        public enum FlagsValue
        {
            None = 0,
            RequiresConstantTable = 1 << 0
        }
    }
}