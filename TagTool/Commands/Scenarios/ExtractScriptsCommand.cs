using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TagTool.Cache;
using TagTool.Commands.Common;
using TagTool.Common;
using TagTool.IO;
using TagTool.Scripting;
using TagTool.Tags.Definitions;

namespace TagTool.Commands.Scenarios
{
    public class ExtractScriptsCommand : Command
    {
        private GameCache Cache { get; }
        private CachedTag Tag { get; }
        private Scenario Definition { get; }

        public ExtractScriptsCommand(GameCache cache, CachedTag tag, Scenario definition)
            : base(true,

                  "ExtractScripts",
                  "Extracts all scripts in the current scenario tag to a file.",

                  "ExtractScripts [no-cleanup] [spaces] [path]",

                  "Extracts all scripts in the current scenario tag to a file." +
                  "\n- no-cleanup: Do not clean up control flow for source accuracy" +
                  "\n- spaces: indent with spaces rather than tabs")
        {
            Cache = cache;
            Tag = tag;
            Definition = definition;
        }

        public override object Execute(List<string> args)
        {
            if (args.Count > 2)
                return new TagToolError(CommandError.ArgCount);

            FileInfo scriptFile;
            string path = "haloscript";
            string extension = ".hsc";
            bool cleanup = true;
            bool spaces = false;

            foreach (var arg in args)
            {
                switch (arg.ToLower())
                {
                    case "no-cleanup":
                        cleanup = false;
                        break;
                    case "spaces":
                        spaces = true;
                        break;
                    default:
                        path = arg;
                        break;
                }
            }

            if (Path.GetExtension(path) != extension)
            {
                var split = path.Split('.');
                string newPath = split[0];

                if (split.Length > 1)
                    path = $"{newPath}{extension}";
                else
                {
                    string scenario = Path.GetFileNameWithoutExtension(Tag.ToString());
                    string platform = Cache.Platform == CachePlatform.Original ? string.Empty : $"{Cache.Platform}";
                    string fileName = $"{Cache.Version}{platform}_{Definition.MapId}_{scenario}{extension}";

                    path = Path.Combine(newPath, fileName);
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path));
            scriptFile = new FileInfo(path);

            using (var scriptFileStream = scriptFile.Create())
            using (var scriptWriter = new StreamWriter(scriptFileStream))
            {
                var decompiler = new ScriptDecompiler(Cache, Definition, Tag, cleanup, spaces);
                decompiler.DecompileScripts(scriptWriter);
            }

            Console.WriteLine($"\nDecompiled script extracted to \"{scriptFile.FullName}\"");

            return true;
        }
    }
}