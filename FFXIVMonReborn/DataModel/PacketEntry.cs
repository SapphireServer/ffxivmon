using System.Xml.Serialization;

namespace FFXIVMonReborn.DataModel
{
    public class PacketEntry
    {
        public string Direction { get; set; }
        public string Message { get; set; }
        public string Timestamp { get; set; }
        public string RouteID { get; set; }
        public byte[] Data { get; set; }
        public int Set { get; set; }
        public uint PacketUnixTime { get; set; }
        public long SystemMsTime { get; set; }

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

        public PacketEntry()
        {
            IsVisible = true;
            RouteID = string.Empty;
        }
    }
}
