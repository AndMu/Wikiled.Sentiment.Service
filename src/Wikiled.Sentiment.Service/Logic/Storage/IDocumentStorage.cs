using System;
using System.Threading.Tasks;
using Wikiled.Sentiment.Api.Request;

namespace Wikiled.Sentiment.Service.Logic.Storage
{
    public interface IDocumentStorage
    {
        Task Save(string client, SaveRequest request);

        IObservable<SingleRequestData> Load(string client, string name);

        string GetLocation(string client, string name, string type = "documents");
    }
}
