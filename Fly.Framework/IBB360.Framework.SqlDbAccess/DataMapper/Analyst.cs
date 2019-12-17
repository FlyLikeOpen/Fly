using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq.Expressions;
using System.Reflection;

namespace Fly.Framework.SqlDbAccess
{
    internal static class Analyst
    {
        public static string GetPropertyCombineStr(Expression exp)
        {
            return Visit(exp);
        }

        private static string Visit(Expression exp)
        {
            if (exp == null)
                return string.Empty;
            switch (exp.NodeType)
            {
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                case ExpressionType.Quote:
                case ExpressionType.TypeAs:
                    return Visit(((UnaryExpression)exp).Operand);
                case ExpressionType.Parameter:
                    return VisitParameter((ParameterExpression)exp);
                case ExpressionType.MemberAccess:
                    return VisitMemberAccess((MemberExpression)exp);
                default:
                    throw new ApplicationException(string.Format("Unhandled expression type: '{0}'", exp.NodeType));
            }
        }

        private static string VisitMemberAccess(MemberExpression m)
        {
            if (m.Member is PropertyInfo)
            {
                string prefix = Visit(m.Expression);
                string name = m.Member.Name;
                if (prefix != null && prefix.Length > 0)
                {
                    return prefix + "." + name;
                }
                return name;
            }
            else
            {
                throw new ApplicationException(m.Member.Name + "不是Property.");
            }
        }

        private static string VisitParameter(ParameterExpression p)
        {
            return string.Empty;
        }

        private const string PARAM_EXPRESSION = @"([^@]+|^)@(?<Variable>\w+)[.\n]*";
        private const string VAR_EXPRESSION = @"\bDECLARE\s*@(?<Variable>\w+)\s*[.\n]*";
        // 1. \*和*\之间的注释，可以有换行的
        // 2. 单引号'和'之间的字符串常量，可以有换行的
        // 3. -- 双横杆开头的并且带有@相关字符的注释，仅单行的
        private const string STR_EXPRESSION = @"/\*(?<Variable>(\w|\W)*?)\*/|'(?<Variable>[^']*?)'|--(?<Variable>[^\r\n]*@\w+[^\r\n]*?)(\r|\n)";
        private static Regex s_RegexParam = new Regex(PARAM_EXPRESSION, RegexOptions.Compiled | RegexOptions.Singleline);
        private static Regex s_RegexVar = new Regex(VAR_EXPRESSION, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static Regex s_RegexStr = new Regex(STR_EXPRESSION, RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static List<string> GetSqlParamNameList(string sqlText)
        {
            List<string> varList = new List<string>(); // 申明的变量
            MatchCollection matchCollection = s_RegexVar.Matches(sqlText);
            for (int i = 0; i < matchCollection.Count; i++)
            {
                string txt = matchCollection[i].Groups["Variable"].Value.Trim();
                if (!varList.Contains(txt))
                {
                    varList.Add(txt);
                }
            }

            //List<KeyValuePair<int, string>> strList = new List<KeyValuePair<int, string>>(); // 注释或字符串常量里出现的变量，其实不是真正的SQL参数
            List<int> strList = new List<int>();
            matchCollection = s_RegexStr.Matches(sqlText);
            for (int i = 0; i < matchCollection.Count; i++)
            {
                var g = matchCollection[i].Groups["Variable"];
                string txt = g.Value.Trim(); // 先解析出注释或字符串常量
                int startedIndex = g.Index;
                if (txt.Length > 0 && txt.IndexOf('@') >= 0) // 要存在@字符
                {
                    var m = s_RegexParam.Matches(txt); // 然后在注释或字符串常量中再寻找是否有SQL参数模样的字符串
                    for (int j = 0; j < m.Count; j++)
                    {
                        var gTmp = m[j].Groups["Variable"];
                        string txtTmp = gTmp.Value.Trim();
                        int indexTmp = gTmp.Index;
                        //strList.Add(new KeyValuePair<int, string>(startedIndex + indexTmp, txtTmp));
                        strList.Add(startedIndex + indexTmp);
                    }
                }
            }

            List<string> rstList = new List<string>(matchCollection.Count);
            matchCollection = s_RegexParam.Matches(sqlText);
            for (int i = 0; i < matchCollection.Count; i++)
            {
                var g = matchCollection[i].Groups["Variable"];
                string txt = g.Value.Trim();
                int index = g.Index;
                if (rstList.Contains(txt)) // 已经找到过了
                {
                    continue;
                }
                if (varList.Contains(txt)) // 说明是申明的SQL变量，而不是SQL参数
                {
                    continue;
                }
                if (strList.Contains(index)) // 说明这个变量不是真正的SQL参数，而是来自于注释或字符串常量里的
                {
                    continue;
                }
                rstList.Add(txt);
            }
            return rstList;
        }
    }
}
