using System;
using System.Diagnostics;
using System.ServiceModel;
using System.Threading;
using System.Xml.Linq;
using Stockerfirmware;
using Stockerfirmware.Log;
using Stockerfirmware.Modules.Conveyor;
using WCF_LBS.Commands;
using WCF_LBS.DataParameter;
using WCF_LBS.Network;

namespace WCF_LBS
{
    //2020.09.04 RGJ WCF 통신 모듈 추가(기본인터페이스완료)
    //2020.09.03 RGJ WCF 통신 모듈 추가(개발중)

    /// <summary>
    /// 상위 LBS.LCS 와의 통신 수립.
    //  받은 메시지를 파싱해서 Firmware로 전달.
    /// </summary>
    public class WCFLBS_Manager : IStkControlServiceCallback ,IDisposable
    {
        public delegate void CommandEventHandler(object sender, WCFCommandEventArgs e);

        public event CommandEventHandler OnCraneCommandReceive; //로봇 동작 커맨드 이벤트

        public event CommandEventHandler OnPortCommandReceive; //포트 커맨드 이벤트

        public event CommandEventHandler OnTowerLampCommandReceive; //타워 램프 커맨드 이벤트

        public event CommandEventHandler OnSysCheckReceive; //최초 시스템 체크 

        public event CommandEventHandler OnIOMonitoringReceive; //IO 모니터링 창 요청

        public delegate void RawDataEventHandler(object sender, RawDataEventArgs e);

        public event RawDataEventHandler OnRawSend; // 

        public event RawDataEventHandler OnRawReceive; //

        private StkControlServiceClient client;
        private ClientInfo cinfo;
        public readonly string Uri;

        private static WCFLBS_Manager wcf_mgr;
        public static WCFLBS_Manager GetManagerInstance()
        {
            if(wcf_mgr == null)
            {
                wcf_mgr = new WCFLBS_Manager();
            }
            return wcf_mgr;
        }

        public WCFLBS_Manager()
        {
            //추후 XML 로 빼도록 검토
            cinfo = new ClientInfo();
            cinfo.IPAddress = "127.0.0.1";
            cinfo.UserID = "RYUGJ";
            cinfo.Description = "TestUser123";
            //Uri = "net.tcp://127.0.0.1:8004/StkControlService";
            Thread LCSCheckThread = new Thread(new ThreadStart(LCSConnectionMonitoring));
            LCSCheckThread.IsBackground = true;
            LCSCheckThread.Name = "LCSCheckThread";
            LCSCheckThread.Start();
        }
        /// <summary>
        /// -LCS 자동 재연결 기능 추가.
        /// LCS 연결 모니터링 해서 연결 리트라이
        /// </summary>
        public void LCSConnectionMonitoring()
        {
            try //-메인 루프 예외 발생시 로그 찍도록 추가.
            {
                while (true)
                {
                    if (client == null)
                    {
                        Thread.Sleep(1000);
                        continue;
                    }
                    if (client.State == CommunicationState.Opened || client.State == CommunicationState.Opening)
                    {
                        //연결된 상태 재연결 필요 없음
                    }
                    else
                    {
                        //프로세스가 존재하는지 확인
                        var ProcessArray = Process.GetProcessesByName("LBS.LCS");
                        if (ProcessArray.Length == 1)
                        {
                            Thread.Sleep(3000); //프로그램 로딩중일수도 있으니 3초대기후 연결을 붙여본다.
                            ReconnectWCF();
                        }
                    }
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }

        }
        public bool OpenWCF()
        {
            try
            {
                InstanceContext instanceContext = new InstanceContext(this);
                client = new StkControlServiceClient(instanceContext);
                client.Connect(cinfo);
                return client.State == CommunicationState.Opened;
            }
            catch (EndpointNotFoundException) //- WCF 연결 실패시 스택 트레이스 Open 안하고 로그만 표시
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "===================================================================================");
                LogManager.WriteConsoleLog(eLogLevel.Info, "LCS 연결에 실패하였습니다. URI : {0}",client.Endpoint.Address.Uri);
                LogManager.WriteConsoleLog(eLogLevel.Info, "===================================================================================");
                return false;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }
        }
        public void CloseWCF()
        {
            try
            {
                if (client != null)
                {
                    //통신이 열려 있으면 닫는다.
                    if (client.State == CommunicationState.Opened)
                    {
                        client.Disconnect(cinfo);
                    }
                    client.Close();
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }

        private void InnerChannel_Opened(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        public void ReconnectWCF()
        {
            try
            {
                if (client.State == CommunicationState.Opened || client.State == CommunicationState.Opening)
                {
                    //이미 연결된 상태 또는 연결 시도중. 접속 시도 취소.
                    LogManager.WriteConsoleLog(eLogLevel.Info, "ReconnectWCF() 이미 연결상태입니다. LCS Communicate State : {0}", client.State.ToString());
                    return;
                }
                InstanceContext instanceContext = new InstanceContext(this);
                client = new StkControlServiceClient(instanceContext);
                client.Connect(cinfo);
            }
            catch(Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }

        public CommunicationState GetLCS_CommunicationState()
        {
            return client.State;
        }

        //상위 LCS 에서 메시지가 오면 윈도우 WCF 모듈이 해당 함수 호출
        public int ReceiveString(out string recvStr, string sendStr)
        {
            try
            {
                OnRawReceive?.Invoke(this, new RawDataEventArgs(sendStr));
                XElement readDocu = XElement.Parse(sendStr);
                enumMessageName mName = (enumMessageName)Enum.Parse(typeof(enumMessageName), readDocu.Element("NAME").Value);//name 항목만 읽어보고 맞는 핸들러를 호출
                switch (mName)
                {
                    case enumMessageName.SYSTEM_CHECK:
                        recvStr = LCSHandler_SYSTEM_CHECK(readDocu);
                        break;
                    case enumMessageName.STATUS_DATA_REQUEST:
                        recvStr = LCSHandler_STATUS_REQUEST(readDocu);
                        break;
                    case enumMessageName.TOWERLAMP_SET:
                        recvStr = LCSHandler_TOWERLAMP_SET(readDocu);
                        break;
                    case enumMessageName.PORT_MANUAL:
                        recvStr = LCSHandler_PORT_MANUAL(readDocu);
                        break;
                    case enumMessageName.IO_MONITORING_REQUEST:
                        recvStr = LCSHandler_IO_MONITORING(readDocu);
                        break;
                    case enumMessageName.CRANE_GET:
                    case enumMessageName.CRANE_PUT:
                    case enumMessageName.CRANE_MOVE:
                    case enumMessageName.CRANE_EMO_GET:
                    case enumMessageName.CRANE_EMO_PUT:
                    case enumMessageName.CRANE_S_GET:
                    case enumMessageName.CRANE_S_PUT:
                    case enumMessageName.CRANE_START:
                    case enumMessageName.CRANE_STOP:
                    case enumMessageName.CRANE_ERROR_RESET:
                    case enumMessageName.CRANE_RETURN_HOME:
                    case enumMessageName.CRANE_EMO_RETURN_HOME:
                    case enumMessageName.CRANE_CHUCK:
                    case enumMessageName.CRANE_UNCHUCK:
                    case enumMessageName.CRANE_ATTEACH_START:
                    case enumMessageName.CRANE_ATTEACH_STOP:
                        recvStr = LCSHandler_CRANE_COMMAND(readDocu, mName);
                        break;
                    default:
                        recvStr = "";
                        break;
                }
                if (!string.IsNullOrEmpty(recvStr))
                { 
                    OnRawSend?.Invoke(this, new RawDataEventArgs(recvStr));
                }
                return 0;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                recvStr = "";
                return -1;
            }
        }

        /// <summary>
        /// 최초 접속시 시스템 체크
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private string LCSHandler_SYSTEM_CHECK(XElement x)
        {
            LCSMessageItem LItem = new LCSMessageItem();
            LItem.MsgType = enumMessageType.Reply;
            LItem.MsgName = enumMessageName.SYSTEM_CHECK;
            LItem.Uid = x.Element("UID").Value;
            LItem.Timestamp = GetCurrentTimeStamp();
            LItem.ReturnCode = "0";

            //시스템 메시지 비동기 이벤트를 발생 시킨다.
            OnSysCheckReceive?.BeginInvoke(this, new SysCheckCommandEventArgs(LItem),new AsyncCallback(EndAsyncEvent),null);
            return LItem.XMLRendering().ToString();
        }
        /// <summary>
        /// IO 모니터링 창 요청
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private string LCSHandler_IO_MONITORING(XElement x)
        { 
            LCSMessageItem LItem = new LCSMessageItem();
            LItem.MsgType = enumMessageType.Reply;
            LItem.MsgName = enumMessageName.IO_MONITORING_REQUEST;
            LItem.Uid = x.Element("UID").Value;
            LItem.Timestamp = GetCurrentTimeStamp();
            LItem.ReturnCode = "0";
            OnIOMonitoringReceive?.BeginInvoke(this,new WCFCommandEventArgs(LItem.MsgName, LItem.Uid, LItem.Timestamp),new AsyncCallback(EndAsyncEvent),null);
            return LItem.XMLRendering().ToString();
        }
        /// <summary>
        /// LBS 모듈 보고 요청
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        private string LCSHandler_STATUS_REQUEST(XElement x)
        {
            LCSMessageItem LItem = new LCSMessageItem();
            LItem.MsgType = enumMessageType.None;
            LItem.MsgName = enumMessageName.STATUS_DATA_REPORT;
            LItem.Uid = x.Element("UID").Value;
            LItem.Timestamp = GetCurrentTimeStamp();
            LItem.ReturnCode = "0";

            enumParaModuleType mType = (enumParaModuleType)Enum.Parse(typeof(enumParaModuleType), x.Element("PARAMETERS").Element("REQUEST_ITEMS").Value);
            switch (mType)
            {
                case enumParaModuleType.BOOTH:
                    LItem.Parameters.Add(GlobalData.Current.MainBooth.GetBoothStatusPara());
                    break;
                case enumParaModuleType.PORT: //포트 갯수만큼 반복필요
                    foreach(var lineItem in GlobalData.Current.LineManager.ModuleList)
                    {
                        foreach (var cItem in lineItem.Value.ModuleList)
                        {
                            if (cItem.CVModuleType != eCVType.OHTIF && cItem.CVModuleType != eCVType.TurnOHTIF) //OHT Interface 모듈은 따로 셔틀로 보고   //2021.05.25 lim, TurnOHT 추가
                            {
                                LItem.Parameters.Add(cItem.GetCVStautsPara());
                            }
                        }
                    }
                    break;
                case enumParaModuleType.ROBOT: //로봇 갯수만큼 반복필요.
                    foreach (var rItem in GlobalData.Current.mRMManager.ModuleList)
                    {
                        LItem.Parameters.Add(rItem.Value.GetRobotStatusPara());
                    }
                    break;
                case enumParaModuleType.SHUTTLE:
                    foreach (var lineItem in GlobalData.Current.LineManager.ModuleList)
                    {
                        foreach (var cItem in lineItem.Value.ModuleList)
                        {
                            if (cItem.CVModuleType == eCVType.OHTIF || cItem.CVModuleType == eCVType.TurnOHTIF) //OHT Interface 모듈은 따로 셔틀로 보고   //2021.05.25 lim, TurnOHT 추가
                            {
                                LItem.Parameters.Add(cItem.GetCVStautsPara());               
                            }
                        }
                    }
                    break;
                default:
                    return "";
            }
            return LItem.XMLRendering().ToString();
        }
        /// <summary>
        /// 크레인 명령 요청
        /// </summary>
        /// <param name="x"></param>
        /// <param name="MsgName"></param>
        /// <returns></returns>
        private string LCSHandler_CRANE_COMMAND(XElement x, enumMessageName MsgName)
        {
            LCSMessageItem LItem = new LCSMessageItem();
            LItem.MsgType = enumMessageType.Reply;
            LItem.MsgName = MsgName;
            LItem.Uid = x.Element("UID").Value;
            LItem.Timestamp = GetCurrentTimeStamp();
            switch (MsgName)
            {
                case enumMessageName.CRANE_GET:
                case enumMessageName.CRANE_PUT:
                case enumMessageName.CRANE_MOVE:
                case enumMessageName.CRANE_EMO_GET:
                case enumMessageName.CRANE_EMO_PUT:
                case enumMessageName.CRANE_S_GET:
                case enumMessageName.CRANE_S_PUT:
                    //Ack 칠지 Nak 칠지 체크 로직 추가 필요
                    string RM = GlobalData.Current.mRMManager.Default_RM;
                    if (GlobalData.Current.mRMManager[RM].CheckRMBusy()) //이미 명령 수행중이므로 NAK
                    {
                        LItem.ReturnCode = "1"; //NAK
                    }
                    else
                    {
                        LItem.ReturnCode = "0"; //OK
                    }
                    LItem.Parameters.Add(new ParameterItem("TYPE",      x.Element("PARAMETERS").Element("TYPE").Value));
                    LItem.Parameters.Add(new ParameterItem("BANK",      x.Element("PARAMETERS").Element("BANK").Value));
                    LItem.Parameters.Add(new ParameterItem("BAY",       x.Element("PARAMETERS").Element("BAY").Value));
                    LItem.Parameters.Add(new ParameterItem("LEVEL",     x.Element("PARAMETERS").Element("LEVEL").Value));
                    LItem.Parameters.Add(new ParameterItem("TAGID",     x.Element("PARAMETERS").Element("TAGID").Value));
                    LItem.Parameters.Add(new ParameterItem("CARRIERID", x.Element("PARAMETERS").Element("CARRIERID").Value));
                    break;
                case enumMessageName.CRANE_START:
                case enumMessageName.CRANE_STOP:
                case enumMessageName.CRANE_ERROR_RESET:
                case enumMessageName.CRANE_RETURN_HOME:
                case enumMessageName.CRANE_EMO_RETURN_HOME:
                case enumMessageName.CRANE_CHUCK: //Chuck, Unchuck 누락분 추가.
                case enumMessageName.CRANE_UNCHUCK:
                    //Ack 칠지 Nak 칠지 체크 로직 추가 필요
                    LItem.ReturnCode = "0"; //임시로 모두 HCACK
                    LItem.Parameters.Add(new ParameterItem("CRANEID", x.Element("PARAMETERS").Element("CRANEID").Value));
                    break;
            }
            if (LItem.ReturnCode == "0")
            {
                OnCraneCommandReceive?.BeginInvoke(this, new CraneCommandEventArgs(LItem), EndAsyncEvent, null);
            }
            return LItem.XMLRendering().ToString();
        }
        private string LCSHandler_PORT_MANUAL(XElement x)
        {
            LCSMessageItem LItem = new LCSMessageItem();
            LItem.MsgType = enumMessageType.Reply;
            LItem.MsgName = enumMessageName.PORT_MANUAL;
            LItem.Uid = x.Element("UID").Value;
            LItem.Timestamp = GetCurrentTimeStamp();
            LItem.ReturnCode = "0";
            LItem.Parameters.Add(new ParameterItem("UNITID", x.Element("PARAMETERS").Element("UNITID").Value));
            LItem.Parameters.Add(new ParameterItem("TYPE",   x.Element("PARAMETERS").Element("TYPE").Value));
            LItem.Parameters.Add(new ParameterItem("TAGID",  x.Element("PARAMETERS").Element("TAGID").Value));
            OnPortCommandReceive?.BeginInvoke(this, new PortCommandEventArgs(LItem), EndAsyncEvent, null);
            return LItem.XMLRendering().ToString();
        }
        private string LCSHandler_TOWERLAMP_SET(XElement x)
        {
            LCSMessageItem LItem = new LCSMessageItem();
            LItem.MsgType = enumMessageType.Reply;
            LItem.MsgName = enumMessageName.TOWERLAMP_SET;
            LItem.Uid = x.Element("UID").Value;
            LItem.Timestamp = GetCurrentTimeStamp();
            LItem.ReturnCode = "0";
            LItem.Parameters.Add(new ParameterItem("VISIBLE", x.Element("PARAMETERS").Element("VISIBLE").Value));
            LItem.Parameters.Add(new ParameterItem("GREEN",   x.Element("PARAMETERS").Element("GREEN").Value));
            LItem.Parameters.Add(new ParameterItem("YELLOW",  x.Element("PARAMETERS").Element("YELLOW").Value));
            LItem.Parameters.Add(new ParameterItem("RED",     x.Element("PARAMETERS").Element("RED").Value));
            LItem.Parameters.Add(new ParameterItem("BUZZER",  x.Element("PARAMETERS").Element("BUZZER").Value));
            LItem.Parameters.Add(new ParameterItem("MUTEMODE",x.Element("PARAMETERS").Element("MUTEMODE").Value));
            OnTowerLampCommandReceive?.BeginInvoke(this, new TowerLampCommandEventArgs(LItem), EndAsyncEvent, null);
            return LItem.XMLRendering().ToString();

        }

        //비동기 호출 종료자
        //쓰레드풀 Leak를 방지하기 위해 EndInvoke 호출
        private void EndAsyncEvent(IAsyncResult result)
        {
            try
            {
                var asyncResult = (System.Runtime.Remoting.Messaging.AsyncResult)result;
                var invokedMethod = (CommandEventHandler)asyncResult.AsyncDelegate;
                invokedMethod.EndInvoke(result);
            }
            catch(Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }
     
        public bool ReportRobotStatus(string RM_ID)
        {
            return ReportRobotStatus(GlobalData.Current.mRMManager[RM_ID].GetRobotStatusPara());
        }
        private bool ReportRobotStatus(Parameter_ROBOT RPara)
        {
            try
            {
                LCSMessageItem LItem = new LCSMessageItem();
                LItem.MsgType = enumMessageType.Event;
                LItem.MsgName = enumMessageName.STATUS_DATA_REPORT;
                LItem.Uid = "0";
                LItem.Timestamp = GetCurrentTimeStamp();
                LItem.ReturnCode = "NO REPORT";
                Parameter_ROBOT robot = RPara;
                LItem.Parameters.Add(robot);
                SendString(LItem.XMLRendering().ToString());
                return true;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }
        }
        public bool ReportBoothStatus()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "Report Booth Status : {0}", GlobalData.Current.MainBooth.BoothState);
            return ReportBoothStatus(GlobalData.Current.MainBooth.GetBoothStatusPara());
        }
        private bool ReportBoothStatus(Parameter_BOOTH BPara)
        {
            try
            {
                LCSMessageItem LItem = new LCSMessageItem();
                LItem.MsgType = enumMessageType.Event;
                LItem.MsgName = enumMessageName.STATUS_DATA_REPORT;
                LItem.Uid = "0";
                LItem.Timestamp = GetCurrentTimeStamp();
                LItem.ReturnCode = "NO REPORT";
                Parameter_BOOTH booth = BPara;
                LItem.Parameters.Add(booth);
                SendString(LItem.XMLRendering().ToString());
                return true;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }
        }
        public bool ReportPortStatus(string Port_ID)
        {
            try
            {
                return ReportPortStatus(GlobalData.Current.LineManager.GetCVModule(Port_ID).GetCVStautsPara());
            }
            catch
            {
                return false;
            }
        }
        private bool ReportPortStatus(Parameter_PORT PPara)
        {
            try
            {
                LCSMessageItem LItem = new LCSMessageItem();
                LItem.MsgType = enumMessageType.Event;
                LItem.MsgName = enumMessageName.STATUS_DATA_REPORT;
                LItem.Uid = "0";
                LItem.Timestamp = GetCurrentTimeStamp();
                LItem.ReturnCode = "NO REPORT";
                Parameter_PORT port = PPara;
                LItem.Parameters.Add(port);
                SendString(LItem.XMLRendering().ToString());
                return true;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }
        }

        public bool ReportPortStatus(string Port_ID, bool isReset)
        {
            try
            {
                var CV = GlobalData.Current.LineManager.GetCVModule(Port_ID);
                return ReportPortStatus(CV.GetCVStautsPara(isReset));
            }
            catch
            {
                return false;
            }
        }

        public bool ReportShuttleStatus(string Shuttle_ID,bool isReset)
        {
            try
            {
                //2021.05.24 lim, TurnOHTIF 상태 보고 추가
                //CV_OHTIFModule OHTModule = GlobalData.Current.LineManager.GetCVModule(Shuttle_ID) as CV_OHTIFModule;
                //return ReportShuttleStatus(OHTModule.GetShttleStautsPara(isReset));
                return ReportShuttleStatus(GlobalData.Current.LineManager.GetCVModule(Shuttle_ID).GetShttleStautsPara(isReset));
            }
            catch
            {
                return false;
            }
        }
        private bool ReportShuttleStatus(Parameter_SHUTTLE SPara)
        {
            try
            {
                LCSMessageItem LItem = new LCSMessageItem();
                LItem.MsgType = enumMessageType.Event;
                LItem.MsgName = enumMessageName.STATUS_DATA_REPORT;
                LItem.Uid = "0";
                LItem.Timestamp = GetCurrentTimeStamp();
                LItem.ReturnCode = "NO REPORT";
                Parameter_SHUTTLE shuttle = SPara;
                LItem.Parameters.Add(shuttle);
                SendString(LItem.XMLRendering().ToString());
                return true;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }
        }
        public bool ReportTowerLampStatus()
        {
            return ReportTowerLampStatus(GlobalData.Current.MainBooth.GetTowerLampStatusPara());
        }
        private bool ReportTowerLampStatus(Parameter_TOWERLAMP TPara)
        {
            try
            {
                LCSMessageItem LItem = new LCSMessageItem();
                LItem.MsgType = enumMessageType.Event;
                LItem.MsgName = enumMessageName.TOWERLAMP_DATA_REPORT;
                LItem.Uid = "0";
                LItem.Timestamp = GetCurrentTimeStamp();
                LItem.ReturnCode = "NO REPORT";
                Parameter_TOWERLAMP towerlamp = TPara;
                LItem.Parameters = towerlamp.GetParameters(); //타워램프는 파라미터 리스트를 직접 업데이트 한다.
                SendString(LItem.XMLRendering().ToString());
                return true;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }
        }

        private bool SendString(string msg)
        {
            try
            {
                if (client.State == CommunicationState.Opened) //보내기전에 연결 상태 체크
                {

                    string rcv = "";
                    client.SendString(msg, out rcv);
                    if (!string.IsNullOrEmpty(msg))
                    {
                        OnRawSend?.Invoke(this, new RawDataEventArgs(msg));
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }
        }

        private string GetCurrentTimeStamp()
        {
            return DateTime.Now.ToString("yyyyMMdd-HHmmss"); //ex) 20200829-173321 
        }

        #region IDisposable Support
        private bool disposedValue = false; // 중복 호출을 검색하려면

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 관리되는 상태(관리되는 개체)를 삭제합니다.
                }

                // TODO: 관리되지 않는 리소스(관리되지 않는 개체)를 해제하고 아래의 종료자를 재정의합니다.
                // TODO: 큰 필드를 null로 설정합니다.
                CloseWCF();
                disposedValue = true;
            }
        }

        // TODO: 위의 Dispose(bool disposing)에 관리되지 않는 리소스를 해제하는 코드가 포함되어 있는 경우에만 종료자를 재정의합니다.
         ~WCFLBS_Manager() {
           // 이 코드를 변경하지 마세요. 위의 Dispose(bool disposing)에 정리 코드를 입력하세요.
           Dispose(false);
         }

        // 삭제 가능한 패턴을 올바르게 구현하기 위해 추가된 코드입니다.
        public void Dispose()
        {
            // 이 코드를 변경하지 마세요. 위의 Dispose(bool disposing)에 정리 코드를 입력하세요.
            Dispose(true);
            //TODO: 위의 종료자가 재정의된 경우 다음 코드 줄의 주석 처리를 제거합니다.
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
