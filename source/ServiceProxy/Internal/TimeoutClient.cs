using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceProxy.Internal
{
    public class TimeoutClient : IClient
    {
        private readonly IClient client;
        private readonly TimeSpan timeout;

        private readonly ResponseData timeoutResponse;

        public TimeoutClient(IClient client, TimeSpan timeout)
        {
            this.client = client;
            this.timeout = timeout;
            this.timeoutResponse = new ResponseData(new TimeoutException());
        }

        public Task<ResponseData> Request(RequestData request, CancellationToken _)
        {
            var cancellation = new CancellationTokenSource(this.timeout);

            var responseTask = this.client.Request(request, cancellation.Token);

            var completion = new TaskCompletionSource<ResponseData>();

            //when the token reaches timeout, tries to set the timeoutResponse as the result
            //if the responseTask already completed, this is ignored
            cancellation.Token.Register(() => completion.TrySetResult(this.timeoutResponse));

            //when the responseTask completes, tries to apply its exception/result properties as long as the timeout isn't reached
            responseTask.ContinueWith(t =>
            {
                if (!cancellation.IsCancellationRequested)
                {
                    if (responseTask.Exception != null)
                    {
                        completion.TrySetException(responseTask.Exception.InnerException);
                    }
                    else
                    {
                        completion.TrySetResult(responseTask.Result);
                    }
                }
            });

            return completion.Task;
        }
    }
}
