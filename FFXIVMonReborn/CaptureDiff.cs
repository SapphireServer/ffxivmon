using System.Collections.Generic;
using System.Diagnostics;

namespace FFXIVMonReborn
{
    public static class CaptureDiff
    {
        private const int MaxLengthDiff = 0x1;

        public static string GenerateLenghtBasedReport(PacketListItem[] baseCap, PacketListItem[] toDiff)
        {
            Dictionary<string, string> newOpMap = new Dictionary<string, string>();

            foreach (var baseCapPacket in baseCap)
            {
                Debug.WriteLine($"Diff for {baseCapPacket.MessageCol}");

                if (!newOpMap.ContainsKey(baseCapPacket.MessageCol))
                {
                    foreach (var toDiffPacket in toDiff)
                    {
                        if ((toDiffPacket.Data.Length < baseCapPacket.Data.Length + MaxLengthDiff) &&
                            (toDiffPacket.Data.Length > baseCapPacket.Data.Length - MaxLengthDiff))
                        {
                            newOpMap.Add(baseCapPacket.MessageCol, $"{toDiffPacket.MessageCol}(0x{toDiffPacket.Data.Length.ToString("X")})");
                            break;
                        }
                    }
                }
            }

            string output = "";

            foreach (var entry in newOpMap)
            {
                output += $"{entry.Value} -> {entry.Key}\n";
            }

            return output;
        }
    }
}