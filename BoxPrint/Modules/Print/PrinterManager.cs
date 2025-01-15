using BoxPrint.Config;
using BoxPrint.Config.Print;
using BoxPrint.GUI.ETC.LoadingPopup;
using BoxPrint.Log;
using BoxPrint.OpenHSMS;
using Microsoft.Office.Interop.Excel;
using OSG.Com.HSMS;
using OSG.Com.HSMS.Common;
using PLCProtocol.DataClass;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BoxPrint.Modules.Print
{
    public class PrinterManager
    {

        private INK_SQUID m_InkPrint = null;
        private Thread m_sndThread = null;
        private Thread m_rcvThread = null;
        private Thread m_AutoThread = null;

        //private HSMSActionBase _HMsgActions = null;
        //private HSMSActionBase HMsgActions
        //{
        //    get
        //    {
        //        return _HMsgActions;
        //    }
        //}


        #region Field
        private const char LF = (char)0x0A; //End
        private readonly object syncRoot = new object();
        private bool SetReplyCheck = true;
        private Dictionary<string, object> CurrentRecipe = new Dictionary<string, object>();
        private System.Diagnostics.Stopwatch ChangeTime = null;
        #endregion

        #region Prop
        public string ModuleName { get; protected set; }

        //private bool m_IsFirstConnectedRecived;
        //public bool IsFirstConnectedRecived
        //{
        //    get => this.m_IsFirstConnectedRecived;
        //    private set
        //    {
        //        if (this.m_IsFirstConnectedRecived == value) return;

        //        this.m_IsFirstConnectedRecived = value;

        //        if (this.m_IsFirstConnectedRecived)
        //            this.FirstConnectedRecived(true);
        //    }
        //}

        public bool enableNextPrint;

        private int m_InkPercent = 0;
        public int InkPercent
        {
            get => this.m_InkPercent;
            set
            {
                if (this.m_InkPercent == value) return;

                this.m_InkPercent = value;

                //Send Touch PC
                //InkjectInkInformationArgs args = new InkjectInkInformationArgs();
                //if (this.Name == HubServiceName.InkjectEquipment1)
                //    args.Line = 1;
                //else if (this.Name == HubServiceName.InkjectEquipment2)
                //    args.Line = 2;

                //args.InkPercent = this.m_InkPercet;

                //EcsServerAppManager.Instance.Hub.Send(HubServiceName.TouchPcCaseErectEquipment, args);
            }
        }

        protected ePrintStep _NextCVCommand = ePrintStep.None;
        public ePrintStep NextCVCommand
        {
            get
            {
                return _NextCVCommand;
            }
            protected set
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "CVModule : {0} CommandChanged [{1}] ==> [{2}]", this.ModuleName, _NextCVCommand, value);
                if (value == ePrintStep.ErrorHandling)
                {
                    BeforeErrorCommand = _NextCVCommand;
                }
                _NextCVCommand = value;

            }
        }
        protected ePrintStep BeforeErrorCommand = ePrintStep.None; //에러 발생시 기존 명령 저장

        public int InitStep { get; protected set; }
        public int AutoStep { get; protected set; }

        public bool ChangingRecipe = false;
        public bool UseRobot = false;

        #endregion

        #region 속성들 정의
        private bool _enabled = false;
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (_enabled == value) return;

                _enabled = value;
            }
        }
        //private ConcurrentQueue<byte[]> SendingQueue { get; set; }
        //private ConcurrentQueue<byte[]> ReceiveQueue { get; set; }

        private bool ThreadStop { get; set; }
        #endregion

        #region 이벤트 정의

        //public event EventHandler<bool> FirstConnectedRecived;
        //public event EventHandler EnablePrintComplete;
        //public event EventHandler PrintCompleteResponse;
        //public event EventHandler<AutoDataResponseEnum> AutoDataRecordResponse;
        //public event EventHandler WriteAutoDataRecivedResponse;
        //public event EventHandler WriteAutoDataQueueClearResponse;
        //public event EventHandler<int> ReadInkLevelResponse;
        //public event EventHandler<string> GetAutoDataStringResponse;

        //public event EventHandler<StateChangedEventArgs> StateChanged;
        //public event EventHandler<PrintMessageReceivedEventArgs> MessageReceived;

        

        //protected virtual void OnStateChanged(bool enabled)
        //{
        //    PrinttateChangedEventArgs args = new PrinttateChangedEventArgs();
        //    args.Enabled = enabled;

        //    StateChanged?.Invoke(this, args);
        //}

        //protected virtual void OnMessageReceived(DataMessage dataMsg)
        //{
        //    PrintMessageReceivedEventArgs args = new PrintMessageReceivedEventArgs(dataMsg);

        //    if (MessageReceived != null)
        //    {
        //        MessageReceived(this, args);
        //    }
        //}
        #endregion

        public PrinterManager()
        {

        }

        public bool initPrintDriver()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "Creating Print Manager...... ");
            //_HMsgActions = CreateCustomHSMSAction(GlobalData.Current.CurrnetLineSite);
            ModuleName = "Print1";
            m_InkPrint = new INK_SQUID(ModuleName, eUnitComType.TCP_IP, false);
            //m_InkPrint.SetCommunicationAddress("192.168.100.50", "4000", null);
            m_InkPrint.SetCommunicationAddress("127.0.0.1", "4000", null);
            //m_InkPrint.InitUnitController();
            //m_InkPrint.TCPSocketConnect();
            //this.Enabled = true;

            m_InkPrint.OnUnitConnectionStateChanged += Print_StateChanged;

            //m_InkPrint.StateChanged += new EventHandler<StateChangedEventArgs>(OpenHSMS_OnStateChanged);
            //m_InkPrint.DataMessageReceived += new EventHandler<DataMessageReceivedEventArgs>(OpenHSMS_OnDataMessageReceived);
            //m_InkPrint.ErrorOccured += new EventHandler<HSMSErrorEventArgs>(OpenHSMS_ErrorOccured);
            //SendingQueue = new ConcurrentQueue<byte[]>();
            //ReceiveQueue = new ConcurrentQueue<byte[]>();

            //m_sndThread = new Thread(SendRun);
            //m_sndThread.Name = "HSMS Send Thread";
            //m_sndThread.IsBackground = true;

            m_rcvThread = new Thread(RecvRun);
            m_rcvThread.Name = "Print Recv Thread";
            m_rcvThread.IsBackground = true;

            m_AutoThread = new Thread(AutoRun);
            m_AutoThread.Name = "Print Auto Thread";
            m_AutoThread.IsBackground = true;

            LogManager.WriteConsoleLog(eLogLevel.Info, "Print Manager has Created. ");
            return true;
        }


        public static string GetPrintConfigFilePath()
        {
            string configFileName = @"\OpenHSMS.config";
            string FullPath = System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
            string TPath = Path.Combine(GlobalData.Current.CurrentFilePaths(FullPath) + configFileName);

            FileInfo File = new FileInfo(TPath);
            if (!File.Exists)
            {
                //TPath = GlobalData.Current.ClientConfigFilePath + "\\OpenHSMS.config";
                //File = new FileInfo(TPath);

                //if (!File.Exists)
                //{
                //    throw new FileNotFoundException("OpenHSMS Config파일이 실행위치나 루트에 존재하지 않습니다!", "OpenHSMS.config");
                //}
                string temppath = GlobalData.Current.ConfigFilePathChange(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName + configFileName, configFileName);

                TPath = temppath;
            }
            return TPath;
        }

        public virtual void Start()
        {
            if (!this.Enabled)
            {
                m_InkPrint.InitUnitController();
                //if (!m_OpenHSMS.IsRunning)
                //{
                //    m_OpenHSMS.Start();
                //}
            }

            if (m_rcvThread.ThreadState == (ThreadState.Background | ThreadState.Unstarted))
            {
                m_rcvThread.Start();
            }

            if (m_AutoThread.ThreadState == (ThreadState.Background | ThreadState.Unstarted))
            {
                m_AutoThread.Start();
            }

        }

        public void StartInitialize()
        {
            InitStep = 10;
            NextCVCommand = ePrintStep.Initialize;
        }

        private void AutoRun()
        {
            bool bExitFlag = false;
            byte[] dataMsg = null;
            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Thread ID : {1}", "Print.AutoRun()", Thread.CurrentThread.ManagedThreadId);

            StartInitialize();

            if (!this.Enabled)
            {
                //if (!m_OpenHSMS.IsRunning)
                //{
                //    m_OpenHSMS.Start();
                //}
            }



            DateTime dtStart = DateTime.Now;
            while (!bExitFlag)
            {
                if (this.ThreadStop)
                {
                    break;
                }

                if (m_InkPrint.CheckConnection() == eUnitConnection.Disconnect)
                {
                    m_InkPrint.TCPSocketConnect();
                    //IsFirstConnectedRecived = false;
                    NextCVCommand = ePrintStep.ErrorHandling;
                    Thread.Sleep(100);
                    continue;
                }
                //Print 연결 완료

                if (!this.Enabled)
                    continue;
                
                switch(NextCVCommand)
                {
                    case ePrintStep.Initialize:
                        InitializeAction();
                        break;
                    case ePrintStep.AutoAction:
                        ScenarioRun();
                        break;
                    case ePrintStep.ErrorHandling:
                        ErrorHandling();
                        break;
                    default:
                        break;
                }


                Thread.Sleep(30);
            }
        }

        protected void ErrorHandling()
        {
            if (m_InkPrint.CheckConnection() == eUnitConnection.Connect)
                StartInitialize();
        }

        protected void InitializeAction()
        {
            if (InitStep == 10)
            {
                ChangeTime = System.Diagnostics.Stopwatch.StartNew();
                LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Module InitializeAction Start", this.ModuleName);
            }

            if (m_InkPrint.CheckConnection() == eUnitConnection.Disconnect)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, "{0} Module Connection Error Step : {1}", this.ModuleName, this.InitStep);

                InitStep = 900; // 에러 처리
            }

            if (ChangeTime.ElapsedMilliseconds > 60 * 1000)
            {
                //timeout
                InitStep = 900; // 에러 처리
            }

            switch (InitStep)
            {
                case 10: //레시피 읽어 오기
                    LodingPopup.Instance.setProgressValue(30, "Recipe Loading....");

                    UpdateRecipe();

                    InitStep++;
                    break;
                case 11: //속도 설정
                    LodingPopup.Instance.setProgressValue(40, "Speed Loading....");

                    m_InkPrint.MakeDataMessage(ePrintCommand.SetManualSpeed.ToString(), CurrentRecipe);
                    InitStep++;
                    break;
                case 12: // 속도 설정 완료
                    if (SetReplyCheck)
                        InitStep++;
                    break;
                case 13:  // 딜레이 설정
                    LodingPopup.Instance.setProgressValue(50, "Delay Loading....");

                    m_InkPrint.MakeDataMessage(ePrintCommand.SetPrintDelay.ToString(), CurrentRecipe);
                    InitStep++;
                    break;
                case 14:  // 딜레이 설정 완료
                    if (SetReplyCheck)
                        InitStep++;
                    break;
                case 15:  // 방향 설정
                    LodingPopup.Instance.setProgressValue(60, "Direction Loading....");

                    m_InkPrint.MakeDataMessage(ePrintCommand.SetPrintDirection.ToString(), CurrentRecipe);
                    InitStep++;
                    break;
                case 16:  // 방향 설정 완료
                    if (SetReplyCheck)
                        InitStep++;
                    break;
                case 17:  // 파일 설정
                    LodingPopup.Instance.setProgressValue(70, "Image Loading....");

                    m_InkPrint.MakeDataMessage(ePrintCommand.Build.ToString(), CurrentRecipe);
                    InitStep++;
                    break;
                case 18:  // 파일 설정 완료
                    if (SetReplyCheck)
                        InitStep = 100;
                    break;
                case 100:   //레시피 변경 완료
                    ChangingRecipe = false;
                    LodingPopup.Instance.AutoStop();

                    NextCVCommand = ePrintStep.AutoAction;
                    InitStep = 0;
                    break;
                case 900:   //에러 발생
                    ChangingRecipe = false;
                    LodingPopup.Instance.AutoStop();

                    NextCVCommand = ePrintStep.ErrorHandling;
                    InitStep = 0;
                    break;
            }

        }

        protected void ScenarioRun()
        {
            if (GlobalData.Current.PrintScenarioList.CurrentState != ePrintScenarioState.Run)
                return;

            if (m_InkPrint.CheckConnection() == eUnitConnection.Disconnect)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, "{0} Module Connection Error Step : {1}", this.ModuleName, this.InitStep);

                AutoStep = 900; // 에러 처리
            }

            
            ScenarioData sd = GlobalData.Current.PrintScenarioList.GetCurrentScenario();

            if (sd != null)
            {
                if (int.TryParse(CurrentRecipe["Recipe_No"].ToString(), out int CurrReNo))
                {
                    if (sd.Recipe_No != CurrReNo)
                    {

                        ChangingRecipe = true;

                        GlobalData.Current.Recipe_Manager.ChangeSelecteRecipe(sd.Recipe_No);
                        return;
                    }
                    else
                        if (AutoStep == 0)   AutoStep = 10;
                }
            }

            switch (AutoStep)
            {
                //case 9:  
                case 10: //로봇 프린트 스텝별 시작 위치 확인
                    if (!UseRobot)
                        AutoStep++;
                    else
                    {
                        //로봇 상태 확인
                        //스텝 위치 확인
                        //sd.iStep;
                        //스텝 위치 이상시 이동 명령 출력
                        AutoStep++;
                    }
                    break;
                case 11: //프린트 시작  ( 로봇 동작 시작 )
                    if (!UseRobot) AutoStep++;
                    else
                    {

                        AutoStep++;
                    }
                    break;
                case 12: // 프린트 완료 대기
                    if (m_InkPrint.enableNextPrint)
                    {
                        m_InkPrint.enableNextPrint = false;
                        AutoStep++;
                    }
                    break;
                case 13:  // Recipe Change // 스텝 초기화
                    if (GlobalData.Current.PrintScenarioList.SetNextStep()) //다음 레시피 있음
                    {
                        AutoStep = 10;
                        ChangingRecipe = true;

                        GlobalData.Current.Recipe_Manager.ChangeSelecteRecipe(sd.Recipe_No);
                    }
                    else // 다음 레시피 없음
                    {
                        AutoStep = 0;
                    }

                    break;
           
                case 100:   //레시피 변경 완료
                    ChangingRecipe = false;
                    //LodingPopup.Instance.AutoStop();

                    NextCVCommand = ePrintStep.AutoAction;
                    AutoStep = 0;
                    break;
                case 900:   //에러 발생
                    ChangingRecipe = false;
                    //LodingPopup.Instance.AutoStop();

                    NextCVCommand = ePrintStep.ErrorHandling;
                    AutoStep = 0;
                    break;
            }

        }
        private void UpdateRecipe()
        {
            DataSet dataSet = GlobalData.Current.Recipe_Manager.GetSelectedDataSet();

            string temp = string.Empty;
            int table = dataSet.Tables.Count;
            for (int i = 0; i < table; i++)// set the table value in list one by one
            {
                foreach (DataRow dr in dataSet.Tables[i].Rows)
                {
                    //if (string.IsNullOrEmpty(dr["CONFIG_NO"].ToString()))
                    //    continue;
                    if (!int.TryParse(dr["Recipe_No"].ToString(), out int reNo))
                        reNo = 0;

                    //if (dr["DataType"].ToString().Equals(eRecipeDataType.BASE.ToString()))
                    CurrentRecipe[dr["Name"].ToString()] = dr["Value"].ToString();
                    //else if (dr["DataType"].ToString().Equals(eRecipeDataType.AUTODATA.ToString()))
                    //    temp += dr["Value"].ToString() + "~";
                    //else if (dr["DataType"].ToString().Equals(eRecipeDataType.COUNT.ToString()))
                    //    CurrentRecipe[dr["Name"].ToString()] = dr["Value"].ToString();

                }
                //CurrentRecipe["AutoData"] = temp;
            }
        }
        //private void SendRun()
        //{
        //    bool bExitFlag = false;
        //    byte[] dataMsg = null;
        //    LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Thread ID : {1}", "HSMS.SendRun()", Thread.CurrentThread.ManagedThreadId);

        //    //if (!this.Enabled)
        //    //{
        //    //    if (!m_OpenHSMS.IsRunning)
        //    //    {
        //    //        m_OpenHSMS.Start();                    
        //    //    }
        //    //}

        //    DateTime dtStart = DateTime.Now;
        //    while (!bExitFlag)
        //    {
        //        if (this.ThreadStop)
        //        {
        //            break;
        //        }

        //        if (this.Enabled && (SendingQueue.Count > 0))
        //        {
        //            if (!SendingQueue.TryDequeue(out dataMsg))
        //            {
        //                LogManager.WriteConsoleLog(eLogLevel.Info, "MessageQueue Dequeue NG - Count : {0}", SendingQueue.Count);
        //                continue;
        //            }
        //            if (dataMsg == null)
        //            {
        //                LogManager.WriteConsoleLog(eLogLevel.Info, "MessageQueue Data Null - Count : {0}", SendingQueue.Count);
        //                continue;
        //            }

        //            string message = Encoding.ASCII.GetString(dataMsg, 0, dataMsg.Length);

        //            m_InkPrint.SendMessage(message);

        //        }
        //        Thread.Sleep(30);
        //    }
        //}

        public INK_SQUID GetPrintModule()
        {
            return m_InkPrint;
        }




        private void RecvRun()
        {
            bool bExitFlag = false;
            byte[] dataMsg = null;
            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Thread ID : {1}", "Print.RecvRun()", Thread.CurrentThread.ManagedThreadId);

            DateTime dtStart = DateTime.Now;
            string msgKey = string.Empty;
            while (!bExitFlag)
            {
                if (this.ThreadStop)
                {
                    break;
                }
                if (this.Enabled && (m_InkPrint.GetRecvCount() > 0))//(this.ReceiveQueue.Count > 0))
                {
                    if (!m_InkPrint.GetRecvData(out string message))
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "ReceiveQueue Dequeue NG - Count : {0}", m_InkPrint.GetRecvCount());
                        continue;
                    }

                    if (message == null)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "ReceiveQueue Data Null - Count : {0}", m_InkPrint.GetRecvCount());
                        continue;
                    }

                    //if (dataMsg.IsPrimaryMessage)
                    //{
                    //LogManager.WriteConsoleLog(eLogLevel.Info, "<Rcv> SF:{0}, SB:{1}", dataMsg.Name, dataMsg.SystemBytes);

                    m_InkPrint.ProcessAsEvent(message);

                }

                Thread.Sleep(100);
            }
        }



        private void Print_StateChanged(bool isConnected)
        {

            this.Enabled = isConnected;
            if (!isConnected)
            {
                //m_InkPrint.IsFirstConnectedRecived = false;
            }
        }

        internal bool GetFirstConnectedRecived()
        {
            return m_InkPrint.IsFirstConnectedRecived;
        }
    }
}
