using Microsoft.Extensions.DependencyInjection;
using Wikiled.Common.Utilities.Modules;
using Wikiled.Sentiment.Api.Service.Flow;

namespace Wikiled.Sentiment.Api.Service.Module
{
    public class SentimentApiModule : IModule
    {
        public IServiceCollection ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ISentimentFlow, SentimentFlow>();
            services.RegisterModule<CommonModule>();
            services.RegisterModule<LoggingModule>();

       
            services.AddSingleton<ISentimentAnalysisSetup, SentimentAnalysisSetup>();
            return services;
        }
    }
}
