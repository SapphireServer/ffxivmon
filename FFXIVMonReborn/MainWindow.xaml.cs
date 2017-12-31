using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Media;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using Machina;
using Microsoft.VisualBasic;
using MessageBox = System.Windows.MessageBox;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace FFXIVMonReborn
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private KeyboardHook _kbHook = new KeyboardHook();

        public MainWindow()
        {
            InitializeComponent();

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
            _kbHook.KeyPressed +=
                new EventHandler<KeyPressedEventArgs>(hook_KeyPressed);

            try
            {
                _kbHook.RegisterHotKey(ModifierKeys.Control | ModifierKeys.Alt, Keys.F12);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        void hook_KeyPressed(object sender, KeyPressedEventArgs e)
        {
            var tab = (XivMonTab) MainTabControl.SelectedContent;

            if (!tab.IsCapturing())
                tab.StartCapture(null, null);
            else
                tab.StopCapture(null, null);

            SystemSounds.Asterisk.Play();
            this.FlashWindow(3);
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
                            ((XivMonTab)((TabItem)closeTab).Content).StopCapture(null, null);
                        }
                        return;
                    }
                }
            }
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
    }
}
