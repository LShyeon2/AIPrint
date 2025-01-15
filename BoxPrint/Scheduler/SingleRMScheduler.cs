
using BoxPrint.Alarm;
using BoxPrint.DataList;
using BoxPrint.DataList.MCS;
using BoxPrint.Log;
using BoxPrint.Modules;
using BoxPrint.Modules.Conveyor;
using BoxPrint.Modules.RM;
using BoxPrint.Modules.Shelf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using WCF_LBS.Commands;
//
namespace BoxPrint.Scheduler
{
    /// <summary>
    /// //220321 RGJ 듀얼랙마 스케쥴링 개발
    /// </summary>
    public class SingleRMScheduler : BaseScheduler
    {
        private object SecureAreaLock = new object();
        private bool AlreadyMoveComp = false;       //크레인 에러발생 혹은 메뉴얼->오토 전환 시 move를 다시 탈때 필요한 flag
      
        Task<Job_Result> JobTaskRM1 = null;
        //Task<Job_Result> JobTaskRM2 = null;

        //220823 조숭진 config로 수정 s
        public SingleRMScheduler()
        {
            string value = string.Empty;
            SchedulerName = "SingleRMScheduler";

            SetUsePortGet(true);

            //JobRecoveryAction(); //복구 작업 수행
        }
        //220823 조숭진 config로 수정 e
        /// <summary>
        /// //220321 RGJ 듀얼랙마 스케쥴링 개발
        /// 작업을 할당하고 태스크를 가동시킨다.
        /// </summary>
        /// <param name="RMNumber"></param>
        /// <param name="Job"></param>
        /// <returns></returns>

        private bool AssignJob(int RMNumber, McsJob Job)
        {
            if (RMNumber == 1)
            {
                if (GlobalData.Current.mRMManager.FirstRM.PLC_ErrorCode > 0 ||  //크레인 알람 코드 올라와 있으면 동작 안함.RGJ 241112 알람코드를 직접보게 수정.
                    GlobalData.Current.mRMManager.FirstRM.CheckModuleHeavyAlarmExist())
                { 
                    return false;
                }
                //231019 RGJ Crane 작업 할당전 상태 체크 추가. 기상반 지상반 상태 체크 추가.
                if(GlobalData.Current.mRMManager.FirstRM.CheckRMAutoMode())
                {
                    if (_RM1_OnProcessJob == null)
                    {
                        Job.AssignedRM = GlobalData.Current.mRMManager[RMNumber]; //231025 RGJ  Job 에 RM 할당이 Task 만들고 이루어지면서  짧은 시간  AssignedRM 검색에 실패해서 예외발생
                        _RM1_OnProcessJob = Job;

                        JobTaskRM1 = Task<Job_Result>.Factory.StartNew(() =>
                        {
                            Thread.CurrentThread.Name = Job.CommandID + " Task";
                            return JobProcess(_RM1_OnProcessJob, 1);
                        });
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                
            }
            return false;
        }

        private McsJob GetNextMcsJob(int RMNumber)
        {
            try
            {
                //RM 의 현재 위치를 구한다.
                var TargetRM = GlobalData.Current.mRMManager[RMNumber];
                int RMCurrentBay = TargetRM.CurrentBay;
                McsJob nextJob = null;
                if (JobList.Count() == 0)
                {
                    return null;
                }
                //해당 RM 이 이미 Carrier 를 들고 있으면 그에 해당하는 Job만을 고른다.
                if (TargetRM.CarrierExistSensor)
                {
                    for (int i = 0; i < JobList.Count(); i++)
                    {
                        var sJob = JobList[i];
                        if (sJob.DestItem == null || sJob.JobNowDeleting) //삭제중인 작업 제외
                        {
                            continue; //목적지를 잃어버림. 레이아웃이 변한경우나 잡에대한 디비를 직접 수정한 경우로 추정.
                        }
                        if (sJob == RM1_OnProcessJob) //크레인 1 이미 실행중인 작업 제외
                        {
                            continue;
                        }
                        if (sJob.TCStatus == eTCState.QUEUED || sJob.TCStatus == eTCState.PAUSED || sJob.TCStatus == eTCState.TRANSFERRING)
                        {
                            if (sJob.CarrierID == TargetRM.CarrierID)
                            {
                                bool DestCheck = sJob.DestItem.CheckPutAble();
                                if (!DestCheck && sJob.DestItem is ShelfItem)
                                {
                                    sJob.DestItem.NotifyScheduled(true);//작업 목록에 있는데 쉘프 예약이  안되어있다면 예약.
                                    continue; //대상 목적지 사용불가
                                }
                                nextJob = sJob;
                                return nextJob;
                            }
                        }
                    }
                    return null;
                }

                int MaxPriority = 0;
                int MinDistance = int.MaxValue;

                List<McsJob> ListJobProcessAble = new List<McsJob>(); //가용한 작업들을 1차로 추려낸다.
                List<McsJob> ListHighPriorityJob = new List<McsJob>(); //우선순위 가장 높을 작업들을 추려낸다.
                //스케쥴링 스탭 - #1 가용가능한 작업을 추려낸다.
                //스케쥴링 스탭 - #2 최우선순위 값을 체크한다.
                //스케쥴링 스탭 - #3 최우선순위 작업이 2개 이상일경우 크레인 기준 가까운 것으로 결정한다.

                #region #1 가용가능한 작업을 추려낸다.
                for (int i = 0; i < JobList.Count(); i++)
                {
                    var sJob = JobList[i];
                    if(sJob.DestItem == null || sJob.JobNowDeleting) //삭제중인 작업 제외
                    {
                        continue; //목적지를 잃어버림. 레이아웃이 변한경우나 잡에대한 디비를 직접 수정한 경우로 추정.
                    }
                    if (sJob.TCStatus == eTCState.QUEUED || sJob.TCStatus == eTCState.PAUSED)
                    {

                        if (sJob.JobType == "TRANSFER")
                        {
                            if (sJob.SubJob == eSubJobType.AlterStore && sJob.CarrierLoc != sJob.Source)  //대체보관  완료 상태
                            {
                                bool BufferCheck = sJob.CarrierLocationItem.CheckGetAble(sJob.CarrierID);
                                bool DestCheck = sJob.DestItem.CheckPutAble();
                                //현재 작업이 불가능한 상태면 스킵
                                if (!BufferCheck || !DestCheck) //소스와 목적지 가용성 점검 하나라도 문제 있으면 작업제외
                                {
                                    if (!BufferCheck && sJob.CarrierLocationItem is ShelfItem)
                                    {
                                        sJob.CarrierLocationItem.NotifyScheduled(true);//작업 목록에 있는데 쉘프 예약이  안되어있다면 예약.
                                    }
                                    if (!DestCheck && sJob.DestItem is ShelfItem)
                                    {
                                        sJob.DestItem.NotifyScheduled(true);//작업 목록에 있는데 쉘프 예약이  안되어있다면 예약.
                                    }
                                    continue;
                                }
                            }
                            else
                            {
                                bool SourceCheck = sJob.SourceItem.CheckGetAble(sJob.CarrierID);
                                bool DestCheck = sJob.DestItem.CheckPutAble();
                               
                                //현재 작업이 불가능한 상태면 스킵
                                if (!SourceCheck || !DestCheck) //소스와 목적지 가용성 점검 하나라도 문제 있으면 작업제외
                                {
                                    if (!SourceCheck && sJob.SourceItem is ShelfItem)
                                    {
                                        sJob.SourceItem.NotifyScheduled(true);//작업 목록에 있는데 쉘프 예약이  안되어있다면 예약.
                                    }
                                    if (!DestCheck && sJob.DestItem is ShelfItem)
                                    {
                                        sJob.DestItem.NotifyScheduled(true);//작업 목록에 있는데 쉘프 예약이  안되어있다면 예약.
                                    }
                                    if (sJob.SourceItem is CV_BaseModule && sJob.DestItem is CV_BaseModule && SourceCheck && !DestCheck)
                                    {
                                        ; //출발 목적지가 모두 포트이고 소스 사용가능 하면 일단 뜬다.
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                            }
                        }
                        ListJobProcessAble.Add(sJob);
                    }
                }
                #endregion

                #region #2 최우선순위 값을 체크한다.
                if (ListJobProcessAble.Count() == 0) //가용 가능한 작업 없음
                {
                    return null;
                }
                else
                {
                    MaxPriority = ListJobProcessAble.Max().ScheduledPriority;
                    ListHighPriorityJob = ListJobProcessAble.Where(J => J.ScheduledPriority == MaxPriority).ToList();
                }
                #endregion

                #region #3 최우선순위 작업이 2개 이상일경우 크레인 기준 가까운 것으로 결정한다.
                for (int i = 0; i < ListHighPriorityJob.Count(); i++) //스케쥴러 동일 우선순위 잡이 여러개 있을경우 가까운 소스 작업 부터 진행.
                {
                    var sJob = ListHighPriorityJob[i];
                    if (sJob.ScheduledPriority == MaxPriority)
                    {
                        if (sJob.JobSourcePortError) //[230503 CIM 검수] 소스 포트 에러 케이스 먼저 처리 요청.
                        {
                            nextJob = sJob;
                            break;
                        }
                        if (CalcDistance(RMCurrentBay, sJob.SourceBay) < MinDistance)
                        {
                            MinDistance = CalcDistance(RMCurrentBay, sJob.SourceBay);
                            nextJob = sJob;
                        }
                    }
                }
                #endregion

                return nextJob;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return null;
            }

        }
     
        /// <summary>
        /// //220321 RGJ 듀얼랙마 스케쥴링 개발
        /// 선택된 작업을 실행.
        /// </summary>
        /// <param name="Job"></param>
        /// <returns></returns>
        private Job_Result JobProcess(McsJob Job, int RMNumber)
        {
            //작업을 시작하려는데 작업리스트에 없다는 의미는 삭제 타이밍과 겹친것임.
            if (!JobList.Contains(Job))
            {
                Job.VoidJob = true;
                Job.TCStatus = eTCState.ABORTING;
                LogManager.WriteConsoleLog(eLogLevel.Info, "ProcessJob  Canceled by Already Job Removing! RM : {0} Job:{1}  CarrierID :{2} Source :{3}  Dest : {4}", Job.AssignRMName, Job.CommandID, Job.CarrierID, Job.Source, Job.Destination);
                return new Job_Result(Job, Job.CommandID, eJob_Result.Aborted, "작업 이미 삭제 되어 취소 처리함");
            }
            int ErrorCode = -1;
            bool bIsPausedJob = Job.TCStatus == eTCState.PAUSED; // 예전에 포즈 되었다가 다시 시작된 작업이면 TransferInitiated 안올린다.
            //Job.AssignedRM = GlobalData.Current.mRMManager[RMNumber]; //231025 RGJ Assign 전에 할당했으므로 주석 처리함. 
            Job.Step = enumScheduleStep.TargetInterlockCheck;
            Job.JobCurrentSecureTryCount = 0;
            Job.TCStatus = eTCState.TRANSFERRING;
            Job.TransferState = 2;

            if (NeedTransferInitiatedReport(Job)) //작업 시작 보고 필요한지 체크
            {
                if (Job.JobFrom != eScheduleJobFrom.HostMCS) //스스로 만든 작업 일 경우
                {
                    //S6F11 OperatorInitiatedAction Report 605 
                    GlobalData.Current.HSMS.SendS6F11(605, "JobData", Job);
                    Thread.Sleep(200); //231103 RGJ 조범석 매니저 요청으로 605 보고후 200ms 딜레이 요청 추가. 50->200 변경
                }

                //220803 조숭진 양보move일때는 보고안한다.
                //TransferInitiated CEID 208 Report
                if (!string.IsNullOrEmpty(Job.CommandID) && !bIsPausedJob && Job.CommandType == "TRANSFER")
                {
                    GlobalData.Current.HSMS.SendS6F11(208, "JobData", Job);
                }
            }
            else if (bIsPausedJob && NeedTransferResume(Job))
            {
                GlobalData.Current.HSMS.SendS6F11(210, "JobData", Job); //Pause 잡 재시작 할때 TransferResumed (210) 보고
            }


            LogManager.WriteConsoleLog(eLogLevel.Info, "ProcessJob Start! RM : {0} Job:{1}  CarrierID :{2} Source :{3}  Dest : {4}", Job.AssignRMName, Job.CommandID, Job.CarrierID, Job.Source, Job.Destination);
            //GlobalData.Current.McdList.UpdateItem(Job);    //220329 HHJ SCS 개발     //- McsJobManager 작업중 항목 취득
            try
            {
                while (true)
                {
                    //작업 리스트에 해당작업이 없으면 강제 삭제로 간주
                    if (!JobList.Contains(Job))
                    {
                        Job.TCStatus = eTCState.ABORTING;
                        LogManager.WriteConsoleLog(eLogLevel.Info, "ProcessJob Force Canceled by Job Removing! RM : {0} Job:{1}  CarrierID :{2} Source :{3}  Dest : {4}", Job.AssignRMName, Job.CommandID, Job.CarrierID, Job.Source, Job.Destination);
                        return new Job_Result(Job, Job.CommandID, eJob_Result.Aborted, "작업 강제 삭제 되어 취소 처리함");
                    }
                    if (Job.JobForceAbortRequest)
                    {
                        Job.TCStatus = eTCState.ABORTING;
                        LogManager.WriteConsoleLog(eLogLevel.Info, "ProcessJob Force Canceled! RM : {0} Job:{1}  CarrierID :{2} Source :{3}  Dest : {4}", Job.AssignRMName, Job.CommandID, Job.CarrierID, Job.Source, Job.Destination);
                        return new Job_Result(Job, Job.CommandID, eJob_Result.Aborted, "사용자 작업 강제 취소 요청으로 취소 처리함");
                    }
                    if (GlobalData.Current.MainBooth.SCState != eSCState.AUTO) //Auto 상태가 아니면 홀딩
                    {
                        Job.JobHoldingState = true;
                        //해당 스탭에 들어갔으면 Auto 상태가 아니더라도 처리하고 태스크 종료
                        if (Job.Step == enumScheduleStep.JobPause)
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "ProcessJob : SCSTate not auto state but process job for end Task.Job:{0}", Job.CommandID);
                            return new Job_Result(Job, Job.CommandID, eJob_Result.Paused, "SCS 오토 상태가 아니므로 해당 작업은 포즈 처리.");
                        }
                        else if (Job.Step == enumScheduleStep.JobAbortComplete || Job.Step == enumScheduleStep.CraneJobComplete)
                        {
                            //dest가 쉘프이고 put중에 pause되면 적재완료 보고를 못한다. 그래서 넣어줬다.
                            if (Job.Step == enumScheduleStep.CraneJobComplete)
                            {
                                if (Job.DestItem is ShelfItem && Job.JobType == "TRANSFER") //쉘프에 Put 보고
                                {
                                    //230407 조숭진 job이 있고 crane 위에 있는 캐리어아이디를 변경했을때 해당부분에서 exception발생..
                                    //추후 어떻게 해야할지 고민해야함...
                                    CarrierStorage.Instance.GetCarrierItem(Job.CarrierID).CarrierState = eCarrierState.COMPLETED;

                                    //Put 완료 했으면 Shelf CarrierStore 305
                                    GlobalData.Current.HSMS.SendS6F11(305, "JobData", Job);
                                    //Zone Capacity Changed
                                    GlobalData.Current.HSMS.SendS6F11(310, "ZONENAME", Job.DestItem.iZoneName);

                                    //TransferCompleted 207
                                    GlobalData.Current.HSMS.SendS6F11(207, "JobData", Job);
                                }
                            }
                            LogManager.WriteConsoleLog(eLogLevel.Info, "ProcessJob : SCSTate not auto state but process job for end Task.Job:{0}", Job.CommandID);
                            return new Job_Result(Job, Job.CommandID, eJob_Result.Complete, "SCS 오토 상태가 아니므로 해당 작업은 종료 처리.");
                        }
                        else
                        {
                            Thread.Sleep(CycleTime);
                            continue;
                        }

                    }
                    else
                    {
                        Job.JobHoldingState = false;
                    }



                    switch (Job.Step)
                    {
                        case enumScheduleStep.TargetInterlockCheck: //동작전 쉘프 또는 포트 인터락 상태 체크
                            bool InterlockCheckOK = CheckTargetInterLock(Job);
                            if (InterlockCheckOK)
                            {
                                Job.Step = enumScheduleStep.RMMoveAssign;
                            }
                            else
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "Target Interlock check => Source : {0}  Dest : {1}", Job.Source, Job.Destination);
                                Job.TCStatus = eTCState.PAUSED;
                                return new Job_Result(Job, Job.CommandID, eJob_Result.Paused, "쉘프 작업 상태 이상으로 작업 중단."); //240902 RGJ 작업 인터락 이상 상태라도 작업 삭제하면 안됨.
                            }
                            break;

                        case enumScheduleStep.RMGetAssign: //GET 작업 할당
                            RMGetAssignAction(Job);
                            break;
                        case enumScheduleStep.RMGetCompleteWait: //GET 작업 완료를 기다린다
                            RMGetWaitAction(Job);
                            if (Job.Step == enumScheduleStep.RMError)
                            {
                                SCWriteTransferLog(Job, "PICK", "ABORT");
                            }
                            else if (Job.Step == enumScheduleStep.ErrorEmptyRetrieve)
                            {
                                SCWriteTransferLog(Job, "PICK", "CANCEL");
                            }
                            else
                            {
                                SCWriteTransferLog(Job, "PICK", "COMPLETE");
                            }
                            break;

                        case enumScheduleStep.RMPutAssign://PUT 작업 할당
                            RMPutAssignAction(Job);
                            break;
                        case enumScheduleStep.RMPutCompleteWait: //PUT 작업 완료를 기다린다
                            RMPutWaitAction(Job);
                            if (Job.Step == enumScheduleStep.RMError)
                            {
                                SCWriteTransferLog(Job, "UNLOAD", "ABORT");
                            }
                            else if (Job.Step == enumScheduleStep.ErrorDoubleStorage)
                            {
                                SCWriteTransferLog(Job, "UNLOAD", "CANCEL");
                            }
                            else
                            {
                                SCWriteTransferLog(Job, "UNLOAD", "COMPLETE");
                            }
                            break;

                        case enumScheduleStep.RMMoveAssign://Move 작업 할당
                            RMMoveAssignAction(Job);
                            break;

                        case enumScheduleStep.RMMoveCompleteWait: //MOVE 작업 완료를 기다린다
                            RMMoveWaitAction(Job);
                            if (Job.Step == enumScheduleStep.RMError)
                            {
                                SCWriteTransferLog(Job, "MOVE", "ABORT");
                            }
                            else
                            {
                                SCWriteTransferLog(Job, "MOVE", "COMPLETE");
                            }
                            break;

                        case enumScheduleStep.CheckDestination: //이동전 목표지점 상태를 체크
                            CheckDestinationAction(Job);
                            break;
                        case enumScheduleStep.WaitHostAbortCommand: //문제 발생시 Abort 받아야 하는경우에만 들어간다.
                            bool AbortComplete = WaitAlarmClearActionForDS(Job);
                            if (AbortComplete)
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "HostAbortCommand => Source : {0}  Dest : {1}", Job.Source, Job.Destination);
                                return new Job_Result(Job, Job.CommandID, eJob_Result.Aborted, "상위 MCS 중단명령으로 중단 처리");
                            }
                            break;
                        case enumScheduleStep.ErrorEmptyRetrieve: //공출고 발생시 처리
                            bool SE_AbortComplete = WaitHostAbortActionForSE(Job); //상위 Abort 대기
                            if (SE_AbortComplete)
                            {
                                EmptyRetrieveAction(Job);
                                LogManager.WriteConsoleLog(eLogLevel.Info, "HostAbortCommand => Source : {0}  Dest : {1}", Job.Source, Job.Destination);
                                return new Job_Result(Job, Job.CommandID, eJob_Result.Aborted, "공출고로 인한 반송 중단 완료.");

                            }
                            break;
                        case enumScheduleStep.ErrorDoubleStorage: //이중 적재 발생시 처리
                            bool DSClear = WaitAlarmClearActionForDS(Job);
                            if (DSClear)
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "Double Storage Alarm Cleared  => Source : {0}  Dest : {1}", Job.Source, Job.Destination);
                                return new Job_Result(Job, Job.CommandID, eJob_Result.Aborted, "작업자 알람 해제로 이중입고 처리");
                            }
                            break;
                        case enumScheduleStep.ErrorPortIF://포트 인터페이스 에러시 처리.
                            Job.Step = enumScheduleStep.JobPause;
                            break;

                        case enumScheduleStep.JobAbortComplete:
                            Job.JobResult = eJobResultCode.SUCCESS;   //2024.07.31 lim, Abort 정상 완료 처리
                            GlobalData.Current.HSMS.SendS6F11(201, "JobData", Job); //TransferAbortCompleted CEID = 201
                            return new Job_Result(Job, Job.CommandID, eJob_Result.Aborted, "반송 중단 완료.");

                        case enumScheduleStep.JobPause:
                            GlobalData.Current.HSMS.SendS6F11(209, "JobData", Job); //TransferPaused CEID = 209
                            return new Job_Result(Job, Job.CommandID, eJob_Result.Paused, "반송 일시 정지 완료.");


                        case enumScheduleStep.CraneJobComplete:
                            //2023.06.19 보고 순서 변경됨.
                            //2022.12.20 MCS 사양 번경 완료보고가 마지막으로 변경됨.
                            if (Job.SubJob == eSubJobType.AlterStore)
                            {
                                if (Job.Destination == Job.CarrierLoc) //240521 RGJ 대체보관 장소가 목적지인경우 완료처리유도.
                                {
                                    LogManager.WriteConsoleLog(eLogLevel.Info, "Job : {0} CarrierID : {1} SubJob AlterStore but CarrierLoc = Destination. Changed JobComplete", Job.CommandID, Job.CarrierID);
                                    Job.SubJob = eSubJobType.None;
                                    CarrierStorage.Instance.GetCarrierItem(Job.CarrierID).CarrierState = eCarrierState.COMPLETED;
                                    GlobalData.Current.HSMS.SendS6F11(305, "JobData", Job); //Put 완료 했으면 Shelf CarrierStore 305
                                }
                                else
                                {
                                    Job.AlterShelfItem.NotifyScheduled(true);
                                    CarrierStorage.Instance.GetCarrierItem(Job.CarrierID).CarrierState = eCarrierState.ALTERNATE; //캐리어 상태 Alternate 로 변경                                                             //CarrierStoreAlt 보고
                                    GlobalData.Current.HSMS.SendS6F11(306, "JobData", Job);
                                }
                                //Zone Capacity Changed
                                GlobalData.Current.HSMS.SendS6F11(310, "ZONENAME", Job.AlterShelfItem.iZoneName);
                            }
                            else
                            {
                                if (Job.DestItem is ShelfItem && Job.JobType == "TRANSFER") //쉘프에 Put 보고
                                {
                                    //230407 조숭진 job이 있고 crane 위에 있는 캐리어아이디를 변경했을때 해당부분에서 exception발생..
                                    //추후 어떻게 해야할지 고민해야함...
                                    CarrierStorage.Instance.GetCarrierItem(Job.CarrierID).CarrierState = eCarrierState.COMPLETED;

                                    //Put 완료 했으면 Shelf CarrierStore 305
                                    GlobalData.Current.HSMS.SendS6F11(305, "JobData", Job);
                                    //Zone Capacity Changed
                                    GlobalData.Current.HSMS.SendS6F11(310, "ZONENAME", Job.DestItem.iZoneName);
                                }
                            }
                            //2022.12.20 MCS 사양 번경 완료보고가 마지막으로 변경됨.
                            if (!string.IsNullOrEmpty(Job.CommandID) && Job.JobType == "TRANSFER")
                            {
                                if (Job.DestItem is CV_BaseModule)
                                {
                                    //포트로 간건 포트에서 작업 완료처리한다.
                                }
                                else
                                {
                                    //TransferCompleted 207
                                    GlobalData.Current.HSMS.SendS6F11(207, "JobData", Job);
                                }
                            }
                            return new Job_Result(Job, Job.CommandID, eJob_Result.Complete, "반송 동작 완료.");

                        case enumScheduleStep.RMError:
                            string ErrorMsg = string.Format("반송 동작중 에러 발생 RM:{0}   ErrorCode:{1} ", Job.AssignRMName, ErrorCode);
                            return new Job_Result(Job, Job.CommandID, eJob_Result.ErrorOccured, ErrorMsg);
                    }
                    //GlobalData.Current.McdList.UpdateItem(Job);    //220329 HHJ SCS 개발     //- McsJobManager 작업중 항목 취득
                    Thread.Sleep(CycleTime);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return new Job_Result(Job, Job.CommandID, eJob_Result.Aborted, "프로세스 비정상 종료로 인한 에러 리턴 ");
            }

        }
        #region 스케줄러 세부 스텝동작

        protected override void CheckDestinationAction(McsJob Job)
        {
            bool bRealFire = false;
            eCarrierSize CarrierWidth = eCarrierSize.Unknown;
            //241018 HoN 화재시나리오 운영 추가       //6. 크레인 운영 추가
            while (true)
            {
                if (Job.AssignedRM.CarrierExistSensor && Job.AssignedRM.InSlotCarrier == null) //240926 RGJ 화물 감지 상태인데 어떠한 이유든 크레인에 데이터가 없다면
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} Carrier :{1} Source: {2} => Dest:{3} Crane's Carrier Data  have lost.Check or Insert CarrierData!", Job.CommandID, Job.CarrierID, Job.Source, Job.Destination);
                    if (CarrierStorage.Instance.CarrierContain(Job.CarrierID)) //스토리지에 남아있으면 복구 시도
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} Carrier :{1} Source: {2} => Dest:{3} Try to update set Carrierdata on Crane", Job.CommandID, Job.CarrierID, Job.Source, Job.Destination);
                        Job.AssignedRM.UpdateCarrier(Job.CarrierID);
                        continue;
                    }
                    else //없으면 복구할 방법이 없음. 어떤 이유에서든 삭제된 상태 -> 알람
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} Carrier :{1} Source: {2} => Dest:{3} Carrier data lost in Storage! Go Alarm!", Job.CommandID, Job.CarrierID, Job.Source, Job.Destination);
                        GlobalData.Current.Alarm_Manager.AlarmOccurbyName("CRANE CARRIER DATA LOST", Job.AssignRMName);
                        Thread.Sleep(1000);
                        return;
                    }
                }

                if (!Job.AssignedRM.CarrierExistSensor && Job.AssignedRM.InSlotCarrier != null) //241004 RGJ 화물 감지 안되는데 어떠한 이유든 크레인에 데이터가 있다면
                {
                    if (CarrierStorage.Instance.CarrierContain(Job.CarrierID)) //스토리지에 남아있으면 실물이 어디있는지 알수 없으므로 알람.
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} Carrier :{1} Source: {2} => Dest:{3} Crane has carrier data  but not sensing.Go Alarm!", Job.CommandID, Job.CarrierID, Job.Source, Job.Destination);
                        GlobalData.Current.Alarm_Manager.AlarmOccurbyName("CRANE CARRIER SENSOR DATA MISMATCH", Job.AssignRMName);
                        Thread.Sleep(1000);
                        return;
                    }
                    else //스토리지에 없으면 리셋.
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} Carrier :{1} Source: {2} => Dest:{3} Crane has carrier data  but not sensing.Carrier was not found in Storage. Reset Data", Job.CommandID, Job.CarrierID, Job.Source, Job.Destination);
                        Job.AssignedRM.ResetCarrierData();
                    }
                }


                {
                    if (Job.DestItem is RMModuleBase) //목적지가 Crane 이면 여기서 완료처리한다. 230722 RGJ  화재 작업우선이므로 위치 이동함.
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} Carrier :{1} Source: {2} => Dest:{3} The destination is Crane, so we will complete it.", Job.CommandID, Job.CarrierID, Job.Source, Job.Destination);
                        Job.JobCarrierItem.CarrierState = eCarrierState.COMPLETED;
                        Job.TCStatus = eTCState.NONE;
                        Job.Step = enumScheduleStep.CraneJobComplete;
                        GlobalData.Current.HSMS.SendS6F11(702, "JOBDATA", Job); //Report CraneIdle
                        break;
                    }
                    //목적지 상태이상시 대체 쉘프로 임시 반송한다.
                    if (Job.DestItem.CheckPutAble() == false)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} Carrier :{1} Source: {2} => Dest:{3} Destination status abnormal. Returns to an alternate Shelf.", Job.CommandID, Job.CarrierID, Job.Source, Job.Destination);
                        CarrierWidth = Job.JobCarrierItem.CarrierSize;
                        var TargetShelfs = ShelfManager.Instance.AllData.Where(s =>
                                           s.CheckCarrierSizeAcceptable(CarrierWidth) &&
                                           s.ShelfAvailable &&
                                           !s.CheckCarrierExist() &&
                                           !s.Scheduled &&
                                           !s.FireSensorValue); //Put 가능한 쉘프를 추려낸다.

                        if (TargetShelfs.Count() <= 0) //Put 가능한 쉘프가 없다. 이러면 무조건 목적지 정상화를 기다리거나 가용쉘프를 기다려야한다.
                        {
                            Thread.Sleep(CycleTime);
                            continue;
                        }
                        ShelfItem AlterDestShelf = TargetShelfs.OrderBy(s => s.CalcDistance(s, Job.SourceItem)).FirstOrDefault(); //소스에서 가장 가까운 쉘프로 보낸다.
                        AlterDestShelf.NotifyScheduled(true);
                        Job.AltShelfDestination = AlterDestShelf.TagName;
                        Job.SubJob = eSubJobType.AlterStore;
                        Job.Step = enumScheduleStep.RMMoveAssign;
                        break;
                    }
                    else //상태 정상
                    {
                        Job.Step = enumScheduleStep.RMMoveAssign;
                        break;
                    }
                }
            }
        }

        private void RMMoveAssignAction(McsJob Job)
        {
            lock (SecureAreaLock)
            {
                if (Job.MCSJobAbortReq) //Move 동작전 JobAbort 내려왔으면 작업 취소한다.
                {
                    Job.TCStatus = eTCState.ABORTING;
                    Job.Step = enumScheduleStep.JobAbortComplete;
                    return;
                }
                ICarrierStoreAble MoveTarget = null;
                CraneCommand mCmd = null;
                if (Job.SubJob == eSubJobType.AlterStore) //대체 보관 작업 목표 변경.
                {
                    if (Job.AssignedRM.CarrierExistSensor) //캐리어를 들고 있다면 
                    {
                        MoveTarget = Job.AlterShelfItem; //대체 보관지로 간다.
                    }
                    else //캐리어가 없다면 대체 보관지에서 꺼내는 작업
                    {
                        MoveTarget = Job.CarrierLocationItem;
                    }
                }
                else if (Job.SubJob == eSubJobType.Push)
                {
                    MoveTarget = Job.DestItem;
                }
     
                else
                {
                    if (Job.AssignedRM.CarrierExistSensor || Job.CommandType == "MOVE") // 이동동작은 항상 목적지로 가야함.
                    {
                        MoveTarget = Job.DestItem;
                    }
                    else
                    {
                        MoveTarget = Job.SourceItem;
                    }
                }
                if (MoveTarget == null) //혹시 목적지가 날아간 경우를 대비해서 추가함
                {
                    Job.Step = enumScheduleStep.CheckDestination;
                    return;
                }

                eReserveAreaResult SResult = SecureAreaForSimultaneous(Job.AssignedRM, MoveTarget.iBay);
                switch (SResult)
                {
                    case eReserveAreaResult.ReserveOK: //이동 가능
                        if (MoveTarget.iLevel == Job.AssignedRM.iLevel && MoveTarget.iBay == Job.AssignedRM.iBay)
                        {
                            //220803 조숭진
                            if (MoveTarget == Job.SourceItem ||
                                Job.PreviousStep == enumScheduleStep.RMError ||
                                (MoveTarget is ShelfItem s && s.CarrierState == eCarrierState.ALTERNATE) || //RGJ 대체 보관된거 꺼내려 가는경우 
                                (MoveTarget == Job.DestItem && Job.SourceItem is RMModuleBase)) //220907 RGJ CIM
                            {
                                if (Job.SubJob != eSubJobType.Push) //Push 잡 Active 보고 사양에 없음.
                                {
                                    GlobalData.Current.HSMS.SendS6F11(701, "JOBDATA", Job); //Report CraneActive
                                }
                            }
                            Job.LastActionTarget = MoveTarget;
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} Carrier :{1} Source: {2} => Dest:{3} The current location is the destination, so the move is complete.", Job.CommandID, Job.CarrierID, Job.Source, Job.Destination);
                            Job.Step = enumScheduleStep.RMMoveCompleteWait;
                            AlreadyMoveComp = true;         //move assign되었으나 명령을 주지 않아 move comp를 못빠져나가니 해당 flag로 대체한다.
                        }
                        else
                        {
                            mCmd = new CraneCommand(Job.CommandID, Job.AssignRMName, eCraneCommand.MOVE, enumCraneTarget.SHELF, MoveTarget, Job.CarrierID);
                            if (!Job.AssignedRM.CheckRMCommandExist())
                            {
                                Job.LastActionTarget = MoveTarget;
                                Job.AssignedRM.SetCraneCommand(mCmd);

                                //220803 조숭진
                                if (MoveTarget == Job.SourceItem ||
                                    Job.PreviousStep == enumScheduleStep.RMError ||
                                    (MoveTarget is ShelfItem s && s.CarrierState == eCarrierState.ALTERNATE) || //RGJ 대체 보관된거 꺼내려 가는경우 
                                    (MoveTarget == Job.DestItem && Job.SourceItem is RMModuleBase)) //220907 RGJ CIM
                                {
                                    GlobalData.Current.HSMS.SendS6F11(701, "JOBDATA", Job); //Report CraneActive
                                }
                                Job.Step = enumScheduleStep.RMMoveCompleteWait;
                            }
                        }
                        break;
                    default:
                        break;
                }
                //20230215 RGJ 사양상 ACTIVE 보고 먼저 해야함 아래로 옮김
                //220803 조숭진 크레인 get완료 후 transferring 보고 
                if (Job.AssignedRM.CarrierExistSensor && Job.NeedTransferringReport && Job.SourceItem is RMModuleBase) //소스가 RM일때 Transferring 을 무빙 동작시 올려준다.
                {
                    GlobalData.Current.HSMS.SendS6F11(307, "JOBDATA", Job);
                    Job.NeedTransferringReport = false;
                }
            }
        }
        private void RMMoveWaitAction(McsJob Job)
        {
            bool bDestPosition = false;
            bool bRMMoveDone = false;
            while (true)
            {
                if (Job.LastActionTarget == null)
                {
                    Job.Step = enumScheduleStep.RMMoveAssign;
                    return;
                }

                if (Job.AssignedRM.CheckModuleHeavyAlarmExist()) //알람 발생 체크
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} CarrierID: {1} RMMoveWaitAction Error Case Enter", Job.CommandID, Job.CarrierID);
                    Job.TCStatus = eTCState.PAUSED;
                    Job.JobResult = eJobResultCode.OTHER_ERROR;
                    Job.Step = enumScheduleStep.RMError;
                    return;
                }

                if (!Job.AssignedRM.CheckRMAutoMode()) //231020 RGJ 작업 완료 대기 도중 크레인 오토 상태가 풀렸다.
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} CarrierID: {1} RMMoveWaitAction Crane Auto Off Case Enter", Job.CommandID, Job.CarrierID);
                    Job.TCStatus = eTCState.PAUSED;
                    Job.JobResult = eJobResultCode.OTHER_ERROR;
                    Job.Step = enumScheduleStep.RMError;
                    return;
                }

                //기존 번거로운 조건식 모두 삭제 마지막 Move 명령 내린곳을 저장해두었다가 비교만 해본다.
                if (Job.LastActionTarget is CV_BaseModule)
                {
                    bDestPosition = (Job.AssignedRM.PLC_RM_Current_WorkPlace == Job.AssignedRM.PC_DestWorkPlace_FORK1) ||
                        ((Job.AssignedRM.Robot_BAY == Job.LastActionTarget.iBay.ToString()) &&
                        (Job.AssignedRM.Robot_LEVEL == Job.LastActionTarget.iLevel.ToString()));
                }
                else
                {
                    bDestPosition =
                        (Job.AssignedRM.Robot_BAY == Job.LastActionTarget.iBay.ToString()) &&
                        (Job.AssignedRM.Robot_LEVEL == Job.LastActionTarget.iLevel.ToString());
                }

                bRMMoveDone = !Job.AssignedRM.CheckRMCommandExist();

                //move assign에서 명령을 주지 않고, 현지 포지션이 목적지와 같으면 alreadymovecomp로 대체한다.
                if (bDestPosition && (bRMMoveDone || AlreadyMoveComp))
                {
                    #region 삭제됨 - 화재 작업 처리
                    //화재 작업 완료 했다면 수조로 갔는지 체크해본다.
                    //if (bRealFireProcess)
                    //{
                    //    Job.AssignedRM.NotifyFireCommand(false); //작업 완료 되었으면 화재 상태 OFF
                    //    bool bCarrierExist = Job.AssignedRM.CarrierExistSensor;
                    //    bool bWaterPut = Job.AssignedRM.CheckWaterPoolPutComplete();
                    //    if (!bCarrierExist && bWaterPut) //센서에 재하 감지 안되고 방화 수조 Put 완료[정상]
                    //    {
                    //        LogManager.WriteConsoleLog(eLogLevel.Info, "Job : {0} 방화수조 Put 완료. 작업 완료처리", Job.CommandID);
                    //    }
                    //    else if (!bCarrierExist && !bWaterPut)  //센서에 재하 감지 안됬는데 수조에 넣은것도 아니고 어디로 간건지는 모름. 완료 처리함 [비정상]
                    //    {
                    //        LogManager.WriteConsoleLog(eLogLevel.Info, "Job : {0} 방화수조 Put은 아니나 재하감지 Off 이므로.작업 완료처리", Job.CommandID);

                    //    }
                    //    else if (bCarrierExist && bWaterPut) //센서에 재하 감지 되었는데 수조로 넣었음 ?? 일단 포트로 보냄 [비정상]
                    //    {
                    //        LogManager.WriteConsoleLog(eLogLevel.Info, "Job : {0} 방화수조로 넣었으나 재하감지 On 상태이므로 목적지로 Put ", Job.CommandID);
                    //    }
                    //    else //센서에 재하 감지 되고 수조에 안넣었음 포트로 감  [정상]
                    //    {
                    //        LogManager.WriteConsoleLog(eLogLevel.Info, "Job : {0} 방화수조 Put 안했으니 재하감지 On 상태이므로 목적지로 Put ", Job.CommandID);
                    //    }
                    //    if (!bCarrierExist) //사실상 재하감지 하나로 동작함.없으면 완료처리
                    //    {
                    //        CarrierItem CItem = Job.JobCarrierItem;
                    //        Job.AssignedRM.RemoveSCSCarrierData();
                    //        //사양 변경으로 CarrierWaitOut 보고 추가.
                    //        var WaterPorts = GlobalData.Current.PortManager.AllCVList.Where(p => p.CVModuleType == eCVType.WaterPool);
                    //        CV_BaseModule PutPort = WaterPorts.OrderBy(p => p.CalcDistance(p, Job.AssignedRM)).FirstOrDefault(); //현재 크레인위치에서 가장 가까운 방화수조로 추정한다. 
                    //        GlobalData.Current.HSMS.SendS6F11(309, "CarrierItem", Job.JobCarrierItem, "JobData", Job, "PORT", PutPort); //방화수조 캐리어 WaitOut
                    //        Job.TCStatus = eTCState.NONE;
                    //        Job.Step = enumScheduleStep.CraneJobComplete;
                    //        break;
                    //    }
                    //}
                    #endregion

                    //alreadymovecomp 초기화
                    if (AlreadyMoveComp)
                    {
                        AlreadyMoveComp = false;
                    }

                    //이전에 에러가 발생하여 jobresult가 success가 아니였으나 클리어해서 완료됐다면 jobresult를 success로 변경한다.
                    if (Job.JobResult != eJobResultCode.SUCCESS)
                    {
                        Job.JobResult = eJobResultCode.SUCCESS;
                    }


                    Job.JobWithDrawRequest = false;
                    if (Job.JobType == "TRANSFER")
                    {
                        //if (Job.JobWithDrawRequest) //양보동작이었다면 //싱글 필요없음 
                        //{
                        //    Job.JobWithDrawRequest = false;
                        //    Job.Step = enumScheduleStep.RMMoveAssign; //다시 RM 이동을 시도한다.
                        //    break;
                        //}
                        
                        if (Job.AssignedRM.CarrierExistSensor) //적재물 있으면 Put
                        {
                            if (Job.AssignedRM.CheckForkInFire() &&
                                !Job.AssignedRM.CheckPLCFireCommand() &&
                                !Job.AssignedRM.SuddenlyFire)
                            {
                                
                            }

                            Job.Step = enumScheduleStep.RMPutAssign; //랙마스터로 명령을 넣는다.
                        }
                        else
                        {
                            Job.Step = enumScheduleStep.RMGetAssign; //랙마스터로 명령을 넣는다.
                        }
                        break;
                    }
                    else if (Job.JobType == "MOVE")
                    {
                        Job.TCStatus = eTCState.NONE;
                        Job.Step = enumScheduleStep.CraneJobComplete;
                        GlobalData.Current.HSMS.SendS6F11(702, "JOBDATA", Job); //Report CraneIdle
                        break;
                    }

                }


                Thread.Sleep(CycleTime);
            }
        }

        private void RMGetAssignAction(McsJob Job)
        {
            ICarrierStoreAble GetSourceItem; //Get 할 목적지 

            if (Job.MCSJobAbortReq) //Get 동작전 JobAbort 내려왔으면 작업 취소한다.
            {
                Job.TCStatus = eTCState.ABORTING;
                Job.Step = enumScheduleStep.JobAbortComplete;
                return;
            }
            

            if (!Job.AssignedRM.CarrierExistSensor && Job.AssignedRM.InSlotCarrier != null) //241004 RGJ 화물 감지 안되는데 어떠한 이유든 크레인에 데이터가 있다면
            {
                if (CarrierStorage.Instance.CarrierContain(Job.CarrierID)) //스토리지에 남아있으면 실물이 어디있는지 알수 없으므로 알람.
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} Carrier :{1} Source: {2} => Dest:{3} Crane has carrier data  but not sensing.Go Alarm!", Job.CommandID, Job.CarrierID, Job.Source, Job.Destination);
                    GlobalData.Current.Alarm_Manager.AlarmOccurbyName("CRANE CARRIER SENSOR DATA MISMATCH", Job.AssignRMName);
                    Thread.Sleep(1000);
                    return;
                }
                else //스토리지에 없으면 리셋.
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} Carrier :{1} Source: {2} => Dest:{3} Crane has carrier data  but not sensing.Carrier was not found in Storage. Reset Data", Job.CommandID, Job.CarrierID, Job.Source, Job.Destination);
                    Job.AssignedRM.ResetCarrierData();
                }
            }

            GetSourceItem = Job.CarrierLocationItem; //어떤 작업이든  GET 은 캐리어가 있는곳에서 한다.

            enumCraneTarget GTarget = enumCraneTarget.None;
            if (GetSourceItem is ShelfItem)
            {
                GTarget = enumCraneTarget.SHELF;
            }
            else if (GetSourceItem is CV_BaseModule)
            {
                GTarget = enumCraneTarget.PORT;
            }

            //작업 내리기전 다시 한번 RM 위치 체크
            if (Job.AssignedRM.CalcDistance(Job.AssignedRM, GetSourceItem) > 0)
            {
                bool bDestPosition = false;
                    //230301 추가 수정 s
                if (GetSourceItem is CV_BaseModule)
                {
                    bDestPosition =
                    (Job.AssignedRM.Robot_BAY == GetSourceItem.iBay.ToString() && Job.AssignedRM.Robot_LEVEL == GetSourceItem.iLevel.ToString())
                    || Job.AssignedRM.PLC_RM_Current_WorkPlace == Job.AssignedRM.PC_DestWorkPlace_FORK1;
                }
                else
                {
                    bDestPosition =
                    Job.AssignedRM.Robot_BAY == GetSourceItem.iBay.ToString() && Job.AssignedRM.Robot_LEVEL == GetSourceItem.iLevel.ToString();
                }
                if(!bDestPosition)
                {
                    Job.Step = enumScheduleStep.RMMoveAssign; //위치가 안맞으면 다시 이동시킨다.
                    return;
                }

            }
            
            if (Job.DestItem is CV_BaseModule) //포트로 가는 반송 작업은 GET 하기전 다시 체크한다.
            {
                
            }

            if (GetSourceItem.CheckGetAble(Job.CarrierID) == false) //GET 명령 내리기전에 소스 상태 다시 체크
            {
                if (Job.SourceItem is CV_BaseModule) // [230503 CIM 검수]
                {
                    Job.JobSourcePortError = true;
                }
                Job.TCStatus = eTCState.PAUSED;
                Job.Step = enumScheduleStep.JobPause;
                GlobalData.Current.HSMS.SendS6F11(702, "JOBDATA", Job); //Report CraneIdle
                return;
            }

            CraneCommand gCmd = new CraneCommand(Job.CommandID, Job.AssignRMName, eCraneCommand.PICKUP, GTarget, GetSourceItem, Job.CarrierID);
            if (!Job.AssignedRM.CheckRMCommandExist())
            {
                Job.LastActionTarget = GetSourceItem;
                Job.AssignedRM.SetCraneCommand(gCmd);
                Job.Step = enumScheduleStep.RMGetCompleteWait;
            }
        }

        private void RMGetWaitAction(McsJob Job)
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} CarrierID: {1} RMGetWaitAction Enter", Job.CommandID, Job.CarrierID);
            //220802 조숭진 조건 추가
            if (Job.PreviousStep == enumScheduleStep.RMError)
                GlobalData.Current.HSMS.SendS6F11(307, "JobData", Job);

            if (Job.JobCarrierItem != null)
            {
                if (Job.JobCarrierItem.CarrierState == eCarrierState.ALTERNATE) //대체 보관 상태였다면 Resume 보고한다.
                {
                    GlobalData.Current.HSMS.SendS6F11(304, "JobData", Job);
                }
                Job.JobCarrierItem.CarrierState = eCarrierState.TRANSFERRING;
            }
            //GET 완료 판정
            //적재물이 있고 소스 위치면 GET완료 처리
            while (true)
            {
                if (Job.LastActionTarget == null)
                {
                    Job.Step = enumScheduleStep.RMGetAssign;
                    return;
                }

                if (Job.MCSJobAbortReq) //Get 동작중 JobAbort 내려왔으면 작업 취소 불가.
                {
                    //LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} CarrierID: {1} Abort 명령내려왔지만 Get 동작중 중단 불가능합니다.", Job.CommandID, Job.CarrierID);
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} CarrierID: {1} Host/Operator sent abort command but during get operation abort failed. ", Job.CommandID, Job.CarrierID);
                    GlobalData.Current.HSMS.SendS6F11(202, "JOBDATA", Job, "COMMANDID", Job.CommandID);
                    Job.SetJobAbort(false); //보고후 Abort 해제
                }

                //TimeOut Check 추가
                bool bCommandBusy = Job.AssignedRM.CheckRMCommandExist();
                bool bCarrierExist = Job.AssignedRM.CarrierExistSensor;
                bool bSourcePosition = true; //Get 동작에서 완료후 위치 체크 필요없음.

                if (Job.AssignedRM.CheckEmptyRetriveState()) //공출고 발생
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} CarrierID: {1} RMGetWaitAction Source Empty Case Enter", Job.CommandID, Job.CarrierID);

                    Job.JobResult = eJobResultCode.EMPTY_RETRIEVAL;
                    Job.Step = enumScheduleStep.ErrorEmptyRetrieve;
                    
                    return;
                }
                if(Job.SourceItem is CV_BaseModule SourcePort && Job.AssignedRM.CheckPortInterfaceErrorState()) //Port I/F Error 처리
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} Source :{1} CarrierID: {2} RMGetWaitAction Source IF Error Case Enter", Job.CommandID, SourcePort.ModuleName, Job.CarrierID);
                    //포트로 알람 코드를 넣는다.
                    if(SourcePort != null)
                    {
                        SourcePort.PC_ErrorCode = short.Parse(GlobalData.PORT_IF_ALARM_CODE);
                    }
                    SourcePort.NotifyCraneErrorOccurred(Job.AssignedRM, Job);//포트에 크레인 알람이 났음을 알림.
                    Thread.Sleep(1000); //알람 보고 올릴 시간 대기
                    Job.JobResult = eJobResultCode.PORT_NG;
                    Job.Step = enumScheduleStep.ErrorPortIF;
                    return;
                }
                if (Job.AssignedRM.CheckModuleHeavyAlarmExist()) //알람 발생 체크
                {
                    if(Job.SourceItem is CV_BaseModule SourceCV)
                    {
                        SourceCV.NotifyCraneErrorOccurred(Job.AssignedRM, Job);//포트에 크레인 알람이 났음을 알림.
                    }
                    ProcesstGetFailed(Job);
                    return;
                }
                if (!Job.AssignedRM.CheckRMAutoMode()) //231020 RGJ 작업 완료 대기 도중 크레인 오토 상태가 풀렸다.
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} CarrierID: {1} RMGetWaitAction Crane Auto Off Case Enter", Job.CommandID, Job.CarrierID);
                    if (Job.SourceItem is CV_BaseModule SourceCV) //240604 RGJ 크레인 GET 작업 도중에 오토 모드 해제도 포트에 알려줘야함.
                    {
                        SourceCV.NotifyCraneErrorOccurred(Job.AssignedRM, Job);//포트에 크레인 알람이 났음을 알림.
                    }
                    ProcesstGetFailed(Job);
                    return;
                }

                if (!bCommandBusy && bCarrierExist && bSourcePosition)
                {
                    //220803 조숭진 크레인 get완료 후 transferring 보고
                    if (Job.AssignedRM.CarrierExistSensor && Job.NeedTransferringReport)
                    {
                        GlobalData.Current.HSMS.SendS6F11(307, "JOBDATA", Job);
                        Job.NeedTransferringReport = false;
                    }
                    Job.LastActionTarget.NotifyScheduled(false);  //작업 예약 해제
                    if (Job.SubJob == eSubJobType.AlterStore) //대체 작업 이었다면 GET 완료후 대체 작업 해제
                    {
                        Job.SubJob = eSubJobType.None;
                    }

                    //이전에 에러가 발생하여 jobresult가 success가 아니였으나 클리어해서 완료됐다면 jobresult를 success로 변경한다.
                    if (Job.JobResult != eJobResultCode.SUCCESS)
                    {
                        Job.JobResult = eJobResultCode.SUCCESS;
                    }

                    if (Job.AssignedRM.CheckForkInFire() &&
                        !Job.AssignedRM.CheckPLCFireCommand() &&
                        !Job.AssignedRM.SuddenlyFire)
                    {
                        
                    }
                    //230819 RGJ Get 동작을 완료했으나 캐리어 데이터를 전달 받지 못함.스케쥴러가 기다렸다가 강제로 데이터 전송함.
                    if (Job.AssignedRM.InSlotCarrier == null)
                    {
                        Thread.Sleep(1000); //조금만 더 기다려 본다.
                        if (Job.AssignedRM.InSlotCarrier == null)
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} Source :{1} RMGetWaitAction data is not transferred.Scheduler will force transferring", Job.CommandID, Job.LastActionTarget.iLocName);
                            if (Job.LastActionTarget != null)
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} Source :{1} => {2} RMGetWaitAction Data has been transferred", Job.CommandID, Job.LastActionTarget.iLocName, Job.AssignedRM.ModuleName);
                                Job.LastActionTarget.ResetCarrierData();
                                Job.AssignedRM.UpdateCarrier(Job.CarrierID);
                            }
                        }
                    }
                    Job.AssignedRM.CraneCarrierInstallTime = DateTime.Now;      //6. 크레인 운영 추가        //6.1) 화재 쉘프 크레인 취출 후 60초 대기
                    Job.Step = enumScheduleStep.CheckDestination;
                    break;
                }
                Thread.Sleep(CycleTime);
            }
        }

        private void RMPutAssignAction(McsJob Job)
        {
            if (Job.MCSJobAbortReq) //Put 동작전 JobAbort 내려왔으면 작업 취소한다.
            {
                Job.TCStatus = eTCState.ABORTING;
                Job.Step = enumScheduleStep.JobAbortComplete;
                return;
            }
            CraneCommand pCmd = null;
            enumCraneTarget PTarget = enumCraneTarget.None;
            eCarrierSize CarrierWidth = eCarrierSize.Unknown;

            if (Job.AssignedRM.CarrierExistSensor && Job.AssignedRM.InSlotCarrier == null) //240926 RGJ 화물 감지 상태인데 어떠한 이유든 크레인에 데이터가 없다면
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} CarrierID: {1} RMPutAssignAction But Carrier Data Lost.Change Step => CheckDestination", Job.CommandID, Job.CarrierID);
                Job.Step = enumScheduleStep.CheckDestination;
                return;
            }

            if (Job.SubJob == eSubJobType.AlterStore) //대체 보관 작업이면 대체 쉘프로 목적지 변경.
            {
                PTarget = enumCraneTarget.SHELF;
                pCmd = new CraneCommand(Job.CommandID, Job.AssignRMName, eCraneCommand.UNLOAD, PTarget, Job.AlterShelfItem, Job.CarrierID);
                if (!Job.AssignedRM.CheckRMCommandExist())
                {
                    Job.NeedTransferringReport = true; //대체 보관 명령을 들어간후에는 다시 보고가 필요하므로 플래그 초기화
                    Job.LastActionTarget = Job.AlterShelfItem;
                    Job.AssignedRM.SetCraneCommand(pCmd);
                    Job.Step = enumScheduleStep.RMPutCompleteWait;
                }

            }
            else
            {
                //Put 명령 내리기전에 상태 정상이 아니면 대체 보관 로직으로 전환
                if (Job.DestItem.CheckPutAble() == false)
                {
                   
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} Carrier :{1} Source: {2} => Dest:{3} 목적지 상태 이상으로 대체 쉘프로 반송합니다.", Job.CommandID, Job.CarrierID, Job.Source, Job.Destination);
                        CarrierWidth = Job.JobCarrierItem.CarrierSize;
                        var TargetShelfs = ShelfManager.Instance.AllData.Where(s =>
                                          s.CheckCarrierSizeAcceptable(CarrierWidth) &&
                                          s.ShelfAvailable &&
                                          !s.CheckCarrierExist() &&
                                          !s.Scheduled &&
                                          !s.FireSensorValue); //Put 가능한 쉘프를 추려낸다.

                        if (TargetShelfs.Count() <= 0) //Put 가능한 쉘프가 없다. 이러면 무조건 목적지 정상화를 기다리거나 가용쉘프를 기다려야한다.
                        {
                            Thread.Sleep(CycleTime);
                            return;
                        }
                        ShelfItem AlterDestShelf = TargetShelfs.OrderBy(s => s.CalcDistance(s, Job.DestItem)).FirstOrDefault(); //소스에서 가장 가까운 쉘프로 보낸다.
                        AlterDestShelf.NotifyScheduled(true);
                        Job.AltShelfDestination = AlterDestShelf.TagName;
                        Job.SubJob = eSubJobType.AlterStore;
                        Job.Step = enumScheduleStep.RMMoveAssign;
                    }
                }
                else
                {
                    if (Job.DestItem is ShelfItem)
                    {
                        PTarget = enumCraneTarget.SHELF;
                    }
                    else
                    {
                        PTarget = enumCraneTarget.PORT;
                    }
                    //작업 내리기전 다시 한번 RM 위치 체크
                    if (Job.AssignedRM.CalcDistance(Job.AssignedRM, Job.DestItem) > 0)
                    {
                        if(Job.DestItem is CV_BaseModule cv )
                        {
                            if(Job.AssignedRM.PC_DestWorkPlace_FORK1 != cv.iWorkPlaceNumber)
                            {
                                Job.Step = enumScheduleStep.RMMoveAssign;//위치가 안맞으면 다시 이동시킨다.
                                return;
                            }
                        }
                        else
                        {
                            Job.Step = enumScheduleStep.RMMoveAssign;//위치가 안맞으면 다시 이동시킨다.
                            return;
                        }
                    }
                    pCmd = new CraneCommand(Job.CommandID, Job.AssignRMName, eCraneCommand.UNLOAD, PTarget, Job.DestItem, Job.CarrierID);
                    if (!Job.AssignedRM.CheckRMCommandExist())
                    {
                        //231012 RGJ 다시 작업 다 하고 쓰는걸로 원복함.
                        //if (Job.DestItem is CV_BaseModule cv) //231002 RGJ Write Tracking 시점 변경 Robot 이 Unload 내리기전에 준다.
                        //{
                        //    LogManager.WriteConsoleLog(eLogLevel.Info, "Port Put Data Write  - PortName :{0} CarrierId : {1}", cv.ModuleName, Job.CarrierID);
                        //    cv.WriteTrackingData(Job.JobCarrierItem); //포트로 Carrier 데이터 Write
                        //}
                        Job.LastActionTarget = Job.DestItem;
                        Job.AssignedRM.SetCraneCommand(pCmd);
                        Job.Step = enumScheduleStep.RMPutCompleteWait;
                    }
                }
            }
        }

        private void RMPutWaitAction(McsJob Job)
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} CarrierID: {1} RMPutWaitAction Enter", Job.CommandID, Job.CarrierID);
            //220802 조숭진 조건 추가
            //Transferring CEID 307 Report
            if (Job.PreviousStep == enumScheduleStep.RMError)
                GlobalData.Current.HSMS.SendS6F11(307, "JobData", Job);

            //PUT 완료 판정
            //적재물이 없고 목표 위치면 PUT 완료 처리
            while (true)
            {
                if (Job.LastActionTarget == null)
                {
                    Job.Step = enumScheduleStep.RMPutAssign;
                    return;
                }

                if (Job.MCSJobAbortReq) //Put 동작중 JobAbort 내려왔으면 작업 취소 불가.
                {
                    //LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} CarrierID: {1} Abort 명령내려왔지만 Put 동작중 중단 불가능합니다.", Job.CommandID, Job.CarrierID);
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} CarrierID: {1} Host/Operator sent abort command but during put operation abort failed. ", Job.CommandID, Job.CarrierID);
                    GlobalData.Current.HSMS.SendS6F11(202, "JOBDATA", Job, "COMMANDID", Job.CommandID);
                    Job.SetJobAbort(false); //보고후 Abort 해제
                }
                //TimeOut Check 추가
                bool bCommandBusy = Job.AssignedRM.CheckRMCommandExist();
                bool bCarrierExist = Job.AssignedRM.CarrierExistSensor;
                bool bDestPosition = true; //Put 동작에서 완료후 위치 체크 필요없음.
             
                //더블 스토리지 케이스 처리
                if (Job.AssignedRM.CheckDoubleStorageState())
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} CarrierID: {1} Double Storage Case Enter", Job.CommandID, Job.CarrierID);

                    //GlobalData.Current.Alarm_Manager.AlarmOccur(GlobalData.DOUBLE_STORAGE_ALARM_CODE, Job.AssignRMName); //241029 RGJ TPLC 에서 알람발생함. 스케쥴러랑 동시에 알람 내면 문제가 될가능성이 있어서 삭제함.

                    #region DS UNK 캐리어 생성.
                    ShelfItem TargetShelf = Job.LastActionTarget as ShelfItem;
                    //해당 위치에 UNK 캐리어 생성  
                    if (TargetShelf != null && !TargetShelf.CheckCarrierExist())
                    {
                        //목적지에 Unknown Carrier 추가.
                        CarrierItem UNKCarrier = CarrierStorage.Instance.CreateDSUnknownCarrier(TargetShelf.TagName, TargetShelf.CarrierID, eCarrierSize.Unknown); //사이즈는 알수 없다. //생성만 시키고 나머지 데이터 입력은 작업자가 처리해야한다.
                        bool Installed = CarrierStorage.Instance.InsertCarrier(UNKCarrier);
                        TargetShelf.UpdateCarrier(UNKCarrier.CarrierID);
                        if (Installed)
                        {
                            //S6F11 CarrierInstallCompleted 301
                            GlobalData.Current.HSMS.SendS6F11(301, "JOBDATA", Job, "SHELFITEM", TargetShelf);
                            //S6F11 ZoneCapacityChange CEID 310
                            GlobalData.Current.HSMS.SendS6F11(310, "ZONENAME", TargetShelf.iZoneName);
                        }
                    }
                    TargetShelf.NotifyScheduled(false);
                    #endregion

                    Job.TCStatus = eTCState.PAUSED;
                    Job.JobResult = eJobResultCode.DOUBLE_STORAGE;
                    Job.Step = enumScheduleStep.ErrorDoubleStorage; //에러 핸들링
                    return;
                }
                if (Job.AssignedRM.CheckModuleHeavyAlarmExist()) //알람 발생 체크
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} CarrierID: {1} RMPutWaitAction Error Case Enter", Job.CommandID, Job.CarrierID);
                    ProcesstPutFailed(Job);
                    return;
                }

                if (!Job.AssignedRM.CheckRMAutoMode()) //231020 RGJ 작업 완료 대기 도중 크레인 오토 상태가 풀렸다.
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} CarrierID: {1} RMPutWaitAction Crane Auto Off Case Enter", Job.CommandID, Job.CarrierID);
                    ProcesstPutFailed(Job);
                    return;
                }

                if (!bCommandBusy && !bCarrierExist && bDestPosition)
                {
                    if(Job.DestItem is CV_BaseModule == false) //포트로 가는건 아직 반송이 종료된게 아님
                    {
                        Job.TCStatus = eTCState.NONE;
                    }
                    Job.Step = enumScheduleStep.CraneJobComplete;

                    //이전에 에러가 발생하여 jobresult가 success가 아니였으나 클리어해서 완료됐다면 jobresult를 success로 변경한다.
                    if (Job.JobResult != eJobResultCode.SUCCESS)
                    {
                        Job.JobResult = eJobResultCode.SUCCESS;
                    }

                    if(Job.prevDestItem != null)
                    {
                        Job.prevDestItem.NotifyScheduled(false);
                    }

                    Job.DestItem.NotifyScheduled(false); //작업 예약 해제
                    GlobalData.Current.HSMS.SendS6F11(702, "JOBDATA", Job); //Report CraneIdle
                    break;
                }
                Thread.Sleep(CycleTime);
            }
        }
        #endregion 

        /// <summary>
        /// Crane 동시 작업을 위한 작업 공간 체크
        /// </summary>
        /// <param name="Job"></param>
        /// <returns></returns>
        private eReserveAreaResult SecureAreaForSimultaneous(RMModuleBase ActionRM, int TargetBay)
        {
            return eReserveAreaResult.ReserveOK; //SINGLE 모드    
        }

        bool CheckRMOnProcess(int RMNumber)
        {
            try
            {
                return JobList.Where(j => j.TCStatus != eTCState.QUEUED && j.AssignedRM != null && j.AssignedRM.RMNumber == RMNumber).Count() > 0;
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return true;
            }

        }


        /// <summary>
        /// 스케쥴러 초기화.
        /// </summary>
        public override void InitScheduler()
        {
            InitReserveData();
            JobRecoveryAction(); //복구 작업 수행
            Thread t = new Thread(new ThreadStart(SchedulerAutoRun));
            t.Name = "SchedulerAutoRun";
            t.IsBackground = true;
            t.Start();
        }
        private void InitReserveData()
        {
            //CurrentScheduleHeight = eTrayHeight.Height0;
            foreach (var sItem in ShelfManager.Instance.FrontData)
            {
                sItem.NotifyScheduled(false);     //231124 RGJ 디비 갱신이 되야 클라이언트도 업데이트 하기에 Init 인자 제외
                //sItem.NotifyScheduled(false, true);     //221012 조숭진 init 인자 추가...
            }
            foreach (var sItem in ShelfManager.Instance.RearData)
            {
                sItem.NotifyScheduled(false);     //231124 RGJ 디비 갱신이 되야 클라이언트도 업데이트 하기에 Init 인자 제외
                //sItem.NotifyScheduled(false, true);     //221012 조숭진 init 인자 추가...
            }
        }
        /// <summary>
        /// 메인 스케쥴링 루프
        /// </summary>
        public override void SchedulerAutoRun()
        {
            var TargetRM1 = GlobalData.Current.mRMManager[1];
            while (true)
            {
                try
                {
                    //230927 RGJ DB 접속상태가 끊어졌으면 스케쥴러 동작 홀딩 
                    if (GlobalData.Current.DBManager.IsConnect == false)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "{0} SchedulerAutoRun Holding State - DB Disconnected", SchedulerName);
                        Thread.Sleep(1000); //1초대기
                        continue;
                    }

                    //동작 가능 상태가 아니더라도 후처리 작업은 계속한다.
                    if (!StartSignal || GlobalData.Current.MainBooth.SCState != eSCState.AUTO)
                    {
                        CraneJobPostProcess();
                        Thread.Sleep(CycleTime);
                        continue;
                    }
                    if (CycleRandomJobRequest) //랜덤 사이클 잡 추가.
                    {
                        Thread.Sleep(CycleTime);
                        if (JobList.Where(j => j.TCStatus != eTCState.PAUSED).Count() == 0)
                        {
                            for (int i = 0; i < CycleRandomCreateCount; i++)
                            {
                                JobList.CreateRandomJob();
                            }
                        }
                    }
                    if (AllShelfUnloadJobRequest)
                    {
                        if (JobList.Count() < 30)
                        {
                            JobList.CreateOldestCarrierUnloadJob();
                        }
                        else
                        {
                            GlobalData.Current.Scheduler.SetAllShelfUnloadJobRequest(false);        //220603 조숭진 30개 생성 후 자동으로 flag off함.
                        }
                    }
                    if (UsePortGet) //포트상태 체크
                    {
                        //상위에서 포트 잡을 내려줘야 하지만 타임아웃 되면 스케쥴러가 직접 만든다.
                        foreach (var line in GlobalData.Current.PortManager.ModuleList)
                        {
                            foreach (var CVItem in line.Value.ModuleList)
                            {
                                if (CVItem.CVModuleType == eCVType.RobotIF &&
                                    CVItem.IsInPort &&
                                    CVItem.RobotAccessAble &&
                                    CVItem.CurrentCarrier != null &&
                                    CVItem.CurrentCarrier.CarrierState == eCarrierState.WAIT_IN &&
                                    CVItem.NextCVCommand == eCVCommand.WaitCraneGet &&
                                    CVItem.CVAvailable &&
                                    CVItem.CheckNeedPortJobCreate(WaitInNoCommandTimeOut) &&
                                    !GlobalData.Current.McdList.IsJobListCheck(CVItem.ModuleName, CVItem.GetCarrierID()))

                                {
                                    LogManager.WriteConsoleLog(eLogLevel.Info, "Port Get WaitIn TimeOut! Create OP Job PORT:{0} CarrierID:{1}", CVItem.ModuleName, CVItem.CurrentCarrier.CarrierID);
                                    JobList.CreateGetPortJob(CVItem);
                                }
                            }
                        }
                    }
                    McsJob NextJob = null;
                    if (IsRM1JobProcessing == false)
                    {
                        NextJob = GetNextMcsJob(1); //RM 1번을 위한 작업을 대기 리스트중에 고른다.
                    }

                    if (NextJob != null && !NextJob.JobNowDeleting) //삭제중인 작업 제외
                    {
                        if (RM1_OnProcessJob == NextJob) //작업을 골랐지만 이미 실행중인 작업이다.
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "NextJob : {0} Selected but OnProcessing ", NextJob.CommandID);
                        }
                        else
                        {
                            //여기서 작업을 분할해야지는 판단.
                            int RNumber = SelectRMForJob(NextJob);
                            if (RNumber > 0)
                            {
                                AssignJob(RNumber, NextJob); //작업 할당에서 동작 스타트까지
                            }
                        }
                    }
                    
                    //작업 후처리
                    CraneJobPostProcess();

                    Thread.Sleep(CycleTime);
                }
                catch (Exception ex)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                }
            }
        }
        /// <summary>
        /// 해당 작업을 어떤 RM 이 Main으로 진행할지 선정
        /// 
        ///출발지가 전용 지역이고 도착지가 공유 지역이면 출발지 전용 랙마스터가 진행
        ///출발지가 공용 지역이고 도착지가 공유 지역이면 출발지에서 가까운 랙마스터가 진행
        ///출발지가 공용 지역이고 도착지가 전용 지역이면 도착지 전용 해당 랙마스터가 진행
        ///출발지가 전용 지역이고 도착지가 전용 지역일떼 전용지역이 같은면 계산 필요없음. 전용지역이 다르다면 무조건 버퍼 전달 로직이 필요하다.
        /// </summary>
        /// <returns> RM1 =>1   RM2 =>2</returns>
        private int SelectRMForJob(McsJob sJob)
        {
            return 1; //SINGLE 모드
        }

        /// <summary>
        /// 공용지역 작업 우선 순위를 정하기 위해 동작 추정 시간을 계산해본다.
        /// 구현예정
        /// </summary>
        /// <returns></returns>
        private int CalcEstimateWorkTime()
        {
            return -1;
        }

        /// <summary>
        /// 작업의 완료 상태를 체크하고 후처리를 진행한다.
        /// </summary>
        private void CraneJobPostProcess()
        {
            #region RM1 완료 체크 및 후작업
            if (JobTaskRM1 != null)
            {
                if (JobTaskRM1.IsFaulted)       //태스크 동작중 예외발생
                {
                    //"테스크 동작중 미처리 예외가 발생하었습니다. 동작 상태를 확인하십시오.";
                    if (_RM1_OnProcessJob != null)
                    {
                        //SCWriteTransferLog(_RM1_OnProcessJob, "FAULT");
                        _RM1_OnProcessJob.TCStatus = eTCState.PAUSED;
                        _RM1_OnProcessJob.Step = enumScheduleStep.None;
                        _RM1_OnProcessJob = null;
                        JobTaskRM1 = null;
                    }
                }
                else if (JobTaskRM1.IsCanceled)     //테스크 취소 됨
                {
                    //SCWriteTransferLog(_RM1_OnProcessJob, "CANCEL");
                    //"테스크  동작이 취소 되었습니다. 동작 상태를 확인하십시오.";
                }
                else if (JobTaskRM1.IsCompleted) //태스크 완료 되었고 작업 성공
                {
                    eJob_Result JR1 = JobTaskRM1.Result.JobResult;
                    switch (JR1)
                    {
                        case eJob_Result.Complete: //완료되면 작업 목록에서 삭제
                            if (_RM1_OnProcessJob.SubJob == eSubJobType.AlterStore && !_RM1_OnProcessJob.MCSJobAbortReq) //대체 보관 작업을 완료 했다면 출발지를 재설정.
                            {
                                //SCWriteTransferLog(_RM1_OnProcessJob, "COMPLETE");
                                //_RM1_OnProcessJob.SubJob = eSubJobType.None;
                                _RM1_OnProcessJob.TCStatus = eTCState.PAUSED;

                                //_RM1_OnProcessJob.Source = _RM1_OnProcessJob.AltShelfDestination;
                                _RM1_OnProcessJob.AlterShelfItem.NotifyScheduled(true);
                                _RM1_OnProcessJob.DestItem.NotifyScheduled(true);

                                _RM1_OnProcessJob.AltShelfDestination = string.Empty;
                                _RM1_OnProcessJob.AssignedRM = null;
                                _RM1_OnProcessJob = null;
                                JobTaskRM1 = null;
                            }
                            else
                            {
                                //SCWriteTransferLog(_RM1_OnProcessJob, "COMPLETE");

                                if (_RM1_OnProcessJob.SubJob == eSubJobType.Push)
                                {
                                    GlobalData.Current.PushCounter++;
                                }
                                else
                                {
                                    GlobalData.Current.TransferCounter++;
                                }

                                if(_RM1_OnProcessJob.DestItem is CV_BaseModule == false) //컨베이어로 간건 컨베이어가 삭제처리함.
                                {
                                    //220628 조숭진 db에 들어가지 않은 job이므로 지우려하면 insert 됨.
                                    //if (_RM1_OnProcessJob.SubJob != eSubJobType.Push && _RM1_OnProcessJob.SubJob != eSubJobType.HandOver)
                                    if (JobTaskRM1.IsCompleted && _RM1_OnProcessJob.JobType != "MOVE")
                                        JobList.DeleteMcsJob(_RM1_OnProcessJob);
                                    else
                                        JobList.DeleteMcsJob(_RM1_OnProcessJob, _RM1_OnProcessJob.SubJob != eSubJobType.None);

                                }
                                else if (_RM1_OnProcessJob.SubJob == eSubJobType.None 
                                    && _RM1_OnProcessJob.DestItem is CV_BaseModule
                                    && JobTaskRM1.IsCompleted 
                                    && _RM1_OnProcessJob.JobType == "MOVE")       //자동으로 돌릴땐 괜찮지만 메뉴얼 커맨드로 컨베이어 move만 했을 때 지우기 위해....
                                {
                                    JobList.DeleteMcsJob(_RM1_OnProcessJob, _RM1_OnProcessJob.SubJob != eSubJobType.None);
                                }

                                _RM1_OnProcessJob.AssignedRM = null;
                                _RM1_OnProcessJob = null;
                                JobTaskRM1 = null;
                            }

                            break;
                        case eJob_Result.Aborted: //작업 중단 되었으면 작업 삭제하고 새로운 커맨드 대기필요
                            if (_RM1_OnProcessJob != null)
                            {
                                if (_RM1_OnProcessJob.VoidJob == false) //작업스케쥴링 도중 삭제된경우 처리 추가.
                                {
                                    //SCWriteTransferLog(_RM1_OnProcessJob, "ABORT");
                                    JobList.DeleteMcsJob(_RM1_OnProcessJob);
                                }
                                _RM1_OnProcessJob = null;
                                JobTaskRM1 = null;
                            }
                            break;
                        case eJob_Result.Paused: //작업 일시 정지 되었으면 대기
                            if (_RM1_OnProcessJob != null)
                            {
                                //SCWriteTransferLog(_RM1_OnProcessJob, "PAUSED");
                                //_RM1_OnProcessJob.AssignedRM.RemoveCraneCommand();    //우선 주석처리. 나중에 현상이 나오면 적용.      //가지고 있는 command도 버리자. _RM1_OnProcessJob가 null이 되지만 command는 남아있더라.
                                _RM1_OnProcessJob.TCStatus = eTCState.PAUSED;
                                _RM1_OnProcessJob.Step = enumScheduleStep.None;
                                _RM1_OnProcessJob = null;
                                JobTaskRM1 = null;
                            }
                            break;
                        case eJob_Result.ErrorOccured:
                            if (_RM1_OnProcessJob != null)
                            {
                                //SCWriteTransferLog(_RM1_OnProcessJob, "ERROR");
                                _RM1_OnProcessJob.AssignedRM.RemoveCraneCommand();      //가지고 있는 command도 버리자. _RM1_OnProcessJob가 null이 되지만 command는 남아있더라.
                                _RM1_OnProcessJob.TCStatus = eTCState.PAUSED;
                                _RM1_OnProcessJob.Step = enumScheduleStep.None;
                                _RM1_OnProcessJob = null;
                                JobTaskRM1 = null;
                            }
                            break;
                        case eJob_Result.TimeOut: //추후 구현
                            break;
                        default:
                            break;
                    }
                }
            }
            #endregion

            #region 가비지 작업 삭제
            var gabageJob = JobList.Where(j => j.Step == enumScheduleStep.CraneJobComplete && j.TCStatus == eTCState.NONE).FirstOrDefault();
            if (gabageJob != null)
            {
                if (gabageJob != RM1_OnProcessJob)
                {
                    if (!CarrierStorage.Instance.CarrierContain(gabageJob.CarrierID))
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "JobID:{0} 작업 완료되었으나 삭제 안된 상태이므로 작업 리스트에서 삭제.", gabageJob.CommandID);
                        JobList.DeleteMcsJob(gabageJob);
                    }
                }
            }
            #endregion
        }
        //241018 HoN 화재시나리오 운영 추가       //화재 작업 존재여부 판단 기능 추가       //SchedulerBase로 이동
        //private bool CheckFireJobExist()
        //{
        //    foreach (McsJob job in JobList)
        //    {
        //        if (job.SubJob == eSubJobType.FireExtinguish)
        //        {
        //            return true;
        //        }
        //    }
        //    return false;
        //}
        public override bool CheckJobOnProcess(string CommandID)
        {
            var Job1 = RM1_OnProcessJob?.CommandID;
            if (Job1 == CommandID)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        //220823 조숭진 config로 수정 s
        public override void SetExeclusiveRange()
        {
            return;
        }
        public override bool CheckSchedulerPaused()
        {
            bool NoJobProcess = !IsRM1JobProcessing;
            return !StartSignal && NoJobProcess;
        }

        public override List<RMModuleBase> GetNotAssignRM()
        {
            if (!IsRM1JobProcessing)
                return new List<RMModuleBase>() { GlobalData.Current.mRMManager.FirstRM };
            
            return null;
        }

    }
}
