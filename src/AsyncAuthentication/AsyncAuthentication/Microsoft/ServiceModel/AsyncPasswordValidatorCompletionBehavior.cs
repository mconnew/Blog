using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.ServiceModel.Dispatcher;

namespace Microsoft.ServiceModel
{
    internal class AsyncPasswordValidatorCompletionBehavior : IOperationBehavior
    {
        public void ApplyDispatchBehavior(OperationDescription operationDescription, DispatchOperation dispatchOperation)
        {
            if (!(dispatchOperation.Invoker is PasswordValidatorCompletionOperationInvoker))
            {
                dispatchOperation.Invoker = new PasswordValidatorCompletionOperationInvoker(dispatchOperation.Invoker);
            }
        }

        public void Validate(OperationDescription operationDescription) { }
        public void ApplyClientBehavior(OperationDescription operationDescription, ClientOperation clientOperation) { }
        public void AddBindingParameters(OperationDescription operationDescription, BindingParameterCollection bindingParameters) { }
    }
}