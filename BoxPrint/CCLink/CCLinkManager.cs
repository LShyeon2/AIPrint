using BoxPrint.CCLink.IODevice;
using BoxPrint.Log;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BoxPrint.CCLink
{
    public class CCLinkManager
    {
        private CCIODevice CCN_First;
        private CCIODevice CCN_Second; //2개 이상 추가될 가능성이 없으므로 컬렉션 안씀
        public static CCLinkManager CCLCurrent;
        private readonly bool bSimulMode = false;
        public bool SimulMode { get { return bSimulMode; } }

        private IOPointList _IOPointList = null;

        public CCLinkManager(string xmlFileName, bool SimulMode)
        {
            try
            {
                bSimulMode = SimulMode;
                _IOPointList = IOPointList.Deserialize(xmlFileName);
                CCN_First = new CCIODevice(0);
                CCN_Second = new CCIODevice(1);
            }
            catch (ArgumentException ae)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ae.ToString());
            }
            catch (Exception e)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, e.ToString());
            }
            CCLCurrent = this;

        }
        public bool Add_IOList(string xmlFileName)
        {
            if (String.IsNullOrEmpty(xmlFileName))
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "Add_IOList() 인자가 Null이거나 공백입니다.");
                return false;
            }
            try
            {
                var AddedIOList = IOPointList.Deserialize(xmlFileName);
                IOPoint temp = null;
                for (int i = 0; i < AddedIOList.Count; i++)
                {
                    temp = AddedIOList[i];
                    _IOPointList.Add(temp);
                }
                return true;
            }
            catch (ArgumentException ae)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ae.ToString());
                return false;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }
        }
        public bool Add_IOList(List<IOPoint> ModuleIOList)
        {
            IOPoint temp = null;
            try
            {
                for (int i = 0; i < ModuleIOList.Count; i++)
                {
                    temp = ModuleIOList[i];
                    _IOPointList.Add(temp);
                }
                return true;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }
        }

        // CCLink I/O 입출력 인터페이스 수정.
        public bool ReadIO(string ModuleID, string IoName)
        {
            try
            {
                var IO = _IOPointList.Where(r => r.ModuleID == ModuleID && r.Name == IoName).FirstOrDefault();
                CCIODevice CCN = GetCCIODevice(IO.Board); //-CCLINK 멀티보드 대응하도록 변경

                if (SimulMode)
                {
                    return IO.SimulValue;
                }
                // 2020.11. 19 RM IO 관련 수정
                bool RMIO = GlobalData.Current.mRMManager.ModuleList.ContainsKey(ModuleID); //Where 문대신 ContainsKey 으로 변경.

                if (RMIO)
                {
                    return GlobalData.Current.mRMManager[ModuleID].ReadRMSensorIO(IO);
                }
                else
                {
                    if (IO.Active)
                    {
                        return IO.Direction == eIODirectionTypeList.In ? CCN.Read_X_Bit(IO.Address) : CCN.Read_Y_Bit(IO.Address);
                    }
                    else
                    {
                        return IO.Direction == eIODirectionTypeList.In ? !CCN.Read_X_Bit(IO.Address) : CCN.Read_Y_Bit(IO.Address);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "모듈:{0} I/O : {1} Read 도중 예외가 발생했습니다.", ModuleID, IoName);
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }
        }

        public void WriteIO(string ModuleID, string IoName, bool OnOff)
        {
            try
            {
                var IO = _IOPointList.Where(r => r.ModuleID == ModuleID && r.Name == IoName).FirstOrDefault();
                if (SimulMode)
                {
                    IO.SimulValue = OnOff;
                    return;
                }
                CCIODevice CCN = GetCCIODevice(IO.Board); //-CCLINK 멀티보드 대응하도록 변경

                // 2020.11. 19 RM IO 관련 수정
                bool RMIO = GlobalData.Current.mRMManager.ModuleList.ContainsKey(ModuleID); //Where 문대신 ContainsKey 으로 변경.
                if (RMIO)
                {
                    GlobalData.Current.mRMManager[ModuleID].WriteRMSensorIO(IO, OnOff);
                }
                else
                {
                    if (IO.Direction == eIODirectionTypeList.Out) //출력 비트체크
                    {
                        CCN.Write_Y_Bit(IO.Address, OnOff);
                    }
                }

            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "모듈:{0} I/O : {1} Write 도중 예외가 발생했습니다.", ModuleID, IoName);
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return;
            }
        }
        public IOPointList GetIOPointList()
        {
            return _IOPointList;
        }
        public List<IOPoint> GetModuleIOList(string ModuleID)
        {
            var iList = _IOPointList.Where(R => R.ModuleID == ModuleID || R.ModuleID == ModuleID + "_ST");
            return iList.ToList<IOPoint>();
        }
        public void CloseAllCClinkDevice()
        {
            try
            {
                CCN_First?.CloseDevice();
                CCN_Second?.CloseDevice();
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }
        private CCIODevice GetCCIODevice(int board)
        {
            switch (board)
            {
                case 0:
                    return CCN_First;
                case 1:
                    return CCN_Second;
                default:
                    return null;
            }
        }
    }
}
