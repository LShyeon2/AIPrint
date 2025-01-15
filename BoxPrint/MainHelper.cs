using BoxPrint.Log;
using System;
using System.Runtime.InteropServices;

namespace BoxPrint
{
    public static class MainHelper
    {
        #region 로컬 시간 설정 API Call
        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEMTIME
        {
            public ushort wYear;
            public ushort wMonth;
            public ushort wDayOfWeek;
            public ushort wDay;
            public ushort wHour;
            public ushort wMinute;
            public ushort wSecond;
            public ushort wMilliseconds;
        }
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetLocalTime(ref SYSTEMTIME st);



        public static bool SetLocaltime(DateTime dt)
        {
            SYSTEMTIME st = new SYSTEMTIME();
            st.wYear = (ushort)dt.Year;
            st.wMonth = (ushort)dt.Month;
            st.wDay = (ushort)dt.Day;
            st.wHour = (ushort)dt.Hour;
            st.wMinute = (ushort)dt.Minute;
            st.wSecond = (ushort)dt.Second;
            st.wMilliseconds = (ushort)dt.Millisecond;

            bool result = SetLocalTime(ref st);
            if (result == false)
            {
                int lastError = Marshal.GetLastWin32Error();
                LogManager.WriteConsoleLog(eLogLevel.Info, "Failed to set LocalTime. Reason Code: {0}", lastError);
            }
            return result;
        }
        #endregion
    }
}
