using Newtonsoft.Json;
using PLCProtocol.DataClass;
using BoxPrint.DataList;
using BoxPrint.GUI.ETC;
using BoxPrint.GUI.EventCollection;
using BoxPrint.GUI.UIControls;
using BoxPrint.GUI.ViewModels.BindingCommand;
using BoxPrint.Log;
using BoxPrint.Modules.RM;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.ViewModels
{
    public class CraneDataViewModel : ViewModelBase
    {
        private ControlBase SelectUnit = null;
        private PLCDataItem SelectItem = null;//SuHwan_20230207 : 피엘씨 쓰기 추가
        private bool RefreshCheckPass = false;//SuHwan_20230207 : 피엘씨 쓰기 추가
        private object SelectUnitLock = new object();
        private DateTime ViewInitTime = DateTime.Now;

        private eClientProcedureUnitType procedureUnitType;     //230207 추가 [ServerClient]

        private bool IsPlayBackControl = false;

        #region Binding Item
        #region Data, Checked Binding
        //RefreshChecked
        private bool _RefreshChecked;
        public bool RefreshChecked
        {
            get => _RefreshChecked;
            set => Set("RefreshChecked", ref _RefreshChecked, value);
        }
        //유닛 타입
        private string _SelectUnitType;
        public string SelectUnitType
        {
            get => _SelectUnitType;
            set => Set("SelectUnitType", ref _SelectUnitType, value);
        }
        //유닛 아이디
        private string _SelectUnitID;
        public string SelectUnitID
        {
            get => _SelectUnitID;
            set => Set("SelectUnitID", ref _SelectUnitID, value);
        }
        //지상반(SRC) 동작 모드
        private string _SRCMode;
        public string SRCMode
        {
            get => _SRCMode;
            set => Set("SRCMode", ref _SRCMode, value);
        }
        //팔렛 정보
        private string _PalletInfo;
        public string PalletInfo
        {
            get => _PalletInfo;
            set => Set("PalletInfo", ref _PalletInfo, value);
        }
        //팔렛 사이즈 정보
        private eCarrierSize _CarrierSize;
        public eCarrierSize CarrierSize
        {
            get => _CarrierSize;
            set => Set("CarrierSize", ref _CarrierSize, value);
        }
        //Fire State
        private bool _FireState;
        public bool FireState
        {
            get => _FireState;
            set => Set("FireState", ref _FireState, value);
        }
        //기상반(SCC) 동작 모드
        private string _SCCMode;
        public string SCCMode
        {
            get => _SCCMode;
            set => Set("SCCMode", ref _SCCMode, value);
        }
        //동작 상태
        private string _MoveState;
        public string MoveState
        {
            get => _MoveState;
            set => Set("MoveState", ref _MoveState, value);
        }
        //주행위치
        private decimal _TraversePosition;
        public decimal TraversePosition
        {
            get => _TraversePosition;
            set => Set("TraversePosition", ref _TraversePosition, value);
        }
        //승강위치
        private decimal _ZPosition;
        public decimal ZPosition
        {
            get => _ZPosition;
            set => Set("ZPosition", ref _ZPosition, value);
        }
        //포크 위치
        private decimal _ForkPosition;
        public decimal ForkPosition
        {
            get => _ForkPosition;
            set => Set("ForkPosition", ref _ForkPosition, value);
        }
        //Error Code
        private decimal _ErrorCode;
        public decimal ErrorCode
        {
            get => _ErrorCode;
            set => Set("ErrorCode", ref _ErrorCode, value);
        }
        //Warning Code
        private decimal _WarningCode;
        public decimal WarningCode
        {
            get => _WarningCode;
            set => Set("WarningCode", ref _WarningCode, value);
        }
        //화물
        //private string _Material;
        //public string Material
        //{
        //    get => _Material;
        //    set => Set("Material", ref _Material, value);
        //}
        //포크 작업 유무
        //private bool _ForkJobFlag;
        //public bool ForkJobFlag
        //{
        //    get => _ForkJobFlag;
        //    set => Set("ForkJobFlag", ref _ForkJobFlag, value);
        //}
        //기상반(SCC) Active
        private eCraneActiveState _SCCActive;
        public eCraneActiveState SCCActive
        {
            get => _SCCActive;
            set => Set("SCCActive", ref _SCCActive, value);
        }
        //작업 구분
        //private string _JobType;
        //public string JobType
        //{
        //    get => _JobType;
        //    set => Set("JobType", ref _JobType, value);
        //}
        #endregion

        #region Command Button
        //221230 HHJ SCS 개선
        public ICommand ButtonCommand { get; private set; }

        private eUnitCommandProperty _InstallState;
        public eUnitCommandProperty InstallState
        {
            get => _InstallState;
            set
            {
                InstallContent = TranslationManager.Instance.Translate(value.ToString()).ToString();
                Set("InstallState", ref _InstallState, value);
            }
        }
        private string _InstallContent;
        public string InstallContent
        {
            get => _InstallContent;
            set => Set("InstallContent", ref _InstallContent, value);
        }
        #endregion

        //SuHwan_20221226 : [1차 UI검수] 폰트 사이즈 설정
        protected int _UIFontSize_Large = 13;  //큰폰트
        public int UIFontSize_Large
        {
            get => _UIFontSize_Large;
            set => Set("UIFontSize_Large", ref _UIFontSize_Large, value);
        }

        protected int _UIFontSize_Medium = 11; //중간폰트
        public int UIFontSize_Medium
        {
            get => _UIFontSize_Medium;
            set => Set("UIFontSize_Medium", ref _UIFontSize_Medium, value);
        }

        protected int _UIFontSize_Small = 9;  //작은폰트
        public int UIFontSize_Small
        {
            get => _UIFontSize_Small;
            set => Set("UIFontSize_Small", ref _UIFontSize_Small, value);
        }

        protected Visibility _CommandButtonVisible = Visibility.Visible;
        public Visibility CommandButtonVisible
        {
            get => _CommandButtonVisible;
            set => Set("CommandButtonVisible", ref _CommandButtonVisible, value);
        }
        #endregion

        public CraneDataViewModel(bool IsPlayBack)
        {
            //미사용일때는 Update 방지를 위해 Checked 막아준다.
            RefreshChecked = false;

            IsPlayBackControl = IsPlayBack;
            CommandButtonVisible = IsPlayBackControl ? Visibility.Collapsed : Visibility.Visible;

            ButtonCommand = new BindingDelegateCommand<eUnitCommandProperty>(ButtonCommandAction);      //221230 HHJ SCS 개선
            UIEventCollection.Instance.OnCultureChanged += _UIEventCollection_OnCultureChanged;
        }

        private void _UIEventCollection_OnCultureChanged(string cultureKey)
        {
            InstallContent = TranslationManager.Instance.Translate(InstallState.ToString()).ToString();
            SelectUnitType = TranslationManager.Instance.Translate(procedureUnitType.ToString()).ToString().ToUpper();
        }

        public void AbleViewModel(ControlBase selectunit)
        {
            //230207 변경 s
            //SelectUnitType = "CRANE";
            procedureUnitType = eClientProcedureUnitType.Crane;
            SelectUnitType = TranslationManager.Instance.Translate(procedureUnitType.ToString()).ToString().ToUpper();
            //230207 변경 e
            
            RefreshChecked = true;

            if (!IsPlayBackControl)
            {
                SelectUnit = selectunit as RMModuleBase;
                if (SelectUnit is RMModuleBase rm)
                {
                    //240731 RGJ 크레인 캐리어 인스톨 조건 변경.
                    //1.실화물 있지만 화물 데이터 없는경우 인스톨.
                    //2.실화물 없지만 화물 데이터 있는경우 삭제.
                    if (rm.CarrierExistSensor)
                        InstallState = eUnitCommandProperty.Install;
                    else
                        InstallState = eUnitCommandProperty.Delete;
                }
            }
        }

        public void DisableViewmodel()
        {
            //미사용일때는 Update 방지를 위해 Checked 막아준다.
            RefreshChecked = false;
        }

        protected override void ViewModelTimer()
        {
            while (true)
            {
                Thread.Sleep(500);

                if (CloseThread) return;

                if (!RefreshChecked)
                {
                    if (RefreshCheckPass)//패스로 한번만 지나가서 데이타 갱신
                        RefreshCheckPass = false;
                    else
                        continue;
                }

                try
                {
                    //230116 RGJ 해당 로그 삭제
                    //221017 조숭진 1시간씩 로그찍기
                    //if (GlobalData.Current.ServerClientType != eServerClientType.Client && IsTimeOut(DateTime.Now))
                    //{
                    //    GlobalData.Current.DBManager.DbGetProcedureShelfInfo(string.Format("{0:D2}", (int)eShelfBank.Front), true);
                    //    GlobalData.Current.DBManager.DbGetProcedureShelfInfo(string.Format("{0:D2}", (int)eShelfBank.Rear), true);
                    //    GlobalData.Current.DBManager.DbGetProcedureCarrierInfo(true);
                    //    GlobalData.Current.DBManager.DbGetProcedureJobInfo(true);
                    //}

                    lock (SelectUnitLock)
                    {
                        if (SelectUnit is null)
                            continue;
                        if (!IsPlayBackControl)
                        {
                            if (SelectUnit is RMModuleBase rm)
                            {
                                SelectUnitID = rm.ControlName;
                                SRCMode = rm.PLC_OnlineMode.ToString();
                                //241027 HoN Sensor <-> InslotData MisMatch 관련 우측 정보창에 PLC 정보가 아닌 Inslot 정보 기입 요청 (조범석 매니저 요청)
                                //PalletInfo = rm.PLC_CarrierID_FORK1;
                                PalletInfo = rm.CarrierID;
                                CarrierSize = rm.CarrierSize;
                                FireState = rm.PC_FireOccurred;     //241030 HoN 화재 관련 추가 수정        //false -> PC에서 Crane으로 주는 Occur Bit로 변경
                                SCCMode = rm.GetRMMode().ToString();
                                MoveState = rm.CraneState.ToString();
                                TraversePosition = rm.XAxisPosition;
                                ZPosition = rm.ZAxisPosition;
                                ForkPosition = rm.ForkAxisPosition;
                                ErrorCode = 0;
                                WarningCode = 0;
                                //Material = string.Empty;
                                //ForkJobFlag = false;
                                SCCActive = rm.PLC_ActiveState;
                                //JobType = string.Empty;
                            }
                        }
                    }
                }
                catch (Exception)
                {

                }
            }
        }

        //221230 HHJ SCS 개선
        private void ButtonCommandAction(eUnitCommandProperty cmdProperty)
        {
            string buttonname = string.Empty;
            bool bcommand = true;

            try
            {

                //각 모듈 베이스로 변환. 변환값 없으면 진행불가.
                RMModuleBase rm = SelectUnit as RMModuleBase;
                if (rm is null)
                    return;

                //제어 Code는 기존 LayOutView에 있는 Code를 그대로 사용한다.
                MessageBoxPopupView msgbox = null;
                string msg = string.Empty;
                CustomMessageBoxResult mBoxResult = null;

                switch (cmdProperty)
                {
                    //Crene용
                    case eUnitCommandProperty.EmergencyStop:
                        #region
                        buttonname = cmdProperty.ToString();

                        LogManager.WriteOperatorLog(string.Format("사용자가 Crane {0}의 수동지시 {1} 을/를 Click하였습니다.", rm.ControlName, buttonname),
                            "CLICK", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 13,
                            rm.ControlName, buttonname);

                        //Crane 상태가 Error가 아닌경우만 진행함
                        if (rm.CraneState.Equals(eCraneUIState.ERROR))
                        {
                            msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}] " +
                                                TranslationManager.Instance.Translate("State is Error").ToString(),
                                                rm.ControlName);
                            //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                            MessageBoxPopupView.Show(msg, "크레인 데이터 변경완료 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                                rm.ControlName, "State is Error", "Fail", true);
                            return;
                        }
                        msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}]\nE-Stop?", rm.ControlName);
                        //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                        msgbox = new MessageBoxPopupView(msg, "크레인 데이터 변경 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                            rm.ControlName, "EmergencyStop", "", true);

                        mBoxResult = msgbox.ShowResult();

                        if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                        {
                            //230207 추가 s [ServerClient]
                            if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                            {
                                ClientSetProcedure(cmdProperty);
                            }
                            else
                            {
                                rm.RMEMG_STOP_Request();

                                //msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}]\nE-Stop " +
                                //                    TranslationManager.Instance.Translate("Occur").ToString(),
                                //                    rm.ControlName);
                                //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}]\nE-Stop " +
                                                TranslationManager.Instance.Translate("Occur").ToString(),
                                                rm.ControlName);

                            MessageBoxPopupView.Show(msg, "크레인 데이터 변경완료 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                                rm.ControlName, "EmergencyStop", "Occur", true);
                            //230207 추가 e [ServerClient]
                        }
                        #endregion
                        break;
                    case eUnitCommandProperty.Active:
                        #region
                        buttonname = cmdProperty.ToString();

                        LogManager.WriteOperatorLog(string.Format("사용자가 Crane {0}의 수동지시 {1} 을/를 Click하였습니다.", rm.ControlName, buttonname),
                            "CLICK", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 13,
                            rm.ControlName, buttonname);

                        #region PLC Resume Command

                        msg = string.Format(TranslationManager.Instance.Translate("SRC Resume").ToString() + " " +  "?", rm.ControlName);

                        msgbox = new MessageBoxPopupView(msg, "크레인 데이터 변경 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                            rm.ControlName, "Start", "", true);

                        mBoxResult = msgbox.ShowResult();

                        if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                        {
                            if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                            {
                                ClientSetProcedure(cmdProperty);
                            }
                            else
                            {
                                GlobalData.Current.MainBooth.ResumePLCAction();
                            }
                            msg = string.Format(TranslationManager.Instance.Translate("SRC").ToString() + " " +
                                                TranslationManager.Instance.Translate("Resume").ToString() + " " +
                                                TranslationManager.Instance.Translate("Occur").ToString(),
                                                rm.ControlName);

                            MessageBoxPopupView.Show(msg, "크레인 데이터 변경완료 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                                rm.ControlName, "Start", "Occur", true);
                        }
                        #endregion

                        #region 기존 로직 주석 처리
                        ////Crane 상태가 Error가 아닌경우만 진행함
                        //if (rm.CraneState.Equals(eCraneUIState.ERROR))
                        //{
                        //    msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}] " +
                        //                        TranslationManager.Instance.Translate("State is Error").ToString(),
                        //                        rm.ControlName);
                        //    //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                        //    MessageBoxPopupView.Show(msg, "크레인 데이터 변경완료 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                        //        rm.ControlName, "State is Error", "Fail", true);
                        //    return;
                        //}

                        //msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}]\n" +
                        //                    TranslationManager.Instance.Translate("Start").ToString() + "?",
                        //                    rm.ControlName);
                        ////msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                        //msgbox = new MessageBoxPopupView(msg, "크레인 데이터 변경 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                        //    rm.ControlName, "Start", "", true);

                        //mBoxResult = msgbox.ShowResult();

                        //if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                        //{
                        //    if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                        //    {
                        //        ClientSetProcedure(cmdProperty);
                        //    }
                        //    else
                        //    {
                        //        rm.RMResume_Request();

                        //        //msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}]\n" +
                        //        //                    TranslationManager.Instance.Translate("Start").ToString() + " " +
                        //        //                    TranslationManager.Instance.Translate("Occur").ToString(),
                        //        //                    rm.ControlName);
                        //        //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                        //    }
                        //    msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}]\n" +
                        //                        TranslationManager.Instance.Translate("Start").ToString() + " " +
                        //                        TranslationManager.Instance.Translate("Occur").ToString(),
                        //                        rm.ControlName);

                        //    MessageBoxPopupView.Show(msg, "크레인 데이터 변경완료 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                        //        rm.ControlName, "Start", "Occur", true);
                        //}
                        #endregion

                        #endregion
                        break;
                    case eUnitCommandProperty.Stop:
                        #region
                        buttonname = cmdProperty.ToString();

                        LogManager.WriteOperatorLog(string.Format("사용자가 Crane {0}의 수동지시 {1} 을/를 Click하였습니다.", rm.ControlName, buttonname),
                            "CLICK", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 13,
                            rm.ControlName, buttonname);

                        #region PLC Stop Commanmd

                        msg = string.Format(TranslationManager.Instance.Translate("SRC Pause").ToString() + " " + "?");
                        //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                        msgbox = new MessageBoxPopupView(msg, "크레인 데이터 변경 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                            rm.ControlName, "Stop", "", true);

                        mBoxResult = msgbox.ShowResult();

                        if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                        {
                            //230207 추가 s [ServerClient]
                            if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                            {
                                ClientSetProcedure(cmdProperty);
                            }
                            else
                            {
                                GlobalData.Current.MainBooth.PausePLCAction();
                            }

                            msg = string.Format(TranslationManager.Instance.Translate("SRC").ToString() + " " +
                                                TranslationManager.Instance.Translate("Pause").ToString() + " " +
                                                TranslationManager.Instance.Translate("Occur").ToString(),
                                                rm.ControlName);
                            MessageBoxPopupView.Show(msg, "크레인 데이터 변경완료 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                                rm.ControlName, "Stop", "Occur", true);
                        }
                        #endregion

                        #region 기존 로직 주석 처리
                        ////Crane 상태가 Error가 아닌경우만 진행함
                        //if (rm.CraneState.Equals(eCraneUIState.ERROR))
                        //{
                        //    msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}] " +
                        //                        TranslationManager.Instance.Translate("State is Error").ToString(),
                        //                        rm.ControlName);
                        //    //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                        //    MessageBoxPopupView.Show(msg, "크레인 데이터 변경완료 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                        //        rm.ControlName, "State is Error", "Fail", true);
                        //    return;
                        //}

                        //msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}]\n" +
                        //                    TranslationManager.Instance.Translate("Stop").ToString() + "?",
                        //                    rm.ControlName);
                        ////msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                        //msgbox = new MessageBoxPopupView(msg, "크레인 데이터 변경 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                        //    rm.ControlName, "Stop", "", true);

                        //mBoxResult = msgbox.ShowResult();

                        //if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                        //{
                        //    //230207 추가 s [ServerClient]
                        //    if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                        //    {
                        //        ClientSetProcedure(cmdProperty);
                        //    }
                        //    else
                        //    {
                        //        rm.RMPause_Request();

                        //        //msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}]\n" +
                        //        //                    TranslationManager.Instance.Translate("Stop").ToString() + " " + 
                        //        //                    TranslationManager.Instance.Translate("Occur").ToString(),
                        //        //                    rm.ControlName);
                        //        //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                        //    }

                        //    msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}]\n" +
                        //                        TranslationManager.Instance.Translate("Stop").ToString() + " " +
                        //                        TranslationManager.Instance.Translate("Occur").ToString(),
                        //                        rm.ControlName);
                        //    MessageBoxPopupView.Show(msg, "크레인 데이터 변경완료 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                        //        rm.ControlName, "Stop", "Occur", true);
                        //}
                        #endregion

                        #endregion
                        break;
                    case eUnitCommandProperty.ErrorReset:
                        #region
                        buttonname = cmdProperty.ToString();

                        LogManager.WriteOperatorLog(string.Format("사용자가 Crane {0}의 수동지시 {1} 을/를 Click하였습니다.", rm.ControlName, buttonname),
                            "CLICK", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 13,
                            rm.ControlName, buttonname);

                        //Crane 상태가 Error가 아닌경우만 진행함
                        if (!rm.CraneState.Equals(eCraneUIState.ERROR))
                        {
                            msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}] " +
                                                TranslationManager.Instance.Translate("State is Not Error").ToString(),
                                                rm.ControlName);
                            //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                            MessageBoxPopupView.Show(msg, "크레인 데이터 변경완료 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                                rm.ControlName, "State is Error", "Fail", true);
                            return;
                        }

                        msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}]\n" + 
                                            TranslationManager.Instance.Translate("Error Reset").ToString() + "?",
                                            rm.ControlName);
                        //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                        msgbox = new MessageBoxPopupView(msg, "크레인 데이터 변경 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                            rm.ControlName, "Error Reset", "", true);

                        mBoxResult = msgbox.ShowResult();

                        if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                        {
                            //230207 추가 s [ServerClient]
                            if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                            {
                                ClientSetProcedure(cmdProperty);
                            }
                            else
                            {
                                rm.RMReset_Request();

                                //msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}]\n" +
                                //                    TranslationManager.Instance.Translate("Error Reset").ToString() +
                                //                    TranslationManager.Instance.Translate("Complete").ToString(),
                                //                    rm.ControlName);
                                //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                            }

                            msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}]\n" +
                                                TranslationManager.Instance.Translate("Error Reset").ToString() +
                                                TranslationManager.Instance.Translate("Complete").ToString(),
                                                rm.ControlName);

                            MessageBoxPopupView.Show(msg, "크레인 데이터 변경완료 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                                rm.ControlName, "Error Reset", "Complete", true);
                            //230207 추가 e [ServerClient]
                        }
                        #endregion
                        break;
                    case eUnitCommandProperty.Home:
                        #region
                        buttonname = cmdProperty.ToString();

                        LogManager.WriteOperatorLog(string.Format("사용자가 Crane {0}의 수동지시 {1} 을/를 Click하였습니다.", rm.ControlName, buttonname),
                            "CLICK", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 13,
                            rm.ControlName, buttonname);

                        if (rm.CraneState.Equals(eCraneUIState.ERROR))
                        {
                            msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "{0}" +
                                                TranslationManager.Instance.Translate("State is Error").ToString(),
                                                rm.ControlName);
                            //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                            MessageBoxPopupView.Show(msg, "크레인 데이터 변경완료 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                                rm.ControlName, "State is Error", "Fail", true);
                            return;
                        }

                        msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}]\n" +
                                            TranslationManager.Instance.Translate("Move Home").ToString() + "?",
                                            rm.ControlName);
                        //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                        msgbox = new MessageBoxPopupView(msg, "크레인 데이터 변경 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                            rm.ControlName, "Move Home", "", true);

                        mBoxResult = msgbox.ShowResult();

                        if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                        {
                            //230207 추가 s [ServerClient]
                            if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                            {
                                ClientSetProcedure(cmdProperty);
                            }
                            else
                            {
                                rm.RMHome_Request();

                                //msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}]\n" +
                                //                    TranslationManager.Instance.Translate("Move Home").ToString() + " " +
                                //                    TranslationManager.Instance.Translate("Complete").ToString(),
                                //                    rm.ControlName);
                                //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                            msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}]\n" +
                                                TranslationManager.Instance.Translate("Move Home").ToString() + " " +
                                                TranslationManager.Instance.Translate("Complete").ToString(),
                                                rm.ControlName);
                            MessageBoxPopupView.Show(msg, "크레인 데이터 변경완료 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                                rm.ControlName, "Move Home", "Complete", true);
                            //230207 추가 e [ServerClient]
                        }
                        #endregion
                        break;
                    case eUnitCommandProperty.ManualJob:
                        #region
                        buttonname = "Manual Command";

                        LogManager.WriteOperatorLog(string.Format("사용자가 Crane {0}의 수동지시 {1} 을/를 Click하였습니다.", rm.ControlName, buttonname),
                            "CLICK", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 13,
                            rm.ControlName, buttonname);
                        //230321 HHJ SCS 개선     //- CraneOrder Window 추가
                        //GlobalData.Current.ViewModelWindowOpenRequest(eOpenWindowName.eCraneManualJob, rm, IsPlayBackControl);
                        GlobalData.Current.ViewModelWindowOpenRequest(eOpenWindowName.eCraneOrder, rm, IsPlayBackControl);
                        #endregion
                        break;
                    case eUnitCommandProperty.Smoke:
                        #region 미구현
                        #endregion
                        break;
                    case eUnitCommandProperty.Fire1:
                        #region 미구현
                        #endregion
                        break;
                    case eUnitCommandProperty.Fire2:
                        #region 미구현
                        #endregion
                        break;
                    case eUnitCommandProperty.FireSignal:
                        #region 미구현
                        #endregion
                        break;
                    //공용
                    case eUnitCommandProperty.Install:
                        #region
                        buttonname = cmdProperty.ToString();

                        LogManager.WriteOperatorLog(string.Format("사용자가 Crane {0}의 수동지시 {1} 을/를 Click하였습니다.", rm.ControlName, buttonname),
                            "CLICK", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 13,
                            rm.ControlName, buttonname);

                        if (rm.CheckCarrierExist())
                        {
                            if(rm.InSlotCarrier != null) //240731 RGJ 크레인 화물 인스톨 체크사항 -> 이미 화물 데이터 있는데 또 인스톨하면 안됨.
                            {
                                msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}]\n" +
                                                    TranslationManager.Instance.Translate("Carrier Exist").ToString(),
                                                    rm.ControlName);
                                MessageBoxPopupView.Show(msg, "크레인 캐리어ID추가 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Error,
                                    rm.ControlName, "Carrier Exist", "Fail", true);
                                return;
                            }
                            //데이터 입력 팝업은 그냥 나오고 데이터 입력 팝업에서 확인을 눌리면 확인해주는 팝업을 띄워준다.
                            CarrierInstall ci = new CarrierInstall(rm.ControlName);

                            CarrierItem carrier = ci.ResultCarrierItem();

                            if (carrier != null)
                            {
                                msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}]\n" +
                                                    TranslationManager.Instance.Translate("CarrierID").ToString() + "[{1}]\n{2}?",
                                                    rm.ControlName, carrier.CarrierID, TranslationManager.Instance.Translate(cmdProperty.ToString()).ToString());
                                //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                                msgbox = new MessageBoxPopupView(msg, "크레인 캐리어ID 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                                    rm.ControlName, carrier.CarrierID, cmdProperty.ToString(), true);

                                mBoxResult = msgbox.ShowResult();

                                if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                                {
                                    //230207 추가 s [ServerClient]
                                    if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                                    {
                                        ClientSetProcedure(cmdProperty, carrier);
                                    }
                                    else
                                    {
                                        carrier.CarrierState = eCarrierState.COMPLETED;
                                        rm.InsertCarrier(carrier);

                                        //msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}]\n" +
                                        //                    TranslationManager.Instance.Translate("CarrierID").ToString() + "[{1}]\n{2}" +
                                        //                    TranslationManager.Instance.Translate("Complete").ToString(),
                                        //                    rm.ControlName, carrier.CarrierID, TranslationManager.Instance.Translate(cmdProperty.ToString()).ToString());
                                        //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);

                                        InstallState = eUnitCommandProperty.Delete;
                                    }
                                    msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}]\n" +
                                                        TranslationManager.Instance.Translate("CarrierID").ToString() + "[{1}]\n{2}" +
                                                        TranslationManager.Instance.Translate("Complete").ToString(),
                                                        rm.ControlName, carrier.CarrierID, TranslationManager.Instance.Translate(cmdProperty.ToString()).ToString());
                                    //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                                    MessageBoxPopupView.Show(msg, "크레인 캐리어ID추가 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                                        rm.ControlName, carrier.CarrierID, "Complete", true);
                                    //230207 추가 e [ServerClient]
                                }
                            }
                        }
                        else
                        {
                            //혹시나 캐리어가 없으면 삭제 모드로 변경해준다.
                            InstallState = eUnitCommandProperty.Delete;
                            msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}]\n" +
                                                TranslationManager.Instance.Translate("Carrier Exist").ToString() + "\n" +
                                                TranslationManager.Instance.Translate("Change").ToString() + " {1} " +
                                                TranslationManager.Instance.Translate("Mode").ToString(),
                                                rm.ControlName, TranslationManager.Instance.Translate(InstallState.ToString()).ToString());
                            //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                            MessageBoxPopupView.Show(msg, "크레인 캐리어ID추가 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Error,
                                rm.ControlName, "Carrier Exist", "Fail", true);

                        }
                        #endregion
                        break;
                    case eUnitCommandProperty.Delete:
                        #region
                        buttonname = cmdProperty.ToString();

                        LogManager.WriteOperatorLog(string.Format("사용자가 Crane {0}의 수동지시 {1} 을/를 Click하였습니다.", rm.ControlName, buttonname),
                            "DELETE", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 13,
                            rm.ControlName, buttonname);

                        if (!rm.CheckCarrierExist())
                        {
                            string carrierid = rm.GetCarrierID();
                            //데이터 입력 팝업은 그냥 나오고 데이터 입력 팝업에서 확인을 눌리면 확인해주는 팝업을 띄워준다.
                            msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}]\n" +
                                                TranslationManager.Instance.Translate("CarrierID").ToString() + "[{1}]\n{2}?",
                                                rm.ControlName, carrierid, TranslationManager.Instance.Translate(cmdProperty.ToString()).ToString());
                            //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                            msgbox = new MessageBoxPopupView(msg, "크레인 캐리어ID 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                                rm.ControlName, carrierid, cmdProperty.ToString(), true);

                            mBoxResult = msgbox.ShowResult();

                            if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                            {
                                //230207 추가 s [ServerClient]
                                if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                                {
                                    ClientSetProcedure(cmdProperty);
                                }
                                else
                                {
                                    rm.ResetCarrierData();

                                    CarrierStorage.Instance.RemoveStorageCarrier(carrierid); //STK Domain 에서 캐리어 제거.

                                    //msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}]\n" +
                                    //                    TranslationManager.Instance.Translate("CarrierID").ToString() + "[{1}]\n{2} " +
                                    //                    TranslationManager.Instance.Translate("Complete").ToString(),
                                    //                    rm.ControlName, carrierid, TranslationManager.Instance.Translate(cmdProperty.ToString()).ToString());
                                    //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);

                                    InstallState = eUnitCommandProperty.Install;
                                }
                                msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}]\n" +
                                                    TranslationManager.Instance.Translate("CarrierID").ToString() + "[{1}]\n{2} " +
                                                    TranslationManager.Instance.Translate("Complete").ToString(),
                                                    rm.ControlName, carrierid, TranslationManager.Instance.Translate(cmdProperty.ToString()).ToString());
                                MessageBoxPopupView.Show(msg, "크레인 캐리어ID삭제 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                                    rm.ControlName, carrierid, "Complete", true);
                                //230207 추가 e [ServerClient]
                            }
                        }
                        else
                        {
                            //혹시나 캐리어가 없으면 인스톨 모드로 변경해준다.
                            InstallState = eUnitCommandProperty.Install;
                            msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}]\n" +
                                                TranslationManager.Instance.Translate("Carrier Not Exist").ToString() + "\n" +
                                                TranslationManager.Instance.Translate("Change").ToString() + " {1} " +
                                                TranslationManager.Instance.Translate("Mode").ToString(),
                                                rm.ControlName, TranslationManager.Instance.Translate(InstallState.ToString()).ToString());
                            //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                            MessageBoxPopupView.Show(msg, "크레인 캐리어ID삭제 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Error,
                                rm.ControlName, "Carrier Not Exist", "Fail", true);
                        }
                        #endregion
                        break;
                    case eUnitCommandProperty.Detail:
                        buttonname = cmdProperty.ToString();

                        LogManager.WriteOperatorLog(string.Format("사용자가 Crane {0}의 수동지시 {1} 을/를 Click하였습니다.", rm.ControlName, buttonname),
                            "CLICK", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 13,
                            rm.ControlName, buttonname);
                        bcommand = false;
                        #region
                        GlobalData.Current.ViewModelWindowOpenRequest(eOpenWindowName.eUnitDetail, rm, IsPlayBackControl);
                        #endregion
                        break;
                    case eUnitCommandProperty.Inform:
                        #region 미구현
                        #endregion
                        break;
                }
                if (bcommand && mBoxResult != null &&
                    mBoxResult.Result.Equals(MessageBoxResult.OK))
                {
                    LogManager.WriteOperatorLog(string.Format("사용자가 Crane {0}의 수동지시 {1} 을/를 Write하였습니다.", rm.ControlName, buttonname),
                        "WRITE", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 21,
                        rm.ControlName, buttonname);
                }
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        //230207 추가 s [ServerClient]
        private void ClientSetProcedure(eUnitCommandProperty cmdProperty, CarrierItem carrier = null)
        {
            if (GlobalData.Current.ServerClientType != eServerClientType.Client)
                return;

            //PlayBackControl은 진행되면 안됨.
            if (IsPlayBackControl)
                return;

            //각 모듈 베이스로 변환. 변환값 없으면 진행불가.
            RMModuleBase rm = SelectUnit as RMModuleBase;
            if (rm is null)
                return;

            ClientReqList reqBuffer = new ClientReqList
            {
                EQPID = GlobalData.Current.EQPID,
                CMDType = cmdProperty.ToString(),
                Target = procedureUnitType.ToString(),
                TargetID = rm.ControlName,
                TargetValue = string.Empty,
                ReqTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Requester = eServerClientType.Client,
            };

            if (cmdProperty.Equals(eUnitCommandProperty.Install))
            {
                reqBuffer.TargetValue = JsonConvert.SerializeObject(carrier);
            }

            GlobalData.Current.DBManager.DbSetProcedureClientReq(reqBuffer.EQPID, reqBuffer.CMDType, reqBuffer.Target, reqBuffer.TargetID, reqBuffer.TargetValue, reqBuffer.ReqTime, reqBuffer.Requester);
        }
        //230301 클라이언트에서 io변경 요청대응 //SuHwan_20230404 : 데이타변경 팝업추가
        private void ClientSetProcedure_IO(string ModuleName, PLCDataItem pItem, string DataValue)
        {
            ClientReqList reqBuffer = new ClientReqList
            {
                EQPID = GlobalData.Current.EQPID,
                CMDType = DataValue,
                Target = "IO",
                TargetID = ModuleName,
                TargetValue = JsonConvert.SerializeObject(pItem),
                ReqTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Requester = eServerClientType.Client,
                JobID = "CarrierDataChg",
            };

            GlobalData.Current.DBManager.DbSetProcedureClientReq(reqBuffer.EQPID, reqBuffer.CMDType, reqBuffer.Target, reqBuffer.TargetID,
                reqBuffer.TargetValue, reqBuffer.ReqTime, reqBuffer.Requester, false, reqBuffer.JobID);
        }

        //SuHwan_20230404 : 데이타변경 팝업추가
        /// <summary>
        /// 테그로 검색하여 PLCDataItem 받아오기
        /// </summary>
        /// <param name="rcvTag">ex : PCtoPLC_0_0 </param>
        /// <returns></returns>
        public PLCDataItem GetPLCDataItem(string rcvTag)
        {
            try
            {
                //PlayBackControl은 진행되면 안됨.
                if (IsPlayBackControl)
                    return null;

                //각 모듈 베이스로 변환. 변환값 없으면 진행불가.
                RMModuleBase rm = SelectUnit as RMModuleBase;
                if (rm is null)
                    return null;

                SelectItem = null;

                if (!string.IsNullOrEmpty(rcvTag))
                {
                    string[] bufferSplitr = rcvTag.Split('_');
                    string bufferArea = string.IsNullOrEmpty(bufferSplitr[0]) ? string.Empty : bufferSplitr[0];
                    int bufferAddressOffset = string.IsNullOrEmpty(bufferSplitr[1]) ? 0 : Convert.ToInt32(bufferSplitr[1]);
                    int bufferBitOffset = string.IsNullOrEmpty(bufferSplitr[2]) ? 0 : Convert.ToInt32(bufferSplitr[2]);

                    if (bufferArea == "PCtoPLC")
                    {
                        foreach (var item in rm.PCtoPLC.Values)
                        {
                            if (item.AddressOffset == bufferAddressOffset && item.BitOffset == bufferBitOffset && item.ItemName != "PC_Area_BatchRead")
                            {
                                SelectItem = item;
                                break;
                            }
                        }
                    }
                    else if (bufferArea == "PLCtoPC")
                    {
                        foreach (var item in rm.PLCtoPC.Values)
                        {
                            if (item.AddressOffset == bufferAddressOffset && item.BitOffset == bufferBitOffset && item.ItemName != "PLC_Area_BatchRead")
                            {
                                SelectItem = item;
                                break;
                            }
                        }
                    }
                }

                return SelectItem;
            }
            catch (Exception ex)
            {
                _ = ex;
                return null;
            }
        }

        //SuHwan_20230404 : 데이타변경 팝업추가
        /// <summary>
        /// 데이타 쓰기
        /// </summary>
        /// <param name="rcvValue"></param>
        public void SetIOData(string rcvValue, string rcvDetailValue = null)
        {
            string msg = string.Empty;
            MessageBoxPopupView msgbox = null;
            CustomMessageBoxResult mBoxResult = null;

            string msgItemName = string.Empty;

            try
            {
                //////////////////////
                //PlayBackControl은 진행되면 안됨.
                if (IsPlayBackControl)
                    return;

                //각 모듈 베이스로 변환. 변환값 없으면 진행불가.
                RMModuleBase rm = SelectUnit as RMModuleBase;
                if (rm is null)
                    return;

                if (SelectItem == null)
                    return;

                msgItemName = (string.IsNullOrEmpty(rcvDetailValue)) ? rcvValue : rcvDetailValue;

                msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}] {1}\n [{2}]\n" +
                                    TranslationManager.Instance.Translate("Change").ToString() + "?",
                                    rm.ControlName, SelectItem.ItemName, msgItemName);
                //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                msgbox = new MessageBoxPopupView(msg, "크레인 IO 변경 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                    rm.ControlName, SelectItem.ItemName, msgItemName, true);
                mBoxResult = msgbox.ShowResult();

                if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                {
                    //230301 클라이언트에서 io변경 요청대응
                    if (GlobalData.Current.ServerClientType == eServerClientType.Server)
                    {
                        if (SelectItem.Area == eAreaType.PCtoPLC)
                            GlobalData.Current.protocolManager.Write(rm.ModuleName, rm.PCtoPLC, SelectItem.ItemName, rcvValue);
                        else if (SelectItem.Area == eAreaType.PLCtoPC)
                            GlobalData.Current.protocolManager.Write(rm.ModuleName, rm.PLCtoPC, SelectItem.ItemName, rcvValue);

                        if (SelectItem.ItemName == "PLC_PalletSize")
                        {
                            Enum.TryParse(rcvValue, out ePalletSize palletsize);
                            var rmCarrier = rm.InSlotCarrier;
                            if (rmCarrier != null) //20230914 RGJ IO Set Null 체크 추가.
                            {
                                rm.InSlotCarrier.PalletSize = palletsize;
                                rm.UpdateCarrier(rm.CarrierID);
                            }
                        }
                        //캐리어 아이디...
                        else
                        {
                            var carrierItem = CarrierStorage.Instance.GetInModuleCarrierItem(rm.ModuleName);
                            if(carrierItem != null)
                            {
                                string beforeCarrierID = carrierItem.CarrierID;
                                eCarrierState beforestate = carrierItem.CarrierState;
                                carrierItem.CarrierID = rcvValue;

                                bool insertcheck = CarrierStorage.Instance.InsertCarrier(carrierItem);
                                if (insertcheck)
                                {
                                    rm.UpdateCarrier(rcvValue);
                                    CarrierStorage.Instance.RemoveStorageCarrier(beforeCarrierID);
                                    carrierItem.CarrierState = beforestate;
                                }
                            }

                        }
                    }
                    else
                    {

                        ClientSetProcedure_IO(rm.ModuleName, SelectItem, rcvValue);
                    }

                    msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString() + "[{0}] {1}\n [{2}]\n" +
                                        TranslationManager.Instance.Translate("Change").ToString() + " " +
                                        TranslationManager.Instance.Translate("Complete").ToString(),
                                        rm.ControlName, SelectItem.ItemName, msgItemName);
                    //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                    MessageBoxPopupView.Show(msg, "크레인 IO 변경완료 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                        rm.ControlName, SelectItem.ItemName, msgItemName, true);

                    RefreshCheckPass = true;

                    LogManager.WriteOperatorLog(string.Format("사용자가 {0} {1}에 {2} 을/를 Write하였습니다.", SelectUnit.ControlName, SelectItem.ItemName, msgItemName),
                        "WRITE", SelectItem.ItemName, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 10,
                        SelectUnit.ControlName, SelectItem.ItemName, msgItemName);
                }
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }
        public void CloseView()
        {
            CloseThread = true;
            viewModelthread.Join();
        }
    }
}
