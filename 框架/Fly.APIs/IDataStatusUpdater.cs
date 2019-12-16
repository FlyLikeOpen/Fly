using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.APIs
{
    public interface IDataStatusUpdater
    {
        void Enable(Guid id);

        void Disable(Guid id);

        void Delete(Guid id);
    }
}
