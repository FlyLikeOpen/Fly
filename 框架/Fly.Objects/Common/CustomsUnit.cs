using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.Objects.Common
{
    /// <summary>
    /// 海关的计量单位代码
    /// </summary>
    public class CustomsUnit
    {
        private static List<CustomsUnit> s_SalesChannelList = new List<CustomsUnit>
        {
            new CustomsUnit("1","台"),
            new CustomsUnit("2","座"),
            new CustomsUnit("3","辆"),
            new CustomsUnit("4","艘"),
            new CustomsUnit("5","架"),
            new CustomsUnit("6","套"),
            new CustomsUnit("7","个"),
            new CustomsUnit("8","只"),
            new CustomsUnit("9","头"),
            new CustomsUnit("10","张"),
            new CustomsUnit("11","件"),
            new CustomsUnit("12","支"),
            new CustomsUnit("13","枝"),
            new CustomsUnit("14","根"),
            new CustomsUnit("15","条"),
            new CustomsUnit("16","把"),
            new CustomsUnit("17","块"),
            new CustomsUnit("18","卷"),
            new CustomsUnit("19","副"),
            new CustomsUnit("20","片"),
            new CustomsUnit("21","组"),
            new CustomsUnit("22","份"),
            new CustomsUnit("23","幅"),
            new CustomsUnit("25","双"),
            new CustomsUnit("26","对"),
            new CustomsUnit("27","棵"),
            new CustomsUnit("28","株"),
            new CustomsUnit("29","井"),
            new CustomsUnit("30","米"),
            new CustomsUnit("31","盘"),
            new CustomsUnit("32","平方米"),
            new CustomsUnit("33","立方米"),
            new CustomsUnit("34","筒"),
            new CustomsUnit("35","千克"),
            new CustomsUnit("36","克"),
            new CustomsUnit("37","盆"),
            new CustomsUnit("38","万个"),
            new CustomsUnit("39","具"),
            new CustomsUnit("40","百副"),
            new CustomsUnit("41","百支"),
            new CustomsUnit("42","百把"),
            new CustomsUnit("43","百个"),
            new CustomsUnit("44","百片"),
            new CustomsUnit("45","刀"),
            new CustomsUnit("46","疋"),
            new CustomsUnit("47","公担"),
            new CustomsUnit("48","扇"),
            new CustomsUnit("49","百枝"),
            new CustomsUnit("50","千只"),
            new CustomsUnit("51","千块"),
            new CustomsUnit("52","千盒"),
            new CustomsUnit("53","千枝"),
            new CustomsUnit("54","千个"),
            new CustomsUnit("55","亿支"),
            new CustomsUnit("56","亿个"),
            new CustomsUnit("57","万套"),
            new CustomsUnit("58","千张"),
            new CustomsUnit("59","万张"),
            new CustomsUnit("60","千伏安"),
            new CustomsUnit("61","千瓦"),
            new CustomsUnit("62","千瓦时"),
            new CustomsUnit("63","千升"),
            new CustomsUnit("67","英尺"),
            new CustomsUnit("70","吨"),
            new CustomsUnit("71","长吨"),
            new CustomsUnit("72","短吨"),
            new CustomsUnit("73","司马担"),
            new CustomsUnit("74","司马斤"),
            new CustomsUnit("75","斤"),
            new CustomsUnit("76","磅"),
            new CustomsUnit("77","担"),
            new CustomsUnit("78","英担"),
            new CustomsUnit("79","短担"),
            new CustomsUnit("80","两"),
            new CustomsUnit("81","市担"),
            new CustomsUnit("83","盎司"),
            new CustomsUnit("84","克拉"),
            new CustomsUnit("85","市尺"),
            new CustomsUnit("86","码"),
            new CustomsUnit("88","英寸"),
            new CustomsUnit("89","寸"),
            new CustomsUnit("95","升"),
            new CustomsUnit("96","毫升"),
            new CustomsUnit("97","英加仑"),
            new CustomsUnit("98","美加仑"),
            new CustomsUnit("99","立方英尺"),
            new CustomsUnit("101","立方尺"),
            new CustomsUnit("110","平方码"),
            new CustomsUnit("111","平方英尺"),
            new CustomsUnit("112","平方尺"),
            new CustomsUnit("115","英制马力"),
            new CustomsUnit("116","公制马力"),
            new CustomsUnit("118","令"),
            new CustomsUnit("120","箱"),
            new CustomsUnit("121","批"),
            new CustomsUnit("122","罐"),
            new CustomsUnit("123","桶"),
            new CustomsUnit("124","扎"),
            new CustomsUnit("125","包"),
            new CustomsUnit("126","箩"),
            new CustomsUnit("127","打"),
            new CustomsUnit("128","筐"),
            new CustomsUnit("129","罗"),
            new CustomsUnit("130","匹"),
            new CustomsUnit("131","册"),
            new CustomsUnit("132","本"),
            new CustomsUnit("133","发"),
            new CustomsUnit("134","枚"),
            new CustomsUnit("135","捆"),
            new CustomsUnit("136","袋"),
            new CustomsUnit("139","粒"),
            new CustomsUnit("140","盒"),
            new CustomsUnit("141","合"),
            new CustomsUnit("142","瓶"),
            new CustomsUnit("143","千支"),
            new CustomsUnit("144","万双"),
            new CustomsUnit("145","万粒"),
            new CustomsUnit("146","千粒"),
            new CustomsUnit("147","千米"),
            new CustomsUnit("148","千英尺"),
            new CustomsUnit("149","百万贝可"),
            new CustomsUnit("163","部")
        };

        public static List<CustomsUnit> All
        {
            get
            {
                return new List<CustomsUnit>(s_SalesChannelList);
            }
        }

        public static CustomsUnit Get(string code)
        {
            return All.Find(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsValidCode(string code)
        {
            return Get(code) != null;
        }

        //------- Instance Member ------------------------------------

        private CustomsUnit(string code, string name)
        {
            Code = code.PadLeft(3, '0');
            Name = name;
        }

        public string Code { get; private set; }

        public string Name { get; private set; }
    }
}