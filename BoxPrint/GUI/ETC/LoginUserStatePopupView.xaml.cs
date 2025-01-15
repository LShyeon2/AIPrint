using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace BoxPrint.GUI.ETC
{
    /// <summary>
    /// LoginUserStatePopupView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LoginUserStatePopupView : UserControl
    {
        public delegate void EventHandler_ChangePage();
        public static event EventHandler_ChangePage _EventCall_PageChange;

        public static string UserPageTag = null;

        private bool bFirstLoad = true;

        public LoginUserStatePopupView()
        {
            InitializeComponent();
            LoginUserStatePopupView.Current = this;

            //GlobalData.Current.currentaccountlogout += OnCurrentAccountLogout;
        }

        private void OnCurrentAccountLogout()
        {
            if (!string.IsNullOrEmpty(MainWindow.checkLoginUserID))
            {
                Button_Click(btnLogout, null);
            }
        }

        public static LoginUserStatePopupView Current { get; private set; }

        public void setChangeLoginUserID()
        {
            CurrentUserIDtxb.Text = "[ " + MainWindow.checkLoginUserID + " ]";
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                if (sender is Button mi)
                {
                    switch (mi.Tag.ToString())
                    {
                        case "AccountManagement":
                            UserPageTag = "AccountManagement";
                            break;

                        case "Logout":
                            UserPageTag = "Logout";
                            break;

                        default:
                            break;
                    }
                }
                _EventCall_PageChange();
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }

        }
        private void BorderButton_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border)
            {
                var senderBuffer = (Border)sender;

                switch (senderBuffer.Tag.ToString())
                {
                    case "EXIT":

                        break;

                    default:
                        break;
                }

            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (bFirstLoad)
                {
                    GlobalData.Current.currentaccountlogout += OnCurrentAccountLogout;
                    bFirstLoad = false;
                }
            }
            catch (Exception ex)
            {
                _ = ex;
            }
        }
    }
}
