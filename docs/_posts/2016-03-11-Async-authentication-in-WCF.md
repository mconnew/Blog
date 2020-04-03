---
layout: post
title: Async authentication in WCF
---

I often deal with customer requests on various performance related aspects of WCF and sometimes the solutions are either non-obvious or non-trivial. I thought it would be useful to publish some of the technical details of some of the problems I investigate and provide the solutions I come up with in a generic reusable way.

This particular issue arrived via a Connect issue from an external customer. The problem they were having is that they had implemented a custom UserNamePasswordValidator which took some time to validate the incoming credentials and this was limiting the request rate of their service. This is caused by an assumption in WCF's design that implementations of many of the extensibility points are quick running and don't block the thread. To understand why this happens, we need to understand a little bit of how WCF receives requests and processes them. When a service is started, a number of asynchronous pending accept calls are made to the transport. When a client connects, the transport calls a callback to notify the arrival of a connection. An asynchronous receive call is then made to receive an incoming message. Depending on your binding and service configuration, there are potentially multiple steps in the receive pipeline that need to be completed before dispatching the incoming request to the service operation. At some point in the pipeline, a new asynchronous call is made to receive the next incoming message, rinse and repeat. If any of the steps in the pipeline take a while to complete, it delays issuing the next asynchronous call for the next incoming message resulting in a reduced request rate.

One of the steps in the pipeline is to authenticate the incoming request to make sure the credentials are correct and create the ServiceSecurityContext for the operation. This is where a custom UserNamePasswordValidator will be run. If the validator takes a while to run, for example to make an off box call via a SQL query, then the asynchronous call to receive the next incoming message will be delayed. Even 10ms would be a large delay as this would limit a single instance of the receive loop to only being able to dispatch at most 100 requests per second.

Presuming you can't speed up your authentication, a mechanism is needed to start the authentication asynchronously storing some intermediate state and allow the receive pipe to continue to request the next incoming request from the transport. The result of the authentication then needs to be picked up at a later stage and fail the request is the authentication fails. So the first thing needed is a new extensibility that supports async authentication in a way which allows easily picking up the result later. The natural answer is to use tasks. A very simple UserNamePasswordValidator looks something like this.

```c#
public class CustomUserNameValidator : UserNamePasswordValidator
{
	public override void Validate(string userName, string password)
	{
		if (null == userName || null == password) throw new ArgumentNullException();
		if (!(userName == "test1" &amp; password == "1tset")) throw new FaultException("Unknown Username or Incorrect Password");
	}
}
```

To make things easy, I'll derive UserNamePasswordValidator and add a virtual method with an async equivalent method ValidateAsync. This will make porting existing code a simple task.
```c#
public class AsyncUserNamePasswordValidator : UserNamePasswordValidator
{
	public override void Validate(string userName, string password) { }
	public virtual Task ValidateAsync(string userName, string password) { return Task.FromResult((object)null); }
}
```
Now I need to replace the class which calls the Validate method with one which calls ValidateAsync and stores the returned Task somewhere that I can pick up later. The class that currently does this is CustomUserNameSecurityTokenAuthenticator so we'll derive a class from this so that we can leverage as much of the existing functionality as possible.
```c#
public class AsyncUserNameSecurityTokenAuthenticator : CustomUserNameSecurityTokenAuthenticator
{
	private UserNamePasswordValidator _validator;
	private bool _isAsyncValidator;

	public AsyncUserNameSecurityTokenAuthenticator(UserNamePasswordValidator validator) : base(validator)
	{
		_validator = validator;
		if (_validator is AsyncUserNamePasswordValidator) { _isAsyncValidator = true; }
	}
}
```
I store whether the validator is derived from AsyncUserNamePasswordValidator so that later I can do the right thing if a non-async validator is used. So far this gives the exact same behavior as the base class. I need to override the ValidateUserNamePasswordCore method to change the behavior and call ValidateAsync. When calling ValidateUserNamePasswordCore on the base class, if authentication is successful it creates a readonly list of IAuthorizationPolicy which contains the ClaimSet for the authenticated username. I don't want to re-implement the code which creates the ClaimSet so I need to make a call to base.ValidateUserNamePasswordCore. It's possible that the provided validator has the authentication also implemented in the synchronous Validate method which would mean the delay I'm trying to avoid would still happen when calling the base class. I can solve this by providing a dummy no-op validator to the base constructor but only if the validator is an AsyncUserNamePasswordValidator. Once I've called ValidateAsync and have a task, I need somewhere to put it until later. The only available place is in the return value from ValidateUserNamePasswordCore which is of type ReadOnlyCollection. Looking at the definition of IAuthorizationPolicy, there's an Evaluate method which is passed an EvaluationContext which has a Properties dictionary property. Later WCF will copy the contents of this dictionary to the ServiceSecurityContext which will be accessible later. So I need to provide my own IAuthorizationPolicy which holds the authorization Task which will copy it into the EvaluationContext when Evaluate is called.
```c#
public class AsyncUserNameSecurityTokenAuthenticator : CustomUserNameSecurityTokenAuthenticator
{
	private UserNamePasswordValidator _validator;
	private static UserNamePasswordValidator s_dummyValidator = new DummyUserNamePasswordValidator();
	private bool _isAsyncValidator;

	public AsyncUserNameSecurityTokenAuthenticator(UserNamePasswordValidator validator) : base(validator is AsyncUserNamePasswordValidator ? s_dummyValidator : validator)
	{
		_validator = validator;
		if (_validator is AsyncUserNamePasswordValidator) { _isAsyncValidator = true; }
	}
	protected override ReadOnlyCollection<IAuthorizationPolicy> ValidateUserNamePasswordCore(string userName, string password)
	{
		var authPolicies = base.ValidateUserNamePasswordCore(userName, password);
		if (_isAsyncValidator)
		{
			var authTask = ((AsyncUserNamePasswordValidator)_validator).ValidateAsync(userName, password);
			if (authTask.IsCompleted) { authTask.GetAwaiter().GetResult(); }
			else { authPolicies = AddAsyncAuthPolicy(authPolicies, authTask); }
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
```
Here's the implementation of AsyncAuthorizationPolicy. A true return value from Evaluate tells WCF to only call Evaluate once.
```c#
internal class AsyncAuthorizationPolicy : IAuthorizationPolicy
{
	private Task _task;

	public AsyncAuthorizationPolicy(Task task)
	{
		if (task == null) { throw new ArgumentNullException(nameof(task)); }
		_task = task;
	}
	public string Id { get; }
	public bool Evaluate(EvaluationContext evaluationContext, ref object state)
	{
		evaluationContext.Properties["PendingAuthorizationTask"] = _task;
		return true;
	}
	public ClaimSet Issuer { get; }
}
```
So now I have an AsyncUserNameSecurityTokenAuthenticator, I need have WCF use it. Normally a new instance of CustomUserNameSecurityTokenAuthenticator is created when ServiceCredentialsSecurityTokenManager.CreateSecurityTokenAuthenticator is called with the relevant SecurityTokenRequirement. I need to derive this class, override the CreateSecurityTokenAuthenticator method and return an instance of my token authenticator where normally a CustomUserNameSecurityTokenAuthenticator would be returned. In all other cases, I need to defer to the base class and maintain the existing behavior. For example, when using certificates for transport security, this same class is used to retrieve the X509Certificate2 instance used by the service.
```c#
public class AuthenticationSecurityTokenManager : ServiceCredentialsSecurityTokenManager
{
	private ServiceCredentials _parent;
	public AuthenticationSecurityTokenManager(ServiceCredentials parent) : base(parent) { _parent = parent; }
	public override SecurityTokenAuthenticator CreateSecurityTokenAuthenticator(SecurityTokenRequirement tokenRequirement, out SecurityTokenResolver outOfBandTokenResolver)
	{
		if (tokenRequirement == null) { throw new ArgumentNullException(nameof(tokenRequirement)); }
		string tokenType = tokenRequirement.TokenType;
		outOfBandTokenResolver = null;
		if (tokenType == SecurityTokenTypes.UserName &amp;&amp; _parent.UserNameAuthentication.UserNamePasswordValidationMode == UserNamePasswordValidationMode.Custom)
			return new AsyncUserNameSecurityTokenAuthenticator(_parent.UserNameAuthentication.CustomUserNamePasswordValidator);
		else
			return base.CreateSecurityTokenAuthenticator(tokenRequirement, out outOfBandTokenResolver);
	}
}
```
Now I have a custom security token manager, I need a way for this to be used. The method responsible for creating this is ServiceCredentials.CreateSecurityTokenManager(). I need to create a class derived from ServiceCredentials and return an instance of AuthenticationSecurityTokenManager. A little bit of boiler plate code is needed to ensure cloning works. Here's the implementation.
```c#
public class AsyncAuthenticationServiceCredentials : ServiceCredentials
{
	public AsyncAuthenticationServiceCredentials() { }
	private AsyncAuthenticationServiceCredentials(AsyncAuthenticationServiceCredentials other) : base(other) { }
	protected override ServiceCredentials CloneCore() { return new AsyncAuthenticationServiceCredentials(this); }
	public override SecurityTokenManager CreateSecurityTokenManager() { return new AuthenticationSecurityTokenManager(Clone()); }
}
```
To use this derived credentials class, ServiceHost needs to be given a new instance. The property ServiceHost.Credentials is a getter property only so the credentials need to be removed/added via the service description behaviors. In the sample project I provide, I've created a helper class to wire up all the behaviors needed including the extensibility points that I'll use to retrieve the authentication result. This is how you would use the AsyncAuthenticationServiceCredentials if modifying the ServiceHost directly.
```c#
ServiceCredentials creds = serviceHost.Description.Behaviors.Find<ServiceCredentials>();
if (creds != null)
{
	serviceHost.Description.Behaviors.Remove<ServiceCredentials>();
}
creds = new AsyncAuthenticationServiceCredentials();
serviceHost.Description.Behaviors.Add(creds);
```
After all this, we have an asynchronous username/password authenticator which saves a Task in ServiceSecurityContext.AuthorizationContext.Properties. The next step is to retrieve the Task and wait for the result later on in the request pipeline. The only asynchronous extensibility point after the communication channel that we can use is IOperationInvoker. This interface has the methods Invoke, InvokeBegin and InvokeEnd. If your service operation is synchronous, then the Invoke method is called, otherwise InvokeBegin and InvokeEnd are used. I'm only going to include the synchronous implementation here as explaining the code for the asynchronous implementation would likely take a whole series of posts to explain on its own. The asynchronous version is semantically the same, the big difference being if the Task hasn't completed yet, I return from the method to release the thread and continue execution when the Task has completed. You can view the source code for the async version by following the link at the end of this post. My custom IOperationInvoker takes the existing invoker as a constructor parameter and will defer the non-invoke methods to the original invoker.
```c#
public class PasswordValidatorCompletionOperationInvoker : IOperationInvoker
{
	private IOperationInvoker _originalInvoker;
	public PasswordValidatorCompletionOperationInvoker(IOperationInvoker originalInvoker) { _originalInvoker = originalInvoker; }
	public object[] AllocateInputs() { return _originalInvoker.AllocateInputs(); }
	public bool IsSynchronous { get { return _originalInvoker.IsSynchronous; } }
}
```
In the Invoke call, I retrieve the authorization task from the ServiceSecurityContext and get the result. If there is an exception, which is the mechanism used to indicate validation failure, then I call a helper method to throw an appropriate exception.
```c#
public object Invoke(object instance, object[] inputs, out object[] outputs)
{
	var securityContext = ServiceSecurityContext.Current;
	if (securityContext.AuthorizationContext.Properties.ContainsKey("PendingAuthorizationTask"))
	{
		var authTask = securityContext.AuthorizationContext.Properties[PendingAuthorizationTaskKeyName] as Task;
		try { authTask.GetAwaiter().GetResult(); }
		catch (Exception e) { HandleAuthorizationException(e); }
	}
	return _originalInvoker.Invoke(instance, inputs, out outputs);
}
private void HandleAuthorizationException(Exception exception)
{
	var subCode = new FaultCode("InvalidSecurityToken", "http://docs.oasis-open.org/wss/2004/01/oasis-200401-wss-wssecurity-secext-1.0.xsd");
	var senderCode = FaultCode.CreateSenderFaultCode(subCode);
	var reason = new FaultReason("An error occurred when processing the security tokens in the message:" + exception.Message);
	var faultMessage = MessageFault.CreateFault(senderCode, reason);
	throw new FaultException(faultMessage, "http://www.w3.org/2005/08/addressing/soap/fault");
}
```
There are some important behavior differences with WCF to be aware of when using this approach.

1. An authentication failure will throw a different exception on the client. A regular synchronous authenticate will throw a MessageSecurityException on the client whereas an async authentication will cause a FaultException to be thrown.
2. A request pending authentication will acquire a throttle before the authentication has been resolved. This opens a potential for a DOS attack if a client can send a large number of requests without valid credentials and acquire all the throttle quota. This means that this approach isn't suitable for usage outside of a trusted network. In other words, don't use this exposed on the internet without some other throttling mechanism.
3. All other extensibility point implementations will be run against an unauthenticated request potentially consuming resources. For example, a message inspector might log all incoming requests. This will cause all unauthenticated requests to now be logged which might not be desirable.

A similar approach can also be used for the other extensibility points where some long running code needs to be run with some minor modifications. The full code can be found in my GitHub repository and released under the MIT license. Now for the requisite disclaimer: this is a sample for illustrating the topic of this post, this is not production-ready code and hasn't gone through the same rigorous testing that production code does. I wrote some basic tests and it worked in those limited scenarios, but I cannot guarantee that it will work for all scenarios. If you use this code, you are responsible for your own testing and validation. Please let me know if you find a bug or something is missing by opening an issue on GitHub. Even better, feel free to send a pull request with a fix. Also, for simplicity sake it doesn’t have a lot of the error handling and logging which production-level code would.