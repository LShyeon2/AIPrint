
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
    public class DualRMScheduler : BaseScheduler
    {
        private object SecureAreaLock = new object();
        private object OPChangeLock = new object(); //OneLackMode 변경 락
        private bool AlreadyMoveComp = false;       //크레인 에러발생 혹은 메뉴얼->오토 전환 시 move를 다시 탈때 필요한 flag
        //private bool UseEmptyRetriveAutoReset = true; //공출고 발생시 자동 오류 해제 할지 설정
        // 듀얼랙마스터 스케쥴링 시퀀스
        // 1.잡 없이 대기중인 랙마스터를 찾느다.
        // 
        // 2.우선순위에 따라 랙마스터에 잡 할당. 경로 계산하여 회피기동이나 버퍼역할도 다른 랙마스터에 할당해야한다.
        private object ReserveLock = new object();
        private ReserveToken[] BayReserveArray = null;

        public int[] ScheduleReserveArray
        {
            get
            {
                int[] tempArray = new int[MaxBay + 2];
                for (int i = 0; i < MaxBay + 2; i++)
                {
                    tempArray[i] = BayReserveArray[i].ReservedRM;
                }
                return tempArray;//카피본을 보내준다.
            }

        }

        //220823 조숭진 config로 수정 s
        //private int RM1_ExclusiveBay = 0; //특정 Bay 열까지 단독으로 사용하도록 한다.일단 하드 코딩 추후 Config 로 변경
        //private int RM2_ExclusiveBay = 0;
        //private int RM_RangeMargin = 0;
        //private int MaxBay = 0;
        //private int HandOverBay = 0;
        //220823 조숭진 config로 수정 e

        private int RM_ExtendSize = 3; //RM  이 현재 위치 기준 몇 열까지 확장해서 점유하는지 저장. 

        private int FirstRMPushBay = 0;//최대로 푸시할때 어디까지 푸시할지 결정
        private int SecondRMPushBay = 0;

        private ShelfItem RM1_WithDrawShelf = null;
        private ShelfItem RM2_WithDrawShelf = null;

        Task<Job_Result> JobTaskRM1 = null;
        Task<Job_Result> JobTaskRM2 = null;

        //private int AreaDeadLockCount = 20;

        //private enumScheduleStep RM1_CurrentProcessStep = enumScheduleStep.None; //RM1 현재 잡 프로세스 실행 스텝
        //private enumScheduleStep RM2_CurrentProcessStep = enumScheduleStep.None; //RM2 현재 잡 프로세스 실행 스텝

        //220823 조숭진 config로 수정 s
        public DualRMScheduler()
        {
            string value = string.Empty;
            SchedulerName = "DualRMScheduler";

            SetUsePortGet(true);
            var RM1 = GlobalData.Current.mRMManager.FirstRM;
            var RM2 = GlobalData.Current.mRMManager.SecondRM;
            int ShelfMinBay = ShelfManager.Instance.GetMinBay();
            int ShelfMaxBay = ShelfManager.Instance.GetMaxBay();
            int PMaxBay = 1;

            MaxBay = ShelfMaxBay;
            if (GlobalData.Current.PortManager.AllCVList.Count > 0)
            {
                PMaxBay = GlobalData.Current.PortManager.AllCVList.Max(p => p.iBay); //포트까지 추가 계산.
            }
            if (PMaxBay > ShelfMaxBay)
            {
                MaxBay = PMaxBay;
            }

            _RM_RangeMargin = RM_ExtendSize * 2 + 1;

            FirstRMPushBay = ShelfMinBay; //1 Bay 가 없는 경우 대비 직접 최소값 찾아서 넣어둠.
            SecondRMPushBay = ShelfMaxBay;

            //220916 조숭진 db로 옮김.
            //if (GlobalData.Current.DBManager.DbGetConfigInfo("RMSection", "EmptyRetriveAutoReset", out value))
            //if (GlobalData.Current.DBManager.DbGetGlobalConfigValue("RMSection", "EmptyRetriveAutoReset", out value))
            //{
            //    SetUseEmptyRetriveAutoReset(Convert.ToBoolean(value));
            //}
            //else
            //{
            //    SetUseEmptyRetriveAutoReset(true);
            //    GlobalData.Current.DBManager.DbSetProcedureConfigInfo("RMSection", "EmptyRetriveAutoReset", UseEmptyRetriveAutoReset.ToString());
            //}

            _RM1_ExclusiveBay = 1 + (RM_ExtendSize * 2);
            _RM2_ExclusiveBay = ShelfMaxBay - (RM_ExtendSize * 2);
            HandOverMinBay = _RM1_ExclusiveBay + 1;
            HandOverMaxBay = _RM2_ExclusiveBay - 1;

            //양보 할때 목적지 쉘프 설정
            RM1_WithDrawShelf = ShelfManager.Instance.AllData.Where(s => s.iBay == FirstRMPushBay && !s.DeadZone).FirstOrDefault();
            RM2_WithDrawShelf = ShelfManager.Instance.AllData.Where(s => s.iBay == SecondRMPushBay && !s.DeadZone).FirstOrDefault();

            ShelfManager.Instance.SetProtectShelf(); //HandOver 용 보호 Shelf 설정
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
                if (GlobalData.Current.mRMManager.FirstRM.CheckRMAutoMode())
                {
                    if (_RM1_OnProcessJob == null)
                    {
                        //240507 RGJ Job 설정전 추가 인터락 
                        if(Job.JobType == "TRANSFER" &&  GlobalData.Current.mRMManager[RMNumber].CheckCarrierExist() 
                            && GlobalData.Current.mRMManager[RMNumber].CarrierID != Job.CarrierID)
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "AssignJob failed Crane in Carrier({0}) is not matched with Job Carrier:{1}", GlobalData.Current.mRMManager[RMNumber].CarrierID, Job.CarrierID);
                            return false;
                        }
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
                return false;
            }
            else if (RMNumber == 2)
            {
                if (GlobalData.Current.mRMManager.SecondRM.PLC_ErrorCode > 0 || //크레인 알람 코드 올라와 있으면 동작 안함.RGJ 241112 알람코드를 직접보게 수정.
                    GlobalData.Current.mRMManager.SecondRM.CheckModuleHeavyAlarmExist())
                {
                    return false;
                }
                //231019 RGJ Crane 작업 할당전 상태 체크 추가. 기상반 지상반 상태 체크 추가.
                if (GlobalData.Current.mRMManager.SecondRM.CheckRMAutoMode())
                {
                    if (_RM2_OnProcessJob == null)
                    {
                        //240507 RGJ Job 설정전 추가 인터락 
                        if (Job.JobType == "TRANSFER" && GlobalData.Current.mRMManager[RMNumber].CheckCarrierExist() 
                            && GlobalData.Current.mRMManager[RMNumber].CarrierID != Job.CarrierID)
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "AssignJob failed Crane in Carrier({0}) is not matched with Job Carrier:{1}", GlobalData.Current.mRMManager[RMNumber].CarrierID, Job.CarrierID);
                            return false;
                        }

                        Job.AssignedRM = GlobalData.Current.mRMManager[RMNumber]; //231025 RGJ  Job 에 RM 할당이 Task 만들고 이루어지면서  짧은 시간  AssignedRM 검색에 실패해서 예외발생
                        _RM2_OnProcessJob = Job;

                        JobTaskRM2 = Task<Job_Result>.Factory.StartNew(() =>
                        {
                            Thread.CurrentThread.Name = Job.CommandID + " Task";
                            return JobProcess(_RM2_OnProcessJob, 2);
                        });
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                return false;
            }
            return false;
        }
        [Obsolete("Bay 예약 방식 비사용함 실시간 위치 계산 으로 처리")]
        private bool SetBayReserveRange(int RMNumber, int StartBay, int EndBay, bool ForceReserve = false)
        {
            if (StartBay > EndBay) //역으로 가는 방향은 시작과 끝을 뒤집어 준다.
            {
                int temp = StartBay;
                StartBay = EndBay;
                EndBay = temp;
            }

            if (StartBay < 0)
            {
                StartBay = 0;
            }
            lock (ReserveLock)
            {
                if (ForceReserve) //강제로 영역을 잡는다.
                {
                    for (int i = StartBay; i <= EndBay; i++)
                    {
                        BayReserveArray[i].ReservedRM = RMNumber;
                    }
                    return true;
                }
                else
                {
                    //영역을 잡을수 있는지 체크
                    for (int i = StartBay; i <= EndBay; i++)
                    {
                        if (BayReserveArray[i].ReservedRM == RMNumber || BayReserveArray[i].ReservedRM == 0)
                        {
                            continue;
                        }
                        else
                        {
                            return false;// 영역이 이미 다른 RM에 예약되어 있다.
                        }
                    }

                    //영역을 모두 체크해서 잡을수 있을때만 예약
                    for (int i = StartBay; i <= EndBay; i++)
                    {
                        BayReserveArray[i].ReservedRM = RMNumber;
                    }
                    return true;
                }
            }
        }

        /// <summary>
        /// 현재 위치를 제외한 예약을 푼다.[비사용]
        /// </summary>
        /// <param name="RMNumber"></param>
        /// <param name="CurrentBay"></param>
        /// <param name="ForceReserve"></param>
        /// <returns></returns>
        [Obsolete("Bay 예약 방식 비사용함 실시간 위치 계산 으로 처리")]
        private bool FreeBayReserveRange(int RMNumber, int CurrentBay)
        {
            lock (ReserveLock)
            {
                for (int i = 1; i <= MaxBay; i++)
                {
                    if (BayReserveArray[i].ReservedRM == RMNumber && CalcDistance(CurrentBay, i) > RM_ExtendSize) //자신이 예약하고 있는 영역을 해제
                    {
                        BayReserveArray[i].ReservedRM = 0;
                    }

                }
                SetBayReserveRange(RMNumber, CurrentBay - RM_ExtendSize, CurrentBay + RM_ExtendSize, true); //현재 위치 영역 예약
                return true;
            }
        }
        private McsJob GetNextMcsJob(int RMNumber)
        {
            try
            {
                if (JobList.Count() == 0)
                {
                    return null;
                }
                //RM 의 현재 위치를 구한다.
                var TargetRM = GlobalData.Current.mRMManager[RMNumber];
                int RMCurrentBay = TargetRM.CurrentBay;
                McsJob nextJob = null;

                //해당 RM 이 이미 Carrier 를 들고 있으면 그에 해당하는 Job만을 고른다.
                #region 크레인이 이미 캐리어를 가지고 있는 경우 작업 선택
                if (TargetRM.CarrierExistSensor)
                {
                    for (int i = 0; i < JobList.Count(); i++)
                    {
                        var sJob = JobList[i];
                        if (sJob.DestItem == null || sJob.JobNowDeleting) //삭제중인 작업 제외
                        {
                            continue; //목적지를 잃어버림. 레이아웃이 변한경우나 잡에대한 디비를 직접 수정한 경우로 추정.
                        }
                        if(RMNumber == 1 && sJob == RM1_OnProcessJob) //크레인 1 이미 실행중인 작업 제외
                        {
                            continue;
                        }
                        else if(RMNumber == 2 && sJob == RM2_OnProcessJob)  //크레인 2 이미 실행중인 작업 제외
                        {
                            continue;
                        }

                        if (sJob.TCStatus == eTCState.QUEUED || sJob.TCStatus == eTCState.PAUSED || sJob.TCStatus == eTCState.TRANSFERRING) //230306 RGJ 검수 반영.
                        {
                            if (sJob.SubJob == eSubJobType.Push && sJob.TargetRMNumber == RMNumber) //240731 RGJ 화물 들고 있는 상태애서 잡이 없는 경우가 있다 이경우라도 Push 작업은 해야함.
                            {
                                nextJob = sJob;
                                return nextJob;
                            }
                            if (sJob.CarrierID == TargetRM.CarrierID)
                            {
                                if (sJob.SubJob == eSubJobType.HandOver && !CheckCraneReachAble(TargetRM, sJob.DestItem)) //핸드오버 1차 크레인이 버퍼에 집어넣기 전상태
                                {
                                    if (sJob.HandOverBufferItem == null) //재시작 해서 HandOver 쉘프가 지정해제됨 
                                    {
                                        var bufferShelf = ShelfManager.Instance.GetProperBufferShelf(HandOverMinBay, HandOverMaxBay, sJob.DestBay, (sJob.SourceLevel + sJob.DestLevel) / 2);
                                        if (bufferShelf != null)
                                        {
                                            sJob.HandOverStoredDest = bufferShelf.TagName; //버퍼 쉘프를 핸드 오버 목적지로 설정.
                                            sJob.HandOverBufferItem.NotifyScheduled(true);
                                            nextJob = sJob;
                                            return nextJob;
                                        }
                                    }
                                    else //240507 RGJ Dual Scheduler handover 버퍼 있으면 이어서 진행함.
                                    {
                                        if(sJob.HandOverBufferItem.CheckPutAble()) //이미 버퍼 지정되어 있고 사용가능.
                                        {
                                            nextJob = sJob;
                                            return nextJob;
                                        }
                                        else // 해당 핸드오버 버퍼 사용 불가.
                                        {
                                            LogManager.WriteConsoleLog(eLogLevel.Info, "Job: {0} CarrierID : {1} HOverShelf: {2}  Handover PutAbleCheck Fail Relocation Start", sJob.CommandID,sJob.CarrierID, sJob.HandOverBufferItem.iLocName);
                                            var bufferShelf = ShelfManager.Instance.GetProperBufferShelf(HandOverMinBay, HandOverMaxBay, sJob.DestBay, (sJob.SourceLevel + sJob.DestLevel) / 2);
                                            if (bufferShelf != null)
                                            {
                                                sJob.HandOverStoredDest = bufferShelf.TagName; //버퍼 쉘프를 핸드 오버 목적지로 설정.
                                                sJob.HandOverBufferItem.NotifyScheduled(true);
                                                nextJob = sJob;
                                                return nextJob;
                                            }
                                            else
                                            {
                                                LogManager.WriteConsoleLog(eLogLevel.Info, "Job: {0} CarrierID : {1}  Handover Relocation Failed...", sJob.CommandID, sJob.CarrierID);
                                                //핸드오버 가능 쉘프 없음 작업 안함.
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    if (sJob.JobType == "TRANSFER") //240731 RGJ 이동 명령은 목적지 예약 안함
                                    {
                                        bool DestCheck = sJob.DestItem.CheckPutAble();
                                        if (!DestCheck && sJob.DestItem is ShelfItem)
                                        {
                                            sJob.DestItem.NotifyScheduled(true);//작업 목록에 있는데 쉘프 예약이  안되어있다면 예약.
                                            continue; //대상 목적지 사용불가
                                        }
                                    }
                                    nextJob = sJob;
                                    return nextJob;
                                }
                            }
                            
                        }
                    }
                    return null;
                }
                #endregion

                int MaxPriority = 0;
                int MinDistance = int.MaxValue;

                List<McsJob> ListJobProcessAble = new List<McsJob>(); //가용한 작업들을 1차로 추려낸다.
                List<McsJob> ListHighPriorityJob = new List<McsJob>(); //우선순위 가장 높을 작업들을 추려낸다.
                //스케쥴링 스탭 - #1 가용가능한 작업을 추려낸다.
                //스케쥴링 스탭 - #2 최우선순위 값을 체크한다.
                //스케쥴링 스탭 - #3 최우선순위 작업이 2개 이상일경우 크레인 기준 가까운 것으로 결정한다.

                #region #1 가용가능한 작업을 추려낸다.
                for (int i = 0; i < JobList.Count(); i++) //최우선순위 부터 결정
                {
                    var sJob = JobList[i];
                    if (sJob.DestItem == null || sJob.JobNowDeleting) //삭제중인 작업 제외
                    {
                        continue; //목적지를 잃어버림. 레이아웃이 변한경우나 잡에대한 디비를 직접 수정한 경우로 추정.
                    }
                    if (sJob.TCStatus == eTCState.QUEUED || sJob.TCStatus == eTCState.PAUSED)
                    {
                       

                        //240507 RGJ 조건문 수정.소스가 아니고 현재 위치를 체크해야함.
                        if (sJob.CarrierLocationItem is RMModuleBase rm) //현재 RM에 대기중이고 RM 넘버가 다르면 들고 있는 RM이 작업을 해야 한다.
                        {
                            if (rm.RMNumber != RMNumber)
                            {
                                continue;
                            }
                        }
                        if (sJob.JobType == "TRANSFER")
                        {
                            if (sJob.SubJob == eSubJobType.HandOver && sJob.CarrierLoc != sJob.Source && sJob.CarrierLocationItem is ShelfItem) //핸드오버 1차 완료 상태. 현재 위치가 쉘프인지 체크 추가
                            {
                                sJob.HandOverStoredDest = sJob.CarrierLoc;
                                if (sJob.HandOverBufferItem == null) //CarrierLoc 이 날아간 경우일단 대비
                                {
                                    continue;
                                }
                                if(!CheckCraneReachAble(TargetRM, sJob.DestItem)) //240507 RGJ 핸드 오버 완료 했는데 목적지를 해당 크레인이 도달 불가능 하면 작업 크레인이 아니다.
                                {
                                    continue;
                                }
                                bool BufferCheck = sJob.HandOverBufferItem.CheckGetAble(sJob.CarrierID);
                                bool DestCheck = sJob.DestItem.CheckPutAble();
                                //현재 작업이 불가능한 상태면 스킵
                                if (!BufferCheck || !DestCheck) //소스와 목적지 가용성 점검 하나라도 문제 있으면 작업제외
                                {
                                    if (!BufferCheck && sJob.HandOverBufferItem is ShelfItem)
                                    {
                                        sJob.HandOverBufferItem.NotifyScheduled(true);//작업 목록에 있는데 쉘프 예약이  안되어있다면 예약.
                                    }
                                    if (!DestCheck && sJob.DestItem is ShelfItem)
                                    {
                                        sJob.DestItem.NotifyScheduled(true);//작업 목록에 있는데 쉘프 예약이  안되어있다면 예약.
                                    }
                                    continue;
                                }
                            }
                            else if(sJob.SubJob == eSubJobType.AlterStore && sJob.CarrierLoc != sJob.Source && sJob.CarrierLocationItem is ShelfItem)  //대체보관  완료 상태. 현재 위치가 쉘프인지 체크 추가
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
                                
                                if(!CheckCraneReachAble(TargetRM, sJob.SourceItem))     //해당 소스에 접근 불가능한 크레인이다.
                                {
                                    continue;
                                }
                                //240910 RGJ 듀얼 스케쥴러 공용구역 -> 타 크레인 전용 구역 검색 조건 제한.
                                if(GetCraneExZone(sJob.SourceItem) == eCraneExZone.SharedZone && !CheckCraneReachAble(TargetRM, sJob.DestItem))//출발지가 공용 구역이고 목적지가 타 크레인 전용 구역이면 해당 호기 작업이 아님.
                                {
                                    continue;
                                }
                                //2024.09.26 lim, 핸드 오버 잡인데 핸드 오버할 공간 없으면 job 할당 안해야함
                                if (!CheckCraneReachAble(TargetRM, sJob.DestItem) && sJob.HandOverBufferItem == null)
                                {
                                    ShelfItem bufferShelf = ShelfManager.Instance.GetProperBufferShelf(HandOverMinBay, HandOverMaxBay, sJob.DestBay, (sJob.SourceLevel + sJob.DestLevel) / 2);
                                    if (bufferShelf == null)
                                        continue;
                                }
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
                        ListJobProcessAble.Add(sJob); //가용 가능한 작업을 넣어둔다.
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
                        if (sJob.SubJob == eSubJobType.Push)// 동일 우선순위라도 Push 먼저 실행
                        {
                            nextJob = sJob;
                            break;
                        }

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
            else if(bIsPausedJob && NeedTransferResume(Job))
            {
                GlobalData.Current.HSMS.SendS6F11(210, "JobData", Job); //Pause 잡 재시작 할때 TransferResumed (210) 보고
            }


            LogManager.WriteConsoleLog(eLogLevel.Info, "ProcessJob Start! RM : {0} Job:{1}  CarrierID :{2} Source :{3}  Dest : {4}", Job.AssignRMName, Job.CommandID, Job.CarrierID, Job.Source, Job.Destination);
            LogManager.WriteConsoleLog(eLogLevel.Info, "Job Data! RM : {0} SubJob:{1}  EstJobNextMoveBay :{2} AltShelfDestination :{3}", Job.AssignRMName, Job.SubJob, Job.EstJobNextMoveBay(), Job.AltShelfDestination);   //2024.06.29 lim, Log 추가
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
                                return new Job_Result(Job, Job.CommandID, eJob_Result.Paused, "타겟 작업 상태 이상으로 작업 중단."); //240902 RGJ 작업 인터락 이상 상태라도 작업 삭제하면 안됨.
                            }
                            break;
                        case enumScheduleStep.RMGetAssign: //GET 작업 할당
                            RMGetAssignAction(Job);
                            break;
                        case enumScheduleStep.RMGetCompleteWait: //GET 작업 완료를 기다린다
                            RMGetWaitAction(Job);
                            if(Job.Step == enumScheduleStep.RMError)
                            {
                                SCWriteTransferLog(Job, "PICK", "ABORT");
                            }
                            else if(Job.Step == enumScheduleStep.ErrorEmptyRetrieve)
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
                            //2022.12.20 MCS 사양 변경 완료보고가 마지막으로 변경됨.
                            if (Job.SubJob == eSubJobType.AlterStore) //대체 보관 했을 경우
                            {
                                if(Job.Destination == Job.CarrierLoc) //240521 RGJ 대체보관 장소가 목적지인경우 완료처리유도.
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
                            else if (Job.SubJob == eSubJobType.HandOver && Job.HandOverStoredDest == Job.CarrierLoc) //1차 반송 완료 했을 경우
                            {
                                CarrierStorage.Instance.GetCarrierItem(Job.CarrierID).CarrierState = eCarrierState.ALTERNATE; //캐리어 상태 Alternate 로 변경

                                //CarrierStoreAlt 보고
                                GlobalData.Current.HSMS.SendS6F11(306, "JobData", Job);

                                //Zone Capacity Changed
                                GlobalData.Current.HSMS.SendS6F11(310, "ZONENAME", Job.DestItem.iZoneName);
                                Job.NeedTransferringReport = true; //경유반송 1차 완료후 다시 보고가 필요하므로 플래그 초기화
                            }
                            else
                            {
                                if (Job.DestItem is ShelfItem && Job.JobType == "TRANSFER") //쉘프에 Put 보고
                                {
                                    CarrierStorage.Instance.GetCarrierItem(Job.CarrierID).CarrierState = eCarrierState.COMPLETED;

                                    //Put 완료 했으면 Shelf CarrierStore 305
                                    GlobalData.Current.HSMS.SendS6F11(305, "JobData", Job);
                                    //Zone Capacity Changed
                                    GlobalData.Current.HSMS.SendS6F11(310, "ZONENAME", Job.DestItem.iZoneName);
                                }
                            }
                            //2022.12.20 MCS 사양 변경 완료보고가 마지막으로 변경됨.
                            if (!string.IsNullOrEmpty(Job.CommandID) && Job.JobType == "TRANSFER")
                            {
                                if (Job.SubJob == eSubJobType.HandOver && Job.Destination == Job.CarrierLoc) //Hand Over 2차 작업 완료
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
                                else if (Job.SubJob != eSubJobType.Push && Job.SubJob != eSubJobType.HandOver) //Push 작업은 완료 보고 안함.
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
                            }
                            return new Job_Result(Job, Job.CommandID, eJob_Result.Complete, "크레인 반송 동작 완료.");

                        case enumScheduleStep.RMError:
                            string ErrorMsg = string.Format("반송 동작중 에러 발생 RM:{0}   ErrorCode:{1} ", Job.AssignRMName, ErrorCode);
                            //231127 RGJ 에러나면 핸드 오버 잡은거 해제 함.
                            if (Job.HandOverBufferItem != null)
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "Handover Shelf Release by Error CarrierID : {0} Shelf => {1} ",Job.CarrierID ,Job.HandOverBufferItem.GetTagName());
                                Job.HandOverBufferItem.NotifyScheduled(false);
                            }
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
                if(Job.AssignedRM.CarrierExistSensor && Job.AssignedRM.InSlotCarrier == null) //240926 RGJ 화물 감지 상태인데 어떠한 이유든 크레인에 데이터가 없다면
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
                        RMModuleBase CraneforJob = Job.AssignedRM;
                        if (CraneforJob == null) //Null Check
                        {
                            Thread.Sleep(CycleTime);
                            continue;
                        }
                        var TargetShelfs = ShelfManager.Instance.AllData.Where(s =>
                                           CheckCraneReachAble(CraneforJob, s) &&    //240124 DualCrane 스케쥴러 대체반송시 접근가능한 쉘프만 지정해야함.
                                           s.CheckCarrierSizeAcceptable(CarrierWidth) &&
                                           s.ShelfAvailable &&
                                           !s.HandOverProtect && //240507 RGJ 대체 반송 목적지에 보호 쉘프 제외함.
                                           !s.Scheduled &&
                                           !s.CheckCarrierExist() &&
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

                //20231025 RGJ 듀얼 크레인 TRANSFER 잡을 할당하는데  해당 크레인Push 잡이 생성 되었으면 타 크레인이 Push 잡을 작업 할당전에 생성한 것임.
                if (Job.JobType == "TRANSFER") 
                {
                    //푸시 생성과 작업 선택 타이밍 불일치 현상은 완전히 막을수 없으므로 이동명령을 할당할때 검사해서 삭제한다.
                    //해당 작업을 삭제한다.
                    McsJob PushJob = GlobalData.Current.McdList.GetTargetRMPushJob(Job.AssignRMNumber);
                    if(PushJob != null)
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "While Transfer Job Executing There is Push job at Crane {0} Existed. Delete {1} Push Job ", Job.AssignRMName, PushJob.CommandID);
                        GlobalData.Current.McdList.DeleteMcsJob(PushJob, true);
                    }

                }

                if (Job.SubJob == eSubJobType.AlterStore) //대체 보관 작업 목표 변경.
                {
                    if(Job.AssignedRM.CarrierExistSensor) //캐리어를 들고 있다면 
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
                else if (Job.SubJob == eSubJobType.HandOver) //Hand OverJob 이고 버퍼쉘프로 가야하는지 체크
                {
                    if (Job.AssignedRM.CarrierExistSensor)
                    {
                        if(CheckCraneReachAble(Job.AssignedRM, Job.DestItem))
                        {
                            MoveTarget = Job.DestItem; //목적지로 간다.
                        }
                        else
                        {
                            MoveTarget = Job.HandOverBufferItem; //이미 설정된 버퍼로 가야함.
                        }
                    }
                    else
                    {
                        MoveTarget = Job.CarrierLocationItem;
                    }
                }
                
                else
                {
                    if (Job.AssignedRM.CarrierExistSensor || Job.CommandType == "MOVE") // 이동동작은 항상 목적지로 가야함.
                    {
                        if (!CheckCraneReachAble(Job.AssignedRM, Job.DestItem) && Job.CommandType == "TRANSFER") //20231213 RGJ 목적지가 도착 불가능하면 Handover 잡으로 변경해야 한다.
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "While Transfer Job Destination Can't Reachable  Crane {0}. SetHandover Job :{1} ", Job.AssignRMName, Job.CommandID);
                            if (Job.HandOverBufferItem == null) //다시 설정된 핸드오버 쉘프가 없다면 
                            {
                                var NewBufferShelf = ShelfManager.Instance.GetProperBufferShelf(HandOverMinBay, HandOverMaxBay, Job.DestBay, (Job.SourceLevel + Job.DestLevel) / 2);
                                if (NewBufferShelf != null)
                                {
                                    Job.HandOverStoredDest = NewBufferShelf.TagName; //버퍼 쉘프를 핸드 오버 목적지로 설정.
                                    Job.HandOverBufferItem.NotifyScheduled(true);
                                    Job.DestItem.NotifyScheduled(true);
                                    LogManager.WriteConsoleLog(eLogLevel.Info, "Set HandoverJob10   Source : {0}  Buffer : {1}   Dest :{2}", Job.Source, NewBufferShelf.TagName, Job.Destination);
                                }
                            }
                            Job.SubJob = eSubJobType.HandOver;
                            Job.Step = enumScheduleStep.RMMoveAssign;
                            return;
                        }
                        MoveTarget = Job.DestItem;
                    }
                    else
                    {
                        MoveTarget = Job.SourceItem;
                    }
                }
                if(MoveTarget == null) //혹시 목적지가 날아간 경우를 대비해서 추가함
                {
                    Job.Step = enumScheduleStep.CheckDestination;
                    return;
                }

                eReserveAreaResult SResult = SecureAreaForSimultaneous(Job.AssignedRM, MoveTarget);
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
                            Thread.Sleep(1000); //스케쥴링용 딜레이 추가.
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
                                    if (Job.SubJob != eSubJobType.Push) //Push 잡 Active 보고 사양에 없음.
                                    {
                                        GlobalData.Current.HSMS.SendS6F11(701, "JOBDATA", Job); //Report CraneActive
                                    }
                                }
                                Job.Step = enumScheduleStep.RMMoveCompleteWait;
                            }
                        }
                        break;
                    case eReserveAreaResult.PushRequire: //다른 RM 을 안전위치로 푸시
                        if (JobList.Where(j => j.SubJob == eSubJobType.Push).Count() == 0) //잡리스트에 이미 푸시잡이 있는지 확인
                        {
                            //JobList.CreatePush_SubJob(Job.UnAssignedRM.RMNumber, Job.UnAssignedRM.RMNumber == 1 ? FirstRMPushBay : SecondRMPushBay); //기존 끝으로 로직
                            int PushBay = CalcProperPushBay(Job.EstJobNextMoveBay(), Job.UnAssignedRM.IsFirstRM);

                            JobList.CreatePush_SubJob(Job.UnAssignedRM.RMNumber, PushBay);
                        }
                        break;
                    case eReserveAreaResult.WaitRequire: //다른 RM 대기
                        //241030 HoN 화재 관련 추가 수정        //SourceWait로 대기하다가 밀려버리면 집결지로 이동할 수 없음.        //화재작업에서 다른 RM대기 걸렸고, SoruceWait 상태라면 CheckDestination으로 다시 보낸다.
                        
                        break;
                    case eReserveAreaResult.WithdrawRequire: //양보 이동 

                        if (Job.AssignedRM.IsFirstRM)
                        {
                            if (Job.AssignedRM.CurrentBay == RM1_WithDrawShelf.ShelfBay)
                            {
                                break; //이미 양보 위치면 양보 동작안함.
                            }
                        }
                        else
                        {
                            if (Job.AssignedRM.CurrentBay == RM2_WithDrawShelf.ShelfBay)
                            {
                                break; //이미 양보 위치면 양보 동작안함.
                            }
                        }
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} Carrier :{1} RM:{2} Yield Action Start", Job.CommandID, Job.CarrierID, Job.AssignedRM.ModuleName);

                        MoveTarget = Job.AssignedRM.IsFirstRM ? RM1_WithDrawShelf : RM2_WithDrawShelf;
                        mCmd = new CraneCommand(Job.CommandID, Job.AssignRMName, eCraneCommand.MOVE, enumCraneTarget.SHELF, MoveTarget, Job.CarrierID);
                        if (!Job.AssignedRM.CheckRMCommandExist())
                        {
                            GlobalData.Current.WithDrawCounter++;
                            Job.JobWithDrawRequest = true;
                            Job.LastActionTarget = MoveTarget;
                            Job.AssignedRM.SetCraneCommand(mCmd);
                            //220803 조숭진 host에 의한 job이 아닌 내부 이동이므로 주석처리..
                            //GlobalData.Current.HSMS.SendS6F11(701, "JOBDATA", Job); //Report CraneActive 
                            Job.Step = enumScheduleStep.RMMoveCompleteWait;
                        }
                        break;
                    case eReserveAreaResult.CannotReachAble: //목적지 접근 불가능 이건 크레인 선택이 잘못됨.
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} Carrier :{1} RM:{2} MoveTarget: {3} Can't ReachAble Job Stop.", Job.CommandID, Job.CarrierID, Job.AssignedRM.ModuleName, MoveTarget.iLocName);
                        Job.TCStatus = eTCState.PAUSED;
                        Job.Step = enumScheduleStep.JobPause;
                        return;
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
                if(Job.LastActionTarget == null)
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
                if(!Job.AssignedRM.CheckRMAutoMode()) //231020 RGJ 작업 완료 대기 도중 크레인 오토 상태가 풀렸다.
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
                    ////화재 작업 완료 했다면 수조로 갔는지 체크해본다.
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

                    //Job.JobWithDrawRequest = false; //여기서 초기화 하면 안됨. 양보동작했으면 다시 이동을 검색해야함.

                    if (Job.JobType == "TRANSFER")
                    {
                        if (Job.JobWithDrawRequest) //양보동작이었다면 
                        {
                            Job.JobWithDrawRequest = false; //양보동작 초기화
                            Job.Step = enumScheduleStep.RMMoveAssign; //다시 RM 이동을 시도한다.
                            break;
                        }
                        if (Job.AssignedRM.CarrierExistSensor) //적재물 있으면 Put
                        {

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
                //2023.10.12 lim, 
                else if (!bDestPosition && bRMMoveDone)   // 목적지에 도착 못 했는데 커맨드가 없다?? 그럼면 다시 커맨드 설정 스텝으로
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} CarrierID: {1} Crane Command is done, But it didn't arrive. step Back Move Assign.", Job.CommandID, Job.CarrierID);
                    Job.Step = enumScheduleStep.RMMoveAssign;
                    return;
                }

                #region Dual 전용 Push 로직
                try
                {
                    //이동 중이라도 다른 RM 이 경로상 대기중이라면 Push 한다.
                    CraneCommand JobCommand = Job.AssignedRM.GetCurrentCmd();
                    if (JobCommand != null)
                    {
                        if (Job.AssignedRM.IsFirstRM)
                        {
                            if (RM2_OnProcessJob == null && JobCommand.TargetBay > Job.UnAssignedRM.CurrentBay - RM_RangeMargin)
                            {
                                if (JobList.Where(j => j.SubJob == eSubJobType.Push).Count() == 0) //잡리스트에 이미 푸시잡이 있는지 확인
                                {
                                    Log.LogManager.WriteConsoleLog(eLogLevel.Info, "Create Push Command during Moving RM1 TargetPos : {0} RM2 Pos : {1}", JobCommand.TargetBay, Job.UnAssignedRM.CurrentBay);
                                    int PushBay = CalcProperPushBay(Job.EstJobNextMoveBay(), false);
                                    JobList.CreatePush_SubJob(Job.UnAssignedRM.RMNumber, PushBay);
                                }
                            }
                        }
                        else
                        {
                            if (RM1_OnProcessJob == null && JobCommand.TargetBay < Job.UnAssignedRM.CurrentBay + RM_RangeMargin)
                            {
                                if (JobList.Where(j => j.SubJob == eSubJobType.Push).Count() == 0) //잡리스트에 이미 푸시잡이 있는지 확인
                                {
                                    Log.LogManager.WriteConsoleLog(eLogLevel.Info, "Create Push Command during Moving RM2 TargetPos : {0} RM1 Pos : {1}", JobCommand.TargetBay, Job.UnAssignedRM.CurrentBay);
                                    int PushBay = CalcProperPushBay(Job.EstJobNextMoveBay(), true);
                                    JobList.CreatePush_SubJob(Job.UnAssignedRM.RMNumber, PushBay);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                }
                #endregion

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
                if (!bDestPosition)
                {
                    Job.Step = enumScheduleStep.RMMoveAssign; //위치가 안맞으면 다시 이동시킨다.
                    return;
                }
            }


            if (GetSourceItem.CheckGetAble(Job.CarrierID) == false) //GET 명령 내리기전에 소스 상태 다시 체크
            {
                if(Job.SourceItem is CV_BaseModule) // [230503 CIM 검수]
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
            {
                GlobalData.Current.HSMS.SendS6F11(307, "JobData", Job);
            }

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
                if(Job.LastActionTarget == null)
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
                if (Job.LastActionTarget is CV_BaseModule SourcePort && Job.AssignedRM.CheckPortInterfaceErrorState()) //Port I/F Error 처리
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} Source :{1} CarrierID: {2} RMGetWaitAction Source IF Error Case Enter", Job.CommandID, SourcePort.ModuleName, Job.CarrierID);
                    //포트로 알람 코드를 넣는다.
                    if (SourcePort != null)
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
                    LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} Source :{1} => {2} RMGetWaitAction Aborted by Alarm", Job.CommandID, Job.LastActionTarget.iLocName, Job.AssignedRM.ModuleName);
                    if (Job.SourceItem is CV_BaseModule SourceCV)
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


                    //230819 RGJ Get 동작을 완료했으나 캐리어 데이터를 전달 받지 못함.스케쥴러가 기다렸다가 강제로 데이터 전송함.
                    if (Job.AssignedRM.InSlotCarrier == null)
                    {
                        Thread.Sleep(1000); //조금만 더 기다려 본다.
                        if (Job.AssignedRM.InSlotCarrier == null)
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} Source :{1} RMGetWaitAction data is not transferred.Scheduler will force transferring", Job.CommandID, Job.LastActionTarget.iLocName);
                            if (Job.LastActionTarget != null)
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "Job:{0} Source :{1} => {2} RMGetWaitAction Data has been transferred", Job.CommandID, Job.LastActionTarget.iLocName,Job.AssignedRM.ModuleName);

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
            else if (Job.SubJob == eSubJobType.HandOver && !CheckCraneReachAble(Job.AssignedRM, Job.DestItem))
            {
                if (Job.HandOverBufferItem.CheckPutAble() == false) //HandOver 해야 하는데 해당 쉘프 Put 불가능 다시 버퍼 쉘프 설정해야함
                {
                    Job.HandOverBufferItem.NotifyScheduled(false);
                    var NewBufferShelf = ShelfManager.Instance.GetProperBufferShelf(HandOverMinBay, HandOverMaxBay, Job.DestBay, (Job.SourceLevel + Job.DestLevel) / 2);
                    if (NewBufferShelf != null)
                    {
                        Job.HandOverStoredDest = NewBufferShelf.TagName; //버퍼 쉘프를 핸드 오버 목적지로 설정.
                        Job.HandOverBufferItem.NotifyScheduled(true);
                        Job.DestItem.NotifyScheduled(true);
                        Job.SubJob = eSubJobType.HandOver;
                        Job.Step = enumScheduleStep.RMMoveAssign;//위치가 안맞으면 다시 이동시킨다.
                        LogManager.WriteConsoleLog(eLogLevel.Info, "Set HandoverJob1   Source : {0}  Buffer : {1}   Dest :{2}", Job.Source,NewBufferShelf.TagName,Job.Destination);
                        return;
                    }
                }
                else
                {
                    PTarget = enumCraneTarget.SHELF;
                    //작업 내리기전 다시 한번 RM 위치 체크
                    if (Job.AssignedRM.CalcDistance(Job.AssignedRM, Job.HandOverBufferItem) > 0)
                    {
                        Job.Step = enumScheduleStep.RMMoveAssign;//위치가 안맞으면 다시 이동시킨다.
                        return;
                    }
                    pCmd = new CraneCommand(Job.CommandID, Job.AssignRMName, eCraneCommand.UNLOAD, PTarget, Job.HandOverBufferItem, Job.CarrierID);
                    if (!Job.AssignedRM.CheckRMCommandExist())
                    {
                        Job.LastActionTarget = Job.HandOverBufferItem;
                        Job.AssignedRM.SetCraneCommand(pCmd);
                        Job.Step = enumScheduleStep.RMPutCompleteWait;
                    }
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
                        RMModuleBase CraneforJob = Job.AssignedRM;
                        if (CraneforJob == null) //Null Check
                        {
                            Thread.Sleep(CycleTime);
                            return;
                        }
                        var TargetShelfs = ShelfManager.Instance.AllData.Where(s =>
                                           CheckCraneReachAble(CraneforJob, s) &&    //240124 DualCrane 스케쥴러 대체반송시 접근가능한 쉘프만 지정해야함.
                                           s.CheckCarrierSizeAcceptable(CarrierWidth) &&
                                           s.ShelfAvailable &&
                                           !s.HandOverProtect && //240507 RGJ 대체 반송 목적지에 보호 쉘프 제외함.
                                           !s.Scheduled &&
                                           !s.CheckCarrierExist() &&
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
                        if (Job.DestItem is CV_BaseModule cv)
                        {
                            if (Job.AssignedRM.PC_DestWorkPlace_FORK1 != cv.iWorkPlaceNumber)
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
                        //if(Job.DestItem is CV_BaseModule cv) //231002 RGJ Write Tracking 시점 변경 Robot 이 Unload 내리기전에 준다.
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
                        CarrierItem UNKCarrier = CarrierStorage.Instance.CreateDSUnknownCarrier(TargetShelf.TagName, TargetShelf.CarrierID, eCarrierSize.Unknown); //사이즈는 알수 없다.
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
                    if (Job.LastActionTarget is CV_BaseModule == false) //포트로 가는건 아직 반송이 종료된게 아님
                    {
                        Job.TCStatus = eTCState.NONE;
                    }
                    Job.Step = enumScheduleStep.CraneJobComplete;

                    //이전에 에러가 발생하여 jobresult가 success가 아니였으나 클리어해서 완료됐다면 jobresult를 success로 변경한다.
                    if (Job.JobResult != eJobResultCode.SUCCESS)
                    {
                        Job.JobResult = eJobResultCode.SUCCESS;
                    }

                    if (Job.prevDestItem != null)
                    {
                        Job.prevDestItem.NotifyScheduled(false);
                    }
                    GlobalData.Current.HSMS.SendS6F11(702, "JOBDATA", Job); //Report CraneIdle
                    break;
                }
                Thread.Sleep(CycleTime);
            }
        }
        #endregion
        /// <summary>
        /// 시작 위치와 목표 위치 상태 인터락 체크
        /// </summary>
        /// <param name="Job"></param>
        /// <returns></returns>


        //기존 구 로직
        //private eReserveAreaResult SecureArea(McsJob Job)
        //{
        //    //동작 영역 확보를 시도한다.
        //    int SourceBay = -999;
        //    int DestBay = -999;
        //    if (Job.SubJob == eSubJobType.AlterStore) //대체 반송 로직 추가.
        //    {
        //        SourceBay = Job.SourceBay;
        //        DestBay = Job.AlterShelfBay;
        //    }
        //    else //일반 로직
        //    {
        //        SourceBay = Job.SourceBay;
        //        DestBay = Job.DestBay;
        //    }

        //    bool JobForwardMove = SourceBay <= DestBay;
        //    bool ReserveOK = false;
        //    if(Job.SubJob == eSubJobType.Push)
        //    {
        //        //Push 잡일때는 예약 필요없이 바로 보낸다.
        //        return eReserveAreaResult.ReserveOK;
        //    }
        //    if (JobForwardMove)
        //    {
        //        if (Job.AssignedRM.CurrentBay <= SourceBay)  //정방향 잡이고 랙마가 출발지 전이면
        //        {
        //            ReserveOK = SetBayReserveRange(Job.AssignRMNumber, Job.AssignedRM.CurrentBay - RM_ExtendSize, DestBay + RM_ExtendSize);
        //        }
        //        else if (Job.AssignedRM.CurrentBay >= DestBay) //정방향 잡이고 랙마가 도착지 후방이면
        //        {
        //            ReserveOK = SetBayReserveRange(Job.AssignRMNumber, SourceBay - RM_ExtendSize, Job.AssignedRM.CurrentBay + RM_ExtendSize);
        //        }
        //        else //정방향 잡이고 랙마가 작업공간사이면 
        //        {
        //            ReserveOK = SetBayReserveRange(Job.AssignRMNumber, SourceBay - RM_ExtendSize, DestBay + RM_ExtendSize);
        //        }
        //    } 
        //    else
        //    {
        //        if (Job.AssignedRM.CurrentBay >= SourceBay)  //역방향 잡이고 랙마가 출발지 후방 이면
        //        {
        //            ReserveOK = SetBayReserveRange(Job.AssignRMNumber, Job.AssignedRM.CurrentBay + RM_ExtendSize, DestBay - RM_ExtendSize);
        //        }
        //        else if (Job.AssignedRM.CurrentBay <= DestBay) //역방향 잡이고 랙마가 도착지 전방 이면
        //        {
        //            ReserveOK = SetBayReserveRange(Job.AssignRMNumber, SourceBay + RM_ExtendSize, Job.AssignedRM.CurrentBay - RM_ExtendSize);
        //        }
        //        else //랙마가 작업공간사이면 
        //        {
        //            ReserveOK = SetBayReserveRange(Job.AssignRMNumber, SourceBay + RM_ExtendSize, DestBay - RM_ExtendSize);
        //        }
        //    }

        //    if (ReserveOK) //영역이 확보 되었으면 바로 다른 RM 동작 필요없이 작업실행
        //    {
        //        //영역 확보 완료 => OK
        //        return eReserveAreaResult.ReserveOK;
        //    }
        //    else
        //    {
        //        //영역 확보에 실패했으면 다른 RM 이 대기중인지 동작중인지 체크
        //        if (CheckRMOnProcess(Job.UnAssignedRM.RMNumber)) 
        //        {
        //            //DeadLock 케이스 발생 체크
        //            if (Job.JobCurrentSecureTryCount > AreaDeadLockCount)
        //            {
        //                var raceJob = JobList.Where(j => j.Step == enumScheduleStep.RMSecureArea && j.CommandID != Job.CommandID).FirstOrDefault(); //다른 작업중에 영역 확보중인 작업이 있는지?
        //                if (raceJob != null)
        //                {
        //                    if(Job.ScheduledPriority > raceJob.ScheduledPriority) //다른 잡의 우선 순위가 높다.
        //                    {
        //                        Job.JobCurrentSecureTryCount = 0;
        //                        return eReserveAreaResult.WithdrawRequire; //양보작업으로 변경.
        //                    }
        //                    else if (Job.ScheduledPriority == raceJob.ScheduledPriority) //우선순위가 같으면 2번 RM 이 양보한다.
        //                    {
        //                        if (Job.AssignRMNumber > raceJob.AssignRMNumber) //RM 번호순으로 우선권
        //                        {
        //                            Job.JobCurrentSecureTryCount = 0;
        //                            return eReserveAreaResult.WithdrawRequire; //양보작업으로 변경
        //                        }
        //                    }
        //                }
        //            }
        //            return eReserveAreaResult.WaitRequire; //영역 에서 다른 RM이 작업진행중 => 대기하는 작업 완료 대기했다가 다시 체크
        //        }
        //        else 
        //        {
        //            return eReserveAreaResult.PushRequire; //영역 에서 다른 Rm 대기 중 => 푸시 명령
        //        }


        //    }

        //}

        /// <summary>
        /// Crane 동시 작업을 위한 작업 공간 체크
        /// </summary>
        /// <param name="Job"></param>
        /// <returns></returns>
        private eReserveAreaResult SecureAreaForSimultaneous(RMModuleBase ActionRM, ICarrierStoreAble Target)
        {
            int TargetBay = Target.iBay;
            int CurrentBay = ActionRM.CurrentBay;
            RMModuleBase AnotherRM = GlobalData.Current.mRMManager.GetAnotherRM(ActionRM);
            CraneCommand AnotherRMCommand = GlobalData.Current.mRMManager.GetAnotherRM(ActionRM).GetCurrentCmd();

            if(!CheckCraneReachAble(ActionRM, Target)) 
            {
                //240517 RGJ 목표로 잡은 목적지가 접근 불가능 위치.
                //애초에 여기까지 와서도 안되지만 스케쥴링 크레인 선택이 잘못 될경우 작업을 중단해야함.
                return eReserveAreaResult.CannotReachAble;
            }


            //현재 위치에서 다른 크레인과 멀어지는 작업 => 성공처리
            if (ActionRM.IsFirstRM)
            {
                if (TargetBay - CurrentBay <= 0)     //<------------방향이동
                {
                    return eReserveAreaResult.ReserveOK;
                }
            }
            else
            {
                if (TargetBay - CurrentBay >= 0)    //------------>방향이동
                {
                    return eReserveAreaResult.ReserveOK;
                }
            }

            //다른 크레인에 접근하지만 다른 크레인은 목표 위치 외부에서 대기중일때 => 성공처리
            //다른 크레인에 접근하지만 다른 크레인은 목표 위치 내부에서 대기중일때 => Push 작업 생성
            //다른 크레인에 접근하지만 다른 크레인의 이동 목표가 목표위치 바깥일때 => 성공처리
            //다른 크레인에 접근하지만 다른 크레인은 이동 목표가 목표위치 내부일때 => 대기

            //다른 크레인에 접근필요
            if (AnotherRMCommand != null) //다른 크레인 작업중
            {
                if (ActionRM.IsFirstRM)
                {
                    if (AnotherRMCommand.TargetBay >= TargetBay + RM_RangeMargin) //다른 크레인이 작업 외부 공간을 목표로 작업중
                    {
                        if (CalcBayDistance(CurrentBay, AnotherRMCommand.TargetBay) > RM_RangeMargin) //여유 마진이 있으므로 명령 보낸다.
                        {
                            //231006 RGJ 다른 크레인이 명령 받아서 Busy 상태임을 확인하고 보낸다.
                            if(AnotherRM.CheckRMPLCBusy() &&  CalcBayDistance(CurrentBay,AnotherRM.CurrentBay) > RM_RangeMargin + RM_AddtionalMargin) //추가 이동 마진 확보후 출발.
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "SecureAreaForSimultaneous : Another RM : {0} Busy Checked! {1} Possible", AnotherRM.ModuleName ,ActionRM.ModuleName);
                                return eReserveAreaResult.ReserveOK;
                            }
                            else
                            {
                                return eReserveAreaResult.WaitRequire; //여유 마진이 생길때까지 대기
                            }
                        }
                        else
                        {
                            return eReserveAreaResult.WaitRequire; //여유 마진이 생길때까지 대기
                        }
                    }
                    else
                    {
                        return eReserveAreaResult.WaitRequire; //작업 끝날때까지 대기
                    }
                }
                else
                {
                    if (AnotherRMCommand.TargetBay <= TargetBay - RM_RangeMargin) //다른 크레인이 작업 외부 공간을 목표로 작업중
                    {
                        if (CalcBayDistance(CurrentBay, AnotherRMCommand.TargetBay) > RM_RangeMargin) //여유 마진이 있으므로 명령 보낸다.
                        {
                            //231006 RGJ 다른 크레인이 명령 받아서 Busy 상태임을 확인하고 보낸다.
                            if (AnotherRM.CheckRMPLCBusy() && CalcBayDistance(CurrentBay, AnotherRM.CurrentBay) > RM_RangeMargin + RM_AddtionalMargin) //추가 이동 마진 확보후 출발.
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "SecureAreaForSimultaneous : Another RM : {0} Busy Checked! {1} Move Possible", AnotherRM.ModuleName, ActionRM.ModuleName);
                                return eReserveAreaResult.ReserveOK;
                            }
                            else
                            {
                                return eReserveAreaResult.WaitRequire; //여유 마진이 생길때까지 대기
                            }
                        }
                        else
                        {
                            return eReserveAreaResult.WaitRequire; //여유 마진이 생길때까지 대기
                        }
                    }
                    else
                    {
                        return eReserveAreaResult.WaitRequire; //작업 끝날때까지 대기
                    }
                }
            }
            else //다른 크레인 대기중
            {
                if (ActionRM.IsFirstRM)
                {
                    if (AnotherRM.CurrentBay >= TargetBay + RM_RangeMargin) //작업 공간 외부 대기중
                    {
                        return eReserveAreaResult.ReserveOK;
                    }
                    else 
                    {
                        //작업 공간 내부 대기중
                        var RM1Job = RM1_OnProcessJob; //레퍼런스 임시 저장
                        var RM2Job = RM2_OnProcessJob;
                        //양보가 필요한지 체크한다.
                        if (RM1Job != null && RM2Job != null)
                        {
                            //서로간 추정 이동경로를 검사한다.
                            if (RM1Job.EstJobNextMoveBay() <= RM2Job.EstJobNextMoveBay() - RM_RangeMargin) //목적지 체크
                            {
                                //return eReserveAreaResult.ReserveOK;
                                return eReserveAreaResult.WaitRequire; // 기달렸다가 타 크레인 명령 진행하면 위에 로직에서 진행함.
                            }
                            else //추정 이동 경로가 겹친다.
                            {
                                if (RM1Job.ScheduledPriority < RM2Job.ScheduledPriority) //다른 잡의 우선 순위가 높다.
                                {
                                    return eReserveAreaResult.WithdrawRequire; //양보작업으로 변경.
                                }
                                else
                                {
                                    return eReserveAreaResult.WaitRequire; //우선 순위가 높다면 다른 양보를 기다린다.
                                }
                            }
                        }
                        else
                        {
                            return eReserveAreaResult.PushRequire;//밀어낸다.
                        }
                    }
                }
                else
                {
                    if (AnotherRM.CurrentBay <= TargetBay - RM_RangeMargin) //작업 공간 외부 대기중
                    {
                        return eReserveAreaResult.ReserveOK;
                    }
                    else 
                    {
                        //작업 공간 내부 대기중
                        var RM1Job = RM1_OnProcessJob; //레퍼런스 임시 저장
                        var RM2Job = RM2_OnProcessJob;
                        //양보가 필요한지 체크한다.
                        if (RM1Job != null && RM2Job != null)
                        {
                            //서로간 추정 이동경로를 검사한다.
                            if (RM1Job.EstJobNextMoveBay() <= RM2Job.EstJobNextMoveBay() - RM_RangeMargin) //목적지 체크
                            {
                                //겹치는 부분이 없으므로 이동 가능
                                //return eReserveAreaResult.ReserveOK;
                                return eReserveAreaResult.WaitRequire; // 기달렸다가 타 크레인 명령 진행하면 위에 로직에서 진행함.
                            }
                            else //추정 이동 경로가 겹친다.
                            {
                                if (RM1Job.ScheduledPriority >= RM2Job.ScheduledPriority) //다른 잡의 우선 순위가 높다. 우선순위가 같으면 2번이 양보
                                {
                                    return eReserveAreaResult.WithdrawRequire; //양보작업으로 변경.
                                }
                                else
                                {
                                    return eReserveAreaResult.WaitRequire; //우선 순위가 높다면 다른 양보를 기다린다.
                                }
                            }
                        }
                        else
                        {
                            return eReserveAreaResult.PushRequire;//밀어낸다.
                        }
                    }
                }
            }
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
            var TargetRM2 = GlobalData.Current.mRMManager[2];
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
                    //크레인 원랙 모드 변경요청 체크
                    //사양 나오면 구현 예정.

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
                        if (CurrentSCOPMode != eSC_OperationMode.SecondRMOnly) //운영모드 체크
                        {
                            NextJob = GetNextMcsJob(1); //RM 1번을 위한 작업을 대기 리스트중에 고른다.
                        }
                    }
                    if (IsRM2JobProcessing == false && NextJob == null)
                    {
                        if (CurrentSCOPMode != eSC_OperationMode.FirstRMOnly) //운영모드 체크
                        {
                            NextJob = GetNextMcsJob(2); ///RM 2번을 위한 작업을  대기 리스트중에 고른다.
                        }
                    }
                    if (NextJob != null && !NextJob.JobNowDeleting) //삭제중인 작업 제외
                    {
                        if (RM1_OnProcessJob == NextJob || RM2_OnProcessJob == NextJob) //작업을 골랐지만 이미 실행중인 작업이다.
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "NextJob : {0} Selected but OnProcessing ", NextJob.CommandID);
                        }
                        else
                        {
                            //여기서 작업을 분할해야지는 판단.
                            int RNumber = SelectRMForJob(NextJob);
                            if (RNumber > 0)
                            {
                                if(RNumber == 1 && CurrentSCOPMode != eSC_OperationMode.SecondRMOnly)
                                {
                                    //2번 크레인 전용모드 인데 1번 크레인 작업이 걸리면 안됨.
                                    AssignJob(RNumber, NextJob); //작업 할당에서 동작 스타트까지
                                }
                                else if (RNumber == 2 && CurrentSCOPMode != eSC_OperationMode.FirstRMOnly)
                                {
                                    //1번 크레인 전용모드 인데 2번 크레인 작업이 걸리면 안됨.
                                    AssignJob(RNumber, NextJob); //작업 할당에서 동작 스타트까지
                                }
                            }
                            //nextjob으로 2portget-shelfput일때 Handover job 발생했고, buffer shelf를 못찾을때 nextjob은 priority때문에 멍하니 있게된다. 그래서 nextjob priority를 제일 하위로 낮춘다.
                            else
                            {
                                SetCurPriorityChangeJob(NextJob);
                            }
                        }
                    }

                    //작업 후처리
                    CraneJobPostProcess();

                    //메뉴얼 포트 Both 포트 자동전환 비사용 
                    //if(NeedManualPortBothOperation) //ManualPort Both Operation
                    //{
                    //    //기본적으로 Output 포트로 변경을 계속 시도한다.
                    //    foreach(var cvLine in ManualPortLineList)
                    //    {
                    //        if(cvLine != null)
                    //        {
                    //            if(cvLine.CheckAllPortIsOutputMode())  //먼저 모든 포트 상태가 배출 포트이면 할게 없음 스킵
                    //            {
                    //                continue;
                    //            }

                    //            if(cvLine.CheckCarrierExistInLine()) //컨베이어 라인에 캐리어가 하나라도 존재한다.스킵
                    //            {
                    //                continue;
                    //            }
                    //            //작업자가 PLC 버튼으로 입고 모드로 변경후 캐리어를 올려 놓을 시간을 감안해야 한다.
                    //            //조건이 맞았다고 바로 변경하지 않고 대기시간을 갖는다.
                    //        }
                    //    }
                    //}

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
            try
            {
                if (sJob.SubJob == eSubJobType.Push) //푸시잡이면 바로 랙마스터 설정하고 끝.
                {
                    if (sJob.TargetRMNumber > 0)
                    {
                        if (GlobalData.Current.mRMManager[sJob.TargetRMNumber].CheckRMAutoMode()) //231127 RGJ SelectRMForJob 크레인 오토 모드 체크
                        {
                            return sJob.TargetRMNumber;
                        }
                        else
                        {
                            return -1;
                        }
                    }
                    else
                    {
                        return -1;
                    }
                }

                if (sJob.AssignRMNumber > 0) //이미 기존 RM 이 설정 되어 있다면 그대로 이어서 한다.
                {
                    ICarrierStoreAble CurrentLoc = sJob.CarrierLocationItem;
                    if(CurrentLoc != null && !CheckCraneReachAble(sJob.AssignedRM,CurrentLoc))
                    {
                        //240517 RGJ RM이 설정 되었으나 현재 화물 위치에 접근 불가능.다른 RM 을 Set 한다.
                        sJob.AssignedRM = sJob.AssignedRM.IsFirstRM ? GlobalData.Current.mRMManager.SecondRM : GlobalData.Current.mRMManager.FirstRM;
                        return -1; //다시 스케쥴링 하게 리턴함.
                    }
                    //HandOver 다시설정해야 하는지 체크
                    if(sJob.AssignedRM.IsFirstRM && sJob.DestBay >= RM2_ExclusiveBay) //1번 크레인
                    {
                        if (sJob.AssignedRM.CheckRMAutoMode()) //231127 RGJ SelectRMForJob 크레인 오토 모드 체크 -> 오토모드가 아니면 버퍼 안잡음
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "MCS retry action Job : {0} Changed RM1 HandOver Job!", sJob.CommandID);
                            ShelfItem bufferShelf = sJob.HandOverBufferItem as ShelfItem;
                            if (bufferShelf == null) //버퍼 쉘프 설정이 안되어 있으면 적절한 버퍼 쉘프 설정.
                            {
                                bufferShelf = ShelfManager.Instance.GetProperBufferShelf(HandOverMinBay, HandOverMaxBay, sJob.DestBay, (sJob.SourceLevel + sJob.DestLevel) / 2);

                            }
                            if (bufferShelf != null)
                            {
                                sJob.HandOverStoredDest = bufferShelf.TagName; //버퍼 쉘프를 핸드 오버 목적지로 설정.
                                if (sJob.CarrierLoc == sJob.Source)
                                {
                                    sJob.SourceItem.NotifyScheduled(true);
                                }
                                sJob.HandOverBufferItem.NotifyScheduled(true);
                                sJob.DestItem.NotifyScheduled(true);
                                sJob.SubJob = eSubJobType.HandOver;
                                LogManager.WriteConsoleLog(eLogLevel.Info, "Set HandoverJob2   Source : {0}  Buffer : {1}   Dest :{2}", sJob.Source, bufferShelf.TagName, sJob.Destination);
                            }
                            else
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "MCS retry action Job : {0} Buffer Shelf Search Failed!", sJob.CommandID);
                                return -1;
                            }
                        }
                        else
                        {
                            return -1;
                        }
                    }
                    else if(!sJob.AssignedRM.IsFirstRM && sJob.DestBay <= RM1_ExclusiveBay) //2번 크레인
                    {
                        if (sJob.AssignedRM.CheckRMAutoMode()) //231127 RGJ SelectRMForJob 크레인 오토 모드 체크 -> 오토모드가 아니면 버퍼 안잡음
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "MCS retry action Job : {0} Changed RM2 HandOver Job!", sJob.CommandID);
                            ShelfItem bufferShelf = sJob.HandOverBufferItem as ShelfItem;
                            if (bufferShelf == null) //버퍼 쉘프 설정이 안되어 있으면 적절한 버퍼 쉘프 설정.
                            {
                                bufferShelf = ShelfManager.Instance.GetProperBufferShelf(HandOverMinBay, HandOverMaxBay, sJob.DestBay, (sJob.SourceLevel + sJob.DestLevel) / 2);
                            }
                            if (bufferShelf != null)
                            {
                                sJob.HandOverStoredDest = bufferShelf.TagName; //버퍼 쉘프를 핸드 오버 목적지로 설정.
                                if (sJob.CarrierLoc == sJob.Source)
                                {
                                    sJob.SourceItem.NotifyScheduled(true);
                                }
                                sJob.HandOverBufferItem.NotifyScheduled(true);
                                sJob.DestItem.NotifyScheduled(true);
                                sJob.SubJob = eSubJobType.HandOver;
                                LogManager.WriteConsoleLog(eLogLevel.Info, "Set HandoverJob3   Source : {0}  Buffer : {1}   Dest :{2}", sJob.Source, bufferShelf.TagName, sJob.Destination);
                            }
                            else
                            {

                                LogManager.WriteConsoleLog(eLogLevel.Info, "MCS retry action Job : {0} Buffer Shelf Search Failed!", sJob.CommandID);
                                return -1;
                            }
                        }
                        else
                        {
                            return -1;
                        }
                    }
                    return sJob.AssignRMNumber;
                }

                int startBay = -1;
                int targetBay = -1;

                int Rm1Bay = int.Parse(GlobalData.Current.mRMManager.FirstRM.Robot_BAY);
                int Rm2Bay = int.Parse(GlobalData.Current.mRMManager.SecondRM.Robot_BAY);
                bool bStart_RM1Ex = false;
                bool bStart_RM2Ex = false;
                bool bTarget_RM1Ex = false;
                bool bTarget_RM2Ex = false;
                //출발지와 목적지 Bay 를 계산해서 랙마스터를 선정.
                if (sJob.SourceItem == null || sJob.DestItem == null) //출발지 또는 목적지 이상
                {
                    return -1;
                }
                //HandOver Job의 2차 작업상태인지 체크
                if(sJob.SubJob == eSubJobType.HandOver && sJob.CarrierLoc != sJob.Source)
                {
                    var BufferShelf = GlobalData.Current.ShelfMgr.GetShelf(sJob.CarrierLoc);
                    if(BufferShelf == null)
                    {
                        return -1;
                    }
                    else
                    {
                        sJob.HandOverStoredDest = BufferShelf.TagName;
                        startBay = BufferShelf.iBay;
                    }    
                }
                else
                {
                    //startBay = sJob.SourceItem.iBay; //240517 RGJ 소스가 아니 현재위치를 봐야함.대체반송된경우를 포함하여 수동으로 옮긴경우도 고려함.

                    ICarrierStoreAble CurrentLocItem = sJob.CarrierLocationItem;
                    if(CurrentLocItem != null)
                    {
                        startBay = CurrentLocItem.iBay;
                    }
                    else if (sJob.SourceItem is RMModuleBase)  //2024.05.18 lim, 크레인 MoveOnly 안됨. 소스가 크레인이면 소스로 움직여아함
                    {
                        startBay = sJob.SourceItem.iBay;
                    }
                    else
                    {
                        return -1; //현재 위치가 없어졌거나 찾을수 없음.
                    }

                }
                targetBay = sJob.DestItem.iBay; //도착지


                //출발지 목적지 체크
                if (RM1_ExclusiveBay >= startBay)
                {
                    bStart_RM1Ex = true;
                }
                if (RM2_ExclusiveBay <= startBay)
                {
                    bStart_RM2Ex = true;
                }

                if (RM1_ExclusiveBay >= targetBay)
                {
                    bTarget_RM1Ex = true;
                }
                if (RM2_ExclusiveBay <= targetBay)
                {
                    bTarget_RM2Ex = true;
                }

                if (bStart_RM1Ex && bTarget_RM2Ex)
                {
                    if (GlobalData.Current.mRMManager[1].CheckRMAutoMode()) //231127 RGJ SelectRMForJob 크레인 오토 모드 체크 -> 오토모드가 아니면 버퍼 안잡음
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "MCS Job : {0} Changed RM1 HandOver Job!", sJob.CommandID);
                        if (sJob.HandOverBufferItem == null)
                        {
                            var bufferShelf = ShelfManager.Instance.GetProperBufferShelf(HandOverMinBay, HandOverMaxBay, sJob.DestBay, (sJob.SourceLevel + sJob.DestLevel) / 2);
                            if (bufferShelf != null)
                            {
                                sJob.HandOverStoredDest = bufferShelf.TagName; //버퍼 쉘프를 핸드 오버 목적지로 설정.
                                sJob.DestItem.NotifyScheduled(true);
                                sJob.HandOverBufferItem.NotifyScheduled(true);
                                sJob.SubJob = eSubJobType.HandOver;
                                LogManager.WriteConsoleLog(eLogLevel.Info, "Set HandoverJob4   Source : {0}  Buffer : {1}   Dest :{2}", sJob.Source, bufferShelf.TagName, sJob.Destination);
                                return 1;
                            }
                            else
                            {
                                return -1;
                            }
                        }
                    }
                    else
                    {
                        return -1;
                    }

                }
                else if (bStart_RM2Ex && bTarget_RM1Ex)
                {
                    if (GlobalData.Current.mRMManager[2].CheckRMAutoMode()) //231127 RGJ SelectRMForJob 크레인 오토 모드 체크 -> 오토모드가 아니면 버퍼 안잡음
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "MCS Job : {0} Changed RM2 HandOver Job!", sJob.CommandID);
                        if (sJob.HandOverBufferItem == null)
                        {
                            //LogManager.WriteConsoleLog(eLogLevel.Info, "MCS Job : {0} Changed RM2 HandOver Job!", sJob.CommandID);
                            var bufferShelf = ShelfManager.Instance.GetProperBufferShelf(HandOverMinBay, HandOverMaxBay, sJob.DestBay, (sJob.SourceLevel + sJob.DestLevel) / 2);
                            if (bufferShelf != null)
                            {
                                sJob.HandOverStoredDest = bufferShelf.TagName; //버퍼 쉘프를 핸드 오버 목적지로 설정.
                                sJob.DestItem.NotifyScheduled(true);
                                sJob.HandOverBufferItem.NotifyScheduled(true);
                                sJob.SubJob = eSubJobType.HandOver;
                                LogManager.WriteConsoleLog(eLogLevel.Info, "Set HandoverJob5   Source : {0}  Buffer : {1}   Dest :{2}", sJob.Source, bufferShelf.TagName, sJob.Destination);
                                return 2;
                            }
                            else
                            {
                                return -1;
                            }
                        }
                    }
                    else
                    {
                        return -1;
                    }
                }

                //조건 체크
                if (bStart_RM1Ex) //출발지가 전용구역이면 계산 필요없음
                {
                    return 1;
                }

                if (bStart_RM2Ex) //출발지가 전용구역이면 계산 필요없음
                {
                    return 2;
                }

                if (bTarget_RM1Ex) //출발지가 공용 구역이고 도착지가 전용 구역이면 도착지 전용 랙마스터가 Get 작업해야함
                {
                    return 1;
                }

                if (bTarget_RM2Ex) //출발지가 공용 구역이고 도착지가 전용 구역이면 도착지 전용 랙마스터가 Get 작업해야함
                {
                    return 2;
                }

                //출발지와 도착지가 전부 공용 구역이라면 
                int estRM1Distance = CalcDistance(startBay, Rm1Bay);
                int estRM2Distance = CalcDistance(startBay, Rm2Bay);
                
                if(CurrentSCOPMode == eSC_OperationMode.FirstRMOnly) //1번만 고른다.
                {
                    return 1;
                }
                else if (CurrentSCOPMode == eSC_OperationMode.SecondRMOnly) //2번만 고른다.
                {
                    return 2;
                }
                else if (GlobalData.Current.mRMManager.FirstRM.CarrierExistSensor) //센서에 화물 감지상태
                {
                    if(GlobalData.Current.mRMManager.FirstRM.CarrierID == sJob.CarrierID) //CarrierID 매칭되면 그냥 자기가 해야함.
                    {
                        return 1;
                    }
                    else
                    {
                        return 2;
                    }

                }
                else if (GlobalData.Current.mRMManager.SecondRM.CarrierExistSensor) //센서에 화물 감지상태
                {
                    if (GlobalData.Current.mRMManager.SecondRM.CarrierID == sJob.CarrierID) //CarrierID 매칭되면 그냥 자기가 해야함.
                    {
                        return 2;
                    }
                    else
                    {
                        return 1;
                    }
                }
                else //일반 모드
                {
                    if (estRM1Distance <= estRM2Distance) //현재 위치 기준 가까운 랙마스터를 작업 할당
                    {
                        //추후 동적 작업 상태까지 계산 해서 랙마 선정 예정 
                        //CalcEstimateWorkTime()
                        //일단 거리기준으로 구현 
                        return 1;
                    }
                    else
                    {
                        return 2;
                    }
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                return -1;
            }
        }


        private int CalcEstimateWorkTime()
        {
            return -1;
        }
        private int CalcProperPushBay(int BaseBay, bool FirstRMPushing)
        {
            int pushBay = BaseBay;
            if (FirstRMPushing) //대상이 첫번재 크레인
            {
                pushBay -= RM_RangeMargin;
                if (pushBay < 1)
                {
                    pushBay = 1;
                }
            }
            else //대상이 두번재 크레인
            {
                pushBay += RM_RangeMargin;
                if (pushBay > ShelfMaxBay)
                {
                    pushBay = ShelfMaxBay;
                }
            }
            return pushBay;
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
                            //핸드 오버 1차 완료 
                            if (_RM1_OnProcessJob.SubJob == eSubJobType.HandOver && _RM1_OnProcessJob.CarrierLoc == _RM1_OnProcessJob.HandOverStoredDest && !_RM1_OnProcessJob.MCSJobAbortReq) //핸드오버 잡을 완료 했으면 목적지와 출발지를 재설정
                            {
                                _RM1_OnProcessJob.TCStatus = eTCState.PAUSED;
                                _RM1_OnProcessJob.HandOverBufferItem.NotifyScheduled(true);
                                _RM1_OnProcessJob.DestItem.NotifyScheduled(true);
                                _RM1_OnProcessJob.AssignedRM = null;
                                _RM1_OnProcessJob = null;
                                JobTaskRM1 = null;
                                GlobalData.Current.HandOverCounter++;
                            }
                            else if (_RM1_OnProcessJob.SubJob == eSubJobType.AlterStore && !_RM1_OnProcessJob.MCSJobAbortReq) //대체 보관 작업을 완료 했다면 출발지를 재설정.
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
                                if (_RM1_OnProcessJob.DestItem is CV_BaseModule == false) //컨베이어로 간건 컨베이어가 삭제처리함.
                                {
                                    //220628 조숭진 db에 들어가지 않은 job이므로 지우려하면 insert 됨.
                                    //if (_RM1_OnProcessJob.SubJob != eSubJobType.Push && _RM1_OnProcessJob.SubJob != eSubJobType.HandOver)
                                    if (JobTaskRM1.IsCompleted && _RM1_OnProcessJob.JobType != "MOVE")
                                        JobList.DeleteMcsJob(_RM1_OnProcessJob);
                                    else
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
                else if (JobTaskRM1.IsCompleted && JobTaskRM1.Result.JobResult != eJob_Result.Complete) //태스크 완료 되었지만 작업은 실패
                {
                    //"테스크 동작이 실패하였습니다.";
                }
            }
            #endregion

            #region RM2 완료 체크 및 후작업
            if (JobTaskRM2 != null)
            {

                if (JobTaskRM2.IsFaulted)       //태스크 동작중 예외발생
                {
                    //"태스크 동작중 미처리 예외가 발생하었습니다. 동작 상태를 확인하십시오.";
                    if (_RM2_OnProcessJob != null)
                    {
                        //SCWriteTransferLog(_RM2_OnProcessJob, "FALUT");
                        _RM2_OnProcessJob.TCStatus = eTCState.PAUSED;
                        _RM2_OnProcessJob.Step = enumScheduleStep.None;
                        _RM2_OnProcessJob = null;
                        JobTaskRM2 = null; //240705 RGJ JobTaskRM1 = null  잘못 설정함 2로 변경. 
                    }
                }
                else if (JobTaskRM2.IsCanceled)     //테스크 취소 됨
                {
                    //SCWriteTransferLog(_RM2_OnProcessJob, "CANCEL");
                    //"테스크  동작이 취소 되었습니다. 동작 상태를 확인하십시오.";
                }
                else if (JobTaskRM2.IsCompleted) //태스크 완료 되었고 작업 성공
                {
                    eJob_Result JR2 = JobTaskRM2.Result.JobResult;
                    switch (JR2)
                    {
                        case eJob_Result.Complete: //완료되면 작업 목록에서 삭제
                            //핸드 오버 1차 완료 
                            if (_RM2_OnProcessJob.SubJob == eSubJobType.HandOver && _RM2_OnProcessJob.CarrierLoc == _RM2_OnProcessJob.HandOverStoredDest && !_RM2_OnProcessJob.MCSJobAbortReq)
                            {
                                _RM2_OnProcessJob.TCStatus = eTCState.PAUSED;
                                //_RM2_OnProcessJob.SourceItem.NotifyScheduled(true)
                                _RM2_OnProcessJob.HandOverBufferItem.NotifyScheduled(true);
                                _RM2_OnProcessJob.DestItem.NotifyScheduled(true);
                                _RM2_OnProcessJob.AssignedRM = null;
                                _RM2_OnProcessJob = null;
                                JobTaskRM2 = null;
                                GlobalData.Current.HandOverCounter++;
                            }
                            else if (_RM2_OnProcessJob.SubJob == eSubJobType.AlterStore && !RM2_OnProcessJob.MCSJobAbortReq) //대체 보관 작업을 완료 했다면 출발지를 재설정.
                            {
                                //SCWriteTransferLog(_RM2_OnProcessJob, "COMPLETE");
                                //_RM2_OnProcessJob.SubJob = eSubJobType.None;
                                _RM2_OnProcessJob.TCStatus = eTCState.PAUSED;

                                //_RM2_OnProcessJob.Source = _RM1_OnProcessJob.AltShelfDestination;
                                _RM2_OnProcessJob.AlterShelfItem.NotifyScheduled(true);
                                _RM2_OnProcessJob.DestItem.NotifyScheduled(true);

                                _RM2_OnProcessJob.AltShelfDestination = string.Empty;
                                _RM2_OnProcessJob.AssignedRM = null;
                                _RM2_OnProcessJob = null;
                                JobTaskRM2 = null;
                            }
                            else
                            {
                                //SCWriteTransferLog(_RM2_OnProcessJob, "COMPLETE");

                                if (_RM2_OnProcessJob.SubJob == eSubJobType.Push)
                                {
                                    GlobalData.Current.PushCounter++;
                                }
                                else
                                {
                                    GlobalData.Current.TransferCounter++;
                                }
                                if (_RM2_OnProcessJob.DestItem is CV_BaseModule == false) //컨베이어로 간건 컨베이어가 삭제처리함.
                                {
                                    //220628 조숭진 db에 들어가지 않은 job이므로 지우려하면 insert 됨.
                                    //if (_RM2_OnProcessJob.SubJob != eSubJobType.Push && _RM2_OnProcessJob.SubJob != eSubJobType.HandOver)
                                    if (JobTaskRM2.IsCompleted && _RM2_OnProcessJob.JobType != "MOVE")
                                        JobList.DeleteMcsJob(_RM2_OnProcessJob);
                                    else
                                        JobList.DeleteMcsJob(_RM2_OnProcessJob, _RM2_OnProcessJob.SubJob != eSubJobType.None);
                                }
                                _RM2_OnProcessJob.AssignedRM = null;
                                _RM2_OnProcessJob = null;
                                JobTaskRM2 = null;
                            }
                            break;
                        case eJob_Result.Aborted: //작업 중단 되었으면 작업 삭제하고 새로운 커맨드 대기필요
                            if (_RM2_OnProcessJob != null)
                            {
                                if (_RM2_OnProcessJob.VoidJob == false) //작업스케쥴링 도중 삭제된경우 처리 추가.
                                {
                                    //SCWriteTransferLog(_RM2_OnProcessJob, "ABORT");
                                    JobList.DeleteMcsJob(_RM2_OnProcessJob);
                                }
                                _RM2_OnProcessJob = null;
                                JobTaskRM2 = null;
                            }
                            break;
                        case eJob_Result.Paused: //작업 일시 정지 되었으면 대기
                            if (_RM2_OnProcessJob != null)
                            {
                                //SCWriteTransferLog(_RM2_OnProcessJob, "PAUSED");
                                _RM2_OnProcessJob.TCStatus = eTCState.PAUSED;
                                _RM2_OnProcessJob.Step = enumScheduleStep.None;
                                _RM2_OnProcessJob = null;
                                JobTaskRM2 = null;
                            }
                            break;
                        case eJob_Result.ErrorOccured:
                            if (_RM2_OnProcessJob != null)
                            {
                                //SCWriteTransferLog(_RM2_OnProcessJob, "ERROR");
                                _RM2_OnProcessJob.AssignedRM.RemoveCraneCommand();      //가지고 있는 command도 버리자. _RM1_OnProcessJob가 null이 되지만 command는 남아있더라.
                                _RM2_OnProcessJob.TCStatus = eTCState.PAUSED;
                                _RM2_OnProcessJob.Step = enumScheduleStep.None;
                                _RM2_OnProcessJob = null;
                                JobTaskRM2 = null; //240705 RGJ JobTaskRM1 = null  잘못 설정함 2로 변경. 
                            }
                            break;
                        case eJob_Result.TimeOut: //추후 구현
                            break;
                        default:
                            break;
                    }
                }
                else if (JobTaskRM2.IsCompleted && JobTaskRM2.Result.JobResult != eJob_Result.Complete) //태스크 완료 되었지만 작업은 실패
                {
                    //"테스크 동작이 실패하였습니다.";
                }
            }
            #endregion

            #region 가비지 작업 삭제
            var gabageJob = JobList.Where(j => j.Step == enumScheduleStep.CraneJobComplete && j.TCStatus == eTCState.NONE).FirstOrDefault();
            if (gabageJob != null)
            {
                if (gabageJob != RM1_OnProcessJob && gabageJob != RM2_OnProcessJob)
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
            var Job2 = RM2_OnProcessJob?.CommandID;
            if (Job1 == CommandID)
            {
                return true;
            }
            else if (Job2 == CommandID)
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
            var RM1 = GlobalData.Current.mRMManager["RM1"];
            var RM2 = GlobalData.Current.mRMManager["RM2"];

            int[] bufferbayarray = new int[MaxBay + 2];

            for (int i = 0; i < MaxBay + 2; i++)
            {
                if (RM1_ExclusiveBay > i)
                {
                    BayReserveArray[i] = new ReserveToken(1, true);
                    continue;
                }
                if (RM2_ExclusiveBay < i)
                {
                    BayReserveArray[i] = new ReserveToken(2, true);
                    continue;
                }
                BayReserveArray[i] = new ReserveToken(0, false);
                bufferbayarray[i] = i;
            }


            //SetBayReserveRange(RM1.RMNumber, RM1.CurrentBay - RM_ExtendSize, RM1.CurrentBay + RM_ExtendSize); //현재 위치 영역 예약
            //SetBayReserveRange(RM2.RMNumber, RM2.CurrentBay - RM_ExtendSize, RM2.CurrentBay + RM_ExtendSize);
        }
        //220823 조숭진 config로 수정 e

        //스케쥴러 중단 상태 판단.
        public override bool CheckSchedulerPaused()
        {
            bool NoJobProcess = !IsRM1JobProcessing && !IsRM2JobProcessing;
            return !StartSignal && NoJobProcess;
        }

        /// <summary>
        /// 크레인 싱글 모드 듀얼 모드 변경
        /// </summary>
        /// <param name="Mode"></param>
        /// <returns></returns>
        public override eSC_OperationModeResultCode ChangeCraneOperationMode(eSC_OperationMode Mode)
        {
            lock (OPChangeLock)
            {
                try
                {
                    //OK = 0, //정상변경
                    //NeedPauseState = 1,//현재 포즈 상태에서만 변경 가능
                    //CranePosition = 2, //다운 크레인 위치가 양끝방향 위치가 아님

                    //2024.09.19 lim, PLC에서 Manual 상태 일때 모드 변경함 따로 퍼즈 상태인지 확인 할 필요 없음
                    //변경전 인터락 체크
                    //if (GlobalData.Current.MainBooth.SCState != eSCState.PAUSED)
                    //{
                    //    return eSC_OperationModeResultCode.NeedPauseState;
                    //}
                    switch (Mode) // 위치 체크
                    {
                        case eSC_OperationMode.NormalMode: //모드 원복 시킬때는 위치 상관없음
                            foreach (ShelfItem sItem in ShelfManager.Instance.AllData) //모든 쉘프 블럭 해제
                            {
                                sItem.SHELFBLOCK = false;
                                GlobalData.Current.ShelfMgr.SaveShelfData(sItem);       //2024.09.21 lim, 수정 후 DB 업데이트 필요
                                Thread.Sleep(20);
                            }
                            foreach (CV_BaseModule cItem in GlobalData.Current.PortManager.AllCVList)  //모든 포트 블럭 해제
                            {
                                cItem.CVBLOCK = false;
                                Thread.Sleep(20);
                            }
                            break;
                        case eSC_OperationMode.FirstRMOnly:
                            if (GlobalData.Current.mRMManager.SecondRM.CurrentBay < ShelfMaxBay) //세컨드 RM 위치가 끝위치가 아님
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "ChangeCraneOperationMode Change Fail Crane 2 CurrentBay value Error  : {0}", GlobalData.Current.mRMManager.SecondRM.CurrentBay);
                                return eSC_OperationModeResultCode.CheckCranePosition;
                            }
                            foreach (ShelfItem sItem in ShelfManager.Instance.AllData.Where(s => s.ShelfBay <= RM1_ExclusiveBay)) //Crane 1 전용 쉘프 블럭 해제
                            {
                                sItem.SHELFBLOCK = false;
                                GlobalData.Current.ShelfMgr.SaveShelfData(sItem);       //2024.09.21 lim, 수정 후 DB 업데이트 필요
                                Thread.Sleep(20);
                            }
                            foreach (CV_BaseModule cItem in GlobalData.Current.PortManager.AllCVList.Where(c => c.Position_Bay <= RM1_ExclusiveBay && c.CVModuleType == eCVType.RobotIF))  //Crane 1 전용 포트 블럭 해제
                            {
                                cItem.CVBLOCK = false;
                                Thread.Sleep(20);
                            }
                            foreach (ShelfItem sItem in ShelfManager.Instance.AllData.Where(s => s.ShelfBay >= RM2_ExclusiveBay)) //Crane 2 전용 쉘프 블럭 설정
                            {
                                sItem.SHELFBLOCK = true;
                                GlobalData.Current.ShelfMgr.SaveShelfData(sItem);       //2024.09.21 lim, 수정 후 DB 업데이트 필요
                                Thread.Sleep(20);
                            }
                            foreach (CV_BaseModule cItem in GlobalData.Current.PortManager.AllCVList.Where(c => c.Position_Bay >= RM2_ExclusiveBay))  //Crane 2 전용 포트 블럭 설정
                            {
                                cItem.CVBLOCK = true;
                                Thread.Sleep(20);
                            }
                            break;
                        case eSC_OperationMode.SecondRMOnly:
                            if (GlobalData.Current.mRMManager.FirstRM.CurrentBay > 1) //퍼스트 RM 위치가 끝위치가 아님
                            {
                                LogManager.WriteConsoleLog(eLogLevel.Info, "ChangeCraneOperationMode Change Fail Crane 1 CurrentBay value Error : {0}", GlobalData.Current.mRMManager.FirstRM.CurrentBay);
                                return eSC_OperationModeResultCode.CheckCranePosition;
                            }
                            foreach (ShelfItem sItem in ShelfManager.Instance.AllData.Where(s => s.ShelfBay <= RM1_ExclusiveBay)) //Crane 1 전용 쉘프 블럭 설정
                            {
                                sItem.SHELFBLOCK = true;
                                GlobalData.Current.ShelfMgr.SaveShelfData(sItem);       //2024.09.21 lim, 수정 후 DB 업데이트 필요
                                Thread.Sleep(20);
                            }
                            foreach (CV_BaseModule cItem in GlobalData.Current.PortManager.AllCVList.Where(c => c.Position_Bay <= RM1_ExclusiveBay && c.CVModuleType == eCVType.RobotIF))  //Crane 1 전용 포트 블럭 설정
                            {
                                cItem.CVBLOCK = true;
                                Thread.Sleep(20);
                            }
                            foreach (ShelfItem sItem in ShelfManager.Instance.AllData.Where(s => s.ShelfBay >= RM2_ExclusiveBay)) //Crane 2 전용 쉘프 블럭 해제
                            {
                                sItem.SHELFBLOCK = false;
                                GlobalData.Current.ShelfMgr.SaveShelfData(sItem);       //2024.09.21 lim, 수정 후 DB 업데이트 필요
                                Thread.Sleep(20);
                            }
                            foreach (CV_BaseModule cItem in GlobalData.Current.PortManager.AllCVList.Where(c => c.Position_Bay >= RM2_ExclusiveBay))  //Crane 2 전용 포트 블럭 해제
                            {
                                cItem.CVBLOCK = false;
                                Thread.Sleep(20);
                            }
                            break;
                    }
                    _CurrentSCOPMode = Mode;
                    return eSC_OperationModeResultCode.OK;
                }
                catch (Exception ex)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "ChangeCraneOperationMode 변경 에러 발생");
                    LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                    return eSC_OperationModeResultCode.ErrorOccurred;
                }
            }
        }

        public override int CheckHandOverRequire(ICarrierStoreAble Source, ICarrierStoreAble Dest)
        {
            bool bStart_RM1Ex = false;
            bool bStart_RM2Ex = false;
            bool bTarget_RM1Ex = false;
            bool bTarget_RM2Ex = false;
            //출발지와 목적지 Bay 를 계산해서 랙마스터를 선정.
            if (Source == null || Dest == null) //출발지 또는 목적지 이상
            {
                return -1;
            }
            int startBay = Source.iBay;
            int targetBay = Dest.iBay; //도착지

            //출발지 목적지 체크
            if (RM1_ExclusiveBay >= startBay)
            {
                bStart_RM1Ex = true;
            }
            if (RM2_ExclusiveBay <= startBay)
            {
                bStart_RM2Ex = true;
            }

            if (RM1_ExclusiveBay >= targetBay)
            {
                bTarget_RM1Ex = true;
            }
            if (RM2_ExclusiveBay <= targetBay)
            {
                bTarget_RM2Ex = true;
            }

            if (bStart_RM1Ex && bTarget_RM2Ex)
            {
                return 1;
            }
            else if (bStart_RM2Ex && bTarget_RM1Ex)
            {
                return 2;
            }
            return 0;
        }

        public override bool CheckShelfExclusiveCraneAccess(ShelfItem sItem)
        {
            if (sItem == null)
            {
                return false;
            }
            int startBay = sItem.ShelfBay;


            //출발지 목적지 체크
            if (RM1_ExclusiveBay >= startBay) //Crane 1번 전용
            {
                return true;
            }
            else if (RM2_ExclusiveBay <= startBay) //Crane 2번 전용
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        public override List<RMModuleBase> GetNotAssignRM()
        {
            List<RMModuleBase> temp = new List<RMModuleBase>();
            if (!IsRM1JobProcessing)
                temp.Add(GlobalData.Current.mRMManager.FirstRM);
            if (!IsRM2JobProcessing)
                temp.Add(GlobalData.Current.mRMManager.SecondRM);

            return temp;
        }

        public override eCraneExZone GetCraneExZone(ICarrierStoreAble Source)
        {
            if(Source == null) //240910 RGJ GetCraneExZone 널 조건 추가.
            {
                return eCraneExZone.NoAccessAble;
            }
            if(Source is RMModuleBase rm)
            {
                if(rm.IsFirstRM)
                {
                    return eCraneExZone.FirstCraneZone;
                }
                else
                {
                    return eCraneExZone.SecondCraneZone;
                }
            }

            if(0 <= Source.iBay  && Source.iBay <= RM1_ExclusiveBay)
            {
                return eCraneExZone.FirstCraneZone;
            }
            else if(Source.iBay >= RM2_ExclusiveBay)
            {
                return eCraneExZone.SecondCraneZone;
            }
            else if(RM1_ExclusiveBay < Source.iBay && Source.iBay < RM2_ExclusiveBay)
            {
                return eCraneExZone.SharedZone;
            }
            else
            {
                return eCraneExZone.NoAccessAble;
            }
        }
        public override bool CheckCraneReachAble(RMModuleBase Crane, ICarrierStoreAble Target)
        {
            if(Crane == null || Target == null)
            {
                return false;
            }
            if(Crane.ModuleName == Target.GetTagName())
            {
                return true;
            }
            else if(Crane.IsFirstRM && GetCraneExZone(Target) == eCraneExZone.FirstCraneZone ||
                GetCraneExZone(Target) == eCraneExZone.SharedZone)
            {
                return true;
            }
            else if(!Crane.IsFirstRM && GetCraneExZone(Target) == eCraneExZone.SecondCraneZone ||
                GetCraneExZone(Target) == eCraneExZone.SharedZone)
            {
                return true;
            }
            return false;
        }
    }
}
