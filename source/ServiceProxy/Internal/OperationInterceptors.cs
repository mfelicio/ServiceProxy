using Castle.DynamicProxy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ServiceProxy.Internal
{
    internal abstract class OperationInterceptor
    {
        protected readonly IClient client;

        protected readonly string service;
        protected readonly string operation;

        public OperationInterceptor(Type contractType, MethodInfo method, IClient client)
        {
            this.service = contractType.AssemblyQualifiedName;
            this.operation = method.Name;
            this.client = client;
        }

        public abstract void Intercept(IInvocation invocation);
    }

    internal class SynchronousOperationInterceptor : OperationInterceptor
    {
        public SynchronousOperationInterceptor(Type contractType, MethodInfo method, IClient client)
            : base(contractType, method, client)
        {

        }

        public override void Intercept(IInvocation invocation)
        {
            var responseTask = this.client.Request(new RequestData(base.service, base.operation, invocation.Arguments));
            var response = responseTask.Result;

            if (response.Exception == null)
            {
                invocation.ReturnValue = response.Data;
            }
            else
            {
                throw response.Exception;
            }
        }
    }

    internal class TaskOperationInterceptor : OperationInterceptor
    {
        private readonly Func<Task<ResponseData>, object> processResponseTask;

        public TaskOperationInterceptor(Type contractType, MethodInfo method, IClient client)
            : base(contractType, method, client)
        {
            var returnType = method.ReturnType == typeof(Task) ?
                                                        null :
                                                        method.ReturnType.GetGenericArguments()[0];

            this.processResponseTask = this.GenerateProcessResponseTask(returnType);
        }

        private Func<Task<ResponseData>, object> GenerateProcessResponseTask(Type returnType)
        {

            var responseTaskExpr = Expression.Parameter(typeof(Task<ResponseData>), "responseTask");

            MethodInfo convertMethod;

            if (returnType != null)
            {
                convertMethod = typeof(TaskOperationInterceptor).GetMethod("ConvertGenericResponseTaskResult", BindingFlags.NonPublic | BindingFlags.Static)
                                                                .MakeGenericMethod(returnType);
            }
            else
            {
                convertMethod = typeof(TaskOperationInterceptor).GetMethod("ConvertResponseTaskResult", BindingFlags.NonPublic | BindingFlags.Static);
            }

            var processResponseExpr = Expression.Call(null, convertMethod, responseTaskExpr);

            var funcExpr = Expression.Lambda<Func<Task<ResponseData>, object>>(processResponseExpr, responseTaskExpr);
            return funcExpr.Compile();
        }

        private static Task<T> ConvertGenericResponseTaskResult<T>(Task<ResponseData> task)
        {
            return ReflectionUtils.Tasks.CreateTaskCompletionSource<T>(task, r => (T)r.Data).Task;
        }

        private static Task ConvertResponseTaskResult(Task<ResponseData> task)
        {
            return ReflectionUtils.Tasks.CreateTaskCompletionSource<object>(task, r => null).Task;
        }

        public override void Intercept(IInvocation invocation)
        {
            var responseTask = this.client.Request(new RequestData(base.service, base.operation, invocation.Arguments));

            invocation.ReturnValue = this.processResponseTask(responseTask);
        }
    }

    internal class BeginAsyncResultOperationInterceptor : OperationInterceptor
    {
        public BeginAsyncResultOperationInterceptor(Type contractType, MethodInfo method, IClient client)
            : base(contractType, method, client)
        {

        }

        public override void Intercept(IInvocation invocation)
        {
            var responseTask = this.client.Request(new RequestData(base.service, base.operation, invocation.Arguments.Take(invocation.Arguments.Length -2).ToArray()));

            var asyncCallback = invocation.Arguments[invocation.Arguments.Length - 2] as AsyncCallback;
            var asyncState = invocation.Arguments[invocation.Arguments.Length - 1];

            responseTask.ContinueWith(t => asyncCallback(t));

            invocation.ReturnValue = responseTask;
        }
    }

    internal class EndAsyncResultOperationInterceptor : OperationInterceptor
    {
        public EndAsyncResultOperationInterceptor(Type contractType, MethodInfo method, IClient client)
            : base(contractType, method, client)
        {

        }

        public override void Intercept(IInvocation invocation)
        {
            var asyncResult = invocation.Arguments[0] as IAsyncResult;

            var responseTask = asyncResult as Task<ResponseData>;

            var response = responseTask.Result;

            if (response.Exception == null)
            {
                invocation.ReturnValue = response.Data;
            }
            else
            {
                throw response.Exception;
            }
        }
    }

}
