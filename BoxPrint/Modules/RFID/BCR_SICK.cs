using BoxPrint.Communication;
using BoxPrint.Log;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace BoxPrint.Modules.RFID
{
    public class BCR_SICK : RFID_ModuleBase, IDisposable
    {
        //220823 조숭진 db config에서 읽어오기 위해 RFID_ModuleBase로 옮김 s
        //private readonly int ReadTimeOut = 6;
        //private readonly int MaxRetry = 0;      //2021.04.08 lim,
        //220823 조숭진 db config에서 읽어오기 위해 RFID_ModuleBase로 옮김 e

        public BCR_SICK(string mName, eRFIDComType Comtype, bool simul) : base(mName, Comtype, simul)
        {
        }

        public override bool ReadRFID(out string ReadData)
        {
            try
            {
                if (!CheckConnection())
                {
                    //연결 안된 상태라면 리트라이 한다.
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Retry BCR Connection IP : {0} Port :{1}", IPAddress, PortNumber);
                    if (!SimulMode) //시뮬 모드일때 불필요한 접속 시도 방지.
                    {
                        ClearRcvQueue();
                        bool Connected = TCPSocketConnect();
                        if(!Connected)
                        {
                            ReadData = "ERROR";
                            return false;
                        }
                        else
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Retry BCR Connection Success! IP : {0} Port :{1}", IPAddress, PortNumber);
                        }
                    }
                }
                if (SimulMode)
                {
                    ReadData = "ERROR";
                    return true;
                }
                ReadCompleted = false;
                ClearRcvQueue();//동작전에 혹시 들어온  메시지 삭제.
                byte[] RcvData;
                //명령 패킷을 만든다.
                byte[] CommandPacket;// = new byte[8];

                string cmd = string.Format("{0}{1}{2} ", "\x02", "sMI 47", "\x03");

                CommandPacket = Encoding.UTF8.GetBytes(cmd);

                //명령 송신
                SendPacket(CommandPacket);
                ReadTriggerSent = true;
                Stopwatch timeWatch = Stopwatch.StartNew();

                int iRetry = 0;
                ReadData = "";
                while (true)
                {
                    if (IsTimeout_SW(timeWatch, ReadTimeOut))
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "BCR Module:{0} Read Timeout.", this.ModuleName);
                        ReadData = "ERROR";
                        return false;
                    }
                    //2021.04.08 lim, retry 추가
                    if (ReadData.CompareTo("ERROR") == 0)
                    {
                        if (iRetry++ >= MaxRetry)
                            return false;

                        ClearRcvQueue();
                        ReadData = "";

                        SendPacket(CommandPacket);
                    }

                    if (RcvQueue.TryDequeue(out RcvData)) //큐에서 꺼내기 성공했다면
                    {
                        //커멘드 수령 응답 "sAI 47 1"
                        if (RcvData.Length == 10)// && RcvData[1] == 0x73 && RcvData[2] == 0x41 && RcvData[3] == 0x49)
                        {
                            string ack = Encoding.ASCII.GetString(RcvData);
                            //string str = string.Format("{0}{1}{2}", "\x02", "sAI 47 1", "\x03");
                            if (ack.CompareTo(string.Format("{0}{1}{2}", "\x02", "sAI 47 1", "\x03")) == 0)
                                continue;
                        }

                        //메시지 검증
                        if (RcvData.Length < 4)
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "BCR Module:{0} Message Length is too short!.", this.ModuleName);
                            ReadData = "ERROR";
                            continue;  //return false;
                        }
                        int ReadCnt = RcvData[1] - '0';
                        //Reading TimeOut
                        if (ReadCnt == 0)
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "BCR Module:{0} Reading Fail", this.ModuleName);
                            ReadData = "ERROR";
                            continue;  //return false;
                        }

                        //응답 길이 체크
                        //if (RcvData.Length <= BCR_DATA_LENGTH + 4 + 8) //211202 RGJ 소박스 SETUP
                        //{
                        //    LogManager.WriteConsoleLog(eLogLevel.Info, "BCR Module:{0} 메시지 길이 이상", this.ModuleName);
                        //    ReadData = "ERROR";
                        //    continue;  //return false;
                        //}
                        if (RcvData[RcvData.Length - 1] != 0x03) //ETX
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "BCR Module:{0} Message Format error.", this.ModuleName);
                            ReadData = "ERROR";
                            continue;  //return false;
                        }
                        if (RcvData[0] != 0x02) //STX
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "BCR Module:{0} Message Format error.", this.ModuleName);
                            ReadData = "ERROR";
                            continue;  //return false;
                        }
                        string BCR_DATA = Encoding.ASCII.GetString(RcvData, 1, RcvData.Length - 2);

                        ReadData = BCR_DATA;
                        ReadCompleted = true;
                        return true;//성공 리턴
                    }
                    Thread.Sleep(50);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                ReadData = "ERROR";
                return false;
            }
            finally
            {
                ReadTriggerSent = false;
            }
        }
        protected override void OnReceive(object sender, AsyncSocketReceiveEventArgs e)
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
                if (CommunicationType == eRFIDComType.TCP_IP)
                {
                    this.ModuleSocket?.Close();
                }
                else if (CommunicationType == eRFIDComType.RS_232)
                {
                    this.ModuleSerialPort?.Close();
                }
                // TODO: 관리되지 않는 리소스(관리되지 않는 개체)를 해제하고 아래의 종료자를 재정의합니다.
                // TODO: 큰 필드를 null로 설정합니다.

                disposedValue = true;
            }
        }

        // TODO: 위의 Dispose(bool disposing)에 관리되지 않는 리소스를 해제하는 코드가 포함되어 있는 경우에만 종료자를 재정의합니다.
        ~BCR_SICK()
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
