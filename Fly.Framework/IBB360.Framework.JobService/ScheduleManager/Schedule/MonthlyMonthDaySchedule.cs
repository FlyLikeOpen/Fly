using System;
using System.Collections.Generic;
using System.Text;

namespace IBB360.Framework.JobService.ScheduleManager
{
    [Serializable]
    public class MonthlyMonthDaySchedule : MonthlySchedule
    {
        private int m_ExecuteDay;

        public int ExecuteDay
        {
            get { return m_ExecuteDay; }
            set
            {
                if (value > 31 || value < 1)
                {
                    throw new ApplicationException("Invaild day of month.");
                }
                m_ExecuteDay = value;
            }
        }

        private bool IsMonthDayValid(DateTime time)
        {
            return (m_ExecuteDay == 0 && time.Month != time.AddDays(1).Month)
                || (time.Day == m_ExecuteDay);
        }

        protected override bool IsValid(DateTime time)
        {
            return base.IsValid(time) && IsMonthDayValid(time);
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
            rst = new DateTime(rst.Year, rst.Month, m_ExecuteDay).Add(TimeSpan.Parse(ExecuteTime));
            if (rst < time)
            {
                rst = rst.AddMonths(IntervalMonths);
            }
            return rst;
        }

        public override string ScheduleTypeName
        {
            get { return "Monthly Month Day Schedule"; }
        }
    }
}
