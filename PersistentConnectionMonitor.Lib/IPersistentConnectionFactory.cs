namespace PersistentConnectionMonitor.Lib
{
    public interface IPersistentConnectionFactory
    {
        IPersistentConnection CreateConnection();
    }
}
