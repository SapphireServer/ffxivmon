using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIVMonReborn
{
    class Util
    {
        public static string ByteArrayToHex(byte[] bytes, int offset = 0, int bytesPerLine = 16)
        {
            if (bytes == null)
            {
                return string.Empty;
            }

            var hexChars = "0123456789ABCDEF".ToCharArray();

            var offsetBlock = 8 + 3;
            var byteBlock = offsetBlock + bytesPerLine * 3 + (bytesPerLine - 1) / 8 + 2;
            var lineLength = byteBlock + bytesPerLine + Environment.NewLine.Length;

            var line = (new string(' ', lineLength - Environment.NewLine.Length) + Environment.NewLine).ToCharArray();
            var numLines = (bytes.Length + bytesPerLine - 1) / bytesPerLine;

            var sb = new StringBuilder(numLines * lineLength);

            for (var i = 0; i < bytes.Length; i += bytesPerLine)
            {
                var h = i + offset;

                line[0] = hexChars[(h >> 28) & 0xF];
                line[1] = hexChars[(h >> 24) & 0xF];
                line[2] = hexChars[(h >> 20) & 0xF];
                line[3] = hexChars[(h >> 16) & 0xF];
                line[4] = hexChars[(h >> 12) & 0xF];
                line[5] = hexChars[(h >> 8) & 0xF];
                line[6] = hexChars[(h >> 4) & 0xF];
                line[7] = hexChars[(h >> 0) & 0xF];

                var hexColumn = offsetBlock;
                var charColumn = byteBlock;

                for (var j = 0; j < bytesPerLine; j++)
                {
                    if (j > 0 && (j & 7) == 0)
                    {
                        hexColumn++;
                    }

                    if (i + j >= bytes.Length)
                    {
                        line[hexColumn] = ' ';
                        line[hexColumn + 1] = ' ';
                        line[charColumn] = ' ';
                    }
                    else
                    {
                        var by = bytes[i + j];
                        line[hexColumn] = hexChars[(by >> 4) & 0xF];
                        line[hexColumn + 1] = hexChars[by & 0xF];
                        line[charColumn] = by < 32 ? '.' : (char)by;
                    }

                    hexColumn += 3;
                    charColumn++;
                }

                sb.Append(line);
            }

            return sb.ToString().TrimEnd(Environment.NewLine.ToCharArray());
        }

        public static DateTime UnixTimeStampToDateTime(double unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddSeconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public static DateTime UnixMSTimeStampToDateTime(long unixTimeStamp)
        {
            // Unix timestamp is seconds past epoch
            System.DateTime dtDateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            dtDateTime = dtDateTime.AddMilliseconds(unixTimeStamp).ToLocalTime();
            return dtDateTime;
        }

        public static string ByteArrayToString(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        public static ActorControlContainer FastParseActorControl(byte[] data)
        {
            var msg = BitConverter.ToUInt32(data, 0x12);
            
            var container = new ActorControlContainer();
            container.Cat = BitConverter.ToUInt16(data, 0x20);
            
            container.Param1 = BitConverter.ToUInt32(data, 0x24);
            container.Param2 = BitConverter.ToUInt32(data, 0x28);
            container.Param3 = BitConverter.ToUInt32(data, 0x2C);
            container.Param4 = BitConverter.ToUInt32(data, 0x30);

            if (msg == 0x143)
            {
                container.Param5 = BitConverter.ToUInt32(data, 0x34);
                container.Param6 = BitConverter.ToUInt32(data, 0x38);
            }

            if (msg == 0x144)
            {
                container.TargetId = BitConverter.ToUInt64(data, 0x38);
            }

            return container;
        }
        
        public class ActorControlContainer
        {
            public uint Cat { get; set; }
            public uint Param1 { get; set; }
            public uint Param2 { get; set; }
            public uint Param3 { get; set; }
            public uint Param4 { get; set; }
            public uint Param5 { get; set; }
            public uint Param6 { get; set; }
            public ulong TargetId { get; set; }
        }

    }
}
