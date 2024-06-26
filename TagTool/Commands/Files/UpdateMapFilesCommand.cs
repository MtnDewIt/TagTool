using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TagTool.Cache;
using TagTool.Commands.Common;
using TagTool.Tags.Definitions;
using TagTool.IO;
using TagTool.BlamFile;
using TagTool.Cache.HaloOnline;

namespace TagTool.Commands.Files
{
    class UpdateMapFilesCommand : Command
    {
        public GameCache Cache { get; }

        public UpdateMapFilesCommand(GameCache cache)
            : base(true,

                  "UpdateMapFiles",
                  "Updates the game's .map files to contain valid scenario indices.",

                  "UpdateMapFiles <MapInfo Directory> [forceupdate]",

                  "Updates the game's .map files to contain valid scenario indices.")
        {
            Cache = cache;
        }

        public override object Execute(List<string> args)
        {
            if (args.Count > 2)
                return new TagToolError(CommandError.ArgCount);

            bool forceUpdate = false;
            bool pathProvided = false;
            string mapInfoPath = "";

            if (args.Count >= 1)
            {
                pathProvided = true;

                if (!Directory.Exists(args[0]))
                    return new TagToolError(CommandError.ArgInvalid, "Given mapinfo directory does not exist.");
                else
                    mapInfoPath = args[0];
            }

            if (args.Count == 2)
                if (args[1].ToLower() == "forceupdate")
                    forceUpdate = true;

            if (Cache is GameCacheHaloOnline)
            {
                // Generate / update the map files
                foreach (var scenario in Cache.TagCache.FindAllInGroup("scnr"))
                {
                    var name = scenario.Name.Split('\\').Last();
                    var mapInfoName = $"{name}.mapinfo";
                    var mapFileName = $"{name}.map";
                    var targetPath = Path.Combine(Cache.Directory.FullName, mapFileName);

                    MapFile map;
                    Blf mapInfo = null;

                    if (pathProvided)
                    {
                        var mapInfoDir = new DirectoryInfo(mapInfoPath);
                        var files = mapInfoDir.GetFiles(mapInfoName);
                        if (files.Length != 0)
                        {
                            var mapInfoFile = files[0];
                            using (var stream = mapInfoFile.Open(FileMode.Open, FileAccess.Read))
                            using (var reader = new EndianReader(stream))
                            {
                                CacheVersion version = CacheVersion.Halo3Retail;
                                CachePlatform platform = CachePlatform.Original;

                                switch (reader.Length)
                                {
                                    case 0x4e91:
                                        version = CacheVersion.Halo3Retail;
                                        break;
                                    case 0x9A01:
                                        version = CacheVersion.Halo3ODST;
                                        break;
                                    case 0xCDD9:
                                        version = CacheVersion.HaloReach;
                                        break;
                                    default:
                                        throw new Exception("Unexpected map info file size");
                                }
                                mapInfo = new Blf(version, platform);
                                mapInfo.Read(reader);
                                mapInfo.ConvertBlf(Cache.Version);
                            }
                        }
                    }

                    try
                    {
                        var fileInfo = Cache.Directory.GetFiles(mapFileName)[0];
                        map = new MapFile();
                        using (var stream = fileInfo.Open(FileMode.Open, FileAccess.Read))
                        using (var reader = new EndianReader(stream))
                            map.Read(reader);

                        var header = (CacheFileHeaderGenHaloOnline)map.Header;
                        header.ScenarioTagIndex = scenario.Index;

                        if (mapInfo != null)
                            if (forceUpdate || map.MapFileBlf == null)
                                map.MapFileBlf = mapInfo;

                    }
                    catch (Exception)
                    {
                        using(var tagStream = Cache.OpenCacheRead())
                        {
                            var scnr = Cache.Deserialize<Scenario>(tagStream, scenario);

                            var mapBuilder = new MapFileBuilder(Cache.Version);
                            if (mapInfo != null)
                                mapBuilder.MapInfo = mapInfo.Scenario;

                            map = mapBuilder.Build(scenario, scnr);
                        }
                        
                    }

                    var targetFile = new FileInfo(targetPath);
                    using (var stream = targetFile.Create())
                    using (var writer = new EndianWriter(stream))
                    {
                        map.Write(writer);
                    }

                    if (mapInfo != null)
                        Console.WriteLine($"Scenario tag index for {name}: 0x{scenario.Index.ToString("X4")} (using map info)");
                    else
                        Console.WriteLine($"Scenario tag index for {name}: 0x{scenario.Index.ToString("X4")} (WARNING: not using map info)");

                }
                Console.WriteLine("Done!");
                return true;
            }
            else if(Cache is GameCacheModPackage modCache)
            {
                // Generate / update the map files
                foreach (var scenario in Cache.TagCache.FindAllInGroup("scnr"))
                {
                    // ignore maps that are in the base cache unmodified
                    if ((scenario as CachedTagHaloOnline).IsEmpty())
                        continue;

                    var name = scenario.Name.Split('\\').Last();
                    var mapInfoName = $"{name}.mapinfo";
                    var mapFileName = $"{name}.map";

                    MapFile map;
                    Blf mapInfo = null;

                    if (pathProvided)
                    {
                        var mapInfoDir = new DirectoryInfo(mapInfoPath);
                        var files = mapInfoDir.GetFiles(mapInfoName);
                        if (files.Length != 0)
                        {
                            var mapInfoFile = files[0];
                            using (var stream = mapInfoFile.Open(FileMode.Open, FileAccess.Read))
                            using (var reader = new EndianReader(stream))
                            {
                                CacheVersion version = CacheVersion.Halo3Retail;
                                CachePlatform platform = CachePlatform.Original;

                                switch (reader.Length)
                                {
                                    case 0x4e91:
                                        version = CacheVersion.Halo3Retail;
                                        break;
                                    case 0x9A01:
                                        version = CacheVersion.Halo3ODST;
                                        break;
                                    case 0xCDD9:
                                        version = CacheVersion.HaloReach;
                                        break;
                                    default:
                                        throw new Exception("Unexpected map info file size");
                                }
                                mapInfo = new Blf(version, platform);
                                mapInfo.Read(reader);
                                mapInfo.ConvertBlf(Cache.Version);
                            }
                        }
                    }

                    var tagStream = Cache.OpenCacheRead();
                    var scnr = Cache.Deserialize<Scenario>(tagStream, scenario);

                    var mapBuilder = new MapFileBuilder(Cache.Version);
                    mapBuilder.MapInfo = mapInfo?.Scenario;
                    map = mapBuilder.Build(scenario, scnr);

                    var mapStream = new MemoryStream();
                    var writer = new EndianWriter(mapStream, leaveOpen: true);
                    map.Write(writer);

                    var header = (CacheFileHeaderGenHaloOnline)map.Header;
                    modCache.AddMapFile(mapStream, header.MapId);

                    if (mapInfo != null)
                        Console.WriteLine($"Scenario 0x{scenario.Index:X4} \"{name}\" using map info");
                    else if (args.Count == 0)
                        Console.WriteLine($"Scenario 0x{scenario.Index:X4} \"{name}\" NOT using map info");
                    else
                        new TagToolWarning($"Scenario 0x{scenario.Index:X4} \"{name}\" NOT using map info");
                }
                Console.WriteLine("Done!");
                return true;
            }

            return new TagToolError(CommandError.CacheUnsupported);
        }
    }
}