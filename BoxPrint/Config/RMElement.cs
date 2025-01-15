namespace BoxPrint.Config
{
    public class RMElement
    {
        private string _ModuleName = string.Empty;
        public string ModuleName
        {
            get { return _ModuleName; }
            set { _ModuleName = value; }
        }

        private string _CraneID = string.Empty;
        public string CraneID
        {
            get { return _CraneID; }
            set { _CraneID = value; }
        }
        private eRMType _RMType;
        public eRMType RMType
        {
            get { return _RMType; }
            set { _RMType = value; }
        }

        private string _IPAddress = string.Empty;
        public string IPAddress
        {
            get { return _IPAddress; }
            set { _IPAddress = value; }
        }

        private int _AutoStartSpeed = 0;
        public int AutoStartSpeed
        {
            get { return _AutoStartSpeed; }
            set { _AutoStartSpeed = value; }
        }

        private int _DoorUnlockSpeed = 0;
        public int DoorUnlockSpeed
        {
            get { return _DoorUnlockSpeed; }
            set { _DoorUnlockSpeed = value; }
        }

        private int _Port = 0;
        public int Port
        {
            get { return _Port; }
            set { _Port = value; }
        }

        private bool _ForkAxisValueReverse = false;
        public bool ForkAxisValueReverse
        {
            get { return _ForkAxisValueReverse; }
            set { _ForkAxisValueReverse = value; }
        }

        private bool _SimulMode = false;
        public bool SimulMode
        {
            get { return _SimulMode; }
            set { _SimulMode = value; }
        }

        //210105 lsj 컨피그 추가
        private bool _IOSimulMode = false;
        public bool IOSimulMode
        {
            get { return _IOSimulMode; }
            set { _IOSimulMode = value; }
        }
        //220628 HHJ SCS 개선     //- PLCDataItems 개선
        private int _PLCNum = 0;
        public int PLCNum
        {
            get { return _PLCNum; }
            set { _PLCNum = value; }
        }
        private int _PLCReadOffset = 0;
        public int PLCReadOffset
        {
            get { return _PLCReadOffset; }
            set { _PLCReadOffset = value; }
        }
        private int _PLCWriteOffset = 0;
        public int PLCWriteOffset
        {
            get { return _PLCWriteOffset; }
            set { _PLCWriteOffset = value; }
        }
    }
}
