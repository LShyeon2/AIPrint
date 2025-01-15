using System;
using System.IO;
using System.Data;
using System.Data.SQLite;
using Stockerfirmware.Log;

namespace Stockerfirmware.Alarm.Database
{
    //Data 복구용 DB 추가.
    public class DataBaseManager : IDisposable
    {
        private readonly object DBLock = new object(); //데이터 베이스 접근 락

        public static DataBaseManager Current;
        private string DBPath
        {
            get
            {
                return "Data Source=" + DBFILE_Path;
            }
        }
        private string DBFILE_Path = "";

        SQLiteConnection conn = null;

        public DataBaseManager(string Path)
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
            lock (DBLock)
            {
                SQLiteCommand cmd = null;
                try
                {
                    string sql = string.Format("SELECT * FROM AlarmLogTable WHERE OccurDateTime BETWEEN '{0}' AND '{1}'", DateTimeSQLite(Start), DateTimeSQLite(End));
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
            lock (DBLock)
            {
                SQLiteCommand cmd = null;
                try
                {
                    string sql = string.Format("INSERT INTO AlarmLogTable (LogID,AlarmID,AlarmName,Module,Description,Solution,LightAlarm,OccurDateTime,ClearDateTime,AlarmClear) VALUES ({0},{1},'{2}','{3}','{4}','{5}',{6},'{7}','{8}',{9})",
                                            Alarm.LogID,
                                            Alarm.AlarmID,
                                            Alarm.AlarmName,
                                            Alarm.Module,
                                            Alarm.Description,
                                            Alarm.Solution,
                                            Alarm.IsLightAlarm ? 1 : 0,
                                            DateTimeSQLite(Alarm.OccurDateTime),
                                            DateTimeSQLite(Alarm.ClearDateTime),
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
            lock (DBLock)
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

        private static string DateTimeSQLite(DateTime datetime)
        {
            return datetime.ToString("yyyy-MM-dd HH:mm:ss.fff");
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

                conn?.Dispose();
                
                disposedValue = true;
            }
        }

        // TODO: 위의 Dispose(bool disposing)에 관리되지 않는 리소스를 해제하는 코드가 포함되어 있는 경우에만 종료자를 재정의합니다.
        ~DataBaseManager()
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
