using System;
using Microsoft.Exchange.WebServices.Autodiscover;
using Microsoft.Exchange.WebServices.Data;

namespace PersistentConnectionMonitor.Lib.ExchangeEws
{
    class ExchangeEwsPersistentConnection : PersistentConnection, IPersistentConnection
    {
        private string lockObject = "LOCK";

        private readonly ExchangeService service;
        private readonly string emailAddress;
        private readonly AutodiscoverRedirectionUrlValidationCallback autoDiscoverCallback;

        /// <summary>
        /// The exception given by the disconnect handler, which is later thrown when the
        /// connection is polled.
        /// </summary>
        private Exception connectionException = null;

        /// <summary>
        /// The time when the connection first responded that it was closed during polling.
        /// </summary>
        private DateTime? timeConnectionPolledFalse = null;

        /// <summary>
        /// Describes the number of seconds to wait for a disconnect event if the connection is
        /// discovered to be closed during polling.  This prevents race conditions causing the 
        /// graceful disconnects to be interpreted as a drop out.
        /// </summary>
        private const int DISCONNECT_EVENT_GRACE_PERIOD_SECONDS = 60;

        /// <summary>
        /// The maximum length of a session.  The server will gracefully close the connection
        /// after this length of time has passed.  This is not to be considered a drop out.
        /// </summary>
        private const int SESSION_LIFETIME_MINUTES = 30;

        /// <summary>
        /// Flags the connection as disconnected gracefully (due to max session length).  When 
        /// polling, if this is set the connection will be reopened without a drop-out being 
        /// registered.
        /// </summary>
        private bool connectionClosedGracefully = false;

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
                SESSION_LIFETIME_MINUTES
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
            lock (this.lockObject)
            {
                if (this.connectionException != null)
                {
                    throw this.connectionException;
                }

                if (this.connectionClosedGracefully)
                {
                    this.persistentConnection.Open();
                    this.timeConnectionPolledFalse = null;
                    
                    this.OnDebug(
                        this,
                        "Gracefully closed connection restored"
                    );
                    this.connectionClosedGracefully = false;
                }

                if (!this.persistentConnection.IsOpen)
                {
                    if (this.timeConnectionPolledFalse == null)
                    {
                        this.timeConnectionPolledFalse = DateTime.Now;
                        this.OnDebug(this, "Connection disconnected but awaiting notification from server");
                    }
                    else
                    {
                        var elapsedSinceConnectionPolledFalse = DateTime.Now.Subtract(
                            this.timeConnectionPolledFalse.Value
                        );

                        if (elapsedSinceConnectionPolledFalse.TotalSeconds > DISCONNECT_EVENT_GRACE_PERIOD_SECONDS)
                        {
                            throw new Exception("Persistent connection dropped without event notification");
                        }
                        else
                        {
                            this.OnDebug(
                                this,
                                "Connection still disconnected - awaiting notification from server " +
                                $"until {DISCONNECT_EVENT_GRACE_PERIOD_SECONDS} seconds have elapsed"
                            );
                        }
                    }
                }
            }
        }        

        private void Connection_OnDisconnect(object sender, SubscriptionErrorEventArgs args)
        {
            lock (this.lockObject)
            {
                if (args.Exception != null)
                {
                    this.connectionException = args.Exception;
                }
                else
                {
                    // It should be possible to reconnect here, but due to a bug in EWS
                    // this is not possible:
                    //
                    // https://github.com/OfficeDev/ews-managed-api/issues/83
                    //
                    // The Nuget package is ancient but other than this workaround it's easier
                    // to use than to download the source and build for this project.

                    this.OnDebug(
                        this,
                        "Connect has been closed gracefully due to max session limit, connection will be restored"
                    );
                    this.connectionClosedGracefully = true;
                }
            }
        }

        private void Connection_OnNotificationEvent(object sender, NotificationEventArgs args)
        {
            //Swallow
        }
    }
}
