using FFXIVMonReborn.DataModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace FFXIVMonReborn.FileOp
{
    public static class FfxivReplayOp
    {
        public static int GetNumPackets(byte[] replay)
        {
            using (MemoryStream stream = new MemoryStream(replay))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    string magic = Encoding.ASCII.GetString(reader.ReadBytes(0xB));
                    if (magic != "FFXIVREPLAY")
                        throw new ArgumentException("Not a FFXIV Replay file: " + magic);

                    stream.Position = 0x354;

                    int num = 0;
                    while (stream.Position < replay.Length)
                    {
                        stream.Position += 2;
                        int length = reader.ReadInt16();
                        stream.Position += 4;
                        stream.Position += 4;

                        stream.Position += length;
                        num++;
                    }

                    return num;
                }
            }
        }

        public static PacketEntry[] Import(byte[] replay, int start, int end)
        {
            var output = new List<PacketEntry>();
            string log = "";

            using (MemoryStream stream = new MemoryStream(replay))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    string magic = Encoding.ASCII.GetString(reader.ReadBytes(0xB));
                    if (magic != "FFXIVREPLAY")
                        throw new ArgumentException("Not a FFXIV Replay file: " + magic);

                    stream.Position = 0x14;
                    long recordTime = reader.ReadInt32();
                    DateTime time = Util.UnixTimeStampToDateTime(recordTime);
                    log += $"Recording time: {time} - {recordTime}\n";

                    stream.Position = 0x18;
                    int recLength = reader.ReadInt32();
                    log += $"Recording length: {recLength}ms\n";
                    
                    stream.Position = 0x1C;
                    int movieLength = reader.ReadInt32();
                    log += $"Movie length: {movieLength}ms\n";

                    stream.Position = 0x20;
                    int contentFinderCondition = reader.ReadInt16();
                    log += $"ContentFinderCondition: {contentFinderCondition}\n";

                    stream.Position = 0x38;
                    log += "Party ClassJob: ";
                    int[] classJobAry = new int[8];
                    for (int i = 0; i < 8; i++)
                    {
                        classJobAry[i] = reader.ReadByte();
                        log += classJobAry[i] + " ";
                    }
                    log += "\n\n";

                    stream.Position = 0x354;

                    int lastTime = 0;
                    for(int i = 0; i < end; i++)
                    {
                        int opcode = reader.ReadInt16();
                        int length = reader.ReadInt16();
                        int timeOffset = reader.ReadInt32();
                        int actorId = reader.ReadInt32();
                        
                        var data = new byte[0x20 + length];
                        reader.Read(data, 0x20, length);

                        time = time.AddMilliseconds(timeOffset - lastTime);
                        lastTime = timeOffset;
                        
                        if(i >= start)
                            output.Add(new PacketEntry {Message = opcode.ToString("X4"), Data = data, Direction = "S", RouteID = "?", Set = 0,
                                Category = "?", IsVisible = true, Size = data.Length.ToString(), Timestamp = time.ToString(@"MM\/dd\/yyyy HH:mm:ss.fff tt")});
                        
                        // log += $"->Packet: {opcode.ToString("X")} - {length} bytes - {timeOffset}ms - for {actorId.ToString("X")} - {time.ToString(@"MM\/dd\/yyyy HH:mm:ss.fff tt")}\n";
                    }
                }
            }

            new ExtendedErrorView($"Ran import on {replay.Length} bytes.", log, "FFXIVMon Reborn").ShowDialog();

            return output.ToArray();
        }
    }
}