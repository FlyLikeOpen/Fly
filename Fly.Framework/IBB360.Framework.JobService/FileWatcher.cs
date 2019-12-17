using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Timers;
using System.Threading;

namespace IBB360.Framework.JobService
{
    internal class FileWatcher : IDisposable
    {
        private FileSystemWatcher m_Watcher;
        private System.Threading.Timer m_Timer;

        private const int TimeoutMillis = 500;

        private List<string> m_ChangedFileList = new List<string>();
        private object m_SyncObj = new object();

        private Action<List<string>> m_Handler;

        public FileWatcher(string folderPath, Action<List<string>> handler)
            : this(folderPath, "*.*", handler)
        {

        }


        public FileWatcher(string folderPath, string filter, Action<List<string>> handler)
        {
            m_Handler = handler;
            m_Watcher = new FileSystemWatcher(folderPath, filter);
            m_Watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime | NotifyFilters.FileName;
            m_Watcher.Changed += new FileSystemEventHandler(FileChanged_Handler);
            m_Watcher.Created += new FileSystemEventHandler(FileChanged_Handler);
            m_Watcher.Deleted += new FileSystemEventHandler(FileChanged_Handler);
            m_Watcher.Renamed += new RenamedEventHandler(FileRenamed_Handler);
            m_Watcher.IncludeSubdirectories = true;

            m_Timer = new System.Threading.Timer(new TimerCallback(FileChanged_TimerChanged), null, Timeout.Infinite, Timeout.Infinite);            
        }

        public void StartToWatch()
        {
            if (!m_Watcher.EnableRaisingEvents)
            {
                m_Watcher.EnableRaisingEvents = true;
            }
        }

        public void StopToWatch()
        {
            m_Watcher.EnableRaisingEvents = false;
        }

        private void FileChanged_Handler(object source, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Deleted)
            {
                return;
            }
            string path = e.FullPath.ToUpper();
            try
            {
                if (!m_ChangedFileList.Contains(path, StringComparer.InvariantCultureIgnoreCase))
                {
                    lock (m_SyncObj)
                    {
                        if (!m_ChangedFileList.Contains(path, StringComparer.InvariantCultureIgnoreCase))
                        {
                            m_ChangedFileList.Add(path);
                        }
                    }
                }
            }
            catch //(Exception ex)
            {
                //Logger.Error(ex);
            }
            m_Timer.Change(TimeoutMillis, Timeout.Infinite);
        }

        private void FileRenamed_Handler(object source, RenamedEventArgs e)
        {
            string path = e.FullPath.ToUpper();
            if (!m_ChangedFileList.Contains(path))
            {
                lock (m_SyncObj)
                {
                    if (!m_ChangedFileList.Contains(path))
                    {
                        m_ChangedFileList.Add(path);
                    }
                }
            }
            m_Timer.Change(TimeoutMillis, Timeout.Infinite);
        }

        private void FileChanged_TimerChanged(object state)
        {
            if (m_ChangedFileList.Count > 0)
            {
                lock (m_SyncObj)
                {
                    if (m_ChangedFileList.Count > 0)
                    {
                        List<string> tmp = new List<string>(m_ChangedFileList);
                        if (m_Handler != null)
                        {
                            m_Handler(tmp);
                        }
                        m_ChangedFileList.Clear();
                    }
                }
            }
        }

        #region IDisposable Members

        public void Dispose()
        {
            if (m_Watcher != null)
            {
                m_Watcher.EnableRaisingEvents = false;
                m_Watcher.Dispose();
            }
            if (m_Timer != null)
            {
                m_Timer.Dispose();
            }
        }

        #endregion
    }
}
