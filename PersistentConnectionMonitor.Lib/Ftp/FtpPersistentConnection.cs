using FluentFTP;

namespace PersistentConnectionMonitor.Lib.Ftp
{
    class FtpPersistentConnection : PersistentConnection, IPersistentConnection
    {
        private readonly FtpClient ftpClient;

        public FtpPersistentConnection(FtpClient ftpClient, int keepAliveInterval, int retryAttempts)
        {
            this.ftpClient = ftpClient;
            this.KeepAliveInteralSeconds = keepAliveInterval;
            this.RetryAttempts = retryAttempts;
        }

        public bool IsConnected => this.ftpClient.IsConnected;

        public int KeepAliveInteralSeconds { get; }
        public int RetryAttempts { get; }

        public string Name => $"ftp://{this.ftpClient.Credentials.UserName}@{this.ftpClient.Host}";

        public void Connect()
        {
            this.ftpClient.Connect();
        }

        public void Dispose()
        {
            this.ftpClient.Dispose();
        }

        public void KeepAlive()
        {
            this.ftpClient.Execute("NOOP");
        }
    }
}
