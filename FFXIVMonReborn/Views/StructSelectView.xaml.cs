using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
using System.Windows.Shapes;
using FFXIVMonReborn.Database;

namespace FFXIVMonReborn
{
    /// <summary>
    /// Interaktionslogik für ExtendedErrorView.xaml
    /// </summary>
    public partial class StructSelectView : Window
    {
        private Dictionary<int, Tuple<string, string>> _opcodes;

        public StructSelectView(Dictionary<int, Tuple<string, string>> opcodes)
        {
            InitializeComponent();
            foreach (var entry in opcodes)
            {
                StructListBox.Items.Add($"{entry.Value.Item1} - {entry.Key.ToString("X4")}");
            }

            StructListBox.SelectedIndex = StructListBox.Items.Count - 1;

            _opcodes = opcodes;
        }

        public int GetSelectedOpCode()
        {
            return _opcodes.ElementAt(StructListBox.SelectedIndex).Key;
        }

        private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
