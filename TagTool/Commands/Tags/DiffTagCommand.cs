using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TagTool.Cache;
using TagTool.Commands.Common;
using TagTool.Common;
using TagTool.Tags;

namespace TagTool.Commands.Tags
{
    class DiffTagCommand : Command
    {
        private GameCache Cache1 { get; }
        private GameCache Cache2 { get; }

        public DiffTagCommand(GameCache cache1, GameCache cache2)
            : base(true,
                   "DiffTag",
                   "Deep compares two tags and lists their differences.",

                   "DiffTag [simple] [ignore_data] [same_count_only] [generic] <tag> [other tag]",

                   "Deep compares two tags and lists their differences. Use the \"simple\" argument to list only the difference count.\n" +
                   "In a porting context, you can specify only one tag name to compare between caches." +
                   "\n- simple: print difference count only" +
                   "\n- ignore_data: do not diff byte arrays" +
                   "\n- same_count_only: do not diff lists with different counts" +
                   "\n- generic: do not perform specialized diffs when available (e.g. rendermethod)")
        {
            Cache1 = cache1;
            Cache2 = cache2;
        }

        public override object Execute(List<string> args)
        {
            if (args.Count < 1)
                return new TagToolError(CommandError.ArgCount);

            bool simple = false;
            bool ignoreData = false;
            bool sameCountOnly = false;
            bool generic = false;

            List<string> names = [];
            foreach (var arg in args)
            {
                switch (arg.ToLower())
                {
                    case "simple":
                        simple = true;
                        break;
                    case "ignore_data":
                        ignoreData = true;
                        break;
                    case "same_count_only":
                        sameCountOnly = true;
                        break;
                    case "generic":
                        generic = true;
                        break;
                    default:
                        names.Add(arg);
                        break;
                }
            }

            if (names.Count == 0 || names.Count > 2)
                return new TagToolError(CommandError.ArgCount, $"Invalid tag name count: {names.Count}"
                    + $"\n> {string.Join("\n> ", names)}");

            if (!Cache1.TagCache.TryGetCachedTag(names.First(), out CachedTag tag1))
                return new TagToolError(CommandError.TagInvalid, $"\"{names.First()}\"");

            if (!Cache2.TagCache.TryGetCachedTag(names.Last(), out CachedTag tag2))
                return new TagToolError(CommandError.TagInvalid, $"\"{names.Last()}\"");

            if (tag1.Group.Tag != tag2.Group.Tag)
                return new TagToolError(CommandError.ArgInvalid, $"Tag group mismatch: {tag1.Group.Tag}, {tag2.Group.Tag}");

            List<TagStructureDiffer.Difference> differences = [];
            using var stream1 = Cache1.OpenCacheRead();
            using var stream2 = Cache2.OpenCacheRead();
            {
                var differ = new TagStructureDiffer(Cache1, Cache2, stream1, stream2)
                {
                    IgnoreData = ignoreData,
                    SameCountOnly = sameCountOnly,
                    Generic = generic,
                };

                var definition1 = (TagStructure)Cache1.Deserialize(stream1, tag1);
                var definition2 = (TagStructure)Cache2.Deserialize(stream2, tag2);
                differences = differ.Diff(definition1, definition2);
            }

            if (!simple)
            {
                Console.WriteLine($"\n{differences.Count} differences:\n");

                foreach (var diff in differences)
                {
                    bool isCachedTag = diff.Value1 is CachedTag || diff.Value2 is CachedTag;
                    var value1 = diff.Value1 ?? (isCachedTag ? "null" : "");
                    var value2 = diff.Value2 ?? (isCachedTag ? "null" : "");

                    if (diff.Kind == TagStructureDiffer.DifferenceKind.ElementCount)
                        Console.WriteLine($"{diff.Path}.Count {Ansi.BrightBlue}{((IList)diff.Value1).Count}{Ansi.Reset} | {Ansi.BrightRed}{((IList)diff.Value2).Count}{Ansi.Reset}");
                    else
                        Console.WriteLine($"{diff.Path} {Ansi.BrightBlue}{value1}{Ansi.Reset} | {Ansi.BrightRed}{value2}{Ansi.Reset}");
                }
            }
            else
            {
                Console.WriteLine($"\n{differences.Count} differences.");
            }

            return true;
        }
    }
}
