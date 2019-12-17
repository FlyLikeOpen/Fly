using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using IBB360.WMSLocal.WebService.Common;

namespace IBB360.WMSLocal.WebService
{
    // 注意: 使用“重构”菜单上的“重命名”命令，可以同时更改代码和配置文件中的接口名“IPurchasesService”。
    [ServiceContract]
    public interface IPurchasesService
    {
        [OperationContract]
        void ImportDeclarationSubmited(RestfulMessage msg);

        [OperationContract]
        void ImportDeclarationProcessed(RestfulMessage msg);

        [OperationContract]
        void ImportDeclarationAbandoned(RestfulMessage msg);

        [OperationContract]
        void InStockCountResultConfirmed(RestfulMessage msg);

        [OperationContract]
        void InStockCountResultRejected(RestfulMessage msg);
    }
}
