using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FFXIVMonReborn
{
    class Struct
    {
        public static StructListItem[] Parse(string data, byte[] packet)
        {
            // Get rid of any comments
            Regex r = new Regex("\\/\\*(.*)\\*\\/");
            data = r.Replace(data,"");
            r = new Regex("\\/\\/(.*)");
            data = r.Replace(data, "");

            Debug.WriteLine(data);

            List<StructListItem> output = new List<StructListItem>();

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

                        Debug.WriteLine(name);
                        item.NameCol = name;

                        item.offset = stream.Position;
                        item.OffsetCol = stream.Position.ToString("X");

                        switch (dataType)
                        {
                            case "uint8_t":
                                item.ValueCol = reader.ReadByte().ToString();
                                item.typeLength = 1;
                                break;
                            case "uint16_t":
                                item.ValueCol = reader.ReadUInt16().ToString();
                                item.typeLength = 2;
                                break;
                            case "uint32_t":
                                item.ValueCol = reader.ReadUInt32().ToString();
                                item.typeLength = 4;
                                break;
                            case "uint64_t":
                                item.ValueCol = reader.ReadUInt64().ToString();
                                item.typeLength = 8;
                                break;
                        }

                        output.Add(item);

                        Debug.WriteLine($"{item.NameCol} - {item.OffsetCol} - {item.DataTypeCol} - {item.ValueCol}");

                        at++;
                    }
                }
            }

            return output.ToArray();
        }

        private int ResolveCTypeToLength(string type)
        {
            switch (type)
            {
                case "uint16_t":
                    return 2;
                default:
                    return 0;
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
    }
}
