using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using Wikiled.Common.Utilities.Resources.Config;
using Wikiled.Sentiment.Analysis.Containers;
using Wikiled.Sentiment.Text.Config;
using Wikiled.Sentiment.Text.Parser;
using Wikiled.Server.Core.Helpers;

namespace Wikiled.Sentiment.Service.Services
{
    public class InitialisationService : IAsyncInitialiser
    {
        private readonly ILogger<InitialisationService> logger;

        private readonly IWebHostEnvironment environment;

        private readonly LexiconLoader loader;

        private readonly IServiceProvider provider;

        private ConfigDownloader<ILexiconConfig> lexiconDownloader;

        public InitialisationService(ILogger<InitialisationService> logger, IWebHostEnvironment environment, IServiceProvider provider, LexiconLoader loader, ConfigDownloader<ILexiconConfig> lexiconDownloader)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.environment = environment ?? throw new ArgumentNullException(nameof(environment));
            this.provider = provider ?? throw new ArgumentNullException(nameof(provider));
            this.loader = loader ?? throw new ArgumentNullException(nameof(loader));
            this.lexiconDownloader = lexiconDownloader;
        }

        public async Task InitializeAsync()
        {
            logger.LogDebug("Initialise");
            await lexiconDownloader.Download(item => item.Model, environment.ContentRootPath)
                                   .ConfigureAwait(false);
            await lexiconDownloader.Download(item => item.Lexicons, environment.ContentRootPath, true)
                                   .ConfigureAwait(false);
            loader.Load();

            using (var scope = provider.CreateScope())
            {
                var container = scope.ServiceProvider.GetService<ISessionContainer>();
                var client = container.GetTesting(null);
                client.Init();
            }
        }
    }
}
