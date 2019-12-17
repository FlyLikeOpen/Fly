using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IBB360.Framework.JobService
{
    public interface IExecuteTask : IDisposable
    {
        void InitFromTask(TaskInfo task);

        void Start();

        void Stop();

        void Restart();

        event ExceptionEventHandler UnhandledException;

        event EventHandler<ExecutedNotifyEventArgs> ExecutedNotify;

        string TaskID
        {
            get;
        }
    }
}
