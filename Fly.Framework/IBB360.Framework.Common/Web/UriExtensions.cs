using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Fly.Framework.Common
{
    public static class UriExtensions
    {
        public static string GetAbsolutePath(this Uri url)
        {
            string path = url.AbsolutePath;
            string first = HttpContext.Current.Items["url_first_section"] as string;
            if (string.IsNullOrWhiteSpace(first) == false)
            {
                path = "/" + first + path;
            }
            return path;
        }

        public static string GetAbsoluteUri(this Uri url)
        {
            return string.Format("{0}://{1}{2}{3}", url.Scheme, url.Authority, url.GetAbsolutePath(), url.Query);
        }

        public static string GetLocalPath(this Uri url)
        {
            string path = url.LocalPath;
            string first = HttpContext.Current.Items["url_first_section"] as string;
            if (string.IsNullOrWhiteSpace(first) == false)
            {
                path = "/" + first + path;
            }
            return path;
        }

        public static string GetExtLeftPart(this Uri url, UriPartial p)
        {
            if (p == UriPartial.Authority)
            {
                string p1 = url.GetLeftPart(p);
                string first = HttpContext.Current.Items["url_first_section"] as string;
                if (string.IsNullOrWhiteSpace(first) == false)
                {
                    p1 = p1 + "/" + first;
                }
                return p1;
            }
            else if (p == UriPartial.Path)
            {
                string p1 = url.GetAbsolutePath();
                return string.Format("{0}://{1}{2}", url.Scheme, url.Authority, p1);
            }
            else if (p == UriPartial.Query)
            {
                return url.GetAbsoluteUri();
            }
            else
            {
                return url.GetLeftPart(p);
            }
        }
    }
}
