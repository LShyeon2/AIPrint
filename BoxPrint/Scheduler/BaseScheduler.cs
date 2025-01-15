using BoxPrint.DataList;
using BoxPrint.DataList.MCS;
using BoxPrint.Log;
using BoxPrint.Modules;      //220420 HHJ SCS 개선     //- ShelfTagHelper 추가
using BoxPrint.Modules.Conveyor;
using BoxPrint.Modules.CVLine;
using BoxPrint.Modules.RM;
using BoxPrint.Modules.Shelf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace BoxPrint.Scheduler
{
    public abstract class BaseScheduler
    {
        protected bool StartSignal;
        protected string SchedulerName = "BaseScheduler";
        protected readonly int CycleTime = 100;
        protected int WaitInNoCommandTimeOut = 60; //WaitIn 하였는데 해당시간 이상 명령이 없으면 스케쥴러가 대신 명령을 생성.
        protected RMModuleBase TargetRM;
        public readonly int CycleRandomCreateCount = 10;
        protected int RM_AddtionalMargin = 4; // 타크레인 뒤를 따라갈때 출발을 어느정도 떨어져 있을때 할지 설정값 [듀얼전용] 
        protected List<CVLineModule> ManualPortLineList = new List<CVLineModule>();

        protected bool NeedManualPortBothOperation
        {
            get
            {
                return ManualPortLineList.Count() > 0;
            }
        }

        protected eSC_OperationMode _CurrentSCOPMode = eSC_OperationMode.NormalMode;
        public eSC_OperationMode CurrentSCOPMode
        {
            get => _CurrentSCOPMode;
        }

        protected McsJobManager JobList
        {
            get
            {
                return GlobalData.Current.McdList;
            }
        }

        public bool CycleRandomJobRequest
        {
            get;
            private set;
        }
        public bool AllShelfUnloadJobRequest
        {
            get;
            private set;
        }
        public bool UsePortGet
        {
            get;
            protected set;
        }

        protected McsJob _RM1_OnProcessJob = null;
        protected McsJob _RM2_OnProcessJob = null;
        public McsJob RM1_OnProcessJob
        {
            get
            {
                return _RM1_OnProcessJob;
            }
            private set
            {
                _RM1_OnProcessJob = value;
            }
        }
        public McsJob RM2_OnProcessJob
        {
            get
            {
                return _RM2_OnProcessJob;
            }
            private set
            {
                _RM2_OnProcessJob = value;
            }
        }
        public bool IsRM1JobProcessing
        {
            get
            {
                if (_RM1_OnProcessJob == null)
                    return false;

                return _RM1_OnProcessJob != null && !_RM1_OnProcessJob.JobHoldingState;
            }
        }
        public bool IsRM2JobProcessing
        {
            get
            {
                if (_RM2_OnProcessJob == null)
                    return false;

                return _RM2_OnProcessJob != null && !_RM2_OnProcessJob.JobHoldingState;
            }
        }

        //220823 조숭진 config로 수정 s
        protected int _RM1_ExclusiveBay = 0;
        public int RM1_ExclusiveBay
        {
            get
            {
                return _RM1_ExclusiveBay;
            }
            private set
            {
                _RM1_ExclusiveBay = value;
            }
        }

        protected int _RM2_ExclusiveBay = 0;
        public int RM2_ExclusiveBay
        {
            get
            {
                return _RM2_ExclusiveBay;
            }
            private set
            {
                _RM2_ExclusiveBay = value;
            }
        }

        private int _MaxBay = 0;
        public int MaxBay
        {
            get
            {
                return _MaxBay;
            }
            protected set
            {
                _MaxBay = value;
            }
        }

        public int ShelfMaxBay
        {
            get
            {
                return ShelfManager.Instance.GetMaxBay();
            }
        }


        protected int _RM_RangeMargin = 0;
        public int RM_RangeMargin
        {
            get
            {
                return _RM_RangeMargin;
            }
            private set
            {
                _RM_RangeMargin = value;
            }
        }

        protected int _HandOverMinBay = 0;
        public int HandOverMinBay
        {
            get
            {
                return _HandOverMinBay;
            }
            protected set
            {
                _HandOverMinBay = value;
            }
        }
        protected int _HandOverMaxBay = 0;
        public int HandOverMaxBay
        {
            get
            {
                return _HandOverMaxBay;
            }
            protected set
            {
                _HandOverMaxBay = value;
            }
        }

        //220916 조숭진 db로 옮김.
        protected bool _UseEmptyRetriveAutoReset = false;
        public bool UseEmptyRetriveAutoReset
        {
            get
            {
                return _UseEmptyRetriveAutoReset;
            }
            private set
            {
                _UseEmptyRetriveAutoReset = value;
            }
        }
        //220823 조숭진 config로 수정 e

        protected string FireSetPath = string.Empty;    //241018 HoN 화재시나리오 운영 추가       4.2) 화재수조 적재 불가시 자동포트 검색 필요. (사이즈가 맞지않는 포트, 브릿지, 랙간이동 포트 제외)

        public BaseScheduler()
        {
            //241018 HoN 화재시나리오 운영 추가       4.2) 화재수조 적재 불가시 자동포트 검색 필요. (사이즈가 맞지않는 포트, 브릿지, 랙간이동 포트 제외)
            FireSetPath = string.Format(GlobalData.Current.CurrentFilePaths(GlobalData.Current.FullPath) + @"\Data\FireSetInfo.ini");
        }

        public void SetRandomCycleJobMode(bool use)
        {
            CycleRandomJobRequest = use;
        }
        public void SetAllShelfUnloadJobRequest(bool use)
        {
            AllShelfUnloadJobRequest = use;
        }
        public void SetUsePortGet(bool use)
        {
            UsePortGet = use;
        }

        public string GetSchedulerName()
        {
            return SchedulerName;
        }

        public virtual void MapChangeForExitThread()
        {

        }

        public virtual void SchedulerAutoRun()
        {
            throw new NotImplementedException("SchedulerAutoRun() 는 구현되지 않았습니다.");
        }
        public virtual void InitScheduler()
        {
            throw new NotImplementedException("InitScheduler() 는 구현되지 않았습니다.");
        }

        public virtual void StartScheduler()
        {
            StartSignal = true;
        }
        public virtual void StopScheduler()
        {
            StartSignal = false;
        }
        //스케쥴러 중단 상태 판단.
        public virtual bool CheckSchedulerPaused()
        {
            return true;
        }

        public virtual eSC_OperationModeResultCode ChangeCraneOperationMode(eSC_OperationMode Mode)
        {
            return eSC_OperationModeResultCode.NotSupported;
        }
        public virtual bool GetStartState()
        {
            return StartSignal;
        }

        protected int GetBankByTag(string Tag)
        {
            try
            {
                //220420 HHJ SCS 개선     //- ShelfTagHelper 추가
                //int bank;
                //int.TryParse(Tag.Substring(0, 1), out bank);
                //return bank;
                return ShelfTagHelper.GetBank(Tag);
            }
            catch (Exception)
            {
                return 0;
            }
        }
        protected int GetLevelByTag(string Tag)
        {
            try
            {
                //220420 HHJ SCS 개선     //- ShelfTagHelper 추가
                //int level;
                //int.TryParse(Tag.Substring(1, 3), out level);
                //return level;
                return ShelfTagHelper.GetLevel(Tag);
            }
            catch (Exception)
            {
                return 0;
            }
        }
        protected int GetBayByTag(string Tag)
        {
            try
            {
                //220420 HHJ SCS 개선     //- ShelfTagHelper 추가
                //int bay;
                //int.TryParse(Tag.Substring(4, 3), out bay);
                //return bay;
                return ShelfTagHelper.GetBay(Tag);
            }
            catch (Exception)
            {
                return 0;
            }
        }
        public void SetAddtionalMargin(int BayMargin) //DualLack 전용 뒤따라 갈때 추가 베이 마진값을 셋한다. 
        {
            if (BayMargin >= 0 && BayMargin <= 10) //10 이상은 제한한다.
            {
                RM_AddtionalMargin = BayMargin;
                LogManager.WriteConsoleLog(eLogLevel.Info, "Set RM Margin:{0}      Additional Margin: {1}", _RM_RangeMargin, RM_AddtionalMargin);

            }
        }
        public void SetWaitInTime(int sectime)
        {
            if (sectime >= 0 && sectime <= 36000) //WaitIn 커맨드 대기시간 최장 10시간정도로 잡아본다.
            {
                WaitInNoCommandTimeOut = sectime;
            }
        }
        public void SetWaitInTime(string sectime)
        {
            try
            {
                int value = int.Parse(sectime);
                SetWaitInTime(value);
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                SetWaitInTime(30);//예외 발생시 기본값 적용.
            }

        }
        //220823 조숭진 config로 수정 s
        public virtual void SetRM1ExeclusiveBay(int value)
        {
            _RM1_ExclusiveBay = value;
        }

        public virtual void SetRM2ExeclusiveBay(int value)
        {
            _RM2_ExclusiveBay = value;
        }

        public virtual void SetExeclusiveRange()
        {
            throw new NotImplementedException("SetExeclusiveRange() 는 구현되지 않았습니다.");
        }

        protected virtual void CheckDestinationAction(McsJob Job)
        {
            throw new NotImplementedException("CheckDestinationAction() 는 구현되지 않았습니다.");
        }

        protected bool CheckTargetInterLock(McsJob Job)
        {
            bool bSourceCheckOK = false;
            bool bDestCheckOK = false;
            var TJob = Job;
            ICarrierStoreAble SourceItem = null;
            try
            {
                if (TJob == null)
                {
                    return false;
                }
                if (TJob.JobType == "MOVE")
                {
                    //이동 명령은 쉘프 체크 필요없음
                    return true;
                }


                //241125 RGJ 인터락 체크에 크레인 데이터 체크도 추가함

                if (Job.AssignedRM.CarrierExistSensor && Job.AssignedRM.InSlotCarrier == null) //240926 RGJ 화물 감지 상태인데 어떠한 이유든 크레인에 데이터가 없다면
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} Carrier :{1} Source: {2} => Dest:{3} Crane's Carrier Data  have lost.Check or Insert CarrierData!", Job.CommandID, Job.CarrierID, Job.Source, Job.Destination);
                    if (CarrierStorage.Instance.CarrierContain(Job.CarrierID)) //스토리지에 남아있으면 복구 시도
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} Carrier :{1} Source: {2} => Dest:{3} Try to update set Carrierdata on Crane", Job.CommandID, Job.CarrierID, Job.Source, Job.Destination);
                        Job.AssignedRM.UpdateCarrier(Job.CarrierID);
                    }
                    else //없으면 복구할 방법이 없음. 어떤 이유에서든 삭제된 상태 -> 알람
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} Carrier :{1} Source: {2} => Dest:{3} Carrier data lost in Storage! Go Alarm!", Job.CommandID, Job.CarrierID, Job.Source, Job.Destination);
                        GlobalData.Current.Alarm_Manager.AlarmOccurbyName("CRANE CARRIER DATA LOST", Job.AssignRMName);
                        Thread.Sleep(1000);
                        return false;
                    }
                }


                if (!Job.AssignedRM.CarrierExistSensor && Job.AssignedRM.InSlotCarrier != null) //241004 RGJ 화물 감지 안되는데 어떠한 이유든 크레인에 데이터가 있다면
                {
                    if (CarrierStorage.Instance.CarrierContain(Job.CarrierID)) //스토리지에 남아있으면 실물이 어디있는지 알수 없으므로 알람.
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} Carrier :{1} Source: {2} => Dest:{3} Crane has carrier data  but not sensing.Go Alarm!", Job.CommandID, Job.CarrierID, Job.Source, Job.Destination);
                        GlobalData.Current.Alarm_Manager.AlarmOccurbyName("CRANE CARRIER SENSOR DATA MISMATCH", Job.AssignRMName);
                        Thread.Sleep(1000);
                        return false; ;
                    }
                    else //스토리지에 없으면 리셋.
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} Carrier :{1} Source: {2} => Dest:{3} Crane has carrier data  but not sensing.Carrier was not found in Storage. Reset Data", Job.CommandID, Job.CarrierID, Job.Source, Job.Destination);
                        Job.AssignedRM.ResetCarrierData();
                    }
                }

                SourceItem = TJob.CarrierLocationItem;

                if (SourceItem is ShelfItem sItem) //소스가 쉘프
                {

                    if (TJob.AssignedRM.CheckCarrierExist() && TJob.AssignedRM.CarrierID == Job.CarrierID) //이미 크레인이 해당 캐리어를  들고 있다.
                    {
                        bSourceCheckOK = true;
                    }
                    else
                    {
                        if (sItem != null)
                        {
                            bSourceCheckOK = sItem.CheckCarrierExist() && sItem.CarrierID == Job.CarrierID;
                        }
                    }

                }
                else if (SourceItem is CV_BaseModule) //소스가 포트
                {
                    //포트 사양이 안나와서 스킵
                    bSourceCheckOK = true;
                }
                else if (SourceItem is RMModuleBase) //소스가 크레인
                {
                    bSourceCheckOK = true;
                }
                //목표가 쉘프
                if (TJob.DestItem is ShelfItem) //목표가 쉘프
                {
                    //일단 쉘프에 캐리어 존재 여부만 체크
                    var tShelf = ShelfManager.Instance.GetShelf(TJob.DestBank, TJob.DestBay, TJob.DestLevel);
                    if (tShelf != null)
                    {
                        bDestCheckOK = !tShelf.CheckCarrierExist();
                    }
                    else
                    {
                        return false;
                    }
                }
                //목표가 포트
                else if (TJob.DestItem is CV_BaseModule) //목표가 포트
                {
                    //포트 사양이 안나와서 스킵
                    bDestCheckOK = true;
                }
                else if (TJob.DestItem is RMModuleBase) //목표가 크레인
                {
                    bDestCheckOK = true;
                }
                bool CheckResult = bSourceCheckOK && bDestCheckOK;
                if(!CheckResult) //로그 추가.
                {
                    string srcLoc = SourceItem is null ? "null" : SourceItem.iLocName;
                    string destLoc = TJob.DestItem is null ? "null" : TJob.DestItem.iLocName;
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Job {0} CheckTargetInterLock Fail Source({1}) : {2} -> Dest({3}) : {4}",Job.CommandID, bSourceCheckOK, srcLoc, bDestCheckOK, destLoc);
                }
                return CheckResult;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }
        }

        public virtual bool CheckJobOnProcess(string JobID)
        {
            throw new NotImplementedException("CheckJobOnProcess() 는 구현되지 않았습니다.");
        }

        protected void SCWriteTransferLog(McsJob Job,string CraneCommandType ,string EndStatus)
        {
            try
            {
                //20230713 RGJ Transfer Log 수정.
                LogManager.WriteTransferLog(eLogLevel.Info, "{0}/{1}/{2}/{3}/{4}/{5}/{6}/{7}/{8}/{9}", Job.AssignRMName, Job.CommandID, CraneCommandType,
                    Job.TCStatus.ToString(), Job.CarrierID, Job.Source, Job.Destination, Job.CarrierLoc, Job.Priority, EndStatus);
            }
            catch (Exception)
            {

            }
        }
        public virtual void SetUseEmptyRetriveAutoReset(bool value)
        {
            _UseEmptyRetriveAutoReset = value;
        }
        /// <summary>
        /// Double Storage Case 발생시 알람 클리어 대기후 처리 부분
        /// </summary>
        /// <param name="Job"></param>
        /// <returns></returns>
        protected bool WaitAlarmClearActionForDS(McsJob Job)
        {
            if (!GlobalData.Current.Alarm_Manager.CheckModuleDSAlarmExist(Job.AssignedRM.ModuleName)) //이중입고 알람 클리어 체크
            {
                Thread.Sleep(300);//241029 RGJ 알람 클리어 보고 순서 보장을 위해 잠시대기 
                //[230503 CIM 검수] Double Storage 605 Abort 로 올라감
                GlobalData.Current.HSMS.SendS6F11(605, "JobData", Job, "CommandType" ,"ABORT"); //OperatorInitiatedAction CEID = 605 CommandType = "ABORT"
                Job.TCStatus = eTCState.ABORTING;
                Thread.Sleep(200); //231103 RGJ 조범석 매니저 요청으로 605 보고후 200ms 딜레이 요청 추가.
                GlobalData.Current.HSMS.SendS6F11(201, "JobData", Job); //TransferAbortCompleted CEID = 201 //Result Code = 7 (Double Storage)
                return true;
            }
            return false;
        }

        /// <summary>
        /// Source Empty 발생시 Host Abort 받아서 처리하는 부분.
        /// </summary>
        /// <param name="Job"></param>
        /// <returns></returns>
        protected bool WaitHostAbortActionForSE(McsJob Job)
        {
            if (Job.MCSJobAbortReq) //호스트로 부터 Abort 커맨드 받았다면
            {
                Job.TCStatus = eTCState.ABORTING;
                GlobalData.Current.Alarm_Manager.AlarmClear(Job.AssignRMName, GlobalData.SOURCE_EMPTY_ALARM_CODE, Job);
                return true;
            }
            return false;
        }

        protected void EmptyRetrieveAction(McsJob Job)
        {
            Job.TCStatus = eTCState.ABORTING;
            if (UseEmptyRetriveAutoReset) //공출고 자동해제 모드이면
            {
                GlobalData.Current.Alarm_Manager.AlarmClear(Job.AssignRMName, GlobalData.SOURCE_EMPTY_ALARM_CODE, Job); //알람도 클리어 시킨다.
            }
            else //알람리셋을 기다린다.
            {
                while (true)
                {
                    if (!Job.AssignedRM.CheckEmptyRetriveState())
                    {
                        break;
                    }
                    Thread.Sleep(50);
                }
            }

            GlobalData.Current.HSMS.SendS6F11(201, "JobData", Job); //TransferAbortCompleted CEID = 201
            ShelfItem sShelf = Job.LastActionTarget as ShelfItem;
            if (sShelf != null)
            {
                ShelfManager.Instance.ProcessCarrierSourceEmpty(Job, sShelf.CarrierID); //쉘프쪽 캐리어 삭제 및 공캐리어 추가.
                sShelf.NotifyScheduled(false);
            }
        }
        protected bool NeedTransferInitiatedReport(McsJob Job)
        {
            if (Job == null)
            {
                return false;
            }
            if (Job.SubJob == eSubJobType.Push) //Push 잡 보고 안함
            {
                return false;
            }
            if (Job.SubJob == eSubJobType.AlterStore) //AlterStore 잡 보고 안함
            {
                return false;
            }
            if (Job.SubJob == eSubJobType.HandOver && Job.CarrierLoc == Job.HandOverStoredDest) //Hand Over 2차 작업시작은 초기화 보고 없음
            {
                return false;
            }
            if(Job.JobSourcePortError) //[230503 CIM 검수]  소스 포트에러시 해당 보고 없음
            {
                return false;
            }
            if(Job.CommandType.ToUpper() == "MOVE")  //231006 RGJ 단순 MOVE 커맨드는 보고 필요없음 -조범석 매니저확인- 
            {
                return false;
            }
            return true;
        }
        protected bool NeedTransferResume(McsJob Job)
        {
            if (Job == null)
            {
                return false;
            }
            else
            {
                return Job.JobSourcePortError;  //[230503 CIM 검수]
            }
        }

        public virtual int CheckHandOverRequire(ICarrierStoreAble Source, ICarrierStoreAble Dest)
        {
            return -1;
        }
        public virtual bool CheckShelfExclusiveCraneAccess(ShelfItem sItem)
        {
            return false;
        }

        
        public virtual List<RMModuleBase> GetNotAssignRM()
        {
            return null;
        }
        protected bool SetCurPriorityChangeJob(McsJob CurJob)
        {
            try
            {
                int MinPriority = CurJob.ScheduledPriority;

                for (int i = 0; i < JobList.Count; i++)
                {
                    var sJob = JobList[i];
                    if (sJob.TCStatus != eTCState.QUEUED || sJob.CommandID == CurJob.CommandID)
                    {
                        continue;
                    }

                    if (sJob.JobType != "MOVE")
                    {
                        if (sJob.ScheduledPriority <= MinPriority)
                            MinPriority = sJob.ScheduledPriority;
                    }
                }

                if (MinPriority < CurJob.ScheduledPriority)
                {
                    if (MinPriority == 0)
                        JobList.ChangePriority(CurJob, MinPriority);
                    else
                        JobList.ChangePriority(CurJob, MinPriority - 1);
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return false;
            }
            return true;
        }
        //프로그램 시작시 작업이 진행 상태였다면 해당 작업 복구를 수행한다.
        protected void JobRecoveryAction()
        {
            var pushJobList = JobList.Where(j => j.SubJob == eSubJobType.Push).ToList(); //기존에 남아있던 Push 작업 삭제
            foreach (var pJob in pushJobList) //230819 RGJ Push 작업 전체 삭제로 변경
            {
                if(pJob != null)
                {
                    JobList.DeleteMcsJob(pJob, true);
                }
                
            }
            //240209 RGJ TRANSFER Job 만 쉘프 예약을 변경한다.
            var JobQueued = JobList.Where(j => j.TCStatus == eTCState.QUEUED && j.JobType == "TRANSFER"); //대기중 작업 Shelf Scheduled 재설정
            foreach (var job in JobQueued)
            {
                if(job.SourceItem != null && job.SourceItem.CheckCarrierExist() && job.CarrierID == job.SourceItem.GetCarrierID())  //화물이 있을때만 예약한다. 화물없는데 Get 예약은 의미없음.
                {
                    job.SourceItem?.NotifyScheduled(true);
                }
                if(job.DestItem != null && !job.DestItem.CheckCarrierExist()) //화물이 없을때만 예약한다. 다른 화물이 존재하는 는데 Put 예약은 의미없음.
                {
                    job.DestItem?.NotifyScheduled(true);
                }

            }

            #region   //크레인 실물과 데이터 불일치 처리 //Get 도중 또는 Put 도중 프로그램 Off 케이스

            foreach (var RM in GlobalData.Current.mRMManager.ModuleList)
            {
                RMModuleBase Crane = RM.Value;
                if (Crane.CheckCarrierExist()) //센서에 캐리어 존재
                {
                    if (Crane.InSlotCarrier == null) //데이터가 없다
                    {
                        //Get 이면 소스 Item 의 캐리어 정보를 크레인으로 옮겨준다.
                        if (Crane.PC_CraneCommand == eCraneCommand.PICKUP) //기존 마지막 커맨드 획득
                        {
                            if (Crane.PC_DestWorkPlace_FORK1 > 0) //포트
                            {
                                CV_BaseModule cItem = GlobalData.Current.PortManager.GetCVModuleWorkPlace(Crane.PC_DestWorkPlace_FORK1);
                                cItem?.TransferCarrierData(Crane);
                            }
                            else //쉘프
                            {
                                ShelfItem sItem = ShelfManager.Instance.GetShelf(Crane.PC_DestBank_FORK1, Crane.PC_DestBay_FORK1, Crane.PC_DestLevel_FORK1);
                                sItem?.TransferCarrierData(Crane);
                            }
                        }
                    }
                }
                else //센서에 캐리어 없음
                {
                    if (Crane.InSlotCarrier != null) //데이터가 있다[PUT 도중에 OFF 로 추정]
                    {
                        //PUT 이면 크레인 캐리어 정보를 목적 Item 으로 옮겨준다.
                        if (Crane.PC_CraneCommand == eCraneCommand.UNLOAD) //기존 마지막 커맨드 획득
                        {
                            if (Crane.PC_DestWorkPlace_FORK1 > 0) //포트
                            {
                                CV_BaseModule cItem = GlobalData.Current.PortManager.GetCVModuleWorkPlace(Crane.PC_DestWorkPlace_FORK1);
                                Crane.TransferCarrierData(cItem);
                            }
                            else //쉘프
                            {
                                ShelfItem sItem = ShelfManager.Instance.GetShelf(Crane.PC_DestBank_FORK1, Crane.PC_DestBay_FORK1, Crane.PC_DestLevel_FORK1);
                                Crane.TransferCarrierData(sItem);
                            }
                        }
                    }
                }
            }

            #endregion

            var JobforRecovery = JobList.Where(j => j.TCStatus == eTCState.TRANSFERRING || j.TCStatus == eTCState.PAUSED).ToList();
            foreach (var job in JobforRecovery)
            {
                //240328 RGJ  잡 삭제는 작업자 혹은 상위에서 취소하거나 혹은 목적지 도착할때만 삭제함.
                //CarrierItem JobCarrierItem = CarrierStorage.Instance.GetCarrierItem(job.CarrierID);
                //if (JobCarrierItem == null) //해당 캐리어가 SCS 내부에 없다. => 작업 삭제
                //{
                //    JobList.DeleteMcsJob(job, false, true);
                //}

                if(job.TCStatus == eTCState.TRANSFERRING) //반송중인데 크레인에 없으면 이미 완료 되었거나 시작전임
                {
                    if(!(job.CarrierLocationItem is RMModuleBase rm))
                    {
                        job.TCStatus = eTCState.PAUSED;
                    }
                }

                if (job.CarrierLocationItem is RMModuleBase rmmodule)
                {

                }
                else
                {
                    if (job.SourceItem != null && job.SourceItem.CheckCarrierExist() && job.CarrierID == job.SourceItem.GetCarrierID())  //화물이 있을때만 예약한다. 화물없는데 Get 예약은 의미없음.
                    {
                        job.SourceItem?.NotifyScheduled(true);
                    }
                }
                if (job.DestItem != null && !job.DestItem.CheckCarrierExist()) //화물이 없을때만 예약한다. 다른 화물이 존재하는 는데 Put 예약은 의미없음.
                {
                    job.DestItem?.NotifyScheduled(true);
                }

                //캐리어가 크레인에 있다면 타켓 작업 생성하고 스탭 처리
                //캐리어가 소스와 다른 위치에 있다면 해당위치로 소스 변경하고 다시 스케쥴링 한다. //Hand Over Case 또는 Alter 반송 케이스
                //캐리어가 목표물에 있다면 완료 보고 하고 완료처리 
            }


        }

        /// <summary>
        /// 해당 캐리어 사이즈를 내보낼만한 적절한 포트를 찾는다.
        /// 사양서상에는 메뉴얼 포트로 보내야하는데 메뉴얼 포트 없는경우는?? 
        /// </summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public virtual CV_BaseModule GetFireAlterOutPort(ICarrierStoreAble Source, eCarrierSize Size)
        {
            return null;
        }
        protected int CalcDistance(int a, int b)
        {
            int c = a - b;
            if (c > 0)
            {
                return c;
            }
            else
            {
                return -c;
            }
        }
        protected int CalcBayDistance(int a, int b)
        {
            int c = b - a;
            return c > 0 ? c : -c;
        }
        public virtual eCraneExZone GetCraneExZone(ICarrierStoreAble Source)
        {
            return eCraneExZone.SharedZone;
        }
        public virtual bool CheckCraneReachAble(RMModuleBase Crane, ICarrierStoreAble Target)
        {
            return true;
        }



        /// <summary>
        /// Get 동작중 에러났을경우 작업과 데이터 처리.개발중
        /// </summary>
        /// <param name="Job"></param>
        protected void ProcesstGetFailed(McsJob Job)
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "Enter ProcesstGetFailed Job :{0} CarrierID :{1} ", Job.CommandID, Job.CarrierID);
            if (Job == null || Job.AssignedRM == null)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "ProcesstGetFailed Aborted by null Parameter");
            }
            Job.TCStatus = eTCState.PAUSED; //작업 상태 처리
            Job.JobResult = eJobResultCode.OTHER_ERROR;
            Job.Step = enumScheduleStep.RMError;

            while (true) //포크 접긴전까진 아무것도 못함.
            {
                if (Job.AssignedRM.CheckForkIsCenter()) //포크를 접었는지 체크
                {
                    //포크를 접었는데 감지 상태
                    if (Job.AssignedRM.CarrierExistSensor) //240212 RGJ 크레인 Get 도중에 문제가 발생했지만 화물이 감지 되면 데이터를 옮긴다.알람 처리하고 재진행 안됨.
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "ProcesstGetFailed {0} Fork is Center Position and Carrier is Existed in Crane", Job.AssignedRM.ModuleName);
                        if (Job.LastActionTarget != null)
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} Source :{1} => {2} ProcesstGetFailed Data has been transferred by Alarm", Job.CommandID, Job.LastActionTarget.iLocName, Job.AssignedRM.ModuleName);
                            Job.LastActionTarget.ResetCarrierData();
                            Job.AssignedRM.UpdateCarrier(Job.CarrierID);
                        }
                    }
                    else //포크를 접었지만 화물 감지 안된상태 -> 따로 처리 해야할건 없음 로그만 찍는다.
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "ProcesstGetFailed {0} Fork is Center Position and Carrier is Not Existed in Crane", Job.AssignedRM.ModuleName);
                    }
                    break;
                }
                else
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "ProcesstGetFailed {0} Fork is Not Center Position. Waiting...", Job.AssignedRM.ModuleName);
                }
                Thread.Sleep(1000);
            }
        }

        /// <summary>
        /// Put 동작중 에러났을경우 작업과 데이터 처리.개발중
        /// </summary>
        /// <param name="Job"></param>
        protected void ProcesstPutFailed(McsJob Job)
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "Enter ProcesstPutFailed Job :{0} CarrierID :{1} ", Job.CommandID, Job.CarrierID);
            if (Job == null || Job.AssignedRM == null)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "ProcesstPutFailed Aborted by null Parameter");
            }
            Job.TCStatus = eTCState.PAUSED; //작업 상태 처리
            Job.JobResult = eJobResultCode.OTHER_ERROR;
            Job.Step = enumScheduleStep.RMError;

            while (true)//포크 접긴전까진 아무것도 못함.
            {
                if (Job.AssignedRM.CheckForkIsCenter()) //포크를 접었는지 체크
                {
                    //포크를 접었는데 감지 상태
                    if (Job.AssignedRM.CarrierExistSensor) //다시 리트라이 하거나 대체반송 해야함.
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "ProcesstPutFailed {0} Fork is Center Position and Carrier is Existed in Crane", Job.AssignedRM.ModuleName);
                    }
                    else //포크를 접었지만 화물 감지 안된상태 -> 이건 화물 데이터를 목적지로 옮겨야 함.
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "ProcesstPutFailed {0} Fork is Center Position and Carrier is Not Existed in Crane", Job.AssignedRM.ModuleName);
                        if (Job.LastActionTarget != null)
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} Source :{1} => {2} ProcesstPutFailed Data has been transferred by Alarm", Job.CommandID, Job.LastActionTarget.iLocName, Job.AssignedRM.ModuleName);
                            //231027 컨베이어 트래킹데이터에 넣어준다.
                            CV_BaseModule RobotAccessCV = Job.LastActionTarget as CV_BaseModule;
                            if (RobotAccessCV != null)
                            {
                                RobotAccessCV.WriteTrackingData(Job.JobCarrierItem);
                                //2024.05.15 lim, 내려 놓아도 멍때려서 동작 안함.
                                RobotAccessCV.NotifyTrayLoadComplete(Job.AssignedRM, Job); //투입했다고 알려준다.
                            }
                            Job.AssignedRM.ResetCarrierData();
                            Job.LastActionTarget.UpdateCarrier(Job.CarrierID);

                            //2024.06.21 lim, shelf면 완료 처리 필요
                            if (Job.LastActionTarget as ShelfItem != null)
                            {
                                Job.TCStatus = eTCState.NONE; //작업 상태 처리
                                Job.JobResult = eJobResultCode.SUCCESS;
                                Job.Step = enumScheduleStep.CraneJobComplete;
                            }
                        }
                    }
                    break;
                }
                else
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "ProcesstPutFailed {0} Fork is Not Center Position. Waiting...", Job.AssignedRM.ModuleName);
                }
                Thread.Sleep(1000);
            }
        }

        protected ShelfItem GetFireAssemblyPoint(RMModuleBase rm)
        {
            int iCenterBay = ShelfMaxBay / 2;        //베이의 중심을 찾는다.
            int iFireAssemblyBay = iCenterBay;
            //듀얼 크레인
            if (GlobalData.Current.mRMManager.ModuleList.Count > 1)
            {
                if (rm.IsFirstRM)
                {
                    iFireAssemblyBay -= 4;      //1번 크레인은 -4 베이 위치로
                }
                else
                {
                    iFireAssemblyBay += 4;      //2번 크레인은 +4 베이 위치로
                }
            }
            
            var DestShelf = ShelfManager.Instance.AllData.Items.Where(s => s.ShelfBay == iFireAssemblyBay && !s.DeadZone && s.ShelfLevel >= 4).OrderBy(s => s.ShelfLevel).FirstOrDefault(); //작업에 선택할 목표 쉘프
            if (DestShelf == null)
            {
                return null;
            }
            return DestShelf;
        }

        /// <summary>
        /// 241105 RGJ TIMEOUT 시스템 시간 의존 개선.
        /// 시스템 시간 변경에도 Stopwatch 를 활용하여  올바르게 동작가능하게함
        /// Stopwatch stopwatch = Stopwatch.StartNew();
        /// IsTimeout(stopwatch, 10); // 10초 타임아웃 체크
        /// </summary>
        /// <param name="stopwatch"></param>
        /// <param name="nTimeoutValue"></param>
        /// <returns></returns>
        public bool IsTimeout_SW(Stopwatch stopwatch, int nTimeoutValue)
        {
            if (stopwatch == null)
            {
                return false;
            }
            // 지정한 초를 기준으로 Timeout 설정
            TimeSpan tLimit = TimeSpan.FromSeconds(nTimeoutValue);
            return stopwatch.Elapsed > tLimit;
        }
    }
}
