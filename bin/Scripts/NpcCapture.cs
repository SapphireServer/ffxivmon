using System;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;

/*
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
*/

if (Packet.Name == "Create")
{
    dynamic parsed = PacketObj;

    if (!Directory.Exists("BNpcHp"))
		Directory.CreateDirectory("BNpcHp");

	if (parsed.ObjKind != 2)
		return;

	var layoutId = parsed.LayoutId;
	var level = parsed.Lv;
    var classJob = parsed.ClassJob;
    var hp = parsed.HpMax;
	var baseId = parsed.NpcId;
	var nameId = parsed.NameId;

	var fileName = Path.Combine(Environment.CurrentDirectory, "BNpcHp/BNpcHp.csv");
	if (!File.Exists(fileName))
	{
		using (StreamWriter sw = File.CreateText(fileName))
			sw.WriteLine("LayoutId, BaseId, NameId, Level, ClassJob, Hp\n");
	}

	using (StreamWriter sw = File.AppendText(fileName))
		sw.WriteLine($"{layoutId}, {baseId}, {nameId}, {level}, {classJob}, {hp}");
}