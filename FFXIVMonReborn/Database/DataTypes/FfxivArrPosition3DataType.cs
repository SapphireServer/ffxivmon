using System;

namespace FFXIVMonReborn.Database.DataTypes
{
    public class FfxivArrPosition3DataType : CustomDataType
    {
        public float X;
        public float Y;
        public float Z;
        
        public override void Parse(byte[] data)
        {
            X = BitConverter.ToSingle(data, 0);
            Y = BitConverter.ToSingle(data, 4);
            Z = BitConverter.ToSingle(data, 8);
        }

        public override string ToString()
        {
            return $"X: {X} Y: {Y} Z: {Z}";
        }
    }
}