using PLCProtocol.Base;
using PLCProtocol.CommunicationType;
using PLCProtocol.DataClass;
using BoxPrint;
using BoxPrint.Config;       //20220728 조숭진 config 방식 변경
using BoxPrint.Log;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace Communication.PLCProtocol
{
    public class Protocol_MCProtocolBase : ProtocolBase
    {
        protected ReaderWriterLockSlim ComSync = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion); //240410 RGJ 프로토콜 멀티 스레드 동시 접근을 막기 위해 락을 건다.

        protected object SerialNoSyncLock = new object();
        public override bool IsConnect
        {
            get
            {
                //230911 HHJ PLC 상태 관련 추가
                //if (tSocket == null)
                //    return false;
                //else
                //{
                //    return tSocket.SocketConnected;
                //}
                bool retValue = false;
                if (tSocket == null)
                    retValue = false;
                else
                    retValue = tSocket.SocketConnected;

                if (_IsConnect != retValue)
                {
                    _IsConnect = retValue;
                    if (_IsConnect)
                    {
                        SetPlcStateData(ePLCStateDataState.Connect);
                    }
                    else
                    {
                        SetPlcStateData(ePLCStateDataState.DisConnect);
                    }
                }

                return retValue;
            }
        }

        protected ePLCSeries PLCSeries;
        protected eCommunicationType ComType;
        protected CommunicationTypeBase PLCCom;

        private short _PlcNum;
        public override short PlcNum => _PlcNum;
        protected TCP_ASync tSocket = null;


        protected virtual byte[] SubHeader(bool bAddedserialNo = true) { return null; }
        protected virtual bool CheckResponseSubHeader(byte[] recv, out byte[] other) { other = null; return false; }

        public Protocol_MCProtocolBase(PLCElement element) : base(element)
        {
            _PlcNum = element.Num;

            PLCSeries = element.Series;
            ComType = element.ComType;

            if (ComType.Equals(eCommunicationType.Ascii))
                PLCCom = new AsciiType(PLCSeries);
            else
                PLCCom = new BinaryType(PLCSeries);

            IP = element.Ip;
            Port = element.Port;

            tSocket = new TCP_ASync(IP, Port);

            if (plcState is null) plcState = new PLCStateData();
            plcState.ConnectInfo = string.Format("{0}:{1}", IP, Port);
            plcState.PLCName = element.PLCName;
            BackupProtocolNum = element.BackupNum;

        }


        //SendPacket => SendMCRequestMsg 기존 소켓 SendPacket 과 중복되서 헷갈리기에 이름 변경함.
        protected bool SendMCRequestMsg(byte[] msg,out byte[] Response) 
        {
            if (GlobalData.Current.WritePLCRawLog) //임시로그
            {
                LogManager.WritePLCLog("Send =>{0}  {1}", this.IP , BitConverter.ToString(msg));
            }
            return tSocket.SendPacket(msg, out Response);
        }

        public override bool Connect()
        {
            try
            {
                if (tSocket.SocketConnected)
                {
                    SetPlcStateData(ePLCStateDataState.Connect);
                    return true;
                }


                //230911 HHJ PLC 상태 관련 추가 Start
                //return tSocket.Connect();
                if (tSocket.Connect())
                {
                    SetPlcStateData(ePLCStateDataState.Connect);
                    return true;
                }
                else
                {
                    SetPlcStateData(ePLCStateDataState.DisConnect);
                    return false;
                }
                //230911 HHJ PLC 상태 관련 추가 End
            }
            catch (Exception)
            {
                SetPlcStateData(ePLCStateDataState.Unknown);        //230911 HHJ PLC 상태 관련 추가
                return false;
            }
        }

        public override bool Close()
        {
            SetPlcStateData(ePLCStateDataState.DisConnect);
            return base.Close();
        }

        private bool CheckConnection()
        {
            return IsConnect;
        }

        public override bool Read(PLCDataItem pItem, out byte[] readValue, out byte ErrorCode)
        {
            readValue = null;
            ErrorCode = 0;
            ComSync.EnterWriteLock();
            try
            {
                if (!CheckConnection())
                {
                    ErrorCode = 1;
                    return false;
                }


                List<byte> lists = new List<byte>();
                byte[] subHeader = SubHeader();
                byte[] command = PLCCom.MakeProtocolCommand(pItem, true);
                lists.AddRange(subHeader);
                lists.AddRange(command);

                string str = Encoding.Default.GetString(lists.ToArray());
                byte[] RcvData;
                if (SendMCRequestMsg(lists.ToArray(), out RcvData) == false ) //20240409 RGJ MC 프로토콜 수정. 패킷 보내고 실패여부를 따로 리턴한다.
                {
                    return false;
                }

                byte[] temp = new byte[RcvData.Length]; //임시 배열에 복사
                for (int i = 0; i < RcvData.Length; i++) { temp[i] = RcvData[i]; }

                if (!CheckResponseSubHeader(temp, out temp))
                {
                    ErrorCode = 2;
                    return false;
                }
                if (PLCCom.CheckResponse(temp, out byte[] responseValue))
                {
                    readValue = responseValue;
                    //readValue = ProtocolHelper.AsciiReadByteChange(responseValue);
                    return true;
                }
                else
                {

                }
            }
            catch (Exception)
            {

            }
            finally
            {
                ComSync.ExitWriteLock();
            }
            ErrorCode = 3;
            return false;
        }

        public override bool Write(PLCDataItem pItem, object writeValue, out byte ErrorCode)
        {
            ComSync.EnterWriteLock();
            try
            {
                ErrorCode = 0;
                if (!CheckConnection())
                {
                    ErrorCode = 1;
                    return false;
                }

                //데이터 타입이 Bool인데, 디바이스가 D(워드형)이라면 해당 워드 리딩이 필요하다.
                byte[] bitWord = null;
                if (pItem.DataType.Equals(eDataType.Bool) &&
                    (pItem.DeviceType.Equals(eDevice.D) || pItem.DeviceType.Equals(eDevice.W)))
                {
                    while (true)
                    {
                        if (!Read(pItem, out bitWord, out byte errorCode))
                        {
                            ErrorCode = 2;
                            LogManager.WriteConsoleLog(eLogLevel.Info,"PLC READ FAIL Before Bit Write {0} FailCode:{1}", pItem.ItemName, errorCode);
                            return false;       //실패하면?
                        }

                        byte[] bitWordTemp = null;
                        if (ComType.Equals(eCommunicationType.Ascii))
                            bitWordTemp = ProtocolHelper.AsciiReadByteChange(bitWord);
                        else
                            bitWordTemp = bitWord;

                        //명령 송신
                        List<byte> lists = new List<byte>();
                        byte[] subHeader = SubHeader();
                        byte[] command = PLCCom.MakeProtocolCommand(pItem, false, writeValue, bitWordTemp);
                        lists.AddRange(subHeader);
                        lists.AddRange(command);
                        byte[] RcvData;
                        if(SendMCRequestMsg(lists.ToArray(),out RcvData) == false) //20240409 RGJ MC 프로토콜 수정. 패킷 보내고 실패여부를 따로 리턴한다.
                        {
                            ErrorCode = 3;
                            return false;
                        }

                        byte[] temp = new byte[RcvData.Length];
                        for (int i = 0; i < RcvData.Length; i++) { temp[i] = RcvData[i]; }

                        if (!CheckResponseSubHeader(temp, out temp))
                        {
                            ErrorCode = 4;
                            return false;
                        }
                        if (PLCCom.CheckResponse(temp, out byte[] responseValue))
                        {
                            return true;
                        }
                        else
                        {
                            ErrorCode = 5;
                            return false;
                        }
                    }
                }
                else
                {
                    //명령 송신
                    List<byte> lists = new List<byte>();
                    byte[] subHeader = SubHeader();
                    byte[] command = PLCCom.MakeProtocolCommand(pItem, false, writeValue, bitWord);
                    lists.AddRange(subHeader);
                    lists.AddRange(command);

                    byte[] RcvData;
                    if (SendMCRequestMsg(lists.ToArray(), out RcvData) == false) //20240409 RGJ MC 프로토콜 수정. 패킷 보내고 실패여부를 따로 리턴한다. 
                    {
                        ErrorCode = 6;
                        return false;
                    }

                    byte[] temp = new byte[RcvData.Length];
                    for (int i = 0; i < RcvData.Length; i++) { temp[i] = RcvData[i]; }

                    if (!CheckResponseSubHeader(temp, out temp))
                    {
                        ErrorCode = 7;
                        return false;
                    }

                    if (PLCCom.CheckResponse(temp, out byte[] responseValue))
                    {
                        return true;
                    }
                    else
                    {
                        ErrorCode = 8;
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorCode = 11;
                LogManager.WriteConsoleLog(BoxPrint.eLogLevel.Info, ex.ToString()); //임시로그 추가
                return false;
            }
            finally
            {
                ComSync.ExitWriteLock();
            }
        }

        public override object ReadValueConvert(PLCDataItem pItem, byte[] readValue)
        {
            return PLCCom.ReadDataConvert(pItem, readValue);
        }
    }
}
