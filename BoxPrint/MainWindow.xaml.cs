using BoxPrint.DataList;
using BoxPrint.GUI.ClassArray;
using BoxPrint.GUI.ETC;
using BoxPrint.GUI.ETC.LoadingPopup;
using BoxPrint.GUI.EventCollection;
using BoxPrint.GUI.UIControls;
using BoxPrint.GUI.UserControls.Views;
using BoxPrint.GUI.Views;
using BoxPrint.GUI.Views.UserPage;
using BoxPrint.GUI.Windows.Views;
using BoxPrint.Log;
using BoxPrint.Modules.User;     //220406 HHJ SCS 개선     //- Login Event 추가
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using TranslationByMarkupExtension;

namespace BoxPrint
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// // 2020.09.29 MainWindow 테두리 정리
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        [DllImport("user32.dll")]
        static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);
        [DllImport("user32.dll")]
        static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        [DllImport("user32.dll")]
        static extern IntPtr RemoveMenu(IntPtr hMenu, uint nPosition, uint wFlags);

        [DllImport("kernel32.dll")]
        static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll")]
        static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr GetStdHandle(int nStdHandle);

        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
        [DllImport("user32.dll")]
        public static extern int ShowWindow(int windowHandle, int command);
        [DllImport("user32.dll")]
        public static extern int stringFindWindow(string className, string windowText);

        private static Mutex mutex = null;

        private int LoginUserPopupCount;

        DispatcherTimer MainWindowtimer = new DispatcherTimer();    //객체생성

        //private MainView mainv = null;
        private ParaView parav = null;
        private LayOutView LayOutv = null;
        //private LayOutView_ARC LayOutARCv = null; //210416 lsj ARC 주석

        private PrintMainView PrintMainv = null;

        //private ManualMoveView Manualv = null;
        private LogView Logv = null;
        private AlarmLogView AlarmLogv = null;
        private BCRLogView BCRLogv = null;
        private HSMSLogView HSMSLogv = null;
        private OperatorLogView OperatorLogv = null;
        private TransferLogView TransferLogv = null;
        //private UtilizationLogView UtilizationLogv = null;
        // private OperatorLogView OperatorLogv = null;
        // private InformLogView InformLogv = null;
        private AlarmView Alarmv = null;

        private StoredCarrierView StoredCarrierv = null;
        private IOMonitorView IOv = null;
        //private IOMonitorRMView IORMv = null;
        //private WPSMonitorView WPSv = null;

        private AlarmManagerView AlarmManagerv = null;//SuHwan_20220623 : 추가

        //220621 HHJ SCS 개선     //- ConfigPage 추가
        private ConfigView configView = null;


        //private LifeTimeView Lifev = null;
        // private LifeTimeWarningWindow LTW = null;
        //private ServoView Servov = null;

        private UserView userView = null;   //220405 HHJ SCS 개선     //- User Page 추가
        //220624 HHJ SCS 개선     //- SearchView Popup으로 변경
        //private SearchView searchView = null;  //220408 RGJ SerachView 추가.
        private SearchViewPopup searchView = null;  //220408 RGJ SerachView 추가.

        //private MapView mapView = null; //SuHwan_20220418

        private TerminalMessageView terminalMsgView = null; //221226 HHJ SCS 개선

        private AlarmPopupView pv = null;       //230315

        // 받은 부터 읽은 데이터를 입력하기 위한 대리자
        private delegate void D_Set_intValue(int nValue);
        private delegate void D_Set_StringValue(string nValue);

        SK_ButtonControl _selectMenuButton = new SK_ButtonControl();

        //230103 YSW TextSubMenu dictionary
        Dictionary<string, SK_ButtonControl> dicTextSubMenu = new Dictionary<string, SK_ButtonControl>();

        private static MainWindow MainWindowObject;
        public static MainWindow GetMainWindowInstance()
        {
            return MainWindowObject;
        }

        //테마 색상 변경 이벤트
        public delegate void EventHandler_ChangeThemeColor();
        public static event EventHandler_ChangeThemeColor _EventCall_ThemeColorChange;

        //테마 색상 모음 
        private GUIColorBase _guiColorMembers;
        public GUIColorBase GUIColorMembers
        {
            get { return _guiColorMembers; }
            set
            {
                _guiColorMembers = value;
                RaisePropertyChanged("GUIColorMembers");
            }
        }

        //테마 색상 변경 
        private bool _isThemeColor;
        public bool isThemeColor
        {
            get { return _isThemeColor; }
            set
            {
                _isThemeColor = value;
                RaisePropertyChanged("isThemeColor");
            }
        }

        //락
        private static Object BrushLock = new Object();

        //재산변경 이벤트
        protected virtual void RaisePropertyChanged(string propertyName)
        {
            lock (BrushLock)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

        }
        public event PropertyChangedEventHandler PropertyChanged;

        //메인 윈도우 타이틀바 
        //SuHwan_20230712 : 윈도우 타이틀바로 변경
        private Point _startPos;
        System.Windows.Forms.Screen[] _screens = System.Windows.Forms.Screen.AllScreens;

        //List<EQPInfo> EQPList = new List<EQPInfo>();    //YSW_20221103 EQPInfo

        //SuHwan_20221226 : [1차 UI검수] 폰트 사이즈 설정
        protected int _UIFontSize_Large = 12;  //큰폰트
        public int UIFontSize_Large
        {
            get => _UIFontSize_Large;
            set
            {
                if (_UIFontSize_Large == value) return;
                _UIFontSize_Large = value;

                RaisePropertyChanged("UIFontSize_Large");
            }
        }
        protected int _UIFontSize_Medium = 10; //중간폰트
        public int UIFontSize_Medium
        {
            get => _UIFontSize_Medium;
            set
            {
                if (_UIFontSize_Medium == value) return;
                _UIFontSize_Medium = value;

                RaisePropertyChanged("UIFontSize_Medium");
            }
        }
        protected int _UIFontSize_Small = 8;  //작은폰트
        public int UIFontSize_Small
        {
            get => _UIFontSize_Small;
            set
            {
                if (_UIFontSize_Small == value) return;
                _UIFontSize_Small = value;

                RaisePropertyChanged("UIFontSize_Small");
            }
        }
        protected bool _fixButtonFocus = false; //버튼 포커스 고정용
        public delegate void EventHandler_ChangeLanguage();
        public static event EventHandler_ChangeLanguage _EventCall_ChangeLanguage;

        private bool bMovingMap = false;

        private bool bFirstSystemState = true;

        //SuHwan_20230117 :  [2차 UI검수]
        Dictionary<string, SK_ButtonControl> dicNavigationTab = new Dictionary<string, SK_ButtonControl>();

        public MainWindow()
        {
            // 중복 실행 방지 Mutex 추가.
            string mutexName = "Toptec_BoxPrinter";
            try
            {
                mutex = new Mutex(false, mutexName);
            }
            catch (Exception)
            {
                Environment.Exit(10);
            }
            if (mutex.WaitOne(0, false))
            {
                MainWindowObject = this;
                // Console에서 Ctrl+c를 막는다
                //Console.OutputEncoding = System.Text.Encoding.UTF8; //현장 환경에서 한글 출력 깨지는 문제 때문에 인코딩변경
                Console.TreatControlCAsInput = true;

                LoginUserStatePopupView._EventCall_PageChange += new LoginUserStatePopupView.EventHandler_ChangePage(this.eventGUIPageChange);
                LayOutView._EventHandler_ShowInTaskbar += new LayOutView.EventHandler_ShowInTaskbar(eventShowInTask);

                //230102 YSW 사용자 권한에 따른 버튼 잠금
                LogInPopupView._EventHandler_LoginChange += TextSubMenuAuthority;
                GroupAccountManagementPage._EventHandler_ChangeAuthority += TextSubMenuAuthority;

                //SuHwan_20221014 : [ServerClient]
                //MapView._EventHandler_EQPIDChange += redefineLayOutView;

                DeleteConsoleCloseButton();

                InitializeComponent();

                InitLoad();

                LoginUserPopupCount = 0;


                //SuHwan_20221227 : [1차 UI검수]
                UIFontSize_Large = 17;
                UIFontSize_Medium = 14;
                UIFontSize_Small = 12;


                DataContext = this;
                GUIColorMembers = GlobalData.Current.GuiColor;

                GlobalData.Current.UserMng.OnLoginUserChange += UserMng_OnLoginUserChange;      //220406 HHJ SCS 개선     //- Login Event 추가

                //AlarmView.OnAlarmOccurred += new AlarmView.EventHandler_AlarmOccur(this.PageBtn_Click_Sub);

                AlarmPopupView._AlarmOccur += new AlarmPopupView.EventHandler_AlarmOccur(this.PageBtn_Click_Sub);

                //SuHwan_20220928
                textblock_EQPID.Text = GlobalData.Current.EQPID;

                if (GlobalData.Current.ServerClientType == eServerClientType.Server)
                {
                    skBtn_MapViewer.Visibility = Visibility.Collapsed; //20230419 RGJ 서버는 맵  선택 안보이게 함.
                }

                this.PreviewMouseDown += MainWindow_PreviewMouseDown;
                //this.PreviewKeyDown += MainWindow_PreviewKeyDown;
            }
            else
            {
                MessageBox.Show(TranslationManager.Instance.Translate("이미 SCS 실행중이거나 완전히 종료되지 않았습니다.").ToString(),
                                TranslationManager.Instance.Translate("중복실행").ToString(), MessageBoxButton.OK, MessageBoxImage.Asterisk);
                Environment.Exit(100);
            }
        }

        //private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        //{
        //    if (e.Key.Equals(Key.LWin) || e.Key.Equals(Key.RWin))
        //    {
        //        Mimimize_Click(null, null);
        //    }
        //}

        private void MainWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!(e.Source is LoginUserStatePopupView))
            {
                if (popupMain.IsOpen)
                    popupMain.IsOpen = false;
            }
        }

        //220406 HHJ SCS 개선     //- Login Event 추가

        /// <summary>
        /// LayOutView 재정의 //SuHwan_20221014 : [ServerClient]
        /// </summary>
        public void redefineLayOutView()
        {
            //YSW_20221026 Map RM 선택시 EQP_ID 변경
            textblock_EQPID.Text = GlobalData.Current.EQPID;

            foreach (Window window in Application.Current.Windows)
            {
                if (window.Name != "StockerControlSystem" && window.Name != "MainWindow")
                {
                    window.Close();
                }
            }

            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                LayOutv.page_reLoaded();
            }));

            Alarmv = null;
            Alarmv = new AlarmView();

            //GlobalData.Current.LogListRefresh();
            //GlobalData.Current.TerminalMessageRefreshedOccur();
            //GlobalData.Current.UserListRefresh();
            //GlobalData.Current.AlarmManagerViewRefresh();

            //YSW_20221117 Mainpage로 이동
            object sender = HomeButton;
            RoutedEventArgs e = new RoutedEventArgs();
            PageBtn_Click(sender, e);

            Title = string.Empty;

            Title += " [EQPID : " + GlobalData.Current.EQPID + " ]";

            Title += Environment.Is64BitProcess ? "  [64  Bit]" : "  [32 Bit]";

            Title += " [Site : " + GlobalData.Current.CurrnetLineSite + " ]";

            Title += " [" + GlobalData.Current.Scheduler.GetSchedulerName() + " ]";

            Title += " [Booth : " + GlobalData.Current.MainBooth.GetType().Name + " ]"; //타이틀 화면에 현재 로딩된 부스,로봇 모듈 표시

            Title += " [RM : " + GlobalData.Current.mRMManager.FirstRM.GetType().Name + " ]";

            if (GlobalData.Current.GlobalSimulMode) //시뮬모드일때는 상단 타이틀에 표시
            {
                this.Title = Title + "    [Simulation Mode]";
            }

            this.textblockHead.Text = this.Title;
        }

        private string _LoginUserName;
        public string LoginUserName
        {
            get { return _LoginUserName; }
            set
            {
                _LoginUserName = value;
                RaisePropertyChanged("LoginUserName");

            }
        }
        private string _LoginUserLevel;
        public static string checkLoginUserLevel;
        public string LoginUserLevel
        {
            get { return _LoginUserLevel; }
            set
            {
                _LoginUserLevel = value;
                checkLoginUserLevel = value;
                UserAccountManagementPage.Current.setChangeLoginUserID();
                UserAccountManagementPage.Current.setGUIUserChangeVisibility();
                GroupAccountManagementPage.Current.setGUIUserChangeVisibility();
                RaisePropertyChanged("LoginUserLevel");

            }
        }
        private string _LoginUserID;
        public static string checkLoginUserID;
        public string LoginUserID
        {
            get { return _LoginUserID; }
            set
            {
                _LoginUserID = value;
                checkLoginUserID = value;
                UserAccountManagementPage.Current.setChangeLoginUserID();
                GroupAccountManagementPage.Current.setChangeLoginUserID();
                LoginUserStatePopupView.Current.setChangeLoginUserID();
                RaisePropertyChanged("LoginUserID");

                GlobalData.Current.CurrentUserID = value;
            }
        }

        private void UserMng_OnLoginUserChange(User usr)
        {
            string msg = string.Empty;

            if (!(usr is null))
            {
                LoginUserID = usr.UserID;
                LoginUserName = usr.UserName;
                LoginUserLevel = usr.UserLevel.ToString();
                msg = TranslationManager.Instance.Translate("User").ToString() + " " + TranslationManager.Instance.Translate("Login").ToString();
                //MessageBoxPopupView.Show(string.Format("{0} {1}", LoginUserName, msg), MessageBoxImage.Information);
                
            }
            else
            {
                bool IsAutoLogout = false;
                User LoginUser = GlobalData.Current.UserMng.GetUserByID(LoginUserID);
                if (LoginUser != null)
                {
                    if (LoginUser.IsAutoLogoutTimeover())
                        IsAutoLogout = true;
                }

                LoginUserID = null;
                LoginUserName = null;
                LoginUserLevel = null;

                //230524 클라이언트 맵 체인지 할때는 메세지 띄우지 말자.
                //2024.05.15 lim, 자동 로그 아웃시 팝업 X
                if (!GlobalData.Current.MapViewStart && !IsAutoLogout)
                {
                    MessageBoxPopupView.Show(TranslationManager.Instance.Translate("User").ToString() + " " + TranslationManager.Instance.Translate("Logout").ToString(), MessageBoxImage.Information);
                }
            }
        }

        /// <summary>
        /// // 모니터링용 콘솔창 추가.
        /// </summary>
        private static void DeleteConsoleCloseButton()
        {
            uint SC_CLOSE = 0xF060;
            uint MF_ENABLED = 0x00000000;
            uint MF_GRAYED = 0x00000001;

            IntPtr hMenu = Process.GetCurrentProcess().MainWindowHandle;

            //InitLoad();

            if (hMenu == IntPtr.Zero || hMenu == null)
            {
                hMenu = FindWindow(null, Console.Title);
            }

            IntPtr hSystemMenu = GetSystemMenu(hMenu, false);

            EnableMenuItem(hSystemMenu, SC_CLOSE, MF_GRAYED);
            RemoveMenu(hSystemMenu, SC_CLOSE, MF_ENABLED);

            //- 콘솔 빠른입력 해제 추가.콘솔 입력에 의한 프로그램 무언정지 방지
            int STD_INPUT_HANDLE = -10;
            uint ENABLE_QUICK_EDIT = 0x0040;
            uint prevMode = 0;
            IntPtr consoleHandle = GetStdHandle(STD_INPUT_HANDLE);
            GetConsoleMode(consoleHandle, out prevMode);
            SetConsoleMode(consoleHandle, prevMode & ~ENABLE_QUICK_EDIT);
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            try
            {
                //시계 갱신
                textTime.Text = DateTime.Now.ToString("yyyy.MM.dd  hh:mm:ss");

                //SuHwan_20220608 : UI 표준화
                MainStateMonitoring();

                RMStateUpdate();

                //20240109 RGJ Starter 로 부터 넘어온 유저를 로그인 시킨다.
                if (GlobalData.Current.ServerClientType == eServerClientType.Client && GlobalData.Current.UserMng.CheckFirstUILoginReady())
                {
                    string[] args = Environment.GetCommandLineArgs(); //Starter 로부터 사용자 이름과 패스워드를 받았으면 접속 시도한다.
                    if (args.Count() >= 4)
                    {
                        string userName = args[2];
                        string Password = args[3];
                        LogInPopupView kw = new LogInPopupView();
                        kw.TryLogin(userName, Password);
                        kw.Close();
                    }
                }

                //TEST 위해 자동 로그인 
                if (GlobalData.Current.ServerClientType == eServerClientType.Server && GlobalData.Current.UserMng.CheckFirstUILoginReady())
                {
                    string userName = "admin";
                    string Password = "toptec135";
                    LogInPopupView kw = new LogInPopupView();
                    kw.TryLogin(userName, Password);
                    kw.Close();
                }

                //20230614 RGJ 자동 로그아웃 상태 체크
                User LoginUser = GlobalData.Current.UserMng.GetUserByID(LoginUserID);
                if (LoginUser != null)
                {
                    if (LoginUser.IsAutoLogoutTimeover()) //자동 로그아웃 타임오버시 
                    {
                        //혹시나 메뉴얼 커맨드 창 같은게 열려있을 수 있으니 닫아버리자..
                        //그냥 로그아웃할때도 모두 닫도록 코드이동
                        //foreach (Window window in Application.Current.Windows)
                        //{
                        //    if (window.Name != "StockerControlSystem" && window.Name != "MainWindow")
                        //    {
                        //        window.Close();
                        //    }
                        //}
                        ProcessCurrentUserLogout();//로그아웃 시킨다.
                    }
                }



            }
            catch(Exception ex)
            {
                _ = ex;
            }

        }

        private void MainStateMonitoring()
        {
            bool bChange = false;
            //221101 YSW Server에서만 상태 판단 Client DB에서 상태 정보 가져오기
            if (GlobalData.Current.ServerClientType == eServerClientType.Server)
            {
                //SuHwan_20230102 : [1차 UI검수] 바인딩 시킬까 하다.. 기존에 하던데로 함
                //디비 서버 접속 상태 모니티렁
                if (GlobalData.Current.DBManager.IsConnect)
                {
                    if (textblock_DBConectivity.Tag.ToString() != "1")
                    {
                        textblock_DBConectivity.Tag = "1";
                    }

                    if (textblock_CoreConectivity.Tag.ToString() != "1")
                    {
                        textblock_CoreConectivity.Tag = "1";
                    }
                }
                else
                {
                    if (textblock_DBConectivity.Tag.ToString() != "0")
                    {
                        textblock_DBConectivity.Tag = "0";
                    }

                    if (textblock_CoreConectivity.Tag.ToString() != "0")
                    {
                        textblock_CoreConectivity.Tag = "0";
                    }
                }
                //MCS 연결 상태 모니터링
                if (GlobalData.Current.MainBooth.MCSConnetionState)
                {
                    #region 삭제
                    //if (GlobalData.Current.MainBooth.CurrentOnlineState == eOnlineState.Remote &&
                    //    GlobalData.Current.MainBooth.SCState == eSCState.AUTO) //리모트 & Auto 조건 추가.
                    //{
                    //    if (textblock_MCSConectivity.Text != TranslationManager.Instance.Translate("Remote").ToString())
                    //    {
                    //        textblock_MCSConectivity.Text = TranslationManager.Instance.Translate("Remote").ToString();
                    //        textblock_MCSConectivity.Tag = "2";

                    //        //221019 조숭진 클라이언트용 상태변경
                    //        if (!bChange)
                    //            bChange = true;
                    //    }
                    //}
                    //else
                    //{
                    //    if (textblock_MCSConectivity.Text != TranslationManager.Instance.Translate("Connect").ToString()) //SuHwan_20220608 : UI 표준화
                    //    {
                    //        textblock_MCSConectivity.Text = TranslationManager.Instance.Translate("Connect").ToString();
                    //        textblock_MCSConectivity.Tag = "1";

                    //        //221019 조숭진 클라이언트용 상태변경
                    //        if (!bChange)
                    //            bChange = true;
                    //    }
                    //}
                    #endregion

                    //240228 RGJ  MCS Remote / Connect 구분 필요없음 Connect로  (조범석 매니저 요청)
                    if (textblock_MCSConectivity.Text != TranslationManager.Instance.Translate("Connect").ToString()) //SuHwan_20220608 : UI 표준화
                    {
                        textblock_MCSConectivity.Text = TranslationManager.Instance.Translate("Connect").ToString();
                        textblock_MCSConectivity.Tag = "1";

                        //221019 조숭진 클라이언트용 상태변경
                        if (!bChange)
                            bChange = true;
                    }
                }
                else
                {
                    //SuHwan_20220608 : UI 표준화
                    if (textblock_MCSConectivity.Text != TranslationManager.Instance.Translate("Disconnect").ToString())
                    {
                        textblock_MCSConectivity.Text = TranslationManager.Instance.Translate("Disconnect").ToString();
                        textblock_MCSConectivity.Tag = "0";

                        //221019 조숭진 클라이언트용 상태변경
                        if (!bChange)
                            bChange = true;
                    }
                }

                //System 상태 모니터링
                if (bFirstSystemState || 
                    textblock_SystemType.Text != TranslationManager.Instance.Translate(GlobalData.Current.MainBooth.SCState.ToString()).ToString())
                {
                    textblock_SystemType.Text = TranslationManager.Instance.Translate(GlobalData.Current.MainBooth.SCState.ToString()).ToString();
                    textblock_SystemType.Tag = Convert.ToInt32(GlobalData.Current.MainBooth.SCState).ToString();

                    //221019 조숭진 클라이언트용 상태변경
                    if (!bChange)
                        bChange = true;

                    if (bFirstSystemState)
                        bFirstSystemState = false;
                }
                //PLC 상태 모니터링
                if (GlobalData.Current.protocolManager.CheckALLPLCConnection())
                {
                    if (textblock_PLCConectivity.Text != TranslationManager.Instance.Translate("Connect").ToString())
                    {
                        textblock_PLCConectivity.Text = TranslationManager.Instance.Translate("Connect").ToString();
                        textblock_PLCConectivity.Tag = "1";

                        //221019 조숭진 클라이언트용 상태변경
                        if (!bChange)
                            bChange = true;
                    }
                }
                else
                {
                    //SuHwan_20220608 : UI 표준화
                    if (textblock_PLCConectivity.Text != TranslationManager.Instance.Translate("Disconnect").ToString())
                    {
                        textblock_PLCConectivity.Text = TranslationManager.Instance.Translate("Disconnect").ToString();
                        textblock_PLCConectivity.Tag = "0";

                        //221019 조숭진 클라이언트용 상태변경
                        if (!bChange)
                            bChange = true;
                    }
                }
                //SCS 상태 모니터링 Offline 
                if(GlobalData.Current.MainBooth.CurrentOnlineState == eOnlineState.Remote)
                {
                    if (textblock_SCSConectivity.Text != TranslationManager.Instance.Translate("Online").ToString())
                    {
                        textblock_SCSConectivity.Text = TranslationManager.Instance.Translate("Online").ToString();
                        textblock_SCSConectivity.Tag = "1";

                        //221019 조숭진 클라이언트용 상태변경
                        if (!bChange)
                            bChange = true;
                    }

                }
                else
                {
                    if (textblock_SCSConectivity.Text != TranslationManager.Instance.Translate("Offline").ToString())
                    {
                        textblock_SCSConectivity.Text = TranslationManager.Instance.Translate("Offline").ToString();
                        textblock_SCSConectivity.Tag = "0";

                        //221019 조숭진 클라이언트용 상태변경
                        if (!bChange)
                            bChange = true;
                    }

                }

                #region 주석
                //알람 상태
                //int currentAlarmCount = GlobalData.Current.Alarm_Manager.ActiveAlarmList.Count();
                //if (currentAlarmCount > 0)
                //{
                //    if (textblockAlarmCounter.Text != currentAlarmCount.ToString())
                //    {
                //        textblockAlarmCounter.Text = currentAlarmCount.ToString();
                //        PageBtn_Click_Sub("AlarmPopup");
                //    }
                //}
                //else
                //{
                //    if (textblockAlarmCounter.Text != "0")
                //    {
                //        textblockAlarmCounter.Text = "0";
                //    }
                //}

                ////Alarm  상태 모니터링
                //if (GlobalData.Current.Alarm_Manager.GetActiveHeavyAlarmCount() > 0)
                //{
                //    if ((string)image_AlarmState.Tag != "0") //태그값 참조해서 이미지 변경 필요할때만 변경.
                //    {
                //        image_AlarmState.Source = new BitmapImage(new Uri("Image/Led_Red.png", UriKind.Relative));
                //        image_AlarmState.Tag = "0";
                //    }
                //}
                //else if (GlobalData.Current.Alarm_Manager.GetActiveAlarmCount() > 0) //경알람만 있는경우
                //{
                //    if ((string)image_AlarmState.Tag != "1")
                //    {
                //        image_AlarmState.Source = new BitmapImage(new Uri("Image/Led_Yellow.png", UriKind.Relative));
                //        image_AlarmState.Tag = "1";
                //    }
                //}
                //else
                //{
                //    if ((string)image_AlarmState.Tag != "2")
                //    {
                //        image_AlarmState.Source = new BitmapImage(new Uri("Image/Led_Green.png", UriKind.Relative));
                //        image_AlarmState.Tag = "2";
                //    }
                //}

                ////221101 YSW Client 요청 확인 >> COMMAND로 수정해야함
                //List<ClientRequestSysStateInfo> CRSStateInfoList = new List<ClientRequestSysStateInfo>();
                //CRSStateInfoList = GlobalData.Current.DBManager.DbGetProcedureClientRequestSystemState();
                //foreach (var item in CRSStateInfoList)
                //{
                //    if (item.EQPID == GlobalData.Current.EQPID)
                //    {
                //        if (item.RequestSignal == "1")
                //        {
                //            GlobalData.Current.MainBooth.SCState = item.SYSTEM_State;
                //            textblock_SystemType.Text = GlobalData.Current.MainBooth.SCState.ToString();
                //            textblock_SystemType.Tag = Convert.ToInt32(GlobalData.Current.MainBooth.SCState).ToString();
                //            GlobalData.Current.DBManager.DbSetProcedureClientRequestSystemState('0', textblock_SystemType.Tag);
                //            bChange = true;
                //        }
                //    }
                //}
                #endregion
                //221019 조숭진 클라이언트용 상태변경
                if (bChange)
                    GlobalData.Current.DBManager.DbSetProcedureEQPInfo(textblock_MCSConectivity.Tag, textblock_SCSConectivity.Tag, textblock_PLCConectivity.Tag, textblock_SystemType.Tag);
            }
            else//Client일 경우
            {
                if (GlobalData.Current.DBManager.IsConnect)
                {
                    if (textblock_DBConectivity.Tag.ToString() != "1")
                    {
                        textblock_DBConectivity.Tag = "1";
                    }

                    if (textblock_CoreConectivity.Tag.ToString() != "1")
                    {
                        textblock_CoreConectivity.Tag = "1";
                    }
                }
                else
                {
                    if (textblock_DBConectivity.Tag.ToString() != "0")
                    {
                        textblock_DBConectivity.Tag = "0";
                    }

                    if (textblock_CoreConectivity.Tag.ToString() != "0")
                    {
                        textblock_CoreConectivity.Tag = "0";
                    }
                }

                foreach (var item in GlobalData.Current.EQPList)
                {
                    if (item.EQPID == GlobalData.Current.EQPID)
                    {
                        if (item.MCS_State != textblock_MCSConectivity.Tag.ToString() || LanguageChange_state)
                        {
                            switch (item.MCS_State)
                            {
                                case "0":
                                    textblock_MCSConectivity.Text = TranslationManager.Instance.Translate("Disconnect").ToString();
                                    textblock_MCSConectivity.Tag = 0;
                                    break;
                                case "1":
                                    textblock_MCSConectivity.Text = TranslationManager.Instance.Translate("Connect").ToString();
                                    textblock_MCSConectivity.Tag = 1;
                                    break;
                                case "2":
                                    textblock_MCSConectivity.Text = TranslationManager.Instance.Translate("Remote").ToString();
                                    textblock_MCSConectivity.Tag = 2;
                                    break;
                            }
                        }

                        if (item.SCS_State != textblock_SCSConectivity.Tag.ToString() || LanguageChange_state)
                        {
                            switch (item.SCS_State)
                            {
                                case "0":
                                    textblock_SCSConectivity.Text = TranslationManager.Instance.Translate("Offline").ToString();
                                    textblock_SCSConectivity.Tag = 0;
                                    break;
                                case "1":
                                    textblock_SCSConectivity.Text = TranslationManager.Instance.Translate("Online").ToString();
                                    textblock_SCSConectivity.Tag = 1;
                                    break;
                            }
                        }

                        if (item.PLC_State != textblock_PLCConectivity.Tag.ToString() || LanguageChange_state)
                        {
                            switch (item.PLC_State)
                            {
                                case "0":
                                    textblock_PLCConectivity.Text = TranslationManager.Instance.Translate("Disconnect").ToString();
                                    textblock_PLCConectivity.Tag = 0;
                                    break;
                                case "1":
                                    textblock_PLCConectivity.Text = TranslationManager.Instance.Translate("Connect").ToString();
                                    textblock_PLCConectivity.Tag = 1;
                                    break;
                            }
                        }

                        if (textblock_SystemType.Text != TranslationManager.Instance.Translate(item.SYSTEM_State.ToString()).ToString() || LanguageChange_state) 
                        {
                            GlobalData.Current.MainBooth.SCState = item.SYSTEM_State;
                            textblock_SystemType.Text = TranslationManager.Instance.Translate(item.SYSTEM_State.ToString()).ToString();
                            textblock_SystemType.Tag = Convert.ToInt32(GlobalData.Current.MainBooth.SCState).ToString();
                        }
                        #region 주석
                        ////221101 YSW System State 상태 변경 Client 요청 >> COMMAND로 수정해야함
                        //if (textblock_SystemType.Text != GlobalData.Current.MainBooth.SCState.ToString())
                        //{
                        //    int keylock = 0;
                        //    List<ClientRequestSysStateInfo> CRSStateInfoList = new List<ClientRequestSysStateInfo>();
                        //    CRSStateInfoList = GlobalData.Current.DBManager.DbGetProcedureClientRequestSystemState();
                        //    foreach (var item2 in CRSStateInfoList)
                        //    {
                        //        if (item2.EQPID == GlobalData.Current.EQPID)
                        //        {
                        //            if (item2.RequestSignal == "1")
                        //            {
                        //                keylock = 1;
                        //            }
                        //            else
                        //            {
                        //                keylock = 0;
                        //            }
                        //        }
                        //    }
                        //    if (textblock_SCSConectivity.Text == "Connect")
                        //    {
                        //        if (keylock != 1)
                        //        {
                        //            textblock_SystemType.Tag = Convert.ToInt32(GlobalData.Current.MainBooth.SCState).ToString();
                        //            GlobalData.Current.DBManager.DbSetProcedureClientRequestSystemState('1', textblock_SystemType.Tag);
                        //        }
                        //    }
                        //}
                        #endregion
                    }
                }
                if(LanguageChange_state)
                    LanguageChange_state = false;
            }

            //알람 상태
            int currentAlarmCount = GlobalData.Current.Alarm_Manager.ActiveAlarmList.Count();
            if (currentAlarmCount > 0)
            {
                if (textblockAlarmCounter.Text != currentAlarmCount.ToString())
                {
                    textblockAlarmCounter.Text = currentAlarmCount.ToString();

                    if (GlobalData.Current.ServerClientType == eServerClientType.Client)     //230224 조숭진 알람팝업뷰를 클라이언트에서만 보이게하자..
                    {
                        if(pv == null) //240502 알람 팝업 오픈 상태면 추가 오픈 안함. 점점 누적되면 UI 다운됨. 
                        {
                            PageBtn_Click_Sub("AlarmPopup");
                        }
                    }
                }
            }
            else
            {
                if (textblockAlarmCounter.Text != "0")
                {
                    textblockAlarmCounter.Text = "0";
                }

                if (GlobalData.Current.ServerClientType == eServerClientType.Client)        //230315
                    PageBtn_Click_Sub("AlarmPopupClose");
            }
        }

        /// <summary>
        /// // 2020.11.06 RM상태 MainWindow 추가 작업중
        /// </summary>
        private void RMStateUpdate()
        {
            //SuHwan_20220608 : UI 표준화 
        }



        private void InitLoad()
        {
            try
            {
                GlobalData.CreateGlobalDataContext();

                //Title += " [EQPID : " + GlobalData.Current.EQPID + " ]";

                //Title += (Environment.Is64BitProcess ? "  [64 Bit]" : "  [32 Bit]");

                //Title += " [Site : " + GlobalData.Current.CurrnetLineSite + " ]";

                //Title += " [" + GlobalData.Current.Scheduler.GetSchedulerName() + " ]";

                //Title += " [Booth : " + GlobalData.Current.MainBooth.GetType().Name + " ]"; //타이틀 화면에 현재 로딩된 부스,로봇 모듈 표시

                //Title += " [RM : " + GlobalData.Current.mRMManager.FirstRM.GetType().Name + " ]";

                //if (GlobalData.Current.GlobalSimulMode) //시뮬모드일때는 상단 타이틀에 표시
                //{
                //    this.Title = Title + "    [Simulation Mode]";
                //}

                // 2020.09.16 // MainView 로딩

                textblockHead.Text += " [EQPID : " + GlobalData.Current.EQPID + " ]";

                textblockHead.Text += Environment.Is64BitProcess ? "  [64 Bit]" : "  [32 Bit]";

                textblockHead.Text += " [Site : " + GlobalData.Current.CurrnetLineSite + " ]";

                textblockHead.Text += " [" + GlobalData.Current.Scheduler.GetSchedulerName() + " ]";

                textblockHead.Text += " [Booth : " + GlobalData.Current.MainBooth.GetType().Name + " ]"; //타이틀 화면에 현재 로딩된 부스,로봇 모듈 표시


                if (GlobalData.Current.GlobalSimulMode) //시뮬모드일때는 상단 타이틀에 표시
                {
                    textblockHead.Text += "    [Simulation Mode]";
                }
                else
                {
                    textblockHead.Text += "    [Ver. 1.0]";
                }

                // 2020.01.05 ARC Type 혼용 할수 있도록 UI필요 없는 항목 삭제 추가
                if (GlobalData.Current.RMSection.RM1Element.RMType == eRMType.PMAC || GlobalData.Current.RMSection.RM1Element.RMType == eRMType.TPLC)
                {
                    LayOutv = new LayOutView();
                    if (GlobalData.Current.RMSection.RM1Element.RMType == eRMType.PMAC)
                        parav = new ParaView();
                }
                else if (GlobalData.Current.RMSection.RM1Element.RMType == eRMType.ARC
                      || GlobalData.Current.RMSection.RM1Element.RMType == eRMType.PLC_EHER || GlobalData.Current.RMSection.RM1Element.RMType == eRMType.PLC_UTL)//2021년 5월 20일 목요일 오전 10:42:36 - Editted by 수환 : PLC 추가
                {
                    //210416 lsj ARC 혼용 수정
                    LayOutv = new LayOutView();
                }

                //210105 lsj 주석
                //Manualv = new ManualMoveView();
                //parav = new ParaView();

                PrintMainv = new PrintMainView();

                Logv = new LogView();
                AlarmLogv = new AlarmLogView();
                BCRLogv = new BCRLogView();
                HSMSLogv = new HSMSLogView();
                OperatorLogv = new OperatorLogView();
                TransferLogv = new TransferLogView();
                //UtilizationLogv = new UtilizationLogView();
                // OperatorLogv = new OperatorLogView();
                // InformLogv = new InformLogView();
                Alarmv = new AlarmView();
                StoredCarrierv = new StoredCarrierView();
                IOv = new IOMonitorView();
                //IORMv = new IOMonitorRMView();

                AlarmManagerv = new AlarmManagerView();//SuHwan_20220623 : 추가
                //WPSv = new WPSMonitorView();
                //Lifev = new LifeTimeView();
                //if (GlobalData.Current.UseServoSystem)
                //{
                //    Servov = new ServoView();
                //}

                configView = new ConfigView();      //220621 HHJ SCS 개선     //- ConfigPage 추가

                userView = new UserView(); //220405 HHJ SCS 개선     //- User Page 추가
                //220624 HHJ SCS 개선     //- SearchView Popup으로 변경
                //searchView = new SearchView();//220408 RGJ SerachView 추가.

                terminalMsgView = new TerminalMessageView();    //221226 HHJ SCS 개선

                //if (GlobalData.Current.RMSection.RM1Element.RMType == eRMType.PMAC)
                //    frame_content.Content = LayOutv;
                //else if (GlobalData.Current.RMSection.RM1Element.RMType == eRMType.ARC)
                //    frame_content.Content = LayOutARCv;

                //skBtn_MapViewer.SetPublicButton(); //240212 RGJ 맵뷰어 버튼은 공용으로 번경함.권한 없는 사람이 접속하면 다른데로 갈수가 없음. (조범석 매니저 요청)

                //main 뷰 변경
                frame_content.Content = PrintMainv;//LayOutv;
                GlobalData.Current.SendTagEvent = "MainPage";
                MainWindowtimer.Interval = TimeSpan.FromMilliseconds(500);    //시간간격 설정
                MainWindowtimer.Tick += new EventHandler(timer_Tick);          //이벤트 추가
                MainWindowtimer.Start();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(string.Format(ex.ToString()));
            }
        }


        /// <summary>
        /// // 2020.11.25 Alarm 테스트 등록및 테스트 작업 
        /// </summary>
        //private void cbAlarmCreate()
        //{
        //    cbAlarm.Items.Add("0: 없음");
        //    // Pmac Alarm 등록

        //    foreach (var item in GlobalData.Current.mRMManager.ModuleList)
        //    {
        //        //201210 lsj 아진제어기 추가
        //        if (GlobalData.Current.mRMManager[item.Value.ModuleName].RMType == eRMType.PMAC)
        //        {
        //            foreach (var alarm in GlobalData.Current.mRMManager[item.Value.ModuleName].PmacAlarmCode.RMAlarmCocde)
        //            {
        //                cbAlarm.Items.Add(alarm.Code + ": " + alarm.Definition);
        //            }
        //        }
        //    }         
        //}

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ExitFirmwareProgram();
        }

        private void PageBtn_Click_Sub(string mi)
        {
            if (mi != null)
            {
                try
                {
                    switch (mi.ToString())
                    {
                        case "Alarm":
                            this.frame_content.Content = Alarmv;
                            GlobalData.Current.SendTagEvent = mi.ToString();
                            break;
                    }

                    switch (mi.ToString())
                    {
                        case "AlarmPopup":
                            pv = new AlarmPopupView();
                            pv.Height = this.ActualHeight;
                            pv.Width = this.ActualWidth;
                            pv.Owner = Application.Current.MainWindow;
                            pv.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                            pv.Show();
                            break;

                        case "AlarmPopupClose":
                            if (pv != null)
                            {
                                pv.Close();
                                pv = null;
                            }
                            break;
                    }
                }


                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(string.Format(ex.ToString()));
                }
            }
        }

        ////2020.09.11 UserControl => PageView로 변경
        private void PageBtn_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                //System.Windows.Controls.MenuItem mi = sender as System.Windows.Controls.MenuItem;
                if (sender is SK_ButtonControl mi)
                {
                    //string strLabelChange = "Firmware";
                    if (mi != null)
                    {
                        try
                        {
                            PageBtn_Click_Sub(mi.Tag.ToString());

                            //strLabelChange = string.Format("{0} {1}", ((System.Windows.Controls.MenuItem)mi.Parent).Header.ToString().ToUpper(), mi.Tag.ToString().ToUpper());

                            PageChenge(mi.Tag.ToString(), mi.Content.ToString());

                            setNavigationTab(true, mi.Tag.ToString());  //Display Null 예외체크 추가함.

                            #region 예전
                            //switch (mi.Tag.ToString())
                            //{
                            //    case "MainPage":

                            //        if ("MainPage" == GlobalData.Current.SendTagEvent)
                            //            break;

                            //        //210416 lsj ARC 주석 하나로 쓰자
                            //        //if (GlobalData.Current.RMSection.RM1Element.RMType == eRMType.PMAC)
                            //        //    this.frame_content.Content = LayOutv;
                            //        //else if (GlobalData.Current.RMSection.RM1Element.RMType == eRMType.ARC)
                            //        //    this.frame_content.Content = LayOutARCv;
                            //        LodingPopup.Instance.Start();
                            //        LodingPopup.Instance.setProgressValue(30, "LayOutView Loading...");
                            //        //LodingPopup a = new LodingPopup();
                            //        //a.Start();

                            //        Dispatcher.Invoke(new Action(() => {
                            //            this.frame_content.Content = LayOutv;
                            //        }), DispatcherPriority.ContextIdle);


                            //        GlobalData.Current.SendTagEvent = mi.Tag.ToString();


                            //        break;
                            //    case "Alarm":
                            //        this.frame_content.Content = Alarmv;
                            //        GlobalData.Current.SendTagEvent = mi.Tag.ToString();
                            //        break;
                            //    //220621 HHJ SCS 개선     //- ConfigPage 추가
                            //    case "Config":
                            //        this.frame_content.Content = configView;
                            //        GlobalData.Current.SendTagEvent = mi.Tag.ToString();
                            //        break;
                            //    //case "ManualMove":
                            //    //    this.frame_content.Content = Manualv;
                            //    //    GlobalData.Current.SendTagEvent = mi.Tag.ToString();
                            //    //    break;
                            //    case "RMParameter":
                            //    case "RMPval":
                            //    case "AxisState":
                            //        this.frame_content.Content = parav;
                            //        GlobalData.Current.SendTagEvent = mi.Tag.ToString();
                            //        break;
                            //    case "FirmwareLog":
                            //        this.frame_content.Content = Logv;
                            //        GlobalData.Current.SendTagEvent = mi.Tag.ToString();
                            //        break;
                            //    case "AlarmLog":
                            //        this.frame_content.Content = AlarmLogv;
                            //        GlobalData.Current.SendTagEvent = mi.Tag.ToString();
                            //        break;
                            //    case "BCRLog":
                            //        this.frame_content.Content = BCRLogv;
                            //        GlobalData.Current.SendTagEvent = mi.Tag.ToString();
                            //        break;
                            //    case "HSMSLog":
                            //        this.frame_content.Content = HSMSLogv;
                            //        GlobalData.Current.SendTagEvent = mi.Tag.ToString();
                            //        break;
                            //    case "TransferLog":
                            //        this.frame_content.Content = TransferLogv;
                            //        GlobalData.Current.SendTagEvent = mi.Tag.ToString();
                            //        break;
                            //    //case "OperatorLog":
                            //    //  this.frame_content.Content = OperatorLogv;
                            //    //  GlobalData.Current.SendTagEvent = mi.Tag.ToString();
                            //    //  break;
                            //    //case "InformLog":
                            //    //  this.frame_content.Content = InformLogv;
                            //    //  GlobalData.Current.SendTagEvent = mi.Tag.ToString();
                            //    //  break;
                            //    //case "IOMonitor":
                            //    //    this.frame_content.Content = IOv;
                            //    //    GlobalData.Current.SendTagEvent = mi.Tag.ToString();
                            //    //    break;
                            //    //case "IOMonitorRM":
                            //    //    this.frame_content.Content = IORMv;
                            //    //    GlobalData.Current.SendTagEvent = mi.Tag.ToString();
                            //    //    break;
                            //    //case "WPSMonitor":
                            //    //    this.frame_content.Content = WPSv;
                            //    //    GlobalData.Current.SendTagEvent = mi.Tag.ToString();
                            //    //    break;
                            //    case "Ports":
                            //        //this.frame_content.Content = PortLayoutv;
                            //        //GlobalData.Current.SendTagEvent = mi.Tag.ToString();
                            //        break;
                            //    case "User":
                            //        this.frame_content.Content = userView;
                            //        GlobalData.Current.SendTagEvent = mi.Tag.ToString();
                            //        CheckAdminLogin();
                            //        break;
                            //    case "CarrierSearch"://220408 RGJ SerachView 추가.
                            //        //220624 HHJ SCS 개선     //- SearchView Popup으로 변경
                            //        //this.frame_content.Content = searchView;
                            //        //GlobalData.Current.SendTagEvent = mi.Tag.ToString();
                            //        if (searchView is null)
                            //        {
                            //            searchView = new SearchViewPopup(LayOutv);
                            //            searchView.Show();
                            //        }
                            //        else
                            //        {
                            //            if (!searchView.IsVisible)
                            //            {
                            //                searchView = null;
                            //                searchView = new SearchViewPopup(LayOutv);
                            //                searchView.Show();
                            //            }
                            //        }
                            //        break;

                            //    case "MapViewer"://SuHwan_20220420
                            //        GlobalData.Current.SendTagEvent = mi.Tag.ToString();
                            //        mapView.timer.Start();
                            //        mapView.isSelectkiosk = false;
                            //        this.frame_content.Content = mapView;
                            //        break;

                            //    case "SmokeDetect": //SuHwan_20220512 : 연기감지기 추가
                            //        GlobalData.Current.SendTagEvent = mi.Tag.ToString();
                            //        this.frame_content.Content = smokeDetectView;
                            //        break;

                            //    case "Login":
                            //        if (string.IsNullOrEmpty(LoginUserName))
                            //        {
                            //            LogInPopupView kw = new LogInPopupView();
                            //            kw.Height = this.ActualHeight;
                            //            kw.Width = this.ActualWidth;
                            //            kw.Owner = Application.Current.MainWindow; // We must also set the owner for this to work.
                            //            kw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                            //            kw.ShowDialog();
                            //        }
                            //        else
                            //        {
                            //            //GlobalData.Current.UserMng.Logout();

                            //            if (LoginUserPopupCount == 0)
                            //            {
                            //                popupMain.PlacementTarget = mi;
                            //                popupMain.Placement = PlacementMode.Bottom;
                            //                popupMain.IsOpen = true;
                            //                LoginUserPopupCount = 1;
                            //            }
                            //            else
                            //            {
                            //                popupMain.IsOpen = false;
                            //                LoginUserPopupCount = 0;
                            //            }
                            //        }
                            //        break;
                            //    case "AlarmManager":
                            //        this.frame_content.Content = AlarmManagerv;
                            //        GlobalData.Current.SendTagEvent = mi.Tag.ToString();
                            //        break;

                            //    case "Exit":
                            //        ExitFirmwareProgram();
                            //        break;

                            //    case "PlayBack":
                            //        //PlayBackDebug SRB = new PlayBackDebug(); //개발중 시연 보류
                            //        //SRB.Owner = Application.Current.MainWindow; // We must also set the owner for this to work.
                            //        //SRB.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                            //        //SRB.Show();
                            //        break;

                            //    //221226 HHJ SCS 개선
                            //    case "TerminalMessage":
                            //        this.frame_content.Content = terminalMsgView;
                            //        GlobalData.Current.SendTagEvent = mi.Tag.ToString();
                            //        break;
                            //    default:
                            //        break;
                            //}
                            #endregion

                            initMenuButton();
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show(string.Format(ex.ToString()));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }

        /// <summary>
        /// 네비게이션 탭 마우스 다운(페이지 이동)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NavigationTab_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Grid senderBuffer)
            {
                setNavigationTab(true, senderBuffer.Tag.ToString());
                PageChenge(senderBuffer.Tag.ToString(), dicNavigationTab[senderBuffer.Tag.ToString()].DisplayName, true);
            }
        }

        /// <summary>
        /// 네이게이션 탭 삭제
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NavigationTabDelete_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Grid senderBuffer)
            {
                setNavigationTab(false, senderBuffer.Tag.ToString());
            }
        }

        /// <summary>
        /// 페이지 이동 
        /// </summary>
        /// <param name="rcvTag">이동할 페이지 이름</param>
        private void PageChenge(string rcvTag, string TagContent = "", bool bNavigation = false)
        {
            bool IsPopupMenu = false;

            if (rcvTag != null)
            {
                try
                {
                    //setNavigationTab(true, mi.Tag.ToString(), mi.DisplayName.ToString());

                    switch (rcvTag)
                    {
                        case "MainPage":
                            if ("MainPage" == GlobalData.Current.SendTagEvent) break;
                            LodingPopup.Instance.Start();
                            LodingPopup.Instance.setProgressValue(30, "PrintMainv Loading...");
                            Dispatcher.Invoke(new Action(() => {this.frame_content.Content = PrintMainv;}), DispatcherPriority.ContextIdle);
                            GlobalData.Current.SendTagEvent = rcvTag;
                            break;
                        case "AlarmManager":
                            this.frame_content.Content = AlarmManagerv;
                            GlobalData.Current.SendTagEvent = rcvTag;
                            IsPopupMenu = true;
                            break;
                        //220621 HHJ SCS 개선     //- ConfigPage 추가
                        case "Config":
                            this.frame_content.Content = configView;
                            GlobalData.Current.SendTagEvent = rcvTag;
                            IsPopupMenu = true;
                            break;
                        //case "MapViewer"://SuHwan_20220420
                        //    GlobalData.Current.SendTagEvent = rcvTag;
                        //    mapView.timer.Start();
                        //    mapView.isSelectkiosk = false;
                        //    this.frame_content.Content = mapView;
                        //    IsPopupMenu = true;
                        //    break;
                        //case "CarrierSearch"://220408 RGJ SerachView 추가. //220624 HHJ SCS 개선     //- SearchView Popup으로 변경
                        //    if (searchView is null)
                        //    {
                        //        searchView = new SearchViewPopup(LayOutv);
                        //        searchView.Show();
                        //    }
                        //    else
                        //    {
                        //        if (!searchView.IsVisible)
                        //        {
                        //            searchView = null;
                        //            searchView = new SearchViewPopup(LayOutv);
                        //            searchView.Show();
                        //        }
                        //    }
                        //    IsPopupMenu = true;
                        //    break;
                        //221226 HHJ SCS 개선
                        case "IOMonitor":
                            this.frame_content.Content = IOv;
                            GlobalData.Current.SendTagEvent = rcvTag;
                            IsPopupMenu = true;
                            break;
                        case "StoredCarrier":
                            this.frame_content.Content = StoredCarrierv;
                            GlobalData.Current.SendTagEvent = rcvTag;
                            IsPopupMenu = true;
                            break;
                        case "AlarmLog":
                            this.frame_content.Content = AlarmLogv;
                            GlobalData.Current.SendTagEvent = rcvTag;
                            IsPopupMenu = true;
                            break;
                        case "BCRLog":
                            this.frame_content.Content = BCRLogv;
                            GlobalData.Current.SendTagEvent = rcvTag;
                            IsPopupMenu = true;
                            break;
                        case "HSMSLog":
                            this.frame_content.Content = HSMSLogv;
                            GlobalData.Current.SendTagEvent = rcvTag;
                            IsPopupMenu = true;
                            break;
                        case "OperatorLog":
                            this.frame_content.Content = OperatorLogv;
                            GlobalData.Current.SendTagEvent = rcvTag;
                            IsPopupMenu = true;
                            break;
                        case "TransferLog":
                            this.frame_content.Content = TransferLogv;
                            GlobalData.Current.SendTagEvent = rcvTag;
                            IsPopupMenu = true;
                            break;
                        //case "UtilizationLog":
                        //    this.frame_content.Content = UtilizationLogv;
                        //    GlobalData.Current.SendTagEvent = rcvTag;
                        //    IsPopupMenu = true;
                        //    break;

                        case "Alarm":
                            this.frame_content.Content = Alarmv;
                            GlobalData.Current.SendTagEvent = rcvTag;
                            //IsPopupMenu = true;
                            break;
                        case "Login":
                            if (string.IsNullOrEmpty(LoginUserName))
                            {
                                LogInPopupView kw = new LogInPopupView();
                                kw.Height = this.ActualHeight;
                                kw.Width = this.ActualWidth;
                                kw.Owner = Application.Current.MainWindow; // We must also set the owner for this to work.
                                kw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                                kw.Show();
                            }
                            else
                            {
                                //GlobalData.Current.UserMng.Logout();

                                if (LoginUserPopupCount == 0)
                                {
                                    popupMain.PlacementTarget = UserBtn;
                                    popupMain.Placement = PlacementMode.Bottom;
                                    popupMain.IsOpen = true;
                                    LoginUserPopupCount = 1;
                                }
                                else
                                {

                                    if (!popupMain.IsOpen)
                                    {
                                        popupMain.PlacementTarget = UserBtn;
                                        popupMain.Placement = PlacementMode.Bottom;
                                        popupMain.IsOpen = true;
                                    }
                                    else
                                    {
                                        popupMain.IsOpen = false;
                                    }
                                }
                            }
                            break;
                        case "User":
                            this.frame_content.Content = userView;
                            GlobalData.Current.SendTagEvent = rcvTag;
                            CheckAdminLogin();
                            break;
                        case "EXIT":
                            LogManager.WriteOperatorLog(string.Format("사용자가 {0} 화면을 Open하였습니다.", rcvTag),
                                "POPUP", rcvTag, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, LoginUserID, 1,
                                rcvTag);
                            ExitFirmwareProgram();
                            break;

                        case "RMParameter":
                        case "RMPval":
                        case "AxisState":
                            this.frame_content.Content = parav;
                            GlobalData.Current.SendTagEvent = rcvTag;
                            break;
                        case "FirmwareLog":
                            this.frame_content.Content = Logv;
                            GlobalData.Current.SendTagEvent = rcvTag;
                            break;
                        case "Ports":
                            break;
                        default:
                            break;
                    }

                    if (IsPopupMenu)
                    {
                        string menuname = string.Empty;

                        if (Menu_Config.IsSelect == true)
                        {
                            menuname = Menu_Config.TagName.ToString();
                        }
                        else if (Menu_Monitoring.IsSelect == true)
                        {
                            menuname = Menu_Monitoring.TagName.ToString();
                        }
                        else if (Menu_Log.IsSelect == true)
                        {
                            menuname = Menu_Log.TagName.ToString();
                        }
                        else if (bNavigation)
                        {
                            menuname = "Navigation Tab";
                        }

                        LogManager.WriteOperatorLog(string.Format("사용자가 {0} 의 {1} Menu Open하였습니다.", menuname, rcvTag),
                            "MENU", rcvTag, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, LoginUserID, 18,
                            menuname, rcvTag);
                    }
                    else
                    {
                        if (rcvTag == "MainPage" && string.IsNullOrEmpty(TagContent))
                        {
                            bMovingMap = true;
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Page moving to {0}.", GlobalData.Current.EQPID);
                        }
                        else
                        {
                            if (bMovingMap)
                            {
                                bMovingMap = false;
                                LogManager.WriteConsoleLog(eLogLevel.Info, "Page moving to {0} has completed.", GlobalData.Current.EQPID);
                            }
                            else
                            {
                                //string testtext = "text={0}";
                                //Log.LogManager.WriteOperatorLog(testtext, GlobalData.Current.EQPID);
                                if (rcvTag != "EXIT")
                                {
                                    LogManager.WriteOperatorLog(string.Format("사용자가 {0} 화면을 Open하였습니다.", rcvTag),
                                        "POPUP", rcvTag, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, LoginUserID, 1,
                                        rcvTag);
                                }
                            }
                        }
                    }
                    initMenuButton();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(string.Format(ex.ToString()));
                }
            }
        }

        List<string> panelTag = new List<string>();//네비게이션탭 언어변경용 태그저장
        /// <summary>
        /// 네이게이션 탭 생성 및 삭제
        /// </summary>
        /// <param name="rcvIsCreate">true = 생성 / false = 삭제</param>
        /// <param name="rcvTag">테그</param>
        private void setNavigationTab(bool rcvIsCreate, string rcvTag)
        {
            if (rcvIsCreate)
            {
                foreach (var item in dicNavigationTab.Values)
                {
                    item.IsSelect = false;
                }

                if (dicNavigationTab.ContainsKey(rcvTag))
                {
                    dicNavigationTab[rcvTag].IsSelect = true;
                    return;
                }

                //아이콘 생성
                string iconPath = string.Empty;
                switch (rcvTag)
                {
                    case "MainPage":
                        iconPath = "M575.8 255.5C575.8 273.5 560.8 287.6 543.8 287.6H511.8L512.5 447.7C512.5 450.5 512.3 453.1 512 455.8V472C512 494.1 494.1 512 472 512H456C454.9 512 453.8 511.1 452.7 511.9C451.3 511.1 449.9 512 448.5 512H392C369.9 512 352 494.1 352 472V384C352 366.3 337.7 352 320 352H256C238.3 352 224 366.3 224 384V472C224 494.1 206.1 512 184 512H128.1C126.6 512 125.1 511.9 123.6 511.8C122.4 511.9 121.2 512 120 512H104C81.91 512 64 494.1 64 472V360C64 359.1 64.03 358.1 64.09 357.2V287.6H32.05C14.02 287.6 0 273.5 0 255.5C0 246.5 3.004 238.5 10.01 231.5L266.4 8.016C273.4 1.002 281.4 0 288.4 0C295.4 0 303.4 2.004 309.5 7.014L564.8 231.5C572.8 238.5 576.9 246.5 575.8 255.5L575.8 255.5z";
                        break;
                    case "AlarmManager":
                        iconPath = "M256,32 L256,49.88 C328.5,61.39 384,124.2 384,200 L384,233.4 C384,278.8 399.5,322.9 427.8,358.4 L442.7,377 C448.5,384.2 449.6,394.1 445.6,402.4 441.6,410.7 433.2,416 424,416 L24,416 C14.77,416 6.365,410.7 2.369,402.4 -1.628,394.1 -0.504,384.2 5.26,377 L20.17,358.4 C48.54,322.9 64,278.8 64,233.4 L64,200 C64,124.2 119.5,61.39 192,49.88 L192,32 C192,14.33 206.3,0 224,0 241.7,0 256,14.33 256,32 L256,32 z M288,448 C288,464.1 281.3,481.3 269.3,493.3 257.3,505.3 240.1,512 224,512 207,512 190.7,505.3 178.7,493.3 166.7,481.3 160,464.1 160,448 L288,448 z";
                        break;
                    case "Config":
                        iconPath = "M495.9 166.6c3.2 8.7 .5 18.4-6.4 24.6l-43.3 39.4c1.1 8.3 1.7 16.8 1.7 25.4s-.6 17.1-1.7 25.4l43.3 39.4c6.9 6.2 9.6 15.9 6.4 24.6c-4.4 11.9-9.7 23.3-15.8 34.3l-4.7 8.1c-6.6 11-14 21.4-22.1 31.2c-5.9 7.2-15.7 9.6-24.5 6.8l-55.7-17.7c-13.4 10.3-28.2 18.9-44 25.4l-12.5 57.1c-2 9.1-9 16.3-18.2 17.8c-13.8 2.3-28 3.5-42.5 3.5s-28.7-1.2-42.5-3.5c-9.2-1.5-16.2-8.7-18.2-17.8l-12.5-57.1c-15.8-6.5-30.6-15.1-44-25.4L83.1 425.9c-8.8 2.8-18.6 .3-24.5-6.8c-8.1-9.8-15.5-20.2-22.1-31.2l-4.7-8.1c-6.1-11-11.4-22.4-15.8-34.3c-3.2-8.7-.5-18.4 6.4-24.6l43.3-39.4C64.6 273.1 64 264.6 64 256s.6-17.1 1.7-25.4L22.4 191.2c-6.9-6.2-9.6-15.9-6.4-24.6c4.4-11.9 9.7-23.3 15.8-34.3l4.7-8.1c6.6-11 14-21.4 22.1-31.2c5.9-7.2 15.7-9.6 24.5-6.8l55.7 17.7c13.4-10.3 28.2-18.9 44-25.4l12.5-57.1c2-9.1 9-16.3 18.2-17.8C227.3 1.2 241.5 0 256 0s28.7 1.2 42.5 3.5c9.2 1.5 16.2 8.7 18.2 17.8l12.5 57.1c15.8 6.5 30.6 15.1 44 25.4l55.7-17.7c8.8-2.8 18.6-.3 24.5 6.8c8.1 9.8 15.5 20.2 22.1 31.2l4.7 8.1c6.1 11 11.4 22.4 15.8 34.3zM256 336c44.2 0 80-35.8 80-80s-35.8-80-80-80s-80 35.8-80 80s35.8 80 80 80z";
                        break;
                    case "MapViewer":
                        iconPath = "M408 120c0 54.6-73.1 151.9-105.2 192c-7.7 9.6-22 9.6-29.6 0C241.1 271.9 168 174.6 168 120C168 53.7 221.7 0 288 0s120 53.7 120 120zm8 80.4c3.5-6.9 6.7-13.8 9.6-20.6c.5-1.2 1-2.5 1.5-3.7l116-46.4C558.9 123.4 576 135 576 152V422.8c0 9.8-6 18.6-15.1 22.3L416 503V200.4zM137.6 138.3c2.4 14.1 7.2 28.3 12.8 41.5c2.9 6.8 6.1 13.7 9.6 20.6V451.8L32.9 502.7C17.1 509 0 497.4 0 480.4V209.6c0-9.8 6-18.6 15.1-22.3l122.6-49zM327.8 332c13.9-17.4 35.7-45.7 56.2-77V504.3L192 449.4V255c20.5 31.3 42.3 59.6 56.2 77c20.5 25.6 59.1 25.6 79.6 0zM288 152c22.1 0 40-17.9 40-40s-17.9-40-40-40s-40 17.9-40 40s17.9 40 40 40z";
                        break;
                    case "AlarmLog":
                    case "BCRLog":
                    case "HSMSLog":
                    case "OperatorLog":
                    case "TransferLog":
                    case "UtilizationLog":
                        iconPath = "M64 0C28.7 0 0 28.7 0 64V448c0 35.3 28.7 64 64 64H320c35.3 0 64-28.7 64-64V160H256c-17.7 0-32-14.3-32-32V0H64zM256 0V128H384L256 0zM112 256H272c8.8 0 16 7.2 16 16s-7.2 16-16 16H112c-8.8 0-16-7.2-16-16s7.2-16 16-16zm0 64H272c8.8 0 16 7.2 16 16s-7.2 16-16 16H112c-8.8 0-16-7.2-16-16s7.2-16 16-16zm0 64H272c8.8 0 16 7.2 16 16s-7.2 16-16 16H112c-8.8 0-16-7.2-16-16s7.2-16 16-16z";
                        break;

                    case "PlayBack":
                        iconPath = "M0 128C0 92.7 28.7 64 64 64H320c35.3 0 64 28.7 64 64V384c0 35.3-28.7 64-64 64H64c-35.3 0-64-28.7-64-64V128zM559.1 99.8c10.4 5.6 16.9 16.4 16.9 28.2V384c0 11.8-6.5 22.6-16.9 28.2s-23 5-32.9-1.6l-96-64L416 337.1V320 192 174.9l14.2-9.5 96-64c9.8-6.5 22.4-7.2 32.9-1.6z";
                        break;

                    default://없으면 나가버려
                        return;
                }

                //버튼생성
                var buttonBuffer = new SK_ButtonControl
                {
                    Style = Resources["NavigationTab"] as Style,
                    TagName = rcvTag,
                    DisplayName = TranslationManager.Instance.Translate(rcvTag.ToString()).ToString(),
                    ImageMargin = new Thickness(8),
                    IconFill = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF4B494A"),
                    Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#FF4B494A"),
                    MouseOverColor = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFF6F6F6"),
                    SelectColor = Brushes.White,
                    Name = rcvTag,
                    PathData = iconPath,
                    IsSelect = true,
                };

                dicNavigationTab.Add(rcvTag, buttonBuffer);
                dockpanelNavigation.Children.Add(buttonBuffer);
                panelTag.Add(rcvTag);
            }
            else
            {
                var buttonBuffer = dicNavigationTab[rcvTag];
                dockpanelNavigation.Children.Remove(buttonBuffer);
                dicNavigationTab.Remove(rcvTag);
                panelTag.Remove(rcvTag);
            }

        }

        #region - MainWindow Tray -
        // 2020.11.11 RGJ MainWindow Tray 기능추가.
        public System.Windows.Forms.NotifyIcon TrayIcon;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TrayIcon = new System.Windows.Forms.NotifyIcon();

            string iconpath = GlobalData.Current.CurrentFilePaths(System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName)) + "\\image\\SCSIcon_Tray.ico";

            FileInfo File = new FileInfo(iconpath);
            if (!File.Exists)
            {
                string temppath = GlobalData.Current.ImagePathChange(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName + "\\image\\SCSIcon_Tray.ico", "\\image\\SCSIcon_Tray.ico");
                iconpath = temppath;
            }
            TrayIcon.Icon = new System.Drawing.Icon(iconpath);
            TrayIcon.Visible = true;

            if (GlobalData.Current.ServerClientType == eServerClientType.Server)
            {
                TrayIcon.Text = "SCS SERVER";
            }
            else
            {
                TrayIcon.Text = "SCS CLIENT";
            }

            TrayIcon.DoubleClick += TrayIcon_DoubleClick;


            System.Windows.Forms.ContextMenu TrayContextMenu = new System.Windows.Forms.ContextMenu();
            System.Windows.Forms.MenuItem MenuItemOpen = new System.Windows.Forms.MenuItem();
            MenuItemOpen.Index = 0;
            MenuItemOpen.Text = "Show SCS";    // menu 이름
            MenuItemOpen.Click += ItemOpen_Click;
            TrayContextMenu.MenuItems.Add(MenuItemOpen);
            TrayIcon.ContextMenu = TrayContextMenu;
            //x버튼 삭제
            //var hwnd = new WindowInteropHelper(this).Handle;
            //SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
            initMenuButton();
        }

        private void ItemOpen_Click(object sender, EventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                ShowMainWindow();
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }

        private void TrayIcon_DoubleClick(object sender, EventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                ShowMainWindow();
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }
        public void ShowMainWindow()
        {
            Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
            {
                this.WindowState = WindowState.Maximized;
                this.Focus();
                this.Show();
                this.Activate(); //창 활성화 코드 추가.
            }));

        }
        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState.Minimized.Equals(WindowState))
            {
                ////231106 RGJ 클라이언트 작업표시줄에 나와야 함. 조범석 매니저 요청.
                //if (GlobalData.Current.ServerClientType == eServerClientType.Server)
                //{
                //    this.Hide();
                //}
                
            }
            base.OnStateChanged(e);
        }

        #endregion

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
            base.OnClosing(e);
        }
        private void Executed_Close(object sender, ExecutedRoutedEventArgs e)
        {
            ExitFirmwareProgram();
        }

        private void CanExecute_Close(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void ExitFirmwareProgram()
        {
            string msg = string.Empty;
            //SuHwan_20230320 : 메시지 박스 통합
            msg = TranslationManager.Instance.Translate("프로그램을 종료 하시겠습니까?").ToString();
            MessageBoxPopupView msgbox = new MessageBoxPopupView(TranslationManager.Instance.Translate("EXIT").ToString().ToUpper(), "", msg, "", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            CustomMessageBoxResult mBoxResult = msgbox.ShowResult();

            //MessageBoxResult result = System.Windows.MessageBox.Show("프로그램을 종료 하시겠습니까?", "Info Message", MessageBoxButton.YesNo); //종료버튼에 캔슬 필요없어서 삭제.
            switch (mBoxResult.Result)
            {
                case MessageBoxResult.Yes:
                    TrayIcon.Dispose();     //2024.05.31 lim, OY merge
                    LogManager.WriteConsoleLog(eLogLevel.Info, "SCS Program will be terminated by Operator");
                    //230801 클라이언트 스케줄러 시작할 때 접속했다는 정보를 지운다.
                    if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                    {
                        GlobalData.Current.DBManager.DbSetProcedureConnectClientInfo(true);
                    }
                    else
                    {
                        GlobalData.Current.protocolManager.Close();
                        GlobalData.Current.DBManager.DbSetProcedureEQPInfo(0, 0, 0, 0);
                    }
                    GlobalData.Current.ReleaseGlobalResource();     //221019 조숭진 클라이언트용 상태변경
                    Environment.Exit(0);
                    break;
                case MessageBoxResult.No:
                    break;
            }
        }

        //상단 RM 스피드 버튼
        private void btnRMSpeed_MouseDown(object sender, MouseButtonEventArgs e)
        {
            RobotSpeedPopupView kw = new RobotSpeedPopupView();
            kw.Owner = Application.Current.MainWindow; // We must also set the owner for this to work.
            kw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            kw.ShowDialog();
        }

        //상단 RM 커넥트 버튼
        private void btnRMCennect_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Grid)
            {
                Grid btn = (Grid)sender;
                if (btn != null)
                {
                    MessageBoxResult result = MessageBox.Show(btn.Tag.ToString() + TranslationManager.Instance.Translate("Do you want to reconnect a Crane?").ToString(),
                                                              TranslationManager.Instance.Translate("Confirmation").ToString(), MessageBoxButton.YesNoCancel);
                    if (result == MessageBoxResult.Yes)
                    {
                        // Yes code here  
                        //210105 lsj PMacOnlineConncet -> RobotOnlineConncet 변경
                        //210115 lsj Connet 함수 변경
                        //GlobalData.Current.mRMManager[btn.Tag.ToString()].RobotOnlineConncet = GlobalData.Current.mRMManager[btn.Tag.ToString()].pmac.PacmConnect(GlobalData.Current.mRMManager[btn.Tag.ToString()].RMParameter.IPAddress);
                        GlobalData.Current.mRMManager[btn.Tag.ToString()].RobotOnlineConncet = GlobalData.Current.mRMManager[btn.Tag.ToString()].ConnectRM(GlobalData.Current.mRMManager[btn.Tag.ToString()].RMIp, GlobalData.Current.mRMManager[btn.Tag.ToString()].RMPort);
                        if (GlobalData.Current.mRMManager[btn.Tag.ToString()].RobotOnlineConncet)
                        {
                            //IblInfo.Content = "PMac Connect Success.192.168.0.200";
                            //210115 lsj RMParameter.IPAddress->RMIp로 변경
                            //GlobalData.Current.SendMessageEvent = string.Format("PMac Connect Success" + GlobalData.Current.mRMManager[btn.Tag.ToString()].RMParameter.IPAddress);
                            GlobalData.Current.SendMessageEvent = string.Format("PMac Connect Success" + GlobalData.Current.mRMManager[btn.Tag.ToString()].RMIp);
                        }
                        else
                        {
                            //210115 lsj RMParameter.IPAddress->RMIp로 변경
                            //GlobalData.Current.SendMessageEvent = string.Format("PMac Connect Fail" + GlobalData.Current.mRMManager[btn.Tag.ToString()].RMParameter.IPAddress);
                            GlobalData.Current.SendMessageEvent = string.Format("PMac Connect Fail" + GlobalData.Current.mRMManager[btn.Tag.ToString()].RMIp);
                        }
                    }
                }
            }


        }


        /// <summary>
        /// // 2020.11.06 SimulationAlarm발생용
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnAlarm_Click(object sender, RoutedEventArgs e)
        {
            if (GlobalData.Current.siumlationAlarm)
                GlobalData.Current.siumlationAlarm = false;
            else
                GlobalData.Current.siumlationAlarm = true;
        }

        private void btnBoothState_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                BoothStateChangePopupView kw = new BoothStateChangePopupView();
                kw.Owner = Application.Current.MainWindow; // We must also set the owner for this to work.
                kw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                kw.ShowDialog();
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }

        }



        //도움말기능추가 : 임광일 201113
        private void Search_Click(object sender, RoutedEventArgs e)
        {
            string recall = System.IO.Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + "\\HELP\\HELP.chm"; // 201113 도움말파일 실행경로 불러오기
            recall = recall.Replace("\\bin\\Debug", "");
            Process.Start(recall);
        }

        private void image_AlarmState_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (GlobalData.Current.Scheduler == null)
            {
                return;
            }
            else if (GlobalData.Current.Scheduler.GetStartState())
            {
                string strMessage = string.Format(TranslationManager.Instance.Translate("스케쥴러 비활성화 메시지").ToString(), GlobalData.Current.Scheduler.GetSchedulerName());
                MessageBoxResult result = System.Windows.MessageBox.Show(strMessage, TranslationManager.Instance.Translate("Info Message").ToString(), MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    GlobalData.Current.Scheduler.StopScheduler();
                }
            }
            else
            {
                string strMessage = string.Format(TranslationManager.Instance.Translate("스케쥴러 활성화 메시지").ToString(), GlobalData.Current.Scheduler.GetSchedulerName());
                MessageBoxResult result = System.Windows.MessageBox.Show(strMessage, TranslationManager.Instance.Translate("Info Message").ToString(), MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    GlobalData.Current.Scheduler.StartScheduler();
                }
            }
        }

        //버튼 통합 마우스 엔터
        private void Button_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is Button)
            {

            }
        }

        //버튼 통합 마우스 리브
        private void Button_MouseLeave(object sender, MouseEventArgs e)
        {
            if (sender is Button)
            {

            }
        }

        private void borderThemeColorChangeButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            isThemeColor = (isThemeColor == true) ? false : true;

            if (isThemeColor == true)
            {
                GlobalData.Current.GuiColor.setThemeColor(eThemeColor.LIGHT);
            }
            else
            {
                GlobalData.Current.GuiColor.setThemeColor(eThemeColor.DARK);
            }


            _EventCall_ThemeColorChange();


            //LayOutv.ThemeChange();      //220322 HHJ SCS 개발     //- DataGrid Header Style 변경
        }

        private void label_Alarm_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ScheduleDebugInfo SRB = new ScheduleDebugInfo();
            SRB.Owner = Application.Current.MainWindow; // We must also set the owner for this to work.
            SRB.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            SRB.Show();
        }

        /// <summary>
        /// 언어 설정 메뉴 오픈인지 확인
        /// </summary>
        bool isOpenLanguageChange = false;
        /// <summary>
        /// 언어 설정 버튼 클릭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LanguageChange_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                if (sender is SK_ButtonControl senderBuffer)
                {
                    string tagBuffer = senderBuffer.Tag.ToString();
                    switch (tagBuffer)
                    {
                        case "KR":
                        case "US":
                        case "HU":
                        case "CN":
                            LanguageChange(tagBuffer);
                            ellipseSelectLanguage.ImagePath = senderBuffer.ImagePath;
                            ellipseSelectLanguage.DisplayName = senderBuffer.DisplayName;

                            LogManager.WriteOperatorLog(string.Format("사용자가 언어를 {0} 으로 변경하였습니다.", tagBuffer),
                                "POPUP", tagBuffer, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, LoginUserID, 19,
                                tagBuffer);
                            break;

                        case "Select":
                            break;
                    }

                    Storyboard storyboardBuffer = null;
                    if (isOpenLanguageChange)
                    {
                        storyboardBuffer = Resources["storyboardCloseLanguageChangeMenu"] as Storyboard;
                        isOpenLanguageChange = false;
                    }
                    else
                    {
                        storyboardBuffer = Resources["storyboardOpenLanguageChangeMenu"] as Storyboard;
                        isOpenLanguageChange = true;
                    }

                    storyboardBuffer.Begin();
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
            
        }

        public bool LanguageChange_state = true; //초기 클라이언트 상태바 초기화를 위해 True 로 초기화

        /// <summary>
        /// 언어 변경 하는곳
        /// </summary>
        /// <param name="rcvTag">국가이름 약자(KR,US,HU,CN)</param>
        private void LanguageChange(string rcvTag)
        {
            //switch (rcvTag)
            //{
            //    case "KR":
            //        TranslationManager.Instance.CurrentLanguage = new CultureInfo("ko-KR");
            //        break;
            //    case "US":
            //        TranslationManager.Instance.CurrentLanguage = new CultureInfo("en-US");
            //        break;
            //    case "HU":
            //        TranslationManager.Instance.CurrentLanguage = new CultureInfo("hu-HU");
            //        break;
            //    case "CN":
            //        TranslationManager.Instance.CurrentLanguage = new CultureInfo("zh-CN");
            //        break;
            //}

            string key = string.Empty;
            switch (rcvTag)
            {
                case "KR":
                    key = "ko-KR";
                    break;
                case "US":
                    key = "en-US";
                    break;
                case "CN":
                    key = "zh-CN";
                    break;
                case "HU":
                    key = "hu-HU";
                    break;
            }

            CultureInfo changeCulture = new CultureInfo(key);
            TranslationManager.Instance.CurrentLanguage = changeCulture;
            UIEventCollection.Instance.InvokerControlSelectionChanged(key);
            //Thread.CurrentThread.CurrentCulture = changeCulture;
            //Thread.CurrentThread.CurrentUICulture = changeCulture;

            LanguageChange_state = true;

            _EventCall_ChangeLanguage();

            if (panelTag.Count != 0)
            {
                int index = panelTag.Count;
                string buf;
                for (int i = 0; i < index; i++)
                {
                    buf = panelTag[0];
                    setNavigationTab(false, panelTag[0]);
                    setNavigationTab(true, buf);
                    
                }
            }

            if (this.WindowState == WindowState.Normal)
            {
                btnMax.ToolTip = TranslationManager.Instance.Translate("이전 크기로 복원").ToString();
            }
            else
            {
                btnMax.ToolTip = TranslationManager.Instance.Translate("최대화").ToString();
            }
        }

        //보더 버튼 통합 마우스 다운
        private void BorderButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border senderBuffer)
            {

                switch (senderBuffer.Tag.ToString())
                {
                    case "Login":
                        if (string.IsNullOrEmpty(LoginUserName))
                        {
                            LogInPopupView kw = new LogInPopupView();
                            kw.Height = this.ActualHeight;
                            kw.Width = this.ActualWidth;
                            kw.Owner = Application.Current.MainWindow; // We must also set the owner for this to work.
                            kw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                            kw.ShowDialog();
                        }
                        else
                        {
                            GlobalData.Current.UserMng.Logout();
                        }
                        break;
                }
            }
        }

        //상단 메뉴 버튼 마우스 엔터
        private void MenuButton_MouseEnter(object sender, MouseEventArgs e)
        {
            if (sender is SK_ButtonControl senderBuffer)
            {
                //switch (senderBuffer.TagName)
                //{
                //    case "Config":
                //        Menu_Config.Focus();
                //        break;

                //    case "Monitoring":
                //        Menu_Monitoring.Focus();
                //        break;
                //    case "Log":
                //        Menu_Log.Focus();
                //        break;

                //    case "PlayBack":
                //        Menu_PlayBack.Focus();
                //        break;

                //    case "TestMode":
                //        Menu_TestMode.Focus();
                //        break;
                //}
            }
            else
            {
                //if (isMainSubMenuOpen)
                //{
                //    isMainSubMenuLostFocus = true; //YSW_MainSubMenu 포커스 변경전까지 고정_20221017
                //    if (isBackLostFocus)
                //    {
                //        switch (isLostFocusTag)
                //        {
                //            case "Config":
                //                Menu_Config.Focus();
                //                break;

                //            case "Monitoring":
                //                Menu_Monitoring.Focus();
                //                break;
                //            case "Log":
                //                Menu_Log.Focus();
                //                break;

                //            case "PlayBack":
                //                Menu_PlayBack.Focus();
                //                break;

                //            case "TestMode":
                //                Menu_TestMode.Focus();
                //                break;
                //        }
                //        isBackLostFocus = false;
                //    }

                //    //setBeginStoryboard("CloseMenu");
                //    //isMainSubMenuOpen = false;
                //    //_selectMenuButton.IsSelect = false;
                //}
            }
        }

        private void setBeginStoryboard(string rcvKeyName, string rcvTarget)
        {

            Storyboard storyboardBuffer = new Storyboard();
            ThicknessAnimation thicknessAnimation = new ThicknessAnimation { Duration = TimeSpan.FromSeconds(0.3) };

            if (rcvKeyName == "CloseMenu")
            {
                storyboardBuffer.Completed += Storyboard_CloseCompleted;
            }

            switch (rcvTarget)
            {
                case "Config":
                    thicknessAnimation.To = (rcvKeyName == "OpenMenu") ? new Thickness(0) : new Thickness(1, -MenuConfig_SubMenu.ActualHeight, 1, 0);
                    Storyboard.SetTarget(storyboardBuffer, MenuConfig_SubMenu);
                    break;
                case "Monitoring":
                    thicknessAnimation.To = (rcvKeyName == "OpenMenu") ? new Thickness(0) : new Thickness(1, -MenuMonitoring_SubMenu.ActualHeight, 1, 0);
                    Storyboard.SetTarget(storyboardBuffer, MenuMonitoring_SubMenu);
                    break;
                case "Log":
                    thicknessAnimation.To = (rcvKeyName == "OpenMenu") ? new Thickness(0) : new Thickness(1, -MenuLog_SubMenu.ActualHeight, 1, 0);
                    Storyboard.SetTarget(storyboardBuffer, MenuLog_SubMenu);
                    break;
            }

            storyboardBuffer.Children.Add(thicknessAnimation);
            Storyboard.SetTargetProperty(storyboardBuffer, new PropertyPath("Margin"));
            storyboardBuffer.Begin();
        }




        private void Storyboard_CloseCompleted(object sender, EventArgs e)
        {

            //Storyboard sb = (Storyboard)sender;

            var targetBuffer = Storyboard.GetTarget((sender as ClockGroup).Timeline);
            var DragDrop = (targetBuffer as Border).Name;

            switch (DragDrop)
            {
                case "MenuConfig_SubMenu":
                    Menu_Config.IsSelect = false;
                    break;
                case "MenuMonitoring_SubMenu":
                    Menu_Monitoring.IsSelect = false;
                    break;
                case "MenuLog_SubMenu":
                    Menu_Log.IsSelect = false;
                    break;
            }
        }

        //221226 HHJ SCS 개선
        //public void AddTerminalMessage(string TMsg, bool FromHost)
        //{
        //    if (FromHost) //MCS 에서 터미널 메시지 수신
        //    {
        //        Log.LogManager.WriteConsoleLog(eLogLevel.Info, "H->E {0}", TMsg);
        //        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
        //        {
        //            LayOutv.AddTeminalMessage("[H->E] " + TMsg);
        //        }));
        //    }
        //    else   //SCS 에서 터미널 메시지 발신
        //    {
        //        Log.LogManager.WriteConsoleLog(eLogLevel.Info, "E->H {0}", TMsg);
        //        Dispatcher.Invoke(DispatcherPriority.Normal, new Action(delegate
        //        {
        //            LayOutv.AddTeminalMessage("[E->H] " + TMsg);
        //        }));
        //    }
        //}

        //20240205 RGJ MCS SCS 상태바 클릭시 동작 변경 CCS 와 동일하게 작동하도록 함(조범석 매니저 요청)
        private void btnSCSState_Click(object sender, MouseButtonEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                string head, msg1, msg2;
                MessageBoxPopupView msgbox = null;

                eOnlineState onlineState = eOnlineState.Offline_EQ;

                LogManager.WriteOperatorLog(string.Format("사용자가 {0} 화면을 Open하였습니다.", "MCS On/Offline State Change"),
                    "POPUP", "MCS On/Offline State Change", GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, LoginUserID, 1,
                    "MCS On/Offline State Change");

                if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                {
                    if (GlobalData.Current.EQPList.Where(v => v.MCS_State != "0" && v.EQPID == GlobalData.Current.EQPID).Count() != 0)     //연결상태가 연결해제일때는 아무것도 하지 않는다.
                    {
                        string stronlineState = GlobalData.Current.EQPList.FirstOrDefault(d => d.EQPID == GlobalData.Current.EQPID).SCS_State;
                        //onlineState = (eOnlineState)Enum.Parse(typeof(eOnlineState), stronlineState);

                        if (stronlineState == "1")  //Online 에서 Offline 으로
                        {
                            onlineState = eOnlineState.Offline_EQ; //요청할 상태저장
                            head = TranslationManager.Instance.Translate("MCS On/Offline State Change").ToString();
                            msg1 = TranslationManager.Instance.Translate("Are you sure you want to").ToString();
                            msg2 = "MCS" + TranslationManager.Instance.Translate("오프라인").ToString() + "?";

                        }
                        else //Offline  에서 Online 으로
                        {
                            onlineState = eOnlineState.Remote; //요청할 상태저장
                            head = TranslationManager.Instance.Translate("MCS On/Offline State Change").ToString();
                            msg1 = TranslationManager.Instance.Translate("Are you sure you want to").ToString();
                            msg2 = "MCS" + TranslationManager.Instance.Translate("온라인").ToString() + "?";
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                else //Server
                {
                    if (!GlobalData.Current.HSMS.Enabled) //소켓 닫혀있으면 의미 없음
                    {
                        return;
                    }
                    if (GlobalData.Current.MainBooth.CurrentOnlineState == eOnlineState.Remote) //리모트 
                    {
                        head = TranslationManager.Instance.Translate("MCS On/Offline State Change").ToString();
                        msg1 = TranslationManager.Instance.Translate("Are you sure you want to").ToString();
                        msg2 = "MCS" + TranslationManager.Instance.Translate("오프라인").ToString() + "?";
                    }
                    else
                    {
                        head = TranslationManager.Instance.Translate("MCS On/Offline State Change").ToString();
                        msg1 = TranslationManager.Instance.Translate("Are you sure you want to").ToString();
                        msg2 = "MCS" + TranslationManager.Instance.Translate("온라인").ToString() + "?";
                    }
                }

                msgbox = new MessageBoxPopupView(head, msg1, msg2, "", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                CustomMessageBoxResult mBoxResult = msgbox.ShowResult();

                if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                {
                    if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                    {
                        //요청할 상태를 저장했으므로 삭제
                        //if (onlineState == eOnlineState.Remote) //리모트 
                        //{
                        //    onlineState = eOnlineState.Offline_EQ;
                        //}
                        //else
                        //{
                        //    onlineState = eOnlineState.Remote;
                        //}
                        ClientSetProcedure("MCSState", onlineState.ToString());
                    }
                    else
                    {
                        if (GlobalData.Current.MainBooth.CurrentOnlineState == eOnlineState.Remote) //리모트 
                        {
                            GlobalData.Current.MainBooth.CurrentOnlineState = eOnlineState.Offline_EQ;
                        }
                        else
                        {
                            GlobalData.Current.MainBooth.CurrentOnlineState = eOnlineState.Remote;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
            
        }

        //20240205 RGJ MCS SCS 상태바 클릭시 동작 변경 CCS 와 동일하게 작동하도록 함(조범석 매니저 요청)
        //SuHwan_20230213 : [1차 UI검수]
        private void btnMCSState_Click(object sender, MouseButtonEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                string head, msg1, msg2;
                MessageBoxPopupView msgbox = null;

                string strscsState = "0";

                LogManager.WriteOperatorLog(string.Format("사용자가 {0} 화면을 Open하였습니다.", "SCS State Change"),
                    "POPUP", "SCS State Change", GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, LoginUserID, 1,
                    "MCS State Change");

                if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                {
                    if (GlobalData.Current.EQPList.Where(v => v.EQPID == GlobalData.Current.EQPID).Count() != 0)     //클라이언트에서도 서버단 HSMS Disconnect / Connect 요청가능
                    {
                        strscsState = GlobalData.Current.EQPList.FirstOrDefault(d => d.EQPID == GlobalData.Current.EQPID).MCS_State;

                        if (strscsState == "1" || strscsState == "2") //HSMS 소켓을 클로즈 시도
                        {
                            head = TranslationManager.Instance.Translate("MCS State Change").ToString();
                            msg1 = TranslationManager.Instance.Translate("Are you sure you want to").ToString();
                            msg2 = "SCS" + TranslationManager.Instance.Translate("Disconnect").ToString() + "?";
                        }
                        else //HSMS 소켓을 오픈 시도
                        {
                            head = TranslationManager.Instance.Translate("MCS State Change").ToString();
                            msg1 = TranslationManager.Instance.Translate("Are you sure you want to").ToString();
                            msg2 = "SCS" + TranslationManager.Instance.Translate("Connect").ToString() + "?";
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                else //Server
                {
                    if (GlobalData.Current.HSMS.Enabled)//HSMS 접속을 완전히 끊을지 물어본다.
                    {
                        head = TranslationManager.Instance.Translate("MCS State Change").ToString();
                        msg1 = TranslationManager.Instance.Translate("Are you sure you want to").ToString();
                        msg2 = "MCS" + TranslationManager.Instance.Translate("Disconnect").ToString() + "?";
                    }
                    else
                    {
                        head = TranslationManager.Instance.Translate("MCS State Change").ToString();
                        msg1 = TranslationManager.Instance.Translate("Are you sure you want to").ToString();
                        msg2 = "MCS" + TranslationManager.Instance.Translate("Connect").ToString() + "?";
                    }
                }

                msgbox = new MessageBoxPopupView(head, msg1, msg2, "", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                CustomMessageBoxResult mBoxResult = msgbox.ShowResult();
                if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                {
                    if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                    {
                        if (strscsState == "1" || strscsState == "2")
                        {
                            ClientSetProcedure("SCSState", "Stop");
                        }
                        else
                        {
                            ClientSetProcedure("SCSState", "Start");
                        }
                    }
                    else //Server
                    {
                        if (GlobalData.Current.HSMS.Enabled)
                        {
                            GlobalData.Current.HSMS.Stop();
                        }
                        else
                        {
                            GlobalData.Current.HSMS.Start();
                        }
                        //if ((string)textblock_SCSConectivity.Tag == "1" || (string)textblock_SCSConectivity.Tag == "2")
                        //    GlobalData.Current.HSMS.Stop();
                        //else
                        //    GlobalData.Current.HSMS.Start();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
            
        }
        private void btnBoothState_Click(object sender, MouseButtonEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                BoothStateChangePopupView kw = new BoothStateChangePopupView();

                LogManager.WriteOperatorLog(string.Format("사용자가 {0} 화면을 Open하였습니다.", kw.Tag.ToString()),
                    "POPUP", kw.Tag.ToString(), GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, LoginUserID, 1,
                    kw.Tag.ToString());

                kw.Owner = Application.Current.MainWindow; // We must also set the owner for this to work.
                kw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                kw.ShowDialog();
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }

        }
        private void btnPLCState_Click(object sender, MouseButtonEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                PLCStateView psv = new PLCStateView();
                psv.ShowDialog();
                psv.DisposeView();
                psv = null;
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }

        //220330 seongwon page전환 이벤트
        public void eventGUIPageChange()
        {
            setGUIUserPageChange();
        }

        public void eventShowInTask()
        {
            if (GlobalData.Current.ServerClientType == eServerClientType.Client)
            {
                //231106 RGJ 클라이언트 작업표시줄에 나와야 함. 조범석 매니저 요청.
                Dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(delegate
                {
                    this.ShowInTaskbar = true;
                }));
            }
        }

        private void setGUIUserPageChange()
        {
            switch (LoginUserStatePopupView.UserPageTag.ToString())
            {
                case "AccountManagement":
                    this.frame_content.Content = userView;
                    GlobalData.Current.SendTagEvent = "User";
                    popupMain.IsOpen = false;

                    //PageBtn_Click(sender, MouseButtonEventArgs e)
                    break;

                case "Logout":
                    ProcessCurrentUserLogout();
                    break;

                default:
                    break;
            }
        }
        private void ProcessCurrentUserLogout()
        {
            GlobalData.Current.UserMng.Logout();
            GlobalData.Current.LoginUserAuthority.Clear(); //230103 LoginUserAuthority TEST
            TextSubMenuAuthority();
            popupMain.IsOpen = false;
            PageChenge("MainPage");
            dockpanelNavigation.Children.Clear();
            dicNavigationTab.Clear();
            panelTag.Clear();

            //혹시나 메뉴얼 커맨드 창 같은게 열려있을 수 있으니 닫아버리자..
            foreach (Window window in Application.Current.Windows)
            {
                if (window.Name != "StockerControlSystem" && window.Name != "MainWindow")
                {
                    window.Close();
                }
            }
        }

        private void CheckAdminLogin()
        {
            if (MainWindow.checkLoginUserLevel != null)
            {
                if (MainWindow.checkLoginUserLevel.ToString() != "Admin")
                {
                    string msg;
                    MessageBoxPopupView msgbox = null;
                    msg = string.Format("A user without user rights. Are you sure you want to login again?");
                    msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                    CustomMessageBoxResult mBoxResult = msgbox.ShowResult();
                    if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                    {
                        GlobalData.Current.UserMng.Logout();
                        popupMain.IsOpen = false;
                        if (string.IsNullOrEmpty(LoginUserName))
                        {
                            LogInPopupView kw = new LogInPopupView();
                            kw.Height = this.ActualHeight;
                            kw.Width = this.ActualWidth;
                            kw.Owner = Application.Current.MainWindow; // We must also set the owner for this to work.
                            kw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                            kw.ShowDialog();
                        }
                    }
                }
            }
            else
            {
                if (string.IsNullOrEmpty(LoginUserName))
                {
                    LogInPopupView kw = new LogInPopupView();
                    kw.Height = this.ActualHeight;
                    kw.Width = this.ActualWidth;
                    kw.Owner = Application.Current.MainWindow; // We must also set the owner for this to work.
                    kw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    kw.ShowDialog();
                }
            }
        }


        //SuHwan_20220706 : 임시 이벤트 나중에
        private void TextBlock_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ScheduleDebugInfo SRB = new ScheduleDebugInfo();
            SRB.Owner = Application.Current.MainWindow; // We must also set the owner for this to work.
            SRB.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            SRB.Show();
        }

        //상단 메뉴 버튼 마우스 클릭
        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                if (sender is SK_ButtonControl senderBuffer)
                {
                    string tagName = senderBuffer.Tag.ToString();

                    if (senderBuffer.IsSelect == true)
                    {
                        setBeginStoryboard("CloseMenu", tagName);
                    }
                    else
                    {
                        senderBuffer.IsSelect = true;
                        switch (tagName)
                        {
                            case "Config":
                            case "Monitoring":
                            case "Log":
                            case "PlayBack":
                            case "TestMode":
                                setBeginStoryboard("OpenMenu", tagName);
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
            
        }
        //SuHwan_20221227 : 메뉴 버튼 초기화
        private void initMenuButton()
        {
            MenuConfig_SubMenu.Margin = new Thickness(1, -MenuConfig_SubMenu.ActualHeight, 1, 0);
            MenuMonitoring_SubMenu.Margin = new Thickness(1, -MenuMonitoring_SubMenu.ActualHeight, 1, 0);
            MenuLog_SubMenu.Margin = new Thickness(1, -MenuLog_SubMenu.ActualHeight, 1, 0);
            if (Menu_Config.IsSelect == true)
            {
                setBeginStoryboard("CloseMenu", Menu_Config.TagName);
            }
            if (Menu_Monitoring.IsSelect == true)
            {
                setBeginStoryboard("CloseMenu", Menu_Monitoring.TagName);
            }
            if (Menu_Log.IsSelect == true)
            {
                setBeginStoryboard("CloseMenu", Menu_Log.TagName);
            }
        }

        //?? ]
        private void BorderButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border)
            {
                var senderBuffer = (Border)sender;

                switch (senderBuffer.Tag.ToString())
                {
                    case "EXIT":
                        popupMain.IsOpen = false;
                        break;

                    default:
                        break;
                }

            }
        }
        //??
        private void btnLanguageState_Click(object sender, MouseButtonEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                if (LanguagePopup.CreatLock == 0)
                {
                    LanguagePopup kw = new LanguagePopup();
                    kw.Owner = Application.Current.MainWindow; // We must also set the owner for this to work.
                    kw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    kw.Show();
                    LanguagePopup.CreatLock = 1;
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }

        }

        //YSW_MainSubMenu 포커스 변경전까지 고정_20221017 //SuHwan_20221227 : focus 관련 
        private void Menu_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is SK_ButtonControl senderBuffer)
            {
                if (_fixButtonFocus == false)
                {
                    setBeginStoryboard("CloseMenu", senderBuffer.TagName);
                }
            }
        }

        //다른버튼 클릭스 포커스 잡아주는 부분
        private void SK_ButtonControl_MouseEnter(object sender, MouseEventArgs e)
        {
            _fixButtonFocus = true;
        }
        private void SK_ButtonControl_MouseLeave(object sender, MouseEventArgs e)
        {
            _fixButtonFocus = false;
        }

        //230103 YSW TextSubMenu_Loaded 
        private void TextSubMenu_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is SK_ButtonControl senderBuffer)
            {
                if (!dicTextSubMenu.ContainsKey(senderBuffer.TagName))
                {
                    dicTextSubMenu.Add(senderBuffer.TagName, senderBuffer);
                }
            }
        }

        //230103 YSW 사용자 권한에 따른 버튼 잠금
        public void TextSubMenuAuthority()
        {
            //return;//일단 잠김기능 안함
            if (!string.IsNullOrEmpty(LoginUserName))
            {
                foreach (var item in dicTextSubMenu.Values)
                {
                    if (GlobalData.Current.LoginUserAuthority.Contains("Read" + item.TagName))
                    {
                        dicTextSubMenu[item.TagName].UserAuthority = true;
                        dicTextSubMenu[item.TagName].LockIcon = Visibility.Hidden;
                    }
                    else
                    {
                        dicTextSubMenu[item.TagName].UserAuthority = false;
                        dicTextSubMenu[item.TagName].LockIcon = Visibility.Visible;
                    }
                }

                //SuHwan_20230711 락기능 아이탬들 추가
                btnMCSState.IsEnabled   = (GlobalData.Current.LoginUserAuthority.Contains("ModifyMCSState"))    ? true : false;
                btnSCSState.IsEnabled   = (GlobalData.Current.LoginUserAuthority.Contains("ModifySCSState"))    ? true : false;
                btnPLCState.IsEnabled   = (GlobalData.Current.LoginUserAuthority.Contains("ModifyPLCState"))    ? true : false;
                btnBoothState.IsEnabled = (GlobalData.Current.LoginUserAuthority.Contains("ModifyBoothState"))  ? true : false;
                EXITBtn.IsEnabled       = (GlobalData.Current.LoginUserAuthority.Contains("ModifyEXIT"))        ? true : false;

            }
            else
            {
                foreach (var item in dicTextSubMenu.Values)
                {
                    dicTextSubMenu[item.TagName].UserAuthority = false;
                    dicTextSubMenu[item.TagName].LockIcon = Visibility.Visible;
                    //dicTextSubMenu[item.TagName].Foreground = (SolidColorBrush)new BrushConverter().ConvertFrom("#FFEC685B");
                    dicTextSubMenu[item.TagName].Foreground = Brushes.Gray;
                }

                //SuHwan_20230711 락기능 아이탬들 추가
                btnMCSState.IsEnabled   = false;
                btnSCSState.IsEnabled   = false;
                btnPLCState.IsEnabled   = false;
                btnBoothState.IsEnabled = false;
                EXITBtn.IsEnabled       = false;
            }

        }

        private void ClientSetProcedure(string cmdtype, string value)
        {
            if (GlobalData.Current.ServerClientType != eServerClientType.Client)
                return;

            ClientReqList reqBuffer = new ClientReqList
            {
                EQPID = GlobalData.Current.EQPID,
                CMDType = cmdtype,
                Target = "BOOTH",
                TargetID = string.Empty,
                TargetValue = value,
                ReqTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Requester = eServerClientType.Client,
                JobID = string.Empty,
            };

            GlobalData.Current.DBManager.DbSetProcedureClientReq(reqBuffer.EQPID, reqBuffer.CMDType, reqBuffer.Target, reqBuffer.TargetID, reqBuffer.TargetValue, reqBuffer.ReqTime, reqBuffer.Requester);
        }

        #region ~~~~ [ 메인 윈도우 타이틀바 기능 모음 ] ~~~~ 
        //SuHwan_20230712 : 윈도우 타이틀바로 변경
        private void Window_LocationChanged(object sender, EventArgs e)
        {
            int sum = 0;
            foreach (var item in _screens)
            {
                sum += item.WorkingArea.Width;
                if (sum >= this.Left + this.Width / 2)
                {
                    this.MaxHeight = item.WorkingArea.Height;
                    break;
                }
            }
        }

        private void System_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                if (e.ClickCount >= 2)
                {
                    this.WindowState = (this.WindowState == WindowState.Normal) ? WindowState.Maximized : WindowState.Normal;
                }
                else
                {
                    _startPos = e.GetPosition(null);
                }
            }
            else if (e.ChangedButton == MouseButton.Right)
            {
                var pos = PointToScreen(e.GetPosition(this));
                IntPtr hWnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
                IntPtr hMenu = GetSystemMenu(hWnd, false);
                int cmd = TrackPopupMenu(hMenu, 0x100, (int)pos.X, (int)pos.Y, 0, hWnd, IntPtr.Zero);
                if (cmd > 0) SendMessage(hWnd, 0x112, (IntPtr)cmd, IntPtr.Zero);
            }
        }

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wp, IntPtr lp);
        [DllImport("user32.dll")]
        static extern int TrackPopupMenu(IntPtr hMenu, uint uFlags, int x, int y, int nReserved, IntPtr hWnd, IntPtr prcRect);

        private void System_MouseMove(object sender, MouseEventArgs e)
        {

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (this.WindowState == WindowState.Maximized && Math.Abs(_startPos.Y - e.GetPosition(null).Y) > 2)
                {
                    var point = PointToScreen(e.GetPosition(null));
                    var pre_length = this.ActualWidth;
                    this.WindowState = WindowState.Normal;
                    var cur_length = this.ActualWidth;

                    var screen_raito = cur_length / pre_length;

                    //this.Left = point.X - this.ActualWidth / 2;
                    //this.Top = point.Y - this.ActualHeight / 2;
                    Left = point.X - (_startPos.X * screen_raito);
                    Top = point.Y - _startPos.Y;
                }
                DragMove();
            }
        }

        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                if (sender is SK_ButtonControl senderBuffer)
                {
                    if (this.WindowState == WindowState.Normal)
                    {
                        senderBuffer.PathData = "M432 48H208c-17.7 0-32 14.3-32 32V96H128V80c0-44.2 35.8-80 80-80H432c44.2 0 80 35.8 80 80V304c0 44.2-35.8 80-80 80H416V336h16c17.7 0 32-14.3 32-32V80c0-17.7-14.3-32-32-32zM48 448c0 8.8 7.2 16 16 16H320c8.8 0 16-7.2 16-16V256H48V448zM64 128H320c35.3 0 64 28.7 64 64V448c0 35.3-28.7 64-64 64H64c-35.3 0-64-28.7-64-64V192c0-35.3 28.7-64 64-64z";
                        this.WindowState = WindowState.Maximized;
                        senderBuffer.ToolTip = TranslationManager.Instance.Translate("이전 크기로 복원").ToString();
                    }
                    else
                    {
                        senderBuffer.PathData = "M.3 89.5C.1 91.6 0 93.8 0 96V224 416c0 35.3 28.7 64 64 64l384 0c35.3 0 64-28.7 64-64V224 96c0-35.3-28.7-64-64-64H64c-2.2 0-4.4 .1-6.5 .3c-9.2 .9-17.8 3.8-25.5 8.2C21.8 46.5 13.4 55.1 7.7 65.5c-3.9 7.3-6.5 15.4-7.4 24zM48 224H464l0 192c0 8.8-7.2 16-16 16L64 432c-8.8 0-16-7.2-16-16l0-192z";
                        this.WindowState = WindowState.Normal;
                        senderBuffer.ToolTip = TranslationManager.Instance.Translate("최대화").ToString();
                    }
                }
                else
                {
                    this.WindowState = (this.WindowState == WindowState.Normal) ? WindowState.Maximized : WindowState.Normal;
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
            
        }

        private void Mimimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
        #endregion
    }
}
