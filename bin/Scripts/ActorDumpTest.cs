using System;
using System.IO;

if (packet.MessageCol == "011D") {
 Console.WriteLine("-> NPC_SPAWN");

 using(MemoryStream memStream = new MemoryStream(packet.Data)) {
  using(BinaryReader binReader = new BinaryReader(memStream)) {
   try {
    binReader.ReadInt16();
    binReader.ReadInt16();
    binReader.ReadInt16();
    binReader.ReadInt16();

    binReader.ReadByte();
    binReader.ReadByte();
    binReader.ReadByte();
    binReader.ReadByte();

    binReader.ReadInt32();

    binReader.ReadInt64();
    binReader.ReadInt32();
    binReader.ReadInt32();

    binReader.ReadInt64();
    binReader.ReadInt64();
    binReader.ReadInt64();

    binReader.ReadInt32();
    binReader.ReadInt32();
    int bnpcBaseId = binReader.ReadInt32();
    int nameId = binReader.ReadInt32();
    binReader.ReadInt32();
    binReader.ReadInt32();
    binReader.ReadInt32();
    binReader.ReadInt32();
    binReader.ReadInt32();

    binReader.ReadInt32();
    int hPMax = binReader.ReadInt32();

    Console.WriteLine($"BaseID:{bnpcBaseId} - NameID:{nameId} - hPMax:{hPMax}");

   } catch (Exception exc) {
    Console.WriteLine(exc);
   }
  }
 }
}