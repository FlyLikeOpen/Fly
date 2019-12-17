using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Runtime.Serialization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace IBB360.Framework.JobService.ScheduleManager
{
    [XmlInclude(typeof(IntervalScheduler)),
    XmlInclude(typeof(DailySchedule)), 
    XmlInclude(typeof(MonthlyMonthDaySchedule)), 
    XmlInclude(typeof(MonthlyWeekDaySchedule)),
    XmlInclude(typeof(WeeklySchedule)),
    XmlInclude(typeof(OneTimeSchedule))]
    [Serializable]
    public abstract class Schedule
    {
        private static int s_DisparityMilliseconds = 1000;

        public static int DisparityMilliseconds
        {
            get { return s_DisparityMilliseconds; }
            set { s_DisparityMilliseconds = value; }
        }

        private string m_TimeZone;
        //private const string BEIJING_TIME_ZONE_ID = "China Standard Time";

        public string TimeZone
        {
            get { return m_TimeZone; }
            set { m_TimeZone = value; }
        }

        public string TimeZoneDisplay
        {
            get
            {
                return TimeZoneInfo.FindSystemTimeZoneById(m_TimeZone).DisplayName;
            }
        }

        private bool m_Enable;

        public bool Enable
        {
            get { return m_Enable; }
            set { m_Enable = value; }
        }

        public abstract bool Check(DateTime dateTime);

        protected DateTime GetSpecialTimeZoneTime(DateTime sourceDateTime)
        {
            return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(sourceDateTime, m_TimeZone);
        }

        public abstract DateTime? GetNextExecuteTimeAfterSpecificTime(DateTime time);
        public abstract DateTime? GetLastExecuteTimeAfterSpecificTime(DateTime time);

        public DateTime? GetNextExecuteTimeFrom(DateTime localTime)
        {
            DateTime newTime = GetSpecialTimeZoneTime(localTime);
            return GetNextExecuteTimeAfterSpecificTime(newTime);
        }

        public DateTime? GetLastExecuteTimeTo(DateTime localTime)
        {
            DateTime newTime = GetSpecialTimeZoneTime(localTime);
            return GetLastExecuteTimeAfterSpecificTime(newTime);
        }

        public DateTime? GetNextExecuteLocalTimeFrom(DateTime localTime)
        {
            DateTime? newTime = GetNextExecuteTimeFrom(localTime);
            if (newTime.HasValue)
            {
                return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(newTime.Value, m_TimeZone, TimeZoneInfo.Local.Id);
            }
            else
            {
                return new DateTime?();
            }
        }

        public DateTime? GetLastExecuteLocalTimeTo(DateTime localTime)
        {
            DateTime? newTime = GetLastExecuteTimeTo(localTime);
            if (newTime.HasValue)
            {
                return TimeZoneInfo.ConvertTimeBySystemTimeZoneId(newTime.Value, m_TimeZone, TimeZoneInfo.Local.Id);
            }
            else
            {
                return new DateTime?();
            }
        }

        public abstract string ScheduleTypeName
        {
            get;
        }
    }
}
