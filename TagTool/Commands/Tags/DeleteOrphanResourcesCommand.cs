using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TagTool.Cache;
using TagTool.Cache.HaloOnline;
using TagTool.Commands.Common;
using TagTool.Common;
using TagTool.Common.Logging;
using TagTool.Porting;
using TagTool.Tags;

namespace TagTool.Commands.Tags
{
    public class DeleteOrphanResourcesCommand : Command
    {
        private readonly GameCacheHaloOnlineBase Cache;

        public DeleteOrphanResourcesCommand(GameCacheHaloOnlineBase cache)
        : base(false,

              name:
              "DeleteOrphanResources",

              description:
              "Deletes resources that are not referenced by any tag in order to free up space.",

              usage:
              "DeleteOrphanResources [dryrun]",

              helpMessage:
              "It is recommended that you run this command at the end of your scripts to reduce pak sizes."
              + "\nOptionally specify 'dryrun' to see the list of orphan resources, without actually deleting them.")
        {
            Cache = cache;
        }

        public override object Execute(List<string> args)
        {
            bool dryRun = false;

            while (args.Count > 0)
            {
                switch (args[0].ToLower())
                {
                    case "dryrun":
                        dryRun = true;
                        args.RemoveAt(0);
                        break;
                    default:
                        return new TagToolError(CommandError.ArgInvalid, $"Invalid option '{args[0]}'");
                }
            }

            Console.WriteLine("Finding referenced resources...");

            HashSet<ResourceHandle> referencedResources = FindReferencedResources(Cache);

            long totalSize = 0;
            int totalCount = 0;

            foreach (ResourceLocation location in referencedResources.Select(x => x.Location).Distinct())
            {
                if (location == ResourceLocation.None)
                    continue;

                ResourceCacheHaloOnline resourceCache = Cache.ResourceCaches.GetResourceCache(location);
                using Stream resourceStream = Cache.ResourceCaches.OpenCacheReadWrite(location);

                for (int resourceIndex = resourceCache.Resources.Count - 1; resourceIndex >= 0; resourceIndex--)
                {
                    var handle = new ResourceHandle(location, resourceIndex);

                    if (referencedResources.Contains(handle))
                        continue;

                    if (resourceCache.Resources[resourceIndex].Offset == uint.MaxValue)
                        continue;

                    if (resourceCache.IsResourceShared(resourceIndex))
                    {
                        Log.Warning($"Skipping shared resource {handle}");
                        continue;
                    }

                    long size = resourceCache.Resources[resourceIndex].ChunkSize;

                    if (!dryRun)
                        resourceCache.NullResource(resourceStream, resourceIndex);

                    Console.WriteLine($"{handle}, Size = {FormatUtils.FormatBytes(size)}");

                    totalSize += size;
                    totalCount++;
                }
            }

            Console.WriteLine();
            Console.WriteLine($"{(dryRun ? "Found" : "Deleted")} {totalCount} orphan resources, Total Size = {FormatUtils.FormatBytes(totalSize)}");

            return true;
        }

        private static HashSet<ResourceHandle> FindReferencedResources(GameCacheHaloOnlineBase cache)
        {
            HashSet<ResourceHandle> foundResource = [];

            if (cache is GameCacheModPackage modCache)
            {
                for (int i = 0; i < modCache.BaseModPackage.GetTagCacheCount(); i++)
                {
                    modCache.SetActiveTagCache(i);
                    FindReferencedResources(modCache, foundResource);
                }
            }
            else
            {
                FindReferencedResources(cache, foundResource);
            }

            return foundResource;
        }

        private static void FindReferencedResources(GameCacheHaloOnlineBase cache, HashSet<ResourceHandle> foundResources)
        {
            using Stream stream = cache.OpenCacheRead();

            foreach (CachedTagHaloOnline tag in cache.TagCache.TagTable)
            {
                if (tag is null || tag.IsEmpty() || !tag.IsInGroup(PortingConstants.ResourceTagGroups))
                    continue;

                Traverse(cache, cache.Deserialize(stream, tag), foundResources);
            }

            static void Traverse(GameCacheHaloOnlineBase cache, object data, HashSet<ResourceHandle> foundResources)
            {
                switch (data)
                {
                    case null:
                    case byte[]:
                        break;
                    case PageableResource resource:
                        foundResources.Add(new ResourceHandle(resource.GetLocation(), resource.Page.Index));
                        break;
                    case TagStructure structure:
                        foreach (TagFieldInfo field in structure.GetTagFieldEnumerable(cache.Version, cache.Platform))
                            Traverse(cache, field.GetValue(structure), foundResources);
                        break;
                    case IList collection:
                        foreach (object element in collection)
                            Traverse(cache, element, foundResources);
                        break;
                }
            }
        }

        record struct ResourceHandle(ResourceLocation Location, int Index);
    }
}
