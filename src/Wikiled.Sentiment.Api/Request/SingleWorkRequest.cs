namespace Wikiled.Sentiment.Api.Request
{
    public class SingleWorkRequest
    {
        public SingleRequestData Review { get; set; }

        public bool CleanText { get; set; }

        public string Domain { get; set; }

        public string Model { get; set; }
    }
}
