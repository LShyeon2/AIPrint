using BoxPrint.Communication;
using BoxPrint.Log;
using System;
using System.Collections.Concurrent;
using System.IO.Ports;
using System.Text;
using System.Threading;


namespace BoxPrint.Modules.RFID
{
    /// <summary>
    /// RFID 기본 클래스
    /// 사양나오면 추가 구현
    /// </summary>
    public abstract class RFID_ModuleBase : ModuleBase
    {
        public delegate void RFIDConnectionStateChange(bool connection);
        public event RFIDConnectionStateChange OnRFIDConnectionStateChanged; //내부 유닛 상태가 변했으므로 GUI에게 업데이트 이벤트를 날린다.

        protected readonly byte BCR_DATA_LENGTH = 21;  //LOT 정보 21자리 + DVC 가변길이 //211202 RGJ 소박스 SETUP

        protected readonly byte RFID_DATA_LENGTH = 112;
        protected readonly byte RFID_WRITE_LENGTH = 104; //Tray ID 8바이트 제외
        protected eRFIDComType CommunicationType;
        protected string IP;
        protected string Port;
        protected string ComPort;
        protected AsyncSocketClient ModuleSocket = null;
        protected SerialPort ModuleSerialPort;
        protected ManualResetEvent MR_connectionEvent = new ManualResetEvent(false);
        protected ConcurrentQueue<byte[]> RcvQueue = null;

        //220823 조숭진 db config에서 읽어오기 위해 RFID_ModuleBase로 옮김 s
        public virtual int ReadTimeOut { get; set; }
        public virtual int MaxRetry { get; set; }
        //220823 조숭진 db config에서 읽어오기 위해 RFID_ModuleBase로 옮김 e

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
        public eRFIDComType ComType
        {
            get
            {
                return CommunicationType;
            }
        }

          
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
        public RFID_ModuleBase(string mName, eRFIDComType ComType, bool simul) : base(mName, simul)
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
        public bool InitRFIDReader()
        {
            try
            {
                RcvQueue = new ConcurrentQueue<byte[]>();
                if (CommunicationType == eRFIDComType.TCP_IP)
                {
                    if (!SimulMode)
                    {
                        TCPCommunication();
                    }
                }
                else if (CommunicationType == eRFIDComType.RS_232)
                {
                    COMCommunication();
                }
                return true;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }
        }
        public bool CheckConnection()
        {
            if (SimulMode)
            {
                return true;
            }
            else
            {
                return this.ModuleSocket.SocketConnected;
            }
        }

        public void SetCommunicationAddress(string ip, string port, string comport)
        {
            IP = ip;
            Port = port;
            ComPort = comport;
        }
        public virtual bool ReadRFID(out string ReadData)
        {
            throw new NotImplementedException();
        }
        public virtual bool WriteRFID(byte[] RFID_Data)
        {
            throw new NotImplementedException();
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
                OnRFIDConnectionStateChanged?.Invoke(true);
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
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
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
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }


        protected void OnError(object sender, AsyncSocketErrorEventArgs e)
        {
            //
            LogManager.WriteConsoleLog(eLogLevel.Info, "Socket on Error : {0} - {1}",this.ModuleName,  e.ToString());
        }

        protected virtual void OnReceive(object sender, AsyncSocketReceiveEventArgs e)
        {
            throw new NotImplementedException();
        }

        protected void OnSend(object sender, AsyncSocketSendEventArgs e)
        {
            //
        }

        protected void OnClose(object sender, AsyncSocketConnectionEventArgs e)
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "IP:{0}  Port:{1} Connection Closed.", IP, Port);
            OnRFIDConnectionStateChanged?.Invoke(false);
        }

        private void OnConnet(object sender, AsyncSocketConnectionEventArgs e)
        {
            MR_connectionEvent.Set();
            OnRFIDConnectionStateChanged?.Invoke(true);
        }

        public bool TCPSocketConnect()
        {
            try
            {
                if (ModuleSocket.SocketConnected)
                {
                    //이미 접속 되었으면 그냥 완료 처리
                    LogManager.WriteConsoleLog(eLogLevel.Info, "IP:{0}  Port:{1} Already Connected.", IP, Port);
                    return true;
                }

                MR_connectionEvent.Reset();
                ModuleSocket.Connect(IP, Port);
                bool ConnectResult = MR_connectionEvent.WaitOne(1000);
                if (ConnectResult)
                {
                    OnRFIDConnectionStateChanged?.Invoke(true);
                    LogManager.WriteConsoleLog(eLogLevel.Info, "RFID IP:{0}  Port:{1} Connected.", IP, Port);
                }
                else
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "RFID IP:{0}  Port:{1} Connecting failed", IP, Port);
                }
                return ConnectResult;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }
        }

        protected void SendMsg(string Command)
        {
            Thread.Sleep(20);  //최소 동작 딜레이 20ms 준다.
            if (CommunicationType == eRFIDComType.TCP_IP)
            {
                ModuleSocket.Send(Encoding.ASCII.GetBytes(Command));
            }
            else if (this.CommunicationType == eRFIDComType.RS_232)
            {
                ModuleSerialPort.Write(Command);
            }
        }
        protected void SendPacket(byte[] packet)
        {
            Thread.Sleep(20);  //최소 동작 딜레이 20ms 준다.
            if (CommunicationType == eRFIDComType.TCP_IP)
            {
                ModuleSocket.Send(packet);
            }
            else if (this.CommunicationType == eRFIDComType.RS_232)
            {
                ModuleSerialPort.Write(packet, 0, packet.Length);
            }
        }
        protected void WriteReceiveLog(byte[] RcvData, int count)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("RFID Module:" + ModuleName + " ==> Firmware  :");
            for (int i = 0; i < count; i++)
            {
                sb.Append(RcvData[i].ToString("X2"));
                sb.Append(' ');
            }
            LogManager.WriteConsoleLog(eLogLevel.Info, sb.ToString());
        }
    }
}
