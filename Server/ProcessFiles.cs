using Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    internal class ProcessFiles
    {
        private readonly TcpClient _client;
        private readonly string _directory;
        private readonly CancellationToken _cancellationToken;
        private readonly Dictionary<string, HashPartInfo> _hashSet;

        public ProcessFiles(TcpClient client, string directory, CancellationToken cancellationToken)
        {
            _client = client;
            _directory = directory;
            _cancellationToken = cancellationToken;
            _hashSet = new Dictionary<string, HashPartInfo>();
        }

        public async Task Process()
        {
            var stream = _client.GetStream();

            using (var reader = new StreamReader(stream))
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    var line = await reader.ReadLineAsync();
                    if (string.IsNullOrEmpty(line))
                        continue;

                    var separator = line.IndexOf('|');
                    if (separator == -1)
                    {
                        Console.WriteLine($"Unknown message: {line}");
                        continue;
                    }

                    await DecodeMessage(line);
                }
            }
        }

        private async Task DecodeMessage(string message)
        {
            var parts = message.Split('|');

            switch (parts[0])
            {
                case "CHA":
                    Change(new ChangeMessage(parts[1], long.Parse(parts[2])));
                    break;
                case "DEL":
                    Delete(new DeleteMessage(parts[1]));
                    break;
                case "HASH":
                    await Hash(new HashMessage(parts[1], long.Parse(parts[2]), int.Parse(parts[3]), parts[4], parts[5]));
                    break;
                case "PART":
                    await Part(new PartMessage(parts[1], long.Parse(parts[2]), int.Parse(parts[3]), parts[4]));
                    break;
                case "REN":
                    Rename(new RenameMessage(parts[1], parts[2]));
                    break;
                default:
                    Console.WriteLine($"Unknown message: {message}");
                    break;
            }
        }

        private void Change(ChangeMessage message)
        {
            Console.WriteLine($"Change :: {message.File}");

            // This is not optimal.. a small change should require recreating the file
            var localPath = Path.Combine(_directory, message.File);
            if (File.Exists(localPath))
                File.Delete(localPath);

            var removed = _hashSet.Where(kvp => kvp.Value.File == message.File).Select(kvp => kvp.Key).ToArray();

            foreach (var remove in removed)
                _hashSet.Remove(remove);
        }

        private void Delete(DeleteMessage message)
        {
            Console.WriteLine($"Delete :: {message.File}");

            var localPath = Path.Combine(_directory, message.File);
            if (File.Exists(localPath))
                File.Delete(localPath);

            var removed = _hashSet.Where(kvp => kvp.Value.File == message.File).Select(kvp => kvp.Key).ToArray();

            foreach (var remove in removed)
                _hashSet.Remove(remove);
        }

        private async Task Part(PartMessage message)
        {
            Console.WriteLine($"Part :: {message.File} - {message.Start} - {message.Length}");

            if (!_hashSet.ContainsKey(message.Hash))
            {
                _hashSet.Add(message.Hash, new HashPartInfo(message.File, message.Start, message.Length));
                return;
            }

            var partInfo = _hashSet[message.Hash];
            var localFile = Path.Combine(_directory, message.File);
            var partFile = Path.Combine(_directory, partInfo.File);

            if (!partInfo.Transmitted)
                return;

            using (var reader = File.OpenRead(partFile))
            using (var stream = File.OpenWrite(localFile))
            {
                var buffer = new byte[partInfo.Length];
                reader.Seek(partInfo.Start, SeekOrigin.Begin);
                await reader.ReadAsync(buffer, 0, partInfo.Length, _cancellationToken);

                stream.Seek(message.Start, SeekOrigin.Begin);
                await stream.WriteAsync(buffer, 0, message.Length, _cancellationToken);
            }
        }

        private async Task Hash(HashMessage message)
        {
            Console.WriteLine($"Hash :: {message.Hash}");

            var partInfo = _hashSet[message.Hash];
            var localFile = Path.Combine(_directory, message.File);

            using (var stream = File.OpenWrite(localFile))
            {
                stream.Seek(message.Start, SeekOrigin.Begin);
                await stream.WriteAsync(Convert.FromBase64String(message.Content), 0, message.Length, _cancellationToken);
            }

            partInfo.Transmitted = true;
        }

        private void Rename(RenameMessage message)
        {
            Console.WriteLine($"Rename :: {message.File} -> {message.NewName}");

            var file = Path.Combine(_directory, message.File);
            if (File.Exists(file))
            {
                var destFile = Path.Combine(_directory, message.NewName);
                File.Move(file, destFile);

                foreach (var entry in _hashSet.Where(kvp => kvp.Value.File == message.File))
                    entry.Value.File = message.NewName;
            } else
            {
                Console.WriteLine($"Could not find file {message.File} for rename");
            }
        }
    }
}
