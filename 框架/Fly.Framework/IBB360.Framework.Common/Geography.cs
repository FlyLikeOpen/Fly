using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.Framework.Common
{
    public class Geography
    {
        public float Latitude { get; set; }

        public float Longitude { get; set; }
    }

    public enum GeographyType
    {
        /// <summary>
        /// 由GPS设备所获取到的经纬度
        /// </summary>
        GPS,

        /// <summary>
        /// 百度地图上获取的经纬度
        /// </summary>
        BaiduMap,

        /// <summary>
        /// 腾讯地图、谷歌地图、高德地图上获取的经纬度
        /// </summary>
        QQ_Google_AmapMap
    }

    public class QueryArea
    {
        public float Latitude { get; set; }

        public float Longitude { get; set; }

        public float? Radius { get; set; }
    }
}
