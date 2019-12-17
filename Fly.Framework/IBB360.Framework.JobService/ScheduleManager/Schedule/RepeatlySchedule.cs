using System;
using System.Collections.Generic;
using System.Text;

namespace IBB360.Framework.JobService.ScheduleManager
{
    [Serializable]
    public abstract class RepeatlySchedule : Schedule
    {
        

        private DateTime m_FromDate;

        public DateTime FromDate
        {
            get { return m_FromDate; }
            set { m_FromDate = value; }
        }

        private DateTime m_ToDate;

        public DateTime ToDate
        {
            get { return m_ToDate; }
            set { m_ToDate = value; }
        }

        public string ExecuteTime
        {
            get;
            set;
        }

        private bool InValidPeriod(DateTime time)
        {
            return (time >= m_FromDate && time < m_ToDate);
        }

        private bool IsTimeOfDayValid(DateTime time)
        {
            double diff = time.TimeOfDay.Subtract(TimeSpan.Parse(ExecuteTime)).TotalMilliseconds;
            return (diff >= 0 && diff < DisparityMilliseconds);
        }

        public override bool Check(DateTime dateTime)
        {
            DateTime newTime = GetSpecialTimeZoneTime(dateTime);
            return IsValid(newTime);
        }

        protected virtual bool IsValid(DateTime time)
        {
            return InValidPeriod(time) && IsTimeOfDayValid(time);
        }

        public override DateTime? GetLastExecuteTimeAfterSpecificTime(DateTime time)
        {
            DateTime? mytime = GetNextExecuteTimeAfterSpecificTime(time);
            while (!mytime.HasValue && time > FromDate)
            {
                time = SubtractInterval(time);
                mytime = GetNextExecuteTimeAfterSpecificTime(time);
            }

            if (!mytime.HasValue)
            {
                return mytime;
            }

            DateTime rst = SubtractInterval(mytime.Value);
            if (rst < FromDate)
            {
                return new DateTime?();
            }
            return rst;
        }

        protected abstract DateTime SubtractInterval(DateTime time);
    }
}
