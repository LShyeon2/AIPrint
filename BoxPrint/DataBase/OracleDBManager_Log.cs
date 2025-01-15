using Oracle.ManagedDataAccess.Client;
using BoxPrint.Log;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BoxPrint.DataBase
{
    public class OracleDBManager_Log : OracleDBManager
    {
        private DataTable LogList;

        public bool bLogSearchCancel = false;

        public OracleDBManager_Log(out bool dbopenstate, string ForLog = "") : base(out dbopenstate, ForLog)
        {
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

        public override DataTable DbGetProcedureLogListInfo(string LogName, DateTime start, DateTime end, string key1 = "", string key2 = "", string key3 = "")
        {
            //DataTable LogList = new DataTable();
            //DataSet dataSet = new DataSet();

            try
            {
                using (OracleConnection conn = new OracleConnection(GlobalData.Current.DBManager.OracleDBPath))
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
    }
}
