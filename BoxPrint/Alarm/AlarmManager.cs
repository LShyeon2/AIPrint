using BoxPrint.Database;
using BoxPrint.DataList;
using BoxPrint.DataList.MCS;
using BoxPrint.Log;
using BoxPrint.Modules.Conveyor;
using BoxPrint.Modules.RM;
using BoxPrint.Modules.Shelf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml.Serialization;


namespace BoxPrint.Alarm
{
    public class AlarmManager
    {
        private int LastAlarmID = 0;
        private readonly string UnDefineAlarmID = "0";
        //public static object ActiveListLock = new object(); //Active List를 조작할때는 Lock 을 건다.
        public delegate void ActiveAlarmListChanged(AlarmData alarm, bool AddList);
        public event ActiveAlarmListChanged OnActiveAlarmListChanged; //내부 유닛 상태가 변했으므로 GUI에게 업데이트 이벤트를 날린다.

        public delegate void HeavyAlarmOccurred();
        public event HeavyAlarmOccurred OnHeavyAlarmOccurred; //중알람 발생 이벤트

        //private bool AllPortOutServiceReport = false; //크레인 전체 다운시 포트 보고 중복을 막기 위한 임시 변수
        private LocalDBManager LocalDatabase;
        /// <summary>
        /// 모든 알람 리스트를 저장
        /// </summary>
        private ObservableList<AlarmData> AllAlarmList;
        public string DataBaseFileName = @"\LOG\SCS_Playback.db3";
        /// <summary>
        /// 현재 발생한 알람 리스트
        /// </summary>
        private ObservableList<AlarmData> mActiveAlarmList;
        public ObservableList<AlarmData> ActiveAlarmList
        {
            get
            {
                return mActiveAlarmList;
            }
        }


        /// <summary>
        /// 외부에서 List 열거할때 컬렉션 예외를 막기위해 열거전용 리스트를 새로 만든다.
        /// </summary>
        /// <returns></returns>
        public List<AlarmData> GetActiveList()
        {
            //lock(ActiveListLock)
            //{
            //    List<AlarmData> TempList = mActiveAlarmList.ToList();
            //    return TempList;
            //}
            List<AlarmData> TempList = mActiveAlarmList.ToList();
            return TempList;
        }

        private int HeavyAlarmCounter = 0; //where문  조회를 줄이기 위해 변수로 빼놓음
        private int TotalAlarmCounter = 0;

        public AlarmManager(string listFilePath)
        {
            string LogPath = LogManager.GetLogRootPath();

            if(GlobalData.Current.ServerClientType == eServerClientType.Server)
            {
                LocalDatabase = new LocalDBManager(LogPath + DataBaseFileName);
                LastAlarmID = LocalDatabase.GetAlarmLogCount();
            }

            AllAlarmList = GlobalData.Current.DBManager.DbGetProcedureAlarmInfo();
            
            if (AllAlarmList.Count == 0)     //계정별로 테이블 생성되기때문에 없으면 새로 생성한다.
            {
                AllAlarmList = Deserialize(listFilePath);

                foreach (var data in AllAlarmList)
                {
                    GlobalData.Current.DBManager.DbSetProcedureAlarmInfo(data, false, "ID");
                }
            }

            foreach (var Adata in AllAlarmList)
            {
                if (string.IsNullOrEmpty(Adata.RecoveryOption))
                {
                    Adata.RecoveryOption = string.Empty;
                }

            }
            mActiveAlarmList = new ObservableList<AlarmData>();


            LogManager.WriteConsoleLog(eLogLevel.Info, "Alarm Manager has been created.");
        }

        //SuHwan_20230317 : 알람이 있는지 만 판단
        public bool CheckAlarmExist()
        {
            //lock (ActiveListLock)
            //{
            //    return (ActiveAlarmList.Count > 0) ? true : false; 
            //}
            return (ActiveAlarmList.Count > 0) ? true : false; //단순 조회용이라 락 잡을 필요 없음
        }

        public bool CheckAlarmExist(string alarmCode)
        {
            AlarmData Alarm;
            Alarm = ActiveAlarmList.Where(a => a.AlarmID == alarmCode).FirstOrDefault();
            return Alarm != null;
        }
        public int GetActiveHeavyAlarmCount()
        {
            return HeavyAlarmCounter;
        }
        public int GetActiveAlarmCount()
        {
            return TotalAlarmCounter;
        }
        public void AlarmOccur(string AlarmID, string Occurred_ModuleName, string RelateJobID = "")
        {
            AlarmData Alarm = null;
            if (AllAlarmList.Where(a => a.AlarmID == AlarmID).Count() != 0)
            {
                Alarm = (AlarmData)(AllAlarmList.Where(a => a.AlarmID == AlarmID).FirstOrDefault()).Clone();
            }

            // AlarmManager 없는 Alarm Call 하는 예외 처리.
            if (Alarm == null) //없는 알람 이면 미정의 알람을 대신 가져온다.
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "Undefine Alarm Occurred!  Module: {0}  Name: {1}", Occurred_ModuleName, AlarmID);

                //230919 조숭진 클론추가... 정의되지 않는 알람이 발생하면 ActiveAlarmList에 있는 기존 modulename을 신규발생한 modulename으로 엎어버린다.
                //그래서 클론해줘야 등록되지 않은 다른 module알람도 클리어 할수 있다.
                //230908 HHJ AlarmID가 등록되어 있지 않은 Alarm이라면 0번 Undefine 알람을 List에서 가져온다.
                //이 경우 S5F1조건에서 현재 발생한 알람이 ActiveAlarmList에 있는 알람과 상이하기에 S5F1 보고가 누락됨.
                //Log 남겨주고 AlarmID를 Undefine Alarm ID로 업데이트 해준다
                AlarmID = UnDefineAlarmID;
                //Alarm = AllAlarmList.FirstOrDefault(a => a.AlarmID == UnDefineAlarmID);
                Alarm = (AlarmData)(AllAlarmList.Where(a => a.AlarmID == UnDefineAlarmID).FirstOrDefault()).Clone();
            }
            if (Alarm.AlarmID == "0")
            {
                ;
            }
            if (Alarm != null)
            {
                if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                {
                    Alarm.ModuleName = Occurred_ModuleName;

                    if (ActiveAlarmList.Where(a => a.ModuleName == Occurred_ModuleName).Count() == 0)
                    {
                        Alarm.OccurDateTime = DateTime.Now;

                        var AlarmModule = GlobalData.Current.GetGlobalCarrierStoreAbleObject(Occurred_ModuleName);

                        if (AlarmModule is CV_BaseModule)
                        {
                            CV_BaseModule CVModule = (CV_BaseModule)GlobalData.Current.GetGlobalCarrierStoreAbleObject(Occurred_ModuleName);
                            if (CVModule != null)
                            {
                                CVModule.ConveyorUIState = eConveyorUIState.Alarm;
                            }
                        }

                        DispatcherService.Invoke((System.Action)(() =>
                        {
                            ActiveAlarmList.Add(Alarm);
                        }));

                        OnActiveAlarmListChanged?.Invoke(Alarm, true);
                    }
                }
                else
                {
                    Alarm.ModuleName = Occurred_ModuleName;
                    if (!Alarm.IsLightAlarm) //중알람만 Alarm 으로 상태 전환.
                    {
                        OnHeavyAlarmOccurred?.Invoke();
                    }

                    var AlarmObject = GlobalData.Current.GetGlobalCarrierStoreAbleObject(Occurred_ModuleName); //230525 RGJ 알람 데이터 캐리어 ID 항목 추가.\
                    if (AlarmObject != null)
                    {
                        Alarm.CarrierID = AlarmObject.GetCarrierID();
                    }

                    //Active 알람 리스트에 해당 모듈이 없을때만 넣는다. 각 유닛당 하나의 알람만 유지
                    //if (ActiveAlarmList.Where(a => a.AlarmID == AlarmID && a.ModuleName == Occurred_ModuleName).Count() == 0)
                    if (ActiveAlarmList.Where(a => a.ModuleName == Occurred_ModuleName).Count() == 0)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Alarm Occurred! Module:{0} AlarmID : {1} AlarmName:{2}  RelateJob :{3}", Occurred_ModuleName, Alarm.AlarmID, Alarm.AlarmName, RelateJobID);
                        Alarm.OccurDateTime = DateTime.Now;

                        DispatcherService.Invoke((Action)(() =>
                        {
                            ActiveAlarmList.Add(Alarm);
                            TotalAlarmCounter = ActiveAlarmList.Count;
                            HeavyAlarmCounter = ActiveAlarmList.Where(r => !r.IsLightAlarm).Count();
                        }));


                        OnActiveAlarmListChanged?.Invoke(Alarm, true);
                        Alarm.LogID = LastAlarmID; //DB 식별자 추가.
                        LocalDatabase.InsertAlarmLog(Alarm, false);
                        AlarmLogToDB(Alarm);        //221027 조숭진
                        LastAlarmID++;


                        //2024.06.18 lim, Alarm 보고 추가
                        //string CarrierID = "";
                        string CarrierLoc = "";
                        //상위 MCS 보고 //SuHwan_20220526 : 시나리오 수정
                        if (ActiveAlarmList.Where(a => a.AlarmID == AlarmID && a.ModuleName == Occurred_ModuleName).Count() == 1)
                        {
                            //2024.07.06 lim, Alarm 보고 S5F1 -> S5F101
                            //GlobalData.Current.HSMS.SendMessageAsync("S5F1", new Dictionary<string, object>() { { "MODULEID", Alarm.ModuleName }, { "ALARM", Alarm }, { "SETALARM", true } });
                            if (Alarm.AlarmID == GlobalData.SOURCE_EMPTY_ALARM_CODE) //더블 스토리지는 MCS Abort안함.
                            {
                                //241125 RGJ 공출고는 화물이 쉘프에 있기에 AlarmObject.GetCarrierID 에서 검색 안됨 수동으로 추가함.
                                RM_TPLC RM = GlobalData.Current.mRMManager[Alarm.ModuleName] as RM_TPLC;
                                var CraneCommand = RM?.GetCurrentCmd();
                                Alarm.CarrierID = CraneCommand != null ? CraneCommand.TargetCarrierID : "";
                                Alarm.RecoveryOption = "ABORT";
                            }
                            //2024.08.28 lim, AssignRMName 은 크레인만 확인 가능 Location 이나 CarrierID로 찾아야한다.
                            //LOC 은 지워지거나 작업자가 강제로 이동 시키면 사라질 수 있다. Carrier id 검색, 밀어내기 작업은 ID가 없기 때문에 ID가 있는 경우만
                            McsJob RelateJob = GlobalData.Current.McdList.Where(j => !string.IsNullOrEmpty(j.CarrierID) && j.CarrierID == Alarm.CarrierID).FirstOrDefault();
                            if (RelateJob != null)
                            {
                                RelateJobID = RelateJob.CommandID;
                                CarrierLoc = RelateJob.CarrierLoc;
                            }
                            GlobalData.Current.HSMS.SendMessageAsync("S5F101", new Dictionary<string, object>() { { "MODULEID", Alarm.ModuleName }, { "ALARM", Alarm }, { "SETALARM", true } , { "JOBID", RelateJobID }, {"CARRIERLOC", CarrierLoc} });
                            Thread.Sleep(100);
                            GlobalData.Current.HSMS.SendS6F11(102, "ALARM", Alarm, "JOBID", RelateJobID);

                        }




                        var AlarmModule = GlobalData.Current.GetGlobalCarrierStoreAbleObject(Occurred_ModuleName);

                        if (Alarm.IsLightAlarm == false) //중알람 일때만
                        {
                            #region 중알람 발생시 서비스 상태 보고
                            if (AlarmModule is RMModuleBase)  //Crane
                            {

                                RMModuleBase AlarmRM = (AlarmModule as RMModuleBase);
                                if (AlarmRM.PLC_ErrorCode == 0) //에러 코드 PLC에 없는 상태 즉 SCS 자체 알람일경우 PLC에 알람 코드를 써준다.
                                {
                                    AlarmRM.PC_AlarmCode = Alarm.iAlarmID;
                                }
                                //Crane State Changed 703
                                GlobalData.Current.HSMS.SendS6F11(703, "CRANE", AlarmModule);
                                GlobalData.Current.PortManager.CraneOutOfServiceAction(AlarmRM.IsFirstRM); //포트매니저에서 직접 처리

                            }

                            else if (AlarmModule is CV_BaseModule)   //Port
                            {
                                CV_BaseModule AlarmCV = (AlarmModule as CV_BaseModule);
                                if (AlarmCV.PLC_ErrorCode == 0) //에러 코드 PLC에 없는 상태 즉 SCS 자체 알람일경우 PLC에 알람 코드를 써준다.
                                {
                                    AlarmCV.PC_ErrorCode = Alarm.iAlarmID;
                                }
                                //GlobalData.Current.HSMS.SendS6F11(402, "PORT", AlarmModule); //PortOutService 402
                                AlarmCV?.RequestOutserviceReport();
                            }
                            #endregion
                        }
                        else //경알람일경우에도 알람 코드 해당 모듈에 써준다.
                        {
                            if (AlarmModule is RMModuleBase)  //Crane
                            {
                                RMModuleBase AlarmRM = (AlarmModule as RMModuleBase);
                                if (AlarmRM.PLC_ErrorCode == 0) //에러 코드 PLC에 없는 상태 즉 SCS 자체 알람일경우 PLC에 알람 코드를 써준다.
                                {
                                    AlarmRM.PC_AlarmCode = Alarm.iAlarmID;
                                }
                            }
                            else if (AlarmModule is CV_BaseModule)   //Port
                            {
                                CV_BaseModule AlarmCV = (AlarmModule as CV_BaseModule);
                                if (AlarmCV.PLC_ErrorCode == 0) //에러 코드 PLC에 없는 상태 즉 SCS 자체 알람일경우 PLC에 알람 코드를 써준다.
                                {
                                    AlarmCV.PC_ErrorCode = Alarm.iAlarmID;
                                }
                            }
                        }

                        //220223 조숭진 알람set을 클라이언트로 보내기
                        GlobalData.Current.DBManager.DbSetProcedureClientReq(GlobalData.Current.EQPID, "SET", "ALARM", Occurred_ModuleName, AlarmID, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), eServerClientType.Server, false, RelateJobID);
                    }
                }
            }
        }

        /// <summary>
        /// // AlarmManager 이름으로 Alarm Call 기능 추가.
        /// </summary>
        /// <param name="alarmName"></param>
        public void AlarmOccurbyName(string alarmName, string Occurred_ModuleName)
        {
            AlarmData Alarm = AllAlarmList.Where(a => a.AlarmName == alarmName).FirstOrDefault();
            if (Alarm == null)
            {
                AlarmOccur(UnDefineAlarmID, Occurred_ModuleName);
            }
            else
            {
                AlarmOccur(Alarm.AlarmID, Occurred_ModuleName);
            }
        }

        public void AlarmClear(AlarmData ClearAlarm, McsJob RelateJop = null, bool ServerReq = false)
        {
            List<ClientReqList> ReqList = new List<ClientReqList>();

            //bool bokclear = false;

            if (ClearAlarm != null)
            {
                if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                {
                    DispatcherService.Invoke((Action)(() =>
                    {
                        ActiveAlarmList.Remove(ClearAlarm);
                    }));

                    OnActiveAlarmListChanged?.Invoke(ClearAlarm, false);

                    var AlarmModule = GlobalData.Current.GetGlobalCarrierStoreAbleObject(ClearAlarm.ModuleName);

                    if (AlarmModule is CV_BaseModule)
                    {
                        CV_BaseModule CVModule = (CV_BaseModule)GlobalData.Current.GetGlobalCarrierStoreAbleObject(ClearAlarm.ModuleName);
                        if (CVModule != null)
                        {
                            CVModule.ConveyorUIState = eConveyorUIState.AlarmClear;
                        }
                    }

                    if (ServerReq == false)
                    {
                        //230801 클라이언트에서 알람클리어할때는 서버 알람셋을 직접 지운다.
                        ReqList = GlobalData.Current.DBManager.DbGetProcedureClientReq(eServerClientType.Server);
                        if (ReqList.Where(L => L.EQPID == GlobalData.Current.EQPID && L.CMDType == "SET" && L.Target == "ALARM" &&
                            L.TargetID == ClearAlarm.ModuleName && L.TargetValue == ClearAlarm.AlarmID).Count() != 0)
                        {
                            GlobalData.Current.DBManager.DbSetProcedureClientReq(GlobalData.Current.EQPID, "SET", "ALARM", ClearAlarm.ModuleName,
                                ClearAlarm.AlarmID, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), eServerClientType.Server, true);
                        }

                        string RelateJobID = string.Empty;
                        if (RelateJop != null)
                        {
                            RelateJobID = RelateJop.CommandID;
                        }
                        for (int list = 0; list < GlobalData.Current.DBManager.ClientList.Count; list++)
                        {
                            if (GlobalData.Current.DBManager.ClientList[list].ClientPCName != GlobalData.Current.ClientPCName)
                            {
                                GlobalData.Current.DBManager.DbSetProcedureClientReq(GlobalData.Current.EQPID, "CLEAR", "ALARM", ClearAlarm.ModuleName,
                                    ClearAlarm.AlarmID, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), eServerClientType.Server, false, RelateJobID,
                                    GlobalData.Current.DBManager.ClientList[list].ClientPCName);
                            }
                        }

                        GlobalData.Current.DBManager.DbSetProcedureClientReq(GlobalData.Current.EQPID, "CLEAR", "ALARM", ClearAlarm.ModuleName, ClearAlarm.AlarmID, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), eServerClientType.Client);
                    }
                    //220223 조숭진 알람 clear을 서버로 보내기
                }
                else
                {
                    var AlarmModule = GlobalData.Current.GetGlobalCarrierStoreAbleObject(ClearAlarm.ModuleName);
                    if (AlarmModule is RMModuleBase rmModule)  //Crane
                    {

                        rmModule.RMReset_Request(); //에러 리셋 시퀀스 동작하도록 ON

                    }
                    else if (AlarmModule is CV_BaseModule)   //Port
                    {
                        (AlarmModule as CV_BaseModule).CV_ErrorResetRequest();//에러 리셋 시퀀스 동작하도록 ON
                    }

                    //2024.06.18 lim, Alarm 보고 추가
                    if (AlarmModule != null)
                    {
                        ClearAlarm.CarrierID = AlarmModule.GetCarrierID();
                    }

                    LogManager.WriteConsoleLog(eLogLevel.Error, string.Format("Alarm ID: {0} Name :{1} Clear started", ClearAlarm.AlarmID, ClearAlarm.AlarmName));

                    ClearAlarm.ClearDateTime = DateTime.Now; //AlarmManager 누락분 추가
                    DispatcherService.Invoke((System.Action)(() =>
                    {
                        ActiveAlarmList.Remove(ClearAlarm);
                        TotalAlarmCounter = ActiveAlarmList.Count;
                        HeavyAlarmCounter = ActiveAlarmList.Where(r => !r.IsLightAlarm).Count();
                    }));
                    LocalDatabase.InsertAlarmLog(ClearAlarm, true);
                    AlarmLogToDB(ClearAlarm);       //221027 조숭진
                    OnActiveAlarmListChanged?.Invoke(ClearAlarm, false);

                    //2024.06.18 lim, Alarm 보고 추가
                    string CarrierLoc = "";
                    //상위 MCS 보고 //SuHwan_20220526 : 시나리오 수정
                    //2024.08.14 lim, 동일 알람 다른 모듈에 발생시 알람 클리어 보고 누락됨.  조건 추가
                    if (ActiveAlarmList.Where(a => a.AlarmID == ClearAlarm.AlarmID && a.ModuleName == ClearAlarm.ModuleName).Count() == 0)
                    {
                        //2024.07.06 lim, Alarm 보고 S5F1 -> S5F101
                        //GlobalData.Current.HSMS.SendMessageAsync("S5F1", new Dictionary<string, object>() { { "MODULEID", ClearAlarm.ModuleName }, { "ALARM", ClearAlarm }, { "SETALARM", false } });

                        string RelateJobID = string.Empty;
                        if (ClearAlarm.AlarmID == GlobalData.SOURCE_EMPTY_ALARM_CODE)  //더블 스토리지는 MCS Abort안함.
                        {
                            ClearAlarm.RecoveryOption = "ABORT";
                        }
                        //2024.08.28 lim, AssignRMName 은 크레인만 확인 가능 Location 이나 CarrierID로 찾아야한다.
                        //LOC 은 지워지거나 작업자가 강제로 이동 시키면 사라질 수 있다. Carrier id 검색, 밀어내기 작업은 ID가 없기 때문에 ID가 있는 경우만
                        McsJob RelateJob = GlobalData.Current.McdList.Where(j => !string.IsNullOrEmpty(j.CarrierID) && j.CarrierID == ClearAlarm.CarrierID).FirstOrDefault();
                        if (RelateJob != null)
                        {
                            RelateJobID = RelateJob.CommandID;
                            CarrierLoc = RelateJob.CarrierLoc;
                        }
                        GlobalData.Current.HSMS.SendMessageAsync("S5F101", new Dictionary<string, object>() { { "MODULEID", ClearAlarm.ModuleName }, { "ALARM", ClearAlarm }, { "SETALARM", false }, { "JOBID", RelateJobID }, { "CARRIERLOC", CarrierLoc } });

                        GlobalData.Current.HSMS.SendS6F11(101, "ALARM", ClearAlarm, "JOBID", RelateJobID);
                    }



                    //크레인 알람을 해제할때 복구 옵션이 있다면 옵션 따라 처리
                    if (ClearAlarm.ModuleType == "CRANE")
                    {
                        //230926 RGJ Alarm 해제시 Result Code 해제 
                        McsJob job = GlobalData.Current.McdList.Where(J => ClearAlarm.ModuleName == J.AssignRMName).FirstOrDefault();
                        if (job != null && job.JobResult == eJobResultCode.OTHER_ERROR)
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, string.Format("Alarm ID: {0} Name :{1} Cleard and Job :{2} ReusltCode Reset", ClearAlarm.AlarmID, ClearAlarm.AlarmName, job.CommandID));

                            job.JobResult = eJobResultCode.SUCCESS;
                        }

                        switch (ClearAlarm.RecoveryOption) //옵션 사양따라 필요시 추가
                        {
                            case "ABORT":
                                //에러 상태로 대기중인 작업을 ABORT 처리한다.
                                McsJob TargetJob = GlobalData.Current.McdList.Where(J => J.TCStatus == eTCState.PAUSED && ClearAlarm.ModuleName == J.AssignRMName).FirstOrDefault();
                                TargetJob?.SetJobAbort(true);
                                //GlobalData.Current.McdList.DeleteMcsJob(TargetJob);
                                break;
                        }

                        if(ClearAlarm.AlarmName == "CRANE CARRIER SENSOR DATA MISMATCH") //241125 RGJ 해당 알람 클리어시 크레인 화물없으면 데이터 삭제
                        {
                            RM_TPLC ClearRM = GlobalData.Current.GetGlobalCarrierStoreAbleObject(ClearAlarm.ModuleName) as RM_TPLC;
                            //화물 감지 OFF 상태에서 해당 알람 클리어시 데이터가 남아 있다면 삭제한다.
                            if (ClearRM != null && !ClearRM.CheckCarrierExist() && !ClearRM.CarrierExistSensor && ClearRM.InSlotCarrier != null)
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "Alarm Clear => RM:{0} Carrier :{1}  Crane has carrier data  but not sensing. Reset Carrier Data", ClearRM.ModuleName, ClearRM.GetCarrierID());
                                ClearRM.ResetCarrierData();
                            }
                        }
                    }

                    if (ClearAlarm.IsLightAlarm == false) //중알람일경우 상태 변경보고
                    {
                        #region 중알람 해제시 서비스 상태 보고

                        if (AlarmModule is RMModuleBase rm)  //Crane
                        {
                            if (RelateJop != null) //연관 작업이 있을경우 Idle 보고 한다.
                            {
                                //Crane Idle 702
                                GlobalData.Current.HSMS.SendS6F11(702, "JOBDATA", RelateJop);
                            }
                            //Crane State Changed 703
                            GlobalData.Current.HSMS.SendS6F11(703, "CRANE", AlarmModule);
                            GlobalData.Current.PortManager.CraneInServiceAction(rm.IsFirstRM); //포트매니저에서 직접 처리

                        }
                        else if (AlarmModule is CV_BaseModule)   //Port
                        {
                            (AlarmModule as CV_BaseModule).CV_ErrorResetRequest();//에러 리셋 시퀀스 동작하도록 ON
                            Thread.Sleep(1000); //240704 RGJ 포트 알람 클리어시 리셋 시퀀스 동작할 시간 대기
                            (AlarmModule as CV_BaseModule).RequestInserviceReport();
                        }
                        #endregion
                    }
                    else
                    {
                        if (AlarmModule is CV_BaseModule)   //Port
                        {
                            (AlarmModule as CV_BaseModule).PC_ErrorCode = 0; //경알람인경우 에러코드 초기화
                        }
                    }

                    //230801 서버가 직접 클리어한다면 클라이언트 알람클리어해야하기때문에 클리어 명령보내야한다.
                    //230601 만일 클라이언트가 아닌 plc 혹은 서버가 직접 알람클리어를 했다면 클라이언트 order의 set명령은 지워버리고 clear 명령 남기지 않는다.
                    ReqList = GlobalData.Current.DBManager.DbGetProcedureClientReq(eServerClientType.Server);

                    if (ReqList.Where(L => L.EQPID == GlobalData.Current.EQPID && L.CMDType == "SET" && L.Target == "ALARM" &&
                        L.TargetID == ClearAlarm.ModuleName && L.TargetValue == ClearAlarm.AlarmID).Count() != 0)
                    {
                        GlobalData.Current.DBManager.DbSetProcedureClientReq(GlobalData.Current.EQPID, "SET", "ALARM", ClearAlarm.ModuleName,
                            ClearAlarm.AlarmID, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), eServerClientType.Server, true);

                        //230801 접속해있는 클라이언트가 없다면 clear 안보내도 된다.
                        string RelateJobID = string.Empty;
                        if (RelateJop != null)
                        {
                            RelateJobID = RelateJop.CommandID;
                        }
                        for (int list = 0; list < GlobalData.Current.DBManager.ClientList.Count; list++)
                        {
                            GlobalData.Current.DBManager.DbSetProcedureClientReq(GlobalData.Current.EQPID, "CLEAR", "ALARM", ClearAlarm.ModuleName,
                                ClearAlarm.AlarmID, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), eServerClientType.Server, false, RelateJobID,
                                GlobalData.Current.DBManager.ClientList[list].ClientPCName);
                        }
                    }
                    //230801 아래 구문은 왜 했는지 기억이 안나서 주석처리
                    //else
                    //{
                    //    GlobalData.Current.DBManager.DbSetProcedureClientReq(GlobalData.Current.EQPID, "CLEAR", "ALARM", ClearAlarm.ModuleName, ClearAlarm.AlarmID, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), eServerClientType.Server);
                    //}
                }
            }
        }

        //230223 조숭진 클라이언트에서 알람클리어 요청
        public void RequestAlarmClear(string ModuleName, string AlarmCode = "", string JobID = "", bool ServerReq = false)
        {
            //lock (ActiveListLock)
            {
                if (JobID == "" && AlarmCode == "")
                {
                    this.AlarmClear(ModuleName, null, ServerReq);
                }
                else if(AlarmCode != "")
                {
                    this.AlarmClear(ModuleName, AlarmCode, null, ServerReq);
                }
                else
                {
                    McsJob Job = GlobalData.Current.McdList.FirstOrDefault(j => j.CommandID == JobID);
                    this.AlarmClear(ModuleName, AlarmCode, Job, ServerReq);
                }
            }
        }

        public void AlarmClear(string ModuleName, string AlarmCode, McsJob RelateJop = null, bool ServerReq = false)
        {
            AlarmData AD = ActiveAlarmList.FirstOrDefault(a => a.ModuleName == ModuleName && a.AlarmID == AlarmCode);
            if (AD != null)
            {
                AlarmClear(AD, RelateJop, ServerReq);
            }
        }
        public void AlarmClear(string ModuleName, McsJob RelateJop = null, bool ServerReq = false)
        {
            AlarmData AD = ActiveAlarmList.FirstOrDefault(a => a.ModuleName == ModuleName);
            if (AD != null)
            {
                AlarmClear(AD, RelateJop, ServerReq);
            }
        }

        public void AlarmAllClear()
        {
            while (ActiveAlarmList.Count > 0)
            {
                AlarmData Alarm = ActiveAlarmList.FirstOrDefault();
                if (Alarm != null)
                {
                    Alarm.ClearDateTime = DateTime.Now;
                    DispatcherService.Invoke((System.Action)(() =>
                    {
                        ActiveAlarmList.Remove(Alarm);
                        TotalAlarmCounter = ActiveAlarmList.Count;
                        HeavyAlarmCounter = ActiveAlarmList.Where(r => !r.IsLightAlarm).Count();
                    }));
                    //Database.InsertAlarmLog(Alarm, true);
                    AlarmLogToDB(Alarm);
                    OnActiveAlarmListChanged?.Invoke(Alarm, false);
                }
            }
        }
        public bool CheckHeavyAlarmExist()
        {
            return HeavyAlarmCounter > 0;
        }
        //240226 RGJ Alarm Binding List 를 ObservableList 로 변환
        public ObservableList<AlarmData> getAllAlarmList()
        {
            return AllAlarmList;
        }

        public void RefreshAllAlarmList(eServerClientType prgtype, string buttontype = "", AlarmData alarmitem = null, AlarmData prevalarmitem = null)
        {
            //AllAlarmList = GlobalData.Current.DBManager.GetDBAlarmInfo();       //220708 조숭진 alarm db추가
            //AllAlarmList = GlobalData.Current.DBManager.DbGetProcedureAlarmInfo();
            if (prgtype == eServerClientType.Server)
            {
                AllAlarmList = GlobalData.Current.DBManager.DbGetProcedureAlarmInfo();
            }
            else
            { 
                switch (buttontype)
                {
                    case "Add":
                        AllAlarmList.Add(alarmitem);
                        break;

                    case "Modify":
                        int alarmnum = AllAlarmList.IndexOf(prevalarmitem);
                        if (!alarmnum.Equals(-1))
                        {
                            AllAlarmList[alarmnum] = alarmitem;
                        }
                        break;

                    case "Delete":
                        AllAlarmList.Remove(alarmitem);
                        break;
                }
            }

            //return true;
        }

        //20240118 RGJ Alarm Active List 삭제 기능 
        public void DropActiveAlarmList()
        {
            ActiveAlarmList.Clear();
        }

        //220916 조숭진 추가
        public async void AlarmXmlLoad()
        {
            AllAlarmList = Deserialize(GlobalData.Current.CurrentFilePaths(GlobalData.Current.FullPath) + GlobalData.Current.AlarmListPath);

            bool asyncsucess = await GlobalData.Current.DBManager.AlarmXmlToDBAsync(AllAlarmList);
        }

        public ObservableList<AlarmData> Deserialize(String fileName)
        {
            bool bSuccess = false;
            ObservableList<AlarmData> bindAlarmList = new ObservableList<AlarmData>();
            List<AlarmData> alarmList = null;
            try
            {
                XmlSerializer xmlSer = new XmlSerializer(typeof(List<AlarmData>));
                FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);

                alarmList = (List<AlarmData>)xmlSer.Deserialize(fs);
                fs.Close();
                foreach (var alarmItem in alarmList)
                {
                    bindAlarmList.Add(alarmItem);
                }
                bSuccess = true;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Fatal, "== AlarmDataList.Deserialize() Exception : {0}", ex.ToString());
                bSuccess = false;
            }
            return (bSuccess) ? bindAlarmList : null;
        }

        public static void SerializeObject(BindingList<AlarmData> list, string fileName)
        {
            var serializer = new XmlSerializer(typeof(BindingList<AlarmData>));
            using (var stream = File.OpenWrite(fileName))
            {
                serializer.Serialize(stream, list);
            }
        }
        public AlarmData GetAlarmByAlarmNum(string AlarmNum)
        {
            AlarmData alm = AllAlarmList.Where(a => a.AlarmID == AlarmNum).FirstOrDefault();

            return alm;
        }
        public DataTable GetAlarmHistory(DateTime Start, DateTime End)
        {
            return LocalDatabase.GetAlarmRangeLog(Start, End);
        }

        /// <summary>
        /// 해달 모듈에 중알람 있는지 체크
        /// </summary>
        /// <param name="ModuleID"></param>
        /// <returns></returns>
        public bool CheckModuleHeavyAlarmExist(string ModuleID)
        {
            if (ActiveAlarmList.Where(a => a.ModuleName == ModuleID && !a.IsLightAlarm).Count() > 0) //모듈명이 같고 중알람 체크
                return true;
            else
                return false;
        }

        /// <summary>
        /// 해달 모듈에 이중입고 알람 있는지 체크
        /// </summary>
        /// <param name="ModuleID"></param>
        /// <returns></returns>
        public bool CheckModuleDSAlarmExist(string ModuleID)
        {
            RM_TPLC DS_RM = GlobalData.Current.GetGlobalCarrierStoreAbleObject(ModuleID) as RM_TPLC;
            if (DS_RM == null)
            {
                return false;
            }

            if (ActiveAlarmList.Where(a => a.ModuleName == ModuleID && a.AlarmID == GlobalData.DOUBLE_STORAGE_ALARM_CODE).Count() > 0 //모듈명이 같고 중알람 체크
                || DS_RM.PLC_ErrorCode.ToString() == GlobalData.DOUBLE_STORAGE_ALARM_CODE) //이중입고 알람코드가 아직 올라와 있으면 있다고 간주
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// 해당 모듈에 알람 있는지 체크,중알람 경알람 모두 체크
        /// </summary>
        /// <param name="ModuleID"></param>
        /// <returns></returns>
        public bool CheckModuleAlarmExist(string ModuleID)
        {
            if (ActiveAlarmList.Where(a => a.ModuleName == ModuleID).Count() > 0) //모듈명이 같고 알람 체크
                return true;
            else
                return false;
        }
        /// <summary>
        /// 해당 모듈에 특정한 알람이 있는지 확인.
        /// </summary>
        /// <param name="ModuleID"></param>
        /// <param name="AlarmID"></param>
        /// <returns></returns>
        public bool CheckModuleAlarmExist(string ModuleID, string AlarmID)
        {
            if (ActiveAlarmList.Where(a => a.ModuleName == ModuleID && a.AlarmID == AlarmID).Count() > 0) //모듈명이 같고 중알람 체크
                return true;
            else
                return false;
        }
        public string GetModule_LastAlarmCode(string ModuleID)
        {
            var ModuleAlarmList = ActiveAlarmList.Where(a => a.ModuleName == ModuleID);
            if (ModuleAlarmList.Count() > 0)
            {
                return ModuleAlarmList.Last().AlarmID;
            }
            else
            {
                return "0";
            }
        }

        private void AlarmLogToDB(AlarmData alarm)
        {

            object[] newargs = new object[14];
            newargs[0] = "ALARM";
            newargs[1] = alarm.ModuleName; //UnitID
            newargs[2] = alarm.AlarmID;     //Error Code
            newargs[3] = alarm.AlarmName;   //ErrorName

            var AlarmObj = GlobalData.Current.GetGlobalCarrierStoreAbleObject(alarm.ModuleName);
            if (AlarmObj != null && AlarmObj is RMModuleBase rm)
            {
                newargs[4] = rm.RMPLC_ID; //CraneNumber
                newargs[5] = "SC"; //Eqp type
            }
            else if (AlarmObj != null && AlarmObj is CV_BaseModule cm)
            {
                newargs[4] = cm.TrackID;
                newargs[5] = "CV"; //Eqp type
            }
            else
            {
                newargs[5] = "Etc"; //Eqp type
            }
            //newargs[6] = alarm.OccurDateTime.ToString("yyyy/MM/dd HH:mm:ss"); //Col6 
            //newargs[7] = alarm.ClearDateTime.ToString("yyyy/MM/dd HH:mm:ss"); //Col7
            newargs[6] = alarm.OccurDateTime.ToString("yyyy-MM-dd HH:mm:ss").Replace("-", "/");
            newargs[7] = alarm.ClearDateTime.ToString("yyyy-MM-dd HH:mm:ss").Replace("-", "/");
            newargs[8] = alarm.IsLightAlarm.ToString();
            newargs[9] = alarm.CarrierID;
            newargs[10] = alarm.Description; 
            newargs[11] = alarm.Description_ENG;
            newargs[12] = alarm.Description_CHN;
            newargs[13] = alarm.Description_HUN;
            GlobalData.Current.DBManager.DbSetProcedureLogInfo(newargs);
        }
    }
    /// <summary>
    /// UI 쓰레드에 의해 점유중인 자원에 대해선 자원에 수정을 가할 때 UI 쓰레드의 Dispatcher에 작업을 위임해야 한다. 
    ///[출처]
    /// http://blog.naver.com/PostView.nhn?blogId=seokcrew&logNo=221309203938&parentCategoryNo=&categoryNo=32&viewDate=&isShowPopularPosts=false&from=postView
    /// </summary>
    //
    public static class DispatcherService
    {
        public static void Invoke(Action action)
        {
            Dispatcher dispatchObject = Application.Current != null ? Application.Current.Dispatcher : null;
            if (dispatchObject == null || dispatchObject.CheckAccess())
                action();
            else
                dispatchObject.Invoke(action);
        }
    }
}
