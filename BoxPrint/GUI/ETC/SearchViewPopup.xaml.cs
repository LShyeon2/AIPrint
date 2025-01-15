using BoxPrint.DataList;
using BoxPrint.GUI.ViewModels;
using BoxPrint.GUI.Views;
using BoxPrint.GUI.Views.UserPage;
using BoxPrint.Log;
using BoxPrint.Modules;
using BoxPrint.Modules.Conveyor;
using BoxPrint.Modules.Shelf;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.ETC
{
    /// <summary>
    /// SearchViewPopup.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SearchViewPopup : Window
    {
        private ViewModelSearchView vm { get; set; }

        //220928 HHJ SCS 개선     //- CarrierSearchView Window Manual Move 기능 추가
        //public SearchViewPopup()
        public SearchViewPopup(LayOutView layout)
        {
            InitializeComponent();

            //230102 YSW 사용자 권한에 따른 버튼 잠금
            ModifyAuthorityCheck();
            LogInPopupView._EventHandler_LoginChange += ModifyAuthorityCheck;
            GroupAccountManagementPage._EventHandler_ChangeAuthority += ModifyAuthorityCheck;

            //220928 HHJ SCS 개선     //- CarrierSearchView Window Manual Move 기능 추가
            //vm = new ViewModelSearchView();
            vm = new ViewModelSearchView(layout);
            DataContext = vm;

        }

        //230119 YSW 수정권한 잠금
        public void ModifyAuthorityCheck()
        {
            if (GlobalData.Current.LoginUserAuthority.Contains("ModifyCarrierSearch"))
            {
                ModifyAuthorityDockPanel.IsHitTestVisible = true;
                ModifyAuthorityDockPanel.Opacity = 1;
                LockIcon.Visibility = Visibility.Collapsed;
            }
            else
            {
                ModifyAuthorityDockPanel.IsHitTestVisible = false;
                ModifyAuthorityDockPanel.Opacity = 0.3;
                LockIcon.Visibility = Visibility.Visible;
            }
        }

        private void SearchButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            string strkey = vm.SearchText;

            if (string.IsNullOrEmpty(strkey))
                return;

            List<SearchViewModelData> search = vm.CarrierListData.Where(r => r.CarrierID.ToString().Contains(strkey)).ToList();

            vm.SearchListData = new ObservableCollection<SearchViewModelData>(search);

            if (sender is Border bd)
            {
                if (bd.Child is TextBlock txt)
                {
                    LogManager.WriteOperatorLog(string.Format("사용자가 {0}의 {1} 을/를 Click하였습니다.", this.Tag, txt.Tag),
                        "CLICK", txt.Tag, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 7,
                        this.Tag, txt.Tag);
                }
            }

        }

        private void RadioButtonCheckedChange(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton rbtn)
            {
                if (!(bool)rbtn.IsChecked)
                {
                    //Checked가 False라면 속해있는 데이터를 초기화 시켜준다.
                    if (rbtn.Tag.ToString().Equals("Port"))
                    {
                        cbbPort.SelectedIndex = -1;
                    }
                    else
                    {
                        vm.DestShelf = string.Empty;
                        cbbBank.SelectedIndex = -1;
                        cbbBay.SelectedIndex = -1;
                        cbbLevel.SelectedIndex = -1;
                    }
                }
            }
        }

        private void BankBayLevelChange(object sender, SelectionChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(vm.DestBank) || string.IsNullOrEmpty(vm.DestBay) || string.IsNullOrEmpty(vm.DestLevel))
                return;
            else
            {
                if (int.TryParse(vm.DestBank, out int bank) && int.TryParse(vm.DestBay, out int bay) && int.TryParse(vm.DestLevel, out int level))
                {
                    vm.DestShelf = ShelfTagHelper.GetTag(bank, bay, level);
                }
            }
        }

        private void RefreshClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                //리프레시는 해당 페이지의 전체항목을 초기화 시킨다.
                vm.SearchText = string.Empty;
                vm.SearchListData = new ObservableCollection<SearchViewModelData>();
                vm.Destination = string.Empty;
                PortRadioButton.IsChecked = false;
                ShelfRadioButton.IsChecked = false;
                priorityUpDown.InitNumericUpDown(1);
                GlobalData.Current.CarrierStore.RefreshStorageUI(); //다시 UI 업데이트 하도록 함.
                if (sender is Border bd)
                {
                    if (bd.Child is TextBlock txt)
                    {
                        LogManager.WriteOperatorLog(string.Format("사용자가 {0}의 {1} 을/를 Click하였습니다.", this.Tag, txt.Tag),
                            "CLICK", txt.Tag, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 7,
                            this.Tag, txt.Tag);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
            
        }
        private void UpdateClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                if (sender is Border bd)
                {
                    if (bd.Child is TextBlock txt)
                    {
                        LogManager.WriteOperatorLog(string.Format("사용자가 {0}의 {1} 을/를 Click하였습니다.", this.Tag, txt.Tag),
                            "CLICK", txt.Tag, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 7,
                            this.Tag, txt.Tag);
                    }
                }

                if (vm.SelValue is null)
                {
                    return;
                }
                string OldLoc = vm.SelValue.Location.ToString();
                int priority = priorityUpDown.Value;
                string CarrierID = vm.SelValue.CarrierID.ToString();
                string msg = string.Empty;
                msg = string.Format(TranslationManager.Instance.Translate("Carrier ID").ToString() + " : {0}\r\n" +
                                    TranslationManager.Instance.Translate("기존 위치").ToString() + " : {1}  =>\r\n" +
                                    TranslationManager.Instance.Translate("새 위치").ToString() + " : {2}\r\n\r\n" +
                                    TranslationManager.Instance.Translate("캐리어 위치를 강제 변경하시겠습니까?").ToString(),
                                    CarrierID, OldLoc, vm.Destination);
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
                        CMDType = "MCHANGECLOC",
                        Target = "CARRIER",
                        TargetID = CarrierID,
                        TargetValue = vm.Destination,
                        ReqTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        Requester = eServerClientType.Client,
                    };
                    GlobalData.Current.DBManager.DbSetProcedureClientReq(reqBuffer.EQPID, reqBuffer.CMDType, reqBuffer.Target, reqBuffer.TargetID, reqBuffer.TargetValue, reqBuffer.ReqTime, reqBuffer.Requester);
                    MessageBoxPopupView MBPV = new MessageBoxPopupView("Request has been sent successfully.", MessageBoxButton.OK, MessageBoxImage.Information);
                    MBPV.ShowResult();

                    return;
                }
                else //Server 에서 실행할경우
                {
                    eCarrierLocationChangeResult result = GlobalData.Current.CarrierStore.ChangeCarrierLocation(CarrierID, vm.Destination);
                    if(result == eCarrierLocationChangeResult.SUCCESS)
                    {
                        MessageBoxPopupView MBPV = new MessageBoxPopupView(TranslationManager.Instance.Translate("캐리어 위치가 해당 위치로 강제 설정 되었습니다.").ToString(), MessageBoxButton.OK, MessageBoxImage.Information);
                        MBPV.ShowResult();
                    }
                    else
                    {
                        string failmsg = string.Format("Location Change Failed.\r\nReason : {0}", result);
                        MessageBoxPopupView MBPV = new MessageBoxPopupView(failmsg, MessageBoxButton.OK, MessageBoxImage.Error);
                        MBPV.ShowResult();
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                try
                {
                    MessageBoxPopupView MBPV = new MessageBoxPopupView("Carrier updating failed. Please retry.", MessageBoxButton.OK, MessageBoxImage.Stop, false);
                    MBPV.ShowResult();
                    LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                }
                catch (Exception)
                {
                    ;
                }
            }

        }

        //220914 HHJ SCS 개선     //- Move 버튼 추가
        private void DeleteClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                if (sender is Border bd)
                {
                    if (bd.Child is TextBlock txt)
                    {
                        LogManager.WriteOperatorLog(string.Format("사용자가 {0}의 {1} 을/를 Click하였습니다.", this.Tag, txt.Tag),
                            "CLICK", txt.Tag, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 7,
                            this.Tag, txt.Tag);
                    }
                }
                if(vm.SelValue == null)
                {
                    return;
                }
                string RemoveTargetID = vm.SelValue.CarrierID.ToString();
                string CarrierLoc = vm.SelValue.Location.ToString();
                //포트에 있는 화물 또는 로케이션 없는 화물만 삭제 가능.

                if (string.IsNullOrEmpty(CarrierLoc) || GlobalData.Current.PortManager.IsCVContains(CarrierLoc))
                {
                    string msg = string.Format("The Carrier {0} will be removed.\r\nProceed it?", RemoveTargetID);

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
                            CMDType = "MREMOVECARRIER",
                            Target = "CARRIER",
                            TargetID = RemoveTargetID,
                            TargetValue = String.Empty,
                            ReqTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                            Requester = eServerClientType.Client,
                        };

                        GlobalData.Current.DBManager.DbSetProcedureClientReq(reqBuffer.EQPID, reqBuffer.CMDType, reqBuffer.Target, reqBuffer.TargetID, reqBuffer.TargetValue, reqBuffer.ReqTime, reqBuffer.Requester);

                        MessageBoxPopupView MBPV = new MessageBoxPopupView("Command has been Sent successfully.", MessageBoxButton.OK, MessageBoxImage.Information);
                        MBPV.ShowResult();
                        return;
                    }
                    else //Server 에서 실행할경우
                    {
                        CarrierStorage.Instance.RemoveStorageCarrier(RemoveTargetID);
                        mbox = new MessageBoxPopupView(string.Format("Carrier :{0} removed", RemoveTargetID), MessageBoxButton.OK, MessageBoxImage.Information);
                        mbox.ShowResult();
                    }
                }
                else
                {
                    string msg = string.Format("Carrier : {0}  cannot be removed.Check loacation", RemoveTargetID);

                    MessageBoxPopupView mbox = new MessageBoxPopupView(msg, MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    CustomMessageBoxResult mResult = mbox.ShowResult();
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
            
        }

        private void RunClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                if (sender is Border bd)
                {
                    if (bd.Child is TextBlock txt)
                    {
                        LogManager.WriteOperatorLog(string.Format("사용자가 {0}의 {1} 을/를 Click하였습니다.", this.Tag, txt.Tag),
                            "CLICK", txt.Tag, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 7,
                            this.Tag, txt.Tag);
                    }
                }

                if (vm.SelValue is null)
                {
                    return;
                }
                ICarrierStoreAble source = GlobalData.Current.GetGlobalCarrierStoreAbleObject(vm.SelValue.Location.ToString());
                if (!string.IsNullOrEmpty(vm.DestPort)) //231006 RGJ 콤보박스 선택을 우선으로 한다.
                {
                    vm.Destination = vm.DestPort;
                }
                else if (!string.IsNullOrEmpty(vm.DestShelf))
                {
                    vm.Destination = vm.DestShelf;
                }
                ICarrierStoreAble dest = GlobalData.Current.GetGlobalCarrierStoreAbleObject(vm.Destination);
                if (source == null || dest == null)
                {
                    return;
                }
                int priority = priorityUpDown.Value;
                string CarrierID = vm.SelValue.CarrierID.ToString();
                string msg = string.Empty;
                msg = string.Format(TranslationManager.Instance.Translate("Carrier ID").ToString() + " : {0}\r\n" +
                                    TranslationManager.Instance.Translate("Source").ToString() + " : {1}  =>\r\n" +
                                    TranslationManager.Instance.Translate("Dest").ToString() + " : {2}\r\n\r\n" +
                                    TranslationManager.Instance.Translate("반송 커맨드를 실행하시겠습니까?").ToString(),
                                    CarrierID, source.iLocName, dest.iLocName);
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
                        TargetValue = string.Format("{0}/{1}/{2}/{3}/{4}/{5}", source.GetTagName(), dest.GetTagName(), CarrierID, priority, "FALSE", 0),
                        ReqTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                        Requester = eServerClientType.Client,
                    };

                    GlobalData.Current.DBManager.DbSetProcedureClientReq(reqBuffer.EQPID, reqBuffer.CMDType, reqBuffer.Target, reqBuffer.TargetID, reqBuffer.TargetValue, reqBuffer.ReqTime, reqBuffer.Requester);

                    MessageBoxPopupView MBPV = new MessageBoxPopupView(TranslationManager.Instance.Translate("Job has been Sent successfully.").ToString(), MessageBoxButton.OK, MessageBoxImage.Information);
                    MBPV.ShowResult();


                    //요롷게 사용
                    //GlobalData.Current.McdList.CreateManualJob_formDB(reqBuffer.TargetValue);

                    return;
                }
                else //Server 에서 실행할경우
                {
                    eManualJobCreateResult result = GlobalData.Current.McdList.CreateManualTransferJob(source, dest, vm.SelValue.CarrierID.ToString(), priority, false, 0);
                    if (result == eManualJobCreateResult.Success)
                    {
                        MessageBoxPopupView MBPV = new MessageBoxPopupView(TranslationManager.Instance.Translate("Job has been created successfully.").ToString(), MessageBoxButton.OK, MessageBoxImage.Information);
                        MBPV.ShowResult();
                        return;
                    }
                    else
                    {
                        Log.LogManager.WriteConsoleLog(eLogLevel.Info, "CreateManualTransferJob Failed. Reason:{0}", result);
                        MessageBoxPopupView MBPV = new MessageBoxPopupView(TranslationManager.Instance.Translate("Job creating failed. Check source or destination state.").ToString() + "\n" + result, MessageBoxButton.OK, MessageBoxImage.Stop, false);
                        MBPV.ShowResult();
                        return;
                    }
                }
            }
            catch(Exception ex)
            {
                try
                {
                    MessageBoxPopupView MBPV = new MessageBoxPopupView("Job creating failed. Please retry again.", MessageBoxButton.OK, MessageBoxImage.Stop, false);
                    MBPV.ShowResult();
                    LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
                }
                catch(Exception)
                {
                    ;
                }
            }
        }
        //220621 HHJ SCS 개선     //- Manual 개선
        //- 리스트에서 더블클릭하면 서칭 화면서 나오도록 변경
        private void CarrierListView_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                SearchViewModelData selected = vm.SelValue;
                //DoubleClick하면 Search 항목을 초기화 하고, 더블클릭한 항목으로 변경해준다.
                vm.SearchListData = new ObservableCollection<SearchViewModelData>();
                vm.SelValue = selected;
                vm.SearchListData.Add(vm.SelValue);
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }
        //라디오 버튼을 선택 후 콤보박스 선택하는게 아닌 콤보박스 선택시 라디오 버튼도 설정 되도록 변경
        private void cbbPort_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            PortRadioButton.IsChecked = true;
        }

        private void cbbBankBayLevel_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ShelfRadioButton.IsChecked = true;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            //220928 HHJ SCS 개선     //- CarrierSearchView Window Manual Move 기능 추가
            //지속적으로 쓰레드로 Carrier 정보 리딩 및 변환작업으로 인해 CPU 부하가 높아짐 이벤트 처리로 변경.

            //240827 RGJ 메모리 누수방지 윈도우 종료시 이벤트 구독 해제
            LogInPopupView._EventHandler_LoginChange -= ModifyAuthorityCheck;
            GroupAccountManagementPage._EventHandler_ChangeAuthority -= ModifyAuthorityCheck;
            vm.DeleteChanged();
            vm = null;
        }

        private void CarrierListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CarrierListView.ScrollIntoView(CarrierListView.SelectedItem);
            CarrierIDListView.ScrollIntoView(vm?.SelValue);
        }

        //2024.05.06 lim, SKOn 요청 사항 Enter 키로 검색
        private void SearchTextBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                SearchButtonDown(Button_Search, null);
            }
        }
    }
}
