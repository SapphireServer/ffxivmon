using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFXIVMonReborn.DataModel
{
    public class PacketBookmark
    {
        public string Message { get; set; }
        public int Offset { get; set; }
        public string Description { get; set; }

        public PacketEntry PacketEntry { get; set; }
    }
}
