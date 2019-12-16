using System;

namespace Fly.Framework.Common
{
    internal class LogAdapter
    {
        public static LogAdapter GetLogger(Type type)
        {
            return new LogAdapter(type);
        }

        public static LogAdapter GetLogger(string name)
        {
            return new LogAdapter(name);
        }

        private string m_LoggerName;
        private LogAdapter(string name) { m_LoggerName = name; }
        private LogAdapter(Type type) { m_LoggerName = type.FullName; }

        public void Error(string message, Exception e)
        {
            string msg = string.Format("[{0}] NAME : {1}; MESSAGE : {2}; EXCEPTION : \r\n\r\n{3}", DateTime.Now, m_LoggerName, message, e.ToString());
            Logger.Write("mc_err", msg);
        }
    }
}
