using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fly.Framework.Common
{
    public interface IContext
    {
        bool HasLogined { get; }

        Guid UserId { get; }

        string SessionId { get; }

        int SalesChannelNumber { get; }

        Guid? SalesAgentId { get; }

		Guid? VendorId { get; }

        Guid? MerchantId { get; }

        string ClientIP { get; }

        string RequestUserAgent { get; }

        DeviceType DeviceType { get; }

        Geography ClientLocation { get; }

        /// <summary>
        /// 将当前的 IContext 实例附加到指定的 IContext 实例
        /// </summary>
        /// <param name="owner">要附加的IContext实例</param>
        void Attach(IContext owner);

        object this[string key]
        {
            get;
            set;
        }
    }
}
