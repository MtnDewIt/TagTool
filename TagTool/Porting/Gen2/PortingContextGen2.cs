using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using TagTool.Audio;
using TagTool.BlamFile;
using TagTool.Cache;
using TagTool.Cache.HaloOnline;
using TagTool.Commands.Common;
using TagTool.Commands.ScenarioStructureBSPs;
using TagTool.Common;
using TagTool.Common.Logging;
using TagTool.IO;
using TagTool.Porting;
using TagTool.Tags;
using TagTool.Tags.Definitions.Gen2;
using static TagTool.Cache.GameCacheGen2;
using static TagTool.Tags.Definitions.Gen2.Scenario.ScenarioLevelDataBlock;
using Gen3Globals = TagTool.Tags.Definitions.Globals;

namespace TagTool.Porting.Gen2
{
    partial class PortingContextGen2 : PortingContext
    {
        private StructureAutoConverter AutoConverter;
        private LocalizedLevelDataStruct LevelData;

        public PortingContextGen2(GameCacheHaloOnlineBase cache, GameCache BlamCache) : base(cache, BlamCache)
        { 
            AutoConverter = new StructureAutoConverter(BlamCache, CacheContext);
        }

        protected override bool TagIsValid(CachedTag blamTag, Stream cacheStream, Stream blamCacheStream, object blamDefinition, out CachedTag resultTag)
        {
            resultTag = null;
            //use hardcoded list of supported tags to prevent unnecessary deserialization
            List<string> supportedTagGroups = new List<string>
            {
                "ant!",
                "coll",
                "jmad",
                "phmo",
                "mode",
                "hlmt",
                "bitm",
                "bloc",
                "vehi",
                "weap",
                "scen",
                "jpt!",
                "proj",
                "trak",
                "shad",
                "sbsp",
                "scnr",
                "snd!",
                "lsnd",
                "mach",
                "ligh",
                "eqip",
                "ctrl",
                "bipd",
                "nhdt",
            };
            // don't print a warning for these
            List<string> hiddenTagGroups = new List<string>
            {
                "*cen",
                "*ipd",
                "*ehi",
                "*qip",
                "*eap",
                "*sce",
                "*igh",
                "dgr*",
                "dec*",
                "cin*",
                "trg*",
                "clu*",
                "/**/",
                "*rea",
                "sslt",
                "dc*s",
                "hsc*",
                "DECR",
                "fog ",
                "itmc",
                "ltmp",
                "sky ",
                "stem",
                "spas",
                "vehc",
                "vrtx"
            };

            var group = blamTag.Group.ToString();
            if (!supportedTagGroups.Contains(group))
            {
                if (!hiddenTagGroups.Contains(group))
                    Log.Warning($"Porting tag group '{group}' not yet supported, returning null!");

                return false;
            }


            if (blamDefinition is Shader shader)
            {
                if (!ShaderTemplateIsSupported(shader.Template.Name.Split('\\').Last()))
                {
                    Log.Warning($"Shader template '{shader.Template.Name}' not yet supported!");
                    CacheContext.TagCache.TryGetTag(@"shaders\invalid.rmsh", out resultTag);
                    return false;
                }
            }

            return true;
        }

        protected override (string Name, Tag GroupTag) GetTargetTagNameAndGroup(CachedTag blamTag, object blamDefinition)
        {
            if (blamDefinition is Shader shader)
                return (blamTag.Name, "rmsh");

            return base.GetTargetTagNameAndGroup(blamTag, blamDefinition);
        }

        protected override object ConvertDefinition(Stream cacheStream, Stream blamCacheStream, CachedTag blamTag, CachedTag edTag, object gen2definition)
        {
            object origGen2definition = BlamCache.Deserialize(blamCacheStream, blamTag);

            gen2definition = ConvertData(cacheStream, blamCacheStream, gen2definition, gen2definition, blamTag.Name);
            object definition;

            switch (gen2definition)
            {
                case Antenna antenna:
                case PointPhysics pointPhysics: // not a widget, but we can group it with widgets
                    definition = ConvertWidget(gen2definition);
                    break;
                case CollisionModel collisionModel:
                    definition = ConvertCollisionModel(collisionModel);
                    break;
                case ModelAnimationGraph modelAnimationGraph:
                    definition = ConvertModelAnimationGraph(modelAnimationGraph);
                    break;
                case PhysicsModel physicsModel:
                    definition = ConvertPhysicsModel(physicsModel);
                    break;
                case RenderModel renderModel:
                    definition = ConvertRenderModel(renderModel);
                    break;
                case Model model:
                    definition = ConvertModel(model, cacheStream);
                    break;
                case Bitmap bitmap:
                    definition = ConvertBitmap(bitmap, blamTag.Name);
                    break;
                case Crate crate:
                case Scenery scenery:
                case Weapon weapon:
                case Vehicle vehicle:
                case Projectile projectile:
                case DeviceMachine devicemachine:
                case Equipment equipment:
                case DeviceControl devicecontrol:
                case Biped biped:
                    definition = ConvertObject(gen2definition, cacheStream);
                    break;
                case CameraTrack track:
                    Tags.Definitions.CameraTrack newtrack = new Tags.Definitions.CameraTrack();
                    AutoConverter.TranslateTagStructure(track, newtrack);
                    definition = newtrack;
                    break;
                case Effect effect:
                case Particle particle:
                case ParticlePhysics pmov:
                case DamageEffect damage:
                    definition = ConvertEffect(gen2definition, origGen2definition, cacheStream, blamCacheStream);
                    break;
                case Shader shader:
                    definition = ConvertShader(shader, (Shader)origGen2definition, blamTag.Name, cacheStream, blamCacheStream, blamTag);
                    break;
                //return Cache.TagCache.GetTag(@"shaders\invalid.shader");
                case ScenarioStructureBsp sbsp:
                    definition = ConvertStructureBSP(sbsp);
                    break;
                case Scenario scnr:
                    Scenario oldscnr = BlamCache.Deserialize<Scenario>(blamCacheStream, blamTag);
                    definition = ConvertScenario(scnr, oldscnr, blamTag.Name, cacheStream, blamCacheStream);
                    switch (oldscnr.Type)
                    {
                        case Scenario.TypeValue.SinglePlayer:
                            LevelData = oldscnr.LevelData[0].CampaignLevelData[0].LocalizedLevelData;
                            break;
                        case Scenario.TypeValue.Multiplayer:
                            LevelData = oldscnr.LevelData[0].Multiplayer[0].LocalizedLevelData;
                            break;
                        default:
                            LevelData = null;
                            break;
                    }
                    break;
                case Light light:
                    definition = ConvertLight(light);
                    break;
                case Sound sound:
                    definition = ConvertSound((Cache.Gen2.CachedTagGen2)blamTag, sound, blamCacheStream, blamTag.Name);
                    break;
                case SoundLooping loop:
                    definition = ConvertLoopingSound((Cache.Gen2.CachedTagGen2)blamTag, loop, cacheStream);
                    break;
                case SoundEnvironment snde:
                    definition = ConvertSoundEnvironment(snde);
                    break;
                case NewHudDefinition nhdt:
                    NewHudDefinition gen2Hud = BlamCache.Deserialize<NewHudDefinition>(blamCacheStream, blamTag);
                    definition = ConvertNewHudDefinition(nhdt, gen2Hud, cacheStream, blamCacheStream, blamTag);
                    break;
                default:
                    throw new NotSupportedException($"Porting tag group '{blamTag.Group}' not yet supported!");
            }

            PostFixups(definition, edTag, cacheStream);
            return definition;
        }

        public override object ConvertData(Stream cacheStream, Stream blamCacheStream, object data, object definition, string blamTagName)
        {
            switch (data)
            {
                case SoundClass soundClass:
                    return ConvertSoundClass(soundClass);
                default:
                    return base.ConvertData(cacheStream, blamCacheStream, data, definition, blamTagName);
            }
        }

        public void PostFixups(object definition, CachedTag destinationTag, Stream cacheStream)
        {
            switch (definition)
            {
                case TagTool.Tags.Definitions.ScenarioStructureBsp sbsp:
                    foreach (var cluster in sbsp.Clusters)
                        if(cluster.InstancedGeometryPhysics != null)
                            cluster.InstancedGeometryPhysics.StructureBsp = destinationTag;
                    break;
                case TagTool.Tags.Definitions.Scenario scnr:
                    {
                        foreach (var block in scnr.StructureBsps)
                        {
                            if (block.StructureBsp == null)
                                continue;

                            CachedTag sbspTag = block.StructureBsp;
                            var sbsp = CacheContext.Deserialize<TagTool.Tags.Definitions.ScenarioStructureBsp>(cacheStream, sbspTag);
                            new GenerateStructureSurfacesCommand(CacheContext, sbspTag, sbsp, cacheStream, scnr).Execute(new List<string> { });
                            CacheContext.Serialize(cacheStream, sbspTag, sbsp);
                        }
                    }
                    break;
                default:
                    return;
            }
        }
        protected private bool ShaderTemplateIsSupported(string templateName)
        {
            switch (templateName)
            {
                case "overlay":
                case "plasma_alpha":
                case "plasma_alpha_active_camo":
                case "tex_alpha_test":
                case "tex_bump":
                case "tex_bump_active_camo":
                case "tex_bump_shiny":
                case "tex_bump_no_specular":
                case "tex_bump_no_alpha":
                case "tex_bump_alpha_test":
                case "tex_bump_alpha_test_clamped":
                case "tex_bump_alpha_test_single_pass":
                case "tex_bump_detail_blend":
                case "tex_bump_detail_keep_blend":
                case "tex_bump_detail_blend_detail":
                case "tex_bump_detail_keep":
                case "tex_bump_detail_overlay":
                case "tex_bump_dprs_env":
                case "tex_bump_env":
                case "tex_bump_env_clamped":
                case "tex_bump_env_combined":
                case "tex_bump_env_dbl_spec":
                case "tex_bump_env_illum":
                case "tex_bump_env_illum_combined":
                case "tex_bump_env_illum_3_channel":
                case "tex_bump_env_illum_3_channel_occlusion_combined":
                case "tex_bump_env_alpha_test":
                case "tex_bump_env_alpha_clamped":
                case "tex_bump_env_alpha_test_indexed":
                case "tex_bump_env_four_change_color":
                case "tex_bump_env_no_detail":
                case "tex_bump_env_two_change_color":
                case "tex_bump_env_two_change_color_indexed":
                case "tex_bump_illum":
                case "tex_bump_illum_3_channel":
                case "tex_bump_one_change_color":
                case "tex_bump_plasma_one_channel_illum":
                case "tex_bump_three_detail_blend":
                case "tex_bump_two_detail":
                case "tex_env":
                case "prt_simple":
                case "illum":
                case "illum_3_channel":
                case "one_add_illum":
                case "one_alpha_env":
                case "one_alpha_env_active_camo":
                case "two_alpha_env_multichannel":
                case "two_add_env_illum":
                case "sky_one_alpha_env":
                case "sky_one_alpha_env_illum":
                case "two_alpha_clouds":
                case "sky_two_alpha_clouds":
                    return true;
                default:
                    return false;
            }
        }

        protected override T ConvertStructure<T>(Stream cacheStream, Stream blamCacheStream, T data, object definition, string blamTagName)
        {
            foreach (var tagFieldInfo in TagStructure.GetTagFieldEnumerable(data.GetType(), BlamCache.Version, BlamCache.Platform))
            {
                var attr = tagFieldInfo.Attribute;
                if (!CacheVersionDetection.TestAttribute(attr, BlamCache.Version, BlamCache.Platform))
                    continue;

                // skip the field if no conversion is needed
                if ((tagFieldInfo.FieldType.IsValueType && tagFieldInfo.FieldType != typeof(StringId)) ||
                tagFieldInfo.FieldType == typeof(string))
                    continue;

                var oldValue = tagFieldInfo.GetValue(data);
                if (oldValue is null)
                    continue;

                // convert the field
                var newValue = ConvertData(cacheStream, blamCacheStream, oldValue, definition, blamTagName);
                tagFieldInfo.SetValue(data, newValue);
            }

            return data;
        }

        public Damage.DamageReportingType ConvertDamageReportingType(Damage.DamageReportingType damageReportingType)
        {
            string value = damageReportingType.Halo2Retail.ToString();

            if (value == null || !Enum.TryParse(value, out damageReportingType.HaloOnline))
            {
                Log.Warning($"Unsupported Damage reporting type '{value}'. Using default.");
                damageReportingType.HaloOnline = Damage.DamageReportingType.HaloOnlineValue.GuardiansUnknown;
            }

            return damageReportingType;
        }

        // wrote this for Gen2 but probably not necessary. create and move into scenario porting utils maybe
        private short GetEquivalentGlobalMaterial(short globalMaterialIndexGen2, Globals globalsGen2, Gen3Globals globals)
        {
            var materialBlockGen2 = globalsGen2.Materials[globalMaterialIndexGen2];

            StringId gen3Name = CacheContext.StringTable.GetStringId(BlamCache.StringTable.GetString(materialBlockGen2.Name));
            if (gen3Name == StringId.Invalid || gen3Name == StringId.Empty)
                gen3Name = CacheContext.StringTable.GetStringId(BlamCache.StringTable.GetString(materialBlockGen2.ParentName));

            short newIndex = (short)globals.Materials.FindIndex(m => m.Name == gen3Name);

            if (newIndex == -1)
                return 0;   // default_material
            else
                return newIndex;
        }

    }
}