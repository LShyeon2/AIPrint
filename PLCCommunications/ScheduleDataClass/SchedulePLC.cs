//220315 HHJ SCS 개발     //- MXComponentNet, SharedMemory 개선
using PLCCommunications.CommunicationDataClass;
using PLCCommunications.SingletonDataClass;
using System;
using System.Diagnostics;
using System.Threading;
using PLCCommunications.Log;

namespace PLCCommunications.ScheduleDataClass
{
    public class SchedulePLC : ScheduleBase
    {
        Global_Singleton _global = Global_Singleton.Instance;//싱글턴 생성

        public SchedulePLC()
        {

        }

        public override bool Run()
        {
            _global._mxComponentNet.OnPlcConnect();
            _global._sharedMemory.InitializeSharedMemory();

            string pName = Process.GetCurrentProcess().ProcessName;
            string MainName = pName.Replace("PLCCommunications", "Conveyorfirmware");

            while (true)
            {
                Thread.Sleep(50);

                if (!_global._mxComponentNet.IsConnected)
                    continue;

                //아직 테스트 단계이기에 프로세스 따라서 종료되는 로직은 제외함.
                //Process[] p = Process.GetProcessesByName(MainName);

                //if (p.Count<Process>() == 0)
                //{
                //    break;
                //}

                UpdateSharedMemory();
            }

            return false;
        }

        private void UpdateSharedMemory()
        {
            int iBaseAddress;
            int iStart = -1;
            int iCnt = 0;
            short[] readsharedBitbuf = null;
            short[] readsharedWordbuf = null;
            try
            {
                //PLC의 어드레스가 기준이기에 어드레스는 0에서 시작하지 않지만, 공유메모리 주소는 0번지부터 시작한다.
                //0번지부터 시작하도록 맞춰주기 위해서 어드레스의 시작인 PLC의 Bit 어드레스를 빼준다.
                iBaseAddress = _global._mxComponentNet.BasePlcLB;
                //1. PLC에서 Bit, Word Data를 읽어온다.
                MxData mxPlcData = _global._mxComponentNet.ReadAll();

                //2. 읽어온 데이터를 공유 메모리에 기재해준다.
                iStart = _global._mxComponentNet.BasePlcLB - iBaseAddress;
                iCnt = 0;
                foreach (short s in mxPlcData.AllBitData)
                {
                    _global._sharedMemory.SharedMemoryWriteWord(iStart + iCnt++, s);
                }

                iStart = _global._mxComponentNet.BasePlcLW - iBaseAddress;
                iCnt = 0;
                foreach (short s in mxPlcData.AllWordData)
                {
                    _global._sharedMemory.SharedMemoryWriteWord(iStart + iCnt++, s);
                }

                //3. PC에서 공유메모리로 기재한 데이터를 읽어와 PLC에 기재해준다.
                iStart = _global._mxComponentNet.BasePcLB - iBaseAddress;
                readsharedBitbuf = _global._sharedMemory.SharedMemoryReadWord(iStart, _global._mxComponentNet.PCBitSize);

                iStart = _global._mxComponentNet.BasePcLW - iBaseAddress;
                readsharedWordbuf = _global._sharedMemory.SharedMemoryReadWord(iStart, _global._mxComponentNet.PCWordSize);

                MxData mxSharedData = new MxData(0, readsharedBitbuf, readsharedWordbuf);
                _global._mxComponentNet.WriteAll(mxSharedData);
            }
            catch (Exception ex)
            {

            }
        }
    }
}
