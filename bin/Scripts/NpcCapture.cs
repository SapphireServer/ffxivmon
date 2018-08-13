using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;

dynamic parsed = PacketObj;

if(!Directory.Exists("CapturedNpcs"))
{
	Directory.CreateDirectory("CapturedNpcs");
}

if(Packet.Name == "InitZone")
{
	Debug.WriteLine("New zone id: " + parsed.zoneId);
	DataStorage.Store("zoneId", parsed.zoneId);

	if(!Directory.Exists(Path.Combine("CapturedNpcs", DataStorage.Get("zoneId").ToString())))
		Directory.CreateDirectory(Path.Combine("CapturedNpcs", DataStorage.Get("zoneId").ToString()));
}

if(Packet.Name == "NpcSpawn")
{
	if(DataStorage.Get("zoneId") == null)
	{
		Debug.WriteLine("No zone id captured, not writing NPC...");
		return;
	}

	var entId = BitConverter.ToUInt32(Packet.Data, 4);
    //        public static string GetExdField(string sheetName, int row, int col)

    var name = FFXIVMonReborn.ExdReader.GetExdFieldAsString("BNpcName", (int)parsed.bNPCName, "Singular");
    
    Debug.WriteLine($"Spawn packet: {entId} Base: {parsed.bNPCBase} Name: {name}");

    File.WriteAllBytes(Path.Combine(Environment.CurrentDirectory, "CapturedNpcs", DataStorage.Get("zoneId").ToString(), $"{entId}-{parsed.modelType}-{parsed.subtype}-{parsed.bNPCBase}.bin"), Packet.Data);
}