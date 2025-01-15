using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoxPrint.Config
{
    public class DBSection
    {
        private string _DBFirstConnIP;
        public string DBFirstConnIP
        {
            get
            {
                return _DBFirstConnIP;
            }
            set
            {
                _DBFirstConnIP = value;
            }
        }

        private string _DBFirstConnPort;
        public string DBFirstConnPort
        {
            get 
            {
                return _DBFirstConnPort;
            }
            set 
            {
                _DBFirstConnPort = value;
            }
        }

        private string _DBFirstConnServiceName;
        public string DBFirstConnServiceName
        {
            get 
            {
                return _DBFirstConnServiceName;
            }
            set 
            {
                _DBFirstConnServiceName = value;
            }
        }

        private string _DBSecondConnIP;
        public string DBSecondConnIP
        {
            get 
            {
                return _DBSecondConnIP;
            }
            set 
            {
                _DBSecondConnIP = value;
            }
        }

        private string _DBSecondConnPort;
        public string DBSecondConnPort
        {
            get 
            {
                return _DBSecondConnPort;
            }
            set 
            {
                _DBSecondConnPort = value;
            }
        }

        private string _DBSecondConnServiceName;
        public string DBSecondConnServiceName
        {
            get 
            {
                return _DBSecondConnServiceName;
            }
            set 
            {
                _DBSecondConnServiceName = value;
            }
        }

        private string _DBAccountName;
        public string DBAccountName
        {
            get 
            {
                return _DBAccountName;
            }
            set 
            {
                _DBAccountName = value;
            }
        }

        private string _DBPassword;
        public string DBPassword
        {
            get 
            {
                return _DBPassword;
            }
            set 
            {
                _DBPassword = value;
            }
        }

    }
}
