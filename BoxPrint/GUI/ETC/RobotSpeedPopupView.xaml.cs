using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.ETC
{
    /// <summary>
    /// RobotSpeedPopupView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class RobotSpeedPopupView : Window
    {

        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public RobotSpeedPopupView()
        {
            InitializeComponent();


            ViewLoaded();

        }
        private void ViewLoaded()
        {


            foreach (var item in GlobalData.Current.mRMManager.ModuleList)
            {
                cbRMCount.Items.Add(item.Value.ModuleName);
            }
            cbRMCount.SelectedIndex = 0;

            SetSpeed();
        }

        public void SetSpeed()
        {
            lblCurSpeed.Content = GlobalData.Current.mRMManager[cbRMCount.SelectedItem.ToString()].MoveSpeed.ToString();

            sdRMspeed.Value = (double)GlobalData.Current.mRMManager[cbRMCount.SelectedItem.ToString()].MoveSpeed;
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            string msg = string.Empty;
            double spd = Math.Truncate(sdRMspeed.Value * 10) / 10;
            msg = TranslationManager.Instance.Translate("RobotSpeed 변경 메시지").ToString();
            string strMessage = string.Format(msg, cbRMCount.SelectedItem.ToString(), spd.ToString());
            MessageBoxResult result = System.Windows.MessageBox.Show(strMessage, TranslationManager.Instance.Translate("Info Message").ToString(), MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                GlobalData.Current.mRMManager[cbRMCount.SelectedItem.ToString()].MoveSpeed = (decimal)spd;
                SetSpeed();
            }
        }


        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            SetSpeed();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //x버튼 삭제
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }
    }
}
