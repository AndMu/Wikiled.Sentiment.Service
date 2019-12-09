using System;
using System.Collections.Generic;
using System.Text;
using Wikiled.WebSockets.Definitions.Messages;

namespace Wikiled.Sentiment.Api.Request.Messages
{
    public class TrainMessage : Message
    {
        public bool CleanText { get; set; }

        public string Domain { get; set; }

        public string Name { get; set; }
    }
}
