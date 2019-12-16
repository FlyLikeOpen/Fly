using System;
using System.Collections.Generic;
using System.Text;

namespace IBB360.Framework.JobService.ScheduleManager
{
    [Serializable]
    public class MonthlyWeekDaySchedule : MonthlySchedule
    {
        private MonthWeekEnum m_ExecuteWeek;

        public MonthWeekEnum ExecuteWeek
        {
            get { return m_ExecuteWeek; }
            set { m_ExecuteWeek = value; }
        }

        private DayOfWeek m_ExecuteWeekDay;

        public DayOfWeek ExecuteWeekDay
        {
            get { return m_ExecuteWeekDay; }
            set { m_ExecuteWeekDay = value; }
        }

        private bool IsWeekValid(DateTime time)
        {
            if (m_ExecuteWeek == MonthWeekEnum.LastWeek)
            {
                return time.AddDays(7).Month != time.Month;
            }
            else
            {
                return time.AddDays(-7 * (int)m_ExecuteWeek).Month != time.Month
                    && time.AddDays(-7 * ((int)m_ExecuteWeek - 1)).Month == time.Month;
            }
        }

        private bool IsWeekDayValid(DateTime time)
        {
            return (time.DayOfWeek == m_ExecuteWeekDay);
        }

        protected override bool IsValid(DateTime time)
        {
            return base.IsValid(time) && IsWeekDayValid(time) && IsWeekValid(time);
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
            int diff = (time.Year - FromDate.Year) * 12 + (time.Month - FromDate.Month);
            int x = diff % IntervalMonths;
            DateTime rst;
            if (x != 0)
            {
                rst = time.AddMonths(IntervalMonths - x);
            }
            else
            {
                rst = time;
            }
            int executeDay;
            if (m_ExecuteWeek == MonthWeekEnum.LastWeek)
            {
                DateTime tmp = new DateTime(rst.Year, rst.Month, 1).AddMonths(1).AddDays(-1); // get the last date of the month.
                int diff2 = ((int)tmp.DayOfWeek - (int)m_ExecuteWeekDay);
                executeDay = tmp.Day - diff2 > 0 ? diff2 : (7 + diff2);
            }
            else
            {
                DateTime tmp = new DateTime(rst.Year, rst.Month, 1); // get the first date of the month.
                int diff2 = (int)m_ExecuteWeekDay - (int)tmp.DayOfWeek + 1;
                diff2 = diff2 > 0 ? diff2 : 7 + diff2;
                if (m_ExecuteWeek == MonthWeekEnum.FirstWeek)
                {
                    executeDay = diff2;
                }
                else
                {
                    executeDay = (7 - (int)tmp.DayOfWeek) + (((int)m_ExecuteWeek) - 2) * 7 + ((int)m_ExecuteWeekDay + 1);
                }
            }
            rst = new DateTime(rst.Year, rst.Month, executeDay).Add(TimeSpan.Parse(ExecuteTime));
            if (rst < time)
            {
                rst = rst.AddMonths(IntervalMonths);
            }
            return rst;
        }

        public override string ScheduleTypeName
        {
            get { return "Monthly Week Day Schedule"; }
        }
    }
}
