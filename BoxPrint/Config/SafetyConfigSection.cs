
using System.Configuration;

namespace BoxPrint.Config
{
    public class SafetyConfigSection : ConfigurationSection
    {
        private ConfigurationProperty _SimulMode = new ConfigurationProperty("SimulMode", typeof(bool));
        private ConfigurationProperty _IOFile = new ConfigurationProperty("IOFile", typeof(string));


        [ConfigurationProperty("IOFile", IsRequired = true)]
        public string IOFile
        {
            get { return (string)base[_IOFile]; }
            set { base[_IOFile] = _IOFile; }
        }

        [ConfigurationProperty("SimulMode", IsRequired = true)]
        public bool SimulMode
        {
            get { return (bool)base[_SimulMode]; }
            set { base[_SimulMode] = _SimulMode; }
        }

        [ConfigurationProperty("Safety_Booth")]
        public SafetyUnitConfigElement SafetyBooth_PLCElement
        {
            get
            {
                return (SafetyUnitConfigElement)this["Safety_Booth"];
            }
            set
            {
                value = (SafetyUnitConfigElement)this["Safety_Booth"];
            }
        }

        [ConfigurationProperty("Safety_RM")]
        public SafetyUnitConfigElement SafetyRM_PLCElement
        {
            get
            {
                return (SafetyUnitConfigElement)this["Safety_RM"];
            }
            set
            {
                value = (SafetyUnitConfigElement)this["Safety_RM"];
            }
        }


    }
}