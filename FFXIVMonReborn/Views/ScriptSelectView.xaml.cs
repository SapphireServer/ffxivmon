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

namespace FFXIVMonReborn
{
    /// <summary>
    /// Interaktionslogik für ExtendedErrorView.xaml
    /// </summary>
    public partial class ScriptSelectView : Window
    {
        public ObservableCollection<CheckedListItem> ScriptList = new ObservableCollection<CheckedListItem>();
        private string _folderPath;

        public ScriptSelectView(string folderPath)
        {
            InitializeComponent();
            ScriptListBox.DataContext = ScriptList;

            var files = Directory.GetFiles(folderPath);

            foreach (var file in files)
            {
                ScriptList.Add(new CheckedListItem() { Name = file });
            }

            _folderPath = folderPath;
        }

        public string[] GetSelectedScripts()
        {
            List<string> output = new List<string>();

            foreach (var script in ScriptList)
            {
                if(script.IsChecked)
                    output.Add(script.Name);
            }

            return output.ToArray();
        }

        private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ButtonReload_OnClick(object sender, RoutedEventArgs e)
        {
            ScriptList.Clear();

            var files = Directory.GetFiles(_folderPath);

            foreach (var file in files)
            {
                ScriptList.Add(new CheckedListItem() { Name = file });
            }
        }
    }

    public class CheckedListItem
    {
        public string Name { get; set; }
        public bool IsChecked { get; set; }
    }
}
