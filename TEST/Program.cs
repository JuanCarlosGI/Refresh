using Client;
using Core;
using System;
using System.Threading;
using System.Threading.Tasks;
using WebhookServer;

namespace TEST
{
    class Program
    {
        static void Main(string[] args)
        {
            var service = new GrpcService();
            var server = new GrpcServer(service, 45679);

            Console.WriteLine("1");
            server.Start();

            Console.WriteLine("2");
            var client = new WebhookServiceClient(new Updater(), @"http://localhost");
            Console.WriteLine("3");
            client.Startup();
            client.WaitAndUpdateAsync(CancellationToken.None);// This will die :)
            Task.Delay(10000).GetAwaiter().GetResult();
            Console.WriteLine("4");

            service.UpdateSubscribers("asd").GetAwaiter().GetResult();
            Console.WriteLine("5");

            Task.Delay(60000).GetAwaiter().GetResult();
            Console.WriteLine("6");
        }

        class Updater : IUpdater
        {
            public Task Update(CancellationToken token, string repo)
            {
                Console.WriteLine(repo);
                return Task.CompletedTask;
            }
        }
    }
}
