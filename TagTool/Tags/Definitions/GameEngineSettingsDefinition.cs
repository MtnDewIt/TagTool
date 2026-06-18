using TagTool.Cache;
using TagTool.Common;
using System;
using System.Collections.Generic;
using static TagTool.Tags.TagFieldFlags;

namespace TagTool.Tags.Definitions
{
    [TagStructure(Name = "game_engine_settings_definition", Tag = "wezr", Size = 0x88, MaxVersion = CacheVersion.Halo3ODST)]
    [TagStructure(Name = "game_engine_settings_definition", Tag = "wezr", Size = 0x8C, MinVersion = CacheVersion.HaloOnlineED, MaxVersion = CacheVersion.HaloOnline700123)]
    [TagStructure(Name = "game_engine_settings_definition", Tag = "wezr", Size = 0x40, MinVersion = CacheVersion.HaloReach)]
    public class GameEngineSettingsDefinition : TagStructure
	{
        public FlagsValue Flags;
        public List<TraitProfile> TraitProfiles;

        [TagField(MaxVersion = CacheVersion.HaloOnline700123)]
        public List<SlayerVariant> SlayerVariants;
        [TagField(MaxVersion = CacheVersion.HaloOnline700123)]
        public List<OddballVariant> OddballVariants;
        [TagField(MaxVersion = CacheVersion.HaloOnline700123)]
        public List<CaptureTheFlagVariant> CaptureTheFlagVariants;
        [TagField(MaxVersion = CacheVersion.HaloOnline700123)]
        public List<AssaultVariant> AssaultVariants;
        [TagField(MaxVersion = CacheVersion.HaloOnline700123)]
        public List<InfectionVariant> InfectionVariants;
        [TagField(MaxVersion = CacheVersion.HaloOnline700123)]
        public List<KingOfTheHillVariant> KingOfTheHillVariants;
        [TagField(MaxVersion = CacheVersion.HaloOnline700123)]
        public List<TerritoriesVariant> TerritoriesVariants;
        [TagField(MaxVersion = CacheVersion.HaloOnline700123)]
        public List<JuggernautVariant> JuggernautVariants;
        [TagField(MaxVersion = CacheVersion.HaloOnline700123)]
        public List<VipVariant> VipVariants;

        [TagField(MinVersion = CacheVersion.HaloReach)]
        public List<AiTraitProfile> AiTraitProfiles;

        public List<SandboxEditorVariant> SandboxEditorVariants;

        [TagField(MinVersion = CacheVersion.HaloReach)]
        public List<SurvivalVariant> SurvivalVariants;
        [TagField(MinVersion = CacheVersion.HaloReach)]
        public List<CampaignVariant> CampaignVariants;

        [TagField(MinVersion = CacheVersion.HaloOnlineED, MaxVersion = CacheVersion.HaloOnline700123)]
        public uint Unknown2;

        [Flags]
        public enum FlagsValue : int
        {
            None,
            Unused = 1 << 0
        }

        public enum ToggleValueReach : sbyte
        {
            Unchanged,
            Disabled,
            Enabled
        }

        [TagStructure(Size = 0x40)]
        public class TraitProfile : TagStructure
		{
            [TagField(Flags = Label)]
            public StringId Name;
            public List<ShieldsAndHealthBlock> ShieldsAndHealth;
            public List<WeaponsAndDamageBlock> WeaponsAndDamage;
            public List<MovementBlock> Movement;
            public List<AppearanceBlock> Appearance;
            public List<Sensor> Sensors;

            [TagStructure(Size = 0x8, MaxVersion = CacheVersion.HaloOnline700123)]
            [TagStructure(Size = 0xC, MinVersion = CacheVersion.HaloReach)]
            public class ShieldsAndHealthBlock : TagStructure
			{
                public DamageResistanceValue DamageResistance;

                [TagField(MinVersion = CacheVersion.HaloReach)]
                public HealthMultiplierValue HealthMultiplier;
                [TagField(MinVersion = CacheVersion.HaloReach)]
                public HealthRechargeRateValue HealthRechargeRate;

                public ShieldMultiplierValue ShieldMultiplier;
                public ShieldRechargeRateValue ShieldRechargeRate;

                [TagField(MinVersion = CacheVersion.HaloReach)]
                public ShieldRechargeRate2Value ShieldRechargeRate2;

                public HeadshotImmunityValue HeadshotImmunity;
                public ShieldVampirismValue ShieldVampirism;

                [TagField(MinVersion = CacheVersion.HaloReach)]
                public AssassinationImmunityValue AssassinationImmunity;
                [TagField(MinVersion = CacheVersion.HaloReach)]
                public ToggleValueReach Deathless;

                [TagField(MaxVersion = CacheVersion.HaloOnline700123)]
                public sbyte Unknown;

                public sbyte Unknown2;
                public sbyte Unknown3;

                public enum DamageResistanceValue : sbyte
                {
                    Unchanged,
                    _10,
                    _50,
                    _90,
                    _100,
                    _110,
                    _150,
                    _200,
                    _300,
                    _500,
                    _1000,
                    _2000,
                    Invulnerable
                }

                public enum ShieldMultiplierValue : sbyte
                {
                    Unchanged,
                    NoShields,
                    NormalShields,
                    _2xOvershields,
                    _3xOvershields,
                    _4xOvershields
                }

                public enum ShieldRechargeRateValue : sbyte
                {
                    Unchanged,
                    _25,
                    _10,
                    _5,
                    _0,
                    _50,
                    _90,
                    _100,
                    _110,
                    _200
                }

                public enum HeadshotImmunityValue : sbyte
                {
                    Unchanged,
                    Enabled,
                    Disabled
                }

                public enum ShieldVampirismValue : sbyte
                {
                    Unchanged,
                    Disabled,
                    _10,
                    _25,
                    _50,
                    _100
                }

                public enum HealthMultiplierValue : sbyte
                {
                    Unchanged,
                    _0,
                    _100,
                    _150,
                    _200,
                    _300,
                    _400
                }

                public enum HealthRechargeRateValue : sbyte
                {
                    Unchanged,
                    _neg25,
                    _neg10,
                    _neg5,
                    _0,
                    _10,
                    _25,
                    _50,
                    _75,
                    _90,
                    _100,
                    _110,
                    _125,
                    _150,
                    _200
                }

                public enum ShieldRechargeRate2Value : sbyte
                {
                    Unchanged,
                    _neg25,
                    _neg10,
                    _neg5,
                    _0,
                    _10,
                    _25,
                    _50,
                    _75,
                    _90,
                    _100,
                    _110,
                    _125,
                    _150,
                    _200
                }

                public enum AssassinationImmunityValue : sbyte
                {
                    Unchanged,
                    Enabled,
                    Disabled
                }

            }

            [TagStructure(Size = 0x10, MaxVersion = CacheVersion.HaloOnline700123)]
            [TagStructure(Size = 0x18, MinVersion = CacheVersion.HaloReach)]
            public class WeaponsAndDamageBlock : TagStructure
			{
                public DamageModifierValue DamageModifier;

                [TagField(MinVersion = CacheVersion.HaloReach)]
                public DamageModifierValue MeleeDamageModifier;

                public GrenadeRegenerationValue GrenadeRegeneration;
                public WeaponPickupValue WeaponPickup;

                [TagField(MinVersion = CacheVersion.HaloReach)]
                public GrenadeCountValueReach GrenadeCountReach;

                public InfiniteAmmoValue InfiniteAmmo;

                [TagField(MinVersion = CacheVersion.HaloReach)]
                public ToggleValueReach EquipmentUsage;
                [TagField(MinVersion = CacheVersion.HaloReach)]
                public ToggleValueReach EquipmentDrop;
                [TagField(MinVersion = CacheVersion.HaloReach)]
                public ToggleValueReach InfiniteEquipment;

                [TagField(MinVersion = CacheVersion.HaloReach)]
                public sbyte UnknownReach0;
                [TagField(MinVersion = CacheVersion.HaloReach)]
                public sbyte UnknownReach1;
                [TagField(MinVersion = CacheVersion.HaloReach)]
                public sbyte UnknownReach2;

                public StringId PrimaryWeapon;
                public StringId SecondaryWeapon;

                [TagField(MinVersion = CacheVersion.HaloReach)]
                public StringId Equipment;

                [TagField(MaxVersion = CacheVersion.HaloOnline700123)]
                public GrenadeCountValue GrenadeCount;
                [TagField(MaxVersion = CacheVersion.HaloOnline700123)]
                public sbyte Unknown;
                [TagField(MaxVersion = CacheVersion.HaloOnline700123)]
                public sbyte Unknown2;

                public enum DamageModifierValue : sbyte
                {
                    Unchanged,
                    _0,
                    _25,
                    _50,
                    _75,
                    _90,
                    _100,
                    _110,
                    _125,
                    _150,
                    _200,
                    _300,
                    InstantKill
                }

                public enum GrenadeRegenerationValue : sbyte
                {
                    Unchanged,
                    Enabled,
                    Disabled
                }

                public enum WeaponPickupValue : sbyte
                {
                    Unchanged,
                    Enabled,
                    Disabled
                }

                public enum InfiniteAmmoValue : sbyte
                {
                    Unchanged,
                    Disabled,
                    Enabled
                }

                public enum GrenadeCountValue : short
                {
                    Unchanged,
                    MapDefault,
                    None
                }

                public enum GrenadeCountValueReach : sbyte
                {
                    Unchanged,
                    MapDefault,
                    _1_Frag,
                    _2_Frag,
                    _3_Frag,
                    _4_Frag,
                    _1_Plasma,
                    _2_Plasma,
                    _3_Plasma,
                    _4_Plasma,
                    _1xEach,
                    _2xEach,
                    _3xEach,
                    _4xEach,
                    None
                }
            }

            [TagStructure(Size = 0x4, MaxVersion = CacheVersion.HaloOnline700123)]
            [TagStructure(Size = 0x8, MinVersion = CacheVersion.HaloReach)]
            public class MovementBlock : TagStructure
			{
                public PlayerSpeedValue PlayerSpeed;
                public PlayerGravityValue PlayerGravity;
                public VehicleUseValue VehicleUse;
                [TagField(MaxVersion = CacheVersion.HaloOnline700123)]
                public sbyte Unknown;

                [TagField(MinVersion = CacheVersion.HaloReach)]
                public ToggleValueReach DoubleJump;
                [TagField(MinVersion = CacheVersion.HaloReach)]
                public int JumpHeight;

                public enum PlayerSpeedValue : sbyte
                {
                    Unchanged,
                    _25,
                    _50,
                    _75,
                    _90,
                    _100,
                    _110,
                    _125,
                    _150,
                    _200,
                    _300
                }

                public enum PlayerGravityValue : sbyte
                {
                    Unchanged,
                    _50,
                    _75,
                    _100,
                    _150,
                    _200
                }

                public enum VehicleUseValue : sbyte
                {
                    Unchanged,
                    None,
                    PassengerOnly,
                    FullUse
                }
            }

            [TagStructure(Size = 0x4, MaxVersion = CacheVersion.HaloOnline700123)]
            [TagStructure(Size = 0x8, MinVersion = CacheVersion.HaloReach)]
            public class AppearanceBlock : TagStructure
			{
                public ActiveCamoValue ActiveCamo;
                public WaypointValue Waypoint;

                [TagField(MinVersion = CacheVersion.HaloReach)]
                public VisibleNameValue VisibleName;

                public AuraValue Aura;
                public ForcedColorValue ForcedColor;

                [TagField(MinVersion = CacheVersion.HaloReach)]
                public sbyte UnknownReach0;
                [TagField(MinVersion = CacheVersion.HaloReach)]
                public sbyte UnknownReach1;
                [TagField(MinVersion = CacheVersion.HaloReach)]
                public sbyte UnknownReach2;

                public enum ActiveCamoValue : sbyte
                {
                    Unchanged,
                    Disabled,
                    BadCamo,
                    PoorCamo,
                    GoodCamo
                }

                public enum WaypointValue : sbyte
                {
                    Unchanged,
                    None,
                    VisibleToAllies,
                    VisibleToEveryone
                }

                public enum AuraValue : sbyte
                {
                    Unchanged,
                    Disabled,
                    Team,
                    Black,
                    White
                }

                public enum ForcedColorValue : sbyte
                {
                    Unchanged,
                    Off,
                    Red,
                    Blue,
                    Green,
                    Orange,
                    Purple,
                    Gold,
                    Brown,
                    Pink,
                    White,
                    Black,
                    Zombie,
                    PinkUnused
                }

                public enum VisibleNameValue : sbyte
                {
                    Unchanged,
                    None,
                    VisibleToAllies,
                    VisibleToEveryone
                }

            }

            [TagStructure(Size = 0x8, MaxVersion = CacheVersion.HaloOnline700123)]
            [TagStructure(Size = 0x4, MinVersion = CacheVersion.HaloReach)]
            public class Sensor : TagStructure
			{
                [TagField(EnumType = typeof(int), MaxVersion = CacheVersion.HaloOnline700123)]
                [TagField(EnumType = typeof(sbyte), MinVersion = CacheVersion.HaloReach)]
                public MotionTrackerModeValue MotionTrackerMode;
                [TagField(EnumType = typeof(int), MaxVersion = CacheVersion.HaloOnline700123)]
                [TagField(EnumType = typeof(sbyte), MinVersion = CacheVersion.HaloReach)]
                public MotionTrackerRangeValue MotionTrackerRange;

                [TagField(MinVersion = CacheVersion.HaloReach)]
                public ToggleValueReach DirectionalDamageIndicator;
                [TagField(MinVersion = CacheVersion.HaloReach)]
                public sbyte UnknownReach;

                public enum MotionTrackerModeValue : int
                {
                    Unchanged,
                    Disabled,
                    AllyMovement,
                    PlayerMovement,
                    PlayerLocations
                }

                public enum MotionTrackerRangeValue : int
                {
                    Unchanged,
                    _10m,
                    _15m,
                    _25m,
                    _50m,
                    _75m,
                    _100m,
                    _150m
                }
            }

        }

        [TagStructure(Size = 0x38, MaxVersion = CacheVersion.Halo3ODST)]
        [TagStructure(Size = 0x58, MinVersion = CacheVersion.HaloOnlineED, MaxVersion = CacheVersion.HaloOnline700123)]
        [TagStructure(Size = 0x50, MinVersion = CacheVersion.HaloReach)]
        public abstract class BaseVariant : TagStructure
		{
            [TagField(Flags = Label, Length = 32, MinVersion = CacheVersion.HaloOnlineED, MaxVersion = CacheVersion.HaloOnline700123)]
            public string NameAscii;
            [TagField(Flags = Label)]
            public StringId Name;
            public StringId Description;
            public List<GeneralSetting> GeneralSettings;
            public List<RespawnSetting> RespawnSettings;
            public List<SocialSetting> SocialSettings;
            public List<MapOverride> MapOverrides;

            [TagField(MinVersion = CacheVersion.HaloReach)]
            public List<TeamOptions> Teams;
            [TagField(MinVersion = CacheVersion.HaloReach)]
            public List<LoadoutOptions> Loadouts;

            [TagStructure(Size = 0x8)]
            public class GeneralSetting : TagStructure
			{
                [TagField(MaxVersion = CacheVersion.HaloOnline700123)]
                public FlagsValue Flags;
                [TagField(MinVersion = CacheVersion.HaloReach)]
                public FlagsValueReach FlagsReach;

                [TagField(MinVersion = CacheVersion.HaloReach)]
                public sbyte EarlyVictoryWinCountReach;

                public sbyte TimeLimit;
                public sbyte NumberOfRounds;

                [TagField(MaxVersion = CacheVersion.HaloOnline700123)]
                public sbyte EarlyVictoryWinCount;
                [TagField(MaxVersion = CacheVersion.HaloOnline700123)]
                public RoundResetsValue RoundResets;

                [TagField(MinVersion = CacheVersion.HaloReach)]
                public short SuddenDeath;
                [TagField(MinVersion = CacheVersion.HaloReach)]
                public short GracePeriod;

                [Flags]
                public enum FlagsValue : int
                {
                    None,
                    TeamsEnabled = 1 << 0,
                    SpectatingEnabled = 1 << 1
                }

                [Flags]
                public enum FlagsValueReach : sbyte
                {
                    TeamsEnabled = 1 << 0,
                    RoundResetPlayers = 1 << 1,
                    RoundResetMap = 1 << 2,
                    PerfectionEnabled = 1 << 3,
                }

                public enum RoundResetsValue : sbyte
                {
                    Nothing,
                    PlayersOnly,
                    Everything,
                }
            }

            [TagStructure(Size = 0x10, MaxVersion = CacheVersion.Halo3ODST)]
            [TagStructure(Size = 0x14, MinVersion = CacheVersion.HaloOnlineED, MaxVersion = CacheVersion.HaloOnline700123)]
            [TagStructure(Size = 0x10, MinVersion = CacheVersion.HaloReach)]
            public class RespawnSetting : TagStructure
            {
                [TagField(MaxVersion = CacheVersion.Halo3ODST)]
                public RespawnFlagsH3 FlagsH3;
                [TagField(MinVersion = CacheVersion.HaloOnlineED)]
                public RespawnFlags Flags;
                public sbyte LivesPerRound;
                public sbyte SharedTeamLives;

                [TagField(MinVersion = CacheVersion.HaloOnlineED, MaxVersion = CacheVersion.HaloOnline700123)]
                public byte Unknown;

                public byte RespawnTime;
                public byte SuicidePenalty;
                public byte BetrayalPenalty;
                public byte RespawnTimeGrowth;

                [TagField(MinVersion = CacheVersion.HaloOnlineED, MaxVersion = CacheVersion.HaloOnline700123)]
                public sbyte Unknown1;
                [TagField(MinVersion = CacheVersion.HaloOnlineED, MaxVersion = CacheVersion.HaloOnline700123)]
                public sbyte Unknown2;
                [TagField(MinVersion = CacheVersion.HaloOnlineED, MaxVersion = CacheVersion.HaloOnline700123)]
                public sbyte Unknown3;

                public StringId RespawnTraitProfile;

                [TagField(MinVersion = CacheVersion.HaloReach)]
                public sbyte LoadoutSelectionTime;

                public sbyte RespawnTraitDuration;
                public sbyte Unknown4;
                public sbyte Unknown5;

                [TagField(MaxVersion = CacheVersion.HaloOnline700123)]
                public sbyte Unknown6;

                [Flags]
                public enum RespawnFlagsH3 : ushort
                {
                    None,
                    InheritRespawnTime = 1 << 0,
                    RespawnWithTeam = 1 << 1,
                    RespawnAtLocation = 1 << 2,
                    RespawnOnKills = 1 << 3
                }
                [Flags]
                public enum RespawnFlags : ushort
                {
                    None,
                    InheritRespawnTime = 1 << 0,
                    RespawnWithTeam = 1 << 1,
                    RespawnAtLocation = 1 << 2,
                    RespawnOnKills = 1 << 3,
                    EarlyRespawnEnabled = 1 << 4
                }
            }

            [TagStructure(Size = 0x4)]
            public class SocialSetting : TagStructure
			{
                [TagField(MaxVersion = CacheVersion.Halo3ODST)]
                public SocialFlagsH3 FlagsH3;
                [TagField(MinVersion = CacheVersion.HaloOnlineED)]
                public SocialFlags Flags;

                [Flags]
                public enum SocialFlagsH3 : int
                {
                    None,
                    ObserversEnabled = 1 << 0,
                    TeamChangingEnabled = 1 << 1,
                    BalancedTeamChanging = 1 << 2,
                    FriendlyFireEnabled = 1 << 3,
                    BetrayalBootingEnabled = 1 << 4,
                    EnemyVoiceEnabled = 1 << 5,
                    OpenChannelVoiceEnabled = 1 << 6,
                    DeadPlayerVoiceEnabled = 1 << 7
                }
                [Flags]
                public enum SocialFlags : int
                {
                    None,
                    ObserversEnabled = 1 << 0,
                    TeamChangingEnabled = 1 << 1,
                    BalancedTeamChanging = 1 << 2,
                    FriendlyFireEnabled = 1 << 3,
                    BetrayalBootingEnabled = 1 << 4,
                    EnemyVoiceEnabled = 1 << 5,
                    OpenChannelVoiceEnabled = 1 << 6,
                    DeadPlayerVoiceEnabled = 1 << 7,
                    SpartansVsElitesEnabled = 1 << 8
                }
            }

            [TagStructure(Size = 0x20, MaxVersion = CacheVersion.HaloOnline700123)]
            [TagStructure(Size = 0x1C, MinVersion = CacheVersion.HaloReach)]
            public class MapOverride : TagStructure
			{
                [TagField(MaxVersion = CacheVersion.HaloOnline700123)]
                public FlagsValue Flags;

                public StringId BasePlayerTraitProfile;
                public StringId WeaponSet;
                public StringId VehicleSet;
                public StringId OvershieldTraitProfile;
                public StringId ActiveCamoTraitProfile;
                public StringId CustomPowerupTraitProfile;
                public sbyte OvershieldTraitDuration;
                public sbyte ActiveCamoTraitDuration;
                public sbyte CustomPowerupTraitDuration;

                [TagField(MaxVersion = CacheVersion.HaloOnline700123)]
                public sbyte Unknown;

                [TagField(MinVersion = CacheVersion.HaloReach)]
                public MapOverrideOptionsFlagsReach FlagsReach;

                [Flags]
                public enum FlagsValue : int
                {
                    None,
                    GrenadesOnMap,
                    IndestructableVehicles
                }

                [Flags]
                public enum MapOverrideOptionsFlagsReach : sbyte
                {
                    GrenadesOnMap = 1 << 0,
                    ShortcutsOnMap = 1 << 1,
                    EquipmentOnMap = 1 << 2,
                    PowerupsOnMap = 1 << 3,
                    TurretsOnMap = 1 << 4,
                    IndestructibleVehicles = 1 << 5,
                }
            }
        }

        [TagStructure(Size = 0x18)]
        public class SlayerVariant : BaseVariant
        {
            public TeamScoringValue TeamScoring;
            public short PointsToWin;
            [TagField(MinVersion = CacheVersion.HaloOnlineED)]
            public short Unknown;
            public sbyte KillPoints;
            public sbyte AssistPoints;
            public sbyte DeathPoints;
            public sbyte SuicidePoints;
            public sbyte BetrayalPoints;
            public sbyte LeaderKillBonus;
            public sbyte EliminationBonus;
            public sbyte AssassinationBonus;
            public sbyte HeadshotBonus;
            public sbyte BeatdownBonus;
            public sbyte StickyBonus;
            public sbyte SplatterBonus;
            public sbyte SpreeBonus;
            public sbyte Unknown2;
            [TagField(MaxVersion = CacheVersion.Halo3ODST)]
            public sbyte Unknown3;
            [TagField(MaxVersion = CacheVersion.Halo3ODST)]
            public sbyte Unknown4;
            public StringId LeaderTraitProfile;

            public enum TeamScoringValue : short
            {
                SumOfTeam,
                MinimumScore,
                MaximumScore,
                Default
            }
        }

        [TagStructure(Size = 0x18)]
        public class OddballVariant : BaseVariant
        {
            public FlagsValue Flags;
            public TeamScoringValue TeamScoring;
            public short PointsToWin;
            [TagField(MinVersion = CacheVersion.HaloOnlineED)]
            public short Unknown;
            public sbyte CarryingPoints;
            public sbyte KillPoints;
            public sbyte BallKillPoints;
            public sbyte BallCarrierKillPoints;
            public sbyte BallCount;
            public sbyte Unknown2;
            [TagField(MaxVersion = CacheVersion.Halo3ODST)]
            public sbyte Unknown3;
            [TagField(MaxVersion = CacheVersion.Halo3ODST)]
            public sbyte Unknown4;
            public short InitialBallDelay;
            public short BallRespawnDelay;
            public StringId BallCarrierTraitProfile;

            [Flags]
            public enum FlagsValue : int
            {
                None,
                AutopickupEnabled = 1 << 0,
                BallEffectEnabled = 1 << 1
            }

            public enum TeamScoringValue : short
            {
                SumOfTeam,
                MinimumScore,
                MaximumScore,
                Default
            }
        }

        [TagStructure(Size = 0x18)]
        public class CaptureTheFlagVariant : BaseVariant
        {
            public FlagsValue Flags;
            public HomeFlagWaypointValue HomeFlagWaypoint;
            public GameModeValue GameMode;
            public RespawnOnCaptureValue RespawnOnCapture;
            public short FlagReturnTime;
            public short SuddenDeathTime;
            public short ScoreToWin;
            [TagField(MinVersion = CacheVersion.HaloOnlineED)]
            public short Unknown;
            public short FlagResetTime;
            [TagField(MaxVersion = CacheVersion.Halo3ODST)]
            public sbyte Unknown1;
            [TagField(MaxVersion = CacheVersion.Halo3ODST)]
            public sbyte Unknown2;
            public StringId FlagCarrierTraitProfile;

            [Flags]
            public enum FlagsValue : int
            {
                None,
                FlagAtHomeToScore = 1 << 0
            }

            public enum HomeFlagWaypointValue : short
            {
                Unknown1,
                Unknown2,
                Unknown3,
                NotInSingle
            }

            public enum GameModeValue : short
            {
                Multiple,
                Single,
                Neutral
            }

            public enum RespawnOnCaptureValue : short
            {
                Disabled,
                OnAllyCapture,
                OnEnemyCapture,
                OnAnyCapture
            }
        }

        [TagStructure(Size = 0x20, MaxVersion = CacheVersion.Halo3ODST)]
        [TagStructure(Size = 0x28, MinVersion = CacheVersion.HaloOnlineED)]
        public class AssaultVariant : BaseVariant
        {
            [TagField(MaxVersion = CacheVersion.Halo3ODST)]
            public FlagsValue FlagsH3;
            [TagField(MinVersion = CacheVersion.HaloOnlineED, MaxVersion = CacheVersion.HaloOnline700123)]
            public FlagsValueHO FlagsHO;
            public RespawnOnCaptureValue RespawnOnCapture;
            public GameModeValue GameMode;
            public EnemyBombWaypointValue EnemyBombWaypoint;
            public short SuddenDeathTime;
            public short DetonationsToWin;
            [TagField(MinVersion = CacheVersion.HaloOnlineED)]
            public short Unknown;
            [TagField(MinVersion = CacheVersion.HaloOnlineED)]
            public short Unknown2;
            [TagField(MinVersion = CacheVersion.HaloOnlineED)]
            public short Unknown3;
            [TagField(MinVersion = CacheVersion.HaloOnlineED)]
            public short Unknown4;
            public short BombResetTime;
            public short BombArmingTime;
            public short BombDisarmingTime;
            public short BombFuseTime;
            public short Unknown5;
            public StringId BombCarrierTraitProfile;
            public StringId UnknownTraitProfile;

            [Flags]
            public enum FlagsValue : int
            {
                None,
                ResetOnDisarm = 1 << 0
            }
            [Flags]
            public enum FlagsValueHO : int
            {
                None,
                ResetOnDisarm = 1 << 0,
                SiegeMode = 1 << 1
            }

            public enum RespawnOnCaptureValue : short
            {
                Disabled,
                OnAllyCapture,
                OnEnemyCapture,
                OnAnyCapture
            }

            public enum GameModeValue : short
            {
                Multiple,
                Single,
                Neutral
            }

            public enum EnemyBombWaypointValue : short
            {
                Unknown1,
                Unknown2,
                Unknown3,
                NotInSingle
            }
        }

        [TagStructure(Size = 0x24)]
        public class InfectionVariant : BaseVariant
        {
            public FlagsValue Flags;
            public SafeHavensValue SafeHavens;
            public NextZombieValue NextZombie;
            public short InitialZombieCount;
            public short SafeHavenMovementTime;
            public sbyte ZombieKillPoints;
            public sbyte InfectionPoints;
            public sbyte SafeHavenArrivalPoints;
            public sbyte SuicidePoints;
            public sbyte BetrayalPoints;
            public sbyte LastManStandingBonus;
            public sbyte Unknown;
            public sbyte Unknown2;
            public StringId ZombieTraitProfile;
            public StringId AlphaZombieTraitProfile;
            public StringId OnHavenTraitProfile;
            public StringId LastHumanTraitProfile;

            [Flags]
            public enum FlagsValue : int
            {
                None,
                RespawnOnHavenMove = 1 << 0
            }

            public enum SafeHavensValue : short
            {
                None,
                Random,
                Sequence
            }

            public enum NextZombieValue : short
            {
                MostPoints,
                FirstInfected,
                Unchanged,
                Random
            }
        }

        [TagStructure(Size = 0x14, MaxVersion = CacheVersion.Halo3ODST)]
        [TagStructure(Size = 0x18, MinVersion = CacheVersion.HaloOnlineED)]
        public class KingOfTheHillVariant : BaseVariant
        {
            public FlagsValue Flags;
            public short ScoreToWin;
            [TagField(MinVersion = CacheVersion.HaloOnlineED)]
            public short Unknown;
            public TeamScoringValue TeamScoring;
            public HillMovementValue HillMovement;
            public HillMovementOrderValue HillMovementOrder;
            [TagField(MinVersion = CacheVersion.HaloOnlineED)]
            public short Unknown2;
            public sbyte OnHillPoints;
            public sbyte UncontestedControlPoints;
            public sbyte OffHillPoints;
            public sbyte KillPoints;
            public StringId OnHillTraitProfile;

            [Flags]
            public enum FlagsValue : int
            {
                None,
                OpaqueHill = 1 << 0
            }

            public enum TeamScoringValue : short
            {
                Sum,
                Minimum,
                Maximum,
                Default
            }

            public enum HillMovementValue : short
            {
                NoMovement,
                After10Seconds,
                After15Seconds,
                After30Seconds,
                After1Minute,
                After2Minutes,
                After3Minutes,
                After4Minutes,
                After5Minutes
            }

            public enum HillMovementOrderValue : short
            {
                Random,
                Sequence
            }
        }

        [TagStructure(Size = 0x14)]
        public class TerritoriesVariant : BaseVariant
        {
            public FlagsValue Flags;
            public RespawnOnCaptureValue RespawnOnCapture;
            public short TerritoryCaptureTime;
            public short SuddenDeathTime;
            public short Unknown;
            public StringId DefenderTraitProfile;
            public StringId AttackerTraitProfile;

            [Flags]
            public enum FlagsValue : int
            {
                None,
                OneSided = 1 << 0,
                LockAfterFirstCapture = 1 << 1
            }

            public enum RespawnOnCaptureValue : short
            {
                Disabled,
                OnAllyCapture,
                OnEnemyCapture,
                OnAnyCapture
            }
        }

        [TagStructure(Size = 0x1C)]
        public class JuggernautVariant : BaseVariant
        {
            public FlagsValue Flags;
            public FirstJuggernautValue FirstJuggernaut;
            public NextJuggernautValue NextJuggernaut;
            public GoalZoneMovementValue GoalZoneMovement;
            public GoalZoneOrderValue GoalZoneOrder;
            public short ScoreToWin;
            [TagField(MinVersion = CacheVersion.HaloOnlineED)]
            public short Unknown;
            public sbyte KillPoints;
            public sbyte TakedownPoints;
            public sbyte KillAsJuggernautPoints;
            public sbyte GoalArrivalPoints;
            public sbyte SuicidePoints;
            public sbyte BetrayalPoints;
            public sbyte NextJuggernautDelay;
            public sbyte Unknown2;
            [TagField(MaxVersion = CacheVersion.Halo3ODST)]
            public sbyte Unknown3;
            [TagField(MaxVersion = CacheVersion.Halo3ODST)]
            public sbyte Unknown4;
            public StringId JuggernautTraitProfile;

            [Flags]
            public enum FlagsValue : int
            {
                None,
                AlliedAgainstJuggernaut = 1 << 0,
                RespawnOnLoneJuggernaut = 1 << 1,
                GoalZonesEnabled = 1 << 2
            }

            public enum FirstJuggernautValue : short
            {
                Random,
                FirstKill,
                FirstDeath,
            }

            public enum NextJuggernautValue : short
            {
                Killer,
                Killed,
                Unchanged,
                Random,
            }

            public enum GoalZoneMovementValue : short
            {
                NoMovement,
                After10Seconds,
                After15Seconds,
                After30Seconds,
                After1Minute,
                After2Minutes,
                After3Minutes,
                After4Minutes,
                After5Minutes,
                OnArrival,
                OnNewJuggernaut,
            }

            public enum GoalZoneOrderValue : short
            {
                Random,
                Sequence,
            }
        }

        [TagStructure(Size = 0x24)]
        public class VipVariant : BaseVariant
        {
            public FlagsValue Flags;
            public short ScoreToWin;
            [TagField(MinVersion = CacheVersion.HaloOnlineED)]
            public short Unknown;
            public NextVipValue NextVip;
            public GoalZoneMovementValue GoalZoneMovement;
            public GoalZoneMovementOrderValue GoalZoneMovementOrder;
            public sbyte KillPoints;
            public sbyte VipTakedownPoints;
            public sbyte KillAsVipPoints;
            public sbyte VipDeathPoints;
            public sbyte GoalArrivalPoints;
            public sbyte SuicidePoints;
            public sbyte VipBetrayalPoints;
            public sbyte BetrayalPoints;
            public sbyte VipProximityTraitRadius;
            public sbyte Unknown2;
            [TagField(MaxVersion = CacheVersion.Halo3ODST)]
            public sbyte Unknown3;
            [TagField(MaxVersion = CacheVersion.Halo3ODST)]
            public sbyte Unknown4;
            public StringId VipTeamTraitProfile;
            public StringId VipProximityTraitProfile;
            public StringId VipTraitProfile;

            [Flags]
            public enum FlagsValue : int
            {
                None,
                SingleVip = 1 << 0,
                GoalZonesEnabled = 1 << 1,
                EndRoundOnVipDeath = 1 << 2
            }

            public enum NextVipValue : short
            {
                Random,
                Unknown,
                NextDeath,
                Unchanged,
            }

            public enum GoalZoneMovementValue : short
            {
                NoMovement,
                After10Seconds,
                After15Seconds,
                After30Seconds,
                After1Minute,
                After2Minutes,
                After3Minutes,
                After4Minutes,
                After5Minutes,
                OnArrival,
                OnNewVip,
            }

            public enum GoalZoneMovementOrderValue : short
            {
                Random,
                Sequence,
            }
        }

        [TagStructure(Size = 0xC)]
        public class SandboxEditorVariant : BaseVariant
        {
            public FlagsValue Flags;
            public EditModeValue EditMode;
            public short EditorRespawnTime;
            public StringId EditorTraitProfile;

            [Flags]
            public enum FlagsValue : int
            {
                None,
                OpenChannelVoiceEnabled = 1 << 0,
                Unknown = 1 << 1,
            }

            public enum EditModeValue : short
            {
                Everyone,
                LeaderOnly
            }
        }

        [TagStructure(Size = 0x10, MinVersion = CacheVersion.HaloReach)]
        public class AiTraitProfile
        {
            public StringId Name;
            public AiVisionValue Vision;
            public AiHearingValue Hearing;
            public AiLuckValue Luck;
            public AiShootinessValue Shootiness;
            public AiGrenadesValue Grenades;
            public ToggleValueReach EquipmentDrop;
            public ToggleValueReach AssassinationImmunity;
            public ToggleValueReach HeadshotImmunity;
            public AiDamageResistanceValue DamageResistance;
            public AiDamageModifierValue DamageModifier;
            public sbyte Unknown0;
            public sbyte Unknown1;

            public enum AiVisionValue : sbyte
            {
                Unchanged,
                Normal,
                Blind,
                Nearsighted,
                EagleEye
            }

            public enum AiHearingValue : sbyte
            {
                Unchanged,
                Normal,
                Deaf,
                Sharp
            }

            public enum AiShootinessValue : sbyte
            {
                Unchanged,
                Normal,
                Marksman,
                TriggerHappy
            }

            public enum AiLuckValue : sbyte
            {
                Unchanged,
                Normal,
                Unlucky,
                Lucky,
                Leprechaun
            }

            public enum AiGrenadesValue : sbyte
            {
                Unchanged,
                Normal,
                None,
                Catch
            }

            public enum AiDamageResistanceValue : sbyte
            {
                Unchanged,
                _10,
                _50,
                _90,
                _100,
                _110,
                _150,
                _200,
                _300,
                _500,
                _1000,
                _2000,
                Invulnerable
            }

            public enum AiDamageModifierValue : sbyte
            {
                Unchanged,
                _0,
                _25,
                _50,
                _75,
                _90,
                _100,
                _110,
                _150,
                _200,
                _300,
                InstantKill
            }
        }

        [TagStructure(Size = 0xA4)]
        public class TeamOptions : TagStructure
        {
            public TeamOptionsModelOverrideType ModelOverrideType;
            public TeamOptionsDesignatorSwitchType DesignatorSwitchType;
            [TagField(Length = 2, Flags = TagTool.Tags.TagFieldFlags.Padding)]
            public byte[] Pad;
            [TagField(Length = 8)]
            public GameEngineTeamOptionsTeamBlock[] Teams;

            public enum TeamOptionsModelOverrideType : sbyte
            {
                NonePlayerChoice,
                ForceSpartan,
                ForceElite,
                SetByTeam,
                SetByDesignator,
            }

            public enum TeamOptionsDesignatorSwitchType : sbyte
            {
                None,
                Random,
                Rotate,
            }

            [TagStructure(Size = 0x14)]
            public class GameEngineTeamOptionsTeamBlock : TagStructure
            {
                public GameEngineTeamOptionsTeamFlags Flags;
                public MultiplayerTeamDesignator InitialTeamDesignator;
                public PlayerModelChoice ModelOverride;
                public sbyte NumberOfFireteams;
                public StringId Description;
                public ArgbColor ColorOverride;
                public ArgbColor UiTextTintColorOverride;
                public ArgbColor UiBitmapTintColorOverride;

                [Flags]
                public enum GameEngineTeamOptionsTeamFlags : byte
                {
                    Enabled = 1 << 0,
                    OverrideColor = 1 << 1,
                    OverrideUiTextTintColor = 1 << 2,
                    OverrideUiBitmapTintColor = 1 << 3,
                }

                public enum MultiplayerTeamDesignator : sbyte
                {
                    Defender,
                    Attacker,
                    ThirdParty,
                    FourthParty,
                    FifthParty,
                    SixthParty,
                    SeventhParty,
                    EighthParty,
                    Neutral,
                }

                public enum PlayerModelChoice : sbyte
                {
                    Spartan,
                    Elite,
                }
            }
        }

        [TagStructure(Size = 0x10)]
        public class LoadoutOptions : TagStructure
        {
            public LoadoutFlagsDefinition Flags;
            [TagField(Length = 3, Flags = TagTool.Tags.TagFieldFlags.Padding)]
            public byte[] Dlvkjser;
            public List<LoadoutPaletteEntry> LoadoutPalettes;

            [Flags]
            public enum LoadoutFlagsDefinition : byte
            {
                SpartanLoadoutsEnabled = 1 << 0,
                EliteLoadoutsEnabled = 1 << 1,
            }

            [TagStructure(Size = 0x4)]
            public class LoadoutPaletteEntry : TagStructure
            {
                public StringId PaletteName;
            }
        }

        [TagStructure(Size = 0x68, MinVersion = CacheVersion.HaloReach)]
        public class SurvivalVariant : BaseVariant
        {
            public SurvivalVariantFlags Flags;

            [TagField(EnumType = typeof(sbyte))]
            public GameDifficulty GameDifficulty;

            [TagField(Length = 2, Flags = TagTool.Tags.TagFieldFlags.Padding)]
            public byte[] Sdfhjren;

            public sbyte SetCount;
            public sbyte BonusLivesAwarded;
            public short BonusTarget;
            public short SpartanLivesOnEliteDeath;
            public short ExtraLifeScoreTarget;
            public short SharedTeamLifeCount;
            public short EliteLifeCount;
            public short MaximumLives;
            public short GeneratorCount;
            public StringId SpartanPlayerTraits;
            public StringId ElitePlayerTraits;
            public StringId AiTraits;
            public List<RespawnSetting> EliteRespawnOptions;
            public List<SurvivalSetProperties> SetProperties;
            public List<SurvivalRoundProperties> RoundProperties;
            public SurvivalBonusWavePropertiesStruct BonusRoundProperties;
            public List<SurvivalCustomSkull> CustomSkulls;

            [TagStructure(Size = 0x4)]
            public class SurvivalSetProperties : TagStructure
            {
                public SkullFlags Skulls;
            }

            [TagStructure(Size = 0x34)]
            public class SurvivalRoundProperties : TagStructure
            {
                public SkullFlags Skulls;
                public SurvivalWavePropertiesStruct InitialWaves;
                public SurvivalWavePropertiesStruct PrimaryWaves;
                public SurvivalWavePropertiesStruct BossWaves;
            }

            [TagStructure(Size = 0x10)]
            public class SurvivalWavePropertiesStruct : TagStructure
            {
                public SurvivalWavePropertiesFlags Flags;
                public SurvivalWaveSquadAdvanceType WaveSelectionType;
                [TagField(Length = 2, Flags = TagTool.Tags.TagFieldFlags.Padding)]
                public byte[] Vjknmfen;
                public List<SurvivalWaveSquadBlock> WaveSquads;

                [Flags]
                public enum SurvivalWavePropertiesFlags : byte
                {
                    DeliveredViaDropship = 1 << 0,
                }

                public enum SurvivalWaveSquadAdvanceType : sbyte
                {
                    Random,
                    Sequence,
                }

                [TagStructure(Size = 0x4)]
                public class SurvivalWaveSquadBlock : TagStructure
                {
                    public StringId SquadType;
                }
            }

            [TagStructure(Size = 0x18)]
            public class SurvivalBonusWavePropertiesStruct : TagStructure
            {
                public SkullFlags Skulls;
                public short Duration;
                [TagField(Length = 2, Flags = TagTool.Tags.TagFieldFlags.Padding)]
                public byte[] Clkjsdf;
                public SurvivalWavePropertiesStruct BaseProperties;
            }

            [TagStructure(Size = 0xC)]
            public class SurvivalCustomSkull : TagStructure
            {
                public StringId SpartanPlayerTraits;
                public StringId ElitePlayerTraits;
                public StringId AiTraits;
            }

            [Flags]
            public enum SurvivalVariantFlags : byte
            {
                EnableScenarioHazards = 1 << 0,
                GeneratorDefendAll = 1 << 1,
                GeneratorRandomSpawn = 1 << 2,
                EnableWeaponDrops = 1 << 3,
                EnableAmmoCrates = 1 << 4,
            }

            [Flags]
            public enum SkullFlags : uint
            {
                SkullIron = 1 << 0,
                SkullBlackEye = 1 << 1,
                SkullToughLuck = 1 << 2,
                SkullCatch = 1 << 3,
                SkullFog = 1 << 4,
                SkullFamine = 1 << 5,
                SkullThunderstorm = 1 << 6,
                SkullTilt = 1 << 7,
                SkullMythic = 1 << 8,
                SkullAssassin = 1 << 9,
                SkullBlind = 1 << 10,
                SkullSuperman = 1 << 11,
                SkullBirthdayParty = 1 << 12,
                SkullDaddy = 1 << 13,
                SkullRed = 1 << 14,
                SkullYellow = 1 << 15,
                SkullBlue = 1 << 16,
            }
        }

        [TagStructure(Size = 0x0, MinVersion = CacheVersion.HaloReach)]
        public class CampaignVariant : BaseVariant
        {
        }
    }
}