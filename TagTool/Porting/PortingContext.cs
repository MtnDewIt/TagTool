using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TagTool.Bitmaps;
using TagTool.BlamFile;
using TagTool.Cache;
using TagTool.Cache.HaloOnline;
using TagTool.Common;
using TagTool.Common.Logging;
using TagTool.Porting.Gen2;
using TagTool.Porting.Gen3;
using TagTool.Porting.HaloOnline;
using TagTool.Tags;
using TagTool.Tags.Definitions;

namespace TagTool.Porting
{
    public abstract partial class PortingContext : IDisposable
    {
        protected record struct CacheKey(int Index, string Name);

        public readonly GameCacheHaloOnlineBase CacheContext;
        public readonly GameCache BlamCache;
        protected readonly TagDefinitionCache TagDefinitionCache = new();
        private readonly HashSet<int> ReplacedTags = [];
        private readonly Dictionary<CacheKey, CachedTag> PortedTags = [];
        private readonly Dictionary<uint, StringId> PortedStringIds = [];
        private bool ShouldGenerateCampaignFile = false;

        public PortingFlags Flags = PortingFlags.Default;
        public List<Tag> DoNotReplaceGroups = [];

        /// <summary>
        /// Set of blam tag indices to ignore when porting
        /// </summary>
        public HashSet<int> IgnoreBlamTags = [];

        public PortingOptions Options = new();

        protected PortingContext(GameCacheHaloOnlineBase cacheContext, GameCache blamCache)
        {
            CacheContext = cacheContext;
            BlamCache = blamCache;
        }

        /// <summary>
        /// Create context for porting from sourceCache to destCache
        /// </summary>
        /// <param name="destCache">Destination cache</param>
        /// <param name="sourceCache">Source cache</param>
        /// <returns>PortingContext</returns>
        /// <exception cref="NotSupportedException">Thrown if the cache is not supported</exception>
        public static PortingContext Create(GameCacheHaloOnlineBase destCache, GameCache sourceCache)
        {
            switch (sourceCache)
            {
                case GameCacheHaloOnlineBase:
                    return new PortingContextHO(destCache, sourceCache);
                case GameCacheGen3:
                    return new PortingContextGen3(destCache, sourceCache);
                case GameCacheGen2:
                    return new PortingContextGen2(destCache, sourceCache);
                default:
                    throw new NotSupportedException($"Porting cache '{sourceCache.DisplayName}' not supported currently");
            }
        }

        /// <summary>
        /// Checks whether a tag can be ported and optionally returns an alternative tag
        /// </summary>
        /// <param name="blamTag">Tag to be ported</param>
        /// <param name="cacheStream">Destination cache stream</param>
        /// <param name="blamCacheStream">Source cache stream</param>
        /// <param name="blamDefinition">Source definition</param>
        /// <param name="resultTag">Alternative tag to use</param>
        /// <returns>True if the tag can be ported, otherwise alternativeTag will be used</returns>
        protected virtual bool TagIsValid(CachedTag blamTag, Stream cacheStream, Stream blamCacheStream, object blamDefinition, out CachedTag resultTag)
        {
            resultTag = null;
            return true;
        }

        public struct ConvertTagOptions
        {
            public ConvertTagOptions() { }

            /// <summary>
            /// If non null, overrides the ported tag name.
            /// </summary>
            public string TargetTagName = null;
        }

        /// <summary>
        /// Ports a tag from the source cache to the destination cache
        /// </summary>
        /// <param name="cacheStream">Destination cache stream</param>
        /// <param name="blamCacheStream">Source cache stream</param>
        /// <param name="blamTag">Tag to be converted</param>
        /// <param name="blamParentTagName">Parent tag name. Currently used to alter the template for particular render methods.</param>
        /// <param name="blamDefinition">Optional pre-deserialized tag definition. Useful for making changes to a tag prior to porting</param>
        /// <param name="options">Extra options</param>
        /// <returns>The resulting the tag from the destination cache</returns>
        public CachedTag ConvertTag(Stream cacheStream, Stream blamCacheStream, CachedTag blamTag, string blamParentTagName = null, object blamDefinition = null, in ConvertTagOptions options = default)
        {
            ProcessTasks();

            if (blamTag == null)
                return null;

            // Ignore tags that have been pre-converted
            if (blamTag is CachedTagHaloOnline hoTag && hoTag.TagCache == CacheContext.TagCache)
                return blamTag;

            var cacheKey = new CacheKey(blamTag.Index, options.TargetTagName ?? blamTag.Name);

            // Ignore tags that have already been ported
            if (PortedTags.TryGetValue(cacheKey, out CachedTag portedTag))
                return portedTag;

            // Ignore tags that we don't have a definition for
            if (!BlamCache.TagCache.TagDefinitions.TagDefinitionExists(blamTag.Group.Tag))
                return null;

            // Deserialize the definition if we need to
            blamDefinition ??= BlamCache.Deserialize(blamCacheStream, blamTag);

            if (TagIsValid(blamTag, cacheStream, blamCacheStream, blamDefinition, out CachedTag result))
            {
                result = ConvertTagInternal(cacheStream, blamCacheStream, blamTag, blamDefinition, blamParentTagName, cacheKey, options);

                if (result == null)
                    Log.Warning($"null tag allocated for reference \"{blamTag}\"");
            }
            else if (blamTag.Name != null && blamTag.IsInGroup("bitm"))
            {
                if (CacheContext.TagCache.TryGetTag($"{blamTag}", out result))
                    Log.Warning($"using existing bitmap \"{blamTag}\"");
            }

            PortedTags[cacheKey] = result;

            return result;
        }

        /// <summary>
        /// Allows subclasses to override the name and group of the ported tag
        /// </summary>
        /// <param name="blamTag">Source tag instance</param>
        /// <param name="blamDefinition">Source tag definition</param>
        /// <returns>A tuple containing the name and group of the ported tag</returns>
        protected virtual (string Name, Tag GroupTag) GetTargetTagNameAndGroup(CachedTag blamTag, object blamDefinition)
        {
            return (blamTag.Name, blamTag.Group.Tag);
        }

        protected virtual CachedTag ConvertTagInternal(Stream cacheStream, Stream blamCacheStream, CachedTag blamTag, object blamDefinition, string blamParentTagName, CacheKey cacheKey, in ConvertTagOptions options)
        {
            (string targetTagName, Tag targetGroupTag) = GetTargetTagNameAndGroup(blamTag, blamDefinition);
            if (options.TargetTagName != null)
                targetTagName = options.TargetTagName;

            if (!CacheContext.TagCache.TagDefinitions.TagDefinitionExists(targetGroupTag))
                return null;

            CachedTag edTag = null;

            //
            // Check to see if the ElDorado tag exists
            //

            if (!FlagIsSet(PortingFlags.New))
            {
                edTag = FindExistingTag(targetGroupTag, targetTagName);
                if (edTag != null)
                {
                    if (ShouldReplaceTag(cacheStream, edTag, blamDefinition))
                    {
                        if (!ReplacedTags.Add(edTag.Index)) // Avoid replacing the tag multiple times
                            return edTag;
                    }
                    else
                    {
                        PortedTags[cacheKey] = edTag; // Avoid re-entrancy

                        if (FlagIsSet(PortingFlags.Merge))
                            MergeTags(cacheStream, blamCacheStream, blamTag, edTag, blamDefinition);

                        return edTag;
                    }
                }
            }

            edTag ??= AllocateNewTag(targetGroupTag, targetTagName);
            PortedTags[cacheKey] = edTag; // Avoid re-entrancy

            //
            // Convert the Blam tag definition
            //

            Task convertTask;
            PushTaskList();
            try
            {
                blamDefinition = ConvertDefinition(cacheStream, blamCacheStream, blamTag, edTag, blamDefinition);
                if (blamDefinition == null)
                    throw new InvalidOperationException("Returning null from ConvertDefinition() is disallowed.");
            }
            finally
            {
                convertTask = Task.WhenAll(PopTaskList());
            }

            Task seriailizeTask = convertTask
                .ContinueWith(task => FinishConvertTag(cacheStream, blamDefinition, edTag), MainThreadScheduler);

            if (!seriailizeTask.IsCompleted)
            {
                AddUnattachedTask(seriailizeTask);
                _tagConvertTasks[edTag.Index] = seriailizeTask;
            }

            return edTag;
        }

        protected CachedTag FinishConvertTag(Stream cacheStream, object definition, CachedTag edTag)
        {
            ArgumentNullException.ThrowIfNull(definition);

            //
            // Finalize and serialize the new ElDorado tag definition
            //
            _tagConvertTasks.Remove(edTag.Index);
            TagDefinitionCache.Evict(edTag);
            CacheContext.Serialize(cacheStream, edTag, definition);

  
            if (FlagIsSet(PortingFlags.Print))
                Console.WriteLine($"['{edTag.Group.Tag}', 0x{edTag.Index:X4}] {edTag}");

            if (definition is Scenario scnr && FlagIsSet(PortingFlags.UpdateMapFiles))
            {
                string mapInfoDir = BlamCache.Directory == null ? "" : Path.Combine(BlamCache.Directory.FullName, "info");
                MapFileUpdater.UpdateMapFile(CacheContext, edTag, scnr, mapInfoDir);

                if (scnr.MapType == ScenarioMapType.SinglePlayer)
                    ShouldGenerateCampaignFile = true;
            }

            return edTag;
        }

        protected bool ShouldReplaceTag(Stream cacheStream, CachedTag existingTag, object blamDefinition)
        {
            if (blamDefinition is Bitmap blamBitmap)
            {
                // Do not replace if the source bitmap is invalid
                if (!BitmapUtils.IsBitmapResourceValid(BlamCache, blamBitmap))
                {
                    if (FlagIsSet(PortingFlags.Replace))
                        Log.Warning($"Skipping bitmap that has an invalid resource '{existingTag.Name}'");

                    return false;
                }

                // Always replace if the existing bitmap is invalid
                var edBitmap = TagDefinitionCache.Deserialize<Bitmap>(CacheContext, cacheStream, existingTag);
                if (!BitmapUtils.IsBitmapResourceValid(CacheContext, edBitmap))
                {
                    Log.Info($"Replacing bitmap that has an invalid resource '{existingTag}'");
                    return true;
                }
            }

            return FlagIsSet(PortingFlags.Replace)
                    && !PortingConstants.DoNotReplaceGroups.Contains(existingTag.Group.Tag)
                    && !DoNotReplaceGroups.Contains(existingTag.Group.Tag);
        }

        private CachedTag FindExistingTag(Tag group, string name)
        {
            // faster than TryGetTag
            foreach (CachedTag instance in CacheContext.TagCache.TagTable)
            {
                if (instance != null && instance.Group.Tag == group && instance.Name == name)
                    return instance;
            }
            return null;
        }

        protected CachedTag AllocateNewTag(Tag groupTag, string name)
        {
            CachedTag edTag = null;

            TagGroup group = CacheContext.TagCache.TagDefinitions.GetTagGroupFromTag(groupTag);

            if (FlagIsSet(PortingFlags.UseNull))
            {
                var i = CacheContext.TagCache.TagTable.ToList().FindIndex(n => n == null);

                if (i >= 0)
                    CacheContext.TagCacheGenHO.Tags[i] = (CachedTagHaloOnline)(edTag = (new CachedTagHaloOnline(CacheContext.TagCache, i, group)));
            }
            else
            {
                edTag = CacheContext.TagCache.AllocateTag(group);
            }

            edTag.Name = name;

            return edTag;
        }

        protected virtual CachedTag GetFallbackTag(CachedTag blamTag)
        {
            if(PortingConstants.DefaultTagNames.TryGetValue(blamTag.Group.Tag, out string defaultTagName))
            {
                CacheContext.TagCache.TryGetTag($"{defaultTagName}.{blamTag.Group.Tag}", out CachedTag result);
                return result;
            }
            return null;
        }

        /// <summary>
        /// Merges blamTag into edTag
        /// </summary>
        /// <param name="cacheStream">Destination cache stream</param>
        /// <param name="blamCacheStream">Source cache stream</param>
        /// <param name="blamTag">Tag to be merged</param>
        /// <param name="edTag">Tag to merge into</param>
        /// <param name="blamDefinition">Source definition</param>
        protected virtual void MergeTags(Stream cacheStream, Stream blamCacheStream, CachedTag blamTag, CachedTag edTag, object blamDefinition)
        {
        }

        /// <summary>
        /// Converts a tag definition
        /// </summary>
        /// <param name="cacheStream">Destination cache stream</param>
        /// <param name="blamCacheStream">Source cache stream</param>
        /// <param name="blamTag">Tag to be ported</param>
        /// <param name="edTag">Tag that is being ported to</param>
        /// <param name="blamDefinition">Deserialized definition data</param>
        /// <returns></returns>
        protected virtual object ConvertDefinition(Stream cacheStream, Stream blamCacheStream, CachedTag blamTag, CachedTag edTag, object blamDefinition)
        {
            return ConvertData(cacheStream, blamCacheStream, blamDefinition, blamDefinition, blamTag.Name);
        }

        /// <summary>
        /// Converts a <see cref="TagStructure"/> from the source cache to the destination cache
        /// </summary>
        /// <remarks>
        /// This should only be used if all you want to do is convert the fields and nothing else. Consider using <see cref="ConvertData"/> or <see cref="ConvertDefinition"/> If it'a tag definition
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheStream"></param>
        /// <param name="blamCacheStream"></param>
        /// <param name="data"></param>
        /// <param name="definition"></param>
        /// <param name="blamTagName"></param>
        /// <returns></returns>
        protected virtual T ConvertStructure<T>(Stream cacheStream, Stream blamCacheStream, T data, object definition, string blamTagName) where T : TagStructure
        {
            foreach (TagFieldInfo tagFieldInfo in TagStructure.GetTagFieldEnumerable(data.GetType(), CacheContext.Version, CacheContext.Platform))
            {
                if (CanSkipFieldConversion(tagFieldInfo))
                    continue;

                object value = tagFieldInfo.GetValue(data);
                if (value is null)
                    continue;

                value = ConvertFieldvalue(cacheStream, blamCacheStream, tagFieldInfo, value, definition, blamTagName);
                tagFieldInfo.SetValue(data, value);
            }

            return (T)data;
        }
        
        private bool CanSkipFieldConversion(TagFieldInfo tagFieldInfo)
        {
            if (!TagFieldInfo.FieldInCacheVersion(tagFieldInfo, BlamCache.Version, BlamCache.Platform))
                return true;

            if ((tagFieldInfo.Attribute.Flags & TagFieldFlags.GlobalMaterial) != 0)
                return false;

            Type type = tagFieldInfo.FieldType;

            return (type.IsValueType && type != typeof(StringId)) || type == typeof(string);
        }

        /// <summary>
        /// Converts arbitrary data
        /// </summary>
        /// <remarks>
        /// Avoid using this on tag definitions, use <see cref="ConvertDefinition"/> instead.
        /// </remarks>
        /// <param name="cacheStream">Destination cache stream</param>
        /// <param name="blamCacheStream">Ssource cache stream</param>
        /// <param name="data">Data to be converted</param>
        /// <param name="definition">Current tag definition</param>
        /// <param name="blamTagName">Current tag name</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public virtual object ConvertData(Stream cacheStream, Stream blamCacheStream, object data, object definition, string blamTagName)
        {
            switch (data)
            {
                case StringId stringId:
                    stringId = ConvertStringId(stringId);
                    return stringId;

                case null:  // no conversion necessary
                case ValueType _:   // no conversion necessary
                case string _:  // no conversion necessary
                    return data;

                case CachedTag tag:
                    {
                        if (IgnoreBlamTags.Contains(tag.Index))
                        {
                            //find equivalent in base cache otherwise use null
                            foreach (var instance in CacheContext.TagCache.FindAllInGroup(tag.Group.Tag))
                            {
                                if (instance == null || instance.Name == null)
                                    continue;

                                if (instance.Name == tag.Name)
                                    return instance;
                            }

                            return null;
                        }

                        if (!FlagIsSet(PortingFlags.Recursive))
                        {
                            foreach (var instance in CacheContext.TagCache.FindAllInGroup(tag.Group.Tag))
                            {
                                if (instance == null || instance.Name == null)
                                    continue;

                                if (instance.Name == tag.Name)
                                    return instance;
                            }

                            return null;
                        }

                        return ConvertTag(cacheStream, blamCacheStream, (CachedTag)data, blamTagName);
                    }

                case Array _:
                case IList _: // All arrays and List<T> implement IList, so we should just use that
                    data = ConvertCollection(cacheStream, blamCacheStream, data as IList, definition, blamTagName);
                    return data;

                case TagStructure tagStructure: // much faster to pattern match a type than to check for custom attributes.
                    tagStructure = ConvertStructure(cacheStream, blamCacheStream, tagStructure, definition, blamTagName);
                    return data;

                default:
                    Log.Warning($"Unhandled type in `ConvertData`: {data.GetType().Name} (probably harmless).");
                    break;
            }

            return data;
        }

        /// <summary>
        /// Converts a field portedTag
        /// </summary>
        /// <param name="cacheStream">Destination cache stream</param>
        /// <param name="blamCacheStream">Source cache stream</param>
        /// <param name="tagFieldInfo">TagFieldInfo of the field to be converted</param>
        /// <param name="value">Value of the field to be converted</param>
        /// <param name="definition">Current tag definition</param>
        /// <param name="blamTagName">Current tag name</param>
        /// <returns></returns>
        protected virtual object ConvertFieldvalue(Stream cacheStream, Stream blamCacheStream, TagFieldInfo tagFieldInfo, object value, object definition, string blamTagName)
        {
            return ConvertData(cacheStream, blamCacheStream, value, definition, blamTagName);
        }

        private IList ConvertCollection(Stream cacheStream, Stream blamCacheStream, IList data, object definition, string blamTagName)
        {
            // return early where possible
            if (data is null || data.Count == 0)
                return data;

            if (data[0] == null)
                return data;

            var type = data[0].GetType();
            if ((type.IsValueType && type != typeof(StringId)) || type == typeof(string))
                return data;

            // convert each element
            for (var i = 0; i < data.Count; i++)
            {
                var oldValue = data[i];
                var newValue = ConvertData(cacheStream, blamCacheStream, oldValue, definition, blamTagName);
                data[i] = newValue;
            }

            return data;
        }

        /// <summary>
        /// Converts a StringId from the source cache to destination cache
        /// </summary>
        /// <remarks>
        /// If the StringId exists in the destination cache exists then it is returned, otherwise one will be allocated
        /// </remarks>
        /// <param name="stringId">String id to convert</param>
        /// <returns>The existing or newly allocated string id in the destination cache</returns>
        protected virtual StringId ConvertStringId(StringId stringId)
        {
            if (stringId == StringId.Invalid || stringId == StringId.Empty)
                return stringId;

            if (PortedStringIds.ContainsKey(stringId.Value))
                return PortedStringIds[stringId.Value];

            string value = BlamCache.StringTable.GetString(stringId);
            if (value == null)
            {
                Log.Error($"Failed to resolve string while converting StringId {stringId}");
                return StringId.Invalid;
            }

            StringId edStringId = CacheContext.StringTable.GetOrAddString(value);

            PortedStringIds[stringId.Value] = edStringId;
            return edStringId;
        }

        /// <summary>
        /// Should be called when finished with the <see cref="PortingContext "/> to finalize any pending tasks and cleanup
        /// </summary>
        /// <remarks>
        /// Consider using <see cref="Dispose"/> or <seealso cref="CreateScope"/>
        /// </remarks>
        /// <param name="saveStrings">Save the string id cache</param>
        public virtual void Finish(bool saveStrings = true)
        {
            try
            {
                FinishAsync();
                FinishInternal();

                if (saveStrings)
                    CacheContext.SaveStrings();
                CacheContext.SaveTagNames();

                if (ShouldGenerateCampaignFile)
                    CampaignFileGenerator.GenerateCampaignFile(CacheContext, BlamCache);
            }
            finally
            {
                // discard the queue in case of an exception
                TagDefinitionCache.Clear();
                PortedTags.Clear();
                ShouldGenerateCampaignFile = false;
            }
        }

        /// <summary>
        /// To be implemented by derived classes
        /// </summary>
        /// <remarks>
        /// Implementators should avoid throwing exceptions
        /// </remarks>
        protected virtual void FinishInternal()
        {
        }

        /// <summary>
        /// Creates a scope that automatically calls <see cref="Finish"/> when disposed
        /// </summary>
        public IDisposable CreateScope(PortingFlags flags = PortingFlags.Default, bool saveStrings = true)
        {
            return new PortingContextScope(this, flags, saveStrings);
        }

        public void Dispose()
        {
            Finish();
            GC.SuppressFinalize(this);
        }

        private class PortingContextScope : IDisposable
        {
            private PortingContext _context;
            private PortingFlags _oldFlags;
            private bool _saveStrings;

            public PortingContextScope(PortingContext context, PortingFlags flags, bool saveStrings)
            {
                _context = context;
                _oldFlags = _context.Flags;
                _context.Flags = flags;
                _saveStrings = saveStrings;
            }

            public void Dispose()
            {
                _context.Finish(saveStrings: _saveStrings);
                _context.Flags = _oldFlags;
            }
        }
    }
}
