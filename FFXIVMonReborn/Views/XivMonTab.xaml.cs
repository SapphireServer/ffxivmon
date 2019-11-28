using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using FFXIVMonReborn.Database;
using Microsoft.VisualBasic;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MessageBox = System.Windows.MessageBox;
using UserControl = System.Windows.Controls.UserControl;
using Be.Windows.Forms;
using System.Linq;
using FFXIVMonReborn.DataModel;
using FFXIVMonReborn.Importers;
using FFXIVMonReborn.LobbyEncryption;
using FFXIVMonReborn.Scripting;
using Machina.FFXIV;

namespace FFXIVMonReborn.Views
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
        private DatabaseParser _db;
        private int _version = -1;
        private string _commitSha = null;

        private LobbyEncryptionProvider _encryptionProvider;

        private string _filterString = "";
        private string _currentXmlFile = "";

        private bool _wasCapturedMs = false;
        private FilterSet[] _filters;

        private List<string> _erroredOpcodes = new List<string>();

        public XivMonTab()
        {
            InitializeComponent();

            try
            {
                if (!string.IsNullOrEmpty(_currentXmlFile))
                {
                    ChangeTitle(System.IO.Path.GetFileNameWithoutExtension(_currentXmlFile));
                    var capture = XmlCaptureImporter.Load(_currentXmlFile);
                    foreach (var packet in capture.Packets)
                    {
                        AddPacketToListView(packet);
                    }
                    _wasCapturedMs = bool.Parse(capture.UsingSystemTime);
                }

                UpdateInfoLabel();
            }
            catch (Exception ex)
            {
                new ExtendedErrorView("Could not load XivMonTab.", ex.ToString(), "Error").ShowDialog();
            }
        }

        public XivMonTab(PacketEntry[] packets)
        {
            InitializeComponent();

            if (!string.IsNullOrEmpty(_currentXmlFile))
            {
                ChangeTitle(System.IO.Path.GetFileNameWithoutExtension("FFXIV Replay"));
                foreach (var packet in packets)
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
            try
            {
                if (_currentPacketStream != null)
                {
                    CaptureInfoLabel.Content += " | Packet Length: 0x" + _currentPacketStream.Length.ToString("X");
                    CaptureInfoLabel.Content += " | Index: " + PacketListView.SelectedIndex;
                }

            }
            catch (ObjectDisposedException) { } // wats this

            if (PacketListView.Items.Count != 0)
                if (_wasCapturedMs)
                    CaptureInfoLabel.Content += " | Using system time";
                else
                    CaptureInfoLabel.Content += " | Using packet time";

            if (_mainWindow != null && !_mainWindow.IsPausedCheckBox.IsChecked)
                CaptureInfoLabel.Content += " | Capture Paused";
            
            var versionInfo = "";
            if (_mainWindow != null)
            {
                if (_commitSha != null)
                    versionInfo = "Commit: " + _commitSha;
                else
                    versionInfo = _mainWindow.VersioningProvider.GetVersionInfo(_version);
                
                CaptureInfoLabel.Content += " | . . .";
            }
            CaptureInfoLabel.ToolTip = versionInfo;
        }

        public void SetParents(TabItem me, MainWindow mainWindow)
        {
            _thisTabItem = me;
            _mainWindow = mainWindow;
        }

        public void ClearCapture(bool silent = false)
        {
            if (silent)
            {
                PacketListView.SelectedIndex = -1;
                PacketListView.Items.Clear();

                _currentPacketStream = new MemoryStream(new byte[] { });
                //HexEditor.Stream = currentPacketStream; //why does this crash sometimes

                _filterString = "";

                _currentXmlFile = "";
                ChangeTitle("");

                UpdateInfoLabel();

                _wasCapturedMs = false;
                return;
            }
            
            MessageBoxResult res = MessageBox.Show("Do you want to clear this capture?", "Unsaved Packets", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                PacketListView.SelectedIndex = -1;
                PacketListView.Items.Clear();

                _currentPacketStream = new MemoryStream(new byte[] { });
                //HexEditor.Stream = currentPacketStream; //why does this crash sometimes

                _filterString = "";

                _currentXmlFile = "";
                ChangeTitle("");

                UpdateInfoLabel();

                _wasCapturedMs = false;
            }
        }

        public void ChangeTitle()
        {
            ChangeTitle(_currentXmlFile == null ? "" : System.IO.Path.GetFileNameWithoutExtension(_currentXmlFile));
        }
        
        public void ChangeTitle(string newTitle)
        {
            string windowTitle = string.IsNullOrEmpty(newTitle) ? $"FFXIVMonReborn({Util.GetGitHash()})" : newTitle;
            windowTitle = !windowTitle.Contains("FFXIVMonReborn") ? $"FFXIVMonReborn({Util.GetGitHash()}) - " + windowTitle : windowTitle;
            if (_mainWindow.IsPausedCheckBox.IsChecked)
                windowTitle += " - PAUSED";
            _mainWindow.Title = windowTitle;

            string header = string.IsNullOrEmpty(newTitle) ? "New Capture" : newTitle;

            if (_captureWorker != null)
                header = "• " + header;
            
            if (_mainWindow.IsPausedCheckBox.IsChecked)
                header += " - PAUSED";

            if(_thisTabItem != null)
                _thisTabItem.Header = header;
        }

        public void OnTabFocus()
        {
            ChangeTitle();
        }

        private void ReloadCurrentPackets()
        {
            var lastIndex = PacketListView.SelectedIndex;

            var array = new PacketEntry[PacketListView.Items.Count];
            PacketListView.Items.CopyTo(array, 0);
            PacketListView.Items.Clear();

            foreach (var item in array)
            {
                AddPacketToListView(item);
            }

            PacketListView.SelectedIndex = lastIndex;
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

            if (_version == -1)
                _version = _mainWindow.VersioningProvider.Api.Tags.Length;
            _commitSha = null;

            _db = _mainWindow.VersioningProvider.GetDatabaseForVersion(_version);

            try
            {
                ClearCapture(true);

                _captureWorker = new MachinaCaptureWorker(this, _mainWindow.CaptureMode, _mainWindow.CaptureFlags);
                _captureThread = new Thread(_captureWorker.Run);

                _captureThread.Start();

                UpdateInfoLabel();
                ChangeTitle(_currentXmlFile);

                _wasCapturedMs = _mainWindow.DontUsePacketTimestamp.IsChecked;
            }
            catch (Exception e)
            {
                new ExtendedErrorView("Failed to initialize MachinaCaptureWorker. Try switching your capture mode.",
                    e.ToString(), "Error").ShowDialog();
                _captureWorker = null;
                _captureThread = null;
            }
        }

        public void StopCapture()
        {
            if (_captureWorker == null)
                return;

            try
            {
                _captureWorker.Stop();
                _captureThread.Join();
                _captureWorker = null;

                UpdateInfoLabel();
                ChangeTitle(_currentXmlFile);
            }
            catch (Exception e)
            {
                new ExtendedErrorView("Failed to shut down MachinaCaptureWorker. Try switching your capture mode.",
                    e.ToString(), "Error").ShowDialog();
                _captureWorker = null;
                _captureThread = null;
            }
        }
        #endregion

        #region PacketListHandling
        private void ApplySpecificStructToPacket(object sender, RoutedEventArgs e)
        {
            if (PacketListView.SelectedIndex == -1)
                return;

            var view = new StructSelectView(_db.ServerZoneIpcType);
            view.ShowDialog();

            var item = (PacketEntry)PacketListView.Items[PacketListView.SelectedIndex];

            StructListView.Items.Clear();

            try
            {
                string structText = null;
                structText = item.Direction == "S" ? _db.GetServerZoneStruct(view.GetSelectedOpCode()) : _db.GetClientZoneStruct(view.GetSelectedOpCode());

                var structProvider = new Struct();
                var structEntries = structProvider.Parse(structText, item.Data);

                foreach (var entry in structEntries.Item1)
                {
                    StructListView.Items.Add(entry);
                }

                if (_mainWindow.ShowObjectMapCheckBox.IsChecked)
                    new ExtendedErrorView("Object map for " + item.Name, structEntries.Item2.Print(), "FFXIVMon Reborn").ShowDialog();
            }
            catch (Exception exc)
            {
                new ExtendedErrorView($"[XivMonTab] Struct error! Could not get struct for {item.Name} - {item.Message}", exc.ToString(), "Error").ShowDialog();
            }

            UpdateInfoLabel();
        }

        private void PacketListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PacketListView.SelectedIndex == -1)
                return;

            var item = (PacketEntry)PacketListView.Items[PacketListView.SelectedIndex];

            var data = item.Data;

            if (Properties.Settings.Default.HideHexBoxActorId)
            {
                byte[] noIdData = new byte[data.Length];
                Array.Copy(data, 0, noIdData, 0, 3);
                Array.Copy(data, 12, noIdData, 12, data.Length - 12);
                data = noIdData;
            }

            _currentPacketStream = new MemoryStream(data);
            try
            {
                HexEditor.ByteProvider = new DynamicByteProvider(data);
            }
            catch (Exception exception)
            {
                new ExtendedErrorView("Failed to load packet.", exception.ToString(), "Error").ShowDialog();
            }


            StructListView.Items.Clear();

            try
            {
                string structText = null;
                structText = item.Direction == "S" ? _db.GetServerZoneStruct(int.Parse(item.Message, NumberStyles.HexNumber)) : _db.GetClientZoneStruct(int.Parse(item.Message, NumberStyles.HexNumber));

                if (structText == null)
                {
                    StructListItem infoItem = new StructListItem { NameCol = "No Struct found" };
                    StructListView.Items.Add(infoItem);
                    return;
                }

                var structProvider = new Struct();
                var structEntries = structProvider.Parse(structText, item.Data);

                var colours = Struct.TypeColours;
                int i = 0;

                
                // Highlight the IPC header lightly grey
                HexEditor.HighlightBytes(0, 0x20, System.Drawing.Color.Black, System.Drawing.Color.LightGray);
                
                foreach (var entry in structEntries.Item1)
                {
                    System.Drawing.Color colour = Struct.TypeColours.ElementAt(i).Value;
                    StructListView.Items.Add(entry);
                    HexEditor.HighlightBytes(entry.offset, entry.typeLength, System.Drawing.Color.Black, colour);
                    ++i;
                    if (i == colours.Count)
                        i = 1;
                }

                if (_mainWindow.ShowObjectMapCheckBox.IsChecked)
                    new ExtendedErrorView("Object map for " + item.Name, structEntries.Item2.Print(), "FFXIVMon Reborn").ShowDialog();
            }
            catch (Exception exc)
            {
#if !DEBUG
                if (_erroredOpcodes.Contains(item.Message))
                {
#endif
                    new ExtendedErrorView($"Struct error! Could not get struct for {item.Name} - {item.Message}", exc.ToString(), "Error").ShowDialog();
#if !DEBUG
                    _erroredOpcodes.Add(item.Message);
                }
#endif

                StructListItem infoItem = new StructListItem { NameCol = "Parsing failed" };
                StructListView.Items.Add(infoItem);
                return;
            }

            UpdateInfoLabel();
        }

        public void AddPacketToListView(PacketEntry item, bool silent = false)
        {
            if (_mainWindow.IsPausedCheckBox.IsChecked && !silent)
                return;
            
            if (_encryptionProvider != null && !item.IsDecrypted &&
                item.Data[0x0C] != 0x09 && item.Data[0x0C] != 0x07 &&
                item.Connection == FFXIVNetworkMonitor.ConnectionType.Lobby)
            {
                var data = item.Data;
                _encryptionProvider.DecryptPacket(data);

                item.Message = BitConverter.ToUInt16(item.Data, 0x12).ToString("X4");

                item.IsDecrypted = true;
            }

            if (item.Direction == "S")
            {
                item.Name = _db.GetServerZoneOpName(int.Parse(item.Message, NumberStyles.HexNumber));
                item.Comment = _db.GetServerZoneOpComment(int.Parse(item.Message, NumberStyles.HexNumber));
                
                switch (item.Message)
                {
                    case "0142":
                    case "0143":
                    case "0144":
                        int cat = BitConverter.ToUInt16(item.Data, 0x20);
                        item.ActorControl = cat;
                        item.Name = _db.GetActorControlTypeName(cat);
                        break;
                }

                if (_mainWindow.ExEnabledCheckbox.IsChecked)
                {
                    try
                    {
                        var structText = _db.GetServerZoneStruct(int.Parse(item.Message, NumberStyles.HexNumber));

                        if (structText != null && structText.Length != 0)
                        {
                            switch (item.Name)
                            {
                                case "NpcSpawn":
                                    {
                                        Struct structProvider = new Struct();
                                        dynamic obj = structProvider.Parse(structText, item.Data).Item2;

                                        item.Comment =
                                            $"Name: {_mainWindow.ExdProvider.GetBnpcName((int)obj.bNPCName)}({obj.bNPCName}) - Base: {obj.bNPCBase}";
                                    }
                                    break;

                                case "ActorCast":
                                    {
                                        Struct structProvider = new Struct();
                                        dynamic obj = structProvider.Parse(structText, item.Data).Item2;

                                        item.Comment = $"Action: {_mainWindow.ExdProvider.GetActionName(obj.action_id)}({obj.action_id}) - Type {obj.skillType} - Cast Time: {obj.cast_time}";
                                    }
                                    break;
                            }

                            if (item.Name.Contains("ActorControl"))
                            {
                                switch (item.ActorControl)
                                {
                                    case 3: //CastStart
                                        {
                                            var ctrl = Util.FastParseActorControl(item.Data);

                                            item.Comment = $"Action: {_mainWindow.ExdProvider.GetActionName((int)ctrl.Param2)}({ctrl.Param2}) - Type {ctrl.Param1}";
                                        }
                                        break;

                                    case 17: //ActionStart
                                        {
                                            var ctrl = Util.FastParseActorControl(item.Data);

                                            item.Comment = $"Action: {_mainWindow.ExdProvider.GetActionName((int)ctrl.Param2)}({ctrl.Param2}) - Type {ctrl.Param1}";
                                        }
                                        break;
                                }
                            }
                        }
                    }
                    catch (Exception exc)
                    {
                        new ExtendedErrorView(
                            $"EXD Error for {item.Message} - {item.Name}. Turning off EXD features.", exc.ToString(), "Error").ShowDialog();
                        _mainWindow.ExEnabledCheckbox.IsChecked = false;
                    }
                }
            }
            else
            {
                item.Name = _db.GetClientZoneOpName(int.Parse(item.Message, NumberStyles.HexNumber));
                item.Comment = _db.GetClientZoneOpComment(int.Parse(item.Message, NumberStyles.HexNumber));
                
                if (item.Data[0x0C] == 0x09 && item.Message == "0000" && item.Connection == FFXIVNetworkMonitor.ConnectionType.Lobby)
                {
                    _encryptionProvider = new LobbyEncryptionProvider(item.Data);

                    item.Comment = "Lobby Encryption INIT";
                }
            }

            item.IsForSelf = BitConverter.ToUInt32(item.Data, 0x04) == BitConverter.ToUInt32(item.Data, 0x08);
            item.Category = item.Set.ToString();

            if (_mainWindow.RunScriptsOnNewCheckBox.IsChecked)
            {
                if (_mainWindow.ScriptProvider == null)
                {
                    MessageBox.Show("No scripts were loaded.", "Error", MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    _mainWindow.RunScriptsOnNewCheckBox.IsChecked = false;
                }
                else
                {
                    try
                    {
                        Scripting_RunOnPacket(item, _mainWindow.ScriptProvider);
                    }
                    catch (Exception exc)
                    {
                        new ExtendedErrorView(
                            $"Scripting exception thrown for {item.Message} - {item.Name}. Turning off auto script running.", exc.ToString(), "Error").ShowDialog();
                        _mainWindow.RunScriptsOnNewCheckBox.IsChecked = false;
                    }
                }
            }

            if (_filters != null && _filters.Length > 0)
            {
                foreach (var filterEntry in _filters)
                {
                    if (!filterEntry.IsApplicableForFilterSet(item))
                    {
                        return;
                    }
                }
            }

            item.Size = item.Data.Length.ToString();

            PacketListView.Items.Add(item);

            if (!silent)
            {
                UpdateInfoLabel();
            }
        }

        private void EditPacketNoteClick(object sender, RoutedEventArgs e)
        {
            var packet = PacketListView.SelectedItem as PacketEntry;
            var index = PacketListView.SelectedIndex;

            packet.Note = new TextInputView(packet.Note, "Change the note that is attached to the packet and click OK.", "FFXIVMon Reborn").ShowDialog();

            PacketListView.Items.RemoveAt(index);
            PacketListView.Items.Insert(index, packet);
        }
        #endregion

        #region Database
        public void SetDBViaVersion(int version)
        {
            _commitSha = null;
            _version = version;
            _db = _mainWindow.VersioningProvider.GetDatabaseForVersion(_version);
            if (_db != null)
            {
                ReloadCurrentPackets();
            }
            UpdateInfoLabel();
        }

        public void SetDBViaCommit(string commitSha)
        {
            _commitSha = commitSha;
            _db = _mainWindow.VersioningProvider.GetDatabaseForCommitHash(_commitSha, true);
            if (_db != null)
            {
                ReloadCurrentPackets();
            }
            UpdateInfoLabel();
        }

        public void ReloadDb()
        {
            _db = _mainWindow.VersioningProvider.GetDatabaseForVersion(_version);
            if (_db != null)
            {
                ReloadCurrentPackets();
            }
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

            var fileDialog = new System.Windows.Forms.SaveFileDialog { Filter = @"XML|*.xml" };

            var result = fileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var capture = new Capture
                {
                    Packets = PacketListView.Items.Cast<PacketEntry>().ToArray(),
                    UsingSystemTime = _wasCapturedMs.ToString().ToLower(),
                    Version = _version
                };
                try
                {
                    capture.LastSavedAppCommit = Util.GetGitHash();
                    XmlCaptureImporter.Save(capture, fileDialog.FileName);
                    MessageBox.Show($"Capture saved to {fileDialog.FileName}.", "FFXIVMon Reborn", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                    _currentXmlFile = fileDialog.FileName;
                    ChangeTitle(System.IO.Path.GetFileNameWithoutExtension(_currentXmlFile));
                }
                catch (Exception ex)
                {
                    new ExtendedErrorView("Could not save capture.", ex.ToString(), "Error").ShowDialog();
                }
            }

            UpdateInfoLabel();
        }

        public void LoadCapture()
        {
            _encryptionProvider = null;
            _currentPacketStream = new MemoryStream(new byte[] { });
            _filterString = "";

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = @"XML|*.xml";
            openFileDialog.Title = @"Select a Capture XML file";

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

        public void LoadFfxivReplay()
        {
            _encryptionProvider = null;
            _currentPacketStream = new MemoryStream(new byte[] { });
            _filterString = "";

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = @"DAT|*.dat",
                Title = @"Select a Replay DAT file"
            };

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                MessageBoxResult res = MessageBox.Show("No to open in current, Yes to open in new tab.", "Open in new tab?", MessageBoxButton.YesNoCancel);
                if (res == MessageBoxResult.Yes)
                {
                    byte[] replay = File.ReadAllBytes(openFileDialog.FileName);

                    int start = int.Parse(Interaction.InputBox("Enter the starting packet number.", "FFXIVMon Reborn", "0"));
                    int end = int.Parse(Interaction.InputBox("Enter the end packet number.", "FFXIVMon Reborn", FfxivReplayImporter.GetNumPackets(replay).ToString()));
                    _mainWindow.AddTab(FfxivReplayImporter.Import(replay, start, end));
                    return;
                }
                else if (res == MessageBoxResult.No)
                {
                    byte[] replay = File.ReadAllBytes(openFileDialog.FileName);

                    int start = int.Parse(Interaction.InputBox("Enter the starting packet number.", "FFXIVMon Reborn", "0"));
                    int end = int.Parse(Interaction.InputBox("Enter the end packet number.", "FFXIVMon Reborn", FfxivReplayImporter.GetNumPackets(replay).ToString()));
                    LoadCapture(FfxivReplayImporter.Import(replay, start, end));
                }
                else
                {
                    return;
                }
            }
            UpdateInfoLabel();
        }
        
        public void LoadActLog()
        {
            _encryptionProvider = null;
            _currentPacketStream = new MemoryStream(new byte[] { });
            _filterString = "";

            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = @"LOG|*.log",
                Title = @"Select a ACT log file"
            };

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                MessageBoxResult res = MessageBox.Show("No to open in current, Yes to open in new tab.", "Open in new tab?", MessageBoxButton.YesNoCancel);
                if (res == MessageBoxResult.Yes)
                {
                    _mainWindow.AddTab(ActLogImporter.Import(openFileDialog.FileName));
                    return;
                }
                else if (res == MessageBoxResult.No)
                {
                    LoadCapture(ActLogImporter.Import(openFileDialog.FileName));
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

            try
            {
                var capture = XmlCaptureImporter.Load(path);

                _commitSha = null;
                _version = capture.Version;
                _db = _mainWindow.VersioningProvider.GetDatabaseForVersion(_version);
                foreach (var packet in capture.Packets)
                {
                    // Add a packet to the view, but no update to the label
                    AddPacketToListView(packet, true);
                }

                // Backwards compatibility
                _wasCapturedMs = capture.UsingSystemTime != null && bool.Parse(capture.UsingSystemTime);

                UpdateInfoLabel();
            }
            catch (Exception exc)
            {
                new ExtendedErrorView($"Could not load capture at {path}.", exc.ToString(), "Error").ShowDialog();
                #if DEBUG
                throw;
                #endif
            }
        }

        public void LoadCapture(PacketEntry[] packets)
        {
            PacketListView.Items.Clear();
            ChangeTitle("Imported Capture");

            _commitSha = null;
            _version = -1;
            _db = _mainWindow.VersioningProvider.GetDatabaseForVersion(_version);
            foreach (var packet in packets)
            {
                AddPacketToListView(packet);
            }

            UpdateInfoLabel();
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
                var packet = (PacketEntry)PacketListView.Items[PacketListView.SelectedIndex];

                var fileDialog = new SaveFileDialog { Filter = @"DAT|*.dat" };

                var result = fileDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    File.WriteAllBytes(fileDialog.FileName, InjectablePacketBuilder.BuildSingle(packet.Data));
                    MessageBox.Show($"Packet saved to {fileDialog.FileName}.", "FFXIVMon Reborn", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                }
            }
            else
            {
                FolderBrowserDialog dialog = new FolderBrowserDialog();

                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    int count = 0;
                    foreach (PacketEntry item in items)
                    {
                        File.WriteAllBytes(System.IO.Path.Combine(dialog.SelectedPath, $"{item.Message}-{String.Join("_", item.Timestamp.Split(System.IO.Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.')}-No{count}.dat"),
                            InjectablePacketBuilder.BuildSingle(item.Data));

                        count++;
                    }
                    MessageBox.Show($"Packets saved to {dialog.SelectedPath}.", "FFXIVMon Reborn", MessageBoxButton.OK, MessageBoxImage.Asterisk);
                }
            }
        }

        private void ExportSelectedSetsForReplay(object sender, RoutedEventArgs e)
        {
            var items = PacketListView.SelectedItems;

            if (items.Count == 0)
            {
                MessageBox.Show("No packets selected.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            FolderBrowserDialog dialog = new FolderBrowserDialog();

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                List<int> inSetIndexes = new List<int>();

                int count = 0;

                foreach (var item in items)
                {
                    var startPacket = (PacketEntry)item;

                    int index = PacketListView.Items.IndexOf(startPacket);

                    if (inSetIndexes.Contains(index) || startPacket.Direction == "C")
                        continue;

                    List<byte[]> packets = new List<byte[]>();
                    packets.Add(startPacket.Data);

                    int at = index - 1;
                    while (true && at != 0 && at != -1)
                    {
                        if (((PacketEntry)PacketListView.Items[at]).Set == startPacket.Set)
                        {
                            packets.Insert(0, ((PacketEntry)PacketListView.Items[at]).Data);
                            inSetIndexes.Add(at);
                        }
                        else
                            break;
                        at--;
                    }

                    at = index + 1;
                    while (true && at < PacketListView.Items.Count)
                    {
                        if (((PacketEntry)PacketListView.Items[at]).Set == startPacket.Set)
                        {
                            packets.Add(((PacketEntry)PacketListView.Items[at]).Data);
                            inSetIndexes.Add(at);
                        }
                        else
                            break;
                        at++;
                    }

                    Console.WriteLine(packets.Count);

                    File.WriteAllBytes(System.IO.Path.Combine(dialog.SelectedPath, $"{startPacket.SystemMsTime.ToString("D14")}-No{count}.dat"),
                        InjectablePacketBuilder.BuildSet(packets));
                    count++;
                }
                MessageBox.Show($"{count} Sets saved to {dialog.SelectedPath}.", "FFXIVMon Reborn", MessageBoxButton.OK, MessageBoxImage.Asterisk);
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
                var startPacket = (PacketEntry)PacketListView.Items[PacketListView.SelectedIndex];

                List<byte[]> packets = new List<byte[]>();
                packets.Add(startPacket.Data);

                int at = PacketListView.SelectedIndex - 1;
                while (true)
                {
                    if (((PacketEntry)PacketListView.Items[at]).Set == startPacket.Set)
                        packets.Insert(0, ((PacketEntry)PacketListView.Items[at]).Data);
                    else
                        break;
                    at--;
                }

                at = PacketListView.SelectedIndex + 1;
                while (true)
                {
                    if (((PacketEntry)PacketListView.Items[at]).Set == startPacket.Set)
                        packets.Add(((PacketEntry)PacketListView.Items[at]).Data);
                    else
                        break;
                    at++;
                }

                Console.WriteLine(packets.Count);

                var fileDialog = new SaveFileDialog { Filter = @"DAT|*.dat" };

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

        private void ExportSelectedPacketsToSet(object sender, RoutedEventArgs e)
        {
            var packets = new List<byte[]>();

            foreach (var item in PacketListView.SelectedItems)
            {
                packets.Add(((PacketEntry)item).Data);
            }

            var fileDialog = new SaveFileDialog { Filter = @"DAT|*.dat" };

            var result = fileDialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                File.WriteAllBytes(fileDialog.FileName, InjectablePacketBuilder.BuildSet(packets));
                MessageBox.Show($"Packet Set containing {packets.Count} packets saved to {fileDialog.FileName}.", "FFXIVMon Reborn", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            }
        }
        #endregion

        #region Filtering

        public void SetFilter()
        {

            string filter = Interaction.InputBox("Enter the packet filter.\nFor help, see Filters->Show Help.", "FFXIVMon Reborn", _filterString);
            _ApplyFilter(filter);
        }

        public void ResetToOriginal()
        {
            _ResetFilter();
        }

        private void _ResetFilter()
        {
            _filterString = "";
            _filters = null;
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
                new ExtendedErrorView("[XivMonTab] Filter Parse error!", exc.ToString(), "Error").ShowDialog();
                return;
            }

            _filters = filters;

            PacketListView.Items.Filter = new Predicate<object>((object item) =>
            {
                bool predResult = false;
                foreach (var filterEntry in _filters)
                {
                    predResult = filterEntry.IsApplicableForFilterSet((PacketEntry)item);

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
            if (FilterEntry.Text.Length == 0)
            {
                FilterEntry.Background = Brushes.White;
                return;
            }

            if (!Filter.IsValidFilter(FilterEntry.Text))
            {
                FilterEntry.Background = Brushes.IndianRed;
                return;
            }

            FilterEntry.Background = Brushes.PaleGreen;

            if (e.Key == Key.Enter)
            {
                _ApplyFilter(FilterEntry.Text);
            }
        }
        #endregion

        #region Scripting
        public void Scripting_RunOnCapture(bool silent = false)
        {
            var res = silent;
            
            if(!res)
                res = MessageBox.Show("Do you want to execute scripts on shown packets? This can take some time, depending on the amount of packets.\n\nPackets: " + PacketListView.Items.Count, "FFXIVMon Reborn", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.OK;

            if (!res) return;
            
            if (_mainWindow.ScriptProvider == null)
            {
                _mainWindow.ScriptProvider = new ScriptingProvider();
                _mainWindow.ScriptProvider.LoadScripts(System.IO.Path.Combine(Environment.CurrentDirectory, "Scripts"));
            }

            try
            {
                foreach (var item in PacketListView.Items)
                {
                    var packet = item as PacketEntry;

                    if (packet.IsVisible)
                    {
                        Scripting_RunOnPacket(packet, _mainWindow.ScriptProvider);
                    }
                }
            }
            catch (Exception exc)
            {
                new ExtendedErrorView("Script error!", exc.ToString(), "Error").ShowDialog();
                _mainWindow.RunScriptsOnNewCheckBox.IsChecked = false;
                return;
            }
        }

        private void Scripting_RunOnPacket(PacketEntry item, ScriptingProvider provider)
        {
            PacketEventArgs args = null;

            string structText = null;
            structText = item.Direction == "S" ? _db.GetServerZoneStruct(int.Parse(item.Message, NumberStyles.HexNumber)) : _db.GetClientZoneStruct(int.Parse(item.Message, NumberStyles.HexNumber));


            if (structText != null)
            {
                if (structText.Length != 0)
                {
                    try
                    {
                        var structProvider = new Struct();
                        var structEntries = structProvider.Parse(structText, item.Data);

                        args = new PacketEventArgs(item, structEntries.Item2, _mainWindow.LogView);
                    }
                    catch (Exception exc)
                    {
                        _mainWindow.LogView.WriteLine($"[EXCEPTION] Thrown for {item.Message} - {item.Name}: {exc}");
                        args = new PacketEventArgs(item, null, _mainWindow.LogView);
                    }
                }
            }
            else
            {
                args = new PacketEventArgs(item, null, _mainWindow.LogView);
            }

            if(args != null)
                provider.ExecuteScripts(null, args);
        }

        
        private void RunSpecificScriptOnPacket(object sender, RoutedEventArgs e)
        {
            var scriptView = new ScriptSelectView("Scripts");
            scriptView.ShowDialog();
            var toLoad = scriptView.GetSelectedScripts();

            var provider = new ScriptingProvider();
            provider.LoadScripts(toLoad);

            var items = PacketListView.SelectedItems;

            foreach (var item in items)
            {
                var packet = item as PacketEntry;

                try
                {
                    Scripting_RunOnPacket(packet, provider);
                }
                catch (Exception exc)
                {
                    new ExtendedErrorView(
                        $"Scripting exception thrown for {packet.Message} - {packet.Name}.", exc.ToString(), "Error").ShowDialog();
                    return;
                }
            }
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
            HexEditor.Select(item.offset, item.typeLength);
        }

        private void StructListView_KeyDown(object sender, KeyEventArgs e)
        {
            if (StructListView.IsKeyboardFocusWithin)
            {
                if (Keyboard.IsKeyDown(Key.LeftCtrl) && e.Key == Key.C)
                {
                    if (Keyboard.IsKeyDown(Key.LeftShift))
                        StructListView_CopyAllCols_Click(null, null);
                    else
                        StructListView_CopyValue_Click(null, null);
                }
            }
        }

        private void StructListView_CopyValue_Click(object sender, RoutedEventArgs e)
        {
            String str = "";
            String newline = (StructListView.SelectedItems.Count > 1 ? Environment.NewLine : "");

            foreach (StructListItem item in StructListView.SelectedItems)
                str += item.ValueCol + newline;

            System.Windows.Clipboard.SetDataObject(str);
            System.Windows.Clipboard.Flush();
        }

        private void StructListView_CopyAllCols_Click(object sender, RoutedEventArgs e)
        {
            // determine width to align tab character to
            int typeWidth = "DataType".Length, nameWidth = "Cannot parse.".Length, valWidth = "Cannot parse.".Length, offsetWidth = "Offset (hex)".Length;
            foreach (StructListItem item in StructListView.SelectedItems)
            {
                typeWidth = item.DataTypeCol?.Length > typeWidth ? item.DataTypeCol.Length : typeWidth;
                valWidth = item.ValueCol?.Length > valWidth ? item.ValueCol.Length : valWidth;
                offsetWidth = item.OffsetCol?.Length > offsetWidth ? item.OffsetCol.Length : offsetWidth;
                nameWidth = item.NameCol?.Length > nameWidth ? item.NameCol.Length : nameWidth;
            }

            // format string
            String fstr = $"{{0,-{typeWidth}}}\t|\t{{1,-{nameWidth}}}\t|\t{{2,-{valWidth}}}\t|\t{{3,-{offsetWidth}}}{{4}}";

            // start the string with header
            String str = String.Format(fstr, "DataType", "Name", "Value", "Offset (hex)", Environment.NewLine);
            // add each entry
            foreach (StructListItem item in StructListView.SelectedItems)
                str += String.Format(fstr, item.DataTypeCol, item.NameCol, item.ValueCol, item.OffsetCol + "h", Environment.NewLine);

            System.Windows.Clipboard.SetDataObject(str);
            System.Windows.Clipboard.Flush();
        }
        #endregion

        #region HexBoxHandling
        private void HexEditor_OnOnSelectionStartChanged(object sender, EventArgs e)
        {
            DataTypeViewer.Apply(_currentPacketStream.ToArray(), (int)HexEditor.SelectionStart);
        }
        #endregion
    }
}

