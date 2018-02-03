using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FFXIVMonReborn
{
    public class FFXIVReplayOp
    {
        public static PacketListItem[] Import(byte[] replay)
        {
            List<PacketListItem> output = new List<PacketListItem>();
            string log = "";

            using (MemoryStream stream = new MemoryStream(replay))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    string magic = Encoding.ASCII.GetString(reader.ReadBytes(0xB));
                    if (magic != "FFXIVREPLAY")
                        throw new ArgumentException("Not a FFXIV Replay file: " + magic);

                    stream.Position = 0x14;
                    long recordTime = reader.ReadInt64();
                    log += $"Recording time: {Util.UnixTimeStampToDateTime(recordTime)} - {recordTime}\n";

                    stream.Position = 0x1C;
                    int length = reader.ReadInt32();
                    log += $"Recording length: {length}\n";

                    stream.Position = 0x20;
                    int contentFinderCondition = reader.ReadInt16();
                    log += $"ContentFinderCondition: {length}\n";

                    stream.Position = 0x38;
                    log += "Party ClassJob: ";
                    int[] classJobAry = new int[8];
                    for (int i = 0; i < 8; i++)
                    {
                        classJobAry[i] = reader.ReadByte();
                        log += classJobAry[i] + ",";
                    }
                    log += "\n";
                }
            }

            new ExtendedErrorView("", log, "FFXIVMon Reborn").ShowDialog();

            return output.ToArray();
        }
    }
}