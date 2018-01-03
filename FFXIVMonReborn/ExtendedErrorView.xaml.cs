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
using System.Windows.Shapes;

namespace FFXIVMonReborn
{
    /// <summary>
    /// Interaktionslogik für ExtendedErrorView.xaml
    /// </summary>
    public partial class ExtendedErrorView : Window
    {
        public ExtendedErrorView(string message, string info, string title)
        {
            InitializeComponent();

            ErrorMessage.Content = message;
            InfoBox.AppendText(info);
            this.Title = title;
        }

        public ExtendedErrorView(string message, string info, string title, WindowStartupLocation startupLocation)
        {
            this.WindowStartupLocation = startupLocation;
            InitializeComponent();

            ErrorMessage.Content = message;
            InfoBox.AppendText(info);
            this.Title = title;
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
