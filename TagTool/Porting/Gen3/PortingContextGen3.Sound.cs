using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TagTool.Audio;
using TagTool.Cache;
using TagTool.Commands;
using TagTool.Cache.Gen3;
using TagTool.Commands.Common;
using TagTool.Commands.Porting;
using TagTool.Commands.Sounds;
using TagTool.Common;
using TagTool.Tags.Definitions;
using TagTool.Common.Logging;
using TagTool.Audio.Utils;

namespace TagTool.Porting.Gen3
{
    partial class PortingContextGen3
    {
        private SoundCacheFileGestalt BlamSoundGestalt { get; set; } = null;

        class SoundConversionResult
        {
            public Sound Definition;
            public byte[] Data;
            // a list of functions that will be run after conversion
            // used for operations like string id conversion which cannot be done concurrently
            public List<Action> PostConversionOperations = new List<Action>();
        }

        private Sound FinishConvertSound(SoundConversionResult result)
        {
            var sound = result.Definition;
            var resourceDefinition = AudioUtils.CreateSoundResourceDefinition(result.Data);
            sound.Resource = CacheContext.ResourceCache.CreateSoundResource(resourceDefinition);
            // do post conversion operations
            result.PostConversionOperations.ForEach(op => op());
            return sound;
        }

        private Sound ConvertSound(Stream cacheStream, Stream blamCacheStream, Sound sound, CachedTag destTag, string blamTag_Name)
        {
            if (BlamSoundGestalt == null)
                BlamSoundGestalt = PortingContextFactory.LoadSoundGestalt(BlamCache, blamCacheStream);

            BlamCache.LoadSoundBanks();

            var task = RunOnThreadPool(() => ConvertSoundInternal(sound, blamTag_Name))
                .ContinueWith(task => FinishConvertSound(task.GetAwaiter().GetResult()), MainThreadScheduler);

            AddTask(task);

            return sound;
        }

        private SoundConversionResult ConvertSoundInternal(Sound sound, string blamTagName)
        {
            //
            // WARNING: not on the main thread. Don't touch external state unless you know what you're doing
            //

            var result = new SoundConversionResult();
            result.Definition = sound;

            Compression targetFormat = Options.AudioCodec;

            int currentSoundDataOffset = 0;
            List<BlamSound> convertedAudioList = [];
            List<PitchRange> newPitchRanges = [];
            uint totalSampleCount = 0;

            for (int pitchRangeIndex = 0; pitchRangeIndex < sound.SoundReference.PitchRangeCount; pitchRangeIndex++)
            {
                PitchRange blamPitchRange = BlamSoundGestalt.PitchRanges[sound.SoundReference.PitchRangeIndex + pitchRangeIndex];

                int permutationCount = BlamSoundGestalt.GetPermutationCount(blamPitchRange, BlamCache.Platform);

                // Create the new pitch range
                var pitchRange = new PitchRange();
                result.PostConversionOperations.Add(() => pitchRange.ImportName = ConvertStringId(BlamSoundGestalt.ImportNames[blamPitchRange.ImportNameIndex].Name));
                pitchRange.PitchRangeParameters = BlamSoundGestalt.PitchRangeParameters[blamPitchRange.PitchRangeParametersIndex];
                pitchRange.XsyncFlags = -1;
                pitchRange.RuntimeUsablePermutationCount = (sbyte)permutationCount;
                pitchRange.RuntimeLastPermutationIndex = -1;
                pitchRange.RuntimeDiscardedPermutationIndex = -1;
                pitchRange.Permutations = new List<Permutation>(permutationCount);
                newPitchRanges.Add(pitchRange);

                for (int permutationIndex = 0; permutationIndex < permutationCount; permutationIndex++)
                {
                    Permutation blamPermutation = BlamSoundGestalt.GetPermutation(blamPitchRange, permutationIndex, BlamCache.Platform);

                    // Convert the audio
                    BlamSound convertedAudio = ConvertAudio(sound, blamTagName, targetFormat, pitchRangeIndex, permutationIndex, blamPermutation);
                    convertedAudioList.Add(convertedAudio);

                    // Create the new permutation
                    var permutation = new Permutation();
                    result.PostConversionOperations.Add(() => permutation.ImportName = ConvertStringId(BlamSoundGestalt.ImportNames[blamPermutation.ImportNameIndex].Name));
                    permutation.SampleCount = convertedAudio.SampleCount;
                    permutation.SkipFraction = (blamPermutation.EncodedSkipFraction / 32767.0f) * 0.5f;
                    permutation.Gain = (float)blamPermutation.EncodedGain;
                    permutation.RawInfoIndex = blamPermutation.RawInfoIndex;

                    // Create the chunk (MS23 uses only a single chunk)
                    var firstBlamChunk = BlamSoundGestalt.GetPermutationChunk(blamPermutation, 0);
                    var lastBlamChunk = BlamSoundGestalt.GetPermutationChunk(blamPermutation, blamPermutation.PermutationChunkCount - 1);
                    var newChunk = new PermutationChunk(currentSoundDataOffset, convertedAudio.Data.Length, firstBlamChunk.FirstSample, lastBlamChunk.LastSample);
                    permutation.FirstSample = firstBlamChunk.FirstSample;
                    permutation.PermutationChunks = [newChunk];
                    pitchRange.Permutations.Add(permutation);

                    // Accumulate
                    currentSoundDataOffset += convertedAudio.Data.Length;
                    totalSampleCount += convertedAudio.SampleCount;
                }
            }

            result.Data = BuildSoundResourceData(convertedAudioList);

            ConvertSoundDefinition(sound, targetFormat, newPitchRanges, totalSampleCount);

            return result;
        }

        private void ConvertSoundDefinition(Sound sound, Compression targetFormat, List<PitchRange> newPitchRanges, uint totalSampleCount)
        {
            PlatformCodec platformCodec = BlamSoundGestalt.PlatformCodecs[sound.SoundReference.PlatformCodecIndex];
            PlaybackParameter playback = BlamSoundGestalt.PlaybackParameters[sound.SoundReference.PlaybackParameterIndex];
            Scale scale = BlamSoundGestalt.Scales[sound.SoundReference.ScaleIndex];
            Promotion promotion = sound.SoundReference.PromotionIndex != -1 ? BlamSoundGestalt.Promotions[sound.SoundReference.PromotionIndex] : null;

            sound.Flags = ConvertSoundFlags(sound.Flags);
            sound.SampleRate = platformCodec.SampleRate;
            sound.Playback = ConvertPlayback(playback);
            sound.Scale = scale;
            sound.PlatformCodec = new PlatformCodec()
            {
                Compression = targetFormat,
                Encoding = platformCodec.Encoding,
                SampleRate = platformCodec.SampleRate,
                LoadMode = 0
            };

            if (BlamCache.Version < CacheVersion.HaloReach)
            {
                if (sound.SoundReference.CustomPlaybackIndex != -1)
                    sound.CustomPlaybacks = [BlamSoundGestalt.CustomPlaybacks[sound.SoundReference.CustomPlaybackIndex]];
            }

            sound.Promotion = new Promotion()
            {
                Rules = promotion?.Rules,
                RuntimeTimers = promotion?.RuntimeTimers,
                RuntimeActivePromotionIndex = promotion?.RuntimeActivePromotionIndex ?? -1,
                RuntimeLastPromotionTime = promotion?.RuntimeLastPromotionTime ?? 0,
                RuntimeSuppressionTimeout = promotion?.RuntimeSuppressionTimeout ?? 0,
            };
            sound.MaximumPlayTime = sound.SoundReference.MaximumPlayTime;
            sound.TotalSampleCount = totalSampleCount;
            sound.PitchRanges = newPitchRanges;

            ConvertExtraInfo(sound);
            ConvertLanguages(sound);
        }

        private BlamSound ConvertAudio(Sound sound, string blamTag_Name, Compression targetFormat, int pitchRangeIndex, int permutationIndex, Permutation blamPermutation)
        {
            bool useCache = Options.AudioCache != null;
            string soundCachePath = Options.AudioCache;

            BlamSound audioData = null;

            // If using an audio cache try to load it from there4
            if (useCache)
            {
                int sampleRate = sound.PlatformCodec.SampleRate.GetSampleRateHz();
                int channelCount = Encoding.GetChannelCount(sound.PlatformCodec.Encoding);
                audioData = AudioCache.Load(soundCachePath, BlamCache.Version, blamTag_Name, pitchRangeIndex, permutationIndex, targetFormat, sampleRate, blamPermutation.SampleCount, channelCount);
            }

            // If no cached audio, extract and convert
            if (audioData == null)
            {
                audioData = SoundExtractorGen3.ExtractSound((GameCacheGen3)BlamCache, BlamSoundGestalt, sound, pitchRangeIndex, permutationIndex);
                audioData = AudioConverter.Convert(audioData, targetFormat);

                // store the converted audio in the cache
                if (useCache)
                    AudioCache.Store(soundCachePath, BlamCache.Version, blamTag_Name, pitchRangeIndex, permutationIndex, audioData);
            }

            // FMOD requires that we add 16 bytes of padding on either side
            if (targetFormat == Compression.PCM)
                audioData.Data = AudioUtils.Pad(audioData.Data, 16);

            return audioData;
        }

        private static byte[] BuildSoundResourceData(List<BlamSound> converted)
        {
            byte[] soundData = new byte[converted.Sum(s => s.Data.Length)];
            int dataOffset = 0;
            foreach (BlamSound sound in converted)
            {
                Array.Copy(sound.Data, 0, soundData, dataOffset, sound.Data.Length);
                dataOffset += sound.Data.Length;
            }
            return soundData;
        }

        private PlaybackParameter ConvertPlayback(PlaybackParameter playback)
        {
            if (BlamCache.Version < CacheVersion.HaloReach)
                return playback;

            // Fix playback parameters for reach

            var newPlayback = new PlaybackParameter()
            {
                MaximumDistance = playback.MaximumDistance,
                MininumDistance = playback.MininumDistance,
                DistanceParameters = playback.DistanceParameters,
                SkipFraction = playback.SkipFraction,
                MaximumBendPerSecond = playback.MaximumBendPerSecond,
                GainBase = playback.GainBase,
                GainVariance = playback.GainVariance,
                RandomPitchBounds = playback.RandomPitchBounds,
                InnerConeAngle = playback.InnerConeAngle,
                OuterConeAngle = playback.OuterConeAngle,
                OuterConeGain = playback.OuterConeGain,
                GainOverrideFlags = playback.GainOverrideFlags,
                Azimuth = playback.Azimuth,
                PositionalGain = playback.PositionalGain,
                FirstPersonGain = playback.FirstPersonGain,
                FieldDisableFlags = 0
            };

            if (playback.DistanceParameters.DontPlayDistance == 0)
                newPlayback.FieldDisableFlags |= PlaybackParameter.FieldDisableFlagsValue.DistanceA;

            if (playback.DistanceParameters.AttackDistance == 0)
                newPlayback.FieldDisableFlags |= PlaybackParameter.FieldDisableFlagsValue.DistanceB;

            if (playback.DistanceParameters.MinimumDistance == 0)
                newPlayback.FieldDisableFlags |= PlaybackParameter.FieldDisableFlagsValue.DistanceC;

            if (playback.DistanceParameters.MaximumDistance == 0)
                newPlayback.FieldDisableFlags |= PlaybackParameter.FieldDisableFlagsValue.DistanceD;

            newPlayback.FieldDisableFlags |= PlaybackParameter.FieldDisableFlagsValue.DirectionalAttenuation;

            return newPlayback;
        }

        private void ConvertLanguages(Sound sound)
        {
            if (sound.SoundReference.LanguageIndex == -1)
                return;
            
            if (BlamCache.Version < CacheVersion.HaloReach)
            {
                sound.Languages = new List<LanguageBlock>();

                foreach (var language in BlamSoundGestalt.LanguageDurations)
                {
                    sound.Languages.Add(new LanguageBlock
                    {
                        Language = language.Language,
                        PermutationDurations = [],
                        PitchRangeDurations = []
                    });

                    //Add all the  Pitch Range Duration block (pitch range count dependent)

                    var curLanguage = sound.Languages.Last();

                    for (int i = 0; i < sound.SoundReference.PitchRangeCount; i++)
                    {
                        curLanguage.PitchRangeDurations.Add(language.PitchRangeDurations[sound.SoundReference.LanguageIndex + i]);
                    }

                    //Add all the Permutation Duration Block and adjust offsets

                    for (int i = 0; i < curLanguage.PitchRangeDurations.Count; i++)
                    {
                        var curPRD = curLanguage.PitchRangeDurations[i];

                        //Get current block count for new index
                        short newPermutationIndex = (short)curLanguage.PermutationDurations.Count();

                        for (int j = curPRD.PermutationStartIndex; j < curPRD.PermutationStartIndex + curPRD.PermutationCount; j++)
                        {
                            curLanguage.PermutationDurations.Add(language.PermutationDurations[j]);
                        }

                        //apply new index
                        curPRD.PermutationStartIndex = newPermutationIndex;
                    }

                }
            }
            else
            {
                // TODO: reverse reach's facial animation resource
            }
        }
        
        private void ConvertExtraInfo(Sound sound)
        {
            var extraInfo = new ExtraInfo();

            // We can avoid converting LanguagePermutations as it is not needed for runtime

            if (sound.SoundReference.ExtraInfoIndex != -1 && BlamCache.Version < CacheVersion.HaloReach)
            {
                // Used for facial animations
                extraInfo.EncodedPermutationSections = BlamSoundGestalt.ExtraInfo[sound.SoundReference.ExtraInfoIndex].EncodedPermutationSections;
            }
            else
            {
                // TODO reach facial animations
            }

            sound.ExtraInfo = [extraInfo];
        }

        private SoundLooping ConvertSoundLooping(SoundLooping soundLooping)
        {
            soundLooping.Unused = null;
            soundLooping.SoundClass = ConvertSoundClass(soundLooping.SoundClass);

			if (BlamCache.Version == CacheVersion.Halo3Retail)
			{
				foreach (var track in soundLooping.Tracks)
				{
					// FadeMode was added in ODST, H3 uses InversePower for in sounds, and Power for out sounds
					track.FadeInMode = SoundLooping.Track.SoundFadeMode.None;
					track.FadeOutMode = SoundLooping.Track.SoundFadeMode.None;
					track.AlternateCrossfadeMode = SoundLooping.Track.SoundFadeMode.None;
					track.AlternateFadeOutMode = SoundLooping.Track.SoundFadeMode.None;
				}
			}
			else if (BlamCache.Version >= CacheVersion.HaloReach)
			{
				foreach (var track in soundLooping.Tracks)
				{
					track.Flags = GetEquivalentFlags(track.Flags, track.FlagsReach);
					track.OutputEffect = track.OutputEffectReach;
					track.FadeInDuration = track.FadeInDurationReach;
					track.FadeInMode = GetEquivalentValue(track.FadeInMode, track.FadeInModeReach);
					track.FadeOutDuration = track.FadeOutDurationReach;
					track.FadeOutMode = GetEquivalentValue(track.FadeOutMode, track.FadeOutModeReach);
					track.AlternateCrossfadeMode = GetEquivalentValue(track.AlternateCrossfadeMode, track.AlternateCrossfadeModeReach);
					track.AlternateFadeOutMode = GetEquivalentValue(track.AlternateFadeOutMode, track.AlternateFadeOutModeReach);
				}
			}

            return soundLooping;
        }

        private Dialogue ConvertDialogue(Stream cacheStream, Dialogue dialogue)
        {

            CachedTag edAdlg = null;
            AiDialogueGlobals adlg; ;
            foreach (var tag in CacheContext.TagCache.FindAllInGroup("adlg"))
            {
                edAdlg = tag;
                break;
            }

            adlg = CacheContext.Deserialize<AiDialogueGlobals>(cacheStream, edAdlg);

            //Create empty udlg vocalization block and fill it with empty blocks matching adlg

            List<Dialogue.SoundReference> newSoundReference = new List<Dialogue.SoundReference>();
            foreach (var soundreference in adlg.Vocalizations)
            {
                Dialogue.SoundReference block = new Dialogue.SoundReference
                {
                    Sound = null,
                    Flags = 0,
                    Vocalization = soundreference.Vocalization,
                };
                newSoundReference.Add(block);
            }

            //Match the tags with the proper stringId

            if(BlamCache.Version < CacheVersion.HaloReach)
            {
                for (int i = 0; i < 304; i++)
                {
                    var soundreference = newSoundReference[i];
                    for (int j = 0; j < dialogue.Vocalizations.Count; j++)
                    {
                        var soundreferenceH3 = dialogue.Vocalizations[j];
                        if (CacheContext.StringTable.GetString(soundreference.Vocalization).Equals(CacheContext.StringTable.GetString(soundreferenceH3.Vocalization)))
                        {
                            soundreference.Flags = soundreferenceH3.Flags;
                            soundreference.Sound = soundreferenceH3.Sound;
                            break;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < 304; i++)
                {
                    var vocalization = newSoundReference[i];
                    for (int j = 0; j < dialogue.Vocalizations.Count; j++)
                    {
                        var vocalizationReach = dialogue.Vocalizations[j];
                        if (CacheContext.StringTable.GetString(vocalization.Vocalization).Equals(CacheContext.StringTable.GetString(vocalizationReach.Vocalization)))
                        {
                            // we use index 0 because other indices are for different situation like stealth. 
                            if(vocalizationReach.Stimuli.Count > 0)
                                vocalization.Sound = vocalizationReach.Stimuli[0].Sound;

                            break;
                        }
                    }
                }
            }

            

            dialogue.Vocalizations = newSoundReference;

            return dialogue;
        }

        private SoundMix ConvertSoundMix(SoundMix soundMix)
        {
            if (BlamCache.Version == CacheVersion.Halo3Retail)
                soundMix.GlobalMix.QuadRouteToLfeGain = 0;

            return soundMix;
        }

        // HO uses ODST classes, with H3 structure
        private SoundClasses ConvertSoundClasses(SoundClasses sncl, CacheVersion version)
        {
            // setup class with "default" values
            var sClass = new SoundClasses.Class()
            {
                MaxSoundsPerTag = 4,
                MaxSoundsPerObject = 1,
                PreemptionTime = 100,

                InternalFlags = (SoundClasses.Class.InternalFlagBits.Valid | SoundClasses.Class.InternalFlagBits.ValidXmaCompressionLevel |
                 SoundClasses.Class.InternalFlagBits.ValidDopplerFactor | SoundClasses.Class.InternalFlagBits.ValidObstructionFactor |
                 SoundClasses.Class.InternalFlagBits.ValidUnderwaterPropagation | SoundClasses.Class.InternalFlagBits.ValidSuppressSpatialization),

                Priority = 5,
                DistanceBounds = new Bounds<float>(8, 120),
                TransmissionMultiplier = 1.0f
            };

            // hud class, unique to HO. the above values seem to be okay
            if (sncl.Classes.Count >= 50)
                sncl.Classes.Insert(50, sClass);

            if (version <= CacheVersion.Halo3Retail)
            {
                foreach (var c in sncl.Classes)
                    c.CacheMissModeODST = (SoundClasses.Class.CacheMissModeODSTValue)c.CacheMissMode;

                // add classes missing from H3
                for (int i = sncl.Classes.Count; i < 65; i++)
                    sncl.Classes.Add(sClass);
            }

            // ms23 requires this flag to play a class on the mainmenu
            sncl.Classes[32].ClassFlags |= SoundClasses.Class.ExternalFlagBits.ClassPlaysOnMainmenu; // Music
            sncl.Classes[52].ClassFlags |= SoundClasses.Class.ExternalFlagBits.ClassPlaysOnMainmenu; // UI

            return sncl;
        }
        private BitFlags<SoundFlags> ConvertSoundFlags(BitFlags<SoundFlags> flags)
        {
            return flags.ConvertBitwise(CacheContext);
        }

        public SoundClass ConvertSoundClass(SoundClass soundClass){
            return soundClass.Convert();
        }

        private bool CheckSoundBank(Sound sound)
        {
            BlamCache.LoadSoundBanks();

            PitchRange pitchRange = BlamSoundGestalt.PitchRanges[sound.SoundReference.PitchRangeIndex];
            int permutationCount = BlamSoundGestalt.GetPermutationCount(pitchRange, BlamCache.Platform);
            for (int permutationIndex = 0; permutationIndex < permutationCount; permutationIndex++)
            {
                Permutation permutation = BlamSoundGestalt.GetPermutation(pitchRange, permutationIndex, BlamCache.Platform);
                if (BlamCache.SoundBanks.FindSound(permutation.FsbSoundHash, out _) == -1)
                    return false;
            }

            return true;
        }
    }
}