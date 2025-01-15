using BoxPrint.DataList;
//220928 HHJ SCS 개선     //- CarrierSearchView Window Manual Move 기능 추가
using BoxPrint.GUI.ViewModels;
using BoxPrint.GUI.Views;
using BoxPrint.Modules;
using BoxPrint.Modules.RM;
using BoxPrint.Modules.Shelf;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TranslationByMarkupExtension;
using WCF_LBS.Commands;

namespace BoxPrint.GUI.ETC
{
    /// <summary>
    /// CraneManualCommand.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class CraneManualCommand : Window
    {
        private ViewModelCraneManualCommand vm { get; set; }
        //RM_TPLC RM;
        public CraneManualCommand(string rm, LayOutView MainLayout)
        {
            InitializeComponent();
            //RM = TargetRM;
            //220928 HHJ SCS 개선     //- CarrierSearchView Window Manual Move 기능 추가
            #region 이전 주석
            //cb_Command.SelectedIndex = 0;
            //tb_CraneID.Text = RM.ModuleName;
            //UD_BANK.InitNumericUpDown();
            //UD_BANK.MinValue = ShelfManager.Instance.GetMinBank();
            //UD_BANK.MaxValue = ShelfManager.Instance.GetMaxBank();
            //UD_BANK.Value = UD_BANK.MinValue;

            //UD_BAY.InitNumericUpDown();
            //UD_BAY.MinValue = 0;
            //UD_BAY.MaxValue = ShelfManager.Instance.GetMaxBay() + 1;
            //UD_BAY.Value = 1;

            //UD_LEVEL.InitNumericUpDown();
            //UD_LEVEL.MinValue = ShelfManager.Instance.GettMinLevel();
            //UD_LEVEL.MaxValue = ShelfManager.Instance.GetMaxLevel();
            //UD_LEVEL.Value = UD_LEVEL.MinValue;
            //MainLayout.OnShelfSelectionChanged += MainLayout_OnShelfSelectionChanged;
            #endregion
            vm = new ViewModelCraneManualCommand(MainLayout);
            vm.SetRM(rm);
            DataContext = vm;
        }

        //220928 HHJ SCS 개선     //- CarrierSearchView Window Manual Move 기능 추가
        #region 이전 주석
        //private void MainLayout_OnShelfSelectionChanged(ShelfItem Selected)
        //{
        //    if(Selected != null)
        //    {
        //        UD_BANK.Value = Selected.iBank;
        //        UD_BAY.Value = Selected.iBay;
        //        UD_LEVEL.Value = Selected.iLevel;
        //    }
        //}
        //private void bt_CommandRun_Click(object sender, RoutedEventArgs e)
        //{
        //    eCraneCommand CMD = eCraneCommand.NONE;
        //    string CarrierID = string.Empty;
        //    if (cb_Command.SelectedItem == null)
        //    {
        //        tb_Status.Text = "Select Command.";
        //        return;
        //    }
        //    else
        //    {
        //        System.Windows.Controls.ComboBoxItem cmd = (System.Windows.Controls.ComboBoxItem)cb_Command.SelectedValue;
        //        switch (cmd.Content.ToString())
        //        {
        //            case "MOVE":
        //                CMD = eCraneCommand.MOVE;
        //                CarrierID = "ManualMove";
        //                break;
        //            case "GET":
        //                CMD = eCraneCommand.GET;
        //                break;
        //            case "PUT":
        //                CMD = eCraneCommand.PUT;
        //                break;

        //        }

        //    }

        //    bool TargetIsPort = false;
        //    //목표 대상 유효성 판단.
        //    ICarrierStoreAble target = ShelfManager.Instance.GetShelf(UD_BANK.Value, UD_BAY.Value, UD_LEVEL.Value);
        //    if (target == null)
        //    {
        //        target = GlobalData.Current.PortManager.AllCVList.Where(p => p.Position_Bank == UD_BANK.Value && p.Position_Bay == UD_BAY.Value && p.Position_Level == UD_LEVEL.Value).FirstOrDefault();
        //        if (target == null)
        //        {
        //            tb_Status.Text = "Invalid Target.";
        //            return;
        //        }
        //        else
        //        {
        //            TargetIsPort = true;
        //        }
        //    }

        //    MessageBoxPopupView mbox = new MessageBoxPopupView(string.Format("CRANE:{0} CMD:{1} BANK:{2} BAY:{3} LEVEL:{4} 커맨드를 실행하시겠습니까?", RM.ModuleName, CMD, target.iBank, target.iBay, target.iLevel), MessageBoxButton.OKCancel);
        //    CustomMessageBoxResult mResult = mbox.ShowResult();

        //    if (mResult.Result == MessageBoxResult.OK)
        //    {
        //        //현재 크레인 동작 상태 판단.
        //        if (RM.CheckRMBusy())
        //        {
        //            tb_Status.Text = "Crane is Busy.";
        //            return;
        //        }
        //        if (target != null)
        //        {
        //            //명령 입력 
        //            CraneCommand mCmd = new CraneCommand("ManualCom", RM.ModuleName, CMD, TargetIsPort ? enumCraneTarget.PORT : enumCraneTarget.SHELF, target, CarrierID);
        //            if (!RM.CheckRMBusy())
        //            {
        //                RM.SetCraneCommand(mCmd);
        //                tb_Status.Text = string.Format("CRANE:{0} CMD:{1} BANK:{2} BAY:{3} LEVEL:{4}", RM.ModuleName, CMD, target.iBank, target.iBay, target.iLevel);
        //                return;
        //            }
        //        }


        //    }

        //}
        #endregion

        private void Window_Closed(object sender, EventArgs e)
        {
            vm.DeleteChanged();
            vm = null;
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

        private void cbbPort_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            PortRadioButton.IsChecked = true;
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

        private void cbbBankBayLevel_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ShelfRadioButton.IsChecked = true;
        }

        private void MoveClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                eCraneCommand CMD = eCraneCommand.NONE;
                string CarrierID = string.Empty;
                string msg = string.Empty;

                RMModuleBase rmbase = vm.GetSelectRM();

                if (rmbase is null)
                {
                    tb_Status.Text = TranslationManager.Instance.Translate("Not Select RM").ToString();
                    return;
                }

                if (sender is Border bd)
                {
                    switch (bd.Tag.ToString())
                    {
                        case "MOVE":
                            CMD = eCraneCommand.MOVE;
                            CarrierID = "ManualMove";
                            break;
                        case "GET":
                            CMD = eCraneCommand.PICKUP;
                            break;
                        case "PUT":
                            CMD = eCraneCommand.UNLOAD;
                            break;

                    }

                    //bool TargetIsPort = false;
                    //목표 대상 유효성 판단.
                    ICarrierStoreAble target = null;

                    if (vm.SelectPort)
                    {
                        target = GlobalData.Current.PortManager.GetCVModule(vm.DestPort);
                        //TargetIsPort = true;
                    }
                    else
                    {
                        target = ShelfManager.Instance.GetShelf(vm.DestShelf);
                    }

                    if (target == null)
                    {
                        tb_Status.Text = TranslationManager.Instance.Translate("Invalid Target").ToString();
                        return;
                    }

                    msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString().ToUpper() + ":{0}" +
                                        TranslationManager.Instance.Translate("CMD").ToString().ToUpper() + ":{1}" +
                                        TranslationManager.Instance.Translate("Bank").ToString().ToUpper() + ":{2}" +
                                        TranslationManager.Instance.Translate("Bay").ToString().ToUpper() + ":{3}" +
                                        TranslationManager.Instance.Translate("Level").ToString().ToUpper() + ":{4}" +
                                        TranslationManager.Instance.Translate("커맨드를 실행하시겠습니까?").ToString(),
                                        rmbase.ModuleName, CMD, target.iBank, target.iBay, target.iLevel);
                    MessageBoxPopupView mbox = new MessageBoxPopupView(msg, MessageBoxButton.OKCancel, MessageBoxImage.Question);
                    CustomMessageBoxResult mResult = mbox.ShowResult();

                    if (mResult.Result == MessageBoxResult.OK)
                    {
                        //현재 크레인 동작 상태 판단.
                        if (rmbase.CheckRMCommandExist())
                        {
                            tb_Status.Text = TranslationManager.Instance.Translate("Crane is Busy").ToString();
                            return;
                        }

                        //목적지 사이즈 체크
                        if (CMD == eCraneCommand.UNLOAD)
                        {
                            if (target.CheckCarrierSizeAcceptable(rmbase.InSlotCarrier.CarrierSize) == false)
                            {
                                tb_Status.Text = TranslationManager.Instance.Translate("Carrier size is mismatched").ToString();
                                return;
                            }
                        }

                        //SuHwan_20221109 : [ServerClient] 클라이언트 명령 전용
                        if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                        {
                            //목적지 사이즈 체크
                            if (CMD == eCraneCommand.UNLOAD)
                            {
                                var carrierBuffer = GlobalData.Current.CarrierStore.GetCarrierItem(rmbase.CarrierID);

                                if (target.CheckCarrierSizeAcceptable(carrierBuffer.CarrierSize) == false)
                                {
                                    tb_Status.Text = TranslationManager.Instance.Translate("Carrier size is mismatched").ToString();
                                    return;
                                }
                            }

                            //팔 위에 적재 상태 확인
                            if (CMD == eCraneCommand.PICKUP)
                            {
                                if (rmbase.CheckCarrierExist())
                                {
                                    tb_Status.Text = TranslationManager.Instance.Translate("Already have a Carrier").ToString();
                                    return;
                                }
                            }

                            ClientReqList reqBuffer = new ClientReqList
                            {
                                EQPID = GlobalData.Current.EQPID,
                                CMDType = CMD.ToString(),
                                Target = "ManualJob",
                                TargetID = rmbase.ModuleName,
                                TargetValue = target.GetTagName(),
                                ReqTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                                Requester = eServerClientType.Client,
                            };

                            GlobalData.Current.DBManager.DbSetProcedureClientReq(reqBuffer.EQPID, reqBuffer.CMDType, reqBuffer.Target, reqBuffer.TargetID, reqBuffer.TargetValue, reqBuffer.ReqTime, reqBuffer.Requester);
                            msg = string.Format(TranslationManager.Instance.Translate("Crane").ToString().ToUpper() + ":{0} " +
                                                TranslationManager.Instance.Translate("CMD").ToString() + ":{1} " +
                                                TranslationManager.Instance.Translate("Bank").ToString().ToUpper() + ":{2} " +
                                                TranslationManager.Instance.Translate("Bay").ToString().ToUpper() + ":{3} " +
                                                TranslationManager.Instance.Translate("Level").ToString().ToUpper() + ":{4}");
                            tb_Status.Text = string.Format(msg, rmbase.ModuleName, CMD, target.iBank, target.iBay, target.iLevel);
                            return;
                        }

                        //작업 추가 입력 
                        switch(CMD)
                        {
                            case eCraneCommand.MOVE:
                                GlobalData.Current.McdList.CreateManualJob(CMD, rmbase, target, 99);
                                break;
                            case eCraneCommand.PICKUP:
                                GlobalData.Current.McdList.CreateManualJob(CMD, target, rmbase, 99);
                                break;
                            case eCraneCommand.UNLOAD:
                                GlobalData.Current.McdList.CreateManualJob(CMD, rmbase, target, 99);
                                break;
                            default:
                                break;

                        }
                        //CraneCommand mCmd = new CraneCommand("ManualCom", rmbase.ModuleName, CMD, TargetIsPort ? enumCraneTarget.PORT : enumCraneTarget.SHELF, target, CarrierID);
                        //if (!rmbase.CheckRMBusy())
                        //{
                        //    rmbase.SetCraneCommand(mCmd);
                        //    tb_Status.Text = string.Format("CRANE:{0} CMD:{1} BANK:{2} BAY:{3} LEVEL:{4}", rmbase.ModuleName, CMD, target.iBank, target.iBay, target.iLevel);
                        //    return;
                        //}
                    }
                }
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }

        public bool ChangeRM(string rm)
        {
            try
            {
                RMModuleBase rmbase = vm.GetSelectRM();

                //선택되어있는 rmbase가 있으면 해당 rm이 Busy가 아니어야 한다.
                if (!(rmbase is null))
                {
                    if (rmbase.CheckRMCommandExist())
                    {
                        return false;
                    }
                }

                vm.SetRM(rm);
            }
            catch (Exception ex)
            {
                _ = ex;
                return false;
            }

            return true;
        }
    }
}
