using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.APIs
{
    public interface ICodeAndIdMapper
    {
        Guid? GetId(string code);

        string GetCode(Guid id);

        IDictionary<string, Guid> GetId(IEnumerable<string> codeList);

        IDictionary<Guid, string> GetCode(IEnumerable<Guid> idList);
    }
}
