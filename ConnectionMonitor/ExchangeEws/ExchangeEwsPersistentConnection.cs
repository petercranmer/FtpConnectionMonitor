using System;
using Microsoft.Exchange.WebServices.Autodiscover;
using Microsoft.Exchange.WebServices.Data;

namespace ConnectionMonitor.ExchangeEws
{
    class ExchangeEwsPersistentConnection : IPersistentConnection
    {
        private readonly ExchangeService service;
        private readonly string emailAddress;
        private readonly AutodiscoverRedirectionUrlValidationCallback autoDiscoverCallback;

        private Exception connectionException = null;

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

            this.persistentConnection = new StreamingSubscriptionConnection(service, 30);

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
        }        

        private void Connection_OnDisconnect(object sender, SubscriptionErrorEventArgs args)
        {
            if (args.Exception != null)
            {
                this.connectionException = args.Exception;
            }
            else
            {
                this.persistentConnection.Open();
            }
        }

        private void Connection_OnNotificationEvent(object sender, NotificationEventArgs args)
        {
            //Swallow
        }
    }
}
