using FFXIVMonReborn.DataModel;
using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

using System.Windows.Documents;
using System.Collections.Generic;
using System.Reflection.PortableExecutable;
using Machina.Infrastructure;
using Machina.FFXIV;

using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;
using System.Linq;
using Machina.FFXIV.Headers;
using static FFXIVMonReborn.MachinaCaptureWorker;
using System.Runtime.InteropServices;

namespace FFXIVMonReborn.Importers
{
    public static class PcapImporter
    {

        public static Capture Load(string path)
        {
            List<PacketEntry> packetEntries = new List<PacketEntry>();
            FFXIVNetworkMonitor monitor = new FFXIVNetworkMonitor();
            #region packet parse
            void MessageReceived(TCPConnection connection, long epoch, byte[] message, int set, FFXIVNetworkMonitor.ConnectionType connectionType)
            {
                var res = Parse(message);

                var item = new PacketEntry
                {
                    IsVisible = true,
                    ActorControl = -1,
                    Data = message,
                    Message = res.header.MessageType.ToString("X4"),
                    Direction = "S",
                    Category = set.ToString(),
                    Timestamp = Util.UnixTimeStampToDateTime(res.header.Seconds).ToString(@"MM\/dd\/yyyy HH:mm:ss"),
                    Size = res.header.MessageLength.ToString(),
                    Set = set,
                    RouteID = res.header.RouteID.ToString(),
                    PacketUnixTime = res.header.Seconds,
                    SystemMsTime = (long.MaxValue + DateTime.Now.ToBinary()) / 10000,
                    Connection = connectionType
                };
                packetEntries.Add(item);
            }

            void MessageSent(TCPConnection connection, long epoch, byte[] message, int set, FFXIVNetworkMonitor.ConnectionType connectionType)
            {
                var res = Parse(message);

                var item = new PacketEntry
                {
                    IsVisible = true,
                    ActorControl = -1,
                    Data = message,
                    Message = res.header.MessageType.ToString("X4"),
                    Direction = "C",
                    Category = set.ToString(),
                    Timestamp = Util.UnixTimeStampToDateTime(res.header.Seconds).ToString(@"MM\/dd\/yyyy HH:mm:ss"),
                    Size = res.header.MessageLength.ToString(),
                    Set = set,
                    RouteID = res.header.RouteID.ToString(),
                    PacketUnixTime = res.header.Seconds,
                    SystemMsTime = (long.MaxValue + DateTime.Now.ToBinary()) / 10000,
                    Connection = connectionType
                };
                packetEntries.Add(item);
            }

            ParseResult Parse(byte[] data)
            {
                GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                var head = (Server_MessageHeader)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(Server_MessageHeader));
                handle.Free();

                ParseResult result = new ParseResult();
                result.header = head;
                result.data = data;

                return result;
            }

            void device_OnPacketArrival(object sender, PacketCapture e)
            {
                var rawPacket = e.GetPacket();
                var packet = PacketDotNet.Packet.ParsePacket(rawPacket.LinkLayerType, rawPacket.Data);
                var tcpPacket = packet.Extract<PacketDotNet.TcpPacket>();
                var ipPacket = (IPPacket)tcpPacket.ParentPacket;
                var ethernetPacket = packet.Extract<EthernetPacket>();

                TCPConnection tcpConn = new TCPConnection();
                tcpConn.LocalIP = BitConverter.ToUInt32(ipPacket.SourceAddress.GetAddressBytes());
                tcpConn.LocalPort = tcpPacket.SourcePort;
                tcpConn.RemoteIP = BitConverter.ToUInt32(ipPacket.DestinationAddress.GetAddressBytes());
                tcpConn.RemotePort = tcpPacket.DestinationPort;

                var ip2 = ipPacket.SourceAddress.GetAddressBytes();
                //var ipStr = ipPacket.SourceAddress.ToString().Split(".");
                if (!(ip2[0] == 192 && ip2[1] == 168) && !(ip2[0] == 10 && ip2[1] == 0) && !(ip2[0] == 172 && ip2[2] == 16))
                {
                    uint tempIp = tcpConn.LocalIP;
                    ushort tempPort = tcpConn.LocalPort;
                    tcpConn.LocalIP = tcpConn.RemoteIP;
                    tcpConn.LocalPort = tcpConn.RemotePort;
                    tcpConn.RemoteIP = tempIp;
                    tcpConn.RemotePort = tempPort;
                    monitor.ProcessReceivedMessage(tcpConn, tcpPacket.PayloadData);
                }
                else
                    monitor.ProcessSentMessage(tcpConn, tcpPacket.PayloadData);
            }
            #endregion
            monitor.MessageReceivedEventHandler = MessageReceived;
            monitor.MessageSentEventHandler = MessageSent;

            ICaptureDevice device;
            device = new CaptureFileReaderDevice(path);
            device.Open();
            device.Filter = "(net 199.91.189.0/24 or net 124.150.157.0/24 or net 195.82.50.0/24 or net 104.156.250.0/24 or net 204.2.229.0/24) and greater 0 ";
            device.OnPacketArrival += device_OnPacketArrival;
            device.Capture();
            device.Close();
            {
                Capture capture = new Capture();
                capture.Version = -1;
                capture.Packets = packetEntries.ToArray();
                return capture;
            }

        }
    }
}