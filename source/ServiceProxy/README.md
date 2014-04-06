ServiceProxy
============

This page contains a brief explanation of how ServiceProxy core components work.

## Client side

On the client side there are only two interfaces that you must deal with: IClient and IServiceClientFactory.

The IClient interface represents your messaging client adapter, see interface below.

```c#
public interface IClient
{
    Task<ResponseData> Request(RequestData request, CancellationToken token);
}
```

For each request sent it returns a task that will complete when the response result is received. The RequestData object contains the required information to do a remote service call: service name, operation name and arguments.

```c#
[Serializable]
public class RequestData
{
    public RequestData(string service, string operation, object[] arguments)
    {
        this.Service = service;
        this.Operation = operation;
        this.Arguments = arguments;
    }

    public string Service { get; private set; }
    public string Operation { get; private set; }
    public object[] Arguments { get; private set; }
}
```

To create service proxies you use the IServiceClientFactory. The actual implementation generates proxies using Castle.Core.DynamicProxy, intercepts all calls and uses an IClient instance to make requests / return responses. You can also specify timeouts when creating proxies, that will be used for all requests made by that proxy.

```c#
public interface IServiceClientFactory
{
    TService CreateServiceClient<TService>(int? timeout = null) where TService : class;
    object CreateServiceClient(Type serviceType, int? timeout = null);
}
```

## Server side

On the server side the interfaces you need to use are the IService and IServiceFactory.

```c#
public interface IServiceFactory
{
    IService CreateService<TService>() where TService : class;

    IService CreateService(string serviceName);

    IService CreateService(Type serviceType);
}

public interface IService
{
    Task<ResponseData> Process(RequestData requestData);
}
```

The IService interface represents a proxy to your actual service instance. You can use whatever mechanism your messaging frameworks provides you to handle the messages you sent from the IClient instance. Then you can use the IServiceFactory to create IService instances that will process the requests using the data received from your message handler. The ResponseData object contains the required information to send response results using your messaging framework to the IClient instance that originated the request. 

```c#
[Serializable]
public class ResponseData
{
    public ResponseData(object data)
    {
        this.Data = data;
        this.Exception = null;
    }

    public ResponseData(Exception error)
    {
        this.Data = null;
        this.Exception = error;
    }

    public object Data { get; private set; }
    public Exception Exception { get; private set; }
}
```
When the IServiceFactory creates an IService for a given service Type, it will dynamically generate, compile and cache delegates that invoke methods on your service instances. These delegates don't use reflection, so it is as fast as having a method that checks what operation it has to call based on the RequestData and directly invokes your service methods.

The IServiceFactory doesn't know how to create your service instances, and won't use reflection using the Activator class. Instead it relies on the Service Locator pattern using the IDependencyResolver interface, which lets you decide how your services will be created. The recommended approach will be to create an adapter for your IoC container and Resolve your services.

```c#
public interface IDependencyResolver
{
    object Resolve(Type type);
}
```

You don't have to implement the IServiceClientFactory nor the IServiceFactory nor IService's. ServiceProxy already has implementations for them, that's the core for this project. However you should always depend on these interfaces and not the actual implementations, so that the core components of ServiceProxy can evolve / change and keep your code compatible with it.

The implementation of ServiceProxy components was carefully done having performance in mind. ServiceProxy aims to be a very lightweight proxy, so most internal components are created once, cached and retrieved using O(1) calls.