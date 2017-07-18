using System;

namespace PersistentConnectionMonitor.Lib
{
    public delegate void ConnectionDebugEventDelegate(IPersistentConnection persistentConnection, string message);

    public interface IPersistentConnection : IDisposable
    {
        bool IsConnected { get; }
        int KeepAliveInteralSeconds { get; }
        string Name { get; }

        void Connect();
        void KeepAlive();

        event ConnectionDebugEventDelegate Debug;
    }
}
