namespace BoxPrint.Config
{
    public class MainSection
    {
        private eLineSite _LineSite = eLineSite.None;
        public eLineSite LineSite
        {
            get
            {
                return _LineSite;
            }
            set
            {
                _LineSite = value;
            }
        }

        private eSCSType _SCSType;
        public eSCSType SCSType
        {
            get
            {
                return _SCSType;
            }
            set
            {
                _SCSType = value;
            }
        }

        private bool _GlobalSimulMode = false;
        public bool GlobalSimulMode
        {
            get
            {
                return _GlobalSimulMode;
            }
            set
            {
                _GlobalSimulMode = value;
            }
        }


        private bool _UseServoSystem = false;
        public bool UseServoSystem
        {
            get
            {
                return _UseServoSystem;
            }
            set
            {
                _UseServoSystem = value;
            }
        }

        //2021.04.06 lim,
        private bool _UseBCR = false;
        //2021.04.06 lim,
        public bool UseBCR
        {
            get { return _UseBCR; }
            set { _UseBCR = value; }
        }

        private string _EQPID = string.Empty;
        public string EQPID
        {
            get { return _EQPID; }
            set { _EQPID = value; }
        }


        //SuHwan_20220929 : [ServerClientType]
        private eServerClientType _ServerClientType;
        public eServerClientType ServerClientType
        {
            get
            {
                return _ServerClientType;
            }
            set
            {
                _ServerClientType = value;
            }
        }

        //230222 조숭진 eqp_info table을 위한 속성값 추가 s
        private string _EqpName = string.Empty;
        public string EqpName
        {
            get
            {
                return _EqpName;
            }
            set
            {
                _EqpName = value;
            }
        }

        private string _EqpNumber = string.Empty;
        public string EqpNumber
        {
            get
            {
                return _EqpNumber;
            }
            set
            {
                _EqpNumber = value;
            }
        }
        //230222 조숭진 eqp_info table을 위한 속성값 추가 e

        private int _FrontBankNum;
        public int FrontBankNum
        {
            get
            {
                return _FrontBankNum;
            }
            set
            {
                _FrontBankNum = value;
            }
        }

        private int _RearBankNum;
        public int RearBankNum
        {
            get
            {
                return _RearBankNum;
            }
            set
            {
                _RearBankNum = value;
            }
        }

        private bool _IntegratedMap;
        public bool IntegratedMap
        {
            get
            {
                return _IntegratedMap;
            }
            set
            {
                _IntegratedMap = value;
            }
        }
        //231205 HHJ SCS 개선     //- Log 저장기한에 따른 삭제기능 추가
        /// <summary>
        /// Log 저장 기한
        /// </summary>
        private int _LogStoragePeriod;
        public int LogStoragePeriod
        {
            get => _LogStoragePeriod;
            set => _LogStoragePeriod = value;
        }

        //2024.07.31 lim, AutoKeyin 사용 설정 추가
        private bool _UseAutoKeyin;
        public bool UseAutoKeyin
        {
            get { return _UseAutoKeyin; }
            set { _UseAutoKeyin = value; }
        }


        private BoothElement _BoothElement = new BoothElement();
        public BoothElement BoothElement
        {
            get { return _BoothElement; }
            set { _BoothElement = value; }
        }

        private SchedulerElement _SchedulerElement = new SchedulerElement();
        public SchedulerElement SchedulerElement
        {
            get { return _SchedulerElement; }
            set { _SchedulerElement = value; }
        }

        public BoothElement BoothCopy()
        {
            return (BoothElement)this.MemberwiseClone();
        }
    }
}
