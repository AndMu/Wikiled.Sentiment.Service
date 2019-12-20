﻿using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;
using Wikiled.Common.Logging;
using Wikiled.Common.Utilities.Modules;
using Wikiled.Common.Utilities.Resources;
using Wikiled.Sentiment.Analysis.Containers;
using Wikiled.Sentiment.Api.Request.Messages;
using Wikiled.Sentiment.Service.Logic;
using Wikiled.Sentiment.Service.Logic.Storage;
using Wikiled.Sentiment.Service.Services;
using Wikiled.Sentiment.Service.Services.Controllers;
using Wikiled.Sentiment.Text.MachineLearning;
using Wikiled.Sentiment.Text.Parser;
using Wikiled.Sentiment.Text.Resources;
using Wikiled.Server.Core.Errors;
using Wikiled.Server.Core.Helpers;
using Wikiled.Server.Core.Middleware;
using Wikiled.WebSockets.Definitions.Messages;
using Wikiled.WebSockets.Server.MiddleTier;
using Wikiled.WebSockets.Server.Processing;
using Wikiled.WebSockets.Server.Protocol.Configuration;

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

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
            });


            app.UseWebSockets(new WebSocketOptions { ReceiveBufferSize = 1024 * 1024 * 2 });

            app.Map("/stream", ws =>
                {
                    ws.UseWebSockets();
                    ws.UseMiddleware<WebSocketMiddleware2>();
                    app.UseExceptionHandler(builder => builder.Run(JsonExceptionHandler));
                }
            );

            // pre-warm
            provider.GetService<LexiconLoader>();
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
                        builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
                });

            // Add framework services.
            services.AddControllers();

            // needed to load configuration from appsettings.json
            services.AddOptions();

            // Create the container builder.
            SetupTestClient(services);
            SetupOther(services);
            SetupControllers(services);
        }

        private void SetupControllers(IServiceCollection services)
        {
            services.RegisterModule<SocketModule>();
            services.AddSingleton<IController, SentimentAnalysisController>();
            services.AddSingleton<IController, SentimentTrainingController>();
            services.RegisterConfiguration<ServiceSettings>(Configuration.GetSection("ServiceSettings"));
            services.AddSingleton<Message, SentimentMessage>();
            services.AddSingleton<Message, TrainMessage>();
            services.AddSingleton<Message, CompletedMessage>();
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
            ParallelHelper.Options = new ParallelOptions();
            ParallelHelper.Options.MaxDegreeOfParallelism = Environment.ProcessorCount > 8 ? Environment.ProcessorCount / 2 : Environment.ProcessorCount;

            builder.RegisterModule(new SentimentMainModule());
            builder.RegisterModule(new SentimentServiceModule(configuration) { Lexicons = path });

            builder.AddSingleton<IDocumentStorage, SimpleDocumentStorage>();
            builder.AddScoped<IDocumentConverter, DocumentConverter>();
            builder.AddHostedService<WarmupService>();
        }

        private static Task JsonExceptionHandler(HttpContext context)
        {
            var exception = context.Features.Get<IExceptionHandlerFeature>();
            var result = new JObject(new JProperty("error", exception.Error.Message));

            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";
            return context.Response.WriteAsync(result.ToString());
        }
    }
}
