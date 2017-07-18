using System;
using System.Collections.Generic;
using System.Net;
using FluentFTP;

namespace PersistentConnectionMonitor.Lib.Ftp
{
    public class FtpPersistentConnectionFactory : IPersistentConnectionFactory
    {
        private const int NOOP_INTERVAL_SECONDS = 30;

        private readonly string hostname, username, password;

        private readonly IList<DateTime> networkFailures = new List<DateTime>();
        
        public FtpPersistentConnectionFactory(string hostname)
        {
            this.hostname = hostname;
        }

        public FtpPersistentConnectionFactory(string hostname, string username, string password)
            : this (hostname)
        {
            this.username = username;
            this.password = password;            
        }

        public IPersistentConnection CreateConnection()
        {
            var ftpClient = new FtpClient(this.hostname);
            ftpClient.Credentials = new NetworkCredential(this.username, this.password);

            return new FtpPersistentConnection(ftpClient, NOOP_INTERVAL_SECONDS, 0);
        }
    }
}
