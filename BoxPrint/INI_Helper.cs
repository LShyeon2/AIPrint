using System.Runtime.InteropServices;
using System.Text;
//241018 HoN 화재시나리오 운영 추가       4.2) 화재수조 적재 불가시 자동포트 검색 필요. (사이즈가 맞지않는 포트, 브릿지, 랙간이동 포트 제외)
using System;
using System.Collections.Generic;

namespace BoxPrint
{
    public static class INI_Helper
    {

        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath);
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        public static string ReadValue(string section, string key)
        {
            StringBuilder sb = new StringBuilder(255);
            string IniFile = GlobalData.Current.CurrentFilePaths(System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName)) + @"\Data\firmware.ini";
            int i = GetPrivateProfileString(section, key, "", sb, 255, IniFile);
            return sb.ToString();
        }

        public static void WriteValue(string section, string key, string value)
        {
            string IniFile = GlobalData.Current.CurrentFilePaths(System.IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName)) + @"\Data\firmware.ini";
            WritePrivateProfileString(section, key, value, IniFile);
        }

        public static string ReadValue(string iniFile, string section, string key)
        {
            StringBuilder sb = new StringBuilder(255);
            int i = GetPrivateProfileString(section, key, "", sb, 255, iniFile);
            return sb.ToString();
        }
        public static void WriteValue(string iniFile, string section, string key, string value)
        {
            WritePrivateProfileString(section, key, value, iniFile);
        }
    }
}
