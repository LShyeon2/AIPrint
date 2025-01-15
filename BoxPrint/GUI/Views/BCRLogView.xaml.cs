﻿using Microsoft.Office.Interop.Excel;
using BoxPrint.GUI.ClassArray;
using BoxPrint.GUI.ETC;
using BoxPrint.GUI.EventCollection;
using BoxPrint.GUI.ViewModels;
using BoxPrint.GUI.Views.UserPage;
using BoxPrint.Log;
using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.Views
{
    // 2020.09.24 RGJ
    /// <summary>
    /// AlarmLogView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class BCRLogView : System.Windows.Controls.Page
    {
        #region Variable
        #region Field
        private string strtag = string.Empty;
        private ViewModelBCRLogView vm;
        #endregion

        #region Event
        private delegate void D_Set_StringValue(string nValue);
        #endregion
        #endregion

        #region Methods
        #region Constructor
        public BCRLogView()
        {
            InitializeComponent();
            vm = new ViewModelBCRLogView();
            //DataContext = this;
            DataContext = vm;

            //230102 YSW 사용자 권한에 따른 버튼 잠금
            ModifyAuthorityCheck();
            LogInPopupView._EventHandler_LoginChange += ModifyAuthorityCheck;
            GroupAccountManagementPage._EventHandler_ChangeAuthority += ModifyAuthorityCheck;

            //DatePick_Start.SelectedDate = DateTime.Now.Date; //초기날짜 오늘날짜로 지정
            //DatePick_End.SelectedDate = DateTime.Now.Date;

            vm.SearchStart = DateTime.Today.AddDays(-7);
            vm.SearchEnd = DateTime.Now;

            GlobalData.Current.SendTagChange += Current_ReceiveEvent;
            GlobalData.Current.loglistrefresh += OnLogListRefresh;

            UIEventCollection.Instance.OnCultureChanged += _UIEventCollection_OnCultureChanged;
        }
        #endregion

        private void OnLogListRefresh()
        {
            vm.ExcuteLogInit();
        }

        #region Event
        private void Current_ReceiveEvent(object sender, EventArgs e)
        {
            string JInfo = (string)sender;
            this.Dispatcher.Invoke(new D_Set_StringValue(_DisplayChange), JInfo);
        }
        private void _UIEventCollection_OnCultureChanged(string cultureKey)
        {
            //this.Language = XmlLanguage.GetLanguage(cultureKey);

            vm.SearchStart = DateTime.Today.AddDays(-7);
            vm.SearchEnd = DateTime.Now;
        }
        #endregion

        #region Etc
        //230119 YSW 수정권한 잠금
        public void ModifyAuthorityCheck()
        {
            if (GlobalData.Current.LoginUserAuthority.Contains("ModifyBCRLog"))
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
        private void _DisplayChange(string strtag)
        {
            try
            {
                this.strtag = strtag;
                initLoad(strtag);
            }
            catch (Exception ex)
            {
                LogManager.WriteConsoleLog(eLogLevel.Fatal, ex.ToString());
            }
        }
        private void initLoad(string tag)
        {
            //불필요한 그리드 클리어 주석 처리
            //LogGrid.Columns.Clear();
            //cbb_LogItem.SelectedIndex = 0;
            //DatePick_Start.SelectedDate = DateTime.Now.Subtract(new TimeSpan(7, 0, 0, 0));
            //DatePick_End.SelectedDate = DateTime.Now - DateTime.Now.TimeOfDay;
        }
        #endregion

        #endregion

        private void ButtonControl_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                if (sender is System.Windows.Controls.Button senderBuffer)
                {
                    LogManager.WriteOperatorLog(string.Format("사용자가 {0} {1} 을/를 Click하였습니다.", "BCR Log", senderBuffer.Tag.ToString()),
                        "CLICK", senderBuffer.Tag.ToString(), GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 4,
                        "BCR Log", senderBuffer.Tag.ToString());
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            if (LogGrid.Items.Count > 0)
            {
                LogGrid.ScrollIntoView(LogGrid.Items[0]);
            }

            vm.SearchStart = DateTime.Today.AddDays(-7);
            vm.SearchEnd = DateTime.Now;
        }
    }
}