using System.Collections.ObjectModel;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;

namespace Microsoft.ServiceModel
{
    internal class AsyncPasswordValidatorBehavior : IServiceBehavior
    {
        public void ApplyDispatchBehavior(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase)
        {
            IOperationBehavior behavior = new AsyncPasswordValidatorCompletionBehavior();
            foreach (ServiceEndpoint endpoint in serviceDescription.Endpoints)
            {
                foreach (OperationDescription operation in endpoint.Contract.Operations)
                {
                    // Prevent double adding the operation behavior as this service behavior adds
                    // the AsyncPasswordValidatorCompletionBehavior to everything.
                    if (operation.Behaviors.Find<AsyncPasswordValidatorCompletionBehavior>() == null)
                    {
                        operation.Behaviors.Add(behavior);
                    }
                }
            }
        }

        public void Validate(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase) { }
        public void AddBindingParameters(ServiceDescription serviceDescription, ServiceHostBase serviceHostBase, Collection<ServiceEndpoint> endpoints, BindingParameterCollection bindingParameters) { }
    }
}