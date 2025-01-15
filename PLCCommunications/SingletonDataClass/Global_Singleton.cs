using PLCCommunications.ConfigDataClass;
using PLCCommunications.CommunicationDataClass;

namespace PLCCommunications.SingletonDataClass
{
    public class Global_Singleton : SingletonBase<Global_Singleton>
    {
        //220315 HHJ SCS 개발     //- MXComponentNet, SharedMemory 개선
        public PLCSection _PlcSection;              //PLC Section
        public MXComponentNet _mxComponentNet;      //MXComponent 저장용
        public SharedMemoryClass _sharedMemory;     //공유 메모리 저장용
    }
}
