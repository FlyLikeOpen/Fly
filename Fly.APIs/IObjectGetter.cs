using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.APIs
{
    public interface IObjectGetter<T> where T : class, new()
    {
        T Get(Guid id);

        IList<T> Get(IEnumerable<Guid> idList);
    }
}
