using org.in2bits.MyXls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.Framework.Excel
{
    public abstract class ExcelSheetMetadata
    {
        internal abstract void BuildSheet(XlsDocument xls, int index);

        public static ExcelSheetMetadata<T> CreateSheet<T>(IEnumerable<T> dataList)
        {
            return CreateSheet<T>(null, dataList);
        }

        public static ExcelSheetMetadata<T> CreateSheet<T>(string name, IEnumerable<T> dataList)
        {
            return new ExcelSheetMetadata<T>(name, dataList);
        }
    }

    public class ExcelSheetMetadata<T> : ExcelSheetMetadata
    {
        private readonly ushort COLUMN_WIDTH_SCALE = 256;

        private List<ExcelColumnMetadata<T>> m_ColumnList = new List<ExcelColumnMetadata<T>>();
        private string SheetName { get; set; }
        private IEnumerable<T> DataList { get; set; }

        //public IReadOnlyList<ExcelColumnMetadata<T>> Columns { get { return m_ColumnList; } }

        internal ExcelSheetMetadata(string name, IEnumerable<T> dataList)
        {
            SheetName = name;
            DataList = dataList;
        }
        
        public ExcelSheetMetadata<T> AddColumn(string title, Func<T, object> dataGetter, Type dataType = null,
            HorizAlignments? horizontalAlignment = null, VertiAlignments? verticalAlignment = null, bool? hasBorder = null, int? width = null)
        {
            m_ColumnList.Add(new ExcelColumnMetadata<T>
            {
                DataGetter = dataGetter,
                ColumnTitle = title,
                HorizontalAlignment = horizontalAlignment,
                VerticalAlignment = verticalAlignment,
                HasBorder = hasBorder,
                Width = width
            });
            return this;
        }

        public ExcelSheetMetadata<T> AddColumn(ExcelColumnMetadata<T> columnMetadata)
        {
            m_ColumnList.Add(new ExcelColumnMetadata<T>
            {
                DataGetter = columnMetadata.DataGetter,
                ColumnTitle = columnMetadata.ColumnTitle,
                HorizontalAlignment = columnMetadata.HorizontalAlignment,
                VerticalAlignment = columnMetadata.VerticalAlignment,
                HasBorder = columnMetadata.HasBorder,
                Width = columnMetadata.Width
            });
            return this;
        }

        internal override void BuildSheet(XlsDocument xls, int index)
        {
            string n;
            if (string.IsNullOrWhiteSpace(SheetName) == false)
            {
                n = SheetName;
            }
            else
            {
                n = "Sheet" + (index + 1);
            }
            Worksheet sheet = xls.Workbook.Worksheets.Add(n);
            if (m_ColumnList == null || m_ColumnList.Count <= 0)
            {
                return;
            }
            SetColumnsWidth(xls, sheet, m_ColumnList);
            int rowIndex = 1;
            int colIndex = 0;
            Cells cells = sheet.Cells;
            foreach (var col in m_ColumnList)
            {
                colIndex++;
                cells.Add(1, colIndex, col.ColumnTitle, col.GetSheetTitleXF(xls));
            }
            if (DataList == null || DataList.Count() <= 0)
            {
                return;
            }
            foreach (T entity in DataList)
            {
                rowIndex++;
                colIndex = 0;
                foreach (var col in m_ColumnList)
                {
                    colIndex++;
                    object v = col.DataGetter(entity);
                    Type dtype = col.DataType != null ? col.DataType : (v == null ? typeof(object) : v.GetType());
                    cells.Add(rowIndex, colIndex, v, col.GetDataCellXF(xls, dtype));
                }
            }
        }

        private void SetColumnsWidth(XlsDocument xls, Worksheet worksheet, List<ExcelColumnMetadata<T>> columns)
        {
            for (int i = 0; i < columns.Count; i++)
            {
                ColumnInfo col;
                if (columns[i].Width == null)
                {
                    // 如果是DateTime类型的列，设默认长度20；
                    Type t = columns[i].DataType;
                    if (t != null && (t == typeof(DateTime) || t == typeof(DateTime?)))
                    {
                        col = new ColumnInfo(xls, worksheet);
                        col.Width = (ushort)(20 * this.COLUMN_WIDTH_SCALE);
                        col.ColumnIndexStart = (ushort)(i);
                        col.ColumnIndexEnd = (ushort)(i);
                        worksheet.AddColumnInfo(col);
                    }
                    continue;
                }
                col = new ColumnInfo(xls, worksheet);
                col.Width = (ushort)(columns[i].Width.Value * this.COLUMN_WIDTH_SCALE);
                col.ColumnIndexStart = (ushort)(i);
                col.ColumnIndexEnd = (ushort)(i);
                worksheet.AddColumnInfo(col);
            }
        }
    }
}
