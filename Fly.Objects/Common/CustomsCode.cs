using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.Objects.Common
{
    public class CustomsCode
    {
        private static List<CustomsCode> s_CustomsCodeList = new List<CustomsCode>
        {
            new CustomsCode("CQ", "重庆海关"),
            new CustomsCode("CD", "成都海关")
        };

        public static List<CustomsCode> All
        {
            get
            {
                return new List<CustomsCode>(s_CustomsCodeList);
            }
        }

        public static CustomsCode Get(string code)
        {
            return All.Find(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));
        }

        public static bool IsValidCode(string code)
        {
            return Get(code) != null;
        }

        //------- Instance Member ------------------------------------

        private CustomsCode(string code, string name)
        {
            Code = code;
            Name = name;
        }

        public string Code { get; private set; }

        public string Name { get; private set; }
    }
}
