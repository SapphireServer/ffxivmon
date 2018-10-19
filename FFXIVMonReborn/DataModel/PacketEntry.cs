using System.Xml.Serialization;
using Machina.FFXIV;

namespace FFXIVMonReborn.DataModel
{
    public class PacketEntry
    {
        public string Direction { get; set; }
        public string Message { get; set; }
        public string Timestamp { get; set; }
        public string RouteID { get; set; }
        [XmlElement(ElementName = "Data")]
        public string DataString { get; set; }
        [XmlIgnore]
        public byte[] Data { get; set; }
        public string Note { get; set; }
        public int Set { get; set; }
        public uint PacketUnixTime { get; set; }
        public long SystemMsTime { get; set; }
        public bool IsDecrypted { get; set; }
        public FFXIVNetworkMonitor.ConnectionType Connection { get; set; }

        // ignored values
        [XmlIgnore]
        public string Name { get; set; }
        [XmlIgnore]
        public string Comment { get; set; }
        [XmlIgnore]
        public string Size { get; set; }
        [XmlIgnore]
        public string Category { get; set; }
        [XmlIgnore]
        public int ActorControl { get; set; }
        [XmlIgnore]
        public bool IsVisible { get; set; }
        [XmlIgnore]
        public bool IsForSelf { get; set; }

        public PacketEntry()
        {
            IsVisible = true;
            RouteID = string.Empty;
        }
    }
}
