using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceProxy.Tests.Stubs
{
    public interface ITestService
    {
        int Sum(int a, int b);
        Task<int> SumAsync(int a, int b);

        Task<string> Concatenate(params string[] strings);

        DateTime GetDate();

        void DoLots(Guid p1, long p2, DateTime p3, Guid? p4, string p5, bool p6);

        Task<int> ReplyAfter(int timeToReplyInMilliseconds);

        void Fail();
        Task FailAsync();
        IAsyncResult BeginFail(AsyncCallback asyncCallback, object asyncState);
        void EndFail(IAsyncResult asyncResult);
    }

    public interface ITestService2
    {
        IEnumerable<Person> ListPersons(int size);
        Task<IEnumerable<Person>> ListPersonsAsync(int size);
        IAsyncResult BeginListPersons(int size, AsyncCallback asyncCallback, object asyncState);
        IEnumerable<Person> EndListPersons(IAsyncResult asyncResult);

        Person GetPerson(int idx);
        Task<Person> GetPersonAsync(int idx);
        IAsyncResult BeginGetPerson(int idx, AsyncCallback asyncCallback, object asyncState);
        Person EndGetPerson(IAsyncResult asyncResult);
    }

    [Serializable]
    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
    }

    public class TestService : ITestService, ITestService2
    {

        public int Sum(int a, int b)
        {
            return a + b;
        }

        public Task<int> SumAsync(int a, int b)
        {
            return Task.FromResult(Sum(a,b));
        }

        public Task<string> Concatenate(params string[] strings)
        {
            return Task.FromResult(string.Concat(strings));
        }

        public DateTime GetDate()
        {
            return DateTime.Now;
        }

        public void DoLots(Guid p1, long p2, DateTime p3, Guid? p4, string p5, bool p6)
        {
            Console.WriteLine("Do Lots");
        }

        public void Fail()
        {
            throw new Exception("it failed");
        }

        public Task FailAsync()
        {
            return Task.Run(() => this.Fail());
        }

        public IAsyncResult BeginFail(AsyncCallback asyncCallback, object asyncState)
        {
            var task = Task.Factory.StartNew(s => this.Fail(), asyncState);
            task.ContinueWith(t => asyncCallback(t));
            return task;
        }

        public void EndFail(IAsyncResult asyncResult)
        {
            var task = asyncResult as Task;

            if (task.Exception != null)
            {
                throw task.Exception.InnerException;
            }
        }

        public Task<int> ReplyAfter(int timeToReplyInMilliseconds)
        {
            return Task.Delay(timeToReplyInMilliseconds).ContinueWith(t => timeToReplyInMilliseconds);
        }

        private Person[] persons = Enumerable.Range(1, 10)
                                             .Select(i => new Person { Age = i * 10, Name = string.Format("Person {0}", i) })
                                             .ToArray();

        public IEnumerable<Person> ListPersons(int size)
        {
            return size >= 0 ? this.persons.Take(size) : null;
        }

        public Task<IEnumerable<Person>> ListPersonsAsync(int size)
        {
            return Task.FromResult(this.ListPersons(size));
        }

        public IAsyncResult BeginListPersons(int size, AsyncCallback asyncCallback, object asyncState)
        {
            var task = Task.Factory.StartNew((s) => this.ListPersons(size), asyncState);
            task.ContinueWith(t => asyncCallback(t));
            return task;
        }

        public IEnumerable<Person> EndListPersons(IAsyncResult asyncResult)
        {
            var task = asyncResult as Task<IEnumerable<Person>>;
            return task.Result;
        }

        public Person GetPerson(int idx)
        {
            return idx >= 0 ? this.persons[idx] : null;
        }

        public Task<Person> GetPersonAsync(int idx)
        {
            return Task.FromResult(this.GetPerson(idx));
        }

        public IAsyncResult BeginGetPerson(int idx, AsyncCallback asyncCallback, object asyncState)
        {
            var task = Task.Factory.StartNew((s) => this.GetPerson(idx), asyncState);
            task.ContinueWith(t => asyncCallback(t));
            return task;
        }

        public Person EndGetPerson(IAsyncResult asyncResult)
        {
            var task = asyncResult as Task<Person>;
            return task.Result;
        }
    }
}
