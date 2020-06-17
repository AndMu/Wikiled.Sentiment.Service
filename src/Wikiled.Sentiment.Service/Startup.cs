using Microsoft.AspNetCore.Builder;
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
using Wikiled.Sentiment.Analysis.Containers;
using Wikiled.Sentiment.Api.Request.Messages;
using Wikiled.Sentiment.Service.Logic;
using Wikiled.Sentiment.Service.Logic.Storage;
using Wikiled.Sentiment.Service.Services;
using Wikiled.Sentiment.Service.Services.Controllers;
using Wikiled.Sentiment.Text.Config;
using Wikiled.Sentiment.Text.MachineLearning;
using Wikiled.Sentiment.Text.Parser;
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

        public Startup(ILoggerFactory loggerFactory, IWebHostEnvironment env)
        {
            ApplicationLogging.LoggerFactory = loggerFactory;
            IConfigurationBuilder builder = new ConfigurationBuilder()
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
                    ws.UseMiddleware<WebSocketMiddleware>();
                    app.UseExceptionHandler(builder => builder.Run(JsonExceptionHandler));
                }
            );

            Initialise(provider);
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
            SetupSentiment(services);
            SetupOther(services);
            SetupControllers(services);
        }

        private void SetupControllers(IServiceCollection services)
        {
            services.RegisterModule<CommonModule>();
            services.RegisterModule<SocketModule>();
            services.AddSingleton<IController, SentimentAnalysisController>();
            services.AddSingleton<IController, SentimentTrainingController>();
            services.RegisterConfiguration<ServiceSettings>(Configuration.GetSection("ServiceSettings"));
            services.RegisterConfiguration<ILexiconConfig>(Configuration.GetSection("lexicon"), new LexiconConfig());
            services.AddSingleton<Message, SentimentMessage>();
            services.AddSingleton<Message, TrainMessage>();
            services.AddSingleton<Message, CompletedMessage>();
        }

        private static void SetupOther(IServiceCollection builder)
        {
            builder.AddTransient<IIpResolve, IpResolve>();
        }

        private void Initialise(IServiceProvider provider)
        {
            // pre-warm
            provider.GetService<LexiconLoader>();
        }

        private void SetupSentiment(IServiceCollection builder)
        {
            ParallelHelper.Options = new ParallelOptions();
            ParallelHelper.Options.MaxDegreeOfParallelism = Math.Max(1, Math.Min(Environment.ProcessorCount / 2, 4));

            builder.RegisterModule<SentimentMainModule>();
            builder.RegisterModule<SentimentServiceModule>();

            builder.AddSingleton<IDocumentStorage, SimpleDocumentStorage>();
            builder.AddScoped<IDocumentConverter, DocumentConverter>();
            builder.AddAsyncInitializer<InitialisationService>();
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
