//220525 HHJ SCS 개선     //- PLC MxVer5 Update
using System.Configuration;

namespace PLCProtocol.DataClass
{
    //20220728 조숭진 config 방식 변경으로 PLCSection -> PLCConfigSection으로 변경.
    public class PLCConfigSection : ConfigurationSection
    {
        public const string SECTION_NAME = @"Plcs";
        private static Configuration _cfg;

        public static string ConfigFile { get; set; }

        private static ConfigurationProperty _propModule;
        private static ConfigurationPropertyCollection _properties;

        //220917 조숭진 추가 s
        private static ConfigurationProperty _PLCSimulMode; //= new ConfigurationProperty("PLCSimulMode", typeof(bool));
        [ConfigurationProperty("PLCSimulMode", IsRequired = true)]
        public bool PLCSimulMode
        {
            get { return (bool)base[_PLCSimulMode]; }
            set { base[_PLCSimulMode] = _PLCSimulMode; }
        }
        //220917 조숭진 추가 e

        #region Section내 Collection 호출 Methods
        //Config내 Collection을 생성하기 위해 Config SectionGet전에 생성이 이뤄줘야한다.
        static PLCConfigSection()
        {
            _cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            _propModule = new ConfigurationProperty(string.Empty, typeof(PLCElementCollection), null,
                ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsDefaultCollection);

            _properties = new ConfigurationPropertyCollection();
            _properties.Add(_propModule);

            //220917 조숭진 추가 s
            _PLCSimulMode = new ConfigurationProperty("PLCSimulMode", typeof(bool), null,
                ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsDefaultCollection);

            _properties.Add(_PLCSimulMode);
            //220917 조숭진 추가 e
        }

        public PLCElementCollection Plcs
        {
            get { return (PLCElementCollection)base[_propModule]; }
        }

        protected override ConfigurationPropertyCollection Properties
        {
            get { return _properties; }
        }
        #endregion

        #region Section 호출 Methods
        public static PLCConfigSection Get()
        {
            if (string.IsNullOrEmpty(ConfigFile) || !System.IO.File.Exists(ConfigFile))
                return null;

            _cfg = GetConfiguration(ConfigFile);
            PLCConfigSection section = _cfg.GetSection(SECTION_NAME) as PLCConfigSection;
            return section;
        }

        public void Save()
        {
            if (string.IsNullOrEmpty(ConfigFile) || !System.IO.File.Exists(ConfigFile))
                return;

            if (_cfg == null)
                _cfg = GetConfiguration(ConfigFile);

            SectionInformation.ForceSave = true;
            _cfg.Save(ConfigurationSaveMode.Full);
        }

        private static Configuration GetConfiguration(string configFile)
        {
            ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap();
            fileMap.ExeConfigFilename = configFile;
            Configuration cfg =
                ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);

            return cfg;
        }
        #endregion
    }
}
