using System;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using MQTTnet.AspNetCore;
using NLog.Web;

namespace Wikiled.Sentiment.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // NLog: setup the logger first to catch all errors
            var logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
            try
            {
                BuildWebHost(args).Run();
            }
            catch (Exception ex)
            {
                //NLog: catch setup errors
                logger.Error(ex, "Stopped program because of exception");
                throw;
            }
            finally
            {
                // Ensure to flush and stop internal timers/threads before application-exit (Avoid segmentation fault on Linux)
                NLog.LogManager.Shutdown();
            }
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseKestrel(o => {
                    o.ListenAnyIP(1883, l => l.UseMqtt()); // mqtt pipeline
                    o.ListenAnyIP(5000); // default http pipeline
                })
                .ConfigureLogging(
                    logging =>
                    {
                        logging.ClearProviders();
                        logging.SetMinimumLevel(LogLevel.Trace);
                    })
                .UseNLog()
                .Build();
    }
}
