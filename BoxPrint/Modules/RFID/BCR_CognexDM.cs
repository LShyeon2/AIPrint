using BoxPrint.Communication;
using BoxPrint.Log;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BoxPrint.Modules.RFID
{
    /// <summary>
    /// Cognex Dataman 전용 BCR Reader 구현
    /// </summary>
    public class BCR_CognexDM : RFID_ModuleBase, IDisposable
    {
        private bool disposedValue;

        public BCR_CognexDM(string mName, eRFIDComType Comtype, bool simul) : base(mName, Comtype, simul)
        {
        }
        public override bool ReadRFID(out string ReadData)
        {
            try
            {
                if (!CheckConnection())
                {
                    ReadData = "ERROR";
                    return false;
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
                byte[] CommandPacket;

                string cmd = string.Format("||>TRIGGER ON\r\n"); //Cognex Dataman Command Trigger 

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
                        LogManager.WriteConsoleLog(eLogLevel.Info, "ReadRFID Module:{0} ReadRFID 타임아웃 발생.", this.ModuleName);
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
                        //메시지 검증
                        if (RcvData.Length < 4)
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "ReadRFID Module:{0} 메시지 길이 미만이상.", this.ModuleName);
                            ReadData = "ERROR";
                            continue;  
                        }


                        string BCR_DATA = Encoding.ASCII.GetString(RcvData, 0, RcvData.Length);

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

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    ClearRcvQueue();
                }
                if (CommunicationType == eRFIDComType.TCP_IP)
                {
                    this.ModuleSocket?.Close();
                }
                else if (CommunicationType == eRFIDComType.RS_232)
                {
                    this.ModuleSerialPort?.Close();
                }

                // TODO: 비관리형 리소스(비관리형 개체)를 해제하고 종료자를 재정의합니다.
                // TODO: 큰 필드를 null로 설정합니다.
                disposedValue = true;
            }
        }

        // // TODO: 비관리형 리소스를 해제하는 코드가 'Dispose(bool disposing)'에 포함된 경우에만 종료자를 재정의합니다.
        ~BCR_CognexDM()
        {
            // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // 이 코드를 변경하지 마세요. 'Dispose(bool disposing)' 메서드에 정리 코드를 입력합니다.
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
