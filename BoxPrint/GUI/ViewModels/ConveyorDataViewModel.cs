using Newtonsoft.Json;
using PLCProtocol.DataClass;
using BoxPrint.DataList;
using BoxPrint.GUI.ETC;
using BoxPrint.GUI.EventCollection;
using BoxPrint.GUI.UIControls;
using BoxPrint.GUI.ViewModels.BindingCommand;
using BoxPrint.Log;
using BoxPrint.Modules.Conveyor;
using BoxPrint.Modules.CVLine;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.ViewModels
{
    public class ConveyorDataViewModel : ViewModelBase
    {
        private ControlBase SelectUnit = null;
        private PLCDataItem SelectItem = null;//SuHwan_20230207 : 피엘씨 쓰기 추가
        private bool RefreshCheckPass = false;//SuHwan_20230207 : 피엘씨 쓰기 추가
        private object SelectUnitLock = new object();
        private DateTime ViewInitTime = DateTime.Now;
        private eClientProcedureUnitType procedureUnitType;     //230207 추가 [ServerClient]
        private bool IsPlayBackControl = false;
        public bool LoginState = false;
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
        //Track 아이디
        private string _TrackID;
        public string TrackID
        {
            get => _TrackID;
            set => Set("TrackID", ref _TrackID, value);
        }
        //팔렛 정보
        private string _PalletInfo;
        public string PalletInfo
        {
            get => _PalletInfo;
            set => Set("PalletInfo", ref _PalletInfo, value);
        }
        //팔렛 사이즈 를 케리어 사이즈로 바꾼 정보
        private eCarrierSize _CarrierSize;
        public eCarrierSize CarrierSize
        {
            get => _CarrierSize;
            set => Set("CarrierSize", ref _CarrierSize, value);
        }
        //팔렛 사이즈 정보 //SuHwan_20230404 : 팔렛 사이즈 추가
        private ePalletSize _PalletSize;
        public ePalletSize PalletSize
        {
            get => _PalletSize;
            set => Set("PalletSize", ref _PalletSize, value);
        }
        //Destination
        private string _Destination;
        public string Destination
        {
            get => _Destination;
            set => Set("Destination", ref _Destination, value);
        }
        //FinalLocation
        private string _FinalLocation;
        public string FinalLocation
        {
            get => _FinalLocation;
            set => Set("FinalLocation", ref _FinalLocation, value);
        }
        //ProductEmpty
        private eProductEmpty _ProductEmpty;
        public eProductEmpty ProductEmpty
        {
            get => _ProductEmpty;
            set => Set("ProductEmpty", ref _ProductEmpty, value);
        }
        //Polarity
        private ePolarity _Polarity;
        public ePolarity Polarity
        {
            get => _Polarity;
            set => Set("Polarity", ref _Polarity, value);
        }
        //WinderDirection
        private eWinderDirection _WinderDirection;
        public eWinderDirection WinderDirection
        {
            get => _WinderDirection;
            set => Set("WinderDirection", ref _WinderDirection, value);
        }
        //ProductQuantity
        private int _ProductQuantity;
        public int ProductQuantity
        {
            get => _ProductQuantity;
            set => Set("ProductQuantity", ref _ProductQuantity, value);
        }
        //InnerTrayType
        private eInnerTrayType _InnerTrayType;
        public eInnerTrayType InnerTrayType
        {
            get => _InnerTrayType;
            set => Set("InnerTrayType", ref _InnerTrayType, value);
        }
        //TrayType
        private eTrayType _TrayType;
        public eTrayType TrayType
        {
            get => _TrayType;
            set => Set("TrayType", ref _TrayType, value);
        }
        //CoreType
        private eCoreType _CoreType;
        public eCoreType CoreType
        {
            get => _CoreType;
            set => Set("CoreType", ref _CoreType, value);
        }
        //ValidationNG
        private string _ValidationNG;
        public string ValidationNG
        {
            get => _ValidationNG;
            set => Set("ValidationNG", ref _ValidationNG, value);
        }
        //InOutMode
        private ePortInOutType _InOutMode;
        public ePortInOutType InOutMode
        {
            get => _InOutMode;
            set => Set("InOutMode", ref _InOutMode, value);
        }
        //Error Code
        private decimal _ErrorCode;
        public decimal ErrorCode
        {
            get => _ErrorCode;
            set => Set("ErrorCode", ref _ErrorCode, value);
        }
        #endregion

        #region Command Button
        //221230 HHJ SCS 개선
        public ICommand ButtonCommand { get; private set; }
        //230102 HHJ SCS 개선
        private eUnitCommandProperty _PortAccessState;
        public eUnitCommandProperty PortAccessState
        {
            get => _PortAccessState;
            set
            {
                PortAccessContent = TranslationManager.Instance.Translate(value.ToString()).ToString();
                Set("PortAccessState", ref _PortAccessState, value);
            }
        }
        private string _PortAccessContent;
        public string PortAccessContent
        {
            get => _PortAccessContent;
            set => Set("PortAccessContent", ref _PortAccessContent, value);
        }

        private eUnitCommandProperty _AutoManualState;
        public eUnitCommandProperty AutoManualState
        {
            get => _AutoManualState;
            set
            {
                AutoManualContent = TranslationManager.Instance.Translate(value.ToString()).ToString();
                Set("AutoManualState", ref _AutoManualState, value);
            }
        }
        private string _AutoManualContent;
        public string AutoManualContent
        {
            get => _AutoManualContent;
            set => Set("AutoManualContent", ref _AutoManualContent, value);
        }

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

        private eUnitCommandProperty _TrackPauseState;
        public eUnitCommandProperty TrackPauseState
        {
            get => _TrackPauseState;
            set
            {
                TrackPauseContent = TranslationManager.Instance.Translate(value.ToString()).ToString();
                Set("TrackPauseState", ref _TrackPauseState, value);
            }
        }
        private string _TrackPauseContent;
        public string TrackPauseContent
        {
            get => _TrackPauseContent;
            set => Set("TrackPauseContent", ref _TrackPauseContent, value);
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

        private eUnitCommandProperty _PortInOutModeState;
        public eUnitCommandProperty PortInOutModeState
        {
            get => _PortInOutModeState;
            set
            {
                PortInOutModeContent = TranslationManager.Instance.Translate(value.ToString()).ToString();
                Set("PortInOutModeState", ref _PortInOutModeState, value);
            }
        }
        private string _PortInOutModeContent;
        public string PortInOutModeContent
        {
            get => _PortInOutModeContent;
            set => Set("PortInOutModeContent", ref _PortInOutModeContent, value);
        }

        private bool _EnableBCRCommand;
        public bool EnableBCRCommand
        {
            get => _EnableBCRCommand;
            set => Set("EnableBCRCommand", ref _EnableBCRCommand, value);
        }

        private bool _EnableKeyInCommand;
        public bool EnableKeyInCommand
        {
            get => _EnableKeyInCommand;
            set => Set("EnableKeyInCommand", ref _EnableKeyInCommand, value);
        }

        private bool _EnablePortTypeCommand;
        public bool EnablePortTypeCommand
        {
            get => _EnablePortTypeCommand;
            set => Set("EnablePortTypeCommand", ref _EnablePortTypeCommand, value);
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

        public ConveyorDataViewModel(bool IsPlayBack)
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
            PortAccessContent = TranslationManager.Instance.Translate(PortAccessState.ToString()).ToString();
            AutoManualContent = TranslationManager.Instance.Translate(AutoManualState.ToString()).ToString();
            EnableContent = TranslationManager.Instance.Translate(EnableState.ToString()).ToString();
            TrackPauseContent = TranslationManager.Instance.Translate(TrackPauseState.ToString()).ToString();
            InstallContent = TranslationManager.Instance.Translate(InstallState.ToString()).ToString();
            PortInOutModeContent = TranslationManager.Instance.Translate(PortInOutModeState.ToString()).ToString();
            SelectUnitType = TranslationManager.Instance.Translate(procedureUnitType.ToString()).ToString().ToUpper();
        }

        //2024.08.22 lim, Login 계정에 따라 CV Button 사용 실시간 적용 
        public void CheckCVLoginState()
        {
            bool IsManualKeyin = false;
            if (SelectUnit is CV_BaseModule cv)
            {
                if (LoginState)
                {
                    bool IsAdmin = GlobalData.Current.UserMng.CurrentUser.UserLevel == eUserLevel.Admin;
                    bool UseAutoKeyin = GlobalData.Current.MainSection.UseAutoKeyin == true;

                    //AutoKeyin 사용시 수동키인 버튼 막게 요청 - 조범석 매니저
                    //나머지 포트도 admin 만 가능하도록 수정 
                    //AutoKeyin 미사용시 로그인 하면 사용 가능
                    //if ((cv.CVModuleType == eCVType.Manual && UseAutoKeyin) || (cv.CVModuleType != eCVType.Manual))
                    if ((cv.CVModuleType == eCVType.Manual && !UseAutoKeyin))       //AutoKeyin 미사용 Manual Port 로그인 시 사용 가능
                        IsManualKeyin = true;
                    else                                                            //나머지 Admin 계정에서 사용
                        IsManualKeyin = IsAdmin;

                    if (cv.CVModuleType != eCVType.WaterPool && cv.CheckPortTypeChagneAble())
                    {
                        EnablePortTypeCommand = true;

                        if (cv.PortInOutType.Equals(ePortInOutType.BOTH))
                            PortInOutModeState = eUnitCommandProperty.DirectionBothMode;
                        else if (cv.PortInOutType.Equals(ePortInOutType.OUTPUT))
                            PortInOutModeState = eUnitCommandProperty.DirectionOutMode;
                        else
                            PortInOutModeState = eUnitCommandProperty.DirectionInMode;
                    }

                    EnableBCRCommand = cv.UseBCR;
                    EnableKeyInCommand = IsManualKeyin;
                }
                else
                {
                    EnableBCRCommand = false;
                    EnablePortTypeCommand = false;
                    EnableKeyInCommand = false;
                }
            }
        }

        public void AbleViewModel(ControlBase selectunit)
        {
            //230207 변경 s
            //SelectUnitType = "CONVEYOR";
            procedureUnitType = eClientProcedureUnitType.CV;
            SelectUnitType = TranslationManager.Instance.Translate(procedureUnitType.ToString()).ToString().ToUpper();
            //230207 변경 e

            RefreshChecked = true;

            //if (!IsPlayBackControl)
            {
                SelectUnit = selectunit as CV_BaseModule;
                if (SelectUnit is CV_BaseModule cv)
                {
                    CheckCVLoginState();
                    //if (LoginState)
                    //{
                    //    EnableBCRCommand = cv.UseBCR;
                    //    EnableKeyInCommand = (cv.CVModuleType == eCVType.Manual); //20230818 RGJ KeyIn 은 사양 확정전까지는 수동포트에서만 사용.
                    //}
                    //else
                    //{
                    //    EnableBCRCommand = false;
                    //    EnableKeyInCommand = false;
                    //}


                    //230217 HHJ SCS 개선     
                    if (cv.PortAccessMode.Equals(ePortAceessMode.AUTO))
                        PortAccessState = eUnitCommandProperty.AccessOPER;
                    else
                        PortAccessState = eUnitCommandProperty.AccessAGV;

                    if (cv.CVUSE)
                        EnableState = eUnitCommandProperty.Disable;
                    else
                        EnableState = eUnitCommandProperty.Enable;

                    if (cv.AutoManualState.Equals(eCVAutoManualState.AutoRun))
                        AutoManualState = eUnitCommandProperty.ManualRun;
                    else
                        AutoManualState = eUnitCommandProperty.AutoRun;

                    if (!cv.CheckCarrierExist())
                        InstallState = eUnitCommandProperty.Install;
                    else
                        InstallState = eUnitCommandProperty.Delete;

                    //if (LoginState && cv.CVModuleType != eCVType.WaterPool && cv.CheckPortTypeChagneAble())
                    //{
                    //    EnablePortTypeCommand = true;

                    //    if (cv.PortInOutType.Equals(ePortInOutType.BOTH))
                    //        PortInOutModeState = eUnitCommandProperty.DirectionBothMode;
                    //    else if (cv.PortInOutType.Equals(ePortInOutType.OUTPUT))
                    //        PortInOutModeState = eUnitCommandProperty.DirectionOutMode;
                    //    else
                    //        PortInOutModeState = eUnitCommandProperty.DirectionInMode;
                    //}
                    //else
                    //{
                    //    EnablePortTypeCommand = false;
                    //}

                    if (cv.PC_TrackPause.Equals(0))
                        TrackPauseState = eUnitCommandProperty.TrackPause;
                    else
                        TrackPauseState = eUnitCommandProperty.TrackResume;
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

                        //if (!IsPlayBackControl)
                        {
                            if (SelectUnit is CV_BaseModule cv)
                            {
                                CarrierItem carrier = cv.ReadTrackingData();

                                SelectUnitID = cv.ControlName;
                                TrackID = cv.TrackID;
                                PalletInfo = carrier.CarrierID;
                                CarrierSize = carrier.CarrierSize;
                                Destination = carrier.Destination;
                                FinalLocation = carrier.FinalLoc;
                                ProductEmpty = carrier.ProductEmpty;
                                Polarity = carrier.Polarity;
                                WinderDirection = carrier.WinderDirection;
                                ProductQuantity = carrier.ProductQuantity;
                                InnerTrayType = carrier.InnerTrayType;
                                TrayType = carrier.TrayType;
                                CoreType = carrier.CoreType;
                                ValidationNG = carrier.ValidationNG;
                                InOutMode = cv.PortInOutType;
                                ErrorCode = cv.PLC_ErrorCode;
                                PalletSize = carrier.PalletSize;//SuHwan_20230404 : PalletSize 추가
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
        private async void ButtonCommandAction(eUnitCommandProperty cmdProperty)
        {
            string buttonname = string.Empty;
            bool bcommand = true;

            try
            {
                //PlayBackControl은 상세 정보만 보여야 한다.
                

                //각 모듈 베이스로 변환. 변환값 없으면 진행불가.
                CV_BaseModule cv = SelectUnit as CV_BaseModule;
                if (cv is null)
                    return;

                //제어 Code는 기존 LayOutView에 있는 Code를 그대로 사용한다.
                MessageBoxPopupView msgbox = null;
                string msg = string.Empty;
                CustomMessageBoxResult mBoxResult = null;
                CVLineModule cvLine = cv.ParentModule as CVLineModule;

                eUnitCommandProperty changedProperty = eUnitCommandProperty.Active;
                switch (cmdProperty)
                {
                    //CV용
                    case eUnitCommandProperty.AccessAGV:
                    case eUnitCommandProperty.AccessOPER:
                        #region 240418 RGJ OH 에서는 해당 기능 사용할일 없으므로 주석처리함.
                        {
                            //eUnitCommandProperty curSt = !cmdProperty.Equals(eUnitCommandProperty.AccessAGV) ?
                            //    eUnitCommandProperty.AccessAGV : eUnitCommandProperty.AccessOPER;
                            //eUnitCommandProperty destSt = cmdProperty.Equals(eUnitCommandProperty.AccessAGV) ?
                            //    eUnitCommandProperty.AccessAGV : eUnitCommandProperty.AccessOPER;

                            //buttonname = destSt.ToString();

                            //LogManager.WriteOperatorLog(string.Format("사용자가 컨베이어 {0}의 수동지시 {1} 을/를 Click하였습니다.", cv.ControlName, buttonname),
                            //    "CLICK", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 12,
                            //    cv.ControlName, buttonname);

                            //msg = string.Format(TranslationManager.Instance.Translate("Conveyor").ToString() + "[{0}]\n{1} -> {2}\n" +
                            //                    TranslationManager.Instance.Translate("Change").ToString() + "?",
                            //                    cv.ControlName,
                            //                    TranslationManager.Instance.Translate(curSt.ToString()).ToString(),
                            //                    TranslationManager.Instance.Translate(destSt.ToString()).ToString());

                            ////msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                            //msgbox = new MessageBoxPopupView(msg, "컨베이어 데이터 변경 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question, 
                            //    cv.ControlName, curSt.ToString(), destSt.ToString(), true);

                            //mBoxResult = msgbox.ShowResult();

                            //if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                            //{
                            //    changedProperty = destSt;

                            //    //230207 추가 s [ServerClient]
                            //    if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                            //    {
                            //        ClientSetProcedure(destSt, eServerClientType.Client);
                            //    }
                            //    else
                            //    {
                            //        await Task.Run(() =>    //20230217 RGJ 포트 컨베이어 모드 변경 비동기 방식으로 변경. 모드 변경에 문제가 있으면 UI Hang 걸림 방지.
                            //        {
                            //            cv.SetPortAccessMode(changedProperty.Equals(eUnitCommandProperty.AccessAGV) ? ePortAceessMode.AUTO : ePortAceessMode.MANUAL);
                            //            //230217 HHJ SCS 개선     //왜 PortAccess Mode와 Enable을 같이 처리하도록 되어있는지?
                            //            //cvLine.ChangeAllPortUseType(!cmdProperty.Equals(eUnitCommandProperty.Enable));
                            //        });

                            //        msg = string.Format(TranslationManager.Instance.Translate("Conveyor").ToString() + "[{0}]\n{1} -> {2}\n" +
                            //                            TranslationManager.Instance.Translate("Change").ToString() + " " +
                            //                            TranslationManager.Instance.Translate("Complete").ToString(),
                            //                            cv.ControlName,
                            //                            TranslationManager.Instance.Translate(curSt.ToString()).ToString(),
                            //                            TranslationManager.Instance.Translate(destSt.ToString()).ToString());
                            //        //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                            //        MessageBoxPopupView.Show(msg, "컨베이어 데이터 변경완료 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information, 
                            //            cv.ControlName, curSt.ToString(), destSt.ToString(), true);

                            //        PortAccessState = curSt;
                            //    }
                            //    //230207 추가 e [ServerClient]
                            //}
                        }
                        #endregion
                        break;

                    case eUnitCommandProperty.Enable:
                    case eUnitCommandProperty.Disable:
                        #region
                        {
                            eUnitCommandProperty curSt = cv.CVUSE ? eUnitCommandProperty.Enable : eUnitCommandProperty.Disable;
                            eUnitCommandProperty destSt = cv.CVUSE ? eUnitCommandProperty.Disable : eUnitCommandProperty.Enable;

                            buttonname = destSt.ToString();

                            LogManager.WriteOperatorLog(string.Format("사용자가 컨베이어 {0}의 수동지시 {1} 을/를 Click하였습니다.", cv.ControlName, buttonname),
                                "CLICK", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 12,
                                cv.ControlName, buttonname);

                            //SuHwan_20230214 : [2차 UI검수]
                            if (GlobalData.Current.MainBooth.SCState != eSCState.PAUSED)
                            {
                                msg = TranslationManager.Instance.Translate("Set SCS mode to Pause for operation.").ToString();
                                msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OK, MessageBoxImage.Stop, false);

                                mBoxResult = msgbox.ShowResult();
                                if (mBoxResult.Result.Equals(MessageBoxResult.OK)) { }
                                return;
                            }

                            //20230314 RGJ 포트 Enable 은 현재 포트 상태를 참고해서 변경함.
                            //eUnitCommandProperty curSt = cmdProperty.Equals(eUnitCommandProperty.Enable) ? 
                            //    eUnitCommandProperty.Enable : eUnitCommandProperty.Disable;
                            //eUnitCommandProperty destSt = cmdProperty.Equals(eUnitCommandProperty.Enable) ?
                            //    eUnitCommandProperty.Disable : eUnitCommandProperty.Enable;

                            msg = string.Format(TranslationManager.Instance.Translate("Conveyor") + "[{0}]\n{1} -> {2}\n" +
                                                TranslationManager.Instance.Translate("Change").ToString() + "?",
                                                cv.ControlName,
                                                TranslationManager.Instance.Translate(curSt.ToString()).ToString(),
                                                TranslationManager.Instance.Translate(destSt.ToString()).ToString());
                            //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                            msgbox = new MessageBoxPopupView(msg, "컨베이어 데이터 변경 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                                cv.ControlName, curSt.ToString(), destSt.ToString(), true);

                            mBoxResult = msgbox.ShowResult();

                            if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                            {
                                changedProperty = destSt;

                                //230207 추가 s [ServerClient]
                                if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                                {
                                    ClientSetProcedure(destSt, eServerClientType.Client);
                                }
                                else
                                {
                                    await Task.Run(() =>    //20230217 RGJ 포트 컨베이어 모드 변경 비동기 방식으로 변경. 모드 변경에 문제가 있으면 UI Hang 걸림 방지.
                                    {
                                        //20230314 RGJ 포트 Enable 은 현재 포트 상태를 참고해서 변경함.
                                        //cvLine.ChangeAllPortUseType(changedProperty.Equals(eUnitCommandProperty.Enable));
                                        cvLine.ChangeAllPortUseType(destSt == eUnitCommandProperty.Enable);
                                    });
                                    msg = string.Format(TranslationManager.Instance.Translate("Conveyor").ToString() + "[{0}]\n{1} -> {2}\n" + 
                                                        TranslationManager.Instance.Translate("Change").ToString() + 
                                                        TranslationManager.Instance.Translate("Complete").ToString(),
                                                        cv.ControlName,
                                                        TranslationManager.Instance.Translate(curSt.ToString()).ToString(),
                                                        TranslationManager.Instance.Translate(destSt.ToString()).ToString());
                                    //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                                    MessageBoxPopupView.Show(msg, "컨베이어 데이터 변경완료 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                                        cv.ControlName, curSt.ToString(), destSt.ToString(), true);

                                    EnableState = curSt;
                                }
                                //230207 추가 e [ServerClient]
                            }
                        }
                        #endregion
                        break;
                    case eUnitCommandProperty.AutoRun:
                    case eUnitCommandProperty.ManualRun:
                        #region
                        {
                            eUnitCommandProperty curSt = !cmdProperty.Equals(eUnitCommandProperty.AutoRun) ?
                                eUnitCommandProperty.AutoRun : eUnitCommandProperty.ManualRun;
                            eUnitCommandProperty destSt = cmdProperty.Equals(eUnitCommandProperty.AutoRun) ?
                                eUnitCommandProperty.AutoRun : eUnitCommandProperty.ManualRun;

                            buttonname = destSt.ToString();

                            LogManager.WriteOperatorLog(string.Format("사용자가 컨베이어 {0}의 수동지시 {1} 을/를 Click하였습니다.", cv.ControlName, buttonname),
                                "CLICK", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 12,
                                cv.ControlName, buttonname);

                            msg = string.Format(TranslationManager.Instance.Translate("Conveyor") + "[{0}]\n{1} -> {2}\n" +
                                                TranslationManager.Instance.Translate("Change").ToString() + "?",
                                                cv.ControlName,
                                                TranslationManager.Instance.Translate(curSt.ToString()).ToString(),
                                                TranslationManager.Instance.Translate(destSt.ToString()).ToString());
                            //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                            msgbox = new MessageBoxPopupView(msg, "컨베이어 데이터 변경 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                                cv.ControlName, curSt.ToString(), destSt.ToString(), true);

                            mBoxResult = msgbox.ShowResult();

                            if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                            {
                                changedProperty = destSt;

                                //230207 추가 s [ServerClient]
                                if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                                {
                                    ClientSetProcedure(destSt, eServerClientType.Client);
                                }
                                else
                                {
                                    await Task.Run(() =>    //20230217 RGJ 포트 컨베이어 모드 변경 비동기 방식으로 변경. 모드 변경에 문제가 있으면 UI Hang 걸림 방지.
                                    {
                                        cv.SetAutoMode(changedProperty.Equals(eUnitCommandProperty.AutoRun) ? eCVAutoManualState.AutoRun : eCVAutoManualState.ManualRun);
                                    });

                                    msg = string.Format(TranslationManager.Instance.Translate("Conveyor") + "[{0}]\n{1} -> {2}\n" +
                                                        TranslationManager.Instance.Translate("Change") +
                                                        TranslationManager.Instance.Translate("Complete"),
                                                        cv.ControlName,
                                                        TranslationManager.Instance.Translate(curSt.ToString()).ToString(),
                                                        TranslationManager.Instance.Translate(destSt.ToString()).ToString());
                                    //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                                    MessageBoxPopupView.Show(msg, "컨베이어 데이터 변경완료 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                                        cv.ControlName, curSt.ToString(), destSt.ToString(), true);

                                    AutoManualState = curSt;
                                }
                                //230207 추가 e [ServerClient]
                            }
                        }
                        #endregion
                        break;
                    case eUnitCommandProperty.TrackPause:
                    case eUnitCommandProperty.TrackResume:
                        #region
                        {
                            eUnitCommandProperty curSt = !cmdProperty.Equals(eUnitCommandProperty.TrackPause) ?
                                eUnitCommandProperty.TrackPause : eUnitCommandProperty.TrackResume;
                            eUnitCommandProperty destSt = cmdProperty.Equals(eUnitCommandProperty.TrackPause) ?
                                eUnitCommandProperty.TrackPause : eUnitCommandProperty.TrackResume;

                            buttonname = destSt.ToString();

                            LogManager.WriteOperatorLog(string.Format("사용자가 컨베이어 {0}의 수동지시 {1} 을/를 Click하였습니다.", cv.ControlName, buttonname),
                                "CLICK", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 12,
                                cv.ControlName, buttonname);

                            msg = string.Format(TranslationManager.Instance.Translate("Conveyor").ToString() + "[{0}]\n{1} -> {2}\n" +
                                                TranslationManager.Instance.Translate("Change").ToString() + "?",
                                                cv.ControlName,
                                                TranslationManager.Instance.Translate(curSt.ToString()).ToString(),
                                                TranslationManager.Instance.Translate(destSt.ToString()).ToString());
                            //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                            msgbox = new MessageBoxPopupView(msg, "컨베이어 데이터 변경 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                                cv.ControlName, curSt.ToString(), destSt.ToString(), true);

                            mBoxResult = msgbox.ShowResult();

                            if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                            {
                                changedProperty = destSt;

                                //230207 추가 s [ServerClient]
                                if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                                {
                                    ClientSetProcedure(destSt, eServerClientType.Client);
                                }
                                else
                                {
                                    await Task.Run(() =>    //20230217 RGJ 포트 컨베이어 모드 변경 비동기 방식으로 변경. 모드 변경에 문제가 있으면 UI Hang 걸림 방지.
                                    {
                                        cv.SetTrackPause(changedProperty.Equals(eUnitCommandProperty.TrackPause) ? true : false);
                                    });

                                    msg = string.Format(TranslationManager.Instance.Translate("Conveyor").ToString() + "[{0}]\n{1} -> {2}\n" +
                                                        TranslationManager.Instance.Translate("Change").ToString() +
                                                        TranslationManager.Instance.Translate("Complete").ToString(),
                                                        cv.ControlName,
                                                        TranslationManager.Instance.Translate(curSt.ToString()).ToString(),
                                                        TranslationManager.Instance.Translate(destSt.ToString()).ToString());
                                    //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                                    MessageBoxPopupView.Show(msg, "컨베이어 데이터 변경완료 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                                        cv.ControlName, curSt.ToString(), destSt.ToString(), true);

                                    TrackPauseState = curSt;
                                }
                                //230207 추가 e [ServerClient]
                            }
                        }
                        #endregion
                        break;
                    case eUnitCommandProperty.Write:
                        #region 미구현

                        #endregion
                        break;
                    case eUnitCommandProperty.BcrRead:
                        #region
                        buttonname = "BCR Read";

                        LogManager.WriteOperatorLog(string.Format("사용자가 컨베이어 {0}의 수동지시 {1} 을/를 Click하였습니다.", cv.ControlName, buttonname),
                            "CLICK", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 12,
                            cv.ControlName, buttonname);

                        if (cv.UseBCR)
                        {
                            msg = string.Format(TranslationManager.Instance.Translate("Conveyor") + "[{0}]\n" +
                                                TranslationManager.Instance.Translate("BCR Read").ToString() + "?",
                                                cv.ControlName);
                            //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                            msgbox = new MessageBoxPopupView(msg, "컨베이어 BCR요청 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                                cv.ControlName, "", "", true);

                            mBoxResult = msgbox.ShowResult();

                            if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                            {
                                //230207 추가 s [ServerClient]
                                if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                                {
                                    ClientSetProcedure(cmdProperty, eServerClientType.Client);
                                }
                                else
                                {
                                    string bcrresult = cv.CVBCR_Read();

                                    msg = string.Format(TranslationManager.Instance.Translate("Conveyor").ToString() + "[{0}]\n" +
                                                        TranslationManager.Instance.Translate("BCR Result").ToString() + "\n{1}",
                                                        cv.ControlName, bcrresult);
                                    //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                                    MessageBoxPopupView.Show(msg, "컨베이어 BCR완료 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                                        cv.ControlName, "BCR Result", bcrresult, true);
                                }
                                //230207 추가 e [ServerClient]
                            }
                        }
                        else
                        {
                            //BCR이 없는 유닛에서 BCR 리퀘스트를 눌린것
                            msg = string.Format(TranslationManager.Instance.Translate("Conveyor").ToString() + "[{0}]\n" +
                                                TranslationManager.Instance.Translate("BCR Not Exist").ToString(),
                                                cv.ControlName);
                            //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                            MessageBoxPopupView.Show(msg, "컨베이어 BCR완료 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                                cv.ControlName, "BCR Not Exist", "", true);
                        }
                        #endregion
                        break;
                    case eUnitCommandProperty.DirectionInMode:
                    case eUnitCommandProperty.DirectionOutMode:
                    case eUnitCommandProperty.DirectionBothMode:
                        #region
                        buttonname = "Port Type";

                        LogManager.WriteOperatorLog(string.Format("사용자가 컨베이어 {0}의 수동지시 {1} 을/를 Click하였습니다.", cv.ControlName, buttonname),
                            "CLICK", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 12,
                            cv.ControlName, buttonname);

                        ModeChangeInOutPopupView modechangeView = new ModeChangeInOutPopupView(cv, false);

                        if (modechangeView is null)
                        {
                            msg = string.Format("[{0}] " + TranslationManager.Instance.Translate("Command Error").ToString(), cmdProperty.ToString());
                            MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }

                        ePortInOutType changedType = modechangeView.ResultPortInOutType();

                        setPortType(changedType, cv);//SuHwan_20230405 : 포트타입 체인지 한곳에서 처리

                        #endregion
                        break;
                    //공용
                    case eUnitCommandProperty.Install:
                        #region
                        buttonname = cmdProperty.ToString();

                        LogManager.WriteOperatorLog(string.Format("사용자가 컨베이어 {0}의 수동지시 {1} 을/를 Click하였습니다.", cv.ControlName, buttonname),
                            "CLICK", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 12,
                            cv.ControlName, buttonname);

                        if (cv.CarrierExistBySensor())
                        {
                            //데이터 입력 팝업은 그냥 나오고 데이터 입력 팝업에서 확인을 눌리면 확인해주는 팝업을 띄워준다.
                            CarrierInstall ci = new CarrierInstall(cv.ControlName);

                            CarrierItem carrier = ci.ResultCarrierItem();

                            if (carrier != null)
                            {
                                msg = string.Format(TranslationManager.Instance.Translate("Conveyor").ToString() + "[{0}]\n" +
                                                    TranslationManager.Instance.Translate("CarrierID").ToString() + "[{1}]\n{2}?",
                                                    cv.ControlName, carrier.CarrierID, TranslationManager.Instance.Translate(cmdProperty.ToString()).ToString());
                                //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                                msgbox = new MessageBoxPopupView(msg, "컨베이어 캐리어ID 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                                    cv.ControlName, carrier.CarrierID, cmdProperty.ToString(), true);

                                mBoxResult = msgbox.ShowResult();

                                if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                                {
                                    //230207 추가 s [ServerClient]
                                    if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                                    {
                                        ClientSetProcedure(cmdProperty, eServerClientType.Client, carrier);
                                    }
                                    else
                                    {
                                        if(GlobalData.Current.CarrierStore.CarrierContain(carrier.CarrierID) == false)//스토리지에 화물이 없으면 넣어야 함.
                                        {
                                            GlobalData.Current.CarrierStore.InsertCarrier(carrier);
                                        }
                                        cv.UpdateCarrier(carrier.CarrierID);
                                        msg = string.Format(TranslationManager.Instance.Translate("Conveyor").ToString() + "[{0}]\n" +
                                                            TranslationManager.Instance.Translate("CarrierID").ToString() + "[{1}]\n{2}" +
                                                            TranslationManager.Instance.Translate("Complete").ToString(),
                                                            cv.ControlName, carrier.CarrierID, TranslationManager.Instance.Translate(cmdProperty.ToString()).ToString());
                                        //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                                        MessageBoxPopupView.Show(msg, "컨베이어 캐리어ID추가 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                                            cv.ControlName, carrier.CarrierID, "Complete", true);

                                        InstallState = eUnitCommandProperty.Delete;
                                    }
                                    //230207 추가 e [ServerClient]
                                }
                            }
                        }
                        else
                        {
                            //혹시나 캐리어가 없으면 인스톨 모드로 변경해준다.
                            InstallState = eUnitCommandProperty.Delete;
                            msg = string.Format(TranslationManager.Instance.Translate("Conveyor").ToString() + "[{0}]\n" +
                                                TranslationManager.Instance.Translate("Carrier Exist").ToString() + "\n" +
                                                TranslationManager.Instance.Translate("Change").ToString() + "{1}" +
                                                TranslationManager.Instance.Translate("Mode").ToString(),
                                                cv.ControlName, TranslationManager.Instance.Translate(InstallState.ToString()).ToString());
                            //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                            MessageBoxPopupView.Show(msg, "컨베이어 캐리어ID추가 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Error,
                                cv.ControlName, "EXIST", "Fail", true);
                        }
                        #endregion
                        break;
                    case eUnitCommandProperty.Delete:
                        #region
                        buttonname = cmdProperty.ToString();

                        LogManager.WriteOperatorLog(string.Format("사용자가 컨베이어 {0}의 수동지시 {1} 을/를 Click하였습니다.", cv.ControlName, buttonname),
                            "DELETE", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 12,
                            cv.ControlName, buttonname);

                        if (cv.CheckCarrierExist())
                        {
                            string carrierid = cv.GetCarrierID();
                            //데이터 입력 팝업은 그냥 나오고 데이터 입력 팝업에서 확인을 눌리면 확인해주는 팝업을 띄워준다.
                            msg = string.Format(TranslationManager.Instance.Translate("Conveyor") + "[{0}]\n" + 
                                                TranslationManager.Instance.Translate("CarrierID").ToString() + "[{1}]\n{2}?",
                                                cv.ControlName, carrierid, TranslationManager.Instance.Translate(cmdProperty.ToString()).ToString());
                            //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                            msgbox = new MessageBoxPopupView(msg, "컨베이어 캐리어ID 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                                cv.ControlName, carrierid, cmdProperty.ToString(), true);

                            mBoxResult = msgbox.ShowResult();

                            if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                            {
                                //230207 추가 s [ServerClient]
                                if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                                {
                                    ClientSetProcedure(cmdProperty, eServerClientType.Client);
                                }
                                else
                                {
                                    if (cv.IsTerminalPort && cv.PortInOutType == ePortInOutType.OUTPUT)
                                    {
                                        cv.RemoveSCSCarrierData(); //도메인에서 지우는건 끝단이고 배출 포트일때만 삭제해야함.
                                    }
                                    else
                                    {
                                        cv.ResetCarrierData();
                                    }

                                    msg = string.Format(TranslationManager.Instance.Translate("Conveyor").ToString() + "[{0}]\n" +
                                                        TranslationManager.Instance.Translate("CarrierID").ToString() + "[{1}]\n{2} " +
                                                        TranslationManager.Instance.Translate("Complete").ToString(),
                                                        cv.ControlName, carrierid, TranslationManager.Instance.Translate(cmdProperty.ToString()).ToString());
                                    //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                                    MessageBoxPopupView.Show(msg, "컨베이어 캐리어ID삭제 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                                        cv.ControlName, carrierid, "Complete", true);

                                    InstallState = eUnitCommandProperty.Install;
                                }
                            }
                        }
                        else
                        {
                            //혹시나 캐리어가 없으면 인스톨 모드로 변경해준다.
                            InstallState = eUnitCommandProperty.Install;
                            msg = string.Format(TranslationManager.Instance.Translate("Conveyor").ToString() + "[{0}]\n" +
                                                TranslationManager.Instance.Translate("Carrier Not Exist").ToString() + "\n" +
                                                TranslationManager.Instance.Translate("Change").ToString() + "{1} " +
                                                TranslationManager.Instance.Translate("Mode").ToString(),
                                                cv.ControlName, TranslationManager.Instance.Translate(InstallState.ToString()).ToString());
                            //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                            MessageBoxPopupView.Show(msg, "컨베이어 캐리어ID삭제 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Error,
                                cv.ControlName, "Carrier Not Exist", "Fail", true);
                        }
                        #endregion
                        break;
                    case eUnitCommandProperty.Detail:
                        #region
                        buttonname = cmdProperty.ToString();

                        LogManager.WriteOperatorLog(string.Format("사용자가 컨베이어 {0}의 수동지시 {1} 을/를 Click하였습니다.", cv.ControlName, buttonname),
                            "CLICK", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 12,
                            cv.ControlName, buttonname);
                        GlobalData.Current.ViewModelWindowOpenRequest(eOpenWindowName.eUnitDetail, cv, IsPlayBackControl);
                        bcommand = false;
                        #endregion
                        break;
                    case eUnitCommandProperty.Inform:
                        //230331 HHJ SCS 개선     //- FireShutter 추가      //Test Code
                        buttonname = cmdProperty.ToString();

                        LogManager.WriteOperatorLog(string.Format("사용자가 컨베이어 {0}의 수동지시 {1} 을/를 Click하였습니다.", cv.ControlName, buttonname),
                            "CLICK", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 12,
                            cv.ControlName, buttonname);

                        bcommand = false;
                        break;
                    case eUnitCommandProperty.KeyIN:
                        {
                            buttonname = "KEY IN";
                            LogManager.WriteOperatorLog(string.Format("사용자가 컨베이어 {0}의 수동지시 {1} 을/를 Click하였습니다.", cv.ControlName, buttonname),
                                "KEYIN", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 12,
                                cv.ControlName, buttonname);

                            //220225 조숭진 key in은 클라이언트만 대응
                            if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                            {
                                CarrierKeyIn cki = new CarrierKeyIn(cv);
                                CarrierItem carrier = cki.ResultCarrierItem();

                                if (carrier != null)
                                {
                                    //msg = string.Format(TranslationManager.Instance.Translate("Key Send 메시지").ToString(), cv.ControlName);
                                    //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);

                                    msg = TranslationManager.Instance.Translate("Key Send 메시지").ToString();
                                    msg = string.Format(msg, cv.ControlName);

                                    msgbox = new MessageBoxPopupView("Info Message", "", msg, "", MessageBoxButton.OKCancel, MessageBoxImage.Question, "Key Send 메시지", cv.ControlName, "", true);

                                    mBoxResult = msgbox.ShowResult();

                                    if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                                    {
                                        ClientSetProcedure(cmdProperty, eServerClientType.Client, carrier); //서버로 키인하라고 명령 보낸다.
                                    }
                                }
                            }
                            else
                            {
                                CarrierKeyIn cki = new CarrierKeyIn(cv);
                                CarrierItem carrier = cki.ResultCarrierItem();
                                cv.SetKeyInCarrierItem(carrier);
                            }
                        }
                        break;
                }

                if (bcommand && mBoxResult != null &&
                    mBoxResult.Result.Equals(MessageBoxResult.OK))
                {
                    LogManager.WriteOperatorLog(string.Format("사용자가 컨베이어 {0}의 수동지시 {1} 을/를 Write하였습니다.", cv.ControlName, buttonname),
                        "WRITE", buttonname, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 20,
                        cv.ControlName, buttonname);
                }

            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        //230207 추가 s [ServerClient]
        private void ClientSetProcedure(eUnitCommandProperty cmdProperty, eServerClientType type, CarrierItem carrier = null)
        {
            //if (GlobalData.Current.ServerClientType != eServerClientType.Client)
            //    return;

            //PlayBackControl은 진행되면 안됨.
            if (IsPlayBackControl)
                return;

            //각 모듈 베이스로 변환. 변환값 없으면 진행불가.
            CV_BaseModule cv = SelectUnit as CV_BaseModule;
            if (cv is null)
                return;

            ClientReqList reqBuffer = new ClientReqList
            {
                EQPID = GlobalData.Current.EQPID,
                CMDType = cmdProperty.ToString(),
                Target = procedureUnitType.ToString(),
                TargetID = cv.ModuleName,
                TargetValue = string.Empty,
                ReqTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                Requester = type,
                JobID = string.Empty,
            };

            if (cmdProperty.Equals(eUnitCommandProperty.Install) || cmdProperty.Equals(eUnitCommandProperty.KeyIN))
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
                CV_BaseModule cv = SelectUnit as CV_BaseModule;
                if (cv is null)
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
                        foreach (var item in cv.PCtoPLC.Values)
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
                        foreach (var item in cv.PLCtoPC.Values)
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
                //PlayBackControl은 진행되면 안됨.
                if (IsPlayBackControl)
                    return;

                rcvDetailValue = rcvDetailValue?.Trim(); //입력값의 앞뒤 공백을 제거한다.
                rcvValue = rcvValue?.Trim(); //입력값의 앞뒤 공백을 제거한다.

                //각 모듈 베이스로 변환. 변환값 없으면 진행불가.
                CV_BaseModule cv = SelectUnit as CV_BaseModule;
                if (cv is null)
                    return;

                if (SelectItem == null)
                    return;

                if (SelectItem.ItemName.Equals("PC_CarrierID"))
                {
                    if(GlobalData.Current.ShelfMgr.CheckCarrierDuplicated(rcvValue)) //쉘프에 이미 존재하면 해당 아이디를 입력하면 안된다.
                    {
                        msg = string.Format("Carrier ID : {0}   Already Existed In Shelf.", rcvValue);

                        msgbox = new MessageBoxPopupView(msg, "Already Existed.", "", "", MessageBoxButton.OK, MessageBoxImage.Warning);

                        mBoxResult = msgbox.ShowResult();
                        return;
                    }
                }

                //포트 인아웃 타입은 따로 
                if (SelectItem.ItemName == "PLC_PortType")
                {
                    setPortType((ePortInOutType)Convert.ToInt32(rcvValue), cv);//SuHwan_20230405 : 포트타입 체인지 한곳에서 처리
                    LogManager.WriteOperatorLog(string.Format("사용자가 {0} {1}에 {2} 을/를 Write하였습니다.", SelectUnit.ControlName, SelectItem.ItemName, rcvValue),
                        "WRITE", SelectItem.ItemName, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 10,
                        SelectUnit.ControlName, SelectItem.ItemName, rcvValue);
                    return;
                }

                msgItemName = string.IsNullOrEmpty(rcvDetailValue) ? rcvValue : rcvDetailValue;

                msg = string.Format(TranslationManager.Instance.Translate("Conveyor").ToString() + "[{0}] {1}\n [{2}]\n"+
                                    TranslationManager.Instance.Translate("Change").ToString() + "?",
                                    cv.ControlName, SelectItem.ItemName, msgItemName);
                //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                msgbox = new MessageBoxPopupView(msg, "컨베이어 IO 변경 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                    cv.ControlName, SelectItem.ItemName, msgItemName, true);

                mBoxResult = msgbox.ShowResult();

                if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                {
                    //230301 클라이언트에서 io변경 요청대응
                    if (GlobalData.Current.ServerClientType == eServerClientType.Server)
                    {
                        //if(SelectItem.Area == eAreaType.PCtoPLC)
                            GlobalData.Current.protocolManager.Write(cv.ModuleName, cv.PCtoPLC, SelectItem.ItemName, rcvValue);
                        //else if (SelectItem.Area == eAreaType.PLCtoPC)
                        //    GlobalData.Current.protocolManager.Write(cv.ModuleName, cv.PLCtoPC, SelectItem.ItemName, rcvValue);

                        if (SelectItem.ItemName.Equals("PC_CarrierID"))
                        {
                            var carrierItem = CarrierStorage.Instance.GetInModuleCarrierItem(cv.ModuleName);

                            //간혹 로봇인터페이스 컨베이어가 stop되어있는 상태에서 컨베이어 plc수동조작으로 인해 화물이 들어올 경우가 있다.
                            //이럴때는 캐리어 데이터가 없기때문에 데이터 수정 시 exception이 발생된다.
                            //이를 방지하기 위해 임의의 캐리어를 생성해준다.
                            if (carrierItem == null)
                            {
                                CarrierItem InPortCarrier = CarrierStorage.Instance.CreatePortUnknownCarrier(cv);
                                carrierItem = InPortCarrier;
                            }

                            string beforeCarrierID = carrierItem.CarrierID;
                            eCarrierState beforestate = carrierItem.CarrierState;
                            carrierItem.CarrierID = rcvValue;

                            bool insertcheck = CarrierStorage.Instance.InsertCarrier(carrierItem);
                            if (insertcheck)
                            {
                                cv.UpdateCarrier(rcvValue);
                                CarrierStorage.Instance.RemoveStorageCarrier(beforeCarrierID);
                                carrierItem.CarrierState = beforestate;
                            }
                        }
                        else
                        {
                            var cvCarrier = cv.InSlotCarrier;
                            if (cvCarrier != null) //230914 RGJ IO Set Null 체크 추가.
                            {
                                switch (SelectItem.ItemName)
                                {
                                    case "PC_PalletSize":
                                        Enum.TryParse(rcvValue, out ePalletSize palletsize);
                                        cvCarrier.PalletSize = palletsize;
                                        break;
                                    case "PC_ProductEmpty":
                                        Enum.TryParse(rcvValue, out eProductEmpty productempty);
                                        cvCarrier.ProductEmpty = productempty;
                                        break;
                                    case "PC_Polarity":
                                        Enum.TryParse(rcvValue, out ePolarity polarity);
                                        cvCarrier.Polarity = polarity;
                                        break;
                                    case "PC_WinderDirection":
                                        Enum.TryParse(rcvValue, out eWinderDirection winderdirection);
                                        //carrierItem.WinderDirection = winderdirection;
                                        cvCarrier.WinderDirection = winderdirection;
                                        break;
                                    case "PC_InnerTrayType":
                                        Enum.TryParse(rcvValue, out eInnerTrayType innertraytype);
                                        //carrierItem.InnerTrayType = innertraytype;
                                        cvCarrier.InnerTrayType = innertraytype;
                                        break;
                                    case "PC_TrayType":
                                        Enum.TryParse(rcvValue, out eTrayType traytype);
                                        //carrierItem.TrayType = traytype;
                                        cvCarrier.TrayType = traytype;
                                        break;
                                }
                                cv.UpdateCarrier(cv.GetCarrierID());
                            }
   
                        }
                    }
                    else
                    {
                        ClientSetProcedure_IO(cv.ModuleName, SelectItem, rcvValue);
                    }

                    msg = string.Format(TranslationManager.Instance.Translate("Conveyor").ToString() + "[{0}] {1}\n [{2}]\n" +
                                        TranslationManager.Instance.Translate("Change").ToString() + " " +
                                        TranslationManager.Instance.Translate("Complete").ToString(),
                                        cv.ControlName, SelectItem.ItemName, msgItemName);
                    //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                    MessageBoxPopupView.Show(msg, "컨베이어 IO 변경완료 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                        cv.ControlName, SelectItem.ItemName, msgItemName, true);

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

        //SuHwan_20230405 : 포트타입 체인지 한곳에서 처리
        /// <summary>
        /// 포트타입 변경 하는곳
        /// </summary>
        /// <param name="rcvChangedType">ePortInOutType</param>
        /// <param name="rcvSelectCV">CV_BaseModule</param>
        public async void setPortType(ePortInOutType rcvChangedType, CV_BaseModule rcvSelectCV)
        {
            string msg = string.Empty;
            MessageBoxPopupView msgbox = null;
            CustomMessageBoxResult mBoxResult = null;

            CVLineModule cvLine = rcvSelectCV.ParentModule as CVLineModule;
            //240730 RGJ 포트라인내 화물이 있으면 포트 타입 변경 불가. 화물이 존재하는 포트만 안바뀌는 현상 방지 
            if (cvLine.CheckCarrierExistInLine())
            {
                msg = string.Format(TranslationManager.Instance.Translate("Conveyor").ToString() + " " +
                                   TranslationManager.Instance.Translate("InOutMode").ToString() + " " +
                                   TranslationManager.Instance.Translate("Can't Change").ToString() + " " +
                                   TranslationManager.Instance.Translate("Carrier Exist").ToString());

                MessageBoxPopupView.Show(msg, "컨베이어 데이터 변경완료 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Warning,
                    rcvSelectCV.ControlName, rcvSelectCV.PortInOutType.ToString(), rcvChangedType.ToString(), true);
                return;
            }
            //동일하면 처리하지 않는다.
            if (rcvSelectCV.PortInOutType.Equals(rcvChangedType))
            {
                msg = string.Format(TranslationManager.Instance.Translate("Current").ToString() + " " +
                                    TranslationManager.Instance.Translate("Conveyor").ToString() + " " +
                                    TranslationManager.Instance.Translate("InOutMode").ToString() + "[{0}]\n" +
                                    TranslationManager.Instance.Translate("Change").ToString() + " " +
                                    TranslationManager.Instance.Translate("Conveyor").ToString() + " " +
                                    TranslationManager.Instance.Translate("InOutMode").ToString() + "[{1}]\n" +
                                    TranslationManager.Instance.Translate("Check Select Value").ToString(),
                                    TranslationManager.Instance.Translate(rcvSelectCV.PortInOutType.ToString()).ToString(),
                                    TranslationManager.Instance.Translate(rcvChangedType.ToString()).ToString());
                //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Stop, false);
                MessageBoxPopupView.Show(msg, "컨베이어 데이터 변경완료 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                    rcvSelectCV.ControlName, rcvSelectCV.PortInOutType.ToString(), rcvChangedType.ToString(), true);
                return;
            }
            //In, Out, Both중에 있다면 처리한다.
            if (rcvChangedType >= ePortInOutType.INPUT && rcvChangedType <= ePortInOutType.BOTH)
            {
                msg = string.Format(TranslationManager.Instance.Translate("Conveyor").ToString() + "[{0}] " +
                                    TranslationManager.Instance.Translate("InOutMode").ToString() + "\n[{1}] -> [{2}]\n" +
                                    TranslationManager.Instance.Translate("Change").ToString() + "?",
                                    rcvSelectCV.ControlName,
                                    TranslationManager.Instance.Translate(rcvSelectCV.PortInOutType.ToString()).ToString(),
                                    TranslationManager.Instance.Translate(rcvChangedType.ToString()).ToString());
                //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                msgbox = new MessageBoxPopupView(msg, "컨베이어 데이터 변경 메시지", "", "", MessageBoxButton.OKCancel, MessageBoxImage.Question,
                    rcvSelectCV.ControlName, rcvSelectCV.PortInOutType.ToString(), rcvChangedType.ToString(), true);

                ePortInOutType prevPortInOutType = rcvSelectCV.PortInOutType;

                mBoxResult = msgbox.ShowResult();

                if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                {
                    //230207 추가 s [ServerClient]
                    if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                    {
                        switch (rcvChangedType)
                        {
                            case ePortInOutType.INPUT:
                                ClientSetProcedure(eUnitCommandProperty.DirectionInMode, eServerClientType.Client);
                                break;
                            case ePortInOutType.OUTPUT:
                                ClientSetProcedure(eUnitCommandProperty.DirectionOutMode, eServerClientType.Client);
                                break;
                            case ePortInOutType.BOTH:
                                ClientSetProcedure(eUnitCommandProperty.DirectionBothMode, eServerClientType.Client);
                                break;
                        }
                    }
                    else
                    {
                        bool ChangeComp = false;
                        await Task.Run(() =>    //20230217 RGJ 포트 컨베이어 모드 변경 비동기 방식으로 변경. 모드 변경에 문제가 있으면 UI Hang 걸림 방지.
                        {
                            ChangeComp = cvLine.ChangeAllPortInOutType(rcvChangedType);
                        });

                        switch (rcvSelectCV.PortInOutType)
                        {
                            case ePortInOutType.INPUT:
                                PortInOutModeState = eUnitCommandProperty.DirectionInMode;
                                break;
                            case ePortInOutType.OUTPUT:
                                PortInOutModeState = eUnitCommandProperty.DirectionOutMode;
                                break;
                            case ePortInOutType.BOTH:
                                PortInOutModeState = eUnitCommandProperty.DirectionBothMode;
                                break;
                        }

                        if (ChangeComp)
                        {
                            msg = string.Format(TranslationManager.Instance.Translate("Conveyor").ToString() + "[{0}] " +
                                                TranslationManager.Instance.Translate("InOutMode").ToString() + "\n[{1}]\n" +
                                                TranslationManager.Instance.Translate("Change").ToString() + " " +
                                                TranslationManager.Instance.Translate("Complete").ToString(),
                                                rcvSelectCV.ControlName,
                                                TranslationManager.Instance.Translate(rcvSelectCV.PortInOutType.ToString()).ToString());
                            //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                            MessageBoxPopupView.Show(msg, "컨베이어 데이터 변경완료 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                                rcvSelectCV.ControlName, prevPortInOutType.ToString(), rcvSelectCV.PortInOutType.ToString(), true);
                        }
                        else
                        {
                            msg = string.Format(TranslationManager.Instance.Translate("Conveyor").ToString() + "[{0}] " +
                                                TranslationManager.Instance.Translate("InOutMode").ToString() + "\n[{1}]\n" +
                                                TranslationManager.Instance.Translate("Change").ToString() + " " +
                                                TranslationManager.Instance.Translate("Fail").ToString(),
                                                rcvSelectCV.ControlName,
                                                TranslationManager.Instance.Translate(rcvChangedType.ToString()).ToString());
                            //MessageBoxPopupView.Show(msg, MessageBoxButton.OK, MessageBoxImage.Information);
                            MessageBoxPopupView.Show(msg, "컨베이어 데이터 변경완료 메시지", "", "", MessageBoxButton.OK, MessageBoxImage.Information,
                                rcvSelectCV.ControlName, prevPortInOutType.ToString(), rcvSelectCV.PortInOutType.ToString(), true);
                        }
                    }
                }
            }
        }


        public void CloseView()
        {
            CloseThread = true;
            viewModelthread.Join();
        }
    }
}
