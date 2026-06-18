using System;
using System.Collections.Generic;
using System.IO;
using TagTool.Bitmaps;
using TagTool.Cache;
using TagTool.Tags.Definitions;

namespace TagTool.Commands.Tags
{
    class ListInvalidBitmapsCommand : Command
    {
        public GameCache Cache { get; }

        public ListInvalidBitmapsCommand(GameCache cache)
            : base(false,

                  "ListInvalidBitmaps",
                  "Lists all bitmaps with an invalid tag resource in the current tag cache",

                  "ListInvalidBitmaps",
                  "Lists all bitmaps with an invalid tag resource in the current tag cache")
        {
            Cache = cache;
        }

        public override object Execute(List<string> args)
        {
            using Stream cacheStream = Cache.OpenCacheRead();

            int count = 0;

            foreach (CachedTag tag in Cache.TagCache.FindAllInGroup<Bitmap>())
            {
                Bitmap bitmap = Cache.Deserialize<Bitmap>(cacheStream, tag);

                if (!BitmapUtils.IsBitmapResourceValid(Cache, bitmap))
                {
                    Console.WriteLine($"{tag}");
                    count++;
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Found {count} invalid bitmap{(count > 1 ? "s" : "")}");

            return true;
        }
    }
}
