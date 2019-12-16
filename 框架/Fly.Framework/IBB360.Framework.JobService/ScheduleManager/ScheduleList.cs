using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace IBB360.Framework.JobService.ScheduleManager
{
    [Serializable]
    [XmlRoot("scheduleList")]
    public class ScheduleList
    {
        [XmlElement("schedule")]
        public List<Schedule> Schedules
        {
            get;
            set;
        }

        public bool CanRun(DateTime localTime)
        {
            if (Schedules == null || Schedules.Count <= 0)
            {
                return false;
            }
            foreach (Schedule schedule in Schedules)
            {
                if (schedule.Enable && schedule.Check(localTime))
                {
                    return true;
                }
            }
            return false;
        }

        public DateTime? GetNextExecuteLocalTime(DateTime localTime)
        {
            if (Schedules == null || Schedules.Count <= 0)
            {
                return new DateTime?();
            }
            DateTime? minDateTime = Schedules[0].GetNextExecuteLocalTimeFrom(localTime);
            for (int i = 1; i < Schedules.Count; i++)
            {
                DateTime? tmp = Schedules[i].GetNextExecuteLocalTimeFrom(localTime);
                if (tmp.HasValue)
                {
                    if (minDateTime.HasValue)
                    {
                        minDateTime = (minDateTime.Value < tmp.Value) ? minDateTime : tmp;
                    }
                    else
                    {
                        minDateTime = tmp;
                    }
                }
            }
            return minDateTime;
        }

        public DateTime? LastExecuteLocalTime
        {
            get
            {
                if (Schedules == null || Schedules.Count <= 0)
                {
                    return new DateTime?();
                }
                DateTime current = DateTime.Now;
                DateTime? maxDateTime = Schedules[0].GetLastExecuteLocalTimeTo(current);
                for (int i = 1; i < Schedules.Count; i++)
                {
                    DateTime? tmp = Schedules[i].GetLastExecuteLocalTimeTo(current);
                    if (tmp.HasValue)
                    {
                        if (maxDateTime.HasValue)
                        {
                            maxDateTime = (maxDateTime.Value > tmp.Value) ? maxDateTime : tmp;
                        }
                        else
                        {
                            maxDateTime = tmp;
                        }
                    }
                }
                return maxDateTime;
            }
        }
    }
}
