namespace BoxPrint.Config
{
    public class RMSection
    {
        private string _SystemName = string.Empty;
        //private RMElement[] _RMElementList = new RMElement[1];

        //public void Init()
        //{
        //    for (int i = 0; i < _RMElementList.Length; i++)
        //    {
        //        _RMElementList[i] = new RMElement();
        //    }
        //}

        public string SystemName
        {
            get { return _SystemName; }
            set { _SystemName = value; }
        }

        private RMElement _RM1Element = new RMElement();
        public RMElement RM1Element
        {
            get
            {
                return _RM1Element;
            }
            set
            {
                _RM1Element = value;
            }
        }
        private RMElement _RM2Element = new RMElement();
        public RMElement RM2Element
        {
            get
            {
                return _RM2Element;
            }
            set
            {
                _RM2Element = value;
            }
        }

        //public RMElement this[int i]
        //{
        //    get
        //    {
        //        if (i == _RMElementList.Length)
        //        {
        //            Array.Resize<RMElement>(ref _RMElementList, _RMElementList.Length + 1);
        //            _RMElementList[i] = new RMElement();
        //        }
        //        return _RMElementList[i];
        //    }
        //    set
        //    {

        //        _RMElementList[i] = value;
        //    }
        //}

        //public int Count
        //{
        //    get { return _RMElementList.Length; }
        //}
    }
}
