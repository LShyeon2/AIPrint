
using System.Configuration;

namespace BoxPrint.Config
{
    public class WPSConfigSection : ConfigurationSection
    {
        [ConfigurationProperty("Converter")]
        public WPSUnitConfigElement ConverterElement
        {
            get
            {
                return (WPSUnitConfigElement)this["Converter"];
            }
            set
            {
                value = (WPSUnitConfigElement)this["Converter"];
            }
        }

        [ConfigurationProperty("Converter_Sub")]
        public WPSUnitConfigElement ConverterSub_Element
        {
            get
            {
                return (WPSUnitConfigElement)this["Converter_Sub"];
            }
            set
            {
                value = (WPSUnitConfigElement)this["Converter_Sub"];
            }
        }

        [ConfigurationProperty("Regulator")]
        public WPSUnitConfigElement RegulatorElement
        {
            get
            {
                return (WPSUnitConfigElement)this["Regulator"];
            }
            set
            {
                value = (WPSUnitConfigElement)this["Regulator"];
            }
        }

        private ConfigurationProperty _UseWPS = new ConfigurationProperty("UseWPS", typeof(bool));
        [ConfigurationProperty("UseWPS", IsRequired = true)]
        public bool UseWPS
        {
            get { return (bool)base[_UseWPS]; }
            set { base[_UseWPS] = _UseWPS; }
        }

    }
}