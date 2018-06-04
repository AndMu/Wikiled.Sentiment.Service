using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Reflection;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog;
using Wikiled.Sentiment.Analysis.Processing;
using Wikiled.Sentiment.Analysis.Processing.Pipeline;
using Wikiled.Sentiment.Analysis.Processing.Splitters;
using Wikiled.Sentiment.Service.Logic;
using Wikiled.Sentiment.Text.NLP;
using Wikiled.Sentiment.Text.Resources;
using Wikiled.Text.Analysis.Cache;
using Wikiled.Text.Analysis.POS;

namespace Wikiled.Sentiment.Service
{
    public class Startup
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
            Env = env;
            logger.Debug($"Starting: {Assembly.GetExecutingAssembly().GetName().Version}");
        }

        public IConfigurationRoot Configuration { get; }

        public IHostingEnvironment Env { get; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            app.UseCors("CorsPolicy");
            
            // add NLog.Web
            app.UseMvc();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            // Needed to add this section, and....
            services.AddCors(
                options =>
                {
                    options.AddPolicy(
                        "CorsPolicy",
                        itemBuider => itemBuider.AllowAnyOrigin()
                                                .AllowAnyMethod()
                                                .AllowAnyHeader()
                                                .AllowCredentials());
                });

            // Add framework services.
            services.AddMvc(
                options => { });

            // needed to load configuration from appsettings.json
            services.AddOptions();

            // Create the container builder.
            var builder = new ContainerBuilder();
            SetupTestClient(builder);
            builder.Populate(services);
            var appContainer = builder.Build();

            logger.Info("Ready!");
            // Create the IServiceProvider based on the container.
            return new AutofacServiceProvider(appContainer);
        }

        private void SetupTestClient(ContainerBuilder builder)
        {
            var configuration = new ConfigurationHandler();
            configuration.SetConfiguration("resources", "resources");
            configuration.StartingLocation = Env.ContentRootPath;
            var resourcesPath = configuration.ResolvePath("Resources");
            var url = Configuration["sentiment:resources"];
            if (Directory.Exists(resourcesPath))
            {
                logger.Info("Resources folder {0} found.", resourcesPath);
            }
            else
            {
                DataDownloader dataDownloader = new DataDownloader();
                var task = dataDownloader.DownloadFile(new Uri(url), resourcesPath);
                task.Wait();
            }

            logger.Info("Loading splitter...");
            var cache = new MemoryCache(new MemoryCacheOptions());
            var splitterHelper = new MainSplitterFactory(new LocalCacheFactory(cache), configuration).Create(POSTaggerType.SharpNLP);
            ReviewSink sink = new ReviewSink(splitterHelper.Splitter);
            ProcessingPipeline pipeline = new ProcessingPipeline(
                TaskPoolScheduler.Default, 
                splitterHelper, 
                sink.Reviews,
                new ParsedReviewManagerFactory());
            TestingClient client = new TestingClient(pipeline);
            client.TrackArff = false;

            logger.Info("Initializing testing client...");
            client.Init();
            sink.ParsedReviews = client.Process();
            builder.RegisterInstance(client);
            builder.RegisterInstance(sink)
                   .As<IReviewSink>()
                   .SingleInstance();
        }
    }
}
