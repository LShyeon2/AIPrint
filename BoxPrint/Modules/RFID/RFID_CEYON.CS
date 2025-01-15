using BoxPrint.Communication;
using BoxPrint.Log;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace BoxPrint.Modules.RFID
{
    public class RFID_CEYON : RFID_ModuleBase, IDisposable
    {
        //private int ReadTimeOut = 6;
        private int WriteTimeOut = 6;

        public RFID_CEYON(string mName, eRFIDComType Comtype, bool simul) : base(mName, Comtype, simul)
        {
            ReadTimeOut = 6;
        }

        public override bool ReadRFID(out string ReadData)
        {
            try
            {
                if (CheckConnection() == false)
                {
                    GlobalData.Current.Alarm_Manager.AlarmOccur("550", ModuleName); //RFID 연결 불량
                    ReadData = "ERROR";
                    return false;
                }
                ReadCompleted = false;
                ClearRcvQueue();//동작전에 혹시 들어온  메시지 삭제.
                byte[] RcvData;
                //명령 패킷을 만든다.
                byte[] CommandPacket = new byte[6];

                CommandPacket[0] = 0x05; //ENQ
                CommandPacket[1] = 0x01; //RID
                CommandPacket[2] = 0x80; //READ CMD 
                CommandPacket[3] = 0x00; //ADDRESS
                CommandPacket[4] = this.RFID_DATA_LENGTH;
                CommandPacket[5] = GetCheckSum(CommandPacket);
                //명령 송신
                SendPacket(CommandPacket);
                ReadTriggerSent = true;
                Stopwatch timeWatch = Stopwatch.StartNew();
                while (true)
                {
                    if (IsTimeout_SW(timeWatch, ReadTimeOut))
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "ReadRFID Module:{0} ReadRFID 타임아웃 발생.", this.ModuleName);
                        ReadData = "ERROR";
                        return false;
                    }
                    if (RcvQueue.TryDequeue(out RcvData)) //큐에서 꺼내기 성공했다면
                    {
                        //응답 길이 체크
                        if (RcvData.Length != RFID_DATA_LENGTH + 4)
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "ReadRFID Module:{0} 메시지 길이 이상", this.ModuleName);
                            ReadData = "ERROR";
                            return false;
                        }
                        //메시지 검증
                        if (RcvData.Length < 5)
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "ReadRFID Module:{0} 메시지 길이 미만이상.", this.ModuleName);
                            ReadData = "ERROR";
                            return false;
                        }
                        if (RcvData[RcvData.Length - 1] != 0x03) //ETX
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "ReadRFID Module:{0} 메시지 포맷 이상.", this.ModuleName);
                            ReadData = "ERROR";
                            return false;
                        }
                        if (RcvData[0] == 0x15) // NAK
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "ReadRFID Module:{0} 메시지 Nak Code : {1}  Error Msg : {2}", this.ModuleName, RcvData[3], GetErrorMessage(RcvData[3]));
                            ReadData = "ERROR";
                            return false;
                        }
                        if (RcvData[0] != 0x02) //STX
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "ReadRFID Module:{0} 메시지 포맷 이상.", this.ModuleName);
                            ReadData = "ERROR";
                            return false;
                        }
                        string RFID_TAGDATA = Encoding.ASCII.GetString(RcvData, 3, RFID_DATA_LENGTH).Replace('\0', ' '); //NULL 값은 스페이스로 변환

                        ReadData = RFID_TAGDATA;
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
        public override bool WriteRFID(byte[] RFID_Data)
        {
            ClearRcvQueue();//동작전에 혹시 들어온  메시지 삭제.
            //요청 데이타가 기준 최대값을 초과
            if (RFID_Data.Length > RFID_WRITE_LENGTH)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "Write RFID Module:{0} WriteRFID 쓰기 요청 데이타가 최대치를 초과했습니다. Size :{0}  ", ModuleName, RFID_Data.Length);
                return false;
            }
            byte[] RcvData;
            //명령 패킷을 만든다.
            byte[] CommandPacket = new byte[RFID_WRITE_LENGTH + 6]; //데이타에 제어바이트 5 체크섬 1바이트추가
            CommandPacket[0] = 0x05;
            CommandPacket[1] = 0x01;
            CommandPacket[2] = 0x90;
            CommandPacket[3] = 0x08;
            CommandPacket[4] = RFID_WRITE_LENGTH;
            RFID_Data.CopyTo(CommandPacket, 5);
            CommandPacket[CommandPacket.Length - 1] = GetCheckSum(CommandPacket);

            //송신
            SendPacket(CommandPacket);

            Stopwatch timeWatch = Stopwatch.StartNew();
            while (true)
            {
                if (IsTimeout_SW(timeWatch, WriteTimeOut))
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "WriteRFID Module:{0} WriteRFID 타임아웃 발생.", this.ModuleName);
                    //GlobalData.Current.Alarm_Manager.AlarmOccurbyName("RFID_FAIL", ParentModule.ModuleName); //알람 안올리고 Error 로 보고만
                    return false;
                }
                if (RcvQueue.TryDequeue(out RcvData)) //큐에서 꺼내기 성공했다면
                {
                    //메시지 검증
                    if (RcvData.Length < 4)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "WriteRFID Module:{0}  응답 메시지 길이 미만이상.", this.ModuleName);

                        return false;
                    }

                    if (RcvData[RcvData.Length - 1] != 0x03) //ETX
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "WriteRFID Module:{0} 응답메시지 포맷 이상.", this.ModuleName);

                        return false;
                    }
                    if (RcvData[0] == 0x15) // NAK
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "WriteRFID Module:{0} 응답 메시지 Nak Code {1}.Message :{2}", this.ModuleName, RcvData[3], GetErrorMessage(RcvData[3]));
                        return false;
                    }
                    if (RcvData[0] != 0x06) //Write Ack 체크
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "WriteRFID Module:{0} 응답 메시지 포맷 이상.", this.ModuleName);

                        return false;
                    }
                    return true; //Write 성공
                }
                Thread.Sleep(50);
            }
        }


        private byte GetCheckSum(byte[] buf)
        {
            int CheckSum = 0;

            for (int i = 0; i < buf.Length; i++)
            {
                CheckSum += buf[i];
            }
            if (CheckSum > byte.MaxValue)
            {
                CheckSum = CheckSum % (byte.MaxValue + 1);
            }
            return (byte)CheckSum;
        }

        string GetErrorMessage(byte ErrCode)
        {
            switch (ErrCode)
            {
                case 0x00: return "No Error";
                case 0x01: return "Unknown Command ID";
                case 0x02: return "Not Yet Implemented Command ID";
                case 0x03: return "Invalid Destination Address(Device ID)";
                case 0x04: return "Invalid System Register Address";
                case 0x05: return "Timeout Error";
                case 0x06: return "Invalid SLRC Register Address";
                case 0x07: return "Out of System Register Address Range";
                case 0x08: return "Out of SLRC Register Address Range";
                case 0x09: return "Out of RF Channel Number";
                case 0x0A: return "Out of Bit Range";
                case 0x0B: return "Invalid Bit Value";
                case 0x0C: return "Check Sum Error";
                case 0x0D: return "Write Command Fail";
                case 0x0E: return "Read Command Fail";
                case 0x0F: return "Long Data Length (max 32 bytes)";
                case 0x10: return "RF Channel Disabled";
                case 0x11: return "SLRC Reset Error";
                case 0x12: return "SLRC Parallel Bus Error";
                case 0x13: return "Max Timeslot Error(max 255)";
                case 0x14: return "Not Supported RF Protocol";
                case 0x15: return "ICODE Wrong Command Parameter";
                case 0x16: return "ICODE Timeout";
                case 0x17: return "ICODE No Tag 0x18 ICODE CRC Error";
                case 0x19: return "ICODE Collision Error";
                case 0x1A: return "ICODE SNR Error";
                case 0x1B: return "ICODE Count Error";
                case 0x1C: return "RFU";
                case 0x1D: return "ICODE Invalid Quit Value";

                case 0x1E: return "ICODE Weak Collision Error";
                case 0x1F: return "ICODE Write Fail";
                case 0x20: return "ICODE Halt Fail";
                case 0x21: return "ICODE Not implemented Error";
                case 0x22: return "RFU";
                case 0x23: return "RFU";
                case 0x24: return "RFU";
                case 0x25: return "RFU";
                case 0x26: return "RFU";
                case 0x27: return "Family Code Mismatch";
                case 0x28: return "Application Code Mismatch";
                case 0x29: return "ICODE Framing Error";
                case 0x2A: return "Carrier Disabled";
            }
            return "Undefine Error";
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
        ~RFID_CEYON()
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
