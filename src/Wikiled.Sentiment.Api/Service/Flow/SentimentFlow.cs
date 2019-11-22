﻿using Microsoft.Extensions.Logging;
using MQTTnet;
using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wikiled.Common.Utilities.Serialization;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Sentiment.Api.Service.Mqtt;
using Wikiled.Text.Analysis.Structure;

namespace Wikiled.Sentiment.Api.Service.Flow
{
    public class SentimentFlow
    {
        private readonly ILogger<SentimentFlow> logger;

        private readonly Func<IMqttConnection> connectionFactory;

        private readonly MqttConnectionInfo connectionInfo;

        private readonly IScheduler scheduler;

        private readonly IJsonSerializer serializer;

        public SentimentFlow(ILogger<SentimentFlow> logger, IScheduler scheduler, Func<IMqttConnection> connection, MqttConnectionInfo connectionInfo, IJsonSerializer serializer)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.connectionFactory = connection ?? throw new ArgumentNullException(nameof(connection));
            this.connectionInfo = connectionInfo ?? throw new ArgumentNullException(nameof(connectionInfo));
            this.serializer = serializer;
            this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        }

        public IObservable<Document> Start(SingleRequestData[] documents, CancellationToken token)
        {
            logger.LogDebug("Start");

            return Observable.Create<Document>(async observer => await ProcessingLogic(documents, observer, token)
                                                   .ConfigureAwait(false))
                             .Take(documents.Length);
        }

        private async Task<IDisposable> ProcessingLogic(SingleRequestData[] documents, IObserver<Document> observer, CancellationToken token)
        {
            var disposable = new CompositeDisposable();
            var connection = connectionFactory();

            var dataStream = connection.CreateSubscription(TopicConstants.GetResultPath(connectionInfo.ClientId))
                                       .Subscription;

            // if main data stream hasn't received anything withing 5 minutes - stop
            var timeout = dataStream.Throttle(TimeSpan.FromMinutes(5), scheduler)
                                    .Subscribe(item =>
                                    {
                                        logger.LogWarning("Stream Timeout");
                                        connection.Dispose();
                                    });

            void ProcessData(MqttApplicationMessage message)
            {
                logger.LogDebug("Received result");
                var processed = serializer.Deserialize<Document[]>(message.Payload);

                foreach (var document in processed)
                {
                    observer.OnNext(document);
                }
            }

            void ProcessDone(MqttApplicationMessage message)
            {
                logger.LogInformation("Received DONE event");
                timeout?.Dispose();
                timeout = dataStream.Throttle(TimeSpan.FromSeconds(10), scheduler)
                                    .Subscribe(item =>
                                    {
                                        logger.LogWarning("Stream Timeout after done!");
                                        connection.Dispose();
                                    });
            }

            void ProcessError(MqttApplicationMessage message)
            {
                var text = Encoding.UTF8.GetString(message.Payload);
                logger.LogError($"Sentiment error: {text}");
                connection.Dispose();
            }

            void ProcessMessage(MqttApplicationMessage message)
            {
                var text = Encoding.UTF8.GetString(message.Payload);
                logger.LogInformation($"Message: {text}");
            }

            await connection.Connect(connectionInfo, token).ConfigureAwait(false);

            disposable.Add(Disposable.Create(() => timeout.Dispose()));
            disposable.Add(dataStream.Subscribe(ProcessData));

            disposable.Add(
                connection.CreateSubscription(TopicConstants.GetDonePath(connectionInfo.ClientId))
                          .Subscription
                          .Subscribe(ProcessDone));

            disposable.Add(
                connection.CreateSubscription(TopicConstants.GetErrorPath(connectionInfo.ClientId))
                          .Subscription
                          .Subscribe(ProcessError));

            disposable.Add(
                connection.CreateSubscription(TopicConstants.GetMessagePath(connectionInfo.ClientId))
                          .Subscription
                          .Subscribe(ProcessMessage));

            disposable.Add(connection);

            return disposable;
        }
    }
}
