using System.IdentityModel.Selectors;
using System.ServiceModel.Description;

namespace Microsoft.ServiceModel
{
    internal class AsyncAuthenticationServiceCredentials : ServiceCredentials
    {
        public AsyncAuthenticationServiceCredentials() { }

        private AsyncAuthenticationServiceCredentials(AsyncAuthenticationServiceCredentials other) : base(other) { }

        protected override ServiceCredentials CloneCore()
        {
            return new AsyncAuthenticationServiceCredentials(this);
        }

        public override SecurityTokenManager CreateSecurityTokenManager()
        {
            return new AuthenticationSecurityTokenManager(Clone());
        }
    }
}