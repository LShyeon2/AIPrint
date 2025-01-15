using ActUtlType64Lib;
using PLCProtocol.Base;
using PLCProtocol.DataClass;
using BoxPrint.Config;           //20220728 조숭진 config 방식 변경
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace PLCProtocol
{
    class Protocol_MxComponent : ProtocolBase
    {
        private bool _IsConnect = false;
        public override bool IsConnect => _IsConnect;

        private short _PlcNum;
        public override short PlcNum => _PlcNum;
        private readonly int OK_Code = 0;
        public ActUtlType64Class _ActUtlType;
        private short LogicalStation { get; set; }

        private object thisLock = new object();

        public Protocol_MxComponent(PLCElement element) : base(element)
        {
            _PlcNum = element.Num;
            //MxComponent에서는 포트를 로지컬 스테이션으로 사용한다.
            LogicalStation = element.Port;

            if (plcState is null) plcState = new PLCStateData();
            plcState.ConnectInfo = LogicalStation.ToString();
        }
        public override bool Close()
        {
            try
            {
                int code = _ActUtlType.Close();

                if (!code.Equals(OK_Code))
                {
                    Console.WriteLine(string.Format("{0} Close Fail. Fail Code {1}", LogicalStation, code));
                    //이경우에는 연결상태를 변경하지 않음.
                    return false;
                }
                else
                {
                    Console.WriteLine(string.Format("{0} Close Complete", LogicalStation));
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("{0} Close Error. Message {1}", LogicalStation, ex.ToString()));
            }

            return false;
        }

        public override bool Connect()
        {
            try
            {
                if (_ActUtlType == null)
                    _ActUtlType = new ActUtlType64Class();

                _ActUtlType.ActLogicalStationNumber = LogicalStation;

                int code = _ActUtlType.Open();

                if (!code.Equals(OK_Code))
                {
                    Console.WriteLine(string.Format("{0} Connect Fail. Fail Code {1}", LogicalStation, code));
                    _IsConnect = false;
                    return false;
                }
                else
                {
                    Console.WriteLine(string.Format("{0} Connect Complete", LogicalStation));
                    _IsConnect = true;
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(string.Format("{0} Connect Error. Message {1}", LogicalStation, ex.ToString()));
            }

            return false;
        }

        public override bool Read(PLCDataItem pItem, out byte[] readValue, out byte ErrorCode)
        {
            ErrorCode = 0;
            readValue = null;
            eDataType dataType = pItem.DataType;
            int iaddressoffset = pItem.ItemPLCAddress;
            int BitOffset = pItem.BitOffset;
            int size = pItem.Size;

            int iaddress;
            short[] read = new short[size];     //읽어올 데이터 갯수만큼 배열 생성
            string strAddress;
            int iret = -1;
            //object ret;

            try
            {
                //Address 계산
                iaddress = iaddressoffset;

                //iaddress = StartAddress;
                strAddress = pItem.DeviceType.ToString() + iaddress;

                iret = _ActUtlType.ReadDeviceBlock2(strAddress, size, out read[0]);

                if (!iret.Equals(0))
                {
                    Console.WriteLine(string.Format("Read Data ReadDeviceBlock Result {0}", iret));
                    return false;
                }

                List<byte> lists = new List<byte>();
                for (int i = 0; i < read.Length; i++)
                {
                    lists.AddRange(BitConverter.GetBytes(read[i]));
                }
                readValue = lists.ToArray();
                return true;


                //switch (dataType)
                //{
                //    case eDataType.Bool:
                //        BitArray bitArr = null;
                //        byte[] byteArr = null;

                //        //읽어온 워드를 바이트로 변환   //bool은 1워드만 읽음.
                //        byteArr = BitConverter.GetBytes(read[0]);

                //        if (NeedByteByBit)
                //            return byteArr;

                //        //바이트 변환 데이터를 비트 배열로 변환
                //        bitArr = new BitArray(byteArr);
                //        //비트 배열에서 읽어올 위치의 비트 정보를 가져옴.
                //        if (bitArr[BitOffset])
                //            ret = true;
                //        else
                //            ret = false;
                //        break;

                //    case eDataType.Short:
                //        string stemp = string.Empty;
                //        for (int i = 0; i < read.Length; i++)
                //        {
                //            string valuetemp = Convert.ToString(read[i], 2).PadLeft(16, '0');

                //            stemp = valuetemp + stemp;
                //        }
                //        ret = Convert.ToInt32(stemp, 2);

                //        break;

                //    case eDataType.String:
                //        string strret = string.Empty;
                //        for (int i = 0; i < read.Length; i++)
                //        {
                //            byte[] bytes = BitConverter.GetBytes(read[i]);
                //            strret += Encoding.Default.GetString(bytes).Trim('\0');

                //        }
                //        ret = strret.Trim();
                //        break;
                //    default:
                //        Console.WriteLine("DataType {0} is not registered", dataType);
                //        return false;
                //}
            }
            catch (Exception ex)
            {
                throw ex;
            }

            //return false;
        }

        public override bool Write(PLCDataItem pItem, object writeValue, out byte ErrorCode)
        {

            eDataType dataType = pItem.DataType;
            string deviceType = pItem.DeviceType.ToString();
            int iaddressoffset = pItem.ItemPLCAddress;
            int BitOffset = pItem.BitOffset;
            int size = pItem.Size * 2;  //1워드에 2글자씩 써진다

            int iaddress;
            int iret = -1;
            short[] write = new short[size];
            string strAddress;
            ErrorCode = 0;
            try
            {
                iaddress = iaddressoffset;

                //DeviceType가 없다면 리딩을 하면 안됨.
                if (string.IsNullOrEmpty(deviceType))
                    return false;

                //iaddress = StartAddress;
                strAddress = deviceType + iaddress;

                lock (thisLock)
                {
                    #region WriteData 생성
                    switch (dataType)
                    {
                        case eDataType.Bool:
                            BitArray bitArr = null;
                            byte[] byteArr = null;
                            byte[] byteTemp = null;

                            if (!Read(pItem, out byteArr, out byte _))
                            {
                                //실패하면?
                                return false;
                            }

                            //바이트 변환 데이터를 비트 배열로 변환
                            bitArr = new BitArray(byteArr);
                            //비트 배열에서 기재할 위치의 비트 정보 업데이트
                            bitArr.Set(BitOffset, (bool)writeValue);
                            //비트 배열을 바이트 배열로 변환
                            byteTemp = ProtocolHelper.BitArrayToByteArray(bitArr);
                            //바이트 배열을 기재할 형식(short)으로 변환
                            write[0] = BitConverter.ToInt16(byteTemp, 0);
                            break;

                        case eDataType.Short:
                            //2바이트 숫자기재 되는지, 안된다면 어떻게해야하는지 방법 확인필요.
                            write[0] = Convert.ToInt16(writeValue);
                            break;

                        case eDataType.String:
                            string strTemp = string.Empty;

                            if (((string)writeValue).Length > size)
                            {
                                strTemp = ((string)writeValue).Remove(size);
                            }
                            else
                                strTemp = (string)writeValue;

                            strTemp = strTemp.PadRight(size, '\0'); //뒤에 공간은 공백으로 채워서 보내야 한다. 매 초기화가 아닌 오버라이팅 방식이기에 자릿수가 달라지면 이전 쓰레기값이 있을 수 있음.
                            ASCIIEncoding asc = new ASCIIEncoding();
                            byte[] bytTemp = asc.GetBytes(strTemp);
                            write = new short[(int)Math.Ceiling(bytTemp.Length / 2.0)];
                            Buffer.BlockCopy(bytTemp, 0, write, 0, bytTemp.Length);

                            size = write.Length;    //PLC에 써주는 사이즈를 데이터 길이 만큼만 써준다.
                            break;
                        default:
                            Console.WriteLine("DataType {0} is not registered", dataType);
                            return false;
                    }
                    #endregion

                    iret = _ActUtlType.WriteDeviceBlock2(strAddress, size, ref write[0]);

                    if (!iret.Equals(0))
                    {
                        Console.WriteLine(string.Format("WriteDeviceBlock Result {0}", iret));
                        return false;
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public override object ReadValueConvert(PLCDataItem pItem, byte[] readValue)
        {
            eDataType dataType = pItem.DataType;
            int BitOffset = pItem.BitOffset;
            int size = pItem.Size;
            object ret;

            switch (dataType)
            {
                case eDataType.Bool:
                    //바이트 변환 데이터를 비트 배열로 변환
                    BitArray bitArr = new BitArray(readValue);
                    //비트 배열에서 읽어올 위치의 비트 정보를 가져옴.
                    if (bitArr[BitOffset])
                        ret = true;
                    else
                        ret = false;
                    break;

                case eDataType.Short:
                    if (readValue.Length.Equals(2))
                        ret = BitConverter.ToUInt16(readValue, 0);
                    else
                        ret = BitConverter.ToUInt32(readValue, 0);
                    break;

                case eDataType.String:
                    string strret = string.Empty;
                    strret += Encoding.Default.GetString(readValue).Trim('\0');
                    ret = strret.Trim();
                    break;
                //221014 HHJ SCS 개선     //- MxCom DataType Raw 추가
                case eDataType.Raw:
                    return readValue;
                default:
                    Console.WriteLine("DataType {0} is not registered", dataType);
                    return false;
            }

            return ret;
        }
    }
}
