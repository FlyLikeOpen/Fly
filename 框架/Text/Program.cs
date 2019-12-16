using IBB360.Framework.Common;
using IBB360.WMSLocal.UI.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Text
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            ContextManager.SetContextType(typeof(WebContext));
            FilterConfig.RegisterGlobalFilters();
            BundleConfig.RegisterBundles();
            Application.Run(new Form1());
        }
    }
}
