using BoxPrint.Communication;
using BoxPrint.Log;
using OSG.Com.HSMS.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace BoxPrint.Modules.Print
{
    public class INK_SQUID : TCP_ModuleBase, IDisposable
    {

        private object thisLock = new object();
        private object thisRcvLock = new object();

        #region Event
        //public event EventHandler<bool> FirstConnectedRecived;
        //public event EventHandler EnablePrintComplete;
        //public event EventHandler PrintCompleteResponse;
        //public event EventHandler<AutoDataResponseEnum> AutoDataRecordResponse;
        //public event EventHandler WriteAutoDataRecivedResponse;
        //public event EventHandler WriteAutoDataQueueClearResponse;
        //public event EventHandler<int> ReadInkLevelResponse;
        //public event EventHandler<string> GetAutoDataStringResponse;


        //public event EventHandler<StateChangedEventArgs> StateChanged;

        #region Old
        //public event EventHandler<DateTime> WrtieSystemDateAndTimeResponse;
        //public event EventHandler<int> ProductionCounterResponse;
        //public event EventHandler<CounterInfo> GetCounterInfoResponse;
        //public event EventHandler<int> SetCounterInfoSuccessfulResponse;
        #endregion

        #endregion

        #region Field
        private const char LF = (char)0x0A; //End
        private readonly object syncRoot = new object();
        #endregion

        #region Prop
        private bool m_IsFirstConnectedRecived;
        public bool IsFirstConnectedRecived
        {
            get => this.m_IsFirstConnectedRecived;
            private set
            {
                if (this.m_IsFirstConnectedRecived == value) return;

                this.m_IsFirstConnectedRecived = value;

                //if (this.m_IsFirstConnectedRecived)
                //    this.FirstConnectedRecived?.Invoke(this, true);
            }
        }

        public bool SetReplyCheck;
        public bool enableNextPrint;
        #endregion

        public INK_SQUID(string mName, eUnitComType Comtype, bool simul) : base(mName, Comtype, simul)
        {
            SetReplyCheck = false;
            enableNextPrint = false;
        }

        public override bool SendMessage(string message)
        {
            if (CheckConnection() == eUnitConnection.Connect)// && this.IsFirstConnectedRecived == true)
            {
                lock (this.syncRoot)
                {

                    byte[] bytes = Encoding.UTF8.GetBytes(message + LF);

                    this.SendPacket(bytes);

                    WriteSendLog(message);

                    return true;
                }
            }

            return false;
        }


        protected override void OnReceive(object sender, AsyncSocketReceiveEventArgs e)
        {

            lock (thisRcvLock)
            {
                WriteReceiveLog(e.ReceiveData, e.ReceiveBytes);
                //로직 수정. 수신시 데이터 복사본을 큐에 넣고 끝냄
                byte[] temp = new byte[e.ReceiveBytes];
                //bool CheckEnd = false;

                int len;
                for (len = 0; len < e.ReceiveBytes; len++)
                {
                    temp[len] = e.ReceiveData[len];
                }

                string data;
                string message = Encoding.ASCII.GetString(temp, 0, temp.Length);
                var index = message.IndexOf(LF);
                if (index > -1)
                {
                    data = message.Substring(0, message.Length - (message.Length - index));
                    //ProcessAsEvent(data);
                    //return true;
                }

                RcvQueue.Enqueue(temp);
            }
        }

        protected override void OnClose(object sender, AsyncSocketConnectionEventArgs e)
        {
            IsFirstConnectedRecived = false;
            base.OnClose(sender, e);
        }

        public int GetRecvCount()
        {
            return RcvQueue.Count;
        }


        public bool GetRecvData(out string data)
        {
            data = string.Empty;

            byte[] bytes;
            if (RcvQueue.TryDequeue(out bytes))
            {
                string message = Encoding.ASCII.GetString(bytes, 0, bytes.Length);
                var index = message.IndexOf(LF);
                if (index > -1)
                {
                    data = message.Substring(0, message.Length - (message.Length - index));
                    return true;
                }

            }

            return false;
        }


        #region send Command

        public void MakeDataMessage(string MessageName, Dictionary<string, object> args = null)
        {

            ePrintCommand pc = (ePrintCommand)Enum.Parse(typeof(ePrintCommand), MessageName);

            SetReplyCheck = false;

            switch (pc)
            {
                case ePrintCommand.EnablePrintComplete:
                    EnablPrintCompleteAcknowledgementSend(args);
                    break;
                case ePrintCommand.ReadAutoDataState:
                    ReadAutoDataStatusSend(args);
                    break;
                case ePrintCommand.WriteAutoDataRedord:
                    WriteAutoDataRecordSend(args);
                    break;
                case ePrintCommand.GetAutoDataString:
                    GetAutoDataStringSend(args);
                    break;
                case ePrintCommand.ClearAutoDataQueue:
                    ClearAutoDataQueueSend(args);
                    break;
                case ePrintCommand.ReadInkLevel:
                    ReadInkLevelSend(args);
                    break;
                case ePrintCommand.Build:
                    SetFileBuildMessage(args);
                    break;
                case ePrintCommand.GetPrintDirection:
                    GetPrintDirection(args);
                    break;
                case ePrintCommand.SetPrintDirection:
                    SetPrintDirection(args);
                    break;
                case ePrintCommand.GetPrintDelay:
                    GetPrintDelay(args);
                    break;
                case ePrintCommand.SetPrintDelay:
                    SetPrintDelay(args);
                    break;
                case ePrintCommand.GetManualSpeed:
                    GetManualSpeed(args);
                    break;
                case ePrintCommand.SetManualSpeed:
                    SetManualSpeed(args);
                    break;
                case ePrintCommand.ReadSystemDateTime:
                    ReadSystemDateTime(args);
                    break;
                case ePrintCommand.WriteSystemDateTime:
                    WriteSystemDateTime(args);
                    break;
                default:
                    return;
            }
        }
        private bool EnablPrintCompleteAcknowledgementSend(Dictionary<string, object> args)
        {
            bool result = this.SendMessage("A");
            //LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, $"Enabl Print Complete Acknowledgement Send : {result}");

            return result;
        }
        private bool ReadAutoDataStatusSend(Dictionary<string, object> args)
        {
            bool result = this.SendMessage("C");
            //LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, $"Read Auto Data Status Send : {result}");

            return result;
        }
        private bool WriteAutoDataRecordSend(Dictionary<string, object> args)
        {
            int cnt = args.Count();

            int floor = (int)args["Floor"];
            string boxType = (string)args["BoxType"];
            int inkejctNo = (int)args["InkjetNo"];
            int currentBoxCount = (int)args["Count"];


            StringBuilder sb = new StringBuilder();
            sb.Append("D");
            sb.Append(floor);
            sb.Append(boxType);
            sb.Append(inkejctNo);
            sb.Append($"{currentBoxCount}".PadLeft(7, '0'));

            bool result = this.SendMessage(sb.ToString());
            //LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, $"Write Auto Data Record Send(floor:{floor}, boxType:{boxType}, inkejctNo:{inkejctNo}), currentBoxCount:{currentBoxCount} : {result}");

            return result;
        }
        private bool ClearAutoDataQueueSend(Dictionary<string, object> args)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("D");
            sb.Append("_CLEAR_ADQ_");

            bool result = this.SendMessage(sb.ToString());
            //LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, $"Clear Auto Data Queue Send : {result}");

            return result;
        }
        private bool ReadInkLevelSend(Dictionary<string, object> args)
        {
            bool result = this.SendMessage("o");
            //LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, $"Read Ink Level Send : {result}");

            return result;
        }
        private bool GetAutoDataStringSend(Dictionary<string, object> args)
        {
            bool result = this.SendMessage("GET_AUTO_DATA_STRING");
            //LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, $"Get Auto Data String Send : {result}");

            return result;
        }

        /// <summary>
        /// 워킹 파일 설정
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private bool WriteWorkingFileName(Dictionary<string, object> args)
        {
            string fileName = (string)args["FileName"];
            string cmd = $"N{fileName}";

            bool result = this.SendMessage(cmd);
            //LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, $"Write Working File Name Send({fileName}) : {result}");
            return result;
        }

        /// <summary>
        /// 설정된 워킹 파일 빌드
        /// </summary>
        /// <returns></returns>
        private bool SetBuildMessage(Dictionary<string, object> args)
        {
            bool result = this.SendMessage("B");
            //LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, $"Build Message Send : {result}");
            return result;
        }

        /// <summary>
        /// 파일 빌드
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        private bool SetFileBuildMessage(Dictionary<string, object> args)
        {
            string fileName = (string)args["Filename"];
            string cmd = $"BUILD_MESSAGE={fileName}";

            bool result = this.SendMessage(cmd);
            //LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, $"Build Message Send({fileName}) : {result}");
            return result;

        }

        /// <summary>
        /// 메세지 이미지로 변경 
        /// </summary>
        /// <returns>
        /// 전송 성공 유무
        /// </returns>
        private bool SetCreatePrintBitMap(Dictionary<string, object> args)
        {
            bool result = this.SendMessage("P");
            //LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, $"Build Message Send : {result}");
            return result;
        }

        private bool GetPrintDirection(Dictionary<string, object> args)
        {
            //string fileName = (string)args["Direction"];
            string cmd = $"GET_PRINT_DIRECTION";
            bool result = this.SendMessage(cmd);
            //LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, $"get Print Direction Send : {result}");
            return result;

        }

        private bool SetPrintDirection(Dictionary<string, object> args)
        {
            string dir = (string)args["Direct"];
            string cmd = $"SET_PRINT_DIRECTION {dir}";

            bool result = this.SendMessage(cmd);
            //LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, $"Set Print Direction Send({dir}) : {result}");
            return result;

        }

        private bool GetPrintDelay(Dictionary<string, object> args)
        {
            //string fileName = (string)args["FileName"];
            string cmd = $"GET_PRINT_DELAY";
            bool result = this.SendMessage(cmd);
            //LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, $"Build Message Send({fileName}) : {result}");
            return result;

        }

        private bool SetPrintDelay(Dictionary<string, object> args)
        {
            //string sVal = (string)args["Delay"];
            bool result = false;

            if (int.TryParse((string)args["Delay"], out int delay))
            {
                int sendDelay = (int)delay;

                string cmd = $"SET_PRINT_DELAY={sendDelay}";

                result = this.SendMessage(cmd);
            }
             //LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, $"Build Message Send({fileName}) : {result}");
            return result;

        }

        private bool GetManualSpeed(Dictionary<string, object> args)
        {
            //string fileName = (string)args["FileName"];
            string cmd = $"GET_MANUAL_SPEED";
            bool result = this.SendMessage(cmd);
            //LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, $"Build Message Send({fileName}) : {result}");
            return result;

        }

        private bool SetManualSpeed(Dictionary<string, object> args)
        {
            int min = 1, max = 135;
            bool result = false;

            //속도 15m/s  => 50inch/s  (15* 3.3 = 49.5) 

            //int meterSpeed = (int)args["Speed"];
            if (int.TryParse(args["Speed"].ToString(), out int meterSpeed))
            {
                int inchSpeed = (int)Math.Round(meterSpeed * 3.3, 0);

                string cmd = $"SET_MANUAL_SPEED {inchSpeed}";

                result = this.SendMessage(cmd);
                //LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, $"Build Message Send({fileName}) : {result}");
            }

            return result;
        }

        private bool ReadSystemDateTime(Dictionary<string, object> args)
        {
            //string fileName = (string)args["FileName"];
            string cmd = $"t";
            bool result = this.SendMessage(cmd);
            //LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, $"Build Message Send({fileName}) : {result}");
            return result;

        }

        private bool WriteSystemDateTime(Dictionary<string, object> args)
        {
            //string fileName = (string)args["FileName"];
            //date = mm/dd/yyyy
            //time = HH:MM:ss

            string cmd = $"T{DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss")}";

            bool result = this.SendMessage(cmd);
            //LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, $"Build Message Send({fileName}) : {result}");
            return result;

        }

        #endregion




        #region 응답 methed

        public void ProcessAsEvent(string message)
        {
            SetReplyCheck = true;
            if (message.Contains("Connected to "))
                IsFirstConnectedRecived = true;
            else if (message.Equals("ACK-Print Complete Enabled"))
                EnablePrintComplete();
            else if (message.Contains("ACK-Auto Data"))
            {
                if (message.Equals("ACK-Auto Data XON"))
                    this.AutoDataRecordResponse(AutoDataResponseEnum.XON);
                else if (message.Equals("ACK-Auto Data XOFF"))
                    this.AutoDataRecordResponse(AutoDataResponseEnum.XOFF);
                else if (message.Equals("ACK-Auto Data Received"))
                    this.WriteAutoDataRecivedResponse();
                else if (message.Equals("ACK-Auto Data Received - Auto Data queue cleared"))
                    this.WriteAutoDataQueueClearResponse();
            }
            else if (message.Equals("ACK-Print Complete"))
                this.PrintCompleteResponse();
            else if (message.Contains("ACK-Ink Level"))
            {
                string[] splited = message.Split('=');
                if (splited.Length == 2)
                {
                    string strValue = splited[1].Replace("%", "");
                    if (int.TryParse(strValue, out int result))
                        this.ReadInkLevelResponse(result);
                    else
                    {
                        //Error
                        this.ReadInkLevelResponse(-1);
                    }
                }
            }

            else if (message.Contains("ACK-AUTO_DATA_STRING"))
            {
                string[] splited = message.Split('=');
                if (splited.Length == 2)
                {
                    string strValue = splited[1].Replace("\n", "");
                    ;//this.GetAutoDataStringResponse?.Invoke(this, strValue);
                }
            }
            #region Old
            else if (message.Contains("ACK-DateTime"))
            {
                string[] splited = message.Split('=');
                if (splited.Length == 2)
                {
                    if (DateTime.TryParse(splited[1], out DateTime result))
                        ;//this.WrtieSystemDateAndTimeResponse?.Invoke(this, result);
                }
            }
            else if (message.Contains("ACK-Production_Counter"))
            {
                string[] splited = message.Split('=');
                if (splited.Length == 2)
                {
                    if (int.TryParse(splited[1], out int count))
                        ;//this.ProductionCounterResponse?.Invoke(this, count);
                }
            }
            //else if (message.Contains("ACK-GET_COUNTER_INFO"))
            //{
            //    string[] splited = message.Split('=');
            //    if (splited.Length == 2)
            //    {
            //        string[] responseSplite = splited[1].Split(',');
            //        if (responseSplite.Length == 6)
            //        {
            //            string strart = responseSplite[0];
            //            string stop = responseSplite[1];
            //            string current = responseSplite[2];
            //            if (Enum.TryParse(responseSplite[3], out DirectionEnum direction) == false) return;
            //            if (Enum.TryParse(responseSplite[4], out TypeEnum type) == false) return;

            //            CounterInfo counterInfo = new CounterInfo()
            //            {
            //                Start = strart,
            //                Stop = stop,
            //                Current = current,
            //                Direction = direction,
            //                Type = type,
            //            };
            //            this.GetCounterInfoResponse?.Invoke(this, counterInfo);
            //        }
            //    }
            //}
            else if (message.Contains("ACK-SET_COUNTER_INFO"))
            {
                string s = "ACK-SET_COUNTER_INFO Successful, New vlaue is ";
                string strValue = message.Substring(s.Length, message.Length - s.Length);
                if (int.TryParse(strValue, out int value))
                    ;//this.SetCounterInfoSuccessfulResponse?.Invoke(this, value);
            }
            #endregion
        }

        protected void FirstConnectedRecived(bool enabled)
        {
            try
            {
                #region Communicator_FirstConnectedRecived
                LogManager.WriteConsoleLog(eLogLevel.Info, "First Connected Recived : {0}", enabled);

                //응답 체크
                MakeDataMessage(ePrintCommand.EnablePrintComplete.ToString());
                //속도 설정
                //MakeDataMessage(ePrintCommand.SetManualSpeed.ToString());
                //딜레이 설정
                //MakeDataMessage(ePrintCommand.SetPrintDelay.ToString());
                //방향 설정
                //MakeDataMessage(ePrintCommand.SetPrintDirection.ToString());
                //출력 이미지 선택
                //MakeDataMessage(ePrintCommand.Build.ToString());

                //this.EnablPrintCompleteAcknowledgementSend();
                //this.GetAutoDataStringSend();
                //this.ReadInkLevelSend();
                #endregion
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, "First Connected Recived Make Message Error : {0}", ex.Message);
            }
        }

        protected void EnablePrintComplete()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "Enable Print Complete");
            SetReplyCheck = true;


            MakeDataMessage(ePrintCommand.GetAutoDataString.ToString());

            MakeDataMessage(ePrintCommand.ReadInkLevel.ToString());
        }

        protected void AutoDataRecordResponse(AutoDataResponseEnum val)
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "Enable Print Complete");
            SetReplyCheck = true;



            if (val == AutoDataResponseEnum.XOFF)
                MakeDataMessage(ePrintCommand.ClearAutoDataQueue.ToString());
            //this.ClearAutoDataQueueSend();
        }

        protected void WriteAutoDataRecivedResponse()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "WriteAutoDataRecivedResponse");

        }
        protected void WriteAutoDataQueueClearResponse()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "WriteAutoDataQueueClearResponse");

        }

        protected void PrintCompleteResponse()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "PrintCompleteResponse");

            enableNextPrint = true;
            MakeDataMessage(ePrintCommand.GetAutoDataString.ToString());
            MakeDataMessage(ePrintCommand.ReadInkLevel.ToString());
        }

        protected void ReadInkLevelResponse(int level)
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "ReadInkLevelResponse");

            GlobalData.Current.PrinterMng.InkPercent = level;
            //InkPercent = level;

        }
        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // 중복 호출을 검색하려면

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ClearRcvQueue();
                    // TODO: 관리되는 상태(관리되는 개체)를 삭제합니다.
                }
                if (CommunicationType == eUnitComType.TCP_IP)
                {
                    this.ModuleSocket?.Close();
                }
                else if (CommunicationType == eUnitComType.RS_232)
                {
                    this.ModuleSerialPort?.Close();
                }
                // TODO: 관리되지 않는 리소스(관리되지 않는 개체)를 해제하고 아래의 종료자를 재정의합니다.
                // TODO: 큰 필드를 null로 설정합니다.

                disposedValue = true;
            }
        }

        // TODO: 위의 Dispose(bool disposing)에 관리되지 않는 리소스를 해제하는 코드가 포함되어 있는 경우에만 종료자를 재정의합니다.
        ~INK_SQUID()
        {
            // 이 코드를 변경하지 마세요. 위의 Dispose(bool disposing)에 정리 코드를 입력하세요.
            Dispose(false);
        }

        // 삭제 가능한 패턴을 올바르게 구현하기 위해 추가된 코드입니다.
        void IDisposable.Dispose()
        {
            // 이 코드를 변경하지 마세요. 위의 Dispose(bool disposing)에 정리 코드를 입력하세요.
            Dispose(true);
            // TODO: 위의 종료자가 재정의된 경우 다음 코드 줄의 주석 처리를 제거합니다.
            GC.SuppressFinalize(this);
        }
        #endregion

    }
}
