using System;
using System.IO;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography;
using Common;

namespace Client
{
    /// <summary>
    /// Process files that has changed on disk and create protocol messages
    /// </summary>
    internal class ProcessFiles
    {
        private const int BlockSize = 100000;
        private readonly BlockingCollection<FileInfo> _collection;
        private readonly BlockingCollection<ProtocolMessage> _messageCollection;
        private readonly CancellationToken _cancellationToken;

        public ProcessFiles(BlockingCollection<FileInfo> collection, BlockingCollection<ProtocolMessage> messageCollection, CancellationToken cancellationToken)
        {
            _collection = collection;
            _messageCollection = messageCollection;
            _cancellationToken = cancellationToken;
        }

        public async Task Process()
        {
            foreach (var fileInfo in _collection.GetConsumingEnumerable())
                await ProcessFile(fileInfo);
        }

        private async Task ProcessFile(FileInfo fileInfo)
        {
            try
            {
                switch (fileInfo.ChangeType)
                {
                    case WatcherChangeTypes.Deleted:
                        await DeleteFile(fileInfo);
                        break;
                    case WatcherChangeTypes.Renamed:
                        await RenameFile(fileInfo);
                        break;
                    case WatcherChangeTypes.Created:
                    case WatcherChangeTypes.Changed:
                    case WatcherChangeTypes.All:
                        await ChangedFile(fileInfo);
                        break;
                }
            } catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private Task RenameFile(FileInfo fileInfo)
        {
            var message = new RenameMessage(fileInfo.Name, fileInfo.NewName);

            _messageCollection.Add(message, _cancellationToken);
            return Task.FromResult(0);
        }

        private Task DeleteFile(FileInfo fileInfo)
        {
            var message = new DeleteMessage(fileInfo.Name);

            _messageCollection.Add(message, _cancellationToken);
            return Task.FromResult(0);
        }

        private async Task ChangedFile(FileInfo file)
        {
            using (var stream = File.OpenRead(file.FullPath))
            {
                var size = stream.Length;
                var message = new ChangeMessage(file.Name, size);

                _messageCollection.Add(message, _cancellationToken);
                var crypto = new SHA256CryptoServiceProvider();

                for (int ii = 0; ii < (size / BlockSize) + 1; ii++)
                {
                    var buffer = new byte[BlockSize];
                    var read = await stream.ReadAsync(buffer, 0, BlockSize, _cancellationToken);
                    var base64 = Convert.ToBase64String(buffer);
                    var hash = Convert.ToBase64String(crypto.ComputeHash(buffer));

                    var partMessage = new PartMessage(file.Name, ii * BlockSize, read, hash);
                    var hashMessage = new HashMessage(file.Name, ii * BlockSize, read, hash, base64);

                    _messageCollection.Add(partMessage, _cancellationToken);
                    _messageCollection.Add(hashMessage, _cancellationToken);
                }
            }
        }
    }
}
