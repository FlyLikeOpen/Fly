using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fly.Framework.Common
{
    internal interface ICookiePersist
    {
        void Save(string cookieName, string cookieValue, Dictionary<string, string> parameters);

        string Get(string cookieName, Dictionary<string, string> parameters);

        void Remove(string cookieName, Dictionary<string, string> parameters);
    }
}
