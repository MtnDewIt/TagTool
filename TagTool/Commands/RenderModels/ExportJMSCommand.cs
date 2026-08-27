using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using TagTool.Cache;
using TagTool.Commands.Common;
using TagTool.Common;
using TagTool.Geometry.Jms;
using TagTool.Tags.Definitions;

namespace TagTool.Commands.RenderModels
{
    public class ExportJMSCommand : Command
    {
        private GameCache Cache { get; }
        private CachedTag Tag { get; }
        private RenderModel Definition { get; }

        public ExportJMSCommand(GameCache cache, CachedTag tag, RenderModel definition) :
            base(true,

                "ExportJMS",
                "Extract render geometry in JMS format.",

                "ExportJMS <path>",

                "Extract render geometry in JMS format.")
        {
            Cache = cache;
            Tag = tag;
            Definition = definition;
        }

        public override object Execute(List<string> args)
        {
            if (args.Count != 1)
                return new TagToolError(CommandError.ArgCount);

            string path = args[0];

            if (!Path.HasExtension(path))
            {
                string fileName = $"{Tag.Name.Split('\\').Last()}_render.jms";
                path = Path.Combine(path, fileName);
            }

            FileInfo file = new(path);
            JmsFormat jms = new();
            
            if (Definition.Nodes is null || Definition.Nodes.Count == 0)
                return new TagToolError(CommandError.OperationFailed, "Model has no nodes, couldn't build JMS!");

            using (var cacheStream = Cache.OpenCacheRead())
            {
                var resource = Cache.ResourceCache.GetRenderGeometryApiResourceDefinition(Definition.Geometry.Resource);
                Definition.Geometry.SetResourceBuffers(resource, true);
                JmsModeExporter exporter = new(Cache, jms);
                exporter.Export(Definition);
            }

            jms.Write(file);
            Console.WriteLine($"Exported to \"{file.FullName}\".");

            return true;
        }
    }
}
