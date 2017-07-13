using System;

namespace ConnectionMonitor
{
    public interface IPersistentConnection : IDisposable
    {
        bool IsConnected { get; }
        int KeepAliveInteralSeconds { get; }
        string Name { get; }

        void Connect();
        void KeepAlive();
    }
}
