using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Wikiled.WebSockets.Client.Connection;

namespace Wikiled.Sentiment.Service.Tests.Acceptance
{
    public class TestSocketFactory : ISocketFactory
    {
        private readonly WebSocket socket;

        public TestSocketFactory(WebSocket socket)
        {
            this.socket = socket ?? throw new ArgumentNullException(nameof(socket));
        }

        public Task<WebSocket> Create(Uri uri)
        {
            return Task.FromResult(socket);
        }
    }
}
