using BoxPrint.Communication;
using BoxPrint.Log;
using System;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Text;
using System.Threading;


namespace BoxPrint.Modules.Print
{
    public class TCP_ModuleBase : ModuleBase
    {
        public delegate void UnitConnectionStateChange(bool connection);
        public event UnitConnectionStateChange OnUnitConnectionStateChanged; //내부 유닛 상태가 변했으므로 GUI에게 업데이트 이벤트를 날린다.

        protected readonly byte BCR_DATA_LENGTH = 21;  //LOT 정보 21자리 + DVC 가변길이 //211202 RGJ 소박스 SETUP

        protected readonly byte Unit_DATA_LENGTH = 112;
        protected readonly byte Unit_WRITE_LENGTH = 104; //Tray ID 8바이트 제외
        protected eUnitComType CommunicationType;

        public eUnitComType ComType
        {
            get
            {
                return CommunicationType;
            }
        }

        #region EtherNet
        protected string IP;
        protected string Port;
        protected AsyncSocketClient ModuleSocket = null;
        protected ManualResetEvent MR_connectionEvent = new ManualResetEvent(false);

        public string IPAddress
        {
            get
            {
                return IP;
            }
        }
        public string PortNumber
        {
            get
            {
                return Port;
            }
        }


        #endregion End Ethernet
        #region RS232
        protected string ComPort;
        protected SerialPort ModuleSerialPort;

        #endregion End Rs232

        protected ConcurrentQueue<byte[]> RcvQueue = null;

        public virtual int ReadTimeOut { get; set; }
        public virtual int MaxRetry { get; set; }



        public bool ReadTriggerSent
        {
            get;
            protected set;
        }
        public bool ReadCompleted
        {
            get;
            protected set;
        }
        public TCP_ModuleBase(string mName, eUnitComType ComType, bool simul) : base(mName, simul)
        {
            this.CommunicationType = ComType;
        }

        protected void ClearRcvQueue()
        {
            byte[] temp;
            while (RcvQueue.TryDequeue(out temp))
            {
                ;
            }
        }
        public bool InitUnitController ()
        {
            try
            {
                RcvQueue = new ConcurrentQueue<byte[]>();
                if (CommunicationType == eUnitComType.TCP_IP)
                {
                    if (!SimulMode)
                    {
                        TCPCommunication();
                    }
                }
                else if (CommunicationType == eUnitComType.RS_232)
                {
                    COMCommunication();
                }
                return true;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.Print, ex.ToString());
                return false;
            }
        }
        public eUnitConnection CheckConnection()
        {
            if (SimulMode)
            {
                
                return eUnitConnection.Connect;
            }
            else
            {
                if (this.ModuleSocket.SocketConnected)
                    return eUnitConnection.Connect;
                else
                    return eUnitConnection.Disconnect;
            }
        }

        public void SetCommunicationAddress(string ip, string port, string comport)
        {
            IP = ip;
            Port = port;
            ComPort = comport;
        }

        protected void TCPCommunication()
        {
            ModuleSocket = new AsyncSocketClient(1);

            // 이벤트 핸들러 재정의
            ModuleSocket.OnConnet += new AsyncSocketConnectEventHandler(OnConnet);
            ModuleSocket.OnClose += new AsyncSocketCloseEventHandler(OnClose);
            ModuleSocket.OnSend += new AsyncSocketSendEventHandler(OnSend);
            ModuleSocket.OnReceive += new AsyncSocketReceiveEventHandler(OnReceive);
            ModuleSocket.OnError += new AsyncSocketErrorEventHandler(OnError);
            if (!SimulMode) //시뮬 모드일때 불필요한 접속 시도 방지.
            {
                TCPSocketConnect();
                ClearRcvQueue();
                OnUnitConnectionStateChanged?.Invoke(ModuleSocket.SocketConnected);
            }
        }
        protected void COMCommunication()
        {
            try
            {
                this.ModuleSerialPort = new SerialPort();
                this.ModuleSerialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPort_DataReceived);
                this.ModuleSerialPort.PortName = ComPort;
                this.ModuleSerialPort.BaudRate = 115200;
                this.ModuleSerialPort.DataBits = 8;
                this.ModuleSerialPort.StopBits = StopBits.One;
                this.ModuleSerialPort.Parity = Parity.None;
                this.ModuleSerialPort.Handshake = Handshake.None;
                this.ModuleSerialPort.ReadTimeout = 500;
                this.ModuleSerialPort.WriteTimeout = 500;
                if (!SimulMode) //시뮬 모드일때 불필요한 접속 시도 방지.
                {
                    ModuleSerialPort.Open();
                    ClearRcvQueue();
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, eModuleList.Print, ex.ToString());
            }

        }

        protected string StringToHex(string strData)
        {
            string resultHex = string.Empty;
            byte[] arr_byteStr = Encoding.Default.GetBytes(strData);
            foreach (byte byteStr in arr_byteStr)
                resultHex += string.Format("{0:X2}", byteStr);
            return resultHex;
        }

        protected void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs args)
        {
            Thread.Sleep(50);
            try
            {
                byte[] RcvData = new byte[ModuleSerialPort.BytesToRead];
                ModuleSerialPort.Read(RcvData, 0, ModuleSerialPort.BytesToRead);
                WriteReceiveLog(RcvData, ModuleSerialPort.BytesToRead);
                RcvQueue.Enqueue(RcvData);
            }
            catch (System.Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, eModuleList.Print, ex.ToString());
            }
        }


        protected void OnError(object sender, AsyncSocketErrorEventArgs e)
        {
            //
            LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.Print, "Socket on Error : {0} - {1}", this.ModuleName, e.ToString());
        }

        protected virtual void OnReceive(object sender, AsyncSocketReceiveEventArgs e)
        {
            WriteReceiveLog(e.ReceiveData, e.ReceiveBytes);
            //로직 수정. 수신시 데이터 복사본을 큐에 넣고 끝냄
            byte[] temp = new byte[e.ReceiveBytes];
            for (int i = 0; i < e.ReceiveBytes; i++)
            {
                temp[i] = e.ReceiveData[i];
            }
            RcvQueue.Enqueue(temp);
        }

        protected void OnSend(object sender, AsyncSocketSendEventArgs e)
        {
            //
        }

        protected virtual void OnClose(object sender, AsyncSocketConnectionEventArgs e)
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.Print, "IP:{0}  Port:{1} Connection Closed.", IP, Port);
            OnUnitConnectionStateChanged?.Invoke(false);
        }

        private void OnConnet(object sender, AsyncSocketConnectionEventArgs e)
        {
            MR_connectionEvent.Set();
            OnUnitConnectionStateChanged?.Invoke(true);
        }

        public bool TCPSocketConnect()
        {
            try
            {
                if (ModuleSocket.SocketConnected)
                {
                    //이미 접속 되었으면 그냥 완료 처리
                    LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.Print, "IP:{0}  Port:{1} Already Connected.", IP, Port);
                    return true;
                }

                MR_connectionEvent.Reset();
                ModuleSocket.Connect(IP, Port);
                bool ConnectResult = MR_connectionEvent.WaitOne(1000);
                if (ConnectResult)
                {
                    OnUnitConnectionStateChanged?.Invoke(ConnectResult);
                    LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.Print, "Unit IP:{0}  Port:{1} Connected.", IP, Port);
                }
                else
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.Print, "Unit IP:{0}  Port:{1} Connecting failed", IP, Port);
                }
                return ConnectResult;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.Print, ex.ToString());
                return false;
            }
        }

        public virtual bool SendMessage(string message)
        {
            SendMsg(message);

            return true;
        }

        protected void SendMsg(string Command)
        {
            Thread.Sleep(20);  //최소 동작 딜레이 20ms 준다.
            if (CommunicationType == eUnitComType.TCP_IP)
            {
                ModuleSocket.Send(Encoding.ASCII.GetBytes(Command));
            }
            else if (this.CommunicationType == eUnitComType.RS_232)
            {
                ModuleSerialPort.Write(Command);
            }
        }
        protected void SendPacket(byte[] packet)
        {
            Thread.Sleep(20);  //최소 동작 딜레이 20ms 준다.
            if (CommunicationType == eUnitComType.TCP_IP)
            {
                ModuleSocket.Send(packet);
            }
            else if (this.CommunicationType == eUnitComType.RS_232)
            {
                ModuleSerialPort.Write(packet, 0, packet.Length);
            }
        }
        protected void WriteReceiveLog(byte[] RcvData, int count)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Unit Module:" + ModuleName + " ==> Firmware  :");
            //for (int i = 0; i < count; i++)
            //{

            //    sb.Append(Encoding.ASCII.GetString(RcvData[i]));
            //    //sb.Append(' ');
            //}
            string temp = Encoding.ASCII.GetString(RcvData);
            
            sb.Append(temp.Substring(0,count-1));
            LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, sb.ToString());
        }
        protected void WriteReceiveLog(string RcvData)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Unit Module:" + ModuleName + " ==> Firmware  :");
            sb.Append(RcvData);

            LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, sb.ToString());
        }
        protected void WriteSendLog(string SendData)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Unit Module:" + " Firmware  ==> " + ModuleName + " : ");
            sb.Append(SendData);

            LogManager.WriteConsoleLog(eLogLevel.Info, eModuleList.CV, sb.ToString());
        }
    }
}
