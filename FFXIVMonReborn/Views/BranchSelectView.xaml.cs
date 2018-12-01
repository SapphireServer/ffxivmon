using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
using Microsoft.VisualBasic;

namespace FFXIVMonReborn.Views
{
    /// <summary>
    /// Interaktionslogik für ExtendedErrorView.xaml
    /// </summary>
    public partial class BranchSelectView : Window
    {
        private string _commitHash;
        
        public BranchSelectView(GitHubBranch[] branches)
        {
            InitializeComponent();
            
            foreach (var gitHubBranch in branches)
            {
                BranchListBox.Items.Add(gitHubBranch);
            }

            BranchListBox.SelectedIndex =
                branches.Select((v, i) => new {v, i}).Where(x => x.v.Name == "master").Select(x => x.i).ToArray()[0];
        }

        public string GetSelectedVersion()
        {
            return _commitHash;
        }

        private void ButtonOK_OnClick(object sender, RoutedEventArgs e)
        {
            var selectedBranch = BranchListBox.SelectedItem as GitHubBranch;

            _commitHash = selectedBranch.Commit.Sha;
            this.Close();
        }

        private void EnterCustomCommitHash_OnClick(object sender, RoutedEventArgs e)
        {
            string sha = Interaction.InputBox("Please enter a git commit hash.", "FFXIVMon Reborn");

            if (sha == "")
                return;

            _commitHash = sha;
            this.Close();
        }
    }
}
