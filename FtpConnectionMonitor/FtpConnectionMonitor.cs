using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using FluentFTP;

namespace FtpConnectionMonitor
{
    class FtpConnectionMonitor
    {
        private const int NOOP_INTERVAL_SECONDS = 30;
        private const int SINGLE_FAILURE_GRACE_PERIOD_MS = 10000;
        private const int SINGLE_FAILURE_RETRY_INTERVAL_MS = 1000;

        private readonly string hostname, username, password;
        private readonly string logFile;

        private readonly IList<DateTime> networkFailures = new List<DateTime>();
        
        public FtpConnectionMonitor(string hostname, string logFile)
        {
            this.hostname = hostname;
            this.logFile = logFile;

            //FtpTrace.AddListener(new ConsoleTraceListener());
        }

        public FtpConnectionMonitor(string hostname, string logFile, string username, string password)
            : this (hostname, logFile)
        {
            this.username = username;
            this.password = password;
            
        }

        private FtpClient CreateFtpClient()
        {
            var ftpClient = new FtpClient(this.hostname);
            ftpClient.Credentials = new NetworkCredential(this.username, this.password);
            return ftpClient;
        }

        public void Start()
        {
            while(true)
            {
                using (var ftpClient = this.CreateFtpClient())
                {
                    try
                    {
                        this.StartConnectionMonitor(ftpClient);
                    }
                    catch (Exception e)
                    {
                        if (this.IsFailureNew())
                        {
                            this.Log($"Failure: {e.Message}");
                            this.networkFailures.Add(DateTime.Now);
                        }
                        else
                        {
                            this.Debug(
                                $"Failure considered part of last failure sleeping"
                            );
                            Thread.Sleep(SINGLE_FAILURE_RETRY_INTERVAL_MS);
                        }
                    }
                }
            }
        }

        private void Log(string message)
        {
            this.Debug(message);

            using (var writer = File.AppendText(this.logFile))
            {
                writer.WriteLine(DateTime.Now.ToString() + " " + message);
            }
        }

        private bool IsFailureNew()
        {
            if (networkFailures.Count > 0)
            {
                var lastFailure = networkFailures.Last();
                var timeSinceLastFailure = DateTime.Now.Subtract(lastFailure);

                if (timeSinceLastFailure.TotalMilliseconds <= SINGLE_FAILURE_GRACE_PERIOD_MS)
                {
                    return false;
                }
            }

            return true;
        }

        private void StartConnectionMonitor(FtpClient ftpClient)
        {
            var sleepTimeout = NOOP_INTERVAL_SECONDS * 1000;

            ftpClient.Connect();
            ftpClient.RetryAttempts = 0;
            this.Debug($"Connection established to {this.hostname}");

            while (true)
            {
                Thread.Sleep(sleepTimeout);

                if (!ftpClient.IsConnected)
                {
                    throw new Exception("FTP Connection lost");
                }

                ftpClient.Execute("noop");
                this.Debug("NOOP sent successfully");
            }
        }

        private void Debug(string message)
        {
            Console.Out.WriteLineAsync(
                $"{DateTime.Now.ToString()}: {message}"
            );
        }
    }
}
