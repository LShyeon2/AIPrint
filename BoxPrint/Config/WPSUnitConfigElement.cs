using System.Configuration;
namespace BoxPrint.Config
{
    public class WPSUnitConfigElement : ConfigurationElement
    {
        [ConfigurationProperty("MonitorType", IsRequired = true)]
        public eWPSMonitorType MonitorType
        {
            get { return (eWPSMonitorType)base["MonitorType"]; }
            set { base["MonitorType"] = value; }
        }

        [ConfigurationProperty("IPAddress", IsRequired = true)]
        public string IPAddress
        {
            get { return (string)base["IPAddress"]; }
            set { base["IPAddress"] = value; }
        }
        [ConfigurationProperty("Port", IsRequired = true)]
        public int Port
        {
            get { return (int)base["Port"]; }
            set { base["Port"] = value; }
        }
        [ConfigurationProperty("SimulMode", IsRequired = true)]
        public bool SimulMode
        {
            get { return (bool)base["SimulMode"]; }
            set { base["SimulMode"] = value; }
        }

    }
}
