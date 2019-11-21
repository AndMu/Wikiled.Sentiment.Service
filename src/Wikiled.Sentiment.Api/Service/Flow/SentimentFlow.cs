using System;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;
using System.Threading;
using MQTTnet;
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

        public SentimentFlow(ILogger<SentimentFlow> logger, IScheduler scheduler, Func<IMqttConnection> connection, MqttConnectionInfo connectionInfo)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.connectionFactory = connection ?? throw new ArgumentNullException(nameof(connection));
            this.connectionInfo = connectionInfo ?? throw new ArgumentNullException(nameof(connectionInfo));
            this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        }

        public IObservable<Document> Start(SingleRequestData[] documents, CancellationToken token)
        {
            logger.LogDebug("Start");

            return Observable.Create<Document>(async observer =>
            {
                var disposable = new CompositeDisposable();
                var connection = connectionFactory();
                var timeout = Observable.Timer(TimeSpan.FromMinutes(5), scheduler);

                void ProcessData(MqttApplicationMessage message)
                {
                }

                void ProcessDone(MqttApplicationMessage message)
                {
                }

                void ProcessError(MqttApplicationMessage message)
                {
                    connection.Dispose();
                }

                void ProcessMessage(MqttApplicationMessage message)
                {

                }

                await connection.Connect(connectionInfo, token).ConfigureAwait(false);
                disposable.Add(
                    connection.CreateSubscription(TopicConstants.GetResultPath(connectionInfo.ClientId))
                        .Subscription
                        .TakeUntil(timeout)
                        .Subscribe(ProcessData));

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
            });
        }
    }
}
