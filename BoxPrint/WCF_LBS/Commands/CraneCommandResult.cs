namespace WCF_LBS.Commands
{
    class CraneCommandResult
    {
        bool _Result;
        bool Result
        {
            get { return _Result; }
        }
        uint _NakCode;
        uint NakCode
        {
            get { return _NakCode; }
        }

        string _Reason;
        string Reason
        {
            get { return _Reason; }
        }


        public void SetReason(string Reason, uint NakCode = 0)
        {
            _Result = false;
            _NakCode = NakCode;
            _Reason = Reason;
        }
    }

}
