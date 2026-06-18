using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using TagTool.Cache;
using TagTool.Common.Logging;

namespace TagTool.Scripting
{
    public interface IScriptDefinitions
    {
        IReadOnlyDictionary<int, string> ValueTypes { get; }
        IReadOnlyDictionary<int, string> Globals { get; }
        IReadOnlyDictionary<int, ScriptInfo> Scripts { get; }
    }

    public class ScriptDefinitionsFactory
    {
        private static readonly ConcurrentDictionary<(CacheVersion, CachePlatform), IScriptDefinitions> _cache = [];

        public static IScriptDefinitions Create(CacheVersion version, CachePlatform platform)
        {
            if (_cache.TryGetValue((version, platform), out IScriptDefinitions definitions))
                return definitions;

            string fileName = GetFileNameForVersion(version, platform);
            if (fileName == null)
                return null;

            string xmlFilePath = Path.Combine(DirectoryPaths.Data, $"hs\\definitions\\{fileName}.xml");
            if (!File.Exists(xmlFilePath))
            {
                Log.Warning($"Script definitions file not found \"{xmlFilePath}\"");
                return null;
            }

            definitions = new ScriptDefinitionsXml(xmlFilePath);
            _cache.TryAdd((version, platform), definitions);
            return definitions;
        }

        private static string GetFileNameForVersion(CacheVersion version, CachePlatform platform)
        {
            return version switch
            {
                CacheVersion.Halo2PC when platform == CachePlatform.MCC => "halo2_mcc",
                CacheVersion.Halo3Retail when platform == CachePlatform.MCC => "halo3_mcc",
                CacheVersion.Halo3ODST when platform == CachePlatform.MCC => "halo3_odst_mcc",
                CacheVersion.HaloReach when platform == CachePlatform.MCC => "halo_reach_mcc",
                CacheVersion.Halo2PC => "halo2_vista",
                CacheVersion.Halo3Retail => "halo3",
                CacheVersion.Halo3ODST => "halo3_odst",
                CacheVersion.HaloReach => "halo_reach",
                CacheVersion.HaloOnline106708 => "halo_online_ed", // TODO: separate
                >= CacheVersion.HaloOnlineED and < CacheVersion.HaloOnlineED_END => "halo_online_ed",
                _ => null,
            };
        }
    }
}
