using System;

namespace FFXIVMonReborn.LobbyEncryption
{
    public class LobbyEncryptionProvider
    {
        public ushort FallbackGameVersion { get; set; } = 5000;

        private Blowfish _blowfish;
        private byte[] _initPacket;
        
        public LobbyEncryptionProvider(byte[] initPacket)
        {
            // Copy the initial packet here because I'm not really sure FFXIVMon mutates the buffer or not.
            _initPacket = new byte[initPacket.Length];
            Buffer.BlockCopy(initPacket, 0, _initPacket, 0, initPacket.Length);
        }

        public void DecryptPacket(byte[] buffer)
        {
            if (_blowfish == null)
            {
                _blowfish = DeduceEncryptionKey(buffer, 0x10, buffer.Length - 0x10);
            }

            _blowfish.Decipher(buffer, 0x10, buffer.Length - 0x10);
        }

        private Blowfish DeduceEncryptionKey(byte[] packet, int pos, int length)
        {
            ulong guessedNullBlock = 0;

            // This is possible because block cipher mode of operation implemented in FFXIV is ECB mode and most of packets are filled with 0x00 byte.
            // (actually, there's so many bytes are filled with 0x00 that it's enough to inspect the first packet sent from the server.)
            //
            // We know that the encryption key is derived from "keyPhrase", "timestamp" and "gameVersion".
            // where "keyPhrase" and "timestamp" are sent as a plaintext in the initial handkeshake phase.
            // Only thing not sent off the wire is the "gameVersion" component but this component can be brute forced in a reasonable time
            // and verify the key by exploiting the fact that ECB mode lacks diffusion.
            //
            // https://en.wikipedia.org/wiki/Block_cipher_mode_of_operation#Electronic_Codebook_(ECB)
            {
                var processor = new BlockProcessor();
                processor.Process(packet, pos, length);
                guessedNullBlock = processor.GetHighestKey();
            }

            for (var gameVer = 1000; gameVer <= 65000; gameVer += 10)
            {
                var key = LobbyKeyHelper.MakeKey(_initPacket, (ushort)gameVer);
                var blowfish = new Blowfish(key);

                var nullBlock = new byte[8];
                blowfish.Encipher(nullBlock, 0, 8);

                var attempedBlock = BitConverter.ToUInt64(nullBlock, 0);
                if (attempedBlock == guessedNullBlock)
                {
                    return blowfish;
                }
            }

            // Failed to guess a key for whatever reason; just use hardcoded key as a fallback.
            var fallbackKey = LobbyKeyHelper.MakeKey(_initPacket, FallbackGameVersion);
            return new Blowfish(fallbackKey);
        }
    }
}