
using System.Configuration;

namespace BoxPrint.Config
{
    public class MainConfigSection : ConfigurationSection
    {

        private ConfigurationProperty _LineSite = new ConfigurationProperty("LineSite", typeof(bool));
        [ConfigurationProperty("LineSite", IsRequired = true)]
        public eLineSite LineSite
        {
            get { return (eLineSite)base[_LineSite]; }
            set { base[_LineSite] = _LineSite; }
        }


        private ConfigurationProperty _SCSType = new ConfigurationProperty("SCSType", typeof(bool));
        [ConfigurationProperty("SCSType", IsRequired = true)]
        public eSCSType SCSType
        {
            get { return (eSCSType)base[_SCSType]; }
            set { base[_SCSType] = _SCSType; }
        }

        private ConfigurationProperty _GlobalSimulMode = new ConfigurationProperty("GlobalSimulMode", typeof(bool));
        [ConfigurationProperty("GlobalSimulMode", IsRequired = true)]
        public bool GlobalSimulMode
        {
            get { return (bool)base[_GlobalSimulMode]; }
            set { base[_GlobalSimulMode] = _GlobalSimulMode; }
        }

        private ConfigurationProperty _UseServoSystem = new ConfigurationProperty("UseServoSystem", typeof(bool));
        [ConfigurationProperty("UseServoSystem", IsRequired = true)]
        public bool UseServoSystem
        {
            get { return (bool)base[_UseServoSystem]; }
            set { base[_UseServoSystem] = _UseServoSystem; }
        }

        //2021.04.06 lim,
        private ConfigurationProperty _UseBCR = new ConfigurationProperty("UseBCR", typeof(bool));
        //2021.04.06 lim,
        [ConfigurationProperty("UseBCR", IsRequired = true)]
        public bool UseBCR
        {
            get { return (bool)base[_UseBCR]; }
            set { base[_UseBCR] = _UseBCR; }
        }

        private ConfigurationProperty _EQPID = new ConfigurationProperty("EQPID", typeof(string));

        [ConfigurationProperty("EQPID", IsRequired = true)]
        public string EQPID
        {
            get { return (string)base[_EQPID]; }
            set { base[_EQPID] = _EQPID; }
        }

        //SuHwan_20220929 : [ServerClient] 
        private ConfigurationProperty _ServerClientType = new ConfigurationProperty("ServerClientType", typeof(bool));
        [ConfigurationProperty("ServerClientType", IsRequired = true)]
        public eServerClientType ServerClientType
        {
            get { return (eServerClientType)base[_ServerClientType]; }
            set { base[_ServerClientType] = _ServerClientType; }
        }

        //230222 조숭진 eqp_info table을 위한 속성값 추가 s
        private ConfigurationProperty _EqpName = new ConfigurationProperty("EqpName", typeof(string));
        [ConfigurationProperty("EqpName", IsRequired = true)]
        public string EqpName
        {
            get { return (string)base[_EqpName]; }
            set { base[_EqpName] = _EqpName; }
        }

        private ConfigurationProperty _EqpNumber = new ConfigurationProperty("EqpNumber", typeof(bool));
        [ConfigurationProperty("EqpNumber", IsRequired = true)]
        public string EqpNumber
        {
            get { return (string)base[_EqpNumber]; }
            set { base[_EqpNumber] = _EqpNumber; }
        }
        //230222 조숭진 eqp_info table을 위한 속성값 추가 e

        private ConfigurationProperty _FrontBankNum = new ConfigurationProperty("FrontBankNum", typeof(bool));
        [ConfigurationProperty("FrontBankNum", IsRequired = true)]
        public int FrontBankNum
        {
            get { return (int)base[_FrontBankNum]; }
            set { base[_FrontBankNum] = _FrontBankNum; }
        }

        private ConfigurationProperty _RearBankNum = new ConfigurationProperty("RearBankNum", typeof(bool));
        [ConfigurationProperty("RearBankNum", IsRequired = true)]
        public int RearBankNum
        {
            get { return (int)base[_RearBankNum]; }
            set { base[_RearBankNum] = _RearBankNum; }
        }

        private ConfigurationProperty _IntegratedMap = new ConfigurationProperty("IntegratedMap", typeof(bool));
        [ConfigurationProperty("IntegratedMap", IsRequired = false)]
        public bool IntegratedMap
        {
            get { return (bool)base[_IntegratedMap]; }
            set { base[_IntegratedMap] = _IntegratedMap; }
        }
        //231205 HHJ SCS 개선     //- Log 저장기한에 따른 삭제기능 추가
        /// <summary>
        /// Log 저장 기한
        /// </summary>
        private ConfigurationProperty _LogStoragePeriod = new ConfigurationProperty("LogStoragePeriod", typeof(int));
        [ConfigurationProperty("LogStoragePeriod", IsRequired = false , DefaultValue = 30)]
        public int LogStoragePeriod
        {
            get { return (int)base[_LogStoragePeriod]; }
            set { base[_LogStoragePeriod] = _LogStoragePeriod; }
        }

        //2024.07.31 lim, AutoKeyin 사용 설정 추가
        private ConfigurationProperty _UseAutoKeyin = new ConfigurationProperty("UseAutoKeyin", typeof(bool));
        [ConfigurationProperty("UseAutoKeyin", IsRequired = true, DefaultValue = false)]
        public bool UseAutoKeyin
        {
            get { return (bool)base[_UseAutoKeyin]; }
            set { base[_UseAutoKeyin] = _UseAutoKeyin; }
        }

        [ConfigurationProperty("Booth")]
        public BoothConfigElement BoothElement
        {
            get
            {
                return (BoothConfigElement)this["Booth"];
            }
            set
            {
                value = (BoothConfigElement)this["Booth"];
            }
        }

        [ConfigurationProperty("Scheduler")]
        public SchedulerConfigElement SchedulerElement
        {
            get
            {
                return (SchedulerConfigElement)this["Scheduler"];
            }
            set
            {
                value = (SchedulerConfigElement)this["Scheduler"];
            }
        }
    }
}