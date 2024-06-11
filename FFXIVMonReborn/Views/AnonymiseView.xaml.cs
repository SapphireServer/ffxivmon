using FFXIVMonReborn.Properties;
using System;
using System.Collections.Generic;
using System.Globalization;
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

namespace FFXIVMonReborn.Views
{
    /// <summary>
    /// Interaction logic for AnonymiseView.xaml
    /// </summary>
    public partial class AnonymiseView : Window
    {
        private string ContentID;
        private string CharacterName;
        private Dictionary<string, string> ReplaceStrings;

        public AnonymiseView()
        {
            InitializeComponent();
            LoadDefaultSettings();
        }

        public string GetContentID()
        {
            return ContentID;
        }

        public string GetCharacterName()
        {
            return CharacterName;
        }

        public Dictionary<string, string> GetReplacementStrings()
        {
            return ReplaceStrings;
        }

        public void LoadDefaultSettings()
        {
            _ContentIDs.Text = Settings.Default.AnonymiseContentID;
            _CharacterNames.Text = Settings.Default.AnonymiseCharacterName;
            _ReplaceStrings.Text = Settings.Default.AnonymiseStrings;
        }

        public void SaveAsDefaultSettings()
        {
            Settings.Default.AnonymiseContentID = _ContentIDs.Text;
            Settings.Default.AnonymiseCharacterName = _CharacterNames.Text;
            Settings.Default.AnonymiseStrings = _ReplaceStrings.Text;

            Settings.Default.Save();
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            var success = false;

            ContentID = "";
            foreach  (var str in _ContentIDs.Text.Split(','))
            {
                var str2 = str.Trim();
                if (str2.Length == 16)
                    ContentID = str2;
            }

            try
            {
                UInt64 val = Convert.ToUInt64("0x" + _ContentIDs.Text.Trim(), 16);
                if (val > 0x0040000000000000 && val < 0x0050000000000000)
                    success = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Invalid ContentID entered. Must be a 8 byte hex string without spaces.");
                success = false;
            }

            CharacterName = _CharacterNames.Text.Trim();

            ReplaceStrings = new Dictionary<string, string>();

            for (var i = 0; i < _ReplaceStrings.LineCount; ++i)
            {
                var find = _ReplaceStrings.GetLineText(i);
                if (string.IsNullOrWhiteSpace(find)) continue;

                var replace = _ReplaceStrings.GetLineText(i + 1);

                ReplaceStrings.Add(find.Trim().ToLower(), replace.Trim());
                ++i;
            }

            if (success)
            {
                SaveAsDefaultSettings();
            }
            this.DialogResult = success;
        }

        private void _ContentIDs_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (this._ParsedContentId == null)
                return;

            try
            {
                UInt64 val = Convert.ToUInt64("0x" + _ContentIDs.Text.Trim(), 16);
                if (val > 0x0040000000000000 && val < 0x0050000000000000)
                    this._ParsedContentId.Content = "UInt64: " + val.ToString();
                else
                    this._ParsedContentId.Content = "UInt64: Invalid";
            }
            catch (Exception ex) { }
        }
    }
}
