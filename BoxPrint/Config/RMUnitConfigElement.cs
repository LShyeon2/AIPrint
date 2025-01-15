using System.Configuration;
namespace BoxPrint.Config
{
    public class RMUnitConfigElement : ConfigurationElement
    {
        [ConfigurationProperty("ModuleName", IsRequired = true)]
        public string ModuleName
        {
            get { return (string)base["ModuleName"]; }
            set { base["ModuleName"] = value; }
        }

        [ConfigurationProperty("CraneID", IsRequired = false)]
        public string CraneID
        {
            get { return (string)base["CraneID"]; }
            set { base["CraneID"] = value; }
        }
        [ConfigurationProperty("RMType", IsRequired = true)]
        public eRMType RMType
        {
            get { return (eRMType)base["RMType"]; }
            set { base["RMType"] = value; }
        }

        [ConfigurationProperty("IPAddress", IsRequired = true)]
        public string IPAddress
        {
            get { return (string)base["IPAddress"]; }
            set { base["IPAddress"] = value; }
        }
        [ConfigurationProperty("ForkAxisValueReverse", IsRequired = false)]
        public bool ForkAxisValueReverse
        {
            get { return (bool)base["ForkAxisValueReverse"]; }
            set { base["ForkAxisValueReverse"] = value; }
        }

        [ConfigurationProperty("AutoStartSpeed", IsRequired = true)]
        public int AutoStartSpeed
        {
            get { return (int)base["AutoStartSpeed"]; }
            set { base["AutoStartSpeed"] = value; }
        }

        [ConfigurationProperty("DoorUnlockSpeed", IsRequired = true)]
        public int DoorUnlockSpeed
        {
            get { return (int)base["DoorUnlockSpeed"]; }
            set { base["DoorUnlockSpeed"] = value; }
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

        //210105 lsj 컨피그 추가
        [ConfigurationProperty("IOSimulMode", IsRequired = true)]
        public bool IOSimulMode
        {
            get { return (bool)base["IOSimulMode"]; }
            set { base["IOSimulMode"] = value; }
        }
        //220628 HHJ SCS 개선     //- PLCDataItems 개선
        [ConfigurationProperty("PLCNum", IsRequired = true)]
        public int PLCNum
        {
            get { return (int)base["PLCNum"]; }
            set { base["PLCNum"] = value; }
        }
        [ConfigurationProperty("PLCReadOffset", IsRequired = true)]
        public int PLCReadOffset
        {
            get { return (int)base["PLCReadOffset"]; }
            set { base["PLCReadOffset"] = value; }
        }
        [ConfigurationProperty("PLCWriteOffset", IsRequired = true)]
        public int PLCWriteOffset
        {
            get { return (int)base["PLCWriteOffset"]; }
            set { base["PLCWriteOffset"] = value; }
        }
    }
}
