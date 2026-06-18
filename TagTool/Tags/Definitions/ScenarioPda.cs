using System.Collections.Generic;
using TagTool.Cache;
using TagTool.Common;

namespace TagTool.Tags.Definitions
{
    [TagStructure(Name = "scenario_pda", Tag = "spda", Size = 0xC, MinVersion = CacheVersion.Halo3ODST, MaxVersion = CacheVersion.Halo3ODST)]
    public class ScenarioPda
    {
        public List<PdaDefinition> PdaDefinitions;
    }

    [TagStructure(Size = 0xC0)]
    public class PdaDefinition
    {
        public StringId Name;
        public List<PdaRenderModel> RenderModels;
        public List<PdaRenderModelColor> PdaRenderModelColors;
        public CachedTag PdaValidMovementMap;
        public CachedTag PdaHeightMap;
        public Bounds<float> PdaHeight;
        public float FalloffRadius;
        public RealPoint2d InitialPosition;
        public RealPoint3d MinimumWorldPosition;
        public RealPoint3d MaximumWorldPosition;
        public RealPoint3d MinimumCameraOffset;
        public RealPoint3d MinimumCameraFocalOffset;
        public RealPoint3d MaximumCameraOffset;
        public RealPoint3d MaximumCameraFocalOffset;
        public float InitialZoom;
        public float ZoomSpeed;

        public float MovementSpeed;
        public RealEulerAngles2d InitialRotation;
        public RealEulerAngles2d MinimumRotation;
        public RealEulerAngles2d MaximumRotation;
        public Angle RotationSpeed;
    }

    [TagStructure(Size = 0x1C)]
    public class PdaRenderModel
    {
        public CachedTag RenderModel;
        public short ModelColorIndex;

        [TagField(Flags = TagFieldFlags.Padding, Length = 0x2)]
        public byte[] Padding0;

        public RealPoint2d Scale;
    }

    [TagStructure(Size = 0x10)]
    public class PdaRenderModelColor
    {
        public RealRgbColor Color;
        public float Intensity;
    }
}