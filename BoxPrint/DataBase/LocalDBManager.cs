using PLCProtocol.DataClass;
using BoxPrint.Alarm;
using BoxPrint.DataList.MCS;
using BoxPrint.Log;
using BoxPrint.Modules.Shelf;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using WCF_LBS.Commands;

namespace BoxPrint.Database
{
    //Data 복구용 DB 추가.
    public class LocalDBManager : IDisposable
    {
        private readonly object DBLock = new object(); //데이터 베이스 접근 락
        private readonly int PreserveDay = 3;
        public static LocalDBManager Current;
        private string DBPath
        {
            get
            {
                return "Data Source=" + DBFILE_Path;
            }
        }
        private string DBFILE_Path = "";

        SQLiteConnection conn = null;

        public LocalDBManager(string Path)
        {
            Current = this;
            DBFILE_Path = Path;
            SQLiteCommand cmd = null;
            try
            {
                //DB 파일이 있는지 확인해 본다.
                if (!File.Exists(DBFILE_Path))
                {
                    string sql;
                    SQLiteConnection.CreateFile(DBFILE_Path); //없으면 디비 생성
                    conn = new SQLiteConnection(DBPath); //연결 시도
                    conn.Open();
                    //Alarm 로그 테이블 생성.
                    sql = @"CREATE TABLE [AlarmLogTable] (
                          [LogID] INT NOT NULL ON CONFLICT REPLACE, 
                          [AlarmID] INT, 
                          [Module] VARCHAR(20), 
                          [AlarmName] VARCHAR(50), 
                          [Description] VARCHAR(255), 
                          [Solution] VARCHAR(255), 
                          [LightAlarm] BOOL, 
                          [OccurDateTime] DATETIME, 
                          [ClearDateTime] DATETIME, 
                          [AlarmClear] BOOL, 
                          CONSTRAINT [sqlite_autoindex_AlarmLogTable_1] PRIMARY KEY ([LogID] COLLATE BINARY DESC) ON CONFLICT REPLACE)";
                    cmd = new SQLiteCommand(sql, conn);
                    cmd.ExecuteNonQuery();


                    //PB CraneCommandLog Table생성
                    sql = @"CREATE TABLE [PB_CraneCommandLog] (
                          [PB_TIME] DATETIME, 
                          [PB_SEQ] INT, 
                          [PB_EVENTTYPE] INT, 
                          [PB_COMMANDID] VARCHAR(40), 
                          [PB_COMMANDNUMBER] INT, 
                          [PB_CARRIERID] VARCHAR(40), 
                          [PB_CRANEID] VARCHAR(20), 
                          [PB_CRANECOMMAND] INT, 
                          [PB_TARGETTYPE] INT, 
                          [PB_COMMANDBANK] INT, 
                          [PB_COMMANDBAY] INT, 
                          [PB_COMMANDLEVEL] INT)";
                    cmd = new SQLiteCommand(sql, conn);
                    cmd.ExecuteNonQuery();

                    //PB IOLog Table생성
                    sql = @"CREATE TABLE [PB_IOLog] (
                          [PB_TIME] DATETIME, 
                          [PB_SEQ] INT, 
                          [PB_UNITTYPE] INT, 
                          [PB_UNITKEY] VARCHAR(20), 
                          [PB_AREAKEY] INT, 
                          [PB_PLCDATA] BLOB NOT NULL,
                          [PB_IO_OFFSET] INT)";
                    cmd = new SQLiteCommand(sql, conn);
                    cmd.ExecuteNonQuery();

                    //PB IOSnapLog Table생성
                    sql = @"CREATE TABLE [PB_IOSnap] (
                          [PB_TIME] DATETIME, 
                          [PB_SEQ] INT NOT NULL, 
                          [PB_IODATA] BLOB NOT NULL)";
                    cmd = new SQLiteCommand(sql, conn);
                    cmd.ExecuteNonQuery();

                    //PB JobLog Table생성
                    sql = @"CREATE TABLE [PB_JobLog] (
                          [PB_TIME] DATETIME, 
                          [PB_SEQ] INT NOT NULL, 
                          [PB_EVENTTYPE] INT, 
                          [PB_COMMANDID] VARCHAR(40), 
                          [PB_JOBTYPE] VARCHAR(10), 
                          [PB_JOBSTATUS] INT, 
                          [PB_JOBSTEP] INT, 
                          [PB_PRIORITY] VARCHAR(10), 
                          [PB_CARRIERID] VARCHAR(40), 
                          [PB_SOURCELOC] VARCHAR(20), 
                          [PB_DESTLOC] VARCHAR(20), 
                          [PB_JOBDTTM] VARCHAR(20))";
                    cmd = new SQLiteCommand(sql, conn);
                    cmd.ExecuteNonQuery();

                    //PB JobSnap Table생성
                    sql = @"CREATE TABLE [PB_JobSnap] (
                          [PB_TIME] DATETIME, 
                          [PB_SEQ] INT, 
                          [PB_EVENTTYPE] INT, 
                          [PB_COMMANDID] VARCHAR(40), 
                          [PB_JOBTYPE] VARCHAR(10), 
                          [PB_JOBSTATUS] INT, 
                          [PB_JOBSTEP] INT, 
                          [PB_PRIORITY] VARCHAR(10), 
                          [PB_CARRIERID] VARCHAR(40), 
                          [PB_SOURCELOC] VARCHAR(20), 
                          [PB_DESTLOC] VARCHAR(20), 
                          [PB_JOBDTTM] VARCHAR(20))";
                    cmd = new SQLiteCommand(sql, conn);
                    cmd.ExecuteNonQuery();

                    //PB ShelfLog Table생성
                    sql = @"CREATE TABLE [PB_ShelfLog] (
                          [PB_TIME] DATETIME, 
                          [PB_SEQ] INT NOT NULL, 
                          [PB_EVENTTYPE] INT, 
                          [PB_SHELFTAG] VARCHAR(20) NOT NULL, 
                          [PB_SHELFTYPE] INT, 
                          [PB_SHELFSTATUS] INT NOT NULL, 
                          [PB_CARRIERID] VARCHAR(40), 
                          [PB_CARRIERSIZE] INT, 
                          [PB_SCHEDULED] BOOL)";
                    cmd = new SQLiteCommand(sql, conn);
                    cmd.ExecuteNonQuery();

                    //PB ShelfSnap Table생성
                    sql = @"CREATE TABLE [PB_ShelfSnap] (
                          [PB_TIME] DATETIME, 
                          [PB_SEQ] INT NOT NULL, 
                          [PB_EVENTTYPE] INT, 
                          [PB_SHELFTAG] VARCHAR(20) NOT NULL, 
                          [PB_SHELFTYPE] INT, 
                          [PB_SHELFSTATUS] INT NOT NULL, 
                          [PB_CARRIERID] VARCHAR(40), 
                          [PB_CARRIERSIZE] INT, 
                          [PB_SCHEDULED] BOOL)";
                    cmd = new SQLiteCommand(sql, conn);
                    cmd.ExecuteNonQuery();

                    #region Index 생성
                    sql = @"CREATE INDEX [IOIndex] ON [PB_IOLog]([PB_TIME])";
                    cmd = new SQLiteCommand(sql, conn);
                    cmd.ExecuteNonQuery();
                    sql = @"CREATE INDEX [IOSnapIndex] ON [PB_IOSnap]([PB_TIME])";
                    cmd = new SQLiteCommand(sql, conn);
                    cmd.ExecuteNonQuery();

                    sql = @"CREATE INDEX [JobIndex] ON [PB_JobLog]([PB_TIME])";
                    cmd = new SQLiteCommand(sql, conn);
                    cmd.ExecuteNonQuery();
                    sql = @"CREATE INDEX [JobSnapIndex] ON [PB_JobSnap]([PB_TIME])";
                    cmd = new SQLiteCommand(sql, conn);
                    cmd.ExecuteNonQuery();

                    sql = @"CREATE INDEX [ShelfIndex] ON [PB_ShelfLog]([PB_TIME])";
                    cmd = new SQLiteCommand(sql, conn);
                    cmd.ExecuteNonQuery();
                    sql = @"CREATE INDEX [ShelfSnapIndex] ON [PB_ShelfSnap]([PB_TIME])";
                    cmd = new SQLiteCommand(sql, conn);
                    cmd.ExecuteNonQuery();

                    sql = @"CREATE INDEX [CommandIndex] ON [PB_CraneCommandLog]([PB_TIME])";
                    cmd = new SQLiteCommand(sql, conn);
                    cmd.ExecuteNonQuery();
                    #endregion
                }
                else
                {
                    conn = new SQLiteConnection(DBPath); //연결 시도
                    conn.Open();
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
            finally
            {
                cmd?.Dispose();
            }
        }

        public DataTable GetAlarmRangeLog(DateTime Start, DateTime End)
        {
            //lock (DBLock)
            {
                SQLiteCommand cmd = null;
                try
                {
                    string sql = string.Format("SELECT * FROM AlarmLogTable WHERE OccurDateTime BETWEEN '{0}' AND '{1}'", SQLiteTime(Start), SQLiteTime(End));
                    //SQLiteDataReader를 이용하여 연결 모드로 데이타 읽기
                    cmd = new SQLiteCommand(sql, conn);
                    DataTable dt = new DataTable();
                    SQLiteDataAdapter SQLDA = new SQLiteDataAdapter(cmd);
                    SQLDA.Fill(dt);

                    return dt;

                }
                catch (Exception ex)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                    return null;
                }
                finally
                {
                    if (cmd != null)
                    {
                        cmd.Dispose();
                    }
                }
            }
        }

        public void InsertAlarmLog(AlarmData Alarm, bool Clear)
        {
            //lock (DBLock)
            {
                SQLiteCommand cmd = null;
                try
                {
                    string sql = string.Format("INSERT INTO AlarmLogTable (LogID,AlarmID,AlarmName,Module,Description,Solution,LightAlarm,OccurDateTime,ClearDateTime,AlarmClear) VALUES ({0},{1},'{2}','{3}','{4}','{5}',{6},'{7}','{8}',{9})",
                                            Alarm.LogID,
                                            Alarm.AlarmID,
                                            Alarm.AlarmName,
                                            Alarm.ModuleName,
                                            Alarm.Description,
                                            Alarm.Solution,
                                            Alarm.IsLightAlarm ? 1 : 0,
                                            SQLiteTime(Alarm.OccurDateTime),
                                            SQLiteTime(Alarm.ClearDateTime),
                                            Clear ? 1 : 0);
                    cmd = new SQLiteCommand(sql, conn);
                    cmd.ExecuteNonQuery();
                }
                catch (Exception ex)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                }
                finally
                {
                    cmd?.Dispose();
                }
            }
        }






        public int GetAlarmLogCount()
        {
            //lock (DBLock)
            {
                Int64 count = 0;
                SQLiteCommand cmd = null;

                try
                {

                    string sql = string.Format("SELECT count(LogID) FROM AlarmLogTable");
                    cmd = new SQLiteCommand(sql, conn);
                    count = (Int64)cmd.ExecuteScalar();
                    return (int)count;
                }
                catch (Exception ex)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
                    return 0;
                }
                finally
                {
                    if (cmd != null)
                    {
                        cmd?.Dispose();
                    }
                }
            }
        }

        private static string SQLiteTime(DateTime datetime)
        {
            return datetime.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }



        #region 기존 비사용 코드
        #region 소모품 관리 삭제
        //public bool GetPartsLifeTimeData(List<PartsLifeItem> PartsDataList)
        //{
        //    if (PartsDataList == null)
        //    {
        //        LogManager.WriteConsoleLog(eLogLevel.Info, "GetPartsLifeTimeData. 인자가 Null 입니다.");
        //        return false;
        //    }
        //    PartsDataList.Clear();
        //    //lock (DBLock)
        //    {
        //        DateTime dt = DateTime.Now;
        //        SQLiteCommand cmd = null;
        //        SQLiteDataReader rdr = null;
        //        //디비 현재 파츠 소모품 상태정보 획득

        //        string pModule = "";
        //        string pName = "";
        //        string pModel = "";
        //        string pMaker = "";
        //        string pDesc = "";
        //        string MU = "";

        //        double currentVaule = 0;
        //        double lifeValue = 0;

        //        try
        //        {
        //            string sql = "SELECT * FROM LifeTimeTable";
        //            cmd = new SQLiteCommand(sql, conn);
        //            rdr = cmd.ExecuteReader();
        //            while (rdr.Read())
        //            {
        //                pModule = (string)rdr["ModuleName"];
        //                pName = (string)rdr["PartsName"];
        //                pModel = (string)rdr["PartsModel"];
        //                pMaker = (string)rdr["PartsMaker"];
        //                pDesc = (string)rdr["PartsDesc"];
        //                MU = (string)rdr["MeasurementUnits"];
        //                currentVaule = (double)rdr["CurrentValue"];
        //                lifeValue = (double)rdr["LifeTimeValue"];

        //                PartsDataList.Add(new PartsLifeItem(pModule, pName, pModel, pMaker, pDesc, MU, currentVaule, lifeValue));
        //            }
        //            return true;
        //        }
        //        catch (Exception ex)
        //        {
        //            LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
        //            return false;
        //        }
        //        finally
        //        {

        //            TimeSpan sp = DateTime.Now - dt;
        //            double k = sp.TotalMilliseconds;
        //            cmd?.Dispose();
        //        }
        //    }
        //}

        //public bool AddPartsLifeTime(int PartsID,string ModuleName, string PartsName,string PartsModel, string PartsMaker,string Desc,string MU,int LifeItme)
        //{
        //    //lock (DBLock)
        //    {
        //        SQLiteCommand cmd = null;
        //        try
        //        {
        //            string sql = string.Format("INSERT INTO LifeTimeTable  (PartsID,ModuleName,PartsName,PartsModel,PartsMaker,PartsDesc,MeasurementUnits,CurrentValue,LifeTimeValue) VALUES ({0},'{1}','{2}','{3}','{4}','{5}','{6}',{7},{8})",
        //                                    PartsID,
        //                                    ModuleName,
        //                                    PartsName,
        //                                    PartsModel,
        //                                    PartsMaker,
        //                                    Desc,
        //                                    MU,
        //                                    0,
        //                                    LifeItme
        //                                    );
        //            cmd = new SQLiteCommand(sql, conn);
        //            cmd.ExecuteNonQuery();
        //            return true;
        //        }
        //        catch (Exception ex)
        //        {
        //            LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
        //            return false;
        //        }
        //        finally
        //        {
        //            cmd?.Dispose();
        //        }
        //    }
        //}

        //public void UpdatePartsLifeTimeState(string ModuleName, string PartsName, double newValue)
        //{
        //    //lock (DBLock)
        //    {
        //        SQLiteCommand cmd = null;
        //        try
        //        {
        //            string sql = string.Format("UPDATE LifeTimeTable SET CurrentValue = {2} where ModuleName = '{0}' and PartsName = '{1}'",
        //                                    ModuleName,
        //                                    PartsName,
        //                                    newValue);
        //            cmd = new SQLiteCommand(sql, conn);
        //            cmd.ExecuteNonQuery();
        //        }
        //        catch (Exception ex)
        //        {
        //            LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
        //        }
        //        finally
        //        {
        //            cmd?.Dispose();
        //        }
        //    }
        //}

        //public void UpdateAllParts(List<PartsLifeItem> PartsDataList)
        //{
        //    //lock (DBLock)
        //    {
        //        SQLiteCommand cmd = null;
        //        SQLiteTransaction transaction = conn.BeginTransaction(); //일괄 작업 시작.
        //        foreach (var item in PartsDataList)
        //        {
        //            if(item.IsUpdateRequire())
        //            {
        //                string sql = string.Format("UPDATE LifeTimeTable SET CurrentValue = {2} where ModuleName = '{0}' and PartsName = '{1}'",
        //                item.ModuleName,
        //                item.PartsName,
        //                item.CurrentValue);
        //                cmd = new SQLiteCommand(sql, conn);
        //                cmd.ExecuteNonQuery();
        //                item.NotifyUpdateComplete();
        //            }
        //            else
        //            {
        //                continue;
        //            }
        //        }
        //        transaction.Commit();
        //    }
        //}
        #endregion

        //public static void MakeLCSShelfQuery()
        //{
        //    StringBuilder sb = new StringBuilder();
        //    string FileName = "SQL.txt";
        //    sb.AppendLine("INSERT INTO scs_shelf (ID,Name,Description,Type,State,Status,StockerID,ZoneID,CarrierID,CarrierSize,Bank,Bay,Level) VALUES");
        //    foreach (var Item in GlobalData.Current.MainBooth.FrontData)
        //    {
        //        if (Item.DeadZone)
        //        {
        //            continue;
        //        }
        //        string Zone = string.Format("{0}{1:D3}{2:D3}", Item.ShelfBank, Item.ShelfBay, Item.ShelfLevel);
        //        string sql = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}',{9},'{10}','{11}','{12}'),",
        //                         "L-51-ML-04_" + Zone,
        //                         "L-51-ML-04_" + Zone,
        //                         "",
        //                         "SHELF",
        //                         "INSERVICE",
        //                         "EMPTY",
        //                         "L-51-ML-04",
        //                         "L-51-ML-04",
        //                          "",
        //                          Item.TrayHeight,
        //                          Item.ShelfBank,
        //                          Item.ShelfBay,
        //                          Item.ShelfLevel
        //                       );
        //        sb.AppendLine(sql);
        //    }
        //    foreach (var Item in GlobalData.Current.MainBooth.RearData)
        //    {
        //        if (Item.DeadZone)
        //        {
        //            continue;
        //        }
        //        string Zone = string.Format("{0}{1:D3}{2:D3}", Item.ShelfBank, Item.ShelfBay, Item.ShelfLevel);
        //        string sql = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}',{9},'{10}','{11}','{12}'),",
        //                          "L-51-ML-04_" + Zone,
        //                         "L-51-ML-04_" + Zone,
        //                         "",
        //                         "SHELF",
        //                         "INSERVICE",
        //                         "EMPTY",
        //                         "L-51-ML-04",
        //                         "L-51-ML-04",
        //                          "",
        //                          Item.TrayHeight,
        //                          Item.ShelfBank,
        //                          Item.ShelfBay,
        //                          Item.ShelfLevel
        //                       );
        //        sb.AppendLine(sql);
        //    }
        //    sb.Remove(sb.Length - 3, 3); //마지막 , 제거
        //    sb.AppendLine(";");
        //    //sw.Write(sb.ToString());

        //    //port
        //    //상부 Port는 경우에 따라 위치 수정이 필요 할 수 있음!!( 공간 부족으로 Shelf 겹침, Front 는 진행 방향 반대)
        //    int InCnt = 0, OutCnt = 0;
        //    Dictionary<string, string> LineType = new Dictionary<string, string>();

        //    LineType.Add("A", "Auto");
        //    LineType.Add("M", "Manual");
        //    LineType.Add("E", "Error");
        //    LineType.Add("B", "Bridge");
        //    LineType.Add("S", "Stack");

        //    LineType.Add("I", "In");
        //    LineType.Add("O", "Out");

        //    sb.AppendLine("INSERT INTO SCS_Port(ID,Name,Description,Type,UnitType,State,Status,Mode,AccessMode,StockerID,ZoneID,CarrierID,REQState,Bank,Bay,Level,RobotLevel,NextPortID,CarrierSize,Line,SubLine,Receive,CMaterial,TagTrans,TimeOut,Enable)  VALUES");
        //    foreach (var lineItem in GlobalData.Current.PortManager.ModuleList)
        //    {
        //        int floor = Convert.ToInt32(lineItem.Value.LineFloor.Substring(0, 1));

        //        if (lineItem.Value.IsInPort)
        //            InCnt++;
        //        else
        //            OutCnt++;

        //        string NextName = "";
        //        int i = 0;
        //        foreach (var Item in lineItem.Value.ModuleList)
        //        {
        //            bool bPort = false;
        //            if (Item.CVModuleType.ToString().Contains("Robot"))
        //                bPort = true;

        //            int Idx = lineItem.Value.IsInPort ? InCnt : OutCnt;

        //            string sModule = Item.ModuleName.Substring(0, 1);
        //            string sMode = Item.ModuleName.Substring(1, 1);

        //            string sID = string.Format("{0}{1}", sMode, Idx);
        //            string Desc = string.Format("{0} {1} {2}_{3}", LineType[sModule], LineType[sMode], Item.ModuleName.Substring(2, 1), sID);

        //            string SubNum = (i == 0) ? "" : string.Format("{0}", i - 1);

        //            int height = 0;
        //            if (floor == 2)
        //            {
        //                if (Item.Position_Bank == 1)
        //                    height = GlobalData.Current.SystemParameter.FrontYcount - lineItem.Value.ModuleList.Count;
        //                else
        //                    height = GlobalData.Current.SystemParameter.RearYcount - lineItem.Value.ModuleList.Count;
        //            }
        //            int Pos_Level = lineItem.Value.ModuleList.Count - i + height;

        //            if (!lineItem.Value.IsInPort)
        //            {
        //                if (lineItem.Value.ModuleList.Count - i > 1)
        //                    NextName = "L-51-ML-04_" + sID + string.Format("{0}", i);
        //                else
        //                    NextName = "";
        //            }

        //            string sLevel = Item.Position_Level == 0 ? "" : Item.Position_Level.ToString();

        //            string sql = string.Format("('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}','{17}','{18}','{19}','{20}','{21}','{22}','{23}','{24}','{25}'),",
        //                             "L-51-ML-04_" + sID + SubNum,                      //0
        //                             Item.ModuleName,                                   //1
        //                             Desc,                                              //2
        //                             bPort ? "PORT" : "BUFFER",                         //3
        //                             Item.ModuleName.Substring(0, 2),                    //4
        //                             "INSERVICE", "EMPTY",                              //5, 6
        //                             lineItem.Value.IsInPort ? "INPUT" : "OUTPUT",      //7
        //                              "AUTO", "L-51-ML-04", "", "", "NO_REQUEST",       //8, 9, 10, 11, 12,
        //                              lineItem.Value.Position_Bank,                     //13
        //                              lineItem.Value.Position_Bay,                      //14
        //                              Pos_Level,                                        //15
        //                              sLevel,                                           //16
        //                              NextName,                                         //17
        //                              !lineItem.Value.IsInPort && bPort ? "0,1,2,3,4,5" : "", //18
        //                              "", "", "NONE", "N", "N", "Y", "Y"                //19, 20, 21, 22, 23, 24, 25
        //                           );

        //            if (lineItem.Value.IsInPort)
        //                NextName = "L-51-ML-04_" + sID + SubNum;

        //            i++;
        //            sb.AppendLine(sql);
        //        }
        //    }
        //    sb.Remove(sb.Length - 3, 3); //마지막 , 제거
        //    StreamWriter sw = new StreamWriter(FileName);
        //    sw.Write(sb.ToString());

        //    sw.Close();

        //    System.Diagnostics.Process.Start(@"C:\Windows\notepad.exe", IO.Path.GetDirectoryName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName) + "\\" + FileName); //쿼리 만들고 메모장 실행까지 추가.
        //}

        ///// <summary>
        ///// 컨베이어 라인에 저장된 마지막 데이터를 리스트에 넣어둔다.
        ///// </summary>
        ///// <returns></returns>
        //public bool GetRecoveryDataCVLines(List<CVLineRecoveryData> LineDataList)
        //{
        //    if (LineDataList == null)
        //    {
        //        LogManager.WriteConsoleLog(eLogLevel.Info, "GetRecoveryDataCVLines실패. 인자가 Null 입니다.");
        //        return false;
        //    }
        //    LineDataList.Clear();
        //    //lock (DBLock)
        //    {
        //        SQLiteCommand cmd = null;
        //        SQLiteDataReader rdr = null;
        //        string LineName = "";
        //        int Step = 0;
        //        try
        //        {
        //            string sql = "SELECT * FROM CVLineStateTable";
        //            cmd = new SQLiteCommand(sql, conn);
        //            rdr = cmd.ExecuteReader();
        //            while (rdr.Read())
        //            {
        //                LineName = (string)rdr["ModuleName"];
        //                Step = (int)rdr["ModuleStep"];
        //                LineDataList.Add(new CVLineRecoveryData() { LineModuleName = LineName, ModuleStep = Step });
        //            }
        //            return true;
        //        }
        //        catch (Exception ex)
        //        {
        //            LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
        //            return false;
        //        }
        //        finally
        //        {
        //            cmd?.Dispose();
        //        }
        //    }
        //}



        //public bool GetRecoveryDataCV(List<CVRecoveryData> CVDataList)
        //{
        //    if (CVDataList == null)
        //    {
        //        LogManager.WriteConsoleLog(eLogLevel.Info, "GetRecoveryDataCVLines실패. 인자가 Null 입니다.");
        //        return false;
        //    }
        //    CVDataList.Clear();
        //    //lock (DBLock)
        //    {
        //        SQLiteCommand cmd = null;
        //        SQLiteDataReader rdr = null;
        //        //각 컨베이어 디비 상태정보 획득
        //        string CVName = "";
        //        string ParentLineName = "";
        //        int Step = 0;
        //        bool TrayExist = false;
        //        string TagID = "";
        //        eCV_Speed RunState = eCV_Speed.None;
        //        eCV_StopperState StopperState = eCV_StopperState.Unknown;
        //        eCV_TurnState TurnState = eCV_TurnState.Unknown;
        //        eCV_DoorState DoorState = eCV_DoorState.Unknown;
        //        eTrayHeight TrayHeight = eTrayHeight.Height0;
        //        string InternalSignal = "";
        //        try
        //        {
        //            string sql = "SELECT * FROM PortStateTable";
        //            cmd = new SQLiteCommand(sql, conn);
        //            rdr = cmd.ExecuteReader();
        //            while (rdr.Read())
        //            {
        //                CVName = (string)rdr["ModuleName"];
        //                ParentLineName = (string)rdr["LineName"];
        //                Step = (int)rdr["ModuleStep"];

        //                RunState = (eCV_Speed)rdr["RunningState"];
        //                StopperState = (eCV_StopperState)rdr["StopperState"];
        //                TurnState = (eCV_TurnState)rdr["TurnState"];
        //                DoorState = (eCV_DoorState)rdr["DoorState"];

        //                TrayExist = (bool)rdr["TrayExist"];
        //                TagID = (string)rdr["TrayTagID"];
        //                TrayHeight = (eTrayHeight)rdr["TrayHeight"];
        //                InternalSignal = (string)rdr["InternalSignal"];

        //                CVDataList.Add(new CVRecoveryData()
        //                {
        //                    _ModuleName = CVName,
        //                    _LineName = ParentLineName,
        //                    _ModuleStep = Step,
        //                    _RunState = RunState,
        //                    _StopperState = StopperState,
        //                    _TurnState = TurnState,
        //                    _DoorState = DoorState,
        //                    _TrayExist = TrayExist,
        //                    _TagID = TagID,
        //                    _TrayHeight = TrayHeight,
        //                    _InternalSignals = InternalSignal
        //                }
        //                );
        //            }
        //            return true;
        //        }
        //        catch (Exception ex)
        //        {
        //            LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
        //            return false;
        //        }
        //        finally
        //        {
        //            cmd?.Dispose();
        //        }
        //    }
        //}
        #endregion


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
                conn?.Dispose();

                disposedValue = true;
            }
        }

        // TODO: 위의 Dispose(bool disposing)에 관리되지 않는 리소스를 해제하는 코드가 포함되어 있는 경우에만 종료자를 재정의합니다.
        ~LocalDBManager()
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

}
