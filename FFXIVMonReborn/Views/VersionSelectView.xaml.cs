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
using FFXIVMonReborn.Database.GitHub.Model;

namespace FFXIVMonReborn
{
    /// <summary>
    /// Interaktionslogik für ExtendedErrorView.xaml
    /// </summary>
    public partial class VersionSelectView : Window
    {
        public VersionSelectView(GitHubTag[] versions)
        {
            InitializeComponent();
            foreach (var version in versions)
            {
                ScriptListBox.Items.Add($"{version.Name} - {version.TagCommit.Sha}");
            }

            ScriptListBox.Items.Add("Latest");
            ScriptListBox.SelectedIndex = ScriptListBox.Items.Count - 1;
        }

        public int GetSelectedVersion()
        {
            return ScriptListBox.SelectedIndex;
        }

        private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }
    }
}
