using TagTool.Cache;
using TagTool.Common;
using TagTool.Tags.Definitions;
using System.IO;
using TagTool.Shaders.ShaderMatching;
using System;
using TagTool.Shaders.ShaderConverter;
using System.Collections.Generic;
using TagTool.Shaders;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TagTool.Bitmaps.Utils;
using TagTool.Bitmaps;
using TagTool.Common.Logging;
using TagTool.Shaders.ShaderGenerator;

namespace TagTool.Porting.Gen3
{
    partial class PortingContextGen3
    {
        protected Dictionary<string, Task> PendingTemplates = new Dictionary<string, Task>();
        protected ShaderMatcher Matcher = new ShaderMatcher();

        private RasterizerGlobals ConvertRasterizerGlobals(RasterizerGlobals rasg)
        {
            if (BlamCache.Version == CacheVersion.Halo3ODST)
            {
                rasg.MotionBlurParametersLegacy.NumberOfTaps = 6;
            }
            return rasg;
        }

        private CachedTag GetDefaultShader(CachedTag tag)
        {
            CachedTag shaderTag;

            switch (tag.Group.Tag.ToString())
            {
                case "rmbk":
                    CacheContext.TagCache.TryGetTag(@"levels\dlc\bunkerworld\shaders\z_black.rmsh", out shaderTag);
                    break;
                case "rmgl":
                    CacheContext.TagCache.TryGetTag(@"levels\dlc\sidewinder\shaders\side_hall_glass03.rmsh", out shaderTag);
                    break;
                default:
                    shaderTag = GetFallbackTag(tag);
                    break;
            }

            if (shaderTag is null)
            {
                shaderTag = CacheContext.TagCache.GetTag<Shader>(@"shaders\invalid");
                Log.Warning($"No default shader found for \"{tag.Group.Tag}\", using \"shaders\\invalid.rmsh\"");
            }
            return shaderTag;
        }

        private RenderMethod ConvertRenderMethod(Stream cacheStream, Stream blamCacheStream, object definition, string blamTagName, RenderMethod renderMethod)
        {
            Matcher.Init(CacheContext, BlamCache, cacheStream, blamCacheStream, this, FlagIsSet(PortingFlags.Ms30), FlagIsSet(PortingFlags.PefectShaderMatchOnly));

            // Deserialize the source render method template.
            var blamRmt2 = TagDefinitionCache.Deserialize<RenderMethodTemplate>(BlamCache, blamCacheStream, renderMethod.ShaderProperties[0].Template);

            // Convert texture constant bitmaps first so that we can do any fixes
            if (FlagIsSet(PortingFlags.Recursive))
                ConvertRenderMethodBitmaps(cacheStream, blamCacheStream, renderMethod, blamTagName, blamRmt2);

            // Convert the render method structure, making clone of the source render method as we will need it for later.
            RenderMethod edRenderMethod = ConvertStructure(cacheStream, blamCacheStream, renderMethod.DeepCloneV2(), definition, blamTagName);

            // Try to find the template in the dest cache. If an exact match is not found the closest tag is returned instead.
            CachedTag rmt2Tag = Matcher.FindClosestTemplate(renderMethod.ShaderProperties[0].Template.Name, blamTagName, out string tagName, out bool isExactMatch);

            // Check if we should generate the template.
            PendingTemplates.TryGetValue(tagName, out Task templateTask);

            if (templateTask == null && FlagIsSet(PortingFlags.GenerateShaders))
            {
                bool shouldReplace = FlagsAllSet(PortingFlags.Replace | PortingFlags.Recursive);

                if (!isExactMatch || shouldReplace)
                {
                    // Start generating
                    templateTask = GenerateTemplateAsync(cacheStream, tagName, out rmt2Tag);
                    PendingTemplates.Add(tagName, templateTask);
                }
            }

            // Set the new rmt2 and rmdf tags now as it will be needed in ConvertShaderInternal.
            edRenderMethod.ShaderProperties[0].Template = rmt2Tag;
            edRenderMethod.BaseRenderMethod = Matcher.FindRmdf(rmt2Tag);

            // Add a continuation to finish converting the render method once the template is ready.
            AddTask((templateTask ?? Task.CompletedTask).ContinueWith(
                _ => ConvertShaderInternal(cacheStream, blamCacheStream, edRenderMethod, renderMethod, blamRmt2), MainThreadScheduler));

            return edRenderMethod;
        }

        private Task<TemplateGenerateResult> GenerateTemplateAsync(Stream cacheStream, string tagName, out CachedTag generatedRmt2)
        {
            generatedRmt2 = null;

            // Parse the options from the tag name.
            if (!Rmt2Descriptor.TryParse(tagName, out Rmt2Descriptor rmt2Desc))
                throw new InvalidOperationException($"Invalid rmt2 tag name {tagName}");

            // Find the rmdf
            if (!Matcher.RenderMethodDefinitions.TryGetValue(rmt2Desc.Type, out RenderMethodDefinition rmdf))
                throw new InvalidOperationException($"No rmdf tag present for {rmt2Desc.Type}");

            // Deserialize dest global vertex and pixel shaders.
            var glps = TagDefinitionCache.Deserialize<GlobalPixelShader>(CacheContext, cacheStream, rmdf.GlobalPixelShader);
            var glvs = TagDefinitionCache.Deserialize<GlobalVertexShader>(CacheContext, cacheStream, rmdf.GlobalVertexShader);

            // Allocate the rmt2 tag.
            if (!CacheContext.TagCache.TryGetTag<RenderMethodTemplate>(tagName, out CachedTag rmt2Tag))
                rmt2Tag = AllocateNewTag("rmt2", tagName);

            generatedRmt2 = rmt2Tag;

            return RunOnThreadPool(() =>
            { 
                var result = new TemplateGenerateResult();

                var allRmopParameters = ShaderGeneratorNew.GatherParametersAsync(Matcher.RenderMethodOptions, rmdf, rmt2Desc.Options);

                // Generate the template
                result.Definition = ShaderGeneratorNew.GenerateTemplate(
                    CacheContext, rmdf, glvs, glps, allRmopParameters, tagName, 
                    out result.PixelShaderDefinition, 
                    out result.VertexShaderDefinition);

                return result;
            })
            .ContinueWith(task =>
            {
                // Back on the main thread here.

                TemplateGenerateResult result = task.GetAwaiter().GetResult();
                RenderMethodTemplate asyncRmt2 = result.Definition;

                // Allocate and serialize the shader tags.
                if (!CacheContext.TagCache.TryGetTag(tagName + ".pixl", out asyncRmt2.PixelShader))
                    asyncRmt2.PixelShader = AllocateNewTag("pixl", tagName);
                if (!CacheContext.TagCache.TryGetTag(tagName + ".vtsh", out asyncRmt2.VertexShader))
                    asyncRmt2.VertexShader = AllocateNewTag("vtsh", tagName);

                CacheContext.Serialize(cacheStream, asyncRmt2.PixelShader, result.PixelShaderDefinition);
                CacheContext.Serialize(cacheStream, asyncRmt2.VertexShader, result.VertexShaderDefinition);

                // Serialize the completed rmt2.
                FinishConvertTag(cacheStream, asyncRmt2, rmt2Tag);

                return result;

            }, MainThreadScheduler);
        }

        private void ConvertRenderMethodBitmaps(Stream cacheStream, Stream blamCacheStream, RenderMethod renderMethod, string blamTagName, RenderMethodTemplate rmt2)
        {
            var postprocessDef = renderMethod.ShaderProperties[0];

            for (int i = 0; i < postprocessDef.TextureConstants.Count; i++)
            {
                string paramName = BlamCache.StringTable.GetString(rmt2.TextureParameterNames[i].Name);
                var constant = postprocessDef.TextureConstants[i];
                if (constant.Bitmap == null)
                {
                    Log.Warning($"Render method in '{blamTagName}' has a null bitmap for parameter '{paramName}'");
                    continue;
                }

                Bitmap bitmap = BlamCache.Deserialize<Bitmap>(blamCacheStream, constant.Bitmap);

                if (Options.UseExperimentalDxt5nm && BumpMapParameterRegex().IsMatch(paramName) && !BitmapUtils.IsNormalMap(bitmap, 0))
                {
                    Log.Warning($"Render method in '{blamTagName}' has an invalid bump map for parameter '{paramName}'");

                    if (BumpFileNameSuffixRegex().IsMatch(constant.Bitmap.Name))
                    {
                        Log.Warning($"Possibly incorrect bitmap usage detected '{constant.Bitmap}'");
                    }

                    // Fix diffuse bitmaps that are incorrectly used as bump maps. This needs to be done for dxt5nm

                    BitmapConverterMode oldBitmapMode = BitmapMode;
                    try
                    {
                        BitmapMode = BitmapConverterMode.DiffuseToNormal;

                        var options = new ConvertTagOptions()
                        {
                            TargetTagName = $"{constant.Bitmap.Name}_dxt5nm"
                        };
                        constant.Bitmap = ConvertTag(cacheStream, blamCacheStream, constant.Bitmap, blamTagName, bitmap, options);    
                    }
                    finally
                    {
                        BitmapMode = oldBitmapMode;
                    }
                }
                else
                {
                    constant.Bitmap = ConvertTag(cacheStream, blamCacheStream, constant.Bitmap, blamTagName, bitmap);
                }

                // Fixup the parameters block
                foreach (var parameter in renderMethod.Parameters)
                {
                    if (parameter.ParameterType == RenderMethodOption.ParameterBlock.OptionDataType.Bitmap && parameter.Name == rmt2.TextureParameterNames[i].Name)
                    {
                        parameter.Bitmap = constant.Bitmap;
                        break;
                    }
                }
            }
        }

        private RenderMethod ConvertShaderInternal(Stream cacheStream, Stream blamCacheStream, RenderMethod finalRm, RenderMethod blamRm, RenderMethodTemplate blamRmt2)
        {
            var rmdf = TagDefinitionCache.Deserialize<RenderMethodDefinition>(CacheContext, cacheStream, finalRm.BaseRenderMethod);
            var rmt2 = TagDefinitionCache.Deserialize<RenderMethodTemplate>(CacheContext, cacheStream, finalRm.ShaderProperties[0].Template);

            var shaderConverter = new ShaderConverter(
                CacheContext, BlamCache, cacheStream, blamCacheStream, finalRm, blamRm, rmdf, rmt2, blamRmt2, Matcher);

            RenderMethod newRm = shaderConverter.ConvertRenderMethod();

            // copy each field as at this point in conversion,
            // we don't know the original tag type and what extra fields exist

            finalRm.BaseRenderMethod = newRm.BaseRenderMethod;
            finalRm.Options = newRm.Options;
            finalRm.Parameters = newRm.Parameters;
            finalRm.ShaderProperties = newRm.ShaderProperties;
            finalRm.RenderFlags = newRm.RenderFlags;
            finalRm.SortLayer = newRm.SortLayer;
            finalRm.Version = newRm.Version;
            finalRm.CustomFogSettingIndex = newRm.CustomFogSettingIndex;
            finalRm.PredictionAtomIndex = newRm.PredictionAtomIndex;

            return finalRm;
        }


        private RenderMethodOption ConvertRenderMethodOption(RenderMethodOption rmop)
        {
            if (BlamCache.Version == CacheVersion.Halo3ODST || BlamCache.Version >= CacheVersion.HaloReach)
            {
                foreach (var block in rmop.Parameters)
                {
                    if (BlamCache.Version == CacheVersion.Halo3ODST && block.RenderMethodExtern >= RenderMethodExtern.emblem_player_shoulder_texture)
                    {
                        block.RenderMethodExtern = (RenderMethodExtern)((int)block.RenderMethodExtern + 2);
                    }

                    if (BlamCache.Version >= CacheVersion.HaloReach)
                    {
                        // TODO
                    }
                }
            }

            return rmop;
        }

        [GeneratedRegex("^(bump_map|bump_detail_map|bump_detail_masked_map|distort_map|wrinkle_normal|detail_bump)")]
        private static partial Regex BumpMapParameterRegex();

        [GeneratedRegex("(_bump|_zbump|_detailbump|_normal)$")]
        private static partial Regex BumpFileNameSuffixRegex();

        private class TemplateGenerateResult
        {
            public RenderMethodTemplate Definition;
            public PixelShader PixelShaderDefinition;
            public VertexShader VertexShaderDefinition;
        }
    }
}