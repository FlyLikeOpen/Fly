using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Fly.Framework.Common
{
    [Serializable]
    public class BizException : ApplicationException
    {
        private bool m_NeedLog = false;

        public BizException(string msg)
            : base(msg)
        {

        }

        public BizException(bool needLog, string msg)
            : base(msg)
        {
            m_NeedLog = needLog;
        }

        public BizException(string format, params object[] args)
            : base(string.Format(format, args))
        {

        }

        public BizException(bool needLog, string format, params object[] args)
            : base(string.Format(format, args))
        {
            m_NeedLog = needLog;
        }

        public BizException(Exception innerException, string msg)
            : base(msg, innerException)
        {

        }

        public BizException(bool needLog, Exception innerException, string msg)
            : base(msg, innerException)
        {
            m_NeedLog = needLog;
        }

        public BizException(Exception innerException, string format, params object[] args)
            : base(string.Format(format, args), innerException)
        {

        }

        public BizException(bool needLog, Exception innerException, string format, params object[] args)
            : base(string.Format(format, args), innerException)
        {
            m_NeedLog = needLog;
        }

        protected BizException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }

        public bool NeedLog
        {
            get
            {
                return m_NeedLog;
            }
        }
    }
}
