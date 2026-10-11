using FFXIVMonReborn.Database.DataTypes;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Windows.Media;

namespace FFXIVMonReborn.Database
{
    class Struct
    {
        internal enum TypePrintMode
        {
            Raw,
            ObjectToString,
            CustomDataType,
            Char
        }

        public static readonly Dictionary<string, Tuple<Type, int, TypePrintMode, string>> DataTypeDictionary = new Dictionary<string, Tuple<Type, int, TypePrintMode, string>>
        {
            // Name -               (C# Type - Length - Print Mode - IDA Compatible Type)

            // Base Types
            { "uint8_t",  new Tuple<Type, int, TypePrintMode, string>(typeof(byte),   1, TypePrintMode.ObjectToString, "") },
            { "uint16_t", new Tuple<Type, int, TypePrintMode, string>(typeof(UInt16), 2, TypePrintMode.ObjectToString, "") },
            { "uint32_t", new Tuple<Type, int, TypePrintMode, string>(typeof(UInt32), 4, TypePrintMode.ObjectToString, "") },
            { "uint64_t", new Tuple<Type, int, TypePrintMode, string>(typeof(UInt64), 8, TypePrintMode.ObjectToString, "") },
            { "char",     new Tuple<Type, int, TypePrintMode, string>(typeof(byte),   1, TypePrintMode.Char,           "") },
            { "int8_t",   new Tuple<Type, int, TypePrintMode, string>(typeof(byte),   1, TypePrintMode.Char,           "") },
            { "int16_t",  new Tuple<Type, int, TypePrintMode, string>(typeof(Int16),  2, TypePrintMode.ObjectToString, "") },
            { "int32_t",  new Tuple<Type, int, TypePrintMode, string>(typeof(Int32),  4, TypePrintMode.ObjectToString, "") },
            { "int64_t",  new Tuple<Type, int, TypePrintMode, string>(typeof(Int64),  8, TypePrintMode.ObjectToString, "") },
            { "float",    new Tuple<Type, int, TypePrintMode, string>(typeof(float),  4, TypePrintMode.ObjectToString, "") },
            { "bool",     new Tuple<Type, int, TypePrintMode, string>(typeof(bool),   1, TypePrintMode.ObjectToString, "") },
            
            // custom types
            { "Common::StatusEffect",       new Tuple<Type, int, TypePrintMode, string>(null, 12, TypePrintMode.Raw, "") },
            { "Common::FFXIVARR_POSITION3", new Tuple<Type, int, TypePrintMode, string>(typeof(FfxivArrPosition3DataType), 12, TypePrintMode.CustomDataType, "") },
            { "Common::Vector3",            new Tuple<Type, int, TypePrintMode, string>(typeof(FfxivArrPosition3DataType), 12, TypePrintMode.CustomDataType, "") },
            { "Common::SkillType",          new Tuple<Type, int, TypePrintMode, string>(typeof(byte), 1, TypePrintMode.ObjectToString, "") },
            { "effectEntry",                new Tuple<Type, int, TypePrintMode, string>(null, 8, TypePrintMode.Raw, "") },
            { "EffectEntry",                new Tuple<Type, int, TypePrintMode, string>(null, 8, TypePrintMode.Raw, "") },
            { "PlayerEntry",                new Tuple<Type, int, TypePrintMode, string>(null, 88, TypePrintMode.Raw, "") }
        };

        public static readonly Dictionary<string, Color> TypeColours = new Dictionary<string, Color>
        {
            { "uint8_t",  Color.FromArgb(0xff, 0xab, 0xc8, 0xf4) },
            { "uint16_t", Color.FromArgb(0xff, 0xd7, 0x89, 0x8c) },
            { "uint32_t", Color.FromArgb(0xff, 0x89, 0xd7, 0xb7) },
            { "uint64_t", Color.FromArgb(0xff, 0x89, 0xd7, 0xd7) },
            { "char",     Color.FromArgb(0xff, 0x7b, 0xc8, 0xf4) },
            { "float",    Color.FromArgb(0xff, 0x7f, 0xc0, 0xc0) },
        };

        public Tuple<StructListItem[], ExpandoObject> Parse(string headerText, byte[] packet)
        {
            StringBuilder debugMsg = new StringBuilder();
            List<StructListItem> uiItemsList = new List<StructListItem>();
            ExpandoObject dynamicObject = new ExpandoObject();

            try
            {
                var parser = new SimpleCppParser();
                var definitions = parser.ParseDefinitions(headerText);

                if (definitions.Count == 0)
                    throw new Exception("No valid struct definitions found in header.");

                var mainStruct = definitions.Last();
                debugMsg.AppendLine($"Target Main Struct: {mainStruct.Name}");

                using (MemoryStream stream = new MemoryStream(packet))
                {
                    stream.Position = 0x20; // skip header
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        ReadStruct(reader, mainStruct, definitions, dynamicObject, uiItemsList, debugMsg);
                    }
                }

                return new Tuple<StructListItem[], ExpandoObject>(uiItemsList.ToArray(), dynamicObject);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new Exception($"\nException:\n{e.Message}\n\nTrace:\n{debugMsg}");
            }
        }

        private void ReadStruct(
            BinaryReader reader,
            StructDefinition def,
            List<StructDefinition> allDefs,
            ExpandoObject targetObj,
            List<StructListItem> uiList,
            StringBuilder debug)
        {
            var dict = (IDictionary<string, object>)targetObj;

            foreach (var field in def.Fields)
            {
                long offset = reader.BaseStream.Position;
                string offsetHex = offset.ToString("X");

                if (field.IsArray)
                {
                    var headItem = new StructListItem
                    {
                        NameCol = $"{field.Name}[{field.ArraySize}]",
                        DataTypeCol = field.TypeName,
                        isArrayDeclaration = true,
                        offset = offset,
                        OffsetCol = offsetHex
                    };
                    uiList.Add(headItem);

                    var arrayData = new List<object>();

                    // pull nested struct
                    var nestedDef = allDefs.FirstOrDefault(d => d.Name == field.TypeName);

                    for (int i = 0; i < field.ArraySize; i++)
                    {
                        if (nestedDef != null)
                        {
                            // struct array
                            uiList.Add(new StructListItem { NameCol = $"  [{i}]", DataTypeCol = field.TypeName, offset = reader.BaseStream.Position, OffsetCol = reader.BaseStream.Position.ToString("X") });
                            dynamic nestedObj = new ExpandoObject();
                            ReadStruct(reader, nestedDef, allDefs, nestedObj, uiList, debug);
                            arrayData.Add(nestedObj);
                        }
                        else
                        {
                            // primitive array
                            var itemUi = new StructListItem { NameCol = $"  [{i}]", DataTypeCol = field.TypeName, isArrayElement = true, offset = reader.BaseStream.Position, OffsetCol = reader.BaseStream.Position.ToString("X") };
                            object val = ReadPrimitive(reader, field.TypeName, ref itemUi, debug);
                            uiList.Add(itemUi);
                            arrayData.Add(val);
                        }
                    }

                    // total visual size
                    if (uiList.Count > uiList.IndexOf(headItem) + 1)
                    {
                        int firstItemIndex = uiList.IndexOf(headItem) + 1;
                        headItem.fullArraySize = field.ArraySize * uiList[firstItemIndex].typeLength;
                    }

                    dict[field.Name] = arrayData.ToArray();
                }
                // nested struct
                else if (allDefs.Any(d => d.Name == field.TypeName))
                {
                    var nestedDef = allDefs.First(d => d.Name == field.TypeName);
                    uiList.Add(new StructListItem { NameCol = field.Name, DataTypeCol = field.TypeName, offset = offset, OffsetCol = offsetHex });

                    dynamic nestedObj = new ExpandoObject();
                    ReadStruct(reader, nestedDef, allDefs, nestedObj, uiList, debug);
                    dict[field.Name] = nestedObj;
                }
                // primitive
                else
                {
                    var uiItem = new StructListItem { NameCol = field.Name, DataTypeCol = field.TypeName, offset = offset, OffsetCol = offsetHex };
                    object val = ReadPrimitive(reader, field.TypeName, ref uiItem, debug);
                    uiList.Add(uiItem);
                    dict[field.Name] = val;
                }
            }
        }

        private static object ReadPrimitive(BinaryReader reader, string typeName, ref StructListItem item, StringBuilder debug)
        {
            if (DataTypeDictionary.TryGetValue(typeName, out var meta))
            {
                byte[] bytes = reader.ReadBytes(meta.Item2);
                item.typeLength = meta.Item2;

                object result = null;
                switch (meta.Item3)
                {
                    case TypePrintMode.CustomDataType:
                        var custom = (CustomDataType)Activator.CreateInstance(meta.Item1);
                        custom.Parse(bytes);
                        result = custom;
                        item.ValueCol = custom.ToString();
                        break;
                    case TypePrintMode.ObjectToString:
                        result = bytes.GetValueByType(meta.Item1, 0);
                        item.ValueCol = result.ToString();
                        break;
                    case TypePrintMode.Char:
                        item.ValueCol = Encoding.ASCII.GetString(bytes).Trim('\0');
                        result = (char)bytes[0];
                        break;
                    case TypePrintMode.Raw:
                        item.ValueCol = BitConverter.ToString(bytes).Replace("-", " ");
                        result = bytes;
                        break;
                }
                item.RawValue = result;
                return result;
            }
            else
            {
                debug.AppendLine($"[Warning] Unknown Primitive Type: {typeName}");
                return null;
            }
        }

        private class SimpleCppParser
        {
            public List<StructDefinition> ParseDefinitions(string text)
            {
                var blockComments = @"/\*(.*?)\*/";
                var lineComments = @"//.*$";

                text = Regex.Replace(text, blockComments, "", RegexOptions.Singleline);
                text = Regex.Replace(text, lineComments, "", RegexOptions.Multiline);

                var definitions = new List<StructDefinition>();
                int cursor = 0;
                while (cursor < text.Length)
                {
                    int structIndex = text.IndexOf("struct", cursor);
                    if (structIndex == -1) break;
                    int braceStart = text.IndexOf('{', structIndex);
                    if (braceStart == -1) break;

                    string decl = text.Substring(structIndex + 6, braceStart - (structIndex + 6)).Trim();
                    string structName = decl.Split(new[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();

                    int braceEnd = FindMatchingBrace(text, braceStart);
                    if (braceEnd == -1) break;
                    string body = text.Substring(braceStart + 1, braceEnd - braceStart - 1);
                    var def = new StructDefinition { Name = structName };
                    def.Fields = ParseFields(body);
                    definitions.Add(def);
                    cursor = braceEnd + 1;
                }
                return definitions;
            }

            private static int FindMatchingBrace(string text, int start)
            {
                int depth = 0;
                for (int i = start; i < text.Length; i++)
                {
                    if (text[i] == '{') depth++;
                    else if (text[i] == '}')
                    {
                        depth--;
                        if (depth == 0) return i;
                    }
                }
                return -1;
            }

            private static List<FieldDefinition> ParseFields(string body)
            {
                var fields = new List<FieldDefinition>();
                var lines = body.Split(';');

                foreach (var rawLine in lines)
                {
                    string line = rawLine.Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    var match = Regex.Match(line, @"^(?<type>[\w: *]+?)\s+(?<name>\w+)(?:\[(?<size>.*?)\])?$");
                    if (match.Success)
                    {
                        var field = new FieldDefinition
                        {
                            TypeName = match.Groups["type"].Value.Trim(),
                            Name = match.Groups["name"].Value.Trim(),
                        };
                        if (match.Groups["size"].Success)
                        {
                            field.IsArray = true;
                            string sizeStr = match.Groups["size"].Value.Trim();
                            field.ArraySize = ParseSize(sizeStr);
                        }
                        fields.Add(field);
                    }
                }
                return fields;
            }

            private static int ParseSize(string sizeStr)
            {
                if (sizeStr.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    return int.Parse(sizeStr.Substring(2), System.Globalization.NumberStyles.HexNumber);
                if (int.TryParse(sizeStr, out int val))
                    return val;
                return 0;
            }
        }

        private class FieldDefinition
        {
            public string TypeName;
            public string Name;
            public bool IsArray;
            public int ArraySize;
        }

        private class StructDefinition
        {
            public string Name;
            public List<FieldDefinition> Fields = new List<FieldDefinition>();
        }
    }

    public class StructListItem
    {
        public string DataTypeCol { get; set; }
        public string NameCol { get; set; }
        public string ValueCol { get; set; }
        public string OffsetCol { get; set; }
        public long offset;
        public byte[] dataChunk;
        public int typeLength;
        public int fullArraySize;
        public bool isArrayDeclaration;
        public bool isArrayElement;
        public object RawValue { get; set; }
        public bool IsVisible { get; set; } = true;
    }
}