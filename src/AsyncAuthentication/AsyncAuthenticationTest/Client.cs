using System;
using System.IdentityModel.Selectors;
using System.ServiceModel;
using System.ServiceModel.Security;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AsyncAuthenticationTest
{
    internal class Client
    {
        private static int idCount = 0;

        private TestContext _testContext;
        private string _id = string.Empty;

        ChannelFactory<ITestService> _channelFactory = null;
        ITestService _proxy = null;

        public Client(TestContext testContext, string password)
        {
            _testContext = testContext;
            _id = $"{Interlocked.Increment(ref idCount):D2}";

            string hostName = Environment.MachineName;

            var binding = new WS2007HttpBinding();
            binding.ReliableSession.Enabled = false;
            binding.ReliableSession.Ordered = false;
            binding.Security.Mode = SecurityMode.TransportWithMessageCredential;
            binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.None;
            binding.Security.Message.ClientCredentialType = MessageCredentialType.UserName;
            binding.Security.Message.EstablishSecurityContext = false;
            binding.Security.Message.NegotiateServiceCredential = false;
            binding.SendTimeout = new TimeSpan(0, 0, 60);

            _channelFactory = new ChannelFactory<ITestService>(binding, new EndpointAddress(new Uri(string.Format("https://{0}:{1}/{2}", hostName, 12345, "testService"))));

            _channelFactory.Credentials.UserName.UserName = _id.ToString();
            _channelFactory.Credentials.UserName.Password = password;
            _channelFactory.Credentials.ServiceCertificate.SslCertificateAuthentication = new X509ServiceCertificateAuthentication();
            _channelFactory.Credentials.ServiceCertificate.SslCertificateAuthentication.CertificateValidationMode = X509CertificateValidationMode.None;
            _channelFactory.Open();
            _proxy = _channelFactory.CreateChannel();
        }

        public void Open()
        {
            _testContext.WriteLine($"Opening client {_id}");
            ((IClientChannel)_proxy).Open();
            _testContext.WriteLine($"Opened client {_id}");
        }

        public void Close()
        {
            _testContext.WriteLine($"Closing client {_id}");
            ((IClientChannel)_proxy).Close();
            _testContext.WriteLine($"Closed client {_id}");
        }

        public void Call()
        {
            _testContext.WriteLine($"Calling Test on client {_id}");
            _proxy.Test();
            _testContext.WriteLine($"Called Test on client {_id}");
        }

        public void CallAsync()
        {
            _testContext.WriteLine($"Calling Test on client {_id}");
            _proxy.TestAsync().GetAwaiter().GetResult();
            _testContext.WriteLine($"Called Test on client {_id}");
        }

        public void CallThrowing()
        {
            _testContext.WriteLine($"Calling Test on client {_id}");
            _proxy.TestThrow();
            _testContext.WriteLine($"Called Test on client {_id}");
        }

        public void CallThrowingAsync()
        {
            _testContext.WriteLine($"Calling Test on client {_id}");
            _proxy.TestThrowAsync().GetAwaiter().GetResult();
            _testContext.WriteLine($"Called Test on client {_id}");
        }
    }
}