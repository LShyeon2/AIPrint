using PLCProtocol.Base;
using PLCProtocol.DataClass;
using BoxPrint.Log;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PLCProtocol.CommunicationType
{
    public class AsciiType : CommunicationTypeBase
    {
        private ePLCSeries _PLCSeries;
        protected override ePLCSeries PLCSeries => _PLCSeries;

        public AsciiType(ePLCSeries series)
        {
            _PLCSeries = series;
        }
        #region MC Protocol Req
        //서브헤더 / 엑세스 루트 / 리퀘스트 데이터 길이 / 모니터링 타이머 / 리퀘스트 데이터
        //서브헤더는 프레임단에서 생성된다.
        /// <summary>
        /// AccessRoute 구성
        /// Network 번호
        /// PC 번호
        /// Request destination module I/O No
        /// Request destination module station No
        /// </summary>
        /// <returns>AccessRoute</returns>
        protected override byte[] AccessRoute()
        {
            //멀티드롭 해당없음.
            //NetWork Number                            ->  00      일단 Fix
            //PC Number                                 ->  FF      일단 Fix
            //Request destination module I/ O No        ->  03FF    Fix
            //Request destination module station No     ->  00      Fix
            return Encoding.Default.GetBytes("00" + "FF" + "03FF" + "00");
        }
        /// <summary>
        /// RequestDataLength
        /// 모니터링 타이머 + 리퀘스트 데이터의 토탈 사이즈 (단위 - 바이트)
        /// </summary>
        /// <param name="reqData">리퀘스트 데이터</param>
        /// <returns>RequestDataLength</returns>
        protected override byte[] RequestDataLength(byte[] reqData, byte[] monitoring)
        {
            //2워드를 사용하고 빈값은 0으로 왼쪽부터 채워준다.
            return Encoding.Default.GetBytes(
                ProtocolHelper.IntToHexString(reqData.Length + monitoring.Length).PadLeft(4, '0'));
        }
        /// <summary>
        /// MonitoringTimer
        /// 응답까지의 시간을 나타냄 0x01당 250ms
        /// </summary>
        /// <returns>MonitoringTimer</returns>
        protected override byte[] MonitoringTimer()
        {
            //2워드를 사용하고 빈값은 0으로 왼쪽부터 채워준다.
            return Encoding.Default.GetBytes("4".PadLeft(4, '0')); //240410 RGJ MonitoringTimer 1초에서 2.5초로 (0x10) 로 변경 다시 1초 원복.
        }
        /// <summary>
        /// RequestData 구성
        /// Command + SubCommand + Device
        /// Command - DataCommand
        /// SubCommand - DataSubCommand
        /// Device - DataHeadDevice
        /// <param name="pItem">데이터 클래스</param>
        /// <param name="read">Read / Write 여부</param>
        /// <param name="WriteValue">Write의 경우 WriteValue</param>
        /// <returns>RequestData</returns>
        protected override byte[] RequestData(PLCDataItem pItem, bool read, object WriteValue = null, byte[] bitWord = null)
        {
            List<byte> lists = new List<byte>();
            lists.AddRange(DataCommand(read));
            lists.AddRange(DataSubCommand(pItem.DeviceType));
            lists.AddRange(DataHeadDevice(pItem));

            if (!read)
                lists.AddRange(WriteDataConvert(pItem, WriteValue, bitWord));

            return lists.ToArray();
        }
        /// <summary>
        /// DataCommand
        /// Read / Write에 따른 Command 구분
        /// </summary>
        /// <param name="read">Read / Write 구분</param>
        /// <returns>DataCommand</returns>
        protected override byte[] DataCommand(bool read)
        {
            if (read)
            {
                return Encoding.Default.GetBytes("0401");
            }
            else
            {
                return Encoding.Default.GetBytes("1401");
            }
        }
        /// <summary>
        /// DataSubCommand
        /// Read / Write Device에 따른 SubCommand
        /// </summary>
        /// <param name="device">구분 Device</param>
        /// <returns>DataSubCommand</returns>
        protected override byte[] DataSubCommand(eDevice device)
        {
            byte[] bytes = null;
            //Q시리즈 워드 : 0000 / 비트 : 0001
            //R시리즈 워드 : 0002 / 비트 : 0003
            switch (device)
            {
                //B만 비트고 W와 D는 워드로 리드 / 라이트 한다.
                case eDevice.B:
                    if (PLCSeries.Equals(ePLCSeries.Q))
                        bytes = Encoding.Default.GetBytes("0001");
                    else
                        bytes = Encoding.Default.GetBytes("0003");
                    break;
                case eDevice.W:
                case eDevice.D:
                    if (PLCSeries.Equals(ePLCSeries.Q))
                        bytes = Encoding.Default.GetBytes("0000");
                    else
                        bytes = Encoding.Default.GetBytes("0002");
                    break;
                default:
                    break;

            }
            return bytes;
        }
        /// <summary>
        /// DataHeadDevice
        /// Read / Write Device 정보
        /// Device Code + Device Number + Size
        /// Device Code - HeadDeviceCode
        /// Device Number - HeadDeviceNumber
        /// Size - DataDevicePoints
        /// </summary>
        /// <returns>DataHeadDevice</returns>
        protected override byte[] DataHeadDevice(PLCDataItem pItem)
        {
            List<byte> lists = new List<byte>();
            lists.AddRange(HeadDeviceCode(pItem.DeviceType));
            lists.AddRange(HeadDeviceNumber(pItem.DeviceType, pItem.ItemPLCAddress));
            lists.AddRange(DataDevicePoints(pItem.Size));

            return lists.ToArray();
        }
        /// <summary>
        /// HeadDeviceCode
        /// Device 구분을 위한 설정 값.
        /// </summary>
        /// <param name="device">디바이스</param>
        /// <returns>HeadDeviceCode</returns>
        protected override byte[] HeadDeviceCode(eDevice device)
        {
            //Q타입은 2바이트 생성, 구분 값 + 우측 '*' 패딩
            //R타입은 4바이트 생성, 구분 값 + 우측 '*' 패딩
            if (PLCSeries.Equals(ePLCSeries.Q))
                return Encoding.Default.GetBytes(device.ToString().PadRight(2, '*'));
            else
                return Encoding.Default.GetBytes(device.ToString().PadRight(4, '*'));
        }
        /// <summary>
        /// HeadDeviceNumber
        /// 주소값
        /// </summary>
        /// <param name="iaddress">주소값</param>
        /// <returns>HeadDeviceNumber</returns>
        protected override byte[] HeadDeviceNumber(eDevice device, int iaddress)
        {
            //Q타입은 6바이트 생성, 구분 값 + 좌측 '0' 패딩
            //R타입은 8바이트 생성, 구분 값 + 좌측 '0' 패딩
            //if (PLCSeries.Equals(ePLCSeries.Q))
            //    return Encoding.Default.GetBytes(ProtocolHelper.IntToHexString(iaddress).PadLeft(6, '0'));
            //else
            //    return Encoding.Default.GetBytes(ProtocolHelper.IntToHexString(iaddress).PadLeft(8, '0'));
            string straddress = string.Empty;
            if (device.Equals(eDevice.D))
                straddress = iaddress.ToString();
            else
                straddress = ProtocolHelper.IntToHexString(iaddress);

            //if (PLCSeries.Equals(ePLCSeries.Q))
            //    return Encoding.Default.GetBytes(iaddress.ToString().PadLeft(6, '0'));
            //else
            //    return Encoding.Default.GetBytes(iaddress.ToString().PadLeft(8, '0'));
            if (PLCSeries.Equals(ePLCSeries.Q))
                return Encoding.Default.GetBytes(straddress.PadLeft(6, '0'));
            else
                return Encoding.Default.GetBytes(straddress.PadLeft(8, '0'));
        }
        /// <summary>
        /// DataDevicePoints
        /// Read / Write Data Size
        /// </summary>
        /// <param name="datasize">사이즈</param>
        /// <returns>사이즈</returns>
        protected override byte[] DataDevicePoints(int datasize)
        {
            //2워드 고정
            //글자수가 아닌 워드 사이즈를 기재해야한다.
            //설정을 워드 사이즈로 설정을 진행하기에 데이터 사이즈 그대로 전송한다.
            //return Encoding.Default.GetBytes(datasize.ToString().PadLeft(4, '0'));
            byte[] ByteArr = Encoding.Default.GetBytes(ProtocolHelper.IntToHexString(datasize).PadLeft(4, '0'));
            return ByteArr;
        }
        #endregion

        #region MC Protocol Res
        //protected override bool CheckResponseSubHeader(byte[] recv, out byte[] other)
        //{
        //    string responseSubHeader = "D000";                  //리스폰스 D000
        //    int iStart = 0, iCount = 4;                         //리스폰스 서브헤서 2워드(4바이트)
        //    List<byte> lists = recv.ToList();                   //배열 요소 삭제를 위해 리스트 변환
        //    List<byte> temp = lists.GetRange(iStart, iCount);   //삭제할 요소 가져옴
        //    lists.RemoveRange(iStart, iCount);                  //가져온 요소 삭제
        //    other = lists.ToArray();                            //삭제되고 남은 요소들 배열로 재변환

        //    string response = Encoding.Default.GetString(temp.ToArray());
        //    if (response.Equals(responseSubHeader))
        //    {
        //        return true;
        //    }

        //    return false;
        //}
        protected override bool CheckResponseAccessRoute(byte[] recv, out byte[] other)
        {
            byte[] responseByte = AccessRoute();                //리스폰스는 리퀘스트랑 동일해야함.
            int iStart = 0, iCount = 10;                        //리스폰스 엑세스루트는 리퀘스트랑 동일한 5워드(10바이트)
            List<byte> lists = recv.ToList();                   //배열 요소 삭제를 위해 리스트 변환
            List<byte> temp = lists.GetRange(iStart, iCount);   //삭제할 요소 가져옴
            lists.RemoveRange(iStart, iCount);                  //가져온 요소 삭제
            other = lists.ToArray();                            //삭제되고 남은 요소들 배열로 재변환

            if (temp.ToArray().SequenceEqual(responseByte))
            {
                return true;
            }
            else
            {
                LogManager.WriteConsoleLog(BoxPrint.eLogLevel.Info, "MC CheckResponseAccessRoute Mismatch!");
                LogManager.WriteConsoleLog(BoxPrint.eLogLevel.Info, "{0} <> {1}", BitConverter.ToString(temp.ToArray()), BitConverter.ToString(responseByte));
                return false;
            }

        }
        protected override int CheckResponseDataLength(byte[] recv, out byte[] other)
        {
            int responseLength = -1;                            //렝쓰는 PLC에서 받은 리스폰스 정보에 있음.
            int iStart = 0, iCount = 4;                         //리스폰스 렝쓰는 2워드(4바이트)
            List<byte> lists = recv.ToList();                   //배열 요소 삭제를 위해 리스트 변환
            List<byte> temp = lists.GetRange(iStart, iCount);   //삭제할 요소 가져옴
            lists.RemoveRange(iStart, iCount);                  //가져온 요소 삭제

            string response = Encoding.Default.GetString(temp.ToArray());

            //220615 HHJ SCS 개선     //- PLC 이상현상 수정
            //Hex형식으로 오는 데이터인지라 헥사형으로 parse해야하는데, 일반형으로 pase해서 데이터 수량을 부족하게 가져옴.
            //if (int.TryParse(response, out responseLength))
            if (int.TryParse(response, System.Globalization.NumberStyles.HexNumber, null, out responseLength))
            {
                if (lists.Count > responseLength)
                {
                    lists.RemoveRange(responseLength, lists.Count - responseLength);
                    other = lists.ToArray();                        //삭제되고 남은 요소들 배열로 재변환
                }
                else
                    other = lists.ToArray();                        //삭제되고 남은 요소들 배열로 재변환

                return responseLength;
            }
            else
            {
                LogManager.WriteConsoleLog(BoxPrint.eLogLevel.Info, "MC CheckResponseDataLength Failed! response:{0}", response);
                other = new byte[0];
                return -1;
            }
        }
        protected override bool CheckResponseEndCode(byte[] recv, out byte[] other)
        {
            string responseEndCode;                             //에러코드는 PLC에서 받은 리스폰스 정보에 있음.
            int iStart = 0, iCount = 4;                         //리스폰스 엔드코드는 2워드(4바이트)
            List<byte> lists = recv.ToList();                   //배열 요소 삭제를 위해 리스트 변환
            List<byte> temp = lists.GetRange(iStart, iCount);   //삭제할 요소 가져옴
            lists.RemoveRange(iStart, iCount);                  //가져온 요소 삭제
            other = lists.ToArray();                            //삭제되고 남은 요소들 배열로 재변환

            //문자열로 변경한것이 에러코드
            responseEndCode = Encoding.Default.GetString(temp.ToArray());

            if (responseEndCode.Equals("0000"))
            {
                return true;
            }
            else
            {
                LogManager.WriteConsoleLog(BoxPrint.eLogLevel.Info, "MC CheckResponseEndCode : {0}", responseEndCode); //임시로그 추가.
                return false;
            }
        }
        #endregion

        #region Converter
        /// <summary>
        /// WriteDataConvert
        /// Object 형식 WriteValue Byte 배열로 변환
        /// </summary>
        /// <param name="pItem">아이템 클래스</param>
        /// <param name="value">WriteValue</param>
        /// <param name="bitWord">워드형 비트인 경우 해당 워드</param>
        /// <returns></returns>
        public override byte[] WriteDataConvert(PLCDataItem pItem, object value, byte[] bitWord = null)
        {
            eDataType dataType = pItem.DataType;
            int BitOffset = pItem.BitOffset;
            int size = pItem.Size * 2;	//1워드당 2바이트 1바이트당 한문자씩
            byte[] ConvertByte = null;

            try
            {
                switch (dataType)
                {
                    case eDataType.Bool:
                        ConvertByte = new byte[0];
                        if (pItem.DeviceType.Equals(eDevice.B))
                        {
                            //Bit 형식에 B Device면 ON이면 1 OFF이면 0
                            if (!(bool)value)
                                ConvertByte = ProtocolHelper.UShortToByte(0); //뒤집지 않음.
                            else
                                ConvertByte = ProtocolHelper.UShortToByte(1); //뒤집지 않음.
                        }
                        else
                        {
                            //Bit 형식에 D Device면 1워드만 픽스일것.
                            //비트를 위해 읽어온 워드 데이터(bitWord)로 현재 비트에 변경해야할 비트를 적용하여 리턴.
                            //앞뒤 바이트 변경 없이 비트 어레이 체크하면 됨.
                            byte[] temp = ProtocolHelper.LowUppByteChange(bitWord);
                            BitArray bitArr = new BitArray(bitWord);
                            bitArr.Set(BitOffset, (bool)value);
                            ConvertByte = ProtocolHelper.BitArrayToByteArray(bitArr);
                        }
                        break;

                    case eDataType.Short:
                        if (value is short)  //Convert Exception 수정
                        {
                            value = Convert.ToUInt16(value);
                        }
                        //밸류값에 따라 2바이트(1워드), 4바이트(2워드)단위로 변경해서 보내야함.
                        if (size.Equals(2))
                        {
                            value = Convert.ToUInt16(value);
                            ConvertByte = ProtocolHelper.UShortToByte((ushort)value, true); //뒤집어서 전송필요.
                            //ConvertByte = ProtocolHelper.UShortToByte((ushort)value, true); //뒤집어서 전송필요.    
                        }
                        else
                        {
                            value = Convert.ToUInt32(value);
                            ConvertByte = ProtocolHelper.UIntToByte((uint)value, size);
                        }
                        break;

                    case eDataType.String:
                        //워드단위로 앞뒤 바이트 변경 후 각 워드 문자로 변경하여 리턴
                        string strTemp = string.Empty;

                        strTemp = ((string)value).Length > size ? ((string)value).Remove(size) : (string)value;

                        //뒤에 공간은 공백으로 채움
                        //오버라이팅 방식이기에 자릿수가 달라지면 이전 쓰레기값이 있을 수 있음.
                        strTemp = strTemp.PadRight(size, '\0');
                        ASCIIEncoding asc = new ASCIIEncoding();
                        ConvertByte = asc.GetBytes(strTemp);
                        break;
                    default:
                        Console.WriteLine("WriteDataConvert NoneType");
                        return null;
                }

                return ProtocolHelper.AsciiWriteByteChange(ConvertByte);
            }
            catch (Exception ex)
            {
                _ = ex;
                return null;
            }
        }
        /// <summary>
        /// ReadDataConvert
        /// Byte배열 형식의 ReadData를 Object 형식으로 변환
        /// </summary>
        /// <param name="pItem">아이템 클래스</param>
        /// <param name="value">ReadValue</param>
        /// <returns></returns>
        public override object ReadDataConvert(PLCDataItem pItem, byte[] value)
        {
            eDataType dataType = pItem.DataType;
            int BitOffset = pItem.BitOffset;
            object ConvertValue = null;

            try
            {
                byte[] convertBytes = ProtocolHelper.AsciiReadByteChange(value);

                switch (dataType)
                {
                    case eDataType.Bool:
                        if (pItem.DeviceType.Equals(eDevice.B))
                        {
                            //Bit 형식에 B Device면 1이면 ON 0이면 OFF
                            if (value[0].Equals(0))
                                ConvertValue = false;
                            else
                                ConvertValue = true;
                        }
                        else
                        {
                            //Ascii Type은 4바이트가 1워드가 되는 구조이다.
                            //PLC의 앞 바이트가 먼저 들어오기에 이를 헥사 스트링으로 변환을 해주기 위해서는 뒤집어 줘야한다.


                            //Bit 형식에 D Device면 1워드만 픽스일것.
                            //앞뒤 바이트 변경 없이 비트 어레이에서 비트 옵셋부분 체크하면 됨.
                            BitArray bitArr = new BitArray(convertBytes);
                            ConvertValue = bitArr[BitOffset];
                        }
                        break;

                    case eDataType.Short:
                        //워드단위로 앞뒤 바이트 변경 없이 숫자로 변경하여 리턴
                        ConvertValue = BitConverter.ToInt16(convertBytes, 0);
                        break;

                    case eDataType.String:
                        //워드단위로 앞뒤 바이트 변경 없이 각 워드 문자로 변경하여 리턴
                        ConvertValue = Encoding.Default.GetString(convertBytes);
                        break;
                    case eDataType.Raw:
                        return convertBytes;
                    default:
                        Console.WriteLine("ReadDataConvert NoneType");
                        break;
                }
            }
            catch (Exception ex)
            {
                _ = ex;
                return null;
            }
            return ConvertValue;
        }
        #endregion
    }
}
