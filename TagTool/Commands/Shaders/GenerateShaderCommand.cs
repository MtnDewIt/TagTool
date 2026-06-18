using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TagTool.Cache;
using TagTool.Commands.Common;
using TagTool.Tags;
using TagTool.Tags.Definitions;
using TagTool.Shaders;
using TagTool.Shaders.ShaderFunctions;
using HaloShaderGenerator.Shader;
using static TagTool.Tags.Definitions.RenderMethod.RenderMethodPostprocessBlock;
using TagTool.Shaders.ShaderGenerator;
using TagTool.Common.Logging;
using TagTool.Common;
using System.Threading.Tasks;

namespace TagTool.Commands.Shaders
{
    public class GenerateShaderCommand : Command
    {
        public struct SDependentRenderMethodData
        {
            public CachedTag Tag;
            public object Definition;
            public ShaderFunctionHelper.AnimatedParameter[] AnimatedParameters;
            public List<string> OrderedRealParameters;
            public List<string> OrderedIntParameters;
            public List<string> OrderedBoolParameters;
            public List<string> OrderedTextures;
            public int EffectIndex;

            public static void AddDependant(List<SDependentRenderMethodData> dependentRenderMethods, GameCache cache, CachedTag dependent, RenderMethod renderMethod, RenderMethodTemplate origRmt2, int effectIndex = -1)
            {
                var animatedParams = ShaderFunctionHelper.GetAnimatedParameters(cache, renderMethod, origRmt2);

                SDependentRenderMethodData dpData = new SDependentRenderMethodData
                {
                    Tag = dependent,
                    Definition = renderMethod,
                    AnimatedParameters = animatedParams.ToArray(),
                    OrderedRealParameters = new List<string>(),
                    OrderedIntParameters = new List<string>(),
                    OrderedBoolParameters = new List<string>(),
                    OrderedTextures = new List<string>(),
                    EffectIndex = effectIndex,
                };

                foreach (var realP in origRmt2.RealParameterNames)
                    dpData.OrderedRealParameters.Add(cache.StringTable.GetString(realP.Name));
                foreach (var intP in origRmt2.IntegerParameterNames)
                    dpData.OrderedIntParameters.Add(cache.StringTable.GetString(intP.Name));
                foreach (var boolP in origRmt2.BooleanParameterNames)
                    dpData.OrderedBoolParameters.Add(cache.StringTable.GetString(boolP.Name));
                foreach (var textureP in origRmt2.TextureParameterNames)
                    dpData.OrderedTextures.Add(cache.StringTable.GetString(textureP.Name));

                dependentRenderMethods.Add(dpData);
            }
        }

        GameCache Cache;

        public GenerateShaderCommand(GameCache cache) :
            base(true,

                "GenerateShader",
                "Generates a shader template",

                "GenerateShader <shader type> <options>",

                "Generates a shader template\n" +
                "<shader type> - Specify shader type, EX. \"shader\" for \'rmsh\'.\n" +
                "Use \"explicit\" for explicit shaders (ps+vs), \"chud\" for chud (ps+vs), and \"glvs\" or \"glps\" for global shaders.\n" +
                "<options> - Specify the template\'s options as either integers or by names.\n" +
                "For explicit shaders, you should specify the name or the rasg shader index.")
        {
            Cache = cache;
        }

        public override object Execute(List<string> args)
        {
            if (args.Count < 2)
                return new TagToolError(CommandError.ArgCount);

            string shaderType = args[0].ToLower();

            if (shaderType == "explicit")
                return GenerateExplicitShader(args[1].ToLower(), args.Count > 2 ? args[2].ToLower() : "default", args.Count > 3 ? args[3].ToLower() : "");
            else if (shaderType == "chud")
                return GenerateChudShader(args[1].ToLower());
            else if (shaderType == "glvs" || shaderType == "glps")
                return GenerateGlobalShader(args[1].ToLower(), shaderType == "glps");

            args.RemoveAt(0); // we should only have options from this point

            using (var stream = Cache.OpenCacheReadWrite())
            {
                // get relevant rmdf
                if (!Cache.TagCache.TryGetTag($"shaders\\{shaderType}.rmdf", out CachedTag rmdfTag))
                {
                    return new TagToolError(CommandError.TagInvalid, $"Could not find rmdf tag for \"{shaderType}\"");
                }

                var rmdf = Cache.Deserialize<RenderMethodDefinition>(stream, rmdfTag);

                if (rmdf.GlobalVertexShader == null || rmdf.GlobalPixelShader == null)
                    return new TagToolError(CommandError.TagInvalid, "A global shader was missing from rmdf");

                List<byte> options = new List<byte>();
                for (int i = 0; i < args.Count; i++)
                {
                    // parse options as int, if fails try finding in rmdf string
                    if (!byte.TryParse(args[i].ToLower(), out byte optionInteger))
                    {
                        bool found = false;

                        for (byte j = 0; j < rmdf.Categories[i].ShaderOptions.Count; j++)
                        {
                            if (Cache.StringTable.GetString(rmdf.Categories[i].ShaderOptions[j].Name) == args[i].ToLower())
                            {
                                found = true;
                                options.Add(j);
                            }
                        }

                        if (!found)
                            return new TagToolError(CommandError.ArgInvalid, $"Shader option \"{args[i]}\" not found");
                    }

                    else
                    {
                        options.Add(optionInteger);
                    }
                }

                // make up options count, may not work very well
                while (options.Count != rmdf.Categories.Count)
                    options.Add(0);

                GenerateRenderMethodTemplate(Cache, stream, shaderType, options.ToArray(), rmdf);
            }

            return true;
        }

        public static HaloShaderGenerator.Generator.IShaderGenerator GetShaderGenerator(string shaderType, byte[] options, bool applyFixes = false)
        {
            switch (shaderType)
            {
                case "beam":            return new HaloShaderGenerator.Beam.BeamGenerator(options, applyFixes);
                case "black":           return new HaloShaderGenerator.Black.ShaderBlackGenerator();
                case "contrail":        return new HaloShaderGenerator.Contrail.ContrailGenerator(options, applyFixes);
                case "cortana":         return new HaloShaderGenerator.Cortana.CortanaGenerator(options, applyFixes);
                case "custom":          return new HaloShaderGenerator.Custom.CustomGenerator(options, applyFixes);
                case "decal":           return new HaloShaderGenerator.Decal.DecalGenerator(options, applyFixes);
                case "foliage":         return new HaloShaderGenerator.Foliage.FoliageGenerator(options, applyFixes);
                //case "glass":           return new HaloShaderGenerator.Glass.GlassGenerator(options, applyFixes);
                case "halogram":        return new HaloShaderGenerator.Halogram.HalogramGenerator(options, applyFixes);
                case "light_volume":    return new HaloShaderGenerator.LightVolume.LightVolumeGenerator(options, applyFixes);
                case "particle":        return new HaloShaderGenerator.Particle.ParticleGenerator(options, applyFixes);
                case "screen":          return new HaloShaderGenerator.Screen.ScreenGenerator(options, applyFixes);
                case "shader":          return new HaloShaderGenerator.Shader.ShaderGenerator(options, applyFixes);
                case "terrain":         return new HaloShaderGenerator.Terrain.TerrainGenerator(options, applyFixes);
                case "water":           return new HaloShaderGenerator.Water.WaterGenerator(options, applyFixes);
                case "zonly":           return new HaloShaderGenerator.ZOnly.ZOnlyGenerator(options, applyFixes);
            }
            return null;
        }

        public static HaloShaderGenerator.Generator.IShaderGenerator GetGlobalShaderGenerator(string shaderType, bool applyFixes = false)
        {
            switch (shaderType)
            {
                case "beam":            return new HaloShaderGenerator.Beam.BeamGenerator(applyFixes);
                case "black":           return new HaloShaderGenerator.Black.ShaderBlackGenerator();
                case "contrail":        return new HaloShaderGenerator.Contrail.ContrailGenerator(applyFixes);
                case "cortana":         return new HaloShaderGenerator.Cortana.CortanaGenerator(applyFixes);
                case "custom":          return new HaloShaderGenerator.Custom.CustomGenerator(applyFixes);
                case "decal":           return new HaloShaderGenerator.Decal.DecalGenerator(applyFixes);
                case "foliage":         return new HaloShaderGenerator.Foliage.FoliageGenerator(applyFixes);
                //case "glass":           return new HaloShaderGenerator.Glass.GlassGenerator(applyFixes);
                case "halogram":        return new HaloShaderGenerator.Halogram.HalogramGenerator(applyFixes);
                case "light_volume":    return new HaloShaderGenerator.LightVolume.LightVolumeGenerator(applyFixes);
                case "particle":        return new HaloShaderGenerator.Particle.ParticleGenerator(applyFixes);
                case "screen":          return new HaloShaderGenerator.Screen.ScreenGenerator(applyFixes);
                case "shader":          return new HaloShaderGenerator.Shader.ShaderGenerator(applyFixes);
                case "terrain":         return new HaloShaderGenerator.Terrain.TerrainGenerator(applyFixes);
                case "water":           return new HaloShaderGenerator.Water.WaterGenerator(applyFixes);
                case "zonly":           return new HaloShaderGenerator.ZOnly.ZOnlyGenerator(applyFixes);
            }
            return null;
        }

        public ShaderConstantTable BuildConstantTable(HaloShaderGenerator.ShaderGeneratorResult generatorResult, GameCache cache, bool pixelShader)
        {
            ShaderConstantTable result = new ShaderConstantTable
            {
                ShaderType = pixelShader ? ShaderType.PixelShader : ShaderType.VertexShader,
                Constants = new List<ShaderParameter>()
            };

            foreach (var register in generatorResult.Registers)
            {
                StringId nameId = cache.StringTable.GetOrAddString(register.Name);

                ShaderParameter.RType rType = (ShaderParameter.RType)Enum.Parse(typeof(ShaderParameter.RType), register.registerType.ToString());

                var parameterBlock = new ShaderParameter { ParameterName = nameId, RegisterCount = (byte)register.Size, RegisterIndex = (ushort)register.Register, RegisterType = rType };
                result.Constants.Add(parameterBlock);
            }

            return result;
        }

        private object GenerateExplicitShader(string shader, string entry, string vertexType)
        {
            if (!Enum.TryParse(shader, out ExplicitShader value))
            {
                if (!int.TryParse(shader, out int intValue))
                    return new TagToolError(CommandError.ArgInvalid);
                value = (ExplicitShader)intValue;
            }

            using (var stream = Cache.OpenCacheReadWrite())
            {
                var rasg = Cache.Deserialize<RasterizerGlobals>(stream, Cache.TagCache.GetTag("globals\\rasterizer_globals.rasterizer_globals"));

                CachedTag pixlTag;
                if (rasg.DefaultShaders[(int)value].PixelShader != null)
                    pixlTag = Cache.TagCache.GetTag(rasg.DefaultShaders[(int)value].PixelShader.Index);
                else
                    pixlTag = Cache.TagCache.AllocateTag<PixelShader>($"rasterizer\\shaders\\{value}");

                CachedTag vtshTag;
                if (rasg.DefaultShaders[(int)value].VertexShader != null)
                    vtshTag = Cache.TagCache.GetTag(rasg.DefaultShaders[(int)value].VertexShader.Index);
                else
                    vtshTag = Cache.TagCache.AllocateTag<VertexShader>($"rasterizer\\shaders\\{value}");

                ShaderGeneratorNew.GenerateExplicitShader(Cache, stream, value.ToString(), out PixelShader pixl, out VertexShader vtsh);

                Cache.Serialize(stream, vtshTag, vtsh);
                Cache.Serialize(stream, pixlTag, pixl);
            }

            Console.WriteLine($"Generated explicit shaders for \"{value}\"");
            return true;
        }

        private object GenerateChudShader(string shader)
        {
            if (shader == "chud_overlay_blend")
            {
                Log.Warning("chud_overlay_blend is not a chud shader - compile as explicit");
                return true;
            }

            if (!Enum.TryParse(shader, out HaloShaderGenerator.Globals.ChudShader value))
            {
                if (!int.TryParse(shader, out int intValue))
                    return new TagToolError(CommandError.ArgInvalid);
                value = (HaloShaderGenerator.Globals.ChudShader)intValue;
            }

            using (var stream = Cache.OpenCacheReadWrite())
            {
                var matg = Cache.Deserialize<Globals>(stream, Cache.TagCache.FindFirstInGroup("matg"));
                var chgd = Cache.Deserialize<ChudGlobalsDefinition>(stream, matg.InterfaceTags[0].HudGlobals);

                CachedTag pixlTag;
                if (chgd.HudShaders[(int)value].PixelShader != null)
                    pixlTag = Cache.TagCache.GetTag(chgd.HudShaders[(int)value].PixelShader.Index);
                else
                    pixlTag = Cache.TagCache.AllocateTag<PixelShader>($"rasterizer\\shaders\\{value}");

                CachedTag vtshTag;
                if (chgd.HudShaders[(int)value].VertexShader != null)
                    vtshTag = Cache.TagCache.GetTag(chgd.HudShaders[(int)value].VertexShader.Index);
                else
                    vtshTag = Cache.TagCache.AllocateTag<VertexShader>($"rasterizer\\shaders\\{value}");

                ShaderGeneratorNew.GenerateChudShader(Cache, stream, value.ToString(), out PixelShader pixl, out VertexShader vtsh);

                Cache.Serialize(stream, vtshTag, vtsh);
                Cache.Serialize(stream, pixlTag, pixl);
            }

            Console.WriteLine($"Generated chud shader for {value}");
            return true;
        }

        private object GenerateGlobalShader(string shaderType, bool pixel)
        {
            var type = (HaloShaderGenerator.Globals.ShaderType)Enum.Parse(typeof(HaloShaderGenerator.Globals.ShaderType), shaderType, true);

            using (var stream = Cache.OpenCacheReadWrite())
            {
                CachedTag rmdfTag = Cache.TagCache.GetTag($"shaders\\{shaderType}.rmdf");
                RenderMethodDefinition rmdf = Cache.Deserialize<RenderMethodDefinition>(stream, rmdfTag);

                // recompile the global shaders
                GlobalVertexShader glvs;
                GlobalPixelShader glps;
                if (pixel)
                {
                    glvs = Cache.Deserialize<GlobalVertexShader>(stream, rmdf.GlobalVertexShader);
                    glps = ShaderGeneratorNew.GenerateSharedPixelShaders(Cache, rmdf, type);
                    CachedTag glpsTag = Cache.TagCache.GetTag(rmdf.GlobalPixelShader.Index);
                    Cache.Serialize(stream, glpsTag, glps);
                }
                else
                {
                    glvs = ShaderGeneratorNew.GenerateSharedVertexShaders(Cache, rmdf, type);
                    glps = Cache.Deserialize<GlobalPixelShader>(stream, rmdf.GlobalPixelShader);
                    CachedTag glvsTag = Cache.TagCache.GetTag(rmdf.GlobalVertexShader.Index);
                    Cache.Serialize(stream, glvsTag, glvs);
                }
                Console.WriteLine($"Generated global {(pixel ? "pixel" : "vertex")} shader for {shaderType}");

                // recompile templates
                if (type == HaloShaderGenerator.Globals.ShaderType.Decal ||
                    type == HaloShaderGenerator.Globals.ShaderType.Water ||
                    type == HaloShaderGenerator.Globals.ShaderType.Particle ||
                    type == HaloShaderGenerator.Globals.ShaderType.LightVolume ||
                    type == HaloShaderGenerator.Globals.ShaderType.Beam ||
                    type == HaloShaderGenerator.Globals.ShaderType.Contrail)
                {
                    RecompileTemplates(Cache, stream, shaderType, rmdf, glvs, glps);
                }

                Cache.SaveStrings();
                (Cache as GameCacheHaloOnlineBase).SaveTagNames();
            }

            return true;
        }

        public static void GenerateRenderMethodTemplate(GameCache cache, Stream stream, string shaderType, byte[] options, RenderMethodDefinition rmdf, bool suppressCli = false)
        {
            string rmt2Name = $"shaders\\{shaderType}_templates\\_{string.Join("_", options)}";

            // Collect dependent render methods, store arguments

            List<SDependentRenderMethodData> dependentRenderMethods = new List<SDependentRenderMethodData>();

            if (cache.TagCache.TryGetTag(rmt2Name + ".rmt2", out var rmt2Tag))
            {
                var origRmt2 = cache.Deserialize<RenderMethodTemplate>(stream, rmt2Tag);
                var dependents = (cache as GameCacheHaloOnlineBase).TagCacheGenHO.NonNull().Where(t => ((Cache.HaloOnline.CachedTagHaloOnline)t).Dependencies.Contains(rmt2Tag.Index));

                foreach (var dependent in dependents)
                {
                    object definition = null;
                    
                    if (dependent.IsInGroup("rm  "))
                    { 
                        definition = cache.Deserialize(stream, dependent);
                    }
                    else
                    {
                        switch (dependent.Group.Tag.ToString())
                        {
                            case "prt3":
                                var prt3 = cache.Deserialize<Particle>(stream, dependent);
                                definition = prt3.RenderMethod;
                                break;
                            case "decs":
                                var decs = cache.Deserialize<DecalSystem>(stream, dependent);
                                for (int i = 0; i < decs.Decal.Count; i++)
                                    if (decs.Decal[i].RenderMethod.ShaderProperties[0].Template.Name == rmt2Name)
                                        SDependentRenderMethodData.AddDependant(dependentRenderMethods, cache, dependent, decs.Decal[i].RenderMethod, origRmt2, i);
                                continue;
                            case "beam":
                                var beam = cache.Deserialize<BeamSystem>(stream, dependent);
                                for (int i = 0; i < beam.Beams.Count; i++)
                                    if (beam.Beams[i].RenderMethod.ShaderProperties[0].Template.Name == rmt2Name)
                                        SDependentRenderMethodData.AddDependant(dependentRenderMethods, cache, dependent, beam.Beams[i].RenderMethod, origRmt2, i);
                                continue;
                            case "ltvl":
                                var ltvl = cache.Deserialize<LightVolumeSystem>(stream, dependent);
                                for (int i = 0; i < ltvl.LightVolumes.Count; i++)
                                    if (ltvl.LightVolumes[i].RenderMethod.ShaderProperties[0].Template.Name == rmt2Name)
                                        SDependentRenderMethodData.AddDependant(dependentRenderMethods, cache, dependent, ltvl.LightVolumes[i].RenderMethod, origRmt2, i);
                                continue;
                            case "cntl":
                                var cntl = cache.Deserialize<ContrailSystem>(stream, dependent);
                                for (int i = 0; i < cntl.Contrails.Count; i++)
                                    if (cntl.Contrails[i].RenderMethod.ShaderProperties[0].Template.Name == rmt2Name)
                                        SDependentRenderMethodData.AddDependant(dependentRenderMethods, cache, dependent, cntl.Contrails[i].RenderMethod, origRmt2, i);
                                continue;
                        }
                    }

                    SDependentRenderMethodData.AddDependant(dependentRenderMethods, cache, dependent, (RenderMethod)definition, origRmt2);
                }
            }

            // Generate template

            //var generator = GetShaderGenerator(shaderType, options, true);

            var glps = cache.Deserialize<GlobalPixelShader>(stream, rmdf.GlobalPixelShader);
            var glvs = cache.Deserialize<GlobalVertexShader>(stream, rmdf.GlobalVertexShader);

            //var rmt2 = TagTool.Shaders.ShaderGenerator.ShaderGenerator.GenerateRenderMethodTemplate(cache, stream, rmdf, glps, glvs, generator, rmt2Name);

            var rmt2 = TagTool.Shaders.ShaderGenerator.ShaderGeneratorNew.GenerateTemplateSafe(cache, stream, rmdf, rmt2Name, out _, out _);
            //TagTool.Shaders.ShaderGenerator.ShaderGeneratorNew.VerifyRmt2Routing(cache, stream, rmt2, rmdf, options.ToList());

            if (rmt2Tag == null)
                rmt2Tag = cache.TagCache.AllocateTag<RenderMethodTemplate>(rmt2Name);

            cache.Serialize(stream, rmt2Tag, rmt2);
            cache.SaveStrings();
            (cache as GameCacheHaloOnlineBase).SaveTagNames();

            if (!suppressCli)
                Console.WriteLine($"Generated shader template \"{rmt2Name}\"");

            // Fixup render method parameters

            foreach (var dependent in dependentRenderMethods)
            {
                var postprocess = (dependent.Definition as RenderMethod).ShaderProperties[0];

                List<TextureConstant> reorderedTextureConstants = new List<TextureConstant>();
                foreach (var textureName in rmt2.TextureParameterNames)
                {
                    int origIndex = dependent.OrderedTextures.IndexOf(cache.StringTable.GetString(textureName.Name));
                    if (origIndex != -1)
                        reorderedTextureConstants.Add(postprocess.TextureConstants[origIndex]);
                    else
                        reorderedTextureConstants.Add(new TextureConstant());
                }
                postprocess.TextureConstants = reorderedTextureConstants;

                List<RealConstant> reorderedRealConstants = new List<RealConstant>();
                foreach (var realName in rmt2.RealParameterNames)
                {
                    int origIndex = dependent.OrderedRealParameters.IndexOf(cache.StringTable.GetString(realName.Name));
                    if (origIndex != -1)
                        reorderedRealConstants.Add(postprocess.RealConstants[origIndex]);
                    else
                        reorderedRealConstants.Add(new RealConstant());
                }
                postprocess.RealConstants = reorderedRealConstants;

                List<uint> reorderedIntConstants = new List<uint>();
                foreach (var intName in rmt2.IntegerParameterNames)
                {
                    int origIndex = dependent.OrderedIntParameters.IndexOf(cache.StringTable.GetString(intName.Name));
                    if (origIndex != -1)
                        reorderedIntConstants.Add(postprocess.IntegerConstants[origIndex]);
                    else
                        reorderedIntConstants.Add(new uint());
                }
                postprocess.IntegerConstants = reorderedIntConstants;

                uint reorderedBoolConstants = 0;
                for (int i = 0; i < rmt2.BooleanParameterNames.Count; i++)
                {
                    int origIndex = dependent.OrderedBoolParameters.IndexOf(cache.StringTable.GetString(rmt2.BooleanParameterNames[i].Name));
                    if (origIndex != -1)
                        reorderedBoolConstants |= ((postprocess.BooleanConstants >> origIndex) & 1) == 1 ? 1u << i : 0;
                }
                postprocess.BooleanConstants = reorderedBoolConstants;

                var textureAnimatedParams = dependent.AnimatedParameters.Where(x => x.Type == ShaderFunctionHelper.ParameterType.Texture);
                var realAnimatedParams = dependent.AnimatedParameters.Where(x => x.Type == ShaderFunctionHelper.ParameterType.Real);
                var intAnimatedParams = dependent.AnimatedParameters.Where(x => x.Type == ShaderFunctionHelper.ParameterType.Int);
                var boolAnimatedParams = dependent.AnimatedParameters.Where(x => x.Type == ShaderFunctionHelper.ParameterType.Bool);

                postprocess.RoutingInfo.Clear();
                var routingInfo = postprocess.RoutingInfo;

                foreach (var pass in postprocess.Passes)
                {
                    pass.RealPixel.Integer = 0;
                    pass.RealVertex.Integer = 0;
                    pass.Texture.Integer = 0;
                }

                for (int k = 0; k < postprocess.EntryPoints.Count; k++)
                {
                    var entry = postprocess.EntryPoints[k];
                    var rmt2Entry = rmt2.EntryPoints[k];

                    for (int i = entry.Offset; i < entry.Offset + entry.Count; i++)
                    {
                        var pass = postprocess.Passes[i];
                        var rmt2Pass = rmt2.Passes[rmt2Entry.Offset + (i - entry.Offset)];

                        // Texture

                        int usageOffset = rmt2Pass.Values[(int)ParameterUsage.Texture].Offset;
                        int usageCount = rmt2Pass.Values[(int)ParameterUsage.Texture].Count;

                        pass.Texture.Offset = (ushort)routingInfo.Count;
                        for (int j = usageOffset; j < usageOffset + usageCount; j++)
                        {
                            var rmt2RoutingInfo = rmt2.RoutingInfo[j];

                            string paramName = cache.StringTable.GetString(rmt2.TextureParameterNames[rmt2RoutingInfo.SourceIndex].Name);

                            foreach (var animatedParam in textureAnimatedParams)
                            {
                                if (animatedParam.Name == paramName)
                                {
                                    var newBlock = new RenderMethodRoutingInfoBlock 
                                    { 
                                        SourceIndex = rmt2RoutingInfo.SourceIndex, 
                                        FunctionIndex = (byte)animatedParam.FunctionIndex,
                                        RegisterIndex = (short)rmt2RoutingInfo.DestinationIndex
                                    };

                                    routingInfo.Add(newBlock);
                                    pass.Texture.Count++;
                                    break;
                                }
                            }
                        }
                        pass.Texture.Offset = pass.Texture.Count == 0 ? (ushort)0 : pass.Texture.Offset;

                        // Real PS

                        usageOffset = rmt2Pass.Values[(int)ParameterUsage.PS_Real].Offset;
                        usageCount = rmt2Pass.Values[(int)ParameterUsage.PS_Real].Count;

                        pass.RealPixel.Offset = (ushort)routingInfo.Count;
                        for (int j = usageOffset; j < usageOffset + usageCount; j++)
                        {
                            var rmt2RoutingInfo = rmt2.RoutingInfo[j];

                            string paramName = cache.StringTable.GetString(rmt2.RealParameterNames[rmt2RoutingInfo.SourceIndex].Name);

                            foreach (var animatedParam in realAnimatedParams)
                            {
                                if (animatedParam.Name == paramName)
                                {
                                    var newBlock = new RenderMethodRoutingInfoBlock
                                    {
                                        SourceIndex = rmt2RoutingInfo.SourceIndex,
                                        FunctionIndex = (byte)animatedParam.FunctionIndex,
                                        RegisterIndex = (short)rmt2RoutingInfo.DestinationIndex
                                    };

                                    routingInfo.Add(newBlock);
                                    pass.RealPixel.Count++;
                                    break;
                                }
                            }
                        }
                        pass.RealPixel.Offset = pass.RealPixel.Count == 0 ? (ushort)0 : pass.RealPixel.Offset;

                        // Real VS

                        usageOffset = rmt2Pass.Values[(int)ParameterUsage.VS_Real].Offset;
                        usageCount = rmt2Pass.Values[(int)ParameterUsage.VS_Real].Count;

                        pass.RealVertex.Offset = (ushort)routingInfo.Count;
                        for (int j = usageOffset; j < usageOffset + usageCount; j++)
                        {
                            var rmt2RoutingInfo = rmt2.RoutingInfo[j];

                            string paramName = cache.StringTable.GetString(rmt2.RealParameterNames[rmt2RoutingInfo.SourceIndex].Name);

                            foreach (var animatedParam in realAnimatedParams)
                            {
                                if (animatedParam.Name == paramName)
                                {
                                    var newBlock = new RenderMethodRoutingInfoBlock
                                    {
                                        SourceIndex = rmt2RoutingInfo.SourceIndex,
                                        FunctionIndex = (byte)animatedParam.FunctionIndex,
                                        RegisterIndex = (short)rmt2RoutingInfo.DestinationIndex
                                    };

                                    routingInfo.Add(newBlock);
                                    pass.RealVertex.Count++;
                                    break;
                                }
                            }
                        }
                        pass.RealVertex.Offset = pass.RealVertex.Count == 0 ? (ushort)0 : pass.RealVertex.Offset;
                    }
                }

                if (dependent.Tag.IsInGroup("rm  "))
                {
                    cache.Serialize(stream, dependent.Tag, dependent.Definition);
                }
                else
                {
                    switch (dependent.Tag.Group.Tag.ToString())
                    {
                        case "prt3":
                            var prt3 = cache.Deserialize<Particle>(stream, dependent.Tag);
                            prt3.RenderMethod = (RenderMethod)dependent.Definition;
                            cache.Serialize(stream, dependent.Tag, prt3);
                            break;
                        case "decs":
                            var decs = cache.Deserialize<DecalSystem>(stream, dependent.Tag);
                            decs.Decal[dependent.EffectIndex].RenderMethod = (RenderMethod)dependent.Definition;
                            cache.Serialize(stream, dependent.Tag, decs);
                            break;
                        case "beam":
                            var beam = cache.Deserialize<BeamSystem>(stream, dependent.Tag);
                            beam.Beams[dependent.EffectIndex].RenderMethod = (RenderMethod)dependent.Definition;
                            cache.Serialize(stream, dependent.Tag, beam);
                            break;
                        case "ltvl":
                            var ltvl = cache.Deserialize<LightVolumeSystem>(stream, dependent.Tag);
                            ltvl.LightVolumes[dependent.EffectIndex].RenderMethod = (RenderMethod)dependent.Definition;
                            cache.Serialize(stream, dependent.Tag, ltvl);
                            break;
                        case "cntl":
                            var cntl = cache.Deserialize<ContrailSystem>(stream, dependent.Tag);
                            cntl.Contrails[dependent.EffectIndex].RenderMethod = (RenderMethod)dependent.Definition;
                            cache.Serialize(stream, dependent.Tag, cntl);
                            break;
                    }
                }
            }

            if (dependentRenderMethods.Count > 0 && !suppressCli)
                Console.WriteLine($"Corrected {dependentRenderMethods.Count} render method{(dependentRenderMethods.Count > 1 ? "s" : "")}");
        }

        // ASYNC -------------------------------

        public struct STemplateRecompileInfo
        {
            // init
            public string Name;
            public string ShaderType;
            public byte[] Options;
            public CachedTag Tag;
            public List<SDependentRenderMethodData> Dependants;
            public List<RenderMethodOption.ParameterBlock> AllRmopParameters;
            // post
            public PixelShader PixelShader;
            public VertexShader VertexShader;
            public RenderMethodTemplate Template;
        }

        public struct SExplicitRecompileInfo
        {
            public CachedTag PixelTag;
            public CachedTag VertexTag;
            public bool IsChud;
            public PixelShader PixelShader;
            public VertexShader VertexShader;
            public string ExplicitName;
        }

        /// <summary>
        /// For async recompile
        /// </summary>
        public static List<SDependentRenderMethodData> GetDependantsAsync(GameCache cache,
            Stream stream,
            string shaderType,
            byte[] options)
        {
            string rmt2Name = $"shaders\\{shaderType}_templates\\_{string.Join("_", options)}";

            // Collect dependent render methods, store arguments

            List<SDependentRenderMethodData> dependentRenderMethods = new List<SDependentRenderMethodData>();

            if (cache.TagCache.TryGetTag(rmt2Name + ".rmt2", out var rmt2Tag))
            {
                RenderMethodTemplate originalRmt2 = cache.Deserialize<RenderMethodTemplate>(stream, rmt2Tag);
                var dependents = (cache as GameCacheHaloOnlineBase).TagCacheGenHO.NonNull().Where(t => ((Cache.HaloOnline.CachedTagHaloOnline)t).Dependencies.Contains(rmt2Tag.Index));

                foreach (var dependent in dependents)
                {
                    object definition = null;

                    if (dependent.IsInGroup("rm  "))
                    {
                        definition = cache.Deserialize(stream, dependent);
                    }
                    else
                    {
                        switch (dependent.Group.Tag.ToString())
                        {
                            case "prt3":
                                var prt3 = cache.Deserialize<Particle>(stream, dependent);
                                definition = prt3.RenderMethod;
                                break;
                            case "decs":
                                var decs = cache.Deserialize<DecalSystem>(stream, dependent);
                                for (int i = 0; i < decs.Decal.Count; i++)
                                    if (decs.Decal[i].RenderMethod.ShaderProperties[0].Template.Name == rmt2Name)
                                        SDependentRenderMethodData.AddDependant(dependentRenderMethods, cache, dependent, decs.Decal[i].RenderMethod, originalRmt2, i);
                                continue;
                            case "beam":
                                var beam = cache.Deserialize<BeamSystem>(stream, dependent);
                                for (int i = 0; i < beam.Beams.Count; i++)
                                    if (beam.Beams[i].RenderMethod.ShaderProperties[0].Template.Name == rmt2Name)
                                        SDependentRenderMethodData.AddDependant(dependentRenderMethods, cache, dependent, beam.Beams[i].RenderMethod, originalRmt2, i);
                                continue;
                            case "ltvl":
                                var ltvl = cache.Deserialize<LightVolumeSystem>(stream, dependent);
                                for (int i = 0; i < ltvl.LightVolumes.Count; i++)
                                    if (ltvl.LightVolumes[i].RenderMethod.ShaderProperties[0].Template.Name == rmt2Name)
                                        SDependentRenderMethodData.AddDependant(dependentRenderMethods, cache, dependent, ltvl.LightVolumes[i].RenderMethod, originalRmt2, i);
                                continue;
                            case "cntl":
                                var cntl = cache.Deserialize<ContrailSystem>(stream, dependent);
                                for (int i = 0; i < cntl.Contrails.Count; i++)
                                    if (cntl.Contrails[i].RenderMethod.ShaderProperties[0].Template.Name == rmt2Name)
                                        SDependentRenderMethodData.AddDependant(dependentRenderMethods, cache, dependent, cntl.Contrails[i].RenderMethod, originalRmt2, i);
                                continue;
                        }
                    }

                    SDependentRenderMethodData.AddDependant(dependentRenderMethods, cache, dependent, (RenderMethod)definition, originalRmt2);
                }
            }

            return dependentRenderMethods;
        }

        /// <summary>
        /// For async recompile
        /// </summary>
        public static void ReserializeDependantsAsync(GameCache cache,
            Stream stream,
            RenderMethodTemplate rmt2,
            List<SDependentRenderMethodData> dependentRenderMethods)
        {
            // Fixup render method parameters

            foreach (var dependent in dependentRenderMethods)
            {
                var postprocess = (dependent.Definition as RenderMethod).ShaderProperties[0];

                List<TextureConstant> reorderedTextureConstants = new List<TextureConstant>();
                foreach (var textureName in rmt2.TextureParameterNames)
                {
                    int origIndex = dependent.OrderedTextures.IndexOf(cache.StringTable.GetString(textureName.Name));
                    if (origIndex != -1)
                        reorderedTextureConstants.Add(postprocess.TextureConstants[origIndex]);
                    else
                        reorderedTextureConstants.Add(new TextureConstant());
                }
                postprocess.TextureConstants = reorderedTextureConstants;

                List<RealConstant> reorderedRealConstants = new List<RealConstant>();
                foreach (var realName in rmt2.RealParameterNames)
                {
                    int origIndex = dependent.OrderedRealParameters.IndexOf(cache.StringTable.GetString(realName.Name));
                    if (origIndex != -1)
                        reorderedRealConstants.Add(postprocess.RealConstants[origIndex]);
                    else
                        reorderedRealConstants.Add(new RealConstant());
                }
                postprocess.RealConstants = reorderedRealConstants;

                List<uint> reorderedIntConstants = new List<uint>();
                foreach (var intName in rmt2.IntegerParameterNames)
                {
                    int origIndex = dependent.OrderedIntParameters.IndexOf(cache.StringTable.GetString(intName.Name));
                    if (origIndex != -1)
                        reorderedIntConstants.Add(postprocess.IntegerConstants[origIndex]);
                    else
                        reorderedIntConstants.Add(new uint());
                }
                postprocess.IntegerConstants = reorderedIntConstants;

                uint reorderedBoolConstants = 0;
                for (int i = 0; i < rmt2.BooleanParameterNames.Count; i++)
                {
                    int origIndex = dependent.OrderedBoolParameters.IndexOf(cache.StringTable.GetString(rmt2.BooleanParameterNames[i].Name));
                    if (origIndex != -1)
                        reorderedBoolConstants |= ((postprocess.BooleanConstants >> origIndex) & 1) == 1 ? 1u << i : 0;
                }
                postprocess.BooleanConstants = reorderedBoolConstants;

                var textureAnimatedParams = dependent.AnimatedParameters.Where(x => x.Type == ShaderFunctionHelper.ParameterType.Texture);
                var realAnimatedParams = dependent.AnimatedParameters.Where(x => x.Type == ShaderFunctionHelper.ParameterType.Real);
                var intAnimatedParams = dependent.AnimatedParameters.Where(x => x.Type == ShaderFunctionHelper.ParameterType.Int);
                var boolAnimatedParams = dependent.AnimatedParameters.Where(x => x.Type == ShaderFunctionHelper.ParameterType.Bool);

                postprocess.RoutingInfo.Clear();
                var routingInfo = postprocess.RoutingInfo;

                foreach (var pass in postprocess.Passes)
                {
                    pass.RealPixel.Integer = 0;
                    pass.RealVertex.Integer = 0;
                    pass.Texture.Integer = 0;
                }

                for (int k = 0; k < postprocess.EntryPoints.Count; k++)
                {
                    var entry = postprocess.EntryPoints[k];
                    var rmt2Entry = rmt2.EntryPoints[k];

                    for (int i = entry.Offset; i < entry.Offset + entry.Count; i++)
                    {
                        var pass = postprocess.Passes[i];
                        var rmt2Pass = rmt2.Passes[rmt2Entry.Offset + (i - entry.Offset)];

                        // Texture

                        int usageOffset = rmt2Pass.Values[(int)ParameterUsage.Texture].Offset;
                        int usageCount = rmt2Pass.Values[(int)ParameterUsage.Texture].Count;

                        pass.Texture.Offset = (ushort)routingInfo.Count;
                        for (int j = usageOffset; j < usageOffset + usageCount; j++)
                        {
                            var rmt2RoutingInfo = rmt2.RoutingInfo[j];

                            string paramName = cache.StringTable.GetString(rmt2.TextureParameterNames[rmt2RoutingInfo.SourceIndex].Name);

                            foreach (var animatedParam in textureAnimatedParams)
                            {
                                if (animatedParam.Name == paramName)
                                {
                                    var newBlock = new RenderMethodRoutingInfoBlock
                                    {
                                        SourceIndex = rmt2RoutingInfo.SourceIndex,
                                        FunctionIndex = (byte)animatedParam.FunctionIndex,
                                        RegisterIndex = (short)rmt2RoutingInfo.DestinationIndex
                                    };

                                    routingInfo.Add(newBlock);
                                    pass.Texture.Count++;
                                    break;
                                }
                            }
                        }
                        pass.Texture.Offset = pass.Texture.Count == 0 ? (ushort)0 : pass.Texture.Offset;

                        // Real PS

                        usageOffset = rmt2Pass.Values[(int)ParameterUsage.PS_Real].Offset;
                        usageCount = rmt2Pass.Values[(int)ParameterUsage.PS_Real].Count;

                        pass.RealPixel.Offset = (ushort)routingInfo.Count;
                        for (int j = usageOffset; j < usageOffset + usageCount; j++)
                        {
                            var rmt2RoutingInfo = rmt2.RoutingInfo[j];

                            string paramName = cache.StringTable.GetString(rmt2.RealParameterNames[rmt2RoutingInfo.SourceIndex].Name);

                            foreach (var animatedParam in realAnimatedParams)
                            {
                                if (animatedParam.Name == paramName)
                                {
                                    var newBlock = new RenderMethodRoutingInfoBlock
                                    {
                                        SourceIndex = rmt2RoutingInfo.SourceIndex,
                                        FunctionIndex = (byte)animatedParam.FunctionIndex,
                                        RegisterIndex = (short)rmt2RoutingInfo.DestinationIndex
                                    };

                                    routingInfo.Add(newBlock);
                                    pass.RealPixel.Count++;
                                    break;
                                }
                            }
                        }
                        pass.RealPixel.Offset = pass.RealPixel.Count == 0 ? (ushort)0 : pass.RealPixel.Offset;

                        // Real VS

                        usageOffset = rmt2Pass.Values[(int)ParameterUsage.VS_Real].Offset;
                        usageCount = rmt2Pass.Values[(int)ParameterUsage.VS_Real].Count;

                        pass.RealVertex.Offset = (ushort)routingInfo.Count;
                        for (int j = usageOffset; j < usageOffset + usageCount; j++)
                        {
                            var rmt2RoutingInfo = rmt2.RoutingInfo[j];

                            string paramName = cache.StringTable.GetString(rmt2.RealParameterNames[rmt2RoutingInfo.SourceIndex].Name);

                            foreach (var animatedParam in realAnimatedParams)
                            {
                                if (animatedParam.Name == paramName)
                                {
                                    var newBlock = new RenderMethodRoutingInfoBlock
                                    {
                                        SourceIndex = rmt2RoutingInfo.SourceIndex,
                                        FunctionIndex = (byte)animatedParam.FunctionIndex,
                                        RegisterIndex = (short)rmt2RoutingInfo.DestinationIndex
                                    };

                                    routingInfo.Add(newBlock);
                                    pass.RealVertex.Count++;
                                    break;
                                }
                            }
                        }
                        pass.RealVertex.Offset = pass.RealVertex.Count == 0 ? (ushort)0 : pass.RealVertex.Offset;
                    }
                }

                if (dependent.Tag.IsInGroup("rm  "))
                {
                    cache.Serialize(stream, dependent.Tag, dependent.Definition);
                }
                else
                {
                    switch (dependent.Tag.Group.Tag.ToString())
                    {
                        case "prt3":
                            var prt3 = cache.Deserialize<Particle>(stream, dependent.Tag);
                            prt3.RenderMethod = (RenderMethod)dependent.Definition;
                            cache.Serialize(stream, dependent.Tag, prt3);
                            break;
                        case "decs":
                            var decs = cache.Deserialize<DecalSystem>(stream, dependent.Tag);
                            decs.Decal[dependent.EffectIndex].RenderMethod = (RenderMethod)dependent.Definition;
                            cache.Serialize(stream, dependent.Tag, decs);
                            break;
                        case "beam":
                            var beam = cache.Deserialize<BeamSystem>(stream, dependent.Tag);
                            beam.Beams[dependent.EffectIndex].RenderMethod = (RenderMethod)dependent.Definition;
                            cache.Serialize(stream, dependent.Tag, beam);
                            break;
                        case "ltvl":
                            var ltvl = cache.Deserialize<LightVolumeSystem>(stream, dependent.Tag);
                            ltvl.LightVolumes[dependent.EffectIndex].RenderMethod = (RenderMethod)dependent.Definition;
                            cache.Serialize(stream, dependent.Tag, ltvl);
                            break;
                        case "cntl":
                            var cntl = cache.Deserialize<ContrailSystem>(stream, dependent.Tag);
                            cntl.Contrails[dependent.EffectIndex].RenderMethod = (RenderMethod)dependent.Definition;
                            cache.Serialize(stream, dependent.Tag, cntl);
                            break;
                    }
                }
            }

            //if (dependentRenderMethods.Count > 0)
            //    Console.WriteLine($"Corrected {dependentRenderMethods.Count} render method{(dependentRenderMethods.Count > 1 ? "s" : "")}");
        }

        public static void RecompileTemplates(GameCache cache, Stream stream, string shaderType, RenderMethodDefinition rmdf, GlobalVertexShader glvs, GlobalPixelShader glps)
        {
            // get templates for this shader type
            List<CachedTag> regenTags = new List<CachedTag>();
            foreach (var tag in cache.TagCache.NonNull())
            {
                if (tag.Group.Tag != "rmt2" ||
                    tag.Name.StartsWith("ms30") ||
                    !tag.Name.Split('\\')[1].StartsWith(shaderType + "_templates"))
                    continue;
                regenTags.Add(tag);
            }

            // build recompile info
            List<STemplateRecompileInfo> recompileInfo = new List<STemplateRecompileInfo>();
            foreach (var tag in regenTags)
            {
                List<byte> options = new List<byte>();
                foreach (var option in tag.Name.Split('\\')[2].Remove(0, 1).Split('_'))
                    options.Add(byte.Parse(option));
                while (options.Count < rmdf.Categories.Count)
                    options.Add(0);
                var aOptions = options.ToArray();

                STemplateRecompileInfo info = new STemplateRecompileInfo
                {
                    Name = $"shaders\\{shaderType}_templates\\_{string.Join("_", aOptions)}",
                    ShaderType = shaderType,
                    Options = aOptions,
                    Tag = tag,
                    Dependants = GetDependantsAsync(cache, stream, shaderType, aOptions),
                    AllRmopParameters = ShaderGeneratorNew.GatherParameters(cache, stream, rmdf, aOptions)
                };

                recompileInfo.Add(info);
            }

            // recompile templates
            List<Task<STemplateRecompileInfo>> tasks = new List<Task<STemplateRecompileInfo>>();
            foreach (var info in recompileInfo)
            {
                Task<STemplateRecompileInfo> generatorTask = Task.Run(() => {
                    return GenerateRenderMethodTemplateAsync(cache, info, rmdf, glvs, glps);
                });
                tasks.Add(generatorTask);
            }

            float percentageComplete = 0.00f;
            Console.Write($"\rRecompiling {shaderType} templates... {string.Format("{0:0.00}", percentageComplete)}%");

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

                System.Threading.Thread.Sleep(250); // wait to prevent constant cli writes
            }

            Console.Write($"\rSuccessfully recompiled {tasks.Count} {shaderType} templates. Serializing...");

            // serialize
            foreach (var task in tasks)
            {
                if (!cache.TagCache.TryGetTag(task.Result.Name + ".pixl", out task.Result.Template.PixelShader))
                    task.Result.Template.PixelShader = cache.TagCache.AllocateTag<PixelShader>(task.Result.Name);
                if (!cache.TagCache.TryGetTag(task.Result.Name + ".vtsh", out task.Result.Template.VertexShader))
                    task.Result.Template.VertexShader = cache.TagCache.AllocateTag<VertexShader>(task.Result.Name);

                cache.Serialize(stream, task.Result.Template.PixelShader, task.Result.PixelShader);
                cache.Serialize(stream, task.Result.Template.VertexShader, task.Result.VertexShader);
                cache.Serialize(stream, task.Result.Tag, task.Result.Template);

                (cache as GameCacheHaloOnlineBase).SaveTagNames();

                ReserializeDependantsAsync(cache, stream, task.Result.Template, task.Result.Dependants);
            }

            Console.Write($"\rSuccessfully recompiled {tasks.Count} {shaderType} templates. Serializing... Done");
            Console.WriteLine();

            // validation
            foreach (var task in tasks)
            {
                var rmt2 = cache.Deserialize<RenderMethodTemplate>(stream, task.Result.Tag);
                var pixl = cache.Deserialize<PixelShader>(stream, rmt2.PixelShader);

                if (rmt2.PixelShader.Name == null || rmt2.PixelShader.Name == "")
                    Log.Warning($"pixel_shader {rmt2.PixelShader.Index:X16} has no name");

                for (int i = 0; i < pixl.EntryPointShaders.Count; i++)
                {
                    bool entryNeeded = rmdf.EntryPoints.Any(x => (int)x.EntryPoint == i) &&
                        (glps.EntryPoints[i].DefaultCompiledShaderIndex == -1 && glps.EntryPoints[i].CategoryDependency.Count == 0);

                    if (pixl.EntryPointShaders[i].Count > 0 && !entryNeeded)
                        Log.Warning($"{rmt2.PixelShader.Name} has unneeded entry point shader {(TagTool.Shaders.EntryPoint)i}");

                    if (pixl.EntryPointShaders[i].Count == 0 && entryNeeded)
                        Log.Warning($"{rmt2.PixelShader.Name} missing entry point shader {(TagTool.Shaders.EntryPoint)i}");

                    if (pixl.EntryPointShaders[i].Count > 0 && pixl.EntryPointShaders[i].Offset >= pixl.Shaders.Count)
                        Log.Warning($"{rmt2.PixelShader.Name} has invalid compiled shader indices {i}");
                }
            }
        }

        /// <summary>
        /// For async recompile.
        /// 
        /// Usage:
        ///     -> Collect all dependants for all targetted templates
        ///     -> Perform recompile on all templates and store results (this is the critical part that utilizes as much cpu as possible)
        ///     -> Serialization
        ///     -> Apply fixups for dependants
        /// </summary>
        public static STemplateRecompileInfo GenerateRenderMethodTemplateAsync(GameCache cache,
            STemplateRecompileInfo recompileInfo,
            RenderMethodDefinition rmdf,
            GlobalVertexShader glvs,
            GlobalPixelShader glps)
        {
            recompileInfo.Template = ShaderGeneratorNew.GenerateTemplate(cache, rmdf, glvs, glps, 
                recompileInfo.AllRmopParameters, recompileInfo.Name, out recompileInfo.PixelShader, out recompileInfo.VertexShader);

            return recompileInfo;
        }

        public static SExplicitRecompileInfo GenerateExplicitShaderAsync(GameCache cache, SExplicitRecompileInfo info)
        {
            Stream fakeStream = null;// unused atm

            if (info.IsChud)
            {
                ShaderGeneratorNew.GenerateChudShader(cache, fakeStream, info.ExplicitName, out info.PixelShader, out info.VertexShader);
            }
            else
            {
                ShaderGeneratorNew.GenerateExplicitShader(cache, fakeStream, info.ExplicitName, out info.PixelShader, out info.VertexShader);
            }

            return info;
        }
    }
}
