namespace FFXIVMonReborn.DataModel
{
    public class Capture
    {
        public string UsingSystemTime { get; set; }
        public int Version { get; set; }
        public PacketEntry[] Packets { get; set; }

        public Capture()
        {
            Packets = new PacketEntry[0];
        }
    }
}
