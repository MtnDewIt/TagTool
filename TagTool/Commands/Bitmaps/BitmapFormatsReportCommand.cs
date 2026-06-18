using System;
using System.Collections.Generic;
using System.Linq;
using TagTool.Bitmaps;
using TagTool.Cache;
using TagTool.Cache.HaloOnline;
using TagTool.Common;
using TagTool.Tags;
using TagTool.Tags.Definitions;

namespace TagTool.Commands.Bitmaps
{
    public class BitmapFormatsReportCommand : Command
    {
        private readonly GameCacheHaloOnlineBase Cache;

        public BitmapFormatsReportCommand(GameCacheHaloOnlineBase cache) :
            base(true,

                "BitmapFormatsReport",
                "Generate a report on bitmap format usage",

                "",

                "")
        {
            Cache = cache;
        }

        public override object Execute(List<string> args)
        {
            using var stream = Cache.OpenCacheRead();

            Dictionary<BitmapFormat, FormatReport> formatReports = [];
            HashSet<long> seen = [];

            foreach (CachedTagHaloOnline tag in Cache.TagCache.FindAllInGroup<Bitmap>())
            {
                // ignore base cache tags for paks
                if (tag.IsEmpty())
                    continue;

                Bitmap bitmap = Cache.Deserialize<Bitmap>(stream, tag);

                for (int i = 0; i < bitmap.Images.Count; i++)
                {
                    Bitmap.Image image = bitmap.Images[i];

                    long resourceId = GetResourceID(bitmap.HardwareTextures[i]);
                    if (resourceId == -1)
                        continue;

                    uint dataSize = bitmap.HardwareTextures[i].HaloOnlinePageableResource?.Page?.CompressedBlockSize ?? 0u;

                    // avoid accumulating shared resources
                    if (!seen.Add(resourceId))
                        dataSize = 0;

                    if (!formatReports.TryGetValue(image.Format, out FormatReport report))
                        formatReports.Add(image.Format, report = new());

                    report.Count++;
                    report.DataSize += dataSize;
                }
            }

            long totalSize = formatReports.Values.Sum(r => r.DataSize);
            int totalCount = formatReports.Values.Sum(r => r.Count);

            Console.WriteLine("╔═══════════════════════════════════════════════════════════╗");
            Console.WriteLine($"║  {"Format",-18}  {"Count",8}  {"Size",12}  {"% of Total",11}  ║");
            Console.WriteLine("╠═══════════════════════════════════════════════════════════╣");

            foreach ((BitmapFormat format, FormatReport report) in formatReports.OrderByDescending(x => x.Value.DataSize))
            {
                double percent = totalSize > 0 ? (report.DataSize * 100.0) / totalSize : 0;
                Console.WriteLine($"║  {format,-18}  {report.Count,8:N0}  {FormatUtils.FormatBytes(report.DataSize),12}  {percent,10:F2}%  ║");
            }

            Console.WriteLine("╠═══════════════════════════════════════════════════════════╣");
            Console.WriteLine($"║  {"TOTAL",-18}  {totalCount,8:N0}  {FormatUtils.FormatBytes(totalSize),12}  {"100.00",10}%  ║");
            Console.WriteLine("╚═══════════════════════════════════════════════════════════╝");

            return true;
        }

        static long GetResourceID(TagResourceReference resource)
        {
            if (resource.HaloOnlinePageableResource is null || resource.HaloOnlinePageableResource.Page.Index < 0)
                return -1;

            return ((long)resource.HaloOnlinePageableResource.GetLocation() << 32) | (long)resource.HaloOnlinePageableResource.Page.Index;
        }
        class FormatReport
        {
            public int Count;
            public uint DataSize;
        }
    }
}
