using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Requests;
using Telegram.Bot.Requests.Abstractions;

namespace Tests
{
    public class FakeBotClient : ITelegramBotClient
    {
        private long botId;
        public string LastText { get; private set; }

        public long? BotId => throw new NotImplementedException();

        long ITelegramBotClient.BotId => botId;

        public TimeSpan Timeout { get; set; }
        public IExceptionParser ExceptionsParser { get; set; }

        public Task DownloadFile(string filePath, Stream destination, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public bool LocalBotServer => throw new NotImplementedException();

        public event AsyncEventHandler<ApiRequestEventArgs> OnMakingApiRequest;
        public event AsyncEventHandler<ApiResponseEventArgs> OnApiResponseReceived;

        public Task DownloadFileAsync(string filePath, Stream destination, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<TResponse> SendRequest<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<TResponse> MakeRequest<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<TResponse> MakeRequestAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (request is SendMessageRequest sendMessageRequest)
            {
                LastText = sendMessageRequest.Text;
            }

            return Task.FromResult(default(TResponse));
        }

        public Task<bool> TestApi(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new NotImplementedException();
        }

        public Task<bool> TestApiAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }
}

