using PLCProtocol.DataClass;
using BoxPrint;      //230911 HHJ enum Value string으로 변경
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PLCProtocol.Base
{
    /// <summary>
    /// 생성 필수.
    /// </summary>
    public class ProtocolHelper
    {
        /// <summary>
        /// 비트 배열을 바이트 배열로 변환
        /// </summary>
        /// <param name="bits">변환할 비트 배열</param>
        /// <returns>변환된 바이트 배열</returns>
        public static byte[] BitArrayToByteArray(BitArray bits)
        {
            const int BITSPERBYTE = 8;
            int iBytesize = 0;
            byte[] bytResult = null;

            // 기본 바이트 사이즈를 구한다
            iBytesize = bits.Length / BITSPERBYTE;

            // 남는 비트가 있으면 그것도 포함
            if (bits.Length % BITSPERBYTE > 0)
            {
                iBytesize++;
            }

            bytResult = new byte[iBytesize];

            byte value = 0;
            byte significance = 1;

            int bytepos = 0;
            int bitpos = 0;

            // 위치를 하나씩 이동시키며 byte 값을 계산한다
            while (bitpos < bits.Length)
            {
                if (true == bits[bitpos])
                {
                    value += significance;
                }
                bitpos++;
                if (0 == bitpos % BITSPERBYTE)
                {
                    bytResult[bytepos] = value;
                    bytepos++;
                    value = 0;
                    significance = 1;
                }
                else
                {
                    significance *= 2;
                }
            }

            return bytResult;
        }
        //PLCtoPC, PCtoPLC PLCDataItem 항목
        //private static ConcurrentDictionary<string, PLCDataItem> PLCtoPC { get; set; }
        //private static ConcurrentDictionary<string, PLCDataItem> PCtoPLC { get; set; }

        //Key - 모듈 타입, Value - 
        private static ConcurrentDictionary<string, List<PLCDataItem>> PLCtoPC { get; set; }
        private static ConcurrentDictionary<string, List<PLCDataItem>> PCtoPLC { get; set; }

        public ProtocolHelper(List<PLCDataItem> plcitems)
        {
            PLCtoPC = new ConcurrentDictionary<string, List<PLCDataItem>>();
            PCtoPLC = new ConcurrentDictionary<string, List<PLCDataItem>>();

            //1. plcitems에 들어있는 모듈 타입을 리스트화 시켜서 중복되지않는 모듈타입이 몇개인지 확인
            List<string> moduletypeList = new List<string>();
            string moduletype = string.Empty;
            foreach (PLCDataItem pitem in plcitems)
            {
                if (!moduletype.Equals(pitem.ModuleType))
                {
                    moduletype = pitem.ModuleType;
                    moduletypeList.Add(pitem.ModuleType);
                }
            }

            //2. 모듈타입별로 구분하여 PLCtoPC, PCtoPLC를 생성
            foreach (string mtype in moduletypeList)
            {
                List<PLCDataItem> plctopctemp = new List<PLCDataItem>();
                plctopctemp = plcitems.Where(r => r.Area.Equals(eAreaType.PLCtoPC) && r.ModuleType.Equals(mtype) && !r.ItemName.Contains("Alive")).ToList();
                PLCtoPC.TryAdd(mtype, plctopctemp);

                List<PLCDataItem> pctoplctemp = new List<PLCDataItem>();
                pctoplctemp = plcitems.Where(r => r.Area.Equals(eAreaType.PCtoPLC) && r.ModuleType.Equals(mtype) && !r.ItemName.Contains("Alive")).ToList();
                PCtoPLC.TryAdd(mtype, pctoplctemp);
            }
        }

        //220628 HHJ SCS 개선     //- PLCDataItems 개선
        public static ConcurrentDictionary<string, PLCDataItem> GetPLCItem(eAreaType area, string moduletyle, short plcnum, ushort baseaddress)
        {
            ConcurrentDictionary<string, PLCDataItem> tmp = new ConcurrentDictionary<string, PLCDataItem>();
            List<PLCDataItem> TempList = new List<PLCDataItem>();

            switch (area)
            {
                case eAreaType.PLCtoPC:
                    if (!PLCtoPC.ContainsKey(moduletyle))
                        return null;

                    TempList = PLCtoPC[moduletyle];
                    break;

                case eAreaType.PCtoPLC:
                    if (!PCtoPLC.ContainsKey(moduletyle))
                        return null;

                    TempList = PCtoPLC[moduletyle];
                    break;

                default:
                    break;
            }

            foreach (var v in TempList)
            {
                PLCDataItem pitem = v.DeepCopy();
                pitem.PLCNum = plcnum;
                pitem.ItemPLCAddress = pitem.AddressOffset + baseaddress;
                tmp.TryAdd(pitem.ItemName, pitem);
            }

            return tmp;
        }

        public static byte[] UShortToByte(ushort value, bool bReverse = false)
        {
            byte[] bytes = BitConverter.GetBytes(value);

            #region Comment
            //GetBytes는 하위바이트가 0번지이다. 상위바이트가 1번지 이다
            //PLC에 기재해야하는것은 상위워드가 앞에있는 경우, 하위 워드가 앞에 있는 경우가 다 있음.
            //데이터를 구할때 변수의 혼돈을 피하기 위해 기본 리턴형은 상위바이트가 0번지에 위치하다록 하고,
            //리버스를 해서 하위 바이트가 0번지에 위치하도록 한다.
            //PLC에 전달되는것은 0번지가 상위 1번지가 하위 바이트로 전달이 됨.
            //ex) 0x1234 => GetByte 진행시 0번지 52, 1번지 18의 결과를 얻음.
            //              하지만 비트의 순서를 보았을때, 18, 52의 순으로 비트는 올라가있음.
            //              0x1234 그대로 전달이 필요하면, 0번지가 18, 1번지가 52가 되어야함.
            //              단 0x1234를 0x3412(바이트 변환)해서 보내야하는경우는 GetBytes된 데이터를 그대로 보내면된다.
            //하여 bReverse가 true가 되면 리버스하지않고, bReverse가 false인 경우에만 뒤집어서 리턴한다.
            #endregion
            if (!bReverse)
                Array.Reverse(bytes);

            return bytes;
        }

        //Address는 1워드 이상(3바이트, 4바이트 Case존재)의 데이터가 존재하여 따로 분류함.
        public static byte[] UIntToByte(uint value, int ibyteSize, bool bReverse = false)
        {
            //바이트 사이즈에 최대 숫자 확인 및 벨류값 범위내에 있는지 확인, 최대 4바이트 까지만 체크
            if (ibyteSize > 4)
                return null;

            string strChecker = string.Empty;
            //1바이트당 최대의 숫자는 FF. 바이트 만큼 FF를 쌓아놓음.
            for (int i = 0; i < ibyteSize; i++)
            {
                strChecker += "FF";
            }
            //바이트만큼 쌓여있는 FF를 unit형으로 변환하여 인자로받은 값과 비교
            if (uint.TryParse(strChecker, System.Globalization.NumberStyles.HexNumber, null, out uint uMax))
            {
                //인자로받은 값이 해당 바이트에서 처리불가능한 숫자라면 null리턴
                if (value > uMax)
                    return null;
            }

            byte[] bytes = BitConverter.GetBytes(value);

            ushort u1 = BitConverter.ToUInt16(bytes, 0);
            ushort u2 = BitConverter.ToUInt16(bytes, 2);

            byte[] byte1 = UShortToByte(u1, bReverse);
            byte[] byte2 = UShortToByte(u2, bReverse);

            List<byte> lists = new List<byte>();
            lists.AddRange(byte1);
            lists.AddRange(byte2);

            //리스트의 갯수가 바이트의 갯수보다 많다면
            if (lists.Count > ibyteSize)
            {
                //리스트 마지막 인자를 삭제해준다.
                lists.RemoveAt(lists.Count - 1);
            }

            return lists.ToArray();
        }

        //바이너리 전용
        public static byte[] LowUppByteChange(byte[] bytes)
        {
            List<byte> lists = new List<byte>();

            //리딩은 워드 단위로하니 무조건 2의 배수로 맞춰짐.
            for (int i = 0; i < bytes.Length; i = i + 2)
            {
                //신규 생성하는 0번지에는 변경해야할 1번지, 1번지에는 0번지
                byte[] _byte = new byte[2] { bytes[i + 1], bytes[i] };
                lists.AddRange(_byte);
            }

            return lists.ToArray();
        }
        //아스키 전용
        public static byte[] AsciiReadByteChange(byte[] bytes)
        {
            //아스키는 4바이트 단위로 1워드가 된다.
            //0xABCD인경우 -> D C B A가 각 1바이트씩 실려서 들어옴.
            //4바이트 단위로 뒤집어주고 이를 헥사스트링으로 변환 후 ushort로 변환하여 바이트 재변환 해야한다.
            List<byte> lists = new List<byte>();
            List<byte> read = bytes.ToList();

            while (read.Count > 0)
            {
                List<byte> templist = read.GetRange(0, 4);
                //templist.Reverse();
                read.RemoveRange(0, 4);
                //순서를 뒤집어서 헥사 스트링으로 변환시켜준다.
                string strhex = Encoding.Default.GetString(templist.ToArray());
                //1워드 단위로 변환이 되기에 UInt로 변환을 시켜준다.

                //ushort convertShort = Convert.ToUInt16(strhex);     
                ushort convertShort;
                if (ushort.TryParse(strhex, System.Globalization.NumberStyles.HexNumber, null, out convertShort))
                {

                }
                //변환된 UInt를 다시 바이트배열로 재변환 시켜준다.
                byte[] changeByte = BitConverter.GetBytes(convertShort);
                //재변환된 바이트배열을 리스트에 넣어준다.
                lists.AddRange(changeByte);
            }

            //최종 리스트에 있는 바이트 리스트를 배열로 리턴해준다.
            return lists.ToArray();
        }
        public static byte[] AsciiWriteByteChange(byte[] bytes)
        {
            //아스키는 4바이트 단위로 1워드가 된다.
            //0xABCD인경우 -> D C B A를 각 1바이트에 넣어서 보내야함.
            //해당 바이트를 2바이트 단위로 ushort로 변환 후 이를 헥사 스트링으로 변환하여 헥사스트링 코드를 한바이트씩 넣어서 리턴해준다.
            List<byte> lists = new List<byte>();
            List<byte> read = bytes.ToList();

            while (read.Count > 0)
            {
                List<byte> templist = read.GetRange(0, 2);
                read.RemoveRange(0, 2);

                //1워드 단위로 변환이 되기에 UInt로 변환을 시켜준다.
                ushort convertShort = BitConverter.ToUInt16(templist.ToArray(), 0);

                //변환된 Uint를 헥사 스트링으로 바꿔주고 이 헥사 스트링을 바이트 배열로 받아온다..
                lists.AddRange(Encoding.Default.GetBytes(IntToHexString(convertShort)));
            }

            //최종 리스트에 있는 바이트 리스트를 배열로 리턴해준다.
            return lists.ToArray();
        }

        public static string IntToHexString(int value)
        {
            return value.ToString("X4");
        }

        //230103 HHJ SCS 개선
        public static object ParseIOData(PLCDataItem item, byte[] ItemData)
        {
            object readdata = null;
            try
            {
                switch (item.DataType)
                {
                    case eDataType.Bool:
                        if (item.BitOffset < 8)
                        {
                            byte MValue = ItemData[0];
                            MValue = (byte)(MValue >> item.BitOffset);
                            readdata = MValue % 2 == 1;
                            readdata = readdata.ToString().Equals("True") ? 1 : 0;
                        }
                        else //bit 8이상
                        {
                            byte MValue = ItemData[1];
                            MValue = (byte)(MValue >> (item.BitOffset - 8));
                            readdata = MValue % 2 == 1;
                            readdata = readdata.ToString().Equals("True") ? 1 : 0;
                        }
                        break;

                    case eDataType.Short:
                        byte L_Value = ItemData[0];     //하위비트
                        byte U_Value = ItemData[1];     //상위비트
                        if(item.BitOffset == 8) //비트 OffSet 8이면 상위 바이트만 가져온다.
                        {
                            readdata = (short)U_Value;
                        }
                        else
                        {
                            readdata = (short)((U_Value << 8) + L_Value);
                        }
                        
                        break;
                    case eDataType.Int32: //추후 byte order  체크 필요
                        byte Value_0 = ItemData[0];
                        byte Value_1 = ItemData[1];
                        byte Value_2 = ItemData[2];
                        byte Value_3 = ItemData[3];
                        readdata = (Value_3 << 24) + (Value_2 << 16) + (Value_1 << 8) + Value_0;
                        break;

                    case eDataType.String:
                        readdata = Encoding.Default.GetString(ItemData, 0, ItemData.Length);
                        readdata = readdata.ToString().Replace("\0", "");
                        break;

                    default:
                        break;

                }
            }
            catch (Exception ex)
            {
                _ = ex;
                readdata = null;
            }

            return readdata;
        }

        //230911 HHJ enum Value string으로 변경
        public static object ReadValueConverter(eDataChangeUnitType unitType, PLCDataItem item, object readdata)
        {
            object ret = default;
            if (unitType.Equals(eDataChangeUnitType.ePort))
            {
                if (item.ItemName.Contains("TrayType"))
                {
                    readdata.GetEnumStringByEnumValue<eTrayType>(out ret);
                }
                else if (item.ItemName.Contains("Polarity"))
                {
                    readdata.GetEnumStringByEnumValue<ePolarity>(out ret);
                }
                else if (item.ItemName.Contains("WinderDirection"))
                {
                    readdata.GetEnumStringByEnumValue<eWinderDirection>(out ret);
                }
                else if (item.ItemName.Contains("ProductEmpty"))
                {
                    readdata.GetEnumStringByEnumValue<eProductEmpty>(out ret);
                }
                else if (item.ItemName.Contains("InnerTrayType"))
                {
                    readdata.GetEnumStringByEnumValue<eInnerTrayType>(out ret);
                }
                else if (item.ItemName.Contains("UnCoatedPart"))
                {
                    readdata.GetEnumStringByEnumValue<eUnCoatedPart>(out ret);
                }
                else if (item.ItemName.Contains("ProductEnd"))
                {
                    readdata.GetEnumStringByEnumValue<eProductEnd>(out ret);
                }
                else if (item.ItemName.Contains("PalletSize"))
                {
                    readdata.GetEnumStringByEnumValue<ePalletSize>(out ret);
                }
                else if (item.ItemName.Contains("CoreType"))
                {
                    readdata.GetEnumStringByEnumValue<eCoreType>(out ret);
                }
                else if (item.ItemName.Contains("SCSMode") || item.ItemName.Contains("PortType"))
                {
                    readdata.GetEnumStringByEnumValue<ePortInOutType>(out ret);
                }
                else
                {
                    ret = readdata;
                }
            }
            else if (unitType.Equals(eDataChangeUnitType.eCrane))
            {
                if (item.ItemName.Contains("OnlineMode"))
                {
                    readdata.GetEnumStringByEnumValue<eCraneOnlineMode>(out ret);
                }
                else if (item.ItemName.Contains("PalletSize"))
                {
                    readdata.GetEnumStringByEnumValue<ePalletSize>(out ret);
                }
                else if (item.ItemName.Contains("SCMODE"))
                {
                    readdata.GetEnumStringByEnumValue<eCraneSCMode>(out ret);
                }
                else if (item.ItemName.Contains("CraneActionState"))
                {
                    readdata.GetEnumStringByEnumValue<eCraneSCState>(out ret);
                }
                else if (item.ItemName.Contains("ActiveState"))
                {
                    readdata.GetEnumStringByEnumValue<eCraneActiveState>(out ret);
                }
                else if (item.ItemName.Contains("CraneJobState"))
                {
                    readdata.GetEnumStringByEnumValue<eCraneJobState>(out ret);
                }
                else if (item.ItemName.Contains("CraneCommandState") || item.ItemName.Contains("CraneCommand"))
                {
                    readdata.GetEnumStringByEnumValue<eCraneCommand>(out ret);
                }
                else
                {
                    ret = readdata;
                }
            }
            else if(unitType.Equals(eDataChangeUnitType.eBooth))
            {
                ret = readdata;
            }

            return ret;
        }
    }
}
