using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace DiscordCleanup
{
    class FileManager
    {
        // Discord puts files in both the local appdata, and the roaming appdata, so we need both places

        private string[] _paths;

        // Memoize files and directories because search is potentially expensive
        private string[] _discoveredDirectories;
        private string[] _discoveredFiles;
        private string[] _allFiles;
        private List<string> _explicitFiles;

        public string[] Directories { get => GetAllDirectories(); }
        public string[] Files { get => GetAllFiles(); }

        public FileManager(string path) : this(new string[] { path }) { }
        
        public FileManager(string[] paths)
        {
            var validPaths = paths.Where((t) => Directory.Exists(t)).ToArray();

            Array.Sort(validPaths);
            _paths = validPaths;
            _explicitFiles = new List<string>();
        }

        // These functions all work the same way. If the directory exists, and
        // is populated, return the files/directories in it, otherwise, return
        // an empty set

        private List<string> GetDirectoryList(string path)
        {
            if (!Directory.Exists(path)) return new List<string>(); // If the directory doesn't exist, return an empty list

            var directories = Directory.GetDirectories(path);

            // Build our directory tree, starting with the subdirectories found
            List<string> dirs = new List<string>(directories);

            // Add all the subdirectories' trees
            var dirTree = directories.SelectMany((t) => GetDirectoryList(t));
            dirs.AddRange(dirTree);

            return dirs;
        }

        private string[] GetDirectoryTrees(string[] paths)
        {
            List<string> directories = new List<string>();
            directories.AddRange(paths);
            directories.AddRange(paths.SelectMany((t) => GetDirectoryList(t)));
            directories.Sort(); // Return them alphabetically
            return directories.ToArray();
        }

        private string[] GetFiles(string path)
        {
            // For each directory in the tree, get a list of files in each directory, flattening the list
            return Directory.Exists(path)
                ? Directory.GetFiles(path)
                : new string[0];

        }

        // Aggregate commands which return strings listing all files and
        // directories, sorted by name, memoizing the results as these are both
        // expensive commands to run which usually won't change

        private string[] GetAllDirectories()
        {
            if (_discoveredDirectories is null || _discoveredDirectories.Length == 0)
            {
                _discoveredDirectories = GetDirectoryTrees(_paths);
                Array.Sort(_discoveredDirectories);
            }

            return _discoveredDirectories;
        }

        private void DiscoverFiles()
        {
            var directories = GetAllDirectories();
            _discoveredFiles = directories.SelectMany((t) => GetFiles(t)).ToArray();
            _allFiles = null;
        }

        private void BuildFileArray()
        {
            var allFiles = _discoveredFiles.Concat(_explicitFiles);
            
            _allFiles = allFiles.ToArray();
            Array.Sort(_allFiles);
        }

        private string[] GetAllFiles()
        {
            if (_discoveredFiles is null || _discoveredFiles.Length == 0)
            {
                DiscoverFiles();
            }

            if (_allFiles is null)
            {
                BuildFileArray();
            }
            
            return _allFiles;
        }

        public void AddExplicitFile(string file) => AddExplicitFiles(new string[] { file });

        public void AddExplicitFiles(string[] files)
        {
            _explicitFiles.AddRange(files);

            _allFiles = null;
        }

        public void Rehash()
        {
            _discoveredDirectories = null;
            _discoveredFiles = null;
            GetAllFiles();  // We just need to run GetAllFiles(), because it
                            // calls GetAllDirectories as part of its work
        }


        private (string Name, long Size, DateTime Date) GetFileData(string file)
        {
            string name = file;
            long size;
            DateTime date;

            if (File.Exists(name))
            {
                var info = new FileInfo(name);
                size = info.Length;
                date = info.CreationTime;
            }
            else
            {
                size = 0;
                date = DateTime.MinValue;
            }

            return (name, size, date);
        }

        public (string Name, long Size, DateTime Date)[] GetFileData()
        {
            return GetAllFiles().Select((t) => { return GetFileData(t); }).ToArray();
        }

        private (string Name, DateTime Date) GetDirectoryData(string dir)
        {
            string name = dir;
            DateTime date;

            if (Directory.Exists(name))
            {
                var info = new DirectoryInfo(name);
                date = info.CreationTime;
            }
            else
            {
                date = DateTime.MinValue;
            }

            return (name, date);
        }

        public (string Name, DateTime Date)[] GetDirectoryData()
        {
            return GetAllDirectories().Select((t) => GetDirectoryData(t)).ToArray();
        }

        public void DeleteFiles()
        {
            Array.ForEach(GetAllFiles(), (Action<string>)((t) => {
                File.Delete(t);
                OnDeleted(this, new FileManagerEventArgs(t, FileManagerEventArgs.FileOperation.Deleted));
            }));
            _discoveredFiles = new string[0];
        }

        // Reverse the order of the directories because the structure given
        // gets the rootmost directories first. This way, we delete the deepest
        // directories first, and thus only ever delete empty directories

        public void DeleteDirectories() {
            string[] reversedDirectories = GetAllDirectories().Reverse().ToArray();

            Array.ForEach(reversedDirectories, (Action<string>)((t) => {
                Directory.Delete(t);
                OnDeleted(this, new FileManagerEventArgs(t, FileManagerEventArgs.FileOperation.Deleted));
            }));
            _discoveredDirectories = new string[0];
        }

        public delegate void OnFileOperation(object o, FileManagerEventArgs e);
        public event OnFileOperation OnDeleted;
    }

    public class FileManagerEventArgs : EventArgs
    {
        public enum FileOperation { Created, Written, Read, Deleted};

        public FileOperation Operation { get; }
        public string FileName { get; }
        public FileManagerEventArgs(string filename, FileOperation operation)
        {
            FileName = filename;
            Operation = operation;
        }
    }
}
