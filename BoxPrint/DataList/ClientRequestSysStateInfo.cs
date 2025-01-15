namespace BoxPrint.DataList
{
    public class ClientRequestSysStateInfo
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

        private string _RequestSignal;
        public string RequestSignal
        {
            get
            {
                return _RequestSignal;
            }
            set
            {
                _RequestSignal = value;
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

    }
}
