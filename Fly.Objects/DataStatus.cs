using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.Objects
{
    /// <summary>
    /// 通用的数据记录状态枚举
    /// 有以下实体使用该枚举做数据状态标识：（请持续添加新用到的实体类型）
    /// Brand
    /// Category
    /// Property
    /// PropertyValue
    /// </summary>
    public enum DataStatus
    {
        [Description("已禁用")]
        Disabled = 0,

        [Description("已启用")]
        Enabled = 1,

        [Description("已删除")]
        Deleted = -1
    }
}
