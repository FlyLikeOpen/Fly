using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Fly.Framework.Common
{
    internal class TextEmitter : ILogEmitter
    {
        private string m_LogFolderPath;
        public void Init(Dictionary<string, string> param)
        {
            string folderPath;
            if (param.TryGetValue("path", out folderPath) == false || folderPath == null || folderPath.Trim().Length <= 0)
            {
                m_LogFolderPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "Log");
                return;
            }
            string p = Path.GetPathRoot(folderPath);
            if (p == null || p.Trim().Length <= 0)
            {
                m_LogFolderPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, folderPath);
                return;
            }
            m_LogFolderPath = folderPath;
        }

        public void EmitLog(LogEntry log)
        {
            string p = m_LogFolderPath;
            if (string.IsNullOrWhiteSpace(log.Category) == false)
            {
                string c = log.Category;
                foreach (var i in Path.GetInvalidPathChars())
                {
                    if (c.IndexOf(i) >= 0)
                    {
                        c = c.Replace(i, '_');
                    }
                }
                if (string.IsNullOrWhiteSpace(c) == false)
                {
                    p = Path.Combine(m_LogFolderPath, c);
                }
            }
            if (!Directory.Exists(p))
            {
                Directory.CreateDirectory(p);
            }

            WriteToFile(log.SerializationWithoutException(),
                Path.Combine(p, DateTime.Now.ToString("yyyy-MM-dd") + ".txt"));
        }

        private static void WriteToFile(string log, string filePath)
        {
            DateTime now = DateTime.Now;
            StringBuilder sb = new StringBuilder();
            sb.Append("\r\n** [" + now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] - Begin **************************************************************");
            sb.Append(log);
            sb.Append("\r\n** [" + now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] - End ****************************************************************\r\n");
            byte[] textByte = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            lock (filePath)
            {
                using (FileStream logStream = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.Write))
                {
                    logStream.Write(textByte, 0, textByte.Length);
                    logStream.Close();
                }
            }
        }
    }
}
