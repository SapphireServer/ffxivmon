using FFXIVMonReborn.DataModel;
using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace FFXIVMonReborn.FileOp
{
    public static class XmlCaptureOp
    {
        public static Capture Load(string path)
        {
            try
            //using (var fileStream = new FileStream(path, FileMode.Open))
            {
                var fileStream = new FileStream(path, FileMode.Open);
                var serializer = new XmlSerializer(typeof(Capture));
                var capture = (Capture)serializer.Deserialize(fileStream);

                Debug.WriteLine($"Loaded Packets: {capture.Packets.Length}");

                return capture;
            }
            catch(Exception ex)
            {
                throw ex;
            }

        }

        public static void Save(Capture capture, string path)
        {
            //using (var fileStream = new FileStream(path, FileMode.Create))
            {
                var fileStream = new FileStream(path, FileMode.Create);
                var serializer = new XmlSerializer(typeof(Capture));
                serializer.Serialize(fileStream, capture);
            }
        }
    }
}