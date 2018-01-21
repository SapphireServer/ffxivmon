using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace FFXIVMonReborn
{
    public static class CaptureDiff
    {
        private const int MaxLengthDiff = 0x1;
        private const int HeadSizeToSkip = 0x20;

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
                output += $"{entry.Key} -> {entry.Value}\n";
            }

            return output;
        }
        
        public static string GenerateDataBasedReport(PacketListItem[] baseCap, PacketListItem[] toDiff)
        {
            Dictionary<string, List<KeyValuePair<float, string>>> newOpMap = new Dictionary<string, List<KeyValuePair<float, string>>>();

            for(int i = 0; i < baseCap.Length; i++)
            {
                var skippedBaseData = baseCap[i].Data.Skip(HeadSizeToSkip).ToArray();
                if(skippedBaseData.Length == 0)
                    continue;
                
                Debug.WriteLine($"Diff for {baseCap[i].MessageCol}");

                if (!newOpMap.ContainsKey(baseCap[i].MessageCol))
                {
                    List<KeyValuePair<float, string>> thisKey = new List<KeyValuePair<float, string>>();
                    for (int j = 0; j < toDiff.Length; j++)
                    {
                        var skippedToDiffData = toDiff[j].Data.Skip(HeadSizeToSkip).ToArray();

                        if(skippedToDiffData.Length == 0)
                            continue;
                        
                        var pctBase = skippedBaseData.DeepComparePercent(skippedToDiffData);
                        var pctToDiff = skippedToDiffData.DeepComparePercent(skippedBaseData);
                        
                        if(pctBase > 50 || pctToDiff > 50)
                        {
                            thisKey.Add(new KeyValuePair<float, string>(pctBase, $"Candidate: {baseCap[i].MessageCol} -> {toDiff[j].MessageCol}({pctBase}% - {pctToDiff}%)[{i} - {j}]\n"));
                            Debug.WriteLine($"Candidate: {baseCap[i].MessageCol} -> {toDiff[j].MessageCol}({pctBase}% - {pctToDiff}%)[{i} - {j}]\n");
                            break;
                        }
                    }
                    
                    newOpMap.Add(baseCap[i].MessageCol, thisKey);
                }
            }
            string output = "";

            foreach (var entry in newOpMap)
            {
                entry.Value.Sort((x, y) => y.Key.CompareTo(x.Key));
                output += $"OPCODE {entry.Key}:\n";
                
                foreach (var listentry in entry.Value)
                {
                    output += $"    ->{listentry.Key}%: {listentry.Value}";
                }
            }
            
            return output;
        }
    }
}