using TagTool.Cache;
using TagTool.Tags;

namespace TagTool.Audio
{
    [TagEnum(IsVersioned = true)]
    public enum SoundFlags
    {
        FitToAdpcmBlocksize,
        [TagEnumMember(MaxVersion = CacheVersion.Halo2PC)]
        SplitLongSoundIntoPermutations,
        AlwaysSpatialize,
        NeverObstruct,
        InternalDontTouch,
        [TagEnumMember(MinVersion = CacheVersion.HaloReach)]
        FacialAnimationDataStripped,
        UseHugeSoundTransmission,
        LinkCountToOwnerUnit,
        PitchRangeIsLanguage,
        DontUseSoundClassSpeakerFlag,
        DontUseLipsyncData,
        [TagEnumMember(MaxVersion = CacheVersion.Halo2PC)]
        PlayOnlyInLegacyMode,
        [TagEnumMember(MaxVersion = CacheVersion.Halo2PC)]
        PlayOnlyInRemasteredMode,
        [TagEnumMember(MinVersion = CacheVersion.Halo3Beta)]
        InstantSoundPropagation,
        [TagEnumMember(MinVersion = CacheVersion.Halo3Beta)]
        FakeSpatializationWithDistance,
        [TagEnumMember(MinVersion = CacheVersion.HaloOnlineED)]
        PlayPermutationsInOrder,
        VehicleWeatherUnknown,
        Invalid,
        Unknown19,
        [TagEnumMember(Version = CacheVersion.Halo3ODST, Platform = CachePlatform.MCC)]
        UnitDialogueODSTMCC,
        Unknown21,
        Unknown22,
        Unknown23,
        Unknown24,
        Unknown25
    }
}
