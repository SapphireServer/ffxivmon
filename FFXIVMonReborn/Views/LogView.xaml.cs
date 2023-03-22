using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;

namespace FFXIVMonReborn
{
    /// <summary>
    /// Interaktionslogik für ExtendedErrorView.xaml
    /// </summary>
    public partial class LogView : Window
    {
        public static LogView Instance { get; set; }

        public LogView()
        {
            InitializeComponent();

            Instance = this;
        }

        public void WriteLine(string line)
        {
            /*Concatenating line endings for the InfoBox.AppendText caused memory consumption and CPU usage to skyrocket
             during Struct parsing thanks to the constant creation of new Strings.
             Given how AppendText does this behind the scenes anyway, this was preferable to a StringBuilder.*/
            InfoBox.Document.Blocks.Add(new Paragraph(new Run(line)));
            Debug.WriteLine(line + "\n");

            /*Unconditionally calling ScrollToEnd causes significant slowdown on AddPacketToListView, thanks to ScrollToEnd
             calling UpdateLayout() regardless of visibility, recreating the layout of the RichTextBox on every state change, 
             which happened to be every Packet addition.*/
            if(IsVisible)
                InfoBox.ScrollToEnd();
        }

        private void ScriptDebugView_OnClosing(object sender, CancelEventArgs e)
        {
            e.Cancel = true;
            this.Visibility = Visibility.Hidden;
        }

        private void LogView_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            //e.NewValue is the new this.IsVisible
            if ((bool) e.NewValue)
                InfoBox.ScrollToEnd();
        }
    }
}
