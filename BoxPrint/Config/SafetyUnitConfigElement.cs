using System.Configuration;
namespace BoxPrint.Config
{
    public class SafetyUnitConfigElement : ConfigurationElement
    {
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
        [ConfigurationProperty("ModuleID", IsRequired = true)]
        public string ModuleID
        {
            get { return (string)base["ModuleID"]; }
            set { base["ModuleID"] = value; }
        }
    }
}
