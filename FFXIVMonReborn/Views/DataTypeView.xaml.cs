using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FFXIVMonReborn.Views
{
    /// <summary>
    /// Interaction logic for DataTypeView.xaml
    /// </summary>
    public partial class DataTypeView : UserControl
    {
        List<StructListItem> _structs;

        public DataTypeView()
        {
            InitializeComponent();
            _structs = new List<StructListItem>
            {
                new StructListItem{ DataTypeCol = "uint8_t", typeLength = 1, ValueCol = "Cannot parse." },
                new StructListItem{ DataTypeCol = "int8_t", typeLength = 1, ValueCol = "Cannot parse." },
                new StructListItem{ DataTypeCol = "uint16_t", typeLength = 2, ValueCol = "Cannot parse." },
                new StructListItem{ DataTypeCol = "int16_t", typeLength = 2, ValueCol = "Cannot parse." },
                new StructListItem{ DataTypeCol = "uint32_t", typeLength = 4, ValueCol = "Cannot parse." },
                new StructListItem{ DataTypeCol = "int32_t", typeLength = 4, ValueCol = "Cannot parse." },
                new StructListItem{ DataTypeCol = "uint64_t", typeLength = 8, ValueCol = "Cannot parse." },
                new StructListItem{ DataTypeCol = "int64_t", typeLength = 8, ValueCol = "Cannot parse." },
                new StructListItem{ DataTypeCol = "float", typeLength = 4, ValueCol = "Cannot parse." },            
                new StructListItem{ DataTypeCol = "time_t", typeLength = 4, ValueCol = "Cannot parse." },
                new StructListItem{ DataTypeCol = "string", typeLength = 1, ValueCol = "Cannot parse." },
            };
        }

        public void Reset()
        {
            this.DataTypeListView.Items.Clear();
        }

        public void Apply(byte[] data, int offset)
        {
            Reset();
            foreach (var type in _structs)
            {
                StructListItem thisItem = new StructListItem();
                thisItem.DataTypeCol = type.DataTypeCol;
                thisItem.ValueCol = type.ValueCol;

                if (offset <= data.Length && data.Length >= type.typeLength)
                {
                    try
                    {
                        if (thisItem.DataTypeCol == "int8_t")
                            thisItem.ValueCol = String.Format("{0}", Convert.ToSByte(data[offset]));
                        else if (thisItem.DataTypeCol == "uint8_t")
                            thisItem.ValueCol = String.Format("{0}", data[offset].ToString());
                        else if (thisItem.DataTypeCol == "int16_t")
                            thisItem.ValueCol = String.Format("{0}", BitConverter.ToInt16(data, offset));
                        else if (thisItem.DataTypeCol == "uint16_t")
                            thisItem.ValueCol = String.Format("{0}", BitConverter.ToUInt16(data, offset));
                        else if (thisItem.DataTypeCol == "int32_t")
                            thisItem.ValueCol = String.Format("{0}", BitConverter.ToInt32(data, offset));
                        else if (thisItem.DataTypeCol == "uint32_t")
                            thisItem.ValueCol = String.Format("{0}", BitConverter.ToUInt32(data, offset));
                        else if (thisItem.DataTypeCol == "int64_t")
                            thisItem.ValueCol = String.Format("{0}", BitConverter.ToInt64(data, offset));
                        else if (thisItem.DataTypeCol == "uint64_t")
                            thisItem.ValueCol = String.Format("{0}", BitConverter.ToUInt64(data, offset));
                        else if (thisItem.DataTypeCol == "float")
                            thisItem.ValueCol = String.Format("{0}", BitConverter.ToDouble(data, offset));
                        else if (thisItem.DataTypeCol == "time_t")
                            thisItem.ValueCol = String.Format("{0}", DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToUInt32(data, offset)).ToLocalTime());
                        else if (thisItem.DataTypeCol == "string")
                        {
                            var str = Encoding.ASCII.GetString(data);
                            str = str.Substring(offset, str.IndexOf('\0'));
                            thisItem.ValueCol = str;
                        }
                    }
                    catch (OverflowException e)
                    {
                        thisItem.ValueCol = String.Format("{0}", Convert.ToSByte(127 - data[offset]));
                    }
                    catch (ArgumentOutOfRangeException e)
                    {
                    }
                    catch (Exception exc)
                    {
                        //new ExtendedErrorView("Failed to update DataTypeView component.", exc.ToString(), "Error").ShowDialog();
                    }
                }
                this.DataTypeListView.Items.Add(thisItem);
            }
        }
    }
}
