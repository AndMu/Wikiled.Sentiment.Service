using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Wikiled.Common.Logging;
using Wikiled.Common.Utilities.Config;
using Wikiled.Common.Utilities.Resources;
using Wikiled.Sentiment.Analysis.Containers;
using Wikiled.Sentiment.Service.Hubs;
using Wikiled.Sentiment.Service.Logic;
using Wikiled.Sentiment.Text.Resources;
using Wikiled.Server.Core.Errors;
using Wikiled.Server.Core.Helpers;
using Wikiled.Server.Core.Middleware;

namespace Wikiled.Sentiment.Service
{
    public class Startup
    {
        private readonly ILogger<Startup> logger;

        private readonly ILoggerFactory loggerFactory;

        public Startup(ILoggerFactory loggerFactory, IHostingEnvironment env)
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

        public IHostingEnvironment Env { get; }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
       
            app.UseCors("CorsPolicy");
            app.UseSignalR((options) =>
            {
                options.MapHub<SentimentHub>("/Sentiment");
            });

            //app.UseHttpsRedirection();
            app.UseRequestLogging();
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
            IContainer appContainer = builder.Build();
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

            var dataDownloader = new DataDownloader(loggerFactory);
            Task.WhenAll(
                    dataDownloader.DownloadFile(new Uri(url), resourcesPath),
                    dataDownloader.DownloadFile(new Uri(urlLexicons), path, true))
                .Wait();

            logger.LogInformation("Adding Lexicons...");
            builder.RegisterModule(new SentimentMainModule());
            builder.RegisterModule(new SentimentServiceModule(configuration) { Lexicons = path });
            builder.RegisterType<ReviewSink>().As<IReviewSink>();
        }
    }
}
