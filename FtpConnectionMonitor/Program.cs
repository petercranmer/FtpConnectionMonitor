using System;

namespace FtpConnectionMonitor
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2 && args.Length != 4)
            {
                Console.Out.WriteLine("Usage: FtpConnectionMonitor.exe hostname logFile [username] [password]");
                return;
            }

            var hostname = args[0];
            var logFile = args[1];

            FtpConnectionMonitor monitor = null;

            if (args.Length == 2)
            {
                monitor = new FtpConnectionMonitor(hostname, logFile);
            }
            else
            {
                var username = args[2];
                var password = args[3];

                monitor = new FtpConnectionMonitor(hostname, logFile, username, password);
            }

            monitor.Start();
        }
    }
}
