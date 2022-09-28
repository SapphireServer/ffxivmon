using FFXIVMonReborn.DataModel;
using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace FFXIVMonReborn.Importers
{
    public static class XmlCaptureImporter
    {
        public static Capture Load(string path)
        {
            try
            //using (var fileStream = new FileStream(path, FileMode.Open))
            {
                var fileStream = new FileStream(path, FileMode.Open);
                var serializer = new XmlSerializer(typeof(Capture));
                var capture = (Capture)serializer.Deserialize(fileStream);
                // make sure to translate data-string to bytes
                if (capture != null)
                {
                    foreach (var packet in capture.Packets)
                    {
                        packet.Data = Util.StringToByteArray(packet.DataString);
                    }
                }

                Debug.WriteLine($"Loaded Packets: {capture.Packets.Length}");
                fileStream.Close();
                return capture;
            }
            catch(Exception ex)
            {
                throw ex;
            }

        }

        public static void Save(Capture capture, string path)
        {
            using (var fileStream = new FileStream(path, FileMode.Create))
            {
                if (capture != null)
                {
                    foreach (var packet in capture.Packets)
                    {
                        packet.DataString = Util.ByteArrayToString(packet.Data);
                    }
                }
                var serializer = new XmlSerializer(typeof(Capture));
                serializer.Serialize(fileStream, capture);
                fileStream.Close();
            }
        }
    }
}