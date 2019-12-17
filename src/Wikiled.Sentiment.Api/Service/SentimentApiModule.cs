using Microsoft.Extensions.DependencyInjection;
using Wikiled.Common.Utilities.Modules;
using Wikiled.WebSockets.Client.Modules;

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
            return services;
        }
    }
}
