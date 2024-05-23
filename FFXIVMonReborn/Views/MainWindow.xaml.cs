using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using FFXIVMonReborn.Database;
using Microsoft.VisualBasic;
using MessageBox = System.Windows.MessageBox;
using FFXIVMonReborn.Importers;
using FFXIVMonReborn.DataModel;
using FFXIVMonReborn.Properties;
using FFXIVMonReborn.Scripting;
using Machina.Infrastructure;
using System.IO;
using System.Runtime.CompilerServices;
using Machina.FFXIV.Oodle;

namespace FFXIVMonReborn.Views
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly KeyboardHook _kbHook = new KeyboardHook();
        public ObservableCollection<OodleImplementationItem> OodleImplementationItems { get; set; }

        public readonly Versioning VersioningProvider = new Versioning();
        public ExdDataCache ExdProvider = null;
        public ScriptingProvider ScriptProvider = null;

        public readonly LogView LogView = new LogView();
        private string[] _selectedScripts = new string[0];
        
        public NetworkMonitorType CaptureMode;
        public OodleImplementation OodleImplementation;
        public MachinaCaptureWorker.ConfigFlags CaptureFlags;

        public MainWindow()
        {
            InitializeComponent();
            
            #if !DEBUG
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs eventArgs)
                {
                    new ExtendedErrorView("FFXIVMon Reborn ran into an error and needs to close.",
                        eventArgs.ExceptionObject.ToString(), "Unhandled Exception").ShowDialog();
                    
                    Process.GetCurrentProcess().Kill();
                };
            #endif

            bool loadedByArg = false;
            var args = Environment.GetCommandLineArgs();
            for (var i = 1; i + 1 < args.Length; i += 2)
            {
                if (args[i] == "--xml")
                {
                    var tab = new TabItem();

                    tab.Content = new XivMonTab();
                    tab.Header = "New Capture";
                    ((XivMonTab)tab.Content).SetParents(tab, this);
                    MainTabControl.Items.Add(tab);
                    ((XivMonTab)tab.Content).LoadCapture(args[i+1]);

                    loadedByArg = true;
                }
            }

            if (!loadedByArg)
            {
                var item = new TabItem();
                item.Content = new XivMonTab();
                item.Header = "New Capture";
                ((XivMonTab)item.Content).SetParents(item, this);
                MainTabControl.Items.Add(item);
            }


            // register the event that is fired after the key press.
            _kbHook.KeyPressed += hook_KeyPressed;

            try
            {
                _kbHook.RegisterHotKey(ModifierKeys.Control | ModifierKeys.Alt, Keys.F12);
            }
            catch (Exception)
            {
                // ignored
            }
            
            CaptureMode = (NetworkMonitorType)Properties.Settings.Default.NetworkMonitorType;

            if (CaptureMode == NetworkMonitorType.RawSocket)
                SwitchModeSockets.IsChecked = true;
            else
                SwitchModePcap.IsChecked = true;

            try
            {
                ExdProvider = new ExdDataCache(Properties.Settings.Default.GamePath);
            }
            catch (Exception exc)
            {
                new ExtendedErrorView("Unable to init EXD data. Please check your game path in Options -> Set Game Path.", exc.ToString(), "FFXIVMon Reborn").ShowDialog();
                Properties.Settings.Default.LoadEXD = false;
            }

            if (!Properties.Settings.Default.DontUsePacketTimestamp)
            {
                DontUsePacketTimestamp.IsChecked = false;
                Properties.Settings.Default.DontUsePacketTimestamp = false;
            }
            else
            {
                CaptureFlags |= MachinaCaptureWorker.ConfigFlags.DontUsePacketTimestamp;
                DontUsePacketTimestamp.IsChecked = true;
                Properties.Settings.Default.DontUsePacketTimestamp = true;
            }

            if (Properties.Settings.Default.ForceRealtimePriority)
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
                ForceRealtimePriority.IsChecked = true;
            }

            if (Properties.Settings.Default.LoadEXD)
                ExEnabledCheckbox.IsChecked = true;

            if (Properties.Settings.Default.EnableFsWatcher)
                WatchDefFilesCheckBox.IsChecked = true;

            if (Properties.Settings.Default.HideHexBoxActorId)
                HideHexBoxActorIdCheckBox.IsChecked = true;

            if (Properties.Settings.Default.StickPacketViewBottom)
                StickPacketViewBottomCheckBox.IsChecked = true;

            if (Properties.Settings.Default.OodleEnforced)
                OodleEnforcedCheckbox.IsChecked = true;

            if (Properties.Settings.Default.UseDeucalion)
            {
                SwitchModeDeucalion.IsChecked = true;
            }
            
            OodleImplementation = (OodleImplementation) Settings.Default.OodleImplementation;
            OodleImplementationItems = new ObservableCollection<OodleImplementationItem>();
            
            foreach (var oodleImplementationType in Enum.GetValues<OodleImplementation>())
            {
                var item = new OodleImplementationItem();
                var name = Enum.GetName(oodleImplementationType);

                item.Name = name;
                item.Type = oodleImplementationType;
                item.IsChecked = OodleImplementation == item.Type;

                OodleImplementationItems.Add(item);
            }
            
            OodleImplementationTypeCheckbox.DataContext = OodleImplementationItems;
            
            VersioningProvider.LocalDbChanged += VersioningProviderOnLocalDbChanged;
            LogView.Show();
            LogView.Visibility = Visibility.Hidden;
            
            VersionChecker.CheckVersion();
        }

        private void VersioningProviderOnLocalDbChanged(object sender, EventArgs eventArgs)
        {
            ReloadAllTabs();
        }

        void hook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            var tab = (XivMonTab) MainTabControl.SelectedContent;

            if (!tab.IsCapturing())
                tab.StartCapture();
            else
                tab.StopCapture();

            SystemSounds.Asterisk.Play();
            this.FlashWindow(3);
        }
        
        public void AddTab()
        {
            var item = new TabItem();

            item.Content = new XivMonTab();
            item.Header = "New Capture";
            ((XivMonTab)item.Content).SetParents(item, this);
            MainTabControl.Items.Add(item);

            MainTabControl.SelectedIndex = MainTabControl.Items.Count - 1;
        }

        public void AddTab(string toLoad)
        {
            var item = new TabItem();

            item.Content = new XivMonTab();
            item.Header = "New Capture";
            ((XivMonTab)item.Content).SetParents(item, this);
            MainTabControl.Items.Add(item);

            if (toLoad != null)
                ((XivMonTab) item.Content).LoadCapture(toLoad);

            MainTabControl.SelectedIndex = MainTabControl.Items.Count - 1;
        }

        public void AddTab(PacketEntry[] toLoad)
        {
            var item = new TabItem();

            item.Content = new XivMonTab();
            item.Header = "New Capture";
            ((XivMonTab)item.Content).SetParents(item, this);
            MainTabControl.Items.Add(item);

            if (toLoad != null)
                ((XivMonTab) item.Content).LoadCapture(toLoad);

            MainTabControl.SelectedIndex = MainTabControl.Items.Count - 1;
        }
        
        private void MainTabControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ((XivMonTab) MainTabControl.SelectedContent)?.OnTabFocus();
        }
        
        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            foreach (var tab in MainTabControl.Items)
            {
                if (!((XivMonTab) ((TabItem) tab).Content).IsCloseAllowed())
                {
                    MessageBoxResult res = MessageBox.Show("One or more tabs have captured packets that were not yet saved.\nDo you want to quit without saving?", "Unsaved Packets", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (res == MessageBoxResult.No)
                    {
                        e.Cancel = true;
                        return;
                    }
                    else
                    {
                        foreach (var closeTab in MainTabControl.Items)
                        {
                            ((XivMonTab)((TabItem)closeTab).Content).StopCapture();
                        }
                        LogView.Close();
                        Environment.Exit(0);
                    }
                }
            }
            LogView.Close();
            Environment.Exit(0);
        }

        private void TabCloseClick(object sender, RoutedEventArgs e)
        {
            if (MainTabControl.Items.Count == 1)
            {
                MessageBox.Show("You can't close your last remaining tab.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            if (((XivMonTab) ((TabItem) MainTabControl.Items[MainTabControl.SelectedIndex]).Content).RequestClose())
            {
                MainTabControl.Items.RemoveAt(MainTabControl.SelectedIndex);
            }
        }
        
        private void SwitchModeSockets_OnClick(object sender, RoutedEventArgs e)
        {
            if (AreTabsCapturing())
            {
                MessageBox.Show("A capture is in progress.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            CaptureMode = NetworkMonitorType.RawSocket;
            SwitchModePcap.IsChecked = false;
            SwitchModeSockets.IsChecked = true;

            Properties.Settings.Default.NetworkMonitorType = (int)CaptureMode;
            Properties.Settings.Default.Save();
        }

        private void SwitchModePcap_OnClick(object sender, RoutedEventArgs e)
        {
            if (AreTabsCapturing())
            {
                MessageBox.Show("A capture is in progress.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            CaptureMode = NetworkMonitorType.WinPCap;
            SwitchModePcap.IsChecked = true;
            SwitchModeSockets.IsChecked = false;

            Properties.Settings.Default.NetworkMonitorType = (int)CaptureMode;
            Properties.Settings.Default.Save();
        }

        //TODO: Find a better way to do this, custom tab headers?
        private void MainTabControl_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            AddTab();
        }
        
        private void ReloadAllTabs()
        {
            foreach (TabItem tab in MainTabControl.Items) {
                tab.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() => {
                    var xmt = tab.Content as XivMonTab;

                    this.Dispatcher.Invoke(DispatcherPriority.Normal,
                        new Action(() =>
                        {
                            xmt.ReloadDb();
                        }));
                }));
            }
        }
        
        private bool AreTabsCapturing()
        {
            foreach (var tab in MainTabControl.Items)
            {
                if (((XivMonTab) ((TabItem) tab).Content).IsCapturing())
                {
                    return true;
                }
            }
            return false;
        }
        
        private void NewTab(object sender, RoutedEventArgs e)
        {
            AddTab();
        }

        private void NewInstance(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(System.Windows.Forms.Application.ExecutablePath);
        }

        private void AboutButton_OnClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                $"goatmon reborn\n\nVersion: {Util.GetGitHash()}\n\nA FFXIV Packet analysis tool for Sapphire\nCapture backend(Machina) by Ravahn of ACT fame\n\nhttps://github.com/SapphireServer\nhttps://github.com/ravahn/machina", "FFXIVMon Reborn", MessageBoxButton.OK, MessageBoxImage.Asterisk);
        }
        
        private void Scripting_SelectScripts(object sender, RoutedEventArgs e)
        {
            var scriptView = new ScriptSelectView("Scripts");
            scriptView.ShowDialog();
            var toLoad = scriptView.GetSelectedScripts();

            ScriptProvider = new ScriptingProvider();
            ScriptProvider.LoadScripts(toLoad);

            _selectedScripts = toLoad;
        }

        private void Scripting_ReloadScripts(object sender, RoutedEventArgs e)
        {
            if (_selectedScripts.Length == 0)
            {
                MessageBox.Show("No scripts were selected.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            ScriptProvider = new ScriptingProvider();
            ScriptProvider.LoadScripts(_selectedScripts);
        }

        private void Scripting_ResetDataStorage(object sender, RoutedEventArgs e)
        {
            ScriptProvider?.DataStorage.Reset();
            MessageBox.Show(
                "Script Data Storage was reset.", "FFXIVMon Reborn", MessageBoxButton.OK, MessageBoxImage.Asterisk);
        }

        #region TabRelays
        private void LoadCaptureRelay(object sender, RoutedEventArgs e)
        {
            ((XivMonTab)MainTabControl.SelectedContent).LoadCapture();
        }

        private void SaveCaptureRelay(object sender, RoutedEventArgs e)
        {
            ((XivMonTab)MainTabControl.SelectedContent).SaveCapture();
        }

        private void ClearCaptureRelay(object sender, RoutedEventArgs e)
        {
            ((XivMonTab)MainTabControl.SelectedContent).ClearCapture();
        }
        
        private void StartCaptureRelay(object sender, RoutedEventArgs e)
        {
            ((XivMonTab)MainTabControl.SelectedContent).StartCapture();
        }
        
        private void StopCaptureRelay(object sender, RoutedEventArgs e)
        {
            ((XivMonTab)MainTabControl.SelectedContent).StopCapture();
        }
        
        private void ReloadDBRelay(object sender, RoutedEventArgs e)
        {
            ((XivMonTab)MainTabControl.SelectedContent).ReloadDb();
            MessageBox.Show("Database reloaded.", "FFXIVMon Reborn", MessageBoxButton.OK, MessageBoxImage.Asterisk);
        }
        
        private void SetFilterRelay(object sender, RoutedEventArgs e)
        {
            ((XivMonTab)MainTabControl.SelectedContent).SetFilter();
        }
        
        private void ResetToOriginalRelay(object sender, RoutedEventArgs e)
        {
            ((XivMonTab)MainTabControl.SelectedContent).ResetToOriginal();
        }
        
        private void Scripting_RunOnCaptureRelay(object sender, RoutedEventArgs e)
        {
            ((XivMonTab)MainTabControl.SelectedContent).Scripting_RunOnCapture();
        }
        #endregion

        private void DontUsePacketTimestamp_OnClick(object sender, RoutedEventArgs e)
        {
            if (AreTabsCapturing())
            {
                MessageBox.Show("A capture is in progress.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }

            if (DontUsePacketTimestamp.IsChecked)
            {
                CaptureFlags ^= MachinaCaptureWorker.ConfigFlags.DontUsePacketTimestamp;
                DontUsePacketTimestamp.IsChecked = false;
                Properties.Settings.Default.DontUsePacketTimestamp = false;
            }
            else
            {
                CaptureFlags |= MachinaCaptureWorker.ConfigFlags.DontUsePacketTimestamp;
                DontUsePacketTimestamp.IsChecked = true;
                Properties.Settings.Default.DontUsePacketTimestamp = true;
            }
            
            Properties.Settings.Default.Save();
        }

        private void ForceRealtimePriority_OnClick(object sender, RoutedEventArgs e)
        {
            if (!ForceRealtimePriority.IsChecked)
            {
                MessageBoxResult res = MessageBox.Show("This will set the process priority of FFXIVMon to Realtime, which can prevent dropped packets. This, however, can have unintended side effects.\nContinue?", "Enable RealTime", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
                    ForceRealtimePriority.IsChecked = true;
                    Properties.Settings.Default.ForceRealtimePriority = true;
                }
            }
            else
            {
                Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;
                ForceRealtimePriority.IsChecked = false;
                Properties.Settings.Default.ForceRealtimePriority = false;
            }

            Properties.Settings.Default.Save();
        }

        private void Diff_BasedOnPacketLength(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XML|*.xml";
            openFileDialog.Title = "Select a Capture to diff against";

            try
            {
                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var toDiff = XmlCaptureImporter.Load(openFileDialog.FileName).Packets;
                    var baseCap = ((XivMonTab)MainTabControl.SelectedContent).PacketListView.Items.Cast<PacketEntry>().ToArray();

                    new ExtendedErrorView($"Compared {baseCap.Length} packets to {toDiff.Length} packets.",
                        CaptureDiff.GenerateLenghtBasedReport(baseCap, toDiff), "FFXIVMon Reborn").Show();
                }
            }
            catch(Exception ex)
            {
                new ExtendedErrorView("Could not generate length based diff.", ex.ToString(), "Error").ShowDialog();
            }
        }
        
        private void Diff_BasedOnPacketData(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XML|*.xml";
            openFileDialog.Title = "Select a Capture to diff against";

            try
            {
                if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    var toDiff = XmlCaptureImporter.Load(openFileDialog.FileName).Packets;
                    var baseCap = ((XivMonTab)MainTabControl.SelectedContent).PacketListView.Items.Cast<PacketEntry>().ToArray();

                    new ExtendedErrorView($"Compared {baseCap.Length} packets to {toDiff.Length} packets.",
                        CaptureDiff.GenerateDataBasedReport(baseCap, toDiff), "FFXIVMon Reborn").Show();
                }
            }
            catch(Exception ex)
            {
                new ExtendedErrorView("Could not generate data based diff.", ex.ToString(), "Error").ShowDialog();
            }
        }

        public void RedownloadDefs(object sender, RoutedEventArgs e)
        {
            var res = MessageBox.Show("Do you want to redownload definition files from the repo? This will override all local changes.", "FFXIVMon Reborn", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (res == MessageBoxResult.OK)
            {
                VersioningProvider.ForceReset();
                ((XivMonTab)MainTabControl.SelectedContent).ReloadDb();
                MessageBox.Show($"Definitions downloaded.", "FFXIVMon Reborn", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            }
        }

        public void SetRepository(object sender, RoutedEventArgs e)
        {
            string repo = Interaction.InputBox("Enter the GitHub repository for the definition files to be downloaded from.\nThis will reset all downloaded definitions.", "FFXIVMon Reborn", Properties.Settings.Default.Repo);
            
            // Dialog dismissed or empty input box, we don't want that
            if (repo == "")
                return;
            
            Properties.Settings.Default.Repo = repo;
            Properties.Settings.Default.Save();
            VersioningProvider.ForceReset();
            ((XivMonTab)MainTabControl.SelectedContent).ReloadDb();
        }

        public void SetGamePath(object sender, RoutedEventArgs e)
        {
            string gamepath = Interaction.InputBox("Enter path to /FINAL FANTASY XIV - A Realm Reborn/game/sqpack folder. This will reinitialise EXDs and may take a few seconds.", "FFXIVMon Reborn", Properties.Settings.Default.GamePath);

            if (gamepath == "")
                return;

            Properties.Settings.Default.GamePath = gamepath;
            Properties.Settings.Default.Save();

            try
            {
                ExdProvider = new ExdDataCache(Properties.Settings.Default.GamePath);
            }
            catch (Exception exc)
            {
                new ExtendedErrorView("Unable to init EXD data. Please check your game path in Options -> Set Game Path.", exc.ToString(), "FFXIVMon Reborn").ShowDialog();
                Properties.Settings.Default.LoadEXD = false;
            }
            
            MessageBox.Show("EXD data set up successfully.", "FFXIVMon Reborn", MessageBoxButton.OK,
                MessageBoxImage.Asterisk);
        }

        public void SetGameWindowName(object sender, RoutedEventArgs e)
        {
            string name = Interaction.InputBox("Enter the window name of the game.\nYou should restart capture after change.", "FFXIVMon Reborn", Properties.Settings.Default.GameWindowName);
            Properties.Settings.Default.GameWindowName = name;
            Properties.Settings.Default.Save();
        }

        private void LoadFFXIVReplayRelay(object sender, RoutedEventArgs e)
        {
            ((XivMonTab)MainTabControl.SelectedContent).LoadFfxivReplay();
        }

        private void SelectVersion(object sender, RoutedEventArgs e)
        {
            var view = new VersionSelectView(VersioningProvider.Api.Tags);
            if (view.ShowDialog() == true)
                ((XivMonTab)MainTabControl.SelectedContent).SetDBViaVersion(view.GetSelectedVersion());
        }

        private void ReloadExClick(object sender, RoutedEventArgs e)
        {
            ExdProvider = new ExdDataCache(Properties.Settings.Default.GamePath);
            ((XivMonTab)MainTabControl.SelectedContent).ReloadDb();
        }

        private void Scripting_OpenOutputWindow(object sender, RoutedEventArgs e)
        {
            LogView.Visibility = Visibility.Visible;
        }

        private void WatchDefFilesCheckBox_OnClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(WatchDefFilesCheckBox.IsChecked.ToString());
            if (!WatchDefFilesCheckBox.IsChecked)
            {
                VersioningProvider.StartWatcher();

                Properties.Settings.Default.EnableFsWatcher = true;
            }
            else
            {
                VersioningProvider.StopWatcher();

                Properties.Settings.Default.EnableFsWatcher = false;
            }

            Properties.Settings.Default.Save();
        }

        private void WatchDefFilesCheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            VersioningProvider.StartWatcher();
            Properties.Settings.Default.EnableFsWatcher = true;
            Properties.Settings.Default.Save();
        }

        private void WatchDefFilesCheckBox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            VersioningProvider.StopWatcher();
            Properties.Settings.Default.EnableFsWatcher = false;
            Properties.Settings.Default.Save();
        }

        private void ExEnabledCheckbox_OnChecked(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ExdProvider == null)
                    ExdProvider = new ExdDataCache(Properties.Settings.Default.GamePath);
            }
            catch (Exception exc)
            {
                new ExtendedErrorView("Unable to init EXD data. Please check your game path in Options -> Set Game Path.", exc.ToString(), "FFXIVMon Reborn").ShowDialog();
                ExEnabledCheckbox.IsChecked = false;
                return;
            }
        
            ((XivMonTab) MainTabControl.SelectedContent)?.ReloadDb();
            
            Properties.Settings.Default.LoadEXD = true;
            Properties.Settings.Default.Save();
        }

        private void ExEnabledCheckbox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.LoadEXD = false;
            Properties.Settings.Default.Save();
        }

        private void HideHexBoxActorIdCheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.HideHexBoxActorId = true;
            Properties.Settings.Default.Save();
        }

        private void HideHexBoxActorIdCheckBox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.HideHexBoxActorId = false;
            Properties.Settings.Default.Save();
        }
        
        private void StickPacketViewBottomCheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.StickPacketViewBottom = true;
            Properties.Settings.Default.Save();
        }

        private void StickPacketViewBottomCheckBox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.StickPacketViewBottom = false;
            Properties.Settings.Default.Save();
        }
        
        private void OodleEnforcedCheckbox_OnChecked(object sender, RoutedEventArgs e)
        {
            Settings.Default.OodleEnforced = true;
            Settings.Default.Save();
        }
        
        private void OodleEnforcedCheckbox_OnUnchecked(object sender, RoutedEventArgs e)
        {
            Settings.Default.OodleEnforced = false;
            Settings.Default.Save();
        }

        private void ShowFilterHelp(object sender, RoutedEventArgs e)
        {
            var helpStr = "You can use the following commands in Filters, divided by a semicolon:\n\n";
            foreach (var helpEntry in Filter.Help)
            {
                helpStr += helpEntry.Key + " - " + helpEntry.Value + "\n";
            }

            MessageBox.Show(helpStr, "FFXIVMon Reborn");
        }

        private void LoadActLog(object sender, RoutedEventArgs e)
        {
            ((XivMonTab)MainTabControl.SelectedContent).LoadActLog();
        }

        private void PauseCapture(object sender, RoutedEventArgs e)
        {
            ((XivMonTab)MainTabControl.SelectedContent).ChangeTitle();
        }

        private void Scripting_RunOnMultipleCaptures(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "XML|*.xml; pcap|*.pcap; pcapng|*.pcapng";
            openFileDialog.Title = "Select captures";
            openFileDialog.Multiselect = true;

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var res = MessageBox.Show("'Yes' if you wish to access the packet as a parsed object (can be considerably slow).", "Process structs?", MessageBoxButton.YesNo);

                foreach (var fileName in openFileDialog.FileNames)
                {
                    var tab = new XivMonTab();
                    
                    tab.SetParents(null, this);
                    tab.LoadCapture(fileName);
                    
                    tab.Scripting_RunOnCapture(true, res == MessageBoxResult.Yes);
                }

                MessageBox.Show("Ran loaded scripts on captures.", "FFXIVMon Reborn", MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }

        private void SelectBranch_OnClick(object sender, RoutedEventArgs e)
        {
            var selector = new BranchSelectView(VersioningProvider.Api.Branches);
            selector.ShowDialog();
            
            if(selector.GetSelectedVersion() != null)
                ((XivMonTab)MainTabControl.SelectedContent).SetDBViaCommit(selector.GetSelectedVersion());
        }

        private void OpenCurrentDbFolder_OnClick(object sender, RoutedEventArgs e)
        {
            XivMonTab currTab = (XivMonTab)MainTabControl.SelectedContent;
            if (currTab != null)
            {
                var dbFolder = currTab.GetDbFolder();
                var path = Path.Combine(Environment.CurrentDirectory, dbFolder);
                Process.Start(new ProcessStartInfo()
                {
                    FileName = path,
                    UseShellExecute = true,
                    Verb = "open"
                });
            }
        }

        private void OodleImplementationTypeCheckbox_OnClick(object sender, RoutedEventArgs e)
        {
            var originalSource = (MenuItem) e.OriginalSource;
            var currentItem = (OodleImplementationItem) originalSource.DataContext;
            
            Settings.Default.OodleImplementation = (int) currentItem.Type;
            Settings.Default.Save();
            OodleImplementation = currentItem.Type;

            foreach (var oodleImplementation in OodleImplementationItems)
            {
                if (oodleImplementation.Type == currentItem.Type) continue;
                
                oodleImplementation.IsChecked = false;
            }
        }


        private void SetOoodleLibraryPath(object sender, RoutedEventArgs e)
        {
            using OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "DLL|*.dll";
            openFileDialog.Title = "Select the Oodle library to be used";

            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                var libraryPath = openFileDialog.FileName;

                Settings.Default.OodleLibraryPath = libraryPath;
                Settings.Default.Save();
                
            }
        }
        
        private void SwitchModeDeucalion_OnChecked(object sender, RoutedEventArgs e)
        {
            if (AreTabsCapturing())
            {
                MessageBox.Show("A capture is in progress.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            
            Settings.Default.UseDeucalion = true;
            Settings.Default.Save();
        }

        private void SwitchModeDeucalion_OnUnchecked(object sender, RoutedEventArgs e)
        {
            if (AreTabsCapturing())
            {
                MessageBox.Show("A capture is in progress.", "Error", MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
            
            Settings.Default.UseDeucalion = false;
            Settings.Default.Save();
        }

        private void SwitchModeDeucalion_OnClick(object sender, RoutedEventArgs e)
        {
            if (Settings.Default.UseDeucalion)
                MessageBox.Show("Deucalion allows you to capture packets by injecting a DLL into the game, " +
                                "skipping network capturing and Oodle. " +
                                "However, Deucalion might not work with all game versions.", 
                    "FFXIVMon Reborn", MessageBoxButton.OK,
                    MessageBoxImage.Information);
        }
    }

    public class OodleImplementationItem : INotifyPropertyChanged
    {
        private string _name = string.Empty;

        private OodleImplementation _type = OodleImplementation.FfxivTcp;

        private bool _isChecked = true;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Name
        {
            get => _name;
            set
            {
                if (value == _name) return;
                _name = value;
                NotifyPropertyChanged();
            }
        }

        public OodleImplementation Type
        {
            get => _type;
            set
            {
                if (value == _type) return;
                _type = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsChecked
        {
            get => _isChecked;
            set
            {
                if (value == _isChecked) return;
                _isChecked = value;
                NotifyPropertyChanged();
            }
        }

        // This method is called by the Set accessor of each property.  
        // The CallerMemberName attribute that is applied to the optional propertyName  
        // parameter causes the property name of the caller to be substituted as an argument.  
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
