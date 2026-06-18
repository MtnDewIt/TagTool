using System;
using System.Collections.Generic;
using System.IO;
using TagTool.Cache;
using TagTool.Cache.HaloOnline;
using TagTool.Common;
using TagTool.Common.Logging;
using TagTool.Commands.Common;

namespace TagTool.Commands.Tags
{
    public class ImportTagCommand : Command
    {
        private GameCacheHaloOnlineBase Cache { get; }

        public ImportTagCommand(GameCacheHaloOnlineBase cache)
            : base(true,

                  "ImportTag",
                  "Overwrites a tag with data from a file.",

                  "ImportTag <Tag> <Path>",

                  "Overwrites a tag with data from a file.")
        {
            Cache = cache;
        }

        public override object Execute(List<string> args)
        {
            if (args.Count != 2)
                return new TagToolError(CommandError.ArgCount);
            if (!Cache.TagCache.TryGetCachedTag(args[0], out var instance))
                return new TagToolError(CommandError.TagInvalid);

            var path = args[1];

            if (!File.Exists(path))
                return new TagToolError(CommandError.FileNotFound, $"\"{path}\"");

            byte[] data;
            using (var inStream = File.OpenRead(path))
            {
                data = new byte[inStream.Length];
                inStream.ReadExactly(data);
            }

            if (!FormatRecognized(in data, in instance))
                return new TagToolError(CommandError.FileType, "Incompatible tag format."
                    + "\nIf this tag is from an editing kit, try ImportLooseTag");

            using (var stream = Cache.OpenCacheReadWrite())
            {
                Cache.TagCacheGenHO.SetTagDataRaw(stream, (CachedTagHaloOnline)instance, data);

                // Reserialize to avoid issues with missing tag reference fixups
                var definition = Cache.Deserialize(stream, instance);
                Cache.Serialize(stream, instance, definition);
            }

            Console.WriteLine($"Imported 0x{data.Length:X} bytes.");

            return true;
        }

        private bool FormatRecognized(in byte[] data, in CachedTag instance)
        {
            if (data is null || data.Length < 0x30)
                return false;

            int size = BitConverter.ToInt32(data, 0x4);
            if (size != data.Length)
                return false;

            Tag tag = new(BitConverter.ToInt32(data, 0x14));
            if (!Cache.TagCache.TagDefinitions.TagDefinitionExists(tag))
                return false;
            if (instance.Group.Tag != tag)
                Log.Warning($"Importing \'{tag}\' tag over instance of group \'{instance.Group.Tag}\'");

            if (data.Length == 0x30)
            {
                Log.Warning($"Tag is empty (size = 0x30)");
                return false;
            }

            return true;
        }
    }
}