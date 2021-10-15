using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace WebhookServer
{
    class WebhookServer
    {
        private readonly uint _port;
        private Func<string, Task> _callback;

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private HttpListener _listener;

        public WebhookServer(uint port, Func<string, Task> callback)
        {
            _port = port;
            _callback = callback;
        }

        public void Startup()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://+:{_port}/");

            _listener.Start();

            Task.Run(LoopAsync);
        }

        public void Shutdown()
        {
            _cts.Cancel();

        }

        private async Task LoopAsync()
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (Exception e)
                {
                    if (_cts.Token.IsCancellationRequested)
                    {
                        break;
                    }

                    Console.WriteLine($"Failed to get HTTP context: {e}");

                    continue;
                }

                WebhookRequest parsed;
                try
                {
                    var request = context.Request;

                    Console.WriteLine("Processing new context...");

                    var reader = new StreamReader(request.InputStream);
                    var json = reader.ReadToEnd();

                    parsed = JsonSerializer.Deserialize<WebhookRequest>(json);
                }
                catch (Exception e)
                {
                    var writer = new StreamWriter(context.Response.OutputStream);
                    writer.WriteLine(e);
                    Console.WriteLine(e);
                    context.Response.OutputStream.Close();

                    continue;
                }

                var response = "OK";
                var writer2 = new StreamWriter(context.Response.OutputStream);
                writer2.WriteLine(response);
                Console.WriteLine(response);
                context.Response.OutputStream.Close();

                // TODO: run in separate thread.
                await _callback.Invoke(parsed.repository.full_name);
            }
        }

        private sealed class WebhookRequest
        {
            public RequestRepo repository { get; set; }
        }

        private sealed class RequestRepo
        {
            public string full_name { get; set; }
        }
    }
}
