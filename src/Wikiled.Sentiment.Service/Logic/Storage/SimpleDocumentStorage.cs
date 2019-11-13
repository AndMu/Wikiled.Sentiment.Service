﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Wikiled.Common.Extensions;
using Wikiled.Common.Utilities.Serialization;
using Wikiled.Sentiment.Api.Request;

namespace Wikiled.Sentiment.Service.Logic.Storage
{
    public class SimpleDocumentStorage : IDocumentStorage
    {
        private readonly IJsonSerializer serializer;

        private readonly ILogger<SimpleDocumentStorage> logger;

        private IHostEnvironment env;

        public SimpleDocumentStorage(ILogger<SimpleDocumentStorage> logger, IJsonSerializer serializer, IHostEnvironment env)
        {
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.env = env ?? throw new ArgumentNullException(nameof(env));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task Save(SaveRequest request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var directoryPositive = GetDocumentClassFolder(request.User, request.Name, true);
            directoryPositive.EnsureDirectoryExistence();

            var directoryNegative = GetDocumentClassFolder(request.User, request.Name, false);
            directoryNegative.EnsureDirectoryExistence();
            var tasks = new List<Task>();
            foreach (var document in request.Documents)
            {
                if (!document.IsPositive.HasValue)
                {
                    logger.LogWarning("Can't save document <{0}> without class id", document.Id);
                    continue;
                }

                if (string.IsNullOrEmpty(document.Id))
                {
                    document.Id = Guid.NewGuid().ToString();
                    logger.LogWarning("Document doesn't have id - generating");
                }

                var directory = document.IsPositive.Value ? directoryPositive : directoryNegative;

                var output = Path.Combine(directory, $"{document.Id}.zip");
                var task = serializer.SerializeJsonZip(document, output);
                tasks.Add(task);
            }

            return Task.WhenAll(tasks);
        }

        public int Count(string client, string name)
        {
            var files = Directory.GetFiles(GetLocation(client, name), ".zip", SearchOption.AllDirectories);
            return files.Length;
        }

        public string GetLocation(string client, string name, string type = "documents")
        {
            var directory = Path.Combine(env.ContentRootPath, "Storage", client, name, type);
            return directory;
        }

        public IObservable<SingleRequestData> Load(string client, string name, bool classType)
        {
            return Observable.Create<SingleRequestData>(
                          (observer) =>
                          {
                              ReadFiles(client, name, classType, observer);
                              return Disposable.Empty;
                          });
        }

        private void ReadFiles(string client, string name, bool classType, IObserver<SingleRequestData> observer)
        {
            try
            {
                var files = Directory.GetFiles(GetDocumentClassFolder(client, name, classType), "*.zip");
                foreach (var file in files)
                {
                    var result = serializer.DeserializeJsonZip<SingleRequestData>(file);
                    observer.OnNext(result);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed");
            }

            observer.OnCompleted();
        }

        private string GetDocumentClassFolder(string user, string name, bool classType)
        {
            return Path.Combine(GetLocation(user, name), classType ? "pos" : "neg");
        }
    }
}
