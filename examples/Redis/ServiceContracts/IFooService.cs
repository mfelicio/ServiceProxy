using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceContracts
{
    public interface IFooService
    {
        Foo GetFoo(int id);
        Task<Foo> GetFooAsync(int id);

        IEnumerable<Foo> ListFoos();
        Task<IEnumerable<Foo>> ListFoosAsync();

        void UpdateFoo(Foo foo);
        Task UpdateFooAsync(Foo foo);
    }
}
