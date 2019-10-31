using System;
using System.Globalization;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wikiled.Common.Extensions;
using Wikiled.Common.Utilities.Serialization;
using Wikiled.Sentiment.Api.Request;

namespace Wikiled.Sentiment.Service.Logic.Storage
{
    public class SimpleDocumentStorage : IDocumentStorage
    {
        private readonly IJsonSerializer serializer;

        private readonly ILogger<SimpleDocumentStorage> logger;

        public SimpleDocumentStorage(ILogger<SimpleDocumentStorage> logger, IJsonSerializer serializer)
        {
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task Save(string client, SaveRequest request)
        {
            if (client == null)
            {
                throw new ArgumentNullException(nameof(client));
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var directory = GetLocation(client, request.Name);
            directory.EnsureDirectoryExistence();
            var output = Path.Combine(directory, $"{DateTime.UtcNow.ToString("yyyyMMddHHhhmmssffff", CultureInfo.InvariantCulture)}.zip");
            await serializer.SerializeJsonZip(request.Documents, output).ConfigureAwait(false);
        }

        public string GetLocation(string client, string name, string type = "documents")
        {
            var directory = Path.Combine("Storage", client, name, type);
            return directory;
        }

        public IObservable<SingleRequestData> Load(string client, string name)
        {
            return Observable.Create<SingleRequestData>(
                          (observer) =>
                          {
                              ReadFiles(client, name, observer);
                              return Disposable.Empty;
                          })
                      .Distinct(item => item.Id);
        }

        private void ReadFiles(string client, string name, IObserver<SingleRequestData> observer)
        {
            try
            {
                var files = Directory.GetFiles(GetLocation(client, name), ".zip");
                foreach (var file in files)
                {
                    var result = serializer.DeserializeJsonZip<SingleRequestData[]>(file);
                    foreach (var requestData in result)
                    {
                        observer.OnNext(requestData);
                    }
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Failed");
            }

            observer.OnCompleted();
        }
    }
}
