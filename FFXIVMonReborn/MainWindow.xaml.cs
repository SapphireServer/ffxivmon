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

        private bool isFiltered = false;

        KeyboardHook hook = new KeyboardHook();

        public MainWindow()
        {
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
            hook.RegisterHotKey(ModifierKeys.Control,
                Keys.F12);

            //var test = Struct.Parse(db.GetServerZoneStruct(0x143), new byte[] {});
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

            PacketListView.Items.Add(item);
        }

        private void StartCapture(object sender, RoutedEventArgs e)
        {
            if(captureWorker != null)
                return;

            ClearCapture(null, null);
            
            captureWorker = new MachinaCaptureWorker(this, TCPNetworkMonitor.NetworkMonitorType.RawSocket);
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

                foreach (var entry in structEntries)
                {
                    StructListView.Items.Add(entry);
                }
                
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

            isFiltered = false;
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
            }
        }

        private void LoadCapture(object sender, RoutedEventArgs e)
        {
            PacketListView.Items.Clear();
            currentPacketStream = new MemoryStream(new byte[] { });
            isFiltered = false;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XML|*.xml";
            openFileDialog.Title = "Select a Capture XML file";

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var packets = CaptureFileOp.Load(openFileDialog.FileName);
                foreach (PacketListItem packet in packets)
                {
                    AddPacketToListView(packet);
                }
            }
        }

        private void ExportSelectedPacketToDat(object sender, RoutedEventArgs e)
        {
            if (PacketListView.SelectedIndex == -1)
            {
                MessageBox.Show("No packet selected.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            var packet = (PacketListItem) PacketListView.Items[PacketListView.SelectedIndex];

            var fileDialog = new System.Windows.Forms.SaveFileDialog();
            fileDialog.Filter = "DAT|*.dat";
            var result = fileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                File.WriteAllBytes(fileDialog.FileName, packet.Data);
                MessageBox.Show($"Packet saved to {fileDialog.FileName}.", "FFXIVMon Reborn", MessageBoxButton.OK, MessageBoxImage.Asterisk);
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

            Properties.Settings.Default.NetworkMonitorType = (int)captureMode;
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

            Properties.Settings.Default.NetworkMonitorType = (int) captureMode;
            Properties.Settings.Default.Save();
        }

        private void ApplyFilterSet(FilterSet set)
        {
            foreach (var packetListEntry in PacketListView.Items.OfType<PacketListItem>())
            {
                switch (set.type)
                {
                    case FilterType.Message:
                        if (packetListEntry.MessageCol == ((int)set.value).ToString("X4"))
                        {
                            packetListEntry.IsVisible = true;
                        }
                        break;
                    case FilterType.ActorControl:
                        if (packetListEntry.ActorControl == (int)set.value)
                        {
                            packetListEntry.IsVisible = true;
                        }
                        break;
                    case FilterType.ActorControlName:
                        if (packetListEntry.ActorControl != -1 && packetListEntry.NameCol.ToLower().Contains(((string)set.value).ToLower()))
                        {
                            packetListEntry.IsVisible = true;
                        }
                        break;
                    case FilterType.PacketName:
                        if (packetListEntry.NameCol.ToLower().Contains(((string)set.value).ToLower()))
                        {
                            packetListEntry.IsVisible = true;
                        }
                        break;
                    case FilterType.StringContents:
                        var findStr = Convert.ToString(set.value).ToLower();
                        var packetStr = Encoding.UTF8.GetString(packetListEntry.Data).ToLower();
                        if (packetStr.Contains(findStr))
                        {
                            packetListEntry.IsVisible = true;
                        }
                        break;
                }
            }
        }

        private void SetFilter(object sender, RoutedEventArgs e)
        {
            // todo: use FilterTextBox in favour of this
            if (isFiltered)
            {
                MessageBox.Show("Please reset Filters first.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            string filter = Interaction.InputBox("Enter the packet filter.\nFormat(hex): {opcode};_S({string});_A({actorcontrol}); . . .", "FFXIVMon Reborn", "");

            _ApplyFilter(filter);
        }

        private void ResetToOriginal(object sender, RoutedEventArgs e)
        {
            _ResetFilter();
        }

        private void _ResetFilter()
        {
            isFiltered = false;

            foreach (var item in PacketListView.Items)
            {
                ((PacketListItem)item).IsVisible = true;
            }

            ExtensionMethods.Refresh(PacketListView);
        }

        private void _ApplyFilter(string filter)
        {
            if (filter == "")
            {
                _ResetFilter();
            }
            FilterSet[] filters = null;
            try
            {
                filters = Filter.Parse(filter);
            }
            catch (Exception exc)
            {
                MessageBox.Show(
                    $"[Main] Filter Parse error!\n\n{exc}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }


            isFiltered = true;

            foreach (var item in PacketListView.Items)
            {
                ((PacketListItem)item).IsVisible = false;
            }

            foreach (var filterEntry in filters)
            {
                ApplyFilterSet(filterEntry);
            }

            ExtensionMethods.Refresh(PacketListView);
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
                Scripting scripting = new Scripting();
                scripting.LoadScripts(System.IO.Path.Combine(Environment.CurrentDirectory, "Scripts"));

                try
                {
                    foreach (var item in PacketListView.Items)
                    {
                        if (((PacketListItem) item).IsVisible)
                        {
                            PacketEventArgs args = new PacketEventArgs((PacketListItem)item);

                            scripting.ExecuteScripts(null, args);
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
                    return;
                }

            }
        }

        private void FilterEntry_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _ApplyFilter(FilterEntry.Text);
            }

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
