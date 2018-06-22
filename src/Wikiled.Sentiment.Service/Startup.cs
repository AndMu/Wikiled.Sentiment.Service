using System;
using System.IO;
using System.Reactive.Concurrency;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Wikiled.Sentiment.Analysis.Processing;
using Wikiled.Sentiment.Analysis.Processing.Pipeline;
using Wikiled.Sentiment.Analysis.Processing.Splitters;
using Wikiled.Sentiment.Service.Hubs;
using Wikiled.Sentiment.Service.Logic;
using Wikiled.Sentiment.Text.NLP;
using Wikiled.Sentiment.Text.Resources;
using Wikiled.Server.Core.Errors;
using Wikiled.Server.Core.Helpers;
using Wikiled.Text.Analysis.Cache;
using Wikiled.Text.Analysis.POS;

namespace Wikiled.Sentiment.Service
{
    public class Startup
    {
        private readonly ILogger<Startup> logger;

        public Startup(ILoggerFactory loggerFactory, IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
            Env = env;
            logger = loggerFactory.CreateLogger<Startup>();
            Configuration.ChangeNlog();
            logger.LogInformation($"Starting: {Assembly.GetExecutingAssembly().GetName().Version}");
        }

        public IConfigurationRoot Configuration { get; }

        public IHostingEnvironment Env { get; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseCors("CorsPolicy");
            app.UseSignalR((options) =>
            {
                options.MapHub<SentimentHub>("/Sentiment");
            });

            app.UseHttpsRedirection();
            app.UseExceptionHandlingMiddleware();
            app.UseHttpStatusCodeExceptionMiddleware();
            app.UseMvc();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddSignalR();
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
            services.AddMvc(options => { });

            // needed to load configuration from appsettings.json
            services.AddOptions();

            // Create the container builder.
            var builder = new ContainerBuilder();
            SetupTestClient(builder);
            SetupOther(builder);
            builder.Populate(services);
            var appContainer = builder.Build();

            logger.LogInformation("Ready!");
            // Create the IServiceProvider based on the container.
            return new AutofacServiceProvider(appContainer);
        }

        private void SetupOther(ContainerBuilder builder)
        {
            builder.RegisterType<IpResolve>().As<IIpResolve>();
        }

        private void SetupTestClient(ContainerBuilder builder)
        {
            var configuration = new ConfigurationHandler();
            configuration.SetConfiguration("resources", "resources");
            configuration.SetConfiguration("lexicons", "lexicons");
            configuration.StartingLocation = Env.ContentRootPath;
            var resourcesPath = configuration.ResolvePath("Resources");

            var url = Configuration["sentiment:resources"];
            var urlLexicons = Configuration["sentiment:lexicons"];
            var path = configuration.ResolvePath("lexicons");
            Task.WhenAll(
                DownloadResources(resourcesPath, url),
                Downloadlexicons(path, urlLexicons))
                .Wait();

            LexiconLoader loader = new LexiconLoader();
            loader.Load(path);
            builder.RegisterInstance(loader).As<ILexiconLoader>();

            logger.LogInformation("Loading splitter...");
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

            logger.LogInformation("Initializing testing client...");
            client.Init();
            sink.ParsedReviews = client.Process();
            builder.RegisterInstance(client);
            builder.RegisterInstance(sink)
                   .As<IReviewSink>()
                   .SingleInstance();
        }

        private Task DownloadResources(string resourcesPath, string url)
        {
            logger.LogInformation("Setup resources - {0} from {1}", resourcesPath, url);
            if (Directory.Exists(resourcesPath))
            {
                logger.LogInformation("Resources folder {0} found.", resourcesPath);
                return Task.CompletedTask;
            }

            DataDownloader dataDownloader = new DataDownloader();
            return dataDownloader.DownloadFile(new Uri(url), resourcesPath);
        }

        private Task Downloadlexicons(string path, string url)
        {
            logger.LogInformation("Setup lexicons - {0} from {1}", path, url);
            if (Directory.Exists(path))
            {
                logger.LogInformation("Removing old lexicon folder - {0}", path);
                Directory.Delete(path);
            }

            DataDownloader dataDownloader = new DataDownloader();
            return dataDownloader.DownloadFile(new Uri(url), path);
        }
    }
}
