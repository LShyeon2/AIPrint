using System;
using PLCCommunications.Log;

namespace PLCCommunications.CommunicationDataClass
{
    //220315 HHJ SCS 개발     //- MXComponentNet, SharedMemory 개선
    public class SharedMemoryClass : IDisposable
    {
        //기존 공유메모리의 메써드는 전부 제거하고 실물 PLC <-> PC 사이의 단순 중간 다리의 역만 진행하도록 한다.
        //private SharedMemory.SharedArray<int> _sharedMemoryStorage;
        private SharedMemory.SharedArray<short> _sharedMemoryStorage;

        /// <summary>
        /// 공유 메모리 - 초기화
        /// </summary>
        public void InitializeSharedMemory()
        {
            try
            {
                this._sharedMemoryStorage = new SharedMemory.SharedArray<short>("SharedMemory", 16000);
                LogManager.WritePLCLog(eLogLevel.Info, "InitializeSharedMemory Complete");
            }
            catch (Exception ex)
            {
                this._sharedMemoryStorage = new SharedMemory.SharedArray<short>("SharedMemory");
                LogManager.WritePLCLog(eLogLevel.Error, "InitializeSharedMemory Fail {0}", ex);
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            this._sharedMemoryStorage.Dispose();
        }

        public short[] SharedMemoryReadWord(int start, int size)
        {
            string valueBuffer = string.Empty;

            try
            {
                short[] ret = new short[size];

                for (int i = 0; i < size; i++)
                {
                    ret[i] = _sharedMemoryStorage[start + i];
                }

                return ret;
            }
            catch (Exception ex)
            {
                LogManager.WritePLCLog(eLogLevel.Error, "SharedMemoryReadWord Fail {0}", ex);
                return new short[size];
            }
        }

        public bool SharedMemoryWriteWord(int rcvAddress, short rcvSetValue)
        {
            try
            {
                _sharedMemoryStorage[rcvAddress] = rcvSetValue;

                return true;
            }
            catch (Exception ex)
            {
                LogManager.WritePLCLog(eLogLevel.Error, "SharedMemoryWriteWord Fail {0}", ex);
                return false;
            }
        }
    }
}
