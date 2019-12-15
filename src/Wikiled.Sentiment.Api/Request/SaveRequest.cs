namespace Wikiled.Sentiment.Api.Request
{
    public class SaveRequest
    {
        public string User { get; set; }

        public SingleRequestData[] Documents { get; set; }

        public string Name { get; set; }
    }
}
