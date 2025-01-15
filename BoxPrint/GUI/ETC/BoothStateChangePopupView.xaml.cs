using BoxPrint.Log;
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
    public partial class BoothStateChangePopupView : Window
    {

        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x80000;
        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        private bool bSaveClick = false;

        public BoothStateChangePopupView()
        {
            InitializeComponent();
            
            ViewLoaded();

        }
        private void ViewLoaded()
        {
            //SuHwan_20230120 : [1차 UI검수]
            //foreach (string suit in Enum.GetNames(typeof(eSCState)))
            //{
            //    cbSCState.Items.Add(suit);
            //}
            cbSCState.Items.Add(TranslationManager.Instance.Translate(eSCState.PAUSED.ToString()).ToString());
            cbSCState.Items.Add(TranslationManager.Instance.Translate(eSCState.AUTO.ToString()).ToString());

            if(GlobalData.Current.MainBooth.SCState != eSCState.AUTO) cbSCState.SelectedIndex = 0;
            else cbSCState.SelectedIndex = 1;
            
            GetBoothState();
        }

        public void GetBoothState()
        {
            lblCurBoothState.Content = TranslationManager.Instance.Translate(GlobalData.Current.MainBooth.SCState.ToString()).ToString();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                if (!bSaveClick)
                {
                    LogManager.WriteOperatorLog(string.Format("사용자가 {0} 화면에서 {1} 을/를 Click하였습니다.", this.Tag.ToString(), btnClose.Tag),
                        "CLOSE", this.Tag.ToString(), GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 3,
                        this.Tag.ToString(), btnClose.Tag);
                }
                this.Close();
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
           
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                string bufSCState = string.Empty;
                switch (cbSCState.SelectedIndex)
                {
                    case 0:
                        bufSCState = eSCState.PAUSED.ToString();
                        break;
                    case 1:
                        bufSCState = eSCState.AUTO.ToString();
                        break;
                }

                string strMessage = TranslationManager.Instance.Translate("BoothState 변경 메시지").ToString();
                strMessage = string.Format(strMessage,
                                           TranslationManager.Instance.Translate(GlobalData.Current.MainBooth.SCState.ToString()).ToString(),
                                           TranslationManager.Instance.Translate(bufSCState).ToString());

                //SuHwan_20230320 : 메시지 박스 통합
                MessageBoxPopupView msgbox = new MessageBoxPopupView("Info Message", "", strMessage, "", MessageBoxButton.YesNo, MessageBoxImage.Question, "BoothState 변경 메시지", GlobalData.Current.MainBooth.SCState.ToString(), bufSCState, true);
                CustomMessageBoxResult mBoxResult = msgbox.ShowResult();

                //MessageBoxResult result = System.Windows.MessageBox.Show(strMessage, "Info Message", MessageBoxButton.YesNo);
                if (mBoxResult.Result == MessageBoxResult.Yes)
                {
                    //SuHwan_20230120 : [1차 UI검수]
                    //eSCState SelectedState = (eSCState)Enum.Parse(typeof(eSCState), cbSCState.SelectedItem.ToString());
                    bSaveClick = true;

                    eSCState SelectedState = eSCState.NONE;
                    if (bufSCState == "AUTO")
                        SelectedState = eSCState.AUTO;
                    else if (bufSCState == "PAUSED")
                        SelectedState = eSCState.PAUSED;

                    //230222 조숭진 클라이언트가 시스템상태변경 요청 s
                    if (GlobalData.Current.ServerClientType == eServerClientType.Client)
                    {
                        GlobalData.Current.DBManager.DbSetProcedureClientReq(GlobalData.Current.EQPID, "SYSTEMSTATE", "BOOTH", string.Empty, bufSCState, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"), eServerClientType.Client);

                        LogManager.WriteOperatorLog(string.Format("사용자가 {0} 화면에서 {1} 을/를 Click하였습니다. {2}", this.Tag.ToString(), btnSave.Tag, bufSCState),
                            "SAVE", btnSave.Tag, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 2,
                            this.Tag.ToString(), btnSave.Tag, bufSCState);

                        btnClose_Click(this, null);
                        return;
                    }
                    //230222 조숭진 클라이언트가 시스템상태변경 요청 e

                    if (SelectedState == eSCState.PAUSED || SelectedState == eSCState.PAUSING)
                    {
                        GlobalData.Current.MainBooth.SCSPauseCommand();
                    }
                    else if (SelectedState == eSCState.AUTO)
                    {
                        GlobalData.Current.MainBooth.SCSResumeCommand();
                    }
                    else
                    {
                        GlobalData.Current.MainBooth.SCState = SelectedState;
                    }
                    GetBoothState();

                    bufSCState = SelectedState.ToString();

                    LogManager.WriteOperatorLog(string.Format("사용자가 {0} 화면에서 {1} 을/를 Click하였습니다. {2}", this.Tag.ToString(), btnSave.Tag, bufSCState),
                        "SAVE", btnSave.Tag, GlobalData.Current.CurrentIP, string.Empty, GlobalData.Current.ClientPCName, GlobalData.Current.CurrentUserID, 2,
                        this.Tag.ToString(), btnSave.Tag, bufSCState);

                    btnClose_Click(this, null);
                }
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }

            
        }

        public static T FindEnumValue<T>(string str)
        {
            string[] enums = Enum.GetNames(typeof(T));

            T result = (T)Enum.ToObject(typeof(T), 0);

            for (int i = 0; i < enums.Length; i++)
            {
                if (str == enums[i])
                    result = (T)Enum.ToObject(typeof(T), i);
            }

            return result;
        }

        private void btnRefresh_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                GetBoothState();
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //x버튼 삭제
            var hwnd = new WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            try //240404 RGJ 클릭 이벤트 예외 못잡으면 프로그램 다운 되므로 전부 예외처리함.
            {
                this.Close();
            }
            catch (Exception ex)
            {
                Log.LogManager.WriteConsoleLog(eLogLevel.Info, ex.ToString());
            }
        }

        /// <summary>
        /// 20230613 JIG SCS스탯 변경창 닫기
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        //private void StateChage_Close(object sender, RoutedEventArgs e)
        //{
        //    this.Close();
        //}
    }
}
