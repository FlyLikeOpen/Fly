using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Fly.WebService.MvcFilters
{
    // 用来做幂等性控制的，防止重试导致的重复处理
    public class EnsureRequestIdempotencyAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            string msgId = filterContext.RequestContext.HttpContext.Request.Headers["X-ESB-MessageID"] + filterContext.RequestContext.HttpContext.Request.Headers["X-ESB-SubscriberID"];
            if (string.IsNullOrWhiteSpace(msgId))
            {
                return;
            }
            if (MsgExisted(msgId)) // 判断消息在缓存中是否已经存在了
            {
                // 如果消息已存在，说明之前已经处理过了，这里直接返回http code 200
                filterContext.Result = new JsonResult
                {
                    Data = new { error = false },
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
            }
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            string msgId = filterContext.RequestContext.HttpContext.Request.Headers["X-ESB-MessageID"] + filterContext.RequestContext.HttpContext.Request.Headers["X-ESB-SubscriberID"];
            if (string.IsNullOrWhiteSpace(msgId))
            {
                return;
            }
            if (filterContext.Exception == null) // 说明没有异常
            {
                // 把消息加入到缓存中
                MsgHandled(msgId);
            }
        }

        private bool MsgExisted(string msgId)
        {
            var obj = Fly.Framework.Common.Cache.GetLocalCache("esb-" + msgId);
            return obj != null;
        }

        private void MsgHandled(string msgId)
        {
            Fly.Framework.Common.Cache.SetLocalCache("esb-" + msgId, new object(), true, 60 * 24 * 7);
        }
    }
}