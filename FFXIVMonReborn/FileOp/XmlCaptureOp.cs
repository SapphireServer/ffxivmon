using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;

namespace FFXIVMonReborn.FileOp
{
    public static class XmlCaptureOp
    {
        public static CaptureContainer Load(string path)
        {
            List<PacketListItem> output = new List<PacketListItem>();
            bool usingSystemTime = false;
            int version = -1;
            
            XmlDocument doc = new XmlDocument();
            doc.Load(path);

            XmlNode settings = doc.DocumentElement.SelectSingleNode("/Capture");

            foreach (XmlNode node in settings.ChildNodes)
            {
                switch (node.Name)
                {
                    case "UsingSystemTime":
                        usingSystemTime = bool.Parse(node.InnerText);
                        break;
                    case "Version":
                        version = int.Parse(node.InnerText);
                        break;
                }
            }

            XmlNode packets = doc.DocumentElement.SelectSingleNode("/Capture/Packets");

            foreach (XmlNode packet in packets.ChildNodes)
            {
                PacketListItem packetItem = new PacketListItem();

                foreach (XmlNode entry in packet.ChildNodes)
                {
                    switch (entry.Name)
                    {
                        case "Direction":
                            packetItem.DirectionCol = entry.InnerText;
                            break;
                        case "Message":
                            packetItem.MessageCol = entry.InnerText;
                            break;
                        case "Timestamp":
                            packetItem.TimeStampCol = entry.InnerText;
                            break;
                        case "RouteID":
                            packetItem.RouteIdCol = entry.InnerText;
                            break;
                        case "Data":
                            packetItem.Data = Util.StringToByteArray(entry.InnerText);
                            break;
                        case "Set":
                            packetItem.Set = int.Parse(entry.InnerText);
                            break;
                        case "PacketUnixTime":
                            packetItem.PacketUnixTime = uint.Parse(entry.InnerText);
                            break;
                        case "SystemMsTime":
                            packetItem.SystemMsTime = long.Parse(entry.InnerText);
                            break;
                    }
                }

                packetItem.IsVisible = true;

                output.Add(packetItem);
            }

            Debug.WriteLine($"Loaded Packets: {output.Count}");
            return new CaptureContainer { Packets = output.ToArray(), UsingSystemTime = usingSystemTime, Version = version };
        }

        public class CaptureContainer
        {
            public PacketListItem[] Packets { get; set; }
            public bool UsingSystemTime { get; set; }
            public int Version { get; set; } = -1;
        }

        public static void Save(ItemCollection packetCollection, string path, bool usingSystemTime, int version)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "  ";
            settings.NewLineOnAttributes = false;
            settings.Encoding = new UTF8Encoding(false); // Do not emit the byte order marker (BOM)

            using (XmlWriter writer = XmlWriter.Create(path, settings))
            {
                // Create a document
                var doc = new XDocument();
                
                // Create a root element
                var xRoot = new XElement("Capture");
                
                // Write some normal stuff
                xRoot.Add(new XElement("UsingSystemTime", usingSystemTime.ToString()));
                xRoot.Add(new XElement("Version", version.ToString()));
                
                // Write packets
                var xPackets = new XElement("Packets");
                foreach (var entry in packetCollection)
                {
                    var packet = (PacketListItem)entry;

                    var xPacketEntry = new XElement("PacketEntry");
                    xPacketEntry.Add(new XElement("Direction", packet.DirectionCol));
                    xPacketEntry.Add(new XElement("Message", packet.MessageCol));
                    xPacketEntry.Add(new XElement("Timestamp", packet.TimeStampCol));
                    xPacketEntry.Add(new XElement("RouteID", packet.RouteIdCol));
                    xPacketEntry.Add(new XElement("Data", Util.ByteArrayToString(packet.Data)));
                    xPacketEntry.Add(new XElement("Set", packet.Set));
                    xPacketEntry.Add(new XElement("PacketUnixTime", packet.PacketUnixTime));
                    xPacketEntry.Add(new XElement("SystemMsTime", packet.SystemMsTime));

                    xPackets.Add(xPacketEntry);
                }
                xRoot.Add(xPackets);

                // Add a root element
                doc.Add(xRoot);

                // Write a document to the file
                doc.Save(writer);
            }
        }
    }
}