using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Reactive.Concurrency;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MQTTnet.AspNetCore;
using MQTTnet.Protocol;
using Wikiled.Common.Logging;
using Wikiled.Common.Utilities.Modules;
using Wikiled.Common.Utilities.Resources;
using Wikiled.Sentiment.Analysis.Containers;
using Wikiled.Sentiment.Analysis.Processing;
using Wikiled.Sentiment.Service.Logic;
using Wikiled.Sentiment.Service.Logic.Allocation;
using Wikiled.Sentiment.Service.Logic.Notifications;
using Wikiled.Sentiment.Service.Logic.Storage;
using Wikiled.Sentiment.Service.Services;
using Wikiled.Sentiment.Service.Services.Topics;
using Wikiled.Sentiment.Text.MachineLearning;
using Wikiled.Sentiment.Text.Resources;
using Wikiled.Server.Core.Errors;
using Wikiled.Server.Core.Helpers;
using Wikiled.Server.Core.Middleware;
using Wikiled.Text.Analysis.Cache;

namespace Wikiled.Sentiment.Service
{
    public class Startup
    {
        private readonly ILogger<Startup> logger;

        private readonly ILoggerFactory loggerFactory;

        public Startup(ILoggerFactory loggerFactory, IWebHostEnvironment env)
        {
            ApplicationLogging.LoggerFactory = loggerFactory;
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
            this.loggerFactory = loggerFactory;
            Env = env;
            logger = loggerFactory.CreateLogger<Startup>();
            Configuration.ChangeNlog();
            logger.LogInformation($"Starting: {Assembly.GetExecutingAssembly().GetName().Version}");
        }

        public IConfigurationRoot Configuration { get; }

        public IWebHostEnvironment Env { get; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider provider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
       
            app.UseCors("CorsPolicy");

            app.UseRouting();
            app.UseRequestLogging();
            app.UseExceptionHandlingMiddleware();
            app.UseHttpStatusCodeExceptionMiddleware();
            app.UseMqttEndpoint();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });

            app.UseMqttServer(server => app.ApplicationServices.GetRequiredService<SentimentService>().ConfigureMqttServer(server));
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Needed to add this section, and....
            services.AddCors(
                options =>
                {
                    options.AddPolicy(
                        "CorsPolicy",
                        itemBuider => itemBuider.AllowAnyOrigin()
                                                .AllowAnyMethod()
                                                .AllowAnyHeader());
                });

            // Add framework services.
            services.AddControllers();

            // needed to load configuration from appsettings.json
            services.AddOptions();

            // Create the container builder.
            SetupTestClient(services);
            SetupOther(services);
            ConfigureMqttServices(services);
        }

        private void ConfigureMqttServices(IServiceCollection services)
        {
            //this adds a hosted mqtt server to the services
            services.AddHostedMqttServer(
                builder =>
                {
                    builder.WithDefaultEndpointPort(1883);
                    builder.WithConnectionValidator(
                        c =>
                        {
                            if (c.ClientId.Length < 10)
                            {
                                c.ReasonCode = MqttConnectReasonCode.ClientIdentifierNotValid;
                                return;
                            }

                            c.ReasonCode = MqttConnectReasonCode.Success;
                        });
                });

            //this adds tcp server support based on System.Net.Socket
            services.AddMqttTcpServerAdapter();

            //this adds websocket support
            services.AddMqttWebSocketServerAdapter();
        }

        private static void SetupOther(IServiceCollection builder)
        {
            builder.AddTransient<IIpResolve, IpResolve>();
        }

        private void SetupTestClient(IServiceCollection builder)
        {
            var configuration = new ConfigurationHandler();
            configuration.SetConfiguration("resources", "resources");
            configuration.SetConfiguration("lexicons", "lexicons");
            configuration.StartingLocation = Env.ContentRootPath;
            var resourcesPath = configuration.ResolvePath("Resources");

            var url = Configuration["sentiment:resources"];
            var urlLexicons = Configuration["sentiment:lexicons"];
            var path = configuration.ResolvePath("lexicons");

            var dataDownloader = new DataDownloader(loggerFactory);
            Task.WhenAll(
                    dataDownloader.DownloadFile(new Uri(url), resourcesPath),
                    dataDownloader.DownloadFile(new Uri(urlLexicons), path, true))
                .Wait();

            logger.LogInformation("Adding Lexicons...");
            builder.RegisterModule<CommonModule>();
            SetupSentiment(builder, configuration, path);
        }

        private static void SetupSentiment(IServiceCollection builder, ConfigurationHandler configuration, string path)
        {
            Classifier.Options = new ParallelOptions();
            Classifier.Options.MaxDegreeOfParallelism = 2;
            builder.RegisterModule(new SentimentMainModule());
            builder.RegisterModule(new SentimentServiceModule(configuration) {Lexicons = path});

            builder.AddSingleton<INotificationsHandler, NotificationsHandler>();
            builder.AddSingleton<IResourcesHandler, ResourcesHandler>();

            builder.AddSingleton<IDocumentStorage, SimpleDocumentStorage>();
            builder.AddScoped<IDocumentConverter, DocumentConverter>();

            builder.AddSingleton<ITopicProcessing, SentimentAnalysisTopic>();
            builder.AddSingleton<ITopicProcessing, DocumentSaveTopic>();
            builder.AddSingleton<ITopicProcessing, SentimentTrainingTopic>();

            builder.AddSingleton<SentimentService>();
        }
    }
}
