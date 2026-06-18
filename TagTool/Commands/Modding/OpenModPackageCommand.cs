using TagTool.Cache;
using System.Collections.Generic;
using System;
using TagTool.Commands.Common;
using TagTool.Commands.Tags;
using System.IO;

namespace TagTool.Commands.Modding
{
    class OpenModPackageCommand : Command
    {
        private readonly GameCacheHaloOnlineBase Cache;
        private CommandContextStack ContextStack { get; }
        private GameCacheModPackage ModCache;
        private CommandContext Context;

        public OpenModPackageCommand(CommandContextStack contextStack, GameCacheHaloOnlineBase cache) :
            base(true,

                "OpenModPackage",
                "Create context for an existing mod package.",

                "OpenModPackage [large] <.pak path>",

                "Create context for an existing mod package.")
        {
            ContextStack = contextStack;
            Cache = cache;
        }

        public override object Execute(List<string> args)
        {
            if (args.Count > 1 && args[0].ToLower() == "large")
            {
                args.RemoveAt(0);
            }

            if(args.Count != 1)
                return new TagToolError(CommandError.ArgCount);

            string path = args[0].Trim('/', '\\');
            if (!path.EndsWith(".pak"))
                path += ".pak";
            
            var file = new FileInfo(path);
            if (!file.Exists && string.IsNullOrEmpty(Path.GetPathRoot(path)))
            {
                GameCache baseCache = Cache;
                while (baseCache is GameCacheModPackage mod)
                    baseCache = mod.BaseCacheReference;

                string parent = baseCache.Directory.Parent.FullName;
                file = new(Path.Combine(parent, "mods", path));
            }

            if (!file.Exists)
                return new TagToolError(CommandError.FileNotFound, $"{path}");

            Console.WriteLine("Initializing cache...");

            ModCache = new GameCacheModPackage(Cache, file);
            Context = TagCacheContextFactory.Create(ContextStack, ModCache, $"{ModCache.BaseModPackage.Metadata.Name}.pak");
            ContextStack.Push(Context);

            Console.WriteLine("Done!");

            return true;
        }
    }
}