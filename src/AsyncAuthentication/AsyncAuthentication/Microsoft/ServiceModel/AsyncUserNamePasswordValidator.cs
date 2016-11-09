using System.Diagnostics.Contracts;
using System.IdentityModel.Selectors;
using System.Threading.Tasks;

namespace Microsoft.ServiceModel
{
    public class AsyncUserNamePasswordValidator : UserNamePasswordValidator
    {
        public override void Validate(string userName, string password)
        {
            Contract.Assert(false, "This method shouldn't be called");
        }

        public virtual Task ValidateAsync(string userName, string password)
        {
            return Task.FromResult((object)null);
        }
    }
}