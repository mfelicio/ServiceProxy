ServiceProxy
============

ServiceProxy is a lightweight asynchronous proxy for .NET that allows you to use service contracts in a request/reply manner with your favorite messaging framework.

## How does it work?

### Client side

On the client side ServiceProxy creates interface proxies for your service contracts, intercepts all service requests, converts them to well defined messages and sends them using a messaging framework. The created proxies are cached for the next requests.

### Server side

On the server side ServiceProxy handles the messages received from your messaging framework, resolves/invokes the real service implementation and sends the response back. It uses the service locator pattern to resolve services, so you may use any IoC container.

The first time a service call is made for a given contract, ServiceProxy generates, compiles and caches delegates that invoke your service operations and integrate seamlessly with the ServiceProxy request/reply interface. This means it is as fast as a direct method call to your services, since after the first execution no reflection will be done.

ServiceProxy doesn't require any configuration.

## Getting started

The quickest way to get started with ServiceProxy is by using the [NuGet package][serviceproxy-nuget]. You may also use one of the bundled request/reply messaging frameworks available in the source code [ServiceProxy.Redis][serviceproxy.redis-github] and [ServiceProxy.Zmq][serviceproxy.zmq-github]. 

The latest stable version is 1.0.1. For more information, visit the [Release notes][serviceproxy-releasenotes-github] page.

## Why use ServiceProxy

Using ServiceProxy your code can be decoupled from the messaging framework you're using and from the ServiceProxy framework itself. If your code uses Dependency Injection and you use a modern IoC container, you can have your code depend on your service interfaces and have the IoC container create the proxies using the ServiceClientFactory. It doesn't intend to replace your messaging framework since ServiceProxy by itself has no messaging capabilities. It just has a simple and asynchronous request/reply model that is supposed to integrate seamlessly with any messaging framework. However, if you don't use one already you can give [ServiceProxy.Redis][serviceproxy.redis-github] and [ServiceProxy.Zmq][serviceproxy.zmq-github] a try.

## Contributing

You can contribute by creating a ServiceProxy.[YourFavoriteMessageBus] and submit your code to github. Make sure to make a reference to this project and I'll make sure it is listed here as well.

### Examples

I'll try to create real examples and add them to the source code. If you would like to post examples let me know.

### Issues

If you find any issues please post them on the [issues][serviceproxy-issues-github] page. 

## Dependencies

ServiceProxy uses [Castle.Core][castle.core-github] DynamicProxy internally to generate client side interface proxies.

## License

ServiceProxy is licensed under the [MIT][serviceproxy-license] license.

[serviceproxy-nuget]: http://packages.nuget.org/Packages/ServiceProxy
[serviceproxy.redis-github]: https://github.com/mfelicio/ServiceProxy/tree/master/source/ServiceProxy.Redis
[serviceproxy.zmq-github]: https://github.com/mfelicio/ServiceProxy/tree/master/source/ServiceProxy.Zmq
[serviceproxy-issues-github]: https://github.com/mfelicio/ServiceProxy/issues
[serviceproxy-releasenotes-github]: https://github.com/mfelicio/ServiceProxy/blob/master/CHANGELOG.md
[castle.core-github]: https://github.com/castleproject/Core
[serviceproxy-license]: http://opensource.org/licenses/mit-license.php