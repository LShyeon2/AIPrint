using BoxPrint.DataList;
using BoxPrint.GUI.ETC;
using BoxPrint.GUI.UIControls;
using BoxPrint.GUI.ViewModels.BindingCommand;
using BoxPrint.GUI.Views;
using BoxPrint.Modules;
using BoxPrint.Modules.Conveyor;
using BoxPrint.Modules.RM;
using BoxPrint.Modules.Shelf;
using System;
using System.Windows;
using System.Windows.Input;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.ViewModels
{
    public class CraneOrderViewModel : ViewModelBase
    {
        #region Variables
        private LayOutView layOut;
        private RMModuleBase SelectRM = null;
        #region Binding
        private double _HeaderTextSize;
        public double HeaderTextSize
        {
            get => _HeaderTextSize;
            set => Set("HeaderTextSize", ref _HeaderTextSize, value);
        }
        private double _BodyTextSize;
        public double BodyTextSize
        {
            get => _BodyTextSize;
            set => Set("BodyTextSize", ref _BodyTextSize, value);
        }

        private string _CraneID;
        public string CraneID
        {
            get => _CraneID;
            set => Set("CraneID", ref _CraneID, value);
        }

        private string _SourceName;
        public string SourceName
        {
            get => _SourceName;
            set => Set("SourceName", ref _SourceName, value);
        }

        private string _DestName;
        public string DestName
        {
            get => _DestName;
            set => Set("DestName", ref _DestName, value);
        }

        private string _CarrierID;
        public string CarrierID
        {
            get => _CarrierID;
            set => Set("CarrierID", ref _CarrierID, value);
        }

        private decimal _Priority;
        public decimal Priority
        {
            get => _Priority;
            set => Set("Priority", ref _Priority, value);
        }

        private string _TaskManual;
        public string TaskManual
        {
            get => _TaskManual;
            set => Set("TaskManual", ref _TaskManual, value);
        }

        private string _TaskInfo;
        public string TaskInfo
        {
            get => _TaskInfo;
            set => Set("TaskInfo", ref _TaskInfo, value);
        }

        private bool _MoveOnly;
        public bool MoveOnly
        {
            get => _MoveOnly;
            set
            {
                Set("MoveOnly", ref _MoveOnly, value);
                if(_MoveOnly)
                {
                    SourceName = CraneID;
                }
            }
        }

        #endregion

        #region Command
        public ICommand InitCommand { get; private set; }
        public ICommand OkCommand { get; private set; }
        #endregion
        #endregion

        #region Constructor
        public CraneOrderViewModel(string craneID, LayOutView layout)
        {
            HeaderTextSize = 15;
            BodyTextSize = 12;

            layOut = layout;
            CraneID = craneID;

            SelectRM = GetRM(craneID);

            InitCommand = new BindingDelegateCommand(ExcuteInitCommand);
            OkCommand = new BindingDelegateCommand(ExcuteOkCommand);

            layOut.vm.OnControlSelectionChanged += LayoutSelectionChanged;

            InitControl();
        }
        #endregion

        #region Methods
        #region Command
        private void ExcuteInitCommand()
        {
            InitControl();
        }
        private void ExcuteOkCommand()
        {
            try //여기서 예외 발생시 프로그램 Down 되므로 예외처리함.
            {
                string CID = CarrierID;
                string Source = SourceName;
                string Dest = DestName;
                ICarrierStoreAble SourceItem = GlobalData.Current.GetGlobalCarrierStoreAbleObject(Source);
                ICarrierStoreAble DestItem = GlobalData.Current.GetGlobalCarrierStoreAbleObject(Dest);
                RMModuleBase RM = SelectRM;
                int priority = (int)Priority;
                string msg = string.Empty;

                if (priority == 0)
                {
                    priority = 1;
                }
                if (SourceItem == null || DestItem == null) //20240125 RGJ 크레인 메뉴얼 명령 출발지나 목적지 누락시 예외처리 추가함.
                {
                    string ErrorMsg = "Source or Destination is not Selected";
                    MessageBoxPopupView mboxError = new MessageBoxPopupView(ErrorMsg, MessageBoxButton.OK, MessageBoxImage.Error);
                    mboxError.ShowResult();
                    return;
                }

                string Confirmmsg = string.Empty;
                msg = string.Format(TranslationManager.Instance.Translate("Carrier ID").ToString() + " : {0}\r\n" +
                                    TranslationManager.Instance.Translate("Source").ToString() + " : {1}  =>\r\n" +
                                    TranslationManager.Instance.Translate("Dest").ToString() + " : {2}\r\n\r\n" +
                                    TranslationManager.Instance.Translate("반송 커맨드를 실행하시겠습니까?").ToString(),
                                    CarrierID, SourceItem.iLocName, DestItem.iLocName);
                MessageBoxPopupView mbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                CustomMessageBoxResult mResult = mbox.ShowResult();

                if (mResult.Result != MessageBoxResult.OK)
                {
                    return;
                }


                //SuHwan_20221107 : [ServerClient] 클라이언트 명령 전용
                if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                {
                    ClientReqList reqBuffer = new ClientReqList
                    {
                        EQPID = GlobalData.Current.EQPID,
                        CMDType = "MJOBCREATE",
                        Target = "Job",
                        TargetID = string.Empty,
                        TargetValue = string.Format("{0}/{1}/{2}/{3}/{4}/{5}", Source, Dest, CarrierID, priority, MoveOnly ? "TRUE" : "FALSE", SelectRM.RMNumber),
                        ReqTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        Requester = eServerClientType.Client,
                    };

                    GlobalData.Current.DBManager.DbSetProcedureClientReq(reqBuffer.EQPID, reqBuffer.CMDType, reqBuffer.Target, reqBuffer.TargetID, reqBuffer.TargetValue, reqBuffer.ReqTime, reqBuffer.Requester);

                    msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}]\n {1}\n {2}->{3}\n" +
                                        TranslationManager.Instance.Translate("ManualRun").ToString() + " " +
                                        TranslationManager.Instance.Translate("Job has been Sent successfully.").ToString(),
                                        SelectRM.ControlName, CarrierID, Source, Dest);

                    //MessageBoxPopupView MBPV = new MessageBoxPopupView(TranslationManager.Instance.Translate("Job has been Send successfully.").ToString(), MessageBoxButton.OK, MessageBoxImage.Information);
                    MessageBoxPopupView MBPV = new MessageBoxPopupView(msg, "크레인 데이터 변경 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                        SelectRM.ControlName, "ManualRun", "Job has been Sent successfully.", true);
                    MBPV.ShowResult();


                    //요롷게 사용
                    //GlobalData.Current.McdList.CreateManualJob_formDB(reqBuffer.TargetValue);

                    return;
                }
                else //Server 에서 실행할경우
                {


                    eManualJobCreateResult result = GlobalData.Current.McdList.CreateManualTransferJob(SourceItem, DestItem, CID, priority, MoveOnly, SelectRM.RMNumber);
                    if (result == eManualJobCreateResult.Success)
                    {
                        msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}]\n {1}\n {2}->{3}\n" +
                                            TranslationManager.Instance.Translate("ManualRun").ToString() + " " +
                                            TranslationManager.Instance.Translate("Job has been created successfully.").ToString(),
                                            SelectRM.ControlName, CarrierID, Source, Dest);

                        //MessageBoxPopupView MBPV = new MessageBoxPopupView(TranslationManager.Instance.Translate("Job has been created successfully.").ToString(), MessageBoxButton.OK, MessageBoxImage.Information);
                        MessageBoxPopupView MBPV = new MessageBoxPopupView(msg, "크레인 데이터 변경 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                            SelectRM.ControlName, "ManualRun", "Job has been created successfully.", true);
                        MBPV.ShowResult();
                        return;
                    }
                    else
                    {
                        Log.LogManager.WriteConsoleLog(eLogLevel.Info, "CreateManualTransferJob Failed. Reason:{0}", result);
                        //230718 RGJ CraneOrder Fail 메시지 구분
                        msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}]\n {1}\n {2}->{3}\n" +
                                            TranslationManager.Instance.Translate("ManualRun").ToString() + " " +
                                            TranslationManager.Instance.Translate("Job creating failed. Check source or destination state.").ToString()
                                            + "\r\nResultCode : {4}",
                                            SelectRM.ControlName, CarrierID, Source, Dest, result);

                        //MessageBoxPopupView MBPV = new MessageBoxPopupView(TranslationManager.Instance.Translate("Job creating failed. Check source or destination state.").ToString() + "\n" + result, MessageBoxButton.OK, MessageBoxImage.Stop, false);
                        MessageBoxPopupView MBPV = new MessageBoxPopupView(msg, "크레인 데이터 변경 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Warning,
                            SelectRM.ControlName, "ManualRun", "Job creating failed. Check source or destination state.", true);
                        MBPV.ShowResult();
                        return;
                    }
                }
            }
            catch(Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info,ex.ToString());
            }
        }
        #endregion
        private void InitControl()
        {
            //Priority는 Click Event에서 처리된다..
            SourceName = string.Empty;
            DestName = string.Empty;
            CarrierID = string.Empty;
            TaskManual = string.Empty;
            TaskInfo = string.Empty;

            //Carrier가 있으면 Dest만 선택하도록 유도해야한다.
            if (SelectRM.CheckCarrierExist() || MoveOnly)
            {
                SourceName = SelectRM.ControlName;
                CarrierID = SelectRM.CarrierID;

                TaskManual = "Right Click to Select Dest";
            }
            //Carrier가 없으면 Source부터 선택하도록 유도해야한다.
            else
            {
                TaskManual = "Right Click to Select Source";
            }
        }
        private RMModuleBase GetRM(string craneID)
        {
            RMModuleBase select = null;
            try
            {
                if (GlobalData.Current.mRMManager.ModuleList.ContainsKey(craneID))
                    select = GlobalData.Current.mRMManager[craneID];
            }
            catch (Exception ex)
            {
                _ = ex;
            }

            return select;
        }
        private void LayoutSelectionChanged(ControlBase control)
        {
            try
            {
                if (control is CV_BaseModule port)
                {
                    //230914 RGJ 조범석 매니저 요청으로 소스 재선택시 바로 변경되게 한다. 
                    //231101 RGJ 조범석 매니저 요청으로 InOut포트에 따라 선택이 되게한다. 
                    if(MoveOnly)
                    {
                        DestName = port.ControlName;
                        TaskManual = "Set Priority and Create Job";
                    }
                    else
                    {
                        if(port.IsInPort && port.CheckCarrierExist()) //InPort 면 화물이 있어야 명령 수행 가능
                        {
                            SourceName = port.ControlName;
                            CarrierID = port.GetCarrierID();
                            TaskManual = "Right Click to Select Dest";
                        }
                        else if(!port.IsInPort) //Output 포트면 화물 상관없이 설정 가능.
                        {
                            DestName = port.ControlName;
                            TaskManual = "Set Priority and Create Job";
                        }
                    }
                }
                else if (control is ShelfItem shelf)
                {
                    //230914 RGJ 조범석 매니저 요청으로 소스 재선택시 바로 변경되게 한다.
                    if (shelf.CheckCarrierExist() && !MoveOnly) //Carrier가 있을때만 진행되어야한다.
                    {
                        SourceName = shelf.TagName;
                        CarrierID = shelf.CarrierID;
                        TaskManual = "Right Click to Select Dest";
                    }
                    else if (!shelf.CheckCarrierExist() || MoveOnly)
                    {
                        DestName = shelf.TagName;
                        TaskManual = "Set Priority and Create Job";
                    }
                }
                else if (control is RMModuleBase rm)
                {
                    if (string.IsNullOrEmpty(SourceName))
                    {
                        //Carrier가 있을때만 진행되어야한다.
                        if (rm.CheckCarrierExist())
                        {
                            SourceName = rm.ControlName;
                            CarrierID = rm.GetCarrierID();
                            TaskManual = "Right Click to Select Dest";
                        }
                    }
                    else
                    {
                        //Carrier가 없을때만 진행되어야한다.
                        if (!rm.CheckCarrierExist())
                        {
                            if(!(GlobalData.Current.GetGlobalCarrierStoreAbleObject(SourceName) is  RMModuleBase))  //소스가 크레인이면 목적지가 크레인이 되면 안됨
                            {
                                DestName = rm.ControlName;
                                TaskManual = "Set Priority and Create Job";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }
        public void AbleViewModel()
        {

        }
        public void DisableVoieModel()
        {
            layOut.vm.OnControlSelectionChanged -= LayoutSelectionChanged;
        }

        public bool ChangeRM(string craneID)
        {
            try
            {
                CraneID = craneID;
                SelectRM = GetRM(craneID);      //231025 HHJ ManualOrder Open 상태에서 Crane 선택시 RMChange 진행
            }
            catch (Exception ex)
            {
                _ = ex;
                return false;
            }

            return true;
        }
        #endregion
    }
}
