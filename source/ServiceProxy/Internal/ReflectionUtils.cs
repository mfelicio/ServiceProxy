using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ServiceProxy.Internal
{
    internal static class ReflectionUtils
    {
        public static bool IsSyncOperation(this MethodInfo m)
        {
            return !typeof(IAsyncResult).IsAssignableFrom(m.ReturnType);
        }

        public static bool IsTaskBasedAsyncOperation(this MethodInfo m)
        {
            return typeof(Task).IsAssignableFrom(m.ReturnType);
        }

        public static bool IsAsyncResultBasedOperation(this MethodInfo m)
        {
            var parameters = m.GetParameters();

            return m.ReturnType == typeof(IAsyncResult)
                && m.Name.StartsWith("Begin")
                && parameters.Length >= 2
                && parameters[parameters.Length - 1].ParameterType == typeof(object)
                && parameters[parameters.Length - 2].ParameterType == typeof(AsyncCallback);
        }

        public static MethodInfo[] GetServiceOperations(Type serviceType)
        {
            //TODO: support wcf contracts, etc, only list [OperationContract] methods
            return serviceType.GetMethods();
        }

        public static Func<object[], object[]> BuildConvertArgumentsFunc(MethodInfo method)
        {
            //TODO: might be needed to convert some types
            return args => args;
        }

        public static bool TryGetEnumerableObjectType(Type resultType, out Type objectType)
        {
            if (resultType != typeof(string) 
                && typeof(IEnumerable).IsAssignableFrom(resultType))
            {
                if (resultType.IsGenericType && typeof(IEnumerable<>) == resultType.GetGenericTypeDefinition())
                {
                    objectType = resultType.GetGenericArguments()[0];
                    return true;
                }
                else
                {
                    throw new NotSupportedException(string.Format("{0} type is currently not supported", resultType.FullName));
                }
            }

            objectType = null;
            return false;
        }

        public static object EnumerableToObject<T>(IEnumerable<T> source)
        {
            if (source is T[] || source is List<T>)
            {
                return source;
            }

            return source.ToArray();
        }

        public static class Sync
        {
            //(object[] args) => (object)instance.Method((ptype0)args[0], (ptype1)args[1], (ptypeN)args[N])
            public static Func<object, object[], object> BuildOperationFunc(MethodInfo method)
            {
                var methodParameters = method.GetParameters();

                //instance
                var instanceArgExpr = Expression.Parameter(typeof(object), "instance");

                //(TService)instance
                var instanceExpr = Expression.Convert(instanceArgExpr, method.DeclaringType);

                //(args)
                var argsExpr = Expression.Parameter(typeof(object[]), "args");

                //(ptype0)args[0], (ptype1)args[1], (ptypeN)args[N])
                var argsToParameterExprList = methodParameters.Select((p, idx) =>
                {
                    var argumentExpr = Expression.ArrayIndex(argsExpr, Expression.Constant(idx));
                    //cast to parameter type
                    return Expression.Convert(argumentExpr, p.ParameterType);
                }).ToArray();

                //instance.Method((ptype0)args[0], (ptype1)args[1], (ptypeN)args[N])
                var invocationExpr = Expression.Call(instanceExpr, method, argsToParameterExprList);

                Expression invocationResultExpr;
                if (method.ReturnType == typeof(void))
                {
                    invocationResultExpr = Expression.Block(
                        invocationExpr,
                        Expression.Constant(null, typeof(object)));
                }
                else
                {
                    //(object)instance.Method((ptype0)args[0], (ptype1)args[1], (ptypeN)args[N])

                    Type enumerableObjectType;
                    if (ReflectionUtils.TryGetEnumerableObjectType(method.ReturnType, out enumerableObjectType))
                    {
                        var convertEnumerableMethod = typeof(ReflectionUtils).GetMethod("EnumerableToObject")
                                                                             .MakeGenericMethod(enumerableObjectType);
                        invocationResultExpr = Expression.Call(null, convertEnumerableMethod, invocationExpr);
                    }
                    else
                    {
                        invocationResultExpr = Expression.Convert(invocationExpr, typeof(object));
                    }
                }

                //(args) => instance.Method((ptype0)args[0], (ptype1)args[1], (ptypeN)args[N])
                var lambdaExpr = Expression.Lambda<Func<object, object[], object>>(invocationResultExpr, instanceArgExpr, argsExpr);
                return lambdaExpr.Compile();
            }
        }

        public static class AsyncResult
        {
            public static Func<object, object[], AsyncCallback, object, IAsyncResult> BuildBeginOperationFunc(MethodInfo method)
            {
                //(object[] args, AsyncCallback callback, object state) => (object)instance.Method((ptype0)args[0], (ptype1)args[1], (ptypeN)args[N], asyncCallback, asyncState)

                var methodParameters = method.GetParameters();

                //instance
                var instanceArgExpr = Expression.Parameter(typeof(object), "instance");

                //(TService)instance
                var instanceExpr = Expression.Convert(instanceArgExpr, method.DeclaringType);

                //(args)
                var argsExpr = Expression.Parameter(typeof(object[]), "args");
                var asyncCallbackExpr = Expression.Parameter(typeof(AsyncCallback), "asyncCallback");
                var asyncStateExpr = Expression.Parameter(typeof(object), "asyncState");

                //(ptype0)args[0], (ptype1)args[1], (ptypeN)args[N])   até length -2 para nao apanhar o asyncCallback e asyncState
                var argsExprList = methodParameters.Take(methodParameters.Length - 2).Select((p, idx) =>
                {
                    var argumentExpr = Expression.ArrayIndex(argsExpr, Expression.Constant(idx));
                    //cast para o tipo do param
                    return Expression.Convert(argumentExpr, p.ParameterType);
                }).OfType<Expression>().ToList();

                argsExprList.Add(asyncCallbackExpr);
                argsExprList.Add(asyncStateExpr);

                //instance.Method((ptype0)args[0], (ptype1)args[1], (ptypeN)args[N], asyncCallback, asyncState)
                var invocationExpr = Expression.Call(instanceExpr, method, argsExprList.ToArray());

                //(args, asyncCallback, asyncState) => instance.Method((ptype0)args[0], (ptype1)args[1], (ptypeN)args[N], asyncCallback, asyncState)
                var lambdaExpr = Expression.Lambda<Func<object, object[], AsyncCallback, object, IAsyncResult>>(invocationExpr, instanceArgExpr, argsExpr, asyncCallbackExpr, asyncStateExpr);
                return lambdaExpr.Compile();
            }

            public static Func<object, IAsyncResult, object> BuildEndOperationFunc(MethodInfo method)
            {
                //instance
                var instanceArgExpr = Expression.Parameter(typeof(object), "instance");

                //(TService)instance
                var instanceExpr = Expression.Convert(instanceArgExpr, method.DeclaringType);

                //asyncResult
                var parameterExpr = Expression.Parameter(typeof(IAsyncResult), "asyncResult");
                //instance.EndMethod(asyncResult)
                var invocationExpr = Expression.Call(instanceExpr, method, parameterExpr);
                //if void: call EndMethod and return null
                //else: (object)instance.EndMethod(asyncResult) //handles deferred IEnumerable<T> too
                Expression invocationResultExpr;
                if (method.ReturnType == typeof(void))
                {
                    invocationResultExpr = Expression.Block(
                        invocationExpr,
                        Expression.Constant(null, typeof(object)));
                }
                else
                {
                    Type enumerableObjectType;
                    if (ReflectionUtils.TryGetEnumerableObjectType(method.ReturnType, out enumerableObjectType))
                    {
                        var convertEnumerableMethod = typeof(ReflectionUtils).GetMethod("EnumerableToObject")
                                                                             .MakeGenericMethod(enumerableObjectType);
                        invocationResultExpr = Expression.Call(null, convertEnumerableMethod, invocationExpr);
                    }
                    else
                    {
                        invocationResultExpr = Expression.Convert(invocationExpr, typeof(object));
                    }
                }

                //asyncResult => (object)instance.EndMethod(asyncResult)
                var lambdaExpr = Expression.Lambda<Func<object, IAsyncResult, object>>(invocationResultExpr, instanceArgExpr, parameterExpr);
                return lambdaExpr.Compile();
            }

            public static MethodInfo GetEndMethod(MethodInfo beginMethod)
            {
                var serviceType = beginMethod.DeclaringType;

                var expectedEndMethodName = string.Format("End{0}", beginMethod.Name.Substring(5));
                var endMethod = serviceType.GetMethod(expectedEndMethodName);

                if (endMethod == null)
                {
                    throw new InvalidOperationException(string.Format("Expected to find a {0} method to be used with {1} method", expectedEndMethodName, beginMethod.Name));
                }

                return endMethod;
            }
        }

        public static class Tasks
        {
            public static Func<object, object[], Task<object>> BuildOperationFunc(MethodInfo method)
            {
                var methodParameters = method.GetParameters();

                //instance
                var instanceArgExpr = Expression.Parameter(typeof(object), "instance");

                //(TService)instance
                var instanceExpr = Expression.Convert(instanceArgExpr, method.DeclaringType);

                //(args)
                var argsExpr = Expression.Parameter(typeof(object[]), "args");

                //(ptype0)args[0], (ptype1)args[1], (ptypeN)args[N])
                var argsToParameterExprList = methodParameters.Select((p, idx) =>
                {
                    var argumentExpr = Expression.ArrayIndex(argsExpr, Expression.Constant(idx));
                    //cast to parameter type
                    return Expression.Convert(argumentExpr, p.ParameterType);
                }).ToArray();

                //instance.Method((ptype0)args[0], (ptype1)args[1], (ptypeN)args[N])
                var invocationExpr = Expression.Call(instanceExpr, method, argsToParameterExprList);

                MethodInfo convertTaskMethod;

                if (method.ReturnType == typeof(Task))
                {
                    convertTaskMethod = typeof(ReflectionUtils.Tasks).GetMethod("ConvertTask", BindingFlags.Static | BindingFlags.NonPublic);
                }
                else
                {
                    //check if it returns an enumerable or an object
                    var taskResultType = method.ReturnType.GenericTypeArguments[0];

                    Type enumerableObjectType;
                    if (ReflectionUtils.TryGetEnumerableObjectType(taskResultType, out enumerableObjectType))
                    {
                        convertTaskMethod = typeof(ReflectionUtils.Tasks).GetMethod("ConvertTaskEnumerable", BindingFlags.Static | BindingFlags.NonPublic)
                                                                       .MakeGenericMethod(enumerableObjectType);
                    }
                    else
                    {
                        convertTaskMethod = typeof(ReflectionUtils.Tasks).GetMethod("ConvertTaskResult", BindingFlags.Static | BindingFlags.NonPublic)
                                                                   .MakeGenericMethod(taskResultType);
                    }
                }

                var invocationResultExpr = Expression.Call(null, convertTaskMethod, invocationExpr);

                //(args) => instance.Method((ptype0)args[0], (ptype1)args[1], (ptypeN)args[N])
                var lambdaExpr = Expression.Lambda<Func<object, object[], Task<object>>>(invocationResultExpr, instanceArgExpr, argsExpr);
                return lambdaExpr.Compile();
            }

            private static Task<object> ConvertTask(Task task)
            {
                return ConvertTaskInternal(task, () => null);
            }

            private static Task<object> ConvertTaskResult<T>(Task<T> task)
            {
                return ConvertTaskInternal(task, () => task.Result);
            }

            private static Task<object> ConvertTaskEnumerable<T>(Task<IEnumerable<T>> task)
            {
                return ConvertTaskInternal(task, () => ReflectionUtils.EnumerableToObject(task.Result));
            }

            private static Task<object> ConvertTaskInternal(Task task, Func<object> getResultFunc)
            {
                var tcs = new TaskCompletionSource<object>(task.AsyncState);

                task.ContinueWith(t =>
                {
                    if (!t.IsCanceled)
                    {
                        if (t.Exception == null)
                        {
                            tcs.SetResult(getResultFunc());
                        }
                        else
                        {
                            tcs.SetException(t.Exception.InnerException);
                        }
                    }
                    else
                    {
                        tcs.SetCanceled();
                    }
                });

                return tcs.Task;
            }

            public static TaskCompletionSource<T> CreateTaskCompletionSource<T>(Task<ResponseData> task, Func<ResponseData, T> getResultFunc, object state = null)
            {
                var tcs = new TaskCompletionSource<T>(state);

                task.ContinueWith(t =>
                {
                    if (t.IsCanceled)
                    {
                        tcs.TrySetCanceled();
                        return;
                    }

                    var response = t.Result;
                    if (response.Exception != null)
                    {
                        tcs.TrySetException(response.Exception);
                    }
                    else
                    {
                        tcs.TrySetResult(getResultFunc(response));
                    }
                });

                return tcs;
            }
        }

    }
}
