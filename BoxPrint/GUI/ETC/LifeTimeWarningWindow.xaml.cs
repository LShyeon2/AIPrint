using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Stockerfirmware.GUI.ETC
{
    /// <summary>
    /// RobotSpeedPopupView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LifeTimeWarningWindow : Window
    {
        private int HideDurationSec = 3600; //임시 숨김 시간
        DateTime HideDateTime = DateTime.MinValue;
        
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public LifeTimeWarningWindow()
        {
            InitializeComponent();
        }

        public void UpdateWarningList()
        {
            dataGrid_Parts.ItemsSource = GlobalData.Current.PartsLife_mgr.GetLifeOverList();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //x버튼 삭제
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }
        //유저가 숨김선택을 했는지 체크
        public bool UserHide()
        {
            if((DateTime.Now - HideDateTime).TotalSeconds > HideDurationSec)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        private void button_exit_Click(object sender, RoutedEventArgs e)
        {
            HideDateTime = DateTime.Now;
            Hide();
        }
    }
}
