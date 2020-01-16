using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using Wikiled.Common.Net.Client;
using Wikiled.Common.Utilities.Modules;
using Wikiled.Sentiment.Api.Request.Messages;
using Wikiled.Text.Analysis.Structure;
using Wikiled.WebSockets.Client.Modules;
using Wikiled.WebSockets.Definitions.Messages;

namespace Wikiled.Sentiment.Api.Service
{
    public class SentimentApiModule : IModule
    {
        private readonly Uri apiUri;

        public SentimentApiModule(Uri apiUri)
        {
            this.apiUri = apiUri;
        }

        public HttpClient Client { get; set; }

        public IServiceCollection ConfigureServices(IServiceCollection services)
        {
            Client ??= new HttpClient();
            services.AddSingleton<IApiClientFactory>(ctx => new ApiClientFactory(Client, apiUri));
            services.AddSingleton<IBasicSentimentAnalysis, BasicSentimentAnalysis>();
            services.RegisterModule<ClientServiceModule>();
            services.RegisterModule<CommonModule>();
            services.RegisterModule<LoggingModule>();
            services.AddTransient<ISentimentAnalysis, SentimentAnalysis>();
            services.AddSingleton<Message, ResultMessage<Document>>();
            services.AddSingleton<Message, CompletedMessage>();
            return services;
        }
    }
}
