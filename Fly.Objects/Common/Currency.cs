using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.Objects.Common
{
    public sealed class Currency
    {
        private static List<Currency> s_CurrencyList = new List<Currency>
        {
            new Currency("CNY", "人民币", "¥{0:N2}", 142),
            new Currency("USD", "美元", "${0:N2}", 502),
            new Currency("EUR", "欧元", "€{0:N2}", 300),
            //new Currency("GBP", "英镑", "￡{0:N2}", 303),
            new Currency("JPY", "日元", "¥{0:N2}", 116),
            new Currency("HKD", "港币", "${0:N2}", 110),
            //new Currency("KRW", "韩元", "${0:N2}", 133),
            new Currency("AUD", "澳元", "${0:N2}", 601),
            //new Currency("TWD", "台币", "${0:N2}", 143),
            //new Currency("NLG", "荷兰盾", "${0:N2}", 309),
            //new Currency("NZD", "新西兰元", "${0:N2}", 609)
            //new Currency("MOP", "澳门币", "${0:N2}"),
            //new Currency("PHP", "Philippine Peso", "₱{0:N2}"),
            //new Currency("MYR", "Malaysian Dollar", "${0:N2}"),
            //new Currency("SGD", "Singapore Dollar", "${0:N2}"),
            //new Currency("THP", "Thai Baht", "฿{0:N2}"),
            //new Currency("IDR", "Indonesian Rupiah", "Rp{0:N2}"),
            //new Currency("INR", "Indian Rupee", "₨{0:N2}"),
            //new Currency("SUR", "Russian Ruble", "руб{0:N2}"),
            //new Currency("CAD", "Canadian Dollar", "${0:N2}"),
            //new Currency("MXP", "Mexican Peso", "${0:N2}"),
            //new Currency("AUD", "Australian Dollar", "${0:N2}"),
            //new Currency("NZD", "New Zealand Dollar", "${0:N2}")
        };

        private readonly static string DEFAULT_CURRENCY_CODE = "CNY";
        //private static Func<Currency> s_DefaultCurrencyDelegate = () => Currency.Get(DEFAULT_CURRENCY_CODE);

        public static Currency DefaultCurrency
        {
            get
            {
                //var d = s_DefaultCurrencyDelegate();
                //if (d == null)
                //{
                //    return Currency.Get(DEFAULT_CURRENCY_CODE);
                //}
                //return d;
                return Currency.Get(DEFAULT_CURRENCY_CODE);
            }
        }
        
        public static List<Currency> All
        {
            get
            {
                return new List<Currency>(s_CurrencyList);
            }
        }

        public static Currency Get(string code)
        {
            return All.Find(x => string.Compare(x.Code, code, true) == 0);
        }

        public static bool IsValidCurrencyCode(string code)
        {
            return Get(code) != null;
        }

        //------- Instance Member ------------------------------------

        private Currency(string code, string englishName, string formatting, int customsNumber)
        {
            Code = code;
            Name = englishName;
            Formatting = formatting;
            CustomsNumber = customsNumber;
        }

        /// <summary>
        /// Gets or sets the ISO 4217 currency code
        /// </summary>
        public string Code { get; private set; }

        /// <summary>
        /// Gets or sets the currency name in english
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// 海关的币制代码
        /// </summary>
        public int CustomsNumber { get; private set; }

        /// <summary>
        /// Gets or sets the formatting of currency
        /// </summary>
        public string Formatting { get; private set; }

        /// <summary>
        /// Format the currency to display
        /// </summary>
        /// <param name="moneyAmont">Amount to display</param>
        /// <returns>The formatted text</returns>
        public string Format(object moneyAmont)
        {
            return string.Format(Formatting, moneyAmont);
        }

        public string Symbol
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Formatting))
                {
                    return string.Empty;
                }
                int index = Formatting.IndexOf('{');
                if (index <= 0)
                {
                    return string.Empty;
                }
                return Formatting.Substring(0, index);
            }
        }

        //public decimal GetRateToCurrency(Currency targetCurrency)
        //{
        //    if (this.Code == targetCurrency.Code)
        //    {
        //        return 1.00M;
        //    }
        //    return 1.00M;
        //}

        //public decimal GetRateFromCurrency(Currency sourceCurrency)
        //{
        //    if (this.Code == sourceCurrency.Code)
        //    {
        //        return 1.00M;
        //    }
        //    return 1.00M;
        //}
    }
}
