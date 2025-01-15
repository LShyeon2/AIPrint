using System.Configuration;
namespace BoxPrint.Config
{
    public class SchedulerConfigElement : ConfigurationElement
    {
        [ConfigurationProperty("TypeName", IsRequired = true)]
        public string TypeName
        {
            get { return (string)base["TypeName"]; }
            set { base["TypeName"] = value; }
        }

        [ConfigurationProperty("UseScheduler", IsRequired = true)]
        public bool UseScheduler
        {
            get { return (bool)base["UseScheduler"]; }
            set { base["UseScheduler"] = value; }
        }
        [ConfigurationProperty("WaitInCommandTime", IsRequired = false, DefaultValue = 30)]
        public int WaitInCommandTime
        {
            get { return (int)base["WaitInCommandTime"]; }
            set { base["WaitInCommandTime"] = value; }
        }
        [ConfigurationProperty("AddtionalMargin", IsRequired = false, DefaultValue = 4)]
        public int AddtionalMargin
        {
            get { return (int)base["AddtionalMargin"]; }
            set { base["AddtionalMargin"] = value; }
        }
    }
}
