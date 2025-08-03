using FFXIVMonReborn.DataModel;
using System;
using System.Collections.Generic;
using Machina.FFXIV;
using System.Runtime.CompilerServices;
using Chronofoil.CaptureFile;
using Chronofoil.CaptureFile.Binary;
using Chronofoil.CaptureFile.Binary.Packet;
using Chronofoil.CaptureFile.Generated;
using Direction = Chronofoil.CaptureFile.Generated.Direction;
using Packet = Chronofoil.CaptureFile.Binary.Packet.Packet;

namespace FFXIVMonReborn.Importers;

public static class ChronofoilImporter
{
    private static readonly List<PacketEntry> PacketEntries = new();

    public static Capture Load(string path)
    {
        var reader = new CaptureReader(path);

        var frameIndex = 0;
        foreach (var frame in reader.GetFrames())
        {
            if (frame.Header.Protocol == Protocol.Chat) continue;
            
            var set = frameIndex % 2;
            var frameHeader = GetFrameHeader(frame.Frame.Span);
            foreach (var packet in PacketsFromFrame(frame.Frame.Span))
            {
                var type = packet.IpcHeader?.Type ?? (ushort)packet.Header.Type;
                var unixTime = packet.IpcHeader?.Timestamp ?? (uint)DateTimeOffset.FromUnixTimeMilliseconds((long)frameHeader.TimeValue).ToUnixTimeSeconds();
                var routeId = packet.IpcHeader?.ServerId ?? 0;
                var connection = frame.Header.Protocol switch
                {
                    Protocol.Zone => FFXIVNetworkMonitor.ConnectionType.Game,
                    // Protocol.Chat => FFXIVNetworkMonitor.ConnectionType.Game,
                    Protocol.Lobby => FFXIVNetworkMonitor.ConnectionType.Lobby,
                    _ => throw new ArgumentOutOfRangeException()
                };
                
                var packetEntry = new PacketEntry
                {
                    IsVisible = true,
                    ActorControl = -1,
                    Data = packet.Data,
                    Message = type.ToString("X4"),
                    Direction = frame.Header.Direction == Direction.Rx ? "S" : "C",
                    Category = set.ToString(),
                    Timestamp = DateTimeOffset.FromUnixTimeMilliseconds((long)frameHeader.TimeValue).ToLocalTime().ToString(@"MM\/dd\/yyyy HH:mm:ss"),
                    Size = packet.Header.Size.ToString(),
                    Set = set,
                    RouteID = routeId.ToString(),
                    PacketUnixTime = unixTime,
                    SystemMsTime = 0,
                    Connection = connection
                };
                PacketEntries.Add(packetEntry);
            }

            frameIndex++;
        }

        var capture = new Capture
        {
            Packets = PacketEntries.ToArray(),
            UsingSystemTime = "false",
            Version = -1,
        };
        return capture;
    }
    
    public static FrameHeader GetFrameHeader(ReadOnlySpan<byte> frame)
    {
        var headerSize = Unsafe.SizeOf<FrameHeader>();
        var headerSpan = frame[..headerSize];
        return headerSpan.CastTo<FrameHeader>();
    }

    public static List<Packet> PacketsFromFrame(ReadOnlySpan<byte> frame)
    {
        var list = new List<Packet>();
        
        var headerSize = Unsafe.SizeOf<FrameHeader>();
        var headerSpan = frame[..headerSize];
        var header = headerSpan.CastTo<FrameHeader>();
        var frameSpan = frame[..(int)header.TotalSize];
        var data = frameSpan.Slice(headerSize, (int)header.TotalSize - headerSize);
        
        // Console.WriteLine($"Frame has {header.Count} packets");
        
        var offset = 0;
        for (int i = 0; i < header.Count; i++)
        {
            var packet = new Packet();
            
            // Get this packet's PacketElementHeader. It tells us the size
            var pktHdrSize = Unsafe.SizeOf<PacketElementHeader>();
            var pktHdrSlice = data.Slice(offset, pktHdrSize);
            var pktHdr = pktHdrSlice.CastTo<PacketElementHeader>();

            packet.Header = pktHdr;
            
            // This span contains all packet data, excluding the element header, including the IPC header
            var pktData = data.Slice(offset, (int)pktHdr.Size);

            if (pktHdr.Type == PacketType.Ipc)
            {
                var ipcHdrSize = Unsafe.SizeOf<PacketIpcHeader>();
                var ipcHdrSlice = pktData.Slice(pktHdrSize, ipcHdrSize);
                var ipcHdr = ipcHdrSlice.CastTo<PacketIpcHeader>();

                packet.IpcHeader = ipcHdr;
            }
            packet.Data = pktData.ToArray();

            // Console.WriteLine($"Adding packet with type {pktHdr.Type} [{pktHdr.Size}] to list");
            list.Add(packet);
            offset += (int)pktHdr.Size;
        }

        // Console.WriteLine($"Returning list with {list.Count} packets");
        return list;
    }
}