using Client;
using Core;
using System.Threading;
using System.Threading.Tasks;

namespace Refresh.Console
{
    using Console = System.Console;
    class Program
    {
        static async Task Main(string[] args)
        {
            var updater = new Updater();
            var subscriber = new WebhookServiceClient(updater, @"http://refresh-webhook.westus2.cloudapp.azure.com");
            subscriber.Startup();

            while (true)
            {
                Console.WriteLine("Waiting...");
                await subscriber.WaitAndUpdateAsync(CancellationToken.None);
            }
        }
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
