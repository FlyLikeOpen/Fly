using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.Objects.Common.MessageTask
{
    public enum MessageSendType
    {
        /// <summary>
        /// 手机短信消息
        /// </summary>
        [Description("手机短信")]
        SMS = 0,

        /// <summary>
        /// 微信模板消息
        /// </summary>
        //[Description("微信消息")]
        //WeiXin = 1,

        /// <summary>
        /// 电子邮件
        /// </summary>
        [Description("电子邮件")]
        Email = 2,


        /// <summary>
        /// 企业微信
        /// </summary>
        [Description("企业微信")]
        WorkWeChat = 3
    }
}
