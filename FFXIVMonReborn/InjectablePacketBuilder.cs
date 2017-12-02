using System;
using System.Collections.Generic;
using System.IO;

namespace FFXIVMonReborn
{
    public class InjectablePacketBuilder
    {
        public static byte[] BuildSingle(byte[] buffer)
        {
            byte[] head = new byte[0x18];
            using (MemoryStream stream = new MemoryStream(head))
            {
                stream.WriteByte(0x01);
                
                stream.Position = 0x04;
                stream.Write(BitConverter.GetBytes((UInt16)buffer.Length), 0, 2);

                stream.WriteByte(0x01);
                
                List<byte> output = new List<byte>(buffer);
                output.InsertRange(0, stream.ToArray());
                
                return output.ToArray();
            }
        }
    }
}