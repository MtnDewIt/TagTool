using System;
using System.IO;
using TagTool.Cache;
using TagTool.Tags.Definitions;

namespace TagTool.Porting.Gen3
{
    public partial class PortingContextGen3
    {
        private Weapon ConvertWeapon(Stream cacheStream, Stream blamCacheStream, Weapon weapon)
        {
            //fix weapon target tracking
            if (weapon.Tracking > 0 || weapon.WeaponType == Weapon.WeaponTypeValue.Needler)
            {
                weapon.TargetTracking =
                [
                    new Unit.TargetTrackingBlock
                    {
                        AcquireTime = (weapon.Tracking == Weapon.TrackingType.HumanTracking ? 1.0f : 0.0f),
                        GraceTime = (weapon.WeaponType == Weapon.WeaponTypeValue.Needler ? 0.2f : 0.1f),
                        DecayTime = (weapon.WeaponType == Weapon.WeaponTypeValue.Needler ? 0.0f : 0.2f),
                        TrackingTypes = (weapon.Tracking == Weapon.TrackingType.HumanTracking ?
                            [
                                new Unit.TargetTrackingBlock.TrackingType{ TrackingType2 = CacheContext.StringTable.GetStringId("ground_vehicles") },
                                new Unit.TargetTrackingBlock.TrackingType{ TrackingType2 = CacheContext.StringTable.GetStringId("flying_vehicles") },
                            ]
                            :
                            [new Unit.TargetTrackingBlock.TrackingType{ TrackingType2 = CacheContext.StringTable.GetStringId("bipeds")}]
                        )
                    }
                ];

                if (weapon.Tracking == Weapon.TrackingType.HumanTracking)
                {
                    weapon.TargetTracking[0].TrackingSound = ConvertTag(cacheStream, blamCacheStream, weapon.TargetTracking[0].TrackingSound);
                    weapon.TargetTracking[0].LockedSound = ConvertTag(cacheStream, blamCacheStream, weapon.TargetTracking[0].LockedSound);
                }
            }

            return weapon;
        }

        private object ConvertWeaponFlags(WeaponFlags weaponFlags)
        {
            // TODO: refactor for Halo 2

            if (CacheVersionDetection.IsInGen(CacheGeneration.HaloOnline, BlamCache.Version))
                return weaponFlags;

            if (BlamCache.Platform == CachePlatform.MCC)
            {
                weaponFlags.NewFlagsMCC &= ~WeaponFlags.NewWeaponFlagsMCC.WeaponUsesOldDualFireErrorCode;

                if (!Enum.TryParse(weaponFlags.NewFlagsMCC.ToString(), out weaponFlags.NewFlags))
                    throw new FormatException(BlamCache.Version.ToString());

                return weaponFlags;
            }

            if (weaponFlags.OldFlags.HasFlag(WeaponFlags.OldWeaponFlags.WeaponUsesOldDualFireErrorCode))
                weaponFlags.OldFlags &= ~WeaponFlags.OldWeaponFlags.WeaponUsesOldDualFireErrorCode;

            if (!Enum.TryParse(weaponFlags.OldFlags.ToString(), out weaponFlags.NewFlags))
                throw new FormatException(BlamCache.Version.ToString());

            return weaponFlags;
        }

        private object ConvertWeaponTrigger(Weapon.Trigger trigger)
        {
            if (BlamCache.Version <= CacheVersion.HaloOnline235640)
                return trigger;

            if (!Enum.TryParse(trigger.BehaviorHO.ToString(), out trigger.Behavior))
                throw new FormatException(BlamCache.Version.ToString());

            return trigger;
        }

        private object ConvertBarrelFlags(BarrelFlags barrelflags)
        {
            //fire locked projectiles flag has been removed completely in HO
            if (barrelflags.Halo3.HasFlag(BarrelFlags.Halo3Value.FiresLockedProjectiles))
                barrelflags.Halo3 &= ~BarrelFlags.Halo3Value.FiresLockedProjectiles;

            if (CacheVersionDetection.IsInGen(CacheGeneration.HaloOnline, BlamCache.Version))
                return barrelflags;

            if (!Enum.TryParse(barrelflags.Halo3.ToString(), out barrelflags.HaloOnline))
                throw new NotSupportedException(barrelflags.Halo3.ToString());

            return barrelflags;
        }

        private object ConvertTargetLockOnData(Model.TargetLockOnData data)
        {
            if (BlamCache.Version < CacheVersion.HaloOnlineED)
                data.Flags = data.FlagsOld.ConvertLexical<Model.TargetLockOnData.FlagsValue>();
            return data;
        }
    }
}
