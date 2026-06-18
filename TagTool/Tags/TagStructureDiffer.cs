using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TagTool.Cache;
using TagTool.Common;
using TagTool.Shaders.ShaderFunctions;
using TagTool.Tags.Definitions;

namespace TagTool.Tags
{
    public class TagStructureDiffer
    {
        private readonly GameCache _cache1;
        private readonly GameCache _cache2;
        private readonly Stream _stream1;
        private readonly Stream _stream2;

        public bool IgnoreData { get; set; }
        public bool SameCountOnly { get; set; }
        public bool ConvertFunctions { get; set; } = true;
        public bool Generic { get; set; }

        public TagStructureDiffer(GameCache cache1, GameCache cache2, Stream stream1, Stream stream2)
        {
            _cache1 = cache1;
            _cache2 = cache2;
            _stream1 = stream1;
            _stream2 = stream2;
        }

        public List<Difference> Diff(object data1, object data2)
        {
            List<Difference> differences = [];
            DiffData(data1.GetType(), data1, data2, differences);
            return differences;
        }

        private void DiffData(Type type, object data1, object data2, List<Difference> differences, string path = "")
        {
            if (data1 == data2)
                return;

            if (data1 == null && data2 != null || data2 == null && data1 != null)
            {
                differences.Add(new Difference(DifferenceKind.Value, path, data1, data2));
                return;
            }

            if (type.IsPrimitive)
            {
                if (typeof(float).IsAssignableFrom(type))
                {
                    if (Math.Abs((float)data1 - (float)data2) >= 0.00001f)
                        differences.Add(new Difference(DifferenceKind.Value, path, data1, data2));
                }
                else if (typeof(double).IsAssignableFrom(type))
                {
                    if (Math.Abs((double)data1 - (double)data2) >= 0.0000001)
                        differences.Add(new Difference(DifferenceKind.Value, path, data1, data2));
                }
                else
                {
                    if (!data1.Equals(data2))
                        differences.Add(new Difference(DifferenceKind.Value, path, data1, data2));
                }
            }
            else
            {
                if (typeof(TagStructure).IsAssignableFrom(type))
                {
                    if (!Generic && TrySpecializedDiff(type, data1, data2, differences))
                        return;

                    var struct1 = (TagStructure)data1;
                    var struct2 = (TagStructure)data2;
                    var fields1 = struct1.GetTagFieldEnumerable(_cache1.Version, _cache1.Platform);
                    var fields2 = struct2.GetTagFieldEnumerable(_cache2.Version, _cache2.Platform);
                    var commonFields = fields1.Join(fields2, a => a.Name, b => b.Name, (a, b) => (a, b));

                    if (typeof(TagFunction).IsAssignableFrom(type))
                    {
                        if (_cache1.Endianness != _cache2.Endianness && ConvertFunctions)
                        {
                            TagFunction.ConvertTagFunction(_cache1.Endianness, (TagFunction)struct1);
                            TagFunction.ConvertTagFunction(_cache2.Endianness, (TagFunction)struct2);
                        }
                    }

                    foreach (var (field1, field2) in commonFields)
                    {
                        var value1 = field1.GetValue(struct1);
                        var value2 = field2.GetValue(struct2);
                        DiffData(field1.FieldType, value1, value2, differences, path.Length == 0 ? field1.Name : $"{path}.{field1.Name}");
                    }
                }
                else if (typeof(IList).IsAssignableFrom(type))
                {
                    var list1 = (IList)data1;
                    var list2 = (IList)data2;
                    int elementCount = list1.Count;
                    bool isData = type == typeof(byte[]);

                    if (list1.Count != list2.Count)
                    {
                        differences.Add(new Difference(DifferenceKind.ElementCount, path, list1, list2));
                        if (SameCountOnly || isData)
                            return;
                        else if (list2.Count < list1.Count)
                            elementCount = list2.Count;
                    }

                    if (IgnoreData && isData)
                        return;

                    Type elementType = type.IsArray ? type.GetElementType() : type.GetGenericArguments().First();
                    for (int i = 0; i < elementCount; i++)
                        DiffData(elementType, list1[i], list2[i], differences, $"{path}[{i}]");
                }
                else if (typeof(CachedTag).IsAssignableFrom(type))
                {
                    var tag1 = (CachedTag)data1;
                    var tag2 = (CachedTag)data2;
                    if (tag1.Group.Tag != tag2.Group.Tag || tag1.Name != tag2.Name)
                        differences.Add(new Difference(DifferenceKind.Value, path, data1, data2));
                }
                else if (typeof(StringId).IsAssignableFrom(type))
                {
                    var string1 = _cache1.StringTable.GetString((StringId)data1);
                    var string2 = _cache2.StringTable.GetString((StringId)data2);
                    if (string1 != string2)
                        differences.Add(new Difference(DifferenceKind.Value, path, string1, string2));
                }
                else if (typeof(IComparable).IsAssignableFrom(type))
                {
                    var comparable1 = (IComparable)data1;
                    var comparable2 = (IComparable)data2;
                    if (comparable1.CompareTo(comparable2) != 0)
                        differences.Add(new Difference(DifferenceKind.Value, path, data1, data2));
                }
                else
                {
                    var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    foreach (var field in properties.Where(x => x.CanRead && x.CanWrite))
                    {
                        var value1 = field.GetValue(data1);
                        var value2 = field.GetValue(data2);
                        DiffData(field.PropertyType, value1, value2, differences, path.Length == 0 ? field.Name : $"{path}.{field.Name}");
                    }

                    var fields = type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    foreach (var field in fields)
                    {
                        var value1 = field.GetValue(data1);
                        var value2 = field.GetValue(data2);
                        DiffData(field.FieldType, value1, value2, differences, path.Length == 0 ? field.Name : $"{path}.{field.Name}");
                    }
                }
            }
        }

        private bool TrySpecializedDiff(Type type, object data1, object data2, List<Difference> differences)
        {
            switch (data1)
            {
                case RenderMethod rm:
                    DiffRenderMethod(rm, (RenderMethod)data2, differences);
                    return true;
            }
            return false;
        }

        private void DiffRenderMethod(RenderMethod data1, RenderMethod data2, List<Difference> differences)
        {
            string GetName(GameCache cache, StringId id) => cache.StringTable.GetString(id);

            var fields1 = data1.GetTagFieldEnumerable(_cache1.Version, _cache1.Platform);
            var properties1 = data1.ShaderProperties[0];
            var properties2 = data2.ShaderProperties[0];

            var rmdf1 = (RenderMethodDefinition)_cache1.Deserialize(_stream1, data1.BaseRenderMethod);
            var rmdf2 = (RenderMethodDefinition)_cache2.Deserialize(_stream2, data2.BaseRenderMethod);
            var template1 = (RenderMethodTemplate)_cache1.Deserialize(_stream1, properties1.Template);
            var template2 = (RenderMethodTemplate)_cache2.Deserialize(_stream2, properties2.Template);

            // category options
            var categoryMapping1 = rmdf1.GetCategoryOptionMapping(_cache1, properties1.Template);
            var categoryMapping2 = rmdf2.GetCategoryOptionMapping(_cache2, properties2.Template);
            DiffDictionaries(categoryMapping1, categoryMapping2, differences);

            // template
            string t1 = properties1.Template?.Name.Split('\\').Last();
            string t2 = properties2.Template?.Name.Split('\\').Last();
            DiffData(typeof(string), t1, t2, differences, "RenderMethodTemplate");

            // texture constants
            var textures1 = template1.TextureParameterNames.Zip(properties1.TextureConstants).ToDictionary(n => GetName(_cache1, n.First.Name), n => $"{n.Second.Bitmap}");
            var textures2 = template2.TextureParameterNames.Zip(properties2.TextureConstants).ToDictionary(n => GetName(_cache2, n.First.Name), n => $"{n.Second.Bitmap}");
            DiffDictionaries(textures1, textures2, differences);

            // real constants
            var realConstants1 = template1.RealParameterNames.Zip(properties1.RealConstants).ToDictionary(n => GetName(_cache1, n.First.Name), n => string.Join(" ", n.Second.Values));
            var realConstants2 = template2.RealParameterNames.Zip(properties2.RealConstants).ToDictionary(n => GetName(_cache2, n.First.Name), n => string.Join(" ", n.Second.Values));
            DiffDictionaries(realConstants1, realConstants2, differences);

            // integer constants
            var integers1 = template1.IntegerParameterNames.Zip(properties1.RealConstants).ToDictionary(n => GetName(_cache1, n.First.Name), n => n.Second);
            var integers2 = template2.IntegerParameterNames.Zip(properties2.RealConstants).ToDictionary(n => GetName(_cache2, n.First.Name), n => n.Second);
            DiffDictionaries(integers1, integers2, differences);

            // boolean constants
            var booleanList1 = Enumerable.Repeat(properties1.BooleanConstants, template1.BooleanParameterNames.Count).ToArray();
            for (int i = 0; i < template1.BooleanParameterNames.Count; i++)
                booleanList1[i] = booleanList1[i] >> i;

            var booleanList2 = Enumerable.Repeat(properties2.BooleanConstants, template2.BooleanParameterNames.Count).ToArray();
            for (int i = 0; i < template2.BooleanParameterNames.Count; i++)
                booleanList2[i] = booleanList2[i] >> i;

            var bools1 = template1.BooleanParameterNames.Zip(booleanList1).ToDictionary(n => GetName(_cache1, n.First.Name), n => n.Second & 1);
            var bools2 = template2.BooleanParameterNames.Zip(booleanList2).ToDictionary(n => GetName(_cache2, n.First.Name), n => n.Second & 1);
            DiffDictionaries(bools1, bools2, differences);

            // functions
            IgnoreData = true;
            var lookup1 = ShaderFunctionHelper.GetAnimatedParameters(_cache1, data1, template1).ToLookup(n => n.FunctionIndex);
            var lookup2 = ShaderFunctionHelper.GetAnimatedParameters(_cache2, data2, template2).ToLookup(n => n.FunctionIndex);
            for (int i = 0; i < Math.Max(properties1.Functions.Count, properties2.Functions.Count); i++)
            {
                string title = $"Functions[{i}]";
                bool hasIndex1 = i < lookup1.Count;
                bool hasIndex2 = i < lookup2.Count;

                string parameterString1 = string.Join(',', lookup1[i].Select(n => n.Name));
                string parameterString2 = string.Join(',', lookup2[i].Select(n => n.Name));
                var firstOfGroup1 = (hasIndex1 ? lookup1[i] : []).FirstOrDefault();
                var firstOfGroup2 = (hasIndex2 ? lookup2[i] : []).FirstOrDefault();

                var function1 = properties1.Functions.ElementAtOrDefault(i) ?? new();
                var function2 = properties2.Functions.ElementAtOrDefault(i) ?? new();
                TagFunction.ConvertTagFunction(_cache1.Endianness, function1.Function);
                TagFunction.ConvertTagFunction(_cache2.Endianness, function2.Function);

                DiffData(firstOfGroup1.Type.GetType(), firstOfGroup1.Type, firstOfGroup2.Type, differences, $"{title} Type");
                DiffData(typeof(string), parameterString1, parameterString2, differences, $"{title} Parameters");
                DiffData(function1.GetType(), function1, function2, differences, $"{title}");
                DiffData(typeof(string), $"\n{function1?.Function}", $"\n{function2?.Function}", differences, $"{title}.Data");
            }

            // misc
            DiffData(properties1.BlendMode.GetType(), properties1.BlendMode, properties2.BlendMode, differences, "BlendMode");
            DiffData(properties1.Flags.GetType(), properties1.Flags, properties2.Flags, differences, "PropertiesFlags");
            DiffData(data1.RenderFlags.GetType(), data1.RenderFlags, data2.RenderFlags, differences, "RenderFlags");
            DiffData(data1.SortLayer.GetType(), data1.SortLayer, data2.SortLayer, differences, "SortLayer");
        }

        private void DiffDictionaries<TOne,TTwo>(Dictionary<TOne, TTwo> dict1, Dictionary<TOne, TTwo> dict2, List<Difference> differences)
        {
            var keysUnion = dict1.Keys.Union(dict2.Keys).ToList();
            foreach (var key in keysUnion)
            {
                dict1.TryGetValue(key, out TTwo value1);
                dict2.TryGetValue(key, out TTwo value2);

                DiffData(typeof(TTwo), value1, value2, differences, key.ToString());
            }
        }

        public enum DifferenceKind
        {
            Value,
            ElementCount,
        }

        public readonly record struct Difference(DifferenceKind Kind, string Path, object Value1, object Value2)
        {
            public override string ToString()
            {
                return $"({Kind}) {Path} {Value1} <=> {Value2}";
            }
        }
    }
}