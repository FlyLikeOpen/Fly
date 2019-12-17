using org.in2bits.MyXls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.Framework.Excel
{
    public static class ExcelFileExporter
    {
        public static byte[] CreateFile(ExcelSheetMetadata sheet)
        {
            return CreateFile(new ExcelSheetMetadata[] { sheet });
        }

        public static byte[] CreateFile(IEnumerable<ExcelSheetMetadata> sheetList)
        {
            if (sheetList == null || sheetList.Count() <= 0)
            {
                throw new ArgumentNullException("sheetList");
            }
            XlsDocument xls = new XlsDocument();
            int i = 0;
            foreach (var sheet in sheetList)
            {
                sheet.BuildSheet(xls, i);
                i++;
            }
            return xls.Bytes.ByteArray;
        }

        public static void SaveFile(string path, ExcelSheetMetadata sheet)
        {
            SaveFile(path, new ExcelSheetMetadata[] { sheet });
        }

        public static void SaveFile(string path, IEnumerable<ExcelSheetMetadata> sheetList)
        {
            if (sheetList == null || sheetList.Count() <= 0)
            {
                throw new ArgumentNullException("sheetList");
            }
            XlsDocument xls = new XlsDocument();
            int i = 0;
            foreach (var sheet in sheetList)
            {
                sheet.BuildSheet(xls, i);
                i++;
            }
            var dp = Path.GetDirectoryName(path);
            if (Directory.Exists(dp) == false)
            {
                Directory.CreateDirectory(dp);
            }
            xls.FileName = Path.GetFileName(path);
            xls.Save(dp, true);
        }
    }
}
