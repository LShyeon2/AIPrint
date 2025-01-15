using PLCProtocol.Base;
using PLCProtocol.DataClass;
using BoxPrint.CCLink;
using BoxPrint.DataList;
using BoxPrint.Log;
using BoxPrint.Modules.Shelf;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using WCF_LBS.Commands;

namespace BoxPrint.Modules.RM
{
    public abstract class RMModuleBase : ModuleBase, ICarrierStoreAble
    {
        #region Event & EventArgs
        public event EventHandler<ProcessEventArgs> ProcessCompleted;

        public event EventHandler<ShelfUpDateEventArgs> OnShelfUpdate;

        public class ProcessEventArgs : EventArgs
        {
            public bool IsSuccessful { get; set; }
            public DateTime CompletionTime { get; set; }
            public CraneCommand cmd { get; set; }

            // 2021.02.22 RGJ
            //소모품 관리 변수 업데이트
            public int Fork_Moving_Distance;
            public int Drive_Moving_Distance;
            public int Z_Moving_Distance;
            public int Turn_Moving_Distance;
            public int Gripper_Moving_Distance;
        }

        public class ShelfUpDateEventArgs : EventArgs
        {
            //public eManualCommand CmdMessage { get; set; }
            public string Tag { get; set; }
            //public CraneCommand cmd { get; set; }
        }
        #endregion

        #region Constructor
        public RMModuleBase(string Name, bool simul, eRMType RMtype, int RMNumber, bool ioSimul)
            : base(Name, simul)
        {
            GData = GlobalData.Current;

            RMType = RMtype;
            ModuleName = Name;
            SimulMode = simul;
            mRMNumber = RMNumber;
            IOSimulMode = ioSimul;

            //CurrentCmd = new CraneCommand();
        }
        #endregion

        #region Member Variable
        public int PLCNumber
        {
            get;
            protected set;
        }

        public string RMStateFilePath; // 2020.11.26 WCF 접속 보고시 Hand 센서 확인 
        public string RMIp;            //제어기 IP
        public int RMPort;             //제어기 통신 PORT
        private string sendMessage = string.Empty;
        private object TokenLock = new object();
        public ParameterList RMParameter = new ParameterList(); // option Parameter변수 
        protected CraneCommand CurrentCmd;

        public ConcurrentDictionary<string, PLCDataItem> PLCtoPC = new ConcurrentDictionary<string, PLCDataItem>();
        public ConcurrentDictionary<string, PLCDataItem> PCtoPLC = new ConcurrentDictionary<string, PLCDataItem>();
        protected bool RMResetRequest = false;

        #endregion

        #region Property 멤버

        protected bool ThreadExitRequested
        {
            get;
            private set;
        }

        public int PLCReadOffset
        {
            get;
            protected set;
        }
        public int PLCWriteOffset
        {
            get;
            protected set;
        }

        public bool IsFirstRM { get { return mRMNumber == 1; } }
        public eRMType RMType { get; protected set; }
        public bool IOSimulMode { get; protected set; }

        private int mRMNumber;

        public int RMNumber
        {
            get
            {
                return mRMNumber;
            }
        }

        private bool _SuddenlyFire;
        public virtual bool SuddenlyFire
        {
            get
            {
                return _SuddenlyFire;
            }
            set
            {
                _SuddenlyFire = value;
            }
        }
        //241018 HoN 화재시나리오 운영 추가       //6. 크레인 운영 추가        //6.1) 화재 쉘프 크레인 취출 후 60초 대기
        private DateTime _CraneCarrierInstallTime;
        public DateTime CraneCarrierInstallTime
        {
            get => _CraneCarrierInstallTime;
            set
            {
                _CraneCarrierInstallTime = value;
                SW_CraneCarrierInstall = Stopwatch.StartNew(); //241120 RGJ CraneCarrierInstallTime 찍을때마다 스탑워치 가동
            }
        }
        public Stopwatch SW_CraneCarrierInstall;


        private string _RMPLC_ID;
        public string RMPLC_ID
        {
            get
            {
                return _RMPLC_ID;
            }
        }
        //public eSVCraneServiceState SVCraneServiceState
        //{
        //    get
        //    {
        //        if (GetRMState() <= eRMPmacState.Initializing ||
        //            GetRMState() == eRMPmacState.Error)
        //            return eSVCraneServiceState.Out_OF_SERVICE;
        //        else
        //            return eSVCraneServiceState.IN_SERVICE;
        //    }
        //}
        public ShelfItemList FrontData
        {
            get
            {
                return ShelfManager.Instance.FrontData;
            }
        }
        public ShelfItemList RearData
        {
            get
            {
                return ShelfManager.Instance.RearData;
            }
        }
        /// <summary>
        /// 기상반 동작 상태 
        /// </summary>
        protected eCraneSCState _CraneSC_State = eCraneSCState.OFFLINE;
        public virtual eCraneSCState CraneSC_State
        {
            get
            {
                return _CraneSC_State;
            }
        }

        //241001 HDK Crane 작업가능상태 표시 개선
        protected eCraneSCMode _CraneSCStatus = eCraneSCMode.AUTO_RUN;
        public virtual eCraneSCMode CraneSCStatus
        {
            get
            {
                return _CraneSCStatus;
            }
        }

        protected eCraneUIState oldCraneState;
        protected eCraneUIState SimulCraneState;
        public eCraneUIState CraneState
        {
            get
            {
                if (SimulMode)
                {
                    return SimulCraneState;
                }
                else
                {
                    eCraneUIState UIState = eCraneUIState.UNKNOWN;

                    //241001 HDK Crane 작업가능상태 표시 개선 PLC_SCMODE => CraneSCStatus
                    if (CraneSCStatus == eCraneSCMode.OFFLINE)
                    {
                        UIState = eCraneUIState.OFFLINE;
                    }
                    else if (CraneSC_State == eCraneSCState.ERROR)
                    {
                        UIState = eCraneUIState.ERROR;
                    }
                    else
                    {
                        eCraneCommand CCS = PLC_CraneCommandState;
                        bool CraneJobEnd = PLC_CraneJobState == eCraneJobState.JobComplete_Fork1;
                        if(CraneJobEnd)
                        {
                            UIState = eCraneUIState.ONLINE;
                        }
                        else
                        {
                            switch (CCS)
                            {
                                case eCraneCommand.UNLOAD:
                                    UIState = eCraneUIState.PUTTING;
                                    break;
                                case eCraneCommand.PICKUP:
                                    UIState = eCraneUIState.GETTING;
                                    break;
                                case eCraneCommand.MOVE:
                                    UIState = eCraneUIState.MOVING;
                                    break;
                                case eCraneCommand.LOCAL_HOME:
                                    UIState = eCraneUIState.HOMING;
                                    break;
                                case eCraneCommand.NONE:
                                    UIState = eCraneUIState.ONLINE;
                                    break;
                            }
                        }

                    }

                    if (oldCraneState != UIState)
                    {
                        oldCraneState = UIState;
                        RaisePropertyChanged("CraneState");
                    }

                    return UIState;
                }
            }
            set
            {
                if (SimulMode)
                {
                    SimulCraneState = value;
                }
                RaisePropertyChanged("CraneState");
            }
        }
        //public eCraneUIState CraneState
        //{
        //    get
        //    {
        //        if (SimulMode)
        //        {
        //            return SimulCraneState;
        //        }
        //        else
        //        {
        //            if (PLC_SCMODE == eCraneSCMode.OFFLINE)
        //            {
        //                return eCraneUIState.OFFLINE;
        //            }
        //            else if (PLC_CraneActionState == eCraneActionState.ERROR)
        //            {
        //                return eCraneUIState.ERROR;
        //            }
        //            else
        //            {
        //                eCraneUIState UIState = eCraneUIState.UNKNOWN;

        //                eCraneCommand CCS = PLC_CraneCommandState;
        //                switch (CCS)
        //                {
        //                    case eCraneCommand.NONE:
        //                        UIState = eCraneUIState.ONLINE;
        //                        break;
        //                    case eCraneCommand.LOCAL_PUT:
        //                    case eCraneCommand.REMOTE_PUT:
        //                    case eCraneCommand.PUT:
        //                        UIState = eCraneUIState.PUTTING;
        //                        break;
        //                    case eCraneCommand.LOCAL_GET:
        //                    case eCraneCommand.REMOTE_GET:
        //                    case eCraneCommand.GET:
        //                        UIState = eCraneUIState.GETTING;
        //                        break;
        //                    case eCraneCommand.LOCAL_DIRECTGET:
        //                    case eCraneCommand.REMOTE_DIRECTGET:
        //                        UIState = eCraneUIState.DIRECT_GETTING;
        //                        break;
        //                    case eCraneCommand.LOCAL_MOVE:
        //                    case eCraneCommand.REMOTE_MOVE:
        //                    case eCraneCommand.MOVE:
        //                        UIState = eCraneUIState.MOVING;
        //                        break;
        //                    case eCraneCommand.FIRE_UNLOADING:
        //                        UIState = eCraneUIState.FIRE_UNLOADING;
        //                        break;
        //                    case eCraneCommand.LOCAL_HOME:
        //                    case eCraneCommand.REMOTE_HOME:
        //                        UIState = eCraneUIState.HOMING;
        //                        break;
        //                }

        //                return UIState;
        //            }
        //        }
        //    }
        //    set
        //    {
        //        if (SimulMode)
        //        {
        //            SimulCraneState = value;
        //        }
        //        RaisePropertyChanged("CraneState");
        //    }
        //}
        public decimal AutoRunSpeed //오토 런 시작할때 자동 속도
        {
            get;
            protected set;
        }
        public decimal SafetyUnlockSpeed //문 열때 안전 속도 제한
        {
            get;
            protected set;
        }

        //220509 HHJ SCS 개선     //- CraneControl 기존 소스에 적용
        /// <summary>
        /// CraneArmState
        /// Extend, Fold 상태를 확인
        /// </summary>
        protected eCraneArmState _CraneArmState;
        public eCraneArmState CraneArmState
        {
            get { return _CraneArmState; }
            set
            {
                if (_CraneArmState == value) return;

                _CraneArmState = value;
                RaisePropertyChanged("CraneArmState");
            }
        }

        public virtual bool RobotOnlineConncet { get; set; }

        public bool SimulExistSensor = false;
        public virtual bool CarrierExistSensor
        {
            get
            {
                if (SimulMode)
                {
                    return SimulExistSensor;
                }
                else
                {
                    bool bExist = PLC_CarrierExistFork1;
                    return bExist;
                }
            }
            set
            {
                if (SimulMode)
                {
                    SimulExistSensor = value;
                }
            }
        }

        public bool SimulForkFireDetect = false;
        public virtual bool ForkFireDetect
        {
            get
            {
                if (SimulMode)
                {
                    return SimulForkFireDetect;
                }
                else
                {
                    bool bDetect = PLC_FireState;
                    return bDetect;
                }
            }
            set
            {
                if (SimulMode)
                {
                    SimulForkFireDetect = value;
                }
            }
        }

        //230306
        public ePalletSize SimulPalletSize = ePalletSize.Cell_Long;
        public ePalletSize GetPalletSize()
        {
            if (SimulMode)
            {
                return SimulPalletSize;
            }
            else
            {
                return PLC_PalletSize;
            }
        }

        public virtual bool RMMotorMCPower { get; set; } //210218 lsj 추가

        public virtual bool RMPanelDoor { get; set; }    //210218 lsj 추가


        public virtual decimal ForkAxisPosition { get; set; }

        public virtual decimal XAxisPosition { get; set; }

        public virtual decimal ZAxisPosition { get; set; }

        public virtual decimal TurnAxisPosition { get; set; }

        public virtual decimal GripAxisPosition { get; set; }
        public virtual decimal MoveSpeed { get; set; }

        public virtual decimal JogSpeed { get; set; }

        //220914 HHJ SCS 개선     //- RM Fork Position UI 연동
        private decimal _UIForkAxisPosition;
        public decimal UIForkAxisPosition
        {
            get
            {
                return _UIForkAxisPosition;
            }
            set
            {
                _UIForkAxisPosition = value;
                RaisePropertyChanged("UIForkAxisPosition");
            }
        }

        //220823 조숭진 rmmodulebase로 이동 s
        public virtual int CommandTimeOut { get; set; } //커맨드 응답 타임 아웃
        public virtual int FireNotifyTimeOut { get; set; }
        public virtual int PLCIF_Delay { get; set; }
        public virtual int CraneActionTimeOut { get; set; }
        public virtual int PLCTimeOut { get; set; }
        //220823 조숭진 rmmodulebase로 이동 e


        #region WCF STATE 조회용 // 2021.01.21 
        public virtual string Robot_TRANSFERRING { get; set; }

        public virtual string Robot_BANK { get; set; }

        public virtual string Robot_BAY { get; set; }

        public virtual string Robot_LEVEL { get; set; }

        public int CurrentBank
        {
            get
            {
                int bank = 0;
                int.TryParse(Robot_BANK, out bank);
                if (bank == 0)
                {
                    bank = 1;
                }
                return bank;
            }
        }
        public int CurrentBay
        {
            get
            {
                int bay = 0;
                int.TryParse(Robot_BAY, out bay);
                return bay;
            }
        }
        public int CurrentLevel
        {
            get
            {
                int level = 0;
                int.TryParse(Robot_LEVEL, out level);
                return level;
            }
        }

        //220411 HHJ SCS 개선     //- 쉘프 그리드 세로 숫자 추가
        public string CurrentTag
        {
            get
            {
                //220420 HHJ SCS 개선     //- ShelfTagHelper 추가
                //return string.Format("{0}{1:D03}{2:D03}", CurrentBank, CurrentLevel, CurrentBay);
                return ShelfTagHelper.GetTag(CurrentBank, CurrentBay, CurrentLevel);
            }
        }


        public virtual string Robot_CARRIERCONTAIN { get; set; }
        public virtual string Robot_HOME { get; set; }
        public virtual string Robot_EMERGENCYS { get; set; }
        public virtual string Robot_BUSY { get; set; }

        public virtual string Robot_ARMSTRETCH { get; set; }
        public virtual string Robot_ERRORSTAUTS { get; set; }
        public virtual string Robot_ERRORCODE { get; set; }
        public virtual string Robot_AUTOTEACHING { get; set; }
        public virtual string Robot_CHUCK { get; set; }

        // 2021.02.18 RM 소모품 진단 관련 추가
        public virtual string Robot_Fork_Accure { get; set; }
        public virtual string Robot_Drive_Accure { get; set; }
        public virtual string Robot_Lift_Accure { get; set; }
        public virtual string Robot_Turn_Accure { get; set; }
        public virtual string Robot_Chuck_Accure { get; set; }

        #endregion


        //명령시 Button 동작 확인
        private bool _InitReq;
        public bool InitReq
        {
            get
            {
                return _InitReq;
            }
            set
            {
                _InitReq = value;
            }
        }

        private bool _MoveReq;
        public bool MoveReq
        {
            get
            {
                return _MoveReq;
            }
            set
            {
                _MoveReq = value;
            }
        }

        private bool _GetReq;
        public bool GetReq
        {
            get
            {
                return _GetReq;
            }
            set
            {
                _GetReq = value;
            }
        }

        // 190724 Put 명령시 Button 동작 확인
        private bool _PutReq;
        public bool PutReq
        {
            get
            {
                return _PutReq;
            }
            set
            {
                _PutReq = value;
            }
        }

        /// 201211 Manual Fork 명령시 Button 동작 확인
        private bool _ArmStretchReq;
        public bool ArmStretchReq
        {
            get
            {
                return _ArmStretchReq;
            }
            set
            {
                _ArmStretchReq = value;
            }
        }

        /// 201214 Manual ArmFolding 명령시 Button 동작 확인
        private bool _ArmFoldingReq;
        public bool ArmFoldingReq
        {
            get
            {
                return _ArmFoldingReq;
            }
            set
            {
                _ArmFoldingReq = value;
            }
        }

        //private string _CarrierID
        public string CarrierID
        {
            get
            {
                return DefaultSlot.MaterialName;
            }
        }

        //221228 HHJ SCS 개선
        public eCarrierSize CarrierSize
        {
            get
            {
                return DefaultSlot.MaterialType;
            }
        }

        // 2021.020.19 TrayHeight 인터락 추가
        //private eTrayHeight _CurrentTrayHeight;
        //public eTrayHeight CurrentTrayHeight
        //{
        //    get
        //    {
        //        //return _CurrentTrayHeight;

        //        string tmp = INI_Helper.ReadValue(RMStateFilePath, "STATE", "CurrentTrayHeight").ToString();
        //        if (tmp == eTrayHeight.Height0.ToString())
        //            return eTrayHeight.Height0;
        //        else if (tmp == eTrayHeight.Height1.ToString())
        //            return eTrayHeight.Height1;
        //        else if (tmp == eTrayHeight.Height2.ToString())
        //            return eTrayHeight.Height2;
        //        else if (tmp == eTrayHeight.Height3.ToString())
        //            return eTrayHeight.Height3;
        //        else if (tmp == eTrayHeight.OverHeight.ToString())
        //            return eTrayHeight.OverHeight;

        //        return eTrayHeight.OverHeight;
        //    }
        //    set
        //    {
        //        _CurrentTrayHeight = value;
        //        INI_Helper.WriteValue(RMStateFilePath, "STATE", "CurrentTrayHeight", _CurrentTrayHeight.ToString());
        //    }
        //}


        #endregion

        #region PMAC만 사용
        public virtual void WriteRMSensorIO(IOPoint io, bool OnOff)
        {
            throw new NotImplementedException("WriteRMSensorIO() 메서드가 구현되지 않았습니다.");
        }

        public virtual void PostionCheck()
        {
            throw new NotImplementedException("PostionCheck() 메서드가 구현되지 않았습니다.");
        }

        public virtual Decimal Fork_Axis() //210105 iki
        {
            throw new NotImplementedException("Fork_Axis() 메서드가 구현되지 않았습니다.");
        }

        public virtual Decimal Drive_Axis() //210105 iki
        {
            throw new NotImplementedException("Drive_Axis() 메서드가 구현되지 않았습니다.");
        }

        public virtual Decimal Lift_Axis() //210105 iki
        {
            throw new NotImplementedException("Lift_Axis() 메서드가 구현되지 않았습니다.");
        }

        public virtual Decimal Turn_Axis() //210105 iki
        {
            throw new NotImplementedException("Turn_Axis() 메서드가 구현되지 않았습니다.");
        }
        public virtual string strPvarGet(string Address)
        {
            throw new NotImplementedException("strPvarGet() 메서드가 구현되지 않았습니다.");
        }
        public virtual string[] SetTextSend(string sendText)
        {
            throw new NotImplementedException("SetTextSend() 메서드가 구현되지 않았습니다.");
        }
        public virtual void RMSafetyReset(bool b)
        {
            throw new NotImplementedException("RMSafetyReset() 메서드가 구현되지 않았습니다.");
        }
        public virtual bool ReadRMSensorIO(IOPoint io)
        {
            throw new NotImplementedException("ReadRMSensorIO() 메서드가 구현되지 않았습니다.");
        }
        public virtual void CloseController()
        {
            throw new NotImplementedException("CloseController() 메서드가 구현되지 않았습니다.");
        }
        public virtual string GetUnGrip()
        {
            throw new NotImplementedException("GetUnGrip() 메서드가 구현되지 않았습니다.");
        }
        public virtual string GetGrip()
        {
            throw new NotImplementedException("GetGrip() 메서드가 구현되지 않았습니다.");
        }
        #endregion

        #region RM 동작관련

        public virtual eRMPmacState GetRMState()
        {
            return eRMPmacState.Unknown;
        }

        public virtual bool CheckMoveComplete(CraneCommand cmd)
        {
            throw new NotImplementedException("CheckMoveComplete() 메서드가 구현되지 않았습니다.");
        }
        public virtual eCraneSCMode GetRMMode()
        {
            throw new NotImplementedException("GetRMState() 메서드가 구현되지 않았습니다.");
        }
        public virtual decimal GetMoterPos(int Axis)
        {
            throw new NotImplementedException("GetRMState() 메서드가 구현되지 않았습니다.");
        }

        public virtual bool ConnectRM(string m_hostName, int m_port)
        {
            throw new NotImplementedException("RMConnecting() 메서드가 구현되지 않았습니다.");
        }

        public virtual bool RMinitCmd()
        {
            throw new NotImplementedException("RMinitCmd() 메서드가 구현되지 않았습니다.");
        }

        public virtual bool getAllServoOff()
        {
            throw new NotImplementedException("getAllServoOff() 메서드가 구현되지 않았습니다.");
        }

        public virtual bool MoveStopCmd()
        {
            throw new NotImplementedException("MoveStopCmd() 메서드가 구현되지 않았습니다.");
        }
        public virtual void RMModChange(eCraneSCMode mode)
        {
            throw new NotImplementedException("RMModChange() 메서드가 구현되지 않았습니다.");
        }




        public virtual bool RMHandGrip()
        {
            throw new NotImplementedException("RMHandGrip() 메서드가 구현되지 않았습니다.");
        }

        public virtual bool RMHandUnGrip()
        {
            throw new NotImplementedException("RMHandUnGrip() 메서드가 구현되지 않았습니다.");
        }
        public virtual bool RMFireExtinguishWork()
        {
            throw new NotImplementedException("RMFireExtinguishWork() 메서드가 구현되지 않았습니다.");
        }
        public virtual bool CheckRMExtinguished()
        {
            throw new NotImplementedException("CheckRMExtinguished() 메서드가 구현되지 않았습니다.");
        }
        public virtual bool CheckRMAutoMode()
        {
            throw new NotImplementedException("CheckRMAutoMode() 메서드가 구현되지 않았습니다.");
        }

        #endregion

        #region Scheduler 


        public bool CheckCurrentComandExist()
        {
            return CurrentCmd != null;
        }

        //220329 HHJ SCS 개발     //- 커맨드 디폴트 생성추가
        public CraneCommand GetCurrentCmd()
        {
            return CurrentCmd;
        }
        public virtual void Run()
        {
            throw new NotImplementedException("Run() 메서드가 구현되지 않았습니다.");
        }

        //2021년 6월 4일 금요일 오전 8:55:22 - Editted by 수환 : 추가
        public virtual void RunForTimer(object sender, EventArgs e)
        {
            throw new NotImplementedException("Run() 메서드가 구현되지 않았습니다.");
        }

        // 2020.11.25 Alarm 테스트 등록및 테스트 작업 
        public virtual void AlarmThread()
        {
            throw new NotImplementedException("AlarmThread() 메서드가 구현되지 않았습니다.");
        }

        public virtual bool CraneCommandAction(CraneCommand cmd)
        {
            throw new NotImplementedException("ActionCommand() 메서드가 구현되지 않았습니다.");
        }

        protected virtual bool RMMCommandAction(CraneCommand cmd, eCraneCommand Type, bool bfrom = false)
        {
            throw new NotImplementedException("RMMoveCommand() 메서드가 구현되지 않았습니다.");
        }

        //public virtual bool CheckTrayInterLock(string Tag, eTrayHeight TrayHeight)
        //{
        //    if (SimulMode)
        //    {
        //        return true;
        //    }
        //    ShelfItem item = null;
        //    if (string.IsNullOrEmpty(Tag))
        //    {
        //        LogManager.WriteConsoleLog(eLogLevel.Info, "Tag :{0} 입력값이 비었거나 Null 입니다..", Tag);
        //        return false;
        //    }
        //    if ((Convert.ToInt32(Tag.Substring(0, 1)) == (int)eShelfBank.Front))
        //    {
        //        item = GlobalData.Current.MainBooth.FrontData.Where(r => r.TagName == Tag).FirstOrDefault();
        //        if (item == null)
        //        {
        //            LogManager.WriteConsoleLog(eLogLevel.Info, "Tag :{0} 와 매칭되는 쉘프가 없습니다.", Tag);
        //            return false;
        //        }

        //    }
        //    else if (Convert.ToInt32(Tag.Substring(0, 1)) == (int)eShelfBank.Rear)
        //    {
        //        item = GlobalData.Current.MainBooth.RearData.Where(r => r.TagName == Tag).FirstOrDefault();
        //        if (item == null)
        //        {
        //            LogManager.WriteConsoleLog(eLogLevel.Info, "Tag :{0} 와 매칭되는 쉘프가 없습니다.", Tag);
        //            return false;
        //        }
        //    }
        //    else
        //    {
        //        LogManager.WriteConsoleLog(eLogLevel.Info, "Tag :{0} 와 매칭되는 쉘프가 없습니다.", Tag);
        //        return false;
        //    }

        //    if (GlobalData.Current.SCSType == eSCSType.TrayLBS)
        //    {
        //        if (item.TrayHeight >= (int)TrayHeight) //높이 큰 쉘프에 작은 트레이 들어가는것 허용
        //            return true;
        //    }
        //    else if (GlobalData.Current.SCSType == eSCSType.BoxLBS)
        //    {
        //        //트레이 박스쉘프              릴박스
        //        if (item.TrayHeight == 2 && TrayHeight == eTrayHeight.Height1)
        //        {
        //            return false; //릴박스가 트레이 박스 쉘프로 들어가는건 금지
        //        }
        //        else //트레이 박스가 릴박스 쉘프에 들어가는건 허용
        //        {
        //            return true;
        //        }

        //    }
        //    else
        //    {
        //        return false;
        //    }
        //    return false;

        //}

        protected virtual bool CheckInitComplete()
        {
            throw new NotImplementedException("CheckInitComplete() 메서드가 구현되지 않았습니다.");
        }
        public virtual bool CheckRMCommandExist() //231006 RGJ RM Command 와 실제 PLC Busy 분리함.
        {
            throw new NotImplementedException("CheckRMCommandExist() 메서드가 구현되지 않았습니다.");
        }
        public virtual bool CheckRMPLCBusy() //231006 RGJ RM Command 와 실제 PLC Busy 분리함.
        {
            throw new NotImplementedException("CheckRMPLCBusy() 메서드가 구현되지 않았습니다.");
        }

        protected virtual void TactLogWrite(CraneCommand cmd)
        {
            throw new NotImplementedException("TactLogWrite() 메서드가 구현되지 않았습니다.");
        }

        protected virtual bool CheckRMActionComplete(CraneCommand cmd)
        {
            throw new NotImplementedException("CheckRMActionComplete() 메서드가 구현되지 않았습니다.");
        }

        public virtual short RMAlarmCheck()
        {
            throw new NotImplementedException("RMAlarmCheck() 메서드가 구현되지 않았습니다.");
        }
        public virtual bool CheckRM_MC_On()
        {
            throw new NotImplementedException("CheckRM_MC_On() 메서드가 구현되지 않았습니다.");
        }
        /// <summary>
        /// 랙마 전장 박스 Open 상태를 체크
        /// </summary>
        /// <returns></returns>
        public virtual bool CheckRMBoxOpen() //On 정상
        {
            throw new NotImplementedException("CheckRMBoxOpen() 메서드가 구현되지 않았습니다.");
        }
        #endregion

        #region 기타함수
        public void SetCranePLCID(string PID)
        {
            _RMPLC_ID = PID;
        }

        //220628 HHJ SCS 개선     //- PLCDataItems 개선
        //public void InitPLCInterface(int ReadOffset, int WriteOffset)
        public void InitPLCInterface(int plcnum, int ReadOffset, int WriteOffset)
        {
            PLCNumber = plcnum;
            PLCReadOffset = ReadOffset;
            PLCWriteOffset = WriteOffset;

            //220628 HHJ SCS 개선     //- PLCDataItems 개선
            //PLCtoPC = ProtocolHelper.ModulePLCItemSetter(eAreaType.PLCtoPC, PLCReadOffset);
            //PCtoPLC = ProtocolHelper.ModulePLCItemSetter(eAreaType.PCtoPLC, PLCWriteOffset);
            PLCtoPC = ProtocolHelper.GetPLCItem(eAreaType.PLCtoPC, "CRANE", (short)plcnum, (ushort)PLCReadOffset);
            PCtoPLC = ProtocolHelper.GetPLCItem(eAreaType.PCtoPLC, "CRANE", (short)plcnum, (ushort)PLCWriteOffset);
        }
        public void SetAutoSafetySpeed(int SafetySpeed, int AutoSpeed)
        {
            this.SafetyUnlockSpeed = (decimal)SafetySpeed;
            this.AutoRunSpeed = (decimal)AutoSpeed;
        }
        public bool ProcessOPManualCommand(eCraneOPManualCommand OperatorCommand)
        {
            switch (OperatorCommand)
            {
                case eCraneOPManualCommand.EMG_STOP:

                    break;
                case eCraneOPManualCommand.ACTIVE:
                    break;
                case eCraneOPManualCommand.STOP:
                    break;
                case eCraneOPManualCommand.ERROR_RESET:
                    break;
                case eCraneOPManualCommand.RETURN_HOME:
                    break;
                case eCraneOPManualCommand.MANUAL_COMMAND:
                    break;
                case eCraneOPManualCommand.SMOKE:
                    break;
                case eCraneOPManualCommand.FIRE1:
                    break;
                case eCraneOPManualCommand.FIRE2:
                    break;
                case eCraneOPManualCommand.FIRE_SIGNAL:
                    break;
            }
            return true;
        }
        public void ActionPostProcess(CraneCommand cmd)
        {
            var data = new ProcessEventArgs();

            try
            {
                data.IsSuccessful = true;
                data.CompletionTime = DateTime.Now;
                data.cmd = cmd;

                // 2021.02.22 RGJ
                //소모품 관리 변수 업데이트
                data.Fork_Moving_Distance = GetFork_CycleDistance();
                data.Drive_Moving_Distance = GetDrive_CycleDistance();
                data.Z_Moving_Distance = GetZ_CycleDistance();
                data.Turn_Moving_Distance = GetTurn_CycleDistance();
                data.Gripper_Moving_Distance = GetGripper_CycleDistance();

                //LogManager.WriteConsoleLog(eLogLevel.Info, "랙마 동작 이동거리 FORK:{0} DRIVE: {1} Z_MOVE: {2} TURN: {3}  GRIPPER : {4} ", data.Fork_Moving_Distance, data.Drive_Moving_Distance, data.Z_Moving_Distance, data.Turn_Moving_Distance, data.Gripper_Moving_Distance);

                //ShelfStateSet(cmd);

                DateTime.Now.ToString("ddd, dd MMM yyy HH’:’mm’:’ss ‘GMT’");
                OnProcessCompleted(data);


                sendMessage = string.Format("Command : {0} TargetBank : {1} TargetBay : {2} TargetLevel : {3} TargetCarrierID : {4} TargetTagID : {5}",
                    cmd.Command, cmd.TargetBank, cmd.TargetBay, cmd.TargetLevel, cmd.TargetCarrierID, cmd.TargetTagID);
                GlobalData.Current.SendMessageEvent = sendMessage.ToUpper();

            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }


        //private void ShelfStateSet(CraneCommand cmd)
        //{
        //    // Yes code here  
        //    lock (TokenLock)
        //    {

        //        string tmpshelfID = String.Format("{0}{1}{2}", cmd.TargetBank, cmd.TargetBay.ToString("D3"), cmd.TargetLevel.ToString("D3"));
        //        if (cmd.TargetBank == (int)eShelfBank.Front)
        //        {
        //            var item = GlobalData.Current.MainBooth.FrontData.Where(r => r.TagName == tmpshelfID).FirstOrDefault();

        //            if (item != null)
        //            {
        //                if (cmd.Command == enumCraneCommand.CRANE_GET)
        //                    item.EXIST_STATE = (int)eCarrierExist.Empty;
        //                else
        //                    item.EXIST_STATE = (int)eCarrierExist.Exist;
        //            }


        //            //ShelfItemList.Serialize(GlobalData.Current.CurrentFilePaths(GlobalData.Current.FullPath) + GlobalData.Current.MainBooth.FrontTeachDataPath,
        //            //    GlobalData.Current.MainBooth.FrontData);
        //        }
        //        else if (cmd.TargetBank == (int)eShelfBank.Rear)
        //        {
        //            var item = GlobalData.Current.MainBooth.RearData.Where(r => r.TagName == tmpshelfID).FirstOrDefault();

        //            if (item != null)
        //            {
        //                if (cmd.Command == enumCraneCommand.CRANE_GET)
        //                    item.EXIST_STATE = (int)eCarrierExist.Empty;
        //                else
        //                    item.EXIST_STATE = (int)eCarrierExist.Exist;
        //            }

        //            //ShelfItemList.Serialize(GlobalData.Current.CurrentFilePaths(GlobalData.Current.FullPath) + GlobalData.Current.MainBooth.RearTeachDataPath,
        //            //    GlobalData.Current.MainBooth.RearData);
        //        }
        //    }
        //}

        public void UpdateShelf(string tagID, eCraneCommand cmd , string JobCarrierID)
        {
            lock (TokenLock)
            {
                //220509 HHJ SCS 개선     //- ShelfControl 변경
                //불합리 개선 Front와 Rear나눠져 있어서 두번의 수정이 필요함.
                var item = GlobalData.Current.ShelfMgr.GetShelf(tagID);
                if (item == null)
                {
                    return;
                }

                item.ShelfBusyRm = eShelfBusyRm.Unknown;

                if (cmd == eCraneCommand.PICKUP)
                {
                    //Get 했을경우 데이터 Update

                    //item.TransferCarrierData(this); ///작업에서  캐리어 직접 가져온다.주석처리.

                    item.ResetCarrierData(); //쉘프 데이터 초기화.
                    UpdateCarrier(JobCarrierID); //크레인으로 데이터 Update

                    //2024.06.13 lim, 로그 남길떄 반대로 데이터 들어감
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Update Carrier Position   Shelf :{0} ==> Crane :{1}  CarrierID : {2}" ,item.TagName, ModuleName, JobCarrierID);

                    item.InstallTime = item.InstallTime; //220705 조숭진 install time 추가
                    //item.NotifyScheduled(false); //쉘프에 걸려있던 예약을 해제 //230405 RGJ 쉘프 예약 제어는 스케쥴러에서 함.
                    item.NeedFireAlarmReport = false; //Get 할때 Fire 보고 필요 해제.
                    //Capacity Change Report
                    GlobalData.Current.HSMS.SendS6F11(310, "ZONENAME", item.iZoneName);
                }
                else if (cmd == eCraneCommand.UNLOAD)
                {
                    ResetCarrierData();//크레인 캐리어 데이터 초기화

                    if(CarrierStorage.Instance.GetCarrierItem(JobCarrierID) is CarrierItem cItem) //Debug 용 임시 로그 추가
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "UpdateShelf Carrier :{0} Storage Exist Check OK!", JobCarrierID);
                    }
                    else
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "UpdateShelf Carrier :{0} Storage Exist Check Failed!!!!", JobCarrierID);
                    }

                    item.UpdateCarrier(JobCarrierID); //쉘프로 데이터 Update                        
                    item.InstallTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");  //220705 조숭진 install time 추가
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Update Carrier Position   Crane :{0} ==> Shelf :{1}  CarrierID : {2}", ModuleName, item.TagName, JobCarrierID);


                    //TransferCarrierData(item);//작업에서  캐리어 직접 가져온다.주석처리

                    //item.NotifyScheduled(false); //쉘프에 걸려있던 예약을 해제 //230405 RGJ 쉘프 예약 제어는 스케쥴러에서 함.
                    //220803 조숭진 스케쥴러에서 보고하고 여기선 주석
                    //Capacity Change Report
                    //GlobalData.Current.HSMS.SendS6F11(310, "ZONENAME", item.iZoneName);

                    if(item.CheckCarrierExist() == false) //231025 RGJ Shelf Update 리트라이 추가 
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Shelf: {0} Carrier : {1} Update Failed! Retry! ", item.TagName, JobCarrierID);
                        item.UpdateCarrier(JobCarrierID); //쉘프로 데이터 Update                        
                        item.InstallTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");  //220705 조숭진 install time 추가
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Reupdate Carrier Position   Crane :{0} ==> Shelf :{1}  CarrierID : {2}", ModuleName, item.TagName, JobCarrierID);
                    }
                }
                if (cmd != eCraneCommand.MOVE)
                {
                    GlobalData.Current.ShelfMgr.SaveShelfData(item);
                }
            }

            //ShelfUpDataProcess(tagID);
        }

        ///// <summary>
        ///// TrayHeight 가져 온다.
        ///// </summary>
        ///// <param name="tagID"></param>
        ///// <param name="cmd"></param>
        //public eTrayHeight GetShelfTrayHeight(string tagID, eRMPmacCommand cmd)
        //{
        //    // Yes code here  
        //    lock (TokenLock)
        //    {
        //        if (cmd == eRMPmacCommand.GET)
        //        {
        //            //220524 HHJ SCS 개선     //- Shelf Xml제거
        //            //ShelfItem item = new ShelfItem();
        //            ShelfItem item;
        //            int bank = Int32.Parse(tagID.Substring(0, 1));
        //            if (bank == (int)eShelfBank.Front)
        //            {
        //                item = GlobalData.Current.MainBooth.FrontData.Where(r => r.TagName == tagID).FirstOrDefault();
        //                return (eTrayHeight)item.TrayHeight;
        //            }
        //            else if (bank == (int)eShelfBank.Rear)
        //            {
        //                item = GlobalData.Current.MainBooth.RearData.Where(r => r.TagName == tagID).FirstOrDefault();
        //                return (eTrayHeight)item.TrayHeight;
        //            }
        //        }
        //    }

        //    return eTrayHeight.Height0;
        //}


        protected virtual void ShelfUpdate(ShelfUpDateEventArgs e)
        {

            OnShelfUpdate?.Invoke(this, e);
        }

        public bool SimulPlace_Sensor()
        {
            string val = INI_Helper.ReadValue(RMStateFilePath, "STATE", "Place_Sensor_Exist");

            return val == "1" ? true : false;

        }

        protected virtual void OnProcessCompleted(ProcessEventArgs e)
        {
            ProcessCompleted?.Invoke(this, e);
        }

        public static void DoEvents()
        {
            //Application.Current.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new ThreadStart(delegate { }));
        }
        public string GetCurrentPositionTag()
        {
            string p = string.Format("{0:D2}{1:D3}{2:D2}", CurrentBank, CurrentBay, CurrentLevel);
            return p;
        }
        /// <summary>
        /// Crane 동작 속도를 설정한다.
        /// </summary>
        /// <param name="Val"></param>
        /// <returns>성공 :true 실패 : false</returns>
        public virtual bool SetAutoSpeed(decimal Val)
        {
            return false;
        }


        /// <summary>
        /// 화재상황임을 랙마스터에  통지 한다.
        /// </summary>
        /// <param name="FireOn"></param>
        /// <returns></returns>
        public virtual bool NotifyFireCommand(bool FireOn)
        {
            throw new NotImplementedException("NotifyFireState() 메서드가 구현되지 않았습니다.");
        }

        /// <summary>
        /// 랙마스터에 화재 플래그 상태를 가져온다.
        /// </summary>
        /// <returns></returns>
        public virtual bool GetRMFireStateFlag()
        {
            throw new NotImplementedException("GetCraneFireStateFlag() 메서드가 구현되지 않았습니다.");
        }

        /// <summary>
        /// InService 2
        /// OutOfService 1
        /// </summary>
        /// <returns></returns>
        public override int GetUnitServiceState()
        {
            if (!CheckModuleHeavyAlarmExist() && CheckInitComplete() && GetRMMode() == eCraneSCMode.AUTO_RUN) //인서비스 조건에 오토런 상태 추가.
            {
                return 2;
            }
            else
            {
                return 1;
            }
        }
        public virtual bool CheckRMConnetion()
        {
            return false;
        }

        public virtual bool GetChuckState()
        {
            return false;
        }
        public virtual eCraneArmState GetArmExtendState()
        {
            return eCraneArmState.Center;
        }

        public virtual bool GetPlaceSensorState()
        {
            return false;
        }

        //SuHwan_20221021 : [ServerClient]
        /// <summary>
        /// PLC 데이타 를 RMModuleBase 적용
        /// </summary>
        public virtual void setRMModuleBase_formPLC()
        {
            return;
        }
        #endregion

        #region WCF 관련
        //public virtual Parameter_ROBOT GetRobotStatusPara()
        //{
        //    throw new NotImplementedException("GetRobotStatusPara() 메서드가 구현되지 않았습니다.");
        //}
        public virtual bool SetCraneCommand(CraneCommand Cmd)
        {
            throw new NotImplementedException("SetCraneCommand() 메서드가 구현되지 않았습니다.");
        }
        public virtual bool RemoveCraneCommand()
        {
            throw new NotImplementedException("RemoveCraneCommand() 메서드가 구현되지 않았습니다.");
        }
        #endregion

        #region 소모품 관련

        public virtual int GetFork_CycleDistance()
        {
            return 0;
            //throw new NotImplementedException("GetFork_CycleDistance() 는 구현되지 않았습니다.");
        }
        public virtual int GetZ_CycleDistance()
        {
            return 0;
            //throw new NotImplementedException("GetZ_CycleDistance() 는 구현되지 않았습니다.");
        }
        public virtual int GetDrive_CycleDistance()
        {
            return 0;
            //throw new NotImplementedException("GetDrive_CycleDistance() 는 구현되지 않았습니다.");
        }
        public virtual int GetTurn_CycleDistance()
        {
            return 0;
            //throw new NotImplementedException("GetTurn_CycleDistance() 는 구현되지 않았습니다.");
        }
        public virtual int GetGripper_CycleDistance()
        {
            return 0;
            //throw new NotImplementedException("GetGripper_CycleDistance() 는 구현되지 않았습니다.");
        }
        #endregion

        #region ICarrierStoreAble Interface
        public bool UpdateCarrier(string CarrierID, bool DBUpdate = true, bool HostReq = false)
        {
            //220509 HHJ SCS 개선     //- ShelfControl 변경
            //Carrier가 있던 없던 무조건 업데이트 진행한다.
            //Carrier가 없으면 빈값으로의 변경이 되어야함.
            DefaultSlot.SetCarrierData(CarrierID, DBUpdate);
            return true;
        }

        public bool ResetCarrierData()
        {
            try
            {
                DefaultSlot.DeleteSlotCarrier();
                return true;
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }
        }

        public bool RemoveSCSCarrierData()
        {
            //RGJ 크레인에서 갑자기 사라질수 없음... 일단 캐리어 리셋만 해둠.
            DefaultSlot.DeleteSlotCarrier();
            return true;
        }

        public string GetCarrierID()
        {
            CarrierItem Temp = InSlotCarrier;
            if (Temp != null)
            {
                return Temp.CarrierID;
            }
            else
            {
                //220525 HHJ SCS 개선     //- ShelfItem 개선
                //return _CarrierID;
                return string.Empty;
            }
        }

        public bool CheckCarrierExist()
        {
            if (SimulMode)
            {
                return DefaultSlot.MaterialExist;
            }
            else
            {
                return CarrierExistSensor;
            }
        }
        public bool TransferCarrierData(ICarrierStoreAble To)
        {
            if (DefaultSlot.MaterialExist)
            {
                string CID = GetCarrierID();
                //220711 조숭진 false -> true로 변경
                //DefaultSlot.RemoveCarrierData(false);

                DefaultSlot.DeleteSlotCarrier();
                To.UpdateCarrier(CID);
            }
            return true;
        }

        public int CalcDistance(ICarrierStoreAble a, ICarrierStoreAble b)
        {
            int BankDistance = 0;
            int BayDistance = a.iBay - b.iBay;
            int LevelDistance = a.iLevel - b.iLevel;

            BayDistance = (BayDistance < 0 ? -BayDistance : BayDistance);
            LevelDistance = (LevelDistance < 0 ? -LevelDistance : LevelDistance);
            return BankDistance + BayDistance + LevelDistance;
        }
        public string GetTagName() { return ModuleName; }
        public void NotifyScheduled(bool Reserved, bool init = false) { return; }       //221012 조숭진 init 인자 추가...

        public override bool CheckGetAble(string CarrierID) { return CarrierExistSensor; } //RM 이 소스 일경우
        public override bool CheckPutAble() { return !CarrierExistSensor; }//RM 이 목적지 일경우

        public bool CheckCarrierSizeAcceptable(eCarrierSize Size) //RM 은 모든 사이즈 취급 가능
        {
            return true;
        }

        public int iBank { get { return CurrentBank; } }

        public int iBay { get { return CurrentBay; } }

        public int iLevel { get { return CurrentLevel; } }

        public short iWorkPlaceNumber
        {
            get
            {
                return 0;
            }
        }

        //230306
        public ePalletSize PalletSize
        {
            get
            {
                return GetPalletSize();
            }
        }

        public string iGroup { get { return "CRANE"; } }
        public string iLocName { get { return ModuleName; } }
        public string iZoneName { get { return string.Empty; } }

        //230102 HHJ SCS 개선     //RM에 Install을 위한 InsertCarrier 추가함     //사용하지 않을경우 삭제 필요함.
        public bool InsertCarrier(CarrierItem targetCarrier)
        {
            if (targetCarrier != null)
            {
                if (CarrierExistSensor && CarrierStorage.Instance.CarrierContain(targetCarrier.CarrierID) == false) //혹시 스토리지에 없는지 체크       //화물감지 들어왔을때만...
                {
                    CarrierStorage.Instance.InsertCarrier(targetCarrier); //스토리지에 없는 경우 추가.
                }
                DefaultSlot.SetCarrierData(targetCarrier.CarrierID);
                return true;
            }
            return false;
        }
        #endregion

        #region PLCInterface

        #region RECV DATA Read Area
        public string PLC_CarrierID_FORK1
        {
            get { return GData.protocolManager.ReadString(ModuleName, PLCtoPC, "PLC_CarrierID_FORK1"); }
        }
        public string PLC_CarrierID_FORK2

        {
            get { return GData.protocolManager.ReadString(ModuleName, PLCtoPC, "PLC_CarrierID_FORK2"); }
        }

        public ePalletSize PLC_PalletSize
        {
            get 
            {
                return (ePalletSize)GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_PalletSize");
            }
        }


        public eCraneJobState PLC_CraneJobState
        {
            get { return (eCraneJobState)GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_CraneJobState"); }
        }

        //private eCraneCommand _oldPLC_CraneCommandState;
        public eCraneCommand PLC_CraneCommandState
        {
            get { return (eCraneCommand)GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_CraneCommandState"); }
            //get 
            //{


            //    var returnValue = (eCraneCommand)GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_CraneCommandState");

            //    if ( _oldPLC_CraneCommandState != returnValue )
            //    {
            //        _oldPLC_CraneCommandState = returnValue;

            //        var jobBuffer = GlobalData.Current.McdList.GetCurrentJob(ModuleName);

            //        if (jobBuffer != null)
            //        {
            //            if(this.CurrentCmd == null)
            //            {
            //                this.CurrentCmd = new CraneCommand();
            //            }

            //            if (this.CurrentCmd.CommandID != jobBuffer.CommandID)
            //            {
            //                this.CurrentCmd = null;
            //                this.CurrentCmd = new CraneCommand(jobBuffer.CommandID, jobBuffer.AssignRMName, eCraneCommand.MOVE, enumCraneTarget.SHELF, jobBuffer.DestItem, jobBuffer.CarrierID);
            //                //this.CurrentCmd.TargetBank = jobBuffer.AlterShelfBank = new CraneCommand(jobBuffer.CommandID, jobBuffer.AssignRMName, eCraneCommand.MOVE, enumCraneTarget.SHELF, jobBuffer.DestItem, jobBuffer.CarrierID);

            //            }
            //        }


            //    }

            //    return returnValue;
            //}
        }

        public short PLC_CommandNumber_FORK1
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_CommandNumber_FORK1"); }
        }

        public short PLC_SourceBank_FORK1
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_SourceBank_FORK1"); }
        }
        public short PLC_SourceBay_FORK1
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_SourceBay_FORK1"); }
        }
        public short PLC_SourceLevel_FORK1
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_SourceLevel_FORK1"); }
        }
        public short PLC_SourceWorkPlace_FORK1
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_SourceWorkPlace_FORK1"); }
        }
        public short PLC_DestBank_FORK1
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_DestBank_FORK1"); }
        }
        public short PLC_DestBay_FORK1
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_DestBay_FORK1"); }
        }
        public short PLC_DestLevel_FORK1
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_DestLevel_FORK1"); }
        }
        public short PLC_DestWorkPlace_FORK1
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_DestWorkPlace_FORK1"); }
        }

        public short PLC_CommandUseFork
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_CommandUseFork"); }
        }
        public short PLC_CommandNumber_FORK2
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_CommandNumber_FORK2"); }
        }
        public short PLC_SourceBank_FORK2
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_SourceBank_FORK2"); }
        }
        public short PLC_SourceBay_FORK2
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_SourceBay_FORK2"); }
        }
        public short PLC_SourceLevel_FORK2
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_SourceLevel_FORK2"); }
        }
        public short PLC_SourceWorkPlace_FORK2
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_SourceWorkPlace_FORK2"); }
        }
        public short PLC_DestBank_FORK2
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_DestBank_FORK2"); }
        }
        public short PLC_DestBay_FORK2
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_DestBay_FORK2"); }
        }
        public short PLC_DestLevel_FORK2
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_DestLevel_FORK2"); }
        }
        public short PLC_DestWorkPlace_FORK2
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_DestWorkPlace_FORK2"); }
        }

        public short PLC_CommandAck
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_CommandAck"); }
        }
        public eCraneOnlineMode PLC_OnlineMode
        {
            get { return (eCraneOnlineMode)GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_OnlineMode"); }
        }

        public eCraneSCMode PLC_SCMODE
        {
            get { return (eCraneSCMode)GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_SCMODE"); }
        }

        public eCraneSCState PLC_CraneActionState
        {
            get { return (eCraneSCState)GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_CraneActionState"); }
        }
        public bool PLC_CarrierExistFork1
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_CarrierExistFork1"); }
        }
        public bool PLC_CarrierExistFork2
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_CarrierExistFork2"); }
        }
        public eCraneActiveState PLC_ActiveState
        {
            get { return (eCraneActiveState)GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_ActiveState"); }
        }
        public bool PLC_FireCommand
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_FireCommand"); }
        }
        public bool PLC_FireState
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_FireState"); }
        }
        public bool PLC_WaterPool_First_Put
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_WaterPool_First_Put"); }
        }
        public short PLC_WarningCode
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_WarningCode"); }
        }
        public short PLC_ErrorCode
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_ErrorCode"); }
        }

        public eCraneArmState PLC_Fork1_Extend
        {
            get { return (eCraneArmState)GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_Fork1_Extend"); }
        }
        public eCraneArmState PLC_Fork2_Extend
        {
            get { return (eCraneArmState)GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_Fork2_Extend"); }
        }


        public bool PLC_Fork1_ErrorRecommandReady
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_Fork1_ErrorRecommandReady"); }
        }
        public bool PLC_Fork1_ErrorDoubleStorage
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_Fork1_ErrorDoubleStorage"); }
        }
        public bool PLC_Fork1_EmptyRetrieve
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_Fork1_EmptyRetrieve"); }
        }

        public bool PLC_Fork2_ErrorRecommandReady
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_Fork2_ErrorRecommandReady"); }
        }
        public bool PLC_Fork2_ErrorDoubleStorage
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_Fork2_ErrorDoubleStorage"); }
        }
        public bool PLC_Fork2_EmptyRetrieve
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_Fork2_EmptyRetrieve"); }
        }

        #region CV Forking Bit
        public bool PLC_CV1Forking
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_CV1Forking"); }
        }
        public bool PLC_CV2Forking
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_CV2Forking"); }
        }
        public bool PLC_CV3Forking
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_CV3Forking"); }
        }
        public bool PLC_CV4Forking
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_CV4Forking"); }
        }
        public bool PLC_CV5Forking
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_CV5Forking"); }
        }
        public bool PLC_CV6Forking
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_CV6Forking"); }
        }
        public bool PLC_CV7Forking
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_CV7Forking"); }
        }
        public bool PLC_CV8Forking
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_CV8Forking"); }
        }
        public bool PLC_CV9Forking
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_CV9Forking"); }
        }
        public bool PLC_CV10Forking
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_CV10Forking"); }
        }
        public bool PLC_CV11Forking
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_CV11Forking"); }
        }
        public bool PLC_CV12Forking
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_CV12Forking"); }
        }
        public bool PLC_CV13Forking
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_CV13Forking"); }
        }
        public bool PLC_CV14Forking
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_CV14Forking"); }
        }
        public bool PLC_CV15Forking
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_CV15Forking"); }
        }
        public bool PLC_CV16Forking
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_CV16Forking"); }
        }
        #endregion

        #region 동작 축 현재값
        public int PLC_RM_XPosition //주행축
        {
            get
            {
                return GData.protocolManager.ReadInt32(ModuleName, PLCtoPC, "PLC_RM_XPosition");
            }
        }
        //public short PLC_RM_Low_XPosition //주행축 하위 2Byte
        //{
        //    get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_RM_Low_XPosition"); }
        //}
        //public short PLC_RM_High_XPosition //주행축 상위 2Byte
        //{
        //    get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_RM_High_XPosition"); }
        //}

        public int PLC_RM_ZPosition //승강축
        {
            get
            {
                int ZPos = GData.protocolManager.ReadInt32(ModuleName, PLCtoPC, "PLC_RM_ZPosition");
                if (ZPos < -10000) //기준값 이하로 들어오면 0으로 간주
                {
                    return 0;
                }
                return ZPos;
            }
        }
        //public short PLC_RM_Low_ZPosition //주행축 하위 2Byte
        //{
        //    get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_RM_Low_ZPosition"); }
        //}
        //public short PLC_RM_High_ZPosition //주행축 상위 2Byte
        //{
        //    get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_RM_High_ZPosition"); }
        //}
        public int PLC_RM_FPosition //포크축
        {
            get
            {
                return GData.protocolManager.ReadInt32(ModuleName, PLCtoPC, "PLC_RM_FPosition");
            }
        }
        //public short PLC_RM_Low_FPosition //포크축 하위 2Byte
        //{
        //    get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_RM_Low_FPosition"); }
        //}
        //public short PLC_RM_High_FPosition //포크 축 상위 2Byte 
        //{
        //    get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_RM_High_FPosition"); }
        //}
        #endregion

        public bool PLC_FireJobCancelAble
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_FireJobCancelAble"); }
        }
        public bool PLC_FireJobCancelBlock
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_FireJobCancelBlock"); }
        }

        public short PLC_RM_Current_Bank
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_RM_Current_Bank"); }
        }
        public short PLC_RM_Current_Bay
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_RM_Current_Bay"); }
        }
        public short PLC_RM_Current_Level
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_RM_Current_Level"); }
        }
        public short PLC_RM_Current_WorkPlace
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_RM_Current_WorkPlace"); }
        }
        public string PLC_RM_X_VibrationData
        {
            get
            {
                short temp = GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_RM_X_VibrationData");

                PLCtoPC.TryGetValue("PLC_RM_X_VibrationData", out PLCDataItem item);
                decimal dpow = (decimal)Math.Pow(10, item.DecimalPoint);

                return (new decimal((int)temp) / dpow).ToString();
                //return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_RM_X_VibrationData");
            }
        }

        public string PLC_RM_Z_VibrationData
        {
            get
            {
                short temp = GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_RM_Z_VibrationData");

                PLCtoPC.TryGetValue("PLC_RM_Z_VibrationData", out PLCDataItem item);
                decimal dpow = (decimal)Math.Pow(10, item.DecimalPoint);

                return (new decimal((int)temp) / dpow).ToString();
                //return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_RM_Z_VibrationData"); 
            }
        }

        public bool PLC_RM_Front_DoubleStorage
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_RM_Front_DoubleStorage"); }
        }

        public bool PLC_RM_Rear_DoubleStorage
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_RM_Rear_DoubleStorage"); }
        }

        #endregion

        #region SEND DATA Write Data Area
        //사용 예시) 확장은 필요없을것으로 생각됨. 특정 모듈에 기재해야하는 비트 / 워드 존재 시, 확장된 모듈에 추가를 하면됨.
        public string PC_CarrierID_FORK1
        {
            get { return GData.protocolManager.ReadString(ModuleName, PCtoPLC, "PC_CarrierID_FORK1"); }
            set
            {

                if (value.Length > 40) //40자리 제한
                {
                    GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_CarrierID_FORK1", value.Substring(0, 40));
                }
                else
                {
                    GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_CarrierID_FORK1", value);
                }
            }
        }
        public string PC_CarrierID_FORK2
        {
            get { return GData.protocolManager.ReadString(ModuleName, PCtoPLC, "PC_CarrierID_FORK2"); }
            set
            {
                if (value.Length > 40) //40자리 제한
                {
                    GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_CarrierID_FORK2", value.Substring(0, 40));
                }
                else
                {
                    GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_CarrierID_FORK2", value);
                }
            }
        }

        #region Crane Command Parameter


        public ePalletSize PC_PalletSize
        {
            get 
            {
                return (ePalletSize)GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_PalletSize");
            }
            set 
            {

                GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_PalletSize", (ushort)value);
            }

        }

        public eCraneCommand PC_CraneCommand
        {
            get { return (eCraneCommand)GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_CraneCommand"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_CraneCommand", (ushort)value); }

        }
        public short PC_CommandNumber_FORK1
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_CommandNumber_FORK1"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_CommandNumber_FORK1", value); }
        }


        public short PC_SourceBank_FORK1
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_SourceBank_FORK1"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_SourceBank_FORK1", value); }

        }
        public short PC_SourceBay_FORK1
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_SourceBay_FORK1"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_SourceBay_FORK1", value); }
        }
        public short PC_SourceLevel_FORK1
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_SourceLevel_FORK1"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_SourceLevel_FORK1", value); }
        }
        public short PC_SourceWorkPlace_FORK1
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_SourceWorkPlace_FORK1"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_SourceWorkPlace_FORK1", value); }
        }
        public short PC_DestBank_FORK1
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_DestBank_FORK1"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_DestBank_FORK1", value); }
        }
        public short PC_DestBay_FORK1
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_DestBay_FORK1"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_DestBay_FORK1", value); }
        }
        public short PC_DestLevel_FORK1
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_DestLevel_FORK1"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_DestLevel_FORK1", value); }
        }
        public short PC_DestWorkPlace_FORK1
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_DestWorkPlace_FORK1"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_DestWorkPlace_FORK1", value); }
        }

        public short PC_CommandUseFork
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_CommandUseFork"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_CommandUseFork", value); }
        }
        public short PC_CommandNumber_FORK2
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_CommandNumber_FORK2"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_CommandNumber_FORK2", value); }
        }
        public short PC_SourceBank_FORK2
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_SourceBank_FORK2"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_SourceBank_FORK2", value); }
        }
        public short PC_SourceBay_FORK2
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_SourceBay_FORK2"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_SourceBay_FORK2", value); }
        }
        public short PC_SourceLevel_FORK2
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_SourceLevel_FORK2"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_SourceLevel_FORK2", value); }
        }
        public short PC_SourceWorkPlace_FORK2
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_SourceWorkPlace_FORK2"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_SourceWorkPlace_FORK2", value); }
        }
        public short PC_DestBank_FORK2
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_DestBank_FORK2"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_DestBank_FORK2", value); }
        }
        public short PC_DestBay_FORK2
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_DestBay_FORK2"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_DestBay_FORK2", value); }
        }
        public short PC_DestLevel_FORK2
        {
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_DestLevel_FORK2", value); }
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_DestLevel_FORK2"); }
        }
        public short PC_DestWorkPlace_FORK2
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_DestWorkPlace_FORK2"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_DestWorkPlace_FORK2", value); }
        }

        public short PC_CommandWriteComplete
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_CommandWriteComplete"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_CommandWriteComplete", value); }

        }
        public short PC_CarrierStability //0 저강성(저속동작) 1 고강성(고속가능)
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_CarrierStability"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_CarrierStability", value); }
        }

        #endregion

        #region REMOTE CONTROL
        public bool PC_EMG_STOP
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PCtoPLC, "PC_EMG_STOP"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_EMG_STOP", value); }
        }
        public bool PC_ActiveCommand
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PCtoPLC, "PC_ActiveCommand"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_ActiveCommand", value); }
        }
        public bool PC_PauseCommand
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PCtoPLC, "PC_PauseCommand"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_PauseCommand", value); }
        }
        public bool PC_ErrorReset
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PCtoPLC, "PC_ErrorReset"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_ErrorReset", value); }
        }

        public bool PC_RemoveFork1Data
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PCtoPLC, "PC_RemoveFork1Data"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_RemoveFork1Data", value); }
        }
        public bool PC_RemoveFork2Data
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PCtoPLC, "PC_RemoveFork2Data"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_RemoveFork2Data", value); }
        }
        public bool PC_RemoveAllForkData
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PCtoPLC, "PC_RemoveAllForkData"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_RemoveAllForkData", value); }
        }

        public bool PC_EmptyRetrievalReset
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PCtoPLC, "PC_EmptyRetrievalReset"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_EmptyRetrievalReset", value); }
        }
        public bool PC_FireOccurred
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PCtoPLC, "PC_FireOccurred"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_FireOccurred", value); }
        }
        public bool PC_FireReset
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PCtoPLC, "PC_FireReset"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_FireReset", value); }
        }


        #endregion

        public short PC_AlarmCode
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_AlarmCode"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_AlarmCode", value); }
        }
        public short PC_OneRackComp    //2024.09.20 lim, 원렉 모드 완료 신호 추가
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_OneRackComp"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_OneRackComp", value); }
        }

        #endregion

        #endregion

        #region Member Method

        public void RMReset_Request()
        {
            RMResetRequest = true;
        }
        public virtual bool RMAlarmResetAction()
        {
            throw new NotImplementedException("RMAlarmReset() 메서드가 구현되지 않았습니다.");
        }

        public virtual bool RMEMG_STOP_Request()
        {
            throw new NotImplementedException("bool RMEMG_STOP_Request() 는 구현되지 않았습니다.");
        }

        public virtual bool RMPause_Request()
        {
            throw new NotImplementedException("bool RMPause_Request() 는 구현되지 않았습니다.");
        }
        public virtual bool RMResume_Request()
        {
            throw new NotImplementedException("bool RMResume_Request() 는 구현되지 않았습니다.");
        }
        public virtual bool RMHome_Request()
        {
            throw new NotImplementedException("bool RMResume_Request() 는 구현되지 않았습니다.");
        }

        /// <summary>
        /// 실제 크레인 내부에서 화재 상태인지 체크
        /// </summary>
        /// <returns></returns>
        public virtual bool CheckForkInFire()
        {
            throw new NotImplementedException("bool CheckForkInFire() 는 구현되지 않았습니다.");

        }

        public virtual bool CheckPLCFireCommand()
        {
            throw new NotImplementedException("bool CheckPLCFireCommand() 는 구현되지 않았습니다.");
        }

        //241030 HoN 화재 관련 추가 수정        //-. PLC로 알려주는 Bit 화재 발생하면 무조건 전 Crane ON 처리. -> OFF시점은 Operator가 수동으로 해야함. 이를 수행하지 않아 발생하는 문제는 오퍼레이터 조작미스로 처리
        public virtual bool CheckCraneFireOccurred()
        {
            throw new NotImplementedException("bool CheckCraneFireOccurred() 는 구현되지 않았습니다.");
        }

        public virtual bool CheckEmptyRetriveState()
        {
            throw new NotImplementedException("bool CheckEmptyRetriveState() 는 구현되지 않았습니다.");
        }
        public virtual bool CheckDoubleStorageState()
        {
            throw new NotImplementedException("bool CheckDoubleStorageState() 는 구현되지 않았습니다.");
        }
        public virtual bool CheckPortInterfaceErrorState()
        {
            throw new NotImplementedException("bool CheckPortInterfaceErrorState() 는 구현되지 않았습니다.");
        }
        public virtual bool CheckForkIsCenter()
        {
            throw new NotImplementedException("bool CheckForkIsCenter() 는 구현되지 않았습니다.");
        }

        /// <summary>
        /// 공출고 발생시 리셋
        /// </summary>
        /// <returns></returns>
        public virtual bool ResetEmptyRetrieve()
        {
            throw new NotImplementedException("bool ResetEmptyRetrieve() 는 구현되지 않았습니다.");
        }
        public bool CheckWaterPoolPutComplete()
        {
            if (SimulMode)
            {
                return false;
            }
            else
            {
                bool bWaterPut = this.PLC_WaterPool_First_Put;
                return bWaterPut;
            }
        }

        public virtual eCraneUtilState GetCraneUtilState()
        {
            return eCraneUtilState.NONE;
        }

        public void ExitRunThread()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "RM Module : {0} RM ClientRun Thread Exis Request!", this.ModuleName);
            ThreadExitRequested = true;
        }
        #endregion
    }
}
