using System.IO;

namespace Client
{
    /// <summary>
    /// Information about a file change on disk
    /// </summary>
    internal class FileInfo
    {
        public FileInfo(string name, string fullPath, WatcherChangeTypes changeType)
        {
            Name = name;
            FullPath = fullPath;
            ChangeType = changeType;
        }

        public FileInfo(string name, string newName, string fullPath, WatcherChangeTypes changeType)
            : this(name, fullPath, changeType)
        {
            NewName = newName;
        }

        public string Name { get; }

        public string NewName { get; }

        public string FullPath { get; }

        public WatcherChangeTypes ChangeType { get; }
    }
}
