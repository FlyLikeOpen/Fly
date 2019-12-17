using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fly.Framework.Common
{
    internal interface ICookieEncryption
    {
        string EncryptCookie<T>(T obj, Dictionary<string, string> parameters);

        T DecryptCookie<T>(string cookieValue, Dictionary<string, string> parameters);
    }
}
