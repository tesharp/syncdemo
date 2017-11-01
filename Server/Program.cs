using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    class Program
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
            
            if (Directory.EnumerateFiles(watchDirectory).Any())
            {
                Console.WriteLine("Directory should be empty.");
                Environment.Exit(1337);
            }

            var cancellationTokenSource = new CancellationTokenSource();
            var ip = IPAddress.Parse("127.0.0.1");
            var listener = new TcpListener(ip, 8080);
            listener.Start();

            Console.WriteLine("Listening for connections...");

            var task = Task.Run(() => WaitForConnections(listener, watchDirectory, cancellationTokenSource.Token));

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("Press a key to exit");
            Console.ResetColor();
            Console.ReadKey();

            Console.WriteLine("Cancelling");
            cancellationTokenSource.Cancel();
            listener.Stop();

            await Task.WhenAll(task);
        }

        static async Task WaitForConnections(TcpListener listener, string directory, CancellationToken cancellationToken)
        {
            var tasks = new ConcurrentBag<Task>();

            try
            {
                var client = await listener.AcceptTcpClientAsync();

                Console.WriteLine("Client connected. Processing commands");
                var processor = new ProcessFiles(client, directory, cancellationToken);
                await processor.Process();
            } catch (ObjectDisposedException)
            {
                Console.WriteLine("Stop acception new connections");
                return;
            }
        }
    }
}
