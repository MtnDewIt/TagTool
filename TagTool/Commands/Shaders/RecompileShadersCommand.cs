using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using TagTool.Cache;
using TagTool.Commands.Common;
using TagTool.Tags.Definitions;
using static TagTool.Commands.Shaders.GenerateShaderCommand;
using TagTool.Shaders.ShaderGenerator;
using TagTool.Cache.HaloOnline;
using TagTool.Common.Logging;

namespace TagTool.Commands.Shaders
{
    class RecompileShadersCommand : Command
    {
        GameCache Cache;

        public RecompileShadersCommand(GameCache cache) :
            base(true,

                "RecompileShaders",
                "Recompiles all shader templates",

                "RecompileShaders [shader type]",

                "Recompiles all shader templates\n" +
                "[shader type] - Specify shader type, EX. \"shader\" for \'rmsh\'.")
        {
            Cache = cache;
        }

        private static Dictionary<string, List<string>> ExplicitShaderGroups = new Dictionary<string, List<string>>
        {
            { "decorator", new List<string> { "decorator_default", "decorator_no_wind", "decorator_shaded", "decorator_static", "decorator_sun", "decorator_wavy" } },
            { "chud", new List<string> { "chud_cortana_camera", "chud_cortana_composite", "chud_cortana_offscreen", "chud_cortana_screen", "chud_cortana_screen_final",
                "chud_crosshair", "chud_directional_damage", "chud_directional_damage_apply", "chud_double_gradient", "chud_emblem", "chud_medal", "chud_meter",
                "chud_meter_chapter", "chud_meter_gradient", "chud_meter_shield", "chud_meter_single_color", "chud_navpoint", "chud_radial_gradient",
                "chud_really_simple", "chud_sensor", "chud_simple", "chud_solid", "chud_text_simple", "chud_texture_cam", "chud_turbulence" } },
            { "shadow_apply", new List<string> { "shadow_apply", "shadow_apply_bilinear", "shadow_apply_fancy", "shadow_apply_faster", "shadow_apply_point", } },
            { "final_composite", new List<string> { "final_composite", "final_composite_debug", "final_composite_dof", "final_composite_zoom" } },
            { "displacement", new List<string> { "displacement", "displacement_motion_blur" } },
        };

        public override object Execute(List<string> args)
        {
            using (var stream = Cache.OpenCacheReadWrite())
            {
                if (args.Count > 0)
                {
                    string shaderType = args[0].ToLower();

                    if (ExplicitShaderGroups.ContainsKey(shaderType))
                        return RecompileExplicitShaderGroup(stream, shaderType);

                    return RecompileShaderTypeAsync(stream, shaderType);
                }
                else
                {
                    foreach (string shaderType in Enum.GetNames(typeof(HaloShaderGenerator.Shared.ShaderType)))
                    {
                        RecompileShaderTypeAsync(stream, shaderType.ToLower());
                    }
                }
            }
            return true;
        }

        private object RecompileExplicitShaderGroup(Stream stream, string shaderType)
        {
            var shaderList = ExplicitShaderGroups[shaderType];

            List<SExplicitRecompileInfo> recompileInfo = new List<SExplicitRecompileInfo>();

            if (shaderType == "chud")
            {
                var chgd = Cache.Deserialize<ChudGlobalsDefinition>(stream, Cache.TagCache.FindFirstInGroup("chgd"));

                for (int i = 0; i < chgd.HudShaders.Count-1; i++) // skip last as it is HO only
                {
                    SExplicitRecompileInfo info = new SExplicitRecompileInfo
                    {
                        IsChud = true,
                        PixelTag = chgd.HudShaders[i].PixelShader,
                        VertexTag = chgd.HudShaders[i].VertexShader,
                        ExplicitName = chgd.HudShaders[i].PixelShader.Name.Split('\\').Last(),
                    };
                    recompileInfo.Add(info);
                }
            }
            else
            {
                foreach (var explicitShader in shaderList)
                {
                    SExplicitRecompileInfo info = new SExplicitRecompileInfo();

                    if (!Cache.TagCache.TryGetTag($"rasterizer\\shaders\\{explicitShader}.pixl", out info.PixelTag) ||
                        !Cache.TagCache.TryGetTag($"rasterizer\\shaders\\{explicitShader}.vtsh", out info.VertexTag))
                    {
                        Log.Error($"Explicit shader {explicitShader} could not be found (skipping)");
                    }
                    else
                    {
                        info.IsChud = false;
                        info.ExplicitName = explicitShader;
                        recompileInfo.Add(info);
                    }
                }
            }

            List<Task<SExplicitRecompileInfo>> tasks = new List<Task<SExplicitRecompileInfo>>();

            foreach (var info in recompileInfo)
            {
                Task<SExplicitRecompileInfo> generatorTask = Task.Run(() => {
                    return GenerateExplicitShaderAsync(Cache, info);
                });
                tasks.Add(generatorTask);
            }

            float percentageComplete = 0.00f;
            Console.Write($"\rRecompiling {shaderType} shaders... {string.Format("{0:0.00}", percentageComplete)}%");

            int completed = 0;
            while (completed != tasks.Count)
            {
                int count = tasks.FindAll(x => x.IsCompleted).Count;
                if (count > completed)
                {
                    completed = count;

                    percentageComplete = ((float)count / (float)tasks.Count) * 100.0f;
                    Console.Write($"\rRecompiling {shaderType} templates... {string.Format("{0:0.00}", percentageComplete)}%");
                }

                System.Threading.Thread.Sleep(100); // wait to prevent constant cli writes
            }

            Console.Write($"\rSuccessfully recompiled {tasks.Count} {shaderType} templates. Serializing...");

            // serialize
            foreach (var task in tasks)
            {
                Cache.Serialize(stream, task.Result.PixelTag, task.Result.PixelShader);
                Cache.Serialize(stream, task.Result.VertexTag, task.Result.VertexShader);
                (Cache as GameCacheHaloOnlineBase).SaveTagNames();
            }

            Console.Write($"\rSuccessfully recompiled {tasks.Count} {shaderType} shaders. Serializing... Done");
            Console.WriteLine();

            return true;
        }

        private object RecompileShaderTypeAsync(Stream stream, string shaderType)
        {
            if (!Cache.TagCache.TryGetTag($"shaders\\{shaderType}.rmdf", out CachedTag rmdfTag))
                return new TagToolError(CommandError.TagInvalid, $"Missing \"{shaderType}\" rmdf");

            var rmdf = Cache.Deserialize<RenderMethodDefinition>(stream, rmdfTag);

            if (rmdf.GlobalVertexShader == null || rmdf.GlobalPixelShader == null)
                return new TagToolError(CommandError.TagInvalid, "A global shader was missing from rmdf");

            var glvs = Cache.Deserialize<GlobalVertexShader>(stream, rmdf.GlobalVertexShader);
            var glps = Cache.Deserialize<GlobalPixelShader>(stream, rmdf.GlobalPixelShader);

            RecompileTemplates(Cache, stream, shaderType, rmdf, glvs, glps);

            return true;
        }
    }
}
