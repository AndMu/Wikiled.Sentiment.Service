using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Wikiled.Common.Net.Client;
using Wikiled.MachineLearning.Mathematics;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Text.Analysis.Structure;

namespace Wikiled.Sentiment.Api.Service
{
    public class SentimentAnalysis : ISentimentAnalysis
    {
        private readonly IStreamApiClient client;

        private readonly WorkRequest request;

        private readonly ILogger<SentimentAnalysis> logger;

        public SentimentAnalysis(ILogger<SentimentAnalysis> logger, IStreamApiClientFactory factory, WorkRequest request)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            this.request = request ?? throw new ArgumentNullException(nameof(request));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            client = factory.Construct();
        }

        public Task<Document> Measure(string text, CancellationToken token)
        {
            return Measure(new SingleRequestData {Text = text}, token);
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
            return Measure(items.Select(item => new SingleRequestData {Id = item.Id, Text = item.Text}).ToArray(), token)
                .Select(
                    item =>
                    {
                        var rating = item.Stars.HasValue ? RatingCalculator.ConvertToRaw(item.Stars.Value) : null;
                        return (item.Id, rating);
                    });
        }

        public async Task<Document> Measure(SingleRequestData document, CancellationToken token)
        {
            return await Measure(new [] {document}, token).FirstOrDefaultAsync();
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

            var current = (WorkRequest)request.Clone();
            current.Documents = documents;
            return client.PostRequest<WorkRequest, Document>("api/sentiment/parsestream", current, token);
        }
    }
}
