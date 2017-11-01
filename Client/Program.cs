using Common;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Application requires one argument which is the directory to watch for file changes.");
                Environment.Exit(1337);
            }

            var watchDirectory = args[0];

            if (!Directory.Exists(watchDirectory))
            {
                Console.WriteLine($"Directory {watchDirectory} does not exist.");
                Environment.Exit(1337);
            }

            var collection = new BlockingCollection<FileInfo>();
            var messageCollection = new BlockingCollection<ProtocolMessage>();
            var cancellationTokenSource = new CancellationTokenSource();
            var watcher = new FileWatchProducer(collection, cancellationTokenSource.Token);
            var processor = new ProcessFiles(collection, messageCollection, cancellationTokenSource.Token);
            var messageProcessor = new ProcessMessages(messageCollection, cancellationTokenSource.Token);

            var tasks = new[]
            {
                Task.Run(() => watcher.Watch(watchDirectory)),
                Task.Run(() => processor.Process()),
                Task.Run(() => messageProcessor.Process())
            };

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Press a key to exit");
            Console.ResetColor();
            Console.ReadKey();

            collection.CompleteAdding();
            messageCollection.CompleteAdding();
            cancellationTokenSource.Cancel();

            await Task.WhenAll(tasks);
        }
    }
}
