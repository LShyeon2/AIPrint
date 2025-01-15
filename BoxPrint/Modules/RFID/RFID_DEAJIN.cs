using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Stockerfirmware.Communication;
using Stockerfirmware.Log;
namespace Stockerfirmware.Modules.RFID
{

    class RFID_DEAJIN : RFID_ModuleBase,IDisposable
    {
        private int ReadTimeOut = 6;
        private int WriteTimeOut = 6;

        public RFID_DEAJIN(string mName, eRFIDComType Comtype, bool simul) : base(mName, Comtype, simul)
        {
        }

        public override bool ReadRFID(out string ReadData)
        {
            ClearRcvQueue();//동작전에 혹시 들어온  메시지 삭제.
            byte[] RcvData;
            //명령 패킷을 만든다.
            byte[] CommandPacket = new byte[16];
            //확정전까지는 둘다 미사용
            //Interface Loader 대진 RFID  커맨드
            CommandPacket[0] = 0x10;
            CommandPacket[1] = 0x02;
            CommandPacket[2] = 0x02;
            CommandPacket[3] = 0x01;
            CommandPacket[4] = 0x14;
            CommandPacket[5] = 0x00;
            CommandPacket[6] = 0x04;
            CommandPacket[7] = 0x01;
            CommandPacket[8] = 0x03;
            CommandPacket[9] = 0x01;
            CommandPacket[10] = 0x01;
            CommandPacket[11] = 0xF2;
            CommandPacket[12] = 0xFB;
            CommandPacket[13] = 0x10;
            CommandPacket[14] = 0x03;
            CommandPacket[15] = 0x0D;
 
            //명령 송신
            string readMsg = string.Empty;
            SendPacket(CommandPacket);
            DateTime dt = DateTime.Now;
            while (true)
            {
                if (IsTimeOut(dt, ReadTimeOut))
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "ReadRFID Module:{0} ReadRFID 타임아웃 발생.", this.ModuleName);
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("RFID_FAIL", ParentModule.ModuleName);
                    ReadData = "ERROR";
                    return false;
                }
                if (RcvQueue.TryDequeue(out RcvData)) //큐에서 꺼내기 성공했다면
                {
                    //응답 길이 체크
                    if (readMsg.Length != RFID_DATA_LENGTH)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "ReadRFID Module:{0} 메시지 길이 이상.Message :{1}", this.ModuleName, StringToHex(readMsg));
                        ReadData = "ERROR";
                        return false;
                    }
                    //자세한 사양 확인전까지는 길이기반으로 데이터 분리.
                    string rData = "";
                    if (RcvData.Length <= 11)
                    {
                        rData = "ERROR";
                    }
                    else
                    {
                        rData = Encoding.ASCII.GetString(e.ReceiveData, 12, RFID_DATA_LENGTH);
                    }

                    string RFID_TAGDATA = readMsg.Replace('\0', ' '); //NULL 값은 스페이스로 변환

                    ReadData = RFID_TAGDATA;
                    return true;//성공 리턴
                }
                Thread.Sleep(50);
            }
        }
        protected override void OnReceive(object sender, AsyncSocketReceiveEventArgs e)
        {
            WriteReceiveLog(e.ReceiveData, e.ReceiveBytes);
            byte[] temp = new byte[e.ReceiveBytes];
            for(int i = 0 ; i < e.ReceiveBytes ;i++)
            {
                temp[i] = e.ReceiveData[i];
            }
            RcvQueue.Enqueue(temp);

            //대진RFID 로직
            //string rData = Encoding.ASCII.GetString(e.ReceiveData);
            //02 20 01 14 00 01 0D AC 93 10 03 00 00 00 00 00 00 00 00 00 00 00 00 00   [READ FAIL]
            //02 20 01 14 00 0E 00 00 01 03 01 01 [31 31 36 30 35 32 31 37] C6 DC 10 03  [11605217]
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
        ~RFID_DEAJIN()
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
