namespace FFXIVMonReborn.Database.DataTypes
{
    public abstract class CustomDataType
    {
        public abstract void Parse(byte[] data);
        public new abstract string ToString();
    }
}