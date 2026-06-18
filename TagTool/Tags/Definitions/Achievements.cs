using TagTool.Cache;
using TagTool.Common;
using System;
using System.Collections.Generic;
using static TagTool.Tags.TagFieldFlags;

namespace TagTool.Tags.Definitions
{
    [TagStructure(Name = "achievements", Tag = "achi", Size = 0xC)]
    public class Achievements : TagStructure
    {
        public List<AchievementInformationBlock> AchievementInformation;

        [TagStructure(Size = 0x18, Platform = CachePlatform.Original, MaxVersion = CacheVersion.HaloOnline700123)]
        [TagStructure(Size = 0x10, Platform = CachePlatform.MCC, MaxVersion = CacheVersion.Halo3ODST)]
        [TagStructure(Size = 0x14, MinVersion = CacheVersion.HaloReach)]
        public class AchievementInformationBlock : TagStructure
        {
            [TagField(MaxVersion = CacheVersion.HaloOnline700123)]
            public Achievements Achievement;
            [TagField(MaxVersion = CacheVersion.HaloOnline700123)]
            public LevelFlags Flags;

            [TagField(Flags = Label, MaxVersion = CacheVersion.HaloOnline700123)]
            public StringId LevelName;

            [TagField(Flags = Label, MaxVersion = CacheVersion.HaloOnline700123)]
            public int Goal;

            [TagField(Platform = CachePlatform.Original, MaxVersion = CacheVersion.HaloOnline700123)]
            [TagField(Platform = CachePlatform.MCC, MaxVersion = CacheVersion.Halo3Retail)]
            public ChudIconFlags IconFlags;

            [TagField(Platform = CachePlatform.Original, MaxVersion = CacheVersion.HaloOnline700123)]
            [TagField(Platform = CachePlatform.MCC, MaxVersion = CacheVersion.Halo3Retail)]
            public int IconIndex;

            [TagField(MinVersion = CacheVersion.HaloReach)]
            public StringId Name;
            [TagField(MinVersion = CacheVersion.HaloReach)]
            public byte Type;
            [TagField(MinVersion = CacheVersion.HaloReach, EnumType = typeof(byte))]
            public GameDifficultyFlags Difficulty;
            [TagField(MinVersion = CacheVersion.HaloReach, Flags = Padding, Length = 2)]
            public byte[] ReachPadding;
            [TagField(MinVersion = CacheVersion.HaloReach)]
            public List<RestrictedLevel> RestrictedLevels;

            public enum Achievements : int
            {
                beat_sc100,
                beat_sc110,
                beat_sc120,
                beat_sc130,
                beat_sc140,
                beat_sc150,
                beat_l200,
                beat_l300,
                beat_campaign_normal,
                beat_campaign_heroic,
                beat_campaign_legendary,
                wraith_killer,
                naughty_naughty,
                good_samaritan,
                dome_inspector,
                laser_blaster,
                both_tubes,
                i_like_fire,
                my_clothes,
                pink_and_deadly,
                dark_times,
                trading_down,
                headcase,
                boom_headshot,
                ewww_sticky,
                junior_detective,
                gumshoe,
                super_sleuth,
                metagame_points_in_sc100,
                metagame_points_in_sc110,
                metagame_points_in_sc120,
                metagame_points_in_sc130a,
                metagame_points_in_sc130b,
                metagame_points_in_sc140,
                metagame_points_in_l200,
                metagame_points_in_l300,
                be_like_marty,
                find_all_audio_logs,
                find_01_audio_logs,
                find_03_audio_logs,
                find_15_audio_logs,
                vidmaster_challenge_deja_vu,
                vidmaster_challenge_endure,
                vidmaster_challenge_classic,
                heal_up,
                stunning,
                tourist
            }

            [Flags]
            public enum LevelFlags : int
            {
                None = 0,
                InvalidInCampaign = 1 << 0,
                InvalidInSurvival = 1 << 1,
                ResetsOnMapReload = 1 << 2,
                UsesGameProgression = 1 << 3,
            }

            [Flags]
            public enum ChudIconFlags : int
            {
                None = 0,
                DisplaysOnHud = 1 << 0,
                Bit1 = 1 << 1 // only used for "super_sleuth"
            }

            [TagStructure(Size = 0x4)]
            public class RestrictedLevel : TagStructure
            {
                public StringId LevelName;
            }
        }
    }
}
