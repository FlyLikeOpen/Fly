using System;
using System.Collections.Generic;
using System.Text;
using System.Messaging;
using System.Diagnostics;
using System.Web;
using System.Net;
using System.Threading;
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Fly.Framework.Common
{
    public static class Logger
    {
        #region 
        internal static void WriteEventLog(string content, EventLogEntryType type)
        {
            const string event_source = "Fly.Framework.Common.Logger";
            const string event_name = "Fly.Framework.Common.Logger_Exception";
            try
            {
                if (!EventLog.SourceExists(event_source))
                {
                    EventLog.CreateEventSource(event_source, event_name);
                }
                using (EventLog elog = new EventLog())
                {
                    elog.Log = event_name;
                    elog.Source = event_source;
                    elog.WriteEntry(content, type);
                    elog.Close();
                }
            }
            catch { }
        }

        private static string WriteLog(LogEntry log, List<ILogEmitter> logEmitterList)
        {
            if (logEmitterList == null || logEmitterList.Count <= 0)
            {
                return log.LogID.ToString();
            }
            try
            {                
                foreach (var logEmitter in logEmitterList)
                {
                    if (logEmitter == null)
                    {
                        continue;
                    }
                    try
                    {
                        logEmitter.EmitLog(log);
                    }
                    catch (Exception ex)
                    {
                        string message = string.Format("Write log failed.\r\n\r\n Error Info: {0}. \r\n\r\n Log Type: {1}. \r\n\r\n Log Info: {2}", ex.ToString(), logEmitter.GetType().AssemblyQualifiedName, log.SerializationWithoutException());
                        Logger.WriteEventLog(message, EventLogEntryType.Error);
                    }
                }
            }
            catch(Exception ex)
            {
                string message = string.Format("Write log failed.\r\n\r\n Error Info: {0}. \r\n\r\n Log Info: {1}", ex.ToString(), log.SerializationWithoutException());
                Logger.WriteEventLog(message, EventLogEntryType.Error);
            }

            return log.LogID.ToString();
        }

        private static string WriteLog(LogEntry log)
        {
            List<ILogEmitter> logEmitterList;
            try
            {
                logEmitterList = EmitterFactory.Create();
            }
            catch(Exception ex)
            {
                string message = string.Format("Failed to create log emitter instance.\r\n\r\n Error Info: {0}. \r\n\r\n Log Info: {1}", ex.ToString(), log.SerializationWithoutException());
                WriteEventLog(message, EventLogEntryType.Error);
                return log.LogID.ToString();
            }
            return WriteLog(log, logEmitterList);
        }

        private static string GetStackTrace()
        {
            //当前堆栈信息
            System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(true);
            System.Diagnostics.StackFrame[] sfs = st.GetFrames();
            StringBuilder sb = new StringBuilder();
            int x = 0;
            for (int i = 3; i < sfs.Length; ++i)
            {
                //非用户代码,系统方法及后面的都是系统调用，不获取用户代码调用结束
                if (System.Diagnostics.StackFrame.OFFSET_UNKNOWN == sfs[i].GetILOffset()) break;
                var m = sfs[i].GetMethod();
                x = x + 1;
                string name = string.Format("\r\n{3}. {0}.{1} ({2}行)", m.DeclaringType.FullName, m.Name, sfs[i].GetFileLineNumber(), x);
                //if (sb.Length > 0)
                //{
                //    sb.Append(" -> ");
                //}
                sb.Append(name);
            }
            return sb.ToString();
        }

        private static string WriteLog(string content, string category = null, string referenceKey = null, List<KeyValuePair<string, object>> extendedProperties = null, bool needStackTrace = false)
        {
            LogEntry log = new LogEntry();
            log.ServerTime = DateTime.Now;
            log.LogID = Guid.NewGuid();
            log.Source = GetSource();
            log.RequestUrl = GetRequestUrl();
            log.RequestReferer = GetRequestReferer();
            log.UserHostName = GetUserHostName();
            log.UserHostAddress = GetUserHostAddress();
            log.ServerIP = GetServerIP();
            try
            {
                log.ServerName = Dns.GetHostName();
            }
            catch { }
            string p_name;
            log.ProcessID = GetProcessInfo(out p_name);
            log.ProcessName = p_name;
            try
            {
                log.ThreadID = Thread.CurrentThread.ManagedThreadId;
            }
            catch { }

            log.Category = category;
            log.Content = content;
            log.ReferenceKey = referenceKey;
            if (extendedProperties != null && extendedProperties.Count > 0)
            {
                foreach (var p in extendedProperties)
                {
                    log.AddExtendedProperty(p.Key, p.Value);
                }
            }
            try
            {
                log.OperatorUser = ResolveOperatorUser();
            }
            catch { }

            if (needStackTrace)
            {
                try
                {
                    log.StackTrace = GetStackTrace();
                }
                catch { }
            }
            return WriteLog(log);
        }

        private static string GetRequestReferer()
        {
            try
            {
                if (HttpContext.Current != null && HttpContext.Current.Request != null && HttpContext.Current.Request.UrlReferrer != null)
                {
                    return HttpContext.Current.Request.UrlReferrer.GetAbsoluteUri();
                }
                //if (OperationContext.Current != null && OperationContext.Current.RequestContext != null
                //    && OperationContext.Current.RequestContext.RequestMessage != null
                //    && OperationContext.Current.RequestContext.RequestMessage.Headers != null)
                //{
                //    return OperationContext.Current.RequestContext.RequestMessage.Headers.re.GetAbsoluteUri();
                //}
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string ResolveOperatorUser()
        {
            if (ContextManager.Current == null)
            {
                return string.Empty;
            }
            try
            {
                string userId = ContextManager.Current.UserId.ToString();
                if (OperatorUserResolver == null)
                {
                    return userId;
                }
                try
                {
                    return OperatorUserResolver(userId);
                }
                catch { }
                return userId;
            }
            catch { }
            return string.Empty;
        }

        private static string GetSource()
        {
            try
            {
                LogSetting s = LogSection.GetSetting();
                if (s != null)
                {
                    return s.Source;
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetRequestUrl()
        {
            try
            {
                if (HttpContext.Current != null && HttpContext.Current.Request != null)
                {
                    return HttpContext.Current.Request.Url.GetAbsoluteUri();
                }
                if (OperationContext.Current != null && OperationContext.Current.RequestContext != null
                    && OperationContext.Current.RequestContext.RequestMessage != null
                    && OperationContext.Current.RequestContext.RequestMessage.Headers != null)
                {
                    return OperationContext.Current.RequestContext.RequestMessage.Headers.To.GetAbsoluteUri();
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string GetUserHostName()
        {
            try
            {
                if (HttpContext.Current != null && HttpContext.Current.Request != null)
                {
                    return HttpContext.Current.Request.UserHostName;
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
            //return string.Empty;
        }

        private static string GetUserHostAddress()
        {
            try
            {
                if (HttpContext.Current != null && HttpContext.Current.Request != null)
                {
                    var context = HttpContext.Current;
                    string result = String.Empty;
                    result = context.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                    if (null == result || result.Trim() == String.Empty)
                    {
                        result = context.Request.ServerVariables["REMOTE_ADDR"];
                    }
                    if (null == result || result.Trim() == String.Empty)
                    {
                        result = context.Request.UserHostAddress;
                    }
                    if (null == result || result.Trim() == String.Empty)
                    {
                        return "Unknown";
                    }
                    return result;
                }
                if (OperationContext.Current != null)
                {
                    RemoteEndpointMessageProperty endpointProperty = OperationContext.Current.IncomingMessageProperties[RemoteEndpointMessageProperty.Name] as RemoteEndpointMessageProperty;
                    if (endpointProperty != null)
                    {
                        return endpointProperty.Address;
                    }
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static string s_ServerIP;
        private static string GetServerIP()
        {
            if (string.IsNullOrEmpty(s_ServerIP))
            {
                try
                {
                    IPAddress[] address = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
                    if (address != null)
                    {
                        foreach (IPAddress addr in address)
                        {
                            if (addr == null)
                            {
                                continue;
                            }
                            string tmp = addr.ToString().Trim();
                            //过滤IPv6的地址信息
                            if (tmp.Length <= 16 && tmp.Length > 5)
                            {
                                s_ServerIP = tmp;
                                break;
                            }
                        }
                    }
                }
                catch
                {
                    //s_ServerIP = string.Empty;
                }
            }
            if (string.IsNullOrEmpty(s_ServerIP))
            {
                return string.Empty;
            }
            return s_ServerIP;
        }

        private static int GetProcessInfo(out string name)
        {
            try
            {
                Process p = Process.GetCurrentProcess();
                if (p == null)
                {
                    name = null;
                    return -1;
                }
                name = p.ProcessName;
                return p.Id;
            }
            catch
            {
                name = string.Empty;
                return -1;
            }
        }
        #endregion

        public static Func<string, string> OperatorUserResolver
        {
            get;
            set;
        }

        public static string Write(string folderName, string message, string referenceKey = null, List<KeyValuePair<string, object>> extendedProperties = null, bool needStackTrace = false)
        {
            return WriteLog(message, folderName, referenceKey, extendedProperties, needStackTrace);
        }

        public static string Write(string folderName, string message, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                return WriteLog(string.Format(message, args), folderName, null, null, false);
            }
            else
            {
                return WriteLog(message, folderName, null, null, false);
            }
        }

        public static string Write(string folderName, bool needStackTrace, string message, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                return WriteLog(string.Format(message, args), folderName, null, null, needStackTrace);
            }
            else
            {
                return WriteLog(message, folderName, null, null, needStackTrace);
            }
        }

        public static string Error(Exception ex, string referenceKey = null, List<KeyValuePair<string, object>> extendedProperties = null)
        {
            string folder = "Error";
            if (ex is ResourceNotFoundException)
            {
                folder = "NotFound";
            }
            else if (ex is NoAuthenticationException)
            {
                folder = "NoAuth";
            }
            else if (ex is NoAuthenticationException)
            {
                folder = "NoAuth";
            }
            return WriteLog(ex.ToString(), folder, referenceKey, extendedProperties);
        }

        public static string Error(string errorMsg, string referenceKey = null, List<KeyValuePair<string, object>> extendedProperties = null)
        {
            return WriteLog(errorMsg, "Error", referenceKey, extendedProperties);
        }

        public static string ErrorFormat(string errorMsg, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                return WriteLog(string.Format(errorMsg, args), "Error", null, null, false);
            }
            else
            {
                return WriteLog(errorMsg, "Error", null, null, false);
            }
        }

        public static string ErrorFormat(bool needStackTrace, string errorMsg, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                return WriteLog(string.Format(errorMsg, args), "Error", null, null, needStackTrace);
            }
            else
            {
                return WriteLog(errorMsg, "Error", null, null, needStackTrace);
            }
        }

        public static string Info(string infoMsg, string referenceKey = null, List<KeyValuePair<string, object>> extendedProperties = null, bool needStackTrace = false)
        {
            return WriteLog(infoMsg, "Info", referenceKey, extendedProperties, needStackTrace);
        }

        public static string InfoFormat(string infoMsg, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                return WriteLog(string.Format(infoMsg, args), "Info", null, null, false);
            }
            else
            {
                return WriteLog(infoMsg, "Info", null, null, false);
            }
        }

        public static string InfoFormat(bool needStackTrace, string infoMsg, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                return WriteLog(string.Format(infoMsg, args), "Info", null, null, needStackTrace);
            }
            else
            {
                return WriteLog(infoMsg, "Info", null, null, needStackTrace);
            }
        }

        public static string Warning(string warningMsg, string referenceKey = null, List<KeyValuePair<string, object>> extendedProperties = null, bool needStackTrace = false)
        {
            return WriteLog(warningMsg, "Warning", referenceKey, extendedProperties, needStackTrace);
        }

        public static string WarningFormat(string warningMsg, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                return WriteLog(string.Format(warningMsg, args), "Warning", null, null, false);
            }
            else
            {
                return WriteLog(warningMsg, "Warning", null, null, false);
            }
        }

        public static string WarningFormat(bool needStackTrace, string warningMsg, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                return WriteLog(string.Format(warningMsg, args), "Warning", null, null, needStackTrace);
            }
            else
            {
                return WriteLog(warningMsg, "Warning", null, null, needStackTrace);
            }
        }
    }
}