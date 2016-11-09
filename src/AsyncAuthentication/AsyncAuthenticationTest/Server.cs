using System;
using System.Collections.Generic;
using System.IdentityModel.Selectors;
using System.ServiceModel;
using System.ServiceModel.Description;
using Microsoft.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AsyncAuthenticationTest
{
    internal class Server
    {
        private TestContext _testContext;
        private ServiceHost _serviceHost;
        public Server(TestContext testContext, bool authSucceeds)
        {
            _testContext = testContext;

            string hostname = Environment.MachineName;

            List<Uri> uris = new List<Uri>();
            uris.Add(new Uri($"https://{hostname}:{12345}/{"testService"}"));
            _serviceHost = new ServiceHost(typeof(TestService), uris.ToArray());
            _serviceHost.UseAsyncAuthentication();
            _serviceHost.Credentials.UserNameAuthentication.UserNamePasswordValidationMode = System.ServiceModel.Security.UserNamePasswordValidationMode.Custom;
            _serviceHost.Credentials.UserNameAuthentication.IncludeWindowsGroups = false;
            UserNamePasswordValidator asyncValidator = new CustomAuthenticator(testContext, authSucceeds);
            _serviceHost.Credentials.UserNameAuthentication.CustomUserNamePasswordValidator = asyncValidator;

            var binding = new WS2007HttpBinding();
            binding.ReliableSession.Enabled = false;
            binding.ReliableSession.Ordered = false;
            binding.Security.Mode = SecurityMode.TransportWithMessageCredential;
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;
            binding.Security.Message.ClientCredentialType = MessageCredentialType.UserName;
            binding.Security.Message.EstablishSecurityContext = false;
            binding.Security.Message.NegotiateServiceCredential = false;

            _serviceHost.AddServiceEndpoint(typeof(ITestService), binding, "");
        }

        public void Open()
        {
            _testContext.WriteLine("Opening service");
            _serviceHost.Open();
            _testContext.WriteLine("Opened service");
        }

        public void Close()
        {
            _testContext.WriteLine("Closing service");
            _serviceHost.Close();
            _testContext.WriteLine("Closed service");
        }
    }
}