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
                new StructListItem{ DataTypeCol = "double", typeLength = 8, ValueCol = "Cannot parse." },
                new StructListItem{ DataTypeCol = "time_t", typeLength = 4, ValueCol = "Cannot parse." },
                new StructListItem{ DataTypeCol = "string (ascii)", typeLength = 1, ValueCol = "Cannot parse." },
                new StructListItem{ DataTypeCol = "string (utf8)", typeLength = 1, ValueCol = "Cannot parse."}
            };
        }

        public void Reset()
        {
            DataTypeListView.Items.Clear();
        }

        public void Apply(byte[] data, int offset)
        {
            Reset();
            foreach (var type in _structs)
            {
                StructListItem thisItem = new StructListItem();
                thisItem.DataTypeCol = type.DataTypeCol;
                thisItem.ValueCol = type.ValueCol;

                if (offset <= data.Length && data.Length - offset >= type.typeLength)
                {
                    thisItem.OffsetCol = offset.ToString("X");
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
                            thisItem.ValueCol = String.Format("{0}", BitConverter.ToSingle(data, offset));
                        else if (thisItem.DataTypeCol == "double")
                            thisItem.ValueCol = String.Format("{0}", BitConverter.ToDouble(data, offset));
                        else if (thisItem.DataTypeCol == "time_t")
                            thisItem.ValueCol = String.Format("{0}", DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToUInt32(data, offset)).ToLocalTime());
                        else if (thisItem.DataTypeCol == "string (ascii)")
                        {
                            var str = Encoding.ASCII.GetString(data);
                            var terminatorOffset = str.IndexOf((char)0, offset);
                            thisItem.ValueCol = str.Substring(offset, terminatorOffset - offset);
                        }
                        else if (thisItem.DataTypeCol == "string (utf8)")
                        {
                            var str = Encoding.UTF8.GetString(data);
                            var terminatorOffset = str.IndexOf((char)0, offset);
                            thisItem.ValueCol = str.Substring(offset, terminatorOffset - offset);
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
                DataTypeListView.Items.Add(thisItem);
            }
        }

        private void DataTypeListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (DataTypeListView.IsKeyboardFocusWithin)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.C)
                {
                    if (Keyboard.IsKeyDown(Key.LeftShift))
                        DataTypeView_CopyAllCols_Click(null, null);
                    else
                        DataTypeView_CopyValue_Click(null, null);
                }
            }
        }

        private void DataTypeView_CopyValue_Click(object sender, RoutedEventArgs e)
        {
            String str = "";
            String newline = (DataTypeListView.SelectedItems.Count > 1 ? Environment.NewLine : "");

            foreach (StructListItem item in DataTypeListView.SelectedItems)
            {
                str += item.ValueCol + newline;
            }
            Clipboard.SetText(str);
            Clipboard.Flush();
        }

        private void DataTypeView_CopyAllCols_Click(object sender, RoutedEventArgs e)
        {
            String str = "DataType\t|\tValue\t|\tOffset (hex)" + Environment.NewLine;
            foreach (StructListItem item in DataTypeListView.SelectedItems)
            {
                str += item.DataTypeCol + "\t|\t" + item.ValueCol + "\t|\t" + item.OffsetCol + "h" + Environment.NewLine;
            }
            Clipboard.SetText(str);
            Clipboard.Flush();
        }
    }
}
