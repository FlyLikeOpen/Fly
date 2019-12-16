using org.in2bits.MyXls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Fly.Framework.Excel
{
    public class ExcelColumnMetadata<T>
    {
        private readonly ushort FONT_HEIGHT_SCALE = 20;

        public Func<T, object> DataGetter { get; set; }

        public string ColumnTitle { get; set; }

        public Type DataType { get; set; }

        public HorizAlignments? HorizontalAlignment { get; set; }

        public VertiAlignments? VerticalAlignment { get; set; }

        public bool? HasBorder { get; set; }

        public int? Width { get; set; }

        protected virtual HorizontalAlignments SetDefaultHorizontalAlignmentsForType(Type type)
        {
            while (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>)
                    && type.GetGenericArguments() != null
                    && type.GetGenericArguments().Length == 1)
            {
                type = type.GetGenericArguments()[0];
            }
            TypeCode code = Type.GetTypeCode(type);
            switch (code)
            {
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                case TypeCode.Byte:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return HorizontalAlignments.Right;
                case TypeCode.DateTime:
                case TypeCode.Char:
                case TypeCode.Boolean:
                case TypeCode.String:
                case TypeCode.Object:
                case TypeCode.DBNull:
                case TypeCode.Empty:
                    return HorizontalAlignments.Left;
                default:
                    return HorizontalAlignments.Centered;
            }
        }

        private HorizontalAlignments? ConvertAlignments(HorizAlignments? h)
        {
            if (h == null)
            {
                return null;
            }
            return (HorizontalAlignments)(int)h.Value;
        }

        private VerticalAlignments? ConvertAlignments(VertiAlignments? v)
        {
            if (v == null)
            {
                return null;
            }
            return (VerticalAlignments)(int)v.Value;
        }

        private XF m_XF = null;
        internal XF GetDataCellXF(XlsDocument xls, Type dataType)
        {
            if (m_XF == null)
            {
                VerticalAlignments v1 = ConvertAlignments(VerticalAlignment).GetValueOrDefault(VerticalAlignments.Centered);
                bool hasBorder1 = HasBorder.GetValueOrDefault(true);

                XF xf = xls.NewXF();
                xf.VerticalAlignment = v1;
                xf.Font.Height = (ushort)(10 * this.FONT_HEIGHT_SCALE);
                if (hasBorder1)
                {
                    xf.LeftLineStyle = 1;
                    xf.LeftLineColor = Colors.Black;
                    xf.TopLineStyle = 1;
                    xf.TopLineColor = Colors.Black;
                    xf.RightLineStyle = 1;
                    xf.RightLineColor = Colors.Black;
                    xf.BottomLineStyle = 1;
                    xf.BottomLineColor = Colors.Black;
                    xf.TextWrapRight = true;
                }
                m_XF = xf;
            }
            HorizontalAlignments h1 = ConvertAlignments(HorizontalAlignment).GetValueOrDefault(SetDefaultHorizontalAlignmentsForType(dataType));
            m_XF.HorizontalAlignment = h1;
            return m_XF;
        }

        private XF m_TitleXF = null;
        internal XF GetSheetTitleXF(XlsDocument xls)
        {
            if (m_TitleXF == null)
            {
                XF xf = xls.NewXF();
                xf.HorizontalAlignment = HorizontalAlignments.Left;
                xf.VerticalAlignment = VerticalAlignments.Centered;
                xf.Font.Height = (ushort)(14 * this.FONT_HEIGHT_SCALE);
                xf.Font.Weight = FontWeight.Bold;
                xf.LeftLineStyle = 1;
                xf.LeftLineColor = Colors.Black;
                xf.TopLineStyle = 1;
                xf.TopLineColor = Colors.Black;
                xf.RightLineStyle = 1;
                xf.RightLineColor = Colors.Black;
                xf.BottomLineStyle = 1;
                xf.BottomLineColor = Colors.Black;
                xf.TextWrapRight = true;
                m_TitleXF = xf;
            }
            return m_TitleXF;
        }
    }

    public enum HorizAlignments
    {
        // Summary:
        //     Default - General        
        Default = 0,
        //
        // Summary:
        //     General
        General = 0,
        //
        // Summary:
        //     Left
        Left = 1,
        //
        // Summary:
        //     Centered
        Centered = 2,
        //
        // Summary:
        //     Right
        Right = 3,
        //
        // Summary:
        //     Filled
        Filled = 4,
        //
        // Summary:
        //     Justified
        Justified = 5,
        //
        // Summary:
        //     Centered Across the Selection
        CenteredAcrossSelection = 6,
        //
        // Summary:
        //     Distributed
        Distributed = 7
    }

    public enum VertiAlignments
    {
        // Summary:
        //     Top
        Top = 0,
        //
        // Summary:
        //     Centered
        Centered = 1,
        //
        // Summary:
        //     Default - Bottom
        Default = 2,
        //
        // Summary:
        //     Bottom
        Bottom = 2,
        //
        // Summary:
        //     Justified
        Justified = 3,
        //
        // Summary:
        //     Distributed
        Distributed = 4,
    }
}
