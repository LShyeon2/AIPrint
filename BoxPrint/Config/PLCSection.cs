using System;

namespace BoxPrint.Config
{
    public class PLCSection
    {
        //public List<PLCElement> elementList = new List<PLCElement>();
        private PLCElement[] _PLCElementList = new PLCElement[1];

        ////private PLCElement _PLC1Element = new PLCElement();
        //public void Init()
        //{
        //    for (int i = 0; i < _PLCElementList.Length; i++)
        //    {
        //        _PLCElementList[i] = new PLCElement();
        //    }
        //}

        //private PLCElement _PLC1Element = new PLCElement();
        //public PLCElement PLC1Element
        //{
        //    get
        //    {
        //        return _PLC1Element;
        //    }
        //    set
        //    {
        //        _PLC1Element = value;
        //    }
        //}

        //private PLCElement _PLC2Element = new PLCElement();
        //public PLCElement PLC2Element
        //{
        //    get
        //    {
        //        return _PLC2Element;
        //    }
        //    set
        //    {
        //        _PLC2Element = value;

        //    }
        //}

        public PLCElement this[int i]
        {
            get
            {
                if (i == _PLCElementList.Length)
                {
                    Array.Resize<PLCElement>(ref _PLCElementList, _PLCElementList.Length + 1);
                    _PLCElementList[i] = new PLCElement();
                }

                if (_PLCElementList[i] == null)
                    _PLCElementList[i] = new PLCElement();

                return _PLCElementList[i];
            }
            set
            {

                _PLCElementList[i] = value;
            }
        }

        public int Count
        {
            get { return _PLCElementList.Length; }
        }

        //220917 조숭진 추가
        private bool _PLCSimulMode = false;
        public bool PLCSimulMode
        {
            get
            {
                return _PLCSimulMode;
            }
            set
            {
                _PLCSimulMode = value;
            }
        }

        private bool _UsePLC = false;
        public bool UsePLC
        {
            get 
            {
                return _UsePLC;
            }
            set 
            {
                _UsePLC = value; 
            }
        }

        //public void Add(PLCElement item)
        //{
        //    elementList.Add(item);
        //}
    }
}
