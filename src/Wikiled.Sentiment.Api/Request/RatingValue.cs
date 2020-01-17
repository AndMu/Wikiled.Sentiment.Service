namespace Wikiled.Sentiment.Api.Request
{
    public class RatingValue
    {
        public double? Stars { get; set; }

        public double Negative { get; set; }

        public double Positive { get; set; }
    }
}
