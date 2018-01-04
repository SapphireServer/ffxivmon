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
using Microsoft.VisualBasic;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;

namespace FFXIVMonReborn
{
    /// <summary>
    /// Interaktionslogik für XivMonTab.xaml
    /// </summary>
    public partial class XivMonTab : UserControl
    {
        private TabItem _thisTabItem;
        private MainWindow _mainWindow;

        private MachinaCaptureWorker _captureWorker;
        private Thread _captureThread;

        private MemoryStream _currentPacketStream;

        private Database _db;

        private string _repo;

        private string _filterString = "";
        private string _currentXmlFile = "";

        public XivMonTab()
        {
            InitializeComponent();
            _repo = Properties.Settings.Default.RepoUrl;
            _db = new Database(_repo);

            if (!string.IsNullOrEmpty(_currentXmlFile))
            {
                ChangeTitle(System.IO.Path.GetFileNameWithoutExtension(_currentXmlFile));
                var packets = CaptureFileOp.Load(_currentXmlFile);
                foreach (PacketListItem packet in packets)
                {
                    AddPacketToListView(packet);
                }
            }

            UpdateInfoLabel();
        }

        #region General
        private void UpdateInfoLabel()
        {
            CaptureInfoLabel.Content = "Amount of Packets: " + PacketListView.Items.Count;

            if (_currentXmlFile.Length != 0)
                CaptureInfoLabel.Content += " | File: " + _currentXmlFile;
            if (_captureWorker != null)
                CaptureInfoLabel.Content += " | Capturing ";
            else
                CaptureInfoLabel.Content += " | Idle";
            if (_currentPacketStream != null)
                CaptureInfoLabel.Content += " | Packet Length: 0x" + _currentPacketStream.Length.ToString("X");
        }

        public void SetParents(TabItem me, MainWindow mainWindow)
        {
            _thisTabItem = me;
            _mainWindow = mainWindow;
        }

        public void ClearCapture()
        {
            if (_captureWorker != null)
            {
                MessageBox.Show("A capture is in progress.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            PacketListView.SelectedIndex = -1;
            PacketListView.Items.Clear();

            _currentPacketStream = new MemoryStream(new byte[] { });
            //HexEditor.Stream = currentPacketStream; //why does this crash sometimes

            _filterString = "";

            _currentXmlFile = "";
            ChangeTitle("");

            UpdateInfoLabel();
        }

        private void ChangeTitle(string newTitle)
        {
            string windowTitle = string.IsNullOrEmpty(newTitle) ? "FFXIVMonReborn" : newTitle;
            windowTitle = !windowTitle.Contains("FFXIVMonReborn") ? "FFXIVMonReborn - " + windowTitle : windowTitle;
            _mainWindow.Title = windowTitle;

            string header = string.IsNullOrEmpty(newTitle) ? "New Capture" : newTitle;

            if (_captureWorker != null)
                header = "• " + header;
            
            _thisTabItem.Header = header;
        }

        public void OnTabFocus()
        {
            ChangeTitle(System.IO.Path.GetFileNameWithoutExtension(_currentXmlFile));
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

        public bool RequestClose()
        {
            if (_captureWorker != null)
            {
                MessageBox.Show("A capture is in progress - you cannot close this tab now.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return false;
            }

            if (PacketListView.Items.Count != 0 && _currentXmlFile == "")
            {
                MessageBoxResult res = MessageBox.Show("Currently captured packets were not yet saved.\nDo you want to close without saving?", "Unsaved Packets", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.No)
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsCloseAllowed()
        {
            if (_captureWorker != null)
            {
                return false;
            }

            if (PacketListView.Items.Count != 0 && _currentXmlFile == "")
            {
                return false;
            }

            return true;
        }

        public bool IsCapturing()
        {
            if (_captureWorker != null)
                return true;
            return false;
        }
        #endregion

        #region CaptureHandling
        public void StartCapture()
        {
            if (_captureWorker != null)
                return;

            ClearCapture();

            _captureWorker = new MachinaCaptureWorker(this, _mainWindow.CaptureMode, _mainWindow.CaptureFlags);
            _captureThread = new Thread(_captureWorker.Run);

            _captureThread.Start();

            UpdateInfoLabel();
            ChangeTitle(_currentXmlFile);
        }

        public void StopCapture()
        {
            if (_captureWorker == null)
                return;

            _captureWorker.Stop();
            _captureThread.Join();
            _captureWorker = null;

            UpdateInfoLabel();
            ChangeTitle(_currentXmlFile);
        }
        #endregion

        #region PacketListHandling
        private void PacketListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PacketListView.SelectedIndex == -1)
                return;

            var item = (PacketListItem)PacketListView.Items[PacketListView.SelectedIndex];

            _currentPacketStream = new MemoryStream(item.Data);
            HexEditor.Stream = _currentPacketStream;

            StructListView.Items.Clear();

            try
            {
                var structText = _db.GetServerZoneStruct(int.Parse(item.MessageCol, NumberStyles.HexNumber));

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
            }
            catch (Exception exc)
            {
                new ExtendedErrorView($"[Main] Struct error! Could not get struct for {item.NameCol} - {item.MessageCol}", exc.ToString(), "Error").ShowDialog();
            }

            UpdateInfoLabel();
        }

        public void AddPacketToListView(PacketListItem item)
        {
            if (item.DirectionCol == "S")
            {
                item.NameCol = _db.GetServerZoneOpName(int.Parse(item.MessageCol, NumberStyles.HexNumber));
                item.CommentCol = _db.GetServerZoneOpComment(int.Parse(item.MessageCol, NumberStyles.HexNumber));
            }
            else
            {
                item.NameCol = _db.GetClientZoneOpName(int.Parse(item.MessageCol, NumberStyles.HexNumber));
                item.CommentCol = _db.GetClientZoneOpComment(int.Parse(item.MessageCol, NumberStyles.HexNumber));
            }

            item.CategoryCol = item.Set.ToString();

            if (item.MessageCol == "0142" || item.MessageCol == "0143" || item.MessageCol == "0144")
            {
                using (MemoryStream stream = new MemoryStream(item.Data))
                {
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        stream.Position = 0x20;
                        int category = reader.ReadUInt16();
                        item.ActorControl = category;
                        item.NameCol = _db.GetActorControlTypeName(category);
                    }
                }
            }

            if (_mainWindow.RunScriptsOnNewCheckBox.IsChecked)
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
                    _mainWindow.RunScriptsOnNewCheckBox.IsChecked = false;
                    return;
                }
            }

            PacketListView.Items.Add(item);

            UpdateInfoLabel();
        }
        #endregion

        #region Database
        public void ReloadDB()
        {
            if (_db.Reload())
            {
                ReloadCurrentPackets();
                MessageBox.Show("Database reloaded.", "FFXIVMon Reborn", MessageBoxButton.OK, MessageBoxImage.Asterisk);  
            }
        }
        
        public void RedownloadDefs()
        {
            var res = MessageBox.Show($"Do you want to redownload definition files from the repo? This will override all local changes.", "FFXIVMon Reborn", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (res == MessageBoxResult.OK)
            {
                _db.DownloadDefinitions();
                _db.Reload();
                ReloadCurrentPackets();
                MessageBox.Show($"Definitions downloaded.", "FFXIVMon Reborn", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            }
        }

        public void SetRepository()
        {
            _repo = Interaction.InputBox("Enter the repository URL for the definition files to be downloaded from.\nThis has to point to the raw files.", "FFXIVMon Reborn", Properties.Settings.Default.RepoUrl);
            Properties.Settings.Default.RepoUrl = _repo;
            Properties.Settings.Default.Save();
            _db.SetRepo(_repo);
        }
        #endregion

        #region SaveLoad
        public void SaveCapture()
        {
            if (_captureWorker != null)
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
                _currentXmlFile = fileDialog.FileName;
                ChangeTitle(System.IO.Path.GetFileNameWithoutExtension(_currentXmlFile));
            }

            UpdateInfoLabel();
        }

        public void LoadCapture()
        {
            _currentPacketStream = new MemoryStream(new byte[] { });
            _filterString = "";

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XML|*.xml";
            openFileDialog.Title = "Select a Capture XML file";

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                MessageBoxResult res = MessageBox.Show("No to open in current, Yes to open in new tab.", "Open in new tab?", MessageBoxButton.YesNoCancel);
                if (res == MessageBoxResult.Yes)
                {
                    _mainWindow.AddTab(openFileDialog.FileName);
                    return;
                }
                else if (res == MessageBoxResult.No)
                {
                    LoadCapture(openFileDialog.FileName);
                }
                else
                {
                    return;
                }

            }

            UpdateInfoLabel();
        }

        public void LoadCapture(string path)
        {
            PacketListView.Items.Clear();
            _currentXmlFile = path;
            ChangeTitle(System.IO.Path.GetFileNameWithoutExtension(_currentXmlFile));

            var packets = CaptureFileOp.Load(path);
            foreach (PacketListItem packet in packets)
            {
                AddPacketToListView(packet);
            }
        }
        #endregion

        #region PacketExporting
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
                var packet = (PacketListItem)PacketListView.Items[PacketListView.SelectedIndex];

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

        private void ExportSelectedPacketSetToDat(object sender, RoutedEventArgs e)
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
                var startPacket = (PacketListItem)PacketListView.Items[PacketListView.SelectedIndex];

                List<byte[]> packets = new List<byte[]>();
                packets.Add(startPacket.Data);

                int at = PacketListView.SelectedIndex - 1;
                while (true)
                {
                    if (((PacketListItem)PacketListView.Items[at]).Set == startPacket.Set)
                        packets.Insert(0, ((PacketListItem)PacketListView.Items[at]).Data);
                    else
                        break;
                    at--;
                }

                at = PacketListView.SelectedIndex + 1;
                while (true)
                {
                    if (((PacketListItem)PacketListView.Items[at]).Set == startPacket.Set)
                        packets.Add(((PacketListItem)PacketListView.Items[at]).Data);
                    else
                        break;
                    at++;
                }

                Console.WriteLine(packets.Count);

                var fileDialog = new System.Windows.Forms.SaveFileDialog();
                fileDialog.Filter = "DAT|*.dat";
                var result = fileDialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    File.WriteAllBytes(fileDialog.FileName, InjectablePacketBuilder.BuildSet(packets));
                    MessageBox.Show($"Packet Set containing {packets.Count} packets saved to {fileDialog.FileName}.", "FFXIVMon Reborn", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                }
            }
            else
            {
                MessageBox.Show("Please only select one packet.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
        }
        #endregion

        #region Filtering

        public void SetFilter()
        {

            string filter = Interaction.InputBox("Enter the packet filter.\nFormat(hex): {opcode};_S({string});_A({actorcontrol}); . . .", "FFXIVMon Reborn", _filterString);

            _ApplyFilter(filter);
        }

        public void ResetToOriginal()
        {
            _ResetFilter();
        }

        private void _ResetFilter()
        {
            _filterString = "";
            PacketListView.Items.Filter = null;

            ExtensionMethods.Refresh(PacketListView);
        }

        private void _ApplyFilter(string filter)
        {
            _filterString = filter;

            if (_filterString == "")
            {
                _ResetFilter();
                return;
            }

            FilterSet[] filters = null;
            try
            {
                filters = Filter.Parse(_filterString);
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

        private void FilterEntry_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _ApplyFilter(FilterEntry.Text);
            }

        }
        #endregion

        #region Scripting
        public void Scripting_RunOnCapture()
        {
            var res = MessageBox.Show("Do you want to execute scripts on shown packets? This can take some time, depending on the amount of packets.\n\nPackets: " + PacketListView.Items.Count, "FFXIVMon Reborn", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (res == MessageBoxResult.OK)
            {
                if (_mainWindow.ScriptProvider == null)
                {
                    _mainWindow.ScriptProvider = new Scripting();
                    _mainWindow.ScriptProvider.LoadScripts(System.IO.Path.Combine(Environment.CurrentDirectory, "Scripts"));
                }

                try
                {
                    foreach (var item in PacketListView.Items)
                    {
                        if (((PacketListItem)item).IsVisible)
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
                    _mainWindow.RunScriptsOnNewCheckBox.IsChecked = false;
                    return;
                }

            }
        }

        private void Scripting_RunOnPacket(PacketEventArgs args)
        {
            if (_mainWindow.ScriptProvider == null)
            {
                MessageBox.Show("No scripts were loaded.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                _mainWindow.RunScriptsOnNewCheckBox.IsChecked = false;
                return;
            }

            _mainWindow.ScriptProvider.ExecuteScripts(null, args);
        }
        #endregion

        #region StructListHandling
        private void StructListView_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            foreach (var item in StructListView.Items)
            {
                if (((StructListItem)item).NameCol.StartsWith("  "))
                    ((StructListItem)item).IsVisible = !((StructListItem)item).IsVisible;
            }

            StructListView.Items.Refresh();
            StructListView.Refresh();
        }

        private void StructListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (StructListView.Items.Count == 0)
                return;

            var item = (StructListItem)StructListView.Items[StructListView.SelectedIndex];
            HexEditor.SetPosition(item.offset);
            HexEditor.SelectionStart = item.offset;
            HexEditor.SelectionStop = item.offset + item.typeLength;
        }
        #endregion
    }
}

