using System;
using System.Net;
using System.Threading.Tasks;

namespace WebhookServer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var grpcService = new GrpcService();
            var server = new GrpcServer(grpcService, 45679);
            server.Start();

            var webhookService = new WebhookServer(45678, grpcService.UpdateSubscribers);

            try
            {
                webhookService.Startup();

                while (true)
                {
                    await Task.Delay(TimeSpan.FromMinutes(1));
                    Console.WriteLine("I'm still alive!");
                }
            }
            finally
            {
                webhookService.Shutdown();
                await server.ShutdownAsync();
            }
        }
    }
}
