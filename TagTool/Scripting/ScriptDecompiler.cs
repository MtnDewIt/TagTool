using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TagTool.Cache;
using TagTool.Common;
using TagTool.Common.Logging;
using TagTool.IO;
using TagTool.Tags.Definitions;

namespace TagTool.Scripting
{
    public class ScriptDecompiler
    {
        public Scenario Definition { get; set; }
        public BinaryReader scriptStringReader { get; set; }
        public GameCache Cache { get; set; }
        public CachedTag Tag { get; set; }
        public List<GenericExpression> Globals = new List<GenericExpression>();
        public List<GenericExpression> Scripts = new List<GenericExpression>();
        public bool PerformCleanup { get; } = true;
        public string Indent { get; }

        public ScriptDecompiler(GameCache cache, Scenario definition, CachedTag tag, bool cleanup = true, bool spaces = false)
        {
            Cache = cache;
            Definition = definition;
            Tag = tag;
            var scriptStringStream = new MemoryStream(Definition.ScriptStrings);
            scriptStringReader = new BinaryReader(scriptStringStream);
            PerformCleanup = cleanup;
            Indent = spaces ? "    " : "	";
        }

        public void DecompileScripts(TextWriter scriptWriter)
        {
            if (Cache.Version >= CacheVersion.HaloReach)
            {
                foreach (var script in Definition.Scripts)
                    script.ScriptName = Cache.StringTable.GetString(script.ScriptNameReach);
            }

            ParseScripts();

            using (var indentWriter = new IndentedTextWriter(scriptWriter, Indent))
            {
                indentWriter.Indent = 0;

                indentWriter.WriteLine($"; TagTool Script Decompiler");
                indentWriter.WriteLine($"; {Tag}");
                indentWriter.WriteLine($"; {Cache.Version},{Cache.Platform} - {Cache.DisplayName}");
                indentWriter.WriteLine();
                indentWriter.WriteLine();

                //
                // Export scenario script globals
                //

                if (Definition.Globals.Count > 0)
                {
                    indentWriter.WriteLine("; Globals");
                }

                for (var g = 0; g < Definition.Globals.Count; g++)
                {
                    var scriptGlobal = Definition.Globals[g];
                    indentWriter.Write($"(global {GetHsTypeAsString(Cache.Version, scriptGlobal.Type).ToLower()} {scriptGlobal.Name} ");

                    WriteExpression(Globals[g], indentWriter);

                    indentWriter.WriteLine(')');
                }

                indentWriter.WriteLine();

                //
                // Export Externals
                //

                if (Definition.Scripts.Exists(s => s.Type == HsScriptType.Extern))
                {
                    indentWriter.WriteLine("; Externs");
                }

                for (var s = 0; s < Definition.Scripts.Count; s++)
                {
                    var script = Definition.Scripts[s];
                    if (script.Type != HsScriptType.Extern)
                        continue;

                    indentWriter.Write($"(script {script.Type.ToString().ToLower()} {GetHsTypeAsString(Cache.Version, script.ReturnType).ToLower()} ");

                    if (script.Parameters.Count == 0)
                    {
                        indentWriter.Write(script.ScriptName);
                    }
                    else
                    {
                        indentWriter.Write($"({script.ScriptName}");

                        foreach (var parameter in script.Parameters)
                        {
                            indentWriter.Write($" ({GetHsTypeAsString(Cache.Version, parameter.Type).ToLower()} {parameter.Name})");
                        }
                        indentWriter.Write(')');
                    }

                    indentWriter.Indent++;
                    WriteScript(Scripts[s], indentWriter);
                    indentWriter.Indent--;

                    if (script != Definition.Scripts.Last())
                    {
                        indentWriter.WriteLine(')');
                    }
                    else
                        indentWriter.Write(')');
                }

                indentWriter.WriteLine();

                //
                // Export scenario scripts
                //

                indentWriter.WriteLine("; Scripts");
                for (var s = 0; s < Definition.Scripts.Count; s++)
                {
                    var script = Definition.Scripts[s];
                    if (script.Type == HsScriptType.Extern)
                        continue;

                    indentWriter.Write($"(script {script.Type.ToString().ToLower()} {GetHsTypeAsString(Cache.Version, script.ReturnType).ToLower()} ");

                    if (script.Parameters.Count == 0)
                    {
                        indentWriter.WriteLine(script.ScriptName);
                    }
                    else
                    {
                        indentWriter.Write($"({script.ScriptName}");

                        foreach (var parameter in script.Parameters)
                        {
                            indentWriter.Write($" ({GetHsTypeAsString(Cache.Version, parameter.Type).ToLower()} {parameter.Name})");
                        }
                        indentWriter.WriteLine(')');
                    }

                    indentWriter.Indent++;
                    WriteScript(Scripts[s], indentWriter);
                    indentWriter.Indent--;

                    if (script != Definition.Scripts.Last())
                    {
                        indentWriter.WriteLine(')');
                        indentWriter.WriteLine();
                    }
                    else
                        indentWriter.Write(')');
                }
            }
        }

        private void ParseScripts()
        {
            foreach (var scriptGlobal in Definition.Globals)
            {
                var exprIndex = scriptGlobal.InitializationExpressionHandle.Index;
                Globals.Add(ParseExpression(exprIndex));
            }
            foreach (var script in Definition.Scripts)
            {
                var exprIndex = script.RootExpressionHandle.Index;
                Scripts.Add(ParseExpression(exprIndex));
            }
        }

        private void WriteScript(GenericExpression expr, IndentedTextWriter indentWriter)
        {
            for (var i = 1; i < expr.ChildExpressions.Count; i++)
            {
                WriteExpression(expr.ChildExpressions[i], indentWriter);
                //multiline groups make their own newlines
                if (expr.ChildExpressions[i].Type != GenericExpression.ExpressionType.MultilineGroup)
                    indentWriter.WriteLine();
            }
        }

        private void WriteExpression(GenericExpression expr, IndentedTextWriter indentWriter)
        {
            switch (expr.Type)
            {
                case GenericExpression.ExpressionType.Group:
                case GenericExpression.ExpressionType.ScriptReference:
                    WriteGroupExpression(expr, indentWriter);
                    break;
                case GenericExpression.ExpressionType.MultilineGroup:
                    if (expr.Opcode == GetOpcode("cond") && PerformCleanup)
                        WriteCondExpression(expr, indentWriter);
                    else
                        WriteMultiLineGroupExpression(expr, indentWriter);
                    break;
                case GenericExpression.ExpressionType.Value:
                    indentWriter.Write(expr.Name);
                    break;
            }
        }

        private void WriteGroupExpression(GenericExpression expr, IndentedTextWriter indentWriter)
        {
            indentWriter.Write('(');
            for (var i = 0; i < expr.ChildExpressions.Count; i++)
            {
                WriteExpression(expr.ChildExpressions[i], indentWriter);
                if (i < expr.ChildExpressions.Count - 1)
                    indentWriter.Write(' ');
            }
            indentWriter.Write(')');
        }

        private void WriteMultiLineGroupExpression(GenericExpression expr, IndentedTextWriter indentWriter)
        {
            indentWriter.Write('(');
            WriteExpression(expr.ChildExpressions[0], indentWriter);
            indentWriter.Indent++;

            //if statements have first member on the same line
            if (expr.Opcode == GetOpcode("if"))
                indentWriter.Write(' ');
            else
                indentWriter.WriteLine();

            for (var i = 1; i < expr.ChildExpressions.Count; i++)
            {
                WriteExpression(expr.ChildExpressions[i], indentWriter);
                //multiline groups make their own newlines
                if (expr.ChildExpressions[i].Type != GenericExpression.ExpressionType.MultilineGroup)
                    indentWriter.WriteLine();
            }
            indentWriter.Indent--;
            indentWriter.WriteLine(')');
        }

        private void WriteCondExpression(GenericExpression expr, IndentedTextWriter indentWriter)
        {
            indentWriter.WriteLine("(cond");
            indentWriter.Indent++;

            List<GenericExpression> conditions = GetConditions(expr);

            foreach (var condition in conditions)
            {
                GenericExpression caseLabel = condition.ChildExpressions[0];
                GenericExpression caseBody = condition.ChildExpressions[1];

                indentWriter.Write('(');
                WriteExpression(caseLabel, indentWriter);
                indentWriter.Indent++;
                indentWriter.WriteLine();
                foreach (var line in caseBody.ChildExpressions.Skip(1))
                {
                    WriteExpression(line, indentWriter);
                    if (line.Type != GenericExpression.ExpressionType.MultilineGroup)
                        indentWriter.WriteLine();
                }
                indentWriter.Indent--;
                indentWriter.WriteLine(')');
            }

            indentWriter.Indent--;
            indentWriter.WriteLine(')');
        }

        private List<GenericExpression> GetConditions(GenericExpression expr)
        {
            List<GenericExpression> conditions = new List<GenericExpression>();
            GenericExpression nextCond = expr;
            GenericExpression child = nextCond.ChildExpressions.Last();

            while (child is not null)
            {
                nextCond.ChildExpressions = nextCond.ChildExpressions[1..^1];
                conditions.Add(nextCond);
                nextCond = child;

                child = nextCond.ChildExpressions.LastOrDefault();
            }

            return conditions;
        }

        private string ReadScriptString(BinaryReader reader, long address)
        {
            var result = "";

            reader.BaseStream.Position = address;
            for (char c; (c = reader.ReadChar()) != 0x00; result += c) ;

            return result;
        }

        private string OpcodeLookup(ushort opcode)
        {
            string result = "unk_op";

            if (Cache.ScriptDefinitions.Scripts.TryGetValue(opcode, out ScriptInfo info))
                return info.Name;

            return result;
        }

        private ushort GetOpcode(string name)
        {
            foreach (var pair in Cache.ScriptDefinitions.Scripts)
            {
                if (pair.Value.Name == name)
                    return (ushort)pair.Key;
            }
            return ushort.MaxValue;
        }

        private bool IsCond(HsSyntaxNode expr)
        {
            if (expr.Opcode == GetOpcode("cond"))
                return true;

            if (expr.Opcode == GetOpcode("if") && expr.LineNumber == 0)
                return true;

            return false;
        }

        private GenericExpression ParseValueExpression(int exprIndex)
        {
            var expr = Definition.ScriptExpressions[exprIndex];
            GenericExpression result = new GenericExpression { Type = GenericExpression.ExpressionType.Value };

            var valueType = GetHsTypeAsString(Cache.Version, expr.ValueType);
            switch (valueType)
            {
                case "FunctionName":
                    result.Name = expr.StringAddress == 0 ? OpcodeLookup(expr.Opcode) : ReadScriptString(scriptStringReader, expr.StringAddress);
                    break; //Trust the string table, its faster than going through the dictionary with OpcodeLookup.

                case "Boolean":
                    result.Name = expr.Data[0] == 0 ? "false" : "true";
                    break;

                case "Real":
                    float real = BitConverter.ToSingle(SortExpressionDataArray(Cache.Endianness, expr.Data, 4), 0);
                    result.Name = real.ToString("0.0############", CultureInfo.InvariantCulture);
                    break;

                case "Short":
                    result.Name = BitConverter.ToInt16(SortExpressionDataArray(Cache.Endianness, expr.Data, 2), 0).ToString();
                    break;

                case "Long":
                    result.Name = BitConverter.ToInt32(SortExpressionDataArray(Cache.Endianness, expr.Data, 4), 0).ToString();
                    break;
                case "String":
                    result.Name = expr.StringAddress == 0 ? "\"\"" : $"\"{ReadScriptString(scriptStringReader, expr.StringAddress)}\"";
                    break;

                case "Script":
                    result.Name = Definition.Scripts[BitConverter.ToInt16(SortExpressionDataArray(Cache.Endianness, expr.Data, 2), 0)].ScriptName;
                    break;

                case "StringId":
                    result.Name = $"\"{Cache.StringTable.GetString(new StringId(BitConverter.ToUInt32(SortExpressionDataArray(Cache.Endianness, expr.Data, 4), 0)))}\"";
                    break;

                case "GameDifficulty":
                    switch (BitConverter.ToInt16(SortExpressionDataArray(Cache.Endianness, expr.Data, 2), 0))
                    {
                        case 0: result.Name = "easy"; break;
                        case 1: result.Name = "normal"; break;
                        case 2: result.Name = "heroic"; break;
                        case 3: result.Name = "legendary"; break;
                        default: throw new NotImplementedException();
                    }
                    break;

                case "CinematicLightprobe":
                    result.Name = expr.StringAddress == 0 ? "none" : $"{ReadScriptString(scriptStringReader, expr.StringAddress)}";
                    break;
                case "Folder":
                case "Unit":
                case "AnimationGraph":
                case "Object":
                case "Device":
                case "CutsceneCameraPoint":
                case "CutsceneFlag":
                case "TriggerVolume":
                case "UnitSeatMapping":
                case "Vehicle":
                case "VehicleName":
                case "Effect":
                case "Sound":
                case "LoopingSound":
                case "AnyTag":
                case "ObjectName":
                case "Scenery":
                case "Ai":
                case "PointReference":
                case "ObjectDefinition":
                case "CutsceneTitle":
                case "ZoneSet":
                case "Damage":
                case "StartingProfile":
                case "ObjectList":
                case "MpTeam":
                case "DeviceGroup":
                    result.Name = expr.StringAddress == 0 ? "none" : $"\"{ReadScriptString(scriptStringReader, expr.StringAddress)}\"";
                    break;

                case "Team":
                case "AiCommandScript":
                case "AiLine":
                    result.Name = expr.StringAddress == 0 ? "none" : $"\"{ReadScriptString(scriptStringReader, expr.StringAddress)}\"";
                    break;

                case "Void":
                    break;
                default:
                    result.Name = $"<UNIMPLEMENTED VALUE: {expr.Flags.ToString()} {valueType}>";
                    break;
            }

            return result;
        }

        private GenericExpression ParseGroupExpression(int exprIndex)
        {
            GenericExpression result = new GenericExpression();
            var expr = Definition.ScriptExpressions[exprIndex];
            result.Opcode = expr.Opcode;
            switch (expr.Flags)
            {
                case HsSyntaxNodeFlags.Group:
                    result.Type = GenericExpression.ExpressionType.Group;
                    if (expr.Opcode <= GetOpcode("or") && expr.Opcode != GetOpcode("set"))
                        result.Type = GenericExpression.ExpressionType.MultilineGroup;
                    result.Name = OpcodeLookup(expr.Opcode);
                    break;
                case HsSyntaxNodeFlags.ExternReference:
                    result.Type = GenericExpression.ExpressionType.ScriptReference;
                    break;
                case HsSyntaxNodeFlags.ScriptReference:
                    result.Type = GenericExpression.ExpressionType.ScriptReference;
                    result.Name = Definition.Scripts[expr.Opcode].ScriptName;
                    break;
            }

            //This is the first expression within the group
            int nextIndex = GetGroupStartExpressionIndex(exprIndex);

            List<int> IndexHistory = new List<int> { nextIndex };
            while (nextIndex != -1)
            {
                result.ChildExpressions.Add(ParseExpression(nextIndex));
                nextIndex = GetNextExpressionIndex(nextIndex);
                IndexHistory.Add(nextIndex);
            }

            if (PerformCleanup && expr.LineNumber == 0)
            {
                if (expr.Opcode == GetOpcode("begin"))
                    result.Opcode = -1;

                if (IsCond(expr))
                {
                    result.Opcode = GetOpcode("cond");
                    result.Name = "cond";
                }
            }

            return result;
        }

        private GenericExpression ParseExpression(int exprIndex)
        {
            var expr = Definition.ScriptExpressions[exprIndex];

            switch (expr.Flags)
            {
                case HsSyntaxNodeFlags.ExternReference:
                case HsSyntaxNodeFlags.ScriptReference:
                case HsSyntaxNodeFlags.Group:
                    return ParseGroupExpression(exprIndex);

                case HsSyntaxNodeFlags.ExternExpression:
                case HsSyntaxNodeFlags.Expression:
                    return ParseValueExpression(exprIndex);

                case HsSyntaxNodeFlags.GlobalsReference:
                case HsSyntaxNodeFlags.ParameterReference:
                    return new GenericExpression
                    {
                        Name = expr.StringAddress == 0 ? "none" : ReadScriptString(scriptStringReader, expr.StringAddress),
                        Type = GenericExpression.ExpressionType.Value,
                        Opcode = expr.Opcode
                    };
                default:
                    Log.Error($"<UNIMPLEMENTED EXPR: {expr.Flags.ToString()} {GetHsTypeAsString(Cache.Version, expr.ValueType)}>");
                    return new GenericExpression();
            }
        }

        private string GetHsTypeAsString(CacheVersion version, HsType type)
        {
            return type.ToString();
        }

        private int GetGroupStartExpressionIndex(int exprIndex)
        {
            if (exprIndex < 0 || exprIndex >= Definition.ScriptExpressions.Count)
                return -1;
            var expr = Definition.ScriptExpressions[exprIndex];
            int result = -1;
            if (expr.Flags == HsSyntaxNodeFlags.Group || expr.Flags == HsSyntaxNodeFlags.ScriptReference || expr.Flags == HsSyntaxNodeFlags.ExternReference)
                switch (Cache.Endianness)
                {
                    case EndianFormat.LittleEndian:
                        result = BitConverter.ToInt16(new byte[] { expr.Data[0], expr.Data[1] }, 0);
                        return SanitizeExpressionIndex(result);
                    default: //big endian
                        result = BitConverter.ToInt16(new byte[] { expr.Data[3], expr.Data[2] }, 0);
                        return SanitizeExpressionIndex(result);
                }
            else
                return -1;
        }

        private int GetNextExpressionIndex(int exprIndex)
        {
            if (exprIndex < 0 || exprIndex >= Definition.ScriptExpressions.Count)
                return -1;
            var expr = Definition.ScriptExpressions[exprIndex];
            return SanitizeExpressionIndex(expr.NextExpressionHandle.Index);
        }

        private int SanitizeExpressionIndex(int exprIndex)
        {
            return (exprIndex & 0xFFFF) == 0xFFFF ? -1 : exprIndex & 0xFFFF;
        }

        private byte[] SortExpressionDataArray(EndianFormat format, byte[] data, int dataLength)
        {
            if (format == EndianFormat.BigEndian)
            {
                byte[] newData = new byte[dataLength];

                // reverse the data array, but only to the specified length
                for (int i = 0; i < dataLength; i++)
                    newData[i] = data[(dataLength - 1) - i];

                return newData;
            }

            return data;
        }

    }

    public class GenericExpression
    {
        public string Name = "";
        public int Opcode = -1;
        public ExpressionType Type = ExpressionType.Invalid;
        public List<GenericExpression> ChildExpressions = new List<GenericExpression>();

        public enum ExpressionType
        {
            Invalid,
            Group,
            MultilineGroup,
            ScriptReference,
            Value
        }
    }
}
