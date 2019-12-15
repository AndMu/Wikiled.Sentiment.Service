using Microsoft.Extensions.Logging;
using System;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Wikiled.Common.Utilities.Serialization;
using Wikiled.Sentiment.Api.Request;
using Wikiled.Text.Analysis.Structure;

namespace Wikiled.Sentiment.Api.Service.Flow
{
    public class SentimentFlow : ISentimentFlow
    {
        private readonly ILogger<SentimentFlow> logger;

        private readonly IScheduler scheduler;

        private readonly IJsonSerializer serializer;

        public SentimentFlow(ILogger<SentimentFlow> logger, IScheduler scheduler, IJsonSerializer serializer)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
            this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        }

        public IObservable<Document> Start(WorkRequest request, CancellationToken token)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            logger.LogDebug("Start");

            return Observable.Create<Document>(async observer => await ProcessingLogic(request, observer, token)
                                                   .ConfigureAwait(false))
                             .Take(request.Documents.Length);
        }

        private async Task<IDisposable> ProcessingLogic(WorkRequest request, IObserver<Document> observer, CancellationToken token)
        {
            //var disposable = new CompositeDisposable();
            //var connection = connectionFactory();

            //var dataStream = connection.CreateSubscription(ServiceConstants.GetResultPath(connectionInfo.ClientId))
            //                           .Subscription;

            //// if main data stream hasn't received anything within 5 minutes - stop
            //var timeout = dataStream.Select(item => Unit.Default)
            //    .StartWith(Unit.Default)
            //    .Throttle(TimeSpan.FromMinutes(5), scheduler)
            //    .Subscribe(item =>
            //    {
            //        logger.LogWarning("Stream Timeout");
            //        connection.Dispose();
            //        observer.OnCompleted();
            //    });

            //void ProcessData(MqttApplicationMessage message)
            //{
            //    logger.LogDebug("Received result");
            //    var processed = serializer.Deserialize<Document[]>(message.Payload);

            //    foreach (var document in processed)
            //    {
            //        observer.OnNext(document);
            //    }
            //}

            //void ProcessDone(MqttApplicationMessage message)
            //{
            //    logger.LogInformation("Received DONE event");
            //    timeout?.Dispose();
            //    timeout = dataStream.Throttle(TimeSpan.FromSeconds(10), scheduler)
            //                        .Subscribe(item =>
            //                        {
            //                            logger.LogWarning("Stream Timeout after done!");
            //                            connection.Dispose();
            //                        });
            //}

            //void ProcessError(MqttApplicationMessage message)
            //{
            //    var text = Encoding.UTF8.GetString(message.Payload);
            //    logger.LogError($"Sentiment error: {text}");
            //    connection.Dispose();
            //}

            //void ProcessMessage(MqttApplicationMessage message)
            //{
            //    var text = Encoding.UTF8.GetString(message.Payload);
            //    logger.LogInformation($"Message: {text}");
            //}

            //await connection.Connect(connectionInfo, token).ConfigureAwait(false);

            //await connection.Publish(ServiceConstants.SentimentAnalysis, serializer.SerializeArray(request)).ConfigureAwait(false);

            //disposable.Add(Disposable.Create(() => timeout.Dispose()));
            //disposable.Add(dataStream.Subscribe(ProcessData));

            //disposable.Add(
            //    connection.CreateSubscription(ServiceConstants.GetDonePath(connectionInfo.ClientId))
            //              .Subscription
            //              .Subscribe(ProcessDone));

            //disposable.Add(
            //    connection.CreateSubscription(ServiceConstants.GetErrorPath(connectionInfo.ClientId))
            //              .Subscription
            //              .Subscribe(ProcessError));

            //disposable.Add(
            //    connection.CreateSubscription(ServiceConstants.GetMessagePath(connectionInfo.ClientId))
            //              .Subscription
            //              .Subscribe(ProcessMessage));

            //disposable.Add(connection);

            //return disposable;

            throw new NotImplementedException();
        }
    }
}
