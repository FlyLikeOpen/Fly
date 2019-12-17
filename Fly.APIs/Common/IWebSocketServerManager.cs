using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.APIs.Common
{
    public interface IWebSocketServerManager : IDisposable
    {
        void Start();
        void Start(int serverPort);
        void Send(string routeKey, string message);
        void Send(string message);
        void AddNewConnectionEvent(Action<object, EventArgs> handler);
        void AddDataReceivedEvent(Action<object,string, EventArgs> handler);
        void AddDisconnectedEvent(Action<object, EventArgs> handler);
    }
    public interface ISocketConnection
    {
        string GetRouteKey();
        void Send(string message);
    }
}
