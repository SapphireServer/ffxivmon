using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;

namespace FFXIVMonReborn
{
    class Struct
    {
        private static readonly Dictionary<string, Tuple<Type, int, bool, string>> _dataTypeDictionary = new Dictionary<string, Tuple<Type, int, bool, string>>
        {
            // Name -               (C# Type - Length - Print as char - IDA Compatible Type)
            { "uint8_t",  new Tuple<Type, int, bool, string>(typeof(byte), 1, false, "") },
            { "uint16_t", new Tuple<Type, int, bool, string>(typeof(UInt16), 2, false, "") },
            { "uint32_t", new Tuple<Type, int, bool, string>(typeof(UInt32), 4, false, "") },
            { "uint64_t", new Tuple<Type, int, bool, string>(typeof(UInt64), 8, false, "") },
            { "char",     new Tuple<Type, int, bool, string>(typeof(byte), 1, true, "") }
        };

        public static Tuple<StructListItem[], System.Dynamic.ExpandoObject> Parse(string data, byte[] packet)
        {
            string debugMsg = "";
            try
            {
                // Get rid of any comments
                Regex r = new Regex("\\/\\*(.*)\\*\\/");
                data = r.Replace(data, "");
                r = new Regex("\\/\\/(.*)");
                data = r.Replace(data, "");

                Debug.WriteLine(data);

                List<StructListItem> output = new List<StructListItem>();
                ExpandoObject exobj = new ExpandoObject();

                var lines = Regex.Split(data, "\r\n|\r|\n");
                int at = 3;

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

                            string dataType = "";

                            while (line[pos] != ' ')
                            {
                                dataType += line[pos];
                                pos++;
                            }

                            Debug.WriteLine(dataType);
                            item.DataTypeCol = dataType;

                            pos++;

                            string name = "";

                            while (line[pos] != ';')
                            {
                                name += line[pos];
                                pos++;
                            }

                            debugMsg += $"Expected:{name} - {dataType} - {line} - {at}\n";

                            Debug.WriteLine(name);
                            item.NameCol = name;

                            item.offset = stream.Position;
                            item.OffsetCol = stream.Position.ToString("X");

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



                            if (aryItems != null)
                                output.AddRange(aryItems);

                            debugMsg += $"Parsed:{item.NameCol} - {item.OffsetCol} - {item.DataTypeCol} - {item.ValueCol}\n\n";

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
                throw new Exception($"\nBase Exception:\n{e}\n\nTrace:\n{debugMsg}\n\nStruct:\n{data}");
            }
        }

        private static StructListItem[] ParseCArray(string dataType, BinaryReader reader, ref StructListItem item, string name, ref string debugMsg)
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
        private static void ParseCType(string dataType, BinaryReader reader, ref StructListItem item, ref string debugMsg)
        {
            Tuple<Type, int, bool, string> type;
            if (_dataTypeDictionary.TryGetValue(dataType, out type))
            {
                byte[] data = reader.ReadBytes(type.Item2);
                var value = data.GetValueByType(type.Item1, 0);

                item.ValueCol = value.ToString();
                item.typeLength = type.Item2;

                if (type.Item3)
                    item.ValueCol = Encoding.ASCII.GetString(data);
            }
            else
            {
                debugMsg += $"No info for native type: {dataType}. Please add this type in Struct.cs.\n\n";
            }
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
