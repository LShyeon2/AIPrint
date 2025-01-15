using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.ETC
{
    /// <summary>
    /// LanguagePopup.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class LanguagePopup : Window
    {
        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        public delegate void EventHandler_ChangeLanguage();
        public static event EventHandler_ChangeLanguage _EventCall_ChangeLanguage;
        public static int CreatLock = 0;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        public LanguagePopup()
        {
            InitializeComponent();
            //InitVar();
        }

        private void InitVar()
        {
            lblCurBoothState.Content = TranslationManager.Instance.Translate("한국어").ToString();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                if (sender is Button)
                {
                    var buffer = (Button)sender;

                    switch (buffer.Tag.ToString())
                    {
                        case "OK":
                            LanguageChange();
                            this.Close();
                            CreatLock = 0;
                            break;

                        case "Cancle":
                            this.Close();
                            CreatLock = 0;
                            break;

                        case "Refresh":
                            GetBoothState();
                            break;

                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }

        private void LanguageChange()
        {
            if (g1.IsChecked == true)
            {
                TranslationManager.Instance.CurrentLanguage = new CultureInfo("ko-KR");

                //App.TranslationSource("ko-KR");
                //TranslationSource.Instance.CurrentCulture = new CultureInfo("ko-KR");
            }
            else if (g2.IsChecked == true)
            {
                TranslationManager.Instance.CurrentLanguage = new CultureInfo("zh-CN");

                //Resource.Culture = new CultureInfo("zh-CN");
                //TranslationSource.Instance.CurrentCulture = new CultureInfo("zh-CN");
                //App.TranslationSource("zh-CN");
            }
            else if (g3.IsChecked == true)
            {
                TranslationManager.Instance.CurrentLanguage = new CultureInfo("hu-HU");

                //App.TranslationSource("hu-HU");
                //TranslationSource.Instance.CurrentCulture = new CultureInfo("hu-HU");
            }
            else if (g4.IsChecked == true)
            {
                TranslationManager.Instance.CurrentLanguage = new CultureInfo("en-US");

                //App.TranslationSource("hu-HU");
                //TranslationSource.Instance.CurrentCulture = new CultureInfo("hu-HU");
            }
            _EventCall_ChangeLanguage();
        }

        public void GetBoothState()
        {
            lblCurBoothState.Content = TranslationManager.Instance.Translate("한국어").ToString();
        }


        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //x버튼 삭제
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }
    }
}
