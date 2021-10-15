using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;

namespace WebhookServer
{
    public class GrpcServer
    {
        private GrpcService _service;
        private int _port;

        private Server _grpcServer;

        public GrpcServer(GrpcService service, int port)
        {
            _service = service;
            _port = port;
        }

        public void Start()
        {
            _grpcServer = new Server()
            {
                Ports = { new ServerPort("127.0.0.1", _port, ServerCredentials.Insecure) },
            };

            var definition = WebhookService.BindService(_service);
            _grpcServer.Services.Add(definition);
            _grpcServer.Start();
        }

        public Task ShutdownAsync()
        {
            return _grpcServer.ShutdownAsync();
        }
    }

    public class GrpcService : WebhookService.WebhookServiceBase
    {
        private ConcurrentDictionary<string, IServerStreamWriter<UpdateReply>> _subscribers = new ConcurrentDictionary<string, IServerStreamWriter<UpdateReply>>();

        public GrpcService()
        {
        }

        public override async Task Subscribe(SubscribeRequest request, IServerStreamWriter<UpdateReply> responseStream, ServerCallContext context)
        {
            _subscribers.TryAdd(context.Host, responseStream);

            Console.WriteLine($"Connected to host {context.Host}");

            try
            {
                await context.CancellationToken;
            }
            catch
            {
                // Do nothing
            }

            _subscribers.TryRemove(context.Host, out _);
        }

        public async Task UpdateSubscribers(string repo)
        {
            var reply = new UpdateReply
            {
                Repo = repo
            };

            foreach (var subscriber in _subscribers.ToArray())
            {
                try
                {
                    Console.WriteLine("Writing to client");
                    await subscriber.Value.WriteAsync(reply);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }
    }

    public static class AsyncExtensions
    {
        /// <summary>
        /// Allows a cancellation token to be awaited.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public static CancellationTokenAwaiter GetAwaiter(this CancellationToken ct)
        {
            // return our special awaiter
            return new CancellationTokenAwaiter
            {
                CancellationToken = ct
            };
        }

        /// <summary>
        /// The awaiter for cancellation tokens.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public struct CancellationTokenAwaiter : INotifyCompletion, ICriticalNotifyCompletion
        {
            public CancellationTokenAwaiter(CancellationToken cancellationToken)
            {
                CancellationToken = cancellationToken;
            }

            internal CancellationToken CancellationToken;

            public object GetResult()
            {
                // this is called by compiler generated methods when the
                // task has completed. Instead of returning a result, we 
                // just throw an exception.
                if (IsCompleted) throw new OperationCanceledException();
                else throw new InvalidOperationException("The cancellation token has not yet been cancelled.");
            }

            // called by compiler generated/.net internals to check
            // if the task has completed.
            public bool IsCompleted => CancellationToken.IsCancellationRequested;

            // The compiler will generate stuff that hooks in
            // here. We hook those methods directly into the
            // cancellation token.
            public void OnCompleted(Action continuation) =>
                CancellationToken.Register(continuation);
            public void UnsafeOnCompleted(Action continuation) =>
                CancellationToken.Register(continuation);
        }
    }
}
