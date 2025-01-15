using BoxPrint.GUI.ETC;
using BoxPrint.GUI.UIControls;
using BoxPrint.GUI.ViewModels;
using BoxPrint.GUI.Views.UserPage;
using BoxPrint.Log;
using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.Views
{
    /// <summary>
    /// ConfigView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ConfigView : Page
    {
        private ViewModelConfigView vm;

        private DataSet dataSet;

        private DateTime ViewInitTime = DateTime.Now;
        private bool bInit;

        public ConfigView()
        {
            InitializeComponent();
            vm = new ViewModelConfigView();
            DataContext = vm;

            //230102 YSW 사용자 권한에 따른 버튼 잠금
            ModifyAuthorityCheck();
            LogInPopupView._EventHandler_LoginChange += ModifyAuthorityCheck;
            GroupAccountManagementPage._EventHandler_ChangeAuthority += ModifyAuthorityCheck;

            //220916 조숭진 s
            cmbConfigValue.Visibility = Visibility.Hidden;
            txtConfigValue.Visibility = Visibility.Visible;
            //220916 조숭진 e

            //GlobalData.Current.configdatarefresh += OnConfigDataRefreshed;
            //bInit = true;
            //OnConfigDataRefreshed();

            if (GlobalData.Current.ServerClientType == eServerClientType.Client)
            {
                cmdSave.Visibility = Visibility.Hidden;
                cmdClose.Visibility = Visibility.Hidden;
                txtConfigValue.Focusable = false;
            }
        }

        //230119 YSW 수정권한 잠금
        public void ModifyAuthorityCheck()
        {
            if (GlobalData.Current.LoginUserAuthority.Contains("ModifyConfig"))
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

        //private void OnConfigDataRefreshed()
        //{
        //    if (GlobalData.Current.ServerClientType == eServerClientType.Client &&
        //        (bInit || IsTimeOut(DateTime.Now)))
        //    {
        //        ObservableCollection<ConfigViewModelData> ConfigListRefreshData = new ObservableCollection<ConfigViewModelData>();

        //        dataSet = GlobalData.Current.DBManager.DbGetProcedureGlobalConfigInfo('1');

        //        int table = dataSet.Tables.Count;
        //        for (int i = 0; i < table; i++)// set the table value in list one by one
        //        {
        //            foreach (DataRow dr in dataSet.Tables[i].Rows)
        //            {
        //                //if (string.IsNullOrEmpty(dr["CONFIG_NO"].ToString()))
        //                //    continue;
        //                ConfigListRefreshData.Add(
        //                    new ConfigViewModelData()
        //                    {
        //                        ConfigNumber = dr["CONFIG_NO"].ToString(),
        //                        ConfigName = dr["CONFIG_NM"].ToString(),
        //                        ConfigType = dr["CONFIG_GB"].ToString(),
        //                        ConfigValue = dr["CONFIG_VAL"].ToString(),
        //                        ConfigDefaultValue = string.IsNullOrEmpty(dr["CONFIG_DEF"].ToString()) ? string.Empty : dr["CONFIG_DEF"].ToString(),
        //                        ConfigDescription = string.IsNullOrEmpty(dr["CONFIG_DES"].ToString()) ? string.Empty : dr["CONFIG_DES"].ToString()
        //                    });
        //            }
        //        }

        //        vm.ConfigListData = ConfigListRefreshData;
        //        if (bInit)
        //            bInit = false;
        //    }
        //    else if (GlobalData.Current.ServerClientType == eServerClientType.Server)
        //    {
        //        ObservableCollection<ConfigViewModelData> ConfigListRefreshData = new ObservableCollection<ConfigViewModelData>();

        //        dataSet = GlobalData.Current.DBManager.DbGetProcedureGlobalConfigInfo('1');

        //        int table = dataSet.Tables.Count;
        //        for (int i = 0; i < table; i++)// set the table value in list one by one
        //        {
        //            foreach (DataRow dr in dataSet.Tables[i].Rows)
        //            {
        //                //if (string.IsNullOrEmpty(dr["CONFIG_NO"].ToString()))
        //                //    continue;
        //                ConfigListRefreshData.Add(
        //                    new ConfigViewModelData()
        //                    {
        //                        ConfigNumber = dr["CONFIG_NO"].ToString(),
        //                        ConfigName = dr["CONFIG_NM"].ToString(),
        //                        ConfigType = dr["CONFIG_GB"].ToString(),
        //                        ConfigValue = dr["CONFIG_VAL"].ToString(),
        //                        ConfigDefaultValue = string.IsNullOrEmpty(dr["CONFIG_DEF"].ToString()) ? string.Empty : dr["CONFIG_DEF"].ToString(),
        //                        ConfigDescription = string.IsNullOrEmpty(dr["CONFIG_DES"].ToString()) ? string.Empty : dr["CONFIG_DES"].ToString()
        //                    });
        //            }
        //        }

        //        vm.ConfigListData = ConfigListRefreshData;
        //    }
        //}


        //private bool IsTimeOut(DateTime dtstart)
        //{
        //    TimeSpan tspan = dtstart - ViewInitTime;
        //    if (tspan.Minutes >= 1)
        //    {
        //        ViewInitTime = DateTime.Now;
        //        return true;
        //    }
        //    else
        //        return false;
        //}

        //private void Save_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        //{
        //    string strName = txtConfigName.Text;
        //    string strType = txtConfigType.Text;

        //    string strValue = string.Empty;
        //    if (cmbConfigValue.Visibility == Visibility.Visible)
        //    {
        //        strValue = cmbConfigValue.SelectedItem.ToString();
        //    }
        //    else
        //    {
        //        strValue = txtConfigValue.Text;
        //    }

        //    string strDefaultValue = txtConfigDefaultValue.Text;
        //    string strDescription = txtConfigDescription.Text;
        //    bool bconfigsetok = false;

        //    ////혹시라도 지우고 세이브하면 안되니 체크해준다.
        //    //if (string.IsNullOrEmpty(strName) || string.IsNullOrEmpty(strType)
        //    //    || string.IsNullOrEmpty(strValue) || string.IsNullOrEmpty(strDefaultValue)
        //    //    || string.IsNullOrEmpty(strDescription))
        //    //    return;

        //    //20220728 조숭진 config 방식 변경 s
        //    if (strType.Contains("CVLine") &&
        //        !(strName.Contains("Time") || strName.Contains("Delay") || strName.Contains("Retry")))
        //    {
        //        if (GlobalData.Current.PortManager.portconfigModify(strType, strName, strValue))
        //        {
        //            int temp = Convert.ToInt32(vm.SelValue.ConfigNumber);
        //            bconfigsetok = GlobalData.Current.DBManager.DbSetProcedureConfigInfo(strType, strName, strValue, strDefaultValue, strDescription, temp);
        //        }
        //    }
        //    //220916 조숭진 db로 옮김. s
        //    else if (strType.Equals("RMSection") &&
        //        !(strName.Contains("Time") || strName.Contains("Delay") || strName.Contains("Retry")))
        //    {
        //        //if (strName.Contains("ExeclusiveBay"))
        //        //{
        //        //    string configname = strName.Substring(0, strName.IndexOf("ExeclusiveBay"));
        //        //    int configvalue = Convert.ToInt32(strValue);
        //        //    //int Maxbay = GlobalData.Current.Scheduler.MaxBay;
        //        //    int RMrange = GlobalData.Current.Scheduler.RM_RangeMargin;
        //        //    int RM1ExeclusiveBay = GlobalData.Current.Scheduler.RM1_ExclusiveBay;
        //        //    int RM2ExeclusiveBay = GlobalData.Current.Scheduler.RM2_ExclusiveBay;
        //        //    int FrontBay = ShelfManager.Instance.FrontData.MaxBay;
        //        //    int RearBay = ShelfManager.Instance.FrontData.MaxBay;
        //        //    int Maxbay = FrontBay >= RearBay ? FrontBay : RearBay;

        //        //    if (configname == "RM1")
        //        //    {
        //        //        if (!(configvalue >= Maxbay || configvalue < RMrange || configvalue >= Maxbay - RMrange || configvalue >= RM2ExeclusiveBay - 1))
        //        //        {
        //        //            int temp = Convert.ToInt32(vm.SelValue.ConfigNumber);
        //        //            bconfigsetok = GlobalData.Current.DBManager.DbSetProcedureConfigInfo(strType, strName, strValue, strDefaultValue, strDescription, temp);

        //        //            if (bconfigsetok)
        //        //            {
        //        //                GlobalData.Current.Scheduler.SetRM1ExeclusiveBay(configvalue);
        //        //                GlobalData.Current.Scheduler.SetExeclusiveRange();
        //        //            }
        //        //        }
        //        //    }
        //        //    else
        //        //    {
        //        //        if (!(configvalue >= Maxbay || configvalue <= RMrange || configvalue <= RM1ExeclusiveBay + 1))
        //        //        {
        //        //            int temp = Convert.ToInt32(vm.SelValue.ConfigNumber);
        //        //            bconfigsetok = GlobalData.Current.DBManager.DbSetProcedureConfigInfo(strType, strName, strValue, strDefaultValue, strDescription, temp);

        //        //            if (bconfigsetok)
        //        //            {
        //        //                GlobalData.Current.Scheduler.SetRM2ExeclusiveBay(configvalue);
        //        //                GlobalData.Current.Scheduler.SetExeclusiveRange();
        //        //            }
        //        //        }
        //        //    }
        //        //}
        //        //else if (strName.Equals("EmptyRetriveAutoReset"))
        //        if (strName.Equals("EmptyRetriveAutoReset"))
        //        {
        //            int temp = Convert.ToInt32(vm.SelValue.ConfigNumber);
        //            bconfigsetok = GlobalData.Current.DBManager.DbSetProcedureConfigInfo(strType, strName, strValue, strDefaultValue, strDescription, temp);

        //            if (bconfigsetok)
        //            {
        //                GlobalData.Current.Scheduler.SetUseEmptyRetriveAutoReset(Convert.ToBoolean(strValue));
        //            }
        //        }
        //    }
        //    //220916 조숭진 db로 옮김. e
        //    else if (strName.Contains("Time") || strName.Contains("Delay") || strName.Contains("Retry"))
        //    {
        //        int temp = Convert.ToInt32(vm.SelValue.ConfigNumber);
        //        bconfigsetok = GlobalData.Current.DBManager.DbSetProcedureConfigInfo(strType, strName, strValue, strDefaultValue, strDescription, temp);
        //        if (bconfigsetok && strType.Equals("RMSection"))
        //            GlobalData.Current.mRMManager.TimeoutSetting();
        //        else if (bconfigsetok && strType.Equals("BCR"))
        //            GlobalData.Current.PortManager.BCRTimeoutSetting();
        //        else if (bconfigsetok && strType.Equals("CVLine"))
        //            GlobalData.Current.PortManager.TimeSetting(false);
        //    }
        //    else
        //    {
        //        if (GlobalData.Current.GlobalConfigModify(strType, strName, strValue))
        //        {
        //            int temp = Convert.ToInt32(vm.SelValue.ConfigNumber);
        //            bconfigsetok = GlobalData.Current.DBManager.DbSetProcedureConfigInfo(strType, strName, strValue, strDefaultValue, strDescription, temp);
        //        }
        //    }
        //    //20220728 조숭진 config 방식 변경 e

        //    if (bconfigsetok)
        //    {
        //        //바인딩 되어있기에 SelValue를 변경해준다.
        //        vm.SelValue.ConfigName = strName;
        //        vm.SelValue.ConfigType = strType;
        //        vm.SelValue.ConfigValue = strValue;
        //        vm.SelValue.ConfigDefaultValue = strDefaultValue;
        //        vm.SelValue.ConfigDescription = strDescription;
        //    }
        //    //SelValue Update로 전체 ConfigList가 Update되었기에 저장해준다.
        //    //추후 구현 예정
        //}

        //220916 조숭진 s
        private void CarrierIDListView_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                return;

            txtConfigValue.Visibility = Visibility.Hidden;
            cmbConfigValue.Visibility = Visibility.Hidden;
            cmbConfigValue.Items.Clear();

            string value = txtConfigValue.Text;
            string name = txtConfigName.Text;

            if (value.ToLower().Equals("true") || value.ToLower().Equals("false"))
            {
                cmbConfigValue.Items.Add("True");
                cmbConfigValue.Items.Add("False");

                if (value.ToLower().Equals("true"))
                    cmbConfigValue.SelectedIndex = 0;
                else
                    cmbConfigValue.SelectedIndex = 1;

                cmbConfigValue.Visibility = Visibility.Visible;
            }
            else if (value.Equals("Plain") || value.Equals("RobotIF") || value.Equals("EQIF") || value.Equals("WaterPool"))
            {
                cmbConfigValue.Items.Add("Plain");
                cmbConfigValue.Items.Add("RobotIF");
                cmbConfigValue.Items.Add("EQIF");
                cmbConfigValue.Items.Add("WaterPool");

                switch (value)
                {
                    case "Plain":
                        cmbConfigValue.SelectedIndex = 0;
                        break;
                    case "RobotIF":
                        cmbConfigValue.SelectedIndex = 1;
                        break;
                    case "EQIF":
                        cmbConfigValue.SelectedIndex = 2;
                        break;
                    case "WaterPool":
                        cmbConfigValue.SelectedIndex = 3;
                        break;
                    default:
                        break;
                }

                cmbConfigValue.Visibility = Visibility.Visible;
            }
            else if (value.Equals("Short") || value.Equals("Long") || value.Equals("Both"))
            {
                cmbConfigValue.Items.Add("Short");
                cmbConfigValue.Items.Add("Long");
                cmbConfigValue.Items.Add("Both");

                switch (value)
                {
                    case "Short":
                        cmbConfigValue.SelectedIndex = 0;
                        break;
                    case "Long":
                        cmbConfigValue.SelectedIndex = 1;
                        break;
                    case "Both":
                        cmbConfigValue.SelectedIndex = 2;
                        break;
                    default:
                        break;
                }

                cmbConfigValue.Visibility = Visibility.Visible;
            }
            else if (value.Equals("INPUT") || value.Equals("OUTPUT"))
            {
                cmbConfigValue.Items.Add("INPUT");
                cmbConfigValue.Items.Add("OUTPUT");

                switch (value)
                {
                    case "INPUT":
                        cmbConfigValue.SelectedIndex = 0;
                        break;
                    case "OUTPUT":
                        cmbConfigValue.SelectedIndex = 1;
                        break;
                    default:
                        break;
                }

                cmbConfigValue.Visibility = Visibility.Visible;
            }
            else if (value.Equals("MaunalIn") || value.Equals("MaunalOut") || value.Equals("AutoIn") || value.Equals("AutoOut"))
            {
                cmbConfigValue.Items.Add("MaunalIn");
                cmbConfigValue.Items.Add("MaunalOut");
                cmbConfigValue.Items.Add("AutoIn");
                cmbConfigValue.Items.Add("AutoOut");

                switch (value)
                {
                    case "MaunalIn":
                        cmbConfigValue.SelectedIndex = 0;
                        break;
                    case "MaunalOut":
                        cmbConfigValue.SelectedIndex = 1;
                        break;
                    case "AutoIn":
                        cmbConfigValue.SelectedIndex = 2;
                        break;
                    case "AutoOut":
                        cmbConfigValue.SelectedIndex = 3;
                        break;
                    default:
                        break;
                }

                cmbConfigValue.Visibility = Visibility.Visible;
            }
            else if (value.Contains("Dual") || value.Contains("Single"))
            {
                if (name.Equals("SCSType"))
                {
                    cmbConfigValue.Items.Add("Dual");
                    cmbConfigValue.Items.Add("Single");
                }
                else if (name.Equals("TypeName"))
                {
                    cmbConfigValue.Items.Add("DualRMScheduler");
                    cmbConfigValue.Items.Add("SingleRMScheduler");
                }

                switch (value)
                {
                    case "Dual":
                    case "DualRMScheduler":
                        cmbConfigValue.SelectedIndex = 0;
                        break;
                    case "Single":
                    case "SingleRMScheduler":
                        cmbConfigValue.SelectedIndex = 1;
                        break;
                }

                cmbConfigValue.Visibility = Visibility.Visible;
            }
            else
            {
                txtConfigValue.Visibility = Visibility.Visible;
            }
        }

        private void cmdSave_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                string strName = txtConfigName.Text;
                string strType = txtConfigType.Text;

                string strValue = string.Empty;
                if (cmbConfigValue.Visibility == Visibility.Visible)
                {
                    strValue = cmbConfigValue.SelectedItem.ToString();
                }
                else
                {
                    strValue = txtConfigValue.Text;
                }

                string strDefaultValue = txtConfigDefaultValue.Text;
                string strDescription = txtConfigDescription.Text;
                bool bconfigsetok = false;

                ////혹시라도 지우고 세이브하면 안되니 체크해준다.
                //if (string.IsNullOrEmpty(strName) || string.IsNullOrEmpty(strType)
                //    || string.IsNullOrEmpty(strValue) || string.IsNullOrEmpty(strDefaultValue)
                //    || string.IsNullOrEmpty(strDescription))
                //    return;

                //20220728 조숭진 config 방식 변경 s
                if (strType.Contains("CVLine") &&
                    !(strName.Contains("Time") || strName.Contains("Delay") || strName.Contains("Retry")))
                {
                    if (GlobalData.Current.PortManager.portconfigModify(strType, strName, strValue))
                    {
                        int temp = Convert.ToInt32(vm.SelValue.ConfigNumber);
                        bconfigsetok = GlobalData.Current.DBManager.DbSetProcedureConfigInfo(strType, strName, strValue, strDefaultValue, strDescription, temp);
                    }
                }
                //220916 조숭진 db로 옮김. s
                else if (strType.Equals("RMSection") &&
                    !(strName.Contains("Time") || strName.Contains("Delay") || strName.Contains("Retry")))
                {
                    //if (strName.Contains("ExeclusiveBay"))
                    //{
                    //    string configname = strName.Substring(0, strName.IndexOf("ExeclusiveBay"));
                    //    int configvalue = Convert.ToInt32(strValue);
                    //    //int Maxbay = GlobalData.Current.Scheduler.MaxBay;
                    //    int RMrange = GlobalData.Current.Scheduler.RM_RangeMargin;
                    //    int RM1ExeclusiveBay = GlobalData.Current.Scheduler.RM1_ExclusiveBay;
                    //    int RM2ExeclusiveBay = GlobalData.Current.Scheduler.RM2_ExclusiveBay;
                    //    int FrontBay = ShelfManager.Instance.FrontData.MaxBay;
                    //    int RearBay = ShelfManager.Instance.FrontData.MaxBay;
                    //    int Maxbay = FrontBay >= RearBay ? FrontBay : RearBay;

                    //    if (configname == "RM1")
                    //    {
                    //        if (!(configvalue >= Maxbay || configvalue < RMrange || configvalue >= Maxbay - RMrange || configvalue >= RM2ExeclusiveBay - 1))
                    //        {
                    //            int temp = Convert.ToInt32(vm.SelValue.ConfigNumber);
                    //            bconfigsetok = GlobalData.Current.DBManager.DbSetProcedureConfigInfo(strType, strName, strValue, strDefaultValue, strDescription, temp);

                    //            if (bconfigsetok)
                    //            {
                    //                GlobalData.Current.Scheduler.SetRM1ExeclusiveBay(configvalue);
                    //                GlobalData.Current.Scheduler.SetExeclusiveRange();
                    //            }
                    //        }
                    //    }
                    //    else
                    //    {
                    //        if (!(configvalue >= Maxbay || configvalue <= RMrange || configvalue <= RM1ExeclusiveBay + 1))
                    //        {
                    //            int temp = Convert.ToInt32(vm.SelValue.ConfigNumber);
                    //            bconfigsetok = GlobalData.Current.DBManager.DbSetProcedureConfigInfo(strType, strName, strValue, strDefaultValue, strDescription, temp);

                    //            if (bconfigsetok)
                    //            {
                    //                GlobalData.Current.Scheduler.SetRM2ExeclusiveBay(configvalue);
                    //                GlobalData.Current.Scheduler.SetExeclusiveRange();
                    //            }
                    //        }
                    //    }
                    //}
                    //else if (strName.Equals("EmptyRetriveAutoReset"))
                    if (strName.Equals("EmptyRetriveAutoReset"))
                    {
                        int temp = Convert.ToInt32(vm.SelValue.ConfigNumber);
                        bconfigsetok = GlobalData.Current.DBManager.DbSetProcedureConfigInfo(strType, strName, strValue, strDefaultValue, strDescription, temp);

                        if (bconfigsetok)
                        {
                            GlobalData.Current.Scheduler.SetUseEmptyRetriveAutoReset(Convert.ToBoolean(strValue));
                        }
                    }
                }
                //220916 조숭진 db로 옮김. e
                else if (strName.Contains("Time") || strName.Contains("Delay") || strName.Contains("Retry"))
                {
                    int temp = Convert.ToInt32(vm.SelValue.ConfigNumber);
                    bconfigsetok = GlobalData.Current.DBManager.DbSetProcedureConfigInfo(strType, strName, strValue, strDefaultValue, strDescription, temp);
                    if (bconfigsetok && strType.Equals("RMSection"))
                        GlobalData.Current.mRMManager.TimeoutSetting();
                    //else if (bconfigsetok && strType.Equals("BCR"))
                    //    GlobalData.Current.PortManager.BCRTimeoutSetting();
                    else if (bconfigsetok && strType.Equals("CVLine"))
                        GlobalData.Current.PortManager.TimeSetting(false);
                    else if (bconfigsetok && strName.Equals("WaitInCommandTime")) //240213 RGJ WaitInTimeOut 컨피그 UI 변경시 적용
                        GlobalData.Current.Scheduler.SetWaitInTime(strValue);
                }
                else
                {
                    if (GlobalData.Current.GlobalConfigModify(strType, strName, strValue))
                    {
                        int temp = Convert.ToInt32(vm.SelValue.ConfigNumber);
                        bconfigsetok = GlobalData.Current.DBManager.DbSetProcedureConfigInfo(strType, strName, strValue, strDefaultValue, strDescription, temp);
                    }
                }
                //20220728 조숭진 config 방식 변경 e

                if (bconfigsetok)
                {
                    //바인딩 되어있기에 SelValue를 변경해준다.
                    vm.SelValue.ConfigName = strName;
                    vm.SelValue.ConfigType = strType;
                    vm.SelValue.ConfigValue = strValue;
                    vm.SelValue.ConfigDefaultValue = strDefaultValue;
                    vm.SelValue.ConfigDescription = strDescription;

                    GlobalData.Current.ConfigDataRefresh();     //ui refresh 해준다.
                }
                //SelValue Update로 전체 ConfigList가 Update되었기에 저장해준다.
                //추후 구현 예정
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            dataGridConfigList.ScrollIntoView(dataGridConfigList.Items[0]);
            GlobalData.Current.ConfigDataRefresh();
        }
        //220916 조숭진 e

        private void SK_ButtonControl_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                if (sender is SK_ButtonControl senderBuffer)
                {
                    switch (senderBuffer.Tag.ToString())
                    {
                        case "SAVE":
                            {
                                cmdSave_Click(sender, null);
                            }
                            break;

                        default:
                            break;
                    }

                    LogManager.WriteOperatorLog(string.Format("사용자가 {0} {1} 을/를 Click하였습니다.", this.Tag.ToString(), senderBuffer.Tag.ToString()),
                        "SAVE", senderBuffer.Tag.ToString(), GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 4,
                        this.Tag.ToString(), senderBuffer.Tag.ToString());
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }
    }
}
