using System;

namespace Wikiled.Sentiment.Api.Request
{
    public class SingleRequestData
    {
        public DateTime? Date { get; set; }

        public string Author { get; set; }

        public string Id { get; set; }

        public string Text { get; set; }

        public bool? IsPositive { get; set; }
    }
}
