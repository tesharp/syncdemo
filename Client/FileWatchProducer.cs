using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    /// <summary>
    /// Watch for file changes and add to collection when anything is changed
    /// </summary>
    internal class FileWatchProducer
    {
        private readonly BlockingCollection<FileInfo> _collection;
        private readonly CancellationToken _cancellationToken;

        public FileWatchProducer(BlockingCollection<FileInfo> collection, CancellationToken cancellationToken)
        {
            _collection = collection;
            _cancellationToken = cancellationToken;
        }

        public async Task Watch(string directory)
        {
            Console.WriteLine($"Watching directory {directory} for changes");

            AddInitialFiles(directory);

            var fileSystemWatcher = new FileSystemWatcher(directory)
            {
                NotifyFilter = NotifyFilters.FileName
            };

            fileSystemWatcher.Created += OnChanged;
            fileSystemWatcher.Changed += OnChanged;
            fileSystemWatcher.Deleted += OnChanged;
            fileSystemWatcher.Renamed += OnRenamed;

            fileSystemWatcher.EnableRaisingEvents = true;

            // Listen until we receive a cancellation
            while (!_cancellationToken.IsCancellationRequested)
                await Task.Delay(1000);

            fileSystemWatcher.EnableRaisingEvents = false;
            fileSystemWatcher.Dispose();
        }

        public void AddInitialFiles(string directory)
        {
            var count = 0;

            foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly))
            {
                _collection.Add(new FileInfo(Path.GetFileName(file), file, WatcherChangeTypes.All), _cancellationToken);
                count++;
            }

            Console.WriteLine($"Found {count} existing files in folder");
        }

        private void OnChanged(object source, FileSystemEventArgs args)
        {
            Console.WriteLine($"{args.ChangeType} :: {args.Name}");
            _collection.Add(new FileInfo(args.Name, args.FullPath, args.ChangeType), _cancellationToken);
        }

        private void OnRenamed(object source, RenamedEventArgs args)
        {
            Console.WriteLine($"{args.ChangeType} :: {args.Name}");
            _collection.Add(new FileInfo(args.OldName, args.Name, args.FullPath, args.ChangeType), _cancellationToken);
        }
    }
}
