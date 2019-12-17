using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace Fly.Framework.Common
{
    public class RouteAuthorizationAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            string authKey = null;
            object tmp;
            if (filterContext.RouteData.DataTokens.TryGetValue("_ibb_authKey", out tmp) && tmp != null)
            {
                authKey = tmp.ToString();
            }
            if (string.IsNullOrWhiteSpace(authKey)) // 没有配置authKey，则不做授权控制
            {
                return;
            }
            Func<string, bool> handler = ContextManager.Current["AuthCheck"] as Func<string, bool>;
            if (handler != null) // 说明有设置判断权限的委托
            {
                AuthorityCalculator calculator = new AuthorityCalculator(handler, authKey);
                if (calculator.GetResult() == false) // 说明没有权限
                {
                    NotAuthorized(filterContext);
                }
            }
        }

        private void NotAuthorized(ActionExecutingContext filterContext)
        {
            // check if be Ajax request or normal web page request
            HttpRequestBase request = filterContext.RequestContext.HttpContext.Request;
            if (request.IsAjaxRequest())
            {
                string acceptType = request.Headers["Accept"];
                string message = "No_Authorized";
                if (acceptType != null && acceptType.ToLower().Contains("application/xml"))
                {
                    filterContext.Result = new ContentResult
                    {
                        Content = string.Format("<?xml version=\"1.0\"?><result><error>true</error><message>{0}</message></result>", message),
                        ContentEncoding = Encoding.UTF8,
                        ContentType = "application/xml"
                    };
                }
                else
                {
                    filterContext.Result = new JsonResult
                    {
                        Data = new { error = true, message = message },
                        JsonRequestBehavior = JsonRequestBehavior.AllowGet
                    };
                }
            }
            else
            {
                filterContext.Result = new RedirectResult(BuildNoAuthorizedUrl(HttpUtility.UrlEncode(request.Url.GetAbsoluteUri())));
            }
        }

        private string BuildNoAuthorizedUrl(string returnUrl)
        {
            //return "/login/?returnUrl=" + HttpUtility.UrlEncode(returnUrl);
            string routeName = ConfigurationManager.AppSettings["NoAuthorizedRouteName"];
            if (string.IsNullOrWhiteSpace(routeName))
            {
                routeName = "NoAuthorized";
            }
            return RouteHelper.BuildUrl(routeName);
        }
    }

    public class AuthorityCalculator
    {
        private Stack<string> m_AuthKeys = new Stack<string>(); // 权限点的栈
        private Stack<char> m_Symbol = new Stack<char>(); //符号栈
        private Func<string, bool> m_Handler; // 判断是否有权限的委托
        private string m_InputString;

        private const string TRUE_STRING = "%TRUE%";
        private const string FALSE_STRING = "%FALSE%";

        public AuthorityCalculator(Func<string, bool> handler, string inputStr)
        {
            if (inputStr == null)
            {
                inputStr = string.Empty;
            }
            else
            {
                if (inputStr.Contains('%'))
                {
                    throw new ApplicationException("无效的权限点设置（权限点字符串中不能有百分号%）：" + inputStr);
                }
                inputStr = inputStr.Trim();
            }
            m_InputString = inputStr;
            m_Handler = handler ?? throw new ArgumentNullException("handler", "判断权限点的委托不能为空");

            for (int i = 0; i < inputStr.Length; i++)
            {
                if (IsBooleanOperator(inputStr[i]))   // 说明是且、或关系运算符 
                {
                    if (m_Symbol.Count <= 0) // 操作符栈是空的
                    {
                        m_Symbol.Push(inputStr[i]);
                    }
                    else
                    {
                        var topChar = m_Symbol.Peek(); // 获取操作符栈顶部的符号
                        if (IsLeftParenthesis(topChar)) // 如果是左括号
                        {
                            m_Symbol.Push(inputStr[i]);
                        }
                        else if (IsRightParenthesis(inputStr[i])) // 如果是右括号
                        {
                            throw new ApplicationException("理论上不应该在操作符栈里出现右括号");
                        }
                        else if (GetOperatorPrecedence(inputStr[i]) > GetOperatorPrecedence(topChar))
                        {
                            m_Symbol.Push(inputStr[i]);
                        }
                        else
                        {
                            if (m_AuthKeys.Count < 2)
                            {
                                throw new ApplicationException("无效的权限点设置（且或运算符之间必须要有权限点字符串）：" + inputStr);
                            }
                            string n2 = m_AuthKeys.Pop();
                            string n1 = m_AuthKeys.Pop();
                            char s1 = m_Symbol.Pop();
                            string result = OperateAuthKey(n1, n2, s1);
                            m_AuthKeys.Push(result);
                            m_Symbol.Push(inputStr[i]);
                        }
                    }
                }
                else if (IsLeftParenthesis(inputStr[i])) // 左括号: (
                {
                    m_Symbol.Push(inputStr[i]);
                }
                else if (IsRightParenthesis(inputStr[i])) // 右括号: )
                {
                    do
                    {
                        if (m_Symbol.Count <= 0)
                        {
                            throw new ApplicationException("无效的权限点设置（缺少对应的左边括号）：" + inputStr);
                        }
                        char topSymbol = m_Symbol.Pop();
                        if (IsLeftParenthesis(topSymbol)) // 如果是左括号，则退出循环
                        {
                            break;
                        }
                        // 走到这里说明不是左括号，那么就是且或运算符，就需要计算出且或结果
                        if (m_AuthKeys.Count < 2)
                        {
                            throw new ApplicationException("无效的权限点设置（且或运算符之间必须要有权限点字符串）：" + inputStr);
                        }
                        string n2 = m_AuthKeys.Pop();
                        string n1 = m_AuthKeys.Pop();
                        string result = OperateAuthKey(n1, n2, topSymbol);
                        m_AuthKeys.Push(result);
                    }
                    while (true);
                }
                else // 不是符号，也就是权限点字符串的字符
                {
                    StringBuilder tmp = new StringBuilder();
                    do
                    {
                        tmp.Append(inputStr[i]);
                        i++;
                    }
                    while (i < inputStr.Length && !IsSymbol(inputStr[i])); // 如果遇到是符号或者到达字符串结尾，就退出do ... while循环
                    i--;
                    m_AuthKeys.Push(tmp.ToString());
                }
            }
        }

        private bool IsBooleanOperator(char c)
        {
            // + 表示且关系
            // | 表示或关系
            return c.Equals('+') || c.Equals('|');
        }

        private bool IsLeftParenthesis(char c)
        {
            return c.Equals('(');
        }

        private bool IsRightParenthesis(char c)
        {
            return c.Equals(')');
        }

        private bool IsSymbol(char c)
        {
            return IsBooleanOperator(c) || IsLeftParenthesis(c) || IsRightParenthesis(c);
        }

        public int GetOperatorPrecedence(char a) // 获取运算符的优先级
        {
            switch (a)
            {
                case '+': // 且
                    return 20;
                case '|': // 或
                    return 10;
                default:
                    return 0;
            }
        }

        public string OperateAuthKey(string n1, string n2, char s1)
        {
            if (string.IsNullOrWhiteSpace(n1) || string.IsNullOrWhiteSpace(n2))
            {
                throw new ApplicationException("无效的权限点配置（且或运算符之间必须要有非空白字符）：" + m_InputString);
            }
            n1 = n1.Trim();
            n2 = n2.Trim();
            if (s1 == '+') // 且
            {
                if (n1 == FALSE_STRING || n2 == FALSE_STRING) // 有1个为false就返回false
                {
                    return FALSE_STRING;
                }
                bool r1 = n1 == TRUE_STRING || m_Handler(n1);
                if (r1 == false)
                {
                    return FALSE_STRING;
                }
                bool r2 = n2 == TRUE_STRING || m_Handler(n2);
                if (r2 == false)
                {
                    return FALSE_STRING;
                }
                return TRUE_STRING;
            }
            else if (s1 == '|') // 或
            {
                if (n1 == TRUE_STRING || n2 == TRUE_STRING) // 有1个为true就返回true
                {
                    return TRUE_STRING;
                }
                bool r1 = n1 == FALSE_STRING ? false : m_Handler(n1);
                if (r1)
                {
                    return TRUE_STRING;
                }
                bool r2 = n2 == FALSE_STRING ? false : m_Handler(n2);
                if (r2)
                {
                    return TRUE_STRING;
                }
                return FALSE_STRING;
            }
            else
            {
                throw new ApplicationException("配置的权限点\"" + m_InputString + "\"中出现了无效的关系运算符：" + s1);
            }
        }

        public bool GetResult()
        {
            if (m_Symbol.Count <= 0)
            {
                if (m_AuthKeys.Count <= 0) // 说明没有配置任何权限
                {
                    return true;
                }
                else if (m_AuthKeys.Count == 1) // 说明就直接配置的一个权限点，没有且或关系运算的
                {
                    return m_Handler(m_AuthKeys.Pop());
                }
                else
                {
                    throw new ApplicationException("无效的权限点设置（解析出了多个权限点，但却没有且或关系运算符）：" + m_InputString);
                }
            }
            // 走到这里，说明一定有运算符了
            do
            {
                if (m_AuthKeys.Count < 2)
                {
                    throw new ApplicationException("无效的权限点设置（且或运算符之间必须要有权限点字符串）：" + m_InputString);
                }
                string n2 = m_AuthKeys.Pop();
                string n1 = m_AuthKeys.Pop();
                char s1 = m_Symbol.Pop();
                string result = OperateAuthKey(n1, n2, s1);
                m_AuthKeys.Push(result);
            }
            while (m_Symbol.Count > 0);
            if (m_AuthKeys.Count != 1)
            {
                throw new ApplicationException("无效的权限点设置（两个权限点之间必须要有且或运算符）：" + m_InputString);
            }
            string finalResult = m_AuthKeys.Pop();
            if (finalResult == TRUE_STRING)
            {
                return true;
            }
            if (finalResult == FALSE_STRING)
            {
                return false;
            }
            return m_Handler(finalResult);
        }
    }
}
