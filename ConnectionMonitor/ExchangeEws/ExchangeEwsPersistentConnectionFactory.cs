using System;
using System.Net;
using Microsoft.Exchange.WebServices.Data;

namespace ConnectionMonitor.ExchangeEws
{
    public class ExchangeEwsPersistentConnectionFactory : IPersistentConnectionFactory
    {
        // Set to a relatively low value so that we get quick feedback when the server goes down.
        private const int TIMEOUT_SECONDS = 5;

        private string endPoint;
        private string username;
        private string password;

        public ExchangeEwsPersistentConnectionFactory(string endPoint, string username, string password)
        {
            this.endPoint = endPoint;
            this.username = username;
            this.password = password;
        }

        public IPersistentConnection CreateConnection()
        {
            var service = new ExchangeService(ExchangeVersion.Exchange2013_SP1);

            service.Credentials = new NetworkCredential(username, password);
            service.UseDefaultCredentials = false; // Exchange 365

            service.KeepAlive = true;
            service.Timeout = TIMEOUT_SECONDS * 1000;
            
            return new ExchangeEwsPersistentConnection(
                service, 
                this.username, 
                this.RedirectionUrlValidationCallback
            );
        }
        
        private bool RedirectionUrlValidationCallback(string redirectionUrl)
        {
            // The default for the validation callback is to reject the URL.
            bool result = false;

            Uri redirectionUri = new Uri(redirectionUrl);

            // Validate the contents of the redirection URL. In this simple validation
            // callback, the redirection URL is considered valid if it is using HTTPS
            // to encrypt the authentication credentials. 
            if (redirectionUri.Scheme == "https")
            {
                result = true;
            }
            return result;
        }
    }
}
