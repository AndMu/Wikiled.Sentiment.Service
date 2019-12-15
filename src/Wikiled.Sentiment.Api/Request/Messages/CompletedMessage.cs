using Wikiled.WebSockets.Definitions.Messages;

namespace Wikiled.Sentiment.Api.Request.Messages
{
    public class CompletedMessage : Message
    {
        public string Message { get; set; }

        public bool IsError { get; set; }
    }
}
