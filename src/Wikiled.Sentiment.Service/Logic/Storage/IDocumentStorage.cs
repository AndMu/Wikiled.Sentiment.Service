using System;
using System.Threading.Tasks;
using Wikiled.Sentiment.Api.Request;

namespace Wikiled.Sentiment.Service.Logic.Storage
{
    public interface IDocumentStorage
    {
        Task Save(SaveRequest message);

        int Count(string client, string name);

        void Delete(string client, string name);

        IObservable<SingleRequestData> Load(string client, string name, bool classType);

        string GetLocation(string client, string name, string type = "documents");
    }
}
