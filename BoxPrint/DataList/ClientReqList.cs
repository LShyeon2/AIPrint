namespace BoxPrint.DataList
{
    public class ClientReqList
    {
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

        private string _CMDType;
        public string CMDType
        {
            get
            {
                return _CMDType;
            }
            set
            {
                _CMDType = value;
            }
        }

        private string _Target;
        public string Target
        {
            get
            {
                return _Target;
            }
            set
            {
                _Target = value;
            }
        }

        private string _TargetID;
        public string TargetID
        {
            get
            {
                return _TargetID;
            }
            set
            {
                _TargetID = value;
            }
        }

        private string _TargetVAR;
        public string TargetVAR
        {
            get
            {
                return _TargetVAR;
            }
            set
            {
                _TargetVAR = value;
            }
        }

        private string _TargetValue;
        public string TargetValue
        {
            get
            {
                return _TargetValue;
            }
            set
            {
                _TargetValue = value;
            }
        }

        private string _ReqTime;
        public string ReqTime
        {
            get
            {
                return _ReqTime;
            }
            set
            {
                _ReqTime = value;
            }
        }

        private eServerClientType _Requester;
        public eServerClientType Requester
        {
            get
            {
                return _Requester;
            }
            set
            {
                _Requester = value;
            }
        }

        private string _JobID;
        public string JobID
        {
            get
            {
                return _JobID;
            }
            set
            {
                _JobID = value;
            }
        }

        private string _ClientID;
        public string ClientID
        {
            get
            {
                return _ClientID;
            }
            set
            {
                _ClientID = value;
            }
        }
    }
}
