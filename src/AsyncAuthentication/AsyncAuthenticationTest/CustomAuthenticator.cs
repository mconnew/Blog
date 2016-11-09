using System.IdentityModel.Tokens;
using System.Threading.Tasks;
using Microsoft.ServiceModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AsyncAuthenticationTest
{
    internal class CustomAuthenticator : AsyncUserNamePasswordValidator
    {
        private TestContext _testContext;
        private bool _authSucceeds;

        public CustomAuthenticator(TestContext testContext, bool authSucceeds)
        {
            _testContext = testContext;
            _authSucceeds = authSucceeds;
        }

        public override async Task ValidateAsync(string userName, string password)
        {
            _testContext.WriteLine("ValidateAsync start delay");
            await Task.Delay(1000);
            _testContext.WriteLine("ValidateAsync end delay");
            if (!_authSucceeds)
            {
                _testContext.WriteLine("Auth throwing exception as authSucceeds = false");
                throw new SecurityTokenValidationException($"Oops, bad robot with userName {userName} and password {password}");
            }
        }
    }
}