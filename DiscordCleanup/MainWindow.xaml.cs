using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DiscordCleanup
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        enum Step { Start, GatherInfo, Running, Finished }

        (Grid Pane, Grid Step)[] _pages;


        FileManager _discordFiles;
        InstallerWrap _discordInstaller;
        ProcessManager _discordProcess;

        string _installerFile;
        string _installerDirectory;

        bool _installNewCopy = true;
        bool _downloadFinished = false;
        bool _filesDeleted = false;
        bool _keepInstall = false;
        int _currentPage = 0;


        public MainWindow()
        {
            _discordFiles = new FileManager(AppConfig.DiscordPaths);
            _discordFiles.AddExplicitFile(AppConfig.DiscordShortcutPath);

            _discordInstaller = new InstallerWrap(AppConfig.DiscordURI);
            _discordProcess = new ProcessManager(AppConfig.FriendlyName);

            _installerFile = "Discord Setup.exe";
            _installerDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            InitializeComponent();

            _pages = new (Grid, Grid)[] { (StartPane, StepStart),
                (GatherInfoPane, StepGatherInfo), (RunningPane, StepRunning)};

            LicenseText.Text = AppConfig.License;

        }

        private void ChangePage(int newPage)
        {
            // Hide and disable the old page
            var page = _pages[_currentPage];

            // Hide and disable the old page
            page.Pane.Visibility = Visibility.Hidden;
            page.Pane.IsEnabled = false;
            page.Step.SetResourceReference(BackgroundProperty, SystemColors.MenuBarBrushKey);
            SetChildProperties(page.Step, ForegroundProperty, SystemColors.WindowTextBrushKey);

            // Show and enable the new page
            _currentPage = newPage;
            page = _pages[_currentPage];
            page.Pane.Visibility = Visibility.Visible;
            page.Pane.IsEnabled = true;
            page.Step.SetResourceReference(BackgroundProperty, SystemColors.ActiveCaptionBrushKey);
            SetChildProperties(page.Step, ForegroundProperty, SystemColors.ActiveCaptionTextBrushKey);
            
        }

        private void SetChildProperties(Grid parent, DependencyProperty property, object value)
        {
            var children = parent.Children.GetEnumerator();
            children.Reset();
            while (children.MoveNext())
            {
                if (children.Current == null) break; // If we're at a null value, stop iterating
                FrameworkElement child = (FrameworkElement)children.Current;
                child.SetResourceReference(property, value);
            }
        }

        private void OnBackButton(object sender, RoutedEventArgs e)
        {
            if (_currentPage <= 0) return;
            ChangePage(_currentPage - 1);
        }

        private void OnNextButton(object sender, RoutedEventArgs e)
        {

            if (_currentPage >= _pages.Length)
            {
                return;
            }
            else if (_currentPage == (_pages.Length - 1))
            {
                this.Close();
            }
            else
            {
                ChangePage(_currentPage + 1);
            }
        }

        private void ActivateText(TextBlock toChange)
        {
            toChange.SetResourceReference(ForegroundProperty, SystemColors.ActiveCaptionTextBrushKey);
        }

        private void DeactivateText(TextBlock toChange)
        {
            toChange.SetResourceReference(ForegroundProperty, SystemColors.InactiveCaptionBrushKey);
        }

        private void OnReadTOCChanged(object sender, RoutedEventArgs e)
        {
            ButtonNext.IsEnabled = (bool)ReadAndAgreeConditions.IsChecked;
        }

        private void OnDownloadAndInstallChecked(object sender, RoutedEventArgs e)
        {
            _installNewCopy = true;
            if (KeepInstaller is null) return;
            KeepInstaller.IsEnabled = true;
            ActivateText(TextKeepCopy);

            _keepInstall = (bool)KeepInstaller.IsChecked;
            _discordInstaller.UseFilePath = _keepInstall;
        }

        private void OnDownloadAndInstallUnchecked(object sender, RoutedEventArgs e)
        {
            KeepInstaller.IsEnabled = false;
            DeactivateText(TextKeepCopy);
            _installNewCopy = false;
            _keepInstall = false;
        }

        private void OnKeepInstallerChanged(object sender, RoutedEventArgs e)
        {
            _keepInstall = (bool)KeepInstaller.IsChecked;
            _discordInstaller.UseFilePath = _keepInstall;
            ButtonChooseFile.Visibility = _keepInstall ? Visibility.Visible : Visibility.Hidden;
            ButtonChooseFile.IsEnabled = _keepInstall;
        }

        private void OnStartPane(object sender, DependencyPropertyChangedEventArgs e)
        {
            ButtonBack.Visibility = Visibility.Hidden;
            ButtonBack.IsEnabled = false;
            if (ReadAndAgreeConditions.IsChecked is bool &&
                (bool)ReadAndAgreeConditions.IsChecked) ButtonNext.IsEnabled = true;
            else ButtonNext.IsEnabled = false;
        }

        private void OnGatherInfo(object sender, DependencyPropertyChangedEventArgs e)
        {
            ButtonBack.Visibility = Visibility.Visible;
            ButtonBack.IsEnabled = true;
            ButtonNext.IsEnabled = false;
            

            ProcessesFound.Text = "Finding processes...";
            FilesFound.Text = "Finding files...";

            Task.Run(() =>
            {
                var processStrings = string.Concat(_discordProcess.Processes.Select(
                    (t) => t.Id.ToString() + " : " + t.ProcessName + '\n'));

                if (processStrings.Length == 0) { processStrings = "No processes found."; }

                var fileList = _discordFiles.Files.Concat(_discordFiles.Directories).ToArray();
                Array.Sort(fileList);
                var foundFiles = string.Concat(fileList.Select((t) => t + '\n'));


                if (foundFiles.Length == 0) { foundFiles = "No files found."; }

                FilesFound.Dispatcher.Invoke(() =>
                {
                    FilesFound.Text = foundFiles;
                    ProcessesFound.Text = processStrings;
                });
            });
        }

        private void OnChooseFile(object sender, RoutedEventArgs e)
        {
            var saveBox = new Microsoft.Win32.SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = "exe",
                FileName = _installerFile,
                Filter = "Executable Files|*.exe",
                InitialDirectory = _installerDirectory,
                OverwritePrompt = true,
                ValidateNames = true
            };

            saveBox.FileOk += (t, args) => {
                _discordInstaller.SavePath = System.IO.Path.GetDirectoryName(saveBox.FileName);
                _discordInstaller.SaveFile = System.IO.Path.GetFileName(saveBox.FileName);
            } ;

            var dialog = saveBox.ShowDialog();
            var choseFile = dialog is bool ? (bool)dialog : false;
            if (!choseFile)
            {
                _discordInstaller.SavePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                _discordInstaller.SaveFile = "Discord Setup.exe";
            }
        }

        private void OnRunningVisible(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (!RunningPane.IsVisible) return; // If the pane is going invisible, don't do anything.
            ButtonBack.Visibility = Visibility.Hidden;
            ButtonNext.Content = "Finished";

            FilesDeleted.Text = "0 / 0";

            if (_installNewCopy)
            {
                InstallerBlock.Visibility = Visibility.Visible;
                ButtonLaunch.Visibility = Visibility.Visible;
                ButtonLaunch.IsEnabled = _downloadFinished;
            }

            Task.Run(() => _discordProcess.KillAll());
            DownloadInstaller();
            DeleteFiles();
        }

        private void DownloadInstaller()
        {
            void onData(object downloader, System.Net.DownloadProgressChangedEventArgs data)
            {
                RunningPane.Dispatcher.Invoke(() =>
                {
                    InstallerDownloadProgress.Value = data.ProgressPercentage;
                    InstallerBytesReceived.Text = data.BytesReceived.ToString() + " / " + data.TotalBytesToReceive.ToString();
                });
            }

            AsyncCompletedEventHandler downloadFinished = (object downloader, AsyncCompletedEventArgs data) =>
            {
                if (data.Error is Exception)
                {
                    DownloadError(data.Error.Message);
                }
                else if (data.Cancelled)
                {
                    DownloadError("Download Cancelled");
                }
                else _downloadFinished = true;
                ButtonLaunch.Dispatcher.Invoke(() => OnUninstallPartFinished());
            };

            if (_installNewCopy == true)
            {
                _discordInstaller.DownloadInstaller(onData, downloadFinished);
            }
        }

        private void DeleteFiles()
        {
            int totalFiles = _discordFiles.Files.Length + _discordFiles.Directories.Length;

            string fileFooter = " / " + totalFiles.ToString();

            if (totalFiles != 0)
            {
                // Spin off background task to delete files
                Task.Run(() =>
                {
                    int filesDeleted = 0;
                    double currentProgress;

                    // Updater function. On each file deleted, it will update
                    // counters and progress bar.
#pragma warning disable IDE0039 // Use local function does not make sense here. This is a file function
                    // passed to an event which will get unsubscribed to as soon as the task is finished.
                    FileManager.OnFileOperation progressUpdater = (object o, FileManagerEventArgs args) =>
                    {
                        filesDeleted++;
                        currentProgress = (double)filesDeleted * 100 / totalFiles;
                        FileDeletionProgress.Dispatcher.Invoke(() =>
                        {
                            FilesDeleted.Text = filesDeleted.ToString() + fileFooter;
                            FileDeletionProgress.Value = currentProgress;
                        });
                    };
#pragma warning restore IDE0039 // Use local function

                    _discordFiles.OnDeleted += progressUpdater;
                    _discordFiles.DeleteFiles();
                    _discordFiles.DeleteDirectories();
                    _discordFiles.OnDeleted -= progressUpdater;
                    _filesDeleted = true;
                    FileDeletionProgress.Dispatcher.Invoke(() =>
                    {
                        OnUninstallPartFinished();
                    });
                });
            }
            else
            {
                _filesDeleted = true;
                OnUninstallPartFinished();
            }
        }

        private void DownloadError(string errorText)
        {
            InstallerError.Dispatcher.Invoke(() =>
            {
                InstallerError.Text = errorText;
                InstallerError.Visibility = Visibility.Visible;
                ButtonRetry.Visibility = Visibility.Visible;
                ButtonRetry.IsEnabled = true;
            });
        }

        private void OnUnderstandChanged(object sender, RoutedEventArgs e)
        {
            var understandDelete = (UnderstandDeleteFiles.IsChecked is bool) ? (bool)UnderstandDeleteFiles.IsChecked : false;

            if (understandDelete)
            {
                ButtonNext.IsEnabled = true;
            }
            else
            {
                ButtonNext.IsEnabled = false;
            }
        }

        private void OnRetry(object sender, RoutedEventArgs e)
        {
            InstallerError.Dispatcher.Invoke(() =>
            {
                InstallerError.Visibility = Visibility.Hidden;
                ButtonRetry.Visibility = Visibility.Hidden;
                ButtonRetry.IsEnabled = false;
            });

            _discordInstaller.DeleteInstaller();
            DownloadInstaller();
        }

        private void OnLaunch(object sender, RoutedEventArgs e)
        {
            ButtonLaunch.IsEnabled = false;
            ButtonLaunch.Content = "Launching...";
            var launcher = _discordInstaller.RunInstaller();
            launcher.ContinueWith((t) =>
            {
                if (t.IsCompleted)
                {
                    ButtonNext.Dispatcher.Invoke(() =>
                    {
                        ButtonNext.IsEnabled = true;
                        ButtonLaunch.Content = "Launched";
                    });
                }
            });
        }

        private void OnUninstallPartFinished()
        {
            if (_downloadFinished && _filesDeleted && _installNewCopy)
            {
                ButtonLaunch.Visibility = Visibility.Visible;
                ButtonLaunch.IsEnabled = true;
            }

            if (_filesDeleted && !_installNewCopy)
            {
                ButtonNext.IsEnabled = true;
            }
        }

        private void Discord_Cleanup_Closing(object sender, CancelEventArgs e)
        {
            if (_filesDeleted && _installNewCopy && !_keepInstall)
                _discordInstaller.DeleteInstaller();
        }
    }
}
