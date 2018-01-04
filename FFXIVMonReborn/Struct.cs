using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace FFXIVMonReborn
{
    class Struct
    {
        internal enum TypePrintMode
        {
            Raw,
            ObjectToString,
            Char
        }

        private static readonly Dictionary<string, Tuple<Type, int, TypePrintMode, string>> _dataTypeDictionary = new Dictionary<string, Tuple<Type, int, TypePrintMode, string>>
        {
            // Name -               (C# Type - Length - Print Mode - IDA Compatible Type)

            // Base Types
            { "uint8_t",  new Tuple<Type, int, TypePrintMode, string>(typeof(byte), 1, TypePrintMode.ObjectToString, "") },
            { "uint16_t", new Tuple<Type, int, TypePrintMode, string>(typeof(UInt16), 2, TypePrintMode.ObjectToString, "") },
            { "uint32_t", new Tuple<Type, int, TypePrintMode, string>(typeof(UInt32), 4, TypePrintMode.ObjectToString, "") },
            { "uint64_t", new Tuple<Type, int, TypePrintMode, string>(typeof(UInt64), 8, TypePrintMode.ObjectToString, "") },
            { "char",     new Tuple<Type, int, TypePrintMode, string>(typeof(byte), 1, TypePrintMode.Char, "") },
            { "float",     new Tuple<Type, int, TypePrintMode, string>(typeof(float), 4, TypePrintMode.ObjectToString, "") },

            //Sapphire Types
            { "Common::StatusEffect", new Tuple<Type, int, TypePrintMode, string>(null, 12, TypePrintMode.Raw, "") },
            { "Common::FFXIVARR_POSITION3", new Tuple<Type, int, TypePrintMode, string>(null, 12, TypePrintMode.Raw, "") }, //TODO: Special handling for this?
            { "Common::SkillType", new Tuple<Type, int, TypePrintMode, string>(typeof(byte), 12, TypePrintMode.ObjectToString, "") },
        };

        private Dictionary<string, List<StructParseDirective>> _nestedStructDictionary = new Dictionary<string, List<StructParseDirective>>();

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

                Debug.WriteLine(structText);

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

                                    output.AddRange(ParseCNestedArray(currentNestedStruct, reader, line, currentNestedStructName,
                                        ref debugMsg));

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
                                    aryItems = ParseCArray(dataType, reader, ref item, name, ref debugMsg);

                                output.Add(item);

                                try
                                {
                                    ((IDictionary<String, Object>)exobj).Add(item.NameCol, int.Parse(item.ValueCol));
                                }
                                catch (Exception) { } //temporary fix till i sort this out

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

                if(debugMsg.Contains("No info for native type"))
                    new ExtendedErrorView($"[Struct] Struct parsed, but there were unknown types. Please add them in Struct.cs.", debugMsg, "Error", WindowStartupLocation.CenterScreen).ShowDialog();

                return new Tuple<StructListItem[], ExpandoObject>(output.ToArray(), exobj);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw new Exception($"\nBase Exception:\n{e}\n\nTrace:\n{debugMsg}\n\nStruct:\n{structText}");
            }
        }

        private StructListItem[] ParseCNestedArray(List<StructParseDirective> nestedStruct, BinaryReader reader,
            string name, string structName, ref string debugMsg)
        {
            List<StructListItem> output = new List<StructListItem>();

            int count = int.Parse(name.SubstringBetweenIndexes(name.IndexOf("[") + 1, name.LastIndexOf("]")));

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

                    ParseCType(item.DataTypeCol, reader, ref item, ref debugMsg);
                    
                    output.Add(item);
                }
            }

            return output.ToArray();
        }

        private StructListItem[] ParseCArray(string dataType, BinaryReader reader, ref StructListItem item, string name, ref string debugMsg)
        {
            List<StructListItem> output = new List<StructListItem>();
            
            int count = int.Parse(name.SubstringBetweenIndexes(name.IndexOf("[") + 1, name.LastIndexOf("]")));

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
        private void ParseCType(string dataType, BinaryReader reader, ref StructListItem item, ref string debugMsg)
        {
            Tuple<Type, int, TypePrintMode, string> type;
            if (_dataTypeDictionary.TryGetValue(dataType, out type))
            {
                byte[] data = reader.ReadBytes(type.Item2);
                item.typeLength = type.Item2;

                switch (type.Item3)
                {
                    case TypePrintMode.ObjectToString:
                        var value = data.GetValueByType(type.Item1, 0);
                        item.ValueCol = value.ToString();
                        break;
                    case TypePrintMode.Char:
                        item.ValueCol = Encoding.ASCII.GetString(data);
                        break;
                    case TypePrintMode.Raw:
                        item.ValueCol = data.ToHexString();
                        break;;
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
        public bool IsVisible { get; set; } = true;
    }
}
