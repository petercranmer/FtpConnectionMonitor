namespace PersistentConnectionMonitor.Lib
{
    abstract class PersistentConnection
    {
        public event ConnectionDebugEventDelegate Debug;

        protected void OnDebug(IPersistentConnection connection, string message)
        {
            this.Debug?.Invoke(connection, message);
        }
    }
}
