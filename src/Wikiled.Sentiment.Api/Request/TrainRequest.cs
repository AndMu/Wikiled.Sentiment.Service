namespace Wikiled.Sentiment.Api.Request
{
    public class TrainRequest
    {
        public bool CleanText { get; set; }

        public string Domain { get; set; }

        public string Name { get; set; }
    }
}
