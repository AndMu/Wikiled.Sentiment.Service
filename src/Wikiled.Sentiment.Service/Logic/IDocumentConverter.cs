using Wikiled.Sentiment.Api.Request;
using Wikiled.Sentiment.Text.Data.Review;

namespace Wikiled.Sentiment.Service.Logic
{
    public interface IDocumentConverter
    {
        ParsingDocumentHolder Convert(SingleRequestData review, bool doCleanup);
    }
}