using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ServiceProxy.Internal
{
    internal abstract class OperationInvoker
    {
        private readonly Func<object[], object[]> convertArgumentsFunc;

        public OperationInvoker(MethodInfo method)
        {
            this.convertArgumentsFunc = ReflectionUtils.BuildConvertArgumentsFunc(method);

            this.Name = method.Name;
        }

        public Task<object> InvokeAsync(object serviceInstance, object[] arguments)
        {
            var args = this.convertArgumentsFunc(arguments);

            return this.DoInvokeAsync(serviceInstance, args);
        }

        public string Name { get; private set; }

        protected abstract Task<object> DoInvokeAsync(object serviceInstance, object[] arguments);
    }

    internal class SynchronousOperationInvoker : OperationInvoker
    {
        private readonly Func<object, object[], object> operationFunc;

        public SynchronousOperationInvoker(MethodInfo method)
            : base(method)
        {
            this.operationFunc = ReflectionUtils.Sync.BuildOperationFunc(method);
        }

        protected override Task<object> DoInvokeAsync(object serviceInstance, object[] arguments)
        {
            return Task.Run(() => this.operationFunc(serviceInstance, arguments));
        }
    }

    internal class TaskBasedOperationInvoker : OperationInvoker
    {
        private readonly Func<object, object[], Task<object>> operationFunc;

        public TaskBasedOperationInvoker(MethodInfo method)
            : base(method)
        {
            this.operationFunc = ReflectionUtils.Tasks.BuildOperationFunc(method);
        }

        protected override Task<object> DoInvokeAsync(object serviceInstance, object[] arguments)
        {
            return this.operationFunc(serviceInstance, arguments);
        }
    }

    internal class AsyncResultBasedOperation : OperationInvoker
    {
        private readonly Func<object, object[], AsyncCallback, object, IAsyncResult> beginFunc;
        private readonly Func<object, IAsyncResult, object> endFunc;

        public AsyncResultBasedOperation(MethodInfo method)
            : base(method)
        {
            this.beginFunc = ReflectionUtils.AsyncResult.BuildBeginOperationFunc(method);

            var endMethod = ReflectionUtils.AsyncResult.GetEndMethod(method);
            this.endFunc = ReflectionUtils.AsyncResult.BuildEndOperationFunc(endMethod);
        }

        protected override Task<object> DoInvokeAsync(object serviceInstance, object[] arguments)
        {
            Func<object[], AsyncCallback, object, IAsyncResult> beginFunc =
                (args, asyncCallback, asyncState) => this.beginFunc(serviceInstance, args, asyncCallback, asyncState);

            Func<IAsyncResult, object> endFunc = asyncResult => this.endFunc(serviceInstance, asyncResult);

            return Task<object>.Factory.FromAsync<object[]>(beginFunc, endFunc, arguments, null);
        }
    }
}
