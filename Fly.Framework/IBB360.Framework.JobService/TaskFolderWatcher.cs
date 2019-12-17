using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace IBB360.Framework.JobService
{
    public class TaskFolderWatcher : IDisposable
    {
        private FileSystemWatcher m_Watcher;
        private Action<string> m_CreateNewHandler;
        private Action<string> m_DeleteHandler;
        private Action<string, string> m_ChangeNameHandler;
        private string[] m_BaseFolderPathArray;
        private string m_ConfigFileName;

        public TaskFolderWatcher(string folderPath, string filter, Action<string> createNewHandler,
            Action<string> deleteHandler, Action<string, string> changeNameHandler)
        {
            m_ConfigFileName = filter;
            m_BaseFolderPathArray = folderPath.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            m_CreateNewHandler = createNewHandler;
            m_DeleteHandler = deleteHandler;
            m_ChangeNameHandler = changeNameHandler;
            m_Watcher = new FileSystemWatcher(AppDomain.CurrentDomain.SetupInformation.ApplicationBase, filter);
            m_Watcher.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName;
            m_Watcher.Created += new FileSystemEventHandler(FileChanged_Handler);
            m_Watcher.Deleted += new FileSystemEventHandler(FileChanged_Handler);
            m_Watcher.Renamed += new RenamedEventHandler(FileRenamed_Handler);
            m_Watcher.IncludeSubdirectories = true;
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
            string[] pathArray = e.FullPath.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (pathArray.Length != m_BaseFolderPathArray.Length + 2 || m_ConfigFileName != pathArray[pathArray.Length - 1].Trim().ToUpper())
            {
                return;
            }
            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                if (m_CreateNewHandler != null)
                {
                    Thread.Sleep(500); // 延迟半秒，防止文件还在copy过程中就去操作文件
                    m_CreateNewHandler(e.FullPath.ToUpper());
                }
            }
            else if (e.ChangeType == WatcherChangeTypes.Deleted)
            {
                if (m_DeleteHandler != null)
                {
                    Thread.Sleep(500);
                    m_DeleteHandler(e.FullPath.ToUpper());
                }
            }
        }

        private void FileRenamed_Handler(object source, RenamedEventArgs e)
        {
            string[] pathArray = e.FullPath.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            string[] pathArray2 = e.OldFullPath.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (pathArray.Length != m_BaseFolderPathArray.Length + 2 || (m_ConfigFileName != pathArray[pathArray.Length - 1].Trim().ToUpper() && m_ConfigFileName != pathArray2[pathArray2.Length - 1].Trim().ToUpper()))
            {
                return;
            }
            if (e.ChangeType == WatcherChangeTypes.Renamed)
            {
                if (m_ChangeNameHandler != null)
                {
                    Thread.Sleep(500);
                    m_ChangeNameHandler(e.OldFullPath.ToUpper(), e.FullPath.ToUpper());
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
        }

        #endregion
    }
}
