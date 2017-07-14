using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace ConnectionMonitor
{
    public class ConnectionMonitor
    {
        protected const int SINGLE_FAILURE_GRACE_PERIOD_MS = 10000;
        protected const int SINGLE_FAILURE_RETRY_INTERVAL_MS = 1000;

        protected readonly string logFile;

        protected readonly IList<DateTime> networkFailures = new List<DateTime>();
        private readonly IPersistentConnectionFactory persitentConnectionFactory;

        public ConnectionMonitor(string logFile, IPersistentConnectionFactory persitentConnectionFactory)
        {
            this.logFile = logFile;
            this.persitentConnectionFactory = persitentConnectionFactory;
        }

        public bool DebugOutput { get; set; } = false;

        public void Start()
        {
            while (true)
            {
                using (var connection = this.persitentConnectionFactory.CreateConnection())
                {
                    try
                    {
                        this.StartConnectionMonitor(connection);
                    }
                    catch (Exception e)
                    {
                        if (this.IsFailureNew())
                        {
                            this.Log(connection, $"Failure: {e.Message}");
                            this.networkFailures.Add(DateTime.Now);
                        }
                        else
                        {
                            this.Debug(
                                connection,
                                $"Failure considered part of last failure sleeping"
                            );
                            Thread.Sleep(SINGLE_FAILURE_RETRY_INTERVAL_MS);
                        }
                    }
                }
            }
        }

        private void StartConnectionMonitor(IPersistentConnection connection)
        {
            var sleepTimeout = connection.KeepAliveInteralSeconds * 1000;

            connection.Connect();

            this.Log(connection, $"Connection established");

            while (true)
            {
                Thread.Sleep(sleepTimeout);

                if (!connection.IsConnected)
                {
                    throw new Exception("Connection lost");
                }

                connection.KeepAlive();
                this.Debug(connection, "Keep alive sent successfully");
            }
        }


        private void Log(IPersistentConnection persistentConnection, string message)
        {
            this.Debug(persistentConnection, message);
            
            using (var writer = File.AppendText(this.logFile))
            {
                writer.WriteLine(
                    this.FormatAsLog(persistentConnection, message)
                );
            }
        }

        private void Debug(IPersistentConnection persistentConnection, string message)
        {
            if (this.DebugOutput)
            {
                Console.Out.WriteLineAsync(
                    this.FormatAsLog(persistentConnection, message)
                );
            }
        }

        private string FormatAsLog(IPersistentConnection persistentConnection, string message)
        {
            return $"[{persistentConnection.Name}] {DateTime.Now.ToString()}: {message}";
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
    }
}
