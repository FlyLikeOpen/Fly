using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fly.Framework.Common
{
    public static class LocalFileQueue
    {
        private static string s_RootFolderPath = BuildRootFolderPath();
        private const string SUFFIX = ".fg";

        private static string BuildRootFolderPath()
        {
            string folderPath = ConfigurationManager.AppSettings["LocalFileQueueRootFolder"];
            if (string.IsNullOrWhiteSpace(folderPath))
            {
                folderPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, "FileQueue");
            }
            string p = Path.GetPathRoot(folderPath);
            if (string.IsNullOrWhiteSpace(p))
            {
                folderPath = Path.Combine(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, folderPath);
            }
            return folderPath;
        }

        private static string GetPendingMsgFolder(string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentNullException("queueName");
            }
            queueName = queueName.ToLower();
            var invalidChars = Path.GetInvalidPathChars();
            foreach (var c in queueName)
            {
                if (invalidChars.Contains(c))
                {
                    throw new ApplicationException("队列名字queueName里不能出现字符：'" + c + "'");
                }
            }
            return Path.Combine(s_RootFolderPath, string.Format("pending\\{0}", queueName));
        }

        private static string GetOngoingMsgFolder(string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentNullException("queueName");
            }
            queueName = queueName.ToLower();
            var invalidChars = Path.GetInvalidPathChars();
            foreach (var c in queueName)
            {
                if (invalidChars.Contains(c))
                {
                    throw new ApplicationException("队列名字queueName里不能出现字符：'" + c + "'");
                }
            }
            return Path.Combine(s_RootFolderPath, string.Format("ongoing\\{0}", queueName));
        }

        private static string GetFailedMsgFolder(string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentNullException("queueName");
            }
            queueName = queueName.ToLower();
            var invalidChars = Path.GetInvalidPathChars();
            foreach (var c in queueName)
            {
                if (invalidChars.Contains(c))
                {
                    throw new ApplicationException("队列名字queueName里不能出现字符：'" + c + "'");
                }
            }
            return Path.Combine(s_RootFolderPath, string.Format("failed\\{0}", queueName));
        }

        private static string GetSucceedMsgFolder(string queueName)
        {
            if (string.IsNullOrWhiteSpace(queueName))
            {
                throw new ArgumentNullException("queueName");
            }
            queueName = queueName.ToLower();
            var invalidChars = Path.GetInvalidPathChars();
            foreach (var c in queueName)
            {
                if (invalidChars.Contains(c))
                {
                    throw new ApplicationException("队列名字queueName里不能出现字符：'" + c + "'");
                }
            }
            return Path.Combine(s_RootFolderPath, string.Format("succeed\\{0}", queueName));
        }

        public static void Enqueue(string queueName, string msg, Encoding encoding = null)
        {
            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }
            byte[] myByte = encoding.GetBytes(msg);
            Enqueue(queueName, myByte);
        }

        public static void Enqueue(string queueName, byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }
            string msgFilePath = Path.Combine(GetPendingMsgFolder(queueName), Guid.NewGuid().ToString() + SUFFIX);
            string f = Path.GetDirectoryName(msgFilePath);
            if (!Directory.Exists(f))
            {
                Directory.CreateDirectory(f);
            }
            using (FileStream logStream = new FileStream(msgFilePath, FileMode.Append, FileAccess.Write, FileShare.None))
            {
                logStream.Write(data, 0, data.Length);
                logStream.Close();
            }
        }

        public static void Dequeue(string queueName, Action<string> handler, Encoding encoding = null)
        {
            Action<byte[]> tmpHandler = data =>
            {
                if (encoding == null)
                {
                    encoding = Encoding.UTF8;
                }
                string msg = encoding.GetString(data);
                handler(msg);
            };
            Dequeue(queueName, tmpHandler);
        }

        public static void Dequeue(string queueName, Action<byte[]> handler)
        {
            string f = GetPendingMsgFolder(queueName);
            if (Directory.Exists(f) == false)
            {
                return;
            }
            DirectoryInfo fdir = new DirectoryInfo(f);
            var fileList = fdir.GetFiles("*"+ SUFFIX, SearchOption.AllDirectories);
            if (fileList != null && fileList.Length > 0)
            {
                foreach (var file in fileList.OrderBy(x => x.CreationTime)) // 按创建时间先后顺序
                {
                    if (file.Exists)
                    {
                        var ongoingFile = MoveToOngoingFolder(queueName, file);
                        byte[] heByte;
                        using (var fsRead = ongoingFile.OpenRead())
                        {
                            int fsLen = (int)fsRead.Length;
                            heByte = new byte[fsLen];
                            fsRead.Read(heByte, 0, heByte.Length);
                        }
                        try
                        {
                            handler(heByte);
                            MoveToSucceedFolder(queueName, ongoingFile);
                        }
                        catch (Exception ex)
                        {
                            MoveToFailedFolder(queueName, ongoingFile);
                            Logger.Error(ex);
                        }
                    }
                }
            }
        }

        private static FileInfo MoveToOngoingFolder(string queueName, FileInfo file)
        {
            string newFilePath = Path.Combine(GetOngoingMsgFolder(queueName), file.Name);
            string f = Path.GetDirectoryName(newFilePath);
            if (!Directory.Exists(f))
            {
                Directory.CreateDirectory(f);
            }
            file.MoveTo(newFilePath);
            return new FileInfo(newFilePath);
        }

        private static void MoveToFailedFolder(string queueName, FileInfo file)
        {
            string newFilePath = Path.Combine(GetFailedMsgFolder(queueName), file.Name);
            string f = Path.GetDirectoryName(newFilePath);
            if (!Directory.Exists(f))
            {
                Directory.CreateDirectory(f);
            }
            file.MoveTo(newFilePath);
        }

        private static void MoveToSucceedFolder(string queueName, FileInfo file)
        {
            string newFilePath = Path.Combine(GetSucceedMsgFolder(queueName), string.Format("{0}\\{1}\\{2}", DateTime.Now.ToString("yyyy-MM-dd"), DateTime.Now.ToString("HH_mm"), file.Name));
            string f = Path.GetDirectoryName(newFilePath);
            if (!Directory.Exists(f))
            {
                Directory.CreateDirectory(f);
            }
            file.MoveTo(newFilePath);
        }

        #region Manager


        private static FileWatcher s_FileWatcher = new FileWatcher(Path.Combine(BuildRootFolderPath(), "pending"), "*"+ SUFFIX, ProcessQueue);
        private static object s_SyncObj = new object();
        private static Dictionary<string, Action<byte[]>> s_ProcessorDicts = new Dictionary<string, Action<byte[]>>(StringComparer.InvariantCultureIgnoreCase);

        private static void ProcessQueue()
        {
            if (s_ProcessorDicts == null || s_ProcessorDicts.Count <= 0)
            {
                return;
            }
            foreach (var entry in s_ProcessorDicts)
            {
                Dequeue(entry.Key, entry.Value);
            }
        }

        public static void StartToProcess(string queueName, Action<string> handler, Encoding encoding = null)
        {
            Action<byte[]> tmpHandler = data =>
            {
                if (encoding == null)
                {
                    encoding = Encoding.UTF8;
                }
                string msg = encoding.GetString(data);
                handler(msg);
            };
            StartToProcess(queueName, tmpHandler);
        }

        public static void StartToProcess(string queueName, Action<byte[]> handler)
        {
            if (s_ProcessorDicts.ContainsKey(queueName))
            {
                throw new ApplicationException("不能重复注册queueName：" + queueName);
            }
            lock (s_SyncObj)
            {
                if (s_ProcessorDicts.ContainsKey(queueName))
                {
                    throw new ApplicationException("不能重复注册queueName：" + queueName);
                }
                s_ProcessorDicts.Add(queueName, handler);
                s_FileWatcher.StartToWatch();
            }
        }

        public static void StopToProcess(string queueName)
        {
            if (s_ProcessorDicts.ContainsKey(queueName) == false)
            {
                return;
            }
            lock (s_SyncObj)
            {
                if (s_ProcessorDicts.ContainsKey(queueName) == false)
                {
                    return;
                }
                s_ProcessorDicts.Remove(queueName);
                if (s_ProcessorDicts.Count <= 0)
                {
                    s_FileWatcher.StopToWatch();
                }
            }
        }
        
        private class FileWatcher
        {
            private FileSystemWatcher m_Watcher;
            //private string m_FolderPath;

            private const int TimeoutMillis = 500;

            private long m_ChangedTimes = 0;
            //private bool m_Running = false;
            private int m_Running = 0;
            private DateTime m_LastProcessMsgTime = DateTime.MinValue;
            //private DateTime m_LastClearEmptyFolderTime = DateTime.MinValue;

            private Action m_Handler;

            public FileWatcher(string folderPath, Action handler)
                : this(folderPath, "*.*", handler)
            {

            }

            public FileWatcher(string folderPath, string filter, Action handler)
            {
                m_Handler = handler;
                //m_FolderPath = folderPath;
                if (Directory.Exists(folderPath) == false)
                {
                    Directory.CreateDirectory(folderPath);
                }
                m_Watcher = new FileSystemWatcher(folderPath, filter);
                m_Watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime | NotifyFilters.FileName;
                m_Watcher.Created += new FileSystemEventHandler(FileChanged_Handler);
                m_Watcher.IncludeSubdirectories = true;
            }

            public void StartToWatch()
            {
                int flag = Interlocked.Exchange(ref m_Running, 1);
                if (flag == 0) // 说明之前还未运行
                {
                    m_Watcher.EnableRaisingEvents = true;
                    Action act = () =>
                    {
                        while (m_Running == 1)
                        {
                            long t = Interlocked.Exchange(ref m_ChangedTimes, 0);
                            if ((t > 0 || m_LastProcessMsgTime.AddMinutes(5) < DateTime.Now) // 说明有新的文件产生，或者是已经有5分钟都没有处理过消息了
                                && m_Handler != null)
                            {
                                try
                                {
                                    m_Handler();
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error(ex);
                                }
                                m_LastProcessMsgTime = DateTime.Now;
                            }
                            //if (m_LastClearEmptyFolderTime.AddMinutes(2) < DateTime.Now) // 已经过了2分钟，还没清除过空目录，那么就执行一次清除空目录的操作
                            //{
                            //    try
                            //    {
                            //        DirectoryInfo d = new DirectoryInfo(m_FolderPath);
                            //        DeleteEmptySubFolder(d);
                            //    }
                            //    catch (Exception ex)
                            //    {
                            //        Logger.Error(ex);
                            //    }
                            //    m_LastClearEmptyFolderTime = DateTime.Now;
                            //}
                            Thread.Sleep(TimeoutMillis);
                        }
                    };
                    act.BeginInvoke(null, null);
                }
            }

            public void StopToWatch()
            {
                int flag = Interlocked.Exchange(ref m_Running, 0);
                if (flag == 1)
                {
                    m_Watcher.EnableRaisingEvents = false;
                }
            }

            private void FileChanged_Handler(object source, FileSystemEventArgs e)
            {
                if (e.ChangeType != WatcherChangeTypes.Created)
                {
                    return;
                }
                Interlocked.Increment(ref m_ChangedTimes); // 有新的文件产生，让计数器自增
            }

            //private void DeleteEmptySubFolder(DirectoryInfo folder)
            //{
            //    if (folder.Exists == false // 目录不存在
            //        || folder.CreationTime.AddMinutes(2) > DateTime.Now) // 目录是在2分钟内创建的，也就是只删除2分钟以前创建的目录，最近2分钟内新创建的目录不管是不是空的，都是不会删除的
            //    {
            //        return;
            //    }
            //    var files = folder.GetFiles("*", SearchOption.TopDirectoryOnly);
            //    if (files != null && files.Length > 0) // 说明还有文件
            //    {
            //        return;
            //    }
            //    var subFolders = folder.GetDirectories("*", SearchOption.TopDirectoryOnly);
            //    if (subFolders != null && subFolders.Length > 0)
            //    {
            //        foreach (var subFolder in subFolders)
            //        {
            //            DeleteEmptySubFolder(subFolder);
            //        }
            //        subFolders = folder.GetDirectories("*", SearchOption.TopDirectoryOnly);
            //    }
            //    if (subFolders == null || subFolders.Length <= 0)
            //    {
            //        folder.Delete();
            //    }
            //}
        }

        #endregion
    }
}
