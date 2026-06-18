using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TagTool.Audio.Bank;
using TagTool.BlamFile;
using TagTool.Cache.HaloOnline;
using TagTool.Cache.Monolithic;
using TagTool.Cache.Resources;
using TagTool.Common;
using TagTool.IO;
using TagTool.Scripting;
using TagTool.Serialization;
using TagTool.Tags;

namespace TagTool.Cache
{
    public abstract class GameCache
    {
        public string DisplayName = "default";
        public CacheVersion Version;
        public CachePlatform Platform;
        public EndianFormat Endianness;
        public TagSerializer Serializer;
        public TagDeserializer Deserializer;
        public DirectoryInfo Directory;

        private IScriptDefinitions _scriptDefinitions;
        public IScriptDefinitions ScriptDefinitions => _scriptDefinitions ??= ScriptDefinitionsFactory.Create(Version, Platform);

        public List<LocaleTable> LocaleTables;
        public SoundBankCache SoundBanks;

        public abstract StringTable StringTable { get; }
        public abstract TagCache TagCache { get; }
        public abstract ResourceCache ResourceCache { get; }

        public abstract Stream OpenCacheRead();
        public abstract Stream OpenCacheReadWrite();
        public abstract Stream OpenCacheWrite();

        public abstract void Serialize(Stream stream, CachedTag instance, object definition);
        public abstract object Deserialize(Stream stream, CachedTag instance, Type type);

        public object Deserialize(Stream stream, CachedTag instance)
        {
            return Deserialize(stream, instance, TagCache.TagDefinitions.GetTagDefinitionType(instance.Group));
        }

        public T Deserialize<T>(Stream stream, CachedTag instance)
        {
            return (T)Deserialize(stream, instance, typeof(T));
        }

        public static GameCache Open(string filePath) => Open(new FileInfo(filePath));

        public static GameCache Open(FileInfo file)
        {
            if(file.Name.Equals("blob_index.dat"))
                return new GameCacheMonolithic(file);

            MapFile map = new MapFile();
            var estimatedVersion = CacheVersion.HaloOnlineED;

            using (var stream = file.OpenRead())
            using (var reader = new EndianReader(stream))
            {
                if (file.Name.Contains(".map"))
                {
                    map.Read(reader);
                    estimatedVersion = map.Version;
                }
                else if (file.Name.Equals("tags.dat"))
                    estimatedVersion = CacheVersion.HaloOnlineED;
                else
                    throw new Exception("Invalid file passed to GameCache constructor");
            }

            switch (CacheVersionDetection.GetGeneration(estimatedVersion))
            {
                case CacheGeneration.First:
                    return new GameCacheGen1(map, file);

                case CacheGeneration.Second:
                    return new GameCacheGen2(map, file);

                case CacheGeneration.Third:
                    return new GameCacheGen3(map, file);

                case CacheGeneration.HaloOnline:
                    {
                        var directory = file.Directory.FullName;
                        var tagsPath = Path.Combine(directory, "tags.dat");
                        var tagsFile = new FileInfo(tagsPath);

                        if (!tagsFile.Exists)
                            throw new Exception("Failed to find tags.dat");

                        return new GameCacheHaloOnline(tagsFile.Directory);
                    }

                case CacheGeneration.Fourth:
                    return new GameCacheGen4(map, file);
            }

            return null;
        }

        public abstract void SaveStrings();

        public virtual void LoadLocaleTables(Stream stream) { }
        public virtual void LoadSoundBanks() { }
    }
}
