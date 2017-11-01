using Common;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    internal class ProcessMessages
    {
        private readonly BlockingCollection<ProtocolMessage> _collection;
        private readonly CancellationToken _cancellationToken;
        private readonly Dictionary<string, HashPartInfo> _hashSet;

        public ProcessMessages(BlockingCollection<ProtocolMessage> collection, CancellationToken cancellationToken)
        {
            _collection = collection;
            _cancellationToken = cancellationToken;
            _hashSet = new Dictionary<string, HashPartInfo>();
        }

        public async Task Process()
        {
            var ip = IPAddress.Parse("127.0.0.1");
            var client = new TcpClient();

            try
            {
                Console.WriteLine("Connection to server");
                await client.ConnectAsync(ip, 8080);

                var stream = client.GetStream();

                using (var writer = new StreamWriter(stream))
                {
                    foreach (var message in _collection.GetConsumingEnumerable())
                    {
                        PrintMessage(message);

                        if (message is PartMessage part && !_hashSet.ContainsKey(part.Hash))
                        {
                            var partInfo = new HashPartInfo(part.File, part.Start, part.Length);
                            _hashSet.Add(part.Hash, partInfo);
                        }
                        if (message is HashMessage hash && _hashSet.ContainsKey(hash.Hash))
                        {
                            if (_hashSet[hash.Hash].Transmitted)
                            {
                                Console.WriteLine("Hash already written, skipping.");
                                continue;
                            }

                            _hashSet[hash.Hash].Transmitted = true;
                        }
                        if (message is DeleteMessage delete)
                        {
                            foreach (var key in _hashSet.Where(kvp => kvp.Value.File == delete.File).Select(kvp => kvp.Key).ToArray())
                                _hashSet.Remove(key);
                        }

                        await writer.WriteLineAsync(message.ToString());

                        await writer.FlushAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
        }

        private void PrintMessage(ProtocolMessage message)
        {
            switch (message)
            {
                case ChangeMessage change:
                    Console.WriteLine($"Change :: {change.File}");
                    break;
                case RenameMessage rename:
                    Console.WriteLine($"Rename :: {rename.File} -> {rename.NewName}");
                    break;
                case DeleteMessage delete:
                    Console.WriteLine($"Delete :: {delete.File}");
                    break;
            }
        }
    }
}
