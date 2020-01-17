using System;
using System.Threading;
using System.Threading.Tasks;
using Wikiled.Common.Net.Client;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Text.Analysis.Structure;

namespace Wikiled.Sentiment.Api.Service
{
    internal class BasicSentimentAnalysis : IBasicSentimentAnalysis
    {
        private readonly IApiClient client;

        public BasicSentimentAnalysis(IApiClientFactory factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            client = factory.GetClient();
        }

        public async Task<Document> Measure(SingleWorkRequest request, CancellationToken token)
        {
            var result = await client.PostRequest<SingleWorkRequest, RawResponse<Document>>("api/sentiment/parse", request, token).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                throw new ApplicationException("Failed to retrieve:" + result.HttpResponseMessage);
            }

            return result.Result.Value;
        }

        public async Task<RatingValue> Calculate(SingleWorkRequest request, CancellationToken token)
        {
            var result = await client.PostRequest<SingleWorkRequest, RawResponse<RatingValue>>("api/sentiment/calculate", request, token).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                throw new ApplicationException("Failed to retrieve:" + result.HttpResponseMessage);
            }

            return result.Result.Value;
        }
    }
}
