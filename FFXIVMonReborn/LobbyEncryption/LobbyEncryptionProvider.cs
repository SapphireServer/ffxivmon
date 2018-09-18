using System;
using System.Linq;
using System.Security.Cryptography;

namespace FFXIVMonReborn.LobbyEncryption
{
    public class LobbyEncryptionProvider
    {
        private Blowfish _blowfish;
        public byte[] EncKey;
        
        public LobbyEncryptionProvider(byte[] initPacket)
        {
            EncKey = MakeKey(BitConverter.ToUInt32(initPacket, 116), initPacket.Skip(52).TakeWhile(x => x != 0x00).ToArray());
            _blowfish = new Blowfish(EncKey);
        }

        public void DecryptPacket(ref byte[] buffer)
        {
            _blowfish.Decipher(buffer, 0x10, buffer.Length - 0x10);
        }

        private byte[] MakeKey(UInt32 key, byte[] keyPhrase)
        {
            var encKey = new byte[0x2C];
            
            encKey[0] = 0x78;
            encKey[1] = 0x56;
            encKey[2] = 0x34;
            encKey[3] = 0x12;
            Array.Copy(BitConverter.GetBytes(key), 0, encKey, 4, 4);
            encKey[8] = 0x30; // TODO: Don't hardcode this constant
            encKey[9] = 0x11;
            
            Array.Copy(keyPhrase, 0, encKey, 0x0c, keyPhrase.Length);
            
            using (MD5 md5Hash = MD5.Create())
            {
                return md5Hash.ComputeHash(encKey);
            }
        }
    }
}