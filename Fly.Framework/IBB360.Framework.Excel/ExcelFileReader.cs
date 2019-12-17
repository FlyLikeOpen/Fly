using org.in2bits.MyXls;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.OleDb;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Fly.Framework.Excel
{
    /*
    public class ExcelFileReader
    {
        private XlsDocument m_XlsDoc;

        public ExcelFileReader(string excelFilePath)
        {
            m_XlsDoc = new XlsDocument(excelFilePath);
        }

        public ExcelFileReader(Stream stream)
        {
            m_XlsDoc = new XlsDocument(stream);
        }

        public int SheetCount
        {
            get { return m_XlsDoc == null || m_XlsDoc.Workbook == null || m_XlsDoc.Workbook.Worksheets == null ? 0 : m_XlsDoc.Workbook.Worksheets.Count; }
        }

        private Worksheet GetWorksheet(int sheetIndex)
        {
            if (m_XlsDoc == null || m_XlsDoc.Workbook == null || m_XlsDoc.Workbook.Worksheets == null || sheetIndex >= m_XlsDoc.Workbook.Worksheets.Count || sheetIndex < 0)
            {
                return null;
            }
            return m_XlsDoc.Workbook.Worksheets[sheetIndex];
        }

        private Worksheet GetWorksheet(string sheetName)
        {
            if (m_XlsDoc == null || m_XlsDoc.Workbook == null || m_XlsDoc.Workbook.Worksheets == null)
            {
                return null;
            }
            return m_XlsDoc.Workbook.Worksheets[sheetName];
        }

        public int GetRowCount(int sheetIndex)
        {
            var sheet = GetWorksheet(sheetIndex);
            if (sheet == null || sheet.Rows == null)
            {
                return 0;
            }
            return sheet.Rows.Count;
        }

        public int GetRowCount(string sheetName)
        {
            var sheet = GetWorksheet(sheetName);
            if (sheet == null || sheet.Rows == null)
            {
                return 0;
            }
            return sheet.Rows.Count;
        }

        public SheetRows GetRows(int sheetIndex)
        {
            var sheet = GetWorksheet(sheetIndex);
            if (sheet == null || sheet.Rows == null)
            {
                return null;
            }
            return new SheetRows(sheet.Rows);
        }

        public SheetRows GetRows(string sheetName)
        {
            var sheet = GetWorksheet(sheetName);
            if (sheet == null || sheet.Rows == null)
            {
                return null;
            }
            return new SheetRows(sheet.Rows);
        }

        public object Read(int sheetIndex, int rowIndex, int columnIndex)
        {
            Worksheet sheet = GetWorksheet(sheetIndex);
            if (sheet == null || sheet.Rows == null || rowIndex < 1 || rowIndex > sheet.Rows.Count)
            {
                return null;
            }
            if (columnIndex < 0)
            {
                return null;
            }
            if (rowIndex < 0)
            {
                return null;
            }
            columnIndex++;
            rowIndex++;
            var row = sheet.Rows[(ushort)rowIndex];
            if (row == null)
            {
                return null;
            }
            var cell = row.GetCell((ushort)columnIndex);
            if (cell == null)
            {
                return null;
            }
            return cell.Value;
        }

        public T Read<T>(int sheetIndex, int rowIndex, int columnIndex)
        {
            var obj = Read(sheetIndex, rowIndex, columnIndex);
            return DataConvertor.GetValue<T>(obj);
        }
    }

    public class SheetRows : IEnumerable<SheetRow>, IEnumerable
    {
        private Rows m_Rows;
        private Dictionary<string, int> m_HeaderIndex = new Dictionary<string, int>();
        internal SheetRows(Rows rows)
        {
            m_Rows = rows;
            if (rows.RowExists(1))
            {
                SheetRow sr = new SheetRow(rows[1], null);
                foreach (var r in sr.Cells)
                {
                    string ht = r != null && r.Value != null ? r.Value.ToString().Trim() : string.Empty;
                    if (string.IsNullOrWhiteSpace(ht) == false && m_HeaderIndex.ContainsKey(ht) == false)
                    {
                        m_HeaderIndex.Add(ht, r.Column);
                    }
                }
            }
        }

        public IEnumerator<SheetRow> GetEnumerator()
        {
            return new SheetRowsEnumerator(m_Rows.GetEnumerator(), m_HeaderIndex);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new SheetRowsEnumerator(m_Rows.GetEnumerator(), m_HeaderIndex);
        }

        private class SheetRowsEnumerator : IEnumerator<SheetRow>, IEnumerator
        {
            private IEnumerator<Row> m_RowEnumerator;
            private Dictionary<string, int> m_HIndex = new Dictionary<string, int>();

            public SheetRowsEnumerator(IEnumerator<Row> rowEnumerator, Dictionary<string, int> hIndex)
            {
                m_RowEnumerator = rowEnumerator;
                m_HIndex = hIndex;
            }

            public SheetRow Current
            {
                get
                {
                    var row = m_RowEnumerator.Current;
                    if (row == null)
                    {
                        return null;
                    }
                    return new SheetRow(row, m_HIndex);
                }
            }

            public void Dispose()
            {
                m_RowEnumerator.Dispose();
            }

            object IEnumerator.Current
            {
                get { return this.Current; }
            }

            public bool MoveNext()
            {
                return m_RowEnumerator.MoveNext();
            }

            public void Reset()
            {
                m_RowEnumerator.Reset();
            }
        }

        public int Count
        {
            get
            {
                if (m_Rows == null)
                {
                    return 0;
                }
                return m_Rows.Count;
            }
        }

        public bool RowExists(int rowIndex)
        {
            return m_Rows.RowExists((ushort)(rowIndex + 1));
        }

        public uint MaxRow { get { return m_Rows.MaxRow; } }

        public uint MinRow { get { return m_Rows.MinRow; } }

        public SheetRow this[int rowIndex] { get { return new SheetRow(m_Rows[(ushort)(rowIndex + 1)], m_HeaderIndex); } }
    }

    public class SheetRow
    {
        private Row m_Row;
        private Dictionary<int, Cell> m_CellsDictionary;
        private Dictionary<string, int> m_HeaderIndex;

        internal IEnumerable<Cell> Cells { get { return m_CellsDictionary.Values; } }

        internal SheetRow(Row row, Dictionary<string, int> headerIndex)
        {
            m_Row = row;
            m_HeaderIndex = headerIndex;
            m_CellsDictionary = new Dictionary<int, Cell>(m_Row.CellCount * 2);
            for (ushort i = m_Row.MinCellCol; i <= m_Row.MaxCellCol; i++)
            {
                if(m_Row.CellExists(i))
                {
                    var cell = m_Row.CellAtCol(i);
                    m_CellsDictionary.Add(cell.Column, cell);
                }
            }
        }

        public object this[int cellIdx] { get { return GetValue(cellIdx); } }

        public object this[string headerText] { get { return GetValue(headerText); } }

        public bool CellExists(int cellIdx)
        {
            //return m_Row.CellExists((ushort)(cellIdx + 1));
            return m_CellsDictionary.ContainsKey(cellIdx + 1);
        }

        public object GetValue(string headerText)
        {
            if (string.IsNullOrWhiteSpace(headerText))
            {
                return null;
            }
            headerText = headerText.Trim();
            int i;
            if (m_HeaderIndex.TryGetValue(headerText, out i))
            {
                return GetValue(i - 1);
            }
            return null;
        }

        public object GetValue(int cellIdx)
        {
            //if (m_Row == null)
            //{
            //    return null;
            //}

            //var c = m_Row.GetCell((ushort)(cellIdx + 1));
            //if (c == null)
            //{
            //    return null;
            //}
            //return c.Value;
            if (m_CellsDictionary == null)
            {
                return null;
            }
            Cell cell;
            if (m_CellsDictionary.TryGetValue(cellIdx + 1, out cell) == false || cell == null)
            {
                return null;
            }
            return cell.Value;
        }

        public int CellCount
        {
            get
            {
                //return m_Row.CellCount;
                return m_Row.MaxCellCol;
            }
        }

        public T GetValue<T>(string headerText)
        {
            var obj = GetValue(headerText);
            return DataConvertor.GetValue<T>(obj);
        }

        public T GetValue<T>(int cellIdx)
        {
            var obj = GetValue(cellIdx);
            return DataConvertor.GetValue<T>(obj);
        }
    }

    internal static class DataConvertor
    {
        private static bool IsGenericEnum(Type type)
        {
            return (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                    && type.GetGenericArguments() != null
                    && type.GetGenericArguments().Length == 1 && type.GetGenericArguments()[0].IsEnum);
        }

        public static T GetValue<T>(object data)
        {
            if (data == null || data == DBNull.Value)
            {
                return default(T);
            }
            try
            {
                if (typeof(T) == data.GetType() || typeof(T).IsAssignableFrom(data.GetType()))
                {
                    return (T)((object)data);
                }
                else if (typeof(T) == typeof(string))
                {
                    return (T)((object)data.ToString().Trim());
                }
                else if (data is string && data.ToString().Trim().Length <= 0)
                {
                    return default(T);
                }
                else if (typeof(T) == typeof(char) || typeof(T) == typeof(char?))
                {
                    return (T)((object)Convert.ToChar(data));
                }
                else if (typeof(T) == typeof(sbyte) || typeof(T) == typeof(sbyte?))
                {
                    return (T)((object)Convert.ToSByte(data));
                }
                else if (typeof(T) == typeof(byte) || typeof(T) == typeof(byte?))
                {
                    return (T)((object)Convert.ToByte(data));
                }
                else if (typeof(T) == typeof(short) || typeof(T) == typeof(short?))
                {
                    return (T)((object)Convert.ToInt16(data));
                }
                else if (typeof(T) == typeof(ushort) || typeof(T) == typeof(ushort?))
                {
                    return (T)((object)Convert.ToUInt16(data));
                }
                else if (typeof(T) == typeof(int) || typeof(T) == typeof(int?))
                {
                    return (T)((object)Convert.ToInt32(data));
                }
                else if (typeof(T) == typeof(uint) || typeof(T) == typeof(uint?))
                {
                    return (T)((object)Convert.ToUInt32(data));
                }
                else if (typeof(T) == typeof(long) || typeof(T) == typeof(long?))
                {
                    return (T)((object)Convert.ToInt64(data));
                }
                else if (typeof(T) == typeof(ulong) || typeof(T) == typeof(ulong?))
                {
                    return (T)((object)Convert.ToUInt64(data));
                }
                else if (typeof(T) == typeof(DateTime) || typeof(T) == typeof(DateTime?))
                {
                    return (T)((object)Convert.ToDateTime(data));
                }
                else if (typeof(T) == typeof(decimal) || typeof(T) == typeof(decimal?))
                {
                    return (T)((object)Convert.ToDecimal(data));
                }
                else if (typeof(T) == typeof(float) || typeof(T) == typeof(float?))
                {
                    return (T)((object)Convert.ToSingle(data));
                }
                else if (typeof(T) == typeof(double) || typeof(T) == typeof(double?))
                {
                    return (T)((object)Convert.ToDouble(data));
                }
                else if (typeof(T) == typeof(bool) || typeof(T) == typeof(bool?))
                {
                    return (T)((object)Convert.ToBoolean(data));
                }
                else if (typeof(T) == typeof(Guid) || typeof(T) == typeof(Guid?))
                {
                    return (T)((object)Guid.Parse(data.ToString()));
                }
                else if (typeof(T).IsEnum || IsGenericEnum(typeof(T)))
                {
                    Type destinationType;
                    if (typeof(T).IsEnum)
                    {
                        destinationType = typeof(T);
                    }
                    else
                    {
                        destinationType = typeof(T).GetGenericArguments()[0];
                    }
                    return (T)Enum.Parse(destinationType, data.ToString(), false);
                }
                return (T)data;
            }
            catch (Exception ex)
            {
                string msg = "Can't cast '" + data.GetType().FullName + "' to '" + typeof(T).FullName + "'. Values is '" + data.ToString() + "'";
                throw new InvalidCastException(msg, ex);
            }
        }
    }
     * */

    public class ExcelFileReader : IDisposable
    {
        //private XlsDocument m_XlsDoc;
        private string m_FilePath;
        private bool m_NeedDeleted = false;

        public ExcelFileReader(Stream stream, string fileExtensionName = ".xls")
        {
            if (string.IsNullOrWhiteSpace(fileExtensionName))
            {
                throw new ArgumentNullException("fileExtensionName");
            }
            fileExtensionName = fileExtensionName.Trim();
            if (fileExtensionName.StartsWith(".") == false)
            {
                fileExtensionName = "." + fileExtensionName;
            }
            string newPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "tmp\\" + Guid.NewGuid() + fileExtensionName);
            string directory = Path.GetDirectoryName(newPath);
            if (Directory.Exists(directory) == false)
            {
                Directory.CreateDirectory(directory);
            }
            using (StreamWriter sw = new StreamWriter(newPath))
            {
                stream.CopyTo(sw.BaseStream);
                sw.Flush();
                sw.Close();
            }
            m_NeedDeleted = true;
            m_FilePath = newPath;
        }

        public ExcelFileReader(string excelFilePath)
        {
            m_NeedDeleted = false;
            string d = Path.GetPathRoot(excelFilePath);
            if (string.IsNullOrWhiteSpace(d))
            {
                excelFilePath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, excelFilePath);
            }
            //m_XlsDoc = new XlsDocument(excelFilePath);
            m_FilePath = excelFilePath;
        }

        //public DataTable ReadSheet(int sheetIndex, bool firstRowAsColumnHeader)
        //{
        //    if (m_XlsDoc == null || m_XlsDoc.Workbook == null || m_XlsDoc.Workbook.Worksheets == null || sheetIndex >= m_XlsDoc.Workbook.Worksheets.Count || sheetIndex < 0)
        //    {
        //        return null;
        //    }
        //    var sheet = m_XlsDoc.Workbook.Worksheets[sheetIndex];
        //    if (sheet == null)
        //    {
        //        return null;
        //    }
        //    return ReadSheet(sheet.Name, firstRowAsColumnHeader);
        //}

        public string[] GetSheetsName()
        {
            string connStr = string.Format(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties='Excel 12.0 Xml;HDR=Yes;IMEX=1'", m_FilePath);
            List<string> list = new List<string>();
            using (OleDbConnection conn = new OleDbConnection(connStr))
            {
                conn.Open();
                DataTable tableStructure = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                if (tableStructure == null || tableStructure.Rows == null || tableStructure.Rows.Count <= 0)
                {
                    return new string[0];
                }
                foreach (DataRow dr in tableStructure.Rows)
                {
                    string sheetName = dr[2].ToString().Trim();
                    list.Add(sheetName);
                }
                conn.Close();
            }
            return list.ToArray();
        }

        public DataTable ReadSheet(int sheetIndex, bool firstRowAsColumnHeader)
        {
            string connStr = string.Format(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties='Excel 12.0 Xml;HDR={1};IMEX=1'", m_FilePath, firstRowAsColumnHeader ? "Yes" : "No");
            DataSet dataset = new DataSet();
            using (OleDbConnection conn = new OleDbConnection(connStr))
            {
                conn.Open();
                DataTable tableStructure = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables, null);
                int totalSheetCount = tableStructure == null || tableStructure.Rows == null ? 0 : tableStructure.Rows.Count;
                if (totalSheetCount <= sheetIndex)
                {
                    throw new ApplicationException(string.Format("该Excel总共只有{0}个Sheet，所以无法找到index为{1}的Sheet", totalSheetCount, sheetIndex));
                }
                DataRow dr = tableStructure.Rows[sheetIndex];
                string sheetName = dr[2].ToString().Trim();
                string sql = "SELECT * FROM [" + sheetName + "]";
                OleDbDataAdapter OleDaExcel = new OleDbDataAdapter(sql, conn);
                OleDaExcel.Fill(dataset, sheetName);
                conn.Close();
            }
            if (dataset.Tables == null || dataset.Tables.Count <= 0)
            {
                return null;
            }
            return dataset.Tables[0];
        }

        public DataTable ReadSheet(string sheetName, bool firstRowAsColumnHeader)
        {
            string connStr = string.Format(@"Provider=Microsoft.ACE.OLEDB.12.0;Data Source={0};Extended Properties='Excel 12.0 Xml;HDR=Yes;IMEX=1'", m_FilePath, firstRowAsColumnHeader ? "Yes" : "No");
            DataSet dataset = new DataSet();
            using (OleDbConnection conn = new OleDbConnection(connStr))
            {
                conn.Open();
                string sql = "SELECT * FROM [" + sheetName + "$]";
                OleDbDataAdapter OleDaExcel = new OleDbDataAdapter(sql, conn);
                OleDaExcel.Fill(dataset, sheetName);
                conn.Close();
            }
            if (dataset.Tables == null || dataset.Tables.Count <= 0)
            {
                return null;
            }
            return dataset.Tables[0];
        }

        public void Dispose()
        {
            if (m_NeedDeleted && File.Exists(m_FilePath))
            {
                File.Delete(m_FilePath);
            }
        }
    }
}
