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
using FFXIVMonReborn.Database;

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
                new StructListItem{ DataTypeCol = "string (utf8)", typeLength = 1, ValueCol = "Cannot parse."},
                new StructListItem{ DataTypeCol = "binary", typeLength = 1, ValueCol = "Cannot parse." }
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
                            thisItem.ValueCol = $"{Convert.ToSByte(data[offset])}";
                        else if (thisItem.DataTypeCol == "uint8_t")
                            thisItem.ValueCol = $"{data[offset].ToString()}";
                        else if (thisItem.DataTypeCol == "int16_t")
                            thisItem.ValueCol = $"{BitConverter.ToInt16(data, offset)}";
                        else if (thisItem.DataTypeCol == "uint16_t")
                            thisItem.ValueCol = $"{BitConverter.ToUInt16(data, offset)}";
                        else if (thisItem.DataTypeCol == "int32_t")
                            thisItem.ValueCol = $"{BitConverter.ToInt32(data, offset)}";
                        else if (thisItem.DataTypeCol == "uint32_t")
                            thisItem.ValueCol = $"{BitConverter.ToUInt32(data, offset)}";
                        else if (thisItem.DataTypeCol == "int64_t")
                            thisItem.ValueCol = $"{BitConverter.ToInt64(data, offset)}";
                        else if (thisItem.DataTypeCol == "uint64_t")
                            thisItem.ValueCol = $"{BitConverter.ToUInt64(data, offset)}";
                        else if (thisItem.DataTypeCol == "float")
                            thisItem.ValueCol = $"{BitConverter.ToSingle(data, offset)}";
                        else if (thisItem.DataTypeCol == "double")
                            thisItem.ValueCol = $"{BitConverter.ToDouble(data, offset)}";
                        else if (thisItem.DataTypeCol == "time_t")
                            thisItem.ValueCol = $"{DateTimeOffset.FromUnixTimeSeconds(BitConverter.ToUInt32(data, offset)).ToLocalTime()}";
                        else if (thisItem.DataTypeCol == "string (ascii)")
                        {
                            var term = getTerminator(data, offset);
                            thisItem.ValueCol = Encoding.ASCII.GetString(data, offset, term - offset);
                        }
                        else if (thisItem.DataTypeCol == "string (utf8)")
                        {
                            var term = getTerminator(data, offset);
                            thisItem.ValueCol = Encoding.UTF8.GetString(data, offset, term - offset);
                        }
                        else if (thisItem.DataTypeCol == "binary")
                        {
                            var str = "";
                            var j = 0;
                            for (var i = offset; i < data.Length; ++i, ++j)
                            {
                                str += Convert.ToString(data[i], 2).PadLeft(8, '0') + ((j + 1) % 2 == 0 ? Environment.NewLine : " ");
                                // stop at 8 bytes
                                if (j == 7 || (i + 1 < data.Length && data[i + 1] == 0))
                                    break;
                            }
                            thisItem.ValueCol = str;
                        }
                    }
                    catch (OverflowException)
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
        int getTerminator(byte[] data, int offset)
        {
            for (int i = offset; i < data.Length; i++)
            {
                if (data[i] == 0) return i;
            }
            return data.Length;
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
                str += (item.DataTypeCol == "binary" ? item.ValueCol.Replace(Environment.NewLine, " ") : item.ValueCol) + newline;

            Clipboard.SetDataObject(str);
            Clipboard.Flush();
        }

        private void DataTypeView_CopyAllCols_Click(object sender, RoutedEventArgs e)
        {
            // determine width to align tab character to
            int typeWidth = "DataType".Length, valWidth = "Cannot parse.".Length, offsetWidth = "Offset (hex)".Length;
            foreach (StructListItem item in DataTypeListView.SelectedItems)
            {
                typeWidth = item.DataTypeCol?.Length > typeWidth ? item.DataTypeCol.Length : typeWidth;
                valWidth = item.ValueCol?.Length > valWidth ? item.ValueCol.Length : valWidth;
                offsetWidth = item.OffsetCol?.Length > offsetWidth ? item.OffsetCol.Length : offsetWidth;
            }

            // format string
            String fstr = $"{{0,-{typeWidth}}}\t|\t{{1,-{valWidth}}}\t|\t{{2,-{offsetWidth}}}{{3}}";

            // start the string with header
            String str = String.Format(fstr, "DataType", "Value", "Offset (hex)", Environment.NewLine);
            // add each entry
            foreach (StructListItem item in DataTypeListView.SelectedItems)
            {
                var valStr = item.ValueCol;

                // align binary
                // 11111111 00000000
                // 00000000 11111111
                if (item.DataTypeCol == "binary")
                {
                    str += String.Format(fstr, item.DataTypeCol, "", item.OffsetCol + "h", Environment.NewLine);

                    var re = new System.Text.RegularExpressions.Regex(Environment.NewLine);
                    var rows = re.Split(valStr);

                    foreach (var row in rows)
                        str += String.Format(fstr, "", row, "", Environment.NewLine);
                }
                else
                    str += String.Format(fstr, item.DataTypeCol, valStr, item.OffsetCol + "h", Environment.NewLine);
            }
            Clipboard.SetDataObject(str);
            Clipboard.Flush();
        }
    }
}
