﻿using TagTool.Cache;
using TagTool.Commands;
using TagTool.Geometry;
using TagTool.Serialization;
using TagTool.Shaders;
using TagTool.Tags.Definitions;
using System;
using System.Collections.Generic;
using System.IO;
using TagTool.Common;
using System.Linq;

namespace TagTool.Commands.Shaders
{
    public class GenerateShader<T> : Command
    {
        private GameCacheContext CacheContext { get; }
        private CachedTagInstance Tag { get; }
        private T Definition { get; }
        public static bool IsVertexShader => typeof(T) == typeof(GlobalVertexShader) || typeof(T) == typeof(VertexShader);
        public static bool IsPixelShader => typeof(T) == typeof(GlobalPixelShader) || typeof(T) == typeof(PixelShader);

        public GenerateShader(GameCacheContext cacheContext, CachedTagInstance tag, T definition) :
            base(true,

                "Generate",
                "Compiles HLSL source file from scratch :D",
                "Generate <index> <shader_type> <drawmode> <parameters...>",
                "Compiles HLSL source file from scratch :D")
        {
            CacheContext = cacheContext;
            Tag = tag;
            Definition = definition;
        }

        public override object Execute(List<string> args)
        {
            if (args.Count <= 0)
                return false;

            if(args.Count < 2)
            {
                Console.WriteLine("Invalid number of args");
                return false;
            }

            Int32 index;
            string type;
            string drawmode_str;
            try
            {
                index = Int32.Parse(args[0]);
                type = args[1].ToLower();
                drawmode_str = args[2].ToLower();
            } catch
            {
                Console.WriteLine("Invalid index, type, and drawmode combination");
                return false;
            }

   //         var drawmode = TemplateShaderGenerator.Drawmode.Default;
   //         {
   //             bool found_drawmode = false;
   //             var drawmode_enums = Enum.GetValues(typeof(TemplateShaderGenerator.Drawmode)).Cast<TemplateShaderGenerator.Drawmode>();
   //             foreach (var drawmode_enum in drawmode_enums)
   //             {
   //                 var enum_name = Enum.GetName(typeof(TemplateShaderGenerator.Drawmode), drawmode_enum).ToLower();
   //                 if (drawmode_str == enum_name)
   //                 {
   //                     drawmode = drawmode_enum;
   //                     found_drawmode = true;
   //                     break;
   //                 }
   //             }

   //             if (!found_drawmode)
   //             {
   //                 //try
   //                 //{
   //                 //    drawmode = (TemplateShaderGenerator.Drawmode)Int32.Parse(drawmode_str);
   //                 //}
   //                 //catch
   //                 {
   //                     Console.WriteLine("Invalid shader arguments! (could not parse to drawmode)");
   //                     return false;
   //                 }
   //             }
   //         }

   //         Int32[] shader_args;
			//try { shader_args = Array.ConvertAll(args.Skip(3).ToArray(), Int32.Parse); }
			//catch { Console.WriteLine("Invalid shader arguments! (could not parse to Int32[].)"); return false; }

            var func_params = typeof(HaloShaderGenerator.HaloShaderGenerator).GetMethod("GenerateShader").GetParameters();

            // runs the appropriate shader-generator for the template type.
            byte[] bytecode = null;
            switch(type)
            {
                case "shader_templates":
                case "shader_template":


                    if (HaloShaderGenerator.HaloShaderGenerator.IsShaderSuppored(HaloShaderGenerator.Enums.ShaderType.Shader, HaloShaderGenerator.Enums.ShaderStage.Albedo))
                    {
                        bytecode = HaloShaderGenerator.HaloShaderGenerator.GenerateShader(
                            HaloShaderGenerator.Enums.ShaderStage.Albedo,
                            HaloShaderGenerator.Enums.Albedo.Two_Change_Color,
                            HaloShaderGenerator.Enums.Bump_Mapping.Off,
                            HaloShaderGenerator.Enums.Alpha_Test.None,
                            HaloShaderGenerator.Enums.Specular_Mask.No_Specular_Mask,
                            HaloShaderGenerator.Enums.Material_Model.None,
                            HaloShaderGenerator.Enums.Environment_Mapping.None,
                            HaloShaderGenerator.Enums.Self_Illumination.Off,
                            HaloShaderGenerator.Enums.Blend_Mode.Opaque,
                            HaloShaderGenerator.Enums.Parallax.Off,
                            HaloShaderGenerator.Enums.Misc.First_Person_Always,
                            HaloShaderGenerator.Enums.Distortion.Off,
                            HaloShaderGenerator.Enums.Soft_fade.Off
                        );
                        Console.WriteLine(bytecode?.Length ?? -1);
                    }

                    break;
                case "beam_templates":
                case "beam_template":
				case "contrail_templates":
                case "contrail_template":
				case "cortana_templates":
				case "cortana_template":
				case "custom_templates":
				case "custom_template":
				case "decal_templates":
                case "decal_template":
                case "foliage_templates":
                case "foliage_template":
                case "halogram_templates":
                case "halogram_template":
                case "light_volume_templates":
                case "light_volume_template":
				case "particle_templates":
				case "particle_template":
				case "screen_templates":
				case "screen_template":
                case "terrain_templates":
                case "terrain_template":
                case "water_templates":
                case "water_template":
				default:
                    Console.WriteLine($"{type} is not implemented");
                    return false;
            }

            if (bytecode == null) return false;

            if (typeof(T) == typeof(PixelShader) || typeof(T) == typeof(GlobalPixelShader))
            {
                var shader_data_block = new PixelShaderBlock
                {
                    PCShaderBytecode = bytecode
                };

                if (typeof(T) == typeof(PixelShader))
                {
                    var _definition = Definition as PixelShader;
                    var existing_block = _definition.Shaders[index];
                    //shader_data_block.PCParameters = existing_block.PCParameters;
                    //TODO: Set parameters
                    //shader_data_block.PCParameters = shader_gen_result.Parameters;

                    _definition.Shaders[index] = shader_data_block;
                }

                if (typeof(T) == typeof(GlobalPixelShader))
                {
                    var _definition = Definition as GlobalPixelShader;
                    var existing_block = _definition.Shaders[index];
                    //shader_data_block.PCParameters = existing_block.PCParameters;
                    //TODO: Set parameters
                    //shader_data_block.PCParameters = shader_gen_result.Parameters;

                    _definition.Shaders[index] = shader_data_block;
                }
            }
            else throw new NotImplementedException();

            //if (typeof(T) == typeof(VertexShader) || typeof(T) == typeof(GlobalVertexShader))
            //{

            //    var shader_data_block = new VertexShaderBlock
            //    {
            //        PCShaderBytecode = bytecode
            //    };

            //    if (typeof(T) == typeof(VertexShader))
            //    {
            //        var _definition = Definition as VertexShader;
            //        var existing_block = _definition.Shaders[index];
            //        shader_data_block.PCParameters = existing_block.PCParameters;

            //        _definition.Shaders[index] = shader_data_block;
            //    }

            //    if (typeof(T) == typeof(GlobalVertexShader))
            //    {
            //        var _definition = Definition as GlobalVertexShader;
            //        var existing_block = _definition.Shaders[index];
            //        shader_data_block.PCParameters = existing_block.PCParameters;


            //        _definition.Shaders[index] = shader_data_block;
            //    }
            //}

            return true;
        }

        public List<ShaderParameter> GetParamInfo(string assembly)
        {
            var parameters = new List<ShaderParameter> { };

            using (StringReader reader = new StringReader(assembly))
            {
                if (string.IsNullOrEmpty(assembly))
                    return null;

                string line;

                while (!(line = reader.ReadLine()).Contains("//   -"))
                    continue;

                while (!string.IsNullOrEmpty((line = reader.ReadLine())))
                {
                    line = (line.Replace("//   ", "").Replace("//", "").Replace(";", ""));

                    while (line.Contains("  "))
                        line = line.Replace("  ", " ");

                    if (!string.IsNullOrEmpty(line))
                    {
                        var split = line.Split(' ');
                        parameters.Add(new ShaderParameter
                        {
                            ParameterName = (CacheContext as HaloOnlineCacheContext).GetStringId(split[0]),
                            RegisterType = (ShaderParameter.RType)Enum.Parse(typeof(ShaderParameter.RType), split[1][0].ToString()),
                            RegisterIndex = byte.Parse(split[1].Substring(1)),
                            RegisterCount = byte.Parse(split[2])
                        });
                    }
                }
            }

            return parameters;
        }
    }
}