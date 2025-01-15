using BoxPrint.Database;
using BoxPrint.Log;
using BoxPrint.Modules;      //220420 HHJ SCS 개선     //- ShelfTagHelper 추가
using BoxPrint.Modules.Conveyor;
using BoxPrint.Modules.RM;
using BoxPrint.Modules.Shelf;
using System;
using System.ComponentModel;
using System.Linq;

namespace BoxPrint.DataList.MCS
{
    public class McsJob : INotifyPropertyChanged,IComparable<McsJob>
    {
        #region Job Variable
        public bool JobForceAbortRequest
        {
            get;
            private set;
        }
        public int JobCurrentSecureTryCount = 0;

        public bool VoidJob = false; //스케쥴러에의해 작업 시작전 리스트에 없을경우 Flag On
        #endregion

        #region Job Property
        public int JobNumber { get; set; }

        public string CreateTime { get; set; }

        public string CommandID { get; set; }

        public string LotIDList { get; set; }

        public string CommandType
        {
            get
            {
                return JobType;
            }
            set
            {
                ;
            }
        }

        public string CarrierType { get; set; }

        private string _CarrierID = string.Empty;
        public string CarrierID
        {
            get
            {
                return _CarrierID;
            }
            set
            {
                if (string.IsNullOrEmpty(_CarrierID) && !IsPlayBackInstance) //240826 RGJ 플레이백 메모리 누수방지
                {
                    //처음 CarrierID Set 할때 이벤트 등록
                    CarrierItem cItem = CarrierStorage.Instance.GetCarrierItem(value);
                    if (cItem != null)
                    {
                        cItem.PropertyChanged += CItem_PropertyChanged;
                    }
                }
                _CarrierID = value;
            }
        }
        public eCarrierSize CarrierSize
        {
            get
            {
                if (!string.IsNullOrEmpty(CarrierID))
                {
                    CarrierItem cItem = CarrierStorage.Instance.GetCarrierItem(CarrierID);
                    if (cItem != null)
                    {
                        return cItem.CarrierSize;
                    }
                }
                return eCarrierSize.Unknown;
            }
        }

        /// <summary>
        /// 240826 RGJ
        /// PropertyChanged 구독 해제가 안되서 McsJob 누수 되므로 구독해체 기능 추가. 
        /// </summary>
        public void UnsubscribeCarrierPropertyEvent()
        {
            CarrierItem cItem = CarrierStorage.Instance.GetCarrierItem(_CarrierID);
            if (cItem != null)
            {
                cItem.PropertyChanged -= CItem_PropertyChanged;
            }
        }

        private void CItem_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CarrierLocation")
            {
                OnPropertyChanged(new PropertyChangedEventArgs("CarrierLoc"));
            }
        }

        public void RaisePropertyChangeEvent(string PropertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(PropertyName));
        }

        public string CarrierLotID { get; set; }

        private string _CarrierLoc; //캐리어 현재 위치 저장
        public string CarrierLoc
        {
            get
            {
                if (!string.IsNullOrEmpty(_CarrierLoc))
                {
                    return _CarrierLoc;
                }
                CarrierItem cItem = CarrierStorage.Instance.GetCarrierItem(CarrierID);
                if (cItem != null)
                {
                    return cItem.CarrierLocation;
                }
                else
                {
                    return string.Empty;
                }
            }
            set
            {
                _CarrierLoc = value;
            }
        }

        public ICarrierStoreAble CarrierLocationItem
        {
            get
            {
                return GlobalData.Current.GetGlobalCarrierStoreAbleObject(CarrierLoc);
            }
        }
        public string CarrierZoneName
        {
            get
            {
                if (JobCarrierItem != null)
                {
                    //현재 캐리어 위치 찾기.
                    return JobCarrierItem.CarrierZoneName;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public bool JobSourcePortError = false; //[230503 CIM 검수] Souce Port 에러 체크 결과 저장
        public bool MCSJobAbortReq { get; private set; }

        public string BottleID { get; set; }


        public bool IsJobActiveState
        {
            get
            {
                return _TCStatus == eTCState.TRANSFERRING || _TCStatus == eTCState.PAUSED;
            }
        }

        public eTCState _TCStatus;
        public eTCState TCStatus
        {
            get
            {
                return _TCStatus;
            }
            set
            {
                if (_TCStatus != value)
                {
                    _TCStatus = value;
                    //230814 조숭진 jobtype이 move일때는 왜 update못하게했는지 기억이 안난다.... move도 update할수 있게 변경한다.
                    //221109 조숭진 job list에 있는 것만 db에 update할 수 있게 변경.
                    //GlobalData.Current.DBManager.DbSetJobInfo(this, false); //220921 Job 상태 업데이트 추가
                    //if (!IsPlayBackInstance && this.JobType != "MOVE" && GlobalData.Current.McdList.Where(j => j.CommandID == this.CommandID).Count() > 0)
                    if (!IsPlayBackInstance && GlobalData.Current.McdList.Where(j => j.CommandID == this.CommandID).Count() > 0 && this.SubJob != eSubJobType.Push)
                    {
                        GlobalData.Current.DBManager.DbSetProcedureJobInfo(this, false); //220921 Job 상태 업데이트 추가
                    }
                    OnPropertyChanged(new PropertyChangedEventArgs("TCStatus"));
                }

            }
        }

        //220621 HHJ SCS 개선     //- MCS 우선순위 변경 관련 추가
        private string _Priority;
        public string Priority
        {
            get => _Priority;
            set
            {
                if (_Priority != value)
                {
                    _Priority = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("Priority"));
                }
            }
        }
        public int ScheduledPriority
        {
            get
            {
                int temp;
                //220621 HHJ SCS 개선     //- MCS 우선순위 변경 관련 추가
                //bool ParseOK = int.TryParse(Priority, out temp);
                bool ParseOK = int.TryParse(_Priority, out temp);
                if (ParseOK)
                {
                    return temp;
                }
                else
                {
                    return int.MaxValue;
                }
            }
        }

        private string _JobType;
        public string JobType
        {
            get => _JobType;
            set
            {
                if (_JobType != value)
                {
                    _JobType = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("JobType"));
                }
            }
        }
        private string _Source;
        public string Source        
        {
            get => _Source;
            set
            {
                if (_Source != value)
                {
                    _Source = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("Source"));
                }
            }
        }


        public string SourceGroup { get; set; }

        private string _SourceZoneName;
        public string SourceZoneName //20230302 RGJ 검수 보고용 추가
        {
            get
            {
                if (string.IsNullOrEmpty(_SourceZoneName)) //혹시 _SourceZoneName 값이 없으면 Source 으로 보고함
                {
                    {
                        return Source;
                    }
                }
                else
                {
                    return _SourceZoneName;
                }
            }
            set
            {
                _SourceZoneName = value;
            }
        }
        private string _Destination;
        public string Destination
        {
            get => _Destination;
            set
            {
                if (_Destination != value)
                {
                    _Destination = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("Destination"));
                }
            }
        }

        private string _prevDestination;
        public string prevDestination
        {
            get => _prevDestination;
            set
            {
                if(_prevDestination != value)
                {
                    _prevDestination = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("prevDestination"));
                }
            }
        }

        public string DestGroup { get; set; }

        private string _DestZoneName;
        public string DestZoneName
        {
            get
            {
                if(string.IsNullOrEmpty(_DestZoneName)) //혹시 _DestZoneName 값이 없으면 Destination 으로 보고함
                {
                    {
                        return Destination;
                    }
                }
                else
                {
                    return _DestZoneName;
                }
            }
            set
            {
                _DestZoneName = value;
            }
        }

        public string AltShelfDestination { get; set; } //Dest 상태 이상시 임시 수용쉘프


        /// <summary>
        /// Hand Over 를 위한 중간  목적지를 저장
        /// </summary>
        public string HandOverStoredDest { get; set; }

        ///// <summary>
        ///// WithDraw 를 위한 목적지를 저장.
        ///// </summary>
        //public string WithDrawStoredDest { get; set; }

        ///// <summary>
        ///// WithDraw 를 위한 작업 형태 저장.
        ///// </summary>
        //public string WithDrawStoredJobType { get; set; }


        public string AssignRMName
        {
            get
            {
                RMModuleBase RM = AssignedRM;
                return RM != null ? RM.ModuleName : "";
            }
        }

        /// <summary>
        /// Push 잡에 목표 RM 번호
        /// </summary>
        public int TargetRMNumber
        {
            get;
            set;
        }


        public int AssignRMNumber
        {
            get
            {
                RMModuleBase RM = AssignedRM;
                return RM != null ? RM.RMNumber : 0;
            }
        }

        public string Loaded { get; set; }


        public enumScheduleStep PreviousStep { get; set; }

        private enumScheduleStep _Step;
        public enumScheduleStep Step
        {
            get
            {
                return _Step;
            }
            set
            {
                if (_Step != value)
                {
                    PreviousStep = _Step;
                    _Step = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("Step"));
                }
            }
        }

        public string Pause { get; set; }

        public string Delete { get; set; }

        //public string CancelReq { get; set; }

        private int _TransferState;
        public int TransferState
        {
            get
            {
                return _TransferState;
            }
            set
            {
                if (_TransferState != value)
                {
                    _TransferState = value;
                    OnPropertyChanged(new PropertyChangedEventArgs("TransferState"));
                }

            }
        }

        private eHandoffType _HandoffType = eHandoffType.AUTO;
        public eHandoffType HandoffType 
        { 
            get
            {
                return _HandoffType;
            }
            set
            {
                _HandoffType = value;
            }
        }

        //20210315 시간이 지나면 스케줄 취소하기 위한 스케줄용 딜레이타임. 초단위
        public string DelayTime { get; set; }

        public int DestBank
        {
            get
            {
                if (DestItem != null)
                {
                    return DestItem.iBank;
                }
                else
                {
                    return -999;
                }
            }
        }
        public int DestBay
        {
            get
            {
                if (DestItem != null)
                {
                    return DestItem.iBay;
                }
                else
                {
                    return -999;
                }
            }
        }

        public int DestLevel
        {
            get
            {
                if (DestItem != null)
                {
                    return DestItem.iLevel;
                }
                else
                {
                    return -999;
                }
            }
        }


        public int SourceBank
        {
            get
            {
                if (SourceItem != null)
                {
                    return SourceItem.iBank;
                }
                else
                {
                    return -999;
                }
            }
        }
        public int SourceBay
        {
            get
            {
                if (SourceItem != null)
                {
                    return SourceItem.iBay;
                }
                else
                {
                    return -999;
                }
            }
        }

        public int SourceLevel
        {
            get
            {
                if (SourceItem != null)
                {
                    return SourceItem.iLevel;
                }
                else
                {
                    return -999;
                }
            }
        }

        public int AlterShelfBank
        {
            get
            {
                if (AlterShelfItem != null)
                {
                    return AlterShelfItem.iBank;
                }
                else
                {
                    return -999;
                }
            }
        }
        public int AlterShelfBay
        {
            get
            {
                if (AlterShelfItem != null)
                {
                    return AlterShelfItem.iBay;
                }
                else
                {
                    return -999;
                }
            }
        }

        public int AlterShelfLevel
        {
            get
            {
                if (AlterShelfItem != null)
                {
                    return AlterShelfItem.iLevel;
                }
                else
                {
                    return -999;
                }
            }
        }




        public eJobResultCode JobResult
        {
            get;
            set; //SuHwan_20220531 : private 삭제
        }

        private RMModuleBase _AssignedRM;

        public RMModuleBase AssignedRM
        {
            get
            {
                return _AssignedRM;
            }
            set
            {
                _AssignedRM = value;
                if (_AssignedRM != null)
                {
                    OnPropertyChanged(new PropertyChangedEventArgs("AssignRM"));
                    OnPropertyChanged(new PropertyChangedEventArgs("AssignRMNumber"));
                }
            }
        }
        public RMModuleBase UnAssignedRM
        {
            get
            {
                if (_AssignedRM == null)
                {
                    return null;
                }
                else
                {
                    if (_AssignedRM.RMNumber == GlobalData.Current.mRMManager.FirstRM.RMNumber)
                    {
                        return GlobalData.Current.mRMManager.SecondRM;
                    }
                    else if (_AssignedRM.RMNumber == GlobalData.Current.mRMManager.SecondRM.RMNumber)
                    {
                        return GlobalData.Current.mRMManager.FirstRM;
                    }
                    else
                    {
                        return null;
                    }

                }
            }
        }

        public ICarrierStoreAble SourceItem
        {
            get
            {
                return GlobalData.Current.GetGlobalCarrierStoreAbleObject(Source);
            }
        }
        public ICarrierStoreAble DestItem
        {
            get
            {
                return GlobalData.Current.GetGlobalCarrierStoreAbleObject(Destination);
            }
        }
        public ICarrierStoreAble prevDestItem
        {
            get
            {
                return GlobalData.Current.GetGlobalCarrierStoreAbleObject(prevDestination);
            }
        }
        public ICarrierStoreAble HandOverBufferItem
        {
            get
            {
                return ShelfManager.Instance.GetShelf(HandOverStoredDest);
            }
        }
        public ICarrierStoreAble AlterShelfItem
        {
            get
            {
                return ShelfManager.Instance.GetShelf(AltShelfDestination); //디폴트로 쉘프를 가져온다.
            }
        }
        /// <summary>
        /// 잡 목표 포트
        /// </summary>
        private CV_BaseModule _TargetCV;

        public CV_BaseModule TargetCV
        {
            get
            {
                return _TargetCV;
            }
            set
            {
                _TargetCV = value;
                if (_TargetCV != null)
                {
                    OnPropertyChanged(new PropertyChangedEventArgs("TargetCV"));
                }
            }
        }

        public string TargetCVPortType
        {
            get
            {
                if (DestItem is CV_BaseModule cm)
                {
                    return cm.PortType.ToString();
                }
                else
                {
                    return "";
                }
                //return _TargetCV != null ? _TargetCV.PortType.ToString() : "";
            }
        }

        public eScheduleJobFrom JobFrom { get; set; }
        public eSubJobType SubJob
        {
            get;
            set;
        }



        public CarrierItem JobCarrierItem
        {
            get
            {
                return CarrierStorage.Instance.GetCarrierItem(CarrierID);
            }
        }

        public bool JobWithDrawRequest { get; set; }

        public bool NeedTransferringReport = true;

        public bool JobWaitOutReported = false; //작업에 대해 WaitOut 보고했음

        public bool JobCarrierOutOfSTK = false; //작업에 대해 해당 캐리어는 포트로 배출되어 STK에서 삭제됨

        public bool JobHoldingState
        {
            get;
            set;
        }

        private bool _MnlCmdJobCreate = false;
        public bool MnlCmdJobCreate 
        {
            get
            {
                return _MnlCmdJobCreate;
            }
            set
            {
                _MnlCmdJobCreate = value;
            }
        }


        public bool JobNowDeleting { get; set; } //240617 RGJ 현재 삭제 프로세스 진행중 플래그. 삭제중인데 스케쥴링에서 겹치는 케이스 제외하기 위해 추가함


        #endregion

        #region Job Property Change Event 
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);

        }
        #endregion

        public McsJob()
        {
            CommandID = "";
            CommandType = "";
            CarrierType = "";
            CarrierID = "";
            CarrierLotID = "";
            //CarrierLoc = "";
            //CarrierZoneName = "";
            Destination = "";
            DestGroup = "";
            Source = "";
            SourceGroup = "";
            CreateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        }
        public void SetJobAbort(bool AbortFlag)
        {
            MCSJobAbortReq = AbortFlag;
        }
        public ePortType GetCurrentJobPortType()
        {
            //추후 구현
            return ePortType.LP;
        }

        /// <summary>
        /// 동시 스케쥴링을 위해 다음 목적지를 추정한다.
        /// </summary>
        /// <returns></returns>
        public int EstJobNextMoveBay()
        {
            RMModuleBase RM = AssignedRM;
            try
            {
                //240701 RGJ 해당로직 삭제 AlterShelfItem 은 쉘프에 들어가면 Null 처리 되므로 무언정지 발생하는 케이스 있음. 실제 캐리어 위치로 검색해야함
                //if (SubJob == eSubJobType.AlterStore)
                //{
                //    return AlterShelfBay;
                //}
                if (SubJob == eSubJobType.Push)
                {
                    return DestBay;
                }

                if (RM != null && RM.CheckCarrierExist()) //캐리어를 들고 있는경우
                {
                    if (CommandType == "MOVE") //이동 작업일경우 목적지로만 간다.
                    {
                        return DestBay;
                    }
                    if (SubJob == eSubJobType.HandOver)
                    {
                        if (GlobalData.Current.Scheduler.CheckCraneReachAble(RM, DestItem))   //목적지로 간다.
                        {
                            return DestBay;
                        }
                        else if (GlobalData.Current.Scheduler.CheckCraneReachAble(RM, HandOverBufferItem))   //경유지로 간다.
                        {
                            return HandOverBufferItem != null ? HandOverBufferItem.iBay : RM.CurrentBay;
                        }
                        else //캐리어 들고 있는게 갈데가 없음..?
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Job : {0}  RM :{1} CurrentBay :{2} Source :{3} HandOver :{4} Dest : {5} Error exist while calculating estimate handover move position.", CommandID, RM.ModuleName, RM.CurrentBay, SourceBay, HandOverBufferItem?.iBay, DestBay);
                            return RM.CurrentBay; //일단 현재 위치 리턴.
                        }

                    }
                    else if(SubJob == eSubJobType.AlterStore) //240701 대체 반송 작업이라면 목적지가 아닌 대체 반송지로 가야함.
                    {
                        if(AlterShelfItem != null) //대체 반송지로 가야함.
                        {
                            return AlterShelfBay;
                        }
                        else
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Job : {0}  RM :{1} CurrentBay :{2} Source :{3} HandOver :{4} Dest : {5} Error exist while calculating estimate AlterStore move position.", CommandID, RM.ModuleName, RM.CurrentBay, SourceBay, HandOverBufferItem?.iBay, DestBay);
                            return RM.CurrentBay; //일단 현재 위치 리턴.
                        }
                    }
                    else
                    {
                        return DestBay;
                    }
                }
                else //빈손일때
                {
                    if (CommandType == "MOVE") //이동 작업일경우 목적지로만 간다.
                    {
                        return DestBay;
                    }
                    else
                    {
                        ICarrierStoreAble CarrierStored = CarrierLocationItem;
                        if (CarrierStored != null)  //2024.07.05 lim, 예외 처리 추가 
                        {
                            if (SubJob == eSubJobType.HandOver)
                            {

                                if (GlobalData.Current.Scheduler.CheckCraneReachAble(RM, CarrierStored))   //캐리어의 위치로 가야함
                                {
                                    return CarrierStored.iBay;
                                }
                                else //빈손인데 캐리어 위치로  갈수가 없음..?
                                {
                                    LogManager.WriteConsoleLog(eLogLevel.Info, "Job : {0}  RM :{1} CurrentBay :{2} Source :{3} HandOver :{4} Dest : {5} Error exist while calculating estimate handover move position.", CommandID, RM.ModuleName, RM.CurrentBay, SourceBay, HandOverBufferItem?.iBay, DestBay);
                                    return RM.CurrentBay; //일단 현재 위치 리턴.
                                }
                            }
                            else
                            {
                                return CarrierStored.iBay; //캐리어의 현재 위치로 가야함
                            }
                        }
                        else
                        {
                            LogManager.WriteConsoleLog(eLogLevel.Info, "Job : {0}  RM :{1} CurrentBay :{2} Source :{3} Dest : {4} Error exist while calculating estimate CarrierLocationItem is null.", CommandID, RM.ModuleName, RM.CurrentBay, SourceBay, DestBay);
                            return RM.CurrentBay; //일단 현재 위치 리턴.
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "EstJobNextMoveBay() Exception Occurred \r\n {0}",ex.ToString());
                return RM.CurrentBay;
            }
        }
        public void SetJobForceAbort()
        {
            LogManager.WriteConsoleLog(eLogLevel.Info, "Job Command:{0} Force Abort Request", CommandID);
            JobForceAbortRequest = true;
        }

        #region PlayBack Interface

        public bool IsPlayBackInstance = false; //현재 인스턴스는 플레이백 엔진이 만든것임을 체크(DB 업데이트 방지용)

        private bool PlaybackTrace = false; //해당 작업 플레이백을 추적할건지 결정.
        public void SetPlayBackTrace()
        {
            PlaybackTrace = true;
        }
        public bool NeedPlayBackWrite()
        {
            return GlobalData.Current.UsePlayBackLog && PlaybackTrace;
        }

        public int CompareTo(McsJob job)
        {
            if(ScheduledPriority > job.ScheduledPriority)
            {
                return 1;
            }
            else if(ScheduledPriority == job.ScheduledPriority)
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }
        #endregion

        private ICarrierStoreAble _LastActionTarget = null;//해당작업을 하면서 마지막으로 내린 명령 타겟을 저장해둠
        public ICarrierStoreAble LastActionTarget
        {
            get
            {
                return _LastActionTarget;
            }
            set
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, "Job: {0} CID : {1} LastActionTarget {2}  =>  {3}",CommandID, CarrierID, _LastActionTarget?.iLocName, value?.iLocName);
                _LastActionTarget = value;

            }
        }

    }
}
