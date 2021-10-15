using Core;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Threading;
using System.Threading.Tasks;
using static WebhookService;

namespace Client
{
    public class WebhookServiceClient : IUpdateSubscriber
    {
        public IUpdater _inner;
        public string _serverAddress;

        private GrpcChannel _channel;
        private WebhookService.WebhookServiceClient _client;
        private AsyncServerStreamingCall<UpdateReply> _call;

        public WebhookServiceClient(IUpdater inner, string serverAddress)
        {
            _inner = inner;
            _serverAddress = serverAddress;
        }

        public void Startup()
        {
            _channel = GrpcChannel.ForAddress($"{_serverAddress}:45679");
            _client = new WebhookService.WebhookServiceClient(_channel);

            _call = _client.Subscribe(new SubscribeRequest());
        }

        public async Task WaitAndUpdateAsync(CancellationToken token)
        {
            try
            {
                Console.WriteLine("Waiting for service response...");
                var success = await _call.ResponseStream.MoveNext(token);
                Console.WriteLine("Got service response!");

                if (!success)
                {
                    throw new Exception("Didn't receive a respose!");
                }

                var response = _call.ResponseStream.Current;

                await _inner.Update(token, response.Repo);
            }
            catch (Exception e)
            {
                if (!token.IsCancellationRequested)
                {
                    Console.WriteLine($"Something went wrong: {e}");
                }
            }
        }

        public void Shutdown()
        {
            _call.Dispose();
            _channel.Dispose();
        }
    }
}
