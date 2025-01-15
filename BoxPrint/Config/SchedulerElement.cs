namespace BoxPrint.Config
{
    public class SchedulerElement
    {
        private string _TypeName = string.Empty;
        public string TypeName
        {
            get { return _TypeName; }
            set { _TypeName = value; }
        }

        private bool _UseScheduler = false;
        public bool UseScheduler
        {
            get { return _UseScheduler; }
            set { _UseScheduler = value; }
        }

        private int _WaitInCommandTime = 0;
        public int WaitInCommandTime
        {
            get { return _WaitInCommandTime; }
            set { _WaitInCommandTime = value; }
        }
        private int _AddtionalMargin = 0;
        public int AddtionalMargin
        {
            get { return _AddtionalMargin; }
            set { _AddtionalMargin = value; }
        }
    }
}
