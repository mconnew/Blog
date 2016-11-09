using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AsyncAuthenticationTest
{
    [TestClass]
    public class AuthenticationTests
    {
        // Visual Studio needs to run as adminsitrator
        // To run these tests, an SSL certificate is needed. Use the following commands to create an SSL certificate
        // and bind it to the SSL port.
        //
        // makecert -r -pe -n "CN=<machineName>" -eku 1.3.6.1.5.5.7.3.1 -ss my -sr localMachine -sky exchange -sp "Microsoft RSA SChannel Cryptographic Provider" -sy 12 testcert.cer
        // netsh http add sslcert ipport=0.0.0.0:12345 certhash=<CERT THUMBPRINT WITHOUT SPACES from mycer.cer> appid={00000000-0000-0000-0000-000000000000}
        private TestContext _testContext;

        public TestContext TestContext
        {
            get { return _testContext; }
            set { _testContext = new TestContextWrapper(value); }
        }

        [TestMethod]
        public void CallHostedServerAsyncAuthSuccess()
        {
            var server = new Server(TestContext, true);
            server.Open();

            var client = new Client(TestContext, "test");
            client.Open();
            client.Call();
            client.Close();
            server.Close();
        }

        [TestMethod]
        public void CallHostedServerAsyncAuthServiceThrows()
        {
            var server = new Server(TestContext, true);
            server.Open();

            var client = new Client(TestContext, "test");
            client.Open();
            bool success = false;
            try
            {
                client.CallThrowing();
                success = true;
            }
            catch (Exception)
            {
            }
            Assert.IsFalse(success);
            client.Close();
            server.Close();
        }

        [TestMethod]
        public void CallAsyncHostedServerAsyncAuthSuccess()
        {
            var server = new Server(TestContext, true);
            server.Open();

            var client = new Client(TestContext, "test");
            client.Open();
            client.CallAsync();
            client.Close();
            server.Close();
        }

        [TestMethod]
        public void CallAsyncHostedServerAsyncAuthServiceThrows()
        {
            var server = new Server(TestContext, true);
            server.Open();

            var client = new Client(TestContext, "test");
            client.Open();
            bool success = false;
            try
            {
                client.CallThrowingAsync();
                success = true;
            }
            catch (Exception)
            {
            }
            Assert.IsFalse(success);
            client.Close();
            server.Close();
        }

        [TestMethod]
        public void CallHostedServerAsyncAuthFails()
        {
            var server = new Server(TestContext, false);
            server.Open();

            var client = new Client(TestContext, "test");
            client.Open();
            try
            {
                client.Call();
            }
            catch (Exception)
            {
            }
            client.Close();
            server.Close();
        }
    }
}
