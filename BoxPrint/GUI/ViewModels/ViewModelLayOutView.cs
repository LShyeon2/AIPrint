using BoxPrint.DataList.MCS;         //230105 HHJ SCS 개선
using BoxPrint.GUI.ETC;
using BoxPrint.GUI.ExtensionCollection;
using BoxPrint.GUI.UIControls;
using BoxPrint.GUI.UserControls;
using BoxPrint.GUI.UserControls.Views;
using BoxPrint.GUI.ViewModels.BindingCommand;
using BoxPrint.Log;
using BoxPrint.Modules.Conveyor;
using BoxPrint.Modules.RM;
using BoxPrint.Modules.Shelf;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.ViewModels
{
    public class ViewModelLayOutView : ViewModelBase
    {
        #region Event
        public delegate void ControlSelectionChanged(ControlBase Selected);
        public event ControlSelectionChanged OnControlSelectionChanged;
        #endregion

        #region Variable
        private DateTime ViewInitTime = DateTime.Now;       //221017 조숭진

        private ControlBase SelectUnit = null;
        private object SelectUnitLock = new object();

        public bool IsPlayBackControl = false;

        private ConveyorIOView conveyorIO;
        private CraneIOView craneIO;
        private BoothIOView boothIO;    //230317 HHJ SCS 개선

        private ConveyorDataView cvDataView;
        private ShelfDataView shelfDataView;
        private CraneDataView rmDataView;

        private readonly decimal ScaleOriginValue = 1;
        #region Binding
        //221226 HHJ SCS 개선
        private string _ControlSystemName;
        public string ControlSystemName
        {
            get => _ControlSystemName;
            set => Set("ControlSystemName", ref _ControlSystemName, value);
        }
        private string _ControlSystemEqpID;
        public string ControlSystemEqpID
        {
            get => _ControlSystemEqpID;
            set => Set("ControlSystemEqpID", ref _ControlSystemEqpID, value);
        }

        //230105 HHJ SCS 개선
        private McsJob _SelectJobItem;
        public McsJob SelectJobItem
        {
            get => _SelectJobItem;
            set => Set("SelectJobItem", ref _SelectJobItem, value);
        }
        private McsJobManager _McsJobList;
        public McsJobManager McsJobList
        {
            get => _McsJobList;
            set => Set("McsJobList", ref _McsJobList, value);
        }
        public ICommand JobDelete { get; private set; }
        public ICommand JobPriorityChange { get; private set; }

        private string _SelectUnitID;
        public string SelectUnitID
        {
            get => _SelectUnitID;
            set => Set("SelectUnitID", ref _SelectUnitID, value);
        }
        private UserControl _IOView;
        public UserControl IOView
        {
            get => _IOView;
            set => Set("IOView", ref _IOView, value);
        }
        private UserControl _DataViewControl;
        public UserControl DataViewControl
        {
            get => _DataViewControl;
            set => Set("DataViewControl", ref _DataViewControl, value);
        }
        //220919 HHJ SCS 개선     //- Layout Slide 배율 바인딩으로 변경
        private decimal _ScaleValue = 1;
        public decimal ScaleValue
        {
            get => _ScaleValue;
            set
            {
                ScalePercent = ScaleOriginValue * value * 100;
                Set("ScaleValue", ref _ScaleValue, value);

                if (LayOutControl != null)
                    LayOutControl.vm.ScaleValue = value;
            }
        }
        private decimal _ScaleTick = 1;
        public decimal ScaleTick
        {
            get => _ScaleTick;
            set => Set("ScaleTick", ref _ScaleTick, value);
        }
        private decimal _ScaleMin = 1;
        public decimal ScaleMin
        {
            get => _ScaleMin;
            set => Set("ScaleMin", ref _ScaleMin, value);
        }
        private decimal _ScaleMax = 1;
        public decimal ScaleMax
        {
            get => _ScaleMax;
            set => Set("ScaleMax", ref _ScaleMax, value);
        }
        private decimal _ChangeOriginValue = 0;
        public decimal ChangeOriginValue
        {
            get => _ChangeOriginValue;
            set => Set("ChangeOriginValue", ref _ChangeOriginValue, value);
        }
        private decimal _ScalePercent = 1;
        public decimal ScalePercent
        {
            get => _ScalePercent;
            set => Set("ScalePercent", ref _ScalePercent, value);
        }
        //SuHwan_20221226 : [1차 UI검수] 폰트 사이즈 설정
        protected int _UIFontSize_Large = 13;  //큰폰트
        public int UIFontSize_Large
        {
            get => _UIFontSize_Large;
            set
            {
                if (_UIFontSize_Large == value) return;
                _UIFontSize_Large = value;

                RaisePropertyChanged("UIFontSize_Large");
            }
        }

        protected int _UIFontSize_Medium = 11; //중간폰트
        public int UIFontSize_Medium
        {
            get => _UIFontSize_Medium;
            set
            {
                if (_UIFontSize_Medium == value) return;
                _UIFontSize_Medium = value;

                RaisePropertyChanged("UIFontSize_Medium");
            }
        }

        protected int _UIFontSize_Small = 9;  //작은폰트
        public int UIFontSize_Small
        {
            get => _UIFontSize_Small;
            set
            {
                if (_UIFontSize_Small == value) return;
                _UIFontSize_Small = value;

                RaisePropertyChanged("UIFontSize_Small");
            }
        }

        //SuHwan_20230317 : 알람 확인
        protected bool _AlarmExist = false;
        public bool AlarmExist
        {
            get => _AlarmExist;
            set
            {
                if (_AlarmExist == value) return;
                _AlarmExist = value;

                RaisePropertyChanged("AlarmExist");
            }
        }

        public ICommand ScaleCommand { get; private set; }
        //lable_LoadRate.Text = string.Format("{0:0.00}%  ({1}/{2})", ShelfManager.Instance.GetShelfLoadRatio(), ShelfManager.Instance.GetOccupiedShelfCount(), ShelfManager.Instance.GetAvailableShelfCount());
        
        private string _TotalCarrierInSTK;
        public string TotalCarrierInSTK
        {
            get
            {
                return _TotalCarrierInSTK;
            }
        }

        private string _ShelfLoadRatio;
        public string ShelfLoadRatio
        {
            get
            {
                return _ShelfLoadRatio;
            }
        }


        private string _ShelfUNKIDCount;
        public string ShelfUNKIDCount
        {
            get
            {
                return _ShelfUNKIDCount;
            }
        }
        /// <summary>
        /// 240809 RGJ 메인화면 좌상단 캐리어 카운트 조회 업데이트 통합
        /// </summary>
        public bool ShelfCountDataUpdate
        {
            set
            {
                int OccupiedShelfCount = ShelfManager.Instance.GetOccupiedShelfCount();
                int FullCarrierCount = ShelfManager.Instance.GetFullCarrierCount();
                int EmptyCarrierCount = ShelfManager.Instance.GetEmptyCarrierCount();
                int NoneCarrierCount = ShelfManager.Instance.GetNoneCarrierCount();
                int TotalPortCarrierCount = GlobalData.Current.PortManager.AllCVList.Where(c => c.CarrierExistBySensor() && c.InSlotCarrier != null).Count();   //2024.08.24 lim, 데이터가 있는지 같이 본다
                int TotalCraneCarrierCount = GlobalData.Current.mRMManager.TotalCarrierInCrane;


                _ShelfUNKIDCount = TranslationManager.Instance.Translate("UNK ID Count :").ToString() + " " + ShelfManager.Instance.GetUNKIDShelfCount().ToString();
                _ShelfProductFullCount = string.Format("Full : {0:0.00}% ({1}/{2})", ((double)FullCarrierCount / (double)OccupiedShelfCount) *100 , FullCarrierCount, OccupiedShelfCount);
                _ShelfProductEmptyCount = string.Format("Empty : {0:0.00}% ({1}/{2})", ((double)EmptyCarrierCount / (double)OccupiedShelfCount) * 100, EmptyCarrierCount, OccupiedShelfCount);
                _ShelfProductNoneCount = string.Format("None : {0:0.00}% ({1}/{2})", ((double)NoneCarrierCount / (double)OccupiedShelfCount) * 100, NoneCarrierCount, OccupiedShelfCount);

                _ShelfLoadRatio = TranslationManager.Instance.Translate("Load Rate :").ToString() + " " + ShelfManager.Instance.GetLoadRatioMessage();
                _TotalCarrierInSTK = string.Format("{0} : {1}", TranslationManager.Instance.Translate("Carrier").ToString(), OccupiedShelfCount + TotalPortCarrierCount + TotalCraneCarrierCount);

                RaisePropertyChanged("ShelfUNKIDCount");
                RaisePropertyChanged("ShelfProductFullCount");
                RaisePropertyChanged("ShelfProductEmptyCount");
                RaisePropertyChanged("ShelfProductNoneCount");
                RaisePropertyChanged("ShelfLoadRatio");
                RaisePropertyChanged("TotalCarrierInSTK");
            }
        }
        private string _ShelfProductFullCount;
        public string ShelfProductFullCount
        {
            get
            {
                return _ShelfProductFullCount;
            }
        }
        private string _ShelfProductEmptyCount;
        public string ShelfProductEmptyCount
        {
            get
            {
                return _ShelfProductEmptyCount;
            }
        }

        private string _ShelfProductNoneCount;
        public string ShelfProductNoneCount
        {
            get
            {
                return _ShelfProductNoneCount;
            }
        }


        private LayOutControlView _LayOutControl;
        public LayOutControlView LayOutControl
        {
            get => _LayOutControl;
            set => Set("LayOutControl", ref _LayOutControl, value);
        }
        #endregion
        #endregion

        #region Constructor, Thread
        public ViewModelLayOutView()
        {
            ViewInitTime = new DateTime(ViewInitTime.Year, ViewInitTime.Month, ViewInitTime.Day, ViewInitTime.Hour, 0, 0);

            //221226 HHJ SCS 개선
            ControlSystemName = "Stocker Control System";
            ControlSystemEqpID = GlobalData.Current.EQPID;

            //221228 HHJ SCS 개선
            cvDataView = new ConveyorDataView(IsPlayBackControl);
            shelfDataView = new ShelfDataView(IsPlayBackControl);
            rmDataView = new CraneDataView(IsPlayBackControl);

            McsJobList = GlobalData.Current.McdList;

            //230105 HHJ SCS 개선
            JobDelete = new BindingDelegateCommand(JobDeleteAction);
            JobPriorityChange = new BindingDelegateCommand(JobPriorityChangeAction);

            conveyorIO = new ConveyorIOView(IsPlayBackControl);
            craneIO = new CraneIOView(IsPlayBackControl);
            boothIO = new BoothIOView(IsPlayBackControl);

            ScaleCommand = new BindingDelegateCommand<eZoomCommandProperty>(ScaleCommandAction);

            LayOutControl = new LayOutControlView(IsPlayBackControl);
            LayOutControl.vm.OnLayOutUnitSelect += Vm_OnLayOutUnitSelect;
            LayOutControl.vm.OnLayOutScaleDataChange += Vm_OnLayOutScaleDataChange;

        }
        #endregion

        #region Command
        //230105 HHJ SCS 개선
        private void JobDeleteAction()
        {
            try
            {
                McsJob TargetJob = SelectJobItem;
                string cmdID = string.Empty;
                MessageBoxPopupView msgbox = null;
                string msg = string.Empty;
                CustomMessageBoxResult mBoxResult = null;

                LogManager.WriteOperatorLog(string.Format("사용자가 {0} 에서 {1} 을/를 Click하였습니다.", "Home", "Delete"),
                    "CLICK", "Delete", GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 5,
                    "Home", "Delete");

                if (TargetJob is null)
                {
                    msg = TranslationManager.Instance.Translate("Job is not selected for removing.").ToString();
                    MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                cmdID = TargetJob.CommandID;

                msg = string.Format(TranslationManager.Instance.Translate("Job").ToString() + "\r\n[{0}]\n" +
                                    TranslationManager.Instance.Translate("Delete").ToString() + "?",
                                    cmdID);
                msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);

                mBoxResult = msgbox.ShowResult();

                if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                {
                    bool bComplete = false;

                    LogManager.WriteOperatorLog(string.Format("사용자가 {0} {1} {2} 을/를 Click하였습니다.", "Home", "Job", "Delete"),
                        "DELETE", "Delete", GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 6,
                        "Home", "Job", "Delete");

                    //SuHwan_20230202 : [ServerClient]
                    if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                    {
                        CV_BaseModule cv = TargetJob.CarrierLocationItem as CV_BaseModule;
                        if (TargetJob.AssignedRM != null && !TargetJob.AssignedRM.CheckForkIsCenter())//240514 포킹중이거나 포트 배출 포트에 화물이 있을경우 삭제 불가.
                        {
                            MessageBoxPopupView.Show("Job cannot be aborted.Crane is forking state", MessageBoxButton.OK, MessageBoxImage.Information); //팝업 박스 보여주기 위해서 어쩔수 없음...
                        }
                        else if(cv != null && cv.PortInOutType == ePortInOutType.OUTPUT && cv.CarrierExist) //배출포트에 있음. //2024.05.17 lim, UI도 삭제 조건 수정
                        {
                            MessageBoxPopupView.Show("Job cannot be aborted.Carrier is in Outport", MessageBoxButton.OK, MessageBoxImage.Information); //팝업 박스 보여주기 위해서 어쩔수 없음...
                        }
                        else
                        {
                            //240513 RGJ 클라이언트도 잡 상태 상관없이 요청은 보낼수 있게 한다.
                            bool SendSuccess = GlobalData.Current.DBManager.DbSetProcedureClientReq(GlobalData.Current.EQPID, "Delete", "Job", cmdID, string.Empty, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), eServerClientType.Client);
                            if (SendSuccess)
                            {
                                MessageBoxPopupView.Show("Job abort request has been sent successfully.", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            else
                            {
                                MessageBoxPopupView.Show("Job abort request failed.", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                    }
                    else
                    {
                        eUIJobRemoveResult result = GlobalData.Current.McdList.ProcessUIJobRemoveRequest(TargetJob);

                        switch(result)
                        {
                            case eUIJobRemoveResult.NoResult:
                                msg = string.Format("{0}\r\n\r\n Job Delete Process - No Result. ", cmdID);
                                break;
                            case eUIJobRemoveResult.Abort:
                                msg = string.Format("{0}\r\n\r\n Job Aborted Successfully.", cmdID);
                                break;
                            case eUIJobRemoveResult.AbortFail:
                                msg = string.Format("{0}\r\n\r\n Job Abort Failed.", cmdID);
                                break;
                            case eUIJobRemoveResult.AbortRequest:
                                msg = string.Format("{0}\r\n\r\n Job is in progress.Sent abort request.", cmdID);
                                break;
                            case eUIJobRemoveResult.AbortAlreadyRequest:
                                msg = string.Format("{0}\r\n\r\n Job is already in aborting.", cmdID);
                                break;
                            case eUIJobRemoveResult.AbortJobForceComplete:
                                msg = string.Format("{0}\r\n\r\n Job is forced completed by User.", cmdID);
                                break;
                            case eUIJobRemoveResult.Cancel:
                                msg = string.Format("{0}\r\n\r\n Job Canceled Successfully.", cmdID); ;
                                break;
                            case eUIJobRemoveResult.CancelFail:
                                msg = string.Format("{0}\r\n\r\n Job Cancel Failed.", cmdID);
                                break;
                        }
                        //if (bComplete)
                        //    msg = string.Format(TranslationManager.Instance.Translate("Job").ToString() + "[{0}]\n" +
                        //                        TranslationManager.Instance.Translate("Delete").ToString() + " " +
                        //                        TranslationManager.Instance.Translate("Complete").ToString(),
                        //                        cmdID);
                        //else
                        //    msg = string.Format(TranslationManager.Instance.Translate("Job").ToString() + "[{0}]\n" +
                        //                        TranslationManager.Instance.Translate("Delete").ToString() + " " +
                        //                        TranslationManager.Instance.Translate("Fail").ToString(),
                        //                        cmdID);
                        MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }
        private void JobPriorityChangeAction()
        {
            try
            {
                string cmdID = string.Empty;
                MessageBoxPopupView msgbox = null;
                string msg = string.Empty;
                CustomMessageBoxResult mBoxResult = null;

                LogManager.WriteOperatorLog(string.Format("사용자가 {0} 에서 {1} 을/를 Click하였습니다.", "Home", "Priority Change"),
                    "CLICK", "Priority Change", GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 5,
                    "Home", "Priority Change");

                if (SelectJobItem is null)
                {
                    msg = TranslationManager.Instance.Translate("Job is not selected for changing priority.").ToString();
                    MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                McsJob selecjob = SelectJobItem;
                cmdID = selecjob.CommandID;

                msg = string.Format(TranslationManager.Instance.Translate("Current").ToString() + " " +
                                    TranslationManager.Instance.Translate("Job").ToString() + "\r\n[{0}]\r\n" +
                                    TranslationManager.Instance.Translate("Priority").ToString() + "[{1}]\r\n\r\n" +
                                    TranslationManager.Instance.Translate("Enter Priority to Change").ToString(),
                                    cmdID, selecjob.Priority);
                msgbox = new MessageBoxPopupView(msg, TranslationManager.Instance.Translate("Priority").ToString(), MessageBoxButton.OKCancel, MessageBoxImage.Question);

                mBoxResult = msgbox.ShowResult();

                if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                {
                    int iresult = 0;
                    if (!int.TryParse(mBoxResult.InputResult, out iresult))
                    {
                        MessageBoxPopupView.Show(TranslationManager.Instance.Translate("Only numbers can be entered").ToString(), MessageBoxImage.Stop, false);
                        return;
                    }

                    msg = string.Format(TranslationManager.Instance.Translate("Job").ToString() + "[{0}]\n" + 
                                        TranslationManager.Instance.Translate("Priority").ToString() + "[{1}] -> [{2}]\n" +
                                        TranslationManager.Instance.Translate("Change").ToString() + "?",
                                        cmdID, selecjob.Priority, iresult);
                    msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);

                    mBoxResult = msgbox.ShowResult();

                    if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                    {
                        bool bComplete = false;

                        LogManager.WriteOperatorLog(string.Format("사용자가 {0} {1} {2} 을/를 Click하였습니다.", "Home", "Job", "Priority Change"),
                            "CLICK", "Delete", GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 6,
                            "Home", "Job", "Priority Change");

                        //230207 추가 s [ServerClient]
                        if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                        {
                            GlobalData.Current.DBManager.DbSetProcedureClientReq(GlobalData.Current.EQPID, "PRIORITYCHANGE", "JOB", cmdID, iresult.ToString(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), eServerClientType.Client);
                        }
                        else
                        {
                            bComplete = GlobalData.Current.McdList.ChangePriority(selecjob, iresult);

                            if (bComplete)
                                msg = string.Format(TranslationManager.Instance.Translate("Job").ToString() + "[{0}]\n" +
                                                    TranslationManager.Instance.Translate("Priority").ToString() + "[{1}] -> [{2}]\n" +
                                                    TranslationManager.Instance.Translate("Change").ToString() + " " +
                                                    TranslationManager.Instance.Translate("Complete").ToString(),
                                                    cmdID, selecjob.Priority, iresult);
                            else
                                msg = string.Format(TranslationManager.Instance.Translate("Job").ToString() + "[{0}]\n" +
                                                    TranslationManager.Instance.Translate("Priority").ToString() + "[{1}] -> [{2}]\n" +
                                                    TranslationManager.Instance.Translate("Change").ToString() + " " +
                                                    TranslationManager.Instance.Translate("Fail").ToString(),
                                                    cmdID, selecjob.Priority, iresult);

                            MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        //230207 추가 e [ServerClient]
                    }
                }
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }
        public void ScaleCommandAction(eZoomCommandProperty cmdProperty)
        {
            try
            {
                if (cmdProperty.Equals(eZoomCommandProperty.eRotate))
                    LayOutControl.RotateLayOut();
                else
                    LayOutControl.vm.ScaleCommandAction(cmdProperty);
                //switch (cmdProperty)
                //{
                //    case eZoomCommandProperty.ePlus:
                //        if (ScaleValue < ScaleMax)
                //            ScaleValue += ScaleTick;
                //        break;
                //    case eZoomCommandProperty.eOrigin:
                //        //확대가된 상황에서 다시 1으로 변경해주면 스크롤바가 남아있는 현상이 있음....
                //        //Tick만큼 빼준거를 초기값으로 잡아준다.
                //        ScaleValue = (ScaleOriginValue - ChangeOriginValue) - ScaleTick;

                //        break;
                //    case eZoomCommandProperty.eMinus:
                //        if (ScaleValue > ScaleMin)
                //            ScaleValue -= ScaleTick;
                //        break;
                //    case eZoomCommandProperty.eVerticalReverse:
                //        break;
                //    case eZoomCommandProperty.eHorizonReserve:
                //        break;
                //}
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }
        #endregion

        #region Methods
        //public void SetSelectUnit(ControlBase select)     //230215 HHJ SCS 개선     //private 변경
        private void SetSelectUnit(ControlBase select)
        {
            try
            {
                lock (SelectUnitLock)
                {
                    //230118 HHJ SCS 개선
                    if (SelectUnit != null)
                        SelectUnit.Selector = false;

                    if (select is null)
                        return;

                    SelectUnit = select;

                    //221228 HHJ SCS 개선
                    rmDataView.vm.DisableViewmodel();
                    cvDataView.vm.DisableViewmodel();
                    shelfDataView.vm.DisableViewmodel();

                    boothIO.DisableControl();       //Booth는 뭘 선택하던 무조건 Disable로 변경

                    SelectUnitID = select != null ? select.ControlName : string.Empty;

                    if (SelectUnit is RMModuleBase rm)
                    {
                        //221228 HHJ SCS 개선
                        //SelectUnitType = "CRANE";
                        DataViewControl = rmDataView;
                        rmDataView.vm.AbleViewModel(SelectUnit);

                        conveyorIO.DisableControl();
                        craneIO.DisableControl();
                        IOView = craneIO;
                        craneIO.AbleControl(SelectUnit);
                    }
                    else if (SelectUnit is CV_BaseModule cv)
                    {
                        //221228 HHJ SCS 개선
                        //SelectUnitType = "CONVEYOR";
                        DataViewControl = cvDataView;
                        cvDataView.vm.AbleViewModel(SelectUnit);

                        conveyorIO.DisableControl();
                        craneIO.DisableControl();
                        IOView = conveyorIO;
                        conveyorIO.AbleControl(SelectUnit);
                    }
                    else if (SelectUnit is ShelfItem shelf)
                    {
                        //221228 HHJ SCS 개선
                        //SelectUnitType = "SHELF";
                        DataViewControl = shelfDataView;
                        shelfDataView.vm.AbleViewModel(SelectUnit);
                    }
                    //230118 HHJ SCS 개선
                    if (SelectUnit != null)
                        SelectUnit.Selector = true;
                }
            }
            catch (Exception)
            {

            }
        }
        //230215 HHJ SCS 개선
        private void Vm_OnLayOutScaleDataChange(eScaleProperty property, decimal value)
        {
            switch (property)
            {
                case eScaleProperty.eValue:
                    ScaleValue = value;
                    break;
                case eScaleProperty.eTick:
                    ScaleTick = value;
                    break;
                case eScaleProperty.eMin:
                    ScaleMin = value;
                    break;
                case eScaleProperty.eMax:
                    ScaleMax = value;
                    break;
                case eScaleProperty.eChangeOrigin:
                    ChangeOriginValue = value;
                    break;
            }
        }
        private void Vm_OnLayOutUnitSelect(UIControlBase selectUnit, bool rightClick)
        {
            ControlBase selecControl = GUIExtensionCollection.GetUnit(selectUnit, IsPlayBackControl);
            SetSelectUnit(selecControl);
            OnControlSelectionChanged?.Invoke(selecControl);
        }
        //230317 HHJ SCS 개선
        public void ChangeBoothIO()
        {
            conveyorIO.DisableControl();
            craneIO.DisableControl();
            boothIO.DisableControl();

            IOView = boothIO;
            SelectUnitID = GlobalData.Current.MainBooth.ControlName;

            boothIO.AbleControl(GlobalData.Current.MainBooth);
        }
        #endregion

        #region Helper

        #endregion
    }
}
