using BoxPrint.Database;
using BoxPrint.Log;
using BoxPrint.Modules;
using BoxPrint.Modules.Conveyor;
using BoxPrint.Modules.RM;
using BoxPrint.Modules.Shelf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;


namespace BoxPrint.DataList.MCS
{
    public class McsJobManager : ObservableList<McsJob>     //220603 조숭진 FastObservableCollection -> ObservableList 변경
    {
        public readonly int cHighPriority = 99;
        public readonly int cLowPriority = 1;

        public McsJob GetCarrierJob(string CarrierID)
        {
            return this.FirstOrDefault(j => j.CarrierID == CarrierID);
        }

        //SuHwan_20221006 : [ServerClient]
        public McsJob GetCommandIDJob(string CommandID)
        {
            return this.FirstOrDefault(j => j.CommandID == CommandID);
        }

        //SuHwan_20221031 : [ServerClient]
        public McsJob GetCurrentJob(string rcvRMName)
        {
            return this.FirstOrDefault(j => j.AssignRMName == rcvRMName);
        }

        //20231025 RGJ 해당 RM 을 대상으로 하는 Push 잡이 있으면 리턴한다.
        public McsJob GetTargetRMPushJob(int RMNumber)
        {
            return this.FirstOrDefault(j => j.TargetRMNumber == RMNumber);
        }

        public bool IsJobListCheck(string source, string dest, string carrierID)
        {
            //return this.Where(j => j.Source == source  || j.Destination == dest || j.CarrierID == carrierID).Count() > 0;
            return this.Where(j => j.CarrierID == carrierID).Count() > 0;
        }
        public bool IsJobListCheck(string source, string carrierID)
        {
            //return this.Where(j => j.Source == source || j.CarrierID == carrierID).Count() > 0;
            return this.Where(j => j.CarrierID == carrierID).Count() > 0;
        }
        public bool IsJobListCheck_DUP_ID(string Location, string carrierID)
        {
            //return this.Where(j => j.Source == source || j.CarrierID == carrierID).Count() > 0;
            //var job = this.Where(j => j.CarrierID == carrierID).FirstOrDefault();
            return this.Where(j => j.CarrierLocationItem != null && j.CarrierLoc != Location && j.CarrierID == carrierID).Count() > 0;
        }
        public bool IsCommandIDContain(string CommandID)
        {
            return this.Where(j => j.CommandID == CommandID).Count() > 0;
        }

        public bool IsCarrierIDContain(string CarrierID)
        {
            return this.Where(j => j.CarrierID == CarrierID).Count() > 0;
        }

        private string MakeCommandID(string CarrierID)
        {
            string CID = string.Format("MNL{0}_{1}", DateTime.Now.ToString("yyyyMMddHHmmss"), CarrierID);

            while (IsCommandIDContain(CID)) //혹시 중복이 생길수도 있기에 검사해본다.
            {
                Thread.Sleep(20);
                CID = string.Format("MNL{0}_{1}", DateTime.Now.ToString("yyyyMMddHHmmss"), CarrierID);
            }

            return CID;

        }

        public void CreateRandomJob()
        {
            Random r = new Random(DateTime.Now.Millisecond);
            var FItmes = ShelfManager.Instance.FrontData.Items.Where(s => s.CheckCarrierExist() && s.Scheduled == false && s.ShelfAvailable); //작업에 선택할 소스 쉘프
            var RItmes = ShelfManager.Instance.RearData.Items.Where(s => s.CheckCarrierExist() && s.Scheduled == false && s.ShelfAvailable);

            ShelfItem SourceShelf = null;
            ShelfItem DestShelf = null;

            if (r.Next(0, 2) == 0) //랜덤하게 고른다.
            {
                if (FItmes.Count() != 0)
                {
                    SourceShelf = FItmes.ElementAt(r.Next(0, FItmes.Count()));
                }
                else if (RItmes.Count() != 0)
                {
                    SourceShelf = RItmes.ElementAt(r.Next(0, RItmes.Count()));
                }
            }
            else
            {
                if (RItmes.Count() != 0)
                {
                    SourceShelf = RItmes.ElementAt(r.Next(0, RItmes.Count()));
                }
                else if (FItmes.Count() != 0)
                {
                    SourceShelf = FItmes.ElementAt(r.Next(0, FItmes.Count()));
                }
            }

            if (SourceShelf == null)
                return;

            eCarrierSize SourceCarrierSize = SourceShelf.CarrierSize; //사이즈 인터락 추가.

            var T_FItmes = ShelfManager.Instance.FrontData.Items.Where(s => !s.CheckCarrierExist() && s.SHELFTYPE < 10 && s.Scheduled == false && s.ShelfAvailable && s.CheckCarrierSizeAcceptable(SourceCarrierSize)); //작업에 선택할 목표 쉘프
            var T_RItmes = ShelfManager.Instance.RearData.Items.Where(s => !s.CheckCarrierExist() && s.SHELFTYPE < 10 && s.Scheduled == false && s.ShelfAvailable && s.CheckCarrierSizeAcceptable(SourceCarrierSize));

            if (r.Next(0, 2) == 0)
            {
                if (T_FItmes.Count() > 0)
                {
                    DestShelf = T_FItmes.ElementAt(r.Next(0, T_FItmes.Count()));
                }
            }
            else
            {
                if (T_RItmes.Count() > 0)
                {
                    DestShelf = T_RItmes.ElementAt(r.Next(0, T_RItmes.Count()));
                }
            }



            if (SourceShelf != null && DestShelf != null)
            {
                var j = new McsJob();
                j.CommandID = MakeCommandID(SourceShelf.CarrierID);
                j.CreateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                j.CarrierID = SourceShelf.CarrierID;
                j.Source = SourceShelf.TagName;
                j.Destination = DestShelf.TagName;
                j.Priority = r.Next(1, 100).ToString();
                j.TransferState = 1;
                j.JobType = "TRANSFER";
                j.CarrierType = "Tray";
                j.TCStatus = eTCState.QUEUED;
                j.JobFrom = eScheduleJobFrom.Operator;
                j.SourceGroup = "SHELF";
                j.DestGroup = "SHELF";
                j.LotIDList = string.Empty;
                j.MnlCmdJobCreate = true;
                AddMcsJob(j);

                SourceShelf.NotifyScheduled(true);
                DestShelf.NotifyScheduled(true);

                //GlobalData.Current.DBManager.DbSetJobInfo(j, false);        //220322 조숭진 job관련 db 저장
                GlobalData.Current.DBManager.DbSetProcedureJobInfo(j, false);
            }
        }
        private int CycleCount = 0;
        public void CreateCycleMoveJob()
        {
            try
            {
                if(GlobalData.Current.SCSType == eSCSType.Dual) //듀얼 크레인은 실행하면 안됨.
                {
                    return;
                }
                bool IsCraneOPPos = GlobalData.Current.mRMManager.FirstRM.CurrentBay == 1;
                int NextBay = IsCraneOPPos ? ShelfManager.Instance.GetMaxBay() : 1;
                var sItem = ShelfManager.Instance.AllData.Items.Where(s => s.ShelfAvailable && s.ShelfBay == NextBay).FirstOrDefault();
                if (sItem != null)
                {
                    var j = new McsJob();
                    j.CommandID = MakeCommandID("CYCLETEST_"+ CycleCount++);
                    j.CreateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    j.CarrierID = "NONE";
                    j.Source = GlobalData.Current.mRMManager.FirstRM.ModuleName;
                    j.Destination = sItem.iLocName;
                    j.Priority = "1";
                    j.TransferState = 1;
                    j.JobType = "MOVE";
                    j.CarrierType = "Tray";
                    j.TCStatus = eTCState.QUEUED;
                    j.JobFrom = eScheduleJobFrom.Operator;
                    j.SourceGroup = GlobalData.Current.mRMManager.FirstRM.iGroup;
                    j.DestGroup = sItem.iGroup;
                    j.LotIDList = string.Empty;
                    j.MnlCmdJobCreate = true;
                    AddMcsJob(j);
                }
            }
            catch(Exception )
            {

            }

        }

        public void CreateHandOffJob()
        {
            Random r = new Random(DateTime.Now.Millisecond);
            bool Forward = r.Next(0, 2) == 0;

            int SourceBay;
            int DestBay;

            if (Forward)
            {
                SourceBay = 1;
                DestBay = ShelfManager.Instance.AllData.MaxBay;
            }
            else
            {
                SourceBay = ShelfManager.Instance.AllData.MaxBay;
                DestBay = 1;
            }

            var FItmes = ShelfManager.Instance.FrontData.Items.Where(s => s.CheckCarrierExist() && s.Scheduled == false && s.ShelfAvailable && s.ShelfBay == SourceBay); //작업에 선택할 소스 쉘프
            var RItmes = ShelfManager.Instance.RearData.Items.Where(s => s.CheckCarrierExist() && s.Scheduled == false && s.ShelfAvailable && s.ShelfBay == SourceBay);

            var T_FItmes = ShelfManager.Instance.FrontData.Items.Where(s => !s.CheckCarrierExist() && s.SHELFTYPE < 10 && s.Scheduled == false && s.ShelfAvailable && s.ShelfBay == DestBay); //작업에 선택할 목표 쉘프
            var T_RItmes = ShelfManager.Instance.RearData.Items.Where(s => !s.CheckCarrierExist() && s.SHELFTYPE < 10 && s.Scheduled == false && s.ShelfAvailable && s.ShelfBay == DestBay);

            ShelfItem SourceShelf = null;
            ShelfItem DestShelf = null;

            if (r.Next(0, 2) == 0) //랜덤하게 고른다.
            {
                if (FItmes.Count() != 0)
                {
                    SourceShelf = FItmes.ElementAt(r.Next(0, FItmes.Count()));
                }
                else if (RItmes.Count() != 0)
                {
                    SourceShelf = RItmes.ElementAt(r.Next(0, RItmes.Count()));
                }
            }
            else
            {
                if (RItmes.Count() != 0)
                {
                    SourceShelf = RItmes.ElementAt(r.Next(0, RItmes.Count()));
                }
                else if (FItmes.Count() != 0)
                {
                    SourceShelf = FItmes.ElementAt(r.Next(0, FItmes.Count()));
                }
            }

            if (r.Next(0, 2) == 0)
            {
                if (T_FItmes.Count() > 0)
                {
                    DestShelf = T_FItmes.ElementAt(r.Next(0, T_FItmes.Count()));
                }
            }
            else
            {
                if (T_RItmes.Count() > 0)
                {
                    DestShelf = T_RItmes.ElementAt(r.Next(0, T_RItmes.Count()));
                }
            }



            if (SourceShelf != null && DestShelf != null)
            {
                var j = new McsJob();
                j.CommandID = MakeCommandID(SourceShelf.CarrierID);
                j.CreateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                j.CarrierID = SourceShelf.CarrierID;
                j.Source = SourceShelf.TagName;
                j.Destination = DestShelf.TagName;
                j.Priority = r.Next(1, 100).ToString();
                j.TransferState = 1;
                j.JobType = "TRANSFER";
                j.CarrierType = "Tray";
                j.TCStatus = eTCState.QUEUED;
                j.JobFrom = eScheduleJobFrom.Operator;
                j.SourceGroup = "SHELF";
                j.DestGroup = "SHELF";
                j.LotIDList = string.Empty;
                j.MnlCmdJobCreate = true;
                AddMcsJob(j);

                SourceShelf.NotifyScheduled(true);
                DestShelf.NotifyScheduled(true);

                //GlobalData.Current.DBManager.DbSetJobInfo(j, false);        //220322 조숭진 job관련 db 저장
                GlobalData.Current.DBManager.DbSetProcedureJobInfo(j, false);
            }
        }


        public void CreatePush_SubJob(int PushRMNumber, int PushTargetBay)
        {
            //230803 RGJ DeadZone 만 Push 제한함
            var DestShelf = ShelfManager.Instance.AllData.Items.Where(s => s.ShelfBay == PushTargetBay && !s.DeadZone).OrderBy(s=>s.ShelfLevel).FirstOrDefault(); //작업에 선택할 목표 쉘프
            if (DestShelf == null)
            {
                return;// 적합한 쉘프가 없어서 작업 불가능
            }
            var j = new McsJob();
            j.CommandID = MakeCommandID("PUSH");
            j.CreateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            j.CarrierID = "";
            j.Source = GlobalData.Current.mRMManager[PushRMNumber].GetCurrentPositionTag();
            j.Destination = DestShelf.TagName;
            j.Priority = cHighPriority.ToString(); //해당작업은 무조건 0순위로 처리해야한다.
            j.TransferState = 1;
            j.JobType = "MOVE";
            j.CarrierType = "Tray";
            j.TCStatus = eTCState.QUEUED;
            j.JobFrom = eScheduleJobFrom.EQItSelf;
            j.SourceGroup = "SHELF";
            j.DestGroup = "SHELF";
            j.SubJob = eSubJobType.Push;
            j.TargetRMNumber = PushRMNumber;
            AddMcsJob(j);
        }
     
        public void CreateGetPortJob(CV_BaseModule PortModule,ShelfItem DestShelf = null)
        {
            //포트에서 뜨는건 명령 여러개 걸리는 케이스 있음. 해당 조건 필요 없음. 
            //bool AlreadyJobExist = this.Where(j => j.Source == PortModule.ModuleName).Count() > 0;
            //if (AlreadyJobExist)
            //{
            //    return; 
            //}
            Random r = new Random();
            if (PortModule == null)
            {
                return;
            }
            eCarrierSize CarrierWidth = PortModule.CurrentCarrier.CarrierSize;



            if (PortModule != null && DestShelf != null)
            {
                var j = new McsJob();
                j.CommandID = MakeCommandID(PortModule.CurrentCarrier?.CarrierID);
                j.CreateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                j.CarrierID = PortModule.CurrentCarrier?.CarrierID;
                j.Source = PortModule.ModuleName;
                j.Destination = DestShelf.TagName;
                j.Priority = r.Next(1,10).ToString();
                j.TransferState = 1;
                j.JobType = "TRANSFER";
                j.CarrierType = "Tray";
                j.TCStatus = eTCState.QUEUED;
                j.JobFrom = eScheduleJobFrom.EQItSelf;
                j.SourceGroup = "PORT";
                j.DestGroup = "SHELF";
                j.LotIDList = string.Empty;
                j.MnlCmdJobCreate = true;
                AddMcsJob(j);
                DestShelf.NotifyScheduled(true);

                //GlobalData.Current.DBManager.DbSetJobInfo(j, false);        //220322 조숭진 job관련 db 저장
                GlobalData.Current.DBManager.DbSetProcedureJobInfo(j, false);
            }
        }

        public void CreateOldestCarrierUnloadJob()
        {
            Random r = new Random();
            var TShelfs = ShelfManager.Instance.AllData.Items.Where(s => s.CheckCarrierExist() && s.Scheduled == false && s.ShelfAvailable && s.CarrierState == eCarrierState.COMPLETED);

            if (TShelfs.Count() == 0)
            {
                return;
            }

            ShelfItem TS = TShelfs.OrderBy(o => o.InstallTime).First();
            bool AlreadyJobExist = this.Where(j => j.Source == TS.TagName).Count() > 0;
            if (AlreadyJobExist)
            {
                return;
            }

            eCarrierSize Size = TS.InSlotCarrier.CarrierSize;

            CV_BaseModule DestPort = GlobalData.Current.PortManager.GetProperOutPort(TS, Size);

            if (TS != null && DestPort != null)
            {
                var j = new McsJob();
                j.CommandID = MakeCommandID(TS.CarrierID);
                j.CreateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                j.CarrierID = TS.CarrierID;
                j.Source = TS.TagName;
                j.Destination = DestPort.ModuleName;
                j.Priority = "2";
                j.TransferState = 1;
                j.JobType = "TRANSFER";
                j.CarrierType = "Tray";
                j.TCStatus = eTCState.QUEUED;
                j.JobFrom = eScheduleJobFrom.EQItSelf;
                j.SourceGroup = "SHELF";
                j.DestGroup = "PORT";
                j.LotIDList = string.Empty;
                j.MnlCmdJobCreate = true;
                AddMcsJob(j);
                TS.NotifyScheduled(true);

                //GlobalData.Current.DBManager.DbSetJobInfo(j, false);        //220322 조숭진 job관련 db 저장
                GlobalData.Current.DBManager.DbSetProcedureJobInfo(j, false);
            }
        }

        /// <summary>
        /// 메뉴얼 반송 작업를 만든다.UI에서 체크 항목을 이쪽으로 옮김
        /// </summary>
        /// <param name="Source"></param>
        /// <param name="Dest"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public eManualJobCreateResult CreateManualTransferJob(ICarrierStoreAble Source, ICarrierStoreAble Dest,string TRCarrierID ,int priority,  bool MoveOnly ,int OptSelectCrane)
        {
            Random r = new Random();

            RMModuleBase OptionSelectRM = GlobalData.Current.mRMManager[OptSelectCrane];

            if(priority < 1 || priority > 99 ) //우선순위 체크
            {
                return eManualJobCreateResult.PriorityCheck;
            }
            
            CarrierItem SelectedCarrier = CarrierStorage.Instance.GetCarrierItem(TRCarrierID);
            if(Source is null) //출발지 유효성 체크
            {
                return eManualJobCreateResult.InvalidSource;
            }
            if(Dest is null) //도착지 유효성 체크
            {
                return eManualJobCreateResult.InvalidDestination;
            }
            if(SelectedCarrier is null && !MoveOnly) //캐리어 유무 체크
            {
                return eManualJobCreateResult.CarrierNotExistInSTK;
            }
            if(Source.GetTagName() == Dest.GetTagName()) //출발지랑 도착지가 같다
            {
                return eManualJobCreateResult.SourceAndDestinationEqual;
            }
            if(Source is RMModuleBase && Dest is RMModuleBase) //출발지랑 도착지가 둘다 Crane 이다.대체 반송하면 가능한 동작이긴하나 일단 허용안함.
            {
                return eManualJobCreateResult.CraneNotReachAble;
            }

            #region 이동 전용 커맨드
            if (MoveOnly)
            {
                RMModuleBase rm = Source as RMModuleBase;
                if (rm == null) //이동동작은 출발지가 크레인
                {
                    return eManualJobCreateResult.InvalidSource;
                }
                else if (!GlobalData.Current.Scheduler.CheckCraneReachAble(rm, Dest))
                {
                    return eManualJobCreateResult.CraneNotReachAble;
                }
                else if (IsJobListCheck(Source.GetTagName(), Dest.GetTagName(), TRCarrierID))       //이미 job list에 있는지 확인
                {
                    return eManualJobCreateResult.SameJobExist;
                }
                
                var ManualMoveJob = new McsJob();
                ManualMoveJob.CommandID = MakeCommandID(Source.GetCarrierID());
                ManualMoveJob.CreateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                ManualMoveJob.CarrierID = TRCarrierID;
                ManualMoveJob.Source = Source.GetTagName();
                ManualMoveJob.Destination = Dest.GetTagName();
                ManualMoveJob.Priority = priority.ToString();
                ManualMoveJob.TransferState = 1;
                ManualMoveJob.JobType = "MOVE";
                ManualMoveJob.CarrierType = "Tray";
                ManualMoveJob.TCStatus = eTCState.QUEUED;
                ManualMoveJob.JobFrom = eScheduleJobFrom.Operator;
                ManualMoveJob.SourceGroup = Source.iGroup;
                ManualMoveJob.DestGroup = Dest.iGroup;
                ManualMoveJob.LotIDList = string.Empty;
                AddMcsJob(ManualMoveJob);
                GlobalData.Current.DBManager.DbSetProcedureJobInfo(ManualMoveJob, false);
                return eManualJobCreateResult.Success;
            }
            #endregion

            #region 출발지 체크 항목
            if (Source is ShelfItem SItem)
            {
                if(!SItem.ShelfAvailable)
                {
                    return eManualJobCreateResult.SourceNotAvailable;
                }
                else if (!SItem.CheckCarrierExist())
                {
                    return eManualJobCreateResult.CarrierNotExistInSource;
                }
                else if (SItem.CarrierID != TRCarrierID)
                {
                    return eManualJobCreateResult.CarrierID_MisMatch;
                }
                else if (SItem.ShelfScheduled != eShelfScheduleState.NONE)
                {
                    return eManualJobCreateResult.SourceNotAvailable;
                }
                
                if (SItem.CarrierState == eCarrierState.COMPLETED || SItem.CarrierState == eCarrierState.ALTERNATE || SItem.CarrierState == eCarrierState.TRANSFERRING)
                {
                    //231219 RGJ 대체 상태일때 작업 지우면 반송 불가능 상태가 됨. Carrier State ALTERNATE 도 수동 반송 가능 하게 함.
                    //240326 RGJ TRANSFERRING 상태에서도 작업을 강제로 삭제하면 반송 불가능함. 이미 걸린 작업은 인터락이 있으므로 해당 조건도 반송 가능 상태로 둠.  
                }
                else
                {
                    return eManualJobCreateResult.CarrierNotReady;
                }
            }
            else if(Source is CV_BaseModule cv)
            {
                //2024.09.17 lim, 조범석 매니저 요청으로 소스 상태 인터락 제외 
                //if (!cv.CVAvailable)
                //{
                //    return eManualJobCreateResult.SourceNotAvailable;
                //}
                //else 
                if (!cv.CheckCarrierExist())
                {
                    return eManualJobCreateResult.CarrierNotExistInSource;
                }
                else if (cv.GetCarrierID() != TRCarrierID)
                {
                    return eManualJobCreateResult.CarrierID_MisMatch;
                }
                else if (SelectedCarrier.CarrierState != eCarrierState.WAIT_IN) //포트는 Wait In 상태 캐리어만 반송가능
                {
                    return eManualJobCreateResult.CarrierNotReady;
                }

                if (cv.CVModuleType != eCVType.RobotIF) //수동 포트는 작업을 내려야만 진행하므로 대응을 위해서 추가.
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "CreateManualTransferJob Dest Changed to {0} => {1}", cv.ModuleName, cv.GetConnectedRobotIFModuleName());
                    Source = GlobalData.Current.PortManager.GetCVModule(cv.GetConnectedRobotIFModuleName());
                }
            }
            else if(Source is RMModuleBase rm)
            {
                //2024.09.17 lim, 조범석 매니저 요청으로 소스 상태 인터락 제외 
                //if(rm.CraneState != eCraneUIState.ONLINE)
                //{
                //    return eManualJobCreateResult.SourceNotAvailable;
                //}
                //else 
                if (!rm.CheckCarrierExist())
                {
                    return eManualJobCreateResult.CarrierNotExistInSource;
                }
                else if (rm.GetCarrierID() != TRCarrierID)
                {
                    return eManualJobCreateResult.CarrierID_MisMatch;
                }
            }
            #endregion

            #region 도착지 체크 항목
            if (Dest is ShelfItem DSItem)
            {
                if (!DSItem.ShelfAvailable)
                {
                    return eManualJobCreateResult.DestinationNotAvailable;
                }
                else if (DSItem.CheckCarrierExist())
                {
                    return eManualJobCreateResult.DestinationNotAvailable;
                }
                else if (DSItem.ShelfScheduled != eShelfScheduleState.NONE)
                {
                    return eManualJobCreateResult.DestinationNotAvailable;
                }
                else if (!DSItem.CheckCarrierSizeAcceptable(SelectedCarrier.CarrierSize))
                {
                    return eManualJobCreateResult.CarrierSize_MisMatch;
                }
                else if(DSItem.HandOverProtect)
                {
                    return eManualJobCreateResult.DestShelfProtected;
                }
            }
            else if (Dest is CV_BaseModule Dcv)
            {
                //2024.09.17 lim, 조범석 매니저 요청으로 데스트 상태 인터락 제외 
                if (/*!Dcv.CVAvailable ||*/ Dcv.CVModuleType != eCVType.RobotIF || Dcv.IsInPort)
                {
                    return eManualJobCreateResult.DestinationNotAvailable;
                }
                else if (!Dcv.CheckCarrierSizeAcceptable(SelectedCarrier.CarrierSize))
                {
                    return eManualJobCreateResult.CarrierSize_MisMatch;
                }
                //가용조건은 스케쥴러가 이후 체크함.
            }
            else if (Dest is RMModuleBase rm)
            {
                if (rm.CraneState != eCraneUIState.ONLINE)
                {
                    return eManualJobCreateResult.DestinationNotAvailable;
                }
                //가용조건은 스케쥴러가 이후 체크함.
            }
            #endregion

            //전용 크레인을 지정했을경우 출발지에 접근 가능한지 체크
            if (OptionSelectRM != null && !GlobalData.Current.Scheduler.CheckCraneReachAble(OptionSelectRM, Source))
            {
                //접근 불가능한 케이스
                return eManualJobCreateResult.CraneNotReachAble;
            }

            if (IsJobListCheck(Source.GetTagName(), Dest.GetTagName(), TRCarrierID))       //이미 job list에 있는지 확인
            {
                return eManualJobCreateResult.SameJobExist;
            }

            //잡 생성
            var ManualTRJob = new McsJob();
            ManualTRJob.CommandID = MakeCommandID(TRCarrierID);
            ManualTRJob.CreateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            ManualTRJob.CarrierID = TRCarrierID;
            ManualTRJob.Source = Source.GetTagName();
            ManualTRJob.Destination = Dest.GetTagName();
            ManualTRJob.DestZoneName = Dest.iZoneName; //231121 RGJ 메뉴얼 반송 Job 도 DestZoneName 설정함. 
            ManualTRJob.Priority = priority.ToString();
            ManualTRJob.TransferState = 1;
            ManualTRJob.JobType = "TRANSFER";
            ManualTRJob.CarrierType = "Tray";
            ManualTRJob.TCStatus = eTCState.QUEUED;
            ManualTRJob.JobFrom = eScheduleJobFrom.Operator;
            ManualTRJob.SourceGroup = Source.iGroup;
            ManualTRJob.DestGroup = Dest.iGroup;
            ManualTRJob.AssignedRM = OptionSelectRM;
            ManualTRJob.LotIDList = string.Empty;
            ManualTRJob.MnlCmdJobCreate = true;

            AddMcsJob(ManualTRJob);
            Source.NotifyScheduled(true);
            Dest.NotifyScheduled(true);

            GlobalData.Current.DBManager.DbSetProcedureJobInfo(ManualTRJob, false);
            //GlobalData.Current.HSMS.SendS6F11(208, "JOBDATA", ManualTRJob);
            return eManualJobCreateResult.Success;

        }

        public eManualJobCreateResult CreateManualTransferJob(string SourceName, string DestName, string TRCarrierID, int priority, bool MoveOnly, int OptSelectCrane)
        {
            ICarrierStoreAble Source = GlobalData.Current.GetGlobalCarrierStoreAbleObject(SourceName);
            ICarrierStoreAble Dest = GlobalData.Current.GetGlobalCarrierStoreAbleObject(DestName);
            return CreateManualTransferJob(Source, Dest, TRCarrierID, priority, MoveOnly, OptSelectCrane);
        }
        /// <summary>
        /// 메뉴얼 동작 작업를 만든다.
        /// </summary>
        /// <param name="Source"></param>
        /// <param name="Dest"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public bool CreateManualJob(eCraneCommand command,ICarrierStoreAble Source, ICarrierStoreAble Dest, int priority)
        {
            Random r = new Random();
            if (Source == null || Dest == null)
            {
                return false; //인자 이상
            }
            if (priority > 99)
            {
                priority = 99;
            }
            if (priority < 1)
            {
                priority = 1;
            }

            var j = new McsJob();
            j.CommandID = MakeCommandID(Source.GetCarrierID());
            j.CreateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            j.CarrierID = Source.GetCarrierID();
            j.Source = Source.GetTagName();
            j.Destination = Dest.GetTagName();
            j.Priority = priority.ToString();
            j.TransferState = 1;

            j.JobType = (command == eCraneCommand.MOVE) ? "MOVE" : "TRANSFER";
            j.CarrierType = "Tray";
            j.TCStatus = eTCState.QUEUED;
            j.JobFrom = eScheduleJobFrom.Operator;
            j.SourceGroup = Source.iGroup;
            j.DestGroup = Dest.iGroup;
            j.LotIDList = string.Empty;
            j.MnlCmdJobCreate = true;

            if (IsJobListCheck(j.Source, j.Destination, j.CarrierID))       //이미 job list에 있는지 확인
                return false;

            if(command != eCraneCommand.MOVE) //이동 동작에서는 사이즈 체크 필요없음.
            {
                bool SizeMatch = Dest.CheckCarrierSizeAcceptable(j.CarrierSize);
                if (!SizeMatch)
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "CreateManualJob Failed. Reason : Size Mismatch  CarrierSize : {0}", j.CarrierSize);
                    return false;
                }
            }

            AddMcsJob(j);
            if (command != eCraneCommand.MOVE) //이동 동작에서는 쉘프 리저브 필요없음
            {
                Source.NotifyScheduled(true);
                Dest.NotifyScheduled(true);
            }

            //GlobalData.Current.DBManager.DbSetJobInfo(j, false);        //220322 조숭진 job관련 db 저장
            GlobalData.Current.DBManager.DbSetProcedureJobInfo(j, false);
            if (j.JobType == "TRANSFER")
            {
                //GlobalData.Current.HSMS.SendS6F11(208, "JOBDATA", j);
            }
            return true;

        }

        //SuHwan_20221108 : 그냥 DB에서 받아온 TargetValue를 넣어준다
        public void CreateManualJob_formDB(string rcvTargetValue)
        {
            string[] buffeSplitr = null;
            List<ICarrierStoreAble> position = new List<ICarrierStoreAble>();

            try
            {
                buffeSplitr = rcvTargetValue.Split('/');

                if (buffeSplitr.Length != 6)
                    return;
                string sourceName = buffeSplitr[0];
                string DestItemName = buffeSplitr[1];
                string CarrierID = buffeSplitr[2];
                int Priority = 0;
                int.TryParse(buffeSplitr[3], out Priority);
                bool MoveOnly = buffeSplitr[4].ToUpper() == "TRUE";
                int OptCrane = 0;
                int.TryParse(buffeSplitr[5], out OptCrane);

                CreateManualTransferJob(sourceName, DestItemName, CarrierID, Priority, MoveOnly, OptCrane);
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} - Exception:{1}", System.Reflection.MethodBase.GetCurrentMethod().Name, ex);
            }
        }

        /// <summary>
        /// S2F49 Host Command 받은후에 반송 작업 생성
        /// </summary>
        /// <param name="CommandID"></param>
        /// <param name="CarrierID"></param>
        /// <param name="Source"></param>
        /// <param name="Dest"></param>
        /// <param name="FianlLoc"></param>
        /// <param name="priority"></param>
        /// <returns></returns>
        public bool CreateMCSHostJob(string CommandID, string CarrierID, string DestZoneName ,ICarrierStoreAble Source, ICarrierStoreAble Dest, ICarrierStoreAble FianlLoc, int priority, string lotidlist = "")
        {
            if (string.IsNullOrEmpty(CommandID) || string.IsNullOrEmpty(CarrierID) || Source == null || Dest == null)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "CreateMCSHostJob Parameter Invalid! " +
                    "CommandID :{0} CarrierID :{1} Source :{2} Dest :{3} FianlLoc :{4} priority :{5}", CommandID, CarrierID, Source, Dest, FianlLoc, priority);
                return false; //인자 이상
            }

            if (IsCommandIDContain(CommandID))//커맨드 아이디 다시 중복체크
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "CreateMCSHostJob CommandID : {0} is already existed.", CommandID);
                return false;
            }
            if (priority > 99)
            {
                priority = 99;
            }
            if (priority < 1)
            {
                priority = 1;
            }

            var j = new McsJob();
            j.CreateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            j.CarrierID = CarrierID;
            j.Source = Source.iLocName;
            j.Destination = Dest.iLocName;
            j.DestZoneName = DestZoneName;
            j.Priority = priority.ToString();
            j.TransferState = 1;
            j.JobType = "TRANSFER";
            j.CarrierType = "Tray";
            j.CommandID = CommandID;
            j.TCStatus = eTCState.QUEUED;
            j.JobFrom = eScheduleJobFrom.HostMCS;
            j.SourceGroup = Source.iGroup;
            j.DestGroup = Dest.iGroup;
            j.LotIDList = lotidlist;
            j.MnlCmdJobCreate = false;

            bool SizeMatch = Dest.CheckCarrierSizeAcceptable(j.CarrierSize);
            if (!SizeMatch)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "CreateMCSHostJob Failed. Reason : Size Mismatch  CarrierSize : {0}", j.CarrierSize);
                return false;
            }


            AddMcsJob(j);
            Source.NotifyScheduled(true);
            Dest.NotifyScheduled(true);
            GlobalData.Current.DBManager.DbSetProcedureJobInfo(j, false);
            return true;

        }

        public void DeleteALLMcsJob()
        {
            if (this.Count() != 0)
            {
                int delnum = this.Count();
                for (int i = delnum; 0 < i; i--)
                {
                    try
                    {
                        var deleteJob = this.ElementAt(i - 1); //스케쥴러와 동시와 삭제 되면 컬렉션 예외 나므로 수정.
                        if (deleteJob.Step == enumScheduleStep.RMPutCompleteWait)
                            continue;

                        DeleteMcsJob(deleteJob);
                    }
                    catch (Exception ex)
                    {
                        _ = ex;
                    }
                }
            }
        }
        public bool AddMcsJob(McsJob TargetJob)
        {
            TargetJob.SetPlayBackTrace();
            Add(TargetJob);

            return true;

        }

        /// <summary>
        /// UI 에서 삭제 요청 들어온건 여기서 처리함.
        /// Queue 상태작업은 Cancel 다른 상태 작업은 Abort 시켜야한다.
        /// </summary>
        /// <param name="TargetJob"></param>
        public eUIJobRemoveResult ProcessUIJobRemoveRequest(McsJob TargetJob)
        {
            if(TargetJob == null)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "ProcessUIJobRemoveRequest Argument Null Check!");
                return eUIJobRemoveResult.NoResult;
            }

            if (TargetJob.TCStatus == eTCState.QUEUED) //바로 삭제 가능 Cancel 보고 필요
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "ProcessUIJobRemoveRequest Cancel(QUEUED)! Job:{0} CarrierID:{1} CarrierLocation:{2}", TargetJob.CommandID, TargetJob.CarrierID, TargetJob.CarrierLoc);
                //240508 RGJ CANCEL OP ACTION 추가.
                GlobalData.Current.HSMS.SendS6F11(605, "JobData", TargetJob, "CommandType", "CANCEL"); //OperatorInitiatedAction CEID = 605 CommandType = "CANCEL"
                Thread.Sleep(200); //231103 RGJ 조범석 매니저 요청으로 605 보고후 200ms 딜레이 요청 추가.
                GlobalData.Current.HSMS.SendS6F11(206, "JOBDATA", TargetJob, "COMMANDID", TargetJob.CommandID); //TransferCancelInitiated 추가사양 보고
                GlobalData.Current.McdList.DeleteMcsJob(TargetJob, false);//삭제 지시
                return eUIJobRemoveResult.Cancel;

            }
            else if (TargetJob.TCStatus == eTCState.PAUSED) //바로 삭제 가능 Abort  보고 필요
            {
                //alt 반송으로 쉘프에 적재되어있던것을 작업자가 지우면 carrier stat이 ALTERNATE 로 남아있어 comp로 변경시킨다.
                if (TargetJob.TCStatus == eTCState.PAUSED && TargetJob.SubJob == eSubJobType.AlterStore && TargetJob.CarrierLocationItem is ShelfItem)
                {
                    TargetJob.JobCarrierItem.CarrierState = eCarrierState.COMPLETED;
                }

                //2024.05.17 lim, Paused 도 강제 완료 조건 추가
                //CV_BaseModule cv = TargetJob.CarrierLocationItem as CV_BaseModule;
                //if (cv != null && cv.PortInOutType == ePortInOutType.OUTPUT) //240508 RGJ 현재 위치가 배출포트인 작업은 Abort 불가
                if (TargetJob.CarrierLocationItem is CV_BaseModule cLoc && cLoc.PortInOutType == ePortInOutType.OUTPUT
                        && cLoc.CarrierExist)  //현재 위치가 아웃포트이고 케리어 있다면 Abort 불가
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "ProcessUIJobRemoveRequest Abort(PAUSED) Failed. Carrier is in port! Job:{0} CarrierID:{1} CarrierLocation:{2}", TargetJob.CommandID, TargetJob.CarrierID, TargetJob.CarrierLoc);
                    return eUIJobRemoveResult.AbortFail;
                }
                else if (TargetJob.DestItem is CV_BaseModule cv2 && // 데스트가 Port 이고 현재 위치가 Port인데 케리어가 없거나 현재 위치가 없다면..
                        (string.IsNullOrEmpty(TargetJob.CarrierLoc) || (TargetJob.CarrierLocationItem is CV_BaseModule cv1 && !cv1.CarrierExist)))
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "ProcessUIJobRemoveRequest Abort(PAUSED) AbortJobForceComplete Action! Job:{0} CarrierID:{1} CarrierLocation:{2}", TargetJob.CommandID, TargetJob.CarrierID, TargetJob.CarrierLoc);
                    GlobalData.Current.McdList.DeleteMcsJob(TargetJob, false);//진행중 강제 완료
                    return eUIJobRemoveResult.AbortJobForceComplete;
                }
                else
                {
                    LogManager.WriteConsoleLog(eLogLevel.Info, "ProcessUIJobRemoveRequest Abort(PAUSED)! Job:{0} CarrierID:{1} CarrierLocation:{2}", TargetJob.CommandID, TargetJob.CarrierID, TargetJob.CarrierLoc);

                    //240508 RGJ ABORT OP ACTION 추가.
                    GlobalData.Current.HSMS.SendS6F11(605, "JobData", TargetJob, "CommandType", "ABORT"); //OperatorInitiatedAction CEID = 605 CommandType = "ABORT"
                    Thread.Sleep(200); //231103 RGJ 조범석 매니저 요청으로 605 보고후 200ms 딜레이 요청 추가.
                    GlobalData.Current.HSMS.SendS6F11(203, "JOBDATA", TargetJob, "COMMANDID", TargetJob.CommandID); //TransferAbortInitiated 추가사양 보고
                    GlobalData.Current.McdList.DeleteMcsJob(TargetJob, false);//UI에서 명령 내린건 강제 삭제
                    return eUIJobRemoveResult.Abort;
                }
            }
            else if (TargetJob.TCStatus == eTCState.TRANSFERRING) //바로 삭제 불가 반송중인 잡
            {
                //dest get하다가 get하지 못하고 알람발생, crane에 자재없는 상태에서 job이 지워지면 carrier state를 completed로 변경.
                if (TargetJob.AssignedRM != null
                    && TargetJob.AssignedRM.PC_CraneCommand == eCraneCommand.PICKUP
                    && !TargetJob.AssignedRM.CheckCarrierExist()
                    && TargetJob.JobCarrierItem.CarrierState == eCarrierState.TRANSFERRING)
                {
                    TargetJob.JobCarrierItem.CarrierState = eCarrierState.COMPLETED;
                }

                if (GlobalData.Current.Scheduler.CheckJobOnProcess(TargetJob.CommandID)) //할당된 작업은 Abort 요청하고 스케쥴러가 처리하게 함.
                {
                    if (TargetJob.MCSJobAbortReq) //이미 Job Abort 요청상태면 또보고안함.
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "ProcessUIJobRemoveRequest Already SetAbort(TRANSFERRING)... Job:{0} CarrierID:{1} CarrierLocation:{2}", TargetJob.CommandID, TargetJob.CarrierID, TargetJob.CarrierLoc);
                        return eUIJobRemoveResult.AbortAlreadyRequest;
                    }
                    else
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "ProcessUIJobRemoveRequest SetAbort(TRANSFERRING)! Job:{0} CarrierID:{1} CarrierLocation:{2}", TargetJob.CommandID, TargetJob.CarrierID, TargetJob.CarrierLoc);
                        //240508 RGJ ABORT OP ACTION 추가.
                        GlobalData.Current.HSMS.SendS6F11(605, "JobData", TargetJob, "CommandType", "ABORT"); //OperatorInitiatedAction CEID = 605 CommandType = "ABORT"
                        Thread.Sleep(200); //231103 RGJ 조범석 매니저 요청으로 605 보고후 200ms 딜레이 요청 추가.
                        GlobalData.Current.HSMS.SendS6F11(203, "JOBDATA", TargetJob, "COMMANDID", TargetJob.CommandID); //TransferAbortInitiated 추가사양 보고
                        TargetJob.SetJobAbort(true);
                        return eUIJobRemoveResult.AbortRequest;
                    }
                }
                else //할당안된 작업은 삭제 허용
                {
                    //2024.05.15 lim, Location에 바로 삭제 안함. 
                    // PortChage 시 삭제됨... 다음 다른 자제 도착시?? 삭제되는듯... 
                    // Location에 케리어 있는지 확인, 있다면 ID가 같은지도 확인 필요

                    // 케리어가 포트에 있다면 삭제 불가 // 아웃포트 인지 확인 필요 ( 정상적인 경우 인포트에서 생성된 잡일 수 있음)
                    // 포트에 삭제하려는 Job 케리어가 없다면 완료 처리
                    // 포트가 아니면 정상 abort 처리

                    // 데이터를 삭제해도 바로 LOC이 없어지지 않는다.. 그래서 혹시 불일치하면 삭제 한다.
                    //if (TargetJob.CarrierLocationItem is CV_BaseModule cv && !cv.CarrierExist)
                    //{
                    //    // 데이터 불일치로 LOC 클리어
                    //}
                    
                    //if (TargetJob.CarrierLocationItem is CV_BaseModule cv && cv.PortInOutType == ePortInOutType.OUTPUT)  //240508 RGJ 현재 위치가 배출포트인 작업은 Abort 불가
                    if (TargetJob.CarrierLocationItem is CV_BaseModule cLoc && cLoc.PortInOutType == ePortInOutType.OUTPUT
                        && cLoc.CarrierExist )  //현재 위치가 아웃포트이고 케리어 있다면 Abort 불가
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "ProcessUIJobRemoveRequest Abort(TRANSFERRING) Failed. Carrier is in outport! Job:{0} CarrierID:{1} CarrierLocation:{2}", TargetJob.CommandID, TargetJob.CarrierID, TargetJob.CarrierLoc);
                        return eUIJobRemoveResult.AbortFail;
                    }
                    //else if(TargetJob.DestItem is CV_BaseModule cv2 && cv2.PortInOutType == ePortInOutType.OUTPUT && 
                    //    string.IsNullOrEmpty(TargetJob.CarrierLoc)) //작업 강제 완료 케이스
                    else if (TargetJob.DestItem is CV_BaseModule cv2 && // 데스트가 Port 이고 현재 위치가 Port인데 케리어가 없거나 현재 위치가 없다면..
                        (string.IsNullOrEmpty(TargetJob.CarrierLoc) || (TargetJob.CarrierLocationItem is CV_BaseModule cv1 && !cv1.CarrierExist)))    
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "ProcessUIJobRemoveRequest AbortJobForceComplete Action! Job:{0} CarrierID:{1} CarrierLocation:{2}", TargetJob.CommandID, TargetJob.CarrierID, TargetJob.CarrierLoc);
                        GlobalData.Current.McdList.DeleteMcsJob(TargetJob, false);//진행중 강제 완료
                        return eUIJobRemoveResult.AbortJobForceComplete;
                    }
                    else
                    {
                        LogManager.WriteConsoleLog(eLogLevel.Info, "ProcessUIJobRemoveRequest Abort(TRANSFERRING) Action! Job:{0} CarrierID:{1} CarrierLocation:{2}", TargetJob.CommandID, TargetJob.CarrierID, TargetJob.CarrierLoc);
                        //240508 RGJ ABORT OP ACTION 추가.
                        GlobalData.Current.HSMS.SendS6F11(605, "JobData", TargetJob, "CommandType", "ABORT"); //OperatorInitiatedAction CEID = 605 CommandType = "ABORT"
                        Thread.Sleep(200); //231103 RGJ 조범석 매니저 요청으로 605 보고후 200ms 딜레이 요청 추가.
                        GlobalData.Current.HSMS.SendS6F11(203, "JOBDATA", TargetJob, "COMMANDID", TargetJob.CommandID); //TransferAbortInitiated 추가사양 보고
                        GlobalData.Current.McdList.DeleteMcsJob(TargetJob, false);//진행중 강제 삭제
                        return eUIJobRemoveResult.Abort;
                    }
                }
            }
            else if (TargetJob.TCStatus == eTCState.NONE || TargetJob.TCStatus == eTCState.ABORTING) //NONE,ABORTING 인경우도 삭제되도록 함.
            {

                LogManager.WriteConsoleLog(eLogLevel.Info, "ProcessUIJobRemoveRequest Abort(NONE)! Job:{0} CarrierID:{1} CarrierLocation:{2}", TargetJob.CommandID, TargetJob.CarrierID, TargetJob.CarrierLoc);
                GlobalData.Current.McdList.DeleteMcsJob(TargetJob, false);//UI에서 명령 내린건 강제 삭제
                return eUIJobRemoveResult.Abort;
            }
            return eUIJobRemoveResult.NoResult;
        }

        /// <summary>
        /// 삭제함수이므로 삭제에만 집중한다.삭제하면 안되는 작업은 콜하기전에 걸러야 한다.
        /// </summary>
        /// <param name="TargetJob"></param>
        /// <param name="bPushJob"></param>
        /// <returns></returns>
        public bool DeleteMcsJob(McsJob TargetJob, bool bPushJob = false)
        {
            if (TargetJob == null) //null 체크
            {
                return false;
            }
            TargetJob.JobNowDeleting = true; //240617 RGJ 더이상 스케쥴링 하지 않도록 설정
            if (TargetJob.JobType == "TRANSFER") //240209 RGJ TRANSFER Job 만 쉘프 예약을 변경한다.
            {
                //2024.07.05 lim, 소스를 비우고 다른 Job이 생성 될 수 있다. 확인 후 삭제 필요
                string source = TargetJob.SourceItem?.GetTagName();
                if (!string.IsNullOrEmpty(source))
                {
                    var Destjob = this.Where(j => j.Destination == source).FirstOrDefault();
                    if (Destjob == null)
                        TargetJob.SourceItem?.NotifyScheduled(false); //잡 삭제할때 잡아 놓았던 예약을 해제한다.
                }
                TargetJob.DestItem?.NotifyScheduled(false);
                //2024.09.26 lim, 핸드오버 Job 예약 잡은것도 해제하면 안된다.
                string HandOver = TargetJob.HandOverBufferItem?.GetTagName();
                if (!string.IsNullOrEmpty(HandOver))
                {
                    var Handoverjob = this.Where(j => j.Destination == HandOver || j.HandOverStoredDest == HandOver).FirstOrDefault();
                    if (Handoverjob == null)
                        TargetJob.HandOverBufferItem?.NotifyScheduled(false); //231025 RGJ 작업이 삭제되면 핸드오버 버퍼예약이 있다면 해제.
                }
                TargetJob.AlterShelfItem?.NotifyScheduled(false); //231025 RGJ 작업이 삭제되면 대체반송 쉘프 예약이 있다면 해제.
                TargetJob.CarrierLocationItem?.NotifyScheduled(false); //240617 RGJ 작업 삭제 되면 현재 위치예약도 해제함.
            }


            //2024.05.15 lim, MOVE JOB DB 안지워짐 위치 변경
            bool isJobDelete = GlobalData.Current.DBManager.DbSetProcedureJobInfo(TargetJob, true); //DB에서 삭제

            if (bPushJob || TargetJob.JobType == "MOVE") //푸시작업 삭제는 보고 필요 없음.
            {
                TargetJob.UnsubscribeCarrierPropertyEvent(); //240826 메모리 누수 방지이벤트 구독 해제
                Remove(TargetJob);
                return true;
            }

            if (isJobDelete)
            {
                if (TargetJob.JobResult != eJobResultCode.DEST_INTERLOCK_NG && 
                    TargetJob.TCStatus == eTCState.QUEUED) //대기 상태일때만 캔슬보고
                {
                    GlobalData.Current.HSMS.SendS6F11(204, "JOBDATA", TargetJob); //TransferCancelCompleted
                }
                else if (TargetJob.TCStatus == eTCState.TRANSFERRING || TargetJob.TCStatus == eTCState.PAUSED) //포즈상태일때 Abort 보고 해야함.
                {
                    //2024.05.07 lim, 조범석 매니저 적용전 추가 확인 예정
                    //2024.05.15 lim, Dest 가 Port 면 out 케리어 아웃이라고 봐야함... PortChange 할 수 있음.
                    //if (string.IsNullOrEmpty(TargetJob.CarrierLoc) && TargetJob.DestItem is CV_BaseModule cv && cv.PortInOutType == ePortInOutType.OUTPUT) //240507 화물위치가 없는 가비지면 삭제보고 (조범석 매니저 협의)
                    if (TargetJob.DestItem is CV_BaseModule cv &&
                        (string.IsNullOrEmpty(TargetJob.CarrierLoc) || (TargetJob.CarrierLocationItem is CV_BaseModule cv1 && !cv1.CarrierExist)))
                    {
                        CV_BaseModule LPcv = GlobalData.Current.GetGlobalCarrierStoreAbleObject(cv.iZoneName) as CV_BaseModule; //끝단 포트를 가져온다.

                        //240514 RGJ 출고포트 OP, BP,에 해당 화물의 CARRIER ID가 존재 하지 않으면 WaitOut(Dest), TransferComplete(Dest), CarrierRemove(Dest)로 보고 (조범석 매니저 요청)
                        GlobalData.Current.HSMS.SendS6F11(309, "CarrierItem", TargetJob.JobCarrierItem, "JobData", TargetJob, "PORT", LPcv);  //CarrierWaitOut 309
                        Thread.Sleep(100);
                        GlobalData.Current.HSMS.SendS6F11(207, "JobData", TargetJob, "VOIDJOB", true); //TransferCompleted 207
                        CarrierStorage.Instance.RemoveStorageCarrier(TargetJob.CarrierID); //STK Domain 에서 캐리어 제거.
                        Thread.Sleep(100);
                        GlobalData.Current.HSMS.SendS6F11(303, "PORT", LPcv, "CARRIERID", TargetJob.CarrierID); //S6F11 CarrierRemoved 303 Port
                    }
                    else
                    {
                        TargetJob.JobResult = eJobResultCode.SUCCESS;   //2024.07.31 lim, Abort 정상 완료 처리
                        GlobalData.Current.HSMS.SendS6F11(201, "JOBDATA", TargetJob); //TransferAbortCompleted
                    }
                }
                TargetJob.UnsubscribeCarrierPropertyEvent(); //240826 메모리 누수 방지이벤트 구독 해제
                Remove(TargetJob);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 해당 CarrierID 와 연결된 작업을 삭제한다.
        /// </summary>
        /// <param name="CarrierID"></param>
        /// <returns></returns>
        public bool DeleteMcsJob(string CarrierID)
        {
            McsJob TargetJob = GetCarrierJob(CarrierID);
            return DeleteMcsJob(TargetJob);
        }

        //220322 HHJ SCS 개발     //- Shelf Control 기능 추가
        public bool ChangePriority(McsJob item, int priority)
        {
            try
            {
                bool bvalue = false;
                int iindex = IndexOf(item);

                if (iindex < 0)
                    return false;

                if (iindex >= base.Count)
                    return false;

                item.Priority = priority.ToString();

                //220603 조숭진 FastObservableCollection -> ObservableList 변경함에 따라 add로 변경
                //SetItem(iindex, item);
                //220621 HHJ SCS 개선     //- MCS 우선순위 변경 관련 추가       //주석
                //Add(item);

                Replace(iindex, item);
                bvalue = GlobalData.Current.DBManager.DbSetProcedureJobInfo(item, false);

                return bvalue;
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} - Exception:{1}", System.Reflection.MethodBase.GetCurrentMethod().Name, ex);
            }
            return false;
        }

        //220329 HHJ SCS 개발     //- McsJobManager 작업중 항목 취득 
        public bool ChangeJobItem(McsJob item)
        {
            try
            {
                bool bvalue = false;
                int iindex = IndexOf(item);

                //220329 HHJ SCS 개발     // -McsJobUpdate Exception 수정
                if (iindex < 0)
                    return false;

                if (iindex >= base.Count)
                    return false;

                //220603 조숭진 FastObservableCollection -> ObservableList 변경함에 따라 add로 변경
                //SetItem(iindex, item);
                //Add(item);

                Replace(iindex, item);
                bvalue = GlobalData.Current.DBManager.DbSetProcedureJobInfo(item, false);

                return bvalue;
            }
            catch (Exception ex)
            {
                Console.WriteLine("{0} - Exception:{1}", System.Reflection.MethodBase.GetCurrentMethod().Name, ex);
            }
            return false;
        }

        #region ICollection<TeachingList> Members

        //public void Add(McsJob item)
        //{
        //    _items.Add(item);
        //}

        //20210203 조숭진 carriageid, bottleid, shelfuse, porttype, weight 추가

        //public void Add(int number, string CreateTime, string CommandID, string CarriageType, string CarrierID,
        //    string BottleID, string TRStatus, string Priority, string JobType, string Source, string Destination,
        //    string DestGroup, string AssignRM, string Loaded, enumScheduleStep Step, enumScheduleStep PreStep,
        //    string Pause, string delete, string CancelReq, int TransferState)
        //{
        //    McsJob item = new McsJob();

        //    item.Number = number;
        //    item.CreateTime = CreateTime;
        //    item.CommandID = CommandID;
        //    item.CarriageType = CarriageType;
        //    item.CarrierID = CarrierID;
        //    item.BottleID = BottleID;
        //    item.TRStatus = TRStatus;
        //    item.Priority = Priority;
        //    item.JobType = JobType;
        //    item.Source = Source;
        //    item.Destination = Destination;
        //    item.DestGroup = DestGroup;
        //    item.AssignRM = AssignRM;
        //    item.Loaded = Loaded;
        //    item.PreviousStep = PreStep;
        //    item.Step = Step;
        //    item.Pause = Pause;
        //    item.Delete = delete;
        //    item.CancelReq = CancelReq;
        //    item.TransferState = TransferState;

        //    _items.Add(item);
        //}

        //public void Clear()
        //{
        //    _items.Clear();
        //}

        //public bool Contains(McsJob item)
        //{
        //    return _items.Contains(item);
        //}

        //public bool ContainKey(string CarrierID)
        //{
        //    bool result = false;
        //    if (_items != null && _items.Where(r => r.CarrierID == CarrierID).Count() > 0)
        //    {
        //        result = true;
        //    }
        //    return result;
        //}

        //public void CopyTo(McsJob[] array, int arrayIndex)
        //{
        //    _items.CopyTo(array, arrayIndex);
        //}

        //public int Count
        //{
        //    get { return _items.Count; }
        //}

        //public bool IsReadOnly
        //{
        //    get { return false; }
        //}

        //public bool Remove(McsJob item)
        //{
        //    return _items.Remove(item);
        //}

        //public bool Remove(int number)
        //{
        //    for (int i = _items.Count - 1; i >= 0; i--)
        //    {
        //        if (Items[i].Number == number)
        //        {
        //            _items.RemoveAt(i);
        //            return true;
        //        }
        //    }
        //    return false;
        //}

        #endregion

        #region IEnumerable<TeachingList> Members

        //public IEnumerator<McsJob> GetEnumerator()
        //{
        //    return _items.GetEnumerator();
        //}

        #endregion

        #region IEnumerable Members

        //System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        //{
        //    return _items.GetEnumerator();
        //}

        #endregion

        public int CalcDistance(int a, int b)
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

        public int CalcShelfDistance(ShelfItem a, ShelfItem b)
        {
            int bankDistance = a.ShelfBank == b.ShelfBank ? 0 : 1;
            int bayDistance = CalcDistance(a.ShelfBay, b.ShelfBay);
            int LevelDistance = CalcDistance(a.ShelfLevel, b.ShelfLevel);
            return bankDistance + bayDistance + LevelDistance;
        }
        public int CalcShelfDistance(ShelfItem a, Modules.Conveyor.CV_BaseModule b)
        {
            int bankDistance = a.ShelfBank == b.Position_Bank ? 0 : 1;
            int bayDistance = CalcDistance(a.ShelfBay, b.Position_Bay);
            int LevelDistance = CalcDistance(a.ShelfLevel, b.Position_Level);
            return bankDistance + bayDistance + LevelDistance;
        }


    }
}
