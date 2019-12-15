using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Threading;
using System.Threading.Tasks;
using Wikiled.Sentiment.Analysis.Containers;

namespace Wikiled.Sentiment.Service.Services
{
    public class WarmupService : IHostedService
    {
        private readonly IServiceProvider provider;

        public WarmupService(IServiceProvider provider)
        {
            this.provider = provider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            using (var scope = provider.CreateScope())
            {
                var container = scope.ServiceProvider.GetService<ISessionContainer>();

                var client = container.GetTesting(null);
                client.Init();
            }

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
