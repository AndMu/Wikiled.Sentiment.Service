using System;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wikiled.Common.Net.Client;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Text.Analysis.Structure;

namespace Wikiled.Sentiment.Api.Service
{
    public class SentimentAnalysis : ISentimentAnalysis
    {
        private readonly IStreamApiClient client;

        private readonly WorkRequest request;

        public SentimentAnalysis(IStreamApiClientFactory factory, WorkRequest request)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            this.request = request ?? throw new ArgumentNullException(nameof(request));
            client = factory.Contruct();
        }

        public Task<Document> Measure(string text, CancellationToken token)
        {
            return Measure(new SingleRequestData {Text = text}, token);
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
