namespace Wikiled.Sentiment.Api.Service.Flow
{
    public static class TopicConstants
    {
        public const string SentimentAnalysis = "Sentiment/Analysis";

        public const string SentimentAnalysisResult = "Sentiment/Result";

        public const string SentimentTraining = "Sentiment/Train";

        public const string SentimentDone = "Sentiment/Done";

        public const string SentimentSave= "Sentiment/Save";

        public const string Error = "Error";

        public const string Message = "Message";

        public const string Model = "model";

        public static string GetResultPath(string userId)
        {
            return $"{SentimentAnalysisResult}/{userId}";
        }

        public static string GetDonePath(string userId)
        {
            return $"{SentimentDone}/{userId}";
        }

        public static string GetErrorPath(string userId)
        {
            return $"{Error}/{userId}";
        }

        public static string GetMessagePath(string userId)
        {
            return $"{Message}/{userId}";
        }
    }
}
