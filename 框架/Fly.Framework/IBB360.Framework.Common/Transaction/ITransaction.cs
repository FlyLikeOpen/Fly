using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fly.Framework.Common
{
    public interface ITransaction : IDisposable
    {
        void Complete();
    }
}
