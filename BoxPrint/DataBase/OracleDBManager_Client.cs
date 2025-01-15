using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using PLCProtocol.DataClass;
using BoxPrint.Alarm;
using BoxPrint.DataList;
using BoxPrint.DataList.MCS;
using BoxPrint.GUI.ETC;
using BoxPrint.Log;
using BoxPrint.Modules.Conveyor;
using BoxPrint.Modules.CVLine;
using BoxPrint.Modules.Shelf;        //220401 HHJ SCS 개선     //- Xml, DB 혼용에 따른 초기 생성 불가능 현상 조치
using BoxPrint.Modules.User;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using TranslationByMarkupExtension;

namespace BoxPrint.DataBase
{
    public class OracleDBManager_Client : OracleDBManager
    {
        private int PollingPeriod = 500; //0.2sec

        private bool _IsConnect;
        public new bool IsConnect
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

        private bool _DBConnectionChanging;
        public new bool DBConnectionChanging
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

        private Thread cth;
        private bool threadExit = true;
        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="dbopenstate"></param>
        public OracleDBManager_Client(out bool dbopenstate) : base(out dbopenstate)
        {
            if (GlobalData.Current.ServerClientType == eServerClientType.Client)
            {
                IsConnect = dbopenstate;
                cth = new Thread(new ThreadStart(GatherServerReqCommand));
                cth.IsBackground = true;
                cth.SetApartmentState(ApartmentState.STA);
                cth.Name = "OracleManagerClientDebug";
                cth.Start();
                //MRE_DBThreadEvent.Reset();
                //MRE_DBThreadEvent.Set();
            }
        }

        public override void CreateShelfInfo(ShelfItemList shelfinfo) { }

        public override void DbSetShelfInfo(ShelfItem shelfitem, bool bUpdate = true) { }

        public override void DbSetProcedureShelfInfo(ShelfItem shelfitem, bool shelfinit = false) { }

        public override bool DbSetJobInfo(McsJob jobitem, bool del) { return false; }

        public override bool DbSetProcedureJobInfo(McsJob jobitem, bool del) { return false; }

        //public override bool DbSetProcedureAlarmInfo(AlarmData alarm, bool del, string target) { return false; }

        public override void DbSetCarrierInfo(CarrierItem item, bool del) { }

        public override void DbSetProcedureCarrierInfo(CarrierItem item, bool del) { }

        //public override bool DbSetProcedureUserInfo(User user, bool del = false) { return false; }

        public override bool DBTableAllDelete(string tablename) { return false; }

        public override bool DbSetProcedurePIOInfo(string moduleID, eAreaType areaType, string piodata) { return false; }

        public override bool DbSetProcedureEQPInfo(object mcs, object scs, object plc, object system, bool init = false) { return false; }

        //public override List<ClientReqList> DbGetProcedureClientReq(eServerClientType requesttype, string clientid = "")
        //{
        //    List<ClientReqList> ReqList = new List<ClientReqList>();
        //    DataSet dataSet = new DataSet();

        //    try
        //    {
        //        using (OracleConnection conn = new OracleConnection(OracleDBPath))
        //        {
        //            try
        //            {
        //                conn.Open();

        //                if (IsConnect == false)
        //                    IsConnect = true;

        //                using (OracleCommand sqlcmd = new OracleCommand("UFN_STC_CLIENT_ORDER_GET", conn))
        //                {
        //                    try
        //                    {
        //                        sqlcmd.CommandType = CommandType.StoredProcedure;

        //                        OracleParameter output = sqlcmd.Parameters.Add("TMP_DATA", OracleDbType.RefCursor);
        //                        output.Direction = ParameterDirection.ReturnValue;

        //                        OracleParameter input1 = sqlcmd.Parameters.Add("P_SCS_CD", OracleDbType.NVarchar2);
        //                        input1.Direction = ParameterDirection.Input;
        //                        input1.Value = GlobalData.Current.EQPID;

        //                        OracleParameter input2 = sqlcmd.Parameters.Add("P_REQUESTER_GB", OracleDbType.Char);
        //                        input2.Direction = ParameterDirection.Input;
        //                        input2.Value = requesttype == eServerClientType.Server ? '0' : '1';

        //                        OracleParameter input3 = sqlcmd.Parameters.Add("P_CLIENT_CD", OracleDbType.NVarchar2);
        //                        input3.Direction = ParameterDirection.Input;
        //                        input3.Value = string.IsNullOrEmpty(clientid) ? "" : clientid;

        //                        sqlcmd.ExecuteNonQuery();

        //                        using (OracleDataAdapter oradata = new OracleDataAdapter(sqlcmd))
        //                        {
        //                            try
        //                            {
        //                                oradata.Fill(dataSet);
        //                            }
        //                            finally
        //                            {
        //                                if (oradata != null)
        //                                    oradata.Dispose();
        //                            }
        //                        }
        //                    }
        //                    finally
        //                    {
        //                        if (sqlcmd != null)
        //                            sqlcmd.Dispose();
        //                    }
        //                }
        //            }
        //            finally
        //            {
        //                conn.Close();

        //                if (conn != null)
        //                    conn.Dispose();
        //            }
        //        }

        //        int table = dataSet.Tables.Count;
        //        for (int i = 0; i < table; i++)// set the table value in list one by one
        //        {
        //            foreach (DataRow dr in dataSet.Tables[i].Rows)
        //            {
        //                ReqList.Add(new ClientReqList
        //                {
        //                    CMDType = dr["CMD_GB"].ToString(),
        //                    Target = dr["TARGET_CD"].ToString(),
        //                    TargetID = dr["TARGET_ID"].ToString(),
        //                    TargetValue = dr["TARGET_VALUE"].ToString(),
        //                    ReqTime = Convert.ToDateTime(dr["CREATE_DTTM"]).ToString("yyyy-MM-dd HH:mm:ss.fff"),
        //                    Requester = Convert.ToInt32(dr["REQUESTER_GB"].ToString()) == 0 ? eServerClientType.Server : eServerClientType.Client,
        //                    ClientID = dr["CLIENT_CD"].ToString(),
        //                });
        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());

        //        //이중화 접속을 위해 필요함.
        //        string excode = ex.HResult.ToString("X");
        //        if (excode == "80004005")
        //            DbConnectDuplexing(OracleDBPath);

        //        return ReqList;
        //    }

        //    return ReqList;
        //}
        public override void MapChangeForExitThread()
        {
            threadExit = false;
        }

        private void GatherServerReqCommand()
        {
            List<ClientReqList> ReqList = new List<ClientReqList>();

            GlobalData.Current.MRE_MapViewChangeEvent.WaitOne();

            while (threadExit)
            {
                //if (GlobalData.Current.LayoutLoadComp == false)
                //    continue;

                //if (GlobalData.Current.MapViewStart)
                //{
                //    cth.Join();
                //}
                try
                {
                    Thread.Sleep(PollingPeriod);

                    //MRE_DBThreadEvent.WaitOne();

                    DbGetProcedureConnectClientInfo();

                    ReqList = DbGetProcedureClientReq(eServerClientType.Server);

                    for (int i = 0; i < ReqList.Count; i++)
                    {
                        if (DBConnectionChanging)
                            break;

                        switch (ReqList[i].Target.ToUpper())
                        {
                            case "ALARM":
                                switch (ReqList[i].CMDType.ToUpper())
                                {
                                    case "SET":
                                        GlobalData.Current.Alarm_Manager.AlarmOccur(ReqList[i].TargetValue, ReqList[i].TargetID, ReqList[i].JobID);
                                        break;

                                    case "CLEAR":
                                        if (ReqList[i].ClientID == GlobalData.Current.ClientPCName)
                                        {
                                            GlobalData.Current.Alarm_Manager.RequestAlarmClear(ReqList[i].TargetID, ReqList[i].TargetValue, ReqList[i].JobID, true);
                                        }
                                        break;
                                }
                                break;

                            case "CV":
                                if (GlobalData.Current.PortManager.CheckCVModuleName(ReqList[i].TargetID))
                                {
                                    CV_BaseModule selcv = GlobalData.Current.PortManager.GetCVModule(ReqList[i].TargetID);
                                    //CVLineModule SelectedCVLine = selcv.ParentModule as CVLineModule;

                                    switch (ReqList[i].CMDType.ToUpper())
                                    {
                                        case "KEYIN":
                                            //Dispatcher.CurrentDispatcher.InvokeAsync(delegate
                                            //{
                                            //    CarrierInstall ci = new CarrierInstall(selcv.ControlName);
                                            //    CarrierItem carrier = ci.ResultCarrierItem();

                                            //}, DispatcherPriority.Normal);

                                            Dispatcher.CurrentDispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                                            {
                                                CarrierKeyIn ci = new CarrierKeyIn(selcv);
                                                CarrierItem carrier = ci.ResultCarrierItem();
                                                MessageBoxPopupView msgbox = null;
                                                string msg = string.Empty;
                                                CustomMessageBoxResult mBoxResult = null;

                                                if (carrier != null)
                                                {
                                                    msg = string.Format(
                                                        TranslationManager.Instance.Translate("Conveyor").ToString() + "[{0}]\n" +
                                                        TranslationManager.Instance.Translate("CarrierID").ToString() + "[{1}]\n" +
                                                        TranslationManager.Instance.Translate("Install").ToString() + "?",
                                                        selcv.ControlName, carrier.CarrierID);
                                                    msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);

                                                    mBoxResult = msgbox.ShowResult();

                                                    if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                                                    {
                                                        ClientReqList reqBuffer = new ClientReqList
                                                        {
                                                            EQPID = GlobalData.Current.EQPID,
                                                            CMDType = "KEYIN",
                                                            Target = "CV",
                                                            TargetID = selcv.ControlName,
                                                            TargetValue = JsonConvert.SerializeObject(carrier),
                                                            ReqTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                                                            Requester = eServerClientType.Client,
                                                        };

                                                        GlobalData.Current.DBManager.DbSetProcedureClientReq(reqBuffer.EQPID, reqBuffer.CMDType, reqBuffer.Target, reqBuffer.TargetID, reqBuffer.TargetValue, reqBuffer.ReqTime, reqBuffer.Requester);

                                                        msg = string.Format(
                                                            TranslationManager.Instance.Translate("Conveyor").ToString() + "[{0}]\n" +
                                                            TranslationManager.Instance.Translate("CarrierID").ToString() + "[{1}]\n" +
                                                            TranslationManager.Instance.Translate("Install").ToString() + " " +
                                                            TranslationManager.Instance.Translate("Complete").ToString(),
                                                            selcv.ControlName, carrier.CarrierID);
                                                        MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                                                    }
                                                }
                                            }));
                                            break;
                                    }
                                }
                                break;

                            case "TERMINALMSG":
                                if (!string.IsNullOrEmpty(ReqList[i].CMDType) && DateTime.TryParse(ReqList[i].CMDType, out DateTime cmddt))
                                {
                                    //DateTime dateTime = DateTime.ParseExact(ReqList[i].CMDType, "yyyy-MM-dd HH:mm:ss.fff", null);
                                    Enum.TryParse(ReqList[i].TargetID, out eHostMessageDirection direction);
                                    string[] buffeSplitr = null;
                                    buffeSplitr = ReqList[i].TargetValue.Split('/');

                                    GlobalData.Current.TerminalMessageChangedOccur(cmddt, direction, buffeSplitr[2], true);
                                }
                                break;
                        }

                        //230801 조숭진 서버1대에 다수의 클라이언트가 접속해있을경우를 위해 alarm set은 지우지않는다.
                        if (ReqList[i].CMDType.ToUpper() != "SET")
                        {
                            if (ReqList[i].ClientID == GlobalData.Current.ClientPCName)
                            {
                                DbSetProcedureClientReq(GlobalData.Current.EQPID, ReqList[i].CMDType, ReqList[i].Target, ReqList[i].TargetID,
                                    ReqList[i].TargetValue, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), ReqList[i].Requester, true, ReqList[i].JobID, ReqList[i].ClientID);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                }
            }
            //cth.Join(); //241121 RGJ 자기가 돌던 쓰레드 조인 기다리는것 무의미 하므로 삭제.
        }

        //public override void DbConnectDuplexing(string ConnInfo)
        //{
        //    IsConnect = false;

        //    if (ConnInfo != OracleFirstDBPath)
        //        OracleDBPath = OracleFirstDBPath;
        //    else if (ConnInfo != OracleSecondDBPath)
        //        OracleDBPath = OracleSecondDBPath;

        //    //if (IsTimeOut(DBConnectionDuplexingTime, 60))
        //    //{
        //    //    DBConnectionDuplexingCount = 0;
        //    //    DBConnectionDuplexingTime = DateTime.Now;
        //    //}

        //    //DBConnectionDuplexingCount++;
        //    //if (DBConnectionDuplexingCount >= 2)
        //    //return false;

        //    //return true;
        //}

        public override bool DBConnectionInfoChange(EQPInfo info)
        {
            OracleConnection conn = null;
            //bool bConnOK = true;

            if (DBConnectionChanging == false)
                DBConnectionChanging = true;

            //MRE_DBThreadEvent.Reset();
            string prevDBFirstConnIP = GlobalData.Current.DBSection.DBFirstConnIP;
            string prevDBFirstConnPort = GlobalData.Current.DBSection.DBFirstConnPort;
            string prevDBFirstConnServiceName = GlobalData.Current.DBSection.DBFirstConnServiceName;
            string prevDBSecondConnIP = GlobalData.Current.DBSection.DBSecondConnIP;
            string prevDBSecondConnPort = GlobalData.Current.DBSection.DBSecondConnPort;
            string prevDBSecondConnServiceName = GlobalData.Current.DBSection.DBSecondConnServiceName;
            string prevDBAccountName = GlobalData.Current.DBSection.DBAccountName;
            string prevDBPassword = GlobalData.Current.DBSection.DBPassword;
            string prevOracleDBPath = OracleDBPath;

            lock (DBConnectionChangeSyncObject)
            {
                GlobalData.Current.DBSection.DBFirstConnIP = info.DBFirstIP;
                GlobalData.Current.DBSection.DBFirstConnPort = info.DBFirstPort;
                GlobalData.Current.DBSection.DBFirstConnServiceName = info.DBFirstServiceName;
                GlobalData.Current.DBSection.DBSecondConnIP = info.DBSecondIP;
                GlobalData.Current.DBSection.DBSecondConnPort = info.DBSecondPort;
                GlobalData.Current.DBSection.DBSecondConnServiceName = info.DBSecondServiceName;
                GlobalData.Current.DBSection.DBAccountName = info.DbAccount;
                GlobalData.Current.DBSection.DBPassword = info.DbPassword;
            }

            try
            {
                conn = new OracleConnection(OracleFirstDBPath);
                conn.Open();

                ConnectPathSet(OracleFirstDBPath);
            }
            catch (Exception)
            {
                try
                {
                    if (conn != null)
                        conn.Dispose();

                    conn = new OracleConnection(OracleSecondDBPath);
                    conn.Open();

                    ConnectPathSet(OracleSecondDBPath);
                }
                catch (Exception)
                {
                    //bConnOK = false;
                    //이전 접속정보로 되돌린다.
                    lock (DBConnectionChangeSyncObject)
                    {
                        GlobalData.Current.DBSection.DBFirstConnIP = prevDBFirstConnIP;
                        GlobalData.Current.DBSection.DBFirstConnPort = prevDBFirstConnPort;
                        GlobalData.Current.DBSection.DBFirstConnServiceName = prevDBFirstConnServiceName;
                        GlobalData.Current.DBSection.DBSecondConnIP = prevDBSecondConnIP;
                        GlobalData.Current.DBSection.DBSecondConnPort = prevDBSecondConnPort;
                        GlobalData.Current.DBSection.DBSecondConnServiceName = prevDBSecondConnServiceName;
                        GlobalData.Current.DBSection.DBAccountName = prevDBAccountName;
                        GlobalData.Current.DBSection.DBPassword = prevDBPassword;
                    }
                    ConnectPathSet(prevOracleDBPath);
                    DBConnectionChanging = false;
                    return false;
                }
            }
            finally
            {
                conn.Close();

                if (conn != null)
                    conn?.Dispose();
            }

            DBConnectionChanging = false;

            //if (bConnOK)
            //{
            //    MRE_DBThreadEvent.Set();
            //}

            return true;
        }
    }
}
