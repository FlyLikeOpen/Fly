using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Fly.Framework.Common
{
    public static class HttpResponseExtensions
    {
        public static void RedirectToUrl(this HttpResponse response, string url)
        {
            response.Clear(); // 这里是关键，清除在返回前已经设置好的标头信息，这样后面的跳转才不会报错
            response.BufferOutput = true; // 设置输出缓冲
            if (!response.IsRequestBeingRedirected) // 在跳转之前做判断,防止重复
            {
                response.Redirect(url, true);
            }
        }

        public static void RedirectToUrl(this HttpResponseBase response, string url)
        {
            response.Clear(); // 这里是关键，清除在返回前已经设置好的标头信息，这样后面的跳转才不会报错
            response.BufferOutput = true; // 设置输出缓冲
            if (!response.IsRequestBeingRedirected) // 在跳转之前做判断,防止重复
            {
                response.Redirect(url, true);
            }
        }
    }
}
