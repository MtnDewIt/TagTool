using System.Collections.Generic;
using System.IO;
using System.Linq;
using TagTool.BlamFile;
using TagTool.Cache;
using TagTool.Cache.HaloOnline;
using TagTool.Commands.Common;
using TagTool.Common;
using TagTool.Porting;
using TagTool.Tags.Definitions;

namespace TagTool.Commands
{
    public class RebuildCacheFileCommand : Command
    {
        private readonly GameCacheHaloOnline Cache;

        public RebuildCacheFileCommand(GameCacheHaloOnline cache) : base(false,
            "RebuildCacheFile",

            "Create new cache files from the current cache.",

            "RebuildCacheFile [force] <dest-dir>",

            "")
        {
            Cache = cache;
        }

        public override object Execute(List<string> args)
        {
            if (args.Count < 1)
                return new TagToolError(CommandError.ArgCount);

            bool force = false;
            if (args.Count > 1 && args[0] == "force")
            {
                force = true;
                args.RemoveAt(0);
            }

            // Create the directory
            var dir = new DirectoryInfo(args[0]);

            if (!force && !FileUtils.IsDirectoryEmpty(dir.FullName))
                return new TagToolError(CommandError.FileIO, "Destination directory is not empty! Use 'force' to override.");

            if (dir.Exists)
                dir.Delete(recursive: true);
            dir.Create();

            var destCache = new GameCacheHaloOnline(dir, Cache.Version);

            // Ensure required resource cache files get created
            destCache.ResourceCaches.GetResourceCache(ResourceLocation.Audio);
            destCache.ResourceCaches.GetResourceCache(ResourceLocation.Resources);
            destCache.ResourceCaches.GetResourceCache(ResourceLocation.ResourcesB);
            destCache.ResourceCaches.GetResourceCache(ResourceLocation.Textures);
            destCache.ResourceCaches.GetResourceCache(ResourceLocation.TexturesB);

            // Copy fonts
            var fontsDir = Path.Combine(Cache.Directory.FullName, "fonts");
            if (Directory.Exists(fontsDir))
                FileUtils.CopyDirectory(fontsDir, Path.Combine(destCache.Directory.FullName, "fonts"));

            // Copy the old string ids
            destCache.StringTable.Clear();
            destCache.StringTable.AddRange(Cache.StringTable);

            using var context = PortingContext.Create(destCache, Cache);

            // Copy globals
            context.PortTag("*.cfgt");

            // Copy the scenario tags
            foreach (CachedTag scnrTag in Cache.TagCache.FindAllInGroup<Scenario>().OrderBy(x => x.Name))
            {
                using (var oldStream = Cache.OpenCacheRead())
                {
                    var scenario = Cache.Deserialize<Scenario>(oldStream, scnrTag);

                    // Copy map file
                    MapFile mapFile = Cache.MapFiles.FindByMapId(scenario.MapId);
                    if (mapFile != null)
                        destCache.MapFiles.Add(mapFile);
                }

                context.PortTag(scnrTag);
            }

            return true;
        }
    }
}
