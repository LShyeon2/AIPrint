//220315 HHJ SCS 개발     //- MXComponentNet, SharedMemory 개선
using System;
using System.IO;
using log4net;

namespace PLCCommunications.Log
{
    public class LogManager
    {
        static LogManager log4;

        private static ILog logPLC;

        public static LogManager InitLog4Net()
        {
            if (log4 == null)
            {
                log4 = new LogManager();
            }

            return log4;
        }
        private LogManager()
        {
            try
            {
                string paths = Environment.CurrentDirectory;
                //실행위치에 config파일을 먼저 찾아본다.
                FileInfo File = new FileInfo(paths + @"\log.config");
                if (!File.Exists)
                {
                    //실행위치에 없으면 프로젝트 루트를 검색.
                    int indexnum = 0;
                    indexnum = paths.IndexOf("\\bin");
                    paths = paths.Remove(indexnum);
                    File = new FileInfo(paths + @"\log.config");
                }

                //둘다 없으면 예외를 던진다.
                if (!File.Exists)
                {
                    throw new FileNotFoundException("LogManager Config파일이 실행위치나 루트에 존재하지 않습니다!", "log.config");
                }
                log4net.Config.XmlConfigurator.ConfigureAndWatch(File);

                logPLC = log4net.LogManager.GetLogger("PLC_Logger");
            }
            catch (FileNotFoundException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private static void WriteLog(ILog logger, eLogLevel logLevel, string Text, params object[] args)
        {
            String sMessage = String.Format(Text, args);
            switch (logLevel)
            {
                case eLogLevel.Debug:
                    logger.Debug(sMessage);
                    break;
                case eLogLevel.Error:
                    logger.Error(sMessage);
                    break;
                case eLogLevel.Fatal:
                    logger.Fatal(sMessage);
                    break;
                case eLogLevel.Info:
                    logger.Info(sMessage);
                    break;
                case eLogLevel.Warn:
                    logger.Warn(sMessage);
                    break;
            }
        }

        public static void WritePLCLog(eLogLevel logLevel, string Text, params object[] args)
        {
            System.Console.WriteLine(Text, args);
            WriteLog(logPLC, logLevel, Text, args);
        }

    }
}
