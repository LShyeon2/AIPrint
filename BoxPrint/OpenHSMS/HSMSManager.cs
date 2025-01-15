using OSG.Com.HSMS.Common;
using BoxPrint.DataList;
using BoxPrint.DataList.MCS;
using BoxPrint.Log;
using BoxPrint.Modules.Conveyor;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;

namespace BoxPrint.OpenHSMS
{
    //220929 HHJ SCS 개선     //- HSMS Client 사용을 위한 구조 변경
    /// <summary>
    /// 중요한것들만 virtual으로 변경
    /// 변경 리스트
    /// public void Start()
    /// public void Stop()
    /// public void SendS6F11(int CEID,Dictionary<string, object> args = null)
    /// public void SendS6F11(int CEID, string ObjKey,Object ReportObject)
    /// public void SendS6F11(int CEID, string ObjKey1, Object ReportObject1, string ObjKey2, Object ReportObject2)
    /// public void SendS6F11(int CEID, string ObjKey1, Object ReportObject1, string ObjKey2, Object ReportObject2, string ObjKey3, Object ReportObject3)
    /// public void SendMessageAsync(string messageName, Dictionary<string, object> args = null)
    /// public void SendMessageAsync(DataMessage dataMsg)
    /// </summary>

    /// <summary>
    /// //220322 RGJ OPENHSMS 연동 추가.
    /// OPEN HSMS 관리 모듈
    /// 예전 인덱스 소스 참조
    /// </summary>
    public class HSMSManager : SingletonBase<HSMSManager>
    {
        private object thisLock = new object();
        private object thisRcvLock = new object();

        private OSG.Com.HSMS.OpenHSMS m_OpenHSMS = null;
        private Thread m_sndThread = null;
        private Thread m_rcvThread = null;

        private HSMSActionBase _HMsgActions = null;
        private HSMSActionBase HMsgActions
        {
            get
            {
                return _HMsgActions;
            }
        }

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
        private ConcurrentQueue<DataMessage> SendingQueue { get; set; }
        private ConcurrentQueue<DataMessage> ReceiveQueue { get; set; }

        private bool ThreadStop { get; set; }
        #endregion

        #region 이벤트 정의
        public event EventHandler<HSMSStateChangedEventArgs> StateChanged;
        public event EventHandler<HSMSMessageReceivedEventArgs> MessageReceived;

        protected virtual void OnStateChanged(bool enabled)
        {
            HSMSStateChangedEventArgs args = new HSMSStateChangedEventArgs();
            args.Enabled = enabled;

            StateChanged?.Invoke(this, args);
        }

        protected virtual void OnMessageReceived(DataMessage dataMsg)
        {
            HSMSMessageReceivedEventArgs args = new HSMSMessageReceivedEventArgs(dataMsg);

            if (MessageReceived != null)
            {
                MessageReceived(this, args);
            }
        }
        #endregion


        public HSMSManager()
        {

        }
        public bool InitHSMSDriver()
        {
            return false ;

            LogManager.WriteConsoleLog(eLogLevel.Info, "Creating HSMS Manager...... ");
            _HMsgActions = CreateCustomHSMSAction(GlobalData.Current.CurrnetLineSite);
            m_OpenHSMS = new OSG.Com.HSMS.OpenHSMS(HSMSManager.GetHSMSConfigFilePath());
            m_OpenHSMS.StateChanged += new EventHandler<StateChangedEventArgs>(OpenHSMS_OnStateChanged);
            m_OpenHSMS.DataMessageReceived += new EventHandler<DataMessageReceivedEventArgs>(OpenHSMS_OnDataMessageReceived);
            m_OpenHSMS.ErrorOccured += new EventHandler<HSMSErrorEventArgs>(OpenHSMS_ErrorOccured);
            SendingQueue = new ConcurrentQueue<DataMessage>();
            ReceiveQueue = new ConcurrentQueue<DataMessage>();

            m_sndThread = new Thread(SendRun);
            m_sndThread.Name = "HSMS Send Thread";
            m_sndThread.IsBackground = true;

            m_rcvThread = new Thread(RecvRun);
            m_rcvThread.Name = "HSMS Recv Thread";
            m_rcvThread.IsBackground = true;
            LogManager.WriteConsoleLog(eLogLevel.Info, "HSMS Manager has Created. ");
            return true;
        }

        private HSMSActionBase CreateCustomHSMSAction(eLineSite TargetSite)
        {
            HSMSActionBase HAction = null;
            switch (TargetSite)
            {
                case eLineSite.TOP_POC:
                    HAction = new HSMSActionBase();
                    break;
                default:
                    HAction = new HSMSActionBase();
                    break;
            }
            return HAction;
        }
        public static string GetHSMSConfigFilePath()
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

        /// <summary>
        /// Start
        /// </summary>
        public virtual void Start()
        {
            if (m_OpenHSMS == null)
                return;

            if (!this.Enabled)
            {
                if (!m_OpenHSMS.IsRunning)
                {
                    m_OpenHSMS.Start();
                }
            }

            if (m_sndThread.ThreadState == (ThreadState.Background | ThreadState.Unstarted))
            {
                m_sndThread.Start();
            }

            if (m_rcvThread.ThreadState == (ThreadState.Background | ThreadState.Unstarted))
            {
                m_rcvThread.Start();
            }
        }

        /// <summary>
        /// Stop
        /// </summary>
        public virtual void Stop()
        {
            m_OpenHSMS.Stop();
        }


        public virtual void SendS6F11(int CEID, Dictionary<string, object> args = null)
        {
            if (args == null)
            {
                args = new Dictionary<string, object>();
            }
            if (!args.ContainsKey("CEID"))
            {
                args.Add("CEID", CEID);
            }
            SendMessageAsync("S6F11", args);
        }
        //S6F11 메시지 발신을 간략화 보고대상 오브젝트가 한개일때
        public virtual void SendS6F11(int CEID, string ObjKey, Object ReportObject)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args.Add("CEID", CEID);
            args.Add(ObjKey.ToUpper(), ReportObject);

            SendMessageAsync("S6F11", args);
        }
        //S6F11 메시지 발신을 간략화 보고대상 오브젝트가 2개일때
        public virtual void SendS6F11(int CEID, string ObjKey1, Object ReportObject1, string ObjKey2, Object ReportObject2)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args.Add("CEID", CEID);
            args.Add(ObjKey1.ToUpper(), ReportObject1);
            args.Add(ObjKey2.ToUpper(), ReportObject2);
            SendMessageAsync("S6F11", args);
        }

        //S6F11 메시지 발신을 간략화 보고대상 오브젝트가 3개일때
        public virtual void SendS6F11(int CEID, string ObjKey1, Object ReportObject1, string ObjKey2, Object ReportObject2, string ObjKey3, Object ReportObject3)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args.Add("CEID", CEID);
            args.Add(ObjKey1.ToUpper(), ReportObject1);
            args.Add(ObjKey2.ToUpper(), ReportObject2);
            args.Add(ObjKey3.ToUpper(), ReportObject3);
            SendMessageAsync("S6F11", args);
        }

        //S6F11 메시지 발신을 간략화 보고대상 오브젝트가 4개일때
        public virtual void SendS6F11(int CEID, string ObjKey1, Object ReportObject1, string ObjKey2, Object ReportObject2, string ObjKey3, Object ReportObject3, string ObjKey4, Object ReportObject4)
        {
            Dictionary<string, object> args = new Dictionary<string, object>();
            args.Add("CEID", CEID);
            args.Add(ObjKey1.ToUpper(), ReportObject1);
            args.Add(ObjKey2.ToUpper(), ReportObject2);
            args.Add(ObjKey3.ToUpper(), ReportObject3);
            args.Add(ObjKey4.ToUpper(), ReportObject4);
            SendMessageAsync("S6F11", args);
        }
        /// <summary>
        /// SendMessageAsync
        /// </summary>
        /// <param name="messageName"></param>
        /// <param name="args"></param>
        public virtual void SendMessageAsync(string messageName, Dictionary<string, object> args = null)
        {

            if (!this.Enabled)
            {
                return;
            }

            if (GlobalData.Current.MainBooth.CurrentOnlineState != eOnlineState.Remote)
            {
                if (messageName.Contains("S1"))
                {
                    //S1 Message 는 처리해야함.
                }
                else
                {
                    //240404 RGJ 조범석 매니저 요청으로 Offline 인경우 Offline 보고 제외 모두 보고안함.
                    if (messageName == "S6F11" && args != null) 
                    {
                        bool IsCEID = args.TryGetValue("CEID", out object cItem);
                        if (IsCEID && cItem != null && cItem.ToString() == "1")
                        {
                            //Offline 보고는 올라가야 함.
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                }
            }

            lock (thisLock)
            {
                DataMessage dataMsg = MakeDataMessage(messageName, args);
                if (dataMsg == null)
                {
                    return;
                }
                SendingQueue.Enqueue(dataMsg);
            }
        }
        /// <summary>
        /// SendMessageAsync
        /// </summary>
        /// <param name="dataMsg"></param>
        public virtual void SendMessageAsync(DataMessage dataMsg)
        {
            //예외사항일때만 해당 메시지가 발송된다.
            if (!this.Enabled)
            {
                return;
            }
            if (GlobalData.Current.MainBooth.CurrentOnlineState != eOnlineState.Remote)
            {
                if(dataMsg.Name.Contains("S1"))
                {
                    //S1 Message 는 처리해야함.
                }
                else
                {
                    return;
                }
            }
            lock (thisLock)
            {
                if (dataMsg == null)
                {
                    return;
                }
                SendingQueue.Enqueue(dataMsg);
            }
        }

        #region HSMS 이벤트 핸들러
        private void OpenHSMS_ErrorOccured(object sender, HSMSErrorEventArgs e)
        {
            LogManager.WriteConsoleLog(eLogLevel.Error, "OpenHSMS Error ==> \r\n" + e.ErrorMessage);
        }

        private void OpenHSMS_OnStateChanged(object sender, StateChangedEventArgs e)
        {
            this.Enabled = (e.TcpConnectionState == TcpConnectionState.Connected && e.SelectState == SelectState.Selected) ? true : false;

            if (this.Enabled)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "OpenHSMS Host Connected");
            }
            else
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "OpenHSMS Host disconnected");
            }
            HSMSStateChangedEventArgs ev = new HSMSStateChangedEventArgs();
            ev.Enabled = this.Enabled;
            StateChanged?.Invoke(this, ev);
        }

        private void OpenHSMS_OnDataMessageReceived(object sender, DataMessageReceivedEventArgs e)
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Thread ID : {1}", string.Format("HSMS.OpenHSMS_Recv({0})", e.Message.FullName), Thread.CurrentThread.ManagedThreadId);

            DataMessage dataMsg = e.Message;

            lock (thisRcvLock)
            {
                this.ReceiveQueue.Enqueue(dataMsg);
            }


        }
        #endregion

        #region Helper Methods
        private void SendRun()
        {
            bool bExitFlag = false;
            DataMessage dataMsg = null;
            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Thread ID : {1}", "HSMS.SendRun()", Thread.CurrentThread.ManagedThreadId);

            //if (!this.Enabled)
            //{
            //    if (!m_OpenHSMS.IsRunning)
            //    {
            //        m_OpenHSMS.Start();                    
            //    }
            //}

            DateTime dtStart = DateTime.Now;
            while (!bExitFlag)
            {
                if (this.ThreadStop)
                {
                    break;
                }

                if (this.Enabled && (SendingQueue.Count > 0))
                {
                    if (!SendingQueue.TryDequeue(out dataMsg))
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "MessageQueue Dequeue NG - Count : {0}", SendingQueue.Count);
                        continue;
                    }
                    if (dataMsg == null)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "MessageQueue Data Null - Count : {0}", SendingQueue.Count);
                        continue;
                    }

                    SendMessage(dataMsg);

                    HSMSLogToDB(false, dataMsg);
                }
                Thread.Sleep(30);
            }
        }

        private void RecvRun()
        {
            bool bExitFlag = false;
            DataMessage dataMsg = null;
            LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Thread ID : {1}", "HSMS.RecvRun()", Thread.CurrentThread.ManagedThreadId);

            DateTime dtStart = DateTime.Now;
            string msgKey = string.Empty;
            while (!bExitFlag)
            {
                if (this.ThreadStop)
                {
                    break;
                }
                if (this.Enabled && (this.ReceiveQueue.Count > 0))
                {
                    if (!this.ReceiveQueue.TryDequeue(out dataMsg))
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "ReceiveQueue Dequeue NG - Count : {0}", this.ReceiveQueue.Count);
                        continue;
                    }

                    if (dataMsg == null)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "ReceiveQueue Data Null - Count : {0}", this.ReceiveQueue.Count);
                        continue;
                    }

                    if (dataMsg.IsPrimaryMessage)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "<Rcv> SF:{0}, SB:{1}", dataMsg.Name, dataMsg.SystemBytes);
                        ProcessPrimaryMessage(dataMsg);
                    }
                    else //PrimaryMessage 아닌것중 특정메시지는 받아서 처리해야한다.
                    {
                        switch (dataMsg.Name)
                        {
                            case "S1F14": //Establish Communication Request Acknowledage
                                LogManager.WriteConsoleLog(eLogLevel.Info, "H->E  S1F14 Host Sent Establish Communication Request");
                                if ((byte)dataMsg.Body.ChildItems[0].Value == 0)
                                {
                                    GlobalData.Current.MainBooth.MCSComEstablishState = eCommunicationEstablishState.Established; //연결 확립
                                }
                                break;

                            case "S2F18": //Data and Time Data
                                DateTime dt = DateTime.ParseExact(dataMsg.Body.Value.ToString(), "yyyyMMddHHmmssFF", CultureInfo.InvariantCulture);
                                LogManager.WriteConsoleLog(eLogLevel.Info, "H->E  S2F18 Host Sent DateTime : {0}", dt);
                                MainHelper.SetLocaltime(dt); //로컬 시간 설정
                                break;
                        }
                    }

                    HSMSLogToDB(true, dataMsg);
                }

                Thread.Sleep(100);
            }
        }

        private void SendMessage(DataMessage dataMessage)
        {
            try
            {
                if (GlobalData.Current.MainBooth.CurrentOnlineState == eOnlineState.Offline_EQ || (!this.m_OpenHSMS.IsRunning))
                {
                    // return;
                }

                this.m_OpenHSMS.SendDataMessage((DataMessage)dataMessage);
                if (dataMessage.Name == "S6F11" && dataMessage.Body != null && dataMessage.Body.HasChildItems)
                {
                    UInt16 ceid = (UInt16)dataMessage.Body.ChildItems[1].Value;
                    eCEIDName ceidName = (eCEIDName)ceid;
                    LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Thread ID : {1}", string.Format("HSMS.OpenHSMS_Send({0} CEID = {1} {2})", dataMessage.FullName, ceid, ceidName), Thread.CurrentThread.ManagedThreadId);
                }
                else
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Thread ID : {1}", string.Format("HSMS.OpenHSMS_Send({0})", dataMessage.FullName), Thread.CurrentThread.ManagedThreadId);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, "SendMessage(1) Thread ID : {0}, {1}", Thread.CurrentThread.ManagedThreadId, ex);
            }
        }

		#endregion

        #region HSMS 메세지 전송 메서드들
        /// <summary>
        /// ProcessPrimaryMessage
        /// </summary>
        /// <param name="primaryMsg"></param>
        private void ProcessPrimaryMessage(DataMessage primaryMsg)
        {
            DataMessage dataMsg = null;
            object outResult = null;
            bool isValidSend = false;
            string etcData = string.Empty; //해당 값은 PortID, RCODE등이 될수 있음.        
            try
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, string.Format("ProcessPrimaryMessage StreamFunction = {0}  Start", primaryMsg.Name));
                if (GlobalData.Current.MainBooth.CurrentOnlineState != eOnlineState.Remote)
                {
                    if (!primaryMsg.Name.Contains("S1F")) //S1 only
                    {
                        //S1 메시지 외 거절함
                        return;
                    }
                }
                switch (primaryMsg.Name)
                {
                    case "S1F1":                    // S1F1(Are you there Request)
                        dataMsg = m_OpenHSMS.GetReplyDataMessage(primaryMsg);
                        dataMsg.Body = HMsgActions.DoAction_S1F1(primaryMsg);
                        break;

                    case "S1F3":                    // S1F3(Selected Equipment Status Request)
                        dataMsg = m_OpenHSMS.GetReplyDataMessage(primaryMsg);
                        dataMsg.Body = HMsgActions.DoAction_S1F3(primaryMsg);
                        break;
                    case "S1F13":                    // S1F13(Establish Commnunication Request)
                        dataMsg = m_OpenHSMS.GetReplyDataMessage(primaryMsg);
                        dataMsg.Body = HMsgActions.DoAction_S1F13(primaryMsg);
                        break;
                    case "S1F15":                    // S1F15(Request OFF-LINE )
                        dataMsg = m_OpenHSMS.GetReplyDataMessage(primaryMsg);
                        dataMsg.Body = HMsgActions.DoAction_S1F15(primaryMsg);
                        break;
                    case "S1F17":                    // S1F17(Request ON-LINE )
                        dataMsg = m_OpenHSMS.GetReplyDataMessage(primaryMsg);
                        dataMsg.Body = HMsgActions.DoAction_S1F17(primaryMsg);
                        break;

                    case "S2F31":                   // S2F31(Date and Time Set Send)
                        dataMsg = m_OpenHSMS.GetReplyDataMessage(primaryMsg);
                        dataMsg.Body = this.HMsgActions.DoAction_S2F31(primaryMsg);
                        break;
                    case "S2F41":                   // S2F41(Host Commmand Sent)
                        dataMsg = m_OpenHSMS.GetReplyDataMessage(primaryMsg);
                        dataMsg.Body = this.HMsgActions.DoAction_S2F41(primaryMsg);
                        break;
                    case "S2F49":                   // S2F49(Enhanced Host Commmand Sent)
                        dataMsg = m_OpenHSMS.GetReplyDataMessage(primaryMsg);
                        dataMsg.Body = this.HMsgActions.DoAction_S2F49(primaryMsg);
                        break;
                    //case "S5F5":                   // S5F5 (List Alarm Request) //230414 RGJ  MCS 사양 변경.
                    //    dataMsg = m_OpenHSMS.GetReplyDataMessage(primaryMsg);
                    //    dataMsg.Body = this.HMsgActions.DoAction_S5F5(primaryMsg);
                    //    break;
                    case "S10F3":
                        dataMsg = m_OpenHSMS.GetReplyDataMessage(primaryMsg);
                        dataMsg.Body = this.HMsgActions.DoAction_S10F3(primaryMsg);
                        break;
                    default:
                        byte stream = primaryMsg.Stream;
                        byte function = primaryMsg.Function;

                        // S9F3 - Unrecognized Stream Type
                        // Stream이 1 ~ 10 또는 64가 아닌 경우 인식할 수 없는 Stream(S9F3) 처리
                        if (!((stream > 0 && stream < 15) || stream == 64))
                        {
                            //dataMsg = m_OpenHSMS.MakeDataMessage(9, 3, false);
                            //byte[] headerBytes = primaryMsg.Header.GetHeaderBytes();
                            //dataMsg.Body = new DataItem(ItemFormatCode.Bin, headerBytes);
                            throw new HSMSException(HSMSExceptionTypeList.Invalid_Stream, string.Format("Unrecognized Stream Type({0})", stream));
                        }
                        else
                        {
                            // S9F5 - Unrecognized Function Type
                            // Function이 206 보다 큰 경우 인식할 수 없는 Function(S9F5) 처리
                            if (function > 206)
                            {
                                //dataMsg = m_OpenHSMS.MakeDataMessage(9, 5, false);
                                //byte[] headerBytes = primaryMsg.Header.GetHeaderBytes();
                                //dataMsg.Body = new DataItem(ItemFormatCode.Bin, headerBytes);
                                throw new HSMSException(HSMSExceptionTypeList.Invalid_Function, string.Format("Unrecognized Function Type({0})", function));
                            }
                        }
                        break;
                }
            }
            catch (HSMSException ex)
            {
                if (ex.ExceptionType == HSMSExceptionTypeList.Invalid_Stream ||
                    ex.ExceptionType == HSMSExceptionTypeList.Invalid_Function ||
                    ex.ExceptionType == HSMSExceptionTypeList.Illegal_Data ||
                    ex.ExceptionType == HSMSExceptionTypeList.Data_TooLong)
                {
                    var function = 0;
                    if (ex.ExceptionType == HSMSExceptionTypeList.Invalid_Stream)
                    {
                        function = 3;
                    }
                    else if (ex.ExceptionType == HSMSExceptionTypeList.Invalid_Function)
                    {
                        function = 5;
                    }
                    else if (ex.ExceptionType == HSMSExceptionTypeList.Illegal_Data)
                    {
                        function = 7;
                    }
                    else if (ex.ExceptionType == HSMSExceptionTypeList.Data_TooLong)
                    {
                        function = 11;
                    }
                    dataMsg = m_OpenHSMS.MakeDataMessage(9, function, false);
                    byte[] headerBytes = primaryMsg.Header.GetHeaderBytes();
                    dataMsg.Body = new DataItem(ItemFormatCode.Bin, headerBytes);
                    SendMessageAsync(dataMsg);
                    isValidSend = true;

                    LogManager.WriteConsoleLog(eLogLevel.Info, string.Format("{0}.{1} - HSMSException Name: {2}, SystemBytes: {3}, Invalid: {4}", this.GetType().Name, System.Reflection.MethodBase.GetCurrentMethod().Name, primaryMsg.Name, primaryMsg.SystemBytes, ex.ExceptionType));
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, "HSMS.ProcessPrimaryMessage() Exception: {0}", ex.ToString());
                dataMsg = m_OpenHSMS.GetReplyDataMessage(primaryMsg);
                dataMsg.Function = 0;
                isValidSend = true;
                SendMessageAsync(dataMsg);

            }

            if (!isValidSend && primaryMsg.WBit && dataMsg != null)
            {
                SendMessageAsync(dataMsg);

                if (dataMsg.Body != null)
                {
                    PrimaryMessagePostAction(primaryMsg, dataMsg, outResult);
                }
            }
            LogManager.WriteConsoleLog(eLogLevel.Info, string.Format("ProcessPrimaryMessage StreamFunction = {0}  End", primaryMsg.Name));
        }
        /// <summary>
        /// PrimaryMessagePostAction 
        /// 호스트로 부터 받은 메시지에 응답 보낸 후 작업을 처리한다.
        /// </summary>
        /// <param name="primaryMsgName">메세지 명(Stream function name)</param>
        /// <param name="primaryReplyMsg">Reply 메세지</param>
        private void PrimaryMessagePostAction(DataMessage primaryMsg, DataMessage primaryReplyMsg, object outResult)
        {
            string moduleID = string.Empty;

            try
            {
                switch (primaryMsg.Name)
                {
                    case "S1F13":
                        int COMACK = Convert.ToInt32(primaryReplyMsg.Body.ChildItems[0].Value.ToString());
                        if (COMACK == 0)
                        {
                            GlobalData.Current.MainBooth.MCSComEstablishState = eCommunicationEstablishState.Established;
                        }
                        else
                        {
                            SendMessageAsync("S1F13");
                        }
                        break;
                    case "S1F15":
                        int OFLACK = Convert.ToInt32(primaryReplyMsg.Body.Value.ToString());
                        if (OFLACK == 0)
                        {
                            GlobalData.Current.MainBooth.CurrentOnlineState = eOnlineState.Offline_EQ;
                        }
                        break;

                    case "S1F17":

                        int ONLACK = Convert.ToInt32(primaryReplyMsg.Body.Value.ToString());
                        if (ONLACK == 0 || ONLACK == 2) //[230503 CIM 검수] Remote 에서 Remote 되는건 문제 없음
                        {
                            GlobalData.Current.MainBooth.CurrentOnlineState = eOnlineState.Remote;
                        }
                        else
                        {
                            GlobalData.Current.MainBooth.MCSComEstablishState = eCommunicationEstablishState.EstablishReqSent;
                            SendMessageAsync("S1F13");
                        }
                        break;
                    case "S2F31": //TimeSet 여기서 PLC 로 시간 동기화 명령 내린다.
                        int TACK = Convert.ToInt32(primaryReplyMsg.Body.Value.ToString());
                        if (TACK == 0 || TACK == 4)
                        {
                            //이미 LocalSystemTime 변경되었으므로 PLC 타임 변경 요청함.
                            GlobalData.Current.MainBooth.SyncPLC_Time(); //PLC Time Sync
                        }
                        break;
                    case "S2F41": //SuHwan_20220526 : S2f41 다음 작업 추가
                        string RCMDBuffer = primaryMsg.Body.ChildItems[0].Value.ToString().Trim();
                        string CommandIDBuffer = "";
                        try
                        {
                            CommandIDBuffer = primaryMsg.Body.ChildItems[1].ValueList[0].ValueList[1].Value.ToString().Trim();
                        }
                        catch (Exception)
                        {
                            CommandIDBuffer = "";
                        }
                        int nHACK = int.Parse(primaryReplyMsg.Body.ChildItems[0].Value.ToString());

                        if (nHACK == 0 || nHACK == 4)
                        {
                            switch (RCMDBuffer)
                            {
                                case "CANCEL":
                                    var cJobItem = GlobalData.Current.McdList.Where(c => c.CommandID == CommandIDBuffer).FirstOrDefault();
                                    if (cJobItem != null)
                                    {
                                        SendS6F11(206, "JOBDATA", cJobItem, "COMMANDID", CommandIDBuffer); //TransferCancelInitiated 추가사양 보고
                                        if (cJobItem.TCStatus != eTCState.QUEUED) //여기서 Cancel Fail 보고
                                        {
                                            SendS6F11(205, "JOBDATA", cJobItem, "COMMANDID", CommandIDBuffer); //TransferCancelFailed 
                                        }
                                        else
                                        {
                                            GlobalData.Current.McdList.DeleteMcsJob(cJobItem); //여기서 Cancel 완료보고 함.
                                        }
                                    }
                                    break;
                                case "ABORT":
                                    var aJobItem = GlobalData.Current.McdList.Where(c => c.CommandID == CommandIDBuffer).FirstOrDefault();
                                    if (aJobItem != null)
                                    {
                                        SendS6F11(203, "JOBDATA", aJobItem, "COMMANDID", CommandIDBuffer); //TransferAbortInitiated 추가사양 보고
                                        if (aJobItem.TCStatus != eTCState.TRANSFERRING && aJobItem.TCStatus != eTCState.PAUSED)
                                        {
                                            SendS6F11(202, "JOBDATA", aJobItem, "COMMANDID", CommandIDBuffer); //TransferAbortFailed
                                        }
                                        else if (aJobItem.CarrierLocationItem is CV_BaseModule cv)
                                        {
                                            if (cv.PortInOutType == ePortInOutType.OUTPUT)
                                            {
                                                //240508 RGJ 현재 위치가 포트인 작업은 Abort 불가
                                                SendS6F11(202, "JOBDATA", aJobItem, "COMMANDID", CommandIDBuffer); //TransferAbortFailed
                                            }
                                        }
                                        else
                                        {
                                            aJobItem.SetJobAbort(true);
                                        }
                                    }
                                    break;
                                case "PORTTYPECHANGE":
                                    var pItem = GlobalData.Current.PortManager.GetCVModule(CommandIDBuffer);
                                    ushort PortType = (ushort)primaryMsg.Body.ChildItems[1].ChildItems[1].ChildItems[1].Value;
                                    var TargetLine = GlobalData.Current.PortManager.GetLineModule(pItem.LineName);
                                    if (TargetLine != null)
                                    {
                                        ePortInOutType RequestType = (ePortInOutType)PortType;
                                        TargetLine.ChangeAllPortInOutType(RequestType); //실패보고는 따로 없음 개별 포트가 PortTypeChanged 보고 올림
                                    }
                                    break;
                                case "BUZZER":
                                    var pbItem = GlobalData.Current.PortManager.GetCVModule(CommandIDBuffer);
                                    pbItem.SetBuzzerState(eBuzzerControlMode.BuzzerON, true); //Buzzer 명령 PLC 로 보낸다.
                                    break;

                                case "CARRIERINFOUPDATE":  //2024.06.27 lim,  Autokeyin Data 입력
                                    //CarrierID로 Port 검색
                                    //Product_empty 입력
                                    //Pallet_size 입력
                                    //var portItem = GlobalData.Current.PortManager.GetCVModule(CommandIDBuffer);

                                    ushort Product_Empty = (ushort)primaryMsg.Body.ChildItems[1].ChildItems[1].ChildItems[1].Value;
                                    string Pallet_size = (string)primaryMsg.Body.ChildItems[1].ChildItems[2].ChildItems[1].Value;

                                    CarrierItem cItem = CarrierStorage.Instance.GetCarrierItem(CommandIDBuffer);
                                    //string 
                                    var portItem = GlobalData.Current.PortManager.AllCVList.Where(c => c.PC_CarrierID == CommandIDBuffer && c.CarrierExist == true).FirstOrDefault();
                                    
                                    if (portItem != null)
                                    {
                                        LogManager.WriteConsoleLog(eLogLevel.Info, "CARRIERINFOUPDATE WRITE START PORT:{0}", portItem.ModuleName);
                                        if(Pallet_size == "SHORT")
                                            portItem.PC_PalletSize = ePalletSize.Cell_Short;
                                        else
                                            portItem.PC_PalletSize = ePalletSize.Cell_Long;

                                        portItem.PC_ProductEmpty = (eProductEmpty)Product_Empty;
                                        //portItem.CarrierState = eCarrierState.WAIT_IN;

                                        portItem.SetAutoKeyinState(Pallet_size, false);

                                        LogManager.WriteConsoleLog(eLogLevel.Info, "CARRIERINFOUPDATE WRITE END PORT:{0}", portItem.ModuleName);
                                    }
                                    else //Port 조회가 안되는 경우가 없어야 하지만 로그는 찍어둔다.
                                    {
                                        LogManager.WriteConsoleLog(eLogLevel.Info, "CARRIERINFOUPDATE PORT SEARCH FAIELD! CMD: {0}", CommandIDBuffer);
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                        else
                        {
                            //230414 RGJ Cancel,Abort Nak 시 Fail 보고 안함.
                            //McsJob JobBuffer = new McsJob();
                            //switch (RCMDBuffer)
                            //{
                            //    case "CANCEL":
                            //        JobBuffer = GlobalData.Current.McdList.Where(c => c.CommandID == CommandIDBuffer).FirstOrDefault();
                            //        SendS6F11(205, "JOBDATA", JobBuffer, "COMMANDID", CommandIDBuffer);
                            //        break;

                            //    case "ABORT":
                            //        JobBuffer = GlobalData.Current.McdList.Where(c => c.CommandID == CommandIDBuffer).FirstOrDefault();
                            //        SendS6F11(202, "JOBDATA", JobBuffer, "COMMANDID", CommandIDBuffer);
                            //        break;
                            //    default:
                            //        break;
                            //}
                            switch (RCMDBuffer)
                            {
                                case "CARRIERINFOUPDATE":  //2024.06.27 lim,  Autokeyin Data Mismatch alarm
                                    ushort Product_Empty = (ushort)primaryMsg.Body.ChildItems[1].ChildItems[1].ChildItems[1].Value;
                                    string Pallet_size = (string)primaryMsg.Body.ChildItems[1].ChildItems[2].ChildItems[1].Value;

                                    var portItem = GlobalData.Current.PortManager.AllCVList.Where(c => c.PC_CarrierID == CommandIDBuffer && c.CarrierExist == true).FirstOrDefault();

                                    if (portItem != null)
                                    {
                                        portItem.SetAutoKeyinState(Pallet_size, true);
                                    }
                                    break;
                                default:
                                    break;
                            }

                        }


                        break;
                    //case "S2F41":
                    //    string strCMD = primaryReplyMsg.Body.ChildItems[0].Value.ToString().Trim();
                    //    int nHACK = int.Parse(primaryReplyMsg.Body.ChildItems[1].Value.ToString());
                    //    string sCommand = string.Empty;
                    //    if (nHACK == 0 || nHACK == 4)
                    //    {
                    //        switch (strCMD)
                    //        {
                    //            case RemoteCommandNames.ABORT:
                    //                HSMSHelper.HostCommandAbortAction(primaryMsg);
                    //                break;
                    //            case RemoteCommandNames.CANCEL:     //"CANCEL"
                    //                HSMSHelper.HostCommandCancelAction(primaryMsg);
                    //                break;
                    //            case RemoteCommandNames.RETRY:
                    //                HSMSHelper.HostCommandRetryAction(primaryMsg);
                    //                break;
                    //            case RemoteCommandNames.INSTALL:    //"INSTALL"
                    //                HSMSHelper.HostCommandInstallAction(primaryMsg);
                    //                break;
                    //            case RemoteCommandNames.SCAN:
                    //                HSMSHelper.HostCommandScanAction(primaryMsg);
                    //                break;
                    //            case RemoteCommandNames.REMOVE:
                    //                HSMSHelper.HostCommandRemoveAction(primaryMsg);
                    //                break;
                    //            case RemoteCommandNames.PAUSE:      //"PAUSE"
                    //                HSMSHelper.HostCommandPauseAction(primaryMsg);
                    //                break;
                    //            case RemoteCommandNames.RESUME:     //"RESUME"
                    //                HSMSHelper.HostCommandResumeAction(primaryMsg);
                    //                break;
                    //            case RemoteCommandNames.UPDATE:     //"UPDATE"
                    //                HSMSHelper.HostCommandUpdateAction(primaryMsg);
                    //                break;
                    //            case RemoteCommandNames.PORTTYPECHG:
                    //                HSMSHelper.HostCommandPortTypeChangeAction(primaryMsg);
                    //                break;
                    //            case RemoteCommandNames.PORTSTATUSCHG:
                    //                HSMSHelper.HostCommandPortStatusChangeAction(primaryMsg);
                    //                break;
                    //        }
                    //    }

                    //    break;

                    //case "S2F49":
                    //    HSMSHelper.HostTransferCommandFailAction(primaryReplyMsg);
                    //    break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "HSMSManager({0}) Exception: {1}", primaryMsg.Name, ex.ToString());
            }
        }

        /// <summary>
        /// HSMS 메세지 생성 메소드
        /// </summary>
        /// <param name="messageName">메세지 명</param>
        /// <param name="args">Arguments</param>
        /// <returns>생성된 메소드</returns>
        private DataMessage MakeDataMessage(string messageName, Dictionary<string, object> args = null)
        {
            //int nCEID = 0;
            string msgName = messageName.ToUpper();
            DataMessage dataMsg = null;
            if (GlobalData.Current.MainBooth == null)
            {
                return null;
            }

            if (GlobalData.Current.MainBooth.CurrentOnlineState != eOnlineState.Remote)
            {
                if (msgName != "S6F11")
                {
                    return null;
                }
            }

            try
            {
                switch (msgName)
                {
                    case "S1F1": //Are You There Request
                        dataMsg = m_OpenHSMS.MakeDataMessage(1, 1);
                        dataMsg.Body = HMsgActions.MakeDataItem_S1F1(args);
                        break;
                    case "S1F13": //Establish Communication Request
                        dataMsg = m_OpenHSMS.MakeDataMessage(1, 13);
                        dataMsg.Body = HMsgActions.MakeDataItem_S1F13(args);
                        break;

                    case "S2F17": //Date and Time Request
                        dataMsg = m_OpenHSMS.MakeDataMessage(2, 17);
                        dataMsg.Body = HMsgActions.MakeDataItem_S2F17(args);
                        break;
                    case "S5F1":  //Alarm Report
                        dataMsg = m_OpenHSMS.MakeDataMessage(5, 1);
                        dataMsg.Body = HMsgActions.MakeDataItem_S5F1(args);
                        break;
                    case "S6F11":
                        dataMsg = m_OpenHSMS.MakeDataMessage(6, 11);
                        dataMsg.Body = HMsgActions.MakeDataItem_S6F11(args);
                        break;
                        //2024.06.18 lim, Alarm 보고 추가
                    case "S5F101":  //Alarm Report
                        dataMsg = m_OpenHSMS.MakeDataMessage(5, 101);
                        dataMsg.Body = HMsgActions.MakeDataItem_S5F101(args);
                        break;


                    default:
                        dataMsg = null;
                        break;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "HSMSManager({0}) Exception: {1}", msgName, ex.ToString());
                dataMsg = null;
            }

            return dataMsg;
        }
        #endregion


        public DataItem GetSVIDItem(int ItemNum)
        {
            DataItem diSVIDItemInfo = new DataItem(ItemFormatCode.List);
            switch (ItemNum)
            {
                //나머지 항목들 구현 예정
                case 5:
                    diSVIDItemInfo = HSMSSVID.SVID5_AlarmSet();
                    break;
                case 24:
                    diSVIDItemInfo = HSMSSVID.SVID24_ControlState();
                    break;
                case 26:
                    diSVIDItemInfo = HSMSSVID.SVID26_CraneState();
                    break;
                case 31:
                    diSVIDItemInfo = HSMSSVID.SVID31_EnHancedCarriers();
                    break;
                case 34:
                    diSVIDItemInfo = HSMSSVID.SVID34_EnHancedPorts();
                    break;
                //220517 조숭진 hsms 메세지 추가 s
                case 35:
                    diSVIDItemInfo = HSMSSVID.SVID35_EnHancedTransfers();
                    break;
                case 37:
                    diSVIDItemInfo = HSMSSVID.SVID37_EnHancedActiveZones();
                    break;
                case 60:
                    diSVIDItemInfo = HSMSSVID.SVID60_SCState();
                    break;
                case 87:
                    diSVIDItemInfo = HSMSSVID.SVID87_EnHancedShelves();
                    break;
                case 119:       //2024.09.06 lim, 리컨사일 알람 보고 사양 추가
                    diSVIDItemInfo = HSMSSVID.SVID119_EnhancedSpecificAlarmReports();
                    break;
                //220517 조숭진 hsms 메세지 추가 e
                default: //svid 번호 없는 케이스 처리
                    return null;
            }
            return diSVIDItemInfo;
        }
        public DataItem GetAllSVIDListItem()//
        {
            DataItem diSVIDDataInfo = new DataItem(ItemFormatCode.List);

            diSVIDDataInfo.AddChildItem(HSMSSVID.SVID5_AlarmSet());
            diSVIDDataInfo.AddChildItem(HSMSSVID.SVID24_ControlState());
            diSVIDDataInfo.AddChildItem(HSMSSVID.SVID26_CraneState());
            diSVIDDataInfo.AddChildItem(HSMSSVID.SVID31_EnHancedCarriers());
            diSVIDDataInfo.AddChildItem(HSMSSVID.SVID34_EnHancedPorts());
            diSVIDDataInfo.AddChildItem(HSMSSVID.SVID35_EnHancedTransfers());
            diSVIDDataInfo.AddChildItem(HSMSSVID.SVID37_EnHancedActiveZones());
            diSVIDDataInfo.AddChildItem(HSMSSVID.SVID60_SCState());
            diSVIDDataInfo.AddChildItem(HSMSSVID.SVID87_EnHancedShelves());
            //diSVIDDataInfo.AddChildItem(HSMSSVID.SVID119_EnhancedSpecificAlarmReports());     //2024.09.06 lim, 나중에 추가 및 SVID5_AlarmSet 주석 처리

            return diSVIDDataInfo;
        }
        /// <summary>
        /// DB col 9 항목에 넣어야 함.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private string GetCommandIDFrom_S6F11DataMessage(DataMessage data)
        {
            try
            {
                if (data == null || data.Name != "S6F11")
                {
                    return string.Empty;
                }
                string CommandID = string.Empty;
                int rptid = Convert.ToInt32(data.Body.ValueList[2].ValueList[0].ValueList[0].Value); //RPTID

                switch (rptid)
                {
                    case 1:
                    case 2:
                    case 3:
                    case 5:
                    case 10:
                        CommandID = data.Body.ValueList[2].ValueList[0].ValueList[1].ValueList[0].Value.ToString(); //CommandID
                        break;

                }
                return CommandID;
            }
            catch (Exception ex)
            {
                _ = ex;
                return string.Empty;
            }
        }
        /// <summary>
        /// DB col 10 항목에 넣어야 함.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private string GetCarrierIDFrom_S6F11DataMessage(DataMessage data)
        {
            try
            {
                if (data == null || data.Name != "S6F11")
                {
                    return string.Empty;
                }

                string CarrierID = string.Empty;
                int rptid = Convert.ToInt32(data.Body.ValueList[2].ValueList[0].ValueList[0].Value); //RPTID

                switch (rptid)
                {
                    case 2:
                    case 3:
                        CarrierID = data.Body.ValueList[2].ValueList[0].ValueList[1].ValueList[1].Value.ToString(); //CarrierID
                        break;
                    case 4:
                        CarrierID = data.Body.ValueList[2].ValueList[0].ValueList[1].ValueList[3].Value.ToString(); //CarrierID
                        break;
                    case 7:
                    case 8:
                    case 12:
                        CarrierID = data.Body.ValueList[2].ValueList[0].ValueList[1].ValueList[0].Value.ToString(); //CarrierID
                        break;
                    case 10:
                        CarrierID = data.Body.ValueList[2].ValueList[0].ValueList[1].ValueList[2].Value.ToString(); //CarrierID
                        break;
                }


                return CarrierID;
            }
            catch (Exception ex)
            {
                _ = ex;
                return string.Empty;
            }
        }

        private string GetUnitIDFrom_S6F11DataMessage(DataMessage data)
        {
            try
            {
                if (data == null || data.Name != "S6F11")
                {
                    return string.Empty;
                }

                string UnitID = string.Empty;
                int rptid = Convert.ToInt32(data.Body.ValueList[2].ValueList[0].ValueList[0].Value); //RPTID

                switch (rptid)
                {
                    case 1:
                        UnitID = data.Body.ValueList[2].ValueList[0].ValueList[1].ValueList[2].ValueList[0].Value.ToString();
                        break;
                    case 2:
                        UnitID = data.Body.ValueList[2].ValueList[0].ValueList[1].ValueList[2].Value.ToString();
                        break;
                    case 3:
                        UnitID = data.Body.ValueList[2].ValueList[0].ValueList[1].ValueList[2].Value.ToString();
                        break;
                    case 4:
                        UnitID = data.Body.ValueList[2].ValueList[0].ValueList[1].ValueList[0].Value.ToString();
                        break;
                    case 5:
                        UnitID = data.Body.ValueList[2].ValueList[0].ValueList[1].ValueList[1].Value.ToString(); 
                        break;
                    case 7:
                        UnitID = data.Body.ValueList[2].ValueList[0].ValueList[1].ValueList[1].Value.ToString();
                        break;
                    case 9:
                        UnitID = data.Body.ValueList[2].ValueList[0].ValueList[1].ValueList[0].Value.ToString();
                        break;
                    case 11:
                        UnitID = data.Body.ValueList[2].ValueList[0].ValueList[1].ValueList[0].Value.ToString();
                        break;
                    case 14:
                        UnitID = data.Body.ValueList[2].ValueList[0].ValueList[1].ValueList[0].Value.ToString();
                        break;
                }
                return UnitID;
            }
            catch (Exception ex)
            {
                _ = ex;
                return string.Empty;
            }
        }

        //로그용 MSG NM 컬럼 항목을  추출한다.
        private string GetMessageNameForDB(DataMessage data)
        {
            try
            {
                if (data == null)
                {
                    return string.Empty;
                }
                string msg = string.Empty;
                switch (data.Name)
                {
                    case "S2F41": //RCMD
                        msg = data.Body.ValueList[0].Value.ToString();
                        break;
                    case "S2F49":
                        msg = data.Body.ValueList[2].Value.ToString();
                        break;
                    case "S6F11": //CEID 
                        ushort ceid = (UInt16)data.Body.ChildItems[1].Value;
                        eCEIDName ceidName = (eCEIDName)ceid;
                        msg = ceidName.ToString();
                        break;
                    default:
                        msg = StreamFunctionNameConverter(data.Name);
                        break; // 나머지는 기본 메시지 이름 리턴
                }
                return string.Format("{0} ({1})", msg, data.Name);
            }
            catch(Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return string.Empty;
            }
        }

        private string GetUnitIDForDB(DataMessage data)
        {
            try
            {
                if (data == null)
                {
                    return string.Empty;
                }
                string unitID = string.Empty;
                if(data.Name == "S2F41")
                {
                    switch (data.Body.ValueList[0].Value.ToString())
                    {
                        case "INSTALL":
                            unitID = data.Body.ValueList[1].ValueList[1].ValueList[1].Value.ToString();
                            break;
                        case "BUZZER":
                            unitID = data.Body.ValueList[1].ValueList[0].ValueList[1].Value.ToString();
                            break;
                        case "CARRIERGEN":
                            unitID = data.Body.ValueList[1].ValueList[1].ValueList[1].Value.ToString();
                            break;
                        case "PORTTYPECHANGE":
                            unitID = data.Body.ValueList[1].ValueList[0].ValueList[1].Value.ToString();
                            break;
                    }

                }
                else if(data.Name == "S6F11")
                {
                    unitID = GetUnitIDFrom_S6F11DataMessage(data);
                }
                else if(data.Name == "S5F1")
                {
                    unitID = data.Body.ValueList[3].Value.ToString();
                }
                else if (data.Name == "S5F101")
                {
                    unitID = data.Body.ValueList[3].Value.ToString();
                }
                return unitID;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return string.Empty;
            }
        }
        /// <summary>
        /// S2F49 => Enhanced_Host_Command_Send 변환 함수
        /// </summary>
        /// <param name="SFName"></param>
        /// <returns>HSMS message name</returns>
        public string StreamFunctionNameConverter(string SFName)
        {
            if(SFName.Contains("F0"))
            {
                return eHSMSMessageName.Abort_Transaction.ToString();
            }    
            switch(SFName)
            {
                case "S1F1"  : return eHSMSMessageName.Are_You_There_Request.ToString();
                case "S1F2"  : return eHSMSMessageName.On_Line_Data.ToString();
                case "S1F3"  : return eHSMSMessageName.Selected_Equipment_Status_Request.ToString();
                case "S1F4"  : return eHSMSMessageName.Selected_Equipment_Status_Data.ToString();  
                case "S1F13" : return eHSMSMessageName.Establish_Communication_Request.ToString();    
                case "S1F14" : return eHSMSMessageName.Establish_Communication_Request_Ack.ToString();
                case "S1F15" : return eHSMSMessageName.Request_OFFLINE.ToString();                    
                case "S1F16" : return eHSMSMessageName.Request_OFFLINE_Ack.ToString();                
                case "S1F17" : return eHSMSMessageName.Request_ONLINE.ToString();                     
                case "S1F18" : return eHSMSMessageName.Request_ONLINE_Ack.ToString();                 
                case "S2F17" : return eHSMSMessageName.Date_and_Time_Request.ToString();              
                case "S2F18" : return eHSMSMessageName.Date_and_Time_Send.ToString();                 
                case "S2F31" : return eHSMSMessageName.Date_and_Time_Set_Send.ToString();             
                case "S2F32" : return eHSMSMessageName.Date_and_Time_Set_Ack.ToString();              
                case "S2F41" : return eHSMSMessageName.Host_Command_Send.ToString();                  
                case "S2F42" : return eHSMSMessageName.Host_Command_Send_Ack.ToString();              
                case "S2F49" : return eHSMSMessageName.Enhanced_Host_Command_Send.ToString();         
                case "S2F50" : return eHSMSMessageName.Enhanced_Host_Command_Send_Ack.ToString();     
                case "S5F1"  : return eHSMSMessageName.Alarm_Report_Send.ToString();                  
                case "S5F2"  : return eHSMSMessageName.Alarm_Report_Ack.ToString();                   
                case "S5F5"  : return eHSMSMessageName.List_Alarm_Request.ToString();                 
                case "S5F6"  : return eHSMSMessageName.List_Alarm_Data.ToString();                    
                case "S5F101": return eHSMSMessageName.Alarm_Report_Send.ToString();    //2024.06.19 lim, alarm 보고 변경       
                case "S5F102": return eHSMSMessageName.Alarm_Report_Ack.ToString();
                case "S6F11" : return eHSMSMessageName.Event_Report_Send.ToString();                  
                case "S6F12" : return eHSMSMessageName.Event_Reprot_Ack.ToString();                   
                case "S9F1"  : return eHSMSMessageName.Unrecognized_Device_ID.ToString();             
                case "S9F3"  : return eHSMSMessageName.Unrecognized_Stream_Type.ToString();           
                case "S9F5"  : return eHSMSMessageName.Unrecognized_Function_Type.ToString();         
                case "S9F7"  : return eHSMSMessageName.Illegal_Data.ToString();                       
                case "S9F9"  : return eHSMSMessageName.Transaction_Timer_Timeout.ToString();          
                case "S9F11" : return eHSMSMessageName.Data_Too_Long.ToString();                      
                case "S9F13" : return eHSMSMessageName.Conversation_Timeout.ToString();               
                case "S10F1" : return eHSMSMessageName.Terminal_Request.ToString();                   
                case "S10F2" : return eHSMSMessageName.Terminal_Request_Ack.ToString();               
                case "S10F3" : return eHSMSMessageName.Terminal_Display_Single.ToString();            
                case "S10F4" :  return eHSMSMessageName.Terminal_Display_Single_Ack.ToString();        
            }
            return "NotDefined";
        }

        private void HSMSLogToDB(bool bDir, DataMessage data)
        {
            try
            {
                object[] newargs = new object[16];

                for (int i = 0; i < newargs.Length; i++)
                {
                    newargs[i] = string.Empty;
                }

                newargs[0] = "HSMS";
                //newargs[1] = data.Header.MakeTime.ToString("yyyy/MM/dd HH:mm:ss.fff");
                newargs[1] = data.Header.MakeTime.ToString("yyyy-MM-dd HH:mm:ss.fff").Replace("-", "/");
                newargs[2] = bDir ? "H → E" : "E → H";
                newargs[3] = data.Name;
                newargs[4] = data.SystemBytes;
                newargs[5] = GetMessageNameForDB(data);
                newargs[6] = GetUnitIDForDB(data);
                newargs[15] = string.Format("[{0}] {1} - {2}", newargs[2], newargs[1], data.ToSECS2LogString());      //SECS2 로그

                switch (data.Name)
                {
                    case "S1F13":
                    case "S1F15":
                    case "S1F17":
                    case "S1F4":
                        break;
                    case "S2F41":
                        switch (data.Body.ValueList[0].Value.ToString())
                        {
                            case "CANCEL":
                            case "ABORT":
                                newargs[10] = data.Body.ValueList[1].ValueList[0].ValueList[1].Value; //Command ID
                                break;
                            case "REMOVE":
                                newargs[11] = data.Body.ValueList[1].ValueList[0].ValueList[1].Value; //Carrier ID
                                break;
                            case "BUZZER":
                                //newargs[11] = data.Body.ValueList[1].ValueList[0].ValueList[1].Value;
                                break;
                            case "INSTALL":
                            case "PORTTYPECHANGE":
                            case "CARRIERGEN":
                            case "CARRIERINFOUPDATE":
                                newargs[11] = data.Body.ValueList[1].ValueList[0].ValueList[1].Value; //Carrier ID
                                break;
                        }
                        break;
                    case "S1F14":
                    case "S1F16":
                    case "S1F18":
                    case "S6F12":
                    case "S10F4":
                    case "S2F32":
                        newargs[9] = data.Body.Value;
                        break;
                    case "S2F31":
                        newargs[14] = data.Body.Value;
                        break;
                    case "S1F3":
                        //string datalist = string.Empty; //로그부 SVID 삭제
                        //for (int i = 0; i < data.Body.ValueList.Count; i++)
                        //{
                        //    if (i + 1 != data.Body.ValueList.Count)
                        //        datalist += data.Body.ValueList[i].Value + ",";
                        //    else
                        //        datalist += data.Body.ValueList[i].Value;
                        //}
                        //newargs[6] = datalist; 
                        break;
                    case "S6F11":
                        newargs[7] = data.Body.ValueList[1].Value; //CEID
                        int ceid = Convert.ToInt32(newargs[7]);
                        if (!(ceid == 1 || ceid == 3 || ceid == 103 || ceid == 105 || ceid == 106 || ceid == 107))  //조건에 있는 ceid는 rptid가 없다.
                            newargs[8] = data.Body.ValueList[2].ValueList[0].ValueList[0].Value; //RPTID
                        newargs[10] = GetCommandIDFrom_S6F11DataMessage(data); //CommandID
                        newargs[11] = GetCarrierIDFrom_S6F11DataMessage(data); //CarrierID
                        break;
                    case "S2F49":
                        //newargs[5] = data.Body.ValueList[2].Value; //위에서 통합 처리
                        newargs[10] = data.Body.ValueList[3].ValueList[0].ValueList[1].ValueList[0].ValueList[1].Value; //CommandID
                        newargs[11] = data.Body.ValueList[3].ValueList[1].ValueList[1].ValueList[0].ValueList[1].Value; //CarrierID
                        newargs[12] = data.Body.ValueList[3].ValueList[1].ValueList[1].ValueList[1].ValueList[1].Value;
                        newargs[13] = data.Body.ValueList[3].ValueList[1].ValueList[1].ValueList[2].ValueList[1].Value;
                        break;
                    case "S10F3":
                        newargs[14] = data.Body.ValueList[1].Value;
                        break;
                    case "S2F42": //Host Command Ack 추가.
                    case "S2F50":
                        newargs[9] = data.Body.ValueList[0].Value;
                        break;
                    case "S5F1": //Alarm Report
                    case "S5F2": //Alarm Ack 추가.
                    case "S5F101": //Alarm Report   2024.06.19 lim, 알람 보고 변경
                    case "S5F102": //Alarm Ack 추가.
                        break;

                    default:
                        return;
                }

                GlobalData.Current.DBManager.DbSetProcedureLogInfo(newargs);
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }
    }
    public enum eHSMSMessageName
    {
        Abort_Transaction,                      // (S*F0)
        Are_You_There_Request,                  // (S1F1) 
        On_Line_Data,                           // (S1F2)
        Selected_Equipment_Status_Request,      // (S1F3)
        Selected_Equipment_Status_Data,         // (S1F4)
        Establish_Communication_Request,        // (S1F13)
        Establish_Communication_Request_Ack,    // (S1F14)
        Request_OFFLINE,                        // (S1F15)
        Request_OFFLINE_Ack,                    // (S1F16)
        Request_ONLINE,                         // (S1F17)
        Request_ONLINE_Ack,                     // (S1F18)
        Date_and_Time_Request,                  // (S2F17)
        Date_and_Time_Send,                     // (S2F18)
        Date_and_Time_Set_Send,                 // (S2F31)
        Date_and_Time_Set_Ack,                  // (S2F32)
        Host_Command_Send,                      // (S2F41)
        Host_Command_Send_Ack,                  // (S2F42)
        Enhanced_Host_Command_Send,             // (S2F49)
        Enhanced_Host_Command_Send_Ack,         // (S2F50)                                  
        Alarm_Report_Send,                      // (S5F1)(S5F101)
        Alarm_Report_Ack,                       // (S5F2)(S5F102)
        List_Alarm_Request,                     // (S5F5) 
        List_Alarm_Data,                        // (S5F6)                                      
        Event_Report_Send,                      // (S6F11)
        Event_Reprot_Ack,                       // (S6F12)                                
        Unrecognized_Device_ID,                 // (S9F1)
        Unrecognized_Stream_Type,               // (S9F3)
        Unrecognized_Function_Type,             // (S9F5) 
        Illegal_Data,                           // (S9F7)
        Transaction_Timer_Timeout,              // (S9F9)
        Data_Too_Long,                          // (S9F11)
        Conversation_Timeout,                   // (S9F13)                                   
        Terminal_Request,                       // (S10F1)
        Terminal_Request_Ack,                   // (S10F2)
        Terminal_Display_Single,                // (S10F3)
        Terminal_Display_Single_Ack,            // (S10F4)

    }

    public enum eRCMDName
    {
        PAUSE,
        RESUME,
        CANCEL,
        ABORT,
        INSTALL,
        REMOVE,
        PORT_TYPE_CHANGE,
    }

    public enum eCEIDName
    {
        Offline = 1,
        OnlineRemote = 3,
        AlarmCleared = 101,
        AlarmSet = 102,
        SCAutoCompleted = 103,
        SCPauseCompleted = 105,
        SCPaused = 106,
        SCPauseInitiated = 107,
        TransferAbortCompleted = 201,
        TransferAbortFail = 202,
        TransferAbortInitiated = 203,
        TransferCancelCompleted = 204,
        TransferCancelFailed = 205,
        TransferCancelInitiated = 206,
        TransferCompleted = 207,
        TransferInitiated = 208,
        TransferPaused = 209,
        TransferResumed = 210,
        CarrierInstallCompleted = 301,
        CarrierRemovedCompleted = 302,
        CarrierRemoved = 303,
        CarrierResumed = 304,
        CarrierStored = 305,
        CarrierStoredAlt = 306,
        Carriertransfering = 307,
        CarriertWaitIn = 308,
        CarriertWaitOut = 309,
        ZoneCapacityChange = 310,
        CarriertLocationChanged = 311,
        CarriertGeneratorRequest = 312,
        CarrierInfoRequest = 313,   //2024.06.26 lim, 임시번호 셀버퍼 자동 Keyin 기능 
        PortInService = 401,
        PortOutService = 402,
        PorttransferBlocked = 403,
        PortReadyToLoad = 404,
        PortReadyUnload = 405,
        PortTypeChange = 406,
        PortModeChange = 407,
        NotchingModeChange = 408,
        CarrierIDRead = 601,
        IDReadError = 604,
        OperatorInitiatedAction = 605,
        FireEmergencyAlarm = 606,
        CarrierLoadRequest = 607,
        CarrierUnloadRequest = 608,
        CraneAcitve = 701,
        CraneIdle = 702,
        CraneStateChangeed = 703,
        UnitAlarmCleared = 801,
        UnitAlarmSet = 802,
        ShelfInService = 901,
        ShelfOutService = 902,
        ShelfStatusChanged = 903,
    }


    public class MessageDictionaryNames
    {
        public const string MDLN = "MDLN";
        public const string CEID = "CEID";

        public const string JOBDATA = "JOBDATA";
        public const string COMMANDID = "COMMANDID";
        public const string CARRIERID = "CARRIERID";
        public const string PRIORITY = "PRIORITY";
        public const string SOURCE = "SOURCE";
        public const string DEST = "DEST";
        public const string MASKCSTID = "MASKCASSETTEID";
        public const string MIDLOC = "MIDLOC";
        public const string CASSETTE_SIZE = "CASSETTE_SIZE";
        public const string FINALLOCATION = "FINALLOCATION";
        public const string FLOORNUMBER = "FLOORNUMBER";
        public const string NEXTDEST = "NEXTDEST";
        public const string LIFTERPORT = "LIFTERPORT";
        public const string GLASSQUANTITY = "GLASSQUANTITY";
        public const string GLASSCHECKFLAG = "GLASSCHECKFLAG";
        public const string WIRECHECKFLAG = "WIRECHECKFLAG";
        public const string STOCKERCRANEID = "STOCKERCRANEID";
        public const string SEQNO = "SEQNO";
        public const string TARGETMODULEID = "TARGETMODULEID";

        public const string UNITID = "UNITID";
        public const string MODULEID = "MODULEID";
        public const string NEXTUNITID = "NEXTUNITID";
        public const string PORTID = "PORTID";
        public const string CARRIERLOC = "CARRIERLOC";
        public const string CRANEID = "CRANEID";
        public const string LOADERMODULE = "LOADERMODULE";
        public const string LIFTERMODULE = "LIFTERMODULE";
        public const string SHELFDATA = "SHELFDATA";
        public const string CARRIERINSTALLTYPE = "CARRIERINSTALLTYPE";
        public const string PORTPROCSTATE = "PORTPROCSTATE";
        public const string ALARM = "ALARM";
        public const string ALARMDATA = "ALARMDATA";

        public const string ZONENAME = "ZONENAME";
        public const string ZONECAPA = "ZONECAPA";
        public const string RESULTCODE = "RESULTCODE";
        public const string ECLIST = "ECLIST";
        public const string STOCKER_CNT = "STOCKER";
        public const string HANDOFF_CNT = "HANDOFF";
        public const string EQSHELF_CNT = "EQSHELF";
        public const string OTHER_CNT = "OTHER";
        public const string IDReadStatus = "IDReadStatus";
        public const string ReadCarrierID = "ReadCarrierID";
        public const string CstSize = "CstSize";

    }
}
