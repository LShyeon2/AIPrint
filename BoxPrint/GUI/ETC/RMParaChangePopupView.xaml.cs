using BoxPrint.DataList;
using System;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using TranslationByMarkupExtension;

namespace BoxPrint.GUI.ETC
{
    /// <summary>
    /// RobotSpeedPopupView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class RMParaChangePopupView : Window
    {

        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private SortingRecClass mSortVal;
        private string mRmNmae;
        private string mtag;



        public RMParaChangePopupView()
        {
            InitializeComponent();
            ViewLoaded();
        }

        public RMParaChangePopupView(SortingRecClass sortVal, string RMname, string strtag)
        {
            InitializeComponent();
            textChangeValue.PreviewKeyDown += textChangeValue_PreviewKeyDown;
            textChangeValue.PreviewTextInput += TextChangeValue_PreviewTextInput;

            this.mtag = strtag;
            mRmNmae = RMname;
            mSortVal = sortVal;
            ViewLoaded();

        }



        private void TextChangeValue_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void textChangeValue_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            //if (!Char.IsDigit((char)KeyInterop.VirtualKeyFromKey(e.Key)) & e.Key != Key.Back | e.Key == Key.Space)
            //{
            //    e.Handled = true;
            //    MessageBox.Show("숫자만 입력해주세요.", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
            //}
        }

        private void ViewLoaded()
        {
            lbCurParaName.Text = mSortVal.Item1.ToString(); // ParaName 
            lbDescription.Text = mSortVal.Item3.ToString(); // Description 
            textCurParaValue.Text = mSortVal.Item4.ToString(); // Para Value 
            textChangeValue.Text = mSortVal.Item4.ToString(); // Change Value 
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            string msg = TranslationManager.Instance.Translate("Parameter 변경 메시지").ToString();
            string strMessage = string.Format(msg, mRmNmae, mSortVal.Item4, textChangeValue.Text);
            MessageBoxResult result = System.Windows.MessageBox.Show(strMessage, TranslationManager.Instance.Translate("Info Message").ToString(), MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                ParameterSave(this.mtag);
            }
        }

        private void ParameterSave(string tag)
        {
            string tmpPath = string.Empty;
            //switch (tag)
            //{
            //    case "RMParameter":
            //         tmpPath = string.Format(GlobalData.Current.CurrentFilePaths(GlobalData.Current.FullPath) + "\\Data\\RM\\Parameter_{0}.xml", mRmNmae);
            //        var itemRMParameter = GlobalData.Current.mRMManager[mRmNmae].nParameterList.Where(r => r.TagName == mSortVal.Item1).FirstOrDefault();

            //        if (itemRMParameter != null)
            //        {
            //            itemRMParameter.Note = textChangeValue.Text;
            //            PMacDataList.Serialize(tmpPath, GlobalData.Current.mRMManager[mRmNmae].nParameterList);
            //        }
            //        break;
            //    case "RMPval":
            //         tmpPath = string.Format(GlobalData.Current.CurrentFilePaths(GlobalData.Current.FullPath) + "\\Data\\RM\\DataValue_{0}.xml", mRmNmae);
            //        var itemRMPval = GlobalData.Current.mRMManager[mRmNmae].nPValue.Where(r => r.TagName == mSortVal.Item1).FirstOrDefault();

            //        if (itemRMPval != null)
            //        {
            //            itemRMPval.Note = textChangeValue.Text;
            //            PMacDataList.Serialize(tmpPath, GlobalData.Current.mRMManager[mRmNmae].nPValue);
            //        }
            //        break;
            //    case "AxisState":

            //    default:
            //        break;
            //}

            GlobalData.Current.RaiseParaChangeEvent();
        }



        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            ViewLoaded();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //x버튼 삭제
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }
    }
}
