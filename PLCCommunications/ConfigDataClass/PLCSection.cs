//220315 HHJ SCS 개발     //- MXComponentNet, SharedMemory 개선
using System.Configuration;

namespace PLCCommunications.ConfigDataClass
{
    public class PLCSection : ConfigurationSection
    {
        public const string SECTION_NAME = @"Plcs";
        private static Configuration _cfg;

        public static string ConfigFile { get; set; }

        private static ConfigurationProperty _propModule;
        private static ConfigurationPropertyCollection _properties;

        #region Section내 Collection 호출 Methods
        //Config내 Collection을 생성하기 위해 Config SectionGet전에 생성이 이뤄줘야한다.
        static PLCSection()
        {
            _cfg = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            _propModule = new ConfigurationProperty(string.Empty, typeof(PLCElementCollection), null,
                ConfigurationPropertyOptions.IsRequired | ConfigurationPropertyOptions.IsDefaultCollection);

            _properties = new ConfigurationPropertyCollection();
            _properties.Add(_propModule);
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
        public static PLCSection Get()
        {
            if (string.IsNullOrEmpty(ConfigFile) || !System.IO.File.Exists(ConfigFile))
                return null;

            _cfg = GetConfiguration(ConfigFile);
            PLCSection section = _cfg.GetSection(SECTION_NAME) as PLCSection;
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
