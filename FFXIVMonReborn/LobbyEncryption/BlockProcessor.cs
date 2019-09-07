using System;
using System.Collections.Generic;

namespace FFXIVMonReborn.LobbyEncryption
{
    internal class BlockProcessor
    {
        private Dictionary<ulong, uint> _occurrences;

        public BlockProcessor()
        {
            _occurrences = new Dictionary<ulong, uint>();
        }

        public void Process(byte[] buffer, int startPos, int length)
        {
            if (startPos < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(startPos), "Start position can not be a negative number.");
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Length can not be a negative number.");
            }

            var endPos = startPos + length;
            if (endPos > buffer.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(buffer), "Buffer is too short.");
            }

            for (var i = startPos; i + 8 <= endPos; i += 8)
            {
                var block = BitConverter.ToUInt64(buffer, i);
                if (_occurrences.TryGetValue(block, out var count))
                {
                    _occurrences[block] = count + 1;
                }
                else
                {
                    _occurrences[block] = 1;
                }
            }
        }

        public ulong GetHighestKey()
        {
            ulong maxKey = 0;
            uint maxValue = 0;

            foreach (var kv in _occurrences)
            {
                if (kv.Value > maxValue)
                {
                    maxKey = kv.Key;
                    maxValue = kv.Value;
                }
            }

            return maxKey;
        }
    }
}
