using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IBB360.Framework.JobService.ScheduleManager
{
    [Serializable]
    public class IntervalScheduler : Schedule
    {
        public DateTime FromDate
        {
            get;
            set;
        }

        public DateTime ToDate
        {
            get;
            set;
        }

        public string FromTimeOfDay
        {
            get;
            set;
        }

        public string ToTimeOfDay
        {
            get;
            set;
        }

        public int IntervalSeconds
        {
            get;
            set;
        }

        private bool InValidPeriod(DateTime time)
        {
            TimeSpan startTime = TimeSpan.Parse(FromTimeOfDay);
            TimeSpan endTime = TimeSpan.Parse(ToTimeOfDay);
            bool rst = time >= FromDate.Date && time < ToDate.Date;
            if (rst == false)
            {
                return false;
            }
            return IsTimeOfDayValid(time, startTime, endTime);
        }

        private bool IsTimeOfDayValid(DateTime time, TimeSpan startTime, TimeSpan endTime)
        {
            if (startTime < endTime) // eg. like 9:00 to 18:00
            {
                return (time.TimeOfDay >= startTime && time.TimeOfDay < endTime);
            }
            else if (startTime == endTime)
            {
                return time.TimeOfDay == startTime;
            }
            else // eg. like 22:00 to 5:00
            {
                return (time.TimeOfDay >= startTime || time.TimeOfDay < endTime);
            }
        }

        public override bool Check(DateTime dateTime)
        {
            DateTime newTime = GetSpecialTimeZoneTime(dateTime);
            if (InValidPeriod(newTime) == false)
            {
                return false;
            }
            DateTime start = FromDate.Date.Add(TimeSpan.Parse(FromTimeOfDay));
            double total = newTime.Subtract(start).TotalSeconds;
            return Convert.ToInt32(total) % IntervalSeconds == 0;
        }

        public override DateTime? GetNextExecuteTimeAfterSpecificTime(DateTime time)
        {
            TimeSpan startTime = TimeSpan.Parse(FromTimeOfDay);
            TimeSpan endTime = TimeSpan.Parse(ToTimeOfDay);
            DateTime start = FromDate.Date.Add(startTime);
            DateTime end = ToDate.Date.AddDays(-1).Add(endTime);
            if (time < start)
            {
                return start;
            }
            do
            {
                if (startTime < endTime) // eg. like 9:30:00 to 18:30:00
                {
                    if (time.TimeOfDay < startTime)
                    {
                        time = time.Date.Add(startTime);
                    }
                    else if (time.TimeOfDay >= endTime)
                    {
                        time = time.Date.AddDays(1).Add(startTime);
                    }
                }
                else // eg. like 22:30:00 to 5:30:00
                {
                    if (time.TimeOfDay >= endTime && time.TimeOfDay < startTime)
                    {
                        time = time.Date.Add(startTime);
                    }
                }
                if (time >= end)
                {
                    return new DateTime?();
                }
                double total = time.Subtract(start).TotalSeconds;
                int x = Convert.ToInt32(total) % IntervalSeconds;
                time = time.AddSeconds(IntervalSeconds - x);
                if (time >= end)
                {
                    return new DateTime?();
                }
            }
            while (!IsTimeOfDayValid(time, startTime, endTime));
            return time;
        }

        public override DateTime? GetLastExecuteTimeAfterSpecificTime(DateTime time)
        {
            TimeSpan startTime = TimeSpan.Parse(FromTimeOfDay);
            DateTime? mytime = GetNextExecuteTimeAfterSpecificTime(time);
            DateTime start = FromDate.Date.Add(startTime);
            while (!mytime.HasValue && time > start)
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

        protected DateTime SubtractInterval(DateTime time)
        {
            return time.AddSeconds(0 - IntervalSeconds);
        }

        public override string ScheduleTypeName
        {
            get { return "Period Interval Schedule"; }
        }
    }
}
