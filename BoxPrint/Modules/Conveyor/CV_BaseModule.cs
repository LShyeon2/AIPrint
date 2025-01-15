//220608 HHJ SCS 개선     //- MCProtocol, MXComponent 추가
using PLCProtocol.Base;
using PLCProtocol.DataClass;
using BoxPrint.CCLink;
using BoxPrint.DataList;
using BoxPrint.DataList.MCS;
using BoxPrint.Log;
using BoxPrint.Modules.CVLine;
using BoxPrint.Modules.RFID;
using BoxPrint.Modules.RM;
using BoxPrint.SSCNet;
using System;
//220315 HHJ SCS 개발     //- MXComponentNet, SharedMemory 개선
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Text;
using System.Threading;
//231025 HHJ Exist Update 분리
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Activation;
using System.Diagnostics;
using System.Threading.Tasks;

namespace BoxPrint.Modules.Conveyor
{
    /// <summary>
    /// 모든 컨베이어 모듈의 부모 클래스
    /// </summary>
    //220902 HHJ SCS 개선     //- Direction 변경에 따른 UI 반응 추가
    //public abstract class CV_BaseModule : ModuleBase, INotifyPropertyChanged, ICarrierStoreAble
    public abstract class CV_BaseModule : ModuleBase, ICarrierStoreAble
    {
        protected const int EMG_ERROR_CODE = 1450;
        protected bool PortInOutTypeChanged = false; //포트의 InOut상태가 변경 되었을시 초기화 동작으로 가도록 플래그추가.
        protected bool RecoveryRequest = false;
        protected bool RecoveryActionFailed = false;
        public string Pre_PC_CarrierID = null; //231226 rhj 임시로 저장할 Pre_PC_CarrierID 추가
        private object PortTypeChangeSyncObject = new object();
        private bool NowPortTypeChanging = false;
        public DateTime LastPortTimeChagnedTime
        {
            get;
            protected set;
        }

        protected const int ErrorStep = 999;
        //public readonly int TrayTransTimeout = 30;     //트레이 전송 타임아웃 30초로 일단 하드코딩
        //public readonly int TurnTimeout = 10;          //턴동작 타임아웃 10초로 일단 하드코딩
        public readonly int StopperUpdownTimeout = 10; //스톱퍼 타임아웃 10초로 일단 하드코딩
        public readonly int StackerActionTimeout = 10; //스택커 타임아웃 10초로 일단 하드코딩
        public readonly int InverterTimeout = 20;    //인버터 동작 타임아웃 20초로 일단 하드코딩
        public readonly int IODelay = 100;  //100ms  //I/O 체크 딜레이
        //public readonly int TraySendRemainDelay = 500; // SendTray 할때 자신의 Stop신호 오프되도 해당시간 동안 인버터 동작유지


        protected int KeyInTimeOut = -1; //Client KeyIn 시간 대기 음수면 무한정 대기한다.

        #region Timeout & Set
        //220921 조숭진 db에서 읽어오는 것으로 변경 s
        //220410 RGJ 정적 변수로 선언 전체 모듈이 동일한 값을 사용함
        public static int CarrierIDCheckTimeOut { get; private set; } //CarrierID 받을때 타임아웃 설정.
        public static int PLCTimeout { get; private set; }
        public static int ValidationWaitDuration { get; private set; }
        public static int LocalStepCycleDelay { get; private set; }

        //가동률 취합을 위한 포트 컨베이어 길이
        protected uint _PortCVLength = 1000; //기본값 1M = 1000 단위는 mm
        public uint PortCVLength
        {
            get
            {
                return _PortCVLength;
            }
        }

        public void SetPortCVLength(uint CVLength)
        {
            if (100 <= CVLength && CVLength <= 100000) //0.1M 에서 100M 사이 통과
            {
                _PortCVLength = CVLength;
            }
            else
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "SetPortCVLength Failed  Module:{0} LValue:{1}", ModuleName, CVLength);
            }

        }


        /// <summary>
        /// 포트 타입체인지 여러쓰레드 접근시 체인지 명령을 한번만 수행하도록 토큰을 건다.
        /// </summary>
        /// <returns></returns>
        protected bool GetPortTypeChangeToken()
        {
            lock (PortTypeChangeSyncObject)
            {
                if (NowPortTypeChanging == false)
                {
                    NowPortTypeChanging = true;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        protected void ReleasePortTypeChangeToken()
        {
            lock (PortTypeChangeSyncObject)
            {
                NowPortTypeChanging = false;
            }
        }

        public static void CarrierIDCheckTimeOutSet(int value)
        {
            CarrierIDCheckTimeOut = value;
        }
        public static void PLCTimeoutSet(int value)
        {
            PLCTimeout = value;
        }
        public static void LocalStepCycleDelaySet(int value)
        {
            LocalStepCycleDelay = value;
        }
        public static void ValidationWaitTimeSet(int value)
        {
            ValidationWaitDuration = value;
        }
        //220921 조숭진 db에서 읽어오는 것으로 변경 e
        #endregion

        protected Stopwatch SW_CarrierWaitInTime;
        private DateTime _LastCarrierWaitInTime;
        protected DateTime LastCarrierWaitInTime
        {
            set
            {
                if (SW_CarrierWaitInTime == null)
                {
                    SW_CarrierWaitInTime = Stopwatch.StartNew();
                }
                else
                {
                    SW_CarrierWaitInTime.Restart();
                }
                _LastCarrierWaitInTime = value;
            }
            get
            {
                return _LastCarrierWaitInTime;
            }
        }

        //-Tray ReceiveStopDelay 하드 코딩 제거 XML에서 변경으로 수정
        protected int ReceiveStopDelay = 500; //Stop 위치까지 이동후 추가 굴림 시간.  디폴트 : 500ms
        protected int CV_SimulPosition = 0;
        protected short SimulAlarmCode = 0;
        //220315 HHJ SCS 개발     //- MXComponentNet, SharedMemory 개선
        public ConcurrentDictionary<string, PLCDataItem> PLCtoPC = new ConcurrentDictionary<string, PLCDataItem>();
        public ConcurrentDictionary<string, PLCDataItem> PCtoPLC = new ConcurrentDictionary<string, PLCDataItem>();

        public string LineName//현대 포트가 배치된 라인 이름. 포트 Inout 타입 변경할때 해당 이름으로 검색해서 동일한 Name 가진 포트는 전체 포트 타입 변경.
        {
            get
            {
                if (ParentModule != null)
                {
                    return ParentModule.ModuleName;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public string TrackID
        {
            get
            {
                return string.Format("{0:D2}{1:D3}", TrackGroup, TrackNum);
            }
        }
        public int TrackGroup { get; protected set; }
        public int TrackNum { get; protected set; }

        public int PortTableID { get; protected set; }

        public ePortSize PortSize { get; protected set; }

        public bool UseStopper { get; protected set; }

        public bool UseBackStopper { get; protected set; }

        public bool UseDoor { get; protected set; }

        public string DoorNumber { get; protected set; } //lsj SESS Door
        public bool UseBCR
        {
            get
            {
                return CVRFIDModule != null;
                //if (GlobalData.Current.UseBCR)
                //{
                //    return CVRFIDModule != null && CVRFID_Extend_Module != null;
                //}
                //else
                //{
                //    return CVRFIDModule != null;
                //}
            }
            //protected set { CVRFIDModule = null; }     //LKJ임의로 수정

        }

        public bool UseManualBCR { get; protected set; }

        protected eBCRState _PortBCRState = eBCRState.OFFLine;
        public eBCRState PortBCRState
        {
            get
            {
                if (!UseBCR) //BCR 없음
                {
                    return eBCRState.NoBCR;
                }
                else if (UseManualBCR) //수동 BCR 사용
                {
                    return eBCRState.ManualBCR;
                }
                else
                {
                    return _PortBCRState;
                }
            }
            set
            {
                _PortBCRState = value;
                RaisePropertyChanged("PortBCRState");
            }
        }

        public bool UseBypassMode { get; protected set; }

        public bool UseServoHomeMode => TurnType != eTurnType.AirCylinder; //에어실린더 타입이 아니면 홈동작이 있다.

        public int TurnAngle { get; protected set; }

        //HA
        public bool UseColorSensor { get; protected set; }

        private eCraneSCMode ConnectedCraneSCMode = eCraneSCMode.OFFLINE; //20240116 RGJ 크레인 메뉴얼 상태여서 OutofService 올렸는데 자체적으로 또 올리는걸 방지하기 위한 변수

        protected int _LightCurtainNumber = -1; //전장 도면에 정의된 라이튼 커튼 번호 
        public int LightCurtainNumber
        {
            get
            {
                return _LightCurtainNumber;
            }
            protected set
            {
                _LightCurtainNumber = value;
            }
        }
        /// <summary>
        /// Servo 동작시 몇번축을 사용할것인지 결정
        /// </summary>
        public int ServoAxis
        {
            get;
            protected set;
        }


        public bool UseLightCurtain
        {
            get
            {
                return _LightCurtainNumber > 0;
            }
        }


        protected DateTime StepTimeOutDT = DateTime.Now;
        protected int _LocalActionStep = 0;
        public int LocalActionStep
        {
            get { return _LocalActionStep; }
            protected set
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "CV: {0} Local Step Before : {1} =>  Next : {2}", ModuleName, _LocalActionStep, value);

                if (value >= ErrorStep)
                {
                    _LocalActionStep = ErrorStep;
                }
                else
                {
                    _LocalActionStep = value;
                    StepTimeOutDT = DateTime.Now; //스텝이 변할때마다 스텝 타임아웃시각 초기화
                }
                WriteCurrentState();
            }
        }

        protected eCVCommand _NextCVCommand = eCVCommand.None;
        public eCVCommand NextCVCommand
        {
            get
            {
                return _NextCVCommand;
            }
            protected set
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "CVModule : {0} CommandChanged [{1}] ==> [{2}]", this.ModuleName, _NextCVCommand, value);
                if (value == eCVCommand.ErrorHandling)
                {
                    BeforeErrorCommand = _NextCVCommand;
                }
                _NextCVCommand = value;

            }
        }
        protected eCVCommand BeforeErrorCommand = eCVCommand.None; //에러 발생시 기존 명령 저장

        protected ePortInOutType _PortInOutType;
        public ePortInOutType PortInOutType //해당 포트 방향 정의
        {
            get
            {
                return _PortInOutType;
            }
            protected set
            {
                if (value != _PortInOutType)
                {
                    _PortInOutType = value;
                    GlobalData.Current.HSMS.SendS6F11(406, "PORT", this); //S6F1 PortTypeChanged CEID = 406
                    //220902 HHJ SCS 개선  //- Direction 변경에 따른 UI 반응 추가
                    //OnPropertyChanged("PortInOutType");
                    RaisePropertyChanged("PortInOutType");
                }
            }
        }
        //230214 HHJ SCS 개선
        protected eCVWay _CVWay;
        public eCVWay CVWay
        {
            get => _CVWay;
            protected set
            {
                if (value != _CVWay)
                {
                    _CVWay = value;
                    RaisePropertyChanged("CVWay");
                }
            }
        }
        protected ePortAceessMode _PortAccessMode;
        public ePortAceessMode PortAccessMode //해당 포트 접근 모드 
        {
            get
            {
                return _PortAccessMode;
            }
            protected set
            {
                if (value != _PortAccessMode)
                {
                    _PortAccessMode = value;
                    GlobalData.Current.HSMS.SendS6F11(407, "PORT", this); //S6F1 PortModeChanged CEID = 407
                    RaisePropertyChanged("PortAccessMode");
                }
            }
        }


        private eCVAutoManualState _AutoManualState = eCVAutoManualState.None;
        public eCVAutoManualState AutoManualState
        {
            get
            {
                return _AutoManualState;
            }

            protected set
            {
                if (_AutoManualState == value)
                {
                    return;
                }
                else
                {
                    //2024.05.27 lim, port 상태 변경 했는데 inservice 보고 안함 로그 추가
                    LogManager.WriteConsoleLog(eLogLevel.Info, "{0} CurrentAutoState {1} ChageAutoState {2} Change, CraneMode {3}",
                        ControlName, _AutoManualState.ToString(), value.ToString(), ConnectedCraneSCMode.ToString());

                    //상태가 변했다
                    _AutoManualState = value;

                    if (_AutoManualState == eCVAutoManualState.AutoRun)
                    {

                        if (ConnectedCraneSCMode != eCraneSCMode.MANUAL_RUN) //크레인이 메뉴얼 상태로 바뀌면서 OutOfService 올렸는데 임의로 올리면 안되기에 막아둠
                        {
                            //GlobalData.Current.HSMS.SendS6F11(401, "PORT", this); //InService 
                            RequestInserviceReport();
                        }
                        //SetCVTrackResume(); //Enable - Disable 일때만 변경. 삭제
                    }
                    else
                    {

                        if (ConnectedCraneSCMode != eCraneSCMode.MANUAL_RUN) //크레인이 메뉴얼 상태로 바뀌면서 OutOfService 올렸는데 임의로 올리면 안되기에 막아둠
                        {
                            //GlobalData.Current.HSMS.SendS6F11(402, "PORT", this); //OutofService
                            RequestOutserviceReport();
                        }
                        //SetCVTrackPause(); //Enable - Disable 일때만 변경. 삭제
                    }

                    //230217 HHJ SCS 개선     //CV UI State 관련 추가     //우선 마땅히 따를 Rule이 없기에 eCVAutoManualState를 따라 UI도 변경한다.
                    ConveyorUIState = _AutoManualState.Equals(eCVAutoManualState.AutoRun) ? eConveyorUIState.Online : eConveyorUIState.Manual;
                }
            }
        }

        public ShelfClass CVRobotTeaching;
        //230217 HHJ SCS 개선     //기존 CVModuleType Binding 처리할 수 있도록 변경
        #region 이전
        //public eCVType CVModuleType //타입 확인없이 현재 모듈타입을 알수 있도록 한다.
        //{
        //    get;
        //    protected set;
        //}
        #endregion
        private eCVType _CVModuleType;
        public eCVType CVModuleType //타입 확인없이 현재 모듈타입을 알수 있도록 한다.
        {
            get => _CVModuleType;
            protected set
            {

                _CVModuleType = value;
                RaisePropertyChanged("CVModuleType");
            }
        }

        //230217 HHJ SCS 개선     //CV UI State 관련 추가
        private eConveyorUIState _ConveyorUIState = eConveyorUIState.None;
        public eConveyorUIState ConveyorUIState
        {
            get => _ConveyorUIState;
            set
            {
                //동일하면 진행되지않아야함.
                if (_ConveyorUIState.Equals(value))
                    return;
                //현 상태가 eAlarm인 경우는 Alarm Clear만 처리가 되어야함.
                if (_ConveyorUIState.Equals(eConveyorUIState.Alarm)
                    && !value.Equals(eConveyorUIState.AlarmClear))
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "{0} CurrentUIState {1} ChangeUIState {2} is not {3}. Change Fail.",
                        ControlName, _ConveyorUIState.ToString(), value.ToString(), eConveyorUIState.AlarmClear.ToString());
                    return;
                }

                //변경해야할 상태가 AlarmClear가 아닌경우는 해당 상태를 업데이트 해준다.
                if (!value.Equals(eConveyorUIState.AlarmClear))
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "{0} CurrentUIState {1} ChangeUIState {2} Change",
                        ControlName, _ConveyorUIState.ToString(), value.ToString());
                    _ConveyorUIState = value;
                }
                //변경해야할 상태가 AlarmClear이며 여기까지 처리가 된 경우는 _AutoManualState를 확인하여 상태를 반영한다.
                else
                {
                    eConveyorUIState recoveryState = _AutoManualState.Equals(eCVAutoManualState.AutoRun) ? eConveyorUIState.Online : eConveyorUIState.Manual;

                    LogManager.WriteConsoleLog(eLogLevel.Info, "{0} CurrentUIState {1} ChangeUIState {2} Change",
                        ControlName, _ConveyorUIState.ToString(), recoveryState.ToString());
                    _ConveyorUIState = recoveryState;
                }

                RaisePropertyChanged("ConveyorUIState");
            }
        }
        private bool _IsTrackPause;
        public bool IsTrackPause
        {
            get => _IsTrackPause;
            set
            {
                if (_IsTrackPause.Equals(value))
                    return;

                _IsTrackPause = value;
                RaisePropertyChanged("IsTrackPause");
            }
        }

        protected IConveyorRunnable CVRunner; //컨베이어 구동모듈


        public string CVRunnerModuleName
        {
            get { return CVRunner.GetRunnerName(); }
        }

        //220922 조숭진 rfid 변수 변경 s
        private RFID_ModuleBase _CVRFIDModule;
        public RFID_ModuleBase CVRFIDModule
        {
            get
            {
                return _CVRFIDModule;
            }
            private set
            {
                _CVRFIDModule = value;
            }
        }
        //protected RFID_ModuleBase CVRFIDModule;

        private RFID_ModuleBase _CVRFID_Extend_Module;
        public RFID_ModuleBase CVRFID_Extend_Module
        {
            get
            {
                return _CVRFID_Extend_Module;
            }
            private set
            {
                _CVRFID_Extend_Module = value;
            }
        }
        //protected RFID_ModuleBase CVRFID_Extend_Module; //Box 형 Read 를 위한 추가 모듈

        private int _BCRNum;
        public int BCRNum
        {
            get
            {
                return _BCRNum;
            }
            private set
            {
                _BCRNum = value;
            }
        }

        public void BCRNumSetting(int bcrnum)
        {
            BCRNum = bcrnum;
        }
        //220922 조숭진 rfid 변수 변경 e

        public CarrierItem CurrentCarrier
        {
            get
            {
                return InSlotCarrier;
            }
        }

        /// <summary>
        /// EX : UDRL
        /// EX : U
        /// EX : LR
        /// </summary>
        /// <param name="PosData"></param>
        public void SetFireShutterPos(string PosData)
        {
            if (string.IsNullOrEmpty(PosData))
            {
                return;
            }
            SetFireShutterPos(PosData.Contains("U"), PosData.Contains("D"), PosData.Contains("L"), PosData.Contains("R"));
        }

        public void SetFireShutterPos(bool Up, bool Down, bool Left, bool Right)
        {
            TopFireShutterExist = Up;
            BottomFireShutterExist = Down;
            LeftFireShutterExist = Left;
            RightFireShutterExist = Right;
        }

        //230331 HHJ SCS 개선     //- FireShutter 추가
        private bool _TopFireShutterExist;
        public bool TopFireShutterExist
        {
            get => _TopFireShutterExist;
            set
            {
                _TopFireShutterExist = value;
                RaisePropertyChanged("TopFireShutterExist");
            }
        }
        private bool _BottomFireShutterExist;
        public bool BottomFireShutterExist
        {
            get => _BottomFireShutterExist;
            set
            {
                _BottomFireShutterExist = value;
                RaisePropertyChanged("BottomFireShutterExist");
            }
        }
        private bool _RightFireShutterExist;
        public bool RightFireShutterExist
        {
            get => _RightFireShutterExist;
            set
            {
                _RightFireShutterExist = value;
                RaisePropertyChanged("RightFireShutterExist");
            }
        }
        private bool _LeftFireShutterExist;
        public bool LeftFireShutterExist
        {
            get => _LeftFireShutterExist;
            set
            {
                _LeftFireShutterExist = value;
                RaisePropertyChanged("LeftFireShutterExist");
            }
        }
        private bool _FireShutterAction;
        public bool FireShutterAction
        {
            get => _FireShutterAction;
            set
            {
                _FireShutterAction = value;
                RaisePropertyChanged("FireShutterAction");
            }
        }

        /// <summary>
        /// 포트 체인지 명령 내리기전 커맨드 Nak 체크용
        /// </summary>
        /// <returns></returns>
        public bool CheckInoutTypeChangeAble()
        {
            //현재 캐리어가 존재 하지 않을때만 가능
            bool bCarrierNotExist = !CheckCarrierExist();
            //추후 조건은 필요시 추가함
            return bCarrierNotExist;
        }
        public bool ChangePortInOutType(ePortInOutType RequestType)
        {
            if (GetPortTypeChangeToken() == false)
            {
                //LogManager.WriteConsoleLog(eLogLevel.Info, "ChangePortInOutType  Failed  Module:{0} ReqType :{1} Reason: Port TypeChange Token Get Fail", ModuleName, RequestType);
                return false;
            }
            try
            {
                ePortInOutType BeforeInOutType = this.PortInOutType; //240321 RGJ 포트 타입 변경 실패하고 나중에  PLC 초기화시 바뀐 값으로 동작 하므로  기존 값으로 원복해야함.
                //캐리어가 내부에 없어야 한다.
                bool bCarrierExist = CheckCarrierExist();
                if (bCarrierExist)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "ChangePortInOutType  Failed  Module:{0} ReqType :{1} Reason: Already Carrier Exist in Port", ModuleName, RequestType);
                    return false;
                }
                //요청 타입 체크
                if (RequestType == ePortInOutType.BOTH || RequestType == ePortInOutType.Unknown)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "ChangePortInOutType  Failed  Module:{0} ReqType :{1} Reason: Invalid Request Type", ModuleName, RequestType);
                    return false;
                }
                if (PC_VehicleJobAssign) //ACS 가 해당포트를 예약했음.
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "ChangePortInOutType  Failed  Module:{0} ReqType :{1} Reason: ACS Job Reserved", ModuleName, RequestType);
                    return false;
                }


                //요청 타입이 현재 타입이면 작업없이 True 리턴 로그만 남겨둠
                if (PortInOutType == RequestType)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "ChangePortInOutType  Skipped  Module:{0} ReqType :{1} Reason: Already Type Changed But Interface Start", ModuleName, RequestType);
                    //return true;  //231115 RGJ PLC 초기화시 요청이 들어오는 경우가 있으므로 인터페이스 대응은 함.주석처리
                }

                if (SimulMode) //시뮬 모드면 바로 변경.
                {
                    PortInOutType = RequestType;
                    PortInOutTypeChanged = true;
                    return true;
                }
                else
                {
                    PC_PortTypeChange = false; //요청 비트 초기화

                    PC_SCSMode = RequestType;
                    //요청 타입을 PLC에 Write
                    PC_PortTypeChange = true;
                    Stopwatch timeWatch = Stopwatch.StartNew();
                    while (true)
                    {
                        if (IsTimeout_SW(timeWatch, PLCTimeout))
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "ChangePortInOutType  Failed  Module:{0} ReqType :{1} Reason: PLC Timeout", ModuleName, RequestType);
                            PC_PortTypeChange = false;
                            PC_SCSMode = BeforeInOutType; //실패시 기존값 원복
                            //240209 RGJ PortTypeChange 인터페이스까지 들어갔는데 실패시 알람 발생.
                            GlobalData.Current.Alarm_Manager.AlarmOccurbyName("PORT_TYPE_CHANGE_FAILED", ModuleName);
                            return false;
                        }
                        //PLC 응답 체크
                        bool CommandResponse = PLC_PortTypeChange;
                        bool TypeChanged = PLC_PortType == RequestType;
                        if (CommandResponse && TypeChanged)
                        {
                            PortInOutType = RequestType;
                            PC_PortTypeChange = false;
                            //PC_SCSMode = RequestType; //해당 항목은 유지함
                            PortInOutTypeChanged = true; //내부 초기화 유도
                            LogManager.WriteConsoleLog(eLogLevel.Info, "ChangePortInOutType  Done!.  Module:{0} ReqType :{1} ", ModuleName, RequestType);
                            timeWatch.Restart();
                            while (true)
                            {
                                if (IsTimeout_SW(timeWatch, PLCTimeout))
                                {
                                    LogManager.WriteConsoleLog(eLogLevel.Info, "ChangePortInOutType  Failed  Module:{0} ReqType :{1} Reason: PLC Off Timeout", ModuleName, RequestType);
                                    //240209 RGJ PortTypeChange 인터페이스까지 들어갔는데 실패시 알람 발생.
                                    PC_SCSMode = BeforeInOutType; //실패시 기존값 원복
                                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("PORT_TYPE_CHANGE_FAILED", ModuleName);
                                    return false;
                                }
                                if (PLC_PortTypeChange == false) //포트 변화 요청 OFF 대기
                                {
                                    return true;
                                }
                                Thread.Sleep(IODelay);
                            }

                        }

                        Thread.Sleep(IODelay);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }
            finally
            {
                ReleasePortTypeChangeToken();
            }
        }
        public virtual void SetBuzzerState(eBuzzerControlMode buzzerMode, bool NeedAlarmOccur)
        {
            if (!SimulMode)
            {
                PC_Buzzer = buzzerMode;
            }
            if (NeedAlarmOccur)
            {
                GlobalData.Current.Alarm_Manager.AlarmOccurbyName("PORT_VALIDATION_NG", ModuleName);
            }

        }
        //2024.06.27 lim, 셀버퍼 Auto Keyin 적용 예정
        public virtual void SetAutoKeyinState(string Pallet_Size, bool NeedAlarmOccur)
        {
            if (!SimulMode)
            {
                //PC_Buzzer = buzzerMode;
            }
            if (NeedAlarmOccur)
            {
                if (Pallet_Size == "SHORT")
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("PORT_PALLETSIZE_DATA_MISMATCH_SHORT", ModuleName);
                else
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("PORT_PALLETSIZE_DATA_MISMATCH_LONG", ModuleName);
            }

        }
        public virtual eBuzzerControlMode GetBuzzerState()
        {
            if (!SimulMode)
            {
                return PC_Buzzer;
            }
            else
            {
                return eBuzzerControlMode.BuzzerOFF;
            }
        }

        public eCarrierState GetPortCarrierState()
        {
            CarrierItem Temp = InSlotCarrier;
            if (Temp != null)
            {
                return Temp.CarrierState;
            }
            else
            {
                return eCarrierState.NONE;
            }
        }

        //230306
        public ePalletSize GetPalletSize()
        {
            CarrierItem Temp = InSlotCarrier;

            if (Temp != null)
            {
                return Temp.PalletSize;
            }
            else
            {
                return ePalletSize.NONE;
            }
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
        public eCarrierSize GetCarrierSize()
        {
            CarrierItem Temp = InSlotCarrier;

            if (Temp != null)
            {
                return Temp.CarrierSize;
            }
            else
            {
                return eCarrierSize.Unknown;
            }
        }
        public string GetCurrentCarrierType()
        {
            string CarrierType = CurrentCarrier?.CarrierType;
            if (string.IsNullOrEmpty(CarrierType))
            {
                return string.Empty;
            }
            else
            {
                return CarrierType;
            }
        }
        public string GetCurrentLot()
        {
            string Lot = CurrentCarrier?.LotID;
            if (string.IsNullOrEmpty(Lot))
            {
                return string.Empty;
            }
            else
            {
                return Lot;
            }
        }
        public eProductEmpty GetCurrentProductEmpty()
        {
            eProductEmpty PE = eProductEmpty.NONE;
            CarrierItem PCarrier = CurrentCarrier; //Race Condition 방지를 위해 로컬변수 사용.
            if (PCarrier != null)
            {
                PE = PCarrier.ProductEmpty;
            }
            return PE;
        }
        public eTrayType GetCurrentTrayType()
        {
            eTrayType TT = eTrayType.NONE;
            CarrierItem PCarrier = CurrentCarrier; //Race Condition 방지를 위해 로컬변수 사용.
            if (PCarrier != null)
            {
                TT = PCarrier.TrayType;
            }
            return TT;
        }
        protected bool ActionAbortRequested = false; //전송 동작 취소 요청 플래그
        protected bool ThreadExitRequested
        {
            get;
            private set;
        }
        private eTurnType _TurnType = eTurnType.AirCylinder; //실린더가 기본
        /// <summary>
        /// Turn 동작시 공압,서보등 어떤 방식인지 결정
        /// </summary>
        public eTurnType TurnType
        {
            get
            {
                return _TurnType;
            }
            protected set
            {
                _TurnType = value;
            }
        }


        public int LifeTime_BeltCounter
        {
            get;
            protected set;
        }
        //public int LifeTime_TimingBeltCounter //벨트류 하나로 통합
        //{
        //    get;
        //    protected set;
        //}

        public int LifeTime_Stopper_FWD_CylinderCounter
        {
            get;
            protected set;
        }
        public int LifeTime_Stopper_BWD_CylinderCounter
        {
            get;
            protected set;
        }
        public int LifeTime_Turn_CylinderCounter
        {
            get;
            protected set;
        }

        public void ResetLifeTimeCounter()
        {
            LifeTime_BeltCounter = 0;
            LifeTime_Stopper_FWD_CylinderCounter = 0;
            LifeTime_Stopper_BWD_CylinderCounter = 0;
            LifeTime_Turn_CylinderCounter = 0;
        }

        #region UI 에 표시할 Property 항목
        private string _CurrentActionDesc;
        public string CurrentActionDesc
        {
            get
            {
                return _CurrentActionDesc;
            }
            protected set
            {
                if (_CurrentActionDesc != value)
                {
                    _CurrentActionDesc = value;
                    //LogManager.WritePortLog(eLogLevel.Info, "Port : {0} CurrentAction : {1}", ModuleName, value);
                }
            }
        }
        private string _LastActionResult;
        public string LastActionResult
        {
            get
            {
                return _LastActionResult;
            }
            protected set
            {
                if (_LastActionResult != value)
                {
                    _LastActionResult = value;
                    LogManager.WritePortLog(eLogLevel.Info, "Port : {0} LastActionResult : {1}", ModuleName, value);
                }
            }
        }

        //public string RequestState
        //{
        //    get
        //    {
        //        return REQUEST == "1" ? "ON" : "OFF";
        //    }
        //}
        //public string ReadyState
        //{
        //    get
        //    {
        //        return PORTREADY == "1" ? "ON" : "OFF";
        //    }
        //}
        //public string CompleteState
        //{
        //    get
        //    {
        //        return this.ROBOTCOMPLETE == "1" ? "ON" : "OFF";
        //    }
        //}

        public bool CarrierExist
        {
            get
            {
                return IsCarrierExist();
            }
        }

        public int CarrierPosition
        {
            get
            {
                if (CurrentCarrier != null)
                {
                    return CV_SimulPosition;
                }
                else
                {
                    return 0;
                }
            }
            set
            {
                if (CurrentCarrier != null)
                {
                    CV_SimulPosition = value;
                    OnPropertyChanged("TrayState");

                }
            }
        }



        protected eCV_Speed _ConveyorRunning = eCV_Speed.None;
        public eCV_Speed ConveyorRunning
        {
            get
            {
                return _ConveyorRunning;
            }
            set
            {
                if (_ConveyorRunning != value)
                {
                    _ConveyorRunning = value;
                    OnPropertyChanged("ConveyorRunning");
                }
            }
        }

        protected eCV_StopperState _LastFWD_StopperState = eCV_StopperState.None;
        public eCV_StopperState LastFWD_StopperState
        {
            get
            {
                return _LastFWD_StopperState;
            }
            set
            {
                if (_LastFWD_StopperState != value)
                {
                    _LastFWD_StopperState = value;
                    OnPropertyChanged("LastFWDStopperState");
                }
            }
        }

        protected eCV_StopperState _LastBWD_StopperState = eCV_StopperState.None;
        public eCV_StopperState LastBWD_StopperState
        {
            get
            {
                return _LastBWD_StopperState;
            }
            set
            {
                if (_LastBWD_StopperState != value)
                {
                    _LastBWD_StopperState = value;
                    OnPropertyChanged("LastBWDStopperState");
                }
            }
        }

        protected eCV_DoorState _LastDoorState = eCV_DoorState.Close;
        public eCV_DoorState LastDoorState
        {
            get
            {
                return _LastDoorState;
            }
            set
            {
                if (_LastDoorState != value)
                {
                    _LastDoorState = value;
                    OnPropertyChanged("LastDoorState");
                }
            }
        }

        protected eCV_TurnState _LastTurnState = eCV_TurnState.Unknown;
        public eCV_TurnState LastTurnState
        {
            get
            {
                return _LastTurnState;
            }
            set
            {
                if (_LastTurnState != value)
                {
                    _LastTurnState = value;
                    OnPropertyChanged("LastTurnState");
                }
            }
        }

        protected eCV_ChuckState _LastChuckState = eCV_ChuckState.Unknown;
        public eCV_ChuckState LastChuckState
        {
            get
            {
                return _LastChuckState;
            }
            set
            {
                if (_LastChuckState != value)
                {
                    _LastChuckState = value;
                    OnPropertyChanged("LastChuckState");
                }
            }
        }

        protected eCV_TrayPadState LastPadState = eCV_TrayPadState.Unknown;

        protected eCV_StackerHoldState _LastHoldState = eCV_StackerHoldState.Unknown;
        public eCV_StackerHoldState LastHoldState
        {
            get
            {
                return _LastHoldState;
            }
            set
            {
                if (_LastHoldState != value)
                {
                    _LastHoldState = value;
                    OnPropertyChanged("LastHoldState");
                }
            }
        }

        public bool _LastLightCutainMuteOnState = false;
        public bool LastLightCutainMuteOnState
        {
            get
            {
                return _LastLightCutainMuteOnState;
            }
            set
            {
                if (_LastLightCutainMuteOnState != value)
                {
                    _LastLightCutainMuteOnState = value;
                    OnPropertyChanged("LastLightCutainMuteOnState");
                }
            }
        }

        protected bool _LastRFIDConnected = false;
        public bool LastRFIDConnected
        {
            get
            {
                return _LastRFIDConnected;
            }
            set
            {
                if (_LastRFIDConnected != value)
                {
                    _LastRFIDConnected = value;
                    OnPropertyChanged("LastRFIDConnected");
                }
            }
        }

        public event PropertyChangedEventHandler CVPropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            CVPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #endregion

        #region CV 간 PIO 사용 시그널
        public bool Internal_ToPrevCV_LoadRequest { get; protected set; }   //트레이 진입 허가.
        public bool Internal_ToPrevCV_InComplete { get; protected set; }    //트레이 진입 완료
        public bool Internal_ToPrevCV_Error { get; protected set; } // 에러 발생


        public bool Internal_ToNextCV_UnloadRequest { get; protected set; } //트레이 제품 도착
        public bool Internal_ToNextCV_OutComplete { get; protected set; }   //트레이 배출 완료
        public bool Internal_ToNextCV_Error { get; protected set; } // 에러 발생


        protected CV_BaseModule ToBoothCV; //해당 컨베이어가 연결되어있는 부스 방향  CV [Null 이면 부스 시작단]
        protected CV_BaseModule AwayBoothCV;  //해당 컨베이어가 연결되어있는 반대 방향  CV [Null 이면 부스 끝단]

        //캐리어가 이동할 다음CV
        public CV_BaseModule NextCV
        {
            get
            {
                if (PortInOutType == ePortInOutType.OUTPUT) //Outport
                {
                    return AwayBoothCV;
                }
                else if (PortInOutType == ePortInOutType.INPUT) //Inport
                {
                    return ToBoothCV;
                }
                else
                {
                    return null;
                }
            }
        }
        //양끝 종단 포트 임을 의미
        public bool IsTerminalPort
        {
            get
            {
                return NextCV == null;

            }
        }



        public CV_BaseModule PrevCV
        {
            get
            {
                if (PortInOutType == ePortInOutType.OUTPUT) //Outport
                {
                    return ToBoothCV;
                }
                else if (PortInOutType == ePortInOutType.INPUT) //Inport
                {
                    return AwayBoothCV;
                }
                else
                {
                    return null;
                }
            }
        }

        #endregion

        #region Robot Access Point
        public int Position_Bank
        {
            get;
            protected set;
        }
        public int Position_Bay
        {
            get;
            protected set;
        }
        public int Position_Level
        {
            get;
            protected set;
        }

        public string GetRobotCommandTag()
        {
            string tag = string.Format("{0:D2}{1:D3}{2:D2}", Position_Bank, Position_Bay, Position_Level);
            return tag;
        }

        private bool PosInitialized = false;//프로그램 실행시 한번만 Set 하도록 제한
        public bool SetRobotAccessPosition(int bank, int bay, int level)
        {
            if (PosInitialized)
            {
                return false;
            }
            //검증 및 중복체크 코드는 나중에 추가
            //220523 HHJ SCS 개선     //- ShelfSetterControl 신규 추가
            //if ((bank == 1 || bank == 2) && bay >= 0 && level > 0)
            if ((bank == GlobalData.Current.FrontBankNum || bank == GlobalData.Current.RearBankNum) && bay >= 0 && level > 0)
            {
                Position_Bank = bank;
                Position_Bay = bay;
                Position_Level = level;
                PosInitialized = true;
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion

        #region Robot Interface
        public bool RobotAccessAble
        {
            get //PLC Bit Check
            {
                bool CarrierPosCheck = false;
                bool bReqSignal = false;
                bool bAutoMode = PLC_KeySwitch; //컨베이어 오토모드일때만 가능
                bool bCV_Idle = !PLC_CVBusy; //컨베이어 구동중 동작불가

                if (IsInPort)
                {
                    CarrierPosCheck = PLC_CarrierSensor; //Carrier 존재해야 동작 가능
                    bReqSignal = PLC_UnloadRequest && !PLC_UnloadComplete;
                }
                else
                {
                    CarrierPosCheck = !PLC_CarrierSensor; //Carrier 없어야 동작 가능
                    bReqSignal = PLC_LoadRequest && !PLC_LoadComplete;
                }
                return CarrierPosCheck && bReqSignal && bAutoMode && bCV_Idle;
            }
        }
        //LKJ 시뮬에서 사용으로 protected->public으로 변경
        public RMModuleBase LastRM = null;
        public bool CarrierPutComplete
        {
            get;
            protected set;
        }
        public bool CarrierGetComplete
        {
            get;
            protected set;
        }
        public bool CraneActionError
        {
            get;
            protected set;
        }
        protected McsJob CraneCommandJob = null;

        //RM 이 캐리어를 내려놨음을 통지
        public bool NotifyTrayLoadComplete(RMModuleBase RM, McsJob ActionJob)
        {
            LastRM = RM;
            CraneCommandJob = ActionJob;
            CarrierPutComplete = true;
            if (ActionJob != null) //로그를 남겨둔다.
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "RM {0} notified that carrier has been Putted. Job :{1} CarrierID: {2}", RM.ModuleName, ActionJob.CommandID, ActionJob.CarrierID);
            }
            else
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "RM {0} notified that carrier has been Putted.But ActionJob is Null... ", RM.ModuleName);
            }

            return true;
        }
        //RM 이 캐리어를 가져갔음을 통지
        public bool NotifyTrayUnloadComplete(RMModuleBase RM, McsJob ActionJob)
        {
            LastRM = RM;
            CraneCommandJob = ActionJob;
            CarrierGetComplete = true;
            if (ActionJob != null) //로그를 남겨둔다.
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "RM {0} notified that carrier has been Getted. Job :{1} CarrierID: {2}", RM.ModuleName, ActionJob.CommandID, ActionJob.CarrierID);
            }
            else
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "RM {0} notified that carrier has been Putted.But ActionJob is Null... ", RM.ModuleName);
            }
            return true;
        }
        //RM 이 크레인 Get,Put 도중 알람이 발생 했음을 알림.
        public bool NotifyCraneErrorOccurred(RMModuleBase RM, McsJob ActionJob)
        {
            LastRM = RM;
            CraneCommandJob = ActionJob;
            CraneActionError = true;
            if (ActionJob != null) //로그를 남겨둔다.
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "RM {0} notified that Crane Alarm Occurred during Port Interface. Job :{1} CarrierID: {2}", RM.ModuleName, ActionJob.CommandID, ActionJob.CarrierID);
            }
            else
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "RM {0}  notified that Crane Alarm Occurred during Port Interface .But ActionJob is Null... ", RM.ModuleName);
            }
            return true;
        }

        public bool CheckRobotActionDone()
        {
            if (LastRM != null)
            {
                if (SimulMode)
                {
                    return true;
                }
                else
                {
                    bool bRobotActionDone = (LastRM.GetRMState() == eRMPmacState.Initialized_Idle);
                    return bRobotActionDone;
                }
            }
            else
            {
                return false;
            }
        }
        #endregion


        public CV_BaseModule(string mName, bool simul) : base(mName, simul)
        {
            ////캐리어 정보 복구시 해당 태그명으로 캐리어 스토리지에서 캐리어 정보를 가져와서 해당 정보로 업데이트한다.
            ////shelf.DefaultSlot.SetCarrierData(trayid);
            //if (CarrierStorage.Instance.GetInModuleCarrierItem(ModuleName) is CarrierItem carrier)
            //{
            //    UpdateCarrier(carrier.CarrierID, false);
            //}

            GlobalData.Current.protocolManager.PLCMap.OnUnitDataChanged += OnUnitDataChangedAction;     //231025 HHJ Exist Update 분리
        }
        protected virtual void CVMainRun()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "CV Module : {0} Run Thread Started!", this.ModuleName);
            while (!ThreadExitRequested)
            {
                //Do Nothing,,
                Thread.Sleep(LocalStepCycleDelay);
            }
            return;
        }

        /// <summary>
        /// //20231009 RGJ 포트 Run 서버 클라이언트 분리 
        /// 혼동 방지 및 관리 원활함을 위하여 기존 메인 런에서 클라이언트 런으로 분리함.
        /// </summary>
        protected virtual void CVClientRun()
        {
            GlobalData.Current.MRE_GlobalDataCreatedEvent.WaitOne(); //모든 모듈 생성전까지 Run 대기
            while (!ThreadExitRequested)
            {

                try //-메인 루프 예외 발생시 로그 찍도록 추가.
                {

                    bool bAuto = PLC_KeySwitch; //클라이언트에서도 Auto Manaul UI 상태 변경 필요
                    //if (AutoManualState == eCVAutoManualState.AutoRun && !bAuto) //오토 상태인데 키스위치 Off 되었으면 메뉴얼로 전환.
                    //{
                    //    SetAutoMode(eCVAutoManualState.ManualRun); //메뉴얼 상태로 변경.
                    //}
                    //else if (AutoManualState != eCVAutoManualState.AutoRun && bAuto) //메뉴얼 상태인데 키스위치 On 되었으면 오토 전환.
                    //{
                    //    SetAutoMode(eCVAutoManualState.AutoRun); //오토 상태로 변경.
                    //}
                    //bAuto = false;
                    AutoManualState = bAuto ? eCVAutoManualState.AutoRun : eCVAutoManualState.ManualRun;

                    if (CVUSE != PC_PortEnable)
                    {
                        CVUSE = PC_PortEnable;
                    }
                    bool TrackPauseState = PC_TrackPause == 1; //240718 RGJ 클라이언트 트랙 포즈 상태 업데이트 추가.
                    if (IsTrackPause != TrackPauseState)
                    {
                        IsTrackPause = TrackPauseState;
                    }

                    if (!CarrierExistBySensor()) //화물 감지 안됨.
                    {
                        ResetCarrierData();
                    }
                    else //화물 감지됨.
                    {
                        if (CarrierStorage.Instance.GetInModuleCarrierItem(ModuleName) is CarrierItem carrier)
                        {
                            UpdateCarrier(carrier.CarrierID, false);
                        }
                        else
                        {
                            //데이터가 없으면 표시라도 해야함.
                            DefaultSlot.SetCarrierExist(true); //UI 표현용 값 Set
                        }
                    }
                    Thread.Sleep(200);
                }
                catch (Exception ex)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                }
            }
        }

        public void ExitRunThread()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "CV Module : {0} CVClientRun Thread Exis Request!", this.ModuleName);
            ThreadExitRequested = true;
        }

        protected bool ProcessCarrierJobEnd(string CarrierID)
        {
            try
            {
                var CarrierJob = GlobalData.Current.McdList.GetCarrierJob(CarrierID);
                LogManager.WriteConsoleLog(eLogLevel.Info, "ProcessCarrierJobEnd CarrierID : {0}, CarrierJob : {1} ", CarrierID, CarrierJob?.CommandID);
                if (CarrierJob != null)
                {

                    CarrierJob.TCStatus = eTCState.NONE;
                    CarrierJob.JobResult = eJobResultCode.SUCCESS; //2024.08.28 lim, Job이 끝났으면 정상 완료 보고 한다.
                    GlobalData.Current.HSMS.SendS6F11(207, "JobData", CarrierJob); //TransferCompleted 207
                    GlobalData.Current.McdList.DeleteMcsJob(CarrierID); //작업 목록에서 삭제
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }
        }

        protected bool ProcessCarrierRemove(string CarrierID)
        {
            try
            {
                CarrierStorage.Instance.RemoveStorageCarrier(CarrierID); //STK Domain 에서 캐리어 제거.
                GlobalData.Current.HSMS.SendS6F11(303, "PORT", this, "CARRIERID", CarrierID); //S6F11 CarrierRemoved 303 Port
                return true;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }
        }

        protected virtual CV_ActionResult InitializeAction()
        {
            return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "CV_BaseModule 의 InitializeAction()을 호출 하였습니다.");
        }
        protected virtual CV_ActionResult CVAutoAction()
        {
            return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "CV_BaseModule 의 CVAutoAction()을 호출 하였습니다.");
        }
        /// <summary>
        /// 크레인-> 포트 캐리어 투입되는 동작
        /// </summary>
        /// <returns></returns>
        protected virtual CV_ActionResult CarrierPutInAction()
        {
            return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "CV_BaseModule 의 CarrierPutInAction()을 호출 하였습니다.");
        }
        /// <summary>
        /// 포트 -> 크레인 캐리어 가져가는 동작
        /// </summary>
        /// <returns></returns>
        protected virtual CV_ActionResult CarrierGetOutAction()
        {
            return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "CV_BaseModule 의 CarrierGetOutAction()을 호출 하였습니다.");
        }
        /// <summary>
        /// 다른 모듈로 부터 캐리어 들어오는 동작
        /// </summary>
        /// <returns></returns>
        protected virtual CV_ActionResult ReceiveCarrierAction()
        {
            return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "CV_BaseModule 의 ReceiveTrayAction()을 호출 하였습니다.");
        }
        /// <summary>
        /// 다른 모듈로 캐리어 나가는 동작
        /// </summary>
        protected virtual CV_ActionResult SendOutCarrierAction()
        {
            return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "CV_BaseModule 의 SendTrayAction()을 호출 하였습니다.");
        }


        /// <summary>
        /// 작업자가 수동으로 포트로 캐리어를 넣는 동작
        /// </summary>
        /// <returns></returns>
        protected virtual CV_ActionResult ManualCarrierLoadAction()
        {
            return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "CV_BaseModule 의 ManualCarrierLoadAction()을 호출 하였습니다.");
        }
        /// <summary>
        /// 작업자가 수동으로 포트에서 캐리어를 가져가는 동작
        /// </summary>
        protected virtual CV_ActionResult ManualCarrierUnloadAction()
        {
            return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "CV_BaseModule 의 ManualCarrierUnloadAction()을 호출 하였습니다.");
        }
        protected virtual CV_ActionResult ModuleAction()
        {
            return new CV_ActionResult(ModuleName, eCV_ActionResult.Aborted, "CV_BaseModule 의 ModuleAction()을 호출 하였습니다.");
        }
        protected virtual CV_ActionResult ErrorHandlingAction()
        {
            CurrentActionDesc = "Error Reset 명령을 기다립니다.";
            LocalActionStep = 0;
            PC_CIMAlarmClear = false;
            PC_CIMReportComp = false;
            RecoveryRequest = false;
            PC_TransferPossible = false;
            while (true)
            {
                bool bPLCAlarmClear = SimulMode ? false : PLC_AlarmClear;
                if (RecoveryRequest || bPLCAlarmClear) //Alarm Reset Request Check
                {
                    RecoveryRequest = false;
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Module : {0}  Alarm Clear Request ON  CIM:{1} PLC:{2}", ModuleName, RecoveryRequest, bPLCAlarmClear);
                    break;
                }
                else
                {
                    //IF 없이 PLC 리셋 해서 처리되는 경우를 대비해서 여기에도 추가.
                    //바로 넘어오면 PLC Write 타이밍상 그대로 0으로 읽힐 가능성이 있으므로 잠시 대기.
                    Thread.Sleep(1000); //1초정도 대기해본다.
                    if (PLC_ErrorCode == 0) //이미 알람 클리어 상태
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "ErrorHandlingAction Module : {0}  Alarm already cleared", ModuleName);
                        GlobalData.Current.Alarm_Manager.AlarmClear(ModuleName);
                        ConveyorUIState = eConveyorUIState.AlarmClear;      //230217 HHJ SCS 개선     //CV UI State 관련 추가
                        NextCVCommand = eCVCommand.Initialize; //모듈에 맞게 초기화작업 다시 시작
                        return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "에러 핸들링 완료");
                    }
                    Thread.Sleep(LocalStepCycleDelay);
                    continue;
                }

            }
            #region 시뮬 모드 바로 종료
            if (SimulMode)
            {
                ConveyorUIState = eConveyorUIState.AlarmClear;      //230217 HHJ SCS 개선     //CV UI State 관련 추가
                NextCVCommand = eCVCommand.Initialize; //모듈에 맞게 초기화작업 다시 시작
                return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "에러 핸들링 완료");
            }
            #endregion

            while (true) // PLC Alarm Clear Step 진행
            {
                switch (LocalActionStep)
                {
                    case 0:
                        CurrentActionDesc = "AlarmClear Step 시작";
                        if (PLC_ErrorCode == 0) //이미 알람 클리어 상태
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "ErrorHandlingAction Module : {0}  Alarm already cleared", ModuleName);
                            GlobalData.Current.Alarm_Manager.AlarmClear(ModuleName);
                            if (PLC_AlarmClear) //231130 RGJ 간혹 포트에서 알람 클리어 요청이 계속 남아 있는 경우가 있어서 리포트 신호는 준다.
                            {
                                PC_CIMReportComp = true; //PLC 에게 MCS 알람 클리어 보고 했음을 알려준다.
                                Thread.Sleep(500);
                                PC_CIMReportComp = false;
                            }
                            ConveyorUIState = eConveyorUIState.AlarmClear;      //230217 HHJ SCS 개선     //CV UI State 관련 추가
                            NextCVCommand = eCVCommand.Initialize; //모듈에 맞게 초기화작업 다시 시작
                            return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "에러 핸들링 완료");
                        }
                        else
                        {
                            PC_CIMAlarmClear = true; //Alarm 클리어 지시
                        }
                        LocalActionStep++;
                        break;
                    case 1:
                        CurrentActionDesc = "PLC AlarmClear 대기";
                        if (PLC_AlarmClear)
                        {
                            PC_CIMAlarmClear = false;
                            //GlobalData.Current.Alarm_Manager.AlarmClear(ModuleName);      //241023 HoN Alarm Clear 시점 변경      //시점 변경 주석 처리
                            PC_CIMReportComp = true; //PLC 에게 MCS 알람 클리어 보고 했음을 알려준다.
                            LocalActionStep++;
                            break;
                        }
                        if (PLC_ErrorCode == 0) //이미 알람 클리어 상태
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "ErrorHandlingAction Module : {0} Step: 1 Alarm already cleared", ModuleName);
                            PC_CIMAlarmClear = false;
                            //GlobalData.Current.Alarm_Manager.AlarmClear(ModuleName);        //241023 HoN Alarm Clear 시점 변경      //시점 변경 주석 처리
                            PC_CIMReportComp = true; //PLC 에게 MCS 알람 클리어 보고 했음을 알려준다.
                            LocalActionStep++;
                        }
                        break;
                    case 2:
                        CurrentActionDesc = "PLC AlarmClear OFF 대기";
                        if (PLC_AlarmClear == false) //PLC 알람 상태 해제 대기
                        {
                            PC_CIMReportComp = false; //PC 신호 OFF
                            PC_ErrorCode = 0;
                            LocalActionStep++;
                        }
                        break;
                    case 3:
                        CurrentActionDesc = "PLC Error Code OFF 대기";
                        if (PLC_ErrorCode == 0)
                        {
                            GlobalData.Current.Alarm_Manager.AlarmClear(ModuleName);      //241023 HoN Alarm Clear 시점 변경      //시점 변경
                            ConveyorUIState = eConveyorUIState.AlarmClear;      //230217 HHJ SCS 개선     //CV UI State 관련 추가
                            NextCVCommand = eCVCommand.Initialize; //모듈에 맞게 초기화작업 다시 시작
                            return new CV_ActionResult(ModuleName, eCV_ActionResult.Complete, "에러 핸들링 완료");
                        }
                        break;
                }
                Thread.Sleep(LocalStepCycleDelay);
            }
        }

        //public virtual void ReportModule_LCS()
        //{
        //    //GlobalData.Current.WCF_mgr.ReportPortStatus(ModuleName);
        //}

        public virtual void RequestAbort()
        {
            ActionAbortRequested = true;
        }
        public virtual void ReleaseAbort()
        {
            ActionAbortRequested = false;
        }

        #region  Carrier Interface
        public bool UpdateCarrier(string CarrierID)
        {
            DefaultSlot.SetCarrierData(CarrierID);
            return true;
        }

        //UpdateCarrier로 단일화
        //public bool InsertCarrier(CarrierItem targetCarrier)
        //{
        //    if (targetCarrier != null)
        //    {
        //        DefaultSlot.SetCarrierData(targetCarrier.CarrierID);
        //        return true;
        //    }
        //    return false;
        //}

        #endregion

        #region LightCurtain Interface

        public void SetLightCurtainNumber(int LC_Number)
        {
            LightCurtainNumber = LC_Number;
        }
        /// <summary>
        /// 부스 모듈로 라이트커튼 뮤트 신호 제어를 요청.
        /// </summary>
        /// <param name="OnOff"></param>
        /// <returns></returns>
        public virtual bool LightCurtainMuteControl(bool OnOff)
        {
            if (SimulMode || LightCurtainNumber <= 0)
            {
                LastLightCutainMuteOnState = OnOff;
                return true;
            }
            else
            {
                bool result = GlobalData.Current.MainBooth.SetLightCurtainMute(LightCurtainNumber, OnOff);
                LastLightCutainMuteOnState = result;
                return OnOff == result;   //요청한 동작과 뮤트 상태가 같다면 성공 리턴

            }
        }

        /// <summary>
        /// 부스모듈에서 해당 컨베이어 라인 라이트커튼 뮤트 상태를 조회
        /// </summary
        /// <returns>        
        /// true  : 라이트커튼 뮤트 상태
        /// false : 라이트커튼 인터락 동작 상태
        /// </returns>
        public bool CheckLightCurtainMute()
        {
            if (SimulMode || LightCurtainNumber <= 0)
            {
                return LastLightCutainMuteOnState;
            }
            else
            {
                LastLightCutainMuteOnState = GlobalData.Current.MainBooth.GetLightCurtainMuteState(LightCurtainNumber);
                return LastLightCutainMuteOnState;
            }
        }

        #endregion

        public void SetTrackInfo(string Group, string number)
        {
            int.TryParse(Group, out int TG);
            int.TryParse(number, out int TN);
            if (TrackGroup == 0 && TrackNum == 0) //한번만 설정가능
            {
                TrackGroup = TG;
                TrackNum = TN;
                BaseAddress = (ushort)(40000 + (TrackNum * 50) - 40);
            }
        }

        //220628 HHJ SCS 개선		//- PLCDataItems 개선
        public short PLCNum { get; protected set; }
        public ushort BaseAddress { get; protected set; }
        public void SetPLCNum(short plcnum)
        {
            PLCNum = plcnum;
        }
        //계산으로 설정하나 변경 할수 있게 일단 보존.
        public void SetBaseAddress(ushort baseaddress)
        {
            BaseAddress = baseaddress; //설절
        }


        public bool AttactchRFID(RFID_ModuleBase RFID)
        {
            if (CVRFIDModule == null)
            {
                CVRFIDModule = RFID;
                RFID.SetParentModule(this);
                LogManager.WriteConsoleLog(eLogLevel.Info, "CV : {0}  RFID :{1} attached", this.ModuleName, RFID.ModuleName);
                CVRFIDModule.OnRFIDConnectionStateChanged += CVRFIDModule_OnRFIDConnectionStateChanged;
                if (GlobalData.Current.ServerInstance)
                {
                    CVRFIDModule.InitRFIDReader();//여기서 초기화까지 실행.
                }

                return true;
            }
            else if (CVRFID_Extend_Module == null)
            {
                CVRFID_Extend_Module = RFID;
                RFID.SetParentModule(this);
                LogManager.WriteConsoleLog(eLogLevel.Info, "CV : {0}  RFID :{1} attached.", this.ModuleName, RFID.ModuleName);
                CVRFID_Extend_Module.OnRFIDConnectionStateChanged += CVRFIDModule_OnRFIDConnectionStateChanged;
                if (GlobalData.Current.ServerInstance)
                {
                    CVRFID_Extend_Module.InitRFIDReader();//여기서 초기화까지 실행.
                }
                return true;
            }
            return false;
        }

        private void CVRFIDModule_OnRFIDConnectionStateChanged(bool connection)
        {
            LastRFIDConnected = connection;

            PortBCRState = connection ? eBCRState.AutoBCROnline : eBCRState.OFFLine;
        }

        public bool AttactchInverter(Inverter CVInverter)
        {
            if (CVRunner == null)
            {
                this.CVRunner = CVInverter;
                CVInverter.SetParentModule(this);
                LogManager.WriteConsoleLog(eLogLevel.Info, "CV모듈 : {0} 에 Inverter 모듈 :{1} 이 부착되었습니다.", this.ModuleName, CVInverter.ModuleName);
                return true;
            }
            else
            {
                return false;
            }
        }
        /// <summary>
        /// 현재 중알람 상태인지 확인
        /// </summary>
        /// <returns></returns>
        public virtual bool CheckFirmwareAlarm()
        {
            bool HeavyAlarm = GlobalData.Current.Alarm_Manager.CheckHeavyAlarmExist();

            return HeavyAlarm;
        }

        public virtual bool IsCarrierExist()
        {
            //if (SimulMode) //LKJ 임시 주석
            //{
            //    //시뮬에서는 캐리어 정보로만 판단.
            //    return CarrierExistByData();
            //}
            //else
            //{
            return CarrierExistBySensor();
            //}

        }
        public virtual int GetTrayHeight()
        {
            if (CarrierExist)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
        public virtual bool CarrierExistBySensor()
        {
            //if (SimulMode) //LKJ 임시 주석
            //{
            //    return false;
            //}
            //else
            //{
            return PLC_CarrierSensor;
            //}
        }
        //2024.08.22 lim, 통신 딜레이로 트렉킹 데이터 누락 체크 추가
        public virtual bool CheckCarrierDataExist()
        {
            bool r1 = !string.IsNullOrEmpty(PC_CarrierID);
            bool r2 = (PC_PalletSize != ePalletSize.NONE);
            bool r3 = (PC_ProductEmpty != eProductEmpty.NONE);


            return r1 && r2 && r3;
        }
        public virtual bool CarrierExistByData()
        {
            return DefaultSlot.MaterialExist && !string.IsNullOrEmpty(DefaultSlot.MaterialName);
            //return InSlotCarrier != null;
        }
        public bool IsInPort
        {
            get
            {
                return (PortInOutType == ePortInOutType.INPUT);
            }
        }
        private ePortType _PortType;
        public ePortType PortType
        {
            get
            {
                return _PortType;
            }
            set
            {
                if (_PortType != value)
                {
                    _PortType = value;
                }
            }
        }

        public virtual bool GetCVBusy()
        {
            return PLC_CVBusy;
        }

        public virtual bool CheckTrayLoadingPosition()
        {
            throw new NotImplementedException("CheckTrayLoadingPosition()는 구현되지 않았습니다.");
        }
        public virtual bool CheckTrayUnloadingPosition()
        {
            throw new NotImplementedException("CheckTrayUnloadingPosition()는 구현되지 않았습니다.");
        }


        public bool CreateRFIDModule(string RFID_ModuleType)
        {
            try
            {
                Type type = Type.GetType("Stockerfirmware.RFID." + RFID_ModuleType);

                this.CVRFIDModule = Activator.CreateInstance(type) as RFID_ModuleBase;

                return true;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }
        }
        public void SetPortSize(ePortSize Size)
        {
            this.PortSize = Size;
        }


        public void SetToBoothCV(CV_BaseModule CV)
        {
            ToBoothCV = CV;
        }
        public void SetAwayBoothCV(CV_BaseModule CV)
        {
            AwayBoothCV = CV;
        }

        public void SetDirection(ePortInOutType direction)
        {
            this.PortInOutType = direction;
        }

        //230214 HHJ SCS 개선
        public void SetCVWay(eCVWay cvway)
        {
            CVWay = cvway;

        }

        public void SetAutoMode(eCVAutoManualState Auto)
        {
            CV_ErrorResetRequest(); //오토 시작 할때마다 에러 복구
            AutoManualState = Auto;
            OnPropertyChanged("ConveyorAutoStateChanged");
        }


        public void NotifyCraneMode(eCraneSCMode Mode)
        {
            ConnectedCraneSCMode = Mode;
        }

        //230217 HHJ SCS 개선     //TrackPause AutoMode와 연동되지않도록
        public void SetTrackPause(bool bvalue)
        {
            PC_TrackPause = bvalue ? (short)1 : (short)0;
        }

        public void SetPortAccessMode(ePortAceessMode Mode)
        {
            this.PortAccessMode = Mode;
        }

        public void SetUsingStopper(bool Use)
        {
            UseStopper = Use;
        }
        public void SetUsingBackStopper(bool Use)
        {
            UseBackStopper = Use;
        }
        public void SetUsingDoor(bool Use)
        {
            UseDoor = Use;
        }

        public void SetDoorNumber(string Number) //lsj SESS Door
        {
            DoorNumber = Number;
        }

        public void SetBypassMode(bool Bypass)
        {
            this.UseBypassMode = Bypass;
        }

        public void SetTurnAngle(int Angle)
        {
            this.TurnAngle = Angle;
        }

        #region CV 기본 동작 인터페이스 노출
        public virtual bool CVForwardRun(eCV_Speed spd = eCV_Speed.Low)
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "CV Fwd Run  {0} spd {1}", this.ModuleName, spd);
            if (CVModuleType == eCVType.RobotIF && NextCV != null && NextCV.CVModuleType == eCVType.ShuttleTurn) //Robot IF 에서 셔틀로 굴릴때  인터락 추가.
            {
                if (NextCV.GetCurrentShuttlePosition() != eShuttlePosition.Master) //정위치가 아니면 어떤 동작도 해선 안된다.
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "CV :{0}  Fwd Run Abort by Shuttle Interlock!", this.ModuleName);
                    return false; //동작 거절
                }
            }
            bool bRun = CVRunner.CV_ForwardRun(spd);
            ConveyorRunning = GetCurrentRunSpeed();
            LifeTime_BeltCounter++; //소모품 관리 변수 업데이트
            return bRun;
        }
        public virtual bool CVBackwardRun(eCV_Speed spd = eCV_Speed.Low)
        {
            bool bRun = CVRunner.CV_BackwardRun(spd);
            ConveyorRunning = GetCurrentRunSpeed();
            LifeTime_BeltCounter++; //소모품 관리 변수 업데이트
            return bRun;
        }

        /// <summary>
        /// 스톱퍼를 업(클로즈) 동작 제어.
        /// </summary>
        /// <param name="ForwardStopper"> True 정방향기준 앞쪽의 스톱퍼를 UP 한다.</param>
        /// <returns></returns>
        public virtual bool CVStopperClose(bool ForwardStopper = true)
        {
            if (!UseStopper) //스톱퍼 미사용 모듈은 바로리턴.
            {
                return true;
            }
            if (GetCVStopperState(ForwardStopper) == eCV_StopperState.Up) //상태 봐서 동작 완료되어있으면 동작 필요 없음
            {
                return true;
            }
            if (SimulMode)
            {
                Thread.Sleep(500);
                if (ForwardStopper)
                {
                    LifeTime_Stopper_FWD_CylinderCounter++; //소모품 관리 변수 업데이트
                    LastFWD_StopperState = eCV_StopperState.Up;
                }
                else
                {
                    LifeTime_Stopper_BWD_CylinderCounter++; //소모품 관리 변수 업데이트
                    LastBWD_StopperState = eCV_StopperState.Up;
                }
                return true;
            }
            CCLinkManager.CCLCurrent.WriteIO(ModuleName, string.Format("CV_STOPPER_{0}_DOWN", ForwardStopper ? "FWD" : "BWD"), false);
            Thread.Sleep(IODelay);
            CCLinkManager.CCLCurrent.WriteIO(ModuleName, string.Format("CV_STOPPER_{0}_UP", ForwardStopper ? "FWD" : "BWD"), true);
            Stopwatch timeWatch = Stopwatch.StartNew();
            while (!IsTimeout_SW(timeWatch, StopperUpdownTimeout))
            {
                if (GetCVStopperState() == eCV_StopperState.Up)
                {
                    Thread.Sleep(IODelay);
                    //헌팅성을 제외하기 위해 한번더 확인.
                    if (GetCVStopperState(ForwardStopper) == eCV_StopperState.Up)
                    {
                        if (ForwardStopper)
                        {
                            LifeTime_Stopper_FWD_CylinderCounter++; //소모품 관리 변수 업데이트
                            LastFWD_StopperState = eCV_StopperState.Up;
                        }
                        else
                        {
                            LifeTime_Stopper_BWD_CylinderCounter++; //소모품 관리 변수 업데이트
                            LastBWD_StopperState = eCV_StopperState.Up;
                        }
                        return true;
                    }
                }
                Thread.Sleep(IODelay);
            }
            GlobalData.Current.Alarm_Manager.AlarmOccurbyName("STOPPER_MOTION_ERROR", ModuleName);
            return false; // 타임아웃이내 실행 실패.
        }
        /// <summary>
        /// 스톱퍼를 다운(오픈) 동작 제어.
        /// </summary>
        /// <param name="ForwardStopper"> True : 정방향기준 앞쪽의 스톱퍼를 Down 한다.</param>
        /// <returns></returns>
        public virtual bool CVStopperOpen(bool ForwardStopper = true)
        {
            if (!UseStopper) //스톱퍼 미사용 모듈은 바로리턴.
            {
                return true;
            }
            if (GetCVStopperState(ForwardStopper) == eCV_StopperState.Down) //상태 봐서 동작 완료되어있으면 동작 필요 없음
            {
                return true;
            }
            if (SimulMode)
            {
                Thread.Sleep(500);
                if (ForwardStopper)
                {
                    LifeTime_Stopper_FWD_CylinderCounter++; //소모품 관리 변수 업데이트
                    LastFWD_StopperState = eCV_StopperState.Down;

                }
                else
                {
                    LifeTime_Stopper_BWD_CylinderCounter++; //소모품 관리 변수 업데이트
                    LastBWD_StopperState = eCV_StopperState.Down;
                }

                return true;
            }
            CCLinkManager.CCLCurrent.WriteIO(ModuleName, string.Format("CV_STOPPER_{0}_UP", ForwardStopper ? "FWD" : "BWD"), false);
            Thread.Sleep(IODelay);
            CCLinkManager.CCLCurrent.WriteIO(ModuleName, string.Format("CV_STOPPER_{0}_DOWN", ForwardStopper ? "FWD" : "BWD"), true);
            Thread.Sleep(IODelay);
            Stopwatch timeWatch = Stopwatch.StartNew();
            while (!IsTimeout_SW(timeWatch, StopperUpdownTimeout))
            {

                if (GetCVStopperState(ForwardStopper) == eCV_StopperState.Down)
                {
                    Thread.Sleep(IODelay);
                    //헌팅성을 제외하기 위해 한번더 확인.
                    if (GetCVStopperState(ForwardStopper) == eCV_StopperState.Down)
                    {
                        if (ForwardStopper)
                        {
                            LifeTime_Stopper_FWD_CylinderCounter++; //소모품 관리 변수 업데이트
                            LastFWD_StopperState = eCV_StopperState.Down;
                        }
                        else
                        {
                            LifeTime_Stopper_BWD_CylinderCounter++; //소모품 관리 변수 업데이트
                            LastBWD_StopperState = eCV_StopperState.Down;
                        }
                        return true;
                    }
                }
                Thread.Sleep(IODelay);
            }
            GlobalData.Current.Alarm_Manager.AlarmOccurbyName("STOPPER_MOTION_ERROR", ModuleName);
            return false; // 타임아웃이내 실행 실패.
        }

        /// <summary>
        /// 스택커 홀더 동작 제어. 
        /// </summary>
        /// <param name="HoldLock"> True : 홀더 락을 걸어서 트레이를 유지 시킨다. False : 홀더 락을 해제.</param>
        /// <returns></returns>
        public virtual bool CVStackHolderControl(bool HoldLock)
        {
            if (SimulMode)
            {
                return true;
            }
            if (HoldLock)
            {
                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "CV_STACK_HOLD_RELEASE", false);
                Thread.Sleep(IODelay);
                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "CV_STACK_HOLD_LOCK", true);
                Thread.Sleep(IODelay);
            }
            else
            {

                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "CV_STACK_HOLD_LOCK", false);
                Thread.Sleep(IODelay);
                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "CV_STACK_HOLD_RELEASE", true);
                Thread.Sleep(IODelay);
            }
            Stopwatch timeWatch = Stopwatch.StartNew();
            while (!IsTimeout_SW(timeWatch, StackerActionTimeout))
            {
                if (HoldLock)
                {
                    if (GetCVHolderState() == eCV_StackerHoldState.Hold)
                    {
                        Thread.Sleep(IODelay);
                        //헌팅성을 제외하기 위해 한번더 확인.
                        if (GetCVHolderState() == eCV_StackerHoldState.Hold)
                        {
                            LastHoldState = eCV_StackerHoldState.Hold;
                            return true;
                        }
                    }
                }
                else
                {
                    if (GetCVHolderState() == eCV_StackerHoldState.Release)
                    {
                        Thread.Sleep(IODelay);
                        //헌팅성을 제외하기 위해 한번더 확인.
                        if (GetCVHolderState() == eCV_StackerHoldState.Release)
                        {
                            LastHoldState = eCV_StackerHoldState.Release;
                            return true;
                        }
                    }
                }

                Thread.Sleep(IODelay);
            }
            GlobalData.Current.Alarm_Manager.AlarmOccurbyName("SHORT_CYLINDER_MOTION_ERROR", ModuleName);
            return false; // 타임아웃이내 실행 실패.
        }


        public virtual bool CVTray_Chuck()
        {
            throw new NotImplementedException("CVTray_Chuck() 는 구현되지 않았습니다.");
        }
        public virtual bool CVTray_Unchuck()
        {
            throw new NotImplementedException("CVTray_Unchuck() 는 구현되지 않았습니다.");
        }

        public virtual void CVDoorOpenSol()
        {
            if (!UseDoor || SimulMode) //도어 미사용 모듈은 바로리턴.
            {
                return;
            }

            //if(GlobalData.Current.CurrnetLineSite != eLineSite.SESS)
            //{
            //    //OHT IN 과 OHT OUT 의 SOL I/O 다름.
            //    if (IsInPort)
            //    {
            //        CCLinkManager.CCLCurrent.WriteIO(GlobalData.Current.MainBooth.ModuleName, "DOOR_OPEN_SOL_OHT_IN", true);
            //    }
            //    else
            //    {
            //        CCLinkManager.CCLCurrent.WriteIO(GlobalData.Current.MainBooth.ModuleName, "DOOR_OPEN_SOL_OHT_OUT", true);
            //    }
            //}
            //else
            //{
            //    //lsj SESS Door
            //    CCLinkManager.CCLCurrent.WriteIO(GlobalData.Current.MainBooth.ModuleName, String.Format("CV_DOOR_OPEN_SOL_{0}", DoorNumber), true);
            //}

        }
        public virtual void CVDoorCloseSol()
        {
            if (!UseDoor || SimulMode) //도어 미사용 모듈은 바로리턴.
            {
                return;
            }

            //if (GlobalData.Current.CurrnetLineSite != eLineSite.SESS)
            //{
            //    //OHT IN 과 OHT OUT 의 SOL I/ O 다름.
            //    if (IsInPort)
            //    {
            //        CCLinkManager.CCLCurrent.WriteIO(GlobalData.Current.MainBooth.ModuleName, "DOOR_OPEN_SOL_OHT_IN", false);
            //    }
            //    else
            //    {
            //        CCLinkManager.CCLCurrent.WriteIO(GlobalData.Current.MainBooth.ModuleName, "DOOR_OPEN_SOL_OHT_OUT", false);
            //    }
            //}
            //else
            //{
            //    CCLinkManager.CCLCurrent.WriteIO(GlobalData.Current.MainBooth.ModuleName, String.Format("CV_DOOR_OPEN_SOL_{0}", DoorNumber), false);
            //}
        }
        /// <summary>
        /// Door Sol 상태 체크
        /// </summary>
        /// <returns></returns>
        public virtual bool GetCVDoorSolOnState()
        {
            if (!UseDoor || SimulMode) //도어 미사용 모듈은 바로리턴.
            {
                return false;
            }
            {
                return CCLinkManager.CCLCurrent.ReadIO(GlobalData.Current.MainBooth.ModuleName, String.Format("CV_DOOR_OPEN_CHECK_{0}", DoorNumber));

            }
        }

        public virtual string CVBCR_Read(bool TargetExtendModule = false, int retryCounter = 1)
        {
            string data = string.Empty;
            for (int i = 0; i < retryCounter; i++)
            {
                if (TargetExtendModule)
                {
                    CVRFID_Extend_Module.ReadRFID(out data);
                }
                else
                {
                    CVRFIDModule.ReadRFID(out data);
                }

                if (data == "ERROR")
                {
                    continue;
                    //GlobalData.Current.Alarm_Manager.AlarmOccurbyName("RFID_FAIL",ModuleName); //알람 안올리고 ERROR 로 보고만
                }
                else
                {
                    //return data;
                    break;
                }
            }

            //220922 조숭진
            LogManager.WriteBCRLNgLog(eLogLevel.Info, "ModuleID:{0}/TrackID:{1}/BCRNo:{2}/ReadData:{3}", ModuleName, TrackID, BCRNum, data);

            return data;
        }
        public bool CheckRFID_Connection()
        {
            if (CVRFIDModule != null)
            {
                return CVRFIDModule.CheckConnection();
            }
            else
            {
                return false;
            }
        }
        public bool CheckRFID_ReadTriggerSent()
        {
            if (CVRFIDModule != null)
            {
                return CVRFIDModule.ReadTriggerSent;
            }
            else
            {
                return false;
            }
        }
        public bool CheckRFID_ReadCompleted()
        {
            if (CVRFIDModule != null)
            {
                return CVRFIDModule.ReadCompleted;
            }
            else
            {
                return false;
            }
        }
        public virtual bool CVRFID_Write(string tagData)
        {
            if (SimulMode)
                return true;
            return this.CVRFIDModule.WriteRFID(Encoding.ASCII.GetBytes(tagData));
        }
        /// <summary>
        /// 인버터 동작 정지.
        /// </summary>
        /// <returns></returns>
        public virtual bool CV_RunStop()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "CV RunStop {0}", this.ModuleName);
            CVRunner.CV_Stop();
            ConveyorRunning = GetCurrentRunSpeed();
            return true;
        }
        public virtual void CV_RunEMGStop()
        {
            GlobalData.Current.Alarm_Manager.AlarmOccur(EMG_ERROR_CODE.ToString(), ModuleName);
            if (!SimulMode)
            {
                PC_ErrorCode = EMG_ERROR_CODE;
            }
        }
        protected ServoAxis_IOControl IOServo;
        //2021.05.25 lim, TurnOHTIF 와 같이 사용하기 위해 변경
        public void SetTurnDevice(eTurnType turnType, int axis)
        {
            TurnType = turnType;

            switch (turnType)
            {
                case eTurnType.AirCylinder:
                    break;
                case eTurnType.Servo:
                    ServoAxis = axis;
                    break;
                case eTurnType.ServoIO:
                    IOServo = new ServoAxis_IOControl(this.ModuleName, SimulMode);
                    break;
                case eTurnType.ServeIO_Dellta:
                    IOServo = new ServoAxis_DeltaControl(this.ModuleName, SimulMode);
                    break;
            }
        }

        public virtual bool DoTurnAction()
        {
            throw new NotImplementedException();
        }
        public virtual bool DoHomeAction()
        {
            throw new NotImplementedException();
        }
        public virtual bool DoReturnAction()
        {
            throw new NotImplementedException();
        }


        #endregion

        public virtual eCV_StopperState GetCVStopperState(bool ForwardStopper = true)
        {
            if (UseStopper)
            {
                if (SimulMode)
                {
                    if (ForwardStopper)
                    {
                        return LastFWD_StopperState;
                    }
                    else
                    {
                        return LastBWD_StopperState;
                    }
                }
                if (UseBackStopper || CVModuleType == eCVType.OHTIF || CVModuleType == eCVType.Turn || CVModuleType == eCVType.TurnEQIF || CVModuleType == eCVType.TurnOHTIF || CVModuleType == eCVType.TurnBridge || CVModuleType == eCVType.ShuttleTurn) //2021.05.24 lim, TurnOHT 추가
                {
                    bool bUp = CCLinkManager.CCLCurrent.ReadIO(ModuleName, string.Format("CV_STOPPER_{0}_UP_CHECK", ForwardStopper ? "FWD" : "BWD"));
                    if (bUp)
                    {
                        return eCV_StopperState.Up;
                    }
                    else
                    {
                        return eCV_StopperState.Down;
                    }
                }
                else
                {
                    if (ForwardStopper)
                    {
                        bool bUp = CCLinkManager.CCLCurrent.ReadIO(ModuleName, string.Format("CV_STOPPER_FWD_UP_CHECK"));
                        if (bUp)
                        {
                            return eCV_StopperState.Up;
                        }
                        else
                        {
                            return eCV_StopperState.Down;
                        }
                    }
                    else
                    {
                        return eCV_StopperState.Down;
                    }
                }


            }
            else
            {
                return eCV_StopperState.Down;
            }
        }

        public virtual eCV_DoorState GetCVDoorState()
        {
            if (UseDoor)
            {
                //부스모듈에서 정보 획득
                if (IsInPort)
                {
                    //오픈 상태 및 도어솔 ON 이면 OPEN 으로 간주
                    if (GlobalData.Current.MainBooth.OHTIn_DoorOpen || GlobalData.Current.MainBooth.OHTIn_DoorOpenSolOn)
                    {
                        return eCV_DoorState.Open;
                    }
                    else
                    {
                        return eCV_DoorState.Close;
                    }
                }
                else
                {
                    //오픈 상태 및 도어솔 ON 이면 OPEN 으로 간주
                    if (GlobalData.Current.MainBooth.OHTOut_DoorOpen || GlobalData.Current.MainBooth.OHTOut_DoorOpenSolOn)
                    {
                        return eCV_DoorState.Open;
                    }
                    else
                    {
                        return eCV_DoorState.Close;
                    }
                }
            }
            else
            {
                return eCV_DoorState.Close; //도어 사용안하면 닫혀있는것으로 본다.
            }
        }

        public virtual eCV_TurnState GetCVTurnState()
        {
            return eCV_TurnState.Unknown;//상속받아서 구현필요
        }
        public virtual eCV_ChuckState GetCVChuckState()
        {
            if (SimulMode)
            {
                return LastChuckState;
            }
            bool bChuck1 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CV_TRAY_CHUCK1");
            bool bChuck2 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CV_TRAY_CHUCK2");

            if (bChuck1 && bChuck2)
            {
                return eCV_ChuckState.Chuck;
            }
            else if (!bChuck1 && !bChuck2)
            {
                return eCV_ChuckState.Unchuck;
            }
            else
            {
                return eCV_ChuckState.Unknown;
            }
        }

        public virtual eCV_StackerHoldState GetCVHolderState()
        {
            if (SimulMode)
            {
                return LastHoldState;
            }
            bool bHold1 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CV_STACK_HOLD_CHECK1");
            bool bHold2 = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CV_STACK_HOLD_CHECK2");

            if (!bHold1 && !bHold2)
            {
                return eCV_StackerHoldState.Hold;
            }
            else if (bHold1 && bHold2)
            {
                return eCV_StackerHoldState.Release;
            }
            else
            {
                return eCV_StackerHoldState.Unknown;
            }
        }

        public virtual bool CVStackPadControl(bool Up)
        {
            if (SimulMode)
            {
                if (Up)
                {
                    LastPadState = eCV_TrayPadState.PadUp;
                }
                else
                {
                    LastPadState = eCV_TrayPadState.PadDown;
                }
                return true;
            }

            if (Up)
            {
                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "CV_STACK_PAD_DOWN", false);
                Thread.Sleep(IODelay);
                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "CV_STACK_PAD_UP", true);
                Thread.Sleep(IODelay);
            }
            else
            {
                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "CV_STACK_PAD_UP", false);
                Thread.Sleep(IODelay);
                CCLinkManager.CCLCurrent.WriteIO(ModuleName, "CV_STACK_PAD_DOWN", true);
                Thread.Sleep(IODelay);
            }
            Stopwatch timeWatch = Stopwatch.StartNew();
            while (!IsTimeout_SW(timeWatch, StopperUpdownTimeout))
            {
                if (Up)
                {
                    if (GetCVPadState() == eCV_TrayPadState.PadUp)
                    {
                        Thread.Sleep(IODelay);
                        //헌팅성을 제외하기 위해 한번더 확인.
                        if (GetCVPadState() == eCV_TrayPadState.PadUp)
                        {
                            LastPadState = eCV_TrayPadState.PadUp;
                            return true;
                        }
                    }
                }
                else
                {
                    if (GetCVPadState() == eCV_TrayPadState.PadDown)
                    {
                        Thread.Sleep(IODelay);
                        //헌팅성을 제외하기 위해 한번더 확인.
                        if (GetCVPadState() == eCV_TrayPadState.PadDown)
                        {
                            LastPadState = eCV_TrayPadState.PadDown;
                            return true;
                        }
                    }
                }

                Thread.Sleep(IODelay);
            }
            GlobalData.Current.Alarm_Manager.AlarmOccurbyName("SHORT_CYLINDER_MOTION_ERROR", ModuleName);
            return false; // 타임아웃이내 실행 실패.
        }

        public virtual eCV_TrayPadState GetCVPadState()
        {
            if (SimulMode)
            {
                return LastPadState;
            }
            bool bDown = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CV_STACK_PAD_DOWN_CHECK");
            bool bUP = CCLinkManager.CCLCurrent.ReadIO(ModuleName, "CV_STACK_PAD_UP_CHECK");

            if (bDown && !bUP)
            {
                return eCV_TrayPadState.PadDown;
            }
            else if (!bDown && bUP)
            {
                return eCV_TrayPadState.PadUp;
            }
            else
            {
                return eCV_TrayPadState.Unknown;
            }
        }

        public virtual eCV_Speed GetCurrentRunSpeed()
        {
            return CVRunner.CV_GetCurrentRunningSpeed();
        }

        public void SetPortTableID(int ID)
        {
            if (ID > 0)
            {
                this.PortTableID = ID;
            }
        }
        public virtual bool CVInitAction()
        {
            //기존 LBS 로직 제거
            //bool bInitRunner = CVRunner.InitConveyorRunner();
            //if (bInitRunner)
            //{
            //    PORTREADY = "1";
            //}

            //220628 HHJ SCS 개선     //- PLCDataItems 개선
            ////220608 HHJ SCS 개선     //- MCProtocol, MXComponent 추가
            ////220315 HHJ SCS 개발     //- MXComponentNet, SharedMemory 개선 //Test용
            ////PLCtoPC = PLCItemHelper.ModulePLCItemSetter(eAreaType.PLCtoPC, GData.iCVNum);
            ////PCtoPLC = PLCItemHelper.ModulePLCItemSetter(eAreaType.PCtoPLC, GData.iCVNum);
            //PLCtoPC = ProtocolHelper.ModulePLCItemSetter(eAreaType.PLCtoPC, GData.iCVNum);
            //PCtoPLC = ProtocolHelper.ModulePLCItemSetter(eAreaType.PCtoPLC, GData.iCVNum);
            //GData.iCVNum++;
            PLCtoPC = ProtocolHelper.GetPLCItem(eAreaType.PLCtoPC, "CV", PLCNum, BaseAddress);
            PCtoPLC = ProtocolHelper.GetPLCItem(eAreaType.PCtoPLC, "CV", PLCNum, BaseAddress);

            //SuHwan_20230127 : [ServerClient] 추가
            //if (GlobalData.Current.ServerClientType != eServerClientType.Client)
            //{
            //    Thread CVMainThread = new Thread(new ThreadStart(CVMainRun));
            //    CVMainThread.Name = this.ModuleName + " CV Run";
            //    CVMainThread.IsBackground = true;
            //    CVMainThread.Start();
            //}

            //클라이언트도 UI 갱신 때문에 쓰레드 돌아아 한다.
            if (GlobalData.Current.ServerInstance)
            {
                Thread CVMainThread = new Thread(new ThreadStart(CVMainRun)); //20231009 RGJ 포트 Run 서버 클라이언트 분리 
                CVMainThread.Name = this.ModuleName + " CV Run";
                CVMainThread.IsBackground = true;
                CVMainThread.Start();
            }
            else if (GlobalData.Current.ClientInstance)
            {
                Thread CVClientThread = new Thread(new ThreadStart(CVClientRun)); //20231009 RGJ 포트 Run 서버 클라이언트 분리 
                CVClientThread.Name = this.ModuleName + " CV Client Run";
                CVClientThread.IsBackground = true;
                CVClientThread.Start();
            }
            return true;
        }

        public virtual bool CVTrayLoadAction(bool RequireRecovery = false)
        {
            throw new NotImplementedException();
        }
        public virtual bool CVTrayUnloadAction(bool RequireRecovery = false)
        {
            throw new NotImplementedException();
        }


        public virtual eShuttlePosition GetCurrentShuttlePosition()
        {
            throw new NotImplementedException("GetCurrentShuttlePosition() 구현 되지 않았습니다.");
        }
        public override bool DoAbnormalCheck()
        {
            //2401116 RGJ DoAbnormal 체크중에 PLC  Manual 상태 체크
            bool bAuto = PLC_KeySwitch;
            if (AutoManualState == eCVAutoManualState.AutoRun && !bAuto) //오토 상태인데 키스위치 Off 되었으면 메뉴얼로 전환.
            {
                SetAutoMode(eCVAutoManualState.ManualRun); //메뉴얼 상태로 변경.
            }
            else if (AutoManualState != eCVAutoManualState.AutoRun && bAuto) //메뉴얼 상태인데 키스위치 On 되었으면 오토 전환.
            {
                SetAutoMode(eCVAutoManualState.AutoRun); //오토 상태로 변경.
            }

            short AlarmCode = 0;
            short PCAlarmCode = PC_ErrorCode; //240809 RGJ 포트 경알람 올리자마자 자동클리어를 막기위해 추가.
            if (!SimulMode)
            {
                AlarmCode = PLC_ErrorCode;
            }
            else
            {
                AlarmCode = SimulAlarmCode; //알람코드 획득
            }
            if (AlarmCode > 0)
            {
                if (GlobalData.Current.Alarm_Manager.CheckModuleAlarmExist(ModuleName) == false) //알람은 유닛당 1개만 발생
                {
                    GlobalData.Current.Alarm_Manager.AlarmOccur(AlarmCode.ToString(), ModuleName);// 알람 발생
                    Thread.Sleep(50);
                    if (GlobalData.Current.Alarm_Manager.CheckModuleHeavyAlarmExist(ModuleName))
                    {
                        ConveyorUIState = eConveyorUIState.Alarm;      //230217 HHJ SCS 개선     //CV UI State 관련 추가
                        return true; //중알람
                    }
                    else
                    {
                        return false; //경알람
                    }

                }
                else //이미 알람상태이면 
                {
                    if (GlobalData.Current.Alarm_Manager.CheckModuleHeavyAlarmExist(ModuleName))
                    {
                        return true; //중알람
                    }
                    else
                    {
                        return false; //경알람
                    }
                }
                //if (GlobalData.Current.Alarm_Manager.CheckModuleHeavyAlarmExist(ModuleName) == false) //알람은 유닛당 1개만 발생
                //{
                //    GlobalData.Current.Alarm_Manager.AlarmOccur(AlarmCode.ToString(), ModuleName);
                //    ConveyorUIState = eConveyorUIState.Alarm;      //230217 HHJ SCS 개선     //CV UI State 관련 추가
                //    return true;
                //}
                //else
                //{
                //    return false; //경알람은 예외처리
                //}
            }
            else
            {
                //올라와 있는 알람이 없어도 알람 상태면 알람 클리어전까지는 에러로 본다.
                if (GlobalData.Current.Alarm_Manager.CheckModuleHeavyAlarmExist(ModuleName))
                {
                    ConveyorUIState = eConveyorUIState.Alarm;      //230217 HHJ SCS 개선     //CV UI State 관련 추가
                    return true;
                }
                else if (GlobalData.Current.Alarm_Manager.CheckModuleAlarmExist(ModuleName)) //경알람은 알람 코드 없으면 해제한다. PC 조건 추가.
                {
                    if (PCAlarmCode == 0) //아직 알람 매니저에서 수정전일수도 있으니 좀 기다릴 필요 있음.
                    {
                        Thread.Sleep(1000); //0.5초 만 더 기다려본다.   //2024.08.12 lim, 1초 필요
                        PCAlarmCode = PC_ErrorCode;
                        if (PCAlarmCode == 0)
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "DoAbnormalCheck Module : {0}  Alarm already cleare. PLC,PCAlarmCode check 0", ModuleName);
                            GlobalData.Current.Alarm_Manager.AlarmClear(ModuleName);
                        }
                        else  //2024.08.31 lim, 2초 동안 PLC 와 PC 알람이 Mismatch 되면 Error code 클리어
                        {
                            Thread.Sleep(1000);
                            PCAlarmCode = PC_ErrorCode;
                            if (PCAlarmCode != 0)
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "DoAbnormalCheck Module : {0}  PLC AlarmCode Mismatch PC Alarmcode change 0", ModuleName);
                                PC_ErrorCode = 0;
                            }
                        }
                    }

                }

                //230217 HHJ SCS 개선     //CV UI State 관련 추가
                //PC에서만 변경이 되면 변경하는 시점에 처리하면 되지만, PLC에서도 변경할 수 있는 부분이기에 실시간 처리가 필요하지만
                //실시간 처리를 하는곳이 없고 PLC변경에 대한 Event를 받아올 수 있는곳이 없다.
                IsTrackPause = PC_TrackPause.Equals(0) ? false : true;      //TrackPauase Value가 0이 아니면 무조건 TrackPause로 판단

                return false;
            }
        }

        public virtual void CV_ErrorResetRequest()
        {
            RecoveryRequest = true; //에러상태 복구요청  
        }

        public bool CV_ManualCommandAction(string command)
        {
            switch (command)
            {
                case "Forward_Low":
                    return CVForwardRun(eCV_Speed.Low);
                case "Forward_Mid":
                    return CVForwardRun(eCV_Speed.Mid);
                case "Forward_High":
                    return CVForwardRun(eCV_Speed.High);
                case "Reverse_Low":
                    return CVBackwardRun(eCV_Speed.Low);
                case "Reverse_Mid":
                    return CVBackwardRun(eCV_Speed.Mid);
                case "Reverse_High":
                    return CVBackwardRun(eCV_Speed.High);
                case "CV_EStop":
                    CV_RunEMGStop();
                    return true;
                case "CV_Stop":
                    return CV_RunStop();

                case "Stopper_FWD_Up":
                    return CVStopperClose();
                case "Stopper_FWD_Down":
                    return CVStopperOpen();

                case "Stopper_BWD_Up":
                    return CVStopperClose(false);
                case "Stopper_BWD_Down":
                    return CVStopperOpen(false);

                case "Mute_On":
                    return LightCurtainMuteControl(true);
                case "Mute_Off":
                    return LightCurtainMuteControl(false);

                case "Door_Open":
                    CVDoorOpenSol();
                    return true;
                case "Door_Close":
                    CVDoorCloseSol();
                    return true;
                case "CV_Turn":
                    return DoTurnAction();
                case "CV_Return":
                    return DoReturnAction();
                case "CV_Home":
                    return DoHomeAction();
                case "RFID_Read":
                    if (!this.CheckRFID_Connection()) //수동일때는  RFID 접속 상태체크
                    {
                        if (!CVRFIDModule.TCPSocketConnect())
                        {
                            return false;
                        }
                    }
                    string temp = CVBCR_Read();
                    if (temp != "ERROR")
                    {
                        UpdateCarrier(temp);
                    }
                    LogManager.WriteConsoleLog(eLogLevel.Info, temp);//메뉴얼로 읽었으면  로그를 따로 찍는다.
                    return true;
                case "RFID_Write":
                    return CVRFID_Write("");
                case "Error_Reset":
                    CV_ErrorResetRequest();
                    return true;
                case "Tray_Chuck":
                    return CVTray_Chuck();
                case "Tray_Unchuck":
                    return CVTray_Chuck();
                case "Simul_TrayLoad":
                    //if(CVModuleType == eCVType.OperatorIF)
                    //{
                    //    Simul_In_IOArray[0] = true; //Entry 가상입력
                    //}
                    //else if(CVModuleType == eCVType.OHTIF)
                    //{
                    //    InsertCarrier(new Tray("test", true));
                    //}
                    //else if (CVModuleType == eCVType.TurnOHTIF) //2021.05.24 lim, TurnOHT 추가
                    //{
                    //    InsertCarrier(new Tray("test", true));
                    //}
                    //else if(CVModuleType == eCVType.EQIF)
                    //{
                    //    InsertCarrier(new Tray("test", true));
                    //}
                    //else if (CVModuleType == eCVType.EQRobot)
                    //{
                    //    InsertCarrier(new Tray("test", true));
                    //}
                    //else if (CVModuleType == eCVType.OHTRobot) //2021.05.24 lim, AGV 추가
                    //{
                    //    InsertCarrier(new Tray("test", true));
                    //}
                    break;
                case "ForceUnload": //Stacker 모듈만 사용
                    //var CVModule = this as CV_StackerModule;
                    //if(CVModule != null)
                    //{
                    //    LogManager.WriteConsoleLog(eLogLevel.Info, "ForceUnload 버튼을 입력 처리합니다.");
                    //    CVModule.PushForceUnloadButton();
                    //}
                    break;
                case "Cancel_Trans": //오퍼레이터의 전송작업 취소 요청.
                    this.RequestAbort();
                    break;

                default:
                    DoCustomAction(command); //미리 정의 안되어 있으면 커스텀 액션 동작
                    break;
            }
            return false;
        }

        public override void WriteCurrentState()
        {

            return;
            //현재 포트 상태 디비 사용할 필요없음. 추후 필요하면 재검토
            //Database.DataBaseManager.Current.UpdateConveyorState(this);
        }

        public virtual void ClearInternalSignals()
        {
            Internal_ToPrevCV_LoadRequest = false;
            Internal_ToPrevCV_InComplete = false;
            Internal_ToPrevCV_Error = false;
            Internal_ToNextCV_UnloadRequest = false;
            Internal_ToNextCV_OutComplete = false;
            Internal_ToNextCV_Error = false;
        }
        public string GetInternalSignals()
        {
            return string.Format("{0}{1}{2}{3}{4}{5}",
            Internal_ToPrevCV_LoadRequest ? "1" : "0",
            Internal_ToPrevCV_InComplete ? "1" : "0",
            Internal_ToPrevCV_Error ? "1" : "0",
            Internal_ToNextCV_UnloadRequest ? "1" : "0",
            Internal_ToNextCV_OutComplete ? "1" : "0",
            Internal_ToNextCV_Error ? "1" : "0");
        }
        public bool RecoveryInternalSignal(string Signals)
        {
            if (string.IsNullOrEmpty(Signals))
                return false;
            if (Signals.Length != 6)
                return false;
            Internal_ToPrevCV_LoadRequest = (Signals[0] == '1');
            Internal_ToPrevCV_InComplete = (Signals[1] == '1');
            Internal_ToPrevCV_Error = (Signals[2] == '1');
            Internal_ToNextCV_UnloadRequest = (Signals[3] == '1');
            Internal_ToNextCV_OutComplete = (Signals[4] == '1');
            Internal_ToNextCV_Error = (Signals[5] == '1');
            return true;

        }
        public override bool ReadLastState()
        {
            return true;
        }
        public virtual bool CheckOHTArmDetected()
        {
            return false;
        }

        public virtual void DoCustomAction(string ActionTag)
        {
            return;
        }

        public string CustomActionTag1
        {
            get;
            protected set;
        }
        public string CustomActionTag2
        {
            get;
            protected set;
        }
        public string CustomActionTag3
        {
            get;
            protected set;
        }

        public virtual string CustomActionName1
        {
            get;
            protected set;
        }
        public virtual string CustomActionName2
        {
            get;
            protected set;
        }
        public virtual string CustomActionName3
        {
            get;
            protected set;
        }
        public virtual bool CheckStartSwitch()
        {
            //Start Switch 체크 필요하면 오버라이드 한다.
            return false;
        }

        public virtual void StartSwitchLampControl(bool OnOff)
        {
            //Lamp 컨트롤 필요하면 오버라이드 한다.
            return;
        }
        public virtual bool RequestStartSwitchBlink()
        {
            return false;
        }
        public virtual bool GetCVDataError()
        {
            return false;
        }
        //220315 HHJ SCS 개발     //- MXComponentNet, SharedMemory 개선 //Test용
        #region PLCInterface

        #region Tracking Read/Write Data  Area
        //사용 예시) 확장은 필요없을것으로 생각됨. 특정 모듈에 기재해야하는 비트 / 워드 존재 시, 확장된 모듈에 추가를 하면됨.
        public string PC_CarrierID
        {
            get
            {

                string readCarrierID = GData.protocolManager.ReadString(ModuleName, PCtoPLC, "PC_CarrierID");
                return readCarrierID.Replace(" ", "");//공백 제거
            }
            set
            {
                if (value.Length > 40) //40자리 제한
                {
                    GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_CarrierID", value.Substring(0, 40));
                }
                else
                {
                    GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_CarrierID", value);
                }
            }
        }

        /// <summary>
        /// Track No는 상위 바이트에는 CPU Group 번호, 하위 바이트에는 CV Number를 기재한다.
        /// Ex)Track번호 21099 -> 상위 바이트 0x15, 하위바이트 0x63 기입
        /// </summary>
        public short PC_DestCPUNum
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_DestCPUNum"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_DestCPUNum", value); }
        }
        public short PC_DestTrackNum
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_DestTrackNum"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_DestTrackNum", value); }
        }
        public static string ConvertDestination(short Group, short TrackNumber)
        {
            //string Dest = Group.ToString() + TrackNumber.ToString();
            string Dest = string.Format("{0}{1:D3}", Group, TrackNumber);
            return Dest;
        }

        public eTrayType PC_TrayType
        {
            get { return (eTrayType)GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_TrayType"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TrayType", (ushort)value); }
        }
        public short PC_TrayStackCount
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_TrayStackCount"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TrayStackCount", value); }
        }
        public ePolarity PC_Polarity
        {
            get { return (ePolarity)GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_Polarity"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_Polarity", (ushort)value); }
        }
        public eProductEmpty PC_ProductEmpty
        {
            get { return (eProductEmpty)GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_ProductEmpty"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_ProductEmpty", (ushort)value); }
        }
        public eWinderDirection PC_WinderDirection
        {
            get { return (eWinderDirection)GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_WinderDirection"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_WinderDirection", (ushort)value); }
        }
        public short PC_ProductQuantiry
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_ProductQuantiry"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_ProductQuantiry", value); }
        }
        public short PC_CellPackingLine//10,11,12,13 2자리수로 적는다.화성검사 라인번호. 
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_CellPackingLine"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_CellPackingLine", value); }
        }
        public eInnerTrayType PC_InnerTrayType
        {
            get { return (eInnerTrayType)GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_InnerTrayType"); }

            set
            {
                GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_InnerTrayType", (ushort)value);
            }
        }
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
        public eUnCoatedPart PC_UnCoatedPart
        {
            get { return (eUnCoatedPart)GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_UnCoatedPart"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_UnCoatedPart", (ushort)value); }
        }
        public eCoreType PC_CoreType
        {
            get { return (eCoreType)GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_CoreType"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_CoreType", (ushort)value); }
        }
        public eProductEnd PC_ProductEnd
        {
            get { return (eProductEnd)GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_ProductEnd"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_ProductEnd", (ushort)value); }
        }
        public short PC_ValidationNG
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_ValidationNG"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_ValidationNG", value); }
        }
        #endregion

        #region NonTracking Read/Write Data Area

        //4) SCS에서는 ENABLE/DISABLE 기능으로 사용함.
        public short PC_TrackPause // 0 => 정상   1 => Pause 요청
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_TrackPause"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TrackPause", value); }
        }

        /// <summary>
        /// 아래 value값은 SKOH2
        /// 1: 입고모드
        /// 2. 출고모드
        /// 3. 양방향모드
        /// 아래 value값은 SKOY
        /// 2. 입고모드
        /// 4. 출고모드
        /// 6. 양방향모드
        /// </summary>
        public ePortInOutType PC_SCSMode
        {
            get
            {
                {
                    short ReadMyValue = GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_SCSMode");
                    return (ePortInOutType)ReadMyValue;
                }
            }
            set
            {
                {
                    GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_SCSMode", (ushort)value);
                }
            }
        }

        public short PC_ErrorCode
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_ErrorCode"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_ErrorCode", value); }
        }

        /// <summary>
        /// 0일경우 : 부저 OFF
        /// 1일경우 : 부저 ON
        /// 9일경우 : Validation NG
        /// </summary>
        public eBuzzerControlMode PC_Buzzer
        {
            get { return (eBuzzerControlMode)GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_Buzzer"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_Buzzer", (ushort)value); }
        }

        public short PC_McsSelect
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PCtoPLC, "PC_McsSelect"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_McsSelect", value); }
        }

        public bool PC_BCRReadComplete
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PCtoPLC, "PC_BCRReadComplete"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_BCRReadComplete", value); }

        }
        public bool PC_TransferPossible
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PCtoPLC, "PC_TransferPossible"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_TransferPossible", value); }
        }
        public bool PC_CIMAlarmClear
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PCtoPLC, "PC_CIMAlarmClear"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_CIMAlarmClear", value); }
        }
        public bool PC_CIMReportComp
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PCtoPLC, "PC_CIMReportComp"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_CIMReportComp", value); }
        }
        //미사용 주석처리
        ///// <summary>
        ///// CIM에서 BCR Read시 PLC에서 기재한 정보와 실물 정보가 다를경우, 자재정보 변경을 위해 Bit On 해준다
        ///// </summary>
        //public bool PC_BCRReadValidNGResponse
        //{
        //    get { return GData.protocolManager.ReadBit(ModuleName, PCtoPLC, "PC_BCRReadValidNGResponse"); }
        //    set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_BCRReadValidNGResponse", value); }
        //}

        /// <summary>
        /// CIM에서 BCR Read 에러 발생시 Bit On해준다.
        /// </summary>
        public bool PC_BCRReadFail
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PCtoPLC, "PC_BCRReadFail"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_BCRReadFail", value); }
        }

        public bool PC_PortEnable
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PCtoPLC, "PC_PortEnable"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_PortEnable", value); }
        }

        public bool PC_PortTypeChange
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PCtoPLC, "PC_PortTypeChange"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_PortTypeChange", value); }
        }

        public bool PC_VehicleJobAssign
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PCtoPLC, "PC_VehicleJobAssign"); }
            set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PC_VehicleJobAssign", value); }
        }
        #endregion

        #region  NonTracking  Read Only PLC Area
        public short PLC_ErrorCode
        {
            get
            {
                short ECode = GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_ErrorCode");
                if (ECode == 1) //241212 RGJ 1도 0으로 간주.원인 불명의 포트 1 알람 올라오는것때문에 임시 수정함. 원인 파악후 원복예정.
                {
                    return 0;
                }
                else
                {
                    return ECode;
                }
            }
        }

        #region Status Data
        public bool PLC_KeySwitch
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_KeySwitch"); }
        }
        public bool PLC_CVBusy
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_CVBusy"); }
        }
        public bool PLC_PortAccessMode
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_PortAccessMode"); }
        }

        public bool PLC_CarrierInPos //도착대 도착
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_CarrierInPos"); }
        }
        public bool PLC_SCLoadingHS
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_SCLoadingHS"); }
        }
        public bool PLC_SCUnloadingHS
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_SCUnloadingHS"); }
        }

        public bool PLC_AGVAccessAble
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_AGVAccessAble"); }
        }

        public bool PLC_LoadRequest
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_LoadRequest"); }
        }
        public bool PLC_LoadComplete
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_LoadComplete"); }
        }
        public bool PLC_UnloadRequest
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_UnloadRequest"); }
        }
        public bool PLC_UnloadComplete
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_UnloadComplete"); }
        }

        public bool PLC_NGUnloadRequest
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_NGUnloadRequest"); }
        }
        public bool PLC_BCRReadRequest
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_BCRReadRequest"); }
        }
        public bool PLC_Timeout
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_Timeout"); }
        }

        #endregion

        #region Sensor Data
        /// <summary>
        /// 45 - 0 bit
        /// </summary>
        public bool PLC_CarrierSensor
        {
            get
            {
                bool Sensor = GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_CarrierSensor");
                return Sensor;
            }
        }
        /// <summary>
        /// 45 - 1 bit
        /// </summary>
        public bool PLC_EmptyBobinSensor
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_EmptyBobinSensor"); }
        }
        /// <summary>
        /// 45 - 2 bit
        /// </summary>
        public bool PLC_MaterialSensor
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_MaterialSensor"); }
        }
        /// <summary>
        /// 45 - 5 bit
        /// </summary>
        public bool PLC_PortTypeChange
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_PortTypeChange"); }
        }
        /// <summary>
        /// 45 - 10 bit
        /// </summary>
        public bool PLC_FireShutterOn
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_FireShutterOn"); }
        }

        /// <summary>
        /// 45 - 8 bit
        /// </summary>
        public bool PLC_AlarmClear
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_AlarmClear"); }
        }

        /// <summary>
        /// 45 - 12 bit
        /// </summary>
        public bool PLC_DataForceClear
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_DataForceClear"); }
        }
        /// <summary>
        /// 45 - 14 bit
        /// </summary>
        public bool PLC_PortSizeShort
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_PortSizeShort"); }
        }



        //미사용 주석
        ///// <summary>
        ///// 45 - 15 bit
        ///// BCR Read Fail에 의한 Oprator 실물 확인시 자재 정보와 실물정보가 상이하여
        ///// 자재 정보의 업데이트가 필요한경우 해당 비트를 켜준다.
        ///// </summary>
        //public bool PLC_BCRReadValidNGRequest
        //{
        //    get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_BCRReadValidNGRequest"); }
        //}
        #endregion

        /// <summary>
        /// 46 기구포지션
        /// </summary>
        public short PLC_CVPosition
        {
            get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_CVPosition"); }
        }

        /// <summary>
        /// "LOW BYTE (우측 표의 BIT 위치 번호 참고)
        ///  VD MG구간 사용영역, 분채 잔량 사용영역
        ///  16BIT 중 6BIT 사용하여 1/2/3/4/5/6 번 위치의 값을 ON/OFF한다
        ///  
        ///  HIGH BYTE
        ///  (우측 표의 BIT 위치 번호 참고)
        ///  16BIT 중 6BIT 사용하여 1/2/3/4/5/6 번 위치에 잔량센서 감지시  ON/OFF 한다
        ///  감지시: ON(1)
        ///  미감지시: OFF(0)_
        /// </summary>
        //미사용 주석
        //public short PLC_MGSensorValue
        //{
        //    get { return GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_MGSensorValue"); }
        //}
        public bool PLC_MGSensorPos1
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_MGSensorPos1"); }
        }
        public bool PLC_MGSensorPos2
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_MGSensorPos2"); }
        }
        public bool PLC_MGSensorPos3
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_MGSensorPos3"); }
        }
        public bool PLC_MGSensorPos4
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_MGSensorPos4"); }
        }
        public bool PLC_MGSensorPos5
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_MGSensorPos5"); }
        }
        public bool PLC_MGSensorPos6
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_MGSensorPos6"); }
        }

        public bool PLC_MGRemainPos1
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_MGRemainPos1"); }
        }
        public bool PLC_MGRemainPos2
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_MGRemainPos2"); }
        }
        public bool PLC_MGRemainPos3
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_MGRemainPos3"); }
        }
        public bool PLC_MGRemainPos4
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_MGRemainPos4"); }
        }
        public bool PLC_MGRemainPos5
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_MGRemainPos5"); }
        }
        public bool PLC_MGRemainPos6
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_MGRemainPos6"); }
        }

        /// <summary>
        /// 아래 value값은 SKOH2
        /// 1: 입고모드
        /// 2. 출고모드
        /// 3. 양방향모드
        /// 아래 value값은 SKOY
        /// 2. 입고모드
        /// 4. 출고모드
        /// 6. 양방향모드
        /// </summary>
        public ePortInOutType PLC_PortType
        {
            get
            {
                {
                    short ReadSValue = GData.protocolManager.ReadShort(ModuleName, PLCtoPC, "PLC_PortType");
                    //ReadSValue = (byte)(ReadSValue >> 8); //상위 8bit 만가져온다 //사양 원복
                    return (ePortInOutType)ReadSValue;
                }
            }
        }
        public bool PLC_AGVLoadReq
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_AGVLoadReq"); }
        }
        public bool PLC_AGVUnloadReq
        {
            get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PLC_AGVUnloadReq"); }
        }
        #endregion

        //231025 HHJ Exist Update 분리
        #region WordData Changed Event
        private void OnUnitDataChangedAction(eDataChangeProperty changeType, eDataChangeUnitType unitType, string unitName, object changeData)
        {
            try
            {
                //여기서는 IO만 처리
                if (!(changeType.Equals(eDataChangeProperty.eIO_PLCtoPC) || changeType.Equals(eDataChangeProperty.eIO_PCtoPLC)))
                    return;

                //여기서는 Port만 처리
                if (!unitType.Equals(eDataChangeUnitType.ePort))
                    return;

                //현재 유닛과 같아야함
                if (!ControlName.Equals(unitName))
                    return;

                ConcurrentDictionary<string, PLCDataItem> changeItems = null;
                if (changeType.Equals(eDataChangeProperty.eIO_PLCtoPC))
                {
                    changeItems = PLCtoPC;
                }
                else if (changeType.Equals(eDataChangeProperty.eIO_PCtoPLC))
                {
                    changeItems = PCtoPLC;
                }
                else
                    return;

                if (changeItems is null)
                    return;

                KeyValuePair<string, PLCDataItem> firstitem = changeItems.Where(r => !r.Value.ItemName.Contains("BatchRead")).OrderBy(s => s.Value.AddressOffset).FirstOrDefault();
                int iStartAddress = firstitem.Value.AddressOffset;

                foreach (PLCDataItem item in changeItems.Values.OrderBy(o => o.AddressOffset).ThenBy(o => o.BitOffset))
                {
                    if (item.ItemName.Contains("BatchRead"))
                        continue;

                    int iChangeDataAddress = item.AddressOffset - iStartAddress;
                    int iChangeDataSize = item.Size;
                    byte[] ItemData = new byte[iChangeDataSize * 2];

                    Array.Copy((byte[])changeData, iChangeDataAddress * 2, ItemData, 0, iChangeDataSize * 2);

                    object readdata = ProtocolHelper.ParseIOData(item, ItemData);


                    switch (item.ItemName)
                    {
                        case "PLC_CarrierSensor":
                            DefaultSlot.SetCarrierExist((int)readdata != 0);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "Conveyor IO Update Fail" + ex.ToString());
            }
        }
        #endregion
        #endregion

        //#region PLCtoPC
        //protected bool PLC_PalletLoadReq
        //{
        //    get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PalletLoadReq"); }
        //}
        //protected bool PLC_PalletUnloadReq
        //{
        //    get { return GData.protocolManager.ReadBit(ModuleName, PLCtoPC, "PalletUnloadReq"); }
        //}
        //#endregion

        //#region PCtoPLC
        //protected bool PC_PalletLoadRes
        //{
        //    get { return GData.protocolManager.ReadBit(ModuleName, PCtoPLC, "PalletLoadRes"); }
        //    set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PalletLoadRes", value); }
        //}
        //protected bool PC_PalletUnloadRes
        //{
        //    get { return GData.protocolManager.ReadBit(ModuleName, PCtoPLC, "PalletUnloadRes"); }
        //    set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PalletUnloadRes", value); }
        //}
        //protected string PC_CarrierID
        //{
        //    get { return (string)GData.protocolManager.Read(ModuleName, PCtoPLC, "PalletID"); }
        //    set { GData.protocolManager.Write(ModuleName, PCtoPLC, "PalletID", value); }
        //}
        //#endregion
        //#endregion

        public CarrierItem ReadTrackingData()
        {
            CarrierItem cItem = new CarrierItem();
            cItem.CarrierID = PC_CarrierID;
            cItem.Destination = ConvertDestination(PC_DestCPUNum, PC_DestTrackNum);
            cItem.TrayType = PC_TrayType;
            cItem.TrayStackCount = PC_TrayStackCount.ToString();
            cItem.Polarity = PC_Polarity;
            cItem.ProductEmpty = PC_ProductEmpty;
            cItem.WinderDirection = PC_WinderDirection;
            cItem.ProductQuantity = PC_ProductQuantiry;
            cItem.FinalLoc = PC_CellPackingLine.ToString();
            cItem.InnerTrayType = PC_InnerTrayType;
            cItem.PalletSize = PC_PalletSize;
            cItem.CarrierSize = CarrierItem.ConvertCarrierSize(cItem.PalletSize);

            cItem.UncoatedPart = PC_UnCoatedPart;
            cItem.CoreType = PC_CoreType;
            cItem.ProductEnd = PC_ProductEnd;
            cItem.ValidationNG = PC_ValidationNG.ToString();
            return cItem;
        }

        /// <summary>
        /// 해당 캐리어에 현재 포트 데이터를 업데이트 한다.
        /// </summary>
        /// <param name="cItem"></param>
        /// <returns></returns>
        public bool UpdateTrackingData(CarrierItem cItem)
        {
            try
            {
                //cItem.CarrierID = PC_CarrierID; //캐리어 아이디는 보존
                cItem.Destination = ConvertDestination(PC_DestCPUNum, PC_DestTrackNum);
                cItem.TrayType = PC_TrayType;
                cItem.TrayStackCount = PC_TrayStackCount.ToString();
                cItem.Polarity = PC_Polarity;
                cItem.ProductEmpty = PC_ProductEmpty;
                cItem.WinderDirection = PC_WinderDirection;
                cItem.ProductQuantity = PC_ProductQuantiry;
                cItem.FinalLoc = PC_CellPackingLine.ToString();
                cItem.InnerTrayType = PC_InnerTrayType;
                cItem.PalletSize = PC_PalletSize;
                cItem.CarrierSize = CarrierItem.ConvertCarrierSize(cItem.PalletSize);

                cItem.UncoatedPart = PC_UnCoatedPart;
                cItem.CoreType = PC_CoreType;
                cItem.ProductEnd = PC_ProductEnd;
                cItem.ValidationNG = PC_ValidationNG.ToString();
                return true; ;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool WriteTrackingData(CarrierItem cItem)
        {
            try
            {
                if (SimulMode)
                {
                    return true;
                }
                if (cItem == null)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Port Put Data Write Failed. Carrier is Null!  - PortName :{0}", ModuleName);
                    return false;
                }
                PC_CarrierID = cItem.CarrierID;

                if (int.TryParse(cItem.Destination, out int dest))
                {
                    PC_DestCPUNum = (short)(dest / 1000);
                    PC_DestTrackNum = (short)(dest % 1000);
                }

                PC_TrayType = cItem.TrayType;

                if (short.TryParse(cItem.TrayStackCount, out short TSC))
                {
                    PC_TrayStackCount = TSC;
                }
                PC_Polarity = cItem.Polarity;
                PC_ProductEmpty = cItem.ProductEmpty;
                PC_WinderDirection = cItem.WinderDirection;

                PC_ProductQuantiry = (short)cItem.ProductQuantity;

                if (short.TryParse(cItem.FinalLoc, out short Final))
                {
                    PC_CellPackingLine = Final;
                }
                PC_InnerTrayType = cItem.InnerTrayType;
                PC_PalletSize = cItem.PalletSize;

                PC_UnCoatedPart = cItem.UncoatedPart;
                PC_CoreType = cItem.CoreType;
                PC_ProductEnd = cItem.ProductEnd;

                if (short.TryParse(cItem.ValidationNG, out short valid))
                {
                    PC_ValidationNG = valid;
                }
                return true;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }
        }

        public string GetPortTagName
        {
            get
            {
                return string.Format("{0:D2}{1:D3}{2:D2}", Position_Bank, Position_Bay, Position_Level);
            }
        }

        public void SleepRandomTime(int MaxSleepMilisec)
        {
            Random r = new Random();
            int sleepTime = r.Next(10, MaxSleepMilisec);
            Thread.Sleep(sleepTime);
        }

        /// <summary>
        /// Wait In 했으나 상위 MCS에서 Job 안내려 줄경우 스케쥴러에서 강제로 만든다.
        /// </summary>
        /// <returns></returns>
        public virtual bool CheckNeedPortJobCreate(int TimeOut)
        {
            if (TimeOut == 0) //무한 대기
            {
                return false;
            }
            if (PortInOutType != ePortInOutType.INPUT)
            {
                return false;
            }

            bool bTimeOut = IsTimeout_SW(SW_CarrierWaitInTime, TimeOut);
            return bTimeOut;
        }
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

        //public bool ResetCarrierData(bool DbUpdate = true)
        //{
        //    try
        //    {
        //        DefaultSlot.DeleteSlotCarrier(DbUpdate);
        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
        //        return false;
        //    }
        //}

        public bool RemoveSCSCarrierData()
        {
            if (DefaultSlot.InSlotCarrier != null)
            {
                string RemoveCarrierID = DefaultSlot.InSlotCarrier.CarrierID;
                DefaultSlot.DeleteSlotCarrier(true); //true 인자값 줘서 DB 삭제처리
                ProcessCarrierRemove(RemoveCarrierID);
            }
            return true;
        }


        public bool CheckCarrierExist()
        {
            //if (SimulMode)//LKJ 임시 주석
            //{
            //    return DefaultSlot.MaterialExist;
            //}
            //else
            //{
            return IsCarrierExist();
            //}
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
        public int iBank
        {
            get { return Position_Bank; }
        }
        public int iBay
        {
            get { return Position_Bay; }
        }
        public int iLevel
        {
            get { return Position_Level; }
        }
        public short iWorkPlaceNumber
        {
            get
            {
                return (short)PortTableID;
                //return (short)(TrackGroup * 1000 + TrackNum);
            }
        }

        //230306
        public ePalletSize PalletSize
        {
            get { return GetPalletSize(); }
        }

        public string iGroup
        {
            get { return "PORT"; }
        }

        public string iLocName { get { return ModuleName; } }
        public string iZoneName { get { return ParentModule.ModuleName; } }  //[230503 CIM 검수] 포트의 Zone Name 은 Line 명을 따라감
        public int CalcDistance(ICarrierStoreAble a, ICarrierStoreAble b)
        {
            int BankDistance = a.iBank == b.iBank ? 0 : 1;
            int BayDistance = a.iBay - b.iBay;
            int LevelDistance = a.iLevel - b.iLevel;

            BayDistance = (BayDistance < 0 ? -BayDistance : BayDistance);
            LevelDistance = (LevelDistance < 0 ? -LevelDistance : LevelDistance);
            return BankDistance + BayDistance + LevelDistance;
        }
        public string GetTagName()
        {
            return ModuleName;
        }

        public override bool CheckGetAble(string CarrierID)
        {
            //220803 조숭진 cvuse 추가
            bool bCarrierIDMatch = GetCarrierID() == CarrierID; //230309 CheckGetAble CarrierID 매칭 추가.
            bool bCarrierSizeExist = GetCarrierSize() != eCarrierSize.Unknown; //240212 RGJ 화물 사이즈가 없으면 Get 불가 
            return RobotAccessAble && CarrierExist && (NextCVCommand == eCVCommand.WaitCraneGet) && CVAvailable && bCarrierIDMatch && bCarrierSizeExist;
        }
        public override bool CheckPutAble()
        {
            //220803 조숭진 cvuse 추가
            return RobotAccessAble && !CarrierExist && (NextCVCommand == eCVCommand.WaitCranePut) && CVAvailable;
        }

        public bool CheckPLCPortTypeChangeRequest()
        {
            if (SimulMode)
            {
                return false;
            }
            return PLC_PortTypeChange && !PLC_CarrierSensor;
        }
        /// <summary>
        /// 해당 사이즈 캐리어가 포트로 Put 가능한지 체크
        /// 반드시 사이즈가 매칭되어야 한다.
        /// Short Carrier 가 LongSize 포트로 들어갈수 없음
        /// </summary>
        /// <param name="CarrierSize"></param>
        /// <returns></returns>
        public bool CheckCarrierSizeAcceptable(eCarrierSize Size)
        {
            if (PortSize == ePortSize.Both)
            {
                return true;
            }
            bool PutAble = false;
            switch (Size)
            {
                case eCarrierSize.Unknown:
                    PutAble = false;
                    break;
                case eCarrierSize.Short:
                    if (PortSize == ePortSize.Short)
                    {
                        PutAble = true;
                    }
                    break;
                case eCarrierSize.Long:
                    if (PortSize == ePortSize.Long)
                    {
                        PutAble = true;
                    }
                    break;
            }
            return PutAble;
        }

        public void NotifyScheduled(bool Reserved, bool init = false)   //221012 조숭진 init 인자 추가...
        {
            //해당 포트로 잡 생성 되었을때 동작 검토필요
        }

        #endregion
        /// <summary>
        /// 포트 서비스 상태 리턴.
        /// </summary>
        /// <returns></returns>
        public virtual ePortTrasnferState GetCurrentPortTransferState()
        {
            //if (CheckModuleHeavyAlarmExist() || !CVAvailable)      //220803 조숭진 cvuse추가
            //{
            //    return ePortTrasnferState.OUT_OF_SERVICE;
            //}
            //else
            //{
            //    return ePortTrasnferState.IN_SERVICE;
            //}

            if (InServiceAble())
            {
                return ePortTrasnferState.IN_SERVICE;
            }
            else
            {
                return ePortTrasnferState.OUT_OF_SERVICE;
            }
        }
        public virtual eNotchingMode GetNotchingMode()
        {
            return eNotchingMode.DEFAULT;
        }

        /// <summary>
        /// InService 2
        /// OutOfService 1
        /// </summary>
        /// <returns></returns>
        public override int GetUnitServiceState()
        {
            if (!CheckModuleHeavyAlarmExist())
            {
                return 2;
            }
            else
            {
                return 1;
            }
        }
        public bool NeedPlayBackWrite()
        {
            return GlobalData.Current.UsePlayBackLog;
        }


        /// <summary>
        /// 포트 블럭 프로퍼티 추가.
        /// 디비에는 저장하지 않고 스케쥴러가 필요시 변경해서 사용함
        /// 원 크레인 모드 사용시  기존 CVUSE 를 변경해서는 안되므로 따로 추가해서 사용함. 
        /// </summary>
        protected bool _CVBLOCK;
        public bool CVBLOCK
        {
            get { return _CVBLOCK; }
            set
            {
                if (_CVBLOCK == value)
                {
                    return;
                }
                _CVBLOCK = value;
                CVUSE = !value; //임시
                if (_CVBLOCK) //포트가 블럭 되면 OutOfService
                {
                    //S6f11 PortOutOfService CEID 402
                    //GlobalData.Current.HSMS.SendS6F11(402, "PORT", this);
                    RequestOutserviceReport();
                }
                else
                {
                    if (CVUSE)//포트가 블럭이 풀려도  CVUSE 가 On 상태여야 InService 보고
                    {
                        //S6f11 PortInService CEID 401
                        //GlobalData.Current.HSMS.SendS6F11(401, "PORT", this);
                        RequestInserviceReport();
                    }
                }

                //220803 조숭진 추가
                //GlobalData.Current.HSMS.SendS6F11(310, "ZONENAME", ModuleName);

                //220902 HHJ SCS 개선     //- CV Able, Disable UI 반응 추가
                RaisePropertyChanged("CVBLOCK");
            }
        }

        bool bFirstCVUseSet = false; //처음 값 세팅할때는 프로퍼티 변수만 바뀌도록 임시 변수 추가.
        protected bool _CVUSE;
        public bool CVUSE
        {
            get { return _CVUSE; }
            set
            {
                if (!bFirstCVUseSet)
                {
                    _CVUSE = value;
                    bFirstCVUseSet = true;
                    return;
                }
                if (_CVUSE == value)
                {
                    return;
                }
                _CVUSE = value;
                if (_CVUSE)
                {
                    SetTrackPause(false);
                    PC_PortEnable = true;
                    //S6f11 PortInService CEID 401
                    //GlobalData.Current.HSMS.SendS6F11(401, "PORT", this);
                    RequestInserviceReport();

                    if (IsInPort && CVModuleType == eCVType.RobotIF)
                    {
                        //GlobalData.Current.HSMS.SendS6F11(404, "PORT", this); //반송 물류에서는 미사용
                    }
                    else if (!IsInPort && CVModuleType == eCVType.RobotIF)
                    {
                        //GlobalData.Current.HSMS.SendS6F11(405, "PORT", this); //반송 물류에서는 미사용
                    }
                }
                else
                {
                    SetTrackPause(true);
                    PC_PortEnable = false;
                    //GlobalData.Current.HSMS.SendS6F11(403, "PORT", this);

                    //S6f11 PortOutOfService CEID 402
                    //GlobalData.Current.HSMS.SendS6F11(402, "PORT", this);
                    RequestOutserviceReport();
                }

                //220803 조숭진 추가
                //GlobalData.Current.HSMS.SendS6F11(310, "ZONENAME", ModuleName);

                //220902 HHJ SCS 개선     //- CV Able, Disable UI 반응 추가
                RaisePropertyChanged("CVUSE");
            }
        }


        /// <summary>
        /// 22.10.24 RGJ 포트 가용가능 프로퍼티 하나로 합침
        /// </summary>
        public bool CVAvailable
        {
            get { return _CVUSE && !CVBLOCK && PLC_KeySwitch; } //240320 RGJ CVAvailable PLC 스위치 추가.
        }

        public virtual void SetKeyInCarrierItem(CarrierItem carrierItem)
        {
            return;
        }
        public virtual void SetCarrierGeneratorRequset(string CarrierID)
        {
            return;
        }
        public virtual void SetCarrierGeneratorRequset(string CarrierID, string LotID)
        {
            return;
        }

        public virtual eHandoffType GetHandOffType()
        {
            if (CVModuleType == eCVType.Manual)
            {
                return eHandoffType.MANUAL;
            }
            else
            {
                return eHandoffType.AUTO;
            }
        }

        /// <summary>
        /// 해당 포트가 ManualPort랑 물리적으로 연결되었는지 체크
        /// </summary>
        /// <returns></returns>
        public bool CheckConnectedWithManualPort()
        {
            if (CVModuleType == eCVType.Manual)
            {
                return true;
            }
            bool Connected = false;
            if (ParentModule is CVLineModule cvLine)
            {
                Connected = cvLine.IsContainManualPort();
            }
            return Connected;
        }
        public string GetConnectedRobotIFModuleName()
        {
            if (CVModuleType == eCVType.RobotIF)
            {
                return ModuleName;
            }
            if (ParentModule is CVLineModule cvLine)
            {
                return cvLine.GetInlineRobotIFPortName();
            }
            return "";
        }

        /// <summary>
        /// Inservice 는 모든 조건을 다 만족해야 보고해야함.
        /// </summary>
        public void RequestInserviceReport()
        {
            if (InServiceAble())
            {
                GlobalData.Current.HSMS.SendS6F11(401, "PORT", this); //PortInService  401
            }
        }

        /// <summary>
        /// OutService는 바로 보고한다.
        /// </summary>
        public void RequestOutserviceReport()
        {
            GlobalData.Current.HSMS.SendS6F11(402, "PORT", this); //PortOutService 402
        }

        public bool InServiceAble()
        {
            bool bAvailable = CVAvailable; //_CVUSE && !CVBLOCK && PLC_KeySwitch; }
            bool bNoAlarmCode = (PLC_ErrorCode == 0);
            return bAvailable && bNoAlarmCode;
        }

        //2024.08.13 lim, PortType 변경 가능한지 확인용 ( Manual Port만 할려다가, 동간물류도 막혀서 컨베이어 수로 확인)
        public bool CheckPortTypeChagneAble()
        {
            if (ParentModule is CVLineModule cvLine)
            {
                return cvLine.IsMultyCV();
            }
            return false;
        }
    }
}
