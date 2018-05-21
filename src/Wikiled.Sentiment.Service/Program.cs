﻿using System.IO;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Web;

namespace Wikiled.Sentiment.Service
{
    public class Program
    {
        public static void Main(string[] args)
        {
            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                   .UseStartup<Startup>()
                   .ConfigureLogging(
                       logging =>
                       {
                           logging.ClearProviders();
                           logging.SetMinimumLevel(LogLevel.Trace);
                       })
                   .UseNLog() // NLog: setup NLog for Dependency injection
                   .Build();
    }
}
