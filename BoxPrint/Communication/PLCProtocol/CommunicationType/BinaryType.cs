using PLCProtocol.Base;
using PLCProtocol.DataClass;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PLCProtocol.CommunicationType
{
    public class BinaryType : CommunicationTypeBase
    {
        private ePLCSeries _PLCSeries;
        protected override ePLCSeries PLCSeries => _PLCSeries;

        public BinaryType(ePLCSeries series)
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
            List<byte> lists = new List<byte>();
            lists.Add(0x00);
            lists.Add(0xFF);
            byte[] bytes = ProtocolHelper.UShortToByte(0x03FF, true);   //뒤집어서 전송필요.
            lists.AddRange(bytes);
            lists.Add(0x00);
            return lists.ToArray();
        }
        /// <summary>
        /// RequestDataLength
        /// 모니터링 타이머 + 리퀘스트 데이터의 토탈 사이즈 (단위 - 바이트)
        /// </summary>
        /// <param name="reqData">리퀘스트 데이터</param>
        /// <returns>RequestDataLength</returns>
        protected override byte[] RequestDataLength(byte[] reqData, byte[] monitoring)
        {
            return ProtocolHelper.UShortToByte((ushort)(reqData.Length + monitoring.Length), true);   //뒤집어서 전송필요.
        }
        /// <summary>
        /// MonitoringTimer
        /// 응답까지의 시간을 나타냄 0x01당 250ms
        /// 1초 픽스(0x04)로 고정시켜놓음
        /// </summary>
        /// <returns>MonitoringTimer</returns>
        protected override byte[] MonitoringTimer()
        {
            return ProtocolHelper.UShortToByte(0x0004, true);   //뒤집어서 전송필요.
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
                return ProtocolHelper.UShortToByte(0x0401, true);   //뒤집어서 전송필요.
            }
            else
            {
                return ProtocolHelper.UShortToByte(0x1401, true);   //뒤집어서 전송필요.
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
            //Q시리즈 워드 : 0000 / 비트 : 0100
            //R시리즈 워드 : 0200 / 비트 : 0300
            switch (device)
            {
                //B만 비트고 W와 D는 워드로 리드 / 라이트 한다.
                case eDevice.B:
                    if (PLCSeries.Equals(ePLCSeries.Q))
                        bytes = ProtocolHelper.UShortToByte(0x0001, true);   //뒤집어서 전송필요.
                    else
                        bytes = ProtocolHelper.UShortToByte(0x0003, true);   //뒤집어서 전송필요.
                    break;
                case eDevice.W:
                case eDevice.D:
                    if (PLCSeries.Equals(ePLCSeries.Q))
                        bytes = ProtocolHelper.UShortToByte(0x0000, true);   //뒤집어서 전송필요.
                    else
                        bytes = ProtocolHelper.UShortToByte(0x0002, true);   //뒤집어서 전송필요.
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
            lists.AddRange(HeadDeviceNumber(pItem.DeviceType, pItem.ItemPLCAddress));
            lists.AddRange(HeadDeviceCode(pItem.DeviceType));
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
            byte[] bytes = null;
            //Q타입은 1바이트 생성, 구분 값
            //R타입은 2바이트 생성, 구분 값 + 좌측 00 채움
            switch (device)
            {
                case eDevice.B:
                    if (PLCSeries.Equals(ePLCSeries.Q))
                        bytes = new byte[1] { 0xA0 };
                    else
                        bytes = ProtocolHelper.UShortToByte(0x00A0, true);   //뒤집어서 전송필요?
                    break;
                case eDevice.W:
                    if (PLCSeries.Equals(ePLCSeries.Q))
                        bytes = new byte[1] { 0xB4 };
                    else
                        bytes = ProtocolHelper.UShortToByte(0x00B4, true);   //뒤집어서 전송필요?
                    break;
                case eDevice.D:
                    if (PLCSeries.Equals(ePLCSeries.Q))
                        bytes = new byte[1] { 0xA8 };
                    else
                        bytes = ProtocolHelper.UShortToByte(0x00A8, true);   //뒤집어서 전송필요?
                    break;
                default:
                    break;
            }

            return bytes;
        }
        /// <summary>
        /// HeadDeviceNumber
        /// 주소값
        /// </summary>
        /// <param name="iaddress">주소값</param>
        /// <returns>HeadDeviceNumber</returns>
        protected override byte[] HeadDeviceNumber(eDevice device, int iaddress)
        {
            //Q타입은 3바이트, R타입은 4바이트 생성
            //생성된 바이트를 완전히 뒤집어 준다.
            //어드레스 1234 -> Q : 001234, R : 00001234 -> Q : 341200, R : 34120000
            //어드레스를 뒤집어주고 비어있다면 우측에 00을 채움.
            if (PLCSeries.Equals(ePLCSeries.Q))
            {
                return ProtocolHelper.UIntToByte((uint)iaddress, 3, true);
            }
            else
                return ProtocolHelper.UIntToByte((uint)iaddress, 4, true);
        }
        /// <summary>
        /// DataDevicePoints
        /// Read / Write Data Size
        /// </summary>
        /// <param name="datasize">사이즈</param>
        /// <returns>사이즈</returns>
        protected override byte[] DataDevicePoints(int datasize)
        {
            //1워드 고정
            //글자수가 아닌 워드 사이즈를 기재해야한다.
            //string형인경우는 1워드에 2글자가 기재되기에 /2해줘야한다.
            return ProtocolHelper.UShortToByte((ushort)datasize, true);     //뒤집어서 전송필요
        }
        #endregion

        #region MC Protocol Res
        //protected override bool CheckResponseSubHeader(byte[] recv, out byte[] other) 
        //{
        //    ushort responseSubHeader = 0xD000;                  //리스폰스 D000
        //    int iStart = 0, iCount = 2;                         //리스폰스 서브헤서 1워드(2바이트)
        //    List<byte> lists = recv.ToList();                   //배열 요소 삭제를 위해 리스트 변환
        //    List<byte> temp = lists.GetRange(iStart, iCount);   //삭제할 요소 가져옴
        //    lists.RemoveRange(iStart, iCount);                  //가져온 요소 삭제
        //    other = lists.ToArray();                            //삭제되고 남은 요소들 배열로 재변환

        //    if (BitConverter.ToUInt16(ProtocolHelper.LowUppByteChange(temp.ToArray()), 0).Equals(responseSubHeader))
        //    {
        //        return true;
        //    }

        //    return false;
        //}
        protected override bool CheckResponseAccessRoute(byte[] recv, out byte[] other)
        {
            byte[] responseByte = AccessRoute();                //리스폰스는 리퀘스트랑 동일해야함.
            int iStart = 0, iCount = 5;                         //리스폰스 엑세스루트는 리퀘스트랑 동일한 2.5워드(5바이트)
            List<byte> lists = recv.ToList();                   //배열 요소 삭제를 위해 리스트 변환
            List<byte> temp = lists.GetRange(iStart, iCount);   //삭제할 요소 가져옴
            lists.RemoveRange(iStart, iCount);                  //가져온 요소 삭제
            other = lists.ToArray();                            //삭제되고 남은 요소들 배열로 재변환

            if (temp.ToArray().SequenceEqual(responseByte))
            {
                return true;
            }

            return false;
        }
        protected override int CheckResponseDataLength(byte[] recv, out byte[] other)
        {
            int responseLength = -1;                            //렝쓰는 PLC에서 받은 리스폰스 정보에 있음.
            int iStart = 0, iCount = 2;                         //리스폰스 렝쓰는 1워드(2바이트)
            List<byte> lists = recv.ToList();                   //배열 요소 삭제를 위해 리스트 변환
            List<byte> temp = lists.GetRange(iStart, iCount);   //삭제할 요소 가져옴
            lists.RemoveRange(iStart, iCount);                  //가져온 요소 삭제

            responseLength = BitConverter.ToInt16(temp.ToArray(), 0);
            if (lists.Count > responseLength)
            {
                lists.RemoveRange(responseLength, lists.Count - responseLength);
                other = lists.ToArray();                        //삭제되고 남은 요소들 배열로 재변환
            }
            else
                other = lists.ToArray();                        //삭제되고 남은 요소들 배열로 재변환

            return responseLength;
        }
        protected override bool CheckResponseEndCode(byte[] recv, out byte[] other)
        {
            int responseEndCode;                                //에러코드는 PLC에서 받은 리스폰스 정보에 있음.
            int iStart = 0, iCount = 2;                         //리스폰스 엔드코드는 1워드(2바이트)
            List<byte> lists = recv.ToList();                   //배열 요소 삭제를 위해 리스트 변환
            List<byte> temp = lists.GetRange(iStart, iCount);   //삭제할 요소 가져옴
            lists.RemoveRange(iStart, iCount);                  //가져온 요소 삭제
            other = lists.ToArray();                            //삭제되고 남은 요소들 배열로 재변환

            //앞뒤 변환해서 나오는 결과가 에러코드
            responseEndCode = BitConverter.ToUInt16(ProtocolHelper.LowUppByteChange(temp.ToArray()), 0);

            if (responseEndCode.Equals(0x0000))
                return true;
            else
                return false;
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
            //사이즈의 단위는 워드 단위로 설정을 한다.
            //숫자형의 경우는 1워드 ushort, 2워드 uint
            //문자형의 경우는 1워드당 2글자씩 이다.
            eDataType dataType = pItem.DataType;
            int BitOffset = pItem.BitOffset;
            int size = pItem.Size;
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
                                ConvertByte = ProtocolHelper.UShortToByte(0, true); //뒤집어서
                            else
                                ConvertByte = ProtocolHelper.UShortToByte(1, true); //뒤집어서
                        }
                        else
                        {
                            //Bit 형식에 D Device면 1워드만 픽스일것.
                            //비트를 위해 읽어온 워드 데이터(bitWord)로 현재 비트에 변경해야할 비트를 적용하여 리턴.
                            //앞뒤 바이트 변경 후 피트 어레이 체크하면 됨.

                            //PLC 리스폰스 확인시 받은 데이터는 뒤집지 않아야 동일한 결과가 나옴.
                            //인티저 형으로 컨버터시 뒤집지 않아야 PLC모니터링과 동일한 결과.
                            //일단 나중에 PLC와 확인하기 전까지는 뒤집지 않고 체크를 한다.
                            //byte[] temp = ProtocolHelper.LowUppByteChange(bitWord);
                            byte[] temp = bitWord;
                            BitArray bitArr = new BitArray(temp);
                            bitArr.Set(BitOffset, (bool)value);

                            //PLC 모니터링에서 확인시 뒤집어서 보내면 모니터링에서도 뒤집혀져 보인다.
                            //일단 나중에 PLC와 확인하기 전까지는 뒤집지않고 프로토콜 전송한다.
                            //ConvertByte = ProtocolHelper.LowUppByteChange(ProtocolHelper.BitArrayToByteArray(bitArr));
                            ConvertByte = ProtocolHelper.BitArrayToByteArray(bitArr);
                        }
                        break;

                    case eDataType.Short:
                        //밸류값에 따라 2바이트(1워드), 4바이트(2워드)단위로 변경해서 보내야함.
                        if (size.Equals(1))
                            ConvertByte = ProtocolHelper.UShortToByte((ushort)value, true);
                        else
                            ConvertByte = ProtocolHelper.UIntToByte((uint)value, size, true);
                        break;

                    case eDataType.String:
                        //워드단위로 앞뒤 바이트 변경 후 각 워드 문자로 변경하여 리턴
                        string strTemp = string.Empty;

                        //입력값의 길이는 사이즈 *2보다 길면 안됨.
                        if (((string)value).Length > size * 2)
                        {
                            strTemp = ((string)value).Remove(size * 2);
                        }
                        else
                            strTemp = (string)value;

                        //뒤에 공간은 공백으로 채움
                        //오버라이팅 방식이기에 자릿수가 달라지면 이전 쓰레기값이 있을 수 있음.
                        strTemp = strTemp.PadRight(size * 2, '\0');
                        ASCIIEncoding asc = new ASCIIEncoding();
                        byte[] bytTemp = asc.GetBytes(strTemp);
                        //워드 별로 앞뒤 바이트를 뒤집어서 보내야한다.
                        ConvertByte = ProtocolHelper.LowUppByteChange(bytTemp);
                        break;
                    default:
                        Console.WriteLine("WriteDataConvert NoneType");
                        break;
                }
            }
            catch (Exception)
            {

            }
            return ConvertByte;
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
            byte[] ConvertByte;

            try
            {
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
                            //Bit 형식에 D Device면 1워드만 픽스일것.
                            //앞뒤 바이트 변경 후 비트 어레이에서 비트 옵셋부분 체크하면 됨.

                            //PLC 리스폰스 확인시 받은 데이터는 뒤집지 않아야 동일한 결과가 나옴.
                            //인티저 형으로 컨버터시 뒤집지 않아야 PLC모니터링과 동일한 결과.
                            //일단 나중에 PLC과 확인하기 뒤집지 않고 체크를 한다.
                            //ConvertByte = ProtocolHelper.LowUppByteChange(value);
                            ConvertByte = value;
                            BitArray bitArr = new BitArray(ConvertByte);
                            ConvertValue = bitArr[BitOffset];
                        }
                        break;

                    case eDataType.Short:
                        //워드단위로 앞뒤 바이트 변경 후 워드 붙여서 숫자로 변경하여 리턴
                        ConvertByte = ProtocolHelper.LowUppByteChange(value);
                        ConvertValue = BitConverter.ToInt32(ConvertByte, 0);
                        break;

                    case eDataType.String:
                        //워드단위로 앞뒤 바이트 변경 후 각 워드 문자로 변경하여 리턴
                        ConvertByte = ProtocolHelper.LowUppByteChange(value);
                        ConvertValue = Encoding.Default.GetString(ConvertByte);
                        break;
                    default:
                        Console.WriteLine("ReadDataConvert NoneType");
                        break;
                }
            }
            catch (Exception)
            {

            }
            return ConvertValue;
        }
        #endregion
    }
}
