using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IBB360.Framework.JobService
{
    public interface IScheduler
    {
        void InitScheduler(TaskInfo task);
        ScheduleActionEnum GetAction(DateTime lastRunTime, DateTime specifiedTime, out DateTime? nextRunTime);
    }

    public enum ScheduleActionEnum
    {
        End,
        Wait,
        Run
    }
}
