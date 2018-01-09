using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;

namespace FFXIVMonReborn
{
    public class CaptureFileOp
    {
        public static CaptureContainer Load(string path)
        {
            List<PacketListItem> output = new List<PacketListItem>();
            bool usingSystemTime = false;
            
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
            return new CaptureContainer { Packets = output.ToArray(), UsingSystemTime = usingSystemTime };
        }

        public class CaptureContainer
        {
            public PacketListItem[] Packets { get; set; }
            public bool UsingSystemTime { get; set; }
        }

        public static void Save(ItemCollection packetCollection, string path, bool usingSystemTime)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.IndentChars = "     ";
            settings.NewLineOnAttributes = false;

            using (XmlWriter writer = XmlWriter.Create(path, settings))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("Capture");
                
                writer.WriteElementString("UsingSystemTime", usingSystemTime.ToString());

                writer.WriteStartElement("Packets");
                writer.WriteEndElement();

                writer.WriteEndElement();
                writer.WriteEndDocument();
            }

            XDocument doc = XDocument.Parse(File.ReadAllText(path));

            foreach (var entry in packetCollection)
            {
                var packet = (PacketListItem)entry;

                XElement packetEntry = new XElement("PacketEntry");
                packetEntry.Add(new XElement("Direction", packet.DirectionCol));
                packetEntry.Add(new XElement("Message", packet.MessageCol));
                packetEntry.Add(new XElement("Timestamp", packet.TimeStampCol));
                packetEntry.Add(new XElement("RouteID", packet.RouteIdCol));
                packetEntry.Add(new XElement("Data", Util.ByteArrayToString(packet.Data)));
                packetEntry.Add(new XElement("Set", packet.Set));
                packetEntry.Add(new XElement("PacketUnixTime", packet.PacketUnixTime));
                packetEntry.Add(new XElement("SystemMsTime", packet.SystemMsTime));

                doc.Element("Capture").Element("Packets").Add(packetEntry);
            }

            doc.Save(path);

        }
    }
}