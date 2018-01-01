using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Security.Policy;
using System.Text;
using System.Windows;

namespace FFXIVMonReborn
{
    public enum FilterType
    {
        ActorControl,
        Message,
        StringContents,
        PacketName,
        ActorControlName
    }

    public class Filter
    {
        public static FilterSet[] Parse(string input)
        {
            Debug.WriteLine(input);
            List<FilterSet> output = new List<FilterSet>();

            if (input.Length == 0)
                return output.ToArray();
            for( int i = 0; i < input.Length; i++ )
            {
                try
                {
                    string thisFilter = "";
                    FilterType type;

                    thisFilter = input.Substring(0, input.IndexOf(";") + 1);
                    input = input.Replace(thisFilter, "");

                    object value;
                    // _A(ActorControlType)
                    if (thisFilter.Substring(0, "_A(".Length) == "_A(")
                    {
                        type = FilterType.ActorControl;
                        string vstring = thisFilter.Substring(3, thisFilter.IndexOf(')', 3) - 3);
                        value = int.Parse(vstring, NumberStyles.HexNumber);
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
                    else if (thisFilter.Substring(0, "_N(".Length) == "_N(")
                    {
                        type = FilterType.PacketName;
                        string vstring = thisFilter.Substring(3, thisFilter.IndexOf(')', 3) - 3);
                        value = vstring;
                    }
                    else
                    {
                        type = FilterType.Message;
                        string vstring = thisFilter.Substring(0, thisFilter.Length - 1);
                        value = int.Parse(vstring, NumberStyles.HexNumber);
                    }

                    FilterSet set = new FilterSet { type = type, value = value };

                    output.Add(set);

                    Debug.WriteLine(input.Length);
                }
                catch (Exception exc)
                {
                    MessageBox.Show(
                        $"[Filter] Filter Parse error!\n\n{exc}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
                }
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

        public bool IsApplicableForFilterSet(PacketListItem item)
        {
            bool isApplicable = false;

            switch (this.type)
            {
                case FilterType.Message:
                    if (item.MessageCol == ((int)this.value).ToString("X4"))
                    {
                        isApplicable = true;
                    }
                    break;

                case FilterType.ActorControl:
                    if (item.ActorControl == (int)this.value)
                    {
                        isApplicable = true;
                    }
                    break;

                case FilterType.ActorControlName:
                    if (item.ActorControl != -1 && item.NameCol.ToLower().Contains(((string)this.value).ToLower()))
                    {
                        isApplicable = true;
                    }
                    break;

                case FilterType.PacketName:
                    if (item.NameCol.ToLower().Contains(((string)this.value).ToLower()))
                    {
                        isApplicable = true;
                    }
                    break;

                case FilterType.StringContents:
                    var findStr = Convert.ToString(this.value).ToLower();
                    var packetStr = Encoding.UTF8.GetString(item.Data).ToLower();

                    if (packetStr.Contains(findStr))
                    {
                        isApplicable = true;
                    }
                    break;

                default:
                    break;
            }

            return isApplicable;
        }
    }
}