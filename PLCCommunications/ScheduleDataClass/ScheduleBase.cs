using System;

namespace PLCCommunications.ScheduleDataClass
{
    public class ScheduleBase
    {
        /// <summary>
        /// 스케쥴
        /// </summary>
        /// <returns></returns>
        public virtual bool Run()
        {
            return false;
        }

        public bool IsTimeOut(DateTime dtstart, double secTimeout)
        {
            secTimeout = secTimeout * 1000;

            TimeSpan TLimite = TimeSpan.FromMilliseconds(secTimeout);
            TimeSpan tspan = DateTime.Now.Subtract(dtstart);
            return (tspan > TLimite) ? true : false;
        }

    }
}
