namespace BoxPrint.DataList
{
    public class EQPInfo
    {
        private string _EQPName;
        public string EQPName
        {
            get
            {
                return _EQPName;
            }
            set
            {
                _EQPName = value;
            }
        }

        private string _EQPID;
        public string EQPID
        {
            get
            {
                return _EQPID;
            }
            set
            {
                _EQPID = value;
            }
        }

        private string _EQPNumber;
        public string EQPNumber
        {
            get
            {
                return _EQPNumber;
            }
            set
            {
                _EQPNumber = value;
            }
        }

        private string _MCS_State = string.Empty;
        public string MCS_State
        {
            get
            {
                return _MCS_State;
            }
            set
            {
                _MCS_State = value;
            }
        }

        private string _SCS_State = string.Empty;
        public string SCS_State
        {
            get
            {
                return _SCS_State;
            }
            set
            {
                _SCS_State = value;
            }
        }

        //221229 YSW Map View안에 각 SCS의 Tooltip에 IP 항목 추가
        private string _SCSIP;
        public string SCSIP
        {
            get { return _SCSIP; }
            set
            {
                if (_SCSIP != value)
                {
                    _SCSIP = value;
                }
            }
        }

        private string _PLC_State = string.Empty;
        public string PLC_State
        {
            get
            {
                return _PLC_State;
            }
            set
            {
                _PLC_State = value;
            }
        }

        private eSCState _SYSTEM_State;
        public eSCState SYSTEM_State
        {
            get
            {
                return _SYSTEM_State;
            }
            set
            {
                _SYSTEM_State = value;
            }
        }

        private string _DBFirstIP;
        public string DBFirstIP
        {
            get
            {
                return _DBFirstIP;
            }
            set
            {
                _DBFirstIP = value;
            }
        }

        private string _DBFirstPort;
        public string DBFirstPort
        {
            get
            {
                return _DBFirstPort;
            }
            set
            {
                _DBFirstPort = value;
            }
        }

        private string _DBFirstServiceName;
        public string DBFirstServiceName
        {
            get
            {
                return _DBFirstServiceName;
            }
            set
            {
                _DBFirstServiceName = value;
            }
        }

        private string _DBSecondIP;
        public string DBSecondIP
        {
            get
            {
                return _DBSecondIP;
            }
            set
            {
                _DBSecondIP = value;
            }
        }

        private string _DBSecondPort;
        public string DBSecondPort
        {
            get
            {
                return _DBSecondPort;
            }
            set
            {
                _DBSecondPort = value;
            }
        }

        private string _DBSecondServiceName;
        public string DBSecondServiceName
        {
            get
            {
                return _DBSecondServiceName;
            }
            set
            {
                _DBSecondServiceName = value;
            }
        }

        private string _DbAccount;
        public string DbAccount
        {
            get
            {
                return _DbAccount;
            }
            set
            {
                _DbAccount = value;
            }
        }

        private string _DbPassword;
        public string DbPassword
        {
            get
            {
                return _DbPassword;
            }
            set
            {
                _DbPassword = value;
            }
        }
    }
}
