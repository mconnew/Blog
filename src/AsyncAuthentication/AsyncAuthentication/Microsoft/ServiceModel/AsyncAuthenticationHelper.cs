using System;
using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace Microsoft.ServiceModel
{
    public static class AsyncAuthenticationHelper
    {
        public static void UseAsyncAuthentication(this ServiceHost serviceHost)
        {
            ServiceCredentials creds = serviceHost.Description.Behaviors.Find<ServiceCredentials>();
            if (creds != null)
            {
                serviceHost.Description.Behaviors.Remove<ServiceCredentials>();
            }

            serviceHost.Description.Behaviors.Add(new AsyncAuthenticationServiceCredentials());
            serviceHost.Description.Behaviors.Add(new AsyncPasswordValidatorBehavior());
        }
    }
}