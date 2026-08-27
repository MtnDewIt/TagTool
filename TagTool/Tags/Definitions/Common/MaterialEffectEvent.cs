using System;
using TagTool.Cache;

namespace TagTool.Tags.Definitions.Common
{
    [TagEnum(IsVersioned = true)]
    public enum MaterialEffectEvent
    {
        Walk,
        Run,
        Sliding,
        Shuffle,
        Jump,
        JumpLand,
        [TagEnumMember(MinVersion = CacheVersion.Halo2Alpha)]
        Sprint,
        [TagEnumMember(MinVersion = CacheVersion.Halo2Alpha)]
        Bodyfall,
        [TagEnumMember(MinVersion = CacheVersion.Halo2Alpha)]
        JumpLandHard, // Gen2 HitByVehicle
        [TagEnumMember(MaxVersion = CacheVersion.HaloCustomEdition)]
        BipedUnused1,
        BipedUnused2,
        [TagEnumMember(MaxVersion = CacheVersion.HaloCustomEdition)]
        Collision,
        [TagEnumMember(MaxVersion = CacheVersion.HaloCustomEdition)]
        VehicleTireSlip,
        [TagEnumMember(MaxVersion = CacheVersion.HaloCustomEdition)]
        VehicleChassisSlip,
        [TagEnumMember(MaxVersion = CacheVersion.HaloCustomEdition)]
        VehicleUnused1,
        [TagEnumMember(MaxVersion = CacheVersion.HaloCustomEdition)]
        VehicleUnused2,
        // Gen2 and above
        [TagEnumMember(MinVersion = CacheVersion.Halo2Alpha)]
        CollisionSmall,
        [TagEnumMember(MinVersion = CacheVersion.Halo2Alpha)]
        CollisionMedium,
        [TagEnumMember(MinVersion = CacheVersion.Halo2Alpha)]
        CollisionLarge,
        [TagEnumMember(MinVersion = CacheVersion.Halo2Alpha)]
        Grinding,
        [TagEnumMember(MinVersion = CacheVersion.Halo2Alpha)]
        Rolling,
        [TagEnumMember(MinVersion = CacheVersion.Halo2Alpha)]
        ImpactDetonate,
        [TagEnumMember(MinVersion = CacheVersion.Halo2Alpha)]
        Fizzle,
        [TagEnumMember(MinVersion = CacheVersion.Halo2Alpha)]
        Overpenetrate,
        [TagEnumMember(MinVersion = CacheVersion.Halo2Alpha)]
        Attach,
        [TagEnumMember(MinVersion = CacheVersion.Halo2Alpha)]
        Bounce,
        [TagEnumMember(MinVersion = CacheVersion.Halo2Alpha)]
        RichochetBounceDud,
        // Gen3 and above
        [TagEnumMember(MinVersion = CacheVersion.Halo3Beta)]
        CollisionDamage,
        [TagEnumMember(MinVersion = CacheVersion.Halo3Beta)]
        MeleeImpact,
        // Reach and above
        [TagEnumMember(MinVersion = CacheVersion.HaloReach)]
        MeleeExplosion
    }
}
