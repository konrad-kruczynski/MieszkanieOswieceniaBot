using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Nito.AsyncEx;
using PeanutButter.SimpleHTTPServer;

namespace MieszkanieOswieceniaBot
{
    public sealed class ApiHandler
    {
        public ApiHandler()
        {
            requests = new AsyncProducerConsumerQueue<Request>();
            responses = new ConcurrentDictionary<Guid, AsyncProducerConsumerQueue<Response>>();
            server = new HttpServer(8080, false, null);
            server.AddHtmlDocumentHandler((http, stream) =>
            {
                var commandText = http.Path.TrimStart('/');
                var id = Guid.NewGuid();
                var responseQueue = new AsyncProducerConsumerQueue<Response>();
                responses.TryAdd(id, responseQueue);
                var request = new Request { For = id, CommandName = commandText, Parameters = http.UrlParameters };
                CircularLogger.Instance.Log($"Request: {commandText}, parameters: {http.UrlParameters}");
                requests.Enqueue(request);
                var response = responseQueue.Dequeue();
                responses.Remove(id, out _);
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    http.WriteFailure(response.StatusCode);
                }
                
                http.WriteDataToStream("HTTP/1.1 200 OK\r\n");
                http.WriteDataToStream("Connection: close\r\n");
                http.WriteDataToStream("Content-Type: text/html\r\n");
                http.WriteDataToStream($"Content-Length: {response.Text.Length}\r\n");
                http.WriteDataToStream("\r\n");
                return response.Text;
            });
        }

        public void Start()
        {
            server.Start();
        }

        public void AddResponseFor(Guid id, HttpStatusCode statusCode, string text)
        {
            if (!responses.TryGetValue(id, out var responseQueue))
            {
                throw new InvalidOperationException("Should not reach here.");
            }

            var response = new Response { StatusCode = statusCode, Text = text };
            responseQueue.Enqueue(response);
        }

        public async Task<(Guid id, string CommandName, IReadOnlyDictionary<string, string> Parameters)> GetNextRequestAsync()
        {
            var request = await requests.DequeueAsync();
            return (request.For, request.CommandName, request.Parameters);
        }

        private readonly AsyncProducerConsumerQueue<Request> requests;
        private readonly ConcurrentDictionary<Guid, AsyncProducerConsumerQueue<Response>> responses;
        private readonly HttpServer server;

        private sealed record Request
        {
            public Guid For { get; init; }
            public string CommandName { get; init; }
            public IReadOnlyDictionary<string, string> Parameters { get; init; }
        }

        private sealed record Response
        {
            public HttpStatusCode StatusCode { get; init; }
            public string Text { get; init; }
        }

    }	
}

