using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;            //220318 조숭진 SQLite.Interop.dll파일 bin폴더에 생성.
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Threading;     //230105 HHJ SCS 개선
using System.Xml;
using WCF_LBS.Commands;
using PLCProtocol;
using PLCProtocol.Base;
//220608 HHJ SCS 개선     //- MCProtocol, MXComponent 추가
using PLCProtocol.DataClass;
using BoxPrint.Alarm;
using BoxPrint.Config;
using BoxPrint.DataBase;
using BoxPrint.DataList;
using BoxPrint.DataList.MCS;
using BoxPrint.GUI.ClassArray;
//220421 HHJ SCS 개선     //- xml, db 별도 사용으로 변경
using BoxPrint.GUI.ETC;
using BoxPrint.GUI.ETC.LoadingPopup;
using BoxPrint.GUI.UIControls;
//220523 HHJ SCS 개선     //- ShelfSetterControl 신규 추가
using BoxPrint.GUI.UserControls;
using BoxPrint.Log;
using BoxPrint.Modules;
using BoxPrint.Modules.CVLine;
using BoxPrint.Modules.RM;
using BoxPrint.Modules.Shelf;
using BoxPrint.Modules.User;     //220405 HHJ SCS 개선     //- User Page 추가
using BoxPrint.OpenHSMS;
using BoxPrint.Scheduler;
using BoxPrint.SimulatorPLC;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading.Tasks;
using System.Net.NetworkInformation;
using BoxPrint.Config.PrintRecipe;
using BoxPrint.Modules.Print;
using System.Collections.ObjectModel;

namespace BoxPrint
{
    public class GlobalData
    {
        public object LockInstance = new object();
        public List<Thread> ThreadList = new List<Thread>();

        public LodingPopup gLodingPopup;//SuHwan_20221018 : [loadingpopup]

        public static readonly string DOUBLE_STORAGE_ALARM_CODE = "1189";
        public static readonly string SOURCE_EMPTY_ALARM_CODE = "1190";
        public static readonly string PORT_IF_ALARM_CODE = "1191";
        public void AddThread(Thread th)
        {
            lock (LockInstance)
            {
                ThreadList.Add(th);
            }
        }

        public bool GlobalSimulMode
        {
            get;
            private set;
        }

        public bool UsePlayBackLog
        {
            get
            {
                return ServerClientType == eServerClientType.Server;
            }
        }
        public bool ServerInstance
        {
            get
            {
                return ServerClientType == eServerClientType.Server;
            }
        }
        public bool ClientInstance
        {
            get
            {
                return ServerClientType == eServerClientType.Client;
            }
        }

        public eLineSite CurrnetLineSite
        {
            get;
            private set;
        }

        public bool UseIntegratedMap
        {
            get;
            private set;
        }
        
        public eSCSType SCSType
        {
            get;
            private set;
        }

        //SuHwan_20220930 : [ServerClient]
        public eServerClientType ServerClientType
        {
            get;
            private set;
        }

        public eLineSite LineSite
        {
            get;
            private set;
        }

        public bool UseServoSystem
        {
            get;
            private set;
        }
        //2021.04.06 lim, Tray Data Read Mode
        public bool UseBCR
        {
            get
            {
                //return SCSType == eSCSType.BoxLBS;
                return false;
            }
        }

        //230222 조숭진 eqp_info table을 위한 속성값 추가 s
        public string EqpName
        {
            get;
            private set;
        }

        public string EqpNumber
        {
            get;
            private set;
        }
        //230222 조숭진 eqp_info table을 위한 속성값 추가 e

        public static GlobalData Current { get; private set; }

        //public event EventHandler eventSomethingClicked;
        readonly object rfObject = new object();
        //public WCFLBS_Manager WCF_mgr
        //{
        //    get
        //    {
        //        return WCFLBS_Manager.GetManagerInstance();
        //    }
        //}

        private RecipeManager _Recipe_Manager;
        public RecipeManager Recipe_Manager 
        {
            get
            {
                return _Recipe_Manager;
            }
            
        }

        public AlarmManager _Alarm_Manager;
        public AlarmManager Alarm_Manager
        {
            get
            {
                return _Alarm_Manager;
            }
        }
        BaseScheduler _Scheduler;
        public BaseScheduler Scheduler
        {
            get
            {
                return _Scheduler;
            }
        }

        // 2020.11.06 SimulationAlarm발생용
        private bool _siumlationAlarm;
        public bool siumlationAlarm
        {
            get
            {
                return _siumlationAlarm;
            }
            set
            {
                _siumlationAlarm = value;
            }
        }

        //SuHwan_20220316 : GUI 테마 
        private GUIColorBase _guiColor;
        public GUIColorBase GuiColor
        {
            get
            {
                return _guiColor;
            }
        }

        private bool _MapViewStart;
        public bool MapViewStart
        {
            get
            {
                return _MapViewStart;
            }
            set
            {
                _MapViewStart = value;
            }
        }

        public int PalletSizeByLine
        {
            get;
            private set;
        }

        public event EventHandler<EventArgs> SendEvent = delegate { }; // Main 화면으로 보내지는 Event 등록
        public event EventHandler<EventArgs> SendTagChange = delegate { }; // TagChange대한 이벤트 등록

        public delegate void PrarChangeEvent();              // RM Parameter 이벤트처리를위한델리게이트정의
        public event PrarChangeEvent OnParaChangeEvent;      // RM Parameter 이벤트 정의

        public ParameterList SystemParameter = new ParameterList(); // option Parameter변수 

        public ParameterList gParameter = new ParameterList(); // option Parameter변수 

        public List<decimal> AxisListCount = new List<decimal>(); // AxisList 수량
        public List<decimal> RMListCount = new List<decimal>();  // RM Count수량 수량

        public string SimulationAlarmCode = string.Empty;

        public PMacDataList nParameterList { get; set; } // Parameter List

        private GridItemListItem griditemTable;
        public BoothBase MainBooth;
        public RMManager mRMManager { get; private set; }

        public CVLineManager PortManager { get; private set; }
        //public SAFETYManager SafetyManager { get; private set; }
        public CCLink.CCLinkManager CCLink_mgr;

        //public WPS_Manager WPS_mgr;

        //public PartsLifeManager PartsLife_mgr;


        private CarrierStorage _CarrierStore;
        public CarrierStorage CarrierStore
        {
            get
            {
                return _CarrierStore;
            }
        }

        public ShelfManager ShelfMgr { get; private set; }

        public ManualResetEvent MRE_CVLineCreateEvent = new ManualResetEvent(false);
        public ManualResetEvent MRE_GlobalDataCreatedEvent = new ManualResetEvent(false);
        public ManualResetEvent MRE_FirstPLCReadEvent = new ManualResetEvent(false);
        public ManualResetEvent MRE_MapViewChangeEvent = new ManualResetEvent(false);


        //220523 HHJ SCS 개선     //- ShelfSetterControl 신규 추가
        //private Dictionary<string, ModuleBase> ModuleStore = new Dictionary<string, ModuleBase>();
        private ConcurrentDictionary<string, ModuleBase> ModuleStore = new ConcurrentDictionary<string, ModuleBase>();

        //서보 관리자 추가.
        //public ServoManager Servo_mgr
        //{
        //    get;
        //    private set;
        //}

        #region  Global 파일 Path 정의
        //온양32-1, 온양33-1, 온양33-2, 온양33-3, 온양33T-3, 온양33T-4, 온양41-1, 온양44-1, 온양1, 온양2, 온양3, 온양P1, 온양C1, 온양C2
        //천안2, 천안3, 천안5,7, 천안6,8,10, 천안9
        //상부 포트 체크 2 H11, c1 H7
        public const string TestModel = "";//@"Model\온양c1\";  //21.06.09 lim, test용 
        public const int ShelfHeight = 35;

        public string FullPath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
        public string PortConfigPath = @"\Data\" + TestModel + @"Ports\PortLayout.xml";
        public string PortUI1F_ConfigPath = @"\Data\" + TestModel + @"Ports\PortUI_1F.xml";
        public string PortUI2F_ConfigPath = @"\Data\" + TestModel + @"Ports\PortUI_2F.xml";
        public string PortIOFilePath = @"\Data\" + TestModel + @"Ports\";
        public string ParameterPath = @"\Data\" + TestModel + @"Parameter_System.xml";
        public string IOFileName = @"\Data\" + TestModel + @"IOPoint.xml";

        public string AlarmListPath = @"\Data\AlarmList.xml";
        public string SAFETYFileName = @"\Data\SafetyIO.xml";
        public string GridDataItemPath = "\\Data\\GridDataItem.xml";
        public string Led_Black = "\\Image\\Led_Black.png";
        public string Led_Blue = "\\Image\\Led_Blue.png";
        public string Led_Green = "\\Image\\Led_Green.png";
        public string Led_Red = "\\Image\\Led_Red.png";
        public string Led_White = "\\Image\\Led_White.png";
        public string Led_Yellow = "\\Image\\Led_Yellow.png";

        public string PLCDataItemPath = @"\Data\PLC\PLCDataItems.xml";  //220315 HHJ SCS 개발     //- MXComponentNet, SharedMemory 개선
        public string PLCDataItemPath_IntegratedMap = @"\Data\PLC\PLCDataItems_Integrated.xml";
        public string AuthorityFilePath = @"\Data\AuthorityData.xml";   //230118 YSW XML파일 주소

        public string RecipeFilePath = @"\Data\RecipeList.xml";
        public string ScenarioFilePath = @"\Data\ScenarioList.xml";   

        public string ClientConfigFilePath = @"D:\01.SCS_UI\Config";
        public string ClientDataFilePath = @"D:\01.SCS_UI\Data";
        public string ClientImageFilePath = @"D:\01.SCS_UI\Image";

        public string ServerConfigFilePath = "E:\\CIM_SCS\\01.SCS_{0}\\03.Config";
        public string ServerDataFilePath = "E:\\CIM_SCS\\01.SCS_{0}\\04.Data";
        public string ServerImageFilePath = "E:\\CIM_SCS\\01.SCS_{0}\\05.Image";
        #endregion

        public List<CraneCommand> CraneActiveJobList = new List<CraneCommand>();

        //220318 HHJ SCS 개발     //- ActiveJob 연동 RouteLine 추가
        public SafeObservableCollection<CraneCommand> CraneActiveJobList_RM1 = new SafeObservableCollection<CraneCommand>();
        public SafeObservableCollection<CraneCommand> CraneActiveJobList_RM2 = new SafeObservableCollection<CraneCommand>();

        public List<PortCommand> PortActiveJobList = new List<PortCommand>();         // 2020.11.18 Port Command Dispaly
        public List<TowerLampCommand> TowerLampActiveJobList = new List<TowerLampCommand>(); // 2020.11.18 TowerLamp Command Dispaly

        //20220728 조숭진 config 방식 변경 s
        public MainConfigSection MainSectionCF;
        //public RMConfigSection RMSection;
        //FireConfigSection FireSection;
        ////220608 HHJ SCS 개선		//- MCProtocol, MXComponent 추가
        //private PLCSection plcSection { get; set; }
        public MainSection MainSection = new MainSection();
        public RMSection RMSection = new RMSection();
        public PLCSection PLCSection = new PLCSection();
        public DBSection DBSection = new DBSection();
        //20220728 조숭진 config 방식 변경 e

        //public DBConfigSection DBSection;      //230314

        //220608 HHJ SCS 개선		//- MCProtocol, MXComponent 추가
        //220315 HHJ SCS 개발     //- MXComponentNet, SharedMemory 개선
        //public SharedMemoryClass SharedMemory { get; set; }

        //220628 HHJ SCS 개선     //- PLCDataItems 개선
        ////220315 HHJ SCS 개발     //- MXComponentNet, SharedMemory 개선 //Test용
        //public int iCVNum = 0;

        public OracleDBManager DBManager;

        //220719 조숭진 eqpid 고정
        //public string EQPID { get; }        //220318 조숭진 eqpid추가
        public string EQPID = string.Empty;

        //221229 YSW Map View안에 각 SCS의 Tooltip에 IP 항목 추가 : 현재 IP
        private string _CurrentIP = string.Empty;
        public string CurrentIP
        {
            get
            {
                return _CurrentIP;
            }
            set
            {
                if (!_CurrentIP.Equals(value))
                {
                    _CurrentIP = value;
                }
            }
        }

        private string _ClientPCName = string.Empty;
        public string ClientPCName
        {
            get
            {
                return _ClientPCName;
            }
            set
            {
                if (!_ClientPCName.Equals(value))
                {
                    _ClientPCName = value;
                }
            }
        }

        public string CurrentUserID { get; set; }


        //230103 YSW LoginUserAuthority
        private List<string> _LoginUserAuthority = new List<string>();
        public List<string> LoginUserAuthority
        {
            get
            {
                return _LoginUserAuthority;
            }
            set
            {
                if (!_LoginUserAuthority.Equals(value))
                {
                    _LoginUserAuthority = value;
                }
            }
        }

        //221102 YSW DB EQPInfoList
        //240108 RGJ EQPList 단일화함
        private object EQPListLock = new object();
        private ObservableList<EQPInfo> _EQPList = new ObservableList<EQPInfo>();
        public ObservableList<EQPInfo> EQPList
        {
            get
            {
                return _EQPList;
            }
        }
        public void EQPListAddOrUpdate(EQPInfo Einfo)
        {
            var EqpInfoUpdate = _EQPList.FirstOrDefault(e => e.EQPID == Einfo.EQPID);
            if (EqpInfoUpdate != null) //이미 목록에 있으면 업데이트
            {
                EqpInfoUpdate.EQPName = Einfo.EQPName;
                EqpInfoUpdate.EQPID = Einfo.EQPID;
                EqpInfoUpdate.SCSIP = Einfo.SCSIP;
                EqpInfoUpdate.EQPNumber = Einfo.EQPNumber;
                EqpInfoUpdate.MCS_State = Einfo.MCS_State;
                EqpInfoUpdate.SCS_State = Einfo.SCS_State;
                EqpInfoUpdate.PLC_State = Einfo.PLC_State;
                EqpInfoUpdate.SYSTEM_State = Einfo.SYSTEM_State;
                EqpInfoUpdate.DBFirstIP = Einfo.DBFirstIP;
                EqpInfoUpdate.DBFirstPort = Einfo.DBFirstPort;
                EqpInfoUpdate.DBFirstServiceName = Einfo.DBFirstServiceName;
                EqpInfoUpdate.DBSecondIP = Einfo.DBSecondIP;
                EqpInfoUpdate.DBSecondPort = Einfo.DBSecondPort;
                EqpInfoUpdate.DBSecondServiceName = Einfo.DBSecondServiceName;
                EqpInfoUpdate.DbAccount = Einfo.DbAccount;
                EqpInfoUpdate.DbPassword = Einfo.DbPassword;
            }
            else //없으면 추가함.
            {
                GlobalData.Current.EQPList.Add(Einfo);
            }
        }

        //YSW_20221108 : [SelectedKiosk]
        private string _SelectedKiosk = null;
        public string SelectedKiosk
        {
            get
            {
                return _SelectedKiosk;
            }
            set
            {
                if (_SelectedKiosk != value)
                {
                    _SelectedKiosk = value;
                }
            }
        }

        public string FIRERACKTYPE { get; }     //220512 조숭진 화재감지추가

        // 디버그용 변수
        public int TransferCounter
        {
            get;
            set;
        }
        public int PushCounter
        {
            get;
            set;
        }
        public int HandOverCounter
        {
            get;
            set;
        }
        public int WithDrawCounter
        {
            get;
            set;
        }

        public int FrontBankNum
        {
            get;
            private set;
        }

        public int RearBankNum
        {
            get;
            private set;
        }

        public bool WritePLCRawLog
        {
            get;
            set;
        }
        public bool DebugUseBackupPLC
        {
            get;
            set;
        }

        #region Event 관련

        /// <summary>
        /// RM 파라메터에 변경에 대한 이벤트
        /// </summary>
        public void RaiseParaChangeEvent()
        {
            //이벤트 가입자가 있는지 확인
            OnParaChangeEvent?.Invoke(); //이벤트 발생
        }

        private bool _GlobalInitComp;
        public bool GlobalInitComp
        {
            get
            {
                return _GlobalInitComp;
            }
            private set
            {
                _GlobalInitComp = value;

            }
        }

        private string _SendMessageEvent;
        public string SendMessageEvent
        {
            get
            {
                return _SendMessageEvent;
            }
            set
            {
                _SendMessageEvent = value;
                FireEvent(value);
            }
        }

        public void FireEvent(string JInfo)
        {
            try
            {
                SendEvent(JInfo, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Fatal, ex.ToString());
            }
        }


        private string _SendTagEvent;
        public string SendTagEvent
        {
            get
            {
                return _SendTagEvent;
            }
            set
            {
                _SendTagEvent = value;
                TagFireEvent(value);
            }
        }


        public void TagFireEvent(string JInfo)
        {
            try
            {
                SendTagChange(JInfo, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Fatal, ex.ToString());
            }
        }

        #endregion

        private McsJobManager _McdList = new McsJobManager();
        public McsJobManager McdList
        {
            get
            {
                return _McdList;
            }
            set
            {
                //SuHwan_20221006 : [ServerClient]
                setMcdList(value);
                foreach (var JItem in _McdList)
                {
                    JItem.SetPlayBackTrace();//복구 정리가 끝난 작업에 플레이백 추적 플래그 설정.
                    JItem.RaisePropertyChangeEvent("CarrierLoc"); //240617 RGJ 클라이언트 CarrierLoc 이벤트 수동으로 발생.
                }
                //_McdList = value;
            }
        }

        //private UILogList _UILogList;
        //public UILogList uiLogList
        //{
        //    get
        //    {
        //        return _UILogList;
        //    }
        //    protected set
        //    {
        //        _UILogList = value;
        //    }
        //}

        //220405 HHJ SCS 개선     //- User Page 추가
        private UserManager _UserMng; //GlobalData 생성시 생성.
        public UserManager UserMng
        {
            get
            {
                return _UserMng;
            }
            protected set
            {
                _UserMng = value;
            }
        }
        private AuthorityManager _AuthorityMng; //GlobalData 생성시 생성.
        public AuthorityManager AuthorityMng
        {
            get
            {
                return _AuthorityMng;
            }
            protected set
            {
                _AuthorityMng = value;
            }
        }

        public static GlobalData CreateGlobalDataContext()
        {
            if (Current == null)
            {
                new GlobalData();
            }
            return GlobalData.Current;
        }

        //bool bClient = false;   //220929 HHJ SCS 개선     //- HSMS Client 사용을 위한 구조 변경    //임시변수

        private HSMSManager _HSMS;
        public HSMSManager HSMS
        {
            get { return _HSMS; }
        }

        private PrinterManager _PrinterMng;
        public PrinterManager PrinterMng
        {
            get { return _PrinterMng; }
        }

        public ScenarioList PrintScenarioList = null;


        public static string SCSProgramVersion
        {
            get
            {
                return "1.02.9";
            }
        }

        //SuHwan_20221025 : [ServerClient]
        public ConcurrentDictionary<string, PLCDataInfo> dicPLCDataInfo_PCtoPLC = new ConcurrentDictionary<string, PLCDataInfo>();
        public ConcurrentDictionary<string, PLCDataInfo> dicPLCDataInfo_PLCtoPC = new ConcurrentDictionary<string, PLCDataInfo>();

        public bool LayoutLoadComp
        {
            get;
            set;
        }
        private bool _bFirstMCSResumeReq = false;
        public bool bFirstMCSResumeReq
        {
            get
            {
                return _bFirstMCSResumeReq;
            }
            set
            {
                _bFirstMCSResumeReq = value;
            }
        }
        //public PLCManager plcManager;
        public ProtocolManager protocolManager { get; set; }

        public event UserListRefreshed userlistrefresh;
        public delegate void UserListRefreshed();

        public void UserListRefresh()
        {
            try
            {
                DispatcherService.Invoke(() =>
                {
                    userlistrefresh?.Invoke();
                });
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        public event ConfigDataRefreshed configdatarefresh;
        public delegate void ConfigDataRefreshed();

        public void ConfigDataRefresh()
        {
            try
            {
                DispatcherService.Invoke(() =>
                {
                    configdatarefresh?.Invoke();
                });
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        public event ScenarioListRefreshed scenarioListrefresh;
        public delegate void ScenarioListRefreshed();

        public void ScenarioListRefresh()
        {
            try
            {
                DispatcherService.Invoke(() =>
                {
                    scenarioListrefresh?.Invoke();
                });
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        public event RecipeListRefreshed recipeListrefresh;
        public delegate void RecipeListRefreshed();

        public void RecipeListRefresh()
        {
            try
            {
                DispatcherService.Invoke(() =>
                {
                    recipeListrefresh?.Invoke();
                });
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        public event RecipeDataRefreshed recipedatarefresh;
        public delegate void RecipeDataRefreshed();

        public void RecipeDataRefresh()
        {
            try
            {
                DispatcherService.Invoke(() =>
                {
                    recipedatarefresh?.Invoke();
                });
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        public delegate void PrintScenarioStateChanged(ePrintScenarioState State);
        public event PrintScenarioStateChanged printScenarioStateChange;

        public void PrintScenarioStateChange(ePrintScenarioState State)
        {
            try
            {
                //PrintScenarioList.ScenarioFilePath = State;
                DispatcherService.Invoke(() =>
                {
                    printScenarioStateChange?.Invoke(State);
                });
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        public event LogListRefreshed loglistrefresh;
        public delegate void LogListRefreshed();

        public void LogListRefresh()
        {
            try
            {
                DispatcherService.Invoke(() =>
                {
                    loglistrefresh?.Invoke();
                });
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        public event CurrentAccountLogout currentaccountlogout;
        public delegate void CurrentAccountLogout();

        public delegate void AlarmManagerViewRefreshed();
        public event AlarmManagerViewRefreshed OnAlarmManagerViewRefreshed;

        public void AlarmManagerViewRefresh()
        {
            try
            {
                DispatcherService.Invoke(() =>
                {
                    OnAlarmManagerViewRefreshed?.Invoke();
                });
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        public delegate void TerminalMessageRefreshed();
        public event TerminalMessageRefreshed OnTerminalMessageRefreshed;

        public void TerminalMessageRefreshedOccur()
        {
            try
            {
                DispatcherService.Invoke(() =>
                {
                    OnTerminalMessageRefreshed?.Invoke();
                });
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        //221226 HHJ SCS 개선
        public delegate void TerminalMessageChanged(DateTime dt, eHostMessageDirection direction, string msg, bool init);
        public event TerminalMessageChanged OnTerminalMessageChanged;
        public void TerminalMessageChangedOccur(DateTime dt, eHostMessageDirection direction, string msg, bool init = false)
        {
            try
            {
                DispatcherService.Invoke(() =>
                {
                    OnTerminalMessageChanged?.Invoke(dt, direction, msg, init);
                });
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        //230103 HHJ SCS 개선
        public delegate void ViewModelWindowOpen(eOpenWindowName windowname, ControlBase control, bool IsPlayBack);
        public event ViewModelWindowOpen OnViewModelWindowOpen;
        public void ViewModelWindowOpenRequest(eOpenWindowName windowname, ControlBase control, bool IsPlayBack)
        {
            try
            {
                DispatcherService.Invoke(() =>
                {
                    OnViewModelWindowOpen?.Invoke(windowname, control, IsPlayBack);
                });
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        //230103 HHJ SCS 개선
        //public delegate void UnitIODataReq(eDataChangeUnitType unitType, string unitName);
        public delegate void UnitIODataReq(eDataChangeUnitType unitType, string unitName, bool isPlayback);
        public event UnitIODataReq OnUnitIODataReq;
        //230103 HHJ SCS 개선
        //public void UnitIODataReqRequest(eDataChangeUnitType unitType, string unitName)
        public void UnitIODataReqRequest(eDataChangeUnitType unitType, string unitName, bool isPlayback)
        {
            try
            {
                DispatcherService.Invoke(() =>
                {
                    //230103 HHJ SCS 개선
                    //OnUnitIODataReq?.Invoke(unitType, unitName);
                    OnUnitIODataReq?.Invoke(unitType, unitName, isPlayback);
                });
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        public void ConsoleWindow()
        {
            if (GlobalData.Current.ServerClientType == eServerClientType.Client)
            {
                IntPtr console = GetConsoleWindow();
                ShowWindow(console, 0);
            }
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private GlobalData()
        {
            try
            {
                FileInfo File;
                bool dbopenstate = false;
                GlobalData.Current = this;
                string msg = string.Empty; //Message Translation용

                CurrnetLineSite = eLineSite.TOP_POC;

                //SuHwan_20221006 : [ServerClient]
                bool bchange = appSettingLoad(); //여기서 EQPID 를 확정한다.


                if (ServerClientType == eServerClientType.Client)
                {
                    string[] args = Environment.GetCommandLineArgs(); //Starter 로부터 EQPID 받았다. 덮어씌운다.
                    if (args.Count() >= 4)
                    {
                        this.EQPID = args[1];
                    }
                }

                LogManager.InitLog4Net(false); // Log 모듈 생성

                //uiLogList = new UILogList();

                LogManager.WriteConsoleLog(eLogLevel.Info, $"Initializing : {this.EQPID} System GlobalData");


                _guiColor = new GUIColorBase(); //SuHwan_20220316 : GUI 테마

                GetCurrentIP(); //YSW_221230 현재IP 가져오기

                
                GetDBConnectionInfo();

                
                if (ServerClientType == eServerClientType.Client)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Creating : Client - DBManager");
                    DBManager = new OracleDBManager_Client(out dbopenstate);
                }
                else
                {
                    DBManager = new OracleDBManager(out dbopenstate);
                }


                //20220728 조숭진 config 방식 변경 s
                if (dbopenstate == false)
                {
                    msg = TranslationByMarkupExtension.TranslationManager.Instance.Translate("DB Open Failed").ToString();
                    MessageBoxPopupView mbox = new MessageBoxPopupView(msg, MessageBoxImage.Error, false);
                    CustomMessageBoxResult mResult = mbox.ShowResult();
                    Environment.Exit(20);
                }


                bool bdbconfig = false;

                DataSet dataSet = DBManager.DbGetProcedureGlobalConfigInfo('1');

                if (dataSet.Tables[dataSet.Tables.Count - 1].Rows.Count != 0)
                    bdbconfig = true;
                else
                    bdbconfig = false;

                //bChange   Config 수정 여부
                //bDBconfig DB 데이터 유무
                //if    > Config 값이 수정 되지 않고 DB에 데이터가 있다면 DB에 있는 데이터 읽어옴
                //else  > Config 값이 수정 되었거나 DB 데이터가 없으면 Config 데이터로 DB 업데이트
                if (!bchange && bdbconfig)
                {
                    //int plccount = DBManager.DBGetInfoCount("Plcs");
                    //DBManager.DbGetConfigInfo(PLCSection);      //220917 조숭진 추가
                    //for (int i = 0; i < plccount; i++)
                    //{
                    //    DBManager.DbGetConfigInfo(PLCSection[i], i);
                    //}
                    DBManager.DbGetGlobalConfigInfo(MainSection);
                    AppSettingRenewal(MainSection);
                    DBManager.DbGetGlobalConfigInfo(RMSection);
                    AppSettingRenewal(RMSection);
                    //DBManager.DbGetGlobalConfigInfo(PLCSection);  //230314 중복주석처리

                    //int plccount = DBManager.DBGetInfoCount("Plcs");
                    string strcount = string.Empty;
                    int plccount = 0;
                    DBManager.DbGetGlobalConfigValue("Plcs", "Count", out strcount);
                    if (!string.IsNullOrEmpty(strcount))
                        plccount = Convert.ToInt32(strcount);

                    DBManager.DbGetGlobalConfigInfo(PLCSection);      //220917 조숭진 추가
                    AppSettingRenewal(PLCSection);

                    bool bInit = true;
                    for (int i = 0; i < plccount; i++)
                    {
                        DBManager.DbGetGlobalConfigInfo(PLCSection[i], i);
                        AppSettingRenewal(PLCSection[i], bInit, i);
                        if (bInit)
                            bInit = false;
                    }
                }
                else
                {
                    if (bchange)
                        DBManager.DBConfigTableDataDelete(Current.EQPID);

                    DBConfigDataInitialize();
                }


                LogManager.SetLogStoragePeriod(MainSection.LogStoragePeriod);       //231205 HHJ SCS 개선     //- Log 저장기한에 따른 삭제기능 추가

                CurrnetLineSite = MainSection.LineSite;

                //SuHwan_20221025 : [ServerClient]

                protocolManager = new ProtocolManager(PLCSection);

                protocolManager.Connect();

                GlobalSimulMode = MainSection.GlobalSimulMode;


                
                _HSMS = new HSMSManager();
                _HSMS.InitHSMSDriver(); //초기화 따로 빼둠

                _PrinterMng = new PrinterManager();
                _PrinterMng.initPrintDriver();
                
                SCSType = MainSection.SCSType;

                UseServoSystem = MainSection.UseServoSystem;

                //EQPID = MainSection.EQPID;          //20220728 조숭진 config 방식 변경   //220318 조숭진 eqpid추가

                //230222 조숭진 eqp_info table을 위한 속성값 추가 s
                EqpName = MainSection.EqpName;

                EqpNumber = MainSection.EqpNumber;
                //230222 조숭진 eqp_info table을 위한 속성값 추가 e

                //UseBCR = MainSection.UseBCR;    //2021.04.06 lim

                FrontBankNum = MainSection.FrontBankNum;

                RearBankNum = MainSection.RearBankNum;

                CheckSqlDllExist();     //220318 조숭진 SQLite.Interop.dll파일 bin폴더에 생성.


                File = new FileInfo(CurrentFilePaths(FullPath) + RecipeFilePath);
                if (!File.Exists)
                {
                    _Recipe_Manager = new RecipeManager(FilePathChange(CurrentFilePaths(FullPath) + RecipeFilePath, RecipeFilePath));
                }
                else
                {
                    _Recipe_Manager = new RecipeManager(CurrentFilePaths(FullPath) + RecipeFilePath); 
                }

                //2025.01.10 lim,
                File = new FileInfo(CurrentFilePaths(FullPath) + ScenarioFilePath);
                if (!File.Exists)
                    PrintScenarioList = ScenarioList.Deserialize(FilePathChange(CurrentFilePaths(FullPath) + ScenarioFilePath, ScenarioFilePath));
                else
                    PrintScenarioList = ScenarioList.Deserialize(CurrentFilePaths(FullPath) + ScenarioFilePath);




                File = new FileInfo(CurrentFilePaths(FullPath) + AlarmListPath);
                if (!File.Exists)
                {
                    _Alarm_Manager = new AlarmManager(FilePathChange(CurrentFilePaths(FullPath) + AlarmListPath, AlarmListPath));
                }
                else
                {
                    _Alarm_Manager = new AlarmManager(CurrentFilePaths(FullPath) + AlarmListPath); //알람 관리 모듈 생성
                }

                CCLink_mgr = new CCLink.CCLinkManager(CurrentFilePaths(FullPath) + IOFileName, true); // CCLink 생성

                File = new FileInfo(CurrentFilePaths(FullPath) + GridDataItemPath);
                if (!File.Exists)
                {
                    griditemTable = GridItemListItem.Deserialize(FilePathChange(CurrentFilePaths(FullPath) + GridDataItemPath, GridDataItemPath));
                }
                else
                {
                    griditemTable = GridItemListItem.Deserialize(CurrentFilePaths(FullPath) + GridDataItemPath);
                }

                File = new FileInfo(CurrentFilePaths(FullPath) + ParameterPath);
                if (!File.Exists)
                {
                    nParameterList = PMacDataList.Deserialize(FilePathChange(CurrentFilePaths(FullPath) + GridDataItemPath, ParameterPath));
                }
                else
                {
                    nParameterList = PMacDataList.Deserialize(CurrentFilePaths(FullPath) + ParameterPath);
                }

                _CarrierStore = CarrierStorage.Instance; //캐리어 데이터를 DB 에서 가져온다.

                ShelfMgr = ShelfManager.Instance;

                UserMng = new UserManager(false);  

                AuthorityMng = new AuthorityManager(); 

                gLodingPopup = LodingPopup.Instance;//SuHwan_20221018 : [loadingpopup]


                //220523 HHJ SCS 개선     //- ShelfSetterControl 신규 추가
                //위치 변경 쉘프 초기화 진행 후 초기화 하던 C/V와 PLCData를 쉘프 초기화 전에 진행해도록 함.
                //220315 HHJ SCS 개발     //- MXComponentNet, SharedMemory 개선
                PLCDataXmlControl pdxc = new PLCDataXmlControl();
                List<PLCDataItem> allPLCDataItem;
                File = new FileInfo(CurrentFilePaths(FullPath) + PLCDataItemPath);
                if (!File.Exists)
                {
                    allPLCDataItem = pdxc.Deserialize(FilePathChange(CurrentFilePaths(FullPath) + PLCDataItemPath, PLCDataItemPath));
                }
                else
                {
                    allPLCDataItem = pdxc.Deserialize(CurrentFilePaths(FullPath) + PLCDataItemPath);
                }

                //220608 HHJ SCS 개선		//- MCProtocol, MXComponent 추가
                //PLCItemHelper helper = new PLCItemHelper(allPLCDataItem);
                //SharedMemory = new SharedMemoryClass();
                ProtocolHelper helper = new ProtocolHelper(allPLCDataItem);

                MRE_CVLineCreateEvent.Reset();
                PortManager = new CVLineManager(); // 포트 매니저
                MRE_CVLineCreateEvent.Set();

                ShelfMgr.GetShelfData();
                ShelfManager.Instance.MRE.Set();

                ParameterSet(nParameterList); // system Parameter Setting

                Type type = Type.GetType("BoxPrint.Modules." + MainSection.BoothElement.TypeName); // 2021.07.12 RGJ
                MainBooth = Activator.CreateInstance(type, EQPID, GlobalSimulMode) as BoothBase; //Booth 모듈 동적 생성 
                MainBooth.InitPLCInterface(MainSection.BoothElement.PLCNum, MainSection.BoothElement.PLCReadOffset, MainSection.BoothElement.PLCWriteOffset); //Booth PLC Interface 추가.
                MainBooth.SetLightCurtainCounter(MainSection.BoothElement.LightCurtainCount);
                MainBooth.SetLightCurtainSync(MainSection.BoothElement.LightCurtainSyncNumber);
                MainBooth.SetEMSCounter(MainSection.BoothElement.EMSCount);

                //SuHwan_20221004 : [serverclient]
                //if (ServerClientType == eServerClientType.Client)
                //    mRMManager = new RMManager_Client(RMSection);
                //else
                mRMManager = new RMManager(RMSection);

                ShelfManager.Instance.ScribeRMEvent(); // RM 생성후 RM 이벤트 구독
                if (ServerClientType == eServerClientType.Server)
                {
                    ShelfManager.Instance.StartFireCheckRun(); // 화재 감시 쓰레드 가동
                }
                //McdList = DBManager.GetDBJobInfo();     //220322 조숭진 job관련 db 불러와 joblist에 저장
                McdList = DBManager.DbGetProcedureJobInfo();

                //DBManager.DbGetProcedureFireInfo();      //220527 조숭진 이제 여기서 안함. //220512 조숭진 화재감지 추가
                //DBManager.DbGetFireInfo();

                //if (UseServoSystem)
                //{
                //    LogManager.WriteConsoleLog(eLogLevel.Info, "서보 초기화 시작");
                //    ServoManager.GetManagerInstance();
                //    LogManager.WriteConsoleLog(eLogLevel.Info, "서보 초기화 종료");
                //}

                //InitWCFCommunication(); //WCF 통신 확립.

                //Initialize_SafetyMonitor();//Safety 통신 확립.


                //WPS_mgr = new WPS_Manager(WPSSection); //WPS 모니터링 모듈 초기화 및 시작

                //PartsLife_mgr = new PartsLifeManager();


                if (ServerClientType == eServerClientType.Server)
                {
                    MainBooth.StartBooth(); //부스 Run 은 서버만 의미 있음.
                }


                GlobalData.Current.SendMessageEvent = "Initializing : GlobalData Complete";
                _GlobalInitComp = true;


                //SuHwan_20221004 : [serverclient]
                if (ServerClientType == eServerClientType.Client)
                {
                    _Scheduler = new ClientScheduler();
                    _Scheduler.InitScheduler();
                }
                else
                {
                    if (MainSection.SchedulerElement.UseScheduler) //스케쥴러 사용시 초기화 작업
                    {
                        Type sctype = Type.GetType("BoxPrint.Scheduler." + MainSection.SchedulerElement.TypeName);
                        _Scheduler = Activator.CreateInstance(sctype) as BaseScheduler;
                        _Scheduler.SetWaitInTime(MainSection.SchedulerElement.WaitInCommandTime);
                        if(EQPID == "M0STK013" || EQPID == "M0STK014")
                        {
                            _Scheduler.SetAddtionalMargin(5); //Config 기본값으로 고정되어 들어가기에 원인 확인 전까지 임시로 하드코딩해둠
                        }
                        else
                        {
                            _Scheduler.SetAddtionalMargin(MainSection.SchedulerElement.AddtionalMargin);
                        }
                        _Scheduler.InitScheduler();
                    }
                    //HSMS.Start(); //HSMS 쓰레드 가동 //241202 HSMS 가동위치 변경
                }

                //SuHwan_20221116 : [simul]
                if (GlobalData.Current.ServerClientType == eServerClientType.Server && GlobalSimulMode)
                {
                    PLCSimulatorManager.Instance.CreateSimulPLCMoudules();
                    PLCSimulatorManager.Instance.StartSimulPLCModules();
                }

                MRE_GlobalDataCreatedEvent.Set(); //대기중인 모듈 개별 Run 쓰레드 동작신호 ON

                GlobalData.Current.ShelfMgr.RefreshDBUpdate(true); //시작하면 Client를 위해 새로 Shelf DB 업데이트 함.

                LogManager.WriteConsoleLog(eLogLevel.Info, "Refresh Shelf DB Data.");

                MainBooth.SCState = eSCState.PAUSED; //초기화 끝나면 포즈 상태로 전이.
                LogManager.WriteConsoleLog(eLogLevel.Info, "GlobalData has been created.");

                GlobalData.Current.DBManager.DbSetProcedureEQPInfo(0, 0, 0, 0, true);       //230314

                if(ServerClientType == eServerClientType.Server)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Start HSMS......");
                    Thread.Sleep(3000); //내부 자동알람 클리어될만한 시간 대기 3초면 충분할듯
                    HSMS.Start(); //HSMS 쓰레드 가동 //241202 RGJ HSMS 가동위치 변경 리컨사일시알람 초기화 문제로 초기화 완료 다하고 HSMS 가동으로 변경함 
                }

                //Print 쓰레드 시작
                PrinterMng.Start();
            }
            catch (Exception ex)
            {
                //GlobalContext 생성 도중 예외 발생하면 프로그램 종료
                Console.WriteLine(ex.ToString());
                Console.WriteLine("\r\n프로그램 실행준비중 오류가 발생하였습니다.\r\nPress any key to exit.");
                Console.ReadKey();
                this.ReleaseGlobalResource();
                Environment.Exit(20);
            }
        }

        private void Initialize_WPSMonitor()
        {

        }

        private void GetAllEqpInfoforClient()
        {
            #region EQPinfo Get from All DB
            int repetitionstart = 0;
            int repetitionend = 0;

            {
                repetitionstart = 31; //호기번호는 하드코딩한다.
                repetitionend = 50;
            }


            for (int i = repetitionstart; i <= repetitionend; i++)
            {
                EQPInfo eqpinfo = GlobalData.Current.DBManager.EqpListForMap(i);
                if (eqpinfo != null && !string.IsNullOrEmpty(eqpinfo.EQPID))
                {
                    GlobalData.Current.EQPListAddOrUpdate(eqpinfo);
                }
            }
            #endregion
        }

        public void DBConfigDataInitialize()
        {
            MainConfigSection tempMain;
            tempMain = ConfigurationManager.GetSection("MainSection") as MainConfigSection;
            DBManager.GetConfigInfo(tempMain);

            RMConfigSection tempRM;
            tempRM = ConfigurationManager.GetSection("RMSection") as RMConfigSection;
            DBManager.GetConfigInfo(tempRM);

            PLCConfigSection tempPLC;
            ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap();
            fileMap.ExeConfigFilename = Path.Combine(CurrentFilePaths(System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName)), "App.config");
            
            FileInfo File = new FileInfo(fileMap.ExeConfigFilename);
            if (!File.Exists)
            {
                string configFile = ConfigFilePathChange(fileMap.ExeConfigFilename, "\\App.config");

                fileMap.ExeConfigFilename = configFile;
            }

            Configuration cfg = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);
            tempPLC = cfg.GetSection(PLCConfigSection.SECTION_NAME) as PLCConfigSection;
            DBManager.DbSetProcedureConfigInfo("Plcs", "Count", tempPLC.Plcs.Count.ToString(), string.Empty, string.Empty);
            DBManager.DbSetProcedureConfigInfo("Plcs", "PLCSimulMode", tempPLC.PLCSimulMode.ToString());        //220917 조숭진 추가

            //DBManager.DbGetConfigInfo(PLCSection);      //220917 조숭진 추가
            DBManager.DbGetGlobalConfigInfo(PLCSection);      //220917 조숭진 추가

            foreach (PLCConfigElement plcinfo in tempPLC.Plcs)
            {
                DBManager.GetConfigInfo(tempPLC, plcinfo);
                for (int i = 0; i < tempPLC.Plcs.Count; i++)
                {
                    //DBManager.DbGetConfigInfo(PLCSection[i], i);
                    DBManager.DbGetGlobalConfigInfo(PLCSection[i], i);
                }
            }

            //DBManager.DbGetConfigInfo(MainSection);
            //DBManager.DbGetConfigInfo(RMSection);
            //DBManager.DbGetConfigInfo(FireSection);
            DBManager.DbGetGlobalConfigInfo(MainSection);
            DBManager.DbGetGlobalConfigInfo(RMSection);
        }

        //SuHwan_20221006 : [ServerClient]
        private void setMcdList(McsJobManager rcvValue)
        {
            try
            {
                foreach (var item in rcvValue)
                {
                    if (McdList.IsCommandIDContain(item.CommandID))
                    {
                        var TargetMcsJob = McdList.GetCommandIDJob(item.CommandID);

                        TargetMcsJob.CreateTime = item.CreateTime;
                        TargetMcsJob.CarrierID = item.CarrierID;
                        TargetMcsJob.Source = item.Source;
                        TargetMcsJob.Destination = item.Destination;
                        TargetMcsJob.Priority = item.Priority;
                        TargetMcsJob.TransferState = item.TransferState;
                        TargetMcsJob.CarrierType = item.CarrierType;
                        TargetMcsJob.CommandID = item.CommandID;
                        TargetMcsJob.JobFrom = item.JobFrom;
                        TargetMcsJob.JobType = item.JobType;
                        TargetMcsJob.TCStatus = item.TCStatus;
                        TargetMcsJob.AssignedRM = item.AssignedRM;
                        TargetMcsJob.Step = item.Step;
                        TargetMcsJob.SubJob = item.SubJob;
                    }
                    else
                    {
                        _McdList.AddMcsJob(item);
                    }
                }

                if (McdList.Count != rcvValue.Count)
                {
                    List<McsJob> deleteJobBuffer = new List<McsJob>();
                    foreach (var item in GlobalData.Current.McdList)
                    {
                        if (!rcvValue.IsCommandIDContain(item.CommandID))
                        {
                            deleteJobBuffer.Add(item);
                        }
                    }

                    foreach (var deleteJob in deleteJobBuffer)
                    {
                        if(GlobalData.Current.ServerClientType == eServerClientType.Server)
                        {
                            McdList.DeleteMcsJob(deleteJob, deleteJob.SubJob == eSubJobType.Push);
                        }
                        else
                        {
                            McdList.Remove(deleteJob);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Fatal, ex.ToString());
            }
        }

        public void appSettingChange()
        {
            string strDerectoryPath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            int iFindArr = -1;

            if (strDerectoryPath.Contains("\\bin"))
            {
                iFindArr = strDerectoryPath.IndexOf("\\bin");
                strDerectoryPath = strDerectoryPath.Remove(iFindArr);
            }
            string configFile = string.Concat(strDerectoryPath, "\\App.config");
            XmlDocument xdoc = new XmlDocument();
            //xdoc.Load(configFile);

            FileInfo File = new FileInfo(configFile);
            if (!File.Exists)
            {
                configFile = ConfigFilePathChange(configFile, "\\App.config");

                xdoc.Load(configFile);
            }
            else
            {
                xdoc.Load(configFile);
            }

            for (int i = 0; i < xdoc.ChildNodes.Count; i++)
            {
                var temp = xdoc.ChildNodes[i];

                if (temp.NodeType != XmlNodeType.Element)
                    continue;
                else
                {
                    for (int j = 0; j < temp.ChildNodes.Count; j++)
                    {
                        var temp2 = temp.ChildNodes[j];

                        if (temp2.Name != "appSettings")
                            continue;
                        else
                        {
                            for (int o = 0; o < temp2.ChildNodes.Count; o++)
                            {
                                var temp3 = temp2.ChildNodes[o];

                                if (temp3.Name != "add")
                                    continue;
                                else
                                {
                                    for (int q = 0; q < temp3.Attributes.Count; q++)
                                    {
                                        var temp4 = temp3.Attributes[q];

                                        if (temp4.NodeType == XmlNodeType.Attribute && temp4.Name == "value")
                                        {
                                            temp4.Value = "true";
                                            xdoc.Save(configFile);
                                            break;
                                        }
                                    }
                                }
                                break;
                            }
                            break;
                        }
                    }
                    break;
                }
            }
        }

        private bool appSettingLoad()
        {
            bool bloadok = false;
            //eqpid = string.Empty;

            //첫 실행 시 클라이언트/서버를 구분할 수 없어 어쩔 수 없이 프로그램경로로 먼저 나눈다.
            //if (System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName.Contains("01.SCS_UI"))
            //{
            //    ServerClientType = eServerClientType.Client;
            //}

            string strDerectoryPath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            int iFindArr = -1;
            if (strDerectoryPath.Contains("\\bin"))
            {
                iFindArr = strDerectoryPath.IndexOf("\\bin");
                strDerectoryPath = strDerectoryPath.Remove(iFindArr);
            }
            string configFile = string.Concat(strDerectoryPath, "\\App.config");
            XmlDocument xdoc = new XmlDocument();
            FileInfo File = new FileInfo(configFile);
            if (!File.Exists)
            {
                configFile = ConfigFilePathChange(configFile, "\\App.config");

                xdoc.Load(configFile);
            }
            else
            {
                xdoc.Load(configFile);
            }

            for (int i = 0; i < xdoc.ChildNodes.Count; i++)
            {
                var temp = xdoc.ChildNodes[i];

                if (temp.NodeType != XmlNodeType.Element)
                    continue;
                else
                {
                    for (int j = 0; j < temp.ChildNodes.Count; j++)
                    {
                        var temp2 = temp.ChildNodes[j];

                        if (temp2.Name != "appSettings" && temp2.Name != "MainSection")
                            continue;
                        else if (temp2.Name == "appSettings")
                        {
                            for (int o = 0; o < temp2.ChildNodes.Count; o++)
                            {
                                var temp3 = temp2.ChildNodes[o];

                                if (temp3.Name != "add")
                                    continue;
                                else
                                {
                                    for (int q = 0; q < temp3.Attributes.Count; q++)
                                    {
                                        var temp4 = temp3.Attributes[q];

                                        if (temp4.NodeType == XmlNodeType.Attribute && temp4.Name == "value")
                                        {
                                            bloadok = Convert.ToBoolean(temp4.Value);
                                            if (bloadok)
                                            {
                                                temp4.Value = "false";
                                                xdoc.Save(configFile);
                                            }
                                            break;
                                        }
                                    }
                                }
                                break;
                            }
                        }
                        else if (temp2.Name == "MainSection")
                        {
                            XmlAttributeCollection attribute = temp2.Attributes;
                            for (int u = 0; u < attribute.Count; u++)
                            {

                                //SuHwan_20220930 : [ServerClient]
                                if (attribute[u].Name == "ServerClientType")
                                {
                                    Enum.TryParse(attribute[u].Value, out eServerClientType outValue);
                                    ServerClientType = outValue;
                                }
                                else if(attribute[u].Name == "LineSite")
                                {
                                    Enum.TryParse(attribute[u].Value, out eLineSite outValue);
                                    LineSite = outValue;
                                }
                                else if (attribute[u].Name == "EQPID")
                                {
                                    //Clinet CurrentEQPID 가 현재 EQPID 가 다르다면 마지막 CurrentEQPID를  EQPID 로 인식.
                                    if (ServerClientType == eServerClientType.Server) //서버
                                    {
                                        Properties.Settings.Default.CurrentEQPID = attribute[u].Value; //appconfig 값 
                                        Properties.Settings.Default.Save();
                                    }
                                    //클라이언트는 CurrentEQPID값을 EQPID로 간주. 
                                    this.EQPID = Properties.Settings.Default.CurrentEQPID;
                                }
                                else
                                {
                                    continue;
                                }


                                //if (attribute[u].Name != "EQPID")
                                //    continue;
                                //else
                                //{
                                //    eqpid = attribute[u].Value;
                                //    break;
                                //}
                            }
                            break;
                        }

                    }
                    break;
                }
            }

            return bloadok;
        }

        private void AppSettingRenewal(object SectionName, bool bInit = false, int num = 0)
        {
            if (ServerClientType == eServerClientType.Client)
                return;

            bool bChange = false;
            string strDerectoryPath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            if (strDerectoryPath.Contains("\\bin"))
            {
                int iFindArr = strDerectoryPath.IndexOf("\\bin");
                strDerectoryPath = strDerectoryPath.Remove(iFindArr);
            }
            string configFile = string.Concat(strDerectoryPath, "\\App.config");
            XmlDocument xdoc = new XmlDocument();
            FileInfo File = new FileInfo(configFile);
            if (!File.Exists)
            {
                configFile = ConfigFilePathChange(configFile, "\\App.config");

                xdoc.Load(configFile);
            }
            else
            {
                xdoc.Load(configFile);
            }

            Type sectiontype = SectionName.GetType();
            PropertyInfo[] props = sectiontype.GetProperties();

            for (int i = 0; i < xdoc.ChildNodes.Count; i++)
            {
                var temp = xdoc.ChildNodes[i];

                if (temp.NodeType != XmlNodeType.Element)
                    continue;
                else
                {
                    for (int j = 0; j < temp.ChildNodes.Count; j++)
                    {
                        var temp2 = temp.ChildNodes[j];

                        if (temp2.Name == sectiontype.Name ||
                            (sectiontype.Name == "PLCSection" && temp2.Name == "Plcs"))     //object인자값의 이름과 app.config 하위노드의 이름이 같은지 확인한다.
                        {
                            XmlAttributeCollection xmlAttribute = temp2.Attributes;
                            int nodecount = 0;

                            foreach (var prop in props)
                            {
                                if (xmlAttribute.GetNamedItem(prop.Name) == null
                                    && !prop.Name.Contains("Element"))
                                    continue;

                                if (!prop.Name.Contains("Element"))
                                {
                                    string tempvalue = prop.GetValue(SectionName).ToString();

                                    if (tempvalue != xmlAttribute[prop.Name].Value)
                                    {
                                        xmlAttribute[prop.Name].Value = tempvalue;

                                        if (!bChange)
                                        {
                                            bChange = true;
                                        }
                                    }
                                }
                                else
                                {
                                    if (prop.Name.Equals("RM2Element") &&
                                        string.IsNullOrEmpty(GlobalData.Current.RMSection.RM2Element.ModuleName))
                                    {
                                        //if (temp2.ChildNodes[nodecount].NodeType == XmlNodeType.Comment)
                                        //{
                                        //    temp2.RemoveChild(temp2.ChildNodes[nodecount]);
                                        //    if (!bChange)
                                        //    {
                                        //        bChange = true;
                                        //    }
                                        //}
                                        if (temp2.ChildNodes.Count >= 2)
                                        {
                                            int totalcount = temp2.ChildNodes.Count;

                                            for (int t = totalcount; t > 1; t--)
                                            {
                                                temp2.RemoveChild(temp2.ChildNodes[t - 1]);
                                            }
                                            bChange = true;
                                        }
                                        continue;
                                    }
                                    else if ((prop.Name.Equals("RM2Element") && temp2.ChildNodes.Count == 1) || 
                                        (prop.Name.Equals("RM2Element") && temp2.ChildNodes[nodecount].NodeType == XmlNodeType.Comment) ||
                                        (prop.Name.Equals("RM2Element") && temp2.ChildNodes[nodecount].Name != "RackMaster_Second"))
                                    {
                                        if (temp2.ChildNodes.Count >= 2)
                                        {
                                            int totalcount = temp2.ChildNodes.Count;

                                            for (int t = totalcount; t > 1; t--)
                                            {
                                                temp2.RemoveChild(temp2.ChildNodes[t - 1]);
                                            }
                                        }

                                        XmlNode newNode;
                                        XmlElement xmlEle;

                                        newNode = temp2;
                                        xmlEle = xdoc.CreateElement("RackMaster_Second");

                                        object tempobject = prop.GetValue(SectionName);
                                        Type subtype = Type.GetType(prop.PropertyType.FullName);
                                        PropertyInfo[] subprops = subtype.GetProperties();

                                        foreach (var subprop in subprops)
                                        {
                                            XmlAttribute xmlAtb;
                                            xmlAtb = xdoc.CreateAttribute(subprop.Name);
                                            xmlAtb.Value = subprop.GetValue(tempobject).ToString();

                                            xmlEle.SetAttributeNode(xmlAtb);
                                        }
                                        newNode.AppendChild(xmlEle);
                                        xdoc.Save(configFile);
                                    }

                                    for (int t = 0; t < temp2.ChildNodes.Count; t++)
                                    {
                                        var temp3 = temp2.ChildNodes[nodecount];

                                        if (temp3.NodeType == XmlNodeType.Element)
                                        {
                                            xmlAttribute = temp3.Attributes;

                                            object tempobject = prop.GetValue(SectionName);
                                            Type subtype = Type.GetType(prop.PropertyType.FullName);
                                            PropertyInfo[] subprops = subtype.GetProperties();

                                            foreach (var subprop in subprops)
                                            {
                                                if (xmlAttribute.GetNamedItem(subprop.Name) == null)
                                                    continue;

                                                string tempsubvalue = subprop.GetValue(tempobject).ToString();

                                                if (tempsubvalue != xmlAttribute[subprop.Name].Value)
                                                {
                                                    xmlAttribute[subprop.Name].Value = tempsubvalue;

                                                    if (!bChange)
                                                    {
                                                        bChange = true;
                                                    }
                                                }
                                            }
                                            nodecount++;
                                            break;
                                        }
                                        nodecount++;
                                    }
                                }
                            }
                            
                            if (bChange)
                                xdoc.Save(configFile);

                            break;
                        }
                        else if (sectiontype.Name == "PLCElement" && temp2.Name == "Plcs")
                        {
                            if (bInit)
                            {
                                int delcount = temp2.ChildNodes.Count;
                                for (int d = 0; d < delcount; d++)
                                {
                                    temp2.RemoveChild(temp2.ChildNodes[0]);
                                }
                            }
                            XmlNode newNode;
                            XmlElement xmlEle;

                            newNode = temp.ChildNodes[j];
                            xmlEle = xdoc.CreateElement("PLC");

                            foreach (var prop in props)
                            {
                                XmlAttribute xmlAtb;
                                xmlAtb = xdoc.CreateAttribute(prop.Name);
                                xmlAtb.Value = prop.GetValue(SectionName).ToString();

                                xmlEle.SetAttributeNode(xmlAtb);
                            }

                            newNode.AppendChild(xmlEle);
                            xdoc.Save(configFile);

                            break;
                        }
                    }
                }
            }
        }

        #region 비사용 안전 모니터링 기능 삭제 
        //private void Initialize_SafetyMonitor()
        //{
        //    this.SafetyManager = new SAFETYManager(SafetySection);

        //    Thread SAFETYMonitorThread = new Thread(new ThreadStart(SAFETYMonitoring));
        //    SAFETYMonitorThread.IsBackground = true;
        //    SAFETYMonitorThread.Start();
        //}
        //private void SAFETYMonitoring()
        //{
        //    int countOfError = 0;
        //    //초기 연결
        //    this.SafetyManager.ConnectPLC();

        //    LogManager.WriteConsoleLog(eLogLevel.Info, "Safety Monitoring 쓰레드 동작을 시작합니다.");
        //    while (true)
        //    {
        //        if (GlobalSimulMode) //시뮬 모드 retry 5회 접속시도
        //        {
        //            if (!SafetyManager.bSAFETY_BOOTH|| !SafetyManager.bSAFETY_RM)
        //            {
        //                if (countOfError>5)
        //                {
        //                    LogManager.WriteConsoleLog(eLogLevel.Error, "Simulation UDP SERVER Connect Retry 횟수 OVER, UDP통신 종료!");
        //                    return; 
        //                }
        //                GlobalData.Current.SafetyManager.ConnectPLC();
        //                GlobalData.Current.SafetyManager.Receive_Req();
        //                countOfError++;
        //            }
        //            else
        //            {
        //                GlobalData.Current.SafetyManager.Receive_Req();
        //            }
        //        }
        //        else 
        //        {
        //            //현장 모드 retry 계속 접속시도
        //            if (!SafetyManager.bSAFETY_BOOTH || !SafetyManager.bSAFETY_RM)
        //            {
        //                GlobalData.Current.SafetyManager.ConnectPLC();
        //                GlobalData.Current.SafetyManager.Receive_Req();
        //            }
        //            else 
        //            {
        //                GlobalData.Current.SafetyManager.Receive_Req();
        //            }
        //        }
        //        Thread.Sleep(500);
        //    }
        //}
        #endregion
        public void ReleaseGlobalResource()
        {
            try
            {
                //종료전에 오피박스 램프 off

                MainBooth?.CloseBooth();

                //여기에 비관리 자원 해제 코드 추가.
                //WCF_mgr.CloseWCF();

                //CCLink_mgr.CloseAllCClinkDevice();

                mRMManager?.CloseControllers();

                //WPS_mgr?.CloseWPSServer();

            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }

        public List<GridItemListItemInfo> GetGridItemList(string strgetTag)
        {
            List<GridItemListItemInfo> retlist = new List<GridItemListItemInfo>();
            retlist = griditemTable[strgetTag];
            return retlist;
        }
        public bool ReportAllStatus()
        {
            //모든 항목을 보고 
            return true;
            //try
            //{
            //    LogManager.WriteConsoleLog(eLogLevel.Info, "현재 Firmware 상태를  LCS 로 보고합니다.");

            //    //  2020.12.08 RM수량 변경에  따른 WCF Exception 수정
            //    foreach (var item in GlobalData.Current.mRMManager.ModuleList)
            //    {
            //        WCF_mgr.ReportRobotStatus(item.Value.ModuleName); //로봇1

            //    }
            //    WCF_mgr.ReportBoothStatus();
            //    WCF_mgr.ReportTowerLampStatus();
            //    foreach (var Lineitem in LineManager.ModuleList) //컨베이어 라인별보고 
            //    {
            //        foreach(var CVitem in Lineitem.Value.ModuleList )
            //        {
            //            CVitem.ReportModule_LCS();
            //        }
            //    }
            //    return true;
            //}
            //catch(Exception ex)
            //{
            //    LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            //    return false;
            //}
        }

        //문자열</returns>
        public string CurrentFilePaths(string sString)
        {
            string paths = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            int indexnum = 0;
            if (paths.Contains("\\bin"))
            {
                indexnum = paths.IndexOf("\\bin");
                paths = paths.Remove(indexnum);
            }
            return paths;
        }

        public void ParameterSet(PMacDataList parameter)
        {

            try
            {
                #region 파라메터에 대한 reading 변수 설정

                //SystemParameter.SCSType = Convert.ToInt32(parameter.Where(r => r.TagName == "SCSType_Number").FirstOrDefault().Note);
                //2021.07.20 lim,
                if (RMSection.RM1Element.RMType == eRMType.PMAC)
                    SystemParameter.ShelfTiltCheckHeight = Convert.ToInt32(parameter.Where(r => r.TagName == "ShelfTiltCheckHeight").FirstOrDefault().Note);

                //SystemParameter.RearXcount = Convert.ToInt32(parameter.Where(r => r.TagName == "RearXcount").FirstOrDefault().Note);
                //SystemParameter.RearYcount = Convert.ToInt32(parameter.Where(r => r.TagName == "RearYcount").FirstOrDefault().Note);
                //SystemParameter.RearTotal = Convert.ToInt32(parameter.Where(r => r.TagName == "RearTotal").FirstOrDefault().Note);

                //SystemParameter.FrontXcount = Convert.ToInt32(parameter.Where(r => r.TagName == "FrontXcount").FirstOrDefault().Note);
                //SystemParameter.FrontYcount = Convert.ToInt32(parameter.Where(r => r.TagName == "FrontYcount").FirstOrDefault().Note);
                //SystemParameter.FrontTotal = Convert.ToInt32(parameter.Where(r => r.TagName == "FrontTotal").FirstOrDefault().Note);

                SystemParameter.FrontXcount = ShelfManager.Instance.FrontData.MaxBay;
                SystemParameter.FrontYcount = ShelfManager.Instance.FrontData.MaxLevel;
                SystemParameter.FrontTotal = ShelfManager.Instance.FrontData.Count();


                SystemParameter.RearXcount = ShelfManager.Instance.RearData.MaxBay;
                SystemParameter.RearYcount = ShelfManager.Instance.RearData.MaxLevel;
                SystemParameter.RearTotal = ShelfManager.Instance.RearData.Count();

                #endregion
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Fatal, ex.ToString());
            }

        }

        public bool GetGlobalSimulMode()
        {
            string value = INI_Helper.ReadValue("Global", "GlobalSimulMode");
            if (string.IsNullOrEmpty(value))
            {
                INI_Helper.WriteValue("Global", "GlobalSimulMode", "TRUE");
                return false;
            }
            else
            {
                return value.ToUpper() == "TRUE";
            }
        }
        int SimulCVBCRTestMode = 0;
        public void SetToggleCVBCRTestMode()
        {
            if (SimulCVBCRTestMode >= 2)
            {
                SimulCVBCRTestMode = 0;
            }
            else
            {
                SimulCVBCRTestMode++;
            }
        }
        public int GetSimulCVBCRTestMode()
        {
            return SimulCVBCRTestMode;
        }

        //220318 조숭진 SQLite.Interop.dll파일 bin폴더에 생성.
        private void CheckSqlDllExist()
        {
            string strDerectoryPath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            string strExePath = strDerectoryPath;       //exe가 생성되는 출력경로에 존재해야한다.
            string SqldllFullPath = Path.Combine(strExePath, "SQLite.Interop.dll");
            string strLibaryPath = string.Empty;

            int iFindArr = -1;
            if (strDerectoryPath.Contains("\\bin"))
            {
                iFindArr = strDerectoryPath.IndexOf("\\bin");
                strDerectoryPath = strDerectoryPath.Remove(iFindArr);
            }

            if (IntPtr.Size == 8)
                strLibaryPath = strDerectoryPath + "\\DLL\\x64";
            else
                strLibaryPath = strDerectoryPath + "\\DLL";

            if (!File.Exists(SqldllFullPath))
            {
                File.Copy(Path.Combine(strLibaryPath, "SQLite.Interop.dll"), SqldllFullPath);
                Thread.Sleep(500);  //파일 카피 시간 대기
            }
        }
        public void InsertModuletoStore(ModuleBase mItem)
        {
            try
            {
                //220523 HHJ SCS 개선     //- ShelfSetterControl 신규 추가
                //ModuleStore.Add(mItem.ModuleName, mItem);
                ModuleStore.TryAdd(mItem.ModuleName, mItem);
            }
            catch (Exception)
            {

            }
        }
        public ModuleBase GetModuleByName(string name)
        {
            if (ModuleStore.ContainsKey(name))
            {
                return ModuleStore[name];
            }
            else
            {
                return null;
            }

        }
        public ICarrierStoreAble GetGlobalCarrierStoreAbleObject(string LocationName)
        {
            if (string.IsNullOrEmpty(LocationName))
            {
                return null;
            }
            if (LocationName == "BOOTH" || LocationName == "Booth" || LocationName == EQPID)
            {
                return null;
            }

            ICarrierStoreAble Location = ShelfManager.Instance.GetShelf(LocationName); //쉘프 검색
            if (Location != null)        //쉘프넘버로 들어오면 put가능한지 확인.
            {
                return Location;
            }

            Location = PortManager.GetCVModule(LocationName); //포트 검색
            if (Location != null)
            {
                return Location;
            }

            Location = mRMManager[LocationName]; //크레인검색
            if (Location != null)
            {
                return Location;
            }

            return null;

        }

        public bool GlobalConfigModify(string configtypename, string configname, string configvalue)
        {
            string strDirectoryPath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            int confignum = 0;
            bool bChange = false;

            try
            {
                if (strDirectoryPath.Contains("\\bin"))
                {
                    int iFindArr = strDirectoryPath.IndexOf("\\bin");
                    strDirectoryPath = strDirectoryPath.Remove(iFindArr);
                }
                string configFile = string.Concat(strDirectoryPath, "\\App.config");
                XmlDocument xdoc = new XmlDocument();
                FileInfo File = new FileInfo(configFile);
                if (!File.Exists)
                {
                    configFile = ConfigFilePathChange(configFile, "\\App.config");

                    xdoc.Load(configFile);
                }
                else
                {
                    xdoc.Load(configFile);
                }

                string[] configarray = Regex.Split(configtypename, @"[_.]");
                if (configarray.Length >= 2)
                {
                    string temp = Regex.Replace(configarray[1], @"[^0-9]", "");

                    if (!string.IsNullOrEmpty(temp))
                        confignum = Convert.ToInt32(temp);
                }
                else
                {
                    confignum = 0;
                }

                foreach (XmlNode firstnode in xdoc.ChildNodes)
                {
                    if (firstnode.NodeType != XmlNodeType.Element)
                        continue;

                    for (int i = 0; i < firstnode.ChildNodes.Count; i++)
                    {
                        if (firstnode.ChildNodes[i].Name != configarray[0])
                            continue;

                        if (configarray.Length == 1)
                        {
                            XmlNode modifynode = firstnode.ChildNodes[i].Attributes.GetNamedItem(configname);
                            modifynode.Value = configvalue;
                            bChange = true;
                            break;
                        }
                        else
                        {
                            if (confignum != 0)
                            {
                                XmlNode modifynode = firstnode.ChildNodes[i].ChildNodes[confignum - 1].Attributes.GetNamedItem(configname);
                                modifynode.Value = configvalue;
                                bChange = true;
                                break;
                            }
                            else
                            {
                                for (int j = 0; j < firstnode.ChildNodes[i].ChildNodes.Count; j++)
                                {
                                    if (!configtypename.Contains(firstnode.ChildNodes[i].ChildNodes[j].Name))
                                        continue;

                                    XmlNode modifynode = firstnode.ChildNodes[i].ChildNodes[j].Attributes.GetNamedItem(configname);
                                    modifynode.Value = configvalue;
                                    bChange = true;
                                    break;
                                }
                                break;
                            }
                        }
                    }
                }

                if (bChange)
                {
                    xdoc.Save(configFile);
                    appSettingChange();
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Fatal, ex.ToString());
                return false;
            }

            appSettingChange();
            return true;
        }

        //public void ConfigDataRefresh()
        //{
        //    configdatarefresh?.Invoke();
        //}

        //public void LogListRefresh()
        //{
        //    loglistrefresh?.Invoke();
        //}

        public void FuncCurrentAccountLogout()
        {
            currentaccountlogout?.Invoke();
        }

        //public void AlarmManagerViewRefresh()
        //{
        //    OnAlarmManagerViewRefreshed?.Invoke();
        //}

        //public void UserListRefresh()
        //{
        //    userlistrefresh?.Invoke();
        //}

        //221121 YSW MainSection.SCSType 값 변경
        public void UpdateMainSection()
        {
            //DBManager.DbGetGlobalConfigInfo(MainSection);
            //SCSType = MainSection.SCSType;
            ////DBManager.DbGetGlobalConfigInfo(RMSection);
            ////mRMManager = new RMManager(RMSection);
            DBManager.DbGetGlobalConfigInfo(MainSection);
            DBManager.DbGetGlobalConfigInfo(RMSection);

            string strcount = string.Empty;
            int plccount = 0;
            DBManager.DbGetGlobalConfigValue("Plcs", "Count", out strcount);
            if (!string.IsNullOrEmpty(strcount))
                plccount = Convert.ToInt32(strcount);

            DBManager.DbGetGlobalConfigInfo(PLCSection);      //220917 조숭진 추가
            for (int i = 0; i < plccount; i++)
            {
                DBManager.DbGetGlobalConfigInfo(PLCSection[i], i);
            }

            SCSType = MainSection.SCSType;

            //if (protocolManager.cth.IsAlive &&
            //    protocolManager.cth.ThreadState == ThreadState.WaitSleepJoin)
            //{
            //    protocolManager.cth.Join();
            //}
            //else if (protocolManager.cth.IsAlive &&
            //    protocolManager.cth.ThreadState == ThreadState.Running)
            //{
            //    protocolManager.cth.Join();
            //}

            mRMManager = new RMManager(RMSection);
            PortManager = new CVLineManager();

        }
        public void MapChangeForClient()
        {
            protocolManager = new ProtocolManager_Client();

            bool dbopenstate = false;
            DBManager = new OracleDBManager_Client(out dbopenstate);

            _Scheduler = new ClientScheduler();
            _Scheduler.InitScheduler();

            Type type = Type.GetType("BoxPrint.Modules." + MainSection.BoothElement.TypeName); // 2021.07.12 RGJ
            MainBooth = Activator.CreateInstance(type, EQPID, GlobalSimulMode) as BoothBase; //Booth 모듈 동적 생성 
            MainBooth.InitPLCInterface(MainSection.BoothElement.PLCNum, MainSection.BoothElement.PLCReadOffset, MainSection.BoothElement.PLCWriteOffset); //Booth PLC Interface 추가.

            //MainBooth.StartBooth(); //부스 Run 은 서버만 의미 있음.
        }

        //221229 YSW Map View안에 각 SCS의 Tooltip에 IP 항목 추가 : 현재 IP값 가져오기
        public void GetCurrentIP()
        {
            if (ServerClientType == eServerClientType.Server)
            {
                //클라이언트 pc의 특정 랜카드 이름을 검색하여 pc의 이름과 ip를 가져온다. 로그에 필요함..
                NetworkInterface[] Adapters = NetworkInterface.GetAllNetworkInterfaces();
                foreach (NetworkInterface Adapter in Adapters)
                {
                    if (Adapter.Name.ToUpper().Contains("MCS")) //230927 서버에서 랜카드가 2개이므로 구분이 필요함. 하나는 백업용임.
                    {
                        ClientPCName = Dns.GetHostName();

                        foreach (UnicastIPAddressInformation ip in Adapter.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                CurrentIP = ip.Address.ToString();
                                break;
                            }
                        }
                        break;
                    }
                }
            }
            else
            {
                //클라이언트 pc의 특정 랜카드 이름을 검색하여 pc의 이름과 ip를 가져온다. 로그에 필요함..
                NetworkInterface[] Adapters = NetworkInterface.GetAllNetworkInterfaces();

                foreach (NetworkInterface Adapter in Adapters)
                {
                    //if(Adapter.Name == "DBCON") //230927 Kiosk 랜카드가 하나이므로 구분 필요없음
                    //{
                        ClientPCName = Dns.GetHostName();

                        foreach (UnicastIPAddressInformation ip in Adapter.GetIPProperties().UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                CurrentIP = ip.Address.ToString();
                                break;
                            }
                        }
                        break;
                    //}
                }
            }
        }

        /// <summary>
        /// Config에서 DB 데이터 읽어옴
        /// </summary>
        private void GetDBConnectionInfo()
        {
            DBConfigSection tempDB;
            tempDB = ConfigurationManager.GetSection("DBSection") as DBConfigSection;        //230314

            DBSection.DBFirstConnIP = tempDB.DBFirstConnIP;
            DBSection.DBFirstConnPort = tempDB.DBFirstConnPort;
            DBSection.DBFirstConnServiceName = tempDB.DBFirstConnServiceName;
            DBSection.DBSecondConnIP = tempDB.DBSecondConnIP;
            DBSection.DBSecondConnPort = tempDB.DBSecondConnPort;
            DBSection.DBSecondConnServiceName = tempDB.DBSecondConnServiceName;
            DBSection.DBAccountName = tempDB.DBAccountName;
            DBSection.DBPassword = tempDB.DBPassword;
        }

        public string ImagePathChange(string fullpath, string filepath)
        {
            string valuestring = string.Empty;
            string filename = string.Empty;
            string temppath = fullpath;

            if (filepath.Contains("\\image"))
            {
                filename = filepath.Replace("\\image", "");
            }

            if (GlobalData.Current.ServerClientType == eServerClientType.Client)
            {
                temppath = ClientImageFilePath + filename;
            }
            else
            {
                int iFindArr = fullpath.LastIndexOf("\\01.CORE");
                string temp = fullpath.Substring(0, iFindArr);
                iFindArr = temp.LastIndexOf('_');
                temp = temp.Substring(iFindArr + 1);

                temppath = string.Format(ServerImageFilePath, temp) + filename;
            }

            FileInfo File = new FileInfo(temppath);
            if (File.Exists)
            {
                valuestring = temppath;
            }
            else
            {
                throw new FileNotFoundException(temppath + "파일을 찾을 수 없습니다.");
            }

            return valuestring;
        }

        public string FilePathChange(string fullpath, string filepath)
        {
            string valuestring = string.Empty;
            string filename = string.Empty;
            string temppath = fullpath;

            if (filepath.Contains("\\Data"))
            {
                filename = filepath.Replace("\\Data", "");
            }
            //else
            //{
            //    filename = filepath;
            //}

            if (GlobalData.Current.ServerClientType == eServerClientType.Client)
            {
                temppath = ClientDataFilePath + filename;
            }
            else
            {
                int iFindArr = fullpath.LastIndexOf("\\01.CORE");
                string temp = fullpath.Substring(0, iFindArr);
                iFindArr = temp.LastIndexOf('_');
                temp = temp.Substring(iFindArr + 1);

                temppath = string.Format(ServerDataFilePath, temp) + filename;
            }

            FileInfo File = new FileInfo(temppath);
            if (File.Exists)
            {
                valuestring = temppath;
            }
            else
            {
                throw new FileNotFoundException(temppath + "파일을 찾을 수 없습니다.");
            }

            return valuestring;
        }

        public string ConfigFilePathChange(string path, string configname)
        {
            string valuestring = string.Empty;
            int iFindArr = -1;
            string temppath = path;
            iFindArr = temppath.LastIndexOf(configname);
            temppath = temppath.Substring(iFindArr);

            if (GlobalData.Current.ServerClientType == eServerClientType.Client)
            {
                temppath = ClientConfigFilePath + temppath;
            }
            else
            {
                iFindArr = path.LastIndexOf("\\01.CORE");
                string temp = path.Substring(0, iFindArr);
                iFindArr = temp.LastIndexOf('_');
                temp = temp.Substring(iFindArr + 1);

                temppath = string.Format(ServerConfigFilePath, temp) + temppath;
            }

            FileInfo File = new FileInfo(temppath);
            if (File.Exists)
            {
                valuestring = temppath;
            }
            else
            {
                throw new FileNotFoundException(temppath + "파일을 찾을 수 없습니다.");
            }

            return valuestring;
        }

        public void FrontBankNumSet(int frontbank)
        {
            FrontBankNum = frontbank;
        }

        public void RearBankNumSet(int rearbank)
        {
            RearBankNum = rearbank;
        }

        /// <summary>
        /// 호기별 사용 팔렛 사이즈를 리턴한다. 총 20대이고 운용 바뀔 가능성도 거의 없으므로 컨피그화 대신 코드로 넣어둔다.
        /// </summary>
        /// <returns></returns>
        public List<ePalletSize> GetPalletSizeList()
        {
            List<ePalletSize> temp = new List<ePalletSize>();
            temp.Add(ePalletSize.NONE);
            switch(EQPID)
            {
                //셀버퍼
                case "M0STK011":
                case "M0STK012":
                    temp.Add(ePalletSize.Cell_Short); //240719 RGJ 키인 Pallet Size 추가 분리함.
                    temp.Add(ePalletSize.Cell_Long);
                    break;
                case "M0STK013":
                case "M0STK014":
                    temp.Add(ePalletSize.Cell_Short);

                    break;

                //모듈 완제품
                case "M0STK031":
                case "M0STK032":
                case "M0STK033":
                    temp.Add(ePalletSize.ModuleProduct_Short); //240719 RGJ 키인 Pallet Size 추가 분리함.
                    break;
                case "M0STK036":
                    temp.Add(ePalletSize.ModuleProduct_Long); //240719 RGJ 키인 Pallet Size 추가 분리함.
                    break;
                case "M0STK037":
                case "M0STK038":
                    temp.Add(ePalletSize.ModuleProduct_Short);
                    temp.Add(ePalletSize.ModuleProduct_Long);
                    break;

                //셀 완제품
                case "M0STK034":
                    temp.Add(ePalletSize.CellProduct_Short);
                    temp.Add(ePalletSize.CellProduct_Long);
                    break;

                //셀 모듈 혼용
                case "M0STK035":
                    temp.Add(ePalletSize.CellProduct_Short);
                    temp.Add(ePalletSize.CellProduct_Long);
                    temp.Add(ePalletSize.ModuleProduct_Short);
                    temp.Add(ePalletSize.ModuleProduct_Long);
                    break;

                //원자재
                case "M0STK021":
                case "M0STK022":
                case "M0STK023":
                case "M0STK024":
                case "M0STK025":
                case "M0STK026":
                case "M0STK027":
                case "M0STK028":
                    temp.Add(ePalletSize.Raw_Material);
                    break;
            }

            return temp;
        }
        

    }

    //230105 HHJ SCS 개선
    /// <summary>
    /// UI 쓰레드에 의해 점유중인 자원에 대해선 자원에 수정을 가할 때 UI 쓰레드의 Dispatcher에 작업을 위임해야 한다. 
    ///[출처]
    /// http://blog.naver.com/PostView.nhn?blogId=seokcrew&logNo=221309203938&parentCategoryNo=&categoryNo=32&viewDate=&isShowPopularPosts=false&from=postView
    /// </summary>
    //
    public static class DispatcherService
    {
        public static void Invoke(Action action)
        {
            Dispatcher dispatchObject = Application.Current != null ? Application.Current.Dispatcher : null;
            if (dispatchObject == null || dispatchObject.CheckAccess())
                action();
            else
                dispatchObject.Invoke(action);
        }
    }
}
