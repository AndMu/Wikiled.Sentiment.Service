using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Wikiled.Common.Utilities.Helpers;
using Wikiled.MachineLearning.Mathematics;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Sentiment.Api.Request.Messages;
using Wikiled.Text.Analysis.Structure;
using Wikiled.WebSockets.Client.Definition;
using Wikiled.WebSockets.Definitions.Messages;
using Wikiled.WebSockets.Definitions.Protocol.Definitions;

namespace Wikiled.Sentiment.Api.Service
{
    public sealed class SentimentAnalysis : ISentimentAnalysis
    {
        private readonly ILogger<SentimentAnalysis> logger;

        private readonly IClient client;

        private readonly IDisposable subscription;

        private readonly IDisposable stateSubscription;

        private readonly Subject<bool> completed = new Subject<bool>();

        private readonly IDataSubscription<Document> dataSubscription;

        public SentimentAnalysis(ILoggerFactory loggerFactory, IClient client, IDataSubscription<Document> dataSubscription)
        {
            logger = loggerFactory?.CreateLogger<SentimentAnalysis>() ?? throw new ArgumentNullException(nameof(logger));
            this.client = client ?? throw new ArgumentNullException(nameof(client));
            this.dataSubscription = dataSubscription ?? throw new ArgumentNullException(nameof(dataSubscription));
            subscription = client.Messages.Subscribe(ProcessMessage);
            stateSubscription = client.State.Where(item => item == ConnectionState.Error).Subscribe(item => completed.OnError(new Exception("Connection error")));
        }

        public Task<Document> Measure(string text, CancellationToken token)
        {
            return Measure(new SingleRequestData { Text = text }, token);
        }

        public async Task<double?> Measure(string text)
        {
            logger.LogDebug("Measure");
            try
            {
                var result = await Measure(text, CancellationToken.None).ConfigureAwait(false);
                if (result == null)
                {
                    logger.LogWarning("No meaningful response");
                    return null;
                }

                logger.LogDebug("MeasureSentiment Calculated: {0}", result.Stars);
                return result.Stars.HasValue ? RatingCalculator.ConvertToRaw(result.Stars.Value) : null;

            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed sentiment processing");
                return null;
            }
        }

        public IObservable<(string, double?)> Measure((string Id, string Text)[] items, CancellationToken token)
        {
            return Measure(items.Select(item => new SingleRequestData { Id = item.Id, Text = item.Text }).ToArray(), token)
                .Select(
                    item =>
                    {
                        var rating = item.Stars.HasValue ? RatingCalculator.ConvertToRaw(item.Stars.Value) : null;
                        return (item.Id, rating);
                    });
        }

        public WorkRequest Settings { get; } = new WorkRequest();

        public Task Connect(Uri uri)
        {
            return client.Connect(uri);
        }

        public async Task<Document> Measure(SingleRequestData document, CancellationToken token)
        {
            return await Measure(new[] { document }, token).FirstOrDefaultAsync();
        }

        public IObservable<Document> Measure(SingleRequestData[] documents, CancellationToken token)
        {
            if (documents is null)
            {
                throw new ArgumentNullException(nameof(documents));
            }

            if (documents.Length == 0)
            {
                throw new ArgumentException("Value cannot be an empty collection.", nameof(documents));
            }

            var current = (WorkRequest)Settings.Clone();
            current.Documents = documents;
            
            client.Send(new SentimentMessage { Request = current }, CancellationToken.None).ForgetOrThrow(logger);
            return dataSubscription.Subscribe().TakeUntil(completed);
        }

        public void Dispose()
        {
            stateSubscription?.Dispose();
            subscription?.Dispose();
            client?.Dispose();
            completed?.Dispose();
        }

        private void ProcessMessage(Message message)
        {
            switch (message)
            {
                case CompletedMessage completedMessage:
                    logger.LogInformation("Processing completed");
                    if (completedMessage.IsError)
                    {
                        logger.LogError("Processing error: {0}", completedMessage.Message);
                        completed.OnError(new Exception("Connection error"));
                    }
                    else
                    {
                        logger.LogInformation("Processing completed: {0}", completedMessage.Message);
                        completed.OnNext(true);
                    }
                    
                    break;
            }
        }
    }
}
