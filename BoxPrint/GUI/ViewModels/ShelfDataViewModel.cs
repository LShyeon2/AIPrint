using Newtonsoft.Json;
using BoxPrint.DataList;
using BoxPrint.GUI.ETC;
using BoxPrint.GUI.EventCollection;
using BoxPrint.GUI.UIControls;
using BoxPrint.GUI.ViewModels.BindingCommand;
using BoxPrint.Modules.Shelf;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using TranslationByMarkupExtension;
using BoxPrint.Log;

namespace BoxPrint.GUI.ViewModels
{
    public class ShelfDataViewModel : ViewModelBase
    {
        private ControlBase SelectUnit = null;
        private object SelectUnitLock = new object();
        private DateTime ViewInitTime = DateTime.Now;
        private bool RefreshCheckPass = false;//SuHwan_20230207 : 피엘씨 쓰기 추가
        private eClientProcedureUnitType procedureUnitType;
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
        private string _UnitType;
        public string UnitType
        {
            get => _UnitType;
            set => Set("UnitType", ref _UnitType, value);
        }
        //Shelf Tag
        private string _UnitID;
        public string UnitID
        {
            get => _UnitID;
            set => Set("UnitID", ref _UnitID, value);
        }
        //Shelf Bank, Bay, Level
        private int _UnitBank;
        public int UnitBank
        {
            get => _UnitBank;
            set => Set("UnitBank", ref _UnitBank, value);
        }
        private int _UnitBay;
        public int UnitBay
        {
            get => _UnitBay;
            set => Set("UnitBay", ref _UnitBay, value);
        }
        private int _UnitLevel;
        public int UnitLevel
        {
            get => _UnitLevel;
            set => Set("UnitLevel", ref _UnitLevel, value);
        }

        //팔렛 정보
        private string _PalletInfo;
        public string PalletInfo
        {
            get => _PalletInfo;
            set => Set("PalletInfo", ref _PalletInfo, value);
        }


        //ProductEmpty 정보
        private eProductEmpty _ProductEmpty;
        public eProductEmpty ProductEmpty
        {
            get => _ProductEmpty;
            set => Set("ProductEmpty", ref _ProductEmpty, value);
        }

        //팔렛 사이즈 정보
        private eCarrierSize _CarrierSize;
        public eCarrierSize CarrierSize
        {
            get => _CarrierSize;
            set => Set("CarrierSize", ref _CarrierSize, value);
        }
        
        private ePalletSize _PalletSize;
        public ePalletSize PalletSize
        {
            get => _PalletSize;
            set => Set("PalletSize", ref _PalletSize, value);
        }

        //Shelf Type
        private eShelfType _ShelfType;
        public eShelfType ShelfType
        {
            get => _ShelfType;
            set => Set("ShelfType", ref _ShelfType, value);
        }

        //Shelf State
        private eShelfState _ShelfState;
        public eShelfState ShelfState
        {
            get => _ShelfState;
            set => Set("ShelfState", ref _ShelfState, value);
        }

        //Shelf Status
        private eShelfStatus _ShelfStatus;
        public eShelfStatus ShelfStatus
        {
            get => _ShelfStatus;
            set => Set("ShelfStatus", ref _ShelfStatus, value);
        }

        //Zone Name
        private string _ZoneName;
        public string ZoneName
        {
            get => _ZoneName;
            set => Set("ZoneName", ref _ZoneName, value);
        }

        //Shelf 우선순위
        private int _ShelfPriority;
        public int ShelfPriority
        {
            get => _ShelfPriority;
            set => Set("ShelfPriority", ref _ShelfPriority, value);
        }
        #endregion

        #region Command Button
        //221230 HHJ SCS 개선
        public ICommand ButtonCommand { get; private set; }

        private eUnitCommandProperty _EnableState;
        public eUnitCommandProperty EnableState
        {
            get => _EnableState;
            set
            {
                EnableContent = TranslationManager.Instance.Translate(value.ToString()).ToString();
                Set("EnableState", ref _EnableState, value);
            }
        }
        private string _EnableContent;
        public string EnableContent
        {
            get => _EnableContent;
            set => Set("EnableContent", ref _EnableContent, value);
        }

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

        private eUnitCommandProperty _ShelfTypeState;
        public eUnitCommandProperty ShelfTypeState
        {
            get => _ShelfTypeState;
            set
            {
                ShelfTypeContent = TranslationManager.Instance.Translate(value.ToString()).ToString();
                Set("ShelfTypeState", ref _ShelfTypeState, value);
            }
        }
        private string _ShelfTypeContent;
        public string ShelfTypeContent
        {
            get => _ShelfTypeContent;
            set => Set("ShelfTypeContent", ref _ShelfTypeContent, value);
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

        public ShelfDataViewModel(bool IsPlayBack)
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
            EnableContent = TranslationManager.Instance.Translate(EnableState.ToString()).ToString();
            InstallContent = TranslationManager.Instance.Translate(InstallState.ToString()).ToString();
            ShelfTypeContent = TranslationManager.Instance.Translate(ShelfTypeState.ToString()).ToString();
            UnitType = TranslationManager.Instance.Translate(procedureUnitType.ToString()).ToString().ToUpper();
        }

        public void AbleViewModel(ControlBase selectunit)
        {
            procedureUnitType = eClientProcedureUnitType.Shelf;
            UnitType = TranslationManager.Instance.Translate(procedureUnitType.ToString()).ToString().ToUpper();

            RefreshChecked = true;

            if (!IsPlayBackControl)
            {
                SelectUnit = selectunit as ShelfItem;
                if (SelectUnit is ShelfItem shelf)
                {
                    if (shelf.DeadZone) //DeadZone 표시
                    {
                        UnitType += string.Format("  [{0}]",TranslationManager.Instance.Translate("DeadZone"));
                    }
                    if (shelf.SHELFUSE)
                        EnableState = eUnitCommandProperty.Disable;
                    else
                        EnableState = eUnitCommandProperty.Enable;

                    if (!shelf.CheckCarrierExist())
                        InstallState = eUnitCommandProperty.Install;
                    else
                        InstallState = eUnitCommandProperty.Delete;

                    if (shelf.ShelfType.Equals(eShelfType.Both))
                        ShelfTypeState = eUnitCommandProperty.BothMode;
                    else if (shelf.ShelfType.Equals(eShelfType.Long))
                        ShelfTypeState = eUnitCommandProperty.LongMode;
                    else
                        ShelfTypeState = eUnitCommandProperty.ShortMode;
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
                    lock (SelectUnitLock)
                    {
                        if (SelectUnit is null)
                            continue;

                        if (!IsPlayBackControl)
                        {
                            if (SelectUnit is ShelfItem shelf)
                            {
                                UnitID = shelf.ControlName;
                                UnitBank = shelf.ShelfBank;
                                UnitBay = shelf.ShelfBay;
                                UnitLevel = shelf.ShelfLevel;
                                PalletInfo = shelf.CarrierID;
                                ProductEmpty = shelf.ProductEmpty;
                                CarrierSize = shelf.CarrierSize;
                                ShelfType = shelf.ShelfType;
                                ShelfState = shelf.ShelfState;
                                ShelfStatus = shelf.ShelfStatus;
                                ZoneName = shelf.ZONE;
                                ShelfPriority = 0;      //내부 변수가 없음.
                                PalletSize = shelf.PalletSize;
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
            string msg = string.Empty;
            string buttonname = string.Empty;
            bool bcommand = true;
            try
            {
                //PlayBackControl은 진행되면 안됨.
                if (IsPlayBackControl)
                    return;

                //각 모듈 베이스로 변환. 변환값 없으면 진행불가.
                ShelfItem shelf = SelectUnit as ShelfItem;
                if (shelf is null || shelf.DeadZone) //Deadzone 커맨드 불능 추가.
                    return;

                //제어 Code는 기존 LayOutView에 있는 Code를 그대로 사용한다.
                MessageBoxPopupView msgbox = null;
                CustomMessageBoxResult mBoxResult = null;

                eUnitCommandProperty changedProperty = eUnitCommandProperty.Active;
                switch (cmdProperty)
                {
                    //Shelf용
                    case eUnitCommandProperty.Enable:
                    case eUnitCommandProperty.Disable:
                        #region

                        eUnitCommandProperty curSt = cmdProperty.Equals(eUnitCommandProperty.Enable) ?
                            eUnitCommandProperty.Disable : eUnitCommandProperty.Enable;
                        eUnitCommandProperty destSt = cmdProperty.Equals(eUnitCommandProperty.Enable) ?
                            eUnitCommandProperty.Enable : eUnitCommandProperty.Disable;

                        buttonname = destSt.ToString();

                        LogManager.WriteOperatorLog(string.Format("사용자가 Shelf {0}의 수동지시 {1} 을/를 Click하였습니다.", shelf.ControlName, buttonname),
                            "CLICK", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 15,
                            shelf.ControlName, buttonname);

                        // 20240124 RGJ 쉘프 Enable/Disable Auto 에서 가능 (조범석 매니저 요청)
                        //SuHwan_20230214 : [2차 UI검수]
                        //if (GlobalData.Current.MainBooth.SCState != eSCState.PAUSED)
                        //{
                        //    msg = TranslationManager.Instance.Translate("Set SCS mode to Pause for operation.").ToString();
                        //    msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OK, MessageBoxImage.Stop, false);
                        //    mBoxResult = msgbox.ShowResult();
                        //    if (mBoxResult.Result.Equals(MessageBoxResult.OK)) { }
                        //    return;
                        //}

                        msg = string.Format(TranslationManager.Instance.Translate("Shelf State").ToString() + " " +
                                                         TranslationManager.Instance.Translate("Change").ToString(),
                                                         string.Format(TranslationManager.Instance.Translate("Shelf").ToString() + "[{0}]",
                                                                       shelf.ControlName),
                                                         string.Format("{0} -> {1}",
                                                                       TranslationManager.Instance.Translate(curSt.ToString()).ToString(),
                                                                       TranslationManager.Instance.Translate(destSt.ToString()).ToString()),
                                                         TranslationManager.Instance.Translate("Do You Want to Change?").ToString());

                        msgbox = new MessageBoxPopupView(msg, "쉘프 데이터 변경 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                            shelf.ControlName, curSt.ToString(), destSt.ToString(), true);

                        //msgbox = new MessageBoxPopupView(TranslationManager.Instance.Translate("Shelf State").ToString() + " " +
                        //                                 TranslationManager.Instance.Translate("Change").ToString(),
                        //                                 string.Format(TranslationManager.Instance.Translate("Shelf").ToString() + "[{0}]",
                        //                                               shelf.ControlName),
                        //                                 string.Format("{0} -> {1}",
                        //                                               TranslationManager.Instance.Translate(curSt.ToString()).ToString(),
                        //                                               TranslationManager.Instance.Translate(destSt.ToString()).ToString()),
                        //                                 TranslationManager.Instance.Translate("Do You Want to Change?").ToString(),
                        //                                 MessageBoxButton.OKCancel, MessageBoxImage.Question);

                        mBoxResult = msgbox.ShowResult();

                        if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                        {
                            changedProperty = destSt;

                            //SuHwan_20230202 : [ServerClient]
                            if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                            {
                                ClientSetProcedure(cmdProperty);
                            }
                            else
                            {
                                //Enable이면 Disable로 변경이니 false, Disable이면 Enable로 변경이니 true
                                shelf.SHELFUSE = changedProperty.Equals(eUnitCommandProperty.Enable);

                                msg = string.Format(TranslationManager.Instance.Translate("Shelf").ToString() + "[{0}]\n{1} -> {2}\n" +
                                                    TranslationManager.Instance.Translate("Change").ToString() + " " +
                                                    TranslationManager.Instance.Translate("Complete"),
                                                    shelf.ControlName,
                                                    TranslationManager.Instance.Translate(curSt.ToString()).ToString(),
                                                    TranslationManager.Instance.Translate(destSt.ToString()).ToString());
                                //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                                MessageBoxPopupView.Show(msg, "쉘프 데이터 변경완료 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                                    shelf.ControlName, curSt.ToString(), destSt.ToString(), true);

                                EnableState = curSt;
                            }
                        }
                        #endregion
                        break;

                    case eUnitCommandProperty.ShortMode:
                    case eUnitCommandProperty.LongMode:
                    case eUnitCommandProperty.BothMode:
                        #region
                        buttonname = cmdProperty.ToString();

                        LogManager.WriteOperatorLog(string.Format("사용자가 Shelf {0}의 수동지시 {1} 을/를 Click하였습니다.", shelf.ControlName, buttonname),
                            "CLICK", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 15,
                            shelf.ControlName, buttonname);

                        ModeChangeShelfTypePopupView modechangeView = new ModeChangeShelfTypePopupView(shelf, false);

                        if (modechangeView is null)
                        {
                            msg = string.Format("[{0}]" + TranslationManager.Instance.Translate("Command Error").ToString(), cmdProperty.ToString());
                            MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }

                        eShelfType changedType = modechangeView.ResultShelfType();
                        //동일하면 처리하지 않는다.
                        if (shelf.ShelfType.Equals(changedType))
                        {
                            msg = string.Format(TranslationManager.Instance.Translate("Current").ToString() + " " +
                                                TranslationManager.Instance.Translate("Shelf Type").ToString() + "[{0}]\n" +
                                                TranslationManager.Instance.Translate("Change").ToString() +
                                                TranslationManager.Instance.Translate("Shelf Type").ToString() + "[{1}]\n" +
                                                TranslationManager.Instance.Translate("Check Select Value").ToString(),
                                                TranslationManager.Instance.Translate(shelf.ShelfType.ToString()).ToString(),
                                                TranslationManager.Instance.Translate(changedType.ToString()).ToString());
                            //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                            msgbox = new MessageBoxPopupView(msg, "쉘프 데이터 변경완료 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                                shelf.ControlName, shelf.ShelfType.ToString(), changedType.ToString(), true);
                            return;
                        }
                        //Short, Long, Both중에 있다면 처리한다.
                        if (changedType >= eShelfType.Short && changedType <= eShelfType.Both)
                        {
                            msg = string.Format(TranslationManager.Instance.Translate("Shelf").ToString() + "[{0}] " +
                                                TranslationManager.Instance.Translate("Type").ToString() + "\n[{1}] -> [{2}]\n" +
                                                TranslationManager.Instance.Translate("Change") + "?",
                                                shelf.ControlName,
                                                TranslationManager.Instance.Translate(shelf.ShelfType.ToString()).ToString(),
                                                TranslationManager.Instance.Translate(changedType.ToString()).ToString());
                            //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                            msgbox = new MessageBoxPopupView(msg, "쉘프 데이터 변경 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                                shelf.ControlName, shelf.ShelfType.ToString(), changedType.ToString(), true);

                            eShelfType prevShelfType = shelf.ShelfType;

                            mBoxResult = msgbox.ShowResult();

                            if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                            {
                                //SuHwan_20230202 : [ServerClient]
                                if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                                {
                                    switch (changedType)
                                    {
                                        case eShelfType.Short:
                                            ClientSetProcedure(eUnitCommandProperty.ShortMode);
                                            break;
                                        case eShelfType.Long:
                                            ClientSetProcedure(eUnitCommandProperty.LongMode);
                                            break;
                                        case eShelfType.Both:
                                            ClientSetProcedure(eUnitCommandProperty.BothMode);
                                            break;
                                        default:
                                            ClientSetProcedure(eUnitCommandProperty.BothMode);
                                            break;
                                    }
                                }
                                else
                                {
                                    shelf.ShelfType = changedType;

                                    switch (shelf.ShelfType)
                                    {
                                        case eShelfType.Short:
                                            ShelfTypeState = eUnitCommandProperty.ShortMode;
                                            break;
                                        case eShelfType.Long:
                                            ShelfTypeState = eUnitCommandProperty.LongMode;
                                            break;
                                        case eShelfType.Both:
                                            ShelfTypeState = eUnitCommandProperty.BothMode;
                                            break;
                                    }

                                    msg = string.Format(TranslationManager.Instance.Translate("Shelf").ToString() + "[{0}] " +
                                                        TranslationManager.Instance.Translate("Type").ToString() + "\n[{1}]\n" +
                                                        TranslationManager.Instance.Translate("Change").ToString() + " " +
                                                        TranslationManager.Instance.Translate("Complete").ToString(),
                                                        shelf.ControlName,
                                                        TranslationManager.Instance.Translate(shelf.ShelfType.ToString()).ToString());
                                    //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                                    MessageBoxPopupView.Show(msg, "쉘프 데이터 변경완료 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                                        shelf.ControlName, prevShelfType.ToString(), shelf.ShelfType.ToString(), true);
                                }

                            }
                        }
                        #endregion
                        break;
                    //공용
                    case eUnitCommandProperty.Install:
                        #region
                        buttonname = cmdProperty.ToString();

                        LogManager.WriteOperatorLog(string.Format("사용자가 Shelf {0}의 수동지시 {1} 을/를 Click하였습니다.", shelf.ControlName, buttonname),
                            "CLICK", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 15,
                            shelf.ControlName, buttonname);

                        if (!shelf.CheckCarrierExist())
                        {
                            //데이터 입력 팝업은 그냥 나오고 데이터 입력 팝업에서 확인을 눌리면 확인해주는 팝업을 띄워준다.
                            CarrierInstall ci = new CarrierInstall(shelf.ControlName);

                            CarrierItem carrier = ci.ResultCarrierItem();

                            if (carrier != null)
                            {
                                msg = string.Format(TranslationManager.Instance.Translate("Shelf").ToString() + "[{0}]\n" +
                                                    TranslationManager.Instance.Translate("CarrierID").ToString() + "[{1}]\n{2}?",
                                                    shelf.ControlName, carrier.CarrierID, TranslationManager.Instance.Translate(cmdProperty.ToString()).ToString());
                                //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                                msgbox = new MessageBoxPopupView(msg, "쉘프 캐리어ID 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                                    shelf.ControlName, carrier.CarrierID, cmdProperty.ToString(), true);

                                mBoxResult = msgbox.ShowResult();

                                if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                                {
                                    carrier.CarrierState = eCarrierState.COMPLETED;     //db에는 state 업데이트가 되지 않아 재실행때 문제가 됨.
                                    //SuHwan_20230202 : [ServerClient]
                                    if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                                    {
                                        ClientSetProcedure(cmdProperty, carrier);
                                    }
                                    else
                                    {
                                        bool GenResult = ShelfManager.Instance.GenerateCarrierRequest(shelf.ControlName, carrier); //캐리워 인스톨 실패하는 경우 케이스 추가.
                                        if (GenResult)
                                        {
                                            msg = string.Format(TranslationManager.Instance.Translate("Shelf").ToString() + "[{0}]\n" +
                                                                TranslationManager.Instance.Translate("CarrierID").ToString() + "[{1}]\n{2} " +
                                                                TranslationManager.Instance.Translate("Complete").ToString(),
                                                                shelf.ControlName, carrier.CarrierID, TranslationManager.Instance.Translate(cmdProperty.ToString()).ToString());
                                            //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                                            MessageBoxPopupView.Show(msg, "쉘프 캐리어ID추가 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                                                shelf.ControlName, carrier.CarrierID, "Complete", true);
                                            InstallState = eUnitCommandProperty.Delete;
                                        }
                                        else
                                        {
                                            msg = string.Format(TranslationManager.Instance.Translate("Shelf").ToString() + "[{0}]\n" +
                                                                TranslationManager.Instance.Translate("CarrierID").ToString() + "[{1}]\n{2} " +
                                                                TranslationManager.Instance.Translate("Fail").ToString(),
                                                                shelf.ControlName, carrier.CarrierID, TranslationManager.Instance.Translate(cmdProperty.ToString()).ToString());
                                            //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Error);
                                            MessageBoxPopupView.Show(msg, "쉘프 캐리어ID추가 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Error,
                                                shelf.ControlName, "EXIST", "Fail", true);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            //혹시나 캐리어가 없으면 인스톨 모드로 변경해준다.
                            InstallState = eUnitCommandProperty.Delete;
                            msg = string.Format(TranslationManager.Instance.Translate("Shelf").ToString() + "[{0}]\n" +
                                                TranslationManager.Instance.Translate("Carrier Exist").ToString() + "\n" +
                                                TranslationManager.Instance.Translate("Change").ToString() + " {1} " +
                                                TranslationManager.Instance.Translate("Mode").ToString(),
                                                shelf.ControlName,
                                                TranslationManager.Instance.Translate(InstallState.ToString()).ToString());
                            //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                            MessageBoxPopupView.Show(msg, "쉘프 캐리어ID추가 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Error,
                                shelf.ControlName, "Carrier Exist", "Fail", true);

                        }
                        #endregion
                        break;
                    case eUnitCommandProperty.Delete:
                        #region
                        buttonname = cmdProperty.ToString();

                        LogManager.WriteOperatorLog(string.Format("사용자가 Shelf {0}의 수동지시 {1} 을/를 Click하였습니다.", shelf.ControlName, buttonname),
                            "CLICK", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 15,
                            shelf.ControlName, buttonname);

                        if (shelf.CheckCarrierExist())
                        {
                            string carrierid = shelf.CarrierID;
                            //데이터 입력 팝업은 그냥 나오고 데이터 입력 팝업에서 확인을 눌리면 확인해주는 팝업을 띄워준다.
                            msg = string.Format(TranslationManager.Instance.Translate("Shelf").ToString() + "[{0}]\n" +
                                                TranslationManager.Instance.Translate("CarrierID").ToString() + "[{1}]\n{2}?",
                                                shelf.ControlName, carrierid, TranslationManager.Instance.Translate(cmdProperty.ToString()).ToString());
                            //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                            msgbox = new MessageBoxPopupView(msg, "쉘프 캐리어ID 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                                shelf.ControlName, carrierid, cmdProperty.ToString(), true);

                            mBoxResult = msgbox.ShowResult();

                            if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                            {
                                if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                                {
                                    ClientSetProcedure(cmdProperty);
                                }
                                else
                                {
                                    ShelfManager.Instance.RequestCarrierRemove(shelf.ControlName);

                                    msg = string.Format(TranslationManager.Instance.Translate("Shelf").ToString() + "[{0}]\n" +
                                                        TranslationManager.Instance.Translate("CarrierID").ToString() + "[{1}]\n{2} " +
                                                        TranslationManager.Instance.Translate("Complete").ToString(),
                                                        shelf.ControlName, carrierid, TranslationManager.Instance.Translate(cmdProperty.ToString()).ToString());
                                    //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                                    MessageBoxPopupView.Show(msg, "쉘프 캐리어ID삭제 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                                        shelf.ControlName, carrierid, "Complete", true);

                                    InstallState = eUnitCommandProperty.Install;
                                }
                            }
                        }
                        else
                        {
                            //혹시나 캐리어가 없으면 인스톨 모드로 변경해준다.
                            InstallState = eUnitCommandProperty.Install;
                            msg = string.Format(TranslationManager.Instance.Translate("Shelf").ToString() + "[{0}]\n" +
                                                TranslationManager.Instance.Translate("Carrier Not Exist").ToString() + "\n" +
                                                TranslationManager.Instance.Translate("Change").ToString() + " {1} " +
                                                TranslationManager.Instance.Translate("Mode").ToString(),
                                                shelf.ControlName,
                                                TranslationManager.Instance.Translate(InstallState.ToString()).ToString());
                            //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                            MessageBoxPopupView.Show(msg, "쉘프 캐리어ID삭제 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Error,
                                shelf.ControlName, "Carrier Not Exist", "Fail", true);
                        }
                        #endregion
                        break;
                    case eUnitCommandProperty.Inform:
                        #region
                        buttonname = cmdProperty.ToString();

                        LogManager.WriteOperatorLog(string.Format("사용자가 Shelf {0}의 수동지시 {1} 을/를 Click하였습니다.", shelf.ControlName, buttonname),
                            "CLICK", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 15,
                            shelf.ControlName, buttonname);

                        //230405 HHJ SCS 개선     //- Memo 기능 추가
                        InformMemoView informMemo = new InformMemoView(shelf.ShelfMemo);
                        ResultInformMemo result = informMemo.InformMemoString();

                        //Cancel으로 나오면 뒷처리 없음.
                        if (result.InformMemoResult.Equals(eResultInformMemo.eCancel))
                            return;

                        string InformMemoString = result.InformMemoString;

                        msg = TranslationManager.Instance.Translate("Comment입력 메시지").ToString();
                        msg = string.Format(msg,
                            string.IsNullOrEmpty(InformMemoString) ? TranslationManager.Instance.Translate("Clear").ToString() : InformMemoString,
                            shelf.ControlName);

                        msgbox = new MessageBoxPopupView("Info Message", "", msg, "", MessageBoxButton.OKCancel, MessageBoxImage.Question, "Comment입력 메시지", string.IsNullOrEmpty(InformMemoString) ? "Clear" : InformMemoString, shelf.ControlName, true);
                        //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);

                        mBoxResult = msgbox.ShowResult();
                        if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                        {
                            if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                            {
                                ClientSetProcedure(cmdProperty, null, InformMemoString);
                            }
                            else
                            {
                                shelf.ShelfMemo = InformMemoString;
                            }

                            if (string.IsNullOrEmpty(InformMemoString))
                                InformMemoString = "(Clear)";

                            LogManager.WriteInformLog(string.Format("ShelfID:{0}, Memo:{1}, UserID:{2}", shelf.GetTagName(), InformMemoString, MainWindow.checkLoginUserID));
                        }
                        #endregion
                        break;
                    case eUnitCommandProperty.Status:
                        {
                            buttonname = cmdProperty.ToString();

                            LogManager.WriteOperatorLog(string.Format("사용자가 Shelf {0}의 수동지시 {1} 을/를 Click하였습니다.", shelf.ControlName, buttonname),
                                "CLICK", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 15,
                                shelf.ControlName, buttonname);

                            //20230407 정인길: 시스템 Pause 상태가 아닐시 실행X
                            if (GlobalData.Current.MainBooth.SCState != eSCState.PAUSED)
                            {
                                msg = TranslationManager.Instance.Translate("Set SCS mode to Pause for operation.").ToString();
                                msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OK, MessageBoxImage.Stop, false);
                                mBoxResult = msgbox.ShowResult();
                                if (mBoxResult.Result.Equals(MessageBoxResult.OK)) { }
                                return;
                            }

                            //ShelfStatusChangePopupView 팝업창 위치 가운데로 적용
                            ShelfStatusChangePopupView sscpv = new ShelfStatusChangePopupView();
                            sscpv.Owner = Application.Current.MainWindow;
                            sscpv.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                            sscpv.ShowDialog();
                        }
                        break;
                }

                //SuHwan_20230202 : [ServerClient] 
                if (GlobalData.Current.ServerClientType == eServerClientType.Server)
                {
                    ShelfItem SItem = ShelfManager.Instance.GetShelf(shelf.ControlName);
                    GlobalData.Current.ShelfMgr.SaveShelfData(SItem);
                }

                if (mBoxResult != null && mBoxResult.Result.Equals(MessageBoxResult.OK))
                {
                    LogManager.WriteOperatorLog(string.Format("사용자가 Shelf {0}의 수동지시 {1} 을/를 Write하였습니다.", shelf.ControlName, buttonname),
                        "WRITE", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 22,
                        shelf.ControlName, buttonname);
                }
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        private void ClientSetProcedure(eUnitCommandProperty cmdProperty, CarrierItem carrier = null, string memo = "")
        {
            if (GlobalData.Current.ServerClientType != eServerClientType.Client)
                return;

            //PlayBackControl은 진행되면 안됨.
            if (IsPlayBackControl)
                return;

            //각 모듈 베이스로 변환. 변환값 없으면 진행불가.
            ShelfItem shelf = SelectUnit as ShelfItem;
            if (shelf is null)
                return;

            ClientReqList reqBuffer = new ClientReqList
            {
                EQPID = GlobalData.Current.EQPID,
                CMDType = cmdProperty.ToString(),
                Target = procedureUnitType.ToString(),
                TargetID = shelf.TagName,
                TargetValue = string.Empty,
                ReqTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Requester = eServerClientType.Client,
            };

            //230404 조건 추가
            if (cmdProperty.Equals(eUnitCommandProperty.Install) ||
                cmdProperty.Equals(eUnitCommandProperty.CarrierSize) ||
                cmdProperty.Equals(eUnitCommandProperty.CarrierID) ||
                cmdProperty.Equals(eUnitCommandProperty.ProductEmpty))
            {
                reqBuffer.TargetValue = JsonConvert.SerializeObject(carrier);
            }
            else if (cmdProperty.Equals(eUnitCommandProperty.Inform))
            {
                reqBuffer.TargetValue = memo;
            }

            GlobalData.Current.DBManager.DbSetProcedureClientReq(reqBuffer.EQPID, reqBuffer.CMDType, reqBuffer.Target, reqBuffer.TargetID, reqBuffer.TargetValue, reqBuffer.ReqTime, reqBuffer.Requester);
        }

        //SuHwan_20230404 : 데이타변경 팝업추가
        /// <summary>
        /// 셀렉트 아이탬 데이타 변경
        /// </summary>
        /// <param name="revItemName"></param>
        /// <param name="rcvValue"></param>
        public void setSelectUnitData(string revItemName, string rcvValue)
        {
            try //여기서 예외 발생시 프로그램 Down 되므로 예외처리함.
            {
                MessageBoxPopupView msgbox = null;
                CustomMessageBoxResult mBoxResult = null;
                ShelfItem shelf = SelectUnit as ShelfItem;
                string msg = string.Empty;
                if (shelf.InSlotCarrier == null) //240125 RGJ 빈 쉘프면 인스톨을 해야한다.중단함 
                {
                    msgbox = new MessageBoxPopupView("No Carrier in Shelf : " + shelf.TagName, MessageBoxButton.OK);
                    msgbox.ShowResult();
                    return;
                }
                revItemName = revItemName?.Trim(); //입력값의 앞뒤 공백을 제거한다.
                rcvValue = rcvValue?.Trim(); //입력값의 앞뒤 공백을 제거한다.

                switch (revItemName)
                {

                    case "CarrierID":
                        if (CarrierStorage.Instance.CarrierContain(rcvValue))   //변경할려는 ID 가 이미 존재하면 취소한다.
                        {
                            msg = string.Format("Carrier ID : {0}   Already Existed.", rcvValue);

                            msgbox = new MessageBoxPopupView(msg, "Check Shelf Carriers.", "", "", MessageBoxButton.OK, MessageBoxImage.Warning,
                                shelf.ControlName, shelf.CarrierID, rcvValue, true);

                            mBoxResult = msgbox.ShowResult();
                            return;
                        }


                        msg = string.Format(TranslationManager.Instance.Translate("Carrier").ToString() + "[{0}] ID\n[{1}] -> [{2}]\n" +
                                            TranslationManager.Instance.Translate("Change").ToString() + "?",
                                            shelf.ControlName, shelf.CarrierID, rcvValue);
                        //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                        msgbox = new MessageBoxPopupView(msg, "쉘프 데이터 변경 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                            shelf.ControlName, shelf.CarrierID, rcvValue, true);

                        mBoxResult = msgbox.ShowResult();

                        if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                        {
                            string beforeCarrierID = shelf.CarrierID;
                            eCarrierState beforestate = shelf.CarrierState;

                            shelf.CarrierID = rcvValue;
                            shelf.InSlotCarrier.CarrierID = rcvValue.ToUpper();

                            if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                            {
                                ClientSetProcedure(eUnitCommandProperty.CarrierID, shelf.InSlotCarrier);
                            }
                            else
                            {
                                CarrierStorage.Instance.InsertCarrier(shelf.InSlotCarrier);
                                shelf.UpdateCarrier(shelf.CarrierID, true, true);
                                CarrierStorage.Instance.RemoveStorageCarrier(beforeCarrierID);
                                shelf.InSlotCarrier.CarrierState = beforestate;
                                //shelf.NotifyShelfStatusChanged();
                            }
                        }
                        break;
                    case "CarrierSize":
                        foreach (eCarrierSize enumItem in Enum.GetValues(typeof(eCarrierSize)))
                        {
                            //230404 s
                            if (enumItem.ToString() == rcvValue)
                            {
                                msg = string.Format(TranslationManager.Instance.Translate("Carrier").ToString() + "[{0}] " +
                                                    TranslationManager.Instance.Translate("Size").ToString() + "\n[{1}] -> [{2}]\n" +
                                                    TranslationManager.Instance.Translate("Change").ToString() + "?",
                                                    shelf.ControlName,
                                                    TranslationManager.Instance.Translate(shelf.CarrierSize.ToString()).ToString(),
                                                    TranslationManager.Instance.Translate(enumItem.ToString()).ToString());
                                //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                                msgbox = new MessageBoxPopupView(msg, "쉘프 데이터 변경 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                                    shelf.ControlName, shelf.CarrierSize.ToString(), enumItem.ToString(), true);

                                mBoxResult = msgbox.ShowResult();

                                if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                                {
                                    shelf.CarrierSize = enumItem;
                                    shelf.InSlotCarrier.CarrierSize = enumItem;

                                    if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                                    {
                                        ClientSetProcedure(eUnitCommandProperty.CarrierSize, shelf.InSlotCarrier);
                                    }
                                    else
                                    {
                                        //CarrierStorage.Instance.InsertCarrier(shelf.InSlotCarrier);
                                        //Enum.TryParse(rcvValue, out eCarrierSize carriersize);
                                        //shelf.InSlotCarrier.CarrierSize = carriersize;
                                        shelf.UpdateCarrier(shelf.CarrierID, true, false);
                                        //shelf.NotifyShelfStatusChanged();
                                    }
                                }
                                return;
                            }
                            //230404 e
                        }
                        break;
                    case "PalletSize":
                        foreach (ePalletSize enumItem in GlobalData.Current.GetPalletSizeList())
                        {
                            //230404 s
                            if (enumItem.ToString() == rcvValue)
                            {
                                msg = string.Format(TranslationManager.Instance.Translate("Carrier").ToString() + "[{0}] " +
                                                    TranslationManager.Instance.Translate("Size").ToString() + "\n[{1}] -> [{2}]\n" +
                                                    TranslationManager.Instance.Translate("Change").ToString() + "?",
                                                    shelf.ControlName,
                                                    TranslationManager.Instance.Translate(shelf.PalletSize.ToString()).ToString(),
                                                    TranslationManager.Instance.Translate(enumItem.ToString()).ToString());
                                //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                                msgbox = new MessageBoxPopupView(msg, "쉘프 데이터 변경 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                                    shelf.ControlName, shelf.PalletSize.ToString(), enumItem.ToString(), true);

                                mBoxResult = msgbox.ShowResult();

                                if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                                {
                                    //shelf.CarrierSize = enumItem;
                                    shelf.InSlotCarrier.PalletSize = enumItem;

                                    if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                                    {
                                        ClientSetProcedure(eUnitCommandProperty.PalletSize, shelf.InSlotCarrier);
                                    }
                                    else
                                    {
                                        //CarrierStorage.Instance.InsertCarrier(shelf.InSlotCarrier);
                                        //Enum.TryParse(rcvValue, out eCarrierSize carriersize);
                                        //shelf.InSlotCarrier.CarrierSize = carriersize;
                                        shelf.UpdateCarrier(shelf.CarrierID, true, false);
                                        //shelf.NotifyShelfStatusChanged();
                                    }
                                }
                                return;
                            }
                            //230404 e
                        }
                        break;
                    case "ProductEmpty":
                        foreach (eProductEmpty enumItem in Enum.GetValues(typeof(eProductEmpty)))
                        {
                            //230404 s
                            if (enumItem.ToString() == rcvValue)
                            {
                                msg = string.Format(TranslationManager.Instance.Translate("Product Empty").ToString() + "[{0}] " +
                                                    "\n[{1}] -> [{2}]\n" +
                                                    TranslationManager.Instance.Translate("Change").ToString() + "?",
                                                    shelf.ControlName,
                                                    shelf.ProductEmpty.ToString(),
                                                    enumItem.ToString());
                                //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                                msgbox = new MessageBoxPopupView(msg, "쉘프 데이터 변경 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                                    shelf.ControlName, shelf.PalletSize.ToString(), enumItem.ToString(), true);

                                mBoxResult = msgbox.ShowResult();

                                if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                                {
                                    //shelf.CarrierSize = enumItem;
                                    shelf.InSlotCarrier.ProductEmpty = enumItem;

                                    if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                                    {
                                        ClientSetProcedure(eUnitCommandProperty.ProductEmpty, shelf.InSlotCarrier);
                                    }
                                    else
                                    {
                                        //CarrierStorage.Instance.InsertCarrier(shelf.InSlotCarrier);
                                        //Enum.TryParse(rcvValue, out eCarrierSize carriersize);
                                        //shelf.InSlotCarrier.CarrierSize = carriersize;
                                        shelf.UpdateCarrier(shelf.CarrierID, true, false);
                                        //shelf.NotifyShelfStatusChanged();
                                    }
                                }
                                return;
                            }
                            //230404 e
                        }
                        break;
                    case "ShelfType":
                        foreach (eShelfType enumItem in Enum.GetValues(typeof(eShelfType)))
                        {
                            if (enumItem.ToString() == rcvValue)
                            {
                                if (shelf.InSlotCarrier.CarrierSize == eCarrierSize.Long
                                    && enumItem == eShelfType.Short)
                                {

                                    msg = string.Format(TranslationManager.Instance.Translate("Shelf").ToString() + "[{0}] " +
                                                        TranslationManager.Instance.Translate("Carrier Size").ToString() + "[{1}]\n" +
                                                        TranslationManager.Instance.Translate("as a reason").ToString() + "\n [{2}] " +
                                                        TranslationManager.Instance.Translate("Can't Change").ToString(),
                                                        shelf.ControlName,
                                                        TranslationManager.Instance.Translate(shelf.InSlotCarrier.CarrierSize.ToString()).ToString(),
                                                        TranslationManager.Instance.Translate(enumItem.ToString()).ToString());
                                    //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                                    msgbox = new MessageBoxPopupView(msg, "쉘프 데이터 변경완료 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                                        shelf.ControlName, shelf.InSlotCarrier.CarrierSize.ToString(), "Fail", true);

                                    mBoxResult = msgbox.ShowResult();
                                    return;
                                }
                                //shelf.ShelfType = enumItem;
                                //return;

                                msg = string.Format(TranslationManager.Instance.Translate("Shelf").ToString() + "[{0}] " +
                                                    TranslationManager.Instance.Translate("Type").ToString() + "\n[{1}] -> [{2}]\n" +
                                                    TranslationManager.Instance.Translate("Change").ToString() + "?",
                                                    shelf.ControlName,
                                                    TranslationManager.Instance.Translate(shelf.ShelfType.ToString()).ToString(),
                                                    TranslationManager.Instance.Translate(enumItem.ToString()).ToString());
                                //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                                msgbox = new MessageBoxPopupView(msg, "쉘프 데이터 변경 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                                    shelf.ControlName, shelf.ShelfType.ToString(), enumItem.ToString(), true);

                                mBoxResult = msgbox.ShowResult();

                                if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                                {
                                    if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                                    {
                                        switch (enumItem)
                                        {
                                            case eShelfType.Short:
                                                ClientSetProcedure(eUnitCommandProperty.ShortMode);
                                                break;
                                            case eShelfType.Long:
                                                ClientSetProcedure(eUnitCommandProperty.LongMode);
                                                break;
                                            case eShelfType.Both:
                                                ClientSetProcedure(eUnitCommandProperty.BothMode);
                                                break;
                                            default:
                                                ClientSetProcedure(eUnitCommandProperty.BothMode);
                                                break;
                                        }

                                        return;
                                    }
                                    else
                                    {
                                        shelf.ShelfType = enumItem;

                                        switch (shelf.ShelfType)
                                        {
                                            case eShelfType.Short:
                                                ShelfTypeState = eUnitCommandProperty.ShortMode;
                                                break;
                                            case eShelfType.Long:
                                                ShelfTypeState = eUnitCommandProperty.LongMode;
                                                break;
                                            case eShelfType.Both:
                                                ShelfTypeState = eUnitCommandProperty.BothMode;
                                                break;
                                        }

                                        shelf.UpdateCarrier(shelf.CarrierID, false, true);

                                        msg = string.Format(TranslationManager.Instance.Translate("Shelf").ToString() + "[{0}] " +
                                                            TranslationManager.Instance.Translate("Type").ToString() + "\n[{1}]\n" +
                                                            TranslationManager.Instance.Translate("Change").ToString() + " " +
                                                            TranslationManager.Instance.Translate("Complete").ToString(),
                                                            shelf.ControlName,
                                                            TranslationManager.Instance.Translate(shelf.ShelfType.ToString()).ToString());
                                        MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                                        msgbox = new MessageBoxPopupView(msg, "쉘프 데이터 변경완료 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                                            shelf.ControlName, shelf.ShelfType.ToString(), enumItem.ToString(), true);

                                        return;
                                    }

                                }
                            }
                        }
                        break;
                    case "ShelfStatus":
                        foreach (eShelfStatus enumItem in Enum.GetValues(typeof(eShelfStatus)))
                        {
                            if (enumItem.ToString() == rcvValue)
                            {
                                shelf.ShelfStatus = enumItem;
                                return;
                            }
                        }
                        break;
                }

                if (mBoxResult != null &&
                    mBoxResult.Result.Equals(MessageBoxResult.OK))
                {
                    LogManager.WriteOperatorLog(string.Format("사용자가 {0} {1}에 {2} 을/를 Write하였습니다.", shelf.ControlName, revItemName, rcvValue),
                        "WRITE", revItemName, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 10,
                        shelf.ControlName, revItemName, rcvValue);
                }

                RefreshCheckPass = true;
            }
            catch(Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }

        public void CloseView()
        {
            CloseThread = true;
            viewModelthread.Join();
        }
    }
}
