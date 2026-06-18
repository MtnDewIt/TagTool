using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using TagTool.Cache.HaloOnline;
using TagTool.Cache.Resources;
using TagTool.Common;
using TagTool.Extensions;
using TagTool.IO;
using TagTool.Serialization;
using TagTool.Tags;

namespace TagTool.Cache.ModPackages
{
    public class ResourceCachesModPackage : ResourceCachesHaloOnlineBase
    {
        private ModPackage Package;

        private Dictionary<string, ResourcePage> ExistingResources;

        private ResourceCacheHaloOnline ResourceCache;

        private GameCacheModPackage ModCache => (GameCacheModPackage)Cache;

        public ResourceCachesModPackage(GameCacheModPackage cache, ModPackage package)
        {
            Package = package;
            Cache = cache;
            ExistingResources = new Dictionary<string, ResourcePage>();
            ResourceCache = new ResourceCacheHaloOnline(cache.Version, cache.Platform, package.ResourcesStream);
            Serializer = new ResourceSerializer(Cache.Version, Cache.Platform);
            Deserializer = new ResourceDeserializer(Cache.Version, Cache.Platform);
        }

        public override ResourceCacheHaloOnline GetResourceCache(ResourceLocation location)
        {
            if (location == ResourceLocation.Mods)
                return ResourceCache;
            else
                return ModCache.BaseCacheReference.ResourceCaches.GetResourceCache(location);
        }

        public override Stream OpenCacheRead(ResourceLocation location)
        {
            if (location == ResourceLocation.Mods)
                return Package.ResourcesStream;
            else
                return ModCache.BaseCacheReference.ResourceCaches.OpenCacheRead(location);
        }

        public override Stream OpenCacheReadWrite(ResourceLocation location)
        {
            if (location == ResourceLocation.Mods)
                return Package.ResourcesStream;
            else
                return ModCache.BaseCacheReference.ResourceCaches.OpenCacheRead(location);
        }

        public override Stream OpenCacheWrite(ResourceLocation location)
        {
            if (location == ResourceLocation.Mods)
                return Package.ResourcesStream;
            else
                return ModCache.BaseCacheReference.ResourceCaches.OpenCacheRead(location);
        }

        public void RebuildResourceDictionary()
        {
            throw new Exception("Not implemented");
        }

        protected override TagResourceReference CreateResource<T>(T resourceDefinition, ResourceLocation location, TagResourceTypeGen3 resourceType)
        {
            location = ResourceLocation.Mods;

            return base.CreateResource(resourceDefinition, location, resourceType);
        }

        public override void ReplaceResource(PageableResource resource, ReadOnlySpan<byte> data)
        {
            RelocateResource(resource);

            base.ReplaceResource(resource, data);
        }

        public override void ReplaceRawResource(PageableResource resource, byte[] data)
        {
            RelocateResource(resource);

            base.ReplaceRawResource(resource, data);
        }

        public override void AddRawResource(PageableResource resource, ReadOnlySpan<byte> data)
        {
            resource.ChangeLocation(ResourceLocation.Mods);

            base.AddRawResource(resource, data);
        }

        public override void AddResource(PageableResource resource, ReadOnlySpan<byte> data)
        {
            ArgumentNullException.ThrowIfNull(resource);

            // change resource location
            resource.ChangeLocation(ResourceLocation.Mods);

            int dataSize = data.Length;
            string hash = Convert.ToBase64String(SHA1.HashData(data));

            // check if a perfect resource match exists, if yes reuse it to save memory in multicache packages
            if (ExistingResources.TryGetValue(hash, out ResourcePage existingPage) && existingPage.UncompressedBlockSize == dataSize)
            {
                resource.Page = existingPage;
                resource.DisableChecksum();
                Debug.WriteLine("Found perfect resource match, reusing resource!");
                return;
            }

            base.AddResource(resource, data);
            ExistingResources[hash] = resource.Page;
        }

        private static void RelocateResource(PageableResource resource)
        {
            resource.GetLocation(out ResourceLocation location);
            if(location != ResourceLocation.Mods)
            {
                // ensure sure we create a new resource
                resource.Page.Index = -1;

                resource.ChangeLocation(ResourceLocation.Mods);
            }
        }
    }
}
