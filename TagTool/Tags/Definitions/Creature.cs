using TagTool.Cache;
using TagTool.Common;
using System;
using System.Collections.Generic;
using TagTool.Tags.Definitions.Common;

namespace TagTool.Tags.Definitions
{
    [TagStructure(Name = "creature", Tag = "crea", Size = 0x100, MaxVersion = CacheVersion.HaloOnline700123)]
    [TagStructure(Name = "creature", Tag = "crea", Size = 0x174, MinVersion = CacheVersion.HaloReach)]
    public class Creature : GameObject
    {
        public CreatureFlags Flags2;

        public Unit.DefaultTeamValue DefaultTeam;
        public Unit.MotionSensorBlipSizeValue MotionSensorBlipSize;

        /// <summary>
        /// The maximum turning velocity of the creature. Ground creatures only.
        /// </summary>
        public Angle TurningVelocityMaximum;
        
        /// <summary>
        /// The maximum turning acceleration of the creature. Ground creatures only.
        /// </summary>
        public Angle TurningAccelerationMaximum;

        public float CasualTurningModifer;
        public float AutoaimWidth;
        public uint Flags3;
        public float HeightStanding;
        public float HeightCrouching;
        public float Radius;
        public float Mass;
        public StringId LivingMaterialName;
        public StringId DeadMaterialName;
        public short LivingGlobalMaterialIndex;
        public short DeadGlobalMaterialIndex;
        public List<PhysicsModel.Sphere> DeadSphereShapes;
        public List<PhysicsModel.Pill> PillShapes;
        public List<PhysicsModel.Sphere> SphereShapes;
        public Angle MaximumSlopeAngle;
        public Angle DownhillFalloffAngle;
        public Angle DownhillCutoffAngle;
        public Angle UphillFalloffAngle;
        public Angle UphillCutoffAngle;
        public float DownhillVelocityScale;
        public float UphillVelocityScale;
        public float CosineMaximumSlopeAngle;
        public float NegativeSineDownhillFalloffAngle;
        public float NegativeSineDownhillCutoffAngle;
        public float SineUphillFalloffAngle;
        public float SineUphillCutoffAngle;
        public Angle ClimbInflectionAngle;
        public float AirborneReactionTimeScale;
        public float GroundAdhesionVelocityScale;
        public float GravityScale;

        [TagField(MinVersion = CacheVersion.HaloReach)]
        public float AirborneAccelerationScale;

        public Angle BankAngle;
        public float BankApplyTime;
        public float BankDecayTime;
        public float PitchRatio;
        public float MaximumVelocity;
        public float MaximumSidestepVelocity;
        public float Acceleration;
        public float Deceleration;
        public Angle AngularVelocityMaximum;
        public Angle AngularAccelerationMaximum;
        public float CrouchVelocityModifier;
        public FlyingCreatureFlags FlyingFlags;
        public CachedTag ImpactDamage;
        public CachedTag ImpactShieldDamage;
        public List<MetagameBucket> MetagameProperties;
        public Bounds<float> DestroyAfterDeathTimeBounds;

        [TagField(MinVersion = CacheVersion.HaloReach)]
        public BigBattleFlagsValue BigBattleFlags;
        [TagField(MinVersion = CacheVersion.HaloReach)]
        public CachedTag BigBattleWeaponEmitter;
        [TagField(MinVersion = CacheVersion.HaloReach)]
        public RealVector3d BigBattleWeaponOffset;
        [TagField(MinVersion = CacheVersion.HaloReach)]
        public CachedTag BigBattleWeaponEmitter2;
        [TagField(MinVersion = CacheVersion.HaloReach)]
        public RealVector3d BigBattleWeaponOffset2;
        [TagField(MinVersion = CacheVersion.HaloReach)]
        public List<BigBattleWeaponTimingBlock> BigBattleWeaponFireTiming;
        [TagField(MinVersion = CacheVersion.HaloReach)]
        public CachedTag BigBattleExpensiveWeaponEffect;
        [TagField(MinVersion = CacheVersion.HaloReach)]
        public Bounds<float> BigBattleExpensiveWeaponFireTime;
        [TagField(MinVersion = CacheVersion.HaloReach)]
        public CachedTag BigBattleDeathEffect;
    }

    [Flags]
    public enum CreatureFlags : uint
    {
        None = 0,
        Unused = 1 << 0,
        InfectionForm = 1 << 1,
        ImmuneToFallingDamage = 1 << 2,
        RotateWhileAirborne = 1 << 3,
        ZappedByShields = 1 << 4,
        AttachUponImpact = 1 << 5,
        NotOnMotionSensor = 1 << 6
    }

    [Flags]
    public enum CreatureFlagsReach : uint
    {
        // TODO: convert
        None = 0,
        Unused = 1 << 0,
        ImmuneToFallingDamage = 1 << 1,
        RotateWhileAirborne = 1 << 2,
        ZappedByShields = 1 << 3,
        AttachUponImpact = 1 << 4,
        NotOnMotionSensor = 1 << 5,
        ForceGroundMovement = 1 << 6
    }

    [Flags]
    public enum FlyingCreatureFlags : uint
    {
        None = 0,
        UseWorldUp = 1 << 0,
    }

    [Flags]
    public enum BigBattleFlagsValue : uint
    {
        None = 0,
        BoidAimsAtBigBattleTarget = 1 << 0,
        BoidsFlyWithNoPitch = 1 << 1,
        BoidsFlyNonDirectionally = 1 << 2,
    }

    [TagStructure(Size = 0x14)]
    public class BigBattleWeaponTimingBlock : TagStructure
    {
        public TagFunction Function;
    }
}
