using System;
using TagTool.Cache;
using TagTool.Tags;
using static TagTool.Audio.SoundClass;

namespace TagTool.Audio
{
    [TagEnum(IsVersioned = true)]
    public enum SoundClass
    {
        ProjectileImpact,
        ProjectileDetonation,
        ProjectileFlyby,
        ProjectileDetonationLod,
        WeaponFire,
        WeaponReady,
        WeaponReload,
        WeaponEmpty,
        WeaponCharge,
        WeaponOverheat,
        WeaponIdle,
        WeaponMelee,
        WeaponAnimation,
        ObjectImpacts,
        ParticleImpacts,
        [TagEnumMember(Gen = CacheGeneration.First)]
        SlowParticleImpacts,
        [TagEnumMember(Gen = CacheGeneration.First)]
        Unused1Impacts,
        [TagEnumMember(Gen = CacheGeneration.First)]
        Unused2Impacts,
        [TagEnumMember(MinVersion = CacheVersion.Halo2Alpha)]
        WeaponFireLod,  // Gen1 SlowParticleImpacts
        [TagEnumMember(MinVersion = CacheVersion.Halo2Alpha, MaxVersion = CacheVersion.HaloOnline700123)]
        WeaponFireLodFar,
        [TagEnumMember(MinVersion = CacheVersion.HaloReach)]
        WaterTransitions,
        [TagEnumMember(MinVersion = CacheVersion.Halo2Alpha)]
        LowpassEffects,
        UnitFootsteps,
        UnitDialog,
        UnitAnimation,      // Gen1 Unused
        UnitUnused,         // Gen1 Unused
        VehicleCollision,
        VehicleEngine,
        VehicleAnimation,
        VehicleEngineLod,
        DeviceDoor,
        [TagEnumMember(Gen = CacheGeneration.First)]
        DeviceForceField,
        [TagEnumMember(MinVersion = CacheVersion.Halo2Alpha)]
        DeviceUnused0,
        DeviceMachinery,
        DeviceStationary,
        [TagEnumMember(Gen = CacheGeneration.First)]
        DeviceComputers,
        [TagEnumMember(MinVersion = CacheVersion.Halo2Alpha)]
        DeviceUnused1,
        DeviceUnused2,
        Music,
        AmbientNature,
        AmbientMachinery,
        AmbientStationary,
        HugeAss,            // Gen1 Unused
        ObjectLooping,      // Gen1 Unused
        CinematicMusic,     // Gen1 Unused
        [TagEnumMember(Gen = CacheGeneration.HaloOnline)]
        PlayerArmor,
        [TagEnumMember(MaxVersion = CacheVersion.Halo2Beta)]
        FirstPersonDamage,
        [TagEnumMember(MinVersion = CacheVersion.Halo2Xbox, MaxVersion = CacheVersion.Halo2PC)]
        Reflection,
        [TagEnumMember(MinVersion = CacheVersion.Halo2Xbox, MaxVersion = CacheVersion.Halo2PC)]
        ReflectionLod,
        [TagEnumMember(MinVersion = CacheVersion.Halo2Xbox, MaxVersion = CacheVersion.Halo2PC)]
        ReflectionLodFar,
        Unused,
        [TagEnumMember(MaxVersion = CacheVersion.Halo3ODST)]
        [TagEnumMember(MinVersion = CacheVersion.HaloReach)]
        Unused1,
        [TagEnumMember(MaxVersion = CacheVersion.Halo2PC)]
        Unused2,
        [TagEnumMember(MaxVersion = CacheVersion.Halo2Beta)]
        Unused3,
        [TagEnumMember(MinVersion = CacheVersion.Halo3Beta)]
        AmbientFlock,
        [TagEnumMember(MaxVersion = CacheVersion.Halo2Beta)]
        ScriptedDialoguePlayer,
        [TagEnumMember(MinVersion = CacheVersion.Halo3Beta)]
        NoPad,
        [TagEnumMember(MinVersion = CacheVersion.Halo3Beta)]
        NoPadStationary,
        [TagEnumMember(MinVersion = CacheVersion.Halo3Beta, MaxVersion = CacheVersion.Halo3Retail)]
        MissionUnused0,
        [TagEnumMember(MinVersion = CacheVersion.Halo3ODST, MaxVersion = CacheVersion.HaloOnline700123)]
        Arg,
        [TagEnumMember(MinVersion = CacheVersion.HaloReach)]
        EquipmentEffect,
        [TagEnumMember(MaxVersion = CacheVersion.Halo2Beta)]
        ScriptedEffect,
        [TagEnumMember(MaxVersion = CacheVersion.Halo2Beta)]
        ScriptedDialogueOther,
        [TagEnumMember(MaxVersion = CacheVersion.Halo2Beta)]
        ScriptedDialogueForceUnspatialized,
        [TagEnumMember(MinVersion = CacheVersion.Halo2Xbox, MaxVersion = CacheVersion.HaloOnline700123)]
        CortanaMission,
        [TagEnumMember(MinVersion = CacheVersion.Halo2Xbox, MaxVersion = CacheVersion.Halo2PC)]
        CortanaCinematic,
        [TagEnumMember(MinVersion = CacheVersion.Halo3Beta, MaxVersion = CacheVersion.HaloOnline700123)]
        CortanaGravemindChannel,
        [TagEnumMember(MinVersion = CacheVersion.Halo2Xbox)]
        MissionDialogue,
        [TagEnumMember(MaxVersion = CacheVersion.Halo2Alpha)]
        DialogueUnused0,
        [TagEnumMember(MinVersion = CacheVersion.Halo2Beta)]
        CinematicDialogue,
        [TagEnumMember(MaxVersion = CacheVersion.Halo2Beta)]
        DialogueUnused1,
        [TagEnumMember(MinVersion = CacheVersion.Halo2Xbox)]
        ScriptedCinematicFoley,
        [TagEnumMember(Gen = CacheGeneration.HaloOnline)]
        Hud,
        GameEvent,
        [TagEnumMember(MinVersion = CacheVersion.Halo2Xbox)]
        Ui,
        [TagEnumMember(MinVersion = CacheVersion.Halo2Beta)]
        Test,
        [TagEnumMember(MinVersion = CacheVersion.Halo2Beta, MaxVersion = CacheVersion.HaloOnline700123)]
        MultilingualTest,
        [TagEnumMember(MinVersion = CacheVersion.HaloReach)]
        MultiplayerDialogue,
        [TagEnumMember(MinVersion = CacheVersion.Halo3ODST)]
        AmbientNatureDetails,
        [TagEnumMember(MinVersion = CacheVersion.Halo3ODST)]
        AmbientMachineryDetails,
        [TagEnumMember(MinVersion = CacheVersion.Halo3ODST)]
        InsideSurroundTail,
        [TagEnumMember(MinVersion = CacheVersion.Halo3ODST)]
        OutsideSurroundTail,
        [TagEnumMember(MinVersion = CacheVersion.Halo3ODST)]
        VehicleDetonation,
        [TagEnumMember(MinVersion = CacheVersion.Halo3ODST)]
        AmbientDetonation,
        [TagEnumMember(MinVersion = CacheVersion.Halo3ODST)]
        FirstPersonInside,
        [TagEnumMember(MinVersion = CacheVersion.Halo3ODST)]
        FirstPersonOutside,
        [TagEnumMember(MinVersion = CacheVersion.Halo3ODST)]
        FirstPersonAnywhere,
        [TagEnumMember(MinVersion = CacheVersion.Halo3ODST, MaxVersion = CacheVersion.HaloOnline700123)]
        UiPda,
        [TagEnumMember(MinVersion = CacheVersion.HaloReach)]
        SpaceProjectileDetonation,
        [TagEnumMember(MinVersion = CacheVersion.HaloReach)]
        SpaceProjectileFlyby,
        [TagEnumMember(MinVersion = CacheVersion.HaloReach)]
        SpaceVehicleEngine,
        [TagEnumMember(MinVersion = CacheVersion.HaloReach)]
        SpaceWeaponFire,
        [TagEnumMember(MinVersion = CacheVersion.HaloReach)]
        PlayerVoiceTeam,
        [TagEnumMember(MinVersion = CacheVersion.HaloReach)]
        PlayerVoiceProxy,
        [TagEnumMember(MinVersion = CacheVersion.HaloReach)]
        ProjectileImpactPostpone,
        [TagEnumMember(MinVersion = CacheVersion.HaloReach)]
        UnitFootstepPostpone,
        [TagEnumMember(MinVersion = CacheVersion.HaloReach)]
        WeaponReadyThirdPerson,
        [TagEnumMember(MinVersion = CacheVersion.HaloReach)]
        UiMusic
    }

    static class SoundClassExtensions
    {
        public static SoundClass Convert(this SoundClass soundClass)
        {
            return soundClass switch
            {
                Unused1Impacts
                or Unused2Impacts
                or Unused1
                or Unused2
                or Unused3
                or MissionUnused0
                or DialogueUnused0
                or DialogueUnused1 => Unused,

                // Gen1
                SlowParticleImpacts => ParticleImpacts,
                DeviceForceField => DeviceStationary,
                DeviceComputers => DeviceStationary,
                FirstPersonDamage => FirstPersonAnywhere,
                // Gen1, Gen2 pre-release builds
                ScriptedDialoguePlayer => CortanaMission,
                ScriptedEffect => ScriptedCinematicFoley,
                ScriptedDialogueOther => MissionDialogue,
                ScriptedDialogueForceUnspatialized => CortanaGravemindChannel,
                // Gen2 MCC
                Reflection => Unused,
                ReflectionLod => Unused,
                ReflectionLodFar => Unused,
                // Reach
                EquipmentEffect => ObjectLooping,
                WaterTransitions => AmbientNature,
                MultiplayerDialogue => GameEvent,
                SpaceProjectileDetonation => ProjectileDetonation,
                SpaceProjectileFlyby => ProjectileFlyby,
                SpaceVehicleEngine => VehicleEngine,
                SpaceWeaponFire => WeaponFire,
                PlayerVoiceTeam => GameEvent,
                PlayerVoiceProxy => GameEvent,
                ProjectileImpactPostpone => ProjectileImpact,
                UnitFootstepPostpone => UnitFootsteps,
                WeaponReadyThirdPerson => WeaponReady,
                UiMusic => Music,

                // Fix the surround sound class
                FirstPersonInside => InsideSurroundTail,
                FirstPersonOutside => OutsideSurroundTail,

                _ => soundClass
            };
        }
    }
}