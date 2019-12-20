using Microsoft.Extensions.DependencyInjection;
using Wikiled.Common.Utilities.Modules;
using Wikiled.Sentiment.Api.Request.Messages;
using Wikiled.Text.Analysis.Structure;
using Wikiled.WebSockets.Client.Modules;
using Wikiled.WebSockets.Definitions.Messages;

namespace Wikiled.Sentiment.Api.Service
{
    public class SentimentApiModule : IModule
    {
        public IServiceCollection ConfigureServices(IServiceCollection services)
        {
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
