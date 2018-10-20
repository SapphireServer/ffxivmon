using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FFXIVMonReborn.DataModel;

namespace FFXIVMonReborn.Importers
{
    public class ActLogImporter
    {
        public static PacketEntry[] Import(string path)
        {
            var lines = File.ReadAllLines(path);

            var output = new List<PacketEntry>();

            foreach (var line in lines)
            {
                var parts = line.Split('|');
                
                if(parts[0] != "252")
                    continue;
                
                var packet = new PacketEntry();
                packet.Timestamp = DateTime.Parse(parts[1]).ToString(@"MM\/dd\/yyyy HH:mm:ss.fff tt");

                var bytes = new byte[(parts.Length - 3) * 4];
                for (int i = 2; i < parts.Length - 1; i++)
                {
                    var data = Util.StringToByteArray(parts[i]);
                    Array.Copy(data.Reverse().ToArray(), 0, bytes, (i - 2) * 4, data.Length);
                }

                packet.Data = bytes;

                // ACT Plugin only logs server messages
                packet.Direction = "S";
                
                packet.Message = BitConverter.ToUInt16(packet.Data, 0x12).ToString("X4");
                
                output.Add(packet);
            }

            return output.ToArray();
        }
    }
}