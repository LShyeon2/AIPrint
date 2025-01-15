using Newtonsoft.Json;
using BoxPrint.DataList;
using BoxPrint.DataList.MCS;
using BoxPrint.GUI.ETC;
using BoxPrint.GUI.ETC.LoadingPopup;
using BoxPrint.GUI.EventCollection;
using BoxPrint.GUI.UIControls;
using BoxPrint.GUI.ViewModels;
using BoxPrint.Log;
using BoxPrint.Modules.Conveyor;
using BoxPrint.Modules.RM;
using BoxPrint.Modules.Shelf;
using BoxPrint.Modules.User;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.Views
{
    public partial class LayOutView : Page
    {
        #region Variable
        #region Window
        private CraneManualCommand cmc = null;
        private UnitIODetailView unitDetailView = null;
        private CraneOrderView craneOrder = null;
        #endregion

        private List<string> ListHeader = new List<string>();
        DispatcherTimer timer = new DispatcherTimer();    //객체생성
        bool LoadComp = false;

        public ViewModelLayOutView vm { get; private set; }
        #endregion

        public delegate void EventHandler_ShowInTaskbar();
        public static event EventHandler_ShowInTaskbar _EventHandler_ShowInTaskbar;

        #region Constructor, Timer
        public LayOutView()
        {
            InitializeComponent();

            beginAnimationDeviceMap("Close");

            vm = new ViewModelLayOutView();
            
            //SuHwan_20230110 : [1차 UI검수]
            vm.UIFontSize_Large = 14;
            vm.UIFontSize_Medium = 12;
            vm.UIFontSize_Small = 10;

            DataContext = vm;

            //SuHwan_20221018 : 위치 변경
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500); //UI 갱신에 부하가 많아서 딜레이 수정 30 -> 100 
            timer.Tick += new EventHandler(timer_Tick);

            GlobalData.Current.OnViewModelWindowOpen += ViewModelWindowOpenAction;


            vm.LayOutControl.vm.OnLayOutUnitSelect += OnLayOutUnitSelect;
            UIEventCollection.Instance.OnCultureChanged += _UIEventCollection_OnCultureChanged;

            //20230711 사용자 권한에 따른 버튼 잠금
            GlobalData.Current.UserMng.OnLoginUserChange += UserMng_OnLoginUserChange;

            _EventHandler_ShowInTaskbar();
        }

        private void _UIEventCollection_OnCultureChanged(string cultureKey)
        {
            InitControl();
            if(bcr_content == null) btn_BcrFlag.Content = TranslationManager.Instance.Translate("BCR TestMode : [0] Normal").ToString();
            else btn_BcrFlag.Content = TranslationManager.Instance.Translate(bcr_content).ToString();
        }

        private void OnLayOutUnitSelect(UIControlBase selectUnit, bool rightClick)
        {
            if (rightClick)
            {
                //craneOrder가 null이거나 craneOrder가 null이 아니라면 IsVislble이 false여야 한다.
                if (craneOrder is null || (!(craneOrder is null) && !craneOrder.IsVisible))
                    LayOutUnit_RightClickSelectControl(selectUnit);
            }

            //230719 HHJ SCS 개선     //- IO View Open상태에서 Shelf Click시 IO View 자동 Close
            if (!(selectUnit is UIControlCV || selectUnit is UIControlRM))
            {
                if (btnDeviceMapOpen.IsSelect)
                    DeviceMapButton_Click(btnDeviceMapOpen, null);
            }

            //231024 HHJ
            //Crane Manual Command Window Open 상태에서 RM을 선택하면 해당 RM으로 RM을 변경해준다.
            if (!(craneOrder is null) && selectUnit is UIControlRM)
            {
                craneOrder.ChangeRM(selectUnit.ControlName);
            }
        }

        /// <summary>
        /// 페이지 리로드
        /// </summary>
        public void page_reLoaded()
        {
            GlobalData.Current.ParameterSet(GlobalData.Current.nParameterList);
            LoadComp = false;
            Page_Loaded(null, null);
        }
        /// <summary>
        /// 페이지 로드
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!LoadComp)
                {
                    vm.ControlSystemEqpID = GlobalData.Current.EQPID;
                    InitControl();
                    
                    LogManager.WriteConsoleLog(eLogLevel.Error, "Layout LoadComp");
                    LoadComp = true;
                    //GlobalData.Current.LayoutLoadComp = true;
                    timer.Start();
                }

                if (timer.IsEnabled == false)
                {
                    timer.Tick += timer_Tick;
                    timer.Start();
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            timer.Stop();
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (this.IsVisible)
                {
                    if (LodingPopup.Instance.Visibility == Visibility.Visible)
                    {
                        LodingPopup.Instance.AutoStop();

                    }


                    //SuHwan_20230317 : 알람 확인
                    vm.AlarmExist = GlobalData.Current.Alarm_Manager.CheckAlarmExist();
                    vm.ShelfCountDataUpdate = true;         // 240809 RGJ 메인화면 좌상단 캐리어 카운트 조회 업데이트 통합

                    lable_SimulTransferCount.Text = GlobalData.Current.TransferCounter.ToString();
                    lable_SimulPushCount.Text     = GlobalData.Current.PushCounter.ToString();
                    lable_SimulHandOverCount.Text = GlobalData.Current.HandOverCounter.ToString();
                    lable_SimulWithDrawCount.Text = GlobalData.Current.WithDrawCounter.ToString();

                    //int OccupiedShelfCount = ShelfManager.Instance.GetOccupiedShelfCount();
                    //int FullCarrierCount = ShelfManager.Instance.GetFullCarrierCount();
                    //int EmptyCarrierCount = ShelfManager.Instance.GetEmptyCarrierCount();

                    //lable_LoadRate.Text = string.Format("{0:0.00}%  ({1}/{2})", ShelfManager.Instance.GetShelfLoadRatio(), OccupiedShelfCount, ShelfManager.Instance.GetAvailableShelfCount());
                    //lable_UNKCount.Text = string.Format("{0}", ShelfManager.Instance.GetUNKIDShelfCount());
                    //lable_FullCount.Text = string.Format("Full : {0} ({1}/{2})", (double)FullCarrierCount / (double)OccupiedShelfCount, FullCarrierCount, OccupiedShelfCount);
                    //lable_EmptyCount.Text = string.Format("Empty : {0} ({1}/{2})", (double)EmptyCarrierCount / (double)OccupiedShelfCount, EmptyCarrierCount, OccupiedShelfCount);

                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Error, ex.ToString());
            }

        }
        #endregion

        #region Event
        private void ViewModelWindowOpenAction(eOpenWindowName windowname, ControlBase control, bool IsPlayBack)
        {
            try
            {
                if (!IsPlayBack.Equals(vm.IsPlayBackControl))
                    return;

                switch (windowname)
                {
                    case eOpenWindowName.eCraneManualJob:
                        if (cmc is null)
                        {
                            cmc = new CraneManualCommand(control.ControlName, this);
                            cmc.Owner = Application.Current.MainWindow;
                            cmc.WindowStartupLocation = WindowStartupLocation.Manual;
                            cmc.Show();
                        }
                        else
                        {
                            if (!cmc.IsVisible)
                            {
                                cmc = null;
                                cmc = new CraneManualCommand(control.ControlName, this);
                                cmc.Owner = Application.Current.MainWindow;
                                cmc.WindowStartupLocation = WindowStartupLocation.Manual;
                                cmc.Show();
                            }
                            //IsVisible이 true 상태라면 팝업이 켜져있다는건데 이 경우 RM만 변경시켜준다.
                            else
                            {
                                cmc.ChangeRM(control.ControlName);
                            }
                        }
                        break;
                    case eOpenWindowName.eUnitDetail:
                        if (unitDetailView is null)
                        {
                            unitDetailView = new UnitIODetailView(control, vm.IsPlayBackControl);
                            unitDetailView.Owner = Application.Current.MainWindow;
                            unitDetailView.WindowStartupLocation = WindowStartupLocation.Manual;
                            unitDetailView.Show();
                        }
                        else
                        {
                            if (!unitDetailView.IsVisible)
                            {
                                unitDetailView = null;
                                unitDetailView = new UnitIODetailView(control, vm.IsPlayBackControl);
                                unitDetailView.Owner = Application.Current.MainWindow;
                                unitDetailView.WindowStartupLocation = WindowStartupLocation.Manual;
                                unitDetailView.Show();
                            }
                        }
                        break;
                    case eOpenWindowName.eCraneOrder:
                        if (craneOrder is null)
                        {
                            craneOrder = new CraneOrderView(control.ControlName, this);
                            craneOrder.Owner = Application.Current.MainWindow;
                            craneOrder.WindowStartupLocation = WindowStartupLocation.Manual;
                            craneOrder.Show();
                        }
                        else
                        {
                            if (!craneOrder.IsVisible)
                            {
                                craneOrder = null;
                                craneOrder = new CraneOrderView(control.ControlName, this);
                                craneOrder.Owner = Application.Current.MainWindow;
                                craneOrder.WindowStartupLocation = WindowStartupLocation.Manual;
                                craneOrder.Show();
                            }
                            //IsVisible이 true 상태라면 팝업이 켜져있다는건데 이 경우 RM만 변경시켜준다.
                            else
                            {
                                craneOrder.ChangeRM(control.ControlName);
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        #region TestCommand Event
        private void btn_tempJob_Click(object sender, RoutedEventArgs e)
        {
            GlobalData.Current.McdList.CreateRandomJob();
        }
        private void btn_HandOffJob_Click(object sender, RoutedEventArgs e)
        {
            GlobalData.Current.McdList.CreateHandOffJob();
        }
        private void btn_CycleJob_Click(object sender, RoutedEventArgs e)
        {
            bool CurrentMode = GlobalData.Current.Scheduler.CycleRandomJobRequest;
            string msg = string.Format("사이클 작업을 {0} 하시겠습니까?", CurrentMode ? "정지" : "시작");
            msg = TranslationManager.Instance.Translate(msg).ToString();

            //SuHwan_20230320 : 메시지 박스 통합
            MessageBoxPopupView msgbox = new MessageBoxPopupView(TranslationManager.Instance.Translate("Info Message").ToString(), "", msg, "", MessageBoxButton.YesNo, MessageBoxImage.Question);
            CustomMessageBoxResult mBoxResult = msgbox.ShowResult();

            //MessageBoxResult result = System.Windows.MessageBox.Show(msg, "Info Message", MessageBoxButton.YesNo);
            string btn_content = string.Empty;
            if (mBoxResult.Result == MessageBoxResult.Yes)
            {
                btn_content = CurrentMode ? "사이클 작업 시작" : "사이클 작업 정지";
                btn_CycleJob.Content = TranslationManager.Instance.Translate(btn_content).ToString();
                GlobalData.Current.Scheduler.SetRandomCycleJobMode(!CurrentMode);//토글 시킨다.
            }
        }
        private void btn_ClearAllJob_Click(object sender, RoutedEventArgs e)
        {
            string msg = TranslationManager.Instance.Translate("전체 작업을 삭제 하시겠습니까?").ToString();
            //MessageBoxResult result = System.Windows.MessageBox.Show(msg, "Info Message", MessageBoxButton.YesNo);

            //SuHwan_20230320 : 메시지 박스 통합
            MessageBoxPopupView msgbox = new MessageBoxPopupView(TranslationManager.Instance.Translate("Info Message").ToString(), "", msg, "", MessageBoxButton.YesNo, MessageBoxImage.Question);
            CustomMessageBoxResult mBoxResult = msgbox.ShowResult();

            if (mBoxResult.Result == MessageBoxResult.Yes)
            {
                GlobalData.Current.McdList.DeleteALLMcsJob();
            }
        }
        private void btn_PortGetMode_Click(object sender, RoutedEventArgs e)
        {
            bool CurrentMode = GlobalData.Current.Scheduler.UsePortGet;

            string msg = string.Format("Port Get 작업을 {0} 하시겠습니까?", CurrentMode ? "중단" : "시작");
            msg = TranslationManager.Instance.Translate(msg).ToString();
            //MessageBoxResult result = System.Windows.MessageBox.Show(msg, "Info Message", MessageBoxButton.YesNo);
            //SuHwan_20230320 : 메시지 박스 통합
            MessageBoxPopupView msgbox = new MessageBoxPopupView(TranslationManager.Instance.Translate("Info Message").ToString(), "", msg, "", MessageBoxButton.YesNo, MessageBoxImage.Question);
            CustomMessageBoxResult mBoxResult = msgbox.ShowResult();
            string btn_content = string.Empty;
            if (mBoxResult.Result == MessageBoxResult.Yes)
            {
                btn_content = CurrentMode ? "포트 로드 시작" : "포트 로드 중단";
                btn_PortGetMode.Content = TranslationManager.Instance.Translate(btn_content).ToString();
                GlobalData.Current.Scheduler.SetUsePortGet(!CurrentMode);//토글 시킨다.
            }
        }
        private void btn_UnloadTest_Click(object sender, RoutedEventArgs e)
        {
            bool CurrentMode = GlobalData.Current.Scheduler.AllShelfUnloadJobRequest;
            string msg = string.Format("전체 쉘프 언로드 작업을 {0} 하시겠습니까?", CurrentMode ? "정지" : "시작");
            msg = TranslationManager.Instance.Translate(msg).ToString();
            //MessageBoxResult result = System.Windows.MessageBox.Show(msg, "Info Message", MessageBoxButton.YesNo);
            //SuHwan_20230320 : 메시지 박스 통합
            MessageBoxPopupView msgbox = new MessageBoxPopupView(TranslationManager.Instance.Translate("Info Message").ToString(), "", msg, "", MessageBoxButton.YesNo, MessageBoxImage.Question);
            CustomMessageBoxResult mBoxResult = msgbox.ShowResult();
            string btn_content = string.Empty;
            if (mBoxResult.Result == MessageBoxResult.Yes)
            {
                btn_content = CurrentMode ? "전체 쉘프 언로드" : "전체 쉘프 언로드 중단";
                btn_UnloadTest.Content = TranslationManager.Instance.Translate(btn_content).ToString();
                GlobalData.Current.Scheduler.SetAllShelfUnloadJobRequest(!CurrentMode);//토글 시킨다.
            }
        }
        private void btn_RMFlagReset_Click(object sender, RoutedEventArgs e)
        {
            //System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            //stopwatch.Start(); // 시간측정 시작
            //GlobalData.Current.PortManager.GetCVModule("AI11").PC_CarrierID = "TestID9999";
            //var cItem = GlobalData.Current.PortManager.GetCVModule("AI11").ReadTrackingData();
            //stopwatch.Stop(); //시간측정 끝
            //LogManager.WriteConsoleLog(eLogLevel.Info, "Call Time :{0} ms", stopwatch.ElapsedMilliseconds);
            //LogManager.WriteConsoleLog(eLogLevel.Info, "PLC Data : {0}", cItem.CarrierID);
            GlobalData.Current.mRMManager.FirstRM.CarrierExistSensor = false;
            if (GlobalData.Current.mRMManager.SecondRM != null)
            {
                GlobalData.Current.mRMManager.SecondRM.CarrierExistSensor = false;
            }
            
        }
        private void btn_BCRReadTest_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CV_BaseModule cv = GlobalData.Current.PortManager.GetCVModule(vm.SelectUnitID);
                if (cv != null && cv.UseBCR)
                {
                    string value = cv.CVBCR_Read();
                    LogManager.WriteConsoleLog(eLogLevel.Info, "ReadValue :{0}", value);
                }
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }

        string bcr_content;
        private void btn_BcrFlag_Click(object sender, RoutedEventArgs e)
        {
            string msg = string.Empty;
            string ModeDesc = string.Empty;
            GlobalData.Current.SetToggleCVBCRTestMode();
            int Mode = GlobalData.Current.GetSimulCVBCRTestMode();
            if (Mode == 0)
            {
                ModeDesc = "Normal";
            }
            else if (Mode == 1)
            {
                ModeDesc = "Read Fail";
            }
            else if (Mode == 2)
            {
                ModeDesc = "Read Duplicate";
            }
            msg = string.Format("BCR TestMode : [{0}] {1}", Mode, ModeDesc);
            bcr_content = msg;
            btn_BcrFlag.Content = TranslationManager.Instance.Translate(msg).ToString();
        }
        #endregion
        #endregion

        #region Create, init
        private void GdSetHeader(string tag, DataGrid Dg)
        {
            List<GridItemListItemInfo> lsititem = GlobalData.Current.GetGridItemList(tag);

            Dg.Columns.Clear();

            foreach (var item in lsititem)
            {
                DataGridTextColumn addedcol = new DataGridTextColumn();

                //220328 HHJ SCS 개발     //- LayoutView DataGrid Theme 공유 수정
                //addedcol.HeaderStyle = GetStyle(true);
                //addedcol.CellStyle = GetStyle(false);

                if (item.GridItem.Contains("\\"))        //\ 있다면 \를 기준으로 띄워쓰기 해준다.
                {
                    addedcol.Header = TranslationManager.Instance.Translate(item.GridItem.Replace("\\", "\n")).ToString().ToUpper();
                }
                else if(item.GridItem == "No")
                    addedcol.Header = TranslationManager.Instance.Translate("NumberOrder").ToString();
                else
                    addedcol.Header = TranslationManager.Instance.Translate(item.GridItem).ToString().ToUpper();

                //220609 HHJ SCS 개선     //- Shelf UIControl 변경
                //확실하지 않은 바인딩 아이템은 전부 빈값으로 처리하여 바인딩 되지않도록 한다.
                //addedcol.Binding = new Binding(item.BindingItem);
                if (!string.IsNullOrEmpty(item.BindingItem))
                    addedcol.Binding = new Binding(item.BindingItem);

                //220322 HHJ SCS 개발     //- DataGrid Header Style 변경
                //컬럼이 데이터 그리드에 꽉 차게 만들어준다.
                //addedcol.Width = item.GridWidth;
                addedcol.Width = new DataGridLength(item.GridWidth, DataGridLengthUnitType.Star);
                addedcol.IsReadOnly = true;

                Dg.Columns.Add(addedcol);
                ListHeader.Add(addedcol.Header.ToString());
            }
        }

        private void InitControl()
        {
            GdSetHeader(eSectionHeader.McsJob.ToString(), DGMcsJobManager);
        }
        #endregion

        #region 디바이스맵 기능 모음
        //SuHwan_20230103 : 디바이스맵 버튼 클릭 
        private void DeviceMapButton_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                if (sender is SK_ButtonControl senderBuffer)
                {
                    //선택된 유닛이 없으면 진행하지않는다.
                    if (vm.IOView is null)
                        return;

                    //에니 설정
                    senderBuffer.IsSelect = senderBuffer.IsSelect ? false : true;
                    string aniName = senderBuffer.IsSelect ? "Open" : "Close";
                    beginAnimationDeviceMap(aniName);
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }

        }

        //SuHwan_20230103 : 디바이스맵 애니 설정
        private void beginAnimationDeviceMap(string rcvName)
        {
            Storyboard storyboardBuffer = new Storyboard();
            DoubleAnimation doubleAnimationHeight = new DoubleAnimation { Duration = TimeSpan.FromSeconds(0.2) };
            DoubleAnimation doubleAnimationWidth = new DoubleAnimation { Duration = TimeSpan.FromSeconds(0.2) };

            switch (rcvName)
            {
                case "Open":
                    doubleAnimationHeight.To = vm.IOView.ActualHeight + gridDeviceMapMain.MinHeight;
                    doubleAnimationWidth.To = vm.IOView.ActualWidth + 5;
                    break;
                default:
                    doubleAnimationHeight.To = gridDeviceMapMain.MinHeight;
                    doubleAnimationWidth.To = gridDeviceMapMain.MinWidth;
                    break;
            }

            gridDeviceMapMain.BeginAnimation(HeightProperty, doubleAnimationHeight);
            gridDeviceMapMain.BeginAnimation(WidthProperty, doubleAnimationWidth);
        }

        //SuHwan_20230103 : 디바이스맵 프레임 사이즈 변경 체크
        private void gridDeviceMapFrame_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (btnDeviceMapOpen.IsSelect)
                beginAnimationDeviceMap("Open");
        }
        #endregion

        private void LayOutUnit_RightClickSelectControl(UIControlBase control)
        {
            if (control is UIControlShelf shelf)
            {
                ShelfItem item = GlobalData.Current.ShelfMgr.GetShelf(shelf.UnitName);
                
                shelf.ContextMenu = (ContextMenu)this.Resources["ShelfContextMenu"];

                //220411 HHJ SCS 개선     //- 컨텍스트 메뉴 선택 쉘프 태그 디스플레이 추가
                if (shelf.ContextMenu.Items[0] is MenuItem mi)
                {
                    //220509 HHJ SCS 개선     //- ShelfControl 변경
                    //mi.Header = shelf.ToolTip.ToString();
                    mi.Header = "_" + shelf.UnitName;
                    mi.FontSize = 15;
                }

                //bool bDeadZoneFind = false;
                //foreach (var v in shelf.ContextMenu.Items)
                //{
                //    if (v is Separator)
                //        continue;

                //    if (v is MenuItem mitem)
                //    {
                //        if (mitem.Tag != null && mitem.Tag.ToString().Equals("DeadZone"))
                //        {
                //            bDeadZoneFind = true;
                //            break;
                //        }
                //    }
                //}

                ////220506 HHJ SCS 개선     //- DeadZone 설정 추가
                //if (GlobalData.Current.UserMng.CurrentUser != null
                //    && GlobalData.Current.UserMng.CurrentUser.UserLevel == eUserLevel.Admin)
                //{
                //    if (!bDeadZoneFind)
                //    {
                //        MenuItem mitem = new MenuItem()
                //        {
                //            Header = "DeadZone",
                //            Tag = "DeadZone",
                //        };
                //        mitem.Click += ShelfMenuItem_Click;
                //        shelf.ContextMenu.Items.Add(new Separator());
                //        shelf.ContextMenu.Items.Add(mitem);
                //    }
                //}
                ////220525 HHJ SCS 개선     //- ShelfDead 설정 관련 추가
                //else
                //{
                //    if (bDeadZoneFind)
                //    {
                //        shelf.ContextMenu.Items.RemoveAt(shelf.ContextMenu.Items.Count - 2);
                //        shelf.ContextMenu.Items.RemoveAt(shelf.ContextMenu.Items.Count - 1);
                //    }
                //}

                shelf.ContextMenu.IsOpen = true;
            }
        }
        /// <summary>
        /// Shelf Context Item 처리
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ShelfMenuItem_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                MenuItem mitem = sender as MenuItem;

                if (mitem != null)
                {
                    string tag = vm.SelectUnitID;
                    string msg;
                    MessageBoxPopupView msgbox = null;
                    string InformMemoString = string.Empty;     //230405 HHJ SCS 개선     //- Memo 기능 추가

                    //230405 HHJ SCS 개선     //- Memo 기능 추가      //위치 이동
                    ShelfItem shelfitem = GlobalData.Current.ShelfMgr.GetShelf(tag);

                    if (shelfitem == null)
                    {
                        return;
                    }
                    if (shelfitem.Scheduled) //20230406 RGJ SCS 정지대신 쉘프 작업 예약 상태로 인터락 완화
                    {
                        MessageBoxPopupView RejectMB = new MessageBoxPopupView(TranslationManager.Instance.Translate("Shelf is already job scheduled.").ToString(), MessageBoxButton.OK, MessageBoxImage.Stop, false);
                        RejectMB.Show();
                        return;
                    }

                    switch (mitem.Tag.ToString())
                    {
                        case "Install":
                        case "Delete":
                            msg = string.Format(TranslationManager.Instance.Translate("캐리어설치/삭제 메시지").ToString(),
                                                TranslationManager.Instance.Translate(mitem.Tag.ToString()).ToString(),
                                                tag);
                            //msg = string.Format("Do you want to {0} a Carrier in {1}?", mitem.Tag.ToString(), tag);
                            msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                            break;
                        case "Enable":
                        case "Disable":
                            msg = string.Format(TranslationManager.Instance.Translate("쉘프사용/미사용 메시지").ToString(),
                                                TranslationManager.Instance.Translate(mitem.Tag.ToString()).ToString(),
                                                tag);
                            //msg = string.Format("Do you want to {0} {1}?", mitem.Tag.ToString(), tag);
                            msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                            break;

                        case "DeadZone":
                            msg = string.Format(TranslationManager.Instance.Translate("DeadZone설정 메시지").ToString(),
                                                tag);
                            msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                            break;
                        //230405 HHJ SCS 개선     //- Memo 기능 추가
                        case "Comment":
                            InformMemoView informMemo = new InformMemoView(shelfitem.ShelfMemo);
                            ResultInformMemo result = informMemo.InformMemoString();

                            //Cancel으로 나오면 뒷처리 없음.
                            if (result.InformMemoResult.Equals(eResultInformMemo.eCancel))
                                return;

                            InformMemoString = result.InformMemoString;
                            //msg = string.Format(TranslationManager.Instance.Translate("Comment입력 메시지").ToString(),
                            //                    string.IsNullOrEmpty(InformMemoString) ? "Clear" : InformMemoString, tag);

                            //msgbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);

                            msg = TranslationManager.Instance.Translate("Comment입력 메시지").ToString();
                            msg = string.Format(msg,
                                string.IsNullOrEmpty(InformMemoString) ? TranslationManager.Instance.Translate("Clear").ToString() : InformMemoString,
                                tag);

                            msgbox = new MessageBoxPopupView("Info Message", "", msg, "", MessageBoxButton.OKCancel, MessageBoxImage.Question, "Comment입력 메시지", string.IsNullOrEmpty(InformMemoString) ? "Clear" : InformMemoString, tag, true);
                            break;
                        default:
                            return;
                    }

                    CustomMessageBoxResult mBoxResult = msgbox.ShowResult();

                    if (mBoxResult.Result.Equals(MessageBoxResult.OK))
                    {
                        //SuHwan_20221107 : [ServerClient] 클라이언트 명령 전용
                        if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                        {
                            ClientReqList reqBuffer = new ClientReqList
                            {
                                EQPID = GlobalData.Current.EQPID,
                                CMDType = mitem.Tag.ToString(),
                                Target = "Shelf",
                                TargetID = shelfitem.TagName,
                                TargetValue = string.Empty,
                                ReqTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                                Requester = eServerClientType.Client,
                            };

                            switch (mitem.Tag.ToString())
                            {
                                case "Install":
                                    CarrierInstall ci = new CarrierInstall(shelfitem.TagName);
                                    CarrierItem carrier = ci.ResultCarrierItem();
                                    if (carrier != null)
                                        reqBuffer.TargetValue = JsonConvert.SerializeObject(carrier);
                                    else
                                        return;
                                    break;

                                case "DeadZone":
                                    reqBuffer.TargetValue = Convert.ToString(!shelfitem.DeadZone);
                                    break;

                                default:
                                    //나머지는 따로 지정할 필요가 없는거 같음..
                                    break;
                            }

                            GlobalData.Current.DBManager.DbSetProcedureClientReq(reqBuffer.EQPID, reqBuffer.CMDType, reqBuffer.Target, reqBuffer.TargetID, reqBuffer.TargetValue, reqBuffer.ReqTime, reqBuffer.Requester);

                            return;
                        }
                        else
                        {
                            switch (mitem.Tag.ToString())
                            {
                                case "Install":
                                    CarrierInstall ci = new CarrierInstall(shelfitem.TagName);
                                    CarrierItem carrier = ci.ResultCarrierItem();

                                    if (carrier != null)
                                    {
                                        carrier.CarrierState = eCarrierState.COMPLETED;
                                        ShelfManager.Instance.GenerateCarrierRequest(tag, carrier);
                                    }
                                    else
                                    {

                                    }
                                    break;
                                case "Delete":
                                    ShelfManager.Instance.RequestCarrierRemove(tag);
                                    break;
                                case "Enable":
                                    shelfitem.SHELFUSE = true;
                                    break;
                                case "Disable":
                                    if (shelfitem.Scheduled == false)
                                    {
                                        shelfitem.SHELFUSE = false;
                                    }
                                    break;

                                case "DeadZone":
                                    shelfitem.DeadZone = !shelfitem.DeadZone;
                                    break;
                                //230405 HHJ SCS 개선     //- Memo 기능 추가
                                case "Comment":
                                    shelfitem.ShelfMemo = InformMemoString;
                                    break;
                            }
                        }

                        GlobalData.Current.ShelfMgr.SaveShelfData(shelfitem);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
            
        }

        private void btnDeviceMapOpen_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is SK_ButtonControl senderBuffer)
            {
                vm.ChangeBoothIO();
            }
        }

        private void btn_PLCSimul_Click(object sender, RoutedEventArgs e)
        {
            ScheduleDebugInfo SRB = new ScheduleDebugInfo();
            SRB.Owner = Application.Current.MainWindow; // We must also set the owner for this to work.
            SRB.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            SRB.Show();
        }

        /// <summary>
        /// 범례 더블 클릭시 테스트 메뉴 삭제 [임시 기능]
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TabItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(GlobalData.Current.ServerClientType == eServerClientType.Server && GlobalData.Current.CurrentUserID == "admin") //특수기능 메뉴 제한.
            {
                TabItem_Test.Visibility = TabItem_Test.Visibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        //20230711 사용자 권한에 따른 버튼 잠금
        private void UserMng_OnLoginUserChange(User usr)
        {
            if (!(usr is null))
            {
                if (GlobalData.Current.LoginUserAuthority.Contains("ModifyJobControl"))
                {
                    btnJobDelete.IsEnabled = true;
                    btnJobPriorityChange.IsEnabled = true;
                }
                else
                {
                    btnJobDelete.IsEnabled = false;
                    btnJobPriorityChange.IsEnabled = false;
                }
            }
            else
            {
                btnJobDelete.IsEnabled = false;
                btnJobPriorityChange.IsEnabled = false;
            }
        }
    }

    public class UniformTabPanel : UniformGrid
    {
        public UniformTabPanel()
        {
            IsItemsHost = true;
            Rows = 1;
            HorizontalAlignment = HorizontalAlignment.Stretch;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            var children = this.Children.OfType<TabItem>();
            var totalMaxWidth = children.Sum(tab => tab.MaxWidth);
            if (!double.IsInfinity(totalMaxWidth))
            {
                this.HorizontalAlignment = (constraint.Width > totalMaxWidth)
                                                    ? HorizontalAlignment.Left
                                                    : HorizontalAlignment.Stretch;
                foreach (var child in children)
                {
                    child.Width = this.HorizontalAlignment == System.Windows.HorizontalAlignment.Left
                            ? child.MaxWidth
                            : Double.NaN;
                }
            }
            return base.MeasureOverride(constraint);
        }
    }
}
