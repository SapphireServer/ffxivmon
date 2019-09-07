using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace FFXIVMonReborn.LobbyEncryption
{
    public static class LobbyKeyHelper
    {
        public static byte[] MakeKey(byte[] initPacket, ushort version)
        {
            var encKey = new byte[0x2C];

            encKey[0] = 0x78;
            encKey[1] = 0x56;
            encKey[2] = 0x34;
            encKey[3] = 0x12;
            Array.Copy(initPacket, 0x74, encKey, 4, 4); // timestamp
            encKey[8] = (byte)version;
            encKey[9] = (byte)(version >> 8);

            {
                const int keyPhaseStartIdx = 0x34;

                var keyPhaseEndIdx = Array.IndexOf<byte>(initPacket, 0, 52);
                if (keyPhaseEndIdx == -1)
                {
                    keyPhaseEndIdx = initPacket.Length - 1;
                }
                var keyPhaseLength = keyPhaseEndIdx - keyPhaseStartIdx;

                Array.Copy(initPacket, keyPhaseStartIdx, encKey, 0x0C, keyPhaseLength);
            }

            using (var md5Hash = MD5.Create())
            {
                return md5Hash.ComputeHash(encKey);
            }
        }
    }
}
