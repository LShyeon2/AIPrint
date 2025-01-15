using PLCProtocol.DataClass;

namespace BoxPrint.Config
{
    public class PLCElement
    {
        private short _Num = -1;
        public short Num
        {
            get { return _Num; }
            set { _Num = value; }
        }

        private short _BackupNum = -1;
        public short BackupNum
        {
            get { return _BackupNum; }
            set { _BackupNum = value; }
        }


        private string _PLCName = string.Empty;
        public string PLCName
        {
            get { return _PLCName; }
            set { _PLCName = value; }
        }

        private ePLCSeries _Series = ePLCSeries.MxCom;
        public ePLCSeries Series
        {
            get { return _Series; }
            set { _Series = value; }
        }

        private eCommunicationType _ComType = eCommunicationType.None;
        public eCommunicationType ComType
        {
            get { return _ComType; }
            set { _ComType = value; }
        }

        private ePLCFrame _Frame = ePLCFrame.None;
        public ePLCFrame Frame
        {
            get { return _Frame; }
            set { _Frame = value; }
        }

        private string _Ip = string.Empty;
        public string Ip
        {
            get { return _Ip; }
            set { _Ip = value; }
        }

        private short _Port = -1;
        public short Port
        {
            get { return _Port; }
            set { _Port = value; }
        }

    }
}
