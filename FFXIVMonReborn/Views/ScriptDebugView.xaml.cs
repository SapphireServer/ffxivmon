using System.ComponentModel;
using System.Diagnostics;
using System.Windows;

namespace FFXIVMonReborn
{
    /// <summary>
    /// Interaktionslogik für ExtendedErrorView.xaml
    /// </summary>
    public partial class ScriptDebugView : Window
    {
        public ScriptDebugView()
        {
            InitializeComponent();
        }

        public void WriteLine(string line)
        {
            string text = $"{line}\n";
            InfoBox.AppendText(text);
            Debug.WriteLine(text);

            InfoBox.ScrollToEnd();
        }

        private void ScriptDebugView_OnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = Visibility.Hidden;
        }
    }
}
