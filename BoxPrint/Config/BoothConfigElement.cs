using System.Configuration;
namespace BoxPrint.Config
{
    public class BoothConfigElement : ConfigurationElement
    {
        [ConfigurationProperty("TypeName", IsRequired = true)]
        public string TypeName
        {
            get { return (string)base["TypeName"]; }
            set { base["TypeName"] = value; }
        }

        [ConfigurationProperty("LightCurtainCount", IsRequired = true)]
        public int LightCurtainCount
        {
            get { return (int)base["LightCurtainCount"]; }
            set { base["LightCurtainCount"] = value; }
        }

        [ConfigurationProperty("LightCurtainSyncNumber", DefaultValue = 0, IsRequired = false)]
        public int LightCurtainSyncNumber
        {
            get { return (int)base["LightCurtainSyncNumber"]; }
            set { base["LightCurtainSyncNumber"] = value; }
        }

        [ConfigurationProperty("EMSCount", IsRequired = true)]
        public int EMSCount
        {
            get { return (int)base["EMSCount"]; }
            set { base["EMSCount"] = value; }
        }

        [ConfigurationProperty("PLCNum", DefaultValue = 0, IsRequired = false)]
        public int PLCNum
        {
            get { return (int)base["PLCNum"]; }
            set { base["PLCNum"] = value; }
        }
        [ConfigurationProperty("PLCWriteOffset", DefaultValue = 0, IsRequired = false)]
        public int PLCWriteOffset
        {
            get { return (int)base["PLCWriteOffset"]; }
            set { base["PLCWriteOffset"] = value; }
        }
        [ConfigurationProperty("PLCReadOffset", DefaultValue = 0, IsRequired = false)]
        public int PLCReadOffset
        {
            get { return (int)base["PLCReadOffset"]; }
            set { base["PLCReadOffset"] = value; }
        }
    }
}
