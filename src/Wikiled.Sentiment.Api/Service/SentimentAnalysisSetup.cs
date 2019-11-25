using Microsoft.Extensions.Logging;
using System;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Sentiment.Api.Service.Flow;

namespace Wikiled.Sentiment.Api.Service
{
    public class SentimentAnalysisSetup : ISentimentAnalysisSetup
    {
        private readonly ILoggerFactory loggerFactory;

        private readonly ISentimentFlow flow;


        public SentimentAnalysisSetup(ILoggerFactory loggerFactory, ISentimentFlow flow)
        {
            this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
            this.flow = flow ?? throw new ArgumentNullException(nameof(flow));
        }

        public ISentimentAnalysis Setup(WorkRequest request)
        {
            return new SentimentAnalysis(loggerFactory.CreateLogger< SentimentAnalysis>(), request, flow);
        }
    }
}
