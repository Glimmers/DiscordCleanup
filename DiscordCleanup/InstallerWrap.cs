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

        public string SavePath { get; set; }
        public string SaveFile { get; set; }
        public string FullPath => Path.Combine(SavePath, SaveFile);

        public InstallerWrap(string uri)
        {
            _installerURI = new Uri(uri);
            SavePath = Path.GetTempPath();
            SaveFile = Path.GetRandomFileName();
            _downloader = new WebClient();
            _downloader.Headers["User-Agent"] = "Unofficial Discord Cleanup";
        }

        public void DownloadInstaller(DownloadProgressChangedEventHandler onData,
            AsyncCompletedEventHandler onFinish)
        {
            _downloader.DownloadProgressChanged += onData;
            _downloader.DownloadFileCompleted += onFinish;
            
            _downloader.DownloadFileAsync(_installerURI, FullPath);
        }

        public Task RunInstaller()
        {
            if (!File.Exists(FullPath)) throw new FileNotFoundException("File does not exist: " + FullPath);

            return Task.Run(() =>
            {
                _installerProcess = Process.Start(FullPath);
                _installerProcess.WaitForExit();
            });

        }

        public void DeleteInstaller()
        {
            if (File.Exists(FullPath))
            {
                File.Delete(FullPath);
            }
        }

    }
}
