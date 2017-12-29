using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Machina;
using Machina.FFXIV;
using Microsoft.VisualBasic;
using WpfHexaEditor;
using MessageBox = System.Windows.MessageBox;
using System.Runtime.InteropServices;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace FFXIVMonReborn
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MachinaCaptureWorker captureWorker;
        private Thread captureThread;

        private MemoryStream currentPacketStream;

        private Database db;

        private string repo;
        private TCPNetworkMonitor.NetworkMonitorType captureMode;

        private string filterString = "";
        private string currentXmlFile;
        private System.Threading.Timer hotkeyTimer;

        KeyboardHook hook = new KeyboardHook();

        private Scripting _scripting = null;

        public MainWindow()
        {
            ParseCommandlineArguments();
            InitializeComponent();
            repo = Properties.Settings.Default.RepoUrl;
            db = new Database(repo);

            captureMode = (TCPNetworkMonitor.NetworkMonitorType) Properties.Settings.Default.NetworkMonitorType;

            if (captureMode == TCPNetworkMonitor.NetworkMonitorType.RawSocket)
                SwitchModeSockets.IsChecked = true;
            else
                SwitchModePcap.IsChecked = true;

            // register the event that is fired after the key press.
            hook.KeyPressed +=
                new EventHandler<KeyPressedEventArgs>(hook_KeyPressed);
            // register the control + alt + F12 combination as hot key.

            hotkeyTimer = new System.Threading.Timer(TryAssignHotkey, null, 0, 500);

            if (!string.IsNullOrEmpty(currentXmlFile))
            {
                ChangeTitle(System.IO.Path.GetFileNameWithoutExtension(currentXmlFile));
                var packets = CaptureFileOp.Load(currentXmlFile);
                foreach (PacketListItem packet in packets)
                {
                    AddPacketToListView(packet);
                }
            }

            //var test = Struct.Parse(db.GetServerZoneStruct(0x143), new byte[] {});
        }

        private void TryAssignHotkey(object state)
        {
            try
            {
                hook.RegisterHotKey(ModifierKeys.Control,
                    Keys.F12);
            }
            catch (Exception) { } //Hook already registered, or something weird happened
        }

        private void ParseCommandlineArguments()
        {
            var args = Environment.GetCommandLineArgs();
            for (var i = 1; i + 1 < args.Length; i += 2)
            {
                if (args[i] == "--xml")
                {
                    currentXmlFile = args[i + 1];
                }
            }
        }

        void hook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            if(captureWorker == null)
                StartCapture(null, null);
            else
                StopCapture(null, null);

            System.Media.SystemSounds.Asterisk.Play();
        }

        public void AddPacketToListView(PacketListItem item)
        {
            if (item.DirectionCol == "S")
            {
                item.NameCol = db.GetServerZoneOpName(int.Parse(item.MessageCol, NumberStyles.HexNumber));
                item.CommentCol = db.GetServerZoneOpComment(int.Parse(item.MessageCol, NumberStyles.HexNumber));
            }
            else
            {
                item.NameCol = db.GetClientZoneOpName(int.Parse(item.MessageCol, NumberStyles.HexNumber));
                item.CommentCol = db.GetClientZoneOpComment(int.Parse(item.MessageCol, NumberStyles.HexNumber));
            }

            item.CategoryCol = item.Set.ToString();

            if (item.MessageCol == "0142" || item.MessageCol == "0143" || item.MessageCol == "0144")
            {
                using(MemoryStream stream = new MemoryStream(item.Data)) {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        stream.Position = 0x20;
                        int category = reader.ReadUInt16();
                        item.ActorControl = category;
                        item.NameCol = db.GetActorControlTypeName(category);
                    }
                }
            }

            if (RunScriptsOnNewCheckBox.IsChecked)
            {
                try
                {
                    Scripting_RunOnPacket(new PacketEventArgs(item));
                }
                catch (Exception exc)
                {
                    MessageBox.Show(
                        $"[Main] Script error!\n\n{exc}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    RunScriptsOnNewCheckBox.IsChecked = false;
                    return;
                }
            }
            
            PacketListView.Items.Add(item);
        }

        private void StartCapture(object sender, RoutedEventArgs e)
        {
            if(captureWorker != null)
                return;

            ClearCapture(null, null);
            
            captureWorker = new MachinaCaptureWorker(this, (TCPNetworkMonitor.NetworkMonitorType)captureMode);
            captureThread = new Thread(captureWorker.Run);

            captureThread.Start();
        }

        private void StopCapture(object sender, RoutedEventArgs e)
        {
            if(captureWorker == null)
                return;

            captureWorker.Stop();
            captureThread.Join();
            captureWorker = null;
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (captureWorker != null)
            {
                e.Cancel = true;
                MessageBox.Show("A capture is in progress - you cannot close this window now.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            if (PacketListView.Items.Count != 0 && currentXmlFile == "")
            {
                MessageBoxResult res = MessageBox.Show("Currently captured packets were not yet saved.\nDo you want to quit without saving?", "Unsaved Packets", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.No)
                {
                    e.Cancel = true;
                }
            }
        }

        private void PacketListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(PacketListView.SelectedIndex == -1)
                return;

            var item = (PacketListItem)PacketListView.Items[PacketListView.SelectedIndex];

            currentPacketStream = new MemoryStream(item.Data);
            HexEditor.Stream = currentPacketStream;

            StructListView.Items.Clear();

            try
            {
                var structText = db.GetServerZoneStruct(int.Parse(item.MessageCol, NumberStyles.HexNumber));

                if (structText == null)
                {
                    StructListItem infoItem = new StructListItem();
                    infoItem.NameCol = "No Struct found";
                    StructListView.Items.Add(infoItem);
                    return;
                }

                var structEntries = Struct.Parse(structText, item.Data);

                foreach (var entry in structEntries.Item1)
                {
                    StructListView.Items.Add(entry);
                }

                //var test = structEntries.Item2;

                //Console.WriteLine(test);

            }
            catch (Exception exc)
            {
                MessageBox.Show(
                    $"[Main] Struct error! Could not get struct for {item.NameCol} - {item.MessageCol}\n\n{exc}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearCapture(object sender, RoutedEventArgs e)
        {
            if (captureWorker != null)
            {
                MessageBox.Show("A capture is in progress.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            PacketListView.SelectedIndex = -1;
            PacketListView.Items.Clear();
            
            currentPacketStream = new MemoryStream(new byte[] {});
            //HexEditor.Stream = currentPacketStream; //why does this crash sometimes

            filterString = "";
            
            currentXmlFile = "";
            ChangeTitle("");
        }

        private void ReloadDB(object sender, RoutedEventArgs e)
        {
            db.Reload();
            ReloadCurrentPackets();
            MessageBox.Show("Database reloaded.", "FFXIVMon Reborn", MessageBoxButton.OK, MessageBoxImage.Asterisk);
        }

        private void SaveCapture(object sender, RoutedEventArgs e)
        {
            if (captureWorker != null)
            {
                MessageBox.Show("A capture is in progress.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var fileDialog = new System.Windows.Forms.SaveFileDialog();
            fileDialog.Filter = "XML|*.xml";
            var result = fileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                CaptureFileOp.Save(PacketListView.Items, fileDialog.FileName);
                MessageBox.Show($"Capture saved to {fileDialog.FileName}.", "FFXIVMon Reborn", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                currentXmlFile = fileDialog.FileName;
                ChangeTitle(System.IO.Path.GetFileNameWithoutExtension(currentXmlFile));
            }
        }

        private void LoadCapture(object sender, RoutedEventArgs e)
        {
            currentPacketStream = new MemoryStream(new byte[] { });
            filterString = "";

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XML|*.xml";
            openFileDialog.Title = "Select a Capture XML file";

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                MessageBoxResult res = MessageBox.Show("No to open in current, Yes to open in new window.", "Open in new window?", MessageBoxButton.YesNoCancel);
                if (res == MessageBoxResult.Yes)
                {
                    Process.Start(System.IO.Path.Combine(Environment.CurrentDirectory, System.AppDomain.CurrentDomain.FriendlyName), "--xml \"" + openFileDialog.FileName + "\"");
                    return;
                }
                else if (res == MessageBoxResult.No)
                {
                    PacketListView.Items.Clear();
                }
                else
                {
                    return;
                }
                currentXmlFile = openFileDialog.FileName;
                ChangeTitle(System.IO.Path.GetFileNameWithoutExtension(currentXmlFile));

                var packets = CaptureFileOp.Load(openFileDialog.FileName);
                foreach (PacketListItem packet in packets)
                {
                    AddPacketToListView(packet);
                }
            }
        }

        private void ChangeTitle(string newTitle)
        {
            newTitle = string.IsNullOrEmpty(newTitle) ? "FFXIVMonReborn" : newTitle;
            newTitle = !newTitle.Contains("FFXIVMonReborn") ? "FFXIVMonReborn - " + newTitle : newTitle;
            this.Title = newTitle;
        }

        private void ExportSelectedPacketToDat(object sender, RoutedEventArgs e)
        {
            var items = PacketListView.SelectedItems;
            
            if (items.Count == 0)
            {
                MessageBox.Show("No packet selected.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            else if (items.Count == 1)
            {
                var packet = (PacketListItem) PacketListView.Items[PacketListView.SelectedIndex];

                var fileDialog = new System.Windows.Forms.SaveFileDialog();
                fileDialog.Filter = "DAT|*.dat";
                var result = fileDialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    File.WriteAllBytes(fileDialog.FileName, InjectablePacketBuilder.BuildSingle(packet.Data));
                    MessageBox.Show($"Packet saved to {fileDialog.FileName}.", "FFXIVMon Reborn", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                }
            }
            else
            {
                FolderBrowserDialog dialog = new FolderBrowserDialog();
                    
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    int count = 0;
                    foreach (PacketListItem item in items)
                    {
                        File.WriteAllBytes(System.IO.Path.Combine(dialog.SelectedPath, $"{item.MessageCol}-{String.Join("_", item.TimeStampCol.Split(System.IO.Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.')}-No{count}.dat"),
                            InjectablePacketBuilder.BuildSingle(item.Data));
                        
                        count++;
                    }
                    MessageBox.Show($"Packets saved to {dialog.SelectedPath}.", "FFXIVMon Reborn", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                }
            }
        }

        private void ReloadCurrentPackets()
        {
            PacketListItem[] array = new PacketListItem[PacketListView.Items.Count];
            PacketListView.Items.CopyTo(array, 0);
            PacketListView.Items.Clear();

            foreach (var item in array)
            {
                AddPacketToListView(item);
            }
        }

        private void StructListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(StructListView.Items.Count == 0)
                return;

            var item = (StructListItem)StructListView.Items[StructListView.SelectedIndex];
            HexEditor.SetPosition(item.offset);
            HexEditor.SelectionStart = item.offset;
            HexEditor.SelectionStop = item.offset + item.typeLength;
        }

        private void RedownloadDefs(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show($"Do you want to redownload definition files from the repo? This will override all local changes.", "FFXIVMon Reborn", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (res == MessageBoxResult.OK)
            {
                db.DownloadDefinitions();
                db.Reload();
                ReloadCurrentPackets();
                MessageBox.Show($"Definitions downloaded.", "FFXIVMon Reborn", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            }
        }

        private void SetRepository(object sender, RoutedEventArgs e)
        {
            repo = Interaction.InputBox("Enter the repository URL for the definition files to be downloaded from.\nThis has to point to the raw files.", "FFXIVMon Reborn", Properties.Settings.Default.RepoUrl);
            Properties.Settings.Default.RepoUrl = repo;
            Properties.Settings.Default.Save();
            db.SetRepo(repo);
        }

        private void SwitchModeSockets_OnClick(object sender, RoutedEventArgs e)
        {
            if (captureWorker != null)
            {
                MessageBox.Show("A capture is in progress.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            captureMode = TCPNetworkMonitor.NetworkMonitorType.RawSocket;
            SwitchModePcap.IsChecked = false;
            SwitchModeSockets.IsChecked = true;

            Properties.Settings.Default.NetworkMonitorType = captureMode;
            Properties.Settings.Default.Save();
        }

        private void SwitchModePcap_OnClick(object sender, RoutedEventArgs e)
        {
            if (captureWorker != null)
            {
                MessageBox.Show("A capture is in progress.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            captureMode = TCPNetworkMonitor.NetworkMonitorType.WinPCap;
            SwitchModePcap.IsChecked = true;
            SwitchModeSockets.IsChecked = false;

            Properties.Settings.Default.NetworkMonitorType = captureMode;
            Properties.Settings.Default.Save();
        }

        private void SetFilter(object sender, RoutedEventArgs e)
        {

            string filter = Interaction.InputBox("Enter the packet filter.\nFormat(hex): {opcode};_S({string});_A({actorcontrol}); . . .", "FFXIVMon Reborn", filterString);

            _ApplyFilter(filter);
        }

        private void ResetToOriginal(object sender, RoutedEventArgs e)
        {
            _ResetFilter();
        }

        private void _ResetFilter()
        {
            filterString = "";
            PacketListView.Items.Filter = null;

            ExtensionMethods.Refresh(PacketListView);
        }

        private void _ApplyFilter(string filter)
        {
            filterString = filter;

            if (filterString == "")
            {
                _ResetFilter();
                return;
            }

            FilterSet[] filters = null;
            try
            {
                filters = Filter.Parse(filterString);
            }
            catch (Exception exc)
            {
                MessageBox.Show(
                    $"[Main] Filter Parse error!\n\n{exc}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            PacketListView.Items.Filter = new Predicate<object>((object item) =>
            {
                bool predResult = false;
                foreach (var filterEntry in filters)
                {
                    predResult = filterEntry.IsApplicableForFilterSet((PacketListItem)item);

                    if (predResult)
                        return predResult;
                }

                return predResult;
            });

            PacketListView.Refresh();
            PacketListView.Items.Refresh();
        }

        private void ResetFilter_Click(object sender, RoutedEventArgs e)
        {
            _ResetFilter();
        }

        private void Scripting_RunOnCapture(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("Do you want to execute scripts on shown packets? This can take some time, depending on the amount of packets.\n\nPackets: " + PacketListView.Items.Count, "FFXIVMon Reborn", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (res == MessageBoxResult.OK)
            {
                if (_scripting == null)
                {
                    _scripting = new Scripting();
                    _scripting.LoadScripts(System.IO.Path.Combine(Environment.CurrentDirectory, "Scripts"));
                }
                
                try
                {
                    foreach (var item in PacketListView.Items)
                    {
                        if (((PacketListItem) item).IsVisible)
                        {
                            PacketEventArgs args = new PacketEventArgs((PacketListItem)item);

                            Scripting_RunOnPacket(args);
                        }
                    }
                    MessageBox.Show("Scripts ran successfully.", "FFXIVMon Reborn", MessageBoxButton.OK,
                        MessageBoxImage.Asterisk);
                }
                catch (Exception exc)
                {
                    MessageBox.Show(
                        $"[Main] Script error!\n\n{exc}",
                        "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    RunScriptsOnNewCheckBox.IsChecked = false;
                    return;
                }

            }
        }

        private void Scripting_RunOnPacket(PacketEventArgs args)
        {
            if (_scripting == null)
            {
                MessageBox.Show("No scripts were loaded.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                RunScriptsOnNewCheckBox.IsChecked = false;
                return;
            }
            
            _scripting.ExecuteScripts(null, args);
        }

        private void Scripting_LoadScripts(object sender, RoutedEventArgs e)
        {
            _scripting = new Scripting();
            _scripting.LoadScripts(System.IO.Path.Combine(Environment.CurrentDirectory, "Scripts"));
        }

        private void FilterEntry_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _ApplyFilter(FilterEntry.Text);
            }

        }

        private void NewInstance(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(System.Windows.Forms.Application.ExecutablePath);
        }

        private void StructListView_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            foreach (var item in StructListView.Items)
            {
                if (((StructListItem) item).NameCol.StartsWith("  "))
                    ((StructListItem) item).IsVisible = !((StructListItem)item).IsVisible;
            }
            
            StructListView.Items.Refresh();
            StructListView.Refresh();
        }
    }

    public static class ExtensionMethods
    {
        private static Action EmptyDelegate = delegate () { };

        public static void Refresh(this UIElement uiElement)
        {
            uiElement.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
        }
    }
}
