using System;
using Microsoft.Exchange.WebServices.Autodiscover;
using Microsoft.Exchange.WebServices.Data;

namespace PersistentConnectionMonitor.Lib.ExchangeEws
{
    class ExchangeEwsPersistentConnection : PersistentConnection, IPersistentConnection
    {
        private readonly ExchangeService service;
        private readonly string emailAddress;
        private readonly AutodiscoverRedirectionUrlValidationCallback autoDiscoverCallback;

        private Exception connectionException = null;

        private DateTime? timeConnectionPolledFalse = null;
        private const int CONNECTION_EVENT_WAIT_PERIOD_SECONDS = 60;
        private const int EXCHANGE_FORCED_RECONNECT_MINUTES = 30;

        private StreamingSubscriptionConnection persistentConnection;

        public ExchangeEwsPersistentConnection(ExchangeService service, string emailAddress, AutodiscoverRedirectionUrlValidationCallback autoDiscoverCallback)
        {
            this.service = service;
            this.emailAddress = emailAddress;
            this.autoDiscoverCallback = autoDiscoverCallback;
        }

        /// <remarks>
        /// Assume connected, attempt to issue a keep alive will throw an exception.
        /// </remarks>
        public bool IsConnected => (this.connectionException == null);
        
        // Keep alive doesn't do anything so set quite low so it regularly verifies the connection.
        public int KeepAliveInteralSeconds => 15;

        public string Name => $"exchange://{this.service.Url}";
        
        public void Connect()
        {
            this.service.AutodiscoverUrl(this.emailAddress, this.autoDiscoverCallback);
            
            var subscription = service.SubscribeToStreamingNotifications(
                new FolderId[]
                {
                    WellKnownFolderName.Inbox
                },
                EventType.NewMail
            );

            this.persistentConnection = new StreamingSubscriptionConnection(
                service, 
                EXCHANGE_FORCED_RECONNECT_MINUTES
            );

            this.persistentConnection.AddSubscription(subscription);
            this.persistentConnection.OnNotificationEvent += Connection_OnNotificationEvent;
            this.persistentConnection.OnDisconnect += Connection_OnDisconnect;
            this.persistentConnection.Open();

        }

        public void Dispose()
        {
            if (this.persistentConnection != null)
            {
                this.persistentConnection.Dispose();
            }            
        }

        /// <remarks>
        /// The Exchange EWS library handles keeps the connection alive natively.
        /// </remarks>
        public void KeepAlive()
        {
            if (this.connectionException != null)
            {
                throw this.connectionException;
            }

            if (!this.persistentConnection.IsOpen)
            {
                if (this.timeConnectionPolledFalse == null)
                {
                    this.timeConnectionPolledFalse = DateTime.Now;
                }
                else
                {
                    var elapsedSinceConnectionPolledFalse = DateTime.Now.Subtract(this.timeConnectionPolledFalse.Value);

                    if (elapsedSinceConnectionPolledFalse.TotalSeconds > CONNECTION_EVENT_WAIT_PERIOD_SECONDS)
                    {
                        throw new Exception("Persistent connection dropped without event notification");
                    }                    
                }
            }
        }        

        private void Connection_OnDisconnect(object sender, SubscriptionErrorEventArgs args)
        {
            if (args.Exception != null)
            {
                this.connectionException = args.Exception;
            }
            else
            {
                try
                {
                    this.persistentConnection.Open();
                    this.timeConnectionPolledFalse = null;

                    this.OnDebug(
                        this,
                        "Connection gracefully restored to Exchange - scheduled disconnect"
                    );
                }
                catch (Exception reconnectException)
                {
                    this.connectionException = reconnectException;
                }
            }
        }

        private void Connection_OnNotificationEvent(object sender, NotificationEventArgs args)
        {
            //Swallow
        }
    }
}
