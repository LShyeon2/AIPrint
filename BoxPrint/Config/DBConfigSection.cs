using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

namespace BoxPrint.Config
{
    public class DBConfigSection : ConfigurationSection
    {
        private ConfigurationProperty _DBFirstConnIP = new ConfigurationProperty("DBFirstConnIP", typeof(string));
        [ConfigurationProperty("DBFirstConnIP", IsRequired = true)]
        public string DBFirstConnIP
        {
            get { return (string)base[_DBFirstConnIP]; }
            set { base[_DBFirstConnIP] = _DBFirstConnIP; }
        }

        private ConfigurationProperty _DBFirstConnPort = new ConfigurationProperty("DBFirstConnPort", typeof(string));
        [ConfigurationProperty("DBFirstConnPort", IsRequired = true)]
        public string DBFirstConnPort
        {
            get { return (string)base[_DBFirstConnPort]; }
            set { base[_DBFirstConnPort] = _DBFirstConnPort; }
        }

        private ConfigurationProperty _DBFirstConnServiceName = new ConfigurationProperty("DBFirstConnServiceName", typeof(string));
        [ConfigurationProperty("DBFirstConnServiceName", IsRequired = true)]
        public string DBFirstConnServiceName
        {
            get { return (string)base[_DBFirstConnServiceName]; }
            set { base[_DBFirstConnServiceName] = _DBFirstConnServiceName; }
        }

        private ConfigurationProperty _DBSecondConnIP = new ConfigurationProperty("DBSecondConnIP", typeof(string));
        [ConfigurationProperty("DBSecondConnIP", IsRequired = true)]
        public string DBSecondConnIP
        {
            get { return (string)base[_DBSecondConnIP]; }
            set { base[_DBSecondConnIP] = _DBSecondConnIP; }
        }

        private ConfigurationProperty _DBSecondConnPort = new ConfigurationProperty("DBSecondConnPort", typeof(string));
        [ConfigurationProperty("DBSecondConnPort", IsRequired = true)]
        public string DBSecondConnPort
        {
            get { return (string)base[_DBSecondConnPort]; }
            set { base[_DBSecondConnPort] = _DBSecondConnPort; }
        }

        private ConfigurationProperty _DBSecondConnServiceName = new ConfigurationProperty("DBSecondConnServiceName", typeof(string));
        [ConfigurationProperty("DBSecondConnServiceName", IsRequired = true)]
        public string DBSecondConnServiceName
        {
            get { return (string)base[_DBSecondConnServiceName]; }
            set { base[_DBSecondConnServiceName] = _DBSecondConnServiceName; }
        }

        private ConfigurationProperty _DBAccountName = new ConfigurationProperty("DBAccountName", typeof(string));
        [ConfigurationProperty("DBAccountName", IsRequired = true)]
        public string DBAccountName
        {
            get { return (string)base[_DBAccountName]; }
            set { base[_DBAccountName] = _DBAccountName; }
        }

        private ConfigurationProperty _DBPassword = new ConfigurationProperty("DBPassword", typeof(string));
        [ConfigurationProperty("DBPassword", IsRequired = true)]
        public string DBPassword
        {
            get { return (string)base[_DBPassword]; }
            set { base[_DBPassword] = _DBPassword; }
        }

    }
}
