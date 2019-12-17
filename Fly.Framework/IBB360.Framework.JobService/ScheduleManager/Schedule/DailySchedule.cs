using System;
using System.Collections.Generic;
using System.Text;

namespace IBB360.Framework.JobService.ScheduleManager
{
    [Serializable]
    public class DailySchedule : RepeatlySchedule
    {
        private int m_IntervalDays;

        public int IntervalDays
        {
            get { return m_IntervalDays; }
            set { m_IntervalDays = value; }
        }

        private bool IsDateValid(DateTime time)
        {
            int diff = time.Date.Subtract(FromDate.Date).Days;
            return (diff % m_IntervalDays) == 0;
        }

        protected override bool IsValid(DateTime time)
        {
            return base.IsValid(time) && IsDateValid(time);
        }

        public override DateTime? GetNextExecuteTimeAfterSpecificTime(DateTime time)
        {
            if (time > ToDate)
            {
                return default(DateTime?);
            }
            if (time < FromDate)
            {
                time = FromDate;
            }
            int diff = time.Date.Subtract(FromDate.Date).Days;
            int x = diff % m_IntervalDays;
            DateTime rst;
            if (x != 0)
            {
                rst = time.AddDays(m_IntervalDays - x);
            }
            else
            {
                rst = time;
            }
            rst = new DateTime(rst.Year, rst.Month, rst.Day).Add(TimeSpan.Parse(ExecuteTime));
            if (rst < time)
            {
                rst = rst.AddDays(m_IntervalDays);
            }
            return rst;
        }

        protected override DateTime SubtractInterval(DateTime time)
        {
            return time.AddDays(-1 * m_IntervalDays);
        }

        public override string ScheduleTypeName
        {
            get { return "Daily Schedule"; }
        }
    }
}
