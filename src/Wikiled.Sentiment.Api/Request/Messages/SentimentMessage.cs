using Wikiled.WebSockets.Definitions.Messages;

namespace Wikiled.Sentiment.Api.Request.Messages
{
    public class SentimentMessage : Message
    {
        public WorkRequest Request { get; set; }
    }
}
