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
    /// Interaction logic for TemplateView.xaml
    /// </summary>
    public partial class TemplateView : Page
    {
        List<StructListItem> _structs;

        public TemplateView()
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
            this.TemplateView1.Items.Clear();
        }

        public void Apply(byte[] data, int offset)
        {
            Reset();
            foreach (var type in _structs)
            {
                StructListItem temp = new StructListItem();
                temp.DataTypeCol = type.DataTypeCol;
                temp.ValueCol = type.ValueCol;

                if (data.Length >= type.typeLength)
                {
                    try
                    {
                        if (temp.DataTypeCol == "int8_t")
                            temp.ValueCol = String.Format("{0}", BitConverter.ToChar(data, offset));
                        else if (temp.DataTypeCol == "uint8_t")
                            temp.ValueCol = String.Format("{0}", data[offset]);
                        else if (temp.DataTypeCol == "int16_t")
                            temp.ValueCol = String.Format("{0}", BitConverter.ToInt16(data, offset));
                        else if (temp.DataTypeCol == "uint16_t")
                            temp.ValueCol = String.Format("{0}", BitConverter.ToUInt16(data, offset));
                        else if (temp.DataTypeCol == "int32_t")
                            temp.ValueCol = String.Format("{0}", BitConverter.ToInt32(data, offset));
                        else if (temp.DataTypeCol == "uint32_t")
                            temp.ValueCol = String.Format("{0}", BitConverter.ToUInt32(data, offset));
                        else if (temp.DataTypeCol == "int64_t")
                            temp.ValueCol = String.Format("{0}", BitConverter.ToInt64(data, offset));
                        else if (temp.DataTypeCol == "uint64_t")
                            temp.ValueCol = String.Format("{0}", BitConverter.ToUInt64(data, offset));
                        else if (temp.DataTypeCol == "float")
                            temp.ValueCol = String.Format("{0}", BitConverter.ToDouble(data, offset));
                        else if (temp.DataTypeCol == "time_t")
                            temp.ValueCol = String.Format("{0}", DateTime.FromFileTime(BitConverter.ToUInt32(data, offset)));
                        else if (temp.DataTypeCol == "string")
                            temp.ValueCol = BitConverter.ToString(data, offset);
                    }
                    catch (Exception e)
                    {
                        
                    }
                }
                this.TemplateView1.Items.Add(temp);
            }
        }
    }
}
