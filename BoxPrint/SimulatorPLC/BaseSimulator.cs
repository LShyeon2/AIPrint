using PLCProtocol.DataClass;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text;
using System.Threading;

namespace BoxPrint.SimulatorPLC
{
    public abstract class BaseSimulator : INotifyPropertyChanged
    {
        protected bool SelfAlarmClearReq = false;
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        public string SPLC_Name
        {
            get;
            set;
        }
        public string PLCModuleType
        {
            get;
            set;
        }
        public string RunState
        {
            get
            {
                return PLCRunState ? "Running" : "Stop";
            }
        }
        private int _ActionStep = 0;
        public int ActionStep
        {
            get
            {
                return _ActionStep;
            }
            set
            {
                _ActionStep = value;
                //Log.LogManager.WriteConsoleLog(eLogLevel.Info, "PortSimul {0} Enter ActionStep : {1} Changed", SPLC_Name, _ActionStep);
                OnPropertyChanged(new PropertyChangedEventArgs("ActionStep"));
            }
        }

        protected bool[] _DebugTestModes = new bool[5];
        public string DebugTestModes
        { 
            get
            {
                StringBuilder sb = new StringBuilder();
                foreach(bool b in _DebugTestModes)
                {
                    if (b)
                    {
                        sb.Append("1 ");
                    }
                    else
                    {
                        sb.Append("0 ");
                    }
                }
                return sb.ToString();
            }
        }

        private bool _PLCRunState;
        protected bool PLCRunState
        {
            get
            {
                return _PLCRunState;
            }
            set
            {
                _PLCRunState = value;
                OnPropertyChanged(new PropertyChangedEventArgs("PLCRunState"));
                OnPropertyChanged(new PropertyChangedEventArgs("RunState"));
            }
        }

        public virtual string CarrierID
        {
            get
            {
                return string.Empty;
            }
        }
        protected int PLCCycleTime = 50;
        protected int SimulCycleTime = 2000;
        protected bool PLCExit = false;

        protected ConcurrentDictionary<string, PLCDataItem> PLCtoPC = new ConcurrentDictionary<string, PLCDataItem>();
        protected ConcurrentDictionary<string, PLCDataItem> PCtoPLC = new ConcurrentDictionary<string, PLCDataItem>();
        protected GlobalData GData = GlobalData.Current;
        protected Thread PLCSimulRun;
        protected short CurrentAlarmCode = 0;
        protected short CurrentWarninCode = 0;
        protected bool PLCPause = false;



        public virtual void SetPLCAddress(int plcNum, int PLCReadOffset, int PLCWriteOffset)
        {

        }
        public void StartSimulPLC()
        {
            if (PLCSimulRun == null)
            {
                PLCSimulRun = new Thread(new ThreadStart(PLCAutoCycleRun));
                PLCSimulRun.IsBackground = true;
                PLCSimulRun.Name = this.SPLC_Name + "-Thread";
                PLCSimulRun.Start();
            }
            PLCRunState = true;
        }
        public void StopSimulPLC()
        {
            PLCRunState = false;
        }
        public virtual void PLCAutoCycleRun()
        {
            return;
        }
        public bool IsTimeOut(DateTime dtstart, double secTimeout)
        {
            secTimeout = secTimeout * 1000;

            TimeSpan TLimite = TimeSpan.FromMilliseconds(secTimeout);
            TimeSpan tspan = DateTime.Now.Subtract(dtstart);
            return (tspan > TLimite) ? true : false;
        }
        public void SetAlarmCode(short Alarm)
        {
            CurrentAlarmCode = Alarm;

        }
        public void TryAlarmClear()
        {
            SelfAlarmClearReq = true;
        }
    }
}
