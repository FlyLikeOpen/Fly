using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IBB360.Framework.JobService
{
    internal static class Logger
    {
        public static void Info(TaskEventArgs e, string memo)
        {
            WriteLog(e.Folder, "HostLog", string.Format("任务\"{0}\"{1}...", e.TaskID, memo));
        }

        public static void Info(ExecutedNotifyEventArgs e)
        {
            string log = string.Format("任务\"{0}\"执行完毕\r\n执行结果: {1} - {2}\r\n开始时间: {3}\r\n执行耗时: {4}ms\r\n下次执行: {5}", e.TaskID, e.ExecuteResult, e.Description, e.ExecuteStartTime, e.ExecuteDuration, e.NextExecuteTime);
            WriteLog(e.Folder, "HostLog", log);
        }

        public static void Error(ExceptionEventArgs e)
        {
            string log = string.Format("目录{0}下的任务\"{1}\"执行出现异常: {2}; \r\n详细异常信息：\r\n{3}", e.Folder.ToLower(), e.TaskID, e.Description, e.Exception.ToString());
            DirectoryInfo d = new DirectoryInfo(e.Folder);
            WriteLog(d.Parent.FullName, "ErrorLog", log);
        }

        public static void Error(string folderPath, string taskId, Exception ex, string description)
        {
            string log = string.Format("目录{0}下的任务\"{1}\"执行出现异常: {2}; \r\n详细异常信息：\r\n{3}", folderPath, taskId, description, ex.ToString());
            DirectoryInfo d = new DirectoryInfo(folderPath);
            WriteLog(d.Parent.FullName, "ErrorLog", log);
        }

        private static void WriteLog(string folderPath, string logType, string text)
        {
            try
            {
                string logFolder = Path.Combine(folderPath, logType);
                if (Directory.Exists(logFolder) == false)
                {
                    Directory.CreateDirectory(logFolder);
                }
                string logFilePath = Path.Combine(logFolder, DateTime.Now.ToString("yyyy-MM-dd") + ".txt");
                DateTime now = DateTime.Now;
                StringBuilder sb = new StringBuilder();
                sb.Append("\r\n**---- [" + now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] - Begin ----******************************************************\r\n");
                sb.Append(text);
                sb.Append("\r\n**---- [" + now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] - End ----********************************************************\r\n");
                byte[] textByte = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
                lock (logFilePath)
                {
                    using (FileStream logStream = new FileStream(logFilePath, FileMode.Append, FileAccess.Write, FileShare.Write))
                    {
                        logStream.Write(textByte, 0, textByte.Length);
                        logStream.Close();
                    }
                }
            }
            catch { }
        }
    }
}
