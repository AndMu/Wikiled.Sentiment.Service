﻿using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Nito.AsyncEx;
using Wikiled.Common.Arguments;
using Wikiled.Sentiment.Analysis.Processing;
using Wikiled.Sentiment.Analysis.Processing.Pipeline;
using Wikiled.Sentiment.Service.Logic;
using Wikiled.Sentiment.Service.Logic.Request;
using Wikiled.Sentiment.Text.Data.Review;
using Wikiled.Sentiment.Text.Parser;
using Wikiled.Sentiment.Text.Sentiment;
using Wikiled.Server.Core.ActionFilters;
using Wikiled.Text.Analysis.Structure;

namespace Wikiled.Sentiment.Service.Controllers
{
    [Route("api/[controller]")]
    [TypeFilter(typeof(RequestValidationAttribute))]
    public class SentimentController : Controller
    {
        private readonly ILogger<SentimentController> logger;

        private readonly IReviewSink reviewSink;

        private TestingClient client;

        public SentimentController(ILogger<SentimentController> logger, IReviewSink reviewSink, TestingClient client)
        {
            Guard.NotNull(() => reviewSink, reviewSink);
            this.reviewSink = reviewSink;
            this.client = client;
            this.logger = logger;
        }

        [Route("parse")]
        [HttpPost]
        public async Task<Document> Parse([FromBody]SingleProcessingData review)
        {
            review.Id = Guid.NewGuid().ToString();
            var result = reviewSink.ParsedReviews
                .Select(item => item.Processed).Where(item => item.Id == review.Id)
                .FirstOrDefaultAsync().GetAwaiter();
            reviewSink.AddReview(review);
            var document = await result;
            return document;
        }

        [HttpPost]
        [Route("parsestream")]
        public async Task GetStream([FromBody] WorkRequest request)
        {
            Response.ContentType = "application/json";

            try
            {
                ISentimentDataHolder loader = default;
                if (request.Dictionary != null)
                {
                    foreach (var item in request.Dictionary)
                    {
                        loader = SentimentDataHolder.Load(new[] { new WordSentimentValueData(item.Key, new SentimentValueData(item.Value)) });
                    }
                }

                Dictionary<string, SingleProcessingData> documentTable = new Dictionary<string, SingleProcessingData>();

                foreach (var document in request.Documents)
                {
                    if (document.Id == null ||
                        documentTable.ContainsKey(document.Id))
                    {
                        document.Id = Guid.NewGuid().ToString();
                    }

                    documentTable[document.Id] = document;
                }

                var data = reviewSink.ParsedReviews.Where(item => documentTable.ContainsKey(item.Processed.Id));
                AsyncCountdownEvent count = new AsyncCountdownEvent(request.Documents.Length);
                var task = ProcessList(data, loader, count);

                StringBuilder response = new StringBuilder();
                response.AppendLine("{");
                response.AppendLine($"\"Total\": {request.Documents.Length},");
                response.AppendLine("\"Documents\": [");
                var buffer = Encoding.UTF8.GetBytes(response.ToString());
                Response.Body.Write(buffer, 0, buffer.Length);

                foreach (var document in request.Documents)
                {
                    reviewSink.AddReview(document);
                }

                await Task.WhenAny(task, count.WaitAsync());

                response = new StringBuilder();
                response.AppendLine("]");
                response.AppendLine("}");
                buffer = Encoding.UTF8.GetBytes(response.ToString());
                Response.Body.Write(buffer, 0, buffer.Length);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed");
            }
        }

        private async Task ProcessList(IObservable<ProcessingContext> data, ISentimentDataHolder loader, AsyncCountdownEvent count)
        {
            var result = data.Select(
                item =>
                {
                    if (loader != default)
                    {
                        LexiconRatingAdjustment adjustment = new LexiconRatingAdjustment(item.Review, loader);
                        adjustment.CalculateRating();
                        return item.Review.GenerateDocument(adjustment);
                    }

                    return item.Processed;
                });

            await result.Select(
                            item =>
                            {
                                var str = JsonConvert.SerializeObject(item);
                                var buffer = Encoding.UTF8.GetBytes(str);
                                lock (Response.Body)
                                {
                                    Response.Body.Write(buffer, 0, buffer.Length);
                                    byte[] newline = Encoding.UTF8.GetBytes(count.CurrentCount > 1 ? "," : string.Empty + Environment.NewLine);
                                    Response.Body.Write(newline, 0, newline.Length);
                                }

                                count.Signal();
                                return item;
                            })
                        .LastOrDefaultAsync();
        }

        [Route("version")]
        [HttpGet]

        public string ServerVersion()
        {
            var version = $"Version: [{Assembly.GetExecutingAssembly().GetName().Version}]]";
            logger.LogInformation("Version request: {0}", version);
            return version;
        }
    }
}
