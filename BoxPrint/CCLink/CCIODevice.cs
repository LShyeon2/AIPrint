using BoxPrint.CCLinkHelper;
using BoxPrint.Log;
using System;
using System.Collections;
using System.Diagnostics;


namespace BoxPrint.CCLink.IODevice
{
    public class CCIODevice
    {
        private static object thisLock = new object();

        private Int16 _ByteSize = 2;
        private Int16 _NetStationNo = 0xFF;
        private Int32 _Channel = 81;
        private Int32 _Path = 81;
        private bool _IsOpen = false;
        private bool _IsOpenning = false;

        private String _ioListFileName = String.Empty;

        private IOPointList _IOPointList = null;

        //private Timer _ioCheckTimer = null;
        //private bool _eventEnable = true;
        /// <summary>
        /// -CCLINK 멀티보드 대응하도록 변경
        /// </summary>
        /// <param name="Channel"></param>
        public CCIODevice(int Board)
        {
            _Channel += Board;
            _Path += Board;
            this.InitDevice();
        }

        #region 보드 자체 I/O 업데이트 삭제 필요 필요한곳에서 직접 호출
        //private void InitIOCheckTimer()
        //{
        //    this.bUpdateIO = false;
        //    var task2 = Task.Factory.StartNew(() =>
        //    {

        //        while (true)
        //        {
        //            if (bUpdateIO)
        //            {
        //                UpdateIO();//UI갱신을 위한 I/O Polling
        //            }
        //            // DeviceNet UI 갱신주기 100ms ->200ms 로 변경(보드부하경감)
        //            Thread.Sleep(200); //UI 모니터링용.실제 I/O 체크는 직접 API 호출 함.

        //        }
        //    });
        //}

        //private void UpdateIO()
        //{
        //    if (!_IsOpen || _IOPointList == null)
        //        return;

        //    bool bVal, bTmp;
        //    if (!IsSimulMode) //Real Mode
        //    {

        //        foreach (IOPoint point in _IOPointList)
        //        {
        //            //bVal = ReadIO(point.ModuleID, point.Name);
        //            bVal = this.Read_X_Bit( point.Address);

        //            if (point.RawValue != bVal)
        //            {
        //                bTmp = point.RawValue;

        //                point.Value = (bVal) ? !(true ^ point.Active) : !(false ^ point.Active);
        //                point.RawValue = bVal;

        //                // Event 발생
        //                OnIOPointValueChanged(new IOEventArgs(point.ModuleID, point.Name, point.Value, point.RawValue, point.Address, point.Direction));
        //            }
        //        }
        //    }
        //    else //Simul Mode
        //    {
        //        foreach (IOPoint point in _IOPointList)
        //        {
        //           // bVal = ReadIO(point.ModuleID, point.Name); ;
        //            bVal = this.Read_X_Bit(point.Address);
        //            if (point.RawValue != bVal)
        //            {
        //                bTmp = point.SimulValue;

        //                point.Value = (bVal) ? !(true ^ point.Active) : !(false ^ point.Active);
        //                point.RawValue = bVal;

        //                // Event 발생
        //                OnIOPointValueChanged(new IOEventArgs(point.ModuleID, point.Name, point.Value, point.RawValue, point.Address, point.Direction));

        //            }
        //        }
        //    }
        //}
        /// <summary>
        /// IO Point의 값이 변경될 때 발생하는 event
        /// Direction이 In 의 경우만 발생하며 out의 경우는 발생하지 않는다
        /// </summary>
        //public event EventHandler<IOEventArgs> IOPointValueChanged;

        //protected void OnIOPointValueChanged(IOEventArgs e)
        //{
        //    EventHandler<IOEventArgs> handler = IOPointValueChanged;
        //    if (handler != null)
        //    {
        //        handler(this, e);
        //    }
        //}
        #endregion

        public bool InitDevice()
        {
            Int16 iResult = 0;
            bool bSuccess = true;

            try
            {

                if (_IsOpenning == true) return false; //이미 mdOpen중이라면 Skip
                _IsOpenning = true;

                Process p = Process.GetCurrentProcess();
                p.MaxWorkingSet = new IntPtr(40 * 1024 * 1024);
                p.MinWorkingSet = new IntPtr(30 * 1024 * 1024);

                iResult = BoxPrint.CCLinkHelper.CCLinkHelper.mdOpen((short)_Channel, -1, ref _Path);

                if (iResult != 0 && iResult != 66)
                    bSuccess = false;
                else
                {
                    bSuccess = true;

                    _IsOpen = true;

                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Fatal, "== CCIODevice.InitDevice() Exception : {0}", ex.ToString());
                bSuccess = false;
            }
            finally
            {
                _IsOpenning = false; //이미 mdOpen중이라면 Skip
            }

            return bSuccess;
        }

        public bool CloseDevice()
        {
            Int16 iResult = 0;
            bool bClose = false;

            try
            {
                lock (thisLock)
                {
                    if (!_IsOpen)
                        bClose = true;
                    else
                        iResult = BoxPrint.CCLinkHelper.CCLinkHelper.mdClose(_Path);

                    if (iResult != 0)
                        bClose = false;
                    else
                    {
                        bClose = true;
                        _IsOpen = false;
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Fatal, "== CCIODevice.CloseDevice() Exception : {0}", ex.ToString());
                bClose = false;
            }

            return bClose;
        }

        public bool Read_X_Bit(string HexAddress) //Read X
        {
            Int16 iResult = 0;
            Int16 iReadData = 0;
            Int16 iDevNo = 0;

            Int32 iOffset = 0;
            Int32 iBit = 0;

            BitArray bit = null;

            Int32 iConvertedAddr = 0;

            try
            {

                //IOPoint IO = ioList.Where(r => r.Name.ToString() == IoName).FirstOrDefault();

                lock (thisLock)
                {
                    if (!_IsOpen)
                        InitDevice();

                    //if (IO.Address.StartsWith("0x"))
                    //    iConvertedAddr = Convert.ToInt32(IO.Address, 16);
                    //else
                    //    iConvertedAddr = Convert.ToInt32(IO.Address);

                    iConvertedAddr = Convert.ToInt32(HexAddress, 16);

                    iOffset = iConvertedAddr / 0x10;
                    iBit = iConvertedAddr % 0x10;
                    iDevNo = (Int16)(iOffset * 0x10);


                    iResult = BoxPrint.CCLinkHelper.CCLinkHelper.mdReceive(_Path, _NetStationNo, (Int16)BoxPrint.CCLinkHelper.DeviceType.DevX, iDevNo, ref _ByteSize, ref iReadData);


                    if (iResult != 0)
                    {
                        //LogMaker.current.WriteLine(eLogFolder.RM, eLogLevelTypeList.Error, string.Format("CCLink Data Can't Read => Address : {0} Error : {1}", IO.Address, iResult));

                    }

                    Byte[] bytTemp = BitConverter.GetBytes(iReadData);

                    bit = new BitArray(bytTemp);

                    if (bit.Count > 0 && (bit.Length > iBit))
                    {
                        if (bit[iBit])
                            return true;
                        else
                            return false;
                    }
                    else
                        return false;
                }

            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, String.Format("CCLink [ReadIO] : {0}", ex));
                //LogHelper.WriteConsole(eLogLevelTypeList.Error, String.Format("CCLink [ReadIO] : {0}", ex));
                //LogMaker.current.WriteLine(eLogFolder.RM, eLogLevelTypeList.Error, String.Format("CCLink [ReadIO] : {0}", ex));
                return false;
            }
        }

        //public bool Read_X_Bit(string IOname) //Read X
        //{

        //    Int16 iResult = 0;
        //    Int16 iReadData = 0;
        //    Int16 iDevNo = 0;

        //    Int32 iOffset = 0;
        //    Int32 iBit = 0;

        //    BitArray bit = null;

        //    Int32 iConvertedAddr = 0;

        //    try
        //    {

        //        //IOPoint IO = ioList.Where(r => r.Name.ToString() == IoName).FirstOrDefault();

        //        lock (thisLock)
        //        {
        //            if (!_IsOpen)
        //                InitDevice();

        //            //if (IO.Address.StartsWith("0x"))
        //            //    iConvertedAddr = Convert.ToInt32(IO.Address, 16);
        //            //else
        //            //    iConvertedAddr = Convert.ToInt32(IO.Address);

        //            iConvertedAddr = Convert.ToInt32(HexAddress, 16);

        //            iOffset = iConvertedAddr / 0x10;
        //            iBit = iConvertedAddr % 0x10;
        //            iDevNo = (Int16)(iOffset * 0x10);


        //            iResult = S.LIB.CCLinkHelper.CCLinkHelper.mdReceive(_Path, _NetStationNo, (Int16)S.LIB.CCLinkHelper.DeviceType.DevX, iDevNo, ref _ByteSize, ref iReadData);


        //            if (iResult != 0)
        //            {
        //                //LogMaker.current.WriteLine(eLogFolder.RM, eLogLevelTypeList.Error, string.Format("CCLink Data Can't Read => Address : {0} Error : {1}", IO.Address, iResult));

        //            }

        //            Byte[] bytTemp = BitConverter.GetBytes(iReadData);

        //            bit = new BitArray(bytTemp);

        //            if (bit.Count > 0 && (bit.Length > iBit))
        //            {
        //                if (bit[iBit])
        //                    return true;
        //                else
        //                    return false;
        //            }
        //            else
        //                return false;
        //        }

        //    }
        //    catch (Exception ex)
        //    {
        //        //LogHelper.WriteConsole(eLogLevelTypeList.Error, String.Format("CCLink [ReadIO] : {0}", ex));
        //        //LogMaker.current.WriteLine(eLogFolder.RM, eLogLevelTypeList.Error, String.Format("CCLink [ReadIO] : {0}", ex));
        //        return false;
        //    }
        //}

        //근바

        public bool Read_Y_Bit(string HexAddress) //Read Y
        {

            Int16 iResult = 0;
            Int16 iReadData = 0;
            Int16 iDevNo = 0;

            Int32 iOffset = 0;
            Int32 iBit = 0;

            BitArray bit = null;

            Int32 iConvertedAddr = 0;

            try
            {
                //IOPoint IO = ioList.Where(r => r.Name.ToString() == IoName).FirstOrDefault();

                lock (thisLock)
                {
                    if (!_IsOpen)
                        InitDevice();

                    //if (HexAddress.StartsWith("0x"))
                    //    iConvertedAddr = Convert.ToInt32(HexAddress, 16);
                    //else
                    //    iConvertedAddr = Convert.ToInt32(HexAddress);

                    iConvertedAddr = Convert.ToInt32(HexAddress, 16);

                    iOffset = iConvertedAddr / 0x10;
                    iBit = iConvertedAddr % 0x10;
                    iDevNo = (Int16)(iOffset * 0x10);


                    iResult = BoxPrint.CCLinkHelper.CCLinkHelper.mdReceive(_Path, _NetStationNo, (Int16)BoxPrint.CCLinkHelper.DeviceType.DevY, iDevNo, ref _ByteSize, ref iReadData);


                    if (iResult != 0)
                    {
                        //LogMaker.current.WriteLine(eLogFolder.RM, eLogLevelTypeList.Error, string.Format("CCLink Data Can't Read => Address : {0} Error : {1}", IO.Address, iResult));
                    }

                    Byte[] bytTemp = BitConverter.GetBytes(iReadData);

                    bit = new BitArray(bytTemp);

                    if (bit.Count > 0 && (bit.Length > iBit))
                    {
                        if (bit[iBit])
                            return true;
                        else
                            return false;
                    }
                    else
                        return false;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                //LogMaker.current.WriteLine(eLogFolder.RM, eLogLevelTypeList.Error, String.Format("CCLink [ReadIO] : {0}", ex));
                return false;
            }
        }


        public void Write_Y_Bit(string HexAddress, bool ioData)
        {
            Int16 iResult = 0;

            Int32 iConvertedAddr = 0;
            Int16 iDevNo = 0;

            Int32 iOffset = 0;
            Int32 iBit = 0;

            bool bVal = ioData;

            try
            {
                lock (thisLock)
                {
                    if (!_IsOpen)
                        InitDevice();

                    //if (!(ioData ^ ioPoint.Active))
                    //    bVal = true;
                    //else
                    //    bVal = false;
                    iConvertedAddr = Convert.ToInt32(HexAddress, 16);

                    iOffset = iConvertedAddr / 0x10;
                    iBit = iConvertedAddr % 0x10;
                    iDevNo = (Int16)(iOffset * 0x10);

                    if (bVal)
                        iResult = BoxPrint.CCLinkHelper.CCLinkHelper.mdDevSet(_Channel, _NetStationNo, (Int16)DeviceType.DevY, (Int16)iConvertedAddr);
                    else
                        iResult = BoxPrint.CCLinkHelper.CCLinkHelper.mdDevRst(_Channel, _NetStationNo, (Int16)DeviceType.DevY, (Int16)iConvertedAddr);

                    if (iResult != 0)
                    {
                        //throw new Exception("데이터를 변경할 수 없습니다");
                        LogManager.WriteConsoleLog(eLogLevel.Error, "IO - 데이터를 변경할 수 없습니다 ADDRESS: {0}", HexAddress);


                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());

            }
        }

        #region \ord

        public void WriteWordData(short _WriteStartAddress, short value)
        {
            Int16 iResult = 0;
            Int16 _ByteSize = 2;
            Int16 iWriteData = 0;

            try
            {
                lock (thisLock)
                {
                    if (!_IsOpen)
                        InitDevice();

                    iWriteData = (Int16)value;

                    iResult = BoxPrint.CCLinkHelper.CCLinkHelper.mdSend(_Channel, _NetStationNo, (Int16)DeviceType.DevWw, _WriteStartAddress, ref _ByteSize, ref iWriteData);

                    if (iResult != 0)
                    {
                        //throw new Exception("CCLink Word 데이터를 변경할 수 없습니다");
                        //LogHelper.WriteConsole(eLogLevelTypeList.Error, "CCLink Word 데이터를 변경할 수 없습니다");
                    }

                    //LogHelper.WriteCTCLog(eLogLevelTypeList.Info, String.Format("IO DevWw : [{0}][{1}]", _WriteStartAddress, value), "IO");

                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                //LogHelper.WriteConsole(eLogLevelTypeList.Error, String.Format("CCLink [WriteIO] : {0}", ex));
            }
        }



        public short ReadWordData(short _ReadStartAddress)
        {
            Int16 iResult = 0;
            Int16 _ByteSize = 2;
            Int16 iReadData = 0;
            //Int16 value = 0;

            try
            {
                lock (thisLock)
                {
                    if (!_IsOpen)
                        InitDevice();

                    iResult = BoxPrint.CCLinkHelper.CCLinkHelper.mdReceive(_Channel, _NetStationNo, (Int16)DeviceType.DevWr, _ReadStartAddress, ref _ByteSize, ref iReadData);

                    if (iResult != 0)
                    {
                        //throw new Exception("CCLink Word 데이터를 읽을 수 없습니다");
                        //LogHelper.WriteConsole(eLogLevelTypeList.Error, string.Format("CCLink Word 데이터를 읽을 수 없습니다"));
                    }

                    //LogHelper.WriteCTCLog(eLogLevelTypeList.Info, String.Format("IO DevWr : [{0}][{1}]", _ReadStartAddress, iReadData), "IO");

                    return iReadData;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                //LogHelper.WriteConsole(eLogLevelTypeList.Error, String.Format("CCLink [ReadIO] : {0}", ex));
            }
            return -1;
        }

        public IOPointList getCCLINkIoList()
        {
            return this._IOPointList;
        }
        #endregion

    }
}
