﻿using BlamCore.Cache;
using BlamCore.Commands;
using BlamCore.Serialization;
using BlamCore.TagDefinitions;
using System;
using System.Collections.Generic;
using System.IO;

namespace TagTool.Files
{
    class AddFileCommand : Command
    {
        private GameCacheContext CacheContext { get; }
        private CachedTagInstance Tag { get; }
        private VFilesList Definition { get; }

        public AddFileCommand(GameCacheContext cacheContext, CachedTagInstance tag, VFilesList definition) :
            base(CommandFlags.Inherit,
                
                "AddFile",
                "Adds a new file to the virtual files list.",

                "AddFile <folder> <path>",

                "Adds a new file to the virtual files list.")
        {
            CacheContext = cacheContext;
            Tag = tag;
            Definition = definition;
        }

        public override object Execute(List<string> args)
        {
            if (args.Count != 2)
                return false;

            var folder = args[0].Replace('/', '\\');
            var file = new FileInfo(args[1]);

            if (!folder.EndsWith("\\"))
                folder += "\\";

            if (!file.Exists)
            {
                Console.WriteLine($"ERROR: File not found: \"{file.FullName}\"");
                return false;
            }

            Definition.Insert(file.Name, folder, File.ReadAllBytes(file.FullName));

            using (var stream = CacheContext.OpenTagCacheReadWrite())
                CacheContext.Serializer.Serialize(new TagSerializationContext(stream, CacheContext, Tag), Definition);

            Console.WriteLine($"Add virtual file \"{folder}\".");

            return true;
        }
    }
}