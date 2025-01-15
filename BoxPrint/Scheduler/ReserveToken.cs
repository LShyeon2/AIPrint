namespace BoxPrint.Scheduler
{
    public class ReserveToken
    {
        bool TokenLocked;

        private int _ReservedRM;
        public int ReservedRM
        {
            get
            {
                return _ReservedRM;
            }
            set
            {
                if (TokenLocked == false)
                {
                    _ReservedRM = value;
                }
            }
        }
        public ReserveToken(int initialRMNumber, bool Lock)
        {
            _ReservedRM = initialRMNumber;
            TokenLocked = Lock;
        }
    }

}
