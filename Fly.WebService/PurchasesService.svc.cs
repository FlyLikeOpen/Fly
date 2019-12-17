using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using IBB360.Framework.Common;
using IBB360.WMSLocal.APIs.Purchases;
using IBB360.WMSLocal.Objects.Purchases;
using IBB360.WMSLocal.WebService.Common;

namespace IBB360.WMSLocal.WebService
{
    // 注意: 使用“重构”菜单上的“重命名”命令，可以同时更改代码、svc 和配置文件中的类名“PurchasesService”。
    // 注意: 为了启动 WCF 测试客户端以测试此服务，请在解决方案资源管理器中选择 PurchasesService.svc 或 PurchasesService.svc.cs，然后开始调试。
    public class PurchasesService : IPurchasesService
    {

        //报关申报事件，创建入库申请
        public void ImportDeclarationSubmited(RestfulMessage msg)
        {
            var request = msg.GetData<InStockRequest>(SerializerType.Xml);
            Api<IInStockRequestApi>.Instance.Create(request);
        }
        //报关完成申报，修改入库申请状态为待理货
        public void ImportDeclarationProcessed(RestfulMessage msg)
        {
            var requestId = Guid.Parse(msg.Content);
            Api<IInStockRequestApi>.Instance.UpdateStatus(requestId, InStockRequestStatus.WaitingCount,InStockRequestChangeType.CustomsDeclared);

        }
        //入库申请作废事件
        public void ImportDeclarationAbandoned(RestfulMessage msg)
        {
            var requestId = Guid.Parse(msg.Content);
            Api<IInStockRequestApi>.Instance.UpdateStatus(requestId,InStockRequestStatus.Abandoned,InStockRequestChangeType.Abandoned);
        }

        //理货结果确认事件
        public void InStockCountResultConfirmed(RestfulMessage msg)
        {
            var requestId = Guid.Parse(msg.Content);
            Api<IInStockRequestApi>.Instance.UpdateStatus(requestId, InStockRequestStatus.WaitingCountConfirmation,InStockRequestChangeType.CountResultConfirmed);
        }

        //理货结果被否决事件
        public void InStockCountResultRejected(RestfulMessage msg)
        {
            var requestId = Guid.Parse(msg.Content);
            Api<IInStockRequestApi>.Instance.UpdateStatus(requestId, InStockRequestStatus.WaitingCount, InStockRequestChangeType.CountResultRejected);
        }
    }
}
