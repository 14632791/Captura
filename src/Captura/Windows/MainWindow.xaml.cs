﻿using System.Drawing;
using System.Linq;
using Captura.Models;
using Captura.ViewModels;
using Captura.Views;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using Screna;

namespace Captura
{
    public partial class MainWindow
    {
        public static MainWindow Instance { get; private set; }

        FFmpegDownloaderWindow _downloader;

        public MainWindow()
        {
            Instance = this;
            
            FFmpegService.FFmpegDownloader += () =>
            {
                if (_downloader == null)
                {
                    _downloader = new FFmpegDownloaderWindow();
                    _downloader.Closed += (Sender, Args) => _downloader = null;
                }

                _downloader.ShowAndFocus();
            };
            
            InitializeComponent();

            if (DataContext is MainViewModel vm)
            {
                vm.Init(!App.CmdOptions.NoPersist, true, !App.CmdOptions.Reset, !App.CmdOptions.NoHotkeys);

                // Register for Windows Messages
                ComponentDispatcher.ThreadPreprocessMessage += (ref MSG Message, ref bool Handled) =>
                {
                    const int wmHotkey = 786;

                    if (Message.message == wmHotkey)
                    {
                        var id = Message.wParam.ToInt32();

                        vm.HotKeyManager.ProcessHotkey(id);
                    }
                };

                Loaded += (Sender, Args) =>
                {
                    RepositionWindowIfOutside();

                    vm.ViewLoaded();
                };
            }

            if (App.CmdOptions.Tray || ServiceProvider.Get<Settings>().UI.MinToTrayOnStartup)
                Hide();

            Closing += (Sender, Args) =>
            {
                if (!TryExit())
                    Args.Cancel = true;
            };
        }

        void RepositionWindowIfOutside()
        {
            // Window dimensions taking care of DPI
            var rect = new Rectangle((int)(Left * Dpi.X),
                (int)(Top * Dpi.Y),
                (int)(ActualWidth * Dpi.X),
                (int)(ActualHeight * Dpi.Y));
            
            if (!Screen.AllScreens.Any(M => M.Bounds.Contains(rect)))
            {
                Left = 50;
                Top = 50;
            }
        }

        void Grid_PreviewMouseLeftButtonDown(object Sender, MouseButtonEventArgs Args)
        {
            DragMove();

            Args.Handled = true;
        }

        void MinButton_Click(object Sender, RoutedEventArgs Args) => SystemCommands.MinimizeWindow(this);

        void CloseButton_Click(object Sender, RoutedEventArgs Args)
        {
            if (ServiceProvider.Get<Settings>().UI.MinToTrayOnClose)
            {
                Hide();
            }
            else Close();
        }

        void SystemTray_TrayMouseDoubleClick(object Sender, RoutedEventArgs Args)
        {
            if (Visibility == Visibility.Visible)
            {
                Hide();
            }
            else
            {
                Show();

                WindowState = WindowState.Normal;

                Activate();
            }
        }

        bool TryExit()
        {
            if (DataContext is MainViewModel vm)
            {
                if (vm.RecorderState == RecorderState.Recording)
                {
                    if (!ServiceProvider.MessageProvider.ShowYesNo(
                        "A Recording is in progress. Are you sure you want to exit?", "Confirm Exit"))
                        return false;
                }
                else if (vm.RunningStopRecordingCount > 0)
                {
                    if (!ServiceProvider.MessageProvider.ShowYesNo(
                        "Some Recordings have not finished writing to disk. Are you sure you want to exit?", "Confirm Exit"))
                        return false;
                }

                vm.Dispose();
            }

            SystemTray.Dispose();

            return true;
        }

        void MenuExit_Click(object Sender, RoutedEventArgs Args) => Close();

        void HideButton_Click(object Sender, RoutedEventArgs Args) => Hide();

        void OpenOverlayManager(object Sender, RoutedEventArgs E)
        {
            OverlayWindow.ShowInstance();
        }

        void OpenHotkeyManager(object Sender, RoutedEventArgs E)
        {
            HotkeysWindow.ShowInstance();
        }

        void SelectTargetFolder(object Sender, MouseButtonEventArgs E)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SelectOutputFolderCommand.ExecuteIfCan();
            }
        }
    }
}