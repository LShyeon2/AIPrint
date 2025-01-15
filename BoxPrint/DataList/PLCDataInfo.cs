using PLCProtocol.DataClass;
using System.Collections.Concurrent;

namespace BoxPrint.DataList
{
    public class PLCDataInfo
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

        private string _ModuleID;
        public string ModuleID
        {
            get
            {
                return _ModuleID;
            }
            set
            {
                if (_ModuleID == value) return;

                _ModuleID = value;



            }
        }

        private void test(string PLCData)
        {
            string[] phoneNumberSplit = PLCData.Split('/');




        }


        private eAreaType _Direction;
        public eAreaType Direction
        {
            get
            {
                return _Direction;
            }
            set
            {
                _Direction = value;
            }
        }

        private string _PLCData;
        public string PLCData
        {
            get
            {
                return _PLCData;
            }
            set
            {
                _PLCData = value;
            }
        }

        private ConcurrentDictionary<string, string> _DicPLCData = new ConcurrentDictionary<string, string>();
        public ConcurrentDictionary<string, string> DicPLCData
        {
            get => _DicPLCData;
            set
            {




            }
        }
    }
}
