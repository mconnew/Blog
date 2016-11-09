using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IdentityModel.Policy;
using System.IdentityModel.Selectors;
using System.Threading.Tasks;

namespace Microsoft.ServiceModel
{
    internal class AsyncUserNameSecurityTokenAuthenticator : CustomUserNameSecurityTokenAuthenticator
    {
        private UserNamePasswordValidator _validator;
        private static UserNamePasswordValidator s_dummyValidator = new DummyUserNamePasswordValidator();
        private bool _isAsyncValidator;

        public AsyncUserNameSecurityTokenAuthenticator(UserNamePasswordValidator validator) : base(validator is AsyncUserNamePasswordValidator ? s_dummyValidator : validator)
        {
            _validator = validator;
            if (_validator is AsyncUserNamePasswordValidator)
            {
                _isAsyncValidator = true;
            }
        }

        protected override ReadOnlyCollection<IAuthorizationPolicy> ValidateUserNamePasswordCore(string userName, string password)
        {
            var authPolicies = base.ValidateUserNamePasswordCore(userName, password);
            if (_isAsyncValidator)
            {
                var authTask = ((AsyncUserNamePasswordValidator)_validator).ValidateAsync(userName, password);
                if (authTask.IsCompleted)
                {
                    // If the task is completed, get the result (throws or doesn't throw)
                    authTask.GetAwaiter().GetResult();
                }
                else
                {
                    // Add an async auth policy
                    authPolicies = AddAsyncAuthPolicy(authPolicies, authTask);
                }
            }

            return authPolicies;
        }

        private ReadOnlyCollection<IAuthorizationPolicy> AddAsyncAuthPolicy(ReadOnlyCollection<IAuthorizationPolicy> policies, Task task)
        {
            var asyncAuthPolicy = new AsyncAuthorizationPolicy(task);
            var policiesList = new List<IAuthorizationPolicy>(policies.Count + 1);
            policiesList.AddRange(policies);
            policiesList.Add(asyncAuthPolicy);
            return policiesList.AsReadOnly();
        }

        private class DummyUserNamePasswordValidator : UserNamePasswordValidator
        {
            public override void Validate(string userName, string password) { }
        }
    }
}