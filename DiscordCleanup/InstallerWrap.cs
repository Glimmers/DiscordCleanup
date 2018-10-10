using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.ComponentModel;

namespace DiscordCleanup
{
    class InstallerWrap
    {
        Uri _installerURI;
        WebClient _downloader;
        Process _installerProcess;
        string _tempFile;

        public bool UseFilePath { get; set; }
        public string SavePath { get; set; }
        public string SaveFile { get; set; }
        public string FullPath => Path.Combine(SavePath, SaveFile);
        public string InstallerFile => UseFilePath ? FullPath : _tempFile;

        public InstallerWrap(string uri)
        {
            _installerURI = new Uri(uri);
            SavePath = Path.GetTempPath();
            SaveFile = Path.GetRandomFileName();
            SaveFile = Path.ChangeExtension(SaveFile, "exe");
            _tempFile = FullPath;
            _downloader = new WebClient();
            _downloader.Headers["User-Agent"] = "Unofficial Discord Cleanup";
        }

        public void DownloadInstaller(DownloadProgressChangedEventHandler onData,
            AsyncCompletedEventHandler onFinish)
        {
            _downloader.DownloadProgressChanged += onData;
            _downloader.DownloadFileCompleted += onFinish;

            _downloader.DownloadFileAsync(_installerURI, InstallerFile);
        }

        public Task RunInstaller()
        {
            if (!File.Exists(FullPath)) throw new FileNotFoundException("File does not exist: " + FullPath);

            return Task.Run(() =>
            {
                _installerProcess = Process.Start(InstallerFile);
                _installerProcess.WaitForExit();
            });

        }

        public void DeleteInstaller()
        {
            if (File.Exists(InstallerFile))
            {
                File.Delete(InstallerFile);
            }
        }

    }
}
