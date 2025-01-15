namespace BoxPrint.Config
{
    public class BoothElement
    {
        private string _TypeName = string.Empty;
        public string TypeName
        {
            get { return _TypeName; }
            set { _TypeName = value; }
        }

        private int _LightCurtainCount = 0;
        public int LightCurtainCount
        {
            get { return _LightCurtainCount; }
            set { _LightCurtainCount = value; }
        }

        public int _LightCurtainSyncNumber = 0;
        public int LightCurtainSyncNumber
        {
            get { return _LightCurtainSyncNumber; }
            set { _LightCurtainSyncNumber = value; }
        }

        private int _EMSCount = 0;
        public int EMSCount
        {
            get { return _EMSCount; }
            set { _EMSCount = value; }
        }

        private int _PLCNum = -1;
        public int PLCNum
        {
            get { return _PLCNum; }
            set { _PLCNum = value; }
        }
        private int _PLCWriteOffset = -1;
        public int PLCWriteOffset
        {
            get { return _PLCWriteOffset; }
            set { _PLCWriteOffset = value; }
        }
        private int _PLCReadOffset = -1;
        public int PLCReadOffset
        {
            get { return _PLCReadOffset; }
            set { _PLCReadOffset = value; }
        }
    }
}
