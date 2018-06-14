using System.Collections.Generic;
using Wikiled.Sentiment.Text.Parser;

namespace Wikiled.Sentiment.Service.Logic
{
    public interface ILexiconLoader
    {
        IEnumerable<string> Supported { get; }

        ISentimentDataHolder GetLexicon(string name);
    }
}