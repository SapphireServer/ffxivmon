using FFXIVMonReborn.DataModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace FFXIVMonReborn
{
    public enum FilterType
    {
        ActorControl,
        Message,
        StringContents,
        PacketName,
        ActorControlName,
        Int64,
        Int32,
        Int16,
        Int8,
        Float,
        Double,
        ByteArray,
    }

    public class Filter
    {
        public static Dictionary<string, string> Help = new Dictionary<string, string>
        {
            {"[Number]", "Opcode to search for by id, [Number] being hexadecimal integer e.g. 1A1 etc." },
            {"_A(ActorControlNumber);", "ActorControl id to search for."},
            {"_AN(ActorControlName);", "ActorControl name to search for."},
            {"_S(String);", "String to search packet contents for."},
            {"_N(Name)", "Opcode search by name."},
            {"_I64([u]long);", "Int64/UInt64 to search packet contents for, number parameter (0x prefix for hex)."},
            {"_I32([u]int);", "Int32/UInt32 to search packet contents for, number parameter (0x prefix for hex)."},
            {"_I16([u]short);", "Int16/UInt16 to search packet contents for, number parameter (0x prefix for hex)."},
            {"_I8([u]int8);", "Int8/UInt8 to search packet contents for, number parameter (0x prefix for hex)."},
            {"_F(float);", "Float to search packet contents for, number parameter."},
            {"_D(double);", "Double to search packet contents for, number parameter."},
            {"_B(AA BB CC DD);", "Byte array to search packet contents for, hexadecimal without 0x prefix. Must be pairs of two." },
        };

        public static bool IsValidFilter(string input)
        {
            try
            {
                Parse(input);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
        
        public static FilterSet[] Parse(string input)
        {
            Debug.WriteLine(input);
            List<FilterSet> output = new List<FilterSet>();
            
            if (input.Length == 0)
                return output.ToArray();
            
            for (int i = 0; i < input.Length; i++ )
            {
                string thisFilter = "";
                FilterType type;

                thisFilter = input.Substring(0, input.IndexOf(";") + 1);
                input = input.Replace(thisFilter, "");

                object value;
                if (thisFilter[0] != '_')
                {
                    type = FilterType.Message;
                    string vstring = thisFilter.Substring(0, thisFilter.Length - 1);
                    value = vstring;
                }
                // _A(ActorControlType)
                else if (thisFilter.Substring(0, "_A(".Length) == "_A(")
                {
                    type = FilterType.ActorControl;
                    string vstring = thisFilter.Substring(3, thisFilter.IndexOf(')', 3) - 3);
                    value = vstring;
                }
                // _AN(ActorControlName)
                else if (thisFilter.Substring(0, "_AN(".Length) == "_AN(")
                {
                    type = FilterType.ActorControlName;
                    string vstring = thisFilter.Substring(4, thisFilter.IndexOf(')', 4) - 4);
                    value = vstring;
                }
                // _S(CharName)
                else if (thisFilter.Substring(0, "_S(".Length) == "_S(")
                {
                    type = FilterType.StringContents;
                    string vstring = thisFilter.Substring(3, thisFilter.IndexOf(')', 3) - 3);
                    value = vstring;
                }
                // _N(Name)
                else if (thisFilter.Substring(0, "_N(".Length) == "_N(")
                {
                    type = FilterType.PacketName;
                    string vstring = thisFilter.Substring(3, thisFilter.IndexOf(')', 3) - 3);
                    value = vstring;
                }
                // _I64([u]long)
                else if (thisFilter.Substring(0, "_I64(".Length) == "_I64(")
                {
                    type = FilterType.Int64;
                    string vstring = thisFilter.Substring("_I64(".Length, thisFilter.IndexOf(')', "_I64(".Length) - "_I64(".Length);
                    value = vstring;
                }
                // _I32([u]int)
                else if (thisFilter.Substring(0, "_I32(".Length) == "_I32(")
                {
                    type = FilterType.Int32;
                    string vstring = thisFilter.Substring("_I32(".Length, thisFilter.IndexOf(')', "_I32(".Length) - "_I32(".Length);
                    value = vstring;
                }
                // _I16([u]short)
                else if (thisFilter.Substring(0, "_I16(".Length) == "_I16(")
                {
                    type = FilterType.Int16;
                    string vstring = thisFilter.Substring("_I16(".Length, thisFilter.IndexOf(')', "_I16(".Length) - "_I16(".Length);
                    value = vstring;
                }
                // _I8(char/byte)
                else if (thisFilter.Substring(0, "_I8(".Length) == "_I8(")
                {
                    type = FilterType.Int8;
                    string vstring = thisFilter.Substring("_I8(".Length, thisFilter.IndexOf(')', "_I8(".Length) - "_I8(".Length);
                    value = vstring;
                }
                // _F(float)
                else if (thisFilter.Substring(0, "_F(".Length) == "_F(")
                {
                    type = FilterType.Float;
                    string vstring = thisFilter.Substring("_F(".Length, thisFilter.IndexOf(')', "_F(".Length) - "_F(".Length);
                    value = vstring;
                }
                // _D(double)
                else if (thisFilter.Substring(0, "_D(".Length) == "_D(")
                {
                    type = FilterType.Double;
                    string vstring = thisFilter.Substring("_D(".Length, thisFilter.IndexOf(')', "_D(".Length) - "_D(".Length);
                    value = vstring;
                }
                // _B(AA BB CC DD EE FF)
                else if (thisFilter.Substring(0, "_B(".Length) == "_B(")
                {
                    type = FilterType.ByteArray;
                    string vstring = thisFilter.Substring("_B(".Length, thisFilter.IndexOf(')', "_B(".Length) - "_B(".Length);
                    value = vstring;
                }
                else
                {
                    type = FilterType.Message;
                    string vstring = thisFilter.Substring(0, thisFilter.Length - 1);
                    value = int.Parse(vstring, NumberStyles.HexNumber);
                }

                FilterSet set = new FilterSet {type = type, value = value};

                output.Add(set);

                Debug.WriteLine(input.Length);

                if (input.Length <= 1)
                    break;
            }

            return output.ToArray();
        }
    }

    public class FilterSet
    {
        public FilterType type;
        public object value;

        public bool IsApplicableForFilterSet(PacketEntry item)
        {
            try
            {
                switch (this.type)
                {
                    case FilterType.Message:
                    {
                        var valStr = (string)value;
                        string[] split;
                        NumberStyles styles = NumberStyles.Any;

                        if ((split = valStr.Split('x')).Length > 1)
                        {
                            valStr = split[1];
                            styles = NumberStyles.HexNumber;
                        }
                        else if ((split = valStr.Split('X')).Length > 1)
                        {
                            valStr = split[1];
                            styles = NumberStyles.HexNumber;
                        }
                        
                        if (UInt16.TryParse(valStr, styles, CultureInfo.CurrentCulture, out var findUInt16))
                        {
                            for (var i = 0; i + sizeof(UInt16) - 1 < item.Data.Length; ++i)
                            {
                                return item.Message == findUInt16.ToString("X4");
                            }
                        }
                    }
                    break;

                    case FilterType.ActorControl:
                    {
                        var valStr = (string)value;
                        string[] split;
                        NumberStyles styles = NumberStyles.Any;

                        if ((split = valStr.Split('x')).Length > 1)
                        {
                            valStr = split[1];
                            styles = NumberStyles.HexNumber;
                        }
                        else if ((split = valStr.Split('X')).Length > 1)
                        {
                            valStr = split[1];
                            styles = NumberStyles.HexNumber;
                        }
                        
                        if (UInt16.TryParse(valStr, styles, CultureInfo.CurrentCulture, out var findUInt16))
                        {
                            for (var i = 0; i + sizeof(UInt16) - 1 < item.Data.Length; ++i)
                            {
                                return item.ActorControl == findUInt16;
                            }
                        }
                    }
                    break;

                    case FilterType.ActorControlName:
                        if (item.ActorControl != -1 && item.Name.ToLower().Contains(((string)this.value).ToLower()))
                        {
                            return true;
                        }
                        break;

                    case FilterType.PacketName:
                        if (item.Name.ToLower().Contains(((string)this.value).ToLower()))
                        {
                            return true;
                        }
                        break;

                    case FilterType.StringContents:
                        var findStr = Convert.ToString(this.value).ToLower();
                        var packetStr = Encoding.UTF8.GetString(item.Data).ToLower();

                        if (packetStr.Contains(findStr))
                        {
                            return true;
                        }
                        break;
                    // todo: these are horribly inefficient
                    case FilterType.Int64:
                        {
                            var valStr = (string)value;
                            string[] split;
                            NumberStyles styles = NumberStyles.Any;

                            if ((split = valStr.Split('x')).Length > 1)
                            {
                                valStr = split[1];
                                styles = NumberStyles.HexNumber;
                            }
                            else if ((split = valStr.Split('X')).Length > 1)
                            {
                                valStr = split[1];
                                styles = NumberStyles.HexNumber;
                            }

                            if (Int64.TryParse(valStr, styles, CultureInfo.CurrentCulture, out var findInt64))
                            {
                                for (var i = 0; i + sizeof(Int64) - 1 < item.Data.Length; ++i)
                                {
                                    if (BitConverter.ToInt64(item.Data, i) == findInt64)
                                        return true;
                                }
                            }
                            if (UInt64.TryParse(valStr, styles, CultureInfo.CurrentCulture, out var findUInt64))
                            {
                                for (var i = 0; i + sizeof(UInt64) - 1 < item.Data.Length; ++i)
                                {
                                    if (BitConverter.ToUInt64(item.Data, i) == findUInt64)
                                        return true;
                                }
                            }
                        }
                        break;
                    case FilterType.Int32:
                        {
                            var valStr = (string)value;
                            string[] split;
                            NumberStyles styles = NumberStyles.Any;

                            if ((split = valStr.Split('x')).Length > 1)
                            {
                                valStr = split[1];
                                styles = NumberStyles.HexNumber;
                            }
                            else if ((split = valStr.Split('X')).Length > 1)
                            {
                                valStr = split[1];
                                styles = NumberStyles.HexNumber;
                            }

                            if (Int32.TryParse(valStr, styles, CultureInfo.CurrentCulture, out var findInt32))
                            {
                                for (var i = 0; i + sizeof(Int32) - 1 < item.Data.Length; ++i)
                                {
                                    if (BitConverter.ToInt32(item.Data, i) == findInt32)
                                        return true;
                                }
                            }
                            if (UInt32.TryParse(valStr, styles, CultureInfo.CurrentCulture, out var findUInt32))
                            {
                                for (var i = 0; i + sizeof(UInt32) - 1 < item.Data.Length; ++i)
                                {
                                    if (BitConverter.ToUInt32(item.Data, i) == findUInt32)
                                        return true;
                                }
                            }
                        }
                        break;
                    case FilterType.Int16:
                        {
                            var valStr = (string)value;
                            string[] split;
                            NumberStyles styles = NumberStyles.Any;

                            if ((split = valStr.Split('x')).Length > 1)
                            {
                                valStr = split[1];
                                styles = NumberStyles.HexNumber;
                            }
                            else if ((split = valStr.Split('X')).Length > 1)
                            {
                                valStr = split[1];
                                styles = NumberStyles.HexNumber;
                            }

                            if (Int16.TryParse(valStr, styles, CultureInfo.CurrentCulture, out var findInt16))
                            {
                                for (var i = 0; i + sizeof(Int16) - 1 < item.Data.Length; ++i)
                                {
                                    if (BitConverter.ToInt16(item.Data, i) == findInt16)
                                        return true;
                                }
                            }
                            if (UInt16.TryParse(valStr, styles, CultureInfo.CurrentCulture, out var findUInt16))
                            {
                                for (var i = 0; i + sizeof(UInt16) - 1 < item.Data.Length; ++i)
                                {
                                    if (BitConverter.ToUInt16(item.Data, i) == findUInt16)
                                        return true;
                                }
                            }
                        }
                        break;
                    case FilterType.Int8:
                        {
                            var valStr = (string)value;
                            string[] split;
                            int convertBase = 10;
                            if ((split = valStr.Split('x')).Length > 1)
                            {
                                valStr = split[1];
                                convertBase = 16;
                            }
                            else if ((split = valStr.Split('X')).Length > 1)
                            {
                                valStr = split[1];
                                convertBase = 16;
                            }

                            var findInt8 = System.Convert.ToChar(System.Convert.ToUInt32(valStr, convertBase));
                            {
                                for (var i = 0; i < item.Data.Length; ++i)
                                {
                                    if (BitConverter.ToChar(item.Data, i) == findInt8)
                                        return true;
                                }
                            }

                            var findUInt8 = System.Convert.ToByte(System.Convert.ToUInt32(valStr, convertBase));
                            {
                                for (var i = 0; i < item.Data.Length; ++i)
                                {
                                    if (item.Data[i] == findUInt8)
                                        return true;
                                }
                            }
                        }
                        break;
                    case FilterType.Float:
                        {
                            if (float.TryParse((string)value, out var findFloat))
                            {
                                for (var i = 0; i + sizeof(float) - 1 < item.Data.Length; ++i)
                                {
                                    if (BitConverter.ToSingle(item.Data, i) == findFloat)
                                        return true;
                                }
                            }
                        }
                        break;
                    case FilterType.Double:
                        {
                            if (double.TryParse((string)value, out var findDouble))
                            {
                                for (var i = 0; i + sizeof(double) - 1 < item.Data.Length; ++i)
                                {
                                    if (BitConverter.ToSingle(item.Data, i) == findDouble)
                                        return true;
                                }
                            }
                        }
                        break;
                    case FilterType.ByteArray:
                        {
                            string valStr = value.ToString().Replace(" ", "");
                            List<byte> findBytes = new List<byte>();

                            for (var i = 0; i + 1 < valStr.Length; i += 2)
                            {
                                findBytes.Add(Convert.ToByte(Convert.ToUInt32(valStr.Substring(i, 2), 16)));
                            }

                            for (var i = 0; i + findBytes.Count - 1 < item.Data.Length; ++i)
                            {
                                if (item.Data[i] == findBytes[0])
                                {
                                    bool isMatch = true;
                                    for (var j = 0; j < findBytes.Count; ++j)
                                    {
                                        if (item.Data[i + j] != findBytes[j])
                                        {
                                            isMatch = false;
                                            break;
                                        }
                                    }

                                    if (isMatch)
                                        return true;
                                }
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception e)
            {
                return false;
            }

            return false;
        }
    }
}