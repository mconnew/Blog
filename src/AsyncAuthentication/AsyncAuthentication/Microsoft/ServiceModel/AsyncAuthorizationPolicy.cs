using System;
using System.IdentityModel.Claims;
using System.IdentityModel.Policy;
using System.Threading.Tasks;

namespace Microsoft.ServiceModel
{
    internal class AsyncAuthorizationPolicy : IAuthorizationPolicy
    {
        private Task _task;

        public AsyncAuthorizationPolicy(Task task)
        {
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            _task = task;
        }

        public string Id { get; }

        public bool Evaluate(EvaluationContext evaluationContext, ref object state)
        {
            evaluationContext.Properties[PasswordValidatorCompletionOperationInvoker.PendingAuthorizationTaskKeyName] = _task;
            return true;
        }

        public ClaimSet Issuer { get; }
    }
}