using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.APIs
{
    public interface INumberAndIdMapper
    {
        Guid? GetId(long idNumber);

        long? GetIdNumber(Guid id);

        IDictionary<long, Guid> GetId(IEnumerable<long> idNumberList);

        IDictionary<Guid, long> GetIdNumber(IEnumerable<Guid> idList);
    }
}
