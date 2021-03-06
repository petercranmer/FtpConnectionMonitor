﻿using System;
using System.Configuration;
using System.Linq;
using PersistentConnectionMonitor.Lib;
using PersistentConnectionMonitor.Lib.ExchangeEws;
using PersistentConnectionMonitor.Lib.Ftp;

namespace PersistentConnectionMonitor
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3 && args.Length != 5)
            {
                Console.Out.WriteLine(
                    $"Usage: {nameof(PersistentConnectionMonitor)}.exe type hostname logFile [username] [password]"
                );
                return;
            }

            var type = args[0];
            var hostname = args[1];
            var logFile = args[2];
            var username = args.ElementAtOrDefault(3);
            var password = args.ElementAtOrDefault(4);

            var factory = CreateFactory(type, hostname, username, password);

            var monitor = new ConnectionMonitor(logFile, factory);

            var debugOutput = false;

            bool.TryParse(
                ConfigurationManager.AppSettings["DebugOutput"],
                out debugOutput
            );

            monitor.DebugOutput = debugOutput;

            monitor.Start();
        }

        private static IPersistentConnectionFactory CreateFactory(string type, string hostname, string username, string password)
        {
            switch(type.ToLower())
            {
                case "exchangeews":
                    return CreateEwsFactory(hostname, username, password);

                case "ftp":
                    return CreateFtpFactory(hostname, username, password);

                default:
                    throw new ArgumentException("Unknown factory type", nameof(type));
            }
        }

        private static FtpPersistentConnectionFactory CreateFtpFactory(string hostname, string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return new FtpPersistentConnectionFactory(hostname);
            }
            else
            {
                return new FtpPersistentConnectionFactory(hostname, username, password);
            }
        }

        private static ExchangeEwsPersistentConnectionFactory CreateEwsFactory(string hostname, string username, string password)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                throw new Exception("EwsExchange does not support unauthenticated connections");
            }
            else
            {
                return new ExchangeEwsPersistentConnectionFactory(hostname, username, password);
            }
        }
    }
}
