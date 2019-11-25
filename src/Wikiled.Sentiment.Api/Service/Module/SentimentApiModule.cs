using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using MQTTnet.Client;
using System;
using Wikiled.Common.Utilities.Modules;
using Wikiled.Sentiment.Api.Service.Flow;
using Wikiled.Sentiment.Api.Service.Mqtt;

namespace Wikiled.Sentiment.Api.Service.Module
{
    public class SentimentApiModule : IModule
    {
        public SentimentApiModule(MqttConnectionInfo connectionInfo)
        {
            ConnectionInfo = connectionInfo ?? throw new ArgumentNullException(nameof(connectionInfo));
        }

        private MqttConnectionInfo ConnectionInfo { get;  }

        public IServiceCollection ConfigureServices(IServiceCollection services)
        {
            services.AddTransient<ISentimentFlow, SentimentFlow>();
            services.RegisterModule<CommonModule>();
            services.RegisterModule<LoggingModule>();

            services.AddSingleton(ConnectionInfo);
            services.AddSingleton<IMqttClientFactory, MqttFactory>();
            services.AddTransient<IMqttConnection, MqttConnection>().AddFactory<IMqttConnection>();

            services.AddSingleton<ISentimentAnalysisSetup, SentimentAnalysisSetup>();
            return services;
        }
    }
}
