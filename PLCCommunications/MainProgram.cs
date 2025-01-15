using System;
using System.IO;
using System.Configuration;

using PLCCommunications.SingletonDataClass;
using PLCCommunications.CommunicationDataClass;
using PLCCommunications.ConfigDataClass;
using PLCCommunications.ScheduleDataClass;
using PLCCommunications.Log;

namespace PLCCommunications
{
    public partial class MainProgram 
    {
        Global_Singleton _global = Global_Singleton.Instance;//싱글턴 생성

        /// <summary>
        /// 시작
        /// </summary>
        /// <param name="args"></param>
        [STAThread]
        static void Main(string[] args)
        {
            MainProgram main = new MainProgram();
        }

        /// <summary>
        /// 생성자
        /// </summary>
        public MainProgram()
        {
            string ConfigPath = Path.Combine(Directory.GetCurrentDirectory());
            string CinfigFilePath = Path.Combine(ConfigPath, "App.config");

            if (!File.Exists(CinfigFilePath))
                throw new Exception(string.Format("Configfile {0} dose not exist.", CinfigFilePath));

            ExeConfigurationFileMap fileMap = new ExeConfigurationFileMap();
            fileMap.ExeConfigFilename = CinfigFilePath;
            Configuration cfg = ConfigurationManager.OpenMappedExeConfiguration(fileMap, ConfigurationUserLevel.None);

            _global._PlcSection = cfg.GetSection(PLCSection.SECTION_NAME) as PLCSection;

            LogManager.InitLog4Net();   //220311 HHJ CCS 개발     //- PLCCommunications Log 추가

            foreach (PLCElement e in _global._PlcSection.Plcs)
            {
                _global._mxComponentNet = new MXComponentNet(e);    //MXComponent 초기화
            }

            _global._sharedMemory = new SharedMemoryClass();

            ScheduleBase MainSchedule = new SchedulePLC();

            MainSchedule.Run();

            Environment.Exit(20);
        }

        /// <summary>
        /// 소멸자
        /// </summary>
        ~MainProgram()
        {
            LogManager.WritePLCLog(eLogLevel.Info, "MainProgram Close");

            _global._sharedMemory.Dispose();
        }
    }
}
