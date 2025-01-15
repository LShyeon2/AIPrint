using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using WCF_LBS.Commands;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using PLCProtocol.DataClass;
using BoxPrint.Alarm;
using BoxPrint.DataList;
using BoxPrint.DataList.MCS;
using BoxPrint.GUI.ViewModels;
using BoxPrint.Log;
using BoxPrint.Modules;
using BoxPrint.Modules.Conveyor;
using BoxPrint.Modules.CVLine;
using BoxPrint.Modules.RM;
using BoxPrint.Modules.Shelf;        //220401 HHJ SCS 개선     //- Xml, DB 혼용에 따른 초기 생성 불가능 현상 조치
using BoxPrint.Modules.User;
using BoxPrint.GUI.Windows.ViewModels;


namespace BoxPrint.DataBase
{
    public class OracleDBManager : IDisposable
    {//
        private readonly object DBLock = new object(); //데이터 베이스 접근 락

        public static OracleDBManager Current;
        private int confignumber = 0;
        //private int DBConnectionDuplexingCount = 0;
        private DateTime DBConnectionDuplexingTime = DateTime.Now;

        private int PollingPeriod = 500; //0.2sec

        private bool _IsConnect;
        public bool IsConnect
        {
            get
            {
                return _IsConnect;
            }
            set
            {
                _IsConnect = value;
            }
        }

        //public ManualResetEvent MRE_DBThreadEvent = new ManualResetEvent(false);

        public object DBConnectionChangeSyncObject = new object();
        private bool _DBConnectionChanging;
        public bool DBConnectionChanging
        {
            get
            {
                return _DBConnectionChanging;
            }
            set
            {
                _DBConnectionChanging = value;
            }
        }

        public string OracleDBPath 
        { 
            get; 
            private set; 
        }

        public string OracleFirstDBPath
        {
            get
            {
                return string.Format("Data Source={0}:{1}/{2};User Id={3};Password={4}", 
                    GlobalData.Current.DBSection.DBFirstConnIP, 
                    GlobalData.Current.DBSection.DBFirstConnPort,
                    GlobalData.Current.DBSection.DBFirstConnServiceName,
                    GlobalData.Current.DBSection.DBAccountName,
                    GlobalData.Current.DBSection.DBPassword);
                //return "Data Source=192.168.1.202:1521/xe;User Id=scstest;Password=1111;";
                //return "Data Source=192.168.1.201:1521/system;User Id=scstest;Password=1111;";
            }
        }

        //220824 조숭진 db접속 이중화.
        public string OracleSecondDBPath
        {
            get
            {
                return string.Format("Data Source={0}:{1}/{2};User Id={3};Password={4}",
                    GlobalData.Current.DBSection.DBSecondConnIP,
                    GlobalData.Current.DBSection.DBSecondConnPort,
                    GlobalData.Current.DBSection.DBSecondConnServiceName,
                    GlobalData.Current.DBSection.DBAccountName,
                    GlobalData.Current.DBSection.DBPassword);
                //return "Data Source=192.168.1.201:1521/system;User Id=scstest;Password=1111;";
                //return "Data Source=192.168.1.202:1521/xe;User Id=scstest;Password=1111;";
            }
        }
        //OracleConnection conn = null;
        //OracleTransaction tran = null;

        private DataTable LogList;

        public bool bLogSearchCancel = false;

        private List<ConnectClientList> _ClientList = new List<ConnectClientList>();
        public List<ConnectClientList> ClientList
        {
            get
            {
                return _ClientList;
            }
            private set
            {
                _ClientList = value;
            }
        }

        public OracleDBManager(out bool dbopenstate, string ForLog = "")
        {
            if (ForLog == "Log")
            {
                dbopenstate = false;
                return;
            }

            LogManager.WriteConsoleLog(eLogLevel.Info, "Creating Oracle DB Manager...... ");
            LogManager.WriteDBLog(eLogLevel.Info, "Creating Oracle DB Manager...... ");

            Current = this;

            OracleConnection conn = null;
            OracleCommand cmd = null;
            OracleDataReader rowdata = null;
            string sql = string.Empty;
            string result = string.Empty;
            string temp = string.Empty;
            dbopenstate = false;

            try
            {
                //tran = new OracleTransaction();
                //220824 조숭진 db접속 이중화. second접속되면 기존 dbpath에 set.
                try
                {
                    conn = new OracleConnection(OracleFirstDBPath);
                    conn.Open();

                    OracleDBPath = OracleFirstDBPath;
                }
                catch (Exception)
                {
                    if (conn != null)
                        conn.Dispose();

                    conn = new OracleConnection(OracleSecondDBPath);
                    conn.Open();

                    OracleDBPath = OracleSecondDBPath;
                }

                LogManager.WriteDBLog(eLogLevel.Info, "Oracle DB Server Open...... ");

                //아래 주석처리되어 있는 것은 테이블을 완전 삭제할 때 사용.
                //sql = string.Format("DROP TABLE TB_MASTER_INFO CASCADE CONSTRAINTS");
                //cmd = new OracleCommand(sql, conn);
                //cmd.ExecuteNonQuery();

                if (GlobalData.Current.ServerClientType == eServerClientType.Server)
                {
                    try
                    {
                        sql = String.Format("SELECT SCS_CD FROM TB_CONFIG_INFO");
                        cmd = new OracleCommand(sql, conn);
                        result = (string)cmd.ExecuteScalar();

                        temp = string.Format("DB Table Find");

                        LogManager.WriteDBLog(eLogLevel.Info, temp, false);
                    }
                    catch (Exception)
                    {
                        LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                        LogManager.WriteDBLog(eLogLevel.Info, "TB_CONFIG_INFO Table Empty", false);

                        if (string.IsNullOrEmpty(result))
                        {
                            sql = string.Format("CREATE TABLE TB_CONFIG_INFO (" +
                                    "SCS_CD NVARCHAR2(64) NOT NULL," +                                                                            //eqpid
                                    "CONFIG_NO NUMBER, " +
                                    "CONFIG_GB NVARCHAR2(64)," +                                                                                //캐리어 현재위치
                                    "CONFIG_NM NVARCHAR2(64)," +                                                                                 //캐리어 아이디
                                    "CONFIG_VAL NVARCHAR2(64)," +                                                                                    //shelf zone name
                                    "CONFIG_DEF NVARCHAR2(64)," +                                                                                        //shelf 캐리어 유무
                                    "CONFIG_DES NVARCHAR2(64))");                                                                                        //shelf 적재 완료시간

                            cmd = new OracleCommand(sql, conn);
                            cmd.ExecuteNonQuery();

                            LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                        }
                    }

                    //20220503 조숭진 sk db사양 준수 s
                    try
                    {
                        if (!string.IsNullOrEmpty(result))
                            result = string.Empty;

                        sql = String.Format("SELECT SCS_CD FROM TB_SHELF_INFO");
                        cmd = new OracleCommand(sql, conn);
                        result = (string)cmd.ExecuteScalar();

                        temp = string.Format("DB Table Find");

                        LogManager.WriteDBLog(eLogLevel.Info, temp, false);
                    }
                    catch (Exception)
                    {
                        LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                        LogManager.WriteDBLog(eLogLevel.Info, "TB_SHELF_INFO Table Empty", false);

                        if (string.IsNullOrEmpty(result))
                        {
                            sql = string.Format("CREATE TABLE TB_SHELF_INFO (" +
                                    "SCS_CD NVARCHAR2(64) NOT NULL," +                                                                            //eqpid
                                    "CARRIER_ID NVARCHAR2(64)," +                                                                                 //캐리어 아이디
                                    "CARRIER_LOC NVARCHAR2(64)," +                                                                                //캐리어 현재위치
                                    "ZONE_NM NVARCHAR2(64)," +                                                                                    //shelf zone name
                                    "EXIST_GB NUMBER," +                                                                                        //shelf 캐리어 유무
                                    "USE_STAT CHAR(1) CHECK(USE_STAT = '1' OR USE_STAT = '0')," +                                               //shelf 사용유무
                                    "SHELFSIZE_GB NUMBER," +                                                                                    //shelf 단/장폭 구분
                                    "STATUS_GB NUMBER," +                                                                                       //shelf status
                                    "DEADZONE_STAT CHAR(1) CHECK(DEADZONE_STAT = '1' OR DEADZONE_STAT = '0')," +
                                    "INSTALL_DTTM TIMESTAMP, " +
                                    "FLOOR_NM NUMBER," +
                                    "MEMO_GB NVARCHAR2(100))");                                                                                        //shelf 적재 완료시간


                            cmd = new OracleCommand(sql, conn);
                            cmd.ExecuteNonQuery();

                            LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                        }
                    }

                    try
                    {
                        if (!string.IsNullOrEmpty(result))
                            result = string.Empty;

                        sql = String.Format("SELECT SCS_CD FROM TB_JOB_INFO");
                        cmd = new OracleCommand(sql, conn);
                        result = (string)cmd.ExecuteScalar();

                        temp = string.Format("DB Table Find");

                        LogManager.WriteDBLog(eLogLevel.Info, temp, false);
                    }
                    catch (Exception)
                    {
                        LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                        LogManager.WriteDBLog(eLogLevel.Info, "TB_JOB_INFO Table Empty", false);

                    }

                    try
                    {
                        if (!string.IsNullOrEmpty(result))
                            result = string.Empty;

                        sql = String.Format("SELECT SCS_CD FROM TB_CARRIER_INFO");
                        cmd = new OracleCommand(sql, conn);
                        result = (string)cmd.ExecuteScalar();

                        temp = string.Format("DB Table Find");

                        LogManager.WriteDBLog(eLogLevel.Info, temp, false);
                    }
                    catch (Exception)
                    {
                        LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                        LogManager.WriteDBLog(eLogLevel.Info, "TB_CARRIER_INFO Table Empty", false);

                        if (string.IsNullOrEmpty(result))
                        {
                            //230207 FINALLOC_NO 2->64로 변경
                            sql = string.Format("CREATE TABLE TB_CARRIER_INFO (" +
                                    "SCS_CD NVARCHAR2(64) NOT NULL," +
                                    "CARRIER_ID NVARCHAR2(64)," +
                                    "CARRIER_LOC NVARCHAR2(64)," +
                                    "PRODUCT_STAT CHAR(1) CHECK(PRODUCT_STAT = '0' OR PRODUCT_STAT = '1' OR PRODUCT_STAT = '2')," +
                                    "POLARITY_STAT CHAR(1) CHECK(POLARITY_STAT = '0' OR POLARITY_STAT = '1' OR POLARITY_STAT = '2')," +
                                    "WINDERDIR_STAT CHAR(1) CHECK(WINDERDIR_STAT = '0' OR WINDERDIR_STAT = '1' OR WINDERDIR_STAT = '2')," +
                                    "PRODUCT_CNT NUMBER," +
                                    "FINALLOC_NO NVARCHAR2(64)," +
                                    "INNERTYPE_GB NUMBER," +
                                    "PALLET_GB NUMBER," +
                                    "TRAYSTACK_CNT NUMBER," +
                                    "TRAYTYPE_STAT CHAR(1) CHECK(TRAYTYPE_STAT = '0' OR TRAYTYPE_STAT = '1' OR TRAYTYPE_STAT = '2' OR TRAYTYPE_STAT = '3')," +
                                    "UNCOATED_STAT CHAR(1) CHECK(UNCOATED_STAT = '0' OR UNCOATED_STAT = '1' OR UNCOATED_STAT = '2')," +
                                    "CORETYPE_STAT CHAR(1) CHECK(CORETYPE_STAT = '0' OR CORETYPE_STAT = '1' OR CORETYPE_STAT = '2')," +
                                    "VALIDATION_ID NVARCHAR2(2)," +
                                    "PRODUCTEND_STAT CHAR(1) CHECK(PRODUCTEND_STAT = '0' OR PRODUCTEND_STAT = '1' OR PRODUCTEND_STAT = '2')," +
                                    "CARRIER_CD NVARCHAR2(64)," +
                                    "CARRIER_GB NUMBER, " +
                                    "CARRIER_STAT CHAR(1) CHECK(CARRIER_STAT='0' OR CARRIER_STAT='1' OR CARRIER_STAT='2' OR CARRIER_STAT='3' OR CARRIER_STAT='4' OR CARRIER_STAT='5')," +
                                    "LOT_ID NVARCHAR2(64)," +
                                    "FIRSTLOT_ID NVARCHAR2(64)," +
                                    "SECONDLOT_ID NVARCHAR2(64)," +
                                    "THIRDLOT_ID NVARCHAR2(64)," +
                                    "FOURTHLOT_ID NVARCHAR2(64)," +
                                    "FIFTHLOT_ID NVARCHAR2(64)," +
                                    "SIXTHLOT_ID NVARCHAR2(64)," +
                                    "CARRYIN_DTTM TIMESTAMP," +
                                    "CARRYOUT_DTTM TIMESTAMP," +
                                    "CARRIERID_STAT CHAR(1) CHECK(CARRIERID_STAT='0' OR CARRIERID_STAT='1' OR CARRIERID_STAT='2' OR CARRIERID_STAT='3' OR CARRIERID_STAT='4')," +
                                    "CARRIERHEIGHT_STAT CHAR(1) CHECK(CARRIERHEIGHT_STAT='0' OR CARRIERHEIGHT_STAT='1'))");     //230207 캐리어하이트 추가
                                                                                                                                //220628 조숭진 playback을 위한 것
                                                                                                                                //"DEL_FLAG CHAR(1) CHECK(DEL_FLAG='0' OR DEL_FLAG='1'))");


                            cmd = new OracleCommand(sql, conn);
                            cmd.ExecuteNonQuery();

                            LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                        }
                    }

                    try
                    {
                        if (!string.IsNullOrEmpty(result))
                        {
                            result = string.Empty;
                        }

                        sql = String.Format("SELECT SCS_CD FROM TB_USER_INFO");
                        cmd = new OracleCommand(sql, conn);
                        result = (string)cmd.ExecuteScalar();

                        temp = string.Format("DB Table Find");

                        LogManager.WriteDBLog(eLogLevel.Info, temp, false);
                    }
                    catch (Exception)
                    {
                        LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                        LogManager.WriteDBLog(eLogLevel.Info, "TB_USER_INFO Table Empty", false);

                        if (string.IsNullOrEmpty(result))
                        {
                            //231030 유저네임 추가
                            {
                                sql = string.Format("CREATE TABLE TB_USER_INFO (" +
                                        "SCS_CD NVARCHAR2(64) NOT NULL," +
                                        "USER_ID NVARCHAR2(64)," +
                                        "PASSWORD_GB NVARCHAR2(128)," +
                                        "GROUP_GB NUMBER," +
                                        "USE_STAT CHAR(1) CHECK(USE_STAT = '1' OR USE_STAT = '0')," +
                                        "USING_DTTM NUMBER," +
                                        "CONSTRAINT TB_USER_PK1 PRIMARY KEY(USER_ID))");
                            }
                            cmd = new OracleCommand(sql, conn);
                            cmd.ExecuteNonQuery();

                            LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                        }
                    }

                    try
                    {
                        if (!string.IsNullOrEmpty(result))
                            result = string.Empty;

                        sql = String.Format("SELECT ALARM_ID FROM TB_ALARM_INFO");
                        cmd = new OracleCommand(sql, conn);
                        rowdata = cmd.ExecuteReader();

                        temp = string.Format("DB Table Find");

                        LogManager.WriteDBLog(eLogLevel.Info, temp, false);
                    }
                    catch (Exception)
                    {
                        LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                        LogManager.WriteDBLog(eLogLevel.Info, "TB_ALARM_INFO Table Empty", false);

                        if (string.IsNullOrEmpty(result))
                        {
                            sql = string.Format("CREATE TABLE TB_ALARM_INFO (" +
                                    "ALARM_ID NUMBER NOT NULL," +                                                      //알람 id
                                    "ALARM_NM NVARCHAR2(80)," +                                                              //알람 name
                                    "LIGHTALARM_GB CHAR(1) CHECK(LIGHTALARM_GB = '1' OR LIGHTALARM_GB = '0')," +            //경알람 구분
                                    "MODULE_NM NVARCHAR2(64)," +                                                              //알람모듈 name
                                    "DESC_NM_KOR NVARCHAR2(1000)," +                                                              //알람 설명 //한글
                                    "DESC_NM_ENG NVARCHAR2(1000)," +                                                                          //영어
                                    "DESC_NM_CHN NVARCHAR2(1000)," +                                                                          //중국어
                                    "DESC_NM_HUN NVARCHAR2(1000)," +                                                                          //헝가리어
                                    "SOLUTION_NM_KOR NVARCHAR2(1000)," +                                                          //알람 조치방법       //한글
                                    "SOLUTION_NM_ENG NVARCHAR2(1000)," +                                                                              //영어
                                    "SOLUTION_NM_CHN NVARCHAR2(1000)," +                                                                              //중국어
                                    "SOLUTION_NM_HUN NVARCHAR2(1000)," +                                                                              //헝가리어
                                    "CONSTRAINT TB_ALARM_PK1 PRIMARY KEY(ALARM_ID))");

                            cmd = new OracleCommand(sql, conn);
                            cmd.ExecuteNonQuery();

                            LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                        }
                    }

                    //221012 조숭진 pio db s
                    try
                    {
                        if (!string.IsNullOrEmpty(result))
                            result = string.Empty;

                        sql = String.Format("SELECT SCS_CD FROM TB_PIO_INFO");
                        cmd = new OracleCommand(sql, conn);
                        result = (string)cmd.ExecuteScalar();

                        temp = string.Format("DB Table Find");

                        LogManager.WriteDBLog(eLogLevel.Info, temp, false);
                    }
                    catch (Exception)
                    {
                        LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                        LogManager.WriteDBLog(eLogLevel.Info, "TB_PIO_INFO Table Empty", false);

                        if (string.IsNullOrEmpty(result))
                        {
                            sql = string.Format("CREATE TABLE TB_PIO_INFO (" +
                                    "SCS_CD NVARCHAR2(64) NOT NULL," +
                                    "MODULE_NM NVARCHAR2(64)," +
                                    "DIRECTION_GB NVARCHAR2(10)," +
                                    "DATA_VAL NVARCHAR2(2000))");

                            cmd = new OracleCommand(sql, conn);
                            cmd.ExecuteNonQuery();

                            LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                        }
                    }
                    //221012 조숭진 pio db e

                    try
                    {
                        if (!string.IsNullOrEmpty(result))
                            result = string.Empty;

                        sql = String.Format("SELECT SCS_CD FROM TB_UNITED_LOG_INFO");
                        cmd = new OracleCommand(sql, conn);
                        result = (string)cmd.ExecuteScalar();

                        temp = string.Format("DB Table Find");

                        LogManager.WriteDBLog(eLogLevel.Info, temp, false);
                    }
                    catch (Exception)
                    {
                        LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                        LogManager.WriteDBLog(eLogLevel.Info, "TB_UNITED_LOG_INFO Table Empty", false);

                        if (string.IsNullOrEmpty(result))
                        {
                            sql = string.Format("CREATE TABLE TB_UNITED_LOG_INFO (" +
                                    "SCS_CD NVARCHAR2(64) NOT NULL," +
                                    "LOG_NM NVARCHAR2(64) NOT NULL," +
                                    "RECODE_DTTM NVARCHAR2(255)," +
                                    "COL_1 NVARCHAR2(255)," +
                                    "COL_2 NVARCHAR2(255)," +
                                    "COL_3 NVARCHAR2(255)," +
                                    "COL_4 NVARCHAR2(255)," +
                                    "COL_5 NVARCHAR2(255)," +
                                    "COL_6 NVARCHAR2(255)," +
                                    "COL_7 NVARCHAR2(255)," +
                                    "COL_8 NVARCHAR2(255)," +
                                    "COL_9 NVARCHAR2(255)," +
                                    "COL_10 NVARCHAR2(255)," +
                                    "COL_11 NVARCHAR2(255)," +
                                    "COL_12 NVARCHAR2(255)," +
                                    "COL_13 NVARCHAR2(255)," +
                                    "HIST_SECS2 CLOB)");

                            cmd = new OracleCommand(sql, conn);
                            cmd.ExecuteNonQuery();

                            LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                        }
                    }

                    try
                    {
                        if (!string.IsNullOrEmpty(result))
                            result = string.Empty;

                        sql = String.Format("SELECT SCS_CD FROM TB_CLIENT_ORDER");
                        cmd = new OracleCommand(sql, conn);
                        result = (string)cmd.ExecuteScalar();

                        temp = string.Format("DB Table Find");

                        LogManager.WriteDBLog(eLogLevel.Info, temp, false);
                    }
                    catch (Exception)
                    {
                        LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                        LogManager.WriteDBLog(eLogLevel.Info, "TB_CLIENT_ORDER Table Empty", false);

                        if (string.IsNullOrEmpty(result))
                        {
                            sql = string.Format("CREATE TABLE TB_CLIENT_ORDER (" +
                                    "SCS_CD NVARCHAR2(64) NOT NULL," +          //대상 server eqpid
                                    "CMD_GB NVARCHAR2(64)," +                   //명령 : insert, delete, update 등등..
                                    "TARGET_CD NVARCHAR2(64)," +                //대상 대분류 : shelf, cv, crane, job 등등
                                    "TARGET_ID NVARCHAR2(64)," +                //대상 소분류 : 대분류의 unit id, job이면 job id
                                    "TARGET_VALUE NVARCHAR2(1000)," +             //변경 값
                                    "CREATE_DTTM NVARCHAR2(64)," +
                                    "REQUESTER_GB CHAR(1) CHECK(REQUESTER_GB='1' OR REQUESTER_GB='0')," +
                                    "JOB_ID NVARCHAR2(64)," +
                                    "CLIENT_CD NVARCHAR2(64))");             //client가 요청한 시간

                            cmd = new OracleCommand(sql, conn);
                            cmd.ExecuteNonQuery();

                            LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                        }
                    }

                    try
                    {
                        if (!string.IsNullOrEmpty(result))
                            result = string.Empty;

                        sql = String.Format("SELECT EQP_ID FROM TB_EQP_INFO");
                        cmd = new OracleCommand(sql, conn);
                        result = (string)cmd.ExecuteScalar();

                        temp = string.Format("DB Table Find");
                        LogManager.WriteDBLog(eLogLevel.Info, temp, false);
                    }
                    catch (Exception)
                    {
                        LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                        LogManager.WriteDBLog(eLogLevel.Info, "TB_EQP_INFO Table Empty", false);

                        if (string.IsNullOrEmpty(result))
                        {
                            sql = string.Format("CREATE TABLE TB_EQP_INFO (" +
                                    "EQP_NM NVARCHAR2(64) NOT NULL," +          //대상 server eqpid
                                    "EQP_ID NVARCHAR2(64) NOT NULL," +                   //명령 : insert, delete, update 등등..
                                    "EQP_NO NVARCHAR2(10) NOT NULL," +                //대상 대분류 : shelf, cv, crane, job 등등
                                    "IP_NO NVARCHAR2(20) NOT NULL, " +
                                    "DBFIRSTIP_NO NVARCHAR2(20) NOT NULL," +
                                    "DBFIRSTPORT_NO NVARCHAR2(20) NOT NULL," +
                                    "DBFIRSTSERVICE_NM NVARCHAR2(20) NOT NULL," +
                                    "DBSECONDIP_NO NVARCHAR2(20) NOT NULL," +
                                    "DBSECONDPORT_NO NVARCHAR2(20) NOT NULL," +
                                    "DBSECONDSERVICE_NM NVARCHAR2(20) NOT NULL," +
                                    "DBACCOUNT_NM NVARCHAR2(64) NOT NULL," +
                                    "DBPASSWORD_GB NVARCHAR2(64) NOT NULL," +
                                    "MCS_STAT CHAR(1) CHECK(MCS_STAT='0' OR MCS_STAT='1' OR MCS_STAT='2')," +                //대상 소분류 : 대분류의 unit id, job이면 job id
                                    "SCS_STAT CHAR(1) CHECK(SCS_STAT='0' OR SCS_STAT='1')," +             //변경 값
                                    "PLC_STAT CHAR(1) CHECK(PLC_STAT='0' OR PLC_STAT='1')," +
                                    "SYSTEM_STAT CHAR(1) CHECK(SYSTEM_STAT='0' OR SYSTEM_STAT='1' OR SYSTEM_STAT='2' OR SYSTEM_STAT='3' OR SYSTEM_STAT='4'))");             //client가 요청한 시간

                            cmd = new OracleCommand(sql, conn);
                            cmd.ExecuteNonQuery();

                            LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                        }
                    }

                    try
                    {
                        if (!string.IsNullOrEmpty(result))
                            result = string.Empty;

                        sql = String.Format("SELECT SCS_CD FROM TB_HIST_TERMINAL_MSG");
                        cmd = new OracleCommand(sql, conn);
                        result = (string)cmd.ExecuteScalar();

                        temp = string.Format("DB Table Find");
                        LogManager.WriteDBLog(eLogLevel.Info, temp, false);
                    }
                    catch (Exception)
                    {
                        LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                        LogManager.WriteDBLog(eLogLevel.Info, "TB_HIST_TERMINAL_MSG Table Empty", false);

                        if (string.IsNullOrEmpty(result))
                        {
                            sql = string.Format("CREATE TABLE TB_HIST_TERMINAL_MSG (" +
                                "SCS_CD NVARCHAR2(64) NOT NULL," +
                                "TID_ID NVARCHAR2(64)," +
                                "TCODE_CD NVARCHAR2(100)," +
                                "TEXT_NM NVARCHAR2(100) NOT NULL," +
                                "EVENTTIME_DTTM NVARCHAR2(64))");

                            cmd = new OracleCommand(sql, conn);
                            cmd.ExecuteNonQuery();

                            LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                        }
                    }

                    try
                    {
                        if (!string.IsNullOrEmpty(result))
                            result = string.Empty;

                        sql = String.Format("SELECT CLIENT_CD FROM TB_CONNECT_CLIENT_INFO");
                        cmd = new OracleCommand(sql, conn);
                        result = (string)cmd.ExecuteScalar();

                        temp = string.Format("DB Table Find");
                        LogManager.WriteDBLog(eLogLevel.Info, temp, false);
                    }
                    catch (Exception)
                    {
                        LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                        LogManager.WriteDBLog(eLogLevel.Info, "TB_CONNECT_CLIENT_INFO Table Empty", false);

                        if (string.IsNullOrEmpty(result))
                        {
                            sql = string.Format("CREATE TABLE TB_CONNECT_CLIENT_INFO (" +
                                "SCS_CD NVARCHAR2(64) NOT NULL," +
                                "CLIENT_CD NVARCHAR2(64) NOT NULL," +
                                "CLIENT_IP NVARCHAR2(64) NOT NULL," +
                                "CONSTRAINT TB_CONNECT_CLIENT_PK1 PRIMARY KEY(CLIENT_CD))");

                            cmd = new OracleCommand(sql, conn);
                            cmd.ExecuteNonQuery();

                            LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                        }
                    }

                    try
                    {
                        if (!string.IsNullOrEmpty(result))
                            result = string.Empty;

                        sql = String.Format("SELECT PLC_CD FROM TB_PLC_INFO");
                        cmd = new OracleCommand(sql, conn);
                        result = (string)cmd.ExecuteScalar();

                        temp = string.Format("DB Table Find");
                        LogManager.WriteDBLog(eLogLevel.Info, temp, false);
                    }
                    catch (Exception)
                    {
                        LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                        LogManager.WriteDBLog(eLogLevel.Info, "TB_PLC_INFO Table Empty", false);

                        if (string.IsNullOrEmpty(result))
                        {
                            sql = string.Format("CREATE TABLE TB_PLC_INFO (" +
                                "PLC_NO NUMBER," +
                                "SCS_CD NVARCHAR2(64) NOT NULL," +
                                "PLC_CD NVARCHAR2(64) NOT NULL," +
                                "PLC_IP NVARCHAR2(64) NOT NULL," +
                                "PLC_STAT NVARCHAR2(64)," +
                                "EVENTTIME_DTTM NVARCHAR2(64))");

                            cmd = new OracleCommand(sql, conn);
                            cmd.ExecuteNonQuery();

                            LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                        }
                    }


                    string name = string.Empty;
                    bool check = IsCheckProcedure(conn, out name);
                    if (!check)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Procedure is not existed", name);
                        dbopenstate = false;
                        return;
                    }

                    check = IsCheckFunction(conn, out name);
                    if (!check)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "{0} Function is not existed.", name);
                        dbopenstate = false;
                        return;
                    }

                    Thread th = new Thread(new ThreadStart(GatherClientReqCommand));
                    th.IsBackground = true;
                    th.Name = "OracleManagerServerDebug";
                    th.Start();
                }

                dbopenstate = true;
                IsConnect = true;

                LogList = new DataTable();

                LogList.Columns.Add("SCS_CD");
                LogList.Columns.Add("LOG_NM");
                LogList.Columns.Add("RECODE_DTTM");
                LogList.Columns.Add("COL_1");
                LogList.Columns.Add("COL_2");
                LogList.Columns.Add("COL_3");
                LogList.Columns.Add("COL_4");
                LogList.Columns.Add("COL_5");
                LogList.Columns.Add("COL_6");
                LogList.Columns.Add("COL_7");
                LogList.Columns.Add("COL_8");
                LogList.Columns.Add("COL_9");
                LogList.Columns.Add("COL_10");
                LogList.Columns.Add("COL_11");
                LogList.Columns.Add("COL_12");
                LogList.Columns.Add("COL_13");
                LogList.Columns.Add("HIST_SECS2");
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
            finally
            {
                conn.Close();

                if (rowdata != null)
                {
                    rowdata.Close(); //240503 OracleDataReader 는 사용후 close 필요함.
                    rowdata.Dispose();
                }
                if (cmd != null)
                    cmd?.Dispose();

                if (conn != null)
                    conn?.Dispose();
            }
        }
        public virtual void MapChangeForExitThread() { }

        private void GatherClientReqCommand()
        {
            List<ClientReqList> ReqList = new List<ClientReqList>();
            //GlobalData.Current.MRE_GlobalDataCreatedEvent.WaitOne(); //모든 모듈 생성전까지 Run 대기
            GlobalData.Current.MRE_MapViewChangeEvent.WaitOne();
            while (true)
            {
                //if (GlobalData.Current.LayoutLoadComp == false)
                //    continue;

                try
                {
                    Thread.Sleep(PollingPeriod);

                    DbGetProcedureConnectClientInfo();

                    ReqList = DbGetProcedureClientReq(eServerClientType.Client);

                    for (int i = 0; i < ReqList.Count; i++)
                    {
                        CarrierItem ci = new CarrierItem();
                        try //20240718 RGJ 클라이언트 요청 처리중 예외발생하면 요청만 계속 시도되므로 예외가 나도 요청은 삭제되도록 다시 예외처리함. 
                        {
                            switch (ReqList[i].Target.ToUpper())
                            {
                                case "ALARM":
                                    switch (ReqList[i].CMDType.ToUpper())
                                    {
                                        //case "SET":
                                        //    GlobalData.Current.Alarm_Manager.AlarmOccur(ReqList[i].TargetID, ReqList[i].Target);
                                        //    break;

                                        case "CLEAR":
                                            //GlobalData.Current.Alarm_Manager.AlarmClear(ReqList[i].Target, ReqList[i].TargetID);
                                            GlobalData.Current.Alarm_Manager.RequestAlarmClear(ReqList[i].TargetID, ReqList[i].TargetValue, ReqList[i].JobID);
                                            break;

                                        case "REFRESH":
                                            GlobalData.Current.Alarm_Manager.RefreshAllAlarmList(eServerClientType.Server);
                                            break;
                                    }
                                    break;
                                //230222 조숭진 클라이언트가 시스템상태변경 요청 s
                                case "BOOTH":
                                    switch (ReqList[i].CMDType.ToUpper())
                                    {
                                        case "SYSTEMSTATE":
                                            if (ReqList[i].TargetValue.ToUpper() == "AUTO")
                                            {
                                                GlobalData.Current.MainBooth.SCSResumeCommand();
                                            }
                                            else
                                            {
                                                GlobalData.Current.MainBooth.SCSPauseCommand();
                                            }
                                            break;
                                        case "MCSSTATE":
                                            eOnlineState onlineState = (eOnlineState)Enum.Parse(typeof(eOnlineState), ReqList[i].TargetValue);

                                            if (onlineState == eOnlineState.Remote) //리모트 
                                            {
                                                GlobalData.Current.MainBooth.CurrentOnlineState = eOnlineState.Remote;
                                            }
                                            else
                                            {
                                                GlobalData.Current.MainBooth.CurrentOnlineState = eOnlineState.Offline_EQ;
                                            }
                                            break;

                                        case "SCSSTATE":
                                            if (ReqList[i].TargetValue.ToUpper() == "START")
                                            {
                                                GlobalData.Current.HSMS.Start();
                                            }
                                            else
                                            {
                                                GlobalData.Current.HSMS.Stop();
                                            }
                                            break;

                                        default:
                                            break;
                                    }
                                    break;
                                //230222 조숭진 클라이언트가 시스템상태변경 요청 e
                                case "SHELF":
                                    ShelfItem shelfitem = GlobalData.Current.ShelfMgr.GetShelf(ReqList[i].TargetID);

                                    switch (ReqList[i].CMDType.ToUpper())
                                    {
                                        case "INSTALL":
                                            ci = JsonConvert.DeserializeObject<CarrierItem>(ReqList[i].TargetValue);
                                            if (ci != null) //240308 RGJ 오라클 DBManager null Check 추가
                                            {
                                                ci.CarrierState = eCarrierState.COMPLETED;
                                                ShelfManager.Instance.GenerateCarrierRequest(shelfitem.TagName, ci);
                                            }
                                            break;

                                        case "DELETE":
                                            ShelfManager.Instance.RequestCarrierRemove(shelfitem.TagName);
                                            break;

                                        case "ENABLE":
                                            shelfitem.SHELFUSE = true;

                                            GlobalData.Current.ShelfMgr.SaveShelfData(shelfitem);//SuHwan_20230202 : [ServerClient]
                                            break;

                                        case "DISABLE":
                                            shelfitem.SHELFUSE = false;
                                            GlobalData.Current.ShelfMgr.SaveShelfData(shelfitem);//SuHwan_20230202 : [ServerClient]
                                            break;

                                        //230207 해당 type이 없어지고 short, long, both 로 구분됨.
                                        //case "TYPE":
                                        //    bool value = Enum.TryParse(ReqList[i].TargetValue, out eShelfType outValue);
                                        //    if (value)
                                        //        shelfitem.SHELFTYPE = (int)outValue;
                                        //    break;

                                        case "SHORTMODE":
                                            shelfitem.ShelfType = eShelfType.Short;
                                            GlobalData.Current.ShelfMgr.SaveShelfData(shelfitem);//SuHwan_20230202 : [ServerClient]
                                            break;

                                        case "LONGMODE":
                                            shelfitem.ShelfType = eShelfType.Long;
                                            GlobalData.Current.ShelfMgr.SaveShelfData(shelfitem);//SuHwan_20230202 : [ServerClient]
                                            break;

                                        case "BOTHMODE":
                                            shelfitem.ShelfType = eShelfType.Both;
                                            GlobalData.Current.ShelfMgr.SaveShelfData(shelfitem);//SuHwan_20230202 : [ServerClient]
                                            break;

                                        //230404 ui 캐리어 사이즈 변경대응
                                        case "CARRIERSIZE":
                                            ci = JsonConvert.DeserializeObject<CarrierItem>(ReqList[i].TargetValue);
                                            if (ci != null) //240308 RGJ 오라클 DBManager null Check 추가
                                            {
                                                Enum.TryParse(ReqList[i].CMDType, out eCarrierSize carriersize);
                                                shelfitem.InSlotCarrier.CarrierSize = carriersize;
                                                shelfitem.UpdateCarrier(ci.CarrierID, true, false);
                                                //shelfitem.NotifyShelfStatusChanged();
                                            }
                                            break;

                                        case "PALLETSIZE":
                                            ci = JsonConvert.DeserializeObject<CarrierItem>(ReqList[i].TargetValue);
                                            if (ci != null)
                                            {
                                                shelfitem.InSlotCarrier.PalletSize = ci.PalletSize;
                                                shelfitem.UpdateCarrier(ci.CarrierID, true, false);
                                            }
                                            break;
                                        case "PRODUCTEMPTY":
                                            ci = JsonConvert.DeserializeObject<CarrierItem>(ReqList[i].TargetValue);
                                            if (ci != null)
                                            {
                                                shelfitem.InSlotCarrier.ProductEmpty = ci.ProductEmpty;
                                                shelfitem.UpdateCarrier(ci.CarrierID, true, false);
                                            }
                                            break;

                                        //230404 ui 캐리어 아이디 변경대응
                                        case "CARRIERID":
                                            //240805 RGJ SHELF 에서 캐리어 아이디만 바꾸는건 허용안함.

                                            //ci = JsonConvert.DeserializeObject<CarrierItem>(ReqList[i].TargetValue);
                                            //CarrierStorage.Instance.InsertCarrier(ci);
                                            //shelfitem.UpdateCarrier(ci.CarrierID, true, true);

                                            //if (ci != null)
                                            //{
                                            //    string beforeCarrierID = shelfitem.CarrierID;
                                            //    eCarrierState beforestate = shelfitem.CarrierState;

                                            //    //CarrierStorage.Instance.InsertCarrier(ci);
                                            //    //ci.CarrierState = beforestate;
                                            //    //shelfitem.UpdateCarrier(ci.CarrierID, true, true);
                                            //    //2024.05.30 lim, OY 소스 머지
                                            //    shelfitem.CarrierID = ci.CarrierID;
                                            //    shelfitem.InSlotCarrier.CarrierID = ci.CarrierID.ToUpper();
                                            //    CarrierStorage.Instance.InsertCarrier(shelfitem.InSlotCarrier);
                                            //    shelfitem.UpdateCarrier(shelfitem.CarrierID, true, true);
                                            //    CarrierStorage.Instance.RemoveStorageCarrier(beforeCarrierID);
                                            //    shelfitem.InSlotCarrier.CarrierState = beforestate;
                                            //    //shelfitem.NotifyShelfStatusChanged();
                                            //}
                                            break;

                                        case "INFORM":
                                            shelfitem.ShelfMemo = ReqList[i].TargetValue;
                                            break;

                                        case "DEADZONE":
                                            //bool value = (bool)Enum.Parse(typeof(bool), ReqList[i].TargetValue);

                                            if (bool.TryParse(ReqList[i].TargetValue, out bool value))
                                            {
                                                shelfitem.DeadZone = value;
                                                GlobalData.Current.ShelfMgr.SaveShelfData(shelfitem);
                                            }
                                            break;
                                    }
                                    break;

                                case "CV":
                                    if (GlobalData.Current.PortManager.CheckCVModuleName(ReqList[i].TargetID))
                                    {
                                        CV_BaseModule selcv = GlobalData.Current.PortManager.GetCVModule(ReqList[i].TargetID);
                                        CVLineModule SelectedCVLine = selcv.ParentModule as CVLineModule;

                                        switch (ReqList[i].CMDType.ToUpper())
                                        {
                                            case "ACCESSAGV":
                                                //selcv.SetPortAccessMode(ePortAceessMode.AUTO);
                                                SetPortAccessMode(selcv, ePortAceessMode.AUTO);
                                                break;

                                            case "ACCESSOPER":
                                                //selcv.SetPortAccessMode(ePortAceessMode.MANUAL);
                                                SetPortAccessMode(selcv, ePortAceessMode.MANUAL);
                                                break;

                                            case "ENABLE":
                                                //SelectedCVLine.ChangeAllPortUseType(true);
                                                setAllPortUseType(SelectedCVLine, true);
                                                break;

                                            case "DISABLE":
                                                //SelectedCVLine.ChangeAllPortUseType(false);
                                                setAllPortUseType(SelectedCVLine, false);
                                                break;

                                            case "DIRECTIONINMODE":
                                                //SelectedCVLine.ChangeAllPortInOutType(ePortInOutType.INPUT);
                                                setPortType(ePortInOutType.INPUT, SelectedCVLine);
                                                break;

                                            case "DIRECTIONOUTMODE":
                                                //SelectedCVLine.ChangeAllPortInOutType(ePortInOutType.OUTPUT);
                                                setPortType(ePortInOutType.OUTPUT, SelectedCVLine);
                                                break;
                                            //230207 DIRECTIONBOTHMODE 추가
                                            case "DIRECTIONBOTHMODE":
                                                //SelectedCVLine.ChangeAllPortInOutType(ePortInOutType.BOTH);
                                                setPortType(ePortInOutType.BOTH, SelectedCVLine);
                                                break;
                                            case "DELETE":
                                                if (selcv.IsTerminalPort && selcv.PortInOutType == ePortInOutType.OUTPUT)
                                                {
                                                    selcv.RemoveSCSCarrierData(); //도메인에서 지우는건 끝단이고 배출 포트일때만 삭제해야함.
                                                }
                                                else
                                                {
                                                    selcv.ResetCarrierData();
                                                }
                                                break;

                                            case "INSTALL":
                                                ci = JsonConvert.DeserializeObject<CarrierItem>(ReqList[i].TargetValue);
                                                if (ci != null)
                                                {
                                                    ci.CarrierState = eCarrierState.WAIT_IN;
                                                    ShelfManager.Instance.GenerateCarrierRequest(selcv.ControlName, ci);
                                                }
                                                break;
                                            case "KEYIN":  //사실상 INSTALL 을 KEYIN 취급한다
                                                CarrierItem cikeyin = JsonConvert.DeserializeObject<CarrierItem>(ReqList[i].TargetValue);
                                                if (cikeyin != null)
                                                {
                                                    cikeyin.CarrierState = eCarrierState.WAIT_IN;
                                                    selcv.SetKeyInCarrierItem(cikeyin);
                                                }

                                                break;
                                            //230207 BCRREAD 추가
                                            case "BCRREAD":
                                                selcv.CVBCR_Read();
                                                break;

                                            case "MANUALRUN":
                                                //selcv.SetAutoMode(eCVAutoManualState.ManualRun);
                                                SetAutoMode(selcv, eCVAutoManualState.ManualRun);
                                                break;
                                            case "AUTORUN":
                                                //selcv.SetAutoMode(eCVAutoManualState.AutoRun);
                                                SetAutoMode(selcv, eCVAutoManualState.AutoRun);
                                                break;

                                            case "TRACKPAUSE":
                                                SetTrackPause(selcv, true);
                                                break;

                                            case "TRACKRESUME":
                                                SetTrackPause(selcv, false);
                                                break;
                                        }
                                    }
                                    break;

                                case "CRANE":
                                    //230207 추가 s
                                    //if (ReqList[i].TargetID.ToUpper() == "C01" || ReqList[i].TargetID.ToUpper() == "C02")       //230207 수정
                                    //{
                                    //RMModuleBase SelectedRM = (ReqList[i].TargetID == "C01") ? GlobalData.Current.mRMManager.FirstRM : GlobalData.Current.mRMManager.SecondRM;
                                    RMModuleBase SelectedRM = (ReqList[i].TargetID == GlobalData.Current.mRMManager.FirstRM.ModuleName) ? GlobalData.Current.mRMManager.FirstRM : GlobalData.Current.mRMManager.SecondRM;

                                    switch (ReqList[i].CMDType.ToUpper())
                                    {
                                        case "DELETE":
                                            ci = JsonConvert.DeserializeObject<CarrierItem>(ReqList[i].TargetValue);
                                            SelectedRM.ResetCarrierData();
                                            if (ci != null) //240308 RGJ 오라클 DBManager null Check 추가
                                            {
                                                CarrierStorage.Instance.RemoveStorageCarrier(ci.CarrierID); //STK Domain 에서 캐리어 제거.
                                            }
                                            break;
                                        case "EMERGENCYSTOP":
                                            SelectedRM.RMEMG_STOP_Request();
                                            break;
                                        case "ERRORRESET":
                                            SelectedRM.RMReset_Request();
                                            break;
                                        case "HOME":
                                            SelectedRM.RMHome_Request();
                                            break;
                                        case "INSTALL":
                                            ci = JsonConvert.DeserializeObject<CarrierItem>(ReqList[i].TargetValue);
                                            if (ci != null) //240308 RGJ 오라클 DBManager null Check 추가
                                            {
                                                ci.CarrierState = eCarrierState.COMPLETED;
                                                SelectedRM.InsertCarrier(ci);
                                            }
                                            break;
                                        case "STOP":
                                            //SelectedRM.RMPause_Request(); //기존 크레인 대신 PLC Pause
                                            GlobalData.Current.MainBooth.PausePLCAction();
                                            break;
                                        case "ACTIVE":
                                            //SelectedRM.RMResume_Request(); //기존 크레인 대신 PLC Resume
                                            GlobalData.Current.MainBooth.ResumePLCAction();
                                            break;
                                    }
                                    //230207 추가 e
                                    //}
                                    break;

                                case "JOB":
                                    if (!string.IsNullOrEmpty(ReqList[i].TargetID) &&
                                        GlobalData.Current.McdList.IsCommandIDContain(ReqList[i].TargetID))
                                    {
                                        var targetJob = GlobalData.Current.McdList.GetCommandIDJob(ReqList[i].TargetID);

                                        if (targetJob != null)
                                        {
                                            switch (ReqList[i].CMDType.ToUpper())
                                            {
                                                case "DELETE"://240507  RGJ클라이언트 요청이 왔다고 그냥 지우면 안된다.
                                                    GlobalData.Current.McdList.ProcessUIJobRemoveRequest(targetJob);
                                                    //GlobalData.Current.McdList.DeleteMcsJob(targetJob, false, true);//SuHwan_20230202
                                                    break;

                                                case "PRIORITYCHANGE":
                                                    int value = Convert.ToInt32(ReqList[i].TargetValue);
                                                    if (value > 0)
                                                        GlobalData.Current.McdList.ChangePriority(targetJob, value);
                                                    break;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        switch (ReqList[i].CMDType.ToUpper())
                                        {
                                            case "MJOBCREATE":
                                                if (!string.IsNullOrEmpty(ReqList[i].TargetValue))
                                                {
                                                    GlobalData.Current.McdList.CreateManualJob_formDB(ReqList[i].TargetValue);
                                                }
                                                break;
                                        }
                                    }
                                    break;

                                case "MANUALJOB":
                                    //230222 조숭진 오토가 아닐때는 넘겨버린다.
                                    if (GlobalData.Current.MainBooth.SCState != eSCState.AUTO)
                                        continue;

                                    RMModuleBase rmbase = (ReqList[i].TargetID == GlobalData.Current.mRMManager.FirstRM.ModuleName) ? GlobalData.Current.mRMManager.FirstRM : GlobalData.Current.mRMManager.SecondRM;
                                    Enum.TryParse(ReqList[i].CMDType, out eCraneCommand CMD);
                                    string CarrierID = string.Empty;

                                    if (CMD == eCraneCommand.MOVE)
                                        CarrierID = "ManualMove";

                                    bool TargetIsPort = true;

                                    ICarrierStoreAble target = GlobalData.Current.PortManager.GetCVModule(ReqList[i].TargetValue);
                                    if (target == null)
                                    {
                                        TargetIsPort = false;
                                        target = ShelfManager.Instance.GetShelf(ReqList[i].TargetValue);
                                    }

                                    CraneCommand mCmd = new CraneCommand("ManualCom", rmbase.ModuleName, CMD, TargetIsPort ? enumCraneTarget.PORT : enumCraneTarget.SHELF, target, CarrierID);
                                    if (!rmbase.CheckRMCommandExist())
                                    {
                                        rmbase.SetCraneCommand(mCmd);
                                    }
                                    break;

                                //230301 클라이언트에서 io변경 요청대응
                                case "IO":
                                    PLCDataItem pItem = JsonConvert.DeserializeObject<PLCDataItem>(ReqList[i].TargetValue);

                                    if (pItem != null) //240308 RGJ 오라클 DBManager null Check 추가
                                    {
                                        object ReceiveValue = new object();

                                        switch (pItem.ModuleType)
                                        {
                                            case "CRANE":
                                                RMModuleBase RMBase = (ReqList[i].TargetID == GlobalData.Current.mRMManager.FirstRM.ModuleName) ? GlobalData.Current.mRMManager.FirstRM : GlobalData.Current.mRMManager.SecondRM;
                                                if (pItem.DataType == eDataType.Bool)
                                                {
                                                    bool value = false;
                                                    bool.TryParse(ReqList[i].CMDType, out value);
                                                    ReceiveValue = value;
                                                }
                                                else
                                                {
                                                    ReceiveValue = ReqList[i].CMDType;
                                                }
                                                //20240718 RGJ 클라이언트 I/O 요청 로그 추가.
                                                LogManager.WriteConsoleLog(eLogLevel.Info, "Client Crane I/O Set Request IOName : {0} Address :{1} RawValue : {2}", pItem.ItemName, pItem.ItemPLCAddress, ReceiveValue);
                                                GlobalData.Current.protocolManager.Write(ReqList[i].TargetID, RMBase.PCtoPLC, pItem.ItemName, ReceiveValue);

                                                //230406 변경하고 db도 변경해야한다.
                                                if (!string.IsNullOrEmpty(ReqList[i].JobID))
                                                {
                                                    if (pItem.ItemName.Equals("PLC_PalletSize"))
                                                    {
                                                        Enum.TryParse(ReceiveValue.ToString(), out ePalletSize palletsize);
                                                        var rmCarrier = RMBase.InSlotCarrier;
                                                        if (rmCarrier != null) //20230914 RGJ IO Set Null 체크 추가.
                                                        {
                                                            RMBase.InSlotCarrier.PalletSize = palletsize;
                                                            RMBase.UpdateCarrier(RMBase.CarrierID);
                                                        }
                                                    }
                                                    //캐리어 아이디..
                                                    else
                                                    {
                                                        var carrierItem = CarrierStorage.Instance.GetInModuleCarrierItem(RMBase.ModuleName);
                                                        if (carrierItem != null) //230914 RGJ IO Set Null 체크 추가.
                                                        {
                                                            string beforeCarrierID = carrierItem.CarrierID;
                                                            eCarrierState beforestate = carrierItem.CarrierState;
                                                            carrierItem.CarrierID = ReceiveValue.ToString();

                                                            bool insertcheck = CarrierStorage.Instance.InsertCarrier(carrierItem);
                                                            if (insertcheck)
                                                            {
                                                                RMBase.UpdateCarrier(ReceiveValue.ToString());
                                                                CarrierStorage.Instance.RemoveStorageCarrier(beforeCarrierID);
                                                                carrierItem.CarrierState = beforestate;
                                                            }
                                                        }
                                                    }
                                                }

                                                break;

                                            case "BOOTH":
                                                if (pItem.DataType == eDataType.Bool)
                                                {
                                                    bool value = false;
                                                    bool.TryParse(ReqList[i].CMDType, out value);
                                                    ReceiveValue = value;
                                                }
                                                else
                                                {
                                                    ReceiveValue = ReqList[i].CMDType;
                                                }
                                                //20240718 RGJ 클라이언트 I/O 요청 로그 추가.
                                                LogManager.WriteConsoleLog(eLogLevel.Info, "Client Booth I/O Set Request IOName : {0} Address :{1} RawValue : {2}", pItem.ItemName, pItem.ItemPLCAddress, ReceiveValue);
                                                GlobalData.Current.protocolManager.Write(ReqList[i].TargetID, GlobalData.Current.MainBooth.PCtoPLC, pItem.ItemName, ReceiveValue);
                                                break;

                                            case "CV":
                                                CV_BaseModule selcv = GlobalData.Current.PortManager.GetCVModule(ReqList[i].TargetID);

                                                if (pItem.DataType == eDataType.Bool)
                                                {
                                                    bool value = false;
                                                    bool.TryParse(ReqList[i].CMDType, out value);
                                                    ReceiveValue = value;
                                                }
                                                else
                                                {
                                                    ReceiveValue = ReqList[i].CMDType;
                                                }
                                                //20240718 RGJ 클라이언트 I/O 요청 로그 추가.
                                                LogManager.WriteConsoleLog(eLogLevel.Info, "Client Port I/O Set Request IOName : {0} Address :{1} RawValue : {2}", pItem.ItemName, pItem.ItemPLCAddress, ReceiveValue);
                                                GlobalData.Current.protocolManager.Write(ReqList[i].TargetID, selcv.PCtoPLC, pItem.ItemName, ReceiveValue);

                                                //230406 변경하고 db도 변경해야한다.
                                                if (!string.IsNullOrEmpty(ReqList[i].JobID))
                                                {
                                                    if (pItem.ItemName.Equals("PC_CarrierID"))
                                                    {
                                                        //var carrierItem = CarrierStorage.Instance.GetInModuleCarrierItem(selcv.ModuleName);

                                                        //string beforeCarrierID = carrierItem.CarrierID;
                                                        //carrierItem.CarrierID = ReceiveValue.ToString();

                                                        //bool InsertCheck = CarrierStorage.Instance.InsertCarrier(carrierItem);
                                                        //if (InsertCheck)
                                                        //{
                                                        //    CarrierStorage.Instance.RemoveStorageCarrier(beforeCarrierID);
                                                        //    selcv.UpdateCarrier(ReceiveValue.ToString());
                                                        //}
                                                        var carrierItem = CarrierStorage.Instance.GetInModuleCarrierItem(selcv.ModuleName);
                                                        if (carrierItem != null) //240716 RGJ 예외발생하여 널체크 추가
                                                        {
                                                            eCarrierState beforestate = carrierItem.CarrierState;
                                                            string beforeCarrierID = carrierItem.CarrierID;
                                                            carrierItem.CarrierID = ReceiveValue.ToString();

                                                            bool insertcheck = CarrierStorage.Instance.InsertCarrier(carrierItem);
                                                            if (insertcheck)
                                                            {
                                                                selcv.UpdateCarrier(ReceiveValue.ToString());
                                                                CarrierStorage.Instance.RemoveStorageCarrier(beforeCarrierID);
                                                                carrierItem.CarrierState = beforestate;
                                                            }
                                                        }


                                                    }
                                                    else
                                                    {
                                                        var cvCarrier = selcv.InSlotCarrier;
                                                        if (cvCarrier != null) //230914 RGJ IO Set Null 체크 추가.
                                                        {
                                                            string data = ReceiveValue.ToString();
                                                            switch (pItem.ItemName)
                                                            {
                                                                case "PC_PalletSize":
                                                                    Enum.TryParse(data, out ePalletSize palletsize);
                                                                    selcv.InSlotCarrier.PalletSize = palletsize;
                                                                    break;
                                                                case "PC_ProductEmpty":
                                                                    Enum.TryParse(data, out eProductEmpty productempty);
                                                                    selcv.InSlotCarrier.ProductEmpty = productempty;
                                                                    break;
                                                                case "PC_Polarity":
                                                                    Enum.TryParse(data, out ePolarity polarity);
                                                                    selcv.InSlotCarrier.Polarity = polarity;
                                                                    break;
                                                                case "PC_WinderDirection":
                                                                    Enum.TryParse(data, out eWinderDirection winderdirection);
                                                                    //carrierItem.WinderDirection = winderdirection;
                                                                    selcv.InSlotCarrier.WinderDirection = winderdirection;
                                                                    break;
                                                                case "PC_InnerTrayType":
                                                                    Enum.TryParse(data, out eInnerTrayType innertraytype);
                                                                    //carrierItem.InnerTrayType = innertraytype;
                                                                    selcv.InSlotCarrier.InnerTrayType = innertraytype;
                                                                    break;
                                                                case "PC_TrayType":
                                                                    Enum.TryParse(data, out eTrayType traytype);
                                                                    //carrierItem.TrayType = traytype;
                                                                    selcv.InSlotCarrier.TrayType = traytype;
                                                                    break;
                                                            }
                                                            selcv.UpdateCarrier(selcv.GetCarrierID());
                                                        }
                                                    }
                                                }
                                                break;
                                        }
                                    }
                                    break;

                                case "USER":
                                    User tempuser = JsonConvert.DeserializeObject<User>(ReqList[i].TargetValue);
                                    if (tempuser != null) //240308 RGJ 오라클 DBManager null Check 추가
                                    {
                                        switch (ReqList[i].CMDType.ToUpper())
                                        {
                                            case "ADD":
                                                GlobalData.Current.UserMng.AddUser(tempuser);
                                                break;

                                            case "DELETE":
                                                GlobalData.Current.UserMng.DeleteUser(tempuser);
                                                break;

                                            case "UPDATE":
                                                GlobalData.Current.UserMng.UpdateUser(tempuser);
                                                break;
                                        }
                                    }
                                    break;
                                case "CARRIER":
                                    switch (ReqList[i].CMDType.ToUpper())
                                    {
                                        case "MREMOVECARRIER":
                                            if (!string.IsNullOrEmpty(ReqList[i].TargetID))
                                            {
                                                CarrierStorage.Instance.RemoveStorageCarrier(ReqList[i].TargetID);
                                            }
                                            break;
                                        case "MCHANGECLOC":
                                            eCarrierLocationChangeResult result = CarrierStorage.Instance.ChangeCarrierLocation(ReqList[i].TargetID, ReqList[i].TargetValue);
                                            if(result == eCarrierLocationChangeResult.SUCCESS)
                                            {
                                                LogManager.WriteConsoleLog(eLogLevel.Info, "Client Req  Carrier : {0} Location => {1} Changed Successful.", ReqList[i].TargetID, ReqList[i].TargetValue);
                                            }
                                            else
                                            {
                                                LogManager.WriteConsoleLog(eLogLevel.Info, "Client Req  Carrier : {0} Location => {1} Changed failed : {2} ", ReqList[i].TargetID, ReqList[i].TargetValue, result);
                                            }
                                            break;
                                    }
                                    break;
                            }
                        }
                        catch(Exception SwitchEx)
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Exception Occurred While Executing Client Order!");
                            LogManager.WriteConsoleLog(eLogLevel.Info, SwitchEx.ToString());
                        }


                        //230222 조숭진 오토가 아닐때는 넘겨버린다.
                        if (!(GlobalData.Current.MainBooth.SCState != eSCState.AUTO && ReqList[i].Target.ToUpper() == "MANUALJOB"))
                        {
                            DbSetProcedureClientReq(GlobalData.Current.EQPID, ReqList[i].CMDType, ReqList[i].Target, ReqList[i].TargetID,
                                ReqList[i].TargetValue, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), ReqList[i].Requester, true, ReqList[i].JobID, ReqList[i].ClientID);
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                }
            }
        }

        public virtual void CreateShelfInfo(ShelfItemList shelfinfo)
        {
            lock (DBLock)
            {
                try
                {
                    foreach (var item in shelfinfo)
                    {
                        //220401 HHJ SCS 개선     //- Xml, DB 혼용에 따른 초기 생성 불가능 현상 조치
                        //DbSetShelfInfo(item);
                        //DbSetShelfInfo(item, false);
                        DbSetProcedureShelfInfo(item);
                    }

                }
                catch (Exception ex)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                }
            }
        }

        #region DbSetShelfInfo 사용 안함
        //220401 HHJ SCS 개선     //- Xml, DB 혼용에 따른 초기 생성 불가능 현상 조치
        //public void DbSetShelfInfo(ShelfItem shelfitem)
        public virtual void DbSetShelfInfo(ShelfItem shelfitem, bool bUpdate = true)
        {
            lock (DBLock)
            {
                OracleConnection conn = null;
                OracleTransaction tran = null;
                OracleCommand sqlcmd = null;
                string sql = string.Empty;
                OracleDataReader rowdata = null;
                string temptag = string.Empty;      //20220510 조숭진 tagname 재조합

                conn = new OracleConnection(OracleDBPath);
                conn.Open();

                tran = conn.BeginTransaction();

                //220524 HHJ SCS 개선     //- Shelf Xml제거
                //220331 HHJ SCS UI 기능 추가       //- ShelfColor 변경 추가
                //UI Itemssource는 해당 Data의 리스트 항목자체가 변경되어야 이벤트 처리가 된다.
                //리스트내 변수 변경으로는 이벤트 처리가 불가능하기에 DB Set전에 리스트 항목 변경을 해준다.
                //if (bUpdate)
                //{
                //    bool bvalue = shelfitem.ShelfBank == (int)eShelfBank.Front ?
                //        GlobalData.Current.MainBooth.FrontData.UpdateShelfItem(shelfitem) :
                //        GlobalData.Current.MainBooth.RearData.UpdateShelfItem(shelfitem);
                //}

                try
                {

                    //동일한 컬럼 조회
                    //20220503 조숭진 sk db사양 준수
                    sql = string.Format("SELECT SCS_CD, CARRIER_LOC, ZONE_NM, EXIST_GB, CARRIER_ID, USE_STAT, SHELFSIZE_GB, STATUS_GB, DEADZONE_STAT" +
                        " FROM TB_SHELF_INFO WHERE SCS_CD='{0}' AND CARRIER_LOC='{1}'", GlobalData.Current.EQPID, shelfitem.TagName);

                    //sql = string.Format("SELECT * FROM SCS_TOTAL_TABLE WHERE SCSID='{0}' AND SHELFNUM='{1}'", GlobalData.Current.EQPID, shelfitem.TagName);
                    sqlcmd = new OracleCommand(sql, conn);
                    rowdata = sqlcmd.ExecuteReader();
                    bool exist = rowdata.HasRows;
                    char shelfuse, deadzone;

                    LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                    LogManager.WriteDBLog(eLogLevel.Info, "Data Exist = {0}" + exist.ToString(), false);

                    if (rowdata.Read())
                    {
                        DataTable schematable = rowdata.GetSchemaTable();

                        string datavalue = string.Empty;
                        foreach (DataRow row in schematable.Rows)
                        {
                            foreach (DataColumn column in schematable.Columns)
                            {
                                string temp = row[column].ToString() + "=" + rowdata[row[column].ToString()].ToString() + ",";
                                datavalue += temp;
                                break;
                            }
                        }
                        LogManager.WriteDBLog(eLogLevel.Info, datavalue, false);
                    }

                    if (shelfitem.SHELFUSE)
                        shelfuse = '1';
                    else
                        shelfuse = '0';

                    if (shelfitem.DeadZone)
                        deadzone = '1';
                    else
                        deadzone = '0';

                    if (exist == true)
                    {
                        sql = string.Format("UPDATE TB_SHELF_INFO SET EXIST_GB='{0}'," +
                            "CARRIER_ID='{1}', USE_STAT='{2}', SHELFSIZE_GB='{3}', STATUS_GB='{4}' WHERE CARRIER_LOC = '{5}'",
                            shelfitem.CheckCarrierExist() ? 1 : 0,
                            shelfitem.CarrierID,
                            shelfuse,
                            shelfitem.SHELFTYPE,
                            shelfitem.RUNSTATE,
                            shelfitem.TagName);
                    }
                    else
                    {
                        sql = string.Format("INSERT INTO TB_SHELF_INFO(SCS_CD, CARRIER_LOC, ZONE_NM, EXIST_GB," +
                            "CARRIER_ID, USE_STAT, SHELFSIZE_GB, STATUS_GB, DEADZONE_STAT) VALUES " +
                            "('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}')",
                            GlobalData.Current.EQPID,
                            shelfitem.TagName,
                            shelfitem.ZONE,
                            shelfitem.CheckCarrierExist() ? 1 : 0,
                            shelfitem.CarrierID,
                            shelfuse,
                            shelfitem.SHELFTYPE,
                            shelfitem.RUNSTATE,
                            deadzone);
                    }

                    sqlcmd = new OracleCommand(sql, conn);
                    sqlcmd.Transaction = tran;
                    sqlcmd.ExecuteNonQuery();

                    LogManager.WriteDBLog(eLogLevel.Info, sql, true);

                    sqlcmd.Transaction.Commit();
                }
                catch (Exception ex)
                {
                    sqlcmd.Transaction.Rollback();
                    LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                }
                finally
                {
                    conn.Close();

                    sqlcmd?.Dispose();
                    rowdata?.Dispose();
                    tran?.Dispose();
                    conn?.Dispose();
                }
            }
        }
        #endregion

        public virtual void DbSetProcedureShelfInfo(ShelfItem shelfitem, bool GlobalInit = false)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        using (OracleCommand sqlcmd = new OracleCommand())
                        {
                            try
                            {
                                sqlcmd.CommandText = "USP_STC_SHELF_INFO_SET";
                                sqlcmd.CommandType = CommandType.StoredProcedure;
                                sqlcmd.Connection = conn;
                                //DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                                //DateTimeForOracle(shelfitem.InstallTime, out int installyear, out int installmonth,
                                //    out int installday, out int installhour, out int installminute, out int installsecond, out int installmillisecond);
                                OracleParameter R_RESULT = new OracleParameter("R_RESULT", OracleDbType.NVarchar2, 20);
                                OracleParameter R_TEMP = new OracleParameter("R_TEMP", OracleDbType.NVarchar2, 20);

                                sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2, ParameterDirection.Input).Value = GlobalData.Current.EQPID;
                                sqlcmd.Parameters.Add("P_CARRIER_LOC", OracleDbType.NVarchar2, ParameterDirection.Input).Value = shelfitem.TagName;
                                sqlcmd.Parameters.Add("P_ZONE_NM", OracleDbType.NVarchar2, ParameterDirection.Input).Value = shelfitem.ZONE;
                                sqlcmd.Parameters.Add("P_EXIST_GB", OracleDbType.Int32, ParameterDirection.Input).Value = shelfitem.CheckCarrierExist() ? 1 : 0;
                                sqlcmd.Parameters.Add("P_CARRIER_ID", OracleDbType.NVarchar2, ParameterDirection.Input).Value = shelfitem.CarrierID;
                                sqlcmd.Parameters.Add("P_USE_STAT", OracleDbType.Char, ParameterDirection.Input).Value = shelfitem.SHELFUSE ? '1' : '0';
                                sqlcmd.Parameters.Add("P_SHELFSIZE_GB", OracleDbType.Int32, ParameterDirection.Input).Value = shelfitem.SHELFTYPE;
                                sqlcmd.Parameters.Add("P_STATUS_GB", OracleDbType.Int32, ParameterDirection.Input).Value = Convert.ToInt32(shelfitem.ShelfStatus);
                                sqlcmd.Parameters.Add("P_DEADZONE_STAT", OracleDbType.Char, ParameterDirection.Input).Value = shelfitem.DeadZone ? '1' : '0';
                                if (GlobalInit)
                                {
                                    DateTimeForOracle(shelfitem.InstallTime, out int installyear, out int installmonth,
                                        out int installday, out int installhour, out int installminute, out int installsecond, out int installmillisecond);
                                    sqlcmd.Parameters.Add("P_INSTALL_DTTM", OracleDbType.TimeStamp, new DateTime(installyear, installmonth, installday, installhour,
                                        installminute, installsecond, installmillisecond), ParameterDirection.Input);
                                }
                                else
                                {
                                    DateTimeForOracle(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), out int installyear, out int installmonth,
                                        out int installday, out int installhour, out int installminute, out int installsecond, out int installmillisecond);
                                    sqlcmd.Parameters.Add("P_INSTALL_DTTM", OracleDbType.TimeStamp, new DateTime(installyear, installmonth, installday, installhour,
                                        installminute, installsecond, installmillisecond), ParameterDirection.Input);
                                }
                                sqlcmd.Parameters.Add("P_FLOOR_NM", OracleDbType.Int32, ParameterDirection.Input).Value = shelfitem.FloorNum;       //221228 조숭진 층수추가
                                sqlcmd.Parameters.Add("P_MEMO_GB", OracleDbType.NVarchar2, ParameterDirection.Input).Value = shelfitem.ShelfMemo;
                                sqlcmd.Parameters.Add("P_FIRE_GB", OracleDbType.Int32, ParameterDirection.Input).Value = shelfitem.FireSensorValue ? 1 : 0; //240820 RGJ 화재 상태 클라이언트 표시 추가.

                                sqlcmd.Parameters.Add(R_RESULT).Direction = ParameterDirection.Output;
                                sqlcmd.Parameters.Add(R_TEMP).Direction = ParameterDirection.Output;

                                sqlcmd.ExecuteNonQuery();

                                string temp = string.Format("Add Result = {0}", R_RESULT.Value.ToString());
                                string temp1 = string.Format("Add Temp = {0}", R_TEMP.Value.ToString());

                                //LogManager.WriteConsoleLog(eLogLevel.Info, temp1 + ", " + temp);
                                LogManager.WriteDBLog(eLogLevel.Info, temp1 + ", " + temp, false);

                                ////sqlcmd.CommandText = "USP_STC_SHELF_INFO_SET_TEST";       //220628 조숭진 playback을 위한 것
                                //sqlcmd.CommandText = "USP_STC_SHELF_INFO_SET_TEST2";
                                //sqlcmd.CommandType = CommandType.StoredProcedure;
                                //sqlcmd.Connection = conn;

                                ////220628 조숭진 playback을 위한 것
                                //////220624 조숭진 playback을 위해 job 변경 시 save 시간 기록
                                ////DateTimeForOracle(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), out int saveyear, out int savemonth, out int saveday,
                                ////    out int savehour, out int saveminute, out int savesecond, out int savemillisecond);

                                ////220705 조숭진 install time 추가
                                //DateTimeForOracle(shelfitem.InstallTime, out int installyear, out int installmonth,
                                //    out int installday, out int installhour, out int installminute, out int installsecond, out int installmillisecond);

                                //OracleParameter R_RESULT = new OracleParameter("R_RESULT", OracleDbType.NVarchar2, 20);
                                //OracleParameter R_TEMP = new OracleParameter("R_TEMP", OracleDbType.NVarchar2, 20);
                                //OracleParameter R_SHELFCONTENT = new OracleParameter("R_SHELFCONTENT", OracleDbType.NVarchar2, 1000);

                                //sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.Varchar2, ParameterDirection.Input).Value = GlobalData.Current.EQPID;
                                //sqlcmd.Parameters.Add("P_CARRIER_LOC", OracleDbType.Varchar2, ParameterDirection.Input).Value = shelfitem.TagName;
                                //sqlcmd.Parameters.Add("P_ZONE_NM", OracleDbType.Varchar2, ParameterDirection.Input).Value = shelfitem.ZONE;
                                //sqlcmd.Parameters.Add("P_EXIST_GB", OracleDbType.Int32, ParameterDirection.Input).Value = shelfitem.CheckCarrierExist() ? 1 : 0;
                                //sqlcmd.Parameters.Add("P_CARRIER_ID", OracleDbType.Varchar2, ParameterDirection.Input).Value = shelfitem.CarrierID;
                                ////sqlcmd.Parameters.Add("P_USE_STAT", OracleDbType.Char, ParameterDirection.Input).Value = Convert.ToInt32(shelfitem.SHELFUSE); //shelfitem.SHELFUSE ? '1' : '0';
                                //sqlcmd.Parameters.Add("P_USE_STAT", OracleDbType.Char, ParameterDirection.Input).Value = shelfitem.SHELFUSE ? '1' : '0';
                                //sqlcmd.Parameters.Add("P_SHELFSIZE_GB", OracleDbType.Int32, ParameterDirection.Input).Value = shelfitem.SHELFTYPE;
                                //sqlcmd.Parameters.Add("P_STATUS_GB", OracleDbType.Int32, ParameterDirection.Input).Value = Convert.ToInt32(shelfitem.ShelfStatus);
                                ////sqlcmd.Parameters.Add("P_DEADZONE_STAT", OracleDbType.Char, ParameterDirection.Input).Value = Convert.ToInt32(shelfitem.DeadZone); //shelfitem.DeadZone ? '1' : '0';
                                //sqlcmd.Parameters.Add("P_DEADZONE_STAT", OracleDbType.Char, ParameterDirection.Input).Value = shelfitem.DeadZone ? '1' : '0';
                                ////220705 조숭진 install time 추가
                                //sqlcmd.Parameters.Add("P_INSTALL_DTTM", OracleDbType.TimeStamp, new DateTime(installyear, installmonth, installday, installhour,
                                //    installminute, installsecond, installmillisecond), ParameterDirection.Input);

                                ////220628 조숭진 playback을 위한 것
                                ////sqlcmd.Parameters.Add("P_SAVE_DTTM", OracleDbType.TimeStamp, new DateTime(saveyear, savemonth, saveday, savehour, 
                                ////    saveminute, savesecond, savemillisecond), ParameterDirection.Input);
                                ////sqlcmd.Parameters.Add("P_SHELF_INIT", OracleDbType.Char, ParameterDirection.Input).Value = shelfinit ? '1' : '0';

                                //sqlcmd.Parameters.Add(R_RESULT).Direction = ParameterDirection.Output;
                                //sqlcmd.Parameters.Add(R_TEMP).Direction = ParameterDirection.Output;
                                //sqlcmd.Parameters.Add(R_SHELFCONTENT).Direction = ParameterDirection.Output;

                                //sqlcmd.ExecuteNonQuery();

                                //string temp = string.Format("Add Result = {0}", R_RESULT.Value.ToString());
                                //string temp1 = string.Format("Add Temp = {0}", R_TEMP.Value.ToString());

                                //LogManager.WriteConsoleLog(eLogLevel.Info, temp1 + ", " + temp);
                                //LogManager.WriteDBLog(eLogLevel.Info, R_SHELFCONTENT.Value.ToString(), true);
                                //LogManager.WriteDBLog(eLogLevel.Info, temp1 + ", " + temp, false);
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }

        #region DbSetJobInfo 사용 안함
        public virtual bool DbSetJobInfo(McsJob jobitem, bool del)
        {
            lock (DBLock)
            {
                OracleCommand sqlcmd = null;
                string sql = string.Empty;
                OracleDataReader rowdata = null;
                OracleConnection conn = null;
                OracleTransaction tran = null;

                try
                {
                    conn = new OracleConnection(OracleDBPath);
                    conn.Open();

                    tran = conn.BeginTransaction();

                    //20220503 조숭진 sk db사양 준수
                    sql = string.Format("SELECT SCS_CD, CREATE_DTTM, CARRIER_ID, SOUR_ID, DEST_ID, PRIORITY_ORDER, TRANSFER_NO, TRAYTYPE_GB, " +
                        "CMD_ID, JOBFROM_GB, TRSTATUS_GB, JOBTYPE_GB, ASSIGNRM_NM, JOBSTEP_NM" +
                        " FROM TB_JOB_INFO WHERE SCS_CD='{0}' AND CMD_ID='{1}'", GlobalData.Current.EQPID, jobitem.CommandID);
                    //sql = string.Format("SELECT * FROM SCS_TOTAL_TABLE WHERE SCSID='{0}' AND COMMANDID='{1}'", GlobalData.Current.EQPID, jobitem.CommandID);
                    sqlcmd = new OracleCommand(sql, conn);
                    rowdata = sqlcmd.ExecuteReader();
                    bool exist = rowdata.HasRows;

                    LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                    LogManager.WriteDBLog(eLogLevel.Info, "Data Exist = {0}" + exist.ToString(), false);

                    if (rowdata.Read())
                    {
                        DataTable schematable = rowdata.GetSchemaTable();

                        string datavalue = string.Empty;
                        foreach (DataRow row in schematable.Rows)
                        {
                            foreach (DataColumn column in schematable.Columns)
                            {
                                string temp = row[column].ToString() + "=" + rowdata[row[column].ToString()].ToString() + ",";
                                datavalue += temp;
                                break;
                            }
                        }
                        LogManager.WriteDBLog(eLogLevel.Info, datavalue, false);
                    }

                    if (exist && !del)              //이미 있는 command... update로 봐야할까?
                    {
                        sql = string.Format("UPDATE TB_JOB_INFO SET PRIORITY_ORDER='{0}', DEST_ID='{1}', TRANSFER_NO='{2}', ASSIGNRM_NM='{3}', JOBSTEP_NM='{4}'" +
                            " WHERE CMD_ID='{5}'AND SCS_CD='{6}'",
                            Convert.ToInt32(jobitem.Priority),
                            jobitem.Destination,
                            jobitem.TransferState,
                            jobitem.AssignRMName,
                            Convert.ToInt32(jobitem.Step),
                            jobitem.CommandID,
                            GlobalData.Current.EQPID);
                    }
                    //return false;
                    else if (exist && del)           //완료job 지우기
                    {
                        sql = string.Format("DELETE FROM TB_JOB_INFO WHERE SCS_CD='{0}' AND CMD_ID='{1}'", GlobalData.Current.EQPID, jobitem.CommandID);
                    }
                    else
                    {
                        sql = string.Format("INSERT INTO TB_JOB_INFO(SCS_CD, CREATE_DTTM, CARRIER_ID, SOUR_ID," +
                            "DEST_ID, PRIORITY_ORDER, TRANSFER_NO, TRAYTYPE_GB, CMD_ID, JOBFROM_GB, TRSTATUS_GB, JOBTYPE_GB, ASSIGNRM_NM, JOBSTEP_NM) " +
                            "VALUES ('{0}', {1}, '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', '{10}', '{11}', '{12}', '{13}')",
                            GlobalData.Current.EQPID,
                            DateTimeOracle(jobitem.CreateTime),
                            jobitem.CarrierID,
                            jobitem.Source,
                            jobitem.Destination,
                            Convert.ToInt32(jobitem.Priority),
                            jobitem.TransferState,
                            jobitem.CarrierType,
                            jobitem.CommandID,
                            Convert.ToInt32(jobitem.JobFrom),
                            jobitem.TCStatus.ToString(),
                            jobitem.JobType,
                            jobitem.AssignRMName,
                            Convert.ToInt32(jobitem.Step));
                    }

                    sqlcmd = new OracleCommand(sql, conn);
                    sqlcmd.Transaction = tran;
                    sqlcmd.ExecuteNonQuery();

                    sqlcmd.Transaction.Commit();

                    LogManager.WriteDBLog(eLogLevel.Info, sql, true);

                }
                catch (Exception ex)
                {
                    sqlcmd.Transaction.Rollback();
                    LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                }
                finally
                {
                    conn.Close();

                    sqlcmd?.Dispose();
                    rowdata?.Dispose();
                    tran?.Dispose();
                    conn?.Dispose();
                }
                return true;
            }
        }
        #endregion

        public virtual bool DbSetProcedureJobInfo(McsJob jobitem, bool del)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        using (OracleCommand sqlcmd = new OracleCommand())
                        {
                            try
                            {
                                DateTimeForOracle(jobitem.CreateTime, out int inyear, out int inmonth, out int inday, out int inhour, out int inminute,
                                    out int insecond, out int inmillisecond);

                                {
                                    sqlcmd.CommandText = "USP_STC_JOB_INFO_SET";
                                }
                                sqlcmd.CommandType = CommandType.StoredProcedure;
                                sqlcmd.Connection = conn;

                                OracleParameter R_RESULT = new OracleParameter("R_RESULT", OracleDbType.NVarchar2, 1000);
                                OracleParameter R_TEMP = new OracleParameter("R_TEMP", OracleDbType.NVarchar2, 1000);

                                sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2, ParameterDirection.Input).Value = GlobalData.Current.EQPID;
                                sqlcmd.Parameters.Add("P_CREATE_DTTM", OracleDbType.TimeStamp,
                                    new DateTime(inyear, inmonth, inday, inhour, inminute, insecond, inmillisecond), ParameterDirection.Input);
                                sqlcmd.Parameters.Add("P_CMD_ID", OracleDbType.NVarchar2, ParameterDirection.Input).Value = jobitem.CommandID;
                                sqlcmd.Parameters.Add("P_CARRIER_ID", OracleDbType.NVarchar2, ParameterDirection.Input).Value = jobitem.CarrierID;
                                sqlcmd.Parameters.Add("P_SOUR_ID", OracleDbType.NVarchar2, ParameterDirection.Input).Value = jobitem.Source;
                                sqlcmd.Parameters.Add("P_DEST_ID", OracleDbType.NVarchar2, ParameterDirection.Input).Value = jobitem.Destination;
                                sqlcmd.Parameters.Add("P_PRIORITY_ORDER", OracleDbType.Int32, ParameterDirection.Input).Value = Convert.ToInt32(jobitem.Priority);
                                sqlcmd.Parameters.Add("P_TRANSFER_NO", OracleDbType.Int32, ParameterDirection.Input).Value = jobitem.TransferState;
                                sqlcmd.Parameters.Add("P_TRAYTYPE_GB", OracleDbType.NVarchar2, ParameterDirection.Input).Value = jobitem.CarrierType;
                                sqlcmd.Parameters.Add("P_TRSTATUS_GB", OracleDbType.NVarchar2, ParameterDirection.Input).Value = jobitem.TCStatus.ToString();
                                sqlcmd.Parameters.Add("P_JOBFROM_GB", OracleDbType.Int32, ParameterDirection.Input).Value = Convert.ToInt32(jobitem.JobFrom);
                                sqlcmd.Parameters.Add("P_JOBTYPE_GB", OracleDbType.NVarchar2, ParameterDirection.Input).Value = jobitem.JobType;
                                sqlcmd.Parameters.Add("P_ASSIGNRM_NM", OracleDbType.NVarchar2, ParameterDirection.Input).Value = jobitem.AssignRMName;
                                sqlcmd.Parameters.Add("P_JOBSTEP_NM", OracleDbType.Int32, ParameterDirection.Input).Value = Convert.ToInt32(jobitem.Step);
                                sqlcmd.Parameters.Add("P_SUBJOBTYPE_NM", OracleDbType.Int32, ParameterDirection.Input).Value = Convert.ToInt32(jobitem.SubJob);
                                sqlcmd.Parameters.Add("P_DEL", OracleDbType.Char, ParameterDirection.Input).Value = del ? '1' : '0';
                                sqlcmd.Parameters.Add("P_SOURZONE_NM", OracleDbType.NVarchar2, ParameterDirection.Input).Value = jobitem.SourceZoneName;
                                sqlcmd.Parameters.Add("P_DESTZONE_NM", OracleDbType.NVarchar2, ParameterDirection.Input).Value = jobitem.DestZoneName;

                                sqlcmd.Parameters.Add(R_RESULT).Direction = ParameterDirection.Output;
                                sqlcmd.Parameters.Add(R_TEMP).Direction = ParameterDirection.Output;

                                sqlcmd.ExecuteNonQuery();

                                string temp = string.Format("Add Result = {0}", R_RESULT.Value.ToString());
                                string temp1 = string.Format("Add Temp = {0}", R_TEMP.Value.ToString());

                                //LogManager.WriteConsoleLog(eLogLevel.Info, temp1 + ", " + temp);
                                LogManager.WriteDBLog(eLogLevel.Info, temp1 + ", " + temp, false);

                                //DateTimeForOracle(jobitem.CreateTime, out int inyear, out int inmonth, out int inday, out int inhour, out int inminute,
                                //    out int insecond, out int inmillisecond);

                                //sqlcmd.CommandText = "USP_STC_JOB_INFO_SET_TEST2";
                                //sqlcmd.CommandType = CommandType.StoredProcedure;
                                //sqlcmd.Connection = conn;

                                //OracleParameter R_RESULT = new OracleParameter("R_RESULT", OracleDbType.NVarchar2, 20);
                                //OracleParameter R_TEMP = new OracleParameter("R_TEMP", OracleDbType.NVarchar2, 20);
                                //OracleParameter R_JOBCONTENT = new OracleParameter("R_JOBCONTENT", OracleDbType.NVarchar2, 1000);

                                //sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.Varchar2, ParameterDirection.Input).Value = GlobalData.Current.EQPID;
                                //sqlcmd.Parameters.Add("P_CREATE_DTTM", OracleDbType.TimeStamp,
                                //    new DateTime(inyear, inmonth, inday, inhour, inminute, insecond, inmillisecond), ParameterDirection.Input);
                                //sqlcmd.Parameters.Add("P_CMD_ID", OracleDbType.Varchar2, ParameterDirection.Input).Value = jobitem.CommandID;
                                //sqlcmd.Parameters.Add("P_CARRIER_ID", OracleDbType.Varchar2, ParameterDirection.Input).Value = jobitem.CarrierID;
                                //sqlcmd.Parameters.Add("P_SOUR_ID", OracleDbType.Varchar2, ParameterDirection.Input).Value = jobitem.Source;
                                //sqlcmd.Parameters.Add("P_DEST_ID", OracleDbType.Varchar2, ParameterDirection.Input).Value = jobitem.Destination;
                                //sqlcmd.Parameters.Add("P_PRIORITY_ORDER", OracleDbType.Int32, ParameterDirection.Input).Value = Convert.ToInt32(jobitem.Priority);
                                //sqlcmd.Parameters.Add("P_TRANSFER_NO", OracleDbType.Int32, ParameterDirection.Input).Value = jobitem.TransferState;
                                //sqlcmd.Parameters.Add("P_TRAYTYPE_GB", OracleDbType.Varchar2, ParameterDirection.Input).Value = jobitem.CarrierType;
                                //sqlcmd.Parameters.Add("P_TRSTATUS_GB", OracleDbType.Varchar2, ParameterDirection.Input).Value = jobitem.TCStatus.ToString();
                                //sqlcmd.Parameters.Add("P_JOBFROM_GB", OracleDbType.Int32, ParameterDirection.Input).Value = Convert.ToInt32(jobitem.JobFrom);
                                //sqlcmd.Parameters.Add("P_JOBTYPE_GB", OracleDbType.Varchar2, ParameterDirection.Input).Value = jobitem.JobType;
                                //sqlcmd.Parameters.Add("P_ASSIGNRM_NM", OracleDbType.Varchar2, ParameterDirection.Input).Value = jobitem.AssignRMName;
                                //sqlcmd.Parameters.Add("P_JOBSTEP_NM", OracleDbType.Int32, ParameterDirection.Input).Value = Convert.ToInt32(jobitem.Step);
                                //sqlcmd.Parameters.Add("P_SUBJOBTYPE_NM", OracleDbType.Int32, ParameterDirection.Input).Value = Convert.ToInt32(jobitem.SubJob);
                                //sqlcmd.Parameters.Add("P_DEL", OracleDbType.Char, ParameterDirection.Input).Value = del ? '1' : '0';

                                //sqlcmd.Parameters.Add(R_RESULT).Direction = ParameterDirection.Output;
                                //sqlcmd.Parameters.Add(R_TEMP).Direction = ParameterDirection.Output;
                                //sqlcmd.Parameters.Add(R_JOBCONTENT).Direction = ParameterDirection.Output;

                                //sqlcmd.ExecuteNonQuery();

                                //string temp = string.Format("Add Result = {0}", R_RESULT.Value.ToString());
                                //string temp1 = string.Format("Add Temp = {0}", R_TEMP.Value.ToString());

                                //LogManager.WriteConsoleLog(eLogLevel.Info, temp1 + ", " + temp);
                                //LogManager.WriteDBLog(eLogLevel.Info, R_JOBCONTENT.Value.ToString(), true);
                                //LogManager.WriteDBLog(eLogLevel.Info, temp1 + ", " + temp, false);
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {

                ////221017 조숭진 db 접속 이중화용 테스트 s
                //string excode = ex.HResult.ToString("X");

                //if (excode == "80004005" && DbConnectDuplexing(OracleDBPath))
                //{
                //    DbSetProcedureJobInfo(jobitem, del);
                //}
                //else if (excode == "80004005" && !DbConnectDuplexing(OracleDBPath))
                //    IsConnect = false;
                ////221017 조숭진 db 접속 이중화용 테스트 e

                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return false;
            }

            return true;
        }

        public bool DbSetProcedureAlarmInfo(AlarmData alarm, bool del, string target)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        using (OracleCommand sqlcmd = new OracleCommand())
                        {
                            try
                            {
                                sqlcmd.CommandText = "USP_STC_ALARM_INFO_SET";
                                sqlcmd.CommandType = CommandType.StoredProcedure;
                                sqlcmd.Connection = conn;

                                OracleParameter R_RESULT = new OracleParameter("R_RESULT", OracleDbType.NVarchar2, 20);
                                OracleParameter R_TEMP = new OracleParameter("R_TEMP", OracleDbType.NVarchar2, 20);

                                sqlcmd.Parameters.Add("P_ALARM_ID", OracleDbType.Int32, ParameterDirection.Input).Value = Convert.ToInt32(alarm.AlarmID);
                                sqlcmd.Parameters.Add("P_ALARM_NM", OracleDbType.NVarchar2, ParameterDirection.Input).Value = alarm.AlarmName;
                                sqlcmd.Parameters.Add("P_LIGHTALARM_GB", OracleDbType.Char, ParameterDirection.Input).Value = alarm.IsLightAlarm ? '1' : '0';
                                sqlcmd.Parameters.Add("P_MODULE_NM", OracleDbType.NVarchar2, ParameterDirection.Input).Value = alarm.ModuleType;
                                sqlcmd.Parameters.Add("P_DESC_NM_KOR", OracleDbType.NVarchar2, ParameterDirection.Input).Value = alarm.Description;
                                sqlcmd.Parameters.Add("P_DESC_NM_ENG", OracleDbType.NVarchar2, ParameterDirection.Input).Value = alarm.Description_ENG;
                                sqlcmd.Parameters.Add("P_DESC_NM_CHN", OracleDbType.NVarchar2, ParameterDirection.Input).Value = alarm.Description_CHN;
                                sqlcmd.Parameters.Add("P_DESC_NM_HUN", OracleDbType.NVarchar2, ParameterDirection.Input).Value = alarm.Description_HUN;
                                sqlcmd.Parameters.Add("P_SOLUTION_NM_KOR", OracleDbType.NVarchar2, ParameterDirection.Input).Value = alarm.Solution;
                                sqlcmd.Parameters.Add("P_SOLUTION_NM_ENG", OracleDbType.NVarchar2, ParameterDirection.Input).Value = alarm.Solution_ENG;
                                sqlcmd.Parameters.Add("P_SOLUTION_NM_CHN", OracleDbType.NVarchar2, ParameterDirection.Input).Value = alarm.Solution_CHN;
                                sqlcmd.Parameters.Add("P_SOLUTION_NM_HUN", OracleDbType.NVarchar2, ParameterDirection.Input).Value = alarm.Solution_HUN;
                                sqlcmd.Parameters.Add("P_DEL", OracleDbType.Char, ParameterDirection.Input).Value = del ? '1' : '0';
                                sqlcmd.Parameters.Add("P_TARGET", OracleDbType.NVarchar2, ParameterDirection.Input).Value = target;
                                sqlcmd.Parameters.Add(R_RESULT).Direction = ParameterDirection.Output;
                                sqlcmd.Parameters.Add(R_TEMP).Direction = ParameterDirection.Output;

                                sqlcmd.ExecuteNonQuery();

                                string temp = string.Format("Add Result = {0}", R_RESULT.Value.ToString());
                                string temp1 = string.Format("Add Temp = {0}", R_TEMP.Value.ToString());

                                //LogManager.WriteConsoleLog(eLogLevel.Info, temp1 + ", " + temp);
                                LogManager.WriteDBLog(eLogLevel.Info, temp1 + ", " + temp, false);
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return false;
            }
            return true;
        }

        #region GetDBAlarmInfo 사용 안함
        public BindingList<AlarmData> GetDBAlarmInfo()
        {
            BindingList<AlarmData> alarmlist = new BindingList<AlarmData>();

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        string sql = string.Format("SELECT ALARM_ID, ALARM_NM, LIGHTALARM_GB, MODULE_NM, DESC_NM_KOR, SOLUTION_NM_KOR " +
                            "FROM TB_ALARM_INFO ORDER BY ALARM_ID ASC");

                        using (OracleCommand sqlcmd = new OracleCommand(sql, conn))
                        {
                            try
                            {
                                using (OracleDataReader rowdata = sqlcmd.ExecuteReader())
                                {
                                    try
                                    {
                                        LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                                        LogManager.WriteDBLog(eLogLevel.Info, "Data Exist = {0}" + rowdata.HasRows.ToString(), false);

                                        while (rowdata.Read())
                                        {
                                            DataTable schematable = rowdata.GetSchemaTable();

                                            string datavalue = string.Empty;
                                            foreach (DataRow row in schematable.Rows)
                                            {
                                                foreach (DataColumn column in schematable.Columns)
                                                {
                                                    string temp = row[column].ToString() + "=" + rowdata[row[column].ToString()].ToString() + ",";
                                                    datavalue += temp;
                                                    break;
                                                }
                                            }

                                            AlarmData alarm = new AlarmData
                                            {
                                                AlarmID = rowdata["ALARM_ID"].ToString(),
                                                IsLightAlarm = rowdata["LIGHTALARM_GB"].ToString() == "1" ? true : false,
                                                ModuleType = rowdata["MODULE_NM"].ToString(),
                                                AlarmName = rowdata["ALARM_NM"].ToString(),
                                                Description = rowdata["DESC_NM_KOR"].ToString(),
                                                Description_ENG = rowdata["DESC_NM_ENG"].ToString(),
                                                Description_CHN = rowdata["DESC_NM_CHN"].ToString(),
                                                Description_HUN = rowdata["DESC_NM_HUN"].ToString(),
                                                Solution = rowdata["SOLUTION_NM_KOR"].ToString(),
                                                Solution_ENG = rowdata["SOLUTION_NM_ENG"].ToString(),
                                                Solution_CHN = rowdata["SOLUTION_NM_CHN"].ToString(),
                                                Solution_HUN = rowdata["SOLUTION_NM_HUN"].ToString()
                                            };

                                            alarmlist.Add(alarm);
                                        }
                                    }
                                    finally
                                    {
                                        if (rowdata != null)
                                        {
                                            rowdata.Close(); //240503 OracleDataReader 는 사용후 close 필요함.
                                            rowdata.Dispose();
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return alarmlist;
            }

            return alarmlist;
        }
        #endregion

        public ObservableList<AlarmData> DbGetProcedureAlarmInfo()
        {
            ObservableList<AlarmData> alarmlist = new ObservableList<AlarmData>();
            DataSet dataSet = new DataSet();

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        using (OracleCommand sqlcmd = new OracleCommand("UFN_STC_ALARM_INFO_GET", conn))
                        {
                            try
                            {
                                //sqlcmd.CommandText = "USP_STC_ALARM_INFO_GET";
                                sqlcmd.CommandType = CommandType.StoredProcedure;
                                //sqlcmd.Connection = conn;

                                //sqlcmd.Parameters.Add("TMP_DATA", OracleDbType.RefCursor, ParameterDirection.ReturnValue);
                                OracleParameter output = sqlcmd.Parameters.Add("TMP_DATA", OracleDbType.RefCursor);
                                output.Direction = ParameterDirection.ReturnValue;

                                sqlcmd.ExecuteNonQuery();

                                using (OracleDataAdapter oradata = new OracleDataAdapter(sqlcmd))
                                {
                                    try
                                    {
                                        oradata.Fill(dataSet);
                                    }
                                    finally
                                    {
                                        if (oradata != null)
                                            oradata.Dispose();
                                    }
                                }
                            }
                            finally
                            {
                                for (int i = 0; i < sqlcmd.Parameters.Count; i++)
                                {
                                    sqlcmd.Parameters[i].Dispose();
                                }

                                sqlcmd.Parameters.Clear();

                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }

                int table = dataSet.Tables.Count;
                for (int i = 0; i < table; i++)// set the table value in list one by one
                {
                    foreach (DataRow dr in dataSet.Tables[i].Rows)
                    {
                        AlarmData alarm = new AlarmData
                        {
                            AlarmID = dr["ALARM_ID"].ToString(),
                            AlarmName = dr["ALARM_NM"].ToString(),
                            IsLightAlarm = dr["LIGHTALARM_GB"].ToString() == "1" ? true : false,
                            ModuleType = dr["MODULE_NM"].ToString(),
                            Description = dr["DESC_NM_KOR"].ToString(),
                            Description_ENG = dr["DESC_NM_ENG"].ToString(),
                            Description_CHN = dr["DESC_NM_CHN"].ToString(),
                            Description_HUN = dr["DESC_NM_HUN"].ToString(),
                            Solution = dr["SOLUTION_NM_KOR"].ToString(),
                            Solution_ENG = dr["SOLUTION_NM_ENG"].ToString(),
                            Solution_CHN = dr["SOLUTION_NM_CHN"].ToString(),
                            Solution_HUN = dr["SOLUTION_NM_HUN"].ToString()
                        };

                        alarmlist.Add(alarm);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return alarmlist;
            }

            return alarmlist;
        }

        #region GetDBShelfInfo 사용 안함
        public ShelfItemList GetDBShelfInfo(string shelfbank)
        {
            string sql = string.Empty;
            ShelfItemList data = new ShelfItemList();

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        //220705 조숭진 install time 추가
                        sql = string.Format("SELECT SCS_CD, CARRIER_LOC, ZONE_NM, EXIST_GB, CARRIER_ID, USE_STAT, SHELFSIZE_GB, STATUS_GB, DEADZONE_STAT, INSTALL_DTTM" +
                            " FROM TB_SHELF_INFO WHERE CARRIER_LOC LIKE '{0}%' AND SCS_CD = '{1}' ORDER BY CARRIER_LOC ASC", shelfbank, GlobalData.Current.EQPID);

                        using (OracleCommand sqlcmd = new OracleCommand(sql, conn))
                        {
                            try
                            {
                                using (OracleDataReader rowdata = sqlcmd.ExecuteReader())
                                {
                                    try
                                    {
                                        LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                                        LogManager.WriteDBLog(eLogLevel.Info, "Data Exist = {0}" + rowdata.HasRows.ToString(), false);

                                        while (rowdata.Read())
                                        {
                                            DataTable schematable = rowdata.GetSchemaTable();

                                            string datavalue = string.Empty;
                                            foreach (DataRow row in schematable.Rows)
                                            {
                                                foreach (DataColumn column in schematable.Columns)
                                                {
                                                    string temp = row[column].ToString() + "=" + rowdata[row[column].ToString()].ToString() + ",";
                                                    datavalue += temp;
                                                    break;
                                                }
                                            }

                                            string tagname = rowdata["CARRIER_LOC"].ToString();

                                            LogManager.WriteDBLog(eLogLevel.Info, datavalue, false);

                                            ShelfItem shelfItem = new ShelfItem(tagname)
                                            {
                                                DeadZone = Convert.ToBoolean(Convert.ToInt32(rowdata["DEADZONE_STAT"])),
                                                //TagName = rowdata["CARRIER_LOC"].ToString(),
                                                ZONE = rowdata["ZONE_NM"].ToString(),
                                                //220525 HHJ SCS 개선     //- ShelfItem 개선
                                                //CarrierID = string.IsNullOrEmpty(rowdata["CARRIER_ID"].ToString()) ? string.Empty : rowdata["CARRIER_ID"].ToString(),
                                                SHELFUSE = Convert.ToBoolean(Convert.ToInt32(rowdata["USE_STAT"])),
                                                SHELFTYPE = Convert.ToInt32(rowdata["SHELFSIZE_GB"]),
                                                RUNSTATE = Convert.ToInt32(rowdata["STATUS_GB"]),
                                                //220705 조숭진 install time 추가
                                                InstallTime = string.IsNullOrEmpty(rowdata["INSTALL_DTTM"].ToString()) ? string.Empty : Convert.ToDateTime(rowdata["INSTALL_DTTM"]).ToString("yyyy-MM-dd HH:mm:ss.fff"),
                                            };

                                            //220524 HHJ SCS 개선     //- Shelf Xml제거
                                            //캐리어 정보 복구시 해당 태그명으로 캐리어 스토리지에서 캐리어 정보를 가져와서 해당 정보로 업데이트한다.
                                            //shelf.DefaultSlot.SetCarrierData(trayid);
                                            if (CarrierStorage.Instance.GetInModuleCarrierItem(tagname) is CarrierItem carrier)
                                            {
                                                //shelfItem.DefaultSlot.SetCarrierData(carrier.CarrierID, false);
                                                shelfItem.UpdateCarrier(carrier.CarrierID, false);
                                            }

                                            data.Add(shelfItem);
                                        }
                                    }
                                    finally
                                    {
                                        if (rowdata != null)
                                        {
                                            rowdata.Close(); //240503 OracleDataReader 는 사용후 close 필요함.
                                            rowdata.Dispose();
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }

                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return data;
                //return false;
            }
            return data;
        }
        #endregion

        public void DbGetProcedureShelfInfoForClient(string shelfbank)
        {
            DataSet dataSet = new DataSet();

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        using (OracleCommand sqlcmd = new OracleCommand("UFN_STC_SHELF_INFO_GET", conn))
                        {
                            try
                            {
                                sqlcmd.CommandType = CommandType.StoredProcedure;
                                OracleParameter output = sqlcmd.Parameters.Add("TMP_DATA", OracleDbType.RefCursor);
                                output.Direction = ParameterDirection.ReturnValue;

                                OracleParameter input1 = sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2);
                                input1.Direction = ParameterDirection.Input;
                                input1.Value = GlobalData.Current.EQPID;

                                OracleParameter input2 = sqlcmd.Parameters.Add("P_BANK", OracleDbType.NVarchar2);
                                input2.Direction = ParameterDirection.Input;
                                {
                                    input2.Value = shelfbank;
                                }

                                sqlcmd.ExecuteNonQuery();

                                using (OracleDataAdapter oradata = new OracleDataAdapter(sqlcmd))
                                {
                                    try
                                    {
                                        oradata.Fill(dataSet);
                                    }
                                    finally
                                    {
                                        if (oradata != null)
                                            oradata.Dispose();
                                    }
                                }

                            }
                            finally
                            {
                                for (int i = 0; i < sqlcmd.Parameters.Count; i++)
                                {
                                    sqlcmd.Parameters[i].Dispose();
                                }

                                sqlcmd.Parameters.Clear();

                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }

                int table = dataSet.Tables.Count;

                for (int i = 0; i < table; i++)// set the table value in list one by one
                {
                    foreach (DataRow dr in dataSet.Tables[i].Rows)
                    {
                        string tagname = dr["CARRIER_LOC"].ToString();
                        ShelfItem shelfItem = new ShelfItem(tagname)
                        {
                            DeadZone = Convert.ToBoolean(Convert.ToInt32(dr["DEADZONE_STAT"])),
                            ZONE = dr["ZONE_NM"].ToString(),
                            SHELFUSE = Convert.ToBoolean(Convert.ToInt32(dr["USE_STAT"])),
                            SHELFTYPE = Convert.ToInt32(dr["SHELFSIZE_GB"]),
                            ShelfStatus = (eShelfStatus)Convert.ToInt32(dr["STATUS_GB"]),
                            InstallTime = string.IsNullOrEmpty(dr["INSTALL_DTTM"].ToString()) ? string.Empty : Convert.ToDateTime(dr["INSTALL_DTTM"]).ToString("yyyy-MM-dd HH:mm:ss.fff"),
                            FloorNum = string.IsNullOrEmpty(dr["FLOOR_NM"].ToString()) ? 0 : Convert.ToInt32(dr["FLOOR_NM"]),     //221228 조숭진 층수 추가
                            ShelfMemo = string.IsNullOrEmpty(dr["MEMO_GB"].ToString()) ? string.Empty : dr["MEMO_GB"].ToString(),
                        };
                        if(shelfbank == string.Format("{0:D2}", GlobalData.Current.FrontBankNum))
                        {
                            foreach (var item in GlobalData.Current.ShelfMgr.FrontData)
                            {
                                if (item.TagName == shelfItem.TagName)
                                {

                                    if (CarrierStorage.Instance.GetInModuleCarrierItem(tagname) is CarrierItem carrier)
                                    {
                                        shelfItem.UpdateCarrier(carrier.CarrierID, false);
                                    }
                                    else
                                    {
                                        //SuHwan_20230206 : 케리어 삭제 추가
                                        if (item.CheckCarrierExist())
                                        {
                                            item.ResetCarrierData(); //240117 RGJ Reset 으로 변경.
                                        }

                                    }

                                    GlobalData.Current.ShelfMgr.setShelfParameter(item, shelfItem);
                                    break;
                                }
                            }
                        }
                        else if (shelfbank == string.Format("{0:D2}", GlobalData.Current.RearBankNum))
                        {
                            foreach (var item in GlobalData.Current.ShelfMgr.RearData)
                            {
                                if (item.TagName == shelfItem.TagName)
                                {
                                    if (CarrierStorage.Instance.GetInModuleCarrierItem(tagname) is CarrierItem carrier)
                                    {
                                        shelfItem.UpdateCarrier(carrier.CarrierID, false);
                                    }
                                    else
                                    {
                                        //SuHwan_20230206 : 케리어 삭제 추가
                                        if (item.CheckCarrierExist())
                                        {
                                            item.ResetCarrierData(); //240117 RGJ Reset 으로 변경.
                                        }
                                    }

                                    GlobalData.Current.ShelfMgr.setShelfParameter(item, shelfItem);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());

            }

        }

        public ShelfItemList DbGetProcedureShelfInfo(string shelfbank, bool bLogging = false)
        {
            ShelfItemList data = new ShelfItemList();
            DataSet dataSet = new DataSet();

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        using (OracleCommand sqlcmd = new OracleCommand("UFN_STC_SHELF_INFO_GET", conn))
                        {
                            try
                            {
                                //sqlcmd.CommandText = "USP_STC_SHELF_INFO_GET";
                                sqlcmd.CommandType = CommandType.StoredProcedure;
                                //sqlcmd.Connection = conn;

                                //sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2, ParameterDirection.Input).Value = GlobalData.Current.EQPID;
                                //sqlcmd.Parameters.Add("P_BANK", OracleDbType.NVarchar2, ParameterDirection.Input).Value = shelfbank;
                                //sqlcmd.Parameters.Add("OUT_DATA", OracleDbType.RefCursor, ParameterDirection.Output);
                                OracleParameter output = sqlcmd.Parameters.Add("TMP_DATA", OracleDbType.RefCursor);
                                output.Direction = ParameterDirection.ReturnValue;

                                OracleParameter input1 = sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2);
                                input1.Direction = ParameterDirection.Input;
                                input1.Value = GlobalData.Current.EQPID;

                                OracleParameter input2 = sqlcmd.Parameters.Add("P_BANK", OracleDbType.NVarchar2);
                                input2.Direction = ParameterDirection.Input;
                                {
                                    input2.Value = shelfbank;
                                }

                                sqlcmd.ExecuteNonQuery();

                                using (OracleDataAdapter oradata = new OracleDataAdapter(sqlcmd))
                                {
                                    try
                                    {
                                        oradata.Fill(dataSet);
                                    }
                                    finally
                                    {
                                        if (oradata != null)
                                            oradata.Dispose();
                                    }
                                }
                            }
                            finally
                            {
                                //for (int i = 0; i < sqlcmd.Parameters.Count; i++)
                                //{
                                //    sqlcmd.Parameters[i].Dispose();
                                //}

                                //sqlcmd.Parameters.Clear();

                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }

                int table = dataSet.Tables.Count;

                for (int i = 0; i < table; i++)// set the table value in list one by one
                {
                    foreach (DataRow dr in dataSet.Tables[i].Rows)
                    {

                        string tagname = dr["CARRIER_LOC"].ToString();

                        ShelfItem shelfItem = new ShelfItem(tagname)
                        {
                            DeadZone = Convert.ToBoolean(Convert.ToInt32(dr["DEADZONE_STAT"])),
                            ZONE = dr["ZONE_NM"].ToString(),
                            SHELFUSE = Convert.ToBoolean(Convert.ToInt32(dr["USE_STAT"])),
                            SHELFTYPE = Convert.ToInt32(dr["SHELFSIZE_GB"]),
                            ShelfStatus = (eShelfStatus)Convert.ToInt32(dr["STATUS_GB"]),
                            InstallTime = string.IsNullOrEmpty(dr["INSTALL_DTTM"].ToString()) ? string.Empty : Convert.ToDateTime(dr["INSTALL_DTTM"]).ToString("yyyy-MM-dd HH:mm:ss.fff"),
                            FloorNum = string.IsNullOrEmpty(dr["FLOOR_NM"].ToString()) ? 0 : Convert.ToInt32(dr["FLOOR_NM"]),    //221228 층수 추가
                            ShelfMemo = string.IsNullOrEmpty(dr["MEMO_GB"].ToString()) ? string.Empty : dr["MEMO_GB"].ToString(),
                        };

                        if (CarrierStorage.Instance.GetInModuleCarrierItem(tagname) is CarrierItem carrier)
                        {
                            //shelfItem.DefaultSlot.SetCarrierData(carrier.CarrierID, false);
                            shelfItem.UpdateCarrier(carrier.CarrierID, false);
                        }

                        data.Add(shelfItem);

                        if (bLogging)
                        {
                            string datavalue = "SHELFITEM,";

                            for (int j = 0; j < dr.ItemArray.Count(); j++)
                            {
                                datavalue += dr.Table.Columns[j].ToString() + "=" + dr.ItemArray[j].ToString() + ",";
                            }

                            LogManager.WriteDBLog(eLogLevel.Info, datavalue, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return data;
            }
            return data;
        }

        #region GetDBJobInfo 사용 안함
        //220322 조숭진 jog관련 db 불러와 joblist에 저장
        public McsJobManager GetDBJobInfo()
        {
            string sql = string.Empty;
            McsJobManager joblisttemp = new McsJobManager();

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        //220705 조숭진 priority 0 초과부터 찾는것 삭제
                        sql = string.Format("SELECT SCS_CD, CREATE_DTTM, CARRIER_ID, SOUR_ID, DEST_ID, PRIORITY_ORDER, TRANSFER_NO, TRAYTYPE_GB, CMD_ID, " +
                            "JOBFROM_GB, TRSTATUS_GB, JOBTYPE_GB, ASSIGNRM_NM, JOBSTEP_NM, SUBJOBTYPE_NM" +
                            " FROM TB_JOB_INFO WHERE SCS_CD = '{0}' ORDER BY PRIORITY_ORDER ASC", GlobalData.Current.EQPID);
                        using (OracleCommand sqlcmd = new OracleCommand(sql, conn))
                        {
                            try
                            {
                                using (OracleDataReader rowdata = sqlcmd.ExecuteReader())
                                {
                                    try
                                    {
                                        LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                                        LogManager.WriteDBLog(eLogLevel.Info, "Data Exist = {0}" + rowdata.HasRows.ToString(), false);

                                        if (rowdata.HasRows == false)
                                        {
                                            return joblisttemp;
                                        }

                                        while (rowdata.Read())
                                        {
                                            DataTable schematable = rowdata.GetSchemaTable();

                                            string datavalue = string.Empty;
                                            foreach (DataRow row in schematable.Rows)
                                            {
                                                foreach (DataColumn column in schematable.Columns)
                                                {
                                                    string temp = row[column].ToString() + "=" + rowdata[row[column].ToString()].ToString() + ",";
                                                    datavalue += temp;
                                                    break;
                                                }
                                            }

                                            LogManager.WriteDBLog(eLogLevel.Info, datavalue, false);

                                            McsJob mcsjob = new McsJob();

                                            mcsjob.CreateTime = Convert.ToDateTime(rowdata["CREATE_DTTM"]).ToString("yyyy-MM-dd HH:mm:ss.fff");
                                            mcsjob.CarrierID = rowdata["CARRIER_ID"].ToString();
                                            mcsjob.Source = rowdata["SOUR_ID"].ToString();
                                            mcsjob.Destination = rowdata["DEST_ID"].ToString();
                                            mcsjob.Priority = rowdata["PRIORITY_ORDER"].ToString();
                                            mcsjob.TransferState = Convert.ToInt32(rowdata["TRANSFER_NO"]);
                                            mcsjob.CarrierType = rowdata["TRAYTYPE_GB"].ToString();
                                            mcsjob.CommandID = rowdata["CMD_ID"].ToString();
                                            mcsjob.JobFrom = (eScheduleJobFrom)Convert.ToInt32(rowdata["JOBFROM_GB"]);
                                            mcsjob.JobType = rowdata["JOBTYPE_GB"].ToString();
                                            mcsjob.TCStatus = (eTCState)Enum.Parse(typeof(eTCState), rowdata["TRSTATUS_GB"].ToString());
                                            if (string.IsNullOrEmpty(rowdata["ASSIGNRM_NM"].ToString()))
                                            {
                                                mcsjob.AssignedRM = null;
                                            }
                                            else
                                            {
                                                mcsjob.AssignedRM = GlobalData.Current.mRMManager[rowdata["ASSIGNRM_NM"].ToString()];
                                            }

                                            if (string.IsNullOrEmpty(rowdata["JOBSTEP_NM"].ToString()))
                                                mcsjob.Step = enumScheduleStep.None;
                                            else
                                                mcsjob.Step = (enumScheduleStep)Convert.ToInt32(rowdata["JOBSTEP_NM"]);

                                            mcsjob.SubJob = (eSubJobType)Convert.ToInt32(rowdata["SUBJOBTYPE_NM"]);

                                            joblisttemp.Add(mcsjob);
                                        }
                                    }
                                    finally
                                    {
                                        if (rowdata != null)
                                        {
                                            rowdata.Close(); //240503 OracleDataReader 는 사용후 close 필요함.
                                            rowdata.Dispose();
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();
                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return joblisttemp;
            }
            return joblisttemp;
        }
        #endregion

        public McsJobManager DbGetProcedureJobInfo(bool bLogging = false)
        {
            McsJobManager joblisttemp = new McsJobManager();
            DataSet dataSet = new DataSet();
            string cmdtext = string.Empty;

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        {
                            cmdtext = "UFN_STC_JOB_INFO_GET";
                        }

                        using (OracleCommand sqlcmd = new OracleCommand(cmdtext, conn))
                        {
                            try
                            {
                                sqlcmd.CommandType = CommandType.StoredProcedure;

                                OracleParameter output = sqlcmd.Parameters.Add("TMP_DATA", OracleDbType.RefCursor);
                                output.Direction = ParameterDirection.ReturnValue;

                                OracleParameter input = sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2);
                                input.Direction = ParameterDirection.Input;
                                input.Value = GlobalData.Current.EQPID;

                                sqlcmd.ExecuteNonQuery();

                                using (OracleDataAdapter oradata = new OracleDataAdapter(sqlcmd))
                                {
                                    try
                                    {
                                        oradata.Fill(dataSet);
                                    }
                                    finally
                                    {
                                        if (oradata != null)
                                            oradata.Dispose();
                                    }
                                }
                            }
                            finally
                            {
                                for (int i = 0; i < sqlcmd.Parameters.Count; i++)
                                {
                                    sqlcmd.Parameters[i].Dispose();
                                }

                                sqlcmd.Parameters.Clear();

                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }

                int table = dataSet.Tables.Count;
                for (int i = 0; i < table; i++)// set the table value in list one by one
                {
                    foreach (DataRow dr in dataSet.Tables[i].Rows)
                    {

                        joblisttemp.Add(new McsJob
                        {
                            CreateTime = Convert.ToDateTime(dr["CREATE_DTTM"]).ToString("yyyy-MM-dd HH:mm:ss.fff"),
                            CarrierID = dr["CARRIER_ID"].ToString(),
                            Source = dr["SOUR_ID"].ToString(),
                            Destination = dr["DEST_ID"].ToString(),
                            Priority = dr["PRIORITY_ORDER"].ToString(),
                            TransferState = Convert.ToInt32(dr["TRANSFER_NO"]),
                            CarrierType = dr["TRAYTYPE_GB"].ToString(),
                            CommandID = dr["CMD_ID"].ToString(),
                            JobFrom = (eScheduleJobFrom)Convert.ToInt32(dr["JOBFROM_GB"]),
                            JobType = dr["JOBTYPE_GB"].ToString(),
                            TCStatus = (eTCState)Enum.Parse(typeof(eTCState), dr["TRSTATUS_GB"].ToString()),
                            AssignedRM = string.IsNullOrEmpty(dr["ASSIGNRM_NM"].ToString()) ? null : GlobalData.Current.mRMManager[dr["ASSIGNRM_NM"].ToString()],
                            Step = string.IsNullOrEmpty(dr["JOBSTEP_NM"].ToString()) ? enumScheduleStep.None : (enumScheduleStep)Convert.ToInt32(dr["JOBSTEP_NM"]),
                            SubJob = string.IsNullOrEmpty(dr["SUBJOBTYPE_NM"].ToString()) ? eSubJobType.None : (eSubJobType)Convert.ToInt32(dr["SUBJOBTYPE_NM"]),       //230207 null일때 none 추가
                            SourceZoneName = string.IsNullOrEmpty(dr["SOURZONE_NM"].ToString()) ? string.Empty : dr["SOURZONE_NM"].ToString(),
                            DestZoneName = string.IsNullOrEmpty(dr["DESTZONE_NM"].ToString()) ? string.Empty : dr["DESTZONE_NM"].ToString(),
                        });

                        if (bLogging)
                        {
                            string datavalue = "JOBINFO,";

                            for (int j = 0; j < dr.ItemArray.Count(); j++)
                            {
                                datavalue += dr.Table.Columns[j].ToString() + "=" + dr.ItemArray[j].ToString() + ",";
                            }

                            LogManager.WriteDBLog(eLogLevel.Info, datavalue, false);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return joblisttemp;
            }
            return joblisttemp;
        }

        #region GetDBCarrierInfo 사용 안함
        public ConcurrentDictionary<string, CarrierItem> GetDBCarrierInfo()
        {
            string sql = string.Empty;
            ConcurrentDictionary<string, CarrierItem> tmp = new ConcurrentDictionary<string, CarrierItem>();

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        sql = string.Format("SELECT CARRIER_ID, CARRIER_LOC, PRODUCT_STAT, POLARITY_STAT, WINDERDIR_STAT, PRODUCT_CNT, FINALLOC_NO," +
                            "INNERTYPE_GB, PALLET_GB, TRAYSTACK_CNT, TRAYTYPE_STAT, UNCOATED_STAT, CORETYPE_STAT, VALIDATION_ID, PRODUCTEND_STAT, CARRIER_CD," +
                            "CARRIER_GB, CARRIER_STAT, LOT_ID, FIRSTLOT_ID, SECONDLOT_ID, THIRDLOT_ID, FOURTHLOT_ID, FIFTHLOT_ID, SIXTHLOT_ID, CARRYIN_DTTM," +
                            "CARRYOUT_DTTM, CARRIERID_STAT FROM TB_CARRIER_INFO WHERE SCS_CD='{0}'", GlobalData.Current.EQPID);

                        using (OracleCommand sqlcmd = new OracleCommand(sql, conn))
                        {
                            try
                            {
                                using (OracleDataReader rowdata = sqlcmd.ExecuteReader())
                                {
                                    try
                                    {
                                        LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                                        LogManager.WriteDBLog(eLogLevel.Info, "Data Exist = {0}" + rowdata.HasRows.ToString(), false);

                                        while (rowdata.Read())
                                        {
                                            DataTable schematable = rowdata.GetSchemaTable();

                                            string datavalue = string.Empty;
                                            foreach (DataRow row in schematable.Rows)
                                            {
                                                foreach (DataColumn column in schematable.Columns)
                                                {
                                                    string temp = row[column].ToString() + "=" + rowdata[row[column].ToString()].ToString() + ",";
                                                    datavalue += temp;
                                                    break;
                                                }
                                            }

                                            LogManager.WriteDBLog(eLogLevel.Info, datavalue, false);

                                            CarrierItem carrierItem = new CarrierItem();

                                            carrierItem.CarrierID = rowdata["CARRIER_ID"].ToString();
                                            carrierItem.CarrierLocation = rowdata["CARRIER_LOC"].ToString();
                                            carrierItem.ProductEmpty = (eProductEmpty)Convert.ToInt32(rowdata["PRODUCT_STAT"]);
                                            carrierItem.Polarity = (ePolarity)Convert.ToInt32(rowdata["POLARITY_STAT"]);
                                            carrierItem.WinderDirection = (eWinderDirection)Convert.ToInt32(rowdata["WINDERDIR_STAT"]);
                                            carrierItem.ProductQuantity = Convert.ToInt32(rowdata["PRODUCT_CNT"]);
                                            carrierItem.FinalLoc = rowdata["FINALLOC_NO"].ToString();
                                            carrierItem.InnerTrayType = (eInnerTrayType)Convert.ToInt32(rowdata["INNERTYPE_GB"]);
                                            carrierItem.PalletSize = (ePalletSize)Convert.ToInt32(rowdata["PALLET_GB"]);
                                            carrierItem.TrayStackCount = rowdata["TRAYSTACK_CNT"].ToString();
                                            carrierItem.TrayType = (eTrayType)Convert.ToInt32(rowdata["TRAYTYPE_STAT"]);
                                            carrierItem.UncoatedPart = (eUnCoatedPart)Convert.ToInt32(rowdata["UNCOATED_STAT"]);
                                            carrierItem.CoreType = (eCoreType)Convert.ToInt32(rowdata["CORETYPE_STAT"]);
                                            carrierItem.ValidationNG = rowdata["VALIDATION_ID"].ToString();
                                            carrierItem.ProductEnd = (eProductEnd)Convert.ToInt32(rowdata["PRODUCTEND_STAT"]);
                                            carrierItem.CarrierType = rowdata["CARRIER_CD"].ToString();
                                            carrierItem.CarrierSize = (eCarrierSize)Convert.ToInt32(rowdata["CARRIER_GB"]);
                                            carrierItem.CarrierState = (eCarrierState)Convert.ToInt32(rowdata["CARRIER_STAT"]);
                                            carrierItem.LotID = rowdata["LOT_ID"].ToString();
                                            carrierItem.First_Lot = rowdata["FIRSTLOT_ID"].ToString();
                                            carrierItem.Second_Lot = rowdata["SECONDLOT_ID"].ToString();
                                            carrierItem.Third_Lot = rowdata["THIRDLOT_ID"].ToString();
                                            carrierItem.Fourth_Lot = rowdata["FOURTHLOT_ID"].ToString();
                                            carrierItem.Fifth_Lot = rowdata["FIFTHLOT_ID"].ToString();
                                            carrierItem.Sixth_Lot = rowdata["SIXTHLOT_ID"].ToString();
                                            carrierItem.CarryInTime = Convert.ToDateTime(rowdata["CARRYIN_DTTM"]).ToString("yyyy-MM-dd HH:mm:ss.fff");
                                            carrierItem.CarryOutTime = string.IsNullOrEmpty(rowdata["CARRYOUT_DTTM"].ToString()) ? string.Empty : Convert.ToDateTime(rowdata["CARRYOUT_DTTM"]).ToString("yyyy-MM-dd HH:mm:ss.fff");
                                            carrierItem.LastReadResult = (eIDReadStatus)Convert.ToInt32(rowdata["CARRIERID_STAT"]);

                                            tmp.TryAdd(carrierItem.CarrierID, carrierItem);
                                        }
                                    }
                                    finally
                                    {
                                        if (rowdata != null)
                                        {
                                            rowdata.Close(); //240503 OracleDataReader 는 사용후 close 필요함.
                                            rowdata.Dispose();
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return tmp;
            }
            return tmp;
        }
        #endregion

        public virtual ConcurrentDictionary<string, CarrierItem> DbGetProcedureCarrierInfo(bool bLogging = false)
        {
            ConcurrentDictionary<string, CarrierItem> tmp = new ConcurrentDictionary<string, CarrierItem>();
            DataSet dataSet = new DataSet();

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        using (OracleCommand sqlcmd = new OracleCommand("UFN_STC_CARRIER_INFO_GET", conn))
                        {
                            try
                            {
                                //sqlcmd.CommandText = "USP_STC_CARRIER_INFO_GET";
                                sqlcmd.CommandType = CommandType.StoredProcedure;
                                //sqlcmd.Connection = conn;

                                //sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2, ParameterDirection.Input).Value = GlobalData.Current.EQPID;
                                //sqlcmd.Parameters.Add("OUT_DATA", OracleDbType.RefCursor, ParameterDirection.Output);
                                OracleParameter output = sqlcmd.Parameters.Add("TMP_DATA", OracleDbType.RefCursor);
                                output.Direction = ParameterDirection.ReturnValue;

                                OracleParameter input = sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2);
                                input.Direction = ParameterDirection.Input;
                                input.Value = GlobalData.Current.EQPID;

                                sqlcmd.ExecuteNonQuery();

                                using (OracleDataAdapter oradata = new OracleDataAdapter(sqlcmd))
                                {
                                    try
                                    {
                                        oradata.Fill(dataSet);
                                    }
                                    finally
                                    {
                                        if (oradata != null)
                                            oradata.Dispose();
                                    }
                                }
                            }
                            finally
                            {
                                for (int i = 0; i < sqlcmd.Parameters.Count; i++)
                                {
                                    sqlcmd.Parameters[i].Dispose();
                                }

                                sqlcmd.Parameters.Clear();

                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }

                int table = dataSet.Tables.Count;

                for (int i = 0; i < table; i++)// set the table value in list one by one
                {
                    foreach (DataRow dr in dataSet.Tables[i].Rows)
                    {
                        if (!bLogging)
                        {
                            CarrierItem carrierItem = new CarrierItem()
                            {
                                CarrierID = dr["CARRIER_ID"].ToString(),
                                CarrierLocation = dr["CARRIER_LOC"].ToString(),
                                ProductEmpty = (eProductEmpty)Convert.ToInt32(dr["PRODUCT_STAT"]),
                                Polarity = (ePolarity)Convert.ToInt32(dr["POLARITY_STAT"]),
                                WinderDirection = (eWinderDirection)Convert.ToInt32(dr["WINDERDIR_STAT"]),
                                ProductQuantity = Convert.ToInt32(dr["PRODUCT_CNT"]),
                                FinalLoc = dr["FINALLOC_NO"].ToString(),
                                InnerTrayType = (eInnerTrayType)Convert.ToInt32(dr["INNERTYPE_GB"]),
                                PalletSize = (ePalletSize)Convert.ToInt32(dr["PALLET_GB"]),
                                TrayStackCount = dr["TRAYSTACK_CNT"].ToString(),
                                TrayType = (eTrayType)Convert.ToInt32(dr["TRAYTYPE_STAT"]),
                                UncoatedPart = (eUnCoatedPart)Convert.ToInt32(dr["UNCOATED_STAT"]),
                                CoreType = (eCoreType)Convert.ToInt32(dr["CORETYPE_STAT"]),
                                ValidationNG = dr["VALIDATION_ID"].ToString(),
                                ProductEnd = (eProductEnd)Convert.ToInt32(dr["PRODUCTEND_STAT"]),
                                CarrierType = dr["CARRIER_CD"].ToString(),
                                CarrierSize = (eCarrierSize)Convert.ToInt32(dr["CARRIER_GB"]),
                                CarrierState = (eCarrierState)Convert.ToInt32(dr["CARRIER_STAT"]),
                                LotID = dr["LOT_ID"].ToString(),
                                First_Lot = dr["FIRSTLOT_ID"].ToString(),
                                Second_Lot = dr["SECONDLOT_ID"].ToString(),
                                Third_Lot = dr["THIRDLOT_ID"].ToString(),
                                Fourth_Lot = dr["FOURTHLOT_ID"].ToString(),
                                Fifth_Lot = dr["FIFTHLOT_ID"].ToString(),
                                Sixth_Lot = dr["SIXTHLOT_ID"].ToString(),
                                CarryInTime = Convert.ToDateTime(dr["CARRYIN_DTTM"]).ToString("yyyy-MM-dd HH:mm:ss.fff"),
                                CarryOutTime = string.IsNullOrEmpty(dr["CARRYOUT_DTTM"].ToString()) ? string.Empty : Convert.ToDateTime(dr["CARRYOUT_DTTM"]).ToString("yyyy-MM-dd HH:mm:ss.fff"),
                                LastReadResult = (eIDReadStatus)Convert.ToInt32(dr["CARRIERID_STAT"]),
                            };

                            tmp.TryAdd(carrierItem.CarrierID, carrierItem);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return tmp;
            }
            return tmp;
        }



        #region DbSetCarrierInfo 사용 안함
        public virtual void DbSetCarrierInfo(CarrierItem item, bool del)
        {
            lock (DBLock)
            {
                OracleCommand sqlcmd = null;
                string sql = string.Empty;
                OracleDataReader rowdata = null;
                OracleConnection conn = null;
                OracleTransaction tran = null;

                try
                {
                    conn = new OracleConnection(OracleDBPath);
                    conn.Open();

                    tran = conn.BeginTransaction();

                    sql = string.Format("SELECT CARRIER_ID, CARRIER_LOC, PRODUCT_STAT, POLARITY_STAT, WINDERDIR_STAT, PRODUCT_CNT, FINALLOC_NO," +
                        "INNERTYPE_GB, PALLET_GB, TRAYSTACK_CNT, TRAYTYPE_STAT, UNCOATED_STAT, CORETYPE_STAT, VALIDATION_ID, PRODUCTEND_STAT, CARRIER_CD," +
                        "CARRIER_GB, CARRIER_STAT, LOT_ID, FIRSTLOT_ID, SECONDLOT_ID, THIRDLOT_ID, FOURTHLOT_ID, FIFTHLOT_ID, SIXTHLOT_ID, CARRYIN_DTTM," +
                        "CARRYOUT_DTTM, CARRIERID_STAT FROM TB_CARRIER_INFO WHERE CARRIER_LOC='{0}' AND SCS_CD='{1}'", item.CarrierLocation, GlobalData.Current.EQPID);

                    sqlcmd = new OracleCommand(sql, conn);
                    rowdata = sqlcmd.ExecuteReader();
                    bool exist = rowdata.HasRows;

                    LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                    LogManager.WriteDBLog(eLogLevel.Info, "Data Exist = {0}" + exist.ToString(), false);

                    if (rowdata.Read())
                    {
                        DataTable schematable = rowdata.GetSchemaTable();

                        string datavalue = string.Empty;
                        foreach (DataRow row in schematable.Rows)
                        {
                            foreach (DataColumn column in schematable.Columns)
                            {
                                string temp = row[column].ToString() + "=" + rowdata[row[column].ToString()].ToString() + ",";
                                datavalue += temp;
                                break;
                            }
                        }
                        LogManager.WriteDBLog(eLogLevel.Info, datavalue, false);
                    }

                    if (exist && !del)              //update
                    {
                        sql = string.Format("UPDATE TB_CARRIER_INFO SET CARRIER_LOC='{0}', PRODUCT_STAT='{1}', POLARITY_STAT='{2}', WINDERDIR_STAT='{3}', " +
                            "PRODUCT_CNT='{4}', FINALLOC_NO='{5}', INNERTYPE_GB='{6}', PALLET_GB='{7}', TRAYSTACK_CNT='{8}', TRAYTYPE_STAT='{9}'," +
                            "UNCOATED_STAT='{10}', CORETYPE_STAT='{11}', VALIDATION_ID='{12}', PRODUCTEND_STAT='{13}', CARRIER_CD='{14}', CARRIER_GB='{15}'," +
                            "CARRIER_STAT='{16}', LOT_ID='{17}', FIRSTLOT_ID='{18}', SECONDLOT_ID='{19}', THIRDLOT_ID='{20}', FOURTHLOT_ID='{21}', " +
                            "FIFTHLOT_ID='{22}', SIXTHLOT_ID='{23}', CARRYIN_DTTM={24}, CARRIERID_STAT='{25}', CARRIER_ID='{26}' WHERE CARRIER_LOC='{27}'AND SCS_CD='{28}'",
                            item.CarrierLocation,
                            Convert.ToInt32(item.ProductEmpty),
                            Convert.ToInt32(item.Polarity),
                            Convert.ToInt32(item.WinderDirection),
                            item.ProductQuantity,
                            item.FinalLoc,
                            Convert.ToInt32(item.InnerTrayType),
                            Convert.ToInt32(item.PalletSize),
                            item.TrayStackCount,
                            Convert.ToInt32(item.TrayType),
                            Convert.ToInt32(item.UncoatedPart),
                            Convert.ToInt32(item.CoreType),
                            item.ValidationNG,
                            Convert.ToInt32(item.ProductEnd),
                            item.CarrierType,
                            Convert.ToInt32(item.CarrierSize),
                            Convert.ToInt32(item.CarrierState),
                            item.LotID,
                            item.First_Lot,
                            item.Second_Lot,
                            item.Third_Lot,
                            item.Fourth_Lot,
                            item.Fifth_Lot,
                            item.Sixth_Lot,
                            DateTimeOracle(item.CarryInTime),
                            //DateTimeOracle(item.CarryOutTime),
                            Convert.ToInt32(item.LastReadResult),
                            item.CarrierID,
                            item.CarrierLocation,
                            GlobalData.Current.EQPID);
                    }
                    //return false;
                    else if (exist && del)           //delete
                    {
                        sql = string.Format("DELETE FROM TB_CARRIER_INFO WHERE SCS_CD='{0}' AND CARRIER_ID='{1}'", GlobalData.Current.EQPID, item.CarrierID);
                    }
                    else//insert
                    {
                        sql = string.Format("INSERT INTO TB_CARRIER_INFO(SCS_CD, CARRIER_ID, CARRIER_LOC, PRODUCT_STAT, POLARITY_STAT, WINDERDIR_STAT," +
                            "PRODUCT_CNT, FINALLOC_NO, INNERTYPE_GB, PALLET_GB, TRAYSTACK_CNT, TRAYTYPE_STAT, UNCOATED_STAT, CORETYPE_STAT, VALIDATION_ID, " +
                            "PRODUCTEND_STAT, CARRIER_CD, CARRIER_GB, CARRIER_STAT, LOT_ID, FIRSTLOT_ID, SECONDLOT_ID, THIRDLOT_ID, FOURTHLOT_ID, " +
                            "FIFTHLOT_ID, SIXTHLOT_ID, CARRYIN_DTTM, CARRIERID_STAT) VALUES " +
                            "('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', '{10}', '{11}', '{12}','{13}', '{14}', '{15}', '{16}', " +
                            "'{17}', '{18}', '{19}', '{20}', '{21}', '{22}', '{23}', '{24}', '{25}', {26}, '{27}')",
                            GlobalData.Current.EQPID,
                            item.CarrierID,
                            item.CarrierLocation,
                            Convert.ToInt32(item.ProductEmpty),
                            Convert.ToInt32(item.Polarity),
                            Convert.ToInt32(item.WinderDirection),
                            item.ProductQuantity,
                            item.FinalLoc,
                            Convert.ToInt32(item.InnerTrayType),
                            Convert.ToInt32(item.PalletSize),
                            item.TrayStackCount,
                            Convert.ToInt32(item.TrayType),
                            Convert.ToInt32(item.UncoatedPart),
                            Convert.ToInt32(item.CoreType),
                            item.ValidationNG,
                            Convert.ToInt32(item.ProductEnd),
                            item.CarrierType,
                            Convert.ToInt32(item.CarrierSize),
                            Convert.ToInt32(item.CarrierState),
                            item.LotID,
                            item.First_Lot,
                            item.Second_Lot,
                            item.Third_Lot,
                            item.Fourth_Lot,
                            item.Fifth_Lot,
                            item.Sixth_Lot,
                            DateTimeOracle(item.CarryInTime),
                            //DateTimeOracle(item.CarryOutTime),
                            Convert.ToInt32(item.LastReadResult));
                    }

                    sqlcmd = new OracleCommand(sql, conn);
                    sqlcmd.Transaction = tran;
                    sqlcmd.ExecuteNonQuery();

                    sqlcmd.Transaction.Commit();

                    LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                }
                catch (Exception ex)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                }
                finally
                {
                    conn.Close();

                    sqlcmd?.Dispose();
                    rowdata?.Dispose();
                    tran?.Dispose();
                    conn?.Dispose();
                }
            }
        }
        #endregion

        public virtual void DbSetProcedureCarrierInfo(CarrierItem item, bool del)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        using (OracleCommand sqlcmd = new OracleCommand())
                        {
                            try
                            {
                                sqlcmd.CommandText = "USP_STC_CARRIER_INFO_SET";
                                sqlcmd.CommandType = CommandType.StoredProcedure;
                                sqlcmd.Connection = conn;

                                OracleParameter R_RESULT = new OracleParameter("R_RESULT", OracleDbType.NVarchar2, 100);
                                OracleParameter R_TEMP = new OracleParameter("R_TEMP", OracleDbType.NVarchar2, 100);
                                OracleParameter R_RESULT2 = new OracleParameter("R_RESULT2", OracleDbType.NVarchar2, 100);
                                OracleParameter R_TEMP2 = new OracleParameter("R_TEMP2", OracleDbType.NVarchar2, 100);
                                OracleParameter R_CARRIER_ID = new OracleParameter("R_CARRIER_ID", OracleDbType.NVarchar2, 100);
                                OracleParameter R_CARRIER_LOC = new OracleParameter("R_CARRIER_LOC", OracleDbType.NVarchar2, 100);

                                DateTimeForOracle(item.CarryInTime, out int carryinyear, out int carryinmonth, out int carryinday,
                                    out int carryinhour, out int carryinminute, out int carryinsecond, out int carryinmillisecond);

                                if (!string.IsNullOrEmpty(item.CarryOutTime))
                                    DateTimeForOracle(item.CarryInTime, out int carryoutyear, out int carryoutmonth, out int carryoutday, out int carryouthour, out int carryoutminute, out int carryoutsecond, out int carryoutmillisecond);

                                if (string.IsNullOrEmpty(item.TrayStackCount))
                                    item.TrayStackCount = "0";

                                sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2, ParameterDirection.Input).Value = GlobalData.Current.EQPID;
                                sqlcmd.Parameters.Add("P_CARRIER_ID", OracleDbType.NVarchar2, ParameterDirection.Input).Value = item.CarrierID;
                                sqlcmd.Parameters.Add("P_CARRIER_LOC", OracleDbType.NVarchar2, ParameterDirection.Input).Value = item.CarrierLocation;
                                sqlcmd.Parameters.Add("P_PRODUCT_STAT", OracleDbType.Char, ParameterDirection.Input).Value = Convert.ToInt32(item.ProductEmpty);
                                sqlcmd.Parameters.Add("P_POLARITY_STAT", OracleDbType.Char, ParameterDirection.Input).Value = Convert.ToInt32(item.Polarity);
                                sqlcmd.Parameters.Add("P_WINDERDIR_STAT", OracleDbType.Char, ParameterDirection.Input).Value = Convert.ToInt32(item.WinderDirection);
                                sqlcmd.Parameters.Add("P_PRODUCT_CNT", OracleDbType.Int32, ParameterDirection.Input).Value = item.ProductQuantity;
                                sqlcmd.Parameters.Add("P_FINALLOC_NO", OracleDbType.NVarchar2, ParameterDirection.Input).Value = item.FinalLoc;
                                sqlcmd.Parameters.Add("P_INNERTYPE_GB", OracleDbType.Int32, ParameterDirection.Input).Value = Convert.ToInt32(item.InnerTrayType);
                                sqlcmd.Parameters.Add("P_PALLET_GB", OracleDbType.Int32, ParameterDirection.Input).Value = Convert.ToInt32(item.PalletSize);
                                sqlcmd.Parameters.Add("P_TRAYSTACK_CNT", OracleDbType.Int32, ParameterDirection.Input).Value = item.TrayStackCount;
                                sqlcmd.Parameters.Add("P_TRAYTYPE_STAT", OracleDbType.Char, ParameterDirection.Input).Value = Convert.ToInt32(item.TrayType);
                                sqlcmd.Parameters.Add("P_UNCOATED_STAT", OracleDbType.Char, ParameterDirection.Input).Value = Convert.ToInt32(item.UncoatedPart);
                                sqlcmd.Parameters.Add("P_CORETYPE_STAT", OracleDbType.Char, ParameterDirection.Input).Value = Convert.ToInt32(item.CoreType);
                                sqlcmd.Parameters.Add("P_VALIDATION_ID", OracleDbType.NVarchar2, ParameterDirection.Input).Value = item.ValidationNG;
                                sqlcmd.Parameters.Add("P_PRODUCTEND_STAT", OracleDbType.Char, ParameterDirection.Input).Value = Convert.ToInt32(item.ProductEnd);
                                sqlcmd.Parameters.Add("P_CARRIER_CD", OracleDbType.NVarchar2, ParameterDirection.Input).Value = item.CarrierType;
                                sqlcmd.Parameters.Add("P_CARRIER_GB", OracleDbType.Int32, ParameterDirection.Input).Value = Convert.ToInt32(item.CarrierSize);
                                sqlcmd.Parameters.Add("P_CARRIER_STAT", OracleDbType.Char, ParameterDirection.Input).Value = Convert.ToInt32(item.CarrierState);
                                sqlcmd.Parameters.Add("P_LOT_ID", OracleDbType.NVarchar2, ParameterDirection.Input).Value = item.LotID;
                                sqlcmd.Parameters.Add("P_FIRSTLOT_ID", OracleDbType.NVarchar2, ParameterDirection.Input).Value = item.First_Lot;
                                sqlcmd.Parameters.Add("P_SECONDLOT_ID", OracleDbType.NVarchar2, ParameterDirection.Input).Value = item.Second_Lot;
                                sqlcmd.Parameters.Add("P_THIRDLOT_ID", OracleDbType.NVarchar2, ParameterDirection.Input).Value = item.Third_Lot;
                                sqlcmd.Parameters.Add("P_FOURTHLOT_ID", OracleDbType.NVarchar2, ParameterDirection.Input).Value = item.Fourth_Lot;
                                sqlcmd.Parameters.Add("P_FIFTHLOT_ID", OracleDbType.NVarchar2, ParameterDirection.Input).Value = item.Fifth_Lot;
                                sqlcmd.Parameters.Add("P_SIXTHLOT_ID", OracleDbType.NVarchar2, ParameterDirection.Input).Value = item.Sixth_Lot;
                                sqlcmd.Parameters.Add("P_CARRYIN_DTTM", OracleDbType.TimeStamp, new DateTime(carryinyear, carryinmonth, carryinday, carryinhour, carryinminute, carryinsecond, carryinmillisecond), System.Data.ParameterDirection.Input);
                                sqlcmd.Parameters.Add("P_CARRIERID_STAT", OracleDbType.Char, ParameterDirection.Input).Value = Convert.ToInt32(item.LastReadResult);
                                sqlcmd.Parameters.Add("P_CARRIERHEIGHT_STAT", OracleDbType.Char, ParameterDirection.Input).Value = Convert.ToInt32(item.CarrierHeight);     //230207 CARRIERHEIGHT 추가
                                sqlcmd.Parameters.Add("P_DEL", OracleDbType.Char, ParameterDirection.Input).Value = del ? '1' : '0';

                                sqlcmd.Parameters.Add(R_RESULT).Direction = ParameterDirection.Output;
                                sqlcmd.Parameters.Add(R_TEMP).Direction = ParameterDirection.Output;
                                sqlcmd.Parameters.Add(R_RESULT2).Direction = ParameterDirection.Output;
                                sqlcmd.Parameters.Add(R_TEMP2).Direction = ParameterDirection.Output;
                                sqlcmd.Parameters.Add(R_CARRIER_LOC).Direction = ParameterDirection.Output;
                                sqlcmd.Parameters.Add(R_CARRIER_ID).Direction = ParameterDirection.Output;

                                sqlcmd.ExecuteNonQuery();

                                string temp = string.Format("Add Result = {0}", R_RESULT.Value.ToString());
                                string temp1 = string.Format("Add Temp = {0}", R_TEMP.Value.ToString());
                                string temp2 = R_RESULT2.Value.ToString() != "null" ? string.Format("Add Result2 = {0}", R_RESULT2.Value.ToString()) : string.Empty;
                                string temp3 = R_TEMP2.Value.ToString() != "null" ? string.Format("Add Temp2 = {0}", R_TEMP2.Value.ToString()) : string.Empty;
                                string tmp = R_CARRIER_ID.Value.ToString() != "null" ? string.Format("Exception ID = {0}", R_CARRIER_ID.Value.ToString()) : string.Empty;
                                string tmp1 = R_CARRIER_LOC.Value.ToString() != "null" ? string.Format("Exception LOC = {0}", R_CARRIER_LOC.Value.ToString()) : string.Empty;

                                //LogManager.WriteConsoleLog(eLogLevel.Info, temp1 + ", " + temp);
                                LogManager.WriteDBLog(eLogLevel.Info, temp1 + ", " + temp, false);

                                if (temp3 != string.Empty && temp2 != string.Empty)
                                {
                                    LogManager.WriteConsoleLog(eLogLevel.Info, temp3 + ", " + temp2);
                                    LogManager.WriteDBLog(eLogLevel.Info, temp3 + ", " + temp2, false);
                                }

                                if (!(string.IsNullOrEmpty(tmp) && string.IsNullOrEmpty(tmp1)))
                                {
                                    LogManager.WriteConsoleLog(eLogLevel.Debug, tmp + ", " + tmp1);
                                    LogManager.WriteDBLog(eLogLevel.Debug, tmp + ", " + tmp1, false);
                                }

                                ////sqlcmd.CommandText = "USP_STC_CARRIER_INFO_SET_TEST";         //220628 조숭진 playback을 위한 것
                                //sqlcmd.CommandText = "USP_STC_CARRIER_INFO_SET_TEST2";
                                //sqlcmd.CommandType = CommandType.StoredProcedure;
                                //sqlcmd.Connection = conn;

                                //OracleParameter R_RESULT = new OracleParameter("R_RESULT", OracleDbType.NVarchar2, 20);
                                //OracleParameter R_TEMP = new OracleParameter("R_TEMP", OracleDbType.NVarchar2, 20);
                                //OracleParameter R_RESULT2 = new OracleParameter("R_RESULT2", OracleDbType.NVarchar2, 20);
                                //OracleParameter R_TEMP2 = new OracleParameter("R_TEMP2", OracleDbType.NVarchar2, 20);
                                //OracleParameter R_CARRIER_ID = new OracleParameter("R_CARRIER_ID", OracleDbType.NVarchar2, 20);
                                //OracleParameter R_CARRIER_LOC = new OracleParameter("R_CARRIER_LOC", OracleDbType.NVarchar2, 20);
                                //OracleParameter R_CARRIERCONTENT = new OracleParameter("R_CARRIERCONTENT", OracleDbType.NVarchar2, 1000);

                                //DateTimeForOracle(item.CarryInTime, out int carryinyear, out int carryinmonth, out int carryinday,
                                //    out int carryinhour, out int carryinminute, out int carryinsecond, out int carryinmillisecond);

                                ////220628 조숭진 playback을 위한 것
                                ////220624 조숭진 playback을 위해 job 변경 시 save 시간 기록
                                ////DateTimeForOracle(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), out int saveyear, out int savemonth, out int saveday,
                                ////    out int savehour, out int saveminute, out int savesecond, out int savemillisecond);

                                //if (!string.IsNullOrEmpty(item.CarryOutTime))
                                //    DateTimeForOracle(item.CarryInTime, out int carryoutyear, out int carryoutmonth, out int carryoutday, out int carryouthour, out int carryoutminute, out int carryoutsecond, out int carryoutmillisecond);

                                //if (string.IsNullOrEmpty(item.TrayStackCount))
                                //    item.TrayStackCount = "0";

                                //sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.Varchar2, ParameterDirection.Input).Value = GlobalData.Current.EQPID;
                                //sqlcmd.Parameters.Add("P_CARRIER_ID", OracleDbType.Varchar2, ParameterDirection.Input).Value = item.CarrierID;
                                //sqlcmd.Parameters.Add("P_CARRIER_LOC", OracleDbType.Varchar2, ParameterDirection.Input).Value = item.CarrierLocation;
                                //sqlcmd.Parameters.Add("P_PRODUCT_STAT", OracleDbType.Char, ParameterDirection.Input).Value = Convert.ToInt32(item.ProductEmpty);
                                //sqlcmd.Parameters.Add("P_POLARITY_STAT", OracleDbType.Char, ParameterDirection.Input).Value = Convert.ToInt32(item.Polarity);
                                //sqlcmd.Parameters.Add("P_WINDERDIR_STAT", OracleDbType.Char, ParameterDirection.Input).Value = Convert.ToInt32(item.WinderDirection);
                                //sqlcmd.Parameters.Add("P_PRODUCT_CNT", OracleDbType.Int32, ParameterDirection.Input).Value = item.ProductQuantity;
                                //sqlcmd.Parameters.Add("P_FINALLOC_NO", OracleDbType.Varchar2, ParameterDirection.Input).Value = item.FinalLoc;
                                //sqlcmd.Parameters.Add("P_INNERTYPE_GB", OracleDbType.Int32, ParameterDirection.Input).Value = Convert.ToInt32(item.InnerTrayType);
                                //sqlcmd.Parameters.Add("P_PALLET_GB", OracleDbType.Int32, ParameterDirection.Input).Value = Convert.ToInt32(item.PalletSize);
                                //sqlcmd.Parameters.Add("P_TRAYSTACK_CNT", OracleDbType.Int32, ParameterDirection.Input).Value = item.TrayStackCount;
                                //sqlcmd.Parameters.Add("P_TRAYTYPE_STAT", OracleDbType.Char, ParameterDirection.Input).Value = Convert.ToInt32(item.TrayType);
                                //sqlcmd.Parameters.Add("P_UNCOATED_STAT", OracleDbType.Char, ParameterDirection.Input).Value = Convert.ToInt32(item.UncoatedPart);
                                //sqlcmd.Parameters.Add("P_CORETYPE_STAT", OracleDbType.Char, ParameterDirection.Input).Value = Convert.ToInt32(item.CoreType);
                                //sqlcmd.Parameters.Add("P_VALIDATION_ID", OracleDbType.Varchar2, ParameterDirection.Input).Value = item.ValidationNG;
                                //sqlcmd.Parameters.Add("P_PRODUCTEND_STAT", OracleDbType.Char, ParameterDirection.Input).Value = Convert.ToInt32(item.ProductEnd);
                                //sqlcmd.Parameters.Add("P_CARRIER_CD", OracleDbType.Varchar2, ParameterDirection.Input).Value = item.CarrierType;
                                //sqlcmd.Parameters.Add("P_CARRIER_GB", OracleDbType.Int32, ParameterDirection.Input).Value = Convert.ToInt32(item.CarrierSize);
                                //sqlcmd.Parameters.Add("P_CARRIER_STAT", OracleDbType.Char, ParameterDirection.Input).Value = Convert.ToInt32(item.CarrierState);
                                //sqlcmd.Parameters.Add("P_LOT_ID", OracleDbType.Varchar2, ParameterDirection.Input).Value = item.LotID;
                                //sqlcmd.Parameters.Add("P_FIRSTLOT_ID", OracleDbType.Varchar2, ParameterDirection.Input).Value = item.First_Lot;
                                //sqlcmd.Parameters.Add("P_SECONDLOT_ID", OracleDbType.Varchar2, ParameterDirection.Input).Value = item.Second_Lot;
                                //sqlcmd.Parameters.Add("P_THIRDLOT_ID", OracleDbType.Varchar2, ParameterDirection.Input).Value = item.Third_Lot;
                                //sqlcmd.Parameters.Add("P_FOURTHLOT_ID", OracleDbType.Varchar2, ParameterDirection.Input).Value = item.Fourth_Lot;
                                //sqlcmd.Parameters.Add("P_FIFTHLOT_ID", OracleDbType.Varchar2, ParameterDirection.Input).Value = item.Fifth_Lot;
                                //sqlcmd.Parameters.Add("P_SIXTHLOT_ID", OracleDbType.Varchar2, ParameterDirection.Input).Value = item.Sixth_Lot;
                                //sqlcmd.Parameters.Add("P_CARRYIN_DTTM", OracleDbType.TimeStamp, new DateTime(carryinyear, carryinmonth, carryinday, carryinhour, carryinminute, carryinsecond, carryinmillisecond), System.Data.ParameterDirection.Input);
                                ////sqlcmd.Parameters.Add(P_CARRYOUT_DTTM);
                                //sqlcmd.Parameters.Add("P_CARRIERID_STAT", OracleDbType.Char, ParameterDirection.Input).Value = Convert.ToInt32(item.LastReadResult);
                                //sqlcmd.Parameters.Add("P_DEL", OracleDbType.Char, ParameterDirection.Input).Value = del ? '1' : '0';

                                ////220628 조숭진 playback을 위한 것
                                ////220624 조숭진 playback을 위해 job 변경 시 save 시간 기록
                                ////sqlcmd.Parameters.Add("P_SAVE_DTTM", OracleDbType.TimeStamp,
                                ////    new DateTime(saveyear, savemonth, saveday, savehour, saveminute, savesecond, savemillisecond), ParameterDirection.Input);

                                //sqlcmd.Parameters.Add(R_RESULT).Direction = ParameterDirection.Output;
                                //sqlcmd.Parameters.Add(R_TEMP).Direction = ParameterDirection.Output;
                                //sqlcmd.Parameters.Add(R_RESULT2).Direction = ParameterDirection.Output;
                                //sqlcmd.Parameters.Add(R_TEMP2).Direction = ParameterDirection.Output;
                                //sqlcmd.Parameters.Add(R_CARRIER_LOC).Direction = ParameterDirection.Output;
                                //sqlcmd.Parameters.Add(R_CARRIER_ID).Direction = ParameterDirection.Output;
                                //sqlcmd.Parameters.Add(R_CARRIERCONTENT).Direction = ParameterDirection.Output;

                                //sqlcmd.ExecuteNonQuery();

                                //string temp = string.Format("Add Result = {0}", R_RESULT.Value.ToString());
                                //string temp1 = string.Format("Add Temp = {0}", R_TEMP.Value.ToString());
                                //string temp2 = R_RESULT2.Value.ToString() != "null" ? string.Format("Add Result2 = {0}", R_RESULT2.Value.ToString()) : string.Empty;
                                //string temp3 = R_TEMP2.Value.ToString() != "null" ? string.Format("Add Temp2 = {0}", R_TEMP2.Value.ToString()) : string.Empty;
                                //string tmp = R_CARRIER_ID.Value.ToString() != "null" ? string.Format("Exception ID = {0}", R_CARRIER_ID.Value.ToString()) : string.Empty;
                                //string tmp1 = R_CARRIER_LOC.Value.ToString() != "null" ? string.Format("Exception LOC = {0}", R_CARRIER_LOC.Value.ToString()) : string.Empty;

                                //LogManager.WriteConsoleLog(eLogLevel.Info, temp1 + ", " + temp);
                                //LogManager.WriteDBLog(eLogLevel.Info, R_CARRIERCONTENT.Value.ToString(), true);
                                //LogManager.WriteDBLog(eLogLevel.Info, temp1 + ", " + temp, false);

                                //if (temp3 != string.Empty && temp2 != string.Empty)
                                //{
                                //    LogManager.WriteConsoleLog(eLogLevel.Info, temp3 + ", " + temp2);
                                //    LogManager.WriteDBLog(eLogLevel.Info, temp3 + ", " + temp2, false);
                                //}

                                //if (!(string.IsNullOrEmpty(tmp) && string.IsNullOrEmpty(tmp1)))
                                //{
                                //    LogManager.WriteConsoleLog(eLogLevel.Debug, tmp + ", " + tmp1);
                                //    LogManager.WriteDBLog(eLogLevel.Debug, tmp + ", " + tmp1, false);
                                //}
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }

        #region DateTimeOracle 사용 안함
        private string DateTimeOracle(string datetime)
        {
            //DateTime datetemp = Convert.ToDateTime(datetime);

            //string temp = datetemp.ToString("yyyy-MM-dd HH:mm:ss.fff");

            string temp = string.Format("TO_TIMESTAMP('{0}', 'YYYY-MM-DD HH24:MI:SS.FF3')", datetime);

            return temp;
        }
        #endregion

        private void DateTimeForOracle(string time, out int year, out int month, out int day, out int hour, out int minute, out int second, out int millisecond)
        {
            DateTime curtime = Convert.ToDateTime(time);

            year = curtime.Year;
            month = curtime.Month;
            day = curtime.Day;
            hour = curtime.Hour;
            minute = curtime.Minute;
            second = curtime.Second;
            millisecond = curtime.Millisecond;
        }
        public string DateTimeForOracle(DateTime DT)
        {
            return DT.ToString("yyyy-MM-dd HH:mm:ss").Replace("-", "/");
        }

        public string DataTimeForOracleUntilMillisecond(DateTime DT)
        {
            return DT.ToString("yyyy-MM-dd HH:mm:ss.fff").Replace("-", "/");
        }


        public virtual bool DbSetProcedureUserInfo(User user, bool del = false)
        {
            bool bresult = false;
            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        using (OracleCommand sqlcmd = new OracleCommand())
                        {
                            try
                            {
                                sqlcmd.CommandText = "USP_STC_USER_INFO_SET";
                                sqlcmd.CommandType = CommandType.StoredProcedure;
                                sqlcmd.Connection = conn;

                                OracleParameter R_RESULT = new OracleParameter("R_RESULT", OracleDbType.NVarchar2, 20);
                                OracleParameter R_TEMP = new OracleParameter("R_TEMP", OracleDbType.NVarchar2, 20);

                                sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2, ParameterDirection.Input).Value = GlobalData.Current.EQPID;
                                sqlcmd.Parameters.Add("P_USER_ID", OracleDbType.NVarchar2, ParameterDirection.Input).Value = user.UserID;
                                sqlcmd.Parameters.Add("P_USER_NM", OracleDbType.NVarchar2, ParameterDirection.Input).Value = user.UserName;
                                sqlcmd.Parameters.Add("P_TEAM_NM", OracleDbType.NVarchar2, ParameterDirection.Input).Value = user.TeamName;   //2024.06.08 lim, OY_Merge 
                                sqlcmd.Parameters.Add("P_PASSWORD_GB", OracleDbType.NVarchar2, ParameterDirection.Input).Value = user.UserPassword;
                                sqlcmd.Parameters.Add("P_GROUP_GB", OracleDbType.Int32, ParameterDirection.Input).Value = Convert.ToInt32(user.UserLevel);
                                sqlcmd.Parameters.Add("P_USER_STAT", OracleDbType.Char, ParameterDirection.Input).Value = user.UserUse ? '1' : '0';
                                sqlcmd.Parameters.Add("P_USING_DTTM", OracleDbType.Int32, ParameterDirection.Input).Value = user.AutoLogoutMinute;
                                sqlcmd.Parameters.Add("P_DEL", OracleDbType.Char, ParameterDirection.Input).Value = del ? '1' : '0';
                                sqlcmd.Parameters.Add(R_RESULT).Direction = ParameterDirection.Output;
                                sqlcmd.Parameters.Add(R_TEMP).Direction = ParameterDirection.Output;

                                sqlcmd.ExecuteNonQuery();

                                string temp = string.Format("Add Result = {0}", R_RESULT.Value.ToString());
                                string temp1 = string.Format("Add Temp = {0}", R_TEMP.Value.ToString());

                                //LogManager.WriteConsoleLog(eLogLevel.Info, temp1 + ", " + temp);
                                LogManager.WriteDBLog(eLogLevel.Info, temp1 + ", " + temp, false);

                                if (!(temp == "NO_USER" || temp1 == "EXCEPTION"))
                                    bresult = true;
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();
                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return bresult;
            }
            return bresult;
        }

        #region DbGetUserInfo 사용 안함
        public List<User> DbGetUserInfo()
        {
            string sql = string.Empty;
            List<User> Userlist = new List<User>();

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        sql = string.Format("SELECT SCS_CD, USER_ID, PASSWORD_GB, GROUP_GB, USE_STAT " +
                            "FROM TB_USER_INFO WHERE SCS_CD='{0}'", GlobalData.Current.EQPID);

                        using (OracleCommand sqlcmd = new OracleCommand(sql, conn))
                        {
                            try
                            {
                                using (OracleDataReader rowdata = sqlcmd.ExecuteReader())
                                {
                                    try
                                    {
                                        LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                                        LogManager.WriteDBLog(eLogLevel.Info, "Data Exist = {0}" + rowdata.HasRows.ToString(), false);

                                        while (rowdata.Read())
                                        {
                                            DataTable schematable = rowdata.GetSchemaTable();

                                            string datavalue = string.Empty;
                                            foreach (DataRow row in schematable.Rows)
                                            {
                                                foreach (DataColumn column in schematable.Columns)
                                                {
                                                    string temp = row[column].ToString() + "=" + rowdata[row[column].ToString()].ToString() + ",";
                                                    datavalue += temp;
                                                    break;
                                                }
                                            }

                                            LogManager.WriteDBLog(eLogLevel.Info, datavalue, false);

                                            User DbUser = new User();

                                            DbUser.UserName = rowdata["USER_ID"].ToString();
                                            DbUser.UserID = rowdata["USER_ID"].ToString();
                                            DbUser.UserPassword = rowdata["PASSWORD_GB"].ToString();
                                            DbUser.UserLevel = (eUserLevel)Convert.ToInt32(rowdata["GROUP_GB"]);
                                            DbUser.UserUse = Convert.ToBoolean(Convert.ToInt32(rowdata["USE_STAT"]));

                                            Userlist.Add(DbUser);
                                        }
                                    }
                                    finally
                                    {
                                        if (rowdata != null)
                                        {
                                            rowdata.Close(); //240503 OracleDataReader 는 사용후 close 필요함.
                                            rowdata.Dispose();
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();
                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return Userlist;
            }

            return Userlist;
        }
        #endregion

        public List<User> DbGetProcedureUserInfo()
        {
            List<User> Userlist = new List<User>();
            DataSet dataSet = new DataSet();

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        using (OracleCommand sqlcmd = new OracleCommand("UFN_STC_USER_INFO_GET", conn))
                        {
                            try
                            {
                                //sqlcmd.CommandText = "USP_STC_USER_INFO_GET";
                                sqlcmd.CommandType = CommandType.StoredProcedure;
                                //sqlcmd.Connection = conn;

                                //sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2, ParameterDirection.Input).Value = GlobalData.Current.EQPID;
                                //sqlcmd.Parameters.Add("OUT_DATA", OracleDbType.RefCursor, ParameterDirection.Output);
                                OracleParameter output = sqlcmd.Parameters.Add("TMP_DATA", OracleDbType.RefCursor);
                                output.Direction = ParameterDirection.ReturnValue;

                                OracleParameter input1 = sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2);
                                input1.Direction = ParameterDirection.Input;
                                input1.Value = GlobalData.Current.EQPID;

                                sqlcmd.ExecuteNonQuery();

                                using (OracleDataAdapter oradata = new OracleDataAdapter(sqlcmd))
                                {
                                    try
                                    {
                                        oradata.Fill(dataSet);
                                    }
                                    finally
                                    {
                                        if (oradata != null)
                                            oradata.Dispose();
                                    }
                                }
                            }
                            finally
                            {
                                for (int i = 0; i < sqlcmd.Parameters.Count; i++)
                                {
                                    sqlcmd.Parameters[i].Dispose();
                                }

                                sqlcmd.Parameters.Clear();

                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }

                int table = dataSet.Tables.Count;
                for (int i = 0; i < table; i++)// set the table value in list one by one
                {
                    foreach (DataRow dr in dataSet.Tables[i].Rows)
                    {
                        Userlist.Add(new User
                        {
                            UserID = Convert.ToString(dr["USER_ID"]),
                            UserName = Convert.ToString(dr["USER_NM"]),     //2024.06.08 lim, OY Merge
                            TeamName = Convert.ToString(dr["TEAM_NM"]),     //2024.06.08 lim, OY Merge
                            UserPassword = Convert.ToString(dr["PASSWORD_GB"]),
                            UserLevel = (eUserLevel)Convert.ToInt32(dr["GROUP_GB"]),
                            UserUse = Convert.ToBoolean(Convert.ToInt32(dr["USE_STAT"])),
                            AutoLogoutMinute = string.IsNullOrEmpty(dr["USING_DTTM"].ToString()) ? 0 : Convert.ToInt32(dr["USING_DTTM"])
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return Userlist;
            }
            return Userlist;
        }

        #region DbSelectConfigInfo 사용 안함
        public bool DbSelectConfigInfo()
        {
            bool findok = false;
            string sql = string.Empty;

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();
                        sql = string.Format("SELECT * FROM TB_CONFIG_INFO WHERE SCS_CD='{0}'", GlobalData.Current.EQPID);

                        using (OracleCommand sqlcmd = new OracleCommand(sql, conn))
                        {
                            try
                            {
                                using (OracleDataReader rowdata = sqlcmd.ExecuteReader())
                                {
                                    try
                                    {
                                        if (rowdata.HasRows)
                                            findok = true;
                                    }
                                    finally
                                    {
                                        if (rowdata != null)
                                        {
                                            rowdata.Close(); //240503 OracleDataReader 는 사용후 close 필요함.
                                            rowdata.Dispose();
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return findok;
            }
            return findok;
        }
        #endregion

        #region DbGetConfigInfo 사용 안함
        public void DbGetConfigInfo(object sectionname, int num = 0)
        {
            string sql = string.Empty;

            try
            {
                //var bindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;

                //foreach (FieldInfo info in typeof(GlobalData).GetFields(bindingFlags))
                //{
                //    if(sectionname.ToString().Contains(info.Name) || info.Name.Contains(sectionnum + "Element"))
                //    {
                {
                    Type sectiontype = sectionname.GetType();

                    PropertyInfo[] props = sectiontype.GetProperties();

                    using (OracleConnection conn = new OracleConnection(OracleDBPath))
                    {
                        try
                        {
                            conn.Open();

                            foreach (var prop in props)
                            {
                                if (prop.Name.Contains("Element"))
                                {
                                    Type subtype = Type.GetType(prop.PropertyType.FullName);
                                    PropertyInfo[] subprops = subtype.GetProperties();
                                    object propsection = Activator.CreateInstance(subtype);
                                    string tempsectionname = sectiontype.Name + "." + prop.Name;
                                    //string config_gb = info.Name + "_" + prop.Name;

                                    foreach (var subprop in subprops)
                                    {
                                        sql = string.Format("SELECT CONFIG_VAL " +
                                            "FROM TB_CONFIG_INFO WHERE SCS_CD='{0}' AND CONFIG_GB='{1}' AND CONFIG_NM='{2}'", GlobalData.Current.EQPID, tempsectionname, subprop.Name);

                                        using (OracleCommand sqlcmd = new OracleCommand(sql, conn))
                                        {
                                            try
                                            {
                                                using (OracleDataReader rowdata = sqlcmd.ExecuteReader())
                                                {
                                                    try
                                                    {
                                                        while (rowdata.Read())
                                                        {
                                                            DataTable schematable = rowdata.GetSchemaTable();

                                                            string datavalue = string.Empty;
                                                            foreach (DataRow row in schematable.Rows)
                                                            {
                                                                foreach (DataColumn column in schematable.Columns)
                                                                {
                                                                    string temp = row[column].ToString() + "=" + rowdata[row[column].ToString()].ToString() + ",";
                                                                    datavalue += temp;
                                                                    break;
                                                                }
                                                            }
                                                            LogManager.WriteDBLog(eLogLevel.Info, datavalue, false);
                                                            string value = rowdata["CONFIG_VAL"].ToString();

                                                            if (subprop.PropertyType.IsEnum)
                                                            {
                                                                subprop.SetValue(propsection, Enum.Parse(subprop.PropertyType, rowdata["CONFIG_VAL"].ToString()));
                                                            }
                                                            else if (subprop.PropertyType == typeof(Boolean))
                                                            {
                                                                subprop.SetValue(propsection, Boolean.Parse(value));
                                                            }
                                                            else if (subprop.PropertyType == typeof(string))
                                                            {
                                                                subprop.SetValue(propsection, value);
                                                            }
                                                            else if (subprop.PropertyType == typeof(Int32))
                                                            {
                                                                subprop.SetValue(propsection, Int32.Parse(value));
                                                            }
                                                            else if (subprop.PropertyType == typeof(short))
                                                            {
                                                                subprop.SetValue(propsection, short.Parse(value));
                                                            }
                                                        }
                                                    }
                                                    finally
                                                    {
                                                        if (rowdata != null)
                                                        {
                                                            rowdata.Close(); //240503 OracleDataReader 는 사용후 close 필요함.
                                                            rowdata.Dispose();
                                                        }
                                                    }
                                                }
                                            }
                                            finally
                                            {
                                                if (sqlcmd != null)
                                                    sqlcmd.Dispose();
                                            }
                                        }
                                    }
                                    prop.SetValue(sectionname, propsection);
                                }
                                else
                                {
                                    string tempsectionname = string.Empty;
                                    if (sectiontype.Name == "PLCElement")
                                    {
                                        tempsectionname = string.Format("Plcs.PLC{0}", num + 1);
                                    }
                                    //220917 조숭진 추가 s
                                    else if (sectiontype.Name == "PLCSection" && prop.Name != "PLCSimulMode")
                                    {
                                        continue;
                                    }
                                    else if (sectiontype.Name == "PLCSection" && prop.Name == "PLCSimulMode")
                                    {
                                        tempsectionname = "Plcs";
                                    }
                                    //220917 조숭진 추가 e
                                    else
                                    {
                                        tempsectionname = sectiontype.Name;
                                    }


                                    sql = string.Format("SELECT CONFIG_VAL " +
                                        "FROM TB_CONFIG_INFO WHERE SCS_CD='{0}' AND CONFIG_GB='{1}' AND CONFIG_NM='{2}'", GlobalData.Current.EQPID, tempsectionname, prop.Name);

                                    using (OracleCommand sqlcmd = new OracleCommand(sql, conn))
                                    {
                                        try
                                        {
                                            using (OracleDataReader rowdata = sqlcmd.ExecuteReader())
                                            {
                                                try
                                                {
                                                    while (rowdata.Read())
                                                    {
                                                        DataTable schematable = rowdata.GetSchemaTable();

                                                        string datavalue = string.Empty;
                                                        foreach (DataRow row in schematable.Rows)
                                                        {
                                                            foreach (DataColumn column in schematable.Columns)
                                                            {
                                                                string temp = row[column].ToString() + "=" + rowdata[row[column].ToString()].ToString() + ",";
                                                                datavalue += temp;
                                                                break;
                                                            }
                                                        }
                                                        LogManager.WriteDBLog(eLogLevel.Info, datavalue, false);
                                                        string value = rowdata["CONFIG_VAL"].ToString();

                                                        if (prop.PropertyType.IsEnum)
                                                        {
                                                            prop.SetValue(sectionname, Enum.Parse(prop.PropertyType, rowdata["CONFIG_VAL"].ToString()));
                                                        }
                                                        else if (prop.PropertyType == typeof(Boolean))
                                                        {
                                                            prop.SetValue(sectionname, Boolean.Parse(value));
                                                        }
                                                        else if (prop.PropertyType == typeof(string))
                                                        {
                                                            prop.SetValue(sectionname, value);
                                                        }
                                                        else if (prop.PropertyType == typeof(Int32))
                                                        {
                                                            prop.SetValue(sectionname, Int32.Parse(value));
                                                        }
                                                        else if (prop.PropertyType == typeof(short))
                                                        {
                                                            prop.SetValue(sectionname, short.Parse(value));
                                                        }
                                                    }
                                                }
                                                finally
                                                {
                                                    if (rowdata != null)
                                                    {
                                                        rowdata.Close(); //240503 OracleDataReader 는 사용후 close 필요함.
                                                        rowdata.Dispose();
                                                    }
                                                }
                                            }
                                        }
                                        finally
                                        {
                                            if (sqlcmd != null)
                                                sqlcmd.Dispose();
                                        }
                                    }
                                }
                            }
                        }
                        finally
                        {
                            conn.Close();

                            if (conn != null)
                                conn.Dispose();
                        }
                    }
                }
                //break;
                //    }
                //}
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }
        #endregion

        public void DbGetGlobalConfigInfo(object sectionname, int num = 0)
        {
            try
            {
                //await Task.Run(() =>
                //{
                    Type sectiontype = sectionname.GetType();
                    PropertyInfo[] props = sectiontype.GetProperties();
                    string tempsectionname = string.Empty;
                    string temppropname = string.Empty;

                    if (sectiontype.Name == "PLCElement")
                    {
                        tempsectionname = string.Format("Plcs.PLC{0}", num + 1);
                    }
                    else if (sectiontype.Name == "PLCSection")
                    {
                        tempsectionname = "Plcs";
                    }
                    else
                    {
                        tempsectionname = sectiontype.Name;
                    }

                    DataSet dataSet = new DataSet();
                    dataSet = DbGetProcedureGlobalConfigInfo('0', tempsectionname);

                    foreach (var prop in props)
                    {
                        if (sectiontype.Name == "PLCSection" &&
                            (prop.Name == "Count" || prop.Name == "Item"))
                            continue;

                        if (prop.Name.Contains("Element"))
                        {
                            Type subtype = Type.GetType(prop.PropertyType.FullName);
                            PropertyInfo[] subprops = subtype.GetProperties();
                            object propsection = Activator.CreateInstance(subtype);
                            tempsectionname = sectiontype.Name + "." + prop.Name;

                            dataSet = DbGetProcedureGlobalConfigInfo('0', tempsectionname);

                            foreach (var subprop in subprops)
                            {
                                int table = dataSet.Tables.Count;
                                for (int i = 0; i < table; i++)// set the table value in list one by one
                                {
                                    foreach (DataRow dr in dataSet.Tables[i].Rows)
                                    {
                                        if (dr["CONFIG_NM"].ToString() != subprop.Name)
                                            continue;

                                        string value = dr["CONFIG_VAL"].ToString();
                                        if (subprop.PropertyType.IsEnum)
                                        {
                                            subprop.SetValue(propsection, Enum.Parse(subprop.PropertyType, value));
                                        }
                                        else if (subprop.PropertyType == typeof(Boolean))
                                        {
                                            subprop.SetValue(propsection, Boolean.Parse(value));
                                        }
                                        else if (subprop.PropertyType == typeof(string))
                                        {
                                            subprop.SetValue(propsection, value);
                                        }
                                        else if (subprop.PropertyType == typeof(Int32))
                                        {
                                            subprop.SetValue(propsection, Int32.Parse(value));
                                        }
                                        else if (subprop.PropertyType == typeof(short))
                                        {
                                            subprop.SetValue(propsection, short.Parse(value));
                                        }
                                        break;
                                    }
                                }
                            }
                            prop.SetValue(sectionname, propsection);
                        }
                        else
                        {
                            int table = dataSet.Tables.Count;
                            for (int i = 0; i < table; i++)// set the table value in list one by one
                            {
                                foreach (DataRow dr in dataSet.Tables[i].Rows)
                                {
                                    if (dr["CONFIG_NM"].ToString() != prop.Name)
                                        continue;

                                    string value = dr["CONFIG_VAL"].ToString();

                                    if (prop.PropertyType.IsEnum)
                                    {
                                        prop.SetValue(sectionname, Enum.Parse(prop.PropertyType, value));
                                    }
                                    else if (prop.PropertyType == typeof(Boolean))
                                    {
                                        prop.SetValue(sectionname, Boolean.Parse(value));
                                    }
                                    else if (prop.PropertyType == typeof(string))
                                    {
                                        prop.SetValue(sectionname, value);
                                    }
                                    else if (prop.PropertyType == typeof(Int32))
                                    {
                                        prop.SetValue(sectionname, Int32.Parse(value));
                                    }
                                    else if (prop.PropertyType == typeof(short))
                                    {
                                        prop.SetValue(sectionname, short.Parse(value));
                                    }
                                    break;
                                }
                            }
                        }
                    }
                //});
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }

        public DataSet DbGetProcedureGlobalConfigInfo(char configall, string configgb = "", string confignm = "")
        {
            DataSet dataSet = new DataSet();

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();
                        using (OracleCommand sqlcmd = new OracleCommand("UFN_STC_CONFIG_INFO_GET", conn))
                        {
                            try
                            {
                                //sqlcmd.CommandText = "USP_STC_CONFIG_INFO_GET";
                                sqlcmd.CommandType = CommandType.StoredProcedure;
                                //sqlcmd.Connection = conn;

                                //sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2, ParameterDirection.Input).Value = GlobalData.Current.EQPID;
                                //sqlcmd.Parameters.Add("P_CONFIG_GB", OracleDbType.NVarchar2, ParameterDirection.Input).Value = configgb;
                                //sqlcmd.Parameters.Add("P_CONFIG_NM", OracleDbType.NVarchar2, ParameterDirection.Input).Value = confignm;
                                //sqlcmd.Parameters.Add("P_CONFIG_ALL", OracleDbType.Char, ParameterDirection.Input).Value = configall;
                                //sqlcmd.Parameters.Add("OUT_DATA", OracleDbType.RefCursor, ParameterDirection.Output);
                                OracleParameter output = sqlcmd.Parameters.Add("TMP_DATA", OracleDbType.RefCursor);
                                output.Direction = ParameterDirection.ReturnValue;

                                OracleParameter input1 = sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2);
                                input1.Direction = ParameterDirection.Input;
                                input1.Value = GlobalData.Current.EQPID;

                                OracleParameter input2 = sqlcmd.Parameters.Add("P_CONFIG_GB", OracleDbType.NVarchar2);
                                input2.Direction = ParameterDirection.Input;
                                input2.Value = configgb;

                                OracleParameter input3 = sqlcmd.Parameters.Add("P_CONFIG_NM", OracleDbType.NVarchar2);
                                input3.Direction = ParameterDirection.Input;
                                input3.Value = confignm;

                                OracleParameter input4 = sqlcmd.Parameters.Add("P_CONFIG_ALL", OracleDbType.Char);
                                input4.Direction = ParameterDirection.Input;
                                input4.Value = configall;

                                sqlcmd.ExecuteNonQuery();

                                using (OracleDataAdapter oradata = new OracleDataAdapter(sqlcmd))
                                {
                                    try
                                    {
                                        oradata.Fill(dataSet);
                                    }
                                    finally
                                    {
                                        if (oradata != null)
                                            oradata.Dispose();
                                    }
                                }
                            }
                            finally
                            {
                                //for (int i = 0; i < sqlcmd.Parameters.Count; i++)
                                //{
                                //    sqlcmd.Parameters[i].Dispose();
                                //}

                                //sqlcmd.Parameters.Clear();

                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return dataSet;
            }

            return dataSet;
        }

        #region DBGetInfoCount 사용 안함
        public int DBGetInfoCount(string configgb)
        {
            int count = 0;
            string sql = string.Empty;

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        sql = string.Format("SELECT CONFIG_VAL FROM TB_CONFIG_INFO " +
                            "WHERE SCS_CD='{0}' AND CONFIG_GB='{1}' AND CONFIG_NM='Count'", GlobalData.Current.EQPID, configgb);

                        using (OracleCommand sqlcmd = new OracleCommand(sql, conn))
                        {
                            try
                            {
                                if (sqlcmd.ExecuteScalar() == null)
                                    count = 0;
                                else
                                {
                                    string temp = sqlcmd.ExecuteScalar().ToString();
                                    count = Convert.ToInt32(temp);
                                }
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return count;
            }

            return count;
        }
        #endregion

        public bool GetConfigInfo(ConfigurationSection section, PLCConfigElement plc = null)
        {
            Dictionary<string, ConfigurationElement> dicelements = new Dictionary<string, ConfigurationElement>();
            //List<ConfigurationElement> elements = new List<ConfigurationElement>();

            try
            {
                if (plc != null)
                {
                    Type hightype = plc.GetType();
                    PropertyInfo[] props = hightype.GetProperties();

                    foreach (var prop in props)
                    {
                        if (prop.Module.Name.Equals("BoxPrint.exe"))
                        {
                            string value = prop.GetValue(plc).ToString();
                            string sectionname = string.Format("{0}.PLC{1}", section.SectionInformation.SectionName, plc.Num + 1);
                            DbSetProcedureConfigInfo(sectionname, prop.Name, value, string.Empty, string.Empty);
                        }
                    }
                }
                else
                {
                    PropertyInformationCollection sectionprop = section.ElementInformation.Properties;

                    for (int i = 0; i < sectionprop.Count; i++)
                    {
                        string keytemp = sectionprop.Keys[i];
                        if (sectionprop[keytemp].Type.Name.Contains("Element"))
                        {
                            dicelements.Add(keytemp, (ConfigurationElement)sectionprop[keytemp].Value);
                            //elements.Add((ConfigurationElement)sectionprop[keytemp].Value);
                        }
                    }

                    Type hightype = section.GetType();
                    PropertyInfo[] props = hightype.GetProperties();

                    foreach (var prop in props)
                    {
                        if (prop.Module.Name.Equals("BoxPrint.exe"))
                        {
                            if (prop.PropertyType.BaseType == typeof(ConfigurationElement))
                            {
                                //prop.GetCustomAttribute(typeof(ConfigurationElement))
                                Type subtype = prop.GetValue(section).GetType();
                                PropertyInfo[] subprops = subtype.GetProperties();

                                foreach (var subprop in subprops)
                                {
                                    if (subprop.Module.Name.Equals("BoxPrint.exe"))
                                    {
                                        string subvalue = string.Empty;
                                        string subconfigname = section.SectionInformation.SectionName + "." + prop.Name;

                                        IList<CustomAttributeData> templist = prop.GetCustomAttributesData();
                                        for (int i = 0; i < templist.Count; i++)
                                        {
                                            bool findok = false;

                                            for (int j = 0; j < templist[i].ConstructorArguments.Count; j++)
                                            {
                                                string arg = templist[i].ConstructorArguments[j].Value.ToString();
                                                var dicelement = dicelements.Where(r => r.Key == arg).FirstOrDefault();

                                                if (dicelement.Value != null)
                                                {
                                                    findok = true;
                                                    subvalue = subprop.GetValue(dicelement.Value).ToString();
                                                    break;
                                                }
                                            }

                                            if (findok)
                                                break;
                                        }

                                        DbSetProcedureConfigInfo(subconfigname, subprop.Name, subvalue, string.Empty, string.Empty);
                                    }
                                }

                            }
                            else
                            {
                                string value = prop.GetValue(section).ToString();
                                DbSetProcedureConfigInfo(section.SectionInformation.SectionName, prop.Name, value, string.Empty, string.Empty);
                                //Console.WriteLine(value);
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return false;
            }

            return true;
        }

        public bool DbSetProcedureConfigInfo(string section, string name, string value, string defaultvalue = "", string description = "", int number = 0)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        using (OracleCommand sqlcmd = new OracleCommand())
                        {
                            try
                            {
                                sqlcmd.CommandText = "USP_STC_CONFIG_INFO_SET";
                                sqlcmd.CommandType = CommandType.StoredProcedure;
                                sqlcmd.Connection = conn;

                                OracleParameter R_RESULT = new OracleParameter("R_RESULT", OracleDbType.NVarchar2, 20);
                                OracleParameter R_TEMP = new OracleParameter("R_TEMP", OracleDbType.NVarchar2, 20);

                                if (name != "Count")
                                {
                                    DbGetGlobalConfigNumber(section, name, out confignumber);

                                    if (number == 0 && confignumber == 0)
                                    {
                                        //confignumber = DBGetConfigCount() + 1;
                                        DataSet dataSet = DbGetProcedureGlobalConfigInfo('1');
                                        confignumber = dataSet.Tables[dataSet.Tables.Count - 1].Rows.Count + 1;
                                    }
                                    else if (number != 0)
                                        confignumber = number;

                                    sqlcmd.Parameters.Add("P_NUM", OracleDbType.Int32, ParameterDirection.Input).Value = confignumber;
                                }
                                else
                                {
                                    sqlcmd.Parameters.Add("P_NUM", OracleDbType.Int32, ParameterDirection.Input).Value = 0;
                                }

                                //sqlcmd.Parameters.Add("P_NUM", OracleDbType.Int32, ParameterDirection.Input).Value = confignumber;
                                sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2, ParameterDirection.Input).Value = GlobalData.Current.EQPID;
                                sqlcmd.Parameters.Add("P_CONFIG_NM", OracleDbType.NVarchar2, ParameterDirection.Input).Value = name;
                                sqlcmd.Parameters.Add("P_CONFIG_GB", OracleDbType.NVarchar2, ParameterDirection.Input).Value = section;
                                sqlcmd.Parameters.Add("P_CONFIG_VAL", OracleDbType.NVarchar2, ParameterDirection.Input).Value = value;
                                //230524 우선 default value는 기존 value값으로 넣는다.
                                //sqlcmd.Parameters.Add("P_CONFIG_DEF", OracleDbType.NVarchar2, ParameterDirection.Input).Value = string.IsNullOrEmpty(defaultvalue) ? string.Empty : defaultvalue;
                                sqlcmd.Parameters.Add("P_CONFIG_DEF", OracleDbType.NVarchar2, ParameterDirection.Input).Value = value;

                                sqlcmd.Parameters.Add("P_CONFIG_DES", OracleDbType.NVarchar2, ParameterDirection.Input).Value = string.IsNullOrEmpty(description) ? string.Empty : description;
                                sqlcmd.Parameters.Add(R_RESULT).Direction = ParameterDirection.Output;
                                sqlcmd.Parameters.Add(R_TEMP).Direction = ParameterDirection.Output;

                                sqlcmd.ExecuteNonQuery();

                                string temp = string.Format("Add Result = {0}", R_RESULT.Value.ToString());
                                string temp1 = string.Format("Add Temp = {0}", R_TEMP.Value.ToString());

                                //LogManager.WriteConsoleLog(eLogLevel.Info, temp1 + ", " + temp);
                                LogManager.WriteDBLog(eLogLevel.Info, temp1 + ", " + temp, false);
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return false;
            }
            return true;
        }

        #region DBGetCVInfoData 사용 안함
        public Dictionary<string, string> DBGetCVInfoData(string configgb)
        {
            Dictionary<string, string> dictemp = new Dictionary<string, string>();
            string sql = string.Empty;

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        sql = string.Format("SELECT CONFIG_NM, CONFIG_VAL FROM TB_CONFIG_INFO " +
                            "WHERE SCS_CD='{0}' AND CONFIG_GB='{1}'", GlobalData.Current.EQPID, configgb);

                        using (OracleCommand sqlcmd = new OracleCommand(sql, conn))
                        {
                            try
                            {
                                using (OracleDataReader rowdata = sqlcmd.ExecuteReader())
                                {
                                    try
                                    {
                                        while (rowdata.Read())
                                        {
                                            DataTable schematable = rowdata.GetSchemaTable();

                                            string datavalue = string.Empty;
                                            foreach (DataRow row in schematable.Rows)
                                            {
                                                foreach (DataColumn column in schematable.Columns)
                                                {
                                                    string temp = row[column].ToString() + "=" + rowdata[row[column].ToString()].ToString() + ",";
                                                    datavalue += temp;
                                                    break;
                                                }
                                            }

                                            LogManager.WriteDBLog(eLogLevel.Info, datavalue, false);

                                            dictemp.Add(rowdata["CONFIG_NM"].ToString(), rowdata["CONFIG_VAL"].ToString());
                                        }
                                    }
                                    finally
                                    {
                                        if (rowdata != null)
                                        {
                                            rowdata.Close(); //240503 OracleDataReader 는 사용후 close 필요함.
                                            rowdata.Dispose();
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return dictemp;
            }

            return dictemp;
        }
        #endregion

        //public Dictionary<string, string> DbGetProcedureCVInfo(string configgb)
        public Dictionary<string, string> DbGetGlobalCVInfo(string configgb)
        {
            Dictionary<string, string> dictemp = new Dictionary<string, string>();
            DataSet dataSet = new DataSet();

            try
            {
                dataSet = DbGetProcedureGlobalConfigInfo('0', configgb);

                int table = dataSet.Tables.Count;
                for (int i = 0; i < table; i++)// set the table value in list one by one
                {
                    foreach (DataRow dr in dataSet.Tables[i].Rows)
                    {
                        dictemp.Add(dr["CONFIG_NM"].ToString(), dr["CONFIG_VAL"].ToString());
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return dictemp;
            }
            return dictemp;
        }

        #region DbGetAllConfigInfo 사용 안함
        public ObservableCollection<ConfigViewModelData> DbGetAllConfigInfo()
        {
            ObservableCollection<ConfigViewModelData> alldata = new ObservableCollection<ConfigViewModelData>();
            string sql = string.Empty;

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        sql = string.Format("SELECT CONFIG_NO, CONFIG_NM, CONFIG_GB, CONFIG_VAL, CONFIG_DEF, CONFIG_DES  " +
                            "FROM TB_CONFIG_INFO WHERE SCS_CD='{0}' ORDER BY CONFIG_NO ASC", GlobalData.Current.EQPID);

                        using (OracleCommand sqlcmd = new OracleCommand(sql, conn))
                        {
                            try
                            {
                                using (OracleDataReader rowdata = sqlcmd.ExecuteReader())
                                {
                                    try
                                    {
                                        while (rowdata.Read())
                                        {
                                            DataTable schematable = rowdata.GetSchemaTable();

                                            string datavalue = string.Empty;
                                            foreach (DataRow row in schematable.Rows)
                                            {
                                                foreach (DataColumn column in schematable.Columns)
                                                {
                                                    string temp = row[column].ToString() + "=" + rowdata[row[column].ToString()].ToString() + ",";
                                                    datavalue += temp;
                                                    break;
                                                }
                                            }

                                            LogManager.WriteDBLog(eLogLevel.Info, datavalue, false);

                                            if (!string.IsNullOrEmpty(rowdata["CONFIG_NO"].ToString()))
                                            {
                                                alldata.Add(
                                                    new ConfigViewModelData()
                                                    {
                                                        ConfigNumber = rowdata["CONFIG_NO"].ToString(),
                                                        ConfigName = rowdata["CONFIG_NM"].ToString(),
                                                        ConfigType = rowdata["CONFIG_GB"].ToString(),
                                                        ConfigValue = rowdata["CONFIG_VAL"].ToString(),
                                                        ConfigDefaultValue = string.IsNullOrEmpty(rowdata["CONFIG_DEF"].ToString()) ? string.Empty : rowdata["CONFIG_DEF"].ToString(),
                                                        ConfigDescription = string.IsNullOrEmpty(rowdata["CONFIG_DES"].ToString()) ? string.Empty : rowdata["CONFIG_DES"].ToString()
                                                    });
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        if (rowdata != null)
                                        {
                                            rowdata.Close(); //240503 OracleDataReader 는 사용후 close 필요함.
                                            rowdata.Dispose();
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return alldata;
            }

            return alldata;
        }
        #endregion

        #region DbGetConfigInfo 사용 안함
        public bool DbGetConfigInfo(string section, string name, out string value)
        {
            bool bfind = false;
            string sql = string.Empty;
            value = string.Empty;

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        sql = string.Format("SELECT CONFIG_VAL " +
                            "FROM TB_CONFIG_INFO WHERE SCS_CD='{0}' AND CONFIG_GB='{1}' AND CONFIG_NM='{2}'", GlobalData.Current.EQPID, section, name);

                        using (OracleCommand sqlcmd = new OracleCommand(sql, conn))
                        {
                            try
                            {
                                using (OracleDataReader rowdata = sqlcmd.ExecuteReader())
                                {
                                    try
                                    {
                                        while (rowdata.Read())
                                        {
                                            DataTable schematable = rowdata.GetSchemaTable();

                                            string datavalue = string.Empty;
                                            foreach (DataRow row in schematable.Rows)
                                            {
                                                foreach (DataColumn column in schematable.Columns)
                                                {
                                                    string temp = row[column].ToString() + "=" + rowdata[row[column].ToString()].ToString() + ",";
                                                    datavalue += temp;
                                                    break;
                                                }
                                            }
                                            LogManager.WriteDBLog(eLogLevel.Info, datavalue, false);

                                            if (!string.IsNullOrEmpty(rowdata["CONFIG_VAL"].ToString()))
                                            {
                                                value = rowdata["CONFIG_VAL"].ToString();
                                                bfind = true;
                                            }
                                        }
                                    }
                                    finally
                                    {
                                        if (rowdata != null)
                                        {
                                            rowdata.Close(); //240503 OracleDataReader 는 사용후 close 필요함.
                                            rowdata.Dispose();
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return bfind;
            }
            return bfind;
        }
        #endregion

        public bool DbGetGlobalConfigValue(string configgb, string name, out string value)
        {
            bool bfind = false;
            value = string.Empty;

            try
            {
                DataSet dataSet = new DataSet();
                dataSet = DbGetProcedureGlobalConfigInfo('0', configgb, name);

                int table = dataSet.Tables.Count;
                for (int i = 0; i < table; i++)// set the table value in list one by one
                {
                    foreach (DataRow dr in dataSet.Tables[i].Rows)
                    {
                        if (!string.IsNullOrEmpty(dr["CONFIG_VAL"].ToString()))
                        {
                            value = dr["CONFIG_VAL"].ToString();
                            bfind = true;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return bfind;
            }
            return bfind;
        }

        public bool DbGetGlobalConfigNumber(string configgb, string name, out int value)
        {
            bool bfind = false;
            value = 0;

            try
            {
                DataSet dataSet = new DataSet();
                dataSet = DbGetProcedureGlobalConfigInfo('0', configgb, name);

                int table = dataSet.Tables.Count;
                for (int i = 0; i < table; i++)// set the table value in list one by one
                {
                    foreach (DataRow dr in dataSet.Tables[i].Rows)
                    {
                        if (!string.IsNullOrEmpty(dr["CONFIG_NO"].ToString()))
                        {
                            value = Convert.ToInt32(dr["CONFIG_NO"].ToString());
                            bfind = true;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return bfind;
            }
            return bfind;
        }

        #region DBSelectCVConfigInfo 사용 안함
        public int DBSelectCVConfigInfo(string configgb, string configname)
        {
            int number = 0;

            string sql = string.Empty;

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();
                        sql = string.Format("SELECT CONFIG_NO FROM TB_CONFIG_INFO " +
                            "WHERE SCS_CD='{0}' AND CONFIG_GB='{1}' AND CONFIG_NM='{2}'", GlobalData.Current.EQPID, configgb, configname);

                        using (OracleCommand sqlcmd = new OracleCommand(sql, conn))
                        {
                            try
                            {
                                using (OracleDataReader rowdata = sqlcmd.ExecuteReader())
                                {
                                    try
                                    {
                                        LogManager.WriteDBLog(eLogLevel.Info, sql, true);
                                        LogManager.WriteDBLog(eLogLevel.Info, "Data Exist = {0}" + rowdata.HasRows.ToString(), false);

                                        while (rowdata.Read())
                                        {
                                            DataTable schematable = rowdata.GetSchemaTable();

                                            string datavalue = string.Empty;
                                            foreach (DataRow row in schematable.Rows)
                                            {
                                                foreach (DataColumn column in schematable.Columns)
                                                {
                                                    string temp = row[column].ToString() + "=" + rowdata[row[column].ToString()].ToString() + ",";
                                                    datavalue += temp;
                                                    break;
                                                }
                                            }

                                            number = Convert.ToInt32(rowdata["CONFIG_NO"]);
                                        }
                                    }
                                    finally
                                    {
                                        if (rowdata != null)
                                        {
                                            rowdata.Close(); //240503 OracleDataReader 는 사용후 close 필요함.
                                            rowdata.Dispose();
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return number;
            }
            return number;
        }
        #endregion

        #region DBGetConfigCount 사용 안함
        public int DBGetConfigCount()
        {
            int count = 0;
            string sql = string.Empty;

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();
                        sql = string.Format("SELECT COUNT(CONFIG_NO) FROM TB_CONFIG_INFO WHERE SCS_CD='{0}' AND CONFIG_NO > 0 ", GlobalData.Current.EQPID);

                        using (OracleCommand sqlcmd = new OracleCommand(sql, conn))
                        {
                            try
                            {
                                object temp = sqlcmd.ExecuteScalar();
                                if (temp != null)
                                {
                                    count = Convert.ToInt32(temp);
                                    confignumber = count;
                                }
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return count;
            }

            return count;
        }
        #endregion

        public virtual bool DBTableAllDelete(string tablename)
        {
            string sql = string.Empty;
            OracleTransaction tran = null;

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();
                        tran = conn.BeginTransaction();

                        sql = string.Format("DELETE FROM {0}", tablename);

                        using (OracleCommand sqlcmd = new OracleCommand(sql, conn))
                        {
                            try
                            {
                                sqlcmd.Transaction = tran;
                                sqlcmd.ExecuteNonQuery();

                                tran.Commit();
                            }
                            catch (Exception ex)
                            {
                                tran.Rollback();
                                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                                return false;
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();

                                if (tran != null)
                                    tran.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return false;
            }

            return true;
        }

        public virtual bool DBConfigTableDataDelete(string EqpID)
        {
            string sql = string.Empty;
            OracleTransaction tran = null;

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();
                        tran = conn.BeginTransaction();

                        sql = string.Format("DELETE FROM TB_CONFIG_INFO WHERE SCS_CD='{0}'", EqpID);

                        using (OracleCommand sqlcmd = new OracleCommand(sql, conn))
                        {
                            try
                            {
                                sqlcmd.Transaction = tran;
                                sqlcmd.ExecuteNonQuery();

                                tran.Commit();
                            }
                            catch (Exception ex)
                            {
                                tran.Rollback();
                                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                                return false;
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();

                                if (tran != null)
                                    tran.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return false;
            }
            return true;
        }

        //220916 조숭진 추가
        public async Task<bool> AlarmXmlToDBAsync(BindingList<AlarmData> AllAlarmList)
        {
            try
            {
                if (DBTableAllDelete("TB_ALARM_INFO"))
                {
                    await Task.Run(() =>
                    {
                        foreach (var data in AllAlarmList)
                        {
                            DbSetProcedureAlarmInfo(data, false, "ID");
                        }
                    });
                    return true;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return false;
            }
        }
        //240226 RGJ Alarm Binding List 를 ObservableList 로 변환
        public async Task<bool> AlarmXmlToDBAsync(ObservableList<AlarmData> AllAlarmList)
        {
            try
            {
                if (DBTableAllDelete("TB_ALARM_INFO"))
                {
                    await Task.Run(() =>
                    {
                        foreach (var data in AllAlarmList)
                        {
                            DbSetProcedureAlarmInfo(data, false, "ID");
                        }
                    });
                    return true;
                }
                else
                    return false;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return false;
            }
        }

        //221012 조숭진 pio db s
        public virtual bool DbSetProcedurePIOInfo(string moduleID, eAreaType areaType, string piodata)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        using (OracleCommand sqlcmd = new OracleCommand())
                        {
                            try
                            {
                                sqlcmd.CommandText = "USP_STC_PIO_INFO_SET";
                                sqlcmd.CommandType = CommandType.StoredProcedure;
                                sqlcmd.Connection = conn;

                                OracleParameter R_RESULT = new OracleParameter("R_RESULT", OracleDbType.NVarchar2, 20);
                                OracleParameter R_TEMP = new OracleParameter("R_TEMP", OracleDbType.NVarchar2, 20);

                                sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2, ParameterDirection.Input).Value = GlobalData.Current.EQPID;
                                sqlcmd.Parameters.Add("P_MODULE_NM", OracleDbType.NVarchar2, ParameterDirection.Input).Value = moduleID;
                                sqlcmd.Parameters.Add("P_DIRECTION_GB", OracleDbType.NVarchar2, ParameterDirection.Input).Value = areaType.ToString();
                                sqlcmd.Parameters.Add("P_DATA_VAL", OracleDbType.NVarchar2, ParameterDirection.Input).Value = piodata;

                                sqlcmd.Parameters.Add(R_RESULT).Direction = ParameterDirection.Output;
                                sqlcmd.Parameters.Add(R_TEMP).Direction = ParameterDirection.Output;

                                sqlcmd.ExecuteNonQuery();

                                string temp = string.Format("Add Result = {0}", R_RESULT.Value.ToString());
                                string temp1 = string.Format("Add Temp = {0}", R_TEMP.Value.ToString());

                                //LogManager.WriteConsoleLog(eLogLevel.Info, temp1 + ", " + temp); //RGJ 해당 로그 임시로 막아둠
                                LogManager.WriteDBLog(eLogLevel.Info, temp1 + ", " + temp, false);

                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return false;
            }

            return true;
        }

        public List<PLCDataInfo> DbGetProcedurePIOInfo()
        {
            List<PLCDataInfo> PIOList = new List<PLCDataInfo>();
            DataSet dataSet = new DataSet();

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        using (OracleCommand sqlcmd = new OracleCommand("UFN_STC_PIO_INFO_GET", conn))
                        {
                            try
                            {
                                //sqlcmd.CommandText = "USP_STC_PIO_INFO_GET";
                                //sqlcmd.CommandText = "UFN_STC_PIO_INFO_GET";
                                sqlcmd.CommandType = CommandType.StoredProcedure;
                                //sqlcmd.Connection = conn;

                                //var curs = new OracleParameter("OUT_DATA", OracleDbType.RefCursor, ParameterDirection.Output);

                                //sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2, ParameterDirection.Input).Value = GlobalData.Current.EQPID;
                                //sqlcmd.Parameters.Add("OUT_DATA", OracleDbType.RefCursor, ParameterDirection.Output);
                                //sqlcmd.Parameters.Add("TMP_DATA", OracleDbType.RefCursor, ParameterDirection.ReturnValue);

                                OracleParameter output = sqlcmd.Parameters.Add("TMP_DATA", OracleDbType.RefCursor);
                                output.Direction = ParameterDirection.ReturnValue;

                                OracleParameter input = sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2);
                                input.Direction = ParameterDirection.Input;
                                input.Value = GlobalData.Current.EQPID;

                                sqlcmd.ExecuteNonQuery();

                                using (OracleDataAdapter oradata = new OracleDataAdapter(sqlcmd))
                                {
                                    try
                                    {
                                        oradata.Fill(dataSet);
                                    }
                                    finally
                                    {
                                        if (oradata != null)
                                            oradata.Dispose();
                                    }
                                }
                            }
                            finally
                            {
                                //for (int i = 0; i < sqlcmd.Parameters.Count; i++)
                                //{
                                //    sqlcmd.Parameters[i].Dispose();
                                //}

                                //sqlcmd.Parameters.Clear();

                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }

                int table = dataSet.Tables.Count;
                for (int i = 0; i < table; i++)// set the table value in list one by one
                {
                    foreach (DataRow dr in dataSet.Tables[i].Rows)
                    {
                        PIOList.Add(new PLCDataInfo
                        {
                            ModuleID = Convert.ToString(dr["MODULE_NM"]),
                            Direction = (eAreaType)Enum.Parse(typeof(eAreaType), dr["DIRECTION_GB"].ToString()),
                            PLCData = Convert.ToString(dr["DATA_VAL"])
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return PIOList;
            }
            return PIOList;
        }
        //221012 조숭진 pio db e

        //221014 조숭진 클라이언트용 s
        public void DbGetProcedureEQPInfo()
        {
            DataSet dataSet = new DataSet();
            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        using (OracleCommand sqlcmd = new OracleCommand("UFN_STC_EQP_INFO_GET", conn))
                        {
                            try
                            {
                                //sqlcmd.CommandText = "USP_STC_EQP_INFO_GET";
                                sqlcmd.CommandType = CommandType.StoredProcedure;
                                //sqlcmd.Connection = conn;

                                //sqlcmd.Parameters.Add("OUT_DATA", OracleDbType.RefCursor, ParameterDirection.Output);
                                OracleParameter output = sqlcmd.Parameters.Add("TMP_DATA", OracleDbType.RefCursor);
                                output.Direction = ParameterDirection.ReturnValue;

                                OracleParameter input = sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2);
                                input.Direction = ParameterDirection.Input;
                                input.Value = GlobalData.Current.EQPID;

                                sqlcmd.ExecuteNonQuery();

                                using (OracleDataAdapter oradata = new OracleDataAdapter(sqlcmd))
                                {
                                    try
                                    {
                                        oradata.Fill(dataSet);
                                    }
                                    finally
                                    {
                                        if (oradata != null)
                                            oradata.Dispose();
                                    }
                                }
                            }
                            finally
                            {
                                for (int i = 0; i < sqlcmd.Parameters.Count; i++)
                                {
                                    sqlcmd.Parameters[i].Dispose();
                                }

                                sqlcmd.Parameters.Clear();

                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }

                int table = dataSet.Tables.Count;
                for (int i = 0; i < table; i++)// set the table value in list one by one
                {
                    foreach (DataRow dr in dataSet.Tables[i].Rows)
                    {
                        EQPInfo Einfo = new EQPInfo
                        {
                            EQPName = Convert.ToString(dr["EQP_NM"]),
                            EQPID = Convert.ToString(dr["EQP_ID"]),
                            SCSIP = Convert.ToString(dr["IP_NO"]),
                            EQPNumber = Convert.ToString(dr["EQP_NO"]),
                            MCS_State = string.IsNullOrEmpty(dr["MCS_STAT"].ToString()) ? "0" : Convert.ToString(dr["MCS_STAT"]),
                            SCS_State = string.IsNullOrEmpty(dr["SCS_STAT"].ToString()) ? "0" : Convert.ToString(dr["SCS_STAT"]),
                            PLC_State = string.IsNullOrEmpty(dr["PLC_STAT"].ToString()) ? "0" : Convert.ToString(dr["PLC_STAT"]),
                            SYSTEM_State = string.IsNullOrEmpty(dr["SYSTEM_STAT"].ToString()) ? eSCState.NONE : (eSCState)Convert.ToInt32(dr["SYSTEM_STAT"]),
                            DBFirstIP = dr["DBFIRSTIP_NO"].ToString(),
                            DBFirstPort = dr["DBFIRSTPORT_NO"].ToString(),
                            DBFirstServiceName = dr["DBFIRSTSERVICE_NM"].ToString(),
                            DBSecondIP = dr["DBSECONDIP_NO"].ToString(),
                            DBSecondPort = dr["DBSECONDPORT_NO"].ToString(),
                            DBSecondServiceName = dr["DBSECONDSERVICE_NM"].ToString(),
                            DbAccount = dr["DBACCOUNT_NM"].ToString(),
                            DbPassword = dr["DBPASSWORD_GB"].ToString(),
                        };
                        GlobalData.Current.EQPListAddOrUpdate(Einfo);
                         
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }

        //221019 조숭진 현재 서버 상태를 db에 기록
        public virtual bool DbSetProcedureEQPInfo(object mcs, object scs, object plc, object system, bool init = false)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        using (OracleCommand sqlcmd = new OracleCommand())
                        {
                            try
                            {
                                sqlcmd.CommandText = "USP_STC_EQP_INFO_SET";
                                sqlcmd.CommandType = CommandType.StoredProcedure;
                                sqlcmd.Connection = conn;

                                OracleParameter R_RESULT = new OracleParameter("R_RESULT", OracleDbType.NVarchar2, 20);
                                OracleParameter R_TEMP = new OracleParameter("R_TEMP", OracleDbType.NVarchar2, 20);

                                sqlcmd.Parameters.Add("P_SCS_IP", OracleDbType.Char, ParameterDirection.Input).Value = GlobalData.Current.CurrentIP;
                                sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2, ParameterDirection.Input).Value = GlobalData.Current.EQPID;
                                sqlcmd.Parameters.Add("P_MCS_STAT", OracleDbType.Char, ParameterDirection.Input).Value = mcs;
                                sqlcmd.Parameters.Add("P_SCS_STAT", OracleDbType.Char, ParameterDirection.Input).Value = scs;
                                sqlcmd.Parameters.Add("P_PLC_STAT", OracleDbType.Char, ParameterDirection.Input).Value = plc;
                                sqlcmd.Parameters.Add("P_SYSTEM_STAT", OracleDbType.Char, ParameterDirection.Input).Value = system;
                                //230222 조숭진 클라이언트가 시스템상태변경 요청 s
                                sqlcmd.Parameters.Add("P_SCS_NM", OracleDbType.NVarchar2, ParameterDirection.Input).Value = GlobalData.Current.EqpName;
                                sqlcmd.Parameters.Add("P_SCS_NO", OracleDbType.NVarchar2, ParameterDirection.Input).Value = GlobalData.Current.EqpNumber;
                                sqlcmd.Parameters.Add("P_DBFIRSTIP_NO", OracleDbType.NVarchar2, ParameterDirection.Input).Value = GlobalData.Current.DBSection.DBFirstConnIP;
                                sqlcmd.Parameters.Add("P_DBFIRSTPORT_NO", OracleDbType.NVarchar2, ParameterDirection.Input).Value = GlobalData.Current.DBSection.DBFirstConnPort;
                                sqlcmd.Parameters.Add("P_DBFIRSTSERVICE_NM", OracleDbType.NVarchar2, ParameterDirection.Input).Value = GlobalData.Current.DBSection.DBFirstConnServiceName;
                                sqlcmd.Parameters.Add("P_DBSECONDIP_NO", OracleDbType.NVarchar2, ParameterDirection.Input).Value = GlobalData.Current.DBSection.DBSecondConnIP;
                                sqlcmd.Parameters.Add("P_DBSECONDPORT_NO", OracleDbType.NVarchar2, ParameterDirection.Input).Value = GlobalData.Current.DBSection.DBSecondConnPort;
                                sqlcmd.Parameters.Add("P_DBSECONDSERVICE_NM", OracleDbType.NVarchar2, ParameterDirection.Input).Value = GlobalData.Current.DBSection.DBSecondConnServiceName;
                                sqlcmd.Parameters.Add("P_DBACCOUNT_NM", OracleDbType.NVarchar2, ParameterDirection.Input).Value = GlobalData.Current.DBSection.DBAccountName;
                                sqlcmd.Parameters.Add("P_DBPASSWORD_GB", OracleDbType.NVarchar2, ParameterDirection.Input).Value = GlobalData.Current.DBSection.DBPassword;
                                sqlcmd.Parameters.Add("P_INIT", OracleDbType.Char, ParameterDirection.Input).Value = init ? '1' : '0';
                                //230222 조숭진 클라이언트가 시스템상태변경 요청 e

                                sqlcmd.Parameters.Add(R_RESULT).Direction = ParameterDirection.Output;
                                sqlcmd.Parameters.Add(R_TEMP).Direction = ParameterDirection.Output;

                                sqlcmd.ExecuteNonQuery();

                                string temp = string.Format("Add Result = {0}", R_RESULT.Value.ToString());
                                string temp1 = string.Format("Add Temp = {0}", R_TEMP.Value.ToString());

                                //LogManager.WriteConsoleLog(eLogLevel.Info, temp1 + ", " + temp);
                                LogManager.WriteDBLog(eLogLevel.Info, temp1 + ", " + temp, false);
                            }
                            finally
                            {
                                sqlcmd.Parameters.Clear();

                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return false;
            }
            return true;
        }
        //221014 조숭진 클라이언트용 e

        public bool DbSetProcedureLogInfo(params object[] args)
        {
            if (args == null ||  args.Count() == 0)
                return false;

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        using (OracleCommand sqlcmd = new OracleCommand())
                        {
                            try
                            {
                                int colnum = 0;
                                sqlcmd.CommandText = "USP_STC_LOG_INFO_SET";
                                sqlcmd.CommandType = CommandType.StoredProcedure;
                                sqlcmd.Connection = conn;

                                OracleParameter R_RESULT = new OracleParameter("R_RESULT", OracleDbType.NVarchar2, 20);
                                OracleParameter R_TEMP = new OracleParameter("R_TEMP", OracleDbType.NVarchar2, 20);

                                sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2, ParameterDirection.Input).Value = GlobalData.Current.EQPID;
                                sqlcmd.Parameters.Add("P_LOG_NM", OracleDbType.NVarchar2, ParameterDirection.Input).Value = args[0];

                                if (args[0].ToString() == "UTIL") //가동률 로그를 따로 빼둠
                                {
                                    sqlcmd.Parameters.Add("P_RECODE_DTTM", OracleDbType.NVarchar2, ParameterDirection.Input).Value = string.Empty;
                                    sqlcmd.Parameters.Add("P_COL_1", OracleDbType.NVarchar2, ParameterDirection.Input).Value = args[2]; //UNIT ID
                                    sqlcmd.Parameters.Add("P_COL_2", OracleDbType.NVarchar2, ParameterDirection.Input).Value = args[1]; //UNIT STATE
                                    sqlcmd.Parameters.Add("P_COL_3", OracleDbType.NVarchar2, ParameterDirection.Input).Value = args[5].ToString(); //State Duration
                                    sqlcmd.Parameters.Add("P_COL_4", OracleDbType.NVarchar2, ParameterDirection.Input).Value = string.Empty;
                                    sqlcmd.Parameters.Add("P_COL_5", OracleDbType.NVarchar2, ParameterDirection.Input).Value = string.Empty;
                                    sqlcmd.Parameters.Add("P_COL_6", OracleDbType.NVarchar2, ParameterDirection.Input).Value = DataTimeForOracleUntilMillisecond((DateTime)args[3]); //State StartTime
                                    sqlcmd.Parameters.Add("P_COL_7", OracleDbType.NVarchar2, ParameterDirection.Input).Value = DataTimeForOracleUntilMillisecond((DateTime)args[4]); //State EndTime

                                    for (int i = 8; i < 14; i++)
                                    {
                                        string name = string.Format("P_COL_{0}", i);
                                        sqlcmd.Parameters.Add(name, OracleDbType.NVarchar2, ParameterDirection.Input).Value = string.Empty;
                                    }
                                    sqlcmd.Parameters.Add("P_SECS2", OracleDbType.Clob, ParameterDirection.Input).Value = string.Empty;
                                }
                                else if (args[0].ToString() != "ALARM")
                                {
                                    sqlcmd.Parameters.Add("P_RECODE_DTTM", OracleDbType.NVarchar2, ParameterDirection.Input).Value = args[1];

                                    for (int i = 2; i < args.Count(); i++)
                                    {
                                        if(args[0].ToString() == "HSMS" && i == 15)
                                        {
                                            colnum = i;
                                            break;
                                        }

                                        string name = string.Format("P_COL_{0}", i - 1);
                                        sqlcmd.Parameters.Add(name, OracleDbType.NVarchar2, ParameterDirection.Input).Value = args[i];
                                        colnum = i;
                                    }

                                    for (int i = colnum; i < 14; i++)
                                    {
                                        string name = string.Format("P_COL_{0}", i);
                                        sqlcmd.Parameters.Add(name, OracleDbType.NVarchar2, ParameterDirection.Input).Value = string.Empty;
                                        colnum = i;
                                    }

                                    if (args[0].ToString() == "HSMS" && colnum == 15)
                                    {
                                        sqlcmd.Parameters.Add("P_SECS2", OracleDbType.Clob, ParameterDirection.Input).Value = args[15];
                                    }
                                    else
                                    {
                                        sqlcmd.Parameters.Add("P_SECS2", OracleDbType.Clob, ParameterDirection.Input).Value = string.Empty;
                                    }
                                }
                                else //Alarm 
                                {
                                    sqlcmd.Parameters.Add("P_RECODE_DTTM", OracleDbType.NVarchar2, ParameterDirection.Input).Value = string.Empty;

                                    for (int i = 1; i < args.Count(); i++)
                                    {
                                        string name = string.Format("P_COL_{0}", i);
                                        sqlcmd.Parameters.Add(name, OracleDbType.NVarchar2, ParameterDirection.Input).Value = args[i];
                                        colnum = i;
                                    }

                                    for (int i = colnum + 1; i < 14; i++)
                                    {
                                        string name = string.Format("P_COL_{0}", i);
                                        sqlcmd.Parameters.Add(name, OracleDbType.NVarchar2, ParameterDirection.Input).Value = string.Empty;
                                        colnum = i;
                                    }

                                    if(colnum == 13)
                                    {
                                        sqlcmd.Parameters.Add("P_SECS2", OracleDbType.Clob, ParameterDirection.Input).Value = string.Empty;
                                    }
                                }

                                sqlcmd.Parameters.Add(R_RESULT).Direction = ParameterDirection.Output;
                                sqlcmd.Parameters.Add(R_TEMP).Direction = ParameterDirection.Output;

                                sqlcmd.ExecuteNonQuery();

                                string temp = string.Format("Add Result = {0}", R_RESULT.Value.ToString());
                                string temp1 = string.Format("Add Temp = {0}", R_TEMP.Value.ToString());

                                //LogManager.WriteConsoleLog(eLogLevel.Info, temp1 + ", " + temp);
                                LogManager.WriteDBLog(eLogLevel.Info, temp1 + ", " + temp, false);
                            }
                            catch(Exception ex)
                            {
                                ;
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return false;
            }
            return true;
        }

        public virtual DataTable DbGetProcedureLogListInfo(string LogName, DateTime start, DateTime end, string key1 = "", string key2 = "", string key3 = "")
        {
            //DataTable LogList = new DataTable();
            //DataSet dataSet = new DataSet();

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        using (OracleCommand sqlcmd = new OracleCommand("UFN_STC_LOG_INFO_GET", conn))
                        {
                            try
                            {
                                //sqlcmd.CommandText = "USP_STC_LOG_INFO_GET";
                                sqlcmd.CommandType = CommandType.StoredProcedure;
                                //sqlcmd.Connection = conn;

                                //sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2, ParameterDirection.Input).Value = GlobalData.Current.EQPID;
                                //sqlcmd.Parameters.Add("P_LOG_NM", OracleDbType.NVarchar2, ParameterDirection.Input).Value = LogName;
                                //sqlcmd.Parameters.Add("OUT_DATA", OracleDbType.RefCursor, ParameterDirection.Output);
                                OracleParameter output = sqlcmd.Parameters.Add("TMP_DATA", OracleDbType.RefCursor);
                                output.Direction = ParameterDirection.ReturnValue;

                                OracleParameter input1 = sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2);
                                input1.Direction = ParameterDirection.Input;
                                input1.Value = GlobalData.Current.EQPID;

                                OracleParameter input2 = sqlcmd.Parameters.Add("P_LOG_NM", OracleDbType.NVarchar2);
                                input2.Direction = ParameterDirection.Input;
                                input2.Value = LogName;

                                if (LogName != "HSMS")
                                {
                                    OracleParameter input3 = sqlcmd.Parameters.Add("P_START_DTTM", OracleDbType.NVarchar2);
                                    input3.Direction = ParameterDirection.Input;
                                    input3.Value = DateTimeForOracle(start); //230411 RGJ Datetime 을 오라클 포맷으로 던지게 함.

                                    OracleParameter input4 = sqlcmd.Parameters.Add("P_END_DTTM", OracleDbType.NVarchar2);
                                    input4.Direction = ParameterDirection.Input;
                                    input4.Value = DateTimeForOracle(end); //230411 RGJ Datetime 을 오라클 포맷으로 던지게 함.
                                }
                                else
                                {
                                    OracleParameter input3 = sqlcmd.Parameters.Add("P_START_DTTM", OracleDbType.NVarchar2);
                                    input3.Direction = ParameterDirection.Input;
                                    input3.Value = DataTimeForOracleUntilMillisecond(start); //230411 RGJ Datetime 을 오라클 포맷으로 던지게 함.

                                    OracleParameter input4 = sqlcmd.Parameters.Add("P_END_DTTM", OracleDbType.NVarchar2);
                                    input4.Direction = ParameterDirection.Input;
                                    input4.Value = DataTimeForOracleUntilMillisecond(end); //230411 RGJ Datetime 을 오라클 포맷으로 던지게 함.
                                }

                                OracleParameter input5 = sqlcmd.Parameters.Add("P_KEY1", OracleDbType.NVarchar2);
                                input5.Direction = ParameterDirection.Input;
                                input5.Value = key1;

                                OracleParameter input6 = sqlcmd.Parameters.Add("P_KEY2", OracleDbType.NVarchar2);
                                input6.Direction = ParameterDirection.Input;
                                input6.Value = key2;

                                OracleParameter input7 = sqlcmd.Parameters.Add("P_KEY3", OracleDbType.NVarchar2);
                                input7.Direction = ParameterDirection.Input;
                                input7.Value = key3;

                                sqlcmd.ExecuteNonQuery();

                                //using (OracleDataAdapter oradata = new OracleDataAdapter(sqlcmd))
                                //{
                                //    try
                                //    {
                                //        oradata.Fill(LogList);
                                //    }
                                //    finally
                                //    {
                                //        if (oradata != null)
                                //            oradata.Dispose();
                                //    }
                                //}

                                if (LogList.Rows.Count != 0)
                                {
                                    LogList.Rows.Clear();
                                }

                                using (OracleDataReader rowdata = sqlcmd.ExecuteReader())
                                {
                                    try
                                    {
                                        while (rowdata.Read())
                                        {
                                            DataRow row = LogList.NewRow();

                                            row["SCS_CD"] = rowdata["SCS_CD"].ToString();
                                            row["LOG_NM"] = rowdata["LOG_NM"].ToString();
                                            row["RECODE_DTTM"] = rowdata["RECODE_DTTM"].ToString();
                                            row["COL_1"] = rowdata["COL_1"].ToString();
                                            row["COL_2"] = rowdata["COL_2"].ToString();
                                            row["COL_3"] = rowdata["COL_3"].ToString();
                                            row["COL_4"] = rowdata["COL_4"].ToString();
                                            row["COL_5"] = rowdata["COL_5"].ToString();
                                            row["COL_6"] = rowdata["COL_6"].ToString();
                                            row["COL_7"] = rowdata["COL_7"].ToString();
                                            row["COL_8"] = rowdata["COL_8"].ToString();
                                            row["COL_9"] = rowdata["COL_9"].ToString();
                                            row["COL_10"] = rowdata["COL_10"].ToString();
                                            row["COL_11"] = rowdata["COL_11"].ToString();
                                            row["COL_12"] = rowdata["COL_12"].ToString();
                                            row["COL_13"] = rowdata["COL_13"].ToString();
                                            row["HIST_SECS2"] = rowdata["HIST_SECS2"].ToString();

                                            LogList.Rows.Add(row);

                                            if (bLogSearchCancel)
                                                break;
                                        }
                                    }
                                    finally
                                    {
                                        if (rowdata != null)
                                        {
                                            rowdata.Close(); //240503 OracleDataReader 는 사용후 close 필요함.
                                            rowdata.Dispose();
                                        }
                                    }
                                }
                            }
                            finally
                            {
                                for (int i = 0; i < sqlcmd.Parameters.Count; i++)
                                {
                                    sqlcmd.Parameters[i].Dispose();
                                }

                                sqlcmd.Parameters.Clear();

                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }

                //int table = dataSet.Tables.Count;

                //for (int i = 0; i < table; i++)// set the table value in list one by one
                //{
                //    LogList = dataSet.Tables[i];
                //}
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return LogList;
            }

            return LogList;
        }

        //230223 조숭진 클라이언트에서 알람클리어 요청
        public bool DbSetProcedureClientReq(string eqpid, string cmdtype, string target, string targetid, string targetvalue, string time, eServerClientType type, bool del = false, string JobID = "", string clientid = "")
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        using (OracleCommand sqlcmd = new OracleCommand("USP_STC_CLIENT_ORDER_SET", conn))
                        {
                            try
                            {
                                sqlcmd.CommandType = CommandType.StoredProcedure;

                                OracleParameter R_RESULT = new OracleParameter("R_RESULT", OracleDbType.NVarchar2, 20);
                                OracleParameter R_TEMP = new OracleParameter("R_TEMP", OracleDbType.NVarchar2, 20);

                                sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2, ParameterDirection.Input).Value = eqpid;
                                sqlcmd.Parameters.Add("P_CMD_GB", OracleDbType.NVarchar2, ParameterDirection.Input).Value = cmdtype;
                                sqlcmd.Parameters.Add("P_TARGET_CD", OracleDbType.NVarchar2, ParameterDirection.Input).Value = target;
                                sqlcmd.Parameters.Add("P_TARGET_ID", OracleDbType.NVarchar2, ParameterDirection.Input).Value = targetid;
                                sqlcmd.Parameters.Add("P_TARGET_VALUE", OracleDbType.NVarchar2, ParameterDirection.Input).Value = targetvalue;
                                sqlcmd.Parameters.Add("P_CREATE_DTTM", OracleDbType.NVarchar2, ParameterDirection.Input).Value = time;
                                sqlcmd.Parameters.Add("P_REQUESTER_GB", OracleDbType.Char, ParameterDirection.Input).Value = type == eServerClientType.Server ? '0' : '1';
                                sqlcmd.Parameters.Add("P_DEL", OracleDbType.Char, ParameterDirection.Input).Value = del ? '1' : '0';
                                sqlcmd.Parameters.Add("P_JOB_ID", OracleDbType.NVarchar2, ParameterDirection.Input).Value = JobID;       //230223
                                sqlcmd.Parameters.Add("P_CLIENT_CD", OracleDbType.NVarchar2, ParameterDirection.Input).Value = clientid;

                                sqlcmd.Parameters.Add(R_RESULT).Direction = ParameterDirection.Output;
                                sqlcmd.Parameters.Add(R_TEMP).Direction = ParameterDirection.Output;

                                sqlcmd.ExecuteNonQuery();

                                string temp = string.Format("Add Result = {0}", R_RESULT.Value.ToString());
                                string temp1 = string.Format("Add Temp = {0}", R_TEMP.Value.ToString());

                                //LogManager.WriteConsoleLog(eLogLevel.Info, temp1 + ", " + temp);
                                LogManager.WriteDBLog(eLogLevel.Info, temp1 + ", " + temp, false);
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return false;
            }
            return true;
        }

        public List<ClientReqList> DbGetProcedureClientReq(eServerClientType requesttype, string clientid = "")
        {
            List<ClientReqList> ReqList = new List<ClientReqList>();
            DataSet dataSet = new DataSet();

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        if (IsConnect == false)
                            IsConnect = true;

                        using (OracleCommand sqlcmd = new OracleCommand("UFN_STC_CLIENT_ORDER_GET", conn))
                        {
                            try
                            {
                                sqlcmd.CommandType = CommandType.StoredProcedure;

                                OracleParameter output = sqlcmd.Parameters.Add("TMP_DATA", OracleDbType.RefCursor);
                                output.Direction = ParameterDirection.ReturnValue;

                                OracleParameter input1 = sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2);
                                input1.Direction = ParameterDirection.Input;
                                input1.Value = GlobalData.Current.EQPID;

                                OracleParameter input2 = sqlcmd.Parameters.Add("P_REQUESTER_GB", OracleDbType.Char);
                                input2.Direction = ParameterDirection.Input;
                                input2.Value = requesttype == eServerClientType.Server ? '0' : '1';

                                OracleParameter input3 = sqlcmd.Parameters.Add("P_CLIENT_CD", OracleDbType.NVarchar2);
                                input3.Direction = ParameterDirection.Input;
                                input3.Value = clientid;

                                sqlcmd.ExecuteNonQuery();

                                using (OracleDataAdapter oradata = new OracleDataAdapter(sqlcmd))
                                {
                                    try
                                    {
                                        oradata.Fill(dataSet);
                                    }
                                    finally
                                    {
                                        if (oradata != null)
                                            oradata.Dispose();
                                    }
                                }
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }

                int table = dataSet.Tables.Count;
                for (int i = 0; i < table; i++)// set the table value in list one by one
                {
                    foreach (DataRow dr in dataSet.Tables[i].Rows)
                    {
                        ReqList.Add(new ClientReqList
                        {
                            EQPID = dr["SCS_CD"].ToString(),
                            CMDType = dr["CMD_GB"].ToString(),
                            Target = dr["TARGET_CD"].ToString(),
                            TargetID = dr["TARGET_ID"].ToString(),
                            TargetValue = dr["TARGET_VALUE"].ToString(),
                            ReqTime = Convert.ToDateTime(dr["CREATE_DTTM"]).ToString("yyyy-MM-dd HH:mm:ss.fff"),
                            Requester = Convert.ToInt32(dr["REQUESTER_GB"].ToString()) == 0 ? eServerClientType.Server : eServerClientType.Client,
                            JobID = string.IsNullOrEmpty(dr["JOB_ID"].ToString()) ? string.Empty : dr["JOB_ID"].ToString(),
                            ClientID = dr["CLIENT_CD"].ToString(),
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                
                //이중화 접속을 위해 필요함.
                string excode = ex.HResult.ToString("X");
                if (excode == "80004005")
                    DbConnectDuplexing(OracleDBPath);

                return ReqList;
            }
            return ReqList;
        }

        public void DbConnectDuplexing(string ConnInfo)
        {
            IsConnect = false;

            if (ConnInfo != OracleFirstDBPath)
                OracleDBPath = OracleFirstDBPath;
            else if (ConnInfo != OracleSecondDBPath)
                OracleDBPath = OracleSecondDBPath;

            //if (IsTimeOut(DBConnectionDuplexingTime, 60))
            //{
            //    DBConnectionDuplexingCount = 0;
            //    DBConnectionDuplexingTime = DateTime.Now;
            //}

            //DBConnectionDuplexingCount++;
            //if (DBConnectionDuplexingCount >= 2)
                //return false;

            //return true;
        }

        //221101 YSW Client SYSTEM STATE REQUEST GET
        public List<ClientRequestSysStateInfo> DbGetProcedureClientRequestSystemState()
        {
            List<ClientRequestSysStateInfo> CRSStateInfoList = new List<ClientRequestSysStateInfo>();
            DataSet dataSet = new DataSet();

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        using (OracleCommand sqlcmd = new OracleCommand("UFN_STC_CLIENT_CHANGE_REQUEST_SYSTEM_STATE_GET", conn))
                        {
                            try
                            {
                                sqlcmd.CommandType = CommandType.StoredProcedure;

                                OracleParameter output = sqlcmd.Parameters.Add("TMP_DATA", OracleDbType.RefCursor);
                                output.Direction = ParameterDirection.ReturnValue;

                                sqlcmd.ExecuteNonQuery();

                                using (OracleDataAdapter oradata = new OracleDataAdapter(sqlcmd))
                                {
                                    try
                                    {
                                        oradata.Fill(dataSet);
                                    }
                                    finally
                                    {
                                        if (oradata != null)
                                            oradata.Dispose();
                                    }
                                }
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }

                int table = dataSet.Tables.Count;
                for (int i = 0; i < table; i++)// set the table value in list one by one
                {
                    foreach (DataRow dr in dataSet.Tables[i].Rows)
                    {
                        CRSStateInfoList.Add(new ClientRequestSysStateInfo
                        {
                            //RequestSignal = string.IsNullOrEmpty(dr["REQUEST_SIGNAL"].ToString()) ? "0" : Convert.ToString(dr["REQUEST_SIGNAL"]),
                            RequestSignal = Convert.ToString(dr["REQUEST_SIGNAL"]),
                            EQPID = Convert.ToString(dr["EQP_ID"]),
                            SYSTEM_State = string.IsNullOrEmpty(dr["SYSTEM_STATE"].ToString()) ? eSCState.NONE : (eSCState)Convert.ToInt32(dr["SYSTEM_STATE"])
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return CRSStateInfoList;
            }

            return CRSStateInfoList;
        }

        //221101 YSW Client SYSTEM STATE REQUEST SET
        public bool DbSetProcedureClientRequestSystemState(object signal, object system)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        using (OracleCommand sqlcmd = new OracleCommand())
                        {
                            try
                            {
                                sqlcmd.CommandText = "USP_STC_CLIENT_CHANGE_REQUEST_SYSTEM_STATE_SET";
                                sqlcmd.CommandType = CommandType.StoredProcedure;
                                sqlcmd.Connection = conn;

                                OracleParameter R_RESULT = new OracleParameter("R_RESULT", OracleDbType.NVarchar2, 50);
                                OracleParameter R_TEMP = new OracleParameter("R_TEMP", OracleDbType.NVarchar2, 50);

                                sqlcmd.Parameters.Add("P_EQP_ID", OracleDbType.NVarchar2, ParameterDirection.Input).Value = GlobalData.Current.EQPID;
                                sqlcmd.Parameters.Add("P_SYSTEM_STATE", OracleDbType.Char, ParameterDirection.Input).Value = system;
                                sqlcmd.Parameters.Add("P_REQUEST_SIGNAL", OracleDbType.Char, ParameterDirection.Input).Value = signal;

                                sqlcmd.Parameters.Add(R_RESULT).Direction = ParameterDirection.Output;
                                sqlcmd.Parameters.Add(R_TEMP).Direction = ParameterDirection.Output;

                                sqlcmd.ExecuteNonQuery();

                                string temp = string.Format("Add Result = {0}", R_RESULT.Value.ToString());
                                string temp1 = string.Format("Add Temp = {0}", R_TEMP.Value.ToString());

                                //LogManager.WriteConsoleLog(eLogLevel.Info, temp1 + ", " + temp);
                                LogManager.WriteDBLog(eLogLevel.Info, temp1 + ", " + temp, false);
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return false;
            }
            return true;
        }

        public bool IsCheckProcedure(OracleConnection conn, out string procedurename)
        {
            OracleCommand sqlcmd = null;
            string sql = string.Empty;
            OracleDataReader rowdata = null;
            bool returnvalue = true;
            procedurename = string.Empty;

            try
            {
                {
                    for (ProcedureName i = 0; i < ProcedureName.Last; i++)
                    {
                        string name = i.ToString();
                        sql = string.Format("SELECT * FROM USER_SOURCE WHERE TYPE='PROCEDURE' AND NAME='{0}'", name);
                        sqlcmd = new OracleCommand(sql, conn);
                        rowdata = sqlcmd.ExecuteReader();

                        bool exist = rowdata.HasRows;

                        if (!exist)
                        {
                            procedurename = i.ToString();
                            returnvalue = false;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return false;
            }
            return returnvalue;
        }

        public bool IsCheckFunction(OracleConnection conn, out string functionname)
        {
            OracleCommand sqlcmd = null;
            string sql = string.Empty;
            OracleDataReader rowdata = null;
            bool returnvalue = true;
            functionname = string.Empty;

            try
            {
                {
                    for (FunctionName i = 0; i < FunctionName.Last; i++)
                    {
                        string name = i.ToString();
                        sql = string.Format("SELECT * FROM USER_SOURCE WHERE TYPE='FUNCTION' AND NAME='{0}'", name);
                        sqlcmd = new OracleCommand(sql, conn);
                        rowdata = sqlcmd.ExecuteReader();

                        bool exist = rowdata.HasRows;

                        if (!exist)
                        {
                            functionname = i.ToString();
                            returnvalue = false;
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return false;
            }

            return returnvalue;
        }

        public bool DbSetProcedureTerminalMSG(string tid, string tcode, string text, DateTime dt)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        using (OracleCommand sqlcmd = new OracleCommand())
                        {
                            try
                            {
                                sqlcmd.CommandText = "USP_STC_TERMINAL_MSG_SET";
                                sqlcmd.CommandType = CommandType.StoredProcedure;
                                sqlcmd.Connection = conn;

                                sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2, ParameterDirection.Input).Value = GlobalData.Current.EQPID;
                                sqlcmd.Parameters.Add("P_TID_ID", OracleDbType.NVarchar2, ParameterDirection.Input).Value = tid;
                                sqlcmd.Parameters.Add("P_TCODE_CD", OracleDbType.NVarchar2, ParameterDirection.Input).Value = tcode;
                                sqlcmd.Parameters.Add("P_TEXT_NM", OracleDbType.NVarchar2, ParameterDirection.Input).Value = text;
                                sqlcmd.Parameters.Add("P_EVENTTIME_DTTM", OracleDbType.NVarchar2, ParameterDirection.Input).Value = dt.ToString();

                                sqlcmd.ExecuteNonQuery();
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return false;
            }
            return true;
        }

        public bool DbGetProcedureTerminalMSG()
        {
            DataSet dataSet = new DataSet();

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        using (OracleCommand sqlcmd = new OracleCommand("UFN_STC_TERMINAL_MSG_GET", conn))
                        {
                            try
                            {
                                sqlcmd.CommandType = CommandType.StoredProcedure;

                                OracleParameter output = sqlcmd.Parameters.Add("TMP_DATA", OracleDbType.RefCursor);
                                output.Direction = ParameterDirection.ReturnValue;

                                OracleParameter input1 = sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2);
                                input1.Direction = ParameterDirection.Input;
                                input1.Value = GlobalData.Current.EQPID;

                                sqlcmd.ExecuteNonQuery();

                                using (OracleDataAdapter oradata = new OracleDataAdapter(sqlcmd))
                                {
                                    try
                                    {
                                        oradata.Fill(dataSet);
                                    }
                                    finally
                                    {
                                        if (oradata != null)
                                            oradata.Dispose();
                                    }
                                }
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }

                int table = dataSet.Tables.Count;
                for (int i = 0; i < table; i++)// set the table value in list one by one
                {
                    foreach (DataRow dr in dataSet.Tables[i].Rows)
                    {
                        DateTime dt = string.IsNullOrEmpty(dr["EVENTTIME_DTTM"].ToString()) ? DateTime.Now : Convert.ToDateTime(dr["EVENTTIME_DTTM"]);
                        GlobalData.Current.TerminalMessageChangedOccur(dt, eHostMessageDirection.eHostToEq, dr["TEXT_NM"].ToString(), true);
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return false;
            }

            return true;
        }
        //클라이언트에서 모든 맵 장비 리스트를 가져와야한다.
        public EQPInfo EqpListForMap(int eqpno = 0)
        {
            string sql = string.Empty;
            EQPInfo eqpinfo = new EQPInfo();
            try
            {
                //for (int i = repetitionstart; i <= repetitionend; i++)
                //{
                    //restarteqpno = i;
                    {
                        sql = string.Format("SELECT * FROM SCS_SKOH2_{0}.TB_EQP_INFO", eqpno);
                    }

                    using (OracleConnection conn = new OracleConnection(OracleDBPath))
                    {
                        try
                        {
                            conn.Open();
                            sql = string.Format(sql, eqpno);

                            using (OracleCommand sqlcmd = new OracleCommand(sql, conn))
                            {
                                try
                                {
                                    using (OracleDataReader rowdata = sqlcmd.ExecuteReader())
                                    {
                                        //if (!rowdata.HasRows)
                                        //{
                                        //    if (rowdata != null)
                                        //        rowdata.Dispose();

                                        //    continue;
                                        //}

                                        try
                                        {
                                            while (rowdata.Read())
                                            {
                                                eqpinfo = new EQPInfo
                                                {
                                                    EQPName = Convert.ToString(rowdata["EQP_NM"]),
                                                    EQPID = Convert.ToString(rowdata["EQP_ID"]),
                                                    SCSIP = Convert.ToString(rowdata["IP_NO"]),
                                                    EQPNumber = Convert.ToString(rowdata["EQP_NO"]),
                                                    MCS_State = string.IsNullOrEmpty(rowdata["MCS_STAT"].ToString()) ? "0" : Convert.ToString(rowdata["MCS_STAT"]),
                                                    SCS_State = string.IsNullOrEmpty(rowdata["SCS_STAT"].ToString()) ? "0" : Convert.ToString(rowdata["SCS_STAT"]),
                                                    PLC_State = string.IsNullOrEmpty(rowdata["PLC_STAT"].ToString()) ? "0" : Convert.ToString(rowdata["PLC_STAT"]),
                                                    SYSTEM_State = string.IsNullOrEmpty(rowdata["SYSTEM_STAT"].ToString()) ? eSCState.NONE : (eSCState)Convert.ToInt32(rowdata["SYSTEM_STAT"]),
                                                    DBFirstIP = rowdata["DBFIRSTIP_NO"].ToString(),
                                                    DBFirstPort = rowdata["DBFIRSTPORT_NO"].ToString(),
                                                    DBFirstServiceName = rowdata["DBFIRSTSERVICE_NM"].ToString(),
                                                    DBSecondIP = rowdata["DBSECONDIP_NO"].ToString(),
                                                    DBSecondPort = rowdata["DBSECONDPORT_NO"].ToString(),
                                                    DBSecondServiceName = rowdata["DBSECONDSERVICE_NM"].ToString(),
                                                    DbAccount = rowdata["DBACCOUNT_NM"].ToString(),
                                                    DbPassword = rowdata["DBPASSWORD_GB"].ToString(),
                                                };

                                                Thread.Sleep(50);
                                            }
                                        }
                                        finally
                                        {
                                            if (rowdata != null)
                                            {
                                                rowdata.Close(); //240503 OracleDataReader 는 사용후 close 필요함.
                                                rowdata.Dispose();
                                            }
                                        }
                                    }
                                }
                                finally
                                {
                                    if (sqlcmd != null)
                                        sqlcmd.Dispose();
                                }
                            }
                        }
                        finally
                        {
                            conn.Close();

                            if (conn != null)
                                conn.Dispose();
                        }
                    }
                //}
            }
            catch (Exception)
            {
                string temp = string.Empty;
                {
                    temp = "SCS_SKOH2_{0}.TB_EQP_INFO";
                }

                LogManager.WriteConsoleLog(eLogLevel.Error, string.Format(temp, eqpno) + " TB_EQP_INFO Data is empty or invalid");
                //LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                //EqpListForMap(++restarteqpno);
            }

            return eqpinfo;
        }

        public bool CheckTableExist(int eqpno ,string tablename)
        {
            string sql = string.Empty;
            bool returnvalue = false;

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        {
                            sql = "SELECT * FROM SCS_SKOH2_{0}.{1}";
                        }

                        conn.Open();
                        sql = string.Format(sql, eqpno, tablename);

                        using (OracleCommand sqlcmd = new OracleCommand(sql, conn))
                        {
                            try
                            {
                                using (OracleDataReader rowdata = sqlcmd.ExecuteReader())
                                {
                                    returnvalue = true;
                                }
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception)
            {
                LogManager.WriteDBLog(eLogLevel.Error, string.Format("{0} 테이블이 없습니다.", tablename));
                return returnvalue;
            }

            return returnvalue;
        }

        private async void setPortType(ePortInOutType rcvChangedType, CVLineModule rcvSelectCVLine)
        {
            await Task.Run(() =>    //20230217 RGJ 포트 컨베이어 모드 변경 비동기 방식으로 변경. 모드 변경에 문제가 있으면 UI Hang 걸림 방지.
            {
                rcvSelectCVLine.ChangeAllPortInOutType(rcvChangedType);
            });
        }

        private async void SetPortAccessMode(CV_BaseModule selcv, ePortAceessMode mode)
        {
            await Task.Run(() =>    //20230217 RGJ 포트 컨베이어 모드 변경 비동기 방식으로 변경. 모드 변경에 문제가 있으면 UI Hang 걸림 방지.
            {
                selcv.SetPortAccessMode(mode);
                //230217 HHJ SCS 개선     //왜 PortAccess Mode와 Enable을 같이 처리하도록 되어있는지?
                //cvLine.ChangeAllPortUseType(!cmdProperty.Equals(eUnitCommandProperty.Enable));
            });
        }

        private async void setAllPortUseType(CVLineModule SelectedCVLine, bool use)
        {
            await Task.Run(() =>    //20230217 RGJ 포트 컨베이어 모드 변경 비동기 방식으로 변경. 모드 변경에 문제가 있으면 UI Hang 걸림 방지.
            {
                SelectedCVLine.ChangeAllPortUseType(use);
            });
        }

        private async void SetAutoMode(CV_BaseModule selcv, eCVAutoManualState runmode)
        {
            await Task.Run(() =>    //20230217 RGJ 포트 컨베이어 모드 변경 비동기 방식으로 변경. 모드 변경에 문제가 있으면 UI Hang 걸림 방지.
            {
                selcv.SetAutoMode(runmode);
            });
        }

        private async void SetTrackPause(CV_BaseModule selcv, bool pausestate)
        {
            await Task.Run(() =>    //20230217 RGJ 포트 컨베이어 모드 변경 비동기 방식으로 변경. 모드 변경에 문제가 있으면 UI Hang 걸림 방지.
            {
                selcv.SetTrackPause(pausestate);
            });
        }

        public virtual bool DBConnectionInfoChange(EQPInfo info)
        {
            //OracleConnection conn = null;

            //if (DBConnectionChanging == false)
            //    DBConnectionChanging = true;

            //lock (DBConnectionChangeSyncObject)
            //{
            //    GlobalData.Current.DBSection.DBFirstConnIP = info.DBFirstIP;
            //    GlobalData.Current.DBSection.DBFirstConnPort = info.DBFirstPort;
            //    GlobalData.Current.DBSection.DBFirstConnServiceName = info.DBFirstServiceName;
            //    GlobalData.Current.DBSection.DBSecondConnIP = info.DBSecondIP;
            //    GlobalData.Current.DBSection.DBSecondConnPort = info.DBSecondPort;
            //    GlobalData.Current.DBSection.DBSecondConnServiceName = info.DBSecondServiceName;
            //    GlobalData.Current.DBSection.DBAccountName = info.DbAccount;
            //    GlobalData.Current.DBSection.DBPassword = info.DbPassword;
            //}

            //try
            //{
            //    conn = new OracleConnection(OracleFirstDBPath);
            //    conn.Open();

            //    OracleDBPath = OracleFirstDBPath;
            //}
            //catch (Exception)
            //{
            //    if (conn != null)
            //        conn.Dispose();

            //    conn = new OracleConnection(OracleSecondDBPath);
            //    conn.Open();

            //    OracleDBPath = OracleSecondDBPath;
            //}
            //finally
            //{
            //    conn.Close();

            //    if (conn != null)
            //        conn?.Dispose();
            //}

            //DBConnectionChanging = false;

            return true;
        }

        public void ConnectPathSet(string path)
        {
            OracleDBPath = path;
        }

        public bool DbSetProcedureConnectClientInfo(bool del = false, string prevEQPID = "", string prevPCName = "", string prevPCIP = "", string prevDBConnInfo = "")
        {
            try
            {
                if (!string.IsNullOrEmpty(prevDBConnInfo))
                {
                    using (OracleConnection conn = new OracleConnection(prevDBConnInfo))
                    {
                        try
                        {
                            conn.Open();

                            using (OracleCommand sqlcmd = new OracleCommand("USP_STC_CONNECT_CLIENT_SET", conn))
                            {
                                try
                                {
                                    sqlcmd.CommandType = CommandType.StoredProcedure;

                                    OracleParameter R_RESULT = new OracleParameter("R_RESULT", OracleDbType.NVarchar2, 20);
                                    OracleParameter R_TEMP = new OracleParameter("R_TEMP", OracleDbType.NVarchar2, 20);

                                    sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2, ParameterDirection.Input).Value = prevEQPID;
                                    sqlcmd.Parameters.Add("P_CLIENT_CD", OracleDbType.NVarchar2, ParameterDirection.Input).Value = prevPCName;
                                    sqlcmd.Parameters.Add("P_CLIENT_IP", OracleDbType.NVarchar2, ParameterDirection.Input).Value = prevPCIP;
                                    sqlcmd.Parameters.Add("P_DEL", OracleDbType.Char, ParameterDirection.Input).Value = del ? '1' : '0';
                                    sqlcmd.Parameters.Add(R_RESULT).Direction = ParameterDirection.Output;
                                    sqlcmd.Parameters.Add(R_TEMP).Direction = ParameterDirection.Output;

                                    sqlcmd.ExecuteNonQuery();

                                    string temp = string.Format("Add Result = {0}", R_RESULT.Value.ToString());
                                    string temp1 = string.Format("Add Temp = {0}", R_TEMP.Value.ToString());

                                    //LogManager.WriteConsoleLog(eLogLevel.Info, temp1 + ", " + temp);
                                    LogManager.WriteDBLog(eLogLevel.Info, temp1 + ", " + temp, false);
                                }
                                finally
                                {
                                    if (sqlcmd != null)
                                        sqlcmd.Dispose();
                                }
                            }
                        }
                        finally
                        {
                            conn.Close();

                            if (conn != null)
                                conn.Dispose();
                        }
                    }
                }
                else
                {
                    using (OracleConnection conn = new OracleConnection(OracleDBPath))
                    {
                        try
                        {
                            conn.Open();

                            using (OracleCommand sqlcmd = new OracleCommand("USP_STC_CONNECT_CLIENT_SET", conn))
                            {
                                try
                                {
                                    sqlcmd.CommandType = CommandType.StoredProcedure;

                                    OracleParameter R_RESULT = new OracleParameter("R_RESULT", OracleDbType.NVarchar2, 20);
                                    OracleParameter R_TEMP = new OracleParameter("R_TEMP", OracleDbType.NVarchar2, 20);

                                    sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2, ParameterDirection.Input).Value = string.IsNullOrEmpty(prevEQPID) ? GlobalData.Current.EQPID : prevEQPID;
                                    sqlcmd.Parameters.Add("P_CLIENT_CD", OracleDbType.NVarchar2, ParameterDirection.Input).Value = string.IsNullOrEmpty(prevPCName) ? GlobalData.Current.ClientPCName : prevPCName;
                                    sqlcmd.Parameters.Add("P_CLIENT_IP", OracleDbType.NVarchar2, ParameterDirection.Input).Value = string.IsNullOrEmpty(prevPCIP) ? GlobalData.Current.CurrentIP : prevPCIP;
                                    sqlcmd.Parameters.Add("P_DEL", OracleDbType.Char, ParameterDirection.Input).Value = del ? '1' : '0';
                                    sqlcmd.Parameters.Add(R_RESULT).Direction = ParameterDirection.Output;
                                    sqlcmd.Parameters.Add(R_TEMP).Direction = ParameterDirection.Output;

                                    sqlcmd.ExecuteNonQuery();

                                    string temp = string.Format("Add Result = {0}", R_RESULT.Value.ToString());
                                    string temp1 = string.Format("Add Temp = {0}", R_TEMP.Value.ToString());

                                    //LogManager.WriteConsoleLog(eLogLevel.Info, temp1 + ", " + temp);
                                    LogManager.WriteDBLog(eLogLevel.Info, temp1 + ", " + temp, false);
                                }
                                finally
                                {
                                    if (sqlcmd != null)
                                        sqlcmd.Dispose();
                                }
                            }
                        }
                        finally
                        {
                            conn.Close();

                            if (conn != null)
                                conn.Dispose();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return false;
            }

            return true;
        }

        public List<ConnectClientList> DbGetProcedureConnectClientInfo()
        {
            //List<ConnectClientList> clientList = new List<ConnectClientList>();
            DataSet dataSet = new DataSet();

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        using (OracleCommand sqlcmd = new OracleCommand("UFN_STC_CONNECT_CLIENT_GET", conn))
                        {
                            try
                            {
                                sqlcmd.CommandType = CommandType.StoredProcedure;

                                OracleParameter output = sqlcmd.Parameters.Add("TMP_DATA", OracleDbType.RefCursor);
                                output.Direction = ParameterDirection.ReturnValue;

                                OracleParameter input = sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2);
                                input.Direction = ParameterDirection.Input;
                                input.Value = GlobalData.Current.EQPID;

                                sqlcmd.ExecuteNonQuery();

                                using (OracleDataAdapter oradata = new OracleDataAdapter(sqlcmd))
                                {
                                    try
                                    {
                                        oradata.Fill(dataSet);
                                    }
                                    finally
                                    {
                                        if (oradata != null)
                                            oradata.Dispose();
                                    }
                                }
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }

                    ClientList.Clear();

                    int table = dataSet.Tables.Count;
                    for (int i = 0; i < table; i++)// set the table value in list one by one
                    {
                        foreach (DataRow dr in dataSet.Tables[i].Rows)
                        {
                            if (ClientList.Where(list => list.ClientPCName == dr["CLIENT_CD"].ToString()).Count() == 0)
                            {
                                ClientList.Add(new ConnectClientList
                                {
                                    EQPID = Convert.ToString(dr["SCS_CD"]),
                                    ClientPCName = Convert.ToString(dr["CLIENT_CD"]),
                                    ClientIP = Convert.ToString(dr["CLIENT_IP"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return ClientList;
            }

            return ClientList;
        }

        public bool DbSetProcedurePLCInfo(PLCStateViewModelData plcstate)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        using (OracleCommand sqlcmd = new OracleCommand("USP_STC_PLC_INFO_SET", conn))
                        {
                            try
                            {
                                sqlcmd.CommandType = CommandType.StoredProcedure;

                                OracleParameter R_RESULT = new OracleParameter("R_RESULT", OracleDbType.NVarchar2, 64);
                                OracleParameter R_TEMP = new OracleParameter("R_TEMP", OracleDbType.NVarchar2, 64);

                                sqlcmd.Parameters.Add("P_EVENTTIME_DTTM", OracleDbType.NVarchar2, ParameterDirection.Input).Value = plcstate.StateChangeTime;
                                sqlcmd.Parameters.Add("P_PLC_NO", OracleDbType.Int32, ParameterDirection.Input).Value = plcstate.No;
                                sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2, ParameterDirection.Input).Value = GlobalData.Current.EQPID;
                                sqlcmd.Parameters.Add("P_PLC_CD", OracleDbType.NVarchar2, ParameterDirection.Input).Value = plcstate.PLCName;
                                sqlcmd.Parameters.Add("P_PLC_IP", OracleDbType.NVarchar2, ParameterDirection.Input).Value = plcstate.ConnectionInfo;
                                sqlcmd.Parameters.Add("P_PLC_STAT", OracleDbType.NVarchar2, ParameterDirection.Input).Value = plcstate.State.ToString();

                                sqlcmd.Parameters.Add(R_RESULT).Direction = ParameterDirection.Output;
                                sqlcmd.Parameters.Add(R_TEMP).Direction = ParameterDirection.Output;

                                sqlcmd.ExecuteNonQuery();

                                string temp = string.Format("Add Result = {0}", R_RESULT.Value.ToString());
                                string temp1 = string.Format("Add Temp = {0}", R_TEMP.Value.ToString());

                                LogManager.WriteDBLog(eLogLevel.Info, temp1 + ", " + temp, false);
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return false;
            }

            return true;
        }

        public bool DbSetProcedurePLCInfo(PLCStateData plcstate, int No)
        {
            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        using (OracleCommand sqlcmd = new OracleCommand("USP_STC_PLC_INFO_SET", conn))
                        {
                            try
                            {
                                sqlcmd.CommandType = CommandType.StoredProcedure;

                                OracleParameter R_RESULT = new OracleParameter("R_RESULT", OracleDbType.NVarchar2, 64);
                                OracleParameter R_TEMP = new OracleParameter("R_TEMP", OracleDbType.NVarchar2, 64);

                                sqlcmd.Parameters.Add("P_EVENTTIME_DTTM", OracleDbType.NVarchar2, ParameterDirection.Input).Value = plcstate.StateChangeTime;
                                sqlcmd.Parameters.Add("P_PLC_NO", OracleDbType.Int32, ParameterDirection.Input).Value = No;
                                sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2, ParameterDirection.Input).Value = GlobalData.Current.EQPID;
                                sqlcmd.Parameters.Add("P_PLC_CD", OracleDbType.NVarchar2, ParameterDirection.Input).Value = plcstate.PLCName;
                                sqlcmd.Parameters.Add("P_PLC_IP", OracleDbType.NVarchar2, ParameterDirection.Input).Value = plcstate.ConnectInfo;
                                sqlcmd.Parameters.Add("P_PLC_STAT", OracleDbType.NVarchar2, ParameterDirection.Input).Value = plcstate.State.ToString();

                                sqlcmd.Parameters.Add(R_RESULT).Direction = ParameterDirection.Output;
                                sqlcmd.Parameters.Add(R_TEMP).Direction = ParameterDirection.Output;

                                sqlcmd.ExecuteNonQuery();

                                string temp = string.Format("Add Result = {0}", R_RESULT.Value.ToString());
                                string temp1 = string.Format("Add Temp = {0}", R_TEMP.Value.ToString());

                                LogManager.WriteDBLog(eLogLevel.Info, temp1 + ", " + temp, false);
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return false;
            }

            return true;
        }

        public List<PLCStateData> DbGetProcedurePLCInfo()
        {
            List<PLCStateData> PLClist = new List<PLCStateData>();
            DataSet dataSet = new DataSet();

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        using (OracleCommand sqlcmd = new OracleCommand("UFN_STC_PLC_INFO_GET", conn))
                        {
                            try
                            {
                                sqlcmd.CommandType = CommandType.StoredProcedure;

                                OracleParameter output = sqlcmd.Parameters.Add("TMP_DATA", OracleDbType.RefCursor);
                                output.Direction = ParameterDirection.ReturnValue;

                                OracleParameter input1 = sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2);
                                input1.Direction = ParameterDirection.Input;
                                input1.Value = GlobalData.Current.EQPID;

                                sqlcmd.ExecuteNonQuery();

                                using (OracleDataAdapter oradata = new OracleDataAdapter(sqlcmd))
                                {
                                    try
                                    {
                                        oradata.Fill(dataSet);
                                    }
                                    finally
                                    {
                                        if (oradata != null)
                                            oradata.Dispose();
                                    }
                                }
                            }
                            finally
                            {
                                for (int i = 0; i < sqlcmd.Parameters.Count; i++)
                                {
                                    sqlcmd.Parameters[i].Dispose();
                                }

                                sqlcmd.Parameters.Clear();

                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }

                int table = dataSet.Tables.Count;
                for (int i = 0; i < table; i++)// set the table value in list one by one
                {
                    foreach (DataRow dr in dataSet.Tables[i].Rows)
                    {
                        PLClist.Add(new PLCStateData
                        {
                            PLCName = dr["PLC_CD"].ToString(),
                            ConnectInfo = dr["PLC_IP"].ToString(),
                            State = (ePLCStateDataState)Enum.Parse(typeof(ePLCStateDataState), dr["PLC_STAT"].ToString()),
                            StateChangeTime = DateTime.Parse(dr["EVENTTIME_DTTM"].ToString()),
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return PLClist;
            }

            return PLClist;
        }

        public List<AuthorityItem> DbGetProcedureAuthorityInfo()
        {
            List<AuthorityItem> Authoritylist = new List<AuthorityItem>();
            DataSet dataSet = new DataSet();

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        using (OracleCommand sqlcmd = new OracleCommand("UFN_STC_GROUP_AUTHORITY_INFO_GET", conn))
                        {
                            try
                            {
                                sqlcmd.CommandType = CommandType.StoredProcedure;

                                OracleParameter output = sqlcmd.Parameters.Add("TMP_DATA", OracleDbType.RefCursor);
                                output.Direction = ParameterDirection.ReturnValue;

                                OracleParameter input1 = sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2);
                                input1.Direction = ParameterDirection.Input;
                                input1.Value = GlobalData.Current.EQPID;

                                sqlcmd.ExecuteNonQuery();

                                using (OracleDataAdapter oradata = new OracleDataAdapter(sqlcmd))
                                {
                                    try
                                    {
                                        oradata.Fill(dataSet);
                                    }
                                    finally
                                    {
                                        if (oradata != null)
                                            oradata.Dispose();
                                    }
                                }
                            }
                            finally
                            {
                                for (int i = 0; i < sqlcmd.Parameters.Count; i++)
                                {
                                    sqlcmd.Parameters[i].Dispose();
                                }

                                sqlcmd.Parameters.Clear();

                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }

                int table = dataSet.Tables.Count;
                for (int i = 0; i < table; i++)// set the table value in list one by one
                {
                    foreach (DataRow dr in dataSet.Tables[i].Rows)
                    {
                        Authoritylist.Add(new AuthorityItem
                        {
                            Authority_Level = (eUserLevel)Enum.Parse(typeof(eUserLevel), dr["A_LEVEL"].ToString()),
                            Authority_Name = dr["A_NAME"].ToString(),
                            Name_KOR = dr["NAME_KOR"].ToString(),
                            Name_HUN = dr["NAME_HUN"].ToString(),
                            Name_CHN = dr["NAME_CHN"].ToString(),
                            ReadAccess = Convert.ToBoolean(Convert.ToInt32(dr["P_READ"])),
                            ModifyAccess = Convert.ToBoolean(Convert.ToInt32(dr["P_MODIFY"])),
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return Authoritylist;
            }

            return Authoritylist;
        }

        public bool DbSetProcedureAuthorityInfo(AuthorityItem AItem)
        {
            try
            {
                if(AItem == null)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "DbSetProcedureAuthorityInfo AuthorityItem is null!");
                    return false;
                }
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        using (OracleCommand sqlcmd = new OracleCommand())
                        {
                            try
                            {
                                sqlcmd.CommandText = "USP_STC_GROUP_AUTHORITY_SET";
                                sqlcmd.CommandType = CommandType.StoredProcedure;
                                sqlcmd.Connection = conn;

                                OracleParameter R_RESULT = new OracleParameter("R_RESULT", OracleDbType.NVarchar2, 20);
                                OracleParameter R_TEMP = new OracleParameter("R_TEMP", OracleDbType.NVarchar2, 20);


                                sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2, ParameterDirection.Input).Value = GlobalData.Current.EQPID;
                                sqlcmd.Parameters.Add("P_A_LEVEL", OracleDbType.NVarchar2, ParameterDirection.Input).Value = AItem.Authority_Level;
                                sqlcmd.Parameters.Add("P_A_NAME", OracleDbType.NVarchar2, ParameterDirection.Input).Value = AItem.Authority_Name;
                                sqlcmd.Parameters.Add("P_NAME_KOR", OracleDbType.NVarchar2, ParameterDirection.Input).Value = AItem.Name_KOR;
                                sqlcmd.Parameters.Add("P_NAME_CHN", OracleDbType.NVarchar2, ParameterDirection.Input).Value = AItem.Name_CHN;
                                sqlcmd.Parameters.Add("P_NAME_HUN", OracleDbType.NVarchar2, ParameterDirection.Input).Value = AItem.Name_HUN;
                                sqlcmd.Parameters.Add("P_READ_STAT", OracleDbType.Char, ParameterDirection.Input).Value = AItem.ReadAccess ? '1' : '0';
                                sqlcmd.Parameters.Add("P_MODIFY_STAT", OracleDbType.Char, ParameterDirection.Input).Value = AItem.ModifyAccess ? '1' : '0';


                                sqlcmd.Parameters.Add(R_RESULT).Direction = ParameterDirection.Output;
                                sqlcmd.Parameters.Add(R_TEMP).Direction = ParameterDirection.Output;

                                sqlcmd.ExecuteNonQuery();

                                string temp = string.Format("Add Result = {0}", R_RESULT.Value.ToString());
                                string temp1 = string.Format("Add Temp = {0}", R_TEMP.Value.ToString());

                                LogManager.WriteDBLog(eLogLevel.Info, temp1 + ", " + temp, false);
                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return false;
            }
            return true;
        }

        public bool DbGetCarrierIDExistInCarrierTable(string CarrierID)
        {
            string sql = string.Empty, bank = string.Empty, bay = string.Empty, lvl = string.Empty;
            string eventinfo = string.Empty, temperature = string.Empty, smokesense = string.Empty, warntemp = string.Empty, dangertemp = string.Empty;
            string eventtime = string.Empty;

            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();

                        sql = string.Format("SELECT  count(*) from tb_carrier_info WHERE carrier_id = '{0}' AND SCS_CD = '{1}'", CarrierID, GlobalData.Current.EQPID);

                        using (OracleCommand sqlcmd = new OracleCommand(sql, conn))
                        {
                            try
                            {
                                var countvalue = sqlcmd.ExecuteScalar();
                                return countvalue.ToString() == "1";

                            }
                            finally
                            {
                                if (sqlcmd != null)
                                    sqlcmd.Dispose();
                            }
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                return false;
            }
        }


        public void RemoveLogData(int PreserveDays)
        {
            if (PreserveDays <= 0) //혹시 0이나 음수 들어오면 삭제안함.
            {
                return;
            }
            DateTime TargetDate = DateTime.Now.AddDays(-PreserveDays);
            try
            {
                using (OracleConnection conn = new OracleConnection(OracleDBPath))
                {
                    try
                    {
                        conn.Open();
                        string sql = string.Format("DELETE FROM TB_UNITED_LOG_INFO WHERE LOG_NM <> 'ALARM' AND RECODE_DTTM < '{0}'", DataTimeForOracleUntilMillisecond(TargetDate));
                        using (var cmd = new OracleCommand(sql, conn))
                        {
                            cmd.ExecuteNonQuery();
                        }

                        sql = string.Format("DELETE FROM TB_UNITED_LOG_INFO WHERE LOG_NM = 'ALARM' AND COL_7 <> '0001/01/01 00:00:00' AND COL_7 < '{0}'", DataTimeForOracleUntilMillisecond(TargetDate));
                        using (var cmd = new OracleCommand(sql, conn))
                        {
                            cmd.ExecuteNonQuery();
                        }
                    }
                    finally
                    {
                        conn.Close();

                        if (conn != null)
                            conn.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }

        #region IDisposable Support
        private bool disposedValue = false; // 중복 호출을 검색하려면

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: 관리되는 상태(관리되는 개체)를 삭제합니다.
                }

                // TODO: 관리되지 않는 리소스(관리되지 않는 개체)를 해제하고 아래의 종료자를 재정의합니다.
                // TODO: 큰 필드를 null로 설정합니다.
                //conn.Close();

                //conn?.Dispose();
                //tran?.Dispose();

                disposedValue = true;
            }
        }

        // TODO: 위의 Dispose(bool disposing)에 관리되지 않는 리소스를 해제하는 코드가 포함되어 있는 경우에만 종료자를 재정의합니다.
        ~OracleDBManager()
        {
            // 이 코드를 변경하지 마세요. 위의 Dispose(bool disposing)에 정리 코드를 입력하세요.
            Dispose(false);
        }

        // 삭제 가능한 패턴을 올바르게 구현하기 위해 추가된 코드입니다.
        public void Dispose()
        {
            // 이 코드를 변경하지 마세요. 위의 Dispose(bool disposing)에 정리 코드를 입력하세요.
            Dispose(true);
            // TODO: 위의 종료자가 재정의된 경우 다음 코드 줄의 주석 처리를 제거합니다.
            GC.SuppressFinalize(this);
        }
        #endregion
    }


    //230329 함수가 추가/변경/삭제되면 enum 수정해야함.
    public enum ProcedureName
    {
        USP_STC_ALARM_INFO_SET,
        USP_STC_CARRIER_INFO_SET,
        USP_STC_CLIENT_ORDER_SET,
        USP_STC_CONFIG_INFO_SET,
        USP_STC_CONNECT_CLIENT_SET,
        USP_STC_EQP_INFO_SET,
        USP_STC_JOB_INFO_SET,
        USP_STC_LOG_INFO_SET,
        USP_STC_PIO_INFO_SET,
        USP_STC_SHELF_INFO_SET,
        USP_STC_USER_INFO_SET,
        USP_STC_TERMINAL_MSG_SET,
        USP_STC_GROUP_AUTHORITY_SET,

        Last,
    }


    public enum FunctionName
    {
        UFN_STC_ALARM_INFO_GET,
        UFN_STC_CARRIER_INFO_GET,
        UFN_STC_CLIENT_ORDER_GET,
        UFN_STC_CONFIG_INFO_GET,
        UFN_STC_CONNECT_CLIENT_GET,
        UFN_STC_EQP_INFO_GET,
        UFN_STC_JOB_INFO_GET,
        UFN_STC_LOG_INFO_GET,
        UFN_STC_PIO_INFO_GET,
        UFN_STC_SHELF_INFO_GET,
        UFN_STC_USER_INFO_GET,
        UFN_STC_TERMINAL_MSG_GET,
        UFN_STC_GROUP_AUTHORITY_INFO_GET,

        Last,
    }

}