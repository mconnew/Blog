using System;
using System.Diagnostics.Contracts;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Dispatcher;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.ServiceModel
{
    internal class PasswordValidatorCompletionOperationInvoker : IOperationInvoker
    {
        internal const string PendingAuthorizationTaskKeyName = "PendingAuthorizationTask";
        private IOperationInvoker _originalInvoker;

        public PasswordValidatorCompletionOperationInvoker(IOperationInvoker originalInvoker)
        {
            _originalInvoker = originalInvoker;
        }

        public object[] AllocateInputs()
        {
            return _originalInvoker.AllocateInputs();
        }

        public object Invoke(object instance, object[] inputs, out object[] outputs)
        {
            var securityContext = ServiceSecurityContext.Current;

            if (securityContext.AuthorizationContext.Properties.ContainsKey(PendingAuthorizationTaskKeyName))
            {
                var authTask = securityContext.AuthorizationContext.Properties[PendingAuthorizationTaskKeyName] as Task;
                try
                {
                    authTask.GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    HandleAuthorizationException(e);
                }
            }

            return _originalInvoker.Invoke(instance, inputs, out outputs);
        }

        public IAsyncResult InvokeBegin(object instance, object[] inputs, AsyncCallback callback, object state)
        {
            var tcs = new TaskCompletionSource<Tuple<object, object[]>>(state);
            var continuationState = Tuple.Create(tcs, callback);
            var task = InvokeWithValidatorAsync(instance, inputs);
            task.ContinueWith(OnContinueInvokeWithValidator, continuationState, CancellationToken.None, TaskContinuationOptions.HideScheduler, TaskScheduler.Default);
            return tcs.Task;
        }

        private static void OnContinueInvokeWithValidator(Task<Tuple<object, object[]>> task, Object state)
        {
            if (task == null)
                throw new ArgumentNullException(nameof(task));
            var tuple = (Tuple < TaskCompletionSource<Tuple<object, object[]>>, AsyncCallback>)state;
            var tcs = tuple.Item1;
            var callbackObj = tuple.Item2;
            if (task.IsFaulted) tcs.TrySetException(task.Exception.InnerException);
            else if (task.IsCanceled) tcs.TrySetCanceled();
            else tcs.TrySetResult(task.Result);

            callbackObj?.Invoke(tcs.Task);
        }

        private async Task<Tuple<object, object[]>> InvokeWithValidatorAsync(object instance, object[] inputs)
        {
            var context = OperationContext.Current;
            var securityContext = ServiceSecurityContext.Current;

            if (securityContext.AuthorizationContext.Properties.ContainsKey(PendingAuthorizationTaskKeyName))
            {
                var authTask = securityContext.AuthorizationContext.Properties[PendingAuthorizationTaskKeyName] as Task;
                if (authTask != null)
                {
                    try
                    {
                        await authTask;
                        OperationContext.Current = context;
                    }
                    catch (Exception e)
                    {
                        OperationContext.Current = context;
                        HandleAuthorizationException(e);
                    }
                }
            }

            var tcs = new TaskCompletionSource<Tuple<object, object[]>>(instance);
            var asyncState = Tuple.Create(this, tcs);
            IAsyncResult iar = _originalInvoker.InvokeBegin(instance, inputs, OnInvoke, asyncState);
            if (iar.CompletedSynchronously)
            {
                CompleteInvoke(iar);
            }

            return await tcs.Task;
        }

        private void HandleAuthorizationException(Exception exception)
        {
            var subCode = new FaultCode("InvalidSecurityToken", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd");
            var senderCode = FaultCode.CreateSenderFaultCode(subCode);
            var reason = new FaultReason("An error occurred when processing the security tokens in the message:" + exception.Message);
            var faultMessage = MessageFault.CreateFault(senderCode, reason);
            throw new FaultException(faultMessage, "http://www.w3.org/2005/08/addressing/soap/fault");
        }

        private static void OnInvoke(IAsyncResult ar)
        {
            if (ar == null)
                throw new ArgumentNullException(nameof(ar));

            if (ar.CompletedSynchronously)
            {
                return;
            }

            var state = (Tuple < PasswordValidatorCompletionOperationInvoker, TaskCompletionSource< Tuple < object, object[]>>> )ar.AsyncState;
            var thisPtr = state.Item1;

            thisPtr.CompleteInvoke(ar);
        }

        private void CompleteInvoke(IAsyncResult ar)
        {
            if (ar == null)
                throw new ArgumentNullException(nameof(ar));

            var state = (Tuple < PasswordValidatorCompletionOperationInvoker, TaskCompletionSource< Tuple < object, object[]>>> )ar.AsyncState;
            var tcs = state.Item2;
            var instance = tcs.Task.AsyncState;

            object[] outputs;
            try
            {
                var result = _originalInvoker.InvokeEnd(instance, out outputs, ar);
                tcs.TrySetResult(Tuple.Create(result, outputs));
            }
            catch (Exception e)
            {
                tcs.TrySetException(e);
            }
        }


        public object InvokeEnd(object instance, out object[] outputs, IAsyncResult result)
        {
            Task<Tuple<object, object[]>> task = (Task < Tuple < object, object[]>> )result;
            Contract.Assert(task.IsCompleted);
            Tuple<object, object[]> tuple = task.GetAwaiter().GetResult();
            outputs = tuple.Item2;
            return tuple.Item1;
        }

        public bool IsSynchronous { get { return _originalInvoker.IsSynchronous; } }
    }

}