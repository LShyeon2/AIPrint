using System;
using System.Windows.Threading;
using ActUtlTypeLib;
using PLCCommunications.Log;
using PLCCommunications.ConfigDataClass;

namespace PLCCommunications.CommunicationDataClass
{
    //220315 HHJ SCS 개발     //- MXComponentNet, SharedMemory 개선
    public class MxData
    {
        public int Result { get; set; }
        public short[] AllBitData { get; set; }
        public short[] AllWordData { get; set; }

        public MxData(int result)
        {
            Result = result;
            AllBitData = null;
            AllWordData = null;
        }
        public MxData(int result, short[] allBitRead, short[] allWordRead)
        {
            Result = result;
            AllBitData = allBitRead;
            AllWordData = allWordRead;
        }
    }
    public class MXComponentNet
    {
        public ActUtlTypeClass _ActUtlType;

        public bool _isPlcConnection;

        private static Object thisLock = new Object();

        DispatcherTimer timer = new DispatcherTimer();    //객체생성

        private static object DataLock = new object();

        public short LogicalStation { get; set; }
        public ushort BasePlcLB { get; set; }
        /// <summary>
        /// PLC Base LW
        /// </summary>
        public ushort BasePlcLW { get; set; }
        /// <summary>
        /// PC Base LB
        /// </summary>
        public ushort BasePcLB { get; set; }
        /// <summary>
        /// PC Base LW
        /// </summary>
        public ushort BasePcLW { get; set; }
        private string BitDeviceType { get; set; }
        private string WordDeviceType { get; set; }
        /// <summary>
        /// PLC Bit Size (PLC에 할당된 전체 Bit Data Size)
        /// </summary>
        public ushort PLCBitSize { get; set; }
        /// <summary>
        /// PLC Word Size (PLC에 할당된 전체 Bit Data Size)
        /// </summary>
        public ushort PLCWordSize { get; set; }
        /// <summary>
        /// PC Bit Size (PC에 할당된 전체 Bit Data Size)
        /// </summary>
        public ushort PCBitSize { get; set; }
        /// <summary>
        /// PC Word Size (PC에 할당된 전체 Word Data Size)
        /// </summary>
        public ushort PCWordSize { get; set; }
        /// <summary>
        /// 생성자
        /// </summary>
        public MXComponentNet(PLCElement element)
        {
            _isPlcConnection = false;

            LogicalStation = element.LogicalStationNum;

            BasePlcLB = element.BaseAddresses.BasePlcLB;
            BasePlcLW = element.BaseAddresses.BasePlcLW;
            BasePcLB = element.BaseAddresses.BasePcLB;
            BasePcLW = element.BaseAddresses.BasePcLW;

            //210908 HHJ 개발 //- PLC Interface 개발
            BitDeviceType = element.BitDeviceType;
            WordDeviceType = element.WordDeviceType;

            PLCBitSize = element.DataSize.PLCBitSize;
            PLCWordSize = element.DataSize.PLCWordSize;
            PCBitSize = element.DataSize.PCBitSize;
            PCWordSize = element.DataSize.PCWordSize;
        }

        /// <summary>
        /// PLC 연결
        /// </summary>
        /// <param name="stationNumber"></param>
        /// <returns></returns>
        public bool OnPlcConnect()
        {
            int code = -1;

            try
            {
                if (_ActUtlType == null)
                    _ActUtlType = new ActUtlTypeClass();

                _ActUtlType.ActLogicalStationNumber = LogicalStation;

                code = _ActUtlType.Open();
            }
            catch (Exception ex)
            {
                LogManager.WritePLCLog(eLogLevel.Error, "{0} - [MXComponentNet] PLC Open Exception (Message : {1})", DateTime.Now, ex.ToString());
            }

            if (code == 0)
            {
                _isPlcConnection = true;
                LogManager.WritePLCLog(eLogLevel.Info, "{0} - [MXComponentNet] PLC Open Success", DateTime.Now);
            }
            else
            {
                _isPlcConnection = false;
                LogManager.WritePLCLog(eLogLevel.Error, "{0} - [MXComponentNet] PLC Open failure (Error Code : {1})", DateTime.Now, code);
            }

            return _isPlcConnection;
        }

        public bool IsConnected { get { return _isPlcConnection; } }

        //PLC 닫기
        public int Close()
        {
            _isPlcConnection = false;

            return this._ActUtlType.Close();
        }

        public MxData ReadAll()
        {
            int iaddress;
            string sBitAddress;
            string sWordAddress;
            short[] Bittemp;
            short[] Wordtemp;

            try
            {
                //Bit, Word Read All
                //시작 어드레스 생성
                iaddress = BasePlcLB;
                if (string.IsNullOrEmpty(BitDeviceType))
                    sBitAddress = "B" + iaddress.ToString("X");
                else
                    sBitAddress = BitDeviceType + iaddress;

                iaddress = BasePlcLW;
                if (string.IsNullOrEmpty(WordDeviceType))
                    sWordAddress = "W" + iaddress.ToString("X");
                else
                    sWordAddress = WordDeviceType + iaddress;

                //리딩 데이터 만큼 배열 생성
                Bittemp = new short[PLCBitSize];
                Wordtemp = new short[PLCWordSize];

                //Data Reading
                int iBitret = _ActUtlType.ReadDeviceBlock2(sBitAddress, PLCBitSize, out Bittemp[0]);
                int iWordret = _ActUtlType.ReadDeviceBlock2(sWordAddress, PLCWordSize, out Wordtemp[0]);

                if (!iBitret.Equals(0) || !iWordret.Equals(0))
                {
                    //비트 리턴값이 0이 아니면 비트 리턴값으로 리턴을하고 비트 리턴이 0이면 워드 리턴값도 확인하여 워드 리턴이 0이 아니면 워드 리턴값을, 워드 리턴도 0이라면 -1을 리턴한다.
                    int iret = !iBitret.Equals(0) ? iBitret : !iWordret.Equals(0) ? iWordret : -1;

                    LogManager.WritePLCLog(eLogLevel.Error, "Bit ReadDeviceBlock2 Result {0}, Word ReadDeviceBlock2 Result {1}", iBitret, iWordret);
                    return new MxData(iret);
                }
                else
                {
                    return new MxData(0, Bittemp, Wordtemp);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return null;
        }

        public int WriteAll(MxData mxData)
        {
            int iaddress;
            string sBitAddress;
            string sWordAddress;
            short[] Bittemp = null;
            short[] Wordtemp = null;
            bool bBitError = false;
            bool bWordError = false;

            try
            {
                //Bit, Word Write All
                //시작 어드레스 생성
                iaddress = BasePcLB;
                if (string.IsNullOrEmpty(BitDeviceType))
                    sBitAddress = "B" + iaddress.ToString("X");
                else
                    sBitAddress = BitDeviceType + iaddress;

                iaddress = BasePcLW;
                if (string.IsNullOrEmpty(WordDeviceType))
                    sWordAddress = "W" + iaddress.ToString("X");
                else
                    sWordAddress = WordDeviceType + iaddress;

                //기재할 데이터 확인
                if (mxData.AllBitData != null)
                    Bittemp = mxData.AllBitData;
                else
                {
                    bBitError = true;
                    Console.WriteLine("WriteBitData is Null");
                }

                if (mxData.AllWordData != null)
                    Wordtemp = mxData.AllWordData;
                else
                {
                    bWordError = true;
                    Console.WriteLine("WriteWordData is Null");
                }

                //Data Write
                int iBitret = bBitError == true ? -1 : _ActUtlType.WriteDeviceBlock2(sBitAddress, PCBitSize, ref Bittemp[0]);
                int iWordret = bWordError == true ? -1 : _ActUtlType.WriteDeviceBlock2(sWordAddress, PCWordSize, ref Wordtemp[0]);

                if (!iBitret.Equals(0) || !iWordret.Equals(0))
                {
                    //비트 리턴값이 0이 아니면 비트 리턴값으로 리턴을하고 비트 리턴이 0이면 워드 리턴값도 확인하여 워드 리턴이 0이 아니면 워드 리턴값을, 워드 리턴도 0이라면 -1을 리턴한다.
                    int iret = !iBitret.Equals(0) ? iBitret : !iWordret.Equals(0) ? iWordret : -1;

                    LogManager.WritePLCLog(eLogLevel.Error, "Bit ReadDeviceBlock2 Result {0}, Word ReadDeviceBlock2 Result {1}", iBitret, iWordret);
                    return iret;
                }
                else
                {
                    return 0;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
