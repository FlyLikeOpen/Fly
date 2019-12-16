using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Fly.UI.Common
{
    public interface IHtmlTable<T>
    {
        IHtmlTable<T> AddColumn(string captionText, Func<T, string> cellDataGetter, string width = null, string captionClassName = null, string cellClassName = null);

        IHtmlString Render(IEnumerable<T> dataList);
    }

    public static class HtmlTable
    {
        private class MyHtmlTable<T> : IHtmlTable<T>
        {
            private class HtmlTableColumn<K>
            {
                public string CaptionText { get; private set; }

                public string CaptionClassName { get; private set; }

                public string CellClassName { get; private set; }

                public string Width { get; private set; }

                public Func<K, string> CellDataGetter { get; private set; }

                public HtmlTableColumn(string captionText, Func<K, string> cellDataGetter, string width = null, string captionClassName = null, string cellClassName = null)
                {
                    CaptionText = captionText;
                    Width = width;
                    CellDataGetter = cellDataGetter;
                    CaptionClassName = captionClassName;
                    CellClassName = cellClassName;
                }
            }

            private List<HtmlTableColumn<T>> m_ColumnList = new List<HtmlTableColumn<T>>();
            private string m_SubjectText;
            private string m_SubjectIconClass;
            private int? m_MaxHeight;
            private string m_EmptyText;

            public MyHtmlTable(string subjectText, string subjectIconClass, int? maxHeight, string emptyText)
            {
                m_SubjectText = subjectText;
                m_SubjectIconClass = subjectIconClass;
                m_MaxHeight = maxHeight;
                m_EmptyText = emptyText;
            }

            public IHtmlTable<T> AddColumn(string captionText, Func<T, string> cellDataGetter, string width = null, string captionClassName = null, string cellClassName = null)
            {
                if (cellDataGetter == null)
                {
                    throw new ArgumentNullException("cellDataGetter");
                }
                m_ColumnList.Add(new HtmlTableColumn<T>(captionText, cellDataGetter, width, captionClassName, cellClassName));
                return this;
            }

            public IHtmlString Render(IEnumerable<T> dataList)
            {
                if (m_ColumnList.Count <= 0)
                {
                    return MvcHtmlString.Empty;
                }
                StringBuilder html = new StringBuilder();
                html.Append("<div class=\"ec-table\">");
                if (string.IsNullOrWhiteSpace(m_SubjectText) == false || string.IsNullOrWhiteSpace(m_SubjectIconClass) == false)
                {
                    string icon = string.IsNullOrWhiteSpace(m_SubjectIconClass) ? string.Empty : "<i class=\"fa fa-star\"></i> ";
                    html.AppendFormat("<div class=\"table-subject\">{0}{1}</div>", icon, m_SubjectText);
                }
                html.Append("<div class=\"table-caption\"><table><tr>");
                foreach (var col in m_ColumnList)
                {
                    string sty = string.IsNullOrWhiteSpace(col.Width) ? string.Empty : string.Format(" style=\"width:{0}\"", col.Width);
                    string css = string.IsNullOrWhiteSpace(col.CaptionClassName) ? string.Empty : string.Format(" class=\"{0}\"", col.CaptionClassName);
                    html.AppendFormat("<td{0}{1}>{2}</td>", sty, css, col.CaptionText);
                }
                html.Append("<td class=\"scrollbar\"></td>");
                html.Append("</tr></table></div>");
                string scrollClass = string.Empty;
                string heightStyle = string.Empty;
                if(m_MaxHeight.HasValue)
                {
                    scrollClass = " scroll-enable";
                    heightStyle = string.Format(" style=\"max-height: {0}px;\"", m_MaxHeight.Value);
                }
                html.AppendFormat("<div class=\"table-body{0}\"{1}>", scrollClass, heightStyle);
                if (dataList != null && dataList.Count() > 0)
                {
                    html.Append("<table>");
                    foreach(var data in dataList)
                    {
                        html.Append("<tr>");
                        foreach (var col in m_ColumnList)
                        {
                            string sty = string.IsNullOrWhiteSpace(col.Width) ? string.Empty : string.Format(" style=\"width:{0}\"", col.Width);
                            string css = string.IsNullOrWhiteSpace(col.CellClassName) ? string.Empty : string.Format(" class=\"{0}\"", col.CaptionClassName);
                            html.AppendFormat("<td{0}{1}>{2}</td>", sty, css, col.CellDataGetter == null ? string.Empty : col.CellDataGetter(data));
                        }
                        html.Append("</tr>");
                    }
                    html.Append("</table>");
                }
                else
                {
                    string txt = m_EmptyText == null ? "暂无符合条件记录" : m_EmptyText;
                    const int defaultHeight = 60;
                    int h = m_MaxHeight == null ? defaultHeight : (m_MaxHeight.Value > defaultHeight ? defaultHeight : m_MaxHeight.Value);
                    html.AppendFormat("<div class=\"empty\" style=\"line-height:{0}px;\">{1}</div>", h, txt);
                }
                html.Append("</div></div>");
                return MvcHtmlString.Create(html.ToString());
            }
        }

        public static IHtmlTable<T> Create<T>(string subjectText = null, string subjectIconClass = null, int? maxHeight = null, string emptyText = null)
        {
            return new MyHtmlTable<T>(subjectText, subjectIconClass, maxHeight, emptyText);
        }

        public static IEnumerable<DataRow> ToRowList(this DataTable table)
        {
            return ToRowList(table.Rows);
        }

        public static IEnumerable<DataRow> ToRowList(this DataRowCollection rows)
        {
            if (rows == null)
            {
                return null;
            }
            List<DataRow> list = new List<DataRow>(rows.Count);
            foreach (DataRow row in rows)
            {
                list.Add(row);
            }
            return list;
        }
    }
}