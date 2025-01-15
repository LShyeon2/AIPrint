namespace BoxPrint.DataList
{
    public class ConnectClientList
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

        private string _ClientPCName;
        public string ClientPCName
        {
            get
            {
                return _ClientPCName;
            }
            set
            {
                _ClientPCName = value;
            }
        }

        private string _ClientIP;
        public string ClientIP
        {
            get
            {
                return _ClientIP;
            }
            set
            {
                _ClientIP = value;
            }
        }
    }
}
