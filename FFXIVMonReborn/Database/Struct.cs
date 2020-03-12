using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using FFXIVMonReborn.Database.DataTypes;

namespace FFXIVMonReborn.Database
{

    //TODO: Clean this up and make it a bit faster
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

            { "float",    new Tuple<Type, int, TypePrintMode, string>(typeof(float), 4, TypePrintMode.ObjectToString, "") },

            //Sapphire Common Types
            { "Common::StatusEffect", new Tuple<Type, int, TypePrintMode, string>(null, 12, TypePrintMode.Raw, "") },
            { "Common::FFXIVARR_POSITION3", new Tuple<Type, int, TypePrintMode, string>(typeof(FfxivArrPosition3DataType), 12, TypePrintMode.CustomDataType, "") },
            { "Common::SkillType", new Tuple<Type, int, TypePrintMode, string>(typeof(byte), sizeof(byte), TypePrintMode.ObjectToString, "") },
            
            // Types in IPC (TODO: Parse?)
            { "effectEntry",  new Tuple<Type, int, TypePrintMode, string>(null, 8, TypePrintMode.Raw, "") }, //used in FFXIVIpcEffect
            { "EffectEntry",  new Tuple<Type, int, TypePrintMode, string>(null, 8, TypePrintMode.Raw, "") }, //used in FFXIVIpcEffect
            { "PlayerEntry",  new Tuple<Type, int, TypePrintMode, string>(null, 88, TypePrintMode.Raw, "") }, //used in FFXIVIpcSocialList
        };

        public static readonly Dictionary<string, System.Drawing.Color> TypeColours = new Dictionary<string, System.Drawing.Color>
        {
            { "uint8_t", System.Drawing.Color.FromArgb(0xab, 0xc8, 0xf4) },
            { "uint16_t", System.Drawing.Color.FromArgb(0xd7, 0x89, 0x8c) },
            { "uint32_t", System.Drawing.Color.FromArgb(0x89, 0xd7, 0xb7) },
            { "uint64_t", System.Drawing.Color.FromArgb(0x89, 0xd7, 0xd7) },
            { "char", System.Drawing.Color.FromArgb(0x7b, 0xc8, 0xf4) },
            { "float", System.Drawing.Color.FromArgb(0x7f, 0xc0, 0xc0) },
        };

        private readonly Dictionary<string, List<StructParseDirective>> _nestedStructDictionary = new Dictionary<string, List<StructParseDirective>>();

        public Tuple<StructListItem[], System.Dynamic.ExpandoObject> Parse(string structText, byte[] packet)
        {
            string debugMsg = "";
            try
            {
                // Get rid of any comments
                Regex r = new Regex("\\/\\*(.*)\\*\\/");
                structText = r.Replace(structText, "");
                r = new Regex("\\/\\/(.*)");
                structText = r.Replace(structText, "");

                List<StructListItem> output = new List<StructListItem>();
                ExpandoObject exobj = new ExpandoObject();

                var lines = Regex.Split(structText, "\r\n|\r|\n");
                int at = 3;

                List<StructParseDirective> currentNestedStruct = null;
                string currentNestedStructName = null;

                using (MemoryStream stream = new MemoryStream(packet))
                {
                    stream.Position = 0x20;
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        while (at != lines.Length - 1)
                        {
                            StructListItem item = new StructListItem();

                            var line = lines[at];

                            if (String.IsNullOrEmpty(line))
                            {
                                debugMsg += $"Line {at} is empty\n";
                                at++;
                                continue;
                            }

                            int pos = 0;
                            while (!Char.IsLetter(line[pos]))
                            {
                                ++pos;
                            }

                            if (currentNestedStructName != null)
                            {
                                if (line.Contains("}"))
                                {
                                    _nestedStructDictionary.Add(currentNestedStructName, currentNestedStruct);
                                    debugMsg += $"Finished nested struct:{currentNestedStructName} - {currentNestedStruct.Count} Entries\n\n";

                                    var aryItems = ParseCNestedArray(currentNestedStruct, reader, line, currentNestedStructName, ref exobj
                                        , ref debugMsg);
                                    
                                    output.AddRange(aryItems);
                                    
                                    List<Object> values = new List<object>();
                                    foreach (var aryItem in aryItems)
                                    {
                                        values.Add(aryItem.RawValue);   
                                    }
                                    
                                    ((IDictionary<String, Object>) exobj).Add(Regex.Replace(aryItems[0].NameCol, "(\\[.*\\])|(\".*\")|('.*')|(\\(.*\\))", ""), values.ToArray());

                                    currentNestedStructName = null;
                                    currentNestedStruct = null;
                                    at++;
                                    continue;
                                }
                            }

                            string dataType = "";

                            while (line[pos] != ' ')
                            {
                                dataType += line[pos];
                                pos++;
                            }

                            if (dataType == "struct")
                            {
                                string structName = "";

                                pos++;
                                while (pos < line.Length)
                                {
                                    structName += line[pos];
                                    pos++;
                                }
                                currentNestedStruct = new List<StructParseDirective>();
                                currentNestedStructName = structName;

                                debugMsg += $"Start nested struct parse of {currentNestedStructName}\n";
                                at += 2;
                                continue;
                            }

                            item.DataTypeCol = dataType;

                            pos++;

                            string name = "";

                            while (line[pos] != ';')
                            {
                                name += line[pos];
                                pos++;
                            }

                            debugMsg += $"Expected:{name} - {dataType} - {line} - {at}\n";

                            item.NameCol = name;

                            item.offset = stream.Position;
                            item.OffsetCol = stream.Position.ToString("X");

                            if (currentNestedStructName == null)
                            {
                                StructListItem[] aryItems = null;

                                if (!name.EndsWith("]"))
                                    ParseCType(dataType, reader, ref item, ref debugMsg);
                                else
                                    aryItems = ParseCArray(dataType, reader, ref item, name, ref exobj, ref debugMsg);

                                output.Add(item);

                                if (aryItems == null)
                                    ((IDictionary<String, Object>) exobj).Add(item.NameCol, item.RawValue);
                                else
                                {
                                    List<Object> values = new List<object>();
                                    foreach (var aryItem in aryItems)
                                    {
                                        values.Add(aryItem.RawValue);
                                    }
                                    
                                    ((IDictionary<String, Object>) exobj).Add(Regex.Replace(aryItems[0].NameCol, "(\\[.*\\])|(\".*\")|('.*')|(\\(.*\\))", ""), values.ToArray());
                                }

                                debugMsg += $"Parsed:{item.NameCol} - {item.OffsetCol} - {item.DataTypeCol} - {item.ValueCol}\n\n";

                                if (aryItems != null)
                                    output.AddRange(aryItems);

                            }
                            else
                            {
                                currentNestedStruct.Add(new StructParseDirective { ArrayCount = 0, Name = item.NameCol, DataType = item.DataTypeCol});
                                debugMsg += $"Added to nested:{item.NameCol} - {item.DataTypeCol}\n\n";
                            }
                            at++;
                        }
                    }
                }

                LogView.Instance?.WriteLine($"[Struct] Struct parsed, but there were unknown types. Please add them in Struct.cs. {debugMsg}");

                return new Tuple<StructListItem[], ExpandoObject>(output.ToArray(), exobj);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new Exception($"\nBase Exception:\n{e}\n\nTrace:\n{debugMsg}\n\nStruct:\n{structText}");
            }
        }

        private StructListItem[] ParseCNestedArray(List<StructParseDirective> nestedStruct, BinaryReader reader,
            string name, string structName, ref ExpandoObject exobj, ref string debugMsg)
        {
            List<StructListItem> output = new List<StructListItem>();

            int count;
            if(name.SubstringBetweenIndexes(name.IndexOf("[") + 1, name.LastIndexOf("]")).Contains("0x"))
                count = int.Parse(name.SubstringBetweenIndexes(name.IndexOf("[") + 3, name.LastIndexOf("]")), NumberStyles.HexNumber);
            else
                count = int.Parse(name.SubstringBetweenIndexes(name.IndexOf("[") + 1, name.LastIndexOf("]")));

            debugMsg += $"Nested Array Start - {name} - {count}\n";

            output.Add(new StructListItem{DataTypeCol = structName, NameCol = name.Replace("}", "").Replace(";", "").Replace(" ", ""), OffsetCol = reader.BaseStream.Position.ToString()});

            for (int i = 0; i < count; i++)
            {
                output.Add(new StructListItem { DataTypeCol = structName, NameCol = $"  {structName}[{i}]", OffsetCol = reader.BaseStream.Position.ToString() });
                foreach (var directive in nestedStruct)
                {
                    StructListItem item = new StructListItem();
                    item.NameCol = "    ->" + directive.Name;
                    item.DataTypeCol = directive.DataType;
                    item.OffsetCol = reader.BaseStream.Position.ToString();
                    item.offset = reader.BaseStream.Position;

                    StructListItem[] aryItems = null;
                    
                    //TODO: All of this is fucked and should be redone also this isn't gonna be in the expando object i'm pretty sure
                    if (!item.NameCol.EndsWith("]"))
                        ParseCType(item.DataTypeCol, reader, ref item, ref debugMsg);
                    else
                        aryItems = ParseCArray(item.DataTypeCol, reader, ref item, item.NameCol, ref exobj, ref debugMsg);
                    
                    output.Add(item);
                    
                    if(aryItems != null)
                        output.AddRange(aryItems);
                }
            }

            return output.ToArray();
        }

        private StructListItem[] ParseCArray(string dataType, BinaryReader reader, ref StructListItem item, string name, ref ExpandoObject exobj, ref string debugMsg)
        {
            List<StructListItem> output = new List<StructListItem>();

            int count;
            if (name.SubstringBetweenIndexes(name.IndexOf("[") + 1, name.LastIndexOf("]")).Contains("0x"))
                count = int.Parse(name.SubstringBetweenIndexes(name.IndexOf("[") + 3, name.LastIndexOf("]")), NumberStyles.HexNumber);
            else
                count = int.Parse(name.SubstringBetweenIndexes(name.IndexOf("[") + 1, name.LastIndexOf("]")));

            debugMsg += $"Array Start - {name} - {count} - {dataType}\n";

            for (int i = 0; i < count; i++)
            {
                StructListItem aryItem = new StructListItem();
                aryItem.NameCol = "  " + name.SubstringBetweenIndexes(0, name.IndexOf("[")) + $"[{i}]";
                aryItem.offset = reader.BaseStream.Position;
                aryItem.OffsetCol = reader.BaseStream.Position.ToString("X");
                
                ParseCType(dataType, reader, ref aryItem, ref debugMsg);
                
                output.Add(aryItem);

                debugMsg += $"  ->{aryItem.NameCol} - {aryItem.OffsetCol} - {aryItem.DataTypeCol} - {aryItem.ValueCol}\n";
            }

            return output.ToArray();
        }

        /// <summary>
        /// Parse value as string and it's lenght to a StructListItem
        /// </summary>
        public void ParseCType(string dataType, BinaryReader reader, ref StructListItem item, ref string debugMsg)
        {
            Tuple<Type, int, TypePrintMode, string> type;
            if (DataTypeDictionary.TryGetValue(dataType, out type))
            {
                byte[] data = reader.ReadBytes(type.Item2);
                item.typeLength = type.Item2;

                switch (type.Item3)
                {
                    case TypePrintMode.CustomDataType:
                    {
                        var value = (CustomDataType) Activator.CreateInstance(type.Item1);
                        value.Parse(data);
                        item.ValueCol = value.ToString();
                        item.RawValue = value;
                    }
                        break;
                    case TypePrintMode.ObjectToString:
                    {
                        var value = data.GetValueByType(type.Item1, 0);
                        item.ValueCol = value.ToString();
                        item.RawValue = value;
                    }
                        break;
                    case TypePrintMode.Char:
                        item.ValueCol = Encoding.ASCII.GetString(data);
                        item.RawValue = (char)data[0];
                        break;
                    case TypePrintMode.Raw:
                        item.ValueCol = data.ToHexString();
                        item.RawValue = data;
                        break;
                }
            }
            else
            {
                debugMsg += $"No info for native type: {dataType}. Please add this type in Struct.cs.\n\n";
            }
        }

        internal class StructParseDirective
        {
            public string Name { get; set; }
            public string DataType { get; set; }
            public int ArrayCount { get; set; }
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
        public object RawValue { get; set; }
        public bool IsVisible { get; set; } = true;
    }
}
