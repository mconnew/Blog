using System;
using System.IdentityModel.Selectors;
using System.IdentityModel.Tokens;
using System.ServiceModel.Description;
using System.ServiceModel.Security;

namespace Microsoft.ServiceModel
{
    internal class AuthenticationSecurityTokenManager : ServiceCredentialsSecurityTokenManager
    {
        private ServiceCredentials _parent;

        public AuthenticationSecurityTokenManager(ServiceCredentials parent) : base(parent)
        {
            _parent = parent;
        }

        public override SecurityTokenAuthenticator CreateSecurityTokenAuthenticator(SecurityTokenRequirement tokenRequirement, out SecurityTokenResolver outOfBandTokenResolver)
        {
            if (tokenRequirement == null)
            {
                throw new ArgumentNullException(nameof(tokenRequirement));
            }

            string tokenType = tokenRequirement.TokenType;
            outOfBandTokenResolver = null;
            SecurityTokenAuthenticator result = null;
            if (tokenType == SecurityTokenTypes.UserName &&
                _parent.UserNameAuthentication.UserNamePasswordValidationMode == UserNamePasswordValidationMode.Custom)
            {
                result = new AsyncUserNameSecurityTokenAuthenticator(
                    _parent.UserNameAuthentication.CustomUserNamePasswordValidator);
            }
            else
            {
                result = base.CreateSecurityTokenAuthenticator(tokenRequirement, out outOfBandTokenResolver);
            }

            return result;
        }
    }
}