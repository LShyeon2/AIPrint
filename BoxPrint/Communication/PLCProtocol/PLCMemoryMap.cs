using PLCProtocol;
using PLCProtocol.DataClass;
using BoxPrint.Database;
using BoxPrint.Log;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BoxPrint.Communication.PLCProtocol
{
    /// <summary>
    /// PLC Memory Map 구조 변경
    /// 0 ~ 1999 [SCS/MCP 상호 공용구역 (1000 Word)]
    /// 2000 ~ 7999 [SCS Crane IF 구역 (3000 Word)
    /// 10000 ~ 19999 [0 번 PLC 컨베이어 (5000 Word)]  - Crane 과 같은 PLC Cpu (최대 100개 컨베이어 유닛 대응)
    /// 20000 ~ 29999 [1 번 PLC 컨베이어 (5000 Word)]
    /// 30000 ~ 39999 [2 번 PLC 컨베이어 (5000 Word)]
    /// 40000 ~ 49999 [3 번 PLC 컨베이어 (5000 Word)]
    /// 50000 ~ 59999 [4 번 PLC 컨베이어 (5000 Word)]
    /// 60000 ~ 69999 [5 번 PLC 컨베이어 (5000 Word)] 필요시 더 영역 늘려야 함
    /// </summary>
    public class PLCMemoryMap
    {
        public event UnitDataChanged OnUnitDataChanged;     //230103 HHJ SCS 개선

        public delegate void MapIOUpdated(int Offset,int UpdatedLength);

        public event MapIOUpdated OnIOUpdated;


        private byte[] PLCReadMemoryBuffer; //PLC Memory 값을 저장하므로 데이터들은 BigEndian 형식으로 저장된다.
        private Dictionary<string, Dictionary<eAreaType, byte[]>> PLCDataByUnit;        //230103 HHJ SCS 개선

        private int PLCMapSize = 80000; //70000 + Reserve 

        public PLCMemoryMap()
        {
            PLCReadMemoryBuffer = new byte[PLCMapSize];
            PLCDataByUnit = new Dictionary<string, Dictionary<eAreaType, byte[]>>();       //230103 HHJ SCS 개선
        }
        public PLCMemoryMap(int MapSize)
        {
            PLCMapSize = MapSize;
            PLCReadMemoryBuffer = new byte[PLCMapSize];
            PLCDataByUnit = new Dictionary<string, Dictionary<eAreaType, byte[]>>();       //230103 HHJ SCS 개선
        }
        public int GetMamoryMapAddress(PLCDataItem PItem)
        {
            if (PItem == null)
            {
                return -1;
            }
            int byteAddress;
            if (PItem.PLCNum == 0) //Crane 또는 공통영역
            {
                byteAddress = PItem.ItemPLCAddress % 10000; //천단위로 절삭
                byteAddress = byteAddress * 2; //PLC Word 단위이므로 2를 곱한다.
                if (PItem.ModuleType == "CV")
                {
                    byteAddress = byteAddress + 10000; //10000Byte 단위 영역 분리
                    return byteAddress;
                }
                else
                {
                    return byteAddress;
                }
            }
            else //PLC Number 1번 이상 모두 CV 로 가정함
            {
                byteAddress = PItem.ItemPLCAddress % 10000; //천단위로 절삭
                byteAddress = byteAddress * 2; //PLC Word 단위이므로 2를 곱한다.
                byteAddress = byteAddress + (PItem.PLCNum + 1) * 10000; //10000Byte 단위 영역 분리
                return byteAddress;
            }
        }
        public void Clear()
        {
            Array.Clear(PLCReadMemoryBuffer, 0, PLCReadMemoryBuffer.Length);
        }
        public byte[] GetBuffer(int PLCNum = 0)
        {
            return PLCReadMemoryBuffer;
        }
        public byte[] ReadRawMemoryBuffer(PLCDataItem PItem)
        {

            int ByteOffset = GetMamoryMapAddress(PItem);
            int Length = PItem.Size * 2;
            if (ByteOffset + Length > PLCMapSize)
            {
                Length = PLCMapSize - ByteOffset;
            }
            if (Length == 0)
            {
                return new byte[1];
            }
            byte[] TempArray = new byte[Length];
            Array.Copy(PLCReadMemoryBuffer, ByteOffset, TempArray, 0, Length);
            return TempArray;
        }
        public byte[] ReadRawMemoryBuffer(int ByteOffset, int Length)
        {
            if (ByteOffset + Length > PLCMapSize)
            {
                Length = PLCMapSize - ByteOffset;
            }
            if (Length == 0)
            {
                return new byte[1];
            }
            byte[] TempArray = new byte[Length];
            Array.Copy(PLCReadMemoryBuffer, ByteOffset, TempArray, 0, Length);
            return TempArray;
        }

        public bool ReadBit(PLCDataItem PItem)
        {
            try
            {
                int ByteOffset = GetMamoryMapAddress(PItem);

                if (PItem.BitOffset < 8)
                {
                    byte MValue = PLCReadMemoryBuffer[ByteOffset];
                    MValue = (byte)(MValue >> PItem.BitOffset);
                    return MValue % 2 == 1;
                }
                else //bit 8이상
                {
                    byte MValue = PLCReadMemoryBuffer[ByteOffset + 1];
                    MValue = (byte)(MValue >> (PItem.BitOffset - 8));
                    return MValue % 2 == 1;
                }
            }
            catch
            {
                return false;
            }
        }

        public bool ReadBit(int ByteOffset, int BitOffset)
        {
            try
            {
                byte MValue = PLCReadMemoryBuffer[ByteOffset];
                MValue = (byte)(MValue >> BitOffset);
                return MValue % 2 == 1;
            }
            catch
            {
                return false;
            }
        }


        public bool ReadBit(string ModuleName, ConcurrentDictionary<string, PLCDataItem> items, string key)
        {
            try
            {
                PLCDataItem p = items[key];
                return ReadBit(p);
            }
            catch
            {
                return false;
            }
        }
        public short ReadShort(PLCDataItem PItem)
        {
            try
            {
                int ByteOffset = GetMamoryMapAddress(PItem);
                byte L_Value = PLCReadMemoryBuffer[ByteOffset];    //하위비트
                byte U_Value = PLCReadMemoryBuffer[ByteOffset + 1];//상위비트
                short sValue = (short)((U_Value << 8) + L_Value);
                return sValue;
            }
            catch
            {
                return 0;
            }
        }
        public short ReadShort(string ModuleName, ConcurrentDictionary<string, PLCDataItem> items, string key)
        {
            try
            {
                PLCDataItem p = items[key];
                return ReadShort(p);
            }
            catch
            {
                return 0;
            }
        }

        public int ReadInt32(PLCDataItem PItem)
        {
            try
            {
                int ByteOffset = GetMamoryMapAddress(PItem);
                byte Value_0 = PLCReadMemoryBuffer[ByteOffset];
                byte Value_1 = PLCReadMemoryBuffer[ByteOffset + 1];
                byte Value_2 = PLCReadMemoryBuffer[ByteOffset + 2];
                byte Value_3 = PLCReadMemoryBuffer[ByteOffset + 3];
                int sValue = (Value_3 << 24) + (Value_2 << 16) + (Value_1 << 8) + Value_0;
                return sValue;
            }
            catch
            {
                return 0;
            }
        }
        public int ReadInt32(int ByteOffset)
        {
            try
            {
                byte Value_0 = PLCReadMemoryBuffer[ByteOffset];
                byte Value_1 = PLCReadMemoryBuffer[ByteOffset + 1];
                byte Value_2 = PLCReadMemoryBuffer[ByteOffset + 2];
                byte Value_3 = PLCReadMemoryBuffer[ByteOffset + 3];
                int sValue = (Value_3 << 24) + (Value_2 << 16) + (Value_1 << 8) + Value_0;
                return sValue;
            }
            catch
            {
                return 0;
            }
        }
        public int ReadInt32(string ModuleName, ConcurrentDictionary<string, PLCDataItem> items, string key)
        {
            try
            {
                PLCDataItem p = items[key];
                return ReadInt32(p);
            }
            catch
            {
                return 0;
            }
        }
        public string ReadString(PLCDataItem PItem)
        {
            try
            {
                int ByteOffset = GetMamoryMapAddress(PItem);
                string ReadValue = Encoding.Default.GetString(PLCReadMemoryBuffer, ByteOffset, PItem.Size * 2);
                ReadValue = ReadValue.Replace("\0", string.Empty);//Null character 제거
                ReadValue = ReadValue.Replace(" ", "");//공백 제거
                return ReadValue;
                //return (string)Read(ModuleName, items, key);
            }
            catch
            {
                return string.Empty;
            }
        }
        public string ReadString(int ByteOffset, int Size)
        {
            try
            {
                string ReadValue = Encoding.Default.GetString(PLCReadMemoryBuffer, ByteOffset, Size * 2);
                ReadValue = ReadValue.Replace("\0", string.Empty);//Null character 제거
                ReadValue = ReadValue.Replace(" ", "");//공백 제거
                return ReadValue;
                //return (string)Read(ModuleName, items, key);
            }
            catch
            {
                return string.Empty;
            }
        }
        public string ReadString(string ModuleName, ConcurrentDictionary<string, PLCDataItem> items, string key)
        {
            try
            {
                PLCDataItem p = items[key];
                return ReadString(p);
            }
            catch
            {
                return string.Empty;
            }
        }

        public bool Write(string ModuleName, ConcurrentDictionary<string, PLCDataItem> items, string key, object value)
        {
            if (!items.ContainsKey(key))
            {
                //LogManager.WriteConsoleLog(eLogLevel.Info, string.Format("PLC WRITE FAIL {0} {1} is not registered", ModuleName, key));
                return false;
            }

            if (items.TryGetValue(key, out PLCDataItem pItem))
            {
                if (value is string && pItem.DataType != eDataType.String)
                {
                    switch (pItem.DataType)
                    {
                        case eDataType.Bool:
                            if ((string)value == "1")
                            {
                                value = true;
                            }
                            else if ((string)value == "0")
                            {
                                value = false;
                            }
                            else
                            {
                                return false;
                            }
                            break;
                        case eDataType.Short:
                            short sValue = 0;
                            if (short.TryParse(value.ToString(), out sValue))
                            {
                                value = sValue;
                            }
                            else
                            {
                                return false;
                            }
                            break;
                    }
                }

                int ByteOffset = GetMamoryMapAddress(pItem);
                switch (pItem.DataType)
                {
                    case eDataType.Bool:
                        if (pItem.BitOffset < 8)
                        {
                            byte MValue = PLCReadMemoryBuffer[ByteOffset];
                            if ((bool)value == true)
                            {
                                MValue = (byte)(MValue | (byte)(0x01 << pItem.BitOffset));
                            }
                            else
                            {
                                MValue = (byte)(MValue & ~(byte)(0x01 << pItem.BitOffset));
                            }

                            PLCReadMemoryBuffer[ByteOffset] = MValue;
                        }
                        else //bit 8이상
                        {
                            byte MValue = PLCReadMemoryBuffer[ByteOffset + 1];
                            if ((bool)value == true)
                            {
                                MValue = (byte)(MValue | (byte)(0x01 << pItem.BitOffset - 8));
                            }
                            else
                            {
                                MValue = (byte)(MValue & ~(byte)(0x01 << pItem.BitOffset - 8));
                            }
                            PLCReadMemoryBuffer[ByteOffset + 1] = MValue;
                        }
                        break;
                    case eDataType.Short:
                        short sValue = Convert.ToInt16(value);
                        byte[] sEncode = BitConverter.GetBytes(sValue);
                        PLCReadMemoryBuffer[ByteOffset] = (byte)(sValue % 256);  //하위비트
                        PLCReadMemoryBuffer[ByteOffset + 1] = (byte)(sValue >> 8); //상위비트
                        break;
                    case eDataType.Int32:
                        int iValue = Convert.ToInt32(value);
                        PLCReadMemoryBuffer[ByteOffset] = (byte)(iValue % 256);  //0 바이트
                        PLCReadMemoryBuffer[ByteOffset + 1] = (byte)((iValue >> 8) % 256); //1 바이트
                        PLCReadMemoryBuffer[ByteOffset + 2] = (byte)((iValue >> 16) % 256); //2 바이트
                        PLCReadMemoryBuffer[ByteOffset + 3] = (byte)((iValue >> 24) % 256); //3 바이트
                        break;
                    case eDataType.String:
                        string PadedString = ((string)value).PadRight(pItem.Size * 2, '\0');//뒤에 공간은 공백으로 채워서 보내야 한다. 매 초기화가 아닌 오버라이팅 방식이기에 자릿수가 달라지면 이전 쓰레기값이 있을 수 있음.
                        byte[] Encoded = Encoding.Default.GetBytes(PadedString);
                        Array.Copy(Encoded, 0, PLCReadMemoryBuffer, ByteOffset, Encoded.Length);
                        break;
                    default:
                        break;
                }
                return true;
            }
            else
            {
                return false;
            }
        }


        //230103 HHJ SCS 개선
        public bool GetPLCUnitData(eDataChangeUnitType unitType, string unitKey)
        {
            try
            {
                Dictionary<eAreaType, byte[]> valueArea = new Dictionary<eAreaType, byte[]>();

                //없으면 안되긴한데 일단 혹시모르니 조건은 추가해줌.
                if (!PLCDataByUnit.ContainsKey(unitKey))
                    return false;

                valueArea = PLCDataByUnit[unitKey];

                //해당 키의 Area 전부를 보내줘야한다.
                foreach (var v in valueArea)
                {
                    OnUnitDataChanged?.Invoke(v.Key.Equals(eAreaType.PLCtoPC) ? eDataChangeProperty.eIO_PLCtoPC : eDataChangeProperty.eIO_PCtoPLC, unitType, unitKey, v.Value);
                }

                return true;
            }
            catch (Exception ex)
            {
                _ = ex;
                return false;
            }
        }
        //231108 HHJ SCS Playback 개선
        //public bool UpdatePLCUnitData(eDataChangeUnitType unitType, string unitKey, eAreaType areaKey, byte[] plcData, bool bClient, int IO_Offset)
        public bool UpdatePLCUnitData(eDataChangeUnitType unitType, string unitKey, eAreaType areaKey, byte[] plcData, bool bClient,int IO_Offset, bool isPlayback)
        {
            try
            {
                if (string.IsNullOrEmpty(unitKey))
                    return true;

                Dictionary<eAreaType, byte[]> valueArea = new Dictionary<eAreaType, byte[]>();
                byte[] beforePlcData = null;
                bool bEventOccur = false;

                if (PLCDataByUnit == null)
                    PLCDataByUnit = new Dictionary<string, Dictionary<eAreaType, byte[]>>();

                //unitKey 존재 여부 체크
                if (!PLCDataByUnit.ContainsKey(unitKey))
                {
                    //unitKey 없으면 해당 유닛은 처음 들어온 것
                    PLCDataByUnit.Add(unitKey, valueArea);
                }

                //유닛키 있으면 키로 벨류값 가져옴
                valueArea = PLCDataByUnit[unitKey];

                //AreaKey 존재 여부 체크
                if (!valueArea.ContainsKey(areaKey))
                {
                    //AreaKey 없으면 해당 Area는 처음 들어온 것
                    valueArea.Add(areaKey, beforePlcData);
                }

                //AreaKey 있으면 키로 벨류값 가져옴
                beforePlcData = valueArea[areaKey];

                //이전값이 null이면 신규값으로 바로 변경한다.
                if (beforePlcData is null)
                {
                    valueArea[areaKey] = plcData;
                    bEventOccur = true;
                }
                else
                {
                    //이전 값과 일치하는지 확인
                    if (!beforePlcData.SequenceEqual(plcData))
                    {
                        //일치하지 않으면 이벤트를 보내고 업데이트 한다.
                        valueArea[areaKey] = plcData;
                        bEventOccur = true;
                    }
                }

                if (bEventOccur)
                {
                    OnUnitDataChanged?.Invoke(areaKey.Equals(eAreaType.PLCtoPC) ? eDataChangeProperty.eIO_PLCtoPC : eDataChangeProperty.eIO_PCtoPLC, unitType, unitKey, plcData);

                    //231108 HHJ SCS Playback 개선
                    //if (!bClient) 
                }

                return true;
            }
            catch (Exception ex)
            {
                _ = ex;
            }

            return false;
        }
        public bool UpdatePLCUnitData(eDataChangeUnitType unitType, string unitKey, eAreaType areaKey, ConcurrentDictionary<string, PLCDataItem> dataitem, bool bClient)
        {
            try
            {
                KeyValuePair<string, PLCDataItem> firstitem = dataitem.Where(r => !r.Value.ItemName.Contains("BatchRead")).OrderBy(s => s.Value.AddressOffset).FirstOrDefault();
                KeyValuePair<string, PLCDataItem> enditem = dataitem.Where(r => !r.Value.ItemName.Contains("BatchRead")).OrderBy(s => s.Value.AddressOffset).LastOrDefault();

                //아이템이 비었다면 어찌해야할지...?
                if (firstitem.Equals(default) || enditem.Equals(default))
                {
                    return false;       //일단 false리턴;;
                }

                //처음 항목에서 시작 위치를 가져옴
                int iAddress = GetMamoryMapAddress(firstitem.Value);
                //마지막 항목과 처음항목을 이용해서 Area의 사이즈를 구한다.
                int iSize = (enditem.Value.AddressOffset + enditem.Value.Size) - firstitem.Value.AddressOffset;
                //시작 위치와 사이즈로 C/V의 PLC Data를 가져온다.
                byte[] cvPlcData = GetCVPLCData(iAddress, iSize);

                //추가 해준다.
                //231108 HHJ SCS Playback 개선
                //UpdatePLCUnitData(unitType, unitKey, areaKey, cvPlcData, bClient, iAddress);
                UpdatePLCUnitData(unitType, unitKey, areaKey, cvPlcData, bClient, iAddress, false);

                return true;
            }
            catch (Exception ex)
            {
                _ = ex;
            }

            return false;
        }
        //231106 HHJ Playback 수정    //private -> public 변경
        public byte[] GetCVPLCData(int iaddress, int isize)
        {
            byte[] tmp = null;
            try
            {
                tmp = new byte[isize * 2];
                Array.Copy(PLCReadMemoryBuffer, iaddress, tmp, 0, isize * 2);
                //PLCReadMemoryBuffer.CopyTo(tmp, iaddress * 2);
            }
            catch (Exception ex)
            {
                _ = ex;
            }
            return tmp;
        }

    }

}
