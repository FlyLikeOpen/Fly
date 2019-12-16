using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.Objects.Common.MessageTask
{
    public class SMSMessage
    {
        public string TemplateId { get; set; }

        public string[] Params { get; set; }

        public static SMSMessage CreateDistributionAcctApproved()
        {
            return new SMSMessage
            {
                TemplateId = "37055",
                Params = new string[0]
            };
        }

        public static SMSMessage CreateDistributionAcctBySelfApproved()
        {
            return new SMSMessage
            {
                TemplateId = "109310",
                Params = new string[0]
            };
        }

        public static SMSMessage CreateWeiDianBillTransferIntoBalance(int year, int month)
        {
            //string n = code + ", " + storeName + "欢迎您的光临，";
            return new SMSMessage
            {
                TemplateId = "34042",
                Params = new string[] { year.ToString(), month.ToString() }
            };
        }

        public static SMSMessage CreateBaoMaBillTransferIntoBalance(int year, int month)
        {
            //string n = code + ", " + storeName + "欢迎您的光临，";
            return new SMSMessage
            {
                TemplateId = "34041",
                Params = new string[] { year.ToString(), month.ToString() }
            };
        }

        public static SMSMessage CreateDistribtionBillTransferIntoBalance(int year, int month)
        {
            //string n = code + ", " + storeName + "欢迎您的光临，";
            return new SMSMessage
            {
                TemplateId = "39045",
                Params = new string[] { year.ToString(), month.ToString() }
            };
        }

        public static SMSMessage CreateUserReferralSellingBillTransferIntoBalance(int year, int month)
        {
            //string n = code + ", " + storeName + "欢迎您的光临，";
            return new SMSMessage
            {
                TemplateId = "34043",
                Params = new string[] { year.ToString(), month.ToString() }
            };
        }
    }
}
